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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class FileSystemTests
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
            fixture.Reset(TestContext.TestName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetAvailableFreeSpace_Succeeds()
        {
            const int freeSpace = 64 * 1 << 20;
            const int usedSpace = 36 * 1 << 20;

            fixture.SetupGetFree(freeSpace);
            fixture.SetupGetUsed(usedSpace);

            var sut = fixture.GetDriveInfo();

            var result = sut.AvailableFreeSpace;

            Assert.AreEqual(freeSpace, result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetDriveFormat_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.DriveFormat;

            Assert.AreEqual(nameof(DokanCloudFS), result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetDriveType_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.DriveType;

            Assert.AreEqual(result, DriveType.Removable);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetIsReady_Succeeds()
        {
            fixture.SetupGetRoot();

            var sut = fixture.GetDriveInfo();

            var result = sut.IsReady;

            Assert.IsTrue(result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetName_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.Name;

            Assert.AreEqual(Fixture.MOUNT_POINT + Path.DirectorySeparatorChar, result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetTotalFreeSpace_Succeeds()
        {
            const int freeSpace = 64 * 1 << 20;
            const int usedSpace = 36 * 1 << 20;

            fixture.SetupGetFree(freeSpace);
            fixture.SetupGetUsed(usedSpace);

            var sut = fixture.GetDriveInfo();

            var result = sut.TotalFreeSpace;

            Assert.AreEqual(usedSpace, result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetTotalSize_Succeeds()
        {
            const int freeSpace = 64 * 1 << 20;
            const int usedSpace = 36 * 1 << 20;

            fixture.SetupGetFree(freeSpace);
            fixture.SetupGetUsed(usedSpace);

            var sut = fixture.GetDriveInfo();

            var result = sut.TotalSize;

            Assert.AreEqual(freeSpace + usedSpace, result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetVolumeLabel_Succeeds()
        {
            const string volumeLabel = "MockVolume";

            fixture.SetupGetDisplayRoot(volumeLabel);

            var sut = fixture.GetDriveInfo();

            var result = sut.VolumeLabel;

            Assert.AreEqual(volumeLabel, result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetRootDirectory_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.RootDirectory;

            Assert.IsNotNull(result);
            Assert.AreEqual(Fixture.MOUNT_POINT + Path.DirectorySeparatorChar, result.Name);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Directory_Delete_WhereDirectoryIsUndefined_Throws()
        {
            var directoryName = $"{Fixture.MOUNT_POINT}\\NonExistingDirectory";

            fixture.SetupGetRootDirectoryItems();

            Directory.Delete(directoryName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Directory_Move_WhereDirectoryIsUndefined_Throws()
        {
            var directoryName = $"{Fixture.MOUNT_POINT}\\NonExistingDirectory";
            var targetName = $"{Fixture.MOUNT_POINT}\\{fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last().Name}";

            fixture.SetupGetRootDirectoryItems();

            Directory.Move(directoryName, targetName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetDirectories_Succeeds()
        {
            fixture.SetupGetRootDirectoryItems();

            var sut = fixture.GetDriveInfo().RootDirectory;
            var directories = sut.GetDirectories();

            CollectionAssert.AreEqual(fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Select(d => d.Name).ToList(), directories.Select(i => i.Name).ToList(), "Mismatched result");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetFiles_Succeeds()
        {
            fixture.SetupGetRootDirectoryItems();

            var sut = fixture.GetDriveInfo().RootDirectory;
            var files = sut.GetFiles();

            CollectionAssert.AreEqual(fixture.RootDirectoryItems.OfType<FileInfoContract>().Select(f => f.Name).ToList(), files.Select(i => i.Name).ToList(), "Mismatched result");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetFileSystemInfos_Succeeds()
        {
            fixture.SetupGetRootDirectoryItems();

            var sut = fixture.GetDriveInfo().RootDirectory;
            var items = sut.GetFileSystemInfos();

            CollectionAssert.AreEqual(fixture.RootDirectoryItems.OfType<FileSystemInfoContract>().Select(f => f.Name).ToList(), items.Select(i => i.Name).ToList(), "Mismatched result");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Create_Succeeds()
        {
            var directoryName = fixture.Named("NewDir");

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), directoryName);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newDirectory = new DirectoryInfo(sut.FullName + directoryName);
            newDirectory.Create();

            Assert.IsTrue(newDirectory.Exists, "Directory creation failed");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Create_WhereParentIsUndefined_Succeeds()
        {
            var directoryName = fixture.Named("NewDir");
            var parentDirectoryName = fixture.Named("Parent");

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), parentDirectoryName);
            fixture.SetupGetEmptyDirectoryItems(Path.DirectorySeparatorChar + parentDirectoryName + Path.DirectorySeparatorChar);
            fixture.SetupNewDirectory(Path.DirectorySeparatorChar + parentDirectoryName + Path.DirectorySeparatorChar, directoryName);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newDirectory = new DirectoryInfo(sut.FullName + parentDirectoryName + @"\" + directoryName);
            newDirectory.Create();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_CreateSubdirectory_Succeeds()
        {
            var directoryName = fixture.Named("NewSubDir");

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), directoryName);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newDirectory = sut.CreateSubdirectory(directoryName);

            Assert.IsTrue(newDirectory.Exists, "Directory creation failed");

            fixture.Verify();
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

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(IOException))]
        public void DirectoryInfo_Delete_WhereDirectoryIsNonEmpty_Throws()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Single(d => d.Name == "SubDir2");

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupGetSubDirectory2Items();
            fixture.SetupDeleteDirectoryOrFile(sutContract);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected directory missing");

            sut.Delete();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void DirectoryInfo_Delete_WhereDirectoryIsUndefined_Throws()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var sut = new DirectoryInfo(Path.DirectorySeparatorChar + "UNDEFINED");

            Assert.IsFalse(sut.Exists, "Unexpected directory found");

            sut.Delete();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetAttributes_ReturnsExpectedValue()
        {
            var directoryName = fixture.Named("NewSubDir");

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), directoryName);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newDirectory = sut.CreateSubdirectory(directoryName);

            Assert.IsTrue(newDirectory.Exists, "Directory creation failed");
            Assert.AreEqual(FileAttributes.Directory, sut.Attributes, "Directory possesses unexpected Attributes");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_MoveToDirectory_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();
            var targetContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();

            var moveCalled = false;

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupMoveDirectoryOrFile(sutContract, targetContract, () => moveCalled = true);
            fixture.SetupGetSubDirectory2Items(() => moveCalled ? fixture.SubDirectory2Items.Concat(new[] { sutContract }) : fixture.SubDirectory2Items);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();
            var target = root.GetDirectories(targetContract.Name).Single();

            sut.MoveTo(target.FullName + Path.DirectorySeparatorChar + sutContract.Name);

            var residualDirectories = root.GetDirectories(sutContract.Name);
            Assert.IsFalse(residualDirectories.Any(), "Original directory not removed");

            var movedDirectories = target.GetDirectories(sutContract.Name);
            Assert.AreEqual(1, movedDirectories.Count(), "Directory not moved");
            Assert.AreEqual(target.FullName, sut.Parent.FullName, "Directory not moved");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Rename_Succeeds()
        {
            var directoryName = fixture.Named("RenamedDirectory");

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

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        //[ExpectedException(typeof(FileNotFoundException))]
        public void File_Delete_WhereFileIsUndefined_Throws()
        {
            var fileName = $"{Fixture.MOUNT_POINT}\\NonExistingFile.ext";

            fixture.SetupGetRootDirectoryItems();

            File.Delete(fileName);

            Assert.Inconclusive();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(FileNotFoundException))]
        public void File_Move_WhereFileIsUndefined_Throws()
        {
            var fileName = $"{Fixture.MOUNT_POINT}\\NonExistingFile.ext";
            var targetName = $"{Fixture.MOUNT_POINT}\\{fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last().Name}";

            fixture.SetupGetRootDirectoryItems();

            File.Move(fileName, targetName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_Create_Succeeds()
        {
            var fileName = fixture.Named("NewFile.ext");
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");

            fixture.SetupGetRootDirectoryItems();
            var file = fixture.SetupNewFile(Path.DirectorySeparatorChar.ToString(), fileName);
            fixture.SetupSetFileContent(file, fileContent);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newFile = new FileInfo(sut.FullName + fileName);

            using (var fileStream = newFile.Create()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            Assert.IsTrue(newFile.Exists, "File creation failed");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void FileInfo_Create_WhereParentIsUndefined_Throws()
        {
            var fileName = fixture.Named("NewFile.ext");

            fixture.SetupGetRootDirectoryItems();

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newFile = new FileInfo(sut.FullName + @"UNDEFINED\" + fileName);

            newFile.Create().Dispose();
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

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_GetAttributes_ReturnsExpectedValue()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected file missing");
            Assert.AreEqual(FileAttributes.NotContentIndexed, sut.Attributes, "File possesses unexpected Attributes");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_GetIsReadOnly_ReturnsFalse()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected file missing");
            Assert.IsFalse(sut.IsReadOnly, "File is read-only");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_GetLength_ReturnsExpectedValue()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected file missing");
            Assert.AreEqual(sutContract.Size.Value, sut.Length, "File length differs");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_CopyToDirectory_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");

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

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_MoveToDirectory_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            var targetContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();

            var moveCalled = false;

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupMoveDirectoryOrFile(sutContract, targetContract, () => moveCalled = true);
            fixture.SetupGetSubDirectory2Items(() => moveCalled ? fixture.SubDirectory2Items.Concat(new[] { sutContract }) : fixture.SubDirectory2Items);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();
            var target = root.GetDirectories(targetContract.Name).Single();

            sut.MoveTo(target.FullName + Path.DirectorySeparatorChar + sutContract.Name);

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.IsFalse(residualFiles.Any(), "Original file not removed");

            var movedFiles = target.GetFiles(sutContract.Name);
            Assert.AreEqual(1, movedFiles.Count(), "File not moved");
            Assert.AreEqual(target.FullName, sut.Directory.FullName, "File not moved");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(IOException))]
        public void FileInfo_Open_CreateNew_WhereFileExists_Throws()
        {
            var fileName = fixture.Named("NewFile.ext");

            fixture.SetupGetRootDirectoryItems();
            var file = fixture.SetupNewFile(Path.DirectorySeparatorChar.ToString(), fileName);

            var sut = fixture.GetDriveInfo().RootDirectory;
            var newFile = new FileInfo(sut.FullName + fileName);

            newFile.Create().Dispose();

            Assert.IsTrue(newFile.Exists, "File creation failed");

            newFile.Open(FileMode.CreateNew).Dispose();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_Rename_Succeeds()
        {
            var fileName = fixture.Named("RenamedFile");

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

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Read_OnOpenRead_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            sutContract.Size = (FileSize)fileContent.Length;

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupGetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);

            var buffer = default(byte[]);
            using (var fileStream = sut.OpenRead()) {
                buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
            }

            Assert.AreEqual(sut.Length, buffer.Length, "Invalid file size");
            CollectionAssert.AreEqual(fileContent, buffer.Take(fileContent.Length).ToArray(), "Unexpected file content");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpenWrite_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);

            using (var fileStream = sut.OpenWrite()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsAppend_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContent(sutContract, Enumerable.Repeat<byte>(0, (int)sutContract.Size).Concat(fileContent).ToArray());

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.Append, FileAccess.Write)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsCreate_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContent(sutContract, Array.Empty<byte>());
            fixture.SetupSetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.Create)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsCreateNew_Succeeds()
        {
            var fileName = fixture.Named("NewFile.ext");
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");

            fixture.SetupGetRootDirectoryItems();
            var sutContract = fixture.SetupNewFile(Path.DirectorySeparatorChar.ToString(), fileName);

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + fileName);

            using (var fileStream = sut.Open(FileMode.CreateNew)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsOpen_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.Open, FileAccess.Write)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsOpenOrCreate_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.OpenOrCreate)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsTruncate_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.Truncate, FileAccess.Write)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Flush_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenWrite()) {
                fileStream.FlushAsync().Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_FlushAfterWrite_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenWrite()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
                fileStream.FlushAsync().Wait();
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Lock_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenRead()) {
                fileStream.Lock(0, 65536);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(IOException))]
        public void FileStream_Lock_WhereFileIsLocked_Throws()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenRead()) {
                fileStream.Lock(0, 65536);

                fileStream.Lock(0, 65536);
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Unlock_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenRead()) {
                fileStream.Lock(0, 65536);

                fileStream.Unlock(0, 65536);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Unlock_WhereFileIsNotLocked_DoesNotThrow()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenRead()) {
                fileStream.Unlock(0, 65536);

                fileStream.Unlock(0, 65536);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(IOException))]
        public void FileStream_ExceptionDuringRead_Throws()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupGetFileContentWithError(sutContract);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            var buffer = default(byte[]);
            using (var fileStream = sut.OpenRead()) {
                buffer = new byte[fileStream.Length];
                var bytesRead = fileStream.Read(buffer, 0, buffer.Length);
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_IOExceptionDuringWrite_RemovesFile()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContentWithError<IOException>(sutContract, fileContent);
            fixture.SetupDeleteDirectoryOrFile(sutContract);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenWrite()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            Assert.IsFalse(sut.Exists, "Defective file found");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_UnauthorizedAccessExceptionDuringWrite_KeepsFile()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            fixture.SetupSetFileContentWithError<UnauthorizedAccessException>(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenWrite()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            Assert.IsTrue(sut.Exists, "File removed");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_NativeAppendTo_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");

            fixture.SetupGetRootDirectoryItems();
            var fileContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            var root = fixture.GetDriveInfo().RootDirectory;

            int bytesWritten;
            var result = NativeMethods.AppendTo(root.FullName + fileContract.Name, fileContent, out bytesWritten);

            Assert.IsFalse(result, "File operation succeeded unexpectedly");
            Assert.AreEqual(0, bytesWritten, "Unexpected number of bytes written");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_NativeTruncate_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetRootDirectoryItems();
            var fileContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            fixture.SetupSetFileContent(sutContract, fileContent);

            var root = fixture.GetDriveInfo().RootDirectory;

            int bytesWritten;
            var result = NativeMethods.Truncate(root.FullName + fileContract.Name, fileContent, out bytesWritten);
            //var file = root.GetFiles(fileContract.Name).Single();

            Assert.IsTrue(result, "File operation failed");
            Assert.AreEqual(fileContent.Length, bytesWritten, "Unexpected number of bytes written");

            fixture.Verify();
        }

        //Temporarily excluded from CI builds due to instability
        [TestMethod, TestCategory(nameof(TestCategories.Manual))]
        //[TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [DeploymentItem("FileSystemTests.Configuration.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\FileSystemTests.Configuration.xml", "ConfigRead", DataAccessMethod.Sequential)]
        public void FileInfo_NativeReadOverlapped_Succeeds()
        {
            var bufferSize = int.Parse((string)TestContext.DataRow["BufferSize"]);
            var fileSize = int.Parse((string)TestContext.DataRow["FileSize"]);
            var testInput = Enumerable.Range(0, fileSize).Select(i => (byte)(i % 251 + 1)).ToArray();
            var sutContract = new FileInfoContract(@"\File_NativeReadOverlapped.ext", "File_NativeReadOverlapped.ext", "2016-01-02 10:11:12".ToDateTime(), "2016-01-02 20:21:22".ToDateTime(), new FileSize("16kB"), "16384".ToHash());

            fixture.SetupGetRootDirectoryItems(fixture.RootDirectoryItems.Concat(new[] { sutContract }));
            fixture.SetupGetFileContent(sutContract, testInput);

            var root = fixture.GetDriveInfo().RootDirectory;
            var chunks = NativeMethods.ReadEx(root.FullName + sutContract.Name, bufferSize, fileSize);

            Assert.IsTrue(chunks.All(c => c.Win32Error == 0), "Win32Error occured");
            var result = chunks.Aggregate(Enumerable.Empty<byte>(), (b, c) => b.Concat(c.Buffer), b => b.ToArray());
            Assert.IsFalse(result.Any(b => b == default(byte)), "Uninitialized data detected");
            CollectionAssert.AreEqual(testInput, result, "Unexpected file content");

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [DeploymentItem("FileSystemTests.Configuration.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\FileSystemTests.Configuration.xml", "ConfigWrite", DataAccessMethod.Sequential)]
        public void FileInfo_NativeWriteOverlapped_Succeeds()
        {
            var bufferSize = int.Parse((string)TestContext.DataRow["BufferSize"]);
            var fileSize = int.Parse((string)TestContext.DataRow["FileSize"]);
            var testInput = Enumerable.Range(0, fileSize).Select(i => (byte)(i % 251 + 1)).ToArray();
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            var differences = new SynchronizedCollection<Tuple<int, int, byte[], byte[]>>();

            fixture.SetupGetRootDirectoryItems();
            var file = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            fixture.SetupSetFileContent(file, testInput, differences);

            var root = fixture.GetDriveInfo().RootDirectory;
            var chunks = Enumerable.Range(0, Fixture.NumberOfChunks(bufferSize, fileSize))
                .Select(i => new NativeMethods.OverlappedChunk(testInput.Skip(i * bufferSize).Take(NativeMethods.BufferSize(bufferSize, fileSize, i)).ToArray())).ToArray();

            NativeMethods.WriteEx(root.FullName + file.Name, bufferSize, fileSize, chunks);

            Assert.IsTrue(chunks.All(c => c.Win32Error == 0), "Win32Error occured");

            Assert.IsFalse(differences.Any(), "Mismatched data detected");

            fixture.Verify();
        }
    }
}