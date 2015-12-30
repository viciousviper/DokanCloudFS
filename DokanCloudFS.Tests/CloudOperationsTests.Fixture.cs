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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using DokanNet;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.DokanCloudFS.Parameters;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class CloudOperationsTests
    {
        internal static class NativeMethods
        {
            private const string KERNEL_32_DLL = "kernel32.dll";

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

        internal class Fixture : IDisposable
        {
            public const string MOUNT_POINT = "Z:";

            public const string VOLUME_LABEL = "Dokan Volume";

            public const string SCHEMA = "onedrive";

            public const string USER_NAME = "IgorDev";

            public const string TEST_DIRECTORY_NAME = "FileSystemTests";

            internal class TestDirectoryFixture : IDisposable
            {
                private readonly DirectoryInfo rootDirectory;

                public DirectoryInfo Directory { get; }

                public TestDirectoryFixture(DirectoryInfo rootDirectory, string path)
                {
                    this.rootDirectory = rootDirectory;

                    var residualDirectory = rootDirectory.GetDirectories().SingleOrDefault(d => d.Name == path);
                    residualDirectory?.Delete(true);

                    Directory = rootDirectory.CreateSubdirectory(path);
                }

                public DirectoryInfo CreateSubdirectory(string directoryName) => Directory.CreateSubdirectory(directoryName);

                public DirectoryInfo CreateDirectory(string directoryName)
                {
                    var directory = new DirectoryInfo(Path.Combine(Directory.FullName, directoryName));
                    directory.Create();
                    return directory;
                }

                public FileInfo CreateFile(string fileName, byte[] content, DirectoryInfo parentDirectory = null)
                {
                    var file = new FileInfo(Path.Combine(parentDirectory?.FullName ?? Directory.FullName, fileName));
                    if (content != null)
                        using (var fileStream = file.Create()) {
                            fileStream.WriteAsync(content, 0, content.Length).Wait();
                            fileStream.Close();
                        }
                    return file;
                }

                public void Dispose()
                {
                    Directory.Delete(true);
                }
            }

            private Thread mounterThread;

            public static Fixture Initialize() => new Fixture();

            private Fixture()
            {
                CompositionInitializer.Preload(typeof(ICloudGateway));
                CompositionInitializer.Initialize(@"..\..\..\Library", "IgorSoft.CloudFS.Gateways.OneDrive.dll");
                var factory = new CloudDriveFactory();
                CompositionInitializer.SatisfyImports(factory);

                var loggerMock = new Moq.Mock<NLog.ILogger>();
                loggerMock.Setup(l => l.Trace(Moq.It.IsAny<string>())).Callback((string message) => Console.WriteLine(message));

                var operations = new CloudOperations(factory.CreateCloudDrive(SCHEMA, USER_NAME, MOUNT_POINT, new CloudDriveParameters() { ApiKey = null, EncryptionKey = "MyOneDriveSecret&I" }), loggerMock.Object);
                (mounterThread = new Thread(new ThreadStart(() => operations.Mount(MOUNT_POINT, DokanOptions.DebugMode | DokanOptions.RemovableDrive, 5, 800, TimeSpan.FromMinutes(5))))).Start();
                var drive = new DriveInfo(MOUNT_POINT);
                while (!drive.IsReady)
                    Thread.Sleep(50);
            }

            internal DriveInfo GetDriveInfo() => new DriveInfo(MOUNT_POINT);

            internal TestDirectoryFixture CreateTestDirectory() => new TestDirectoryFixture(new DriveInfo(MOUNT_POINT).RootDirectory, TEST_DIRECTORY_NAME);

            internal static int BufferSize(long bufferSize, long fileSize, int chunks) => (int)Math.Min(bufferSize, fileSize - chunks * bufferSize);

            internal static int NumberOfChunks(long bufferSize, long fileSize)
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
