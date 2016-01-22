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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Castle.DynamicProxy;
using DokanNet;
using Moq;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class CloudOperationsTests
    {
        private static class NativeMethods
        {
            private const string KERNEL_32_DLL = "kernel32.dll";

            private const string SHELL_32_DLL = "shell32.dll";

            [Flags]
            public enum DesiredAccess : uint
            {
                GENERIC_ALL = 0x10000000,
                GENERIC_EXECUTE = 0x20000000,
                GENERIC_WRITE = 0x40000000,
                GENERIC_READ = 0x80000000
            }

            [Flags]
            public enum ShareMode : uint
            {
                FILE_SHARE_NONE = 0x0,
                FILE_SHARE_READ = 0x1,
                FILE_SHARE_WRITE = 0x2,
                FILE_SHARE_DELETE = 0x4
            }

            public enum CreationDisposition : uint
            {
                CREATE_NEW = 1,
                CREATE_ALWAYS = 2,
                OPEN_EXISTING = 3,
                OPEN_ALWAYS = 4,
                TRUNCATE_EXSTING = 5
            }

            public enum MoveMethod : uint
            {
                FILE_BEGIN = 0,
                FILE_CURRENT = 1,
                FILE_END = 2
            }

            [Flags]
            public enum FlagsAndAttributes : uint
            {
                FILE_ATTRIBUTE_READONLY = 0x0001,
                FILE_ATTRIBUTE_HIDDEN = 0x0002,
                FILE_ATTRIBUTE_SYSTEM = 0x0004,
                FILE_ATTRIBUTE_ARCHIVE = 0x0020,
                FILE_ATTRIBUTE_NORMAL = 0x0080,
                FILE_ATTRIBUTE_TEMPORARY = 0x100,
                FILE_ATTRIBUTE_OFFLINE = 0x1000,
                FILE_ATTRIBUTE_ENCRYPTED = 0x4000,
                FILE_FLAG_OPEN_NO_RECALL = 0x00100000,
                FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
                FILE_FLAG_SESSION_AWARE = 0x00800000,
                FILE_FLAG_POSIX_SEMANTICS = 0x01000000,
                FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,
                FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,
                FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000,
                FILE_FLAG_RANDOM_ACCESS = 0x10000000,
                FILE_FLAG_NO_BUFFERING = 0x20000000,
                FILE_FLAG_OVERLAPPED = 0x40000000,
                FILE_FLAG_WRITE_THROUGH = 0x80000000
            }

            [DllImport(KERNEL_32_DLL, SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
            private static extern SafeFileHandle CreateFile(string lpFileName, DesiredAccess dwDesiredAccess, ShareMode dwShareMode, IntPtr lpSecurityAttributes, CreationDisposition dwCreationDisposition, FlagsAndAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

            [DllImport(KERNEL_32_DLL, SetLastError = true)]
            private static extern bool ReadFileEx(SafeFileHandle hFile, byte[] lpBuffer, int nNumberOfBytesToRead, ref NativeOverlapped lpOverlapped, FileIOCompletionRoutine lpCompletionRoutine);

            [DllImport(KERNEL_32_DLL, SetLastError = true)]
            private static extern bool SetEndOfFile(SafeFileHandle hFile);

            [DllImport(KERNEL_32_DLL, SetLastError = true)]
            private static extern int SetFilePointer(SafeFileHandle hFile, int lDistanceToMove, out int lpDistanceToMoveHigh, MoveMethod dwMoveMethod);

            [DllImport(KERNEL_32_DLL, SetLastError = true)]
            private static extern bool WriteFileEx(SafeFileHandle hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, ref NativeOverlapped lpOverlapped, FileIOCompletionRoutine lpCompletionRoutine);

            private delegate void FileIOCompletionRoutine(int dwErrorCode, int dwNumberOfBytesTransfered, ref NativeOverlapped lpOverlapped);

            [DebuggerDisplay("{DebuggerDisplay(),nq}")]
            internal class OverlappedChunk
            {
                public byte[] Buffer { get; }

                public int BytesTransferred { get; set; }

                public int Win32Error { get; set; }

                public OverlappedChunk(int count) : this(new byte[count])
                {
                }

                public OverlappedChunk(byte[] buffer)
                {
                    Buffer = buffer;
                    BytesTransferred = 0;
                    Win32Error = 0;
                }

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for debugging only")]
                private string DebuggerDisplay() => $"{nameof(OverlappedChunk)} Buffer={Buffer?.Length ?? -1} BytesTransferred={BytesTransferred}";
            }

            internal static int BufferSize(long bufferSize, long fileSize, int chunks) => (int)Math.Min(bufferSize, fileSize - chunks * bufferSize);

            internal static int NumberOfChunks(long bufferSize, long fileSize)
            {
                var remainder = default(long);
                var quotient = Math.DivRem(fileSize, bufferSize, out remainder);
                return (int)quotient + (remainder > 0 ? 1 : 0);
            }

            internal static OverlappedChunk[] ReadEx(string fileName, long bufferSize, long fileSize)
            {
                var chunks = Enumerable.Range(0, NumberOfChunks(bufferSize, fileSize))
                    .Select(i => new OverlappedChunk(BufferSize(bufferSize, fileSize, i)))
                    .ToArray();
                var waitHandles = Enumerable.Repeat<Func<EventWaitHandle>>(() => new ManualResetEvent(false), chunks.Length).Select(e => e()).ToArray();
                var completions = Enumerable.Range(0, (int)(fileSize / bufferSize + 1)).Select<int, FileIOCompletionRoutine>(i => (int dwErrorCode, int dwNumberOfBytesTransferred, ref NativeOverlapped lpOverlapped) =>
                {
                    chunks[i].Win32Error = dwErrorCode;
                    chunks[i].BytesTransferred = dwNumberOfBytesTransferred;
                    waitHandles[i].Set();
                }).ToArray();

                var awaiterThread = new Thread(new ThreadStart(() => WaitHandle.WaitAll(waitHandles)));
                awaiterThread.Start();

                using (var handle = CreateFile(fileName, DesiredAccess.GENERIC_READ, ShareMode.FILE_SHARE_READ | ShareMode.FILE_SHARE_DELETE, IntPtr.Zero, CreationDisposition.OPEN_EXISTING, FlagsAndAttributes.FILE_FLAG_NO_BUFFERING | FlagsAndAttributes.FILE_FLAG_OVERLAPPED, IntPtr.Zero)) {
                    for (int i = 0; i < chunks.Length; ++i) {
                        var offset = i * bufferSize;
                        var overlapped = new NativeOverlapped() { OffsetHigh = (int)(offset >> 32), OffsetLow = (int)(offset & 0xffffffff), EventHandle = IntPtr.Zero };

                        if (!ReadFileEx(handle, chunks[i].Buffer, BufferSize(bufferSize, fileSize, i), ref overlapped, completions[i]))
                            chunks[i].Win32Error = Marshal.GetLastWin32Error();
                    }
                }

                awaiterThread.Join();

                Array.ForEach(completions, c => GC.KeepAlive(c));

                return chunks;
            }

            internal static void WriteEx(string fileName, long bufferSize, long fileSize, OverlappedChunk[] chunks)
            {
                var waitHandles = Enumerable.Repeat<Func<EventWaitHandle>>(() => new ManualResetEvent(false), chunks.Length).Select(e => e()).ToArray();
                var completions = Enumerable.Range(0, NumberOfChunks(bufferSize, fileSize)).Select<int, FileIOCompletionRoutine>(i => (int dwErrorCode, int dwNumberOfBytesTransferred, ref NativeOverlapped lpOverlapped) =>
                {
                    chunks[i].Win32Error = dwErrorCode;
                    chunks[i].BytesTransferred = dwNumberOfBytesTransferred;
                    waitHandles[i].Set();
                }).ToArray();

                var awaiterThread = new Thread(new ThreadStart(() => WaitHandle.WaitAll(waitHandles)));
                awaiterThread.Start();

                using (var handle = CreateFile(fileName, DesiredAccess.GENERIC_WRITE, ShareMode.FILE_SHARE_NONE, IntPtr.Zero, CreationDisposition.OPEN_ALWAYS, FlagsAndAttributes.FILE_FLAG_NO_BUFFERING | FlagsAndAttributes.FILE_FLAG_OVERLAPPED, IntPtr.Zero)) {
                    var offsetHigh = (int)(fileSize >> 32);
                    if (SetFilePointer(handle, (int)(fileSize & 0xffffffff), out offsetHigh, MoveMethod.FILE_BEGIN) != fileSize || offsetHigh != (int)(fileSize >> 32) || !SetEndOfFile(handle)) {
                        chunks[0].Win32Error = Marshal.GetLastWin32Error();
                        return;
                    }

                    for (int i = 0; i < chunks.Length; ++i) {
                        var offset = i * bufferSize;
                        var overlapped = new NativeOverlapped() { OffsetHigh = (int)(offset >> 32), OffsetLow = (int)(offset & 0xffffffff), EventHandle = IntPtr.Zero };

                        if (!WriteFileEx(handle, chunks[i].Buffer, BufferSize(bufferSize, fileSize, i), ref overlapped, completions[i]))
                            chunks[i].Win32Error = Marshal.GetLastWin32Error();
                    }
                }

                awaiterThread.Join();

                Array.ForEach(completions, c => GC.KeepAlive(c));
            }
        }

        private class RetargetingInterceptor<TInterface> : IInterceptor
        {
            private TInterface invocationTarget;

            public void RedirectInvocationsTo(TInterface invocationTarget)
            {
                this.invocationTarget = invocationTarget;
            }

            public void Intercept(IInvocation invocation)
            {
                if (!object.Equals(invocation.InvocationTarget, invocationTarget)) {
                    var changeProxyTarget = (IChangeProxyTarget)invocation;
                    changeProxyTarget.ChangeInvocationTarget(invocationTarget);
                    changeProxyTarget.ChangeProxyTarget(invocationTarget);
                }

                invocation.Proceed();
            }
        }

        internal class Fixture : IDisposable
        {
            public const string MOUNT_POINT = "Z:";

            //public const string VOLUME_LABEL = "Dokan Volume";

            public const string SCHEMA = "mock";

            public const string USER_NAME = "IgorDev";

            private const long freeSpace = 64 * 1 << 20;

            private const long usedSpace = 36 * 1 << 20;

            private static RootDirectoryInfoContract rootDirectory = new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), new DateTime(2016, 1, 1), new DateTime(2016, 1, 1)) { Drive = new DriveInfoContract(MOUNT_POINT, freeSpace, usedSpace) };

            private static SHA1 sha1 = SHA1.Create();

            private IDokanOperations operations;

            private NLog.ILogger logger;

            private RetargetingInterceptor<IDokanOperations> interceptor = new RetargetingInterceptor<IDokanOperations>();

            private Thread mounterThread;

            internal Mock<ICloudDrive> Drive { get; private set; }

            public FileSystemInfoContract[] RootDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir", "SubDir", ToDateTime("2015-01-01 10:11:12"), ToDateTime("2015-01-01 20:21:22")),
                new DirectoryInfoContract(@"\SubDir2", "SubDir2", ToDateTime("2015-01-01 13:14:15"), ToDateTime("2015-01-01 23:24:25")),
                new FileInfoContract(@"\File.ext", "File.ext", ToDateTime("2015-01-02 10:11:12"), ToDateTime("2015-01-02 20:21:22"), 16384, GetHash("16384")),
                new FileInfoContract(@"\SecondFile.ext", "SecondFile.ext", ToDateTime("2015-01-03 10:11:12"), ToDateTime("2015-01-03 20:21:22"), 32768, GetHash("32768")),
                new FileInfoContract(@"\ThirdFile.ext", "ThirdFile.ext", ToDateTime("2015-01-04 10:11:12"), ToDateTime("2015-01-04 20:21:22"), 65536, GetHash("65536"))
            };

            public FileSystemInfoContract[] SubDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir\SubSubDir", "SubSubDir", ToDateTime("2015-02-01 10:11:12"), ToDateTime("2015-02-01 20:21:22")),
                new FileInfoContract(@"\SubDir\SubFile.ext", "SubFile.ext", ToDateTime("2015-02-02 10:11:12"), ToDateTime("2015-02-02 20:21:22"), 981256915, GetHash("981256915")),
                new FileInfoContract(@"\SubDir\SecondSubFile.ext", "SecondSubFile.ext", ToDateTime("2015-02-03 10:11:12"), ToDateTime("2015-02-03 20:21:22"), 30858025, GetHash("30858025")),
                new FileInfoContract(@"\SubDir\ThirdSubFile.ext", "ThirdSubFile.ext", ToDateTime("2015-02-04 10:11:12"), ToDateTime("2015-02-04 20:21:22"), 45357, GetHash("45357"))
            };

            public FileSystemInfoContract[] SubDirectory2Items { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir2\SubSubDir2", "SubSubDir2", ToDateTime("2015-02-01 10:11:12"), ToDateTime("2015-02-01 20:21:22")),
                new FileInfoContract(@"\SubDir2\SubFile2.ext", "SubFile2.ext", ToDateTime("2015-02-02 10:11:12"), ToDateTime("2015-02-02 20:21:22"), 981256915, GetHash("981256915")),
                new FileInfoContract(@"\SubDir2\SecondSubFile2.ext", "SecondSubFile2.ext", ToDateTime("2015-02-03 10:11:12"), ToDateTime("2015-02-03 20:21:22"), 30858025, GetHash("30858025")),
                new FileInfoContract(@"\SubDir2\ThirdSubFile2.ext", "ThirdSubFile2.ext", ToDateTime("2015-02-04 10:11:12"), ToDateTime("2015-02-04 20:21:22"), 45357, GetHash("45357"))
            };

            public FileSystemInfoContract[] SubSubDirectoryItems { get; } = new FileSystemInfoContract[] {
                new FileInfoContract(@"\SubDir\SubSubDir\SubSubFile.ext", "SubSubFile.ext", ToDateTime("2015-03-01 10:11:12"), ToDateTime("2015-03-01 20:21:22"), 7198265, GetHash("7198265")),
                new FileInfoContract(@"\SubDir\SubSubDir\SecondSubSubFile.ext", "SecondSubSubFile.ext", ToDateTime("2015-03-02 10:11:12"), ToDateTime("2015-03-02 20:21:22"), 5555, GetHash("5555")),
                new FileInfoContract(@"\SubDir\SubSubDir\ThirdSubSubFile.ext", "ThirdSubSubFile.ext", ToDateTime("2015-03-03 10:11:12"), ToDateTime("2015-03-03 20:21:22"), 102938576, GetHash("102938576"))
            };

            public static Fixture Initialize() => new Fixture();

            private Fixture()
            {
                operations = new ProxyGenerator().CreateInterfaceProxyWithTargetInterface<IDokanOperations>(null, interceptor);

                var loggerMock = new Mock<NLog.ILogger>();
                loggerMock.Setup(l => l.Trace(It.IsAny<string>())).Callback((string message) => Console.WriteLine(message));
                logger = loggerMock.Object;

                Reset();
                SetupGetRoot();

                (mounterThread = new Thread(new ThreadStart(() => operations.Mount(MOUNT_POINT, DokanOptions.DebugMode | DokanOptions.RemovableDrive, 5, 800, TimeSpan.FromMinutes(5))))).Start();
                var drive = new DriveInfo(MOUNT_POINT);
                while (!drive.IsReady)
                    Thread.Sleep(50);
            }

            internal void Reset()
            {
                Drive = new Mock<ICloudDrive>(MockBehavior.Strict);

                interceptor.RedirectInvocationsTo(new CloudOperations(Drive.Object, logger));

                foreach (var directory in RootDirectoryItems.OfType<DirectoryInfoContract>())
                    directory.Parent = rootDirectory;
                foreach (var file in RootDirectoryItems.OfType<FileInfoContract>())
                    file.Directory = rootDirectory;
            }

            internal DriveInfo GetDriveInfo() => new DriveInfo(MOUNT_POINT);

            internal void SetupGetRoot()
            {
                Drive
                    .Setup(d => d.GetRoot())
                    .Returns(rootDirectory);
            }

            internal void SetupGetDisplayRoot()
            {
                Drive
                    .SetupGet(d => d.DisplayRoot)
                    .Returns((new RootName(SCHEMA, USER_NAME, MOUNT_POINT)).Value);
            }

            internal void SetupGetRootDirectoryItems(IEnumerable<FileSystemInfoContract> items = null)
            {
                SetupGetRoot();

                Drive
                    .Setup(drive => drive.GetChildItem(It.Is<DirectoryInfoContract>(directory => directory.Id.Value == Path.DirectorySeparatorChar.ToString())))
                    .Returns(items ?? RootDirectoryItems);
            }

            internal void SetupGetSubDirectory2Items(IEnumerable<FileSystemInfoContract> items = null)
            {
                Drive
                    .Setup(drive => drive.GetChildItem(It.Is<DirectoryInfoContract>(directory => directory.Id.Value == @"\SubDir2")))
                    .Returns(items ?? SubDirectory2Items);
            }

            internal void SetupGetEmptyDirectoryItems(string directoryId)
            {
                Drive
                    .Setup(drive => drive.GetChildItem(It.Is<DirectoryInfoContract>(directory => directory.Id.Value == directoryId)))
                    .Returns(Enumerable.Empty<DirectoryInfoContract>());
            }

            internal DirectoryInfoContract SetupNewDirectory(string parentName, string directoryName)
            {
                var parentId = new DirectoryId(parentName);
                var directory = new DirectoryInfoContract($"{parentId.Value}{directoryName}\\", directoryName, ToDateTime("2016-01-01 12:00:00"), ToDateTime("2016-01-01 12:00:00"));
                Drive
                    .Setup(drive => drive.NewDirectoryItem(It.Is<DirectoryInfoContract>(parent => parent.Id == parentId), directoryName))
                    .Returns(directory);
                return directory;
            }

            internal FileInfoContract SetupNewFile(string parentId, string fileName)
            {
                return SetupNewFile(new DirectoryId(parentId), fileName);
            }

            internal FileInfoContract SetupNewFile(DirectoryId parentId, string fileName)
            {
                var file = new FileInfoContract($"{parentId.Value.TrimEnd('\\')}\\{fileName}", fileName, ToDateTime("2016-02-01 12:00:00"), ToDateTime("2016-02-01 12:00:00"), 0, null);
                Drive
                    .Setup(drive => drive.NewFileItem(It.Is<DirectoryInfoContract>(parent => parent.Id == parentId), fileName, It.Is<Stream>(s => s.Length == 0)))
                    .Returns(file);
                return file;
            }

            internal void SetupGetFileContent(FileInfoContract file, string content)
            {
                Drive
                    .Setup(drive => drive.GetContent(It.Is<FileInfoContract>(f => f.Id == file.Id)))
                    .Returns(!string.IsNullOrEmpty(content) ? new MemoryStream(Encoding.Default.GetBytes(content)) : new MemoryStream());
            }

            internal void SetupSetFileContent(FileInfoContract file, string content)
            {
                Drive
                    .Setup(drive => drive.SetContent(It.Is<FileInfoContract>(f => f.Id == file.Id), It.Is<Stream>(s => Contains(s, content))));
            }

            internal void SetupGetFileContentWithError(FileInfoContract file)
            {
                Drive
                    .Setup(drive => drive.GetContent(It.Is<FileInfoContract>(f => f.Id == file.Id)))
                    .Throws(new IOException("Error during GetContent"));
            }

            internal void SetupSetFileContentWithError(FileInfoContract file, string content)
            {
                Drive
                    .Setup(drive => drive.SetContent(It.Is<FileInfoContract>(f => f.Id == file.Id), It.Is<Stream>(s => Contains(s, content))))
                    .Throws(new IOException("Error during SetContent"));
            }

            internal void SetupDeleteDirectoryOrFile(FileSystemInfoContract directoryOrFile, bool recurse = false)
            {
                Drive
                    .Setup(drive => drive.RemoveItem(It.Is<FileSystemInfoContract>(item => item.Id == directoryOrFile.Id), recurse));
            }

            internal void SetupMoveDirectoryOrFile(FileSystemInfoContract directoryOrFile, DirectoryInfoContract target)
            {
                SetupMoveItem(directoryOrFile, directoryOrFile.Name, target);
            }

            internal void SetupRenameDirectoryOrFile(FileSystemInfoContract directoryOrFile, string name)
            {
                SetupMoveItem(directoryOrFile, name, (directoryOrFile as DirectoryInfoContract)?.Parent ?? (directoryOrFile as FileInfoContract)?.Directory ?? null);
            }

            private void SetupMoveItem(FileSystemInfoContract directoryOrFile, string name, DirectoryInfoContract target)
            {
                Drive
                    .Setup(drive => drive.MoveItem(It.Is<FileSystemInfoContract>(item => item.Id == directoryOrFile.Id), name, target))
                    .Returns((FileSystemInfoContract source, string movePath, DirectoryInfoContract destination) => {
                        var directorySource = source as DirectoryInfoContract;
                        if (directorySource != null)
                            return new DirectoryInfoContract(source.Id.Value, movePath, source.Created, source.Updated) { Parent = target };
                        var fileSource = source as FileInfoContract;
                        if (fileSource != null)
                            return new FileInfoContract(source.Id.Value, movePath, source.Created, source.Updated, fileSource.Size, fileSource.Hash) { Directory = target };
                        throw new InvalidOperationException($"Unsupported type '{source.GetType().Name}'");
                    });
            }

            internal static bool Contains(Stream stream, string content)
            {
                using (var reader = new StreamReader(stream)) {
                    return reader.ReadToEnd() == content;
                }
            }

            internal static int BufferSize(long bufferSize, long fileSize, int chunks) => (int)Math.Min(bufferSize, fileSize - chunks * bufferSize);

            internal static int NumberOfChunks(long bufferSize, long fileSize)
            {
                var remainder = default(long);
                var quotient = Math.DivRem(fileSize, bufferSize, out remainder);
                return (int)quotient + (remainder > 0 ? 1 : 0);
            }

            public static string GetHash(string value) => GetHash(Encoding.Default.GetBytes(value));

            public static string GetHash(byte[] value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var hashCode = sha1.ComputeHash(value);

                return BitConverter.ToString(hashCode).Replace("-", string.Empty);
            }

            private static DateTimeOffset ToDateTime(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

            public void Dispose()
            {
                mounterThread.Abort();
                Dokan.Unmount(MOUNT_POINT[0]);
                Dokan.RemoveMountPoint(MOUNT_POINT);
            }
        }
    }
}