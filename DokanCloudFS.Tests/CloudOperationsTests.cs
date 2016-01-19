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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class CloudOperationsTests
    {
        private static Fixture fixture;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            fixture = Fixture.Initialize();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            fixture?.Dispose();
            fixture = null;
        }

        [TestInitialize]
        public void Initialize()
        {
            fixture.Reset();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetAvailableFreeSpace_Succeeds()
        {
            var freeSpace = 64 * 1 << 20;
            var usedSpace = 36 * 1 << 20;

            fixture.Drive.SetupGet(d => d.Free).Returns(freeSpace);
            fixture.Drive.SetupGet(d => d.Used).Returns(usedSpace);

            var sut = fixture.GetDriveInfo();

            var result = sut.AvailableFreeSpace;

            Assert.AreEqual(freeSpace, result);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetDriveFormat_Succeeds()
        {
            fixture.Drive.SetupGet(d => d.DisplayRoot).Returns(default(string));

            var sut = fixture.GetDriveInfo();

            var result = sut.DriveFormat;

            Assert.AreEqual(nameof(DokanCloudFS), result);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetDriveType_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.DriveType;

            Assert.AreEqual(result, DriveType.Removable);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetIsReady_Succeeds()
        {
            fixture.SetupGetRoot();

            var sut = fixture.GetDriveInfo();

            var result = sut.IsReady;

            Assert.IsTrue(result);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetName_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.Name;

            Assert.AreEqual(Fixture.MOUNT_POINT + Path.DirectorySeparatorChar, result);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetTotalFreeSpace_Succeeds()
        {
            var freeSpace = 64 * 1 << 20;
            var usedSpace = 36 * 1 << 20;

            fixture.Drive.SetupGet(d => d.Free).Returns(freeSpace);
            fixture.Drive.SetupGet(d => d.Used).Returns(usedSpace);

            var sut = fixture.GetDriveInfo();

            var result = sut.TotalFreeSpace;

            Assert.AreEqual(usedSpace, result);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetTotalSize_Succeeds()
        {
            var freeSpace = 64 * 1 << 20;
            var usedSpace = 36 * 1 << 20;

            fixture.Drive.SetupGet(d => d.Free).Returns(freeSpace);
            fixture.Drive.SetupGet(d => d.Used).Returns(usedSpace);

            var sut = fixture.GetDriveInfo();

            var result = sut.TotalSize;

            Assert.AreEqual(freeSpace + usedSpace, result);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetVolumeLabel_Succeeds()
        {
            var volumeLabel = "MockVolume";

            fixture.Drive.SetupGet(d => d.DisplayRoot).Returns(volumeLabel);

            var sut = fixture.GetDriveInfo();

            var result = sut.VolumeLabel;

            Assert.AreEqual(volumeLabel, result);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetRootDirectory_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.RootDirectory;

            Assert.IsNotNull(result);
            Assert.AreEqual(Fixture.MOUNT_POINT + Path.DirectorySeparatorChar, result.Name);

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetDirectories_ReturnsResults()
        {
            fixture.SetupGetRootDirectoryItems();

            var sut = fixture.GetDriveInfo().RootDirectory;
            var directories = sut.GetDirectories();

            CollectionAssert.AreEqual(fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Select(d => d.Name).ToList(), directories.Select(i => i.Name).ToList(), "Diverging result");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetFiles_ReturnsResults()
        {
            fixture.SetupGetRootDirectoryItems();

            var sut = fixture.GetDriveInfo().RootDirectory;
            var files = sut.GetFiles();

            CollectionAssert.AreEqual(fixture.RootDirectoryItems.OfType<FileInfoContract>().Select(f => f.Name).ToList(), files.Select(i => i.Name).ToList(), "Diverging result");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetFileSystemInfos_ReturnsResults()
        {
            fixture.SetupGetRootDirectoryItems();

            var sut = fixture.GetDriveInfo().RootDirectory;
            var items = sut.GetFileSystemInfos();

            CollectionAssert.AreEqual(fixture.RootDirectoryItems.OfType<FileSystemInfoContract>().Select(f => f.Name).ToList(), items.Select(i => i.Name).ToList(), "Diverging result");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Create_Succeeds()
        {
            var directoryName = "NewDir";

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), directoryName);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newDirectory = new DirectoryInfo(sut.FullName + directoryName);
            newDirectory.Create();

            Assert.IsTrue(newDirectory.Exists, "Directory creation failed");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_CreateSubdirectory_Succeeds()
        {
            var directoryName = "NewSubDir";

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), directoryName);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newDirectory = sut.CreateSubdirectory(directoryName);

            Assert.IsTrue(newDirectory.Exists, "Directory creation failed");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Delete_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupGetEmptyDirectoryItems(sutContract.Id.Value);
            fixture.SetupDeleteDirectoryOrFile(sutContract);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected directory missing");

            sut.Delete();
            sut.Refresh();

            Assert.IsFalse(sut.Exists, "Directory deletion failed");

            var residualDirectories = root.GetDirectories(sutContract.Name);
            Assert.IsFalse(residualDirectories.Any(), "Excessive directory found");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_MoveToDirectory_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();
            var targetContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupMoveDirectoryOrFile(sutContract, targetContract);
            fixture.SetupGetSubDirectory2Items(fixture.SubDirectory2Items.Concat(new[] { sutContract }));

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();
            var target = root.GetDirectories(targetContract.Name).Single();

            sut.MoveTo(target.FullName + Path.DirectorySeparatorChar + sutContract.Name);
                
            var residualDirectories = root.GetDirectories(sutContract.Name);
            Assert.IsFalse(residualDirectories.Any(), "Original directory not removed");

            var movedDirectories = target.GetDirectories(sutContract.Name);
            Assert.AreEqual(1, movedDirectories.Count(), "Directory not moved");
            Assert.AreEqual(target.FullName, sut.Parent.FullName, "Directory not moved");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Rename_Succeeds()
        {
            var directoryName = "RenamedDirectory";

            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupRenameDirectoryOrFile(sutContract, directoryName);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();

            sut.MoveTo(root.FullName + Path.DirectorySeparatorChar + directoryName);

            var residualDirectories = root.GetDirectories(sutContract.Name);
            Assert.IsFalse(residualDirectories.Any(), "Original directory not removed");

            var renamedDirectories = root.GetDirectories(directoryName);
            Assert.AreEqual(1, renamedDirectories.Count(), "Directory not renamed");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_Create_Succeeds()
        {
            var fileName = "NewFile.ext";
            var fileInput = "Why did the chicken cross the road?";

            fixture.SetupGetRootDirectoryItems();
            var file = fixture.SetupNewFile(Path.DirectorySeparatorChar.ToString(), fileName);
            fixture.SetupSetFileContent(file, fileInput);
            fixture.SetupGetFileContent(file, fileInput);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newFile = new FileInfo(sut.FullName + fileName);
            using (var fileStream = newFile.Create()) {
                fileStream.WriteAsync(Encoding.Default.GetBytes(fileInput), 0, Encoding.Default.GetByteCount(fileInput)).Wait();
                fileStream.Close();
            }

            Assert.IsTrue(newFile.Exists, "File creation failed");

            var fileOutput = default(string);
            using (var fileStream = newFile.OpenRead()) {
                var buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                fileOutput = Encoding.Default.GetString(buffer);
            }

            Assert.AreEqual(fileInput, fileOutput, "Unexpected file content");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_Delete_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupDeleteDirectoryOrFile(sutContract);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected file missing");

            sut.Delete();
            sut.Refresh();

            Assert.IsFalse(sut.Exists, "File deletion failed");

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.IsFalse(residualFiles.Any(), "Excessive file found");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_CopyToDirectory_Succeeds()
        {
            var fileContent = "Why did the chicken cross the road?";

            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            var targetContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();
            var copyContract = new FileInfoContract(targetContract.Id + Path.DirectorySeparatorChar.ToString() + sutContract.Name, sutContract.Name, sutContract.Created, sutContract.Updated, sutContract.Size, sutContract.Hash) {
                Directory = targetContract
            };

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupGetSubDirectory2Items(fixture.SubDirectory2Items);
            fixture.SetupGetFileContent(sutContract, fileContent);
            fixture.SetupNewFile(targetContract.Id.Value, copyContract.Name);
            fixture.SetupSetFileContent(copyContract, fileContent);
            fixture.SetupGetDisplayRoot();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();
            var target = root.GetDirectories(targetContract.Name).Single();

            sut.CopyTo(target.FullName + Path.DirectorySeparatorChar + copyContract.Name);

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.AreEqual(1, residualFiles.Count(), "Original file removed");
            Assert.AreEqual(root.FullName, sut.Directory.FullName, "Original file relocated");

            var copiedFiles = target.GetFiles(copyContract.Name);
            Assert.AreEqual(1, copiedFiles.Count(), "File not copied");
            Assert.AreEqual(target.FullName, copiedFiles[0].Directory.FullName, "Unexpected copy location");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_MoveToDirectory_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            var targetContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupMoveDirectoryOrFile(sutContract, targetContract);
            fixture.SetupGetSubDirectory2Items(fixture.SubDirectory2Items.Concat(new[] { sutContract }));

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();
            var target = root.GetDirectories(targetContract.Name).Single();

            sut.MoveTo(target.FullName + Path.DirectorySeparatorChar + sutContract.Name);

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.IsFalse(residualFiles.Any(), "Original file not removed");

            var movedFiles = target.GetFiles(sutContract.Name);
            Assert.AreEqual(1, movedFiles.Count(), "File not moved");
            Assert.AreEqual(target.FullName, sut.Directory.FullName, "File not moved");

            fixture.Drive.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_Rename_Succeeds()
        {
            var fileName = "RenamedFile";

            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupRenameDirectoryOrFile(sutContract, fileName);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            sut.MoveTo(root.FullName + Path.DirectorySeparatorChar + fileName);

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.IsFalse(residualFiles.Any(), "Original file not removed");

            var renamedFiles = root.GetFiles(fileName);
            Assert.AreEqual(1, renamedFiles.Count(), "File not renamed");

            fixture.Drive.VerifyAll();
        }

        /*[TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [DeploymentItem("CloudOperationsTests.Configuration.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\CloudOperationsTests.Configuration.xml", "ConfigRead", DataAccessMethod.Sequential)]
        public void FileInfo_ReadOverlapped_Succeeds()
        {
            var bufferSize = int.Parse((string)TestContext.DataRow["BufferSize"]);
            var fileSize = int.Parse((string)TestContext.DataRow["FileSize"]);

            using (var testDirectory = fixture.CreateTestDirectory()) {
                var testInput = Enumerable.Range(0, fileSize).Select(i => (byte)(i % 251)).ToArray();

                var file = testDirectory.CreateFile("File.ext", testInput);

                var chunks = NativeMethods.ReadEx(file.FullName, bufferSize, fileSize);

                CollectionAssert.AreEqual(testInput, chunks.Aggregate(Enumerable.Empty<byte>(), (b, c) => b.Concat(c.Buffer), b => b.ToArray()), "Unexpected file content");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [DeploymentItem("CloudOperationsTests.Configuration.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\CloudOperationsTests.Configuration.xml", "ConfigWrite", DataAccessMethod.Sequential)]
        public void FileInfo_WriteOverlapped_Succeeds()
        {
            var bufferSize = int.Parse((string)TestContext.DataRow["BufferSize"]);
            var fileSize = int.Parse((string)TestContext.DataRow["FileSize"]);
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var file = testDirectory.CreateFile("File.ext", new[] { (byte)0 });

                var testInput = Enumerable.Range(0, fileSize).Select(i => (byte)(i % 251)).ToArray();

                var chunks = Enumerable.Range(0, Fixture.NumberOfChunks(bufferSize, fileSize))
                    .Select(i => new NativeMethods.OverlappedChunk(testInput.Skip(i * bufferSize).Take(NativeMethods.BufferSize(bufferSize, fileSize, i)).ToArray())).ToArray();

                NativeMethods.WriteEx(file.FullName, bufferSize, fileSize, chunks);

                var testOutput = new byte[fileSize];
                using (var fileStream = file.OpenRead()) {
                    fileStream.Read(testOutput, 0, testOutput.Length);
                }

                CollectionAssert.AreEqual(testInput, testOutput, "Unexpected file content");
            }
        }*/
    }
}
