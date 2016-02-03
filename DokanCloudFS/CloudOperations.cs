/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using DokanNet;
using FileAccess = DokanNet.FileAccess;
using NLog;
using IgorSoft.DokanCloudFS.IO;

namespace IgorSoft.DokanCloudFS
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class CloudOperations : IDokanOperations
    {
        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        private class StreamContext : IDisposable
        {
            public CloudFileNode File { get; }

            public FileAccess Access { get; }

            public Stream Stream { get; set; }

            public Task Task { get; set; }

            public bool IsLocked { get; set; }

            public StreamContext(CloudFileNode file, FileAccess access)
            {
                File = file;
                Access = access;
            }

            public void Dispose()
            {
                Stream?.Dispose();
            }

            public override string ToString() => DebuggerDisplay;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private string DebuggerDisplay => $"{nameof(StreamContext)} {File.Name} [{Access}] [{nameof(Stream.Length)}={((Stream?.CanSeek ?? false) ? Stream.Length : 0)}] [{nameof(Task.Status)}={Task?.Status}] {nameof(IsLocked)}={IsLocked}";
        }

        private ICloudDrive drive;

        private CloudDirectoryNode root;

        private ILogger logger;

        private static readonly IList<FileInformation> emptyDirectoryDefaultFiles = new[] { ".", ".." }.Select(fileName =>
            new FileInformation() { FileName = fileName, Attributes = FileAttributes.Directory, CreationTime = DateTime.Today, LastWriteTime = DateTime.Today, LastAccessTime = DateTime.Today }
        ).ToList();

        public CloudOperations(ICloudDrive drive, ILogger logger)
        {
            this.drive = drive;
            this.logger = logger;
        }

        private CloudItemNode GetItem(string fileName)
        {
            var result = root ?? (root = new CloudDirectoryNode(drive.GetRoot())) as CloudItemNode;

            var pathSegments = new Queue<string>(fileName.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));

            while (result != null && pathSegments.Count > 0)
                result = (result as CloudDirectoryNode)?.GetChildItemByName(drive, pathSegments.Dequeue());

            return result;
        }

        private string ToTrace(DokanFileInfo info)
        {
            var contextDescriptor = info.Context != null ? $"{info.Context}" : "<null>";
            return $"{{{contextDescriptor}, {info.DeleteOnClose}, {info.IsDirectory}, {info.NoCache}, {info.PagingIo}, {info.ProcessId}, {info.SynchronousIo}, {info.WriteToEndOfFile}}}";
        }

        private NtStatus Trace(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            logger?.Trace($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {method}({fileName}, {ToTrace(info)}{extraParameters}) -> {result}");

            return result;
        }

        private NtStatus Trace(string method, string fileName, DokanFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, NtStatus result)
        {
            logger?.Trace($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {method}({fileName}, {ToTrace(info)}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}");

            return result;
        }

        public void Cleanup(string fileName, DokanFileInfo info)
        {
            if (info.DeleteOnClose) {
                (GetItem(fileName) as CloudFileNode)?.Remove(drive);
            } else if (!info.IsDirectory) {
                var context = info.Context as StreamContext;
                if (context != null && context.Access.HasFlag(FileAccess.WriteData) && (context.Stream?.CanRead ?? false)) {
                    context.Stream.Seek(0, SeekOrigin.Begin);
                    context.Task = Task.Run(() => {
                            try {
                                context.File.SetContent(drive, context.Stream);
                            } catch (Exception ex) {
                                if (!(ex is UnauthorizedAccessException))
                                    context.File.Remove(drive);
                                logger.Trace($"{nameof(context.File.SetContent)} failed on file '{fileName}' with {ex.GetType().Name} '{ex.Message}'");
                                throw;
                            }
                        })
                        .ContinueWith(t => logger.Trace($"{nameof(context.File.SetContent)} finished on file '{fileName}'"), TaskContinuationOptions.OnlyOnRanToCompletion);
                }

                if (context?.Task != null) {
                    context.Task.Wait();

                    Trace(nameof(Cleanup), fileName, info, context.Task.IsCompleted ? DokanResult.Success : DokanResult.Error);
                    context.Dispose();
                    info.Context = null;
                    return;
                }
            }

            Trace(nameof(Cleanup), fileName, info, DokanResult.Success);
        }

        public void CloseFile(string fileName, DokanFileInfo info)
        {
            Trace(nameof(CloseFile), fileName, info, DokanResult.Success);

            var context = info.Context as StreamContext;
            context?.Dispose();
        }

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            // HACK: Fix for Bug in Dokany related to a missing trailing slash for directory names
            if (string.IsNullOrEmpty(fileName))
                fileName = @"\";

            if (fileName == @"\") {
                info.IsDirectory = true;
                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.Success);
            }

            fileName = fileName.TrimEnd(Path.DirectorySeparatorChar);

            var parent = GetItem(Path.GetDirectoryName(fileName)) as CloudDirectoryNode;
            if (parent == null)
                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.PathNotFound);

            var itemName = Path.GetFileName(fileName);
            var item = parent.GetChildItemByName(drive, itemName);
            var fileItem = default(CloudFileNode);
            switch (mode) {
                case FileMode.Create:
                    fileItem = item as CloudFileNode;
                    if (fileItem != null)
                        fileItem.Truncate(drive);
                    else
                        fileItem = parent.NewFileItem(drive, itemName);

                    info.Context = new StreamContext(fileItem, FileAccess.WriteData);

                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.Success);
                case FileMode.Open:
                    fileItem = item as CloudFileNode;
                    if (fileItem != null) {
                        if (access.HasFlag(FileAccess.ReadData))
                            info.Context = new StreamContext(fileItem, FileAccess.ReadData);
                        else if (access.HasFlag(FileAccess.WriteData))
                            info.Context = new StreamContext(fileItem, FileAccess.WriteData);
                        else if (access.HasFlag(FileAccess.Delete))
                            info.Context = new StreamContext(fileItem, FileAccess.Delete);
                        else
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.NotImplemented);
                    }
                    else {
                        info.IsDirectory = item != null;
                    }

                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, item != null ? DokanResult.Success : DokanResult.FileNotFound);
                case FileMode.OpenOrCreate:
                    fileItem = item as CloudFileNode ?? parent.NewFileItem(drive, itemName);

                    if (access.HasFlag(FileAccess.ReadData) && !access.HasFlag(FileAccess.WriteData))
                        info.Context = new StreamContext(fileItem, FileAccess.ReadData);
                    else
                        info.Context = new StreamContext(fileItem, FileAccess.WriteData);

                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.Success);
                case FileMode.CreateNew:
                    if (item != null)
                        return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, info.IsDirectory ? DokanResult.AlreadyExists : DokanResult.FileExists);

                    if (info.IsDirectory) {
                        parent.NewDirectoryItem(drive, itemName);
                    } else {
                        fileItem = parent.NewFileItem(drive, itemName);

                        info.Context = new StreamContext(fileItem, FileAccess.WriteData);
                    }
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.Success);
                case FileMode.Append:
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.NotImplemented);
                case FileMode.Truncate:
                    //fileItem = item as CloudFileNode;
                    //if (fileItem == null)
                    //    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.FileNotFound);

                    //fileItem.Truncate(drive);

                    //info.Context = new StreamContext(fileItem, FileAccess.WriteData);

                    //return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.Success);
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.NotImplemented);
                default:
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.NotImplemented);
            }
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
            var item = GetItem(fileName) as CloudDirectoryNode;
            if (item == null)
                return Trace(nameof(DeleteDirectory), fileName, info, DokanResult.PathNotFound);
            if (item.GetChildItems(drive).Any())
                return Trace(nameof(DeleteDirectory), fileName, info, DokanResult.DirectoryNotEmpty);

            item.Remove(drive);

            return Trace(nameof(DeleteDirectory), fileName, info, DokanResult.Success);
        }

        public NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            ((StreamContext)info.Context).File.Remove(drive);

            return Trace(nameof(DeleteFile), fileName, info, DokanResult.Success);
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            var parent = GetItem(fileName) as CloudDirectoryNode;

            var childItems = parent.GetChildItems(drive).ToList();
            files = childItems.Any()
                ? childItems.Select(i => new FileInformation() {
                    FileName = i.Name, Length = (i as CloudFileNode)?.Contract.Size ?? 0,
                    Attributes = i is CloudDirectoryNode ? FileAttributes.Directory : FileAttributes.ReadOnly | FileAttributes.NotContentIndexed,
                    CreationTime = i.Contract.Created.DateTime, LastWriteTime = i.Contract.Updated.DateTime, LastAccessTime = i.Contract.Updated.DateTime
                }).ToList()
                : emptyDirectoryDefaultFiles;

            return Trace(nameof(FindFiles), fileName, info, DokanResult.Success);
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = Enumerable.Empty<FileInformation>().ToList();
            return Trace(nameof(FindStreams), fileName, info, DokanResult.NotImplemented, $"out [{streams.Count}]");
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            try {
                ((StreamContext)info.Context).Stream?.Flush();

                return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.Success);
            } catch (IOException) {
                return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.DiskFull);
            }
        }

        public NtStatus GetDiskFreeSpace(out long free, out long total, out long used, DokanFileInfo info)
        {
            free = drive.Free ?? 0;
            used = drive.Used ?? 0;
            total = free + used;

            return Trace(nameof(GetDiskFreeSpace), null, info, DokanResult.Success, $"out {free}", $"out {total}", $"out {used}");
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            var item = GetItem(fileName);
            if (item == null) {
                fileInfo = default(FileInformation);
                return Trace(nameof(GetFileInformation), fileName, info, DokanResult.PathNotFound);
            }

            fileInfo = new FileInformation() {
                FileName = fileName, Length = (info.Context as StreamContext)?.Stream?.Length ?? (item as CloudFileNode)?.Contract.Size ?? 0,
                Attributes = item is CloudDirectoryNode ? FileAttributes.Directory : FileAttributes.NotContentIndexed,
                CreationTime = item.Contract.Created.DateTime, LastWriteTime = item.Contract.Updated.DateTime, LastAccessTime = item.Contract.Updated.DateTime
            };

            return Trace(nameof(GetFileInformation), fileName, info, DokanResult.Success, $"out {{{fileInfo.FileName}, [{fileInfo.Length}], [{fileInfo.Attributes}], {fileInfo.CreationTime}, {fileInfo.LastWriteTime}, {fileInfo.LastAccessTime}}}");
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            security = info.IsDirectory
                ? new DirectorySecurity() as FileSystemSecurity
                : new FileSecurity() as FileSystemSecurity;

            return Trace(nameof(GetFileSecurity), fileName, info, DokanResult.Success, $"out {security}", $"{sections}");
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info)
        {
            volumeLabel = drive.DisplayRoot;
            features = FileSystemFeatures.CaseSensitiveSearch | FileSystemFeatures.CasePreservedNames | FileSystemFeatures.UnicodeOnDisk |
                       FileSystemFeatures.PersistentAcls | FileSystemFeatures.SupportsRemoteStorage;
            fileSystemName = nameof(DokanCloudFS);

            return Trace(nameof(GetVolumeInformation), null, info, DokanResult.Success, $"out {volumeLabel}", $"out {features}", $"out {fileSystemName}");
        }

        public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            var context = ((StreamContext)info.Context);
            var result = !context.IsLocked ? DokanResult.Success : DokanResult.AccessDenied;
            context.IsLocked = true;
            return Trace(nameof(LockFile), fileName, info, result, offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus Mounted(DokanFileInfo info)
        {
            return Trace(nameof(Mounted), null, info, DokanResult.Success);
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            var item = GetItem(oldName);
            if (item == null)
                return Trace(nameof(MoveFile), oldName, info, DokanResult.FileNotFound, newName, replace.ToString(CultureInfo.InvariantCulture));

            var destinationDirectory = GetItem(Path.GetDirectoryName(newName)) as CloudDirectoryNode;
            if (destinationDirectory == null)
                return Trace(nameof(MoveFile), oldName, info, DokanResult.PathNotFound, newName, replace.ToString(CultureInfo.InvariantCulture));

            item.Move(drive, Path.GetFileName(newName), destinationDirectory);

            return Trace(nameof(MoveFile), oldName, info, DokanResult.Success, newName, replace.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus OpenDirectory(string fileName, DokanFileInfo info)
        {
            var item = GetItem(fileName) as CloudDirectoryNode;
            if (item == null)
                return Trace(nameof(OpenDirectory), fileName, info, DokanResult.PathNotFound);

            return Trace(nameof(OpenDirectory), fileName, info, DokanResult.Success);
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            var context = (StreamContext)info.Context;

            lock (context) {
                if (context.Stream == null)
                    try {
                        context.Stream = Stream.Synchronized(context.File.GetContent(drive));
                    } catch (Exception ex) {
                        bytesRead = 0;
                        return Trace(nameof(ReadFile), fileName, info, DokanResult.Error, $"out {bytesRead}", offset.ToString(CultureInfo.InvariantCulture), $"{ex.GetType().Name} '{ex.Message}'");
                    }

                context.Stream.Position = offset;
                bytesRead = context.Stream.Read(buffer, 0, buffer.Length);
            }

            return Trace(nameof(ReadFile), fileName, info, DokanResult.Success, $"out {bytesRead}", offset.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            if (length > 0) {
                var scatterStream = default(Stream);
                var gatherStream = default(Stream);
                new ScatterGatherStreamFactory().CreateScatterGatherStreams((int)length, out scatterStream, out gatherStream);

                var context = (StreamContext)info.Context;
                context.Stream = scatterStream;

                context.Task = Task.Run(() => {
                        try {
                            context.File.SetContent(drive, gatherStream);
                        } catch (Exception ex) {
                            if (!(ex is UnauthorizedAccessException))
                                context.File.Remove(drive);
                            logger.Trace($"{nameof(context.File.SetContent)} failed on file '{fileName}' with {ex.GetType().Name} '{ex.Message}'");
                            throw;
                        }
                    })
                    .ContinueWith(t => logger.Trace($"{nameof(context.File.SetContent)} finished on file '{fileName}'"), TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            return Trace(nameof(SetAllocationSize), fileName, info, DokanResult.Success, length.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            return Trace(nameof(SetEndOfFile), fileName, info, DokanResult.Success, length.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            // TODO: Possibly return NotImplemented here
            return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.Success, attributes.ToString());
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.NotImplemented, sections.ToString());
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
        {
            // TODO: Possibly return NotImplemented here
            return Trace(nameof(SetFileTime), fileName, info, DokanResult.Success, creationTime.ToString(), lastAccessTime.ToString(), lastWriteTime.ToString());
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            var context = ((StreamContext)info.Context);
            var result = context.IsLocked ? DokanResult.Success : DokanResult.AccessDenied;
            context.IsLocked = false;
            return Trace(nameof(UnlockFile), fileName, info, DokanResult.Success, offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus Unmounted(DokanFileInfo info)
        {
            var result = Trace(nameof(Unmounted), null, info, DokanResult.Success);

            drive = null;
            logger = null;

            return result;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            var context = ((StreamContext)info.Context);

            lock (context) {
                if (context.Stream == null)
                    context.Stream = Stream.Synchronized(new MemoryStream());

                context.Stream.Position = offset;
                context.Stream.Write(buffer, 0, buffer.Length);
                bytesWritten = (int)(context.Stream.Position - offset);
            }

            return Trace(nameof(WriteFile), fileName, info, DokanResult.Success, $"out {bytesWritten}", offset.ToString(CultureInfo.InvariantCulture));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(CloudOperations)}";
    }
}