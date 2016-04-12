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
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Castle.DynamicProxy;
using DokanNet;
using Moq;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class FileSystemTests
    {
        private static class NativeMethods
        {
            private const string KERNEL_32_DLL = "kernel32.dll";

            private const string SHELL_32_DLL = "shell32.dll";

            [Flags]
            public enum DesiredAccess : uint
            {
                FILE_READ_DATA = 0x0001,
                FILE_WRITE_DATA = 0x0002,
                FILE_APPEND_DATA = 0x0004,
                FILE_READ_EA = 0x0008,
                FILE_WRITE_EA = 0x0010,
                FILE_EXECUTE = 0x0020,
                FILE_READ_ATTRIBUTES = 0x0080,
                FILE_WRITE_ATTRIBUTES = 0x0100,
                DELETE = 0x10000,
                READ_CONTROL = 0x20000,
                WRITE_DAC = 0x40000,
                WRITE_OWNER = 0x80000,
                SYNCHRONIZE = 0x100000,
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
                TRUNCATE_EXISTING = 5
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
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ReadFileEx(SafeFileHandle hFile, byte[] lpBuffer, int nNumberOfBytesToRead, ref NativeOverlapped lpOverlapped, FileIOCompletionRoutine lpCompletionRoutine);

            [DllImport(KERNEL_32_DLL, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetEndOfFile(SafeFileHandle hFile);

            [DllImport(KERNEL_32_DLL, SetLastError = true)]
            private static extern int SetFilePointer(SafeFileHandle hFile, int lDistanceToMove, out int lpDistanceToMoveHigh, MoveMethod dwMoveMethod);

            [DllImport(KERNEL_32_DLL, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, out int lpNumberOfBytesWritten, ref NativeOverlapped lpOverlapped);

            [DllImport(KERNEL_32_DLL, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
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
                private string DebuggerDisplay() => $"{nameof(OverlappedChunk)} Buffer={Buffer?.Length ?? -1} BytesTransferred={BytesTransferred}".ToString(CultureInfo.CurrentCulture);
            }

            internal static int BufferSize(long bufferSize, long fileSize, int chunks) => (int)Math.Min(bufferSize, fileSize - chunks * bufferSize);

            internal static int NumberOfChunks(long bufferSize, long fileSize)
            {
                var remainder = default(long);
                var quotient = Math.DivRem(fileSize, bufferSize, out remainder);
                return (int)quotient + (remainder > 0 ? 1 : 0);
            }

            internal static bool AppendTo(string fileName, byte[] content, out int bytesWritten)
            {
                var overlapped = default(NativeOverlapped);
                using (var handle = CreateFile(fileName, DesiredAccess.FILE_APPEND_DATA, ShareMode.FILE_SHARE_NONE, IntPtr.Zero, CreationDisposition.OPEN_EXISTING, FlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, IntPtr.Zero)) {
                    return WriteFile(handle, content, content.Length, out bytesWritten, ref overlapped);
                }
            }

            internal static bool Truncate(string fileName, byte[] content, out int bytesWritten)
            {
                var overlapped = default(NativeOverlapped);
                using (var handle = CreateFile(fileName, DesiredAccess.GENERIC_WRITE, ShareMode.FILE_SHARE_NONE, IntPtr.Zero, CreationDisposition.TRUNCATE_EXISTING, FlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, IntPtr.Zero)) {
                    return WriteFile(handle, content, content.Length, out bytesWritten, ref overlapped);
                }
            }

            internal static OverlappedChunk[] ReadEx(string fileName, long bufferSize, long fileSize)
            {
                var numberOfChunks = NumberOfChunks(bufferSize, fileSize);
                var chunks = Enumerable.Range(0, numberOfChunks)
                    .Select(i => new OverlappedChunk(BufferSize(bufferSize, fileSize, i)))
                    .ToArray();
                var waitHandles = Enumerable.Repeat<Func<EventWaitHandle>>(() => new ManualResetEvent(false), chunks.Length).Select(e => e()).ToArray();
                var completions = Enumerable.Range(0, numberOfChunks).Select<int, FileIOCompletionRoutine>(i => (int dwErrorCode, int dwNumberOfBytesTransferred, ref NativeOverlapped lpOverlapped) =>
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

                var finishedNormally = awaiterThread.Join(TimeSpan.FromSeconds(1));

                Array.ForEach(completions, GC.KeepAlive);

                if (!finishedNormally)
                    throw new TimeoutException($"{nameof(ReadFileEx)} completions timed out".ToString(CultureInfo.CurrentCulture));

                return chunks;
            }

            internal static void WriteEx(string fileName, long bufferSize, long fileSize, OverlappedChunk[] chunks)
            {
                var numberOfChunks = NumberOfChunks(bufferSize, fileSize);
                var waitHandles = Enumerable.Repeat<Func<EventWaitHandle>>(() => new ManualResetEvent(false), chunks.Length).Select(e => e()).ToArray();
                var completions = Enumerable.Range(0, numberOfChunks).Select<int, FileIOCompletionRoutine>(i => (int dwErrorCode, int dwNumberOfBytesTransferred, ref NativeOverlapped lpOverlapped) =>
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

                Array.ForEach(completions, GC.KeepAlive);
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
                if (invocation == null)
                    throw new ArgumentNullException(nameof(invocation));

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

            private static readonly RootDirectoryInfoContract rootDirectory = new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), new DateTime(2016, 1, 1), new DateTime(2016, 1, 1)) { Drive = new DriveInfoContract(MOUNT_POINT, freeSpace, usedSpace) };

            private readonly IDokanOperations operations;

            private readonly NLog.ILogger logger;

            private readonly RetargetingInterceptor<IDokanOperations> interceptor = new RetargetingInterceptor<IDokanOperations>();

            private readonly Thread mounterThread;

            private Mock<ICloudDrive> drive;

            public FileSystemInfoContract[] RootDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir", "SubDir", "2015-01-01 10:11:12".ToDateTime(), "2015-01-01 20:21:22".ToDateTime()),
                new DirectoryInfoContract(@"\SubDir2", "SubDir2", "2015-01-01 13:14:15".ToDateTime(), "2015-01-01 23:24:25".ToDateTime()),
                new FileInfoContract(@"\File.ext", "File.ext", "2015-01-02 10:11:12".ToDateTime(), "2015-01-02 20:21:22".ToDateTime(), 16384, "16384".ToHash()),
                new FileInfoContract(@"\SecondFile.ext", "SecondFile.ext", "2015-01-03 10:11:12".ToDateTime(), "2015-01-03 20:21:22".ToDateTime(), 32768, "32768".ToHash()),
                new FileInfoContract(@"\ThirdFile.ext", "ThirdFile.ext", "2015-01-04 10:11:12".ToDateTime(), "2015-01-04 20:21:22".ToDateTime(), 65536, "65536".ToHash())
            };

            public FileSystemInfoContract[] SubDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir\SubSubDir", "SubSubDir", "2015-02-01 10:11:12".ToDateTime(), "2015-02-01 20:21:22".ToDateTime()),
                new FileInfoContract(@"\SubDir\SubFile.ext", "SubFile.ext", "2015-02-02 10:11:12".ToDateTime(), "2015-02-02 20:21:22".ToDateTime(), 981256915, "981256915".ToHash()),
                new FileInfoContract(@"\SubDir\SecondSubFile.ext", "SecondSubFile.ext", "2015-02-03 10:11:12".ToDateTime(), "2015-02-03 20:21:22".ToDateTime(), 30858025, "30858025".ToHash()),
                new FileInfoContract(@"\SubDir\ThirdSubFile.ext", "ThirdSubFile.ext", "2015-02-04 10:11:12".ToDateTime(), "2015-02-04 20:21:22".ToDateTime(), 45357, "45357".ToHash())
            };

            public FileSystemInfoContract[] SubDirectory2Items { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir2\SubSubDir2", "SubSubDir2", "2015-02-01 10:11:12".ToDateTime(), "2015-02-01 20:21:22".ToDateTime()),
                new FileInfoContract(@"\SubDir2\SubFile2.ext", "SubFile2.ext", "2015-02-02 10:11:12".ToDateTime(), "2015-02-02 20:21:22".ToDateTime(), 981256915, "981256915".ToHash()),
                new FileInfoContract(@"\SubDir2\SecondSubFile2.ext", "SecondSubFile2.ext", "2015-02-03 10:11:12".ToDateTime(), "2015-02-03 20:21:22".ToDateTime(), 30858025, "30858025".ToHash()),
                new FileInfoContract(@"\SubDir2\ThirdSubFile2.ext", "ThirdSubFile2.ext", "2015-02-04 10:11:12".ToDateTime(), "2015-02-04 20:21:22".ToDateTime(), 45357, "45357".ToHash())
            };

            public FileSystemInfoContract[] SubSubDirectoryItems { get; } = new FileSystemInfoContract[] {
                new FileInfoContract(@"\SubDir\SubSubDir\SubSubFile.ext", "SubSubFile.ext", "2015-03-01 10:11:12".ToDateTime(), "2015-03-01 20:21:22".ToDateTime(), 7198265, "7198265".ToHash()),
                new FileInfoContract(@"\SubDir\SubSubDir\SecondSubSubFile.ext", "SecondSubSubFile.ext", "2015-03-02 10:11:12".ToDateTime(), "2015-03-02 20:21:22".ToDateTime(), 5555, "5555".ToHash()),
                new FileInfoContract(@"\SubDir\SubSubDir\ThirdSubSubFile.ext", "ThirdSubSubFile.ext", "2015-03-03 10:11:12".ToDateTime(), "2015-03-03 20:21:22".ToDateTime(), 102938576, "102938576".ToHash())
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

                (mounterThread = new Thread(new ThreadStart(() => operations.Mount(MOUNT_POINT, DokanOptions.DebugMode | DokanOptions.RemovableDrive, 5, 1000, TimeSpan.FromMinutes(5))))).Start();
                var mountedDrive = GetDriveInfo();
                while (!mountedDrive.IsReady)
                    Thread.Sleep(50);
            }

            public void Reset()
            {
                drive = new Mock<ICloudDrive>(MockBehavior.Strict);

                interceptor.RedirectInvocationsTo(new CloudOperations(drive.Object, logger));

                foreach (var directory in RootDirectoryItems.OfType<DirectoryInfoContract>())
                    directory.Parent = rootDirectory;
                foreach (var file in RootDirectoryItems.OfType<FileInfoContract>())
                    file.Directory = rootDirectory;

                SetupGetDisplayRoot();
            }

            public DriveInfo GetDriveInfo() => new DriveInfo(MOUNT_POINT);

            public void SetupGetRoot()
            {
                drive
                    .Setup(d => d.GetRoot())
                    .Returns(rootDirectory)
                    .Verifiable();
            }

            public void SetupGetDisplayRoot(string root = null)
            {
                var verifies = drive
                    .SetupGet(d => d.DisplayRoot)
                    .Returns(root ?? (new RootName(SCHEMA, USER_NAME, MOUNT_POINT)).Value);

                if (!string.IsNullOrEmpty(root))
                    verifies.Verifiable();
            }

            public void SetupGetFree(long free)
            {
                drive
                    .SetupGet(d => d.Free)
                    .Returns(free)
                    .Verifiable();
            }

            public void SetupGetUsed(long used)
            {
                drive
                    .SetupGet(d => d.Used)
                    .Returns(used)
                    .Verifiable();
            }

            public void SetupGetRootDirectoryItems(IEnumerable<FileSystemInfoContract> items = null)
            {
                SetupGetRoot();

                drive
                    .Setup(drive => drive.GetChildItem(It.Is<DirectoryInfoContract>(directory => directory.Id.Value == Path.DirectorySeparatorChar.ToString())))
                    .Returns(items ?? RootDirectoryItems)
                    .Verifiable();
            }

            public void SetupGetSubDirectory2Items(IEnumerable<FileSystemInfoContract> items = null)
            {
                drive
                    .Setup(drive => drive.GetChildItem(It.Is<DirectoryInfoContract>(directory => directory.Id.Value == @"\SubDir2")))
                    .Returns(items ?? SubDirectory2Items)
                    .Verifiable();
            }

            public void SetupGetSubDirectory2Items(Func<IEnumerable<FileSystemInfoContract>> itemsProvider)
            {
                drive
                    .Setup(drive => drive.GetChildItem(It.Is<DirectoryInfoContract>(directory => directory.Id.Value == @"\SubDir2")))
                    .Returns(() => itemsProvider())
                    .Verifiable();
            }

            public void SetupGetEmptyDirectoryItems(string directoryId)
            {
                drive
                    .Setup(drive => drive.GetChildItem(It.Is<DirectoryInfoContract>(directory => directory.Id.Value == directoryId)))
                    .Returns(Enumerable.Empty<DirectoryInfoContract>())
                    .Verifiable();
            }

            public DirectoryInfoContract SetupNewDirectory(string parentName, string directoryName)
            {
                var parentId = new DirectoryId(parentName);
                var directory = new DirectoryInfoContract($"{parentId.Value}{directoryName}\\".ToString(CultureInfo.CurrentCulture), directoryName, "2016-01-01 12:00:00".ToDateTime(), "2016-01-01 12:00:00".ToDateTime());
                drive
                    .Setup(drive => drive.NewDirectoryItem(It.Is<DirectoryInfoContract>(parent => parent.Id == parentId), directoryName))
                    .Returns(directory)
                    .Verifiable();
                return directory;
            }

            public FileInfoContract SetupNewFile(string parentId, string fileName)
            {
                return SetupNewFile(new DirectoryId(parentId), fileName);
            }

            public FileInfoContract SetupNewFile(DirectoryId parentId, string fileName)
            {
                var file = new FileInfoContract($"{parentId.Value.TrimEnd('\\')}\\{fileName}".ToString(CultureInfo.CurrentCulture), fileName, "2016-02-01 12:00:00".ToDateTime(), "2016-02-01 12:00:00".ToDateTime(), 0, null);
                drive
                    .Setup(drive => drive.NewFileItem(It.Is<DirectoryInfoContract>(parent => parent.Id == parentId), fileName, It.Is<Stream>(s => s.Length == 0)))
                    .Returns(file)
                    .Verifiable();
                return file;
            }

            public void SetupGetFileContent(FileInfoContract file, byte[] content)
            {
                drive
                    .Setup(drive => drive.GetContent(It.Is<FileInfoContract>(f => f.Id == file.Id)))
                    .Returns(new MemoryStream(content))
                    .Verifiable();
            }

            public void SetupSetFileContent(FileInfoContract file, byte[] content)
            {
                drive
                    .Setup(drive => drive.SetContent(It.Is<FileInfoContract>(f => f.Id == file.Id), It.Is<Stream>(s => s.Contains(content))))
                    .Verifiable();
            }

            public void SetupSetFileContent(FileInfoContract file, byte[] content, ICollection<Tuple<int, int, byte[], byte[]>> differences)
            {
                drive
                    .Setup(drive => drive.SetContent(It.Is<FileInfoContract>(f => f.Id == file.Id), It.IsAny<Stream>()))
                    .Callback((FileInfoContract _file, Stream _stream) => _stream.FindDifferences(content, differences))
                    .Verifiable();
            }

            public void SetupGetFileContentWithError(FileInfoContract file)
            {
                drive
                    .Setup(drive => drive.GetContent(It.Is<FileInfoContract>(f => f.Id == file.Id)))
                    .Throws(new IOException("Error during GetContent"));
            }

            public void SetupSetFileContentWithError<TException>(FileInfoContract file, byte[] content)
                where TException : Exception, new()
            {
                drive
                    .Setup(drive => drive.SetContent(It.Is<FileInfoContract>(f => f.Id == file.Id), It.Is<Stream>(s => s.Contains(content))))
                    .Throws<TException>();
            }

            public void SetupDeleteDirectoryOrFile(FileSystemInfoContract directoryOrFile, bool recurse = false)
            {
                drive
                    .Setup(drive => drive.RemoveItem(It.Is<FileSystemInfoContract>(item => item.Id == directoryOrFile.Id), recurse))
                    .Verifiable();
            }

            public void SetupMoveDirectoryOrFile(FileSystemInfoContract directoryOrFile, DirectoryInfoContract target, Action callback = null)
            {
                SetupMoveItem(directoryOrFile, directoryOrFile.Name, target, callback);
            }

            public void SetupRenameDirectoryOrFile(FileSystemInfoContract directoryOrFile, string name)
            {
                SetupMoveItem(directoryOrFile, name, (directoryOrFile as DirectoryInfoContract)?.Parent ?? (directoryOrFile as FileInfoContract)?.Directory ?? null);
            }

            private void SetupMoveItem(FileSystemInfoContract directoryOrFile, string name, DirectoryInfoContract target, Action callback = null)
            {
                drive
                    .Setup(drive => drive.MoveItem(It.Is<FileSystemInfoContract>(item => item.Id == directoryOrFile.Id), name, target))
                    .Returns((FileSystemInfoContract source, string movePath, DirectoryInfoContract destination) => {
                        var directorySource = source as DirectoryInfoContract;
                        if (directorySource != null)
                            return new DirectoryInfoContract(source.Id.Value, movePath, source.Created, source.Updated) { Parent = target };
                        var fileSource = source as FileInfoContract;
                        if (fileSource != null)
                            return new FileInfoContract(source.Id.Value, movePath, source.Created, source.Updated, fileSource.Size, fileSource.Hash) { Directory = target };
                        throw new InvalidOperationException($"Unsupported type '{source.GetType().Name}'".ToString(CultureInfo.CurrentCulture));
                    })
                    .Callback(() => { if (callback != null) callback(); })
                    .Verifiable();
            }

            public void Verify()
            {
                drive.Verify();
            }

            public static int BufferSize(long bufferSize, long fileSize, int chunks) => (int)Math.Min(bufferSize, fileSize - chunks * bufferSize);

            public static int NumberOfChunks(long bufferSize, long fileSize)
            {
                var remainder = default(long);
                var quotient = Math.DivRem(fileSize, bufferSize, out remainder);
                return (int)quotient + (remainder > 0 ? 1 : 0);
            }

            public void Dispose()
            {
                mounterThread.Abort();
                Dokan.Unmount(MOUNT_POINT[0]);
                Dokan.RemoveMountPoint(MOUNT_POINT);
            }
        }
    }
}
