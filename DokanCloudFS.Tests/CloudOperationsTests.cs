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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.AppDomainResolver;
using System.Linq;

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
            AssemblyResolver.Initialize();
            fixture = Fixture.Initialize();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            fixture?.Dispose();
            fixture = null;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetAvailableFreeSpace_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.AvailableFreeSpace;

            Assert.IsTrue(result > 0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetDriveFormat_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.DriveFormat;

            Assert.AreEqual(nameof(DokanCloudFS), result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetDriveType_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.DriveType;

            Assert.AreEqual(result, DriveType.Removable);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetIsReady_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.IsReady;

            Assert.IsTrue(result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetName_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.Name;

            Assert.AreEqual(Fixture.MOUNT_POINT + Path.DirectorySeparatorChar, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetTotalFreeSpace_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.TotalFreeSpace;

            Assert.IsTrue(result > 0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetTotalSize_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.TotalSize;

            Assert.IsTrue(result > 0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetVolumeLabel_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.VolumeLabel;

            Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}", result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DriveInfo_GetRootDirectory_Succeeds()
        {
            var sut = fixture.GetDriveInfo();

            var result = sut.RootDirectory;

            Assert.IsNotNull(result);
            Assert.AreEqual(Fixture.MOUNT_POINT + Path.DirectorySeparatorChar, result.Name);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DirectoryInfo_GetDirectories_ReturnsResults()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                testDirectory.Directory.CreateSubdirectory("DirectoryContent");
                testDirectory.CreateFile("File.ext", new byte[1000]);

                System.Threading.Thread.Sleep(20);

                var items = testDirectory.Directory.GetDirectories();

                Assert.AreEqual(1, items.Count(), "Unexpected number of results");
                Assert.IsTrue(items.Any(i => i.Name == "DirectoryContent"), "Expected directory is missing");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DirectoryInfo_GetFiles_ReturnsResults()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                testDirectory.Directory.CreateSubdirectory("DirectoryContent");
                testDirectory.CreateFile("File.ext", new byte[1000]);

                var items = testDirectory.Directory.GetFiles();

                Assert.AreEqual(1, items.Count(), "Unexpected number of results");
                Assert.IsTrue(items.Any(i => i.Name == "File.ext"), "Expected file is missing");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DirectoryInfo_GetFileSystemInfos_ReturnsResults()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                testDirectory.Directory.CreateSubdirectory("DirectoryContent");
                testDirectory.CreateFile("File.ext", new byte[1000]);

                var items = testDirectory.Directory.GetFileSystemInfos();

                Assert.AreEqual(2, items.Count(), "Unexpected number of results");
                Assert.IsTrue(items.OfType<DirectoryInfo>().Any(i => i.Name == "DirectoryContent"), "Expected directory is missing");
                Assert.IsTrue(items.OfType<FileInfo>().Any(i => i.Name == "File.ext"), "Expected file is missing");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DirectoryInfo_Create_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var directory = testDirectory.CreateDirectory("Directory");

                Assert.IsTrue(directory.Exists, "Directory was not created");

                var items = testDirectory.Directory.GetDirectories("Directory");

                Assert.AreEqual(1, items.Count(), "Unexpected number of results");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DirectoryInfo_CreateSubdirectory_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var directory = testDirectory.CreateSubdirectory("Directory");

                Assert.IsTrue(directory.Exists, "Directory was not created");

                var items = testDirectory.Directory.GetDirectories("Directory");

                Assert.AreEqual(1, items.Count(), "Unexpected number of results");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DirectoryInfo_Delete_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var directory = testDirectory.CreateSubdirectory("Directory");

                Assert.IsTrue(directory.Exists, "Directory does not exist");

                directory.Delete();
                directory.Refresh();

                Assert.IsFalse(directory.Exists, "Directory still exists");

                var items = testDirectory.Directory.GetDirectories("Directory");

                Assert.IsFalse(items.Any(), "Unexpected results");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DirectoryInfo_MoveToDirectory_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var directory = testDirectory.CreateSubdirectory("Directory");
                testDirectory.CreateFile("File.ext", new byte[1000], directory);
                var targetDirectory = testDirectory.CreateSubdirectory("TargetDirectory");

                directory.MoveTo(targetDirectory.FullName + Path.DirectorySeparatorChar + directory.Name);
                targetDirectory.Refresh();

                var items = targetDirectory.GetDirectories("Directory");

                Assert.AreEqual(1, items.Count(), "Directory not moved");
                Assert.AreEqual(targetDirectory.FullName, directory.Parent.FullName, "File not moved");
                Assert.AreEqual(0, testDirectory.Directory.GetDirectories("Directory").Count(), "Original directory not removed");

                var files = items.Single().GetFiles("File.ext");

                Assert.AreEqual(1, files.Count(), "Enclosed file not moved");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void DirectoryInfo_Rename_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var directory = testDirectory.CreateSubdirectory("Directory");
                testDirectory.CreateFile("File.ext", new byte[1000], directory);

                directory.MoveTo(testDirectory.Directory.FullName + Path.DirectorySeparatorChar + "RenamedDirectory");

                Assert.AreEqual(1, testDirectory.Directory.GetDirectories("RenamedDirectory").Count(), "Directory not renamed");
                Assert.AreEqual(0, testDirectory.Directory.GetDirectories("Directory").Count(), "Original directory not removed");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void FileInfo_Create_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var testInput = "Why did the chicken cross the road?";

                var file = testDirectory.CreateFile("File.ext", System.Text.Encoding.Default.GetBytes(testInput));

                Assert.IsTrue(file.Exists, "File was not created");

                var items = testDirectory.Directory.GetFiles("File.ext");

                Assert.AreEqual(1, items.Count(), "Unexpected number of results");

                var testOutput = default(string);
                using (var fileStream = file.OpenRead()) {
                    var buffer = new byte[fileStream.Length];
                    fileStream.Read(buffer, 0, buffer.Length);
                    testOutput = System.Text.Encoding.Default.GetString(buffer);
                }

                Assert.AreEqual(testInput, testOutput, "Unexpected file content");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void FileInfo_CreateMultiple_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory())
            {
                var testInputs = new[] {
                    new { Name = "File1.ext", Content = "Why did the chicken cross the road?" },
                    new { Name = "File2.ext", Content = "Mary had a little lamb" },
                    new { Name = "File3.ext", Content = "Who has been spoken of twofold?" }
                };

                var files = testInputs.Select(i => testDirectory.CreateFile(i.Name, System.Text.Encoding.Default.GetBytes(i.Content))).ToArray();

                Assert.IsTrue(files.All(f => f.Exists), "One of the specified files was not created");

                var items = testDirectory.Directory.GetFiles("File*.ext");

                Assert.AreEqual(testInputs.Count(), items.Count(), "Unexpected number of results");

                Array.ForEach(files, f => {
                    var testInput = testInputs.Single(i => i.Name == f.Name).Content;
                    var testOutput = default(string);
                    using (var fileStream = f.OpenRead()) {
                        var buffer = new byte[fileStream.Length];
                        fileStream.Read(buffer, 0, buffer.Length);
                        testOutput = System.Text.Encoding.Default.GetString(buffer);
                    }

                    Assert.AreEqual(testInput, testOutput, "Unexpected file content");
                });
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void FileInfo_Delete_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var file = testDirectory.CreateFile("File.ext", new byte[1000]);

                Assert.IsTrue(file.Exists, "File does not exist");

                file.Delete();
                file.Refresh();

                Assert.IsFalse(file.Exists, "File still exists");

                var items = testDirectory.Directory.GetFiles("File.ext");

                Assert.IsFalse(items.Any(), "Unexpected results");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void FileInfo_CopyToDirectory_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var file = testDirectory.CreateFile("File.ext", new byte[1000]);
                var targetDirectory = testDirectory.CreateSubdirectory("TargetDirectory");

                file.CopyTo(targetDirectory.FullName + Path.DirectorySeparatorChar + file.Name);
                targetDirectory.Refresh();

                Assert.AreEqual(1, targetDirectory.GetFiles("File.ext").Count(), "File not copied");
                Assert.AreEqual(testDirectory.Directory.FullName, file.DirectoryName, "Original file moved");
                Assert.AreEqual(1, testDirectory.Directory.GetFiles("File.ext").Count(), "Original file removed");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void FileInfo_MoveToDirectory_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var file = testDirectory.CreateFile("File.ext", new byte[1000]);
                var targetDirectory = testDirectory.CreateSubdirectory("TargetDirectory");

                file.MoveTo(targetDirectory.FullName + Path.DirectorySeparatorChar + file.Name);
                targetDirectory.Refresh();

                Assert.AreEqual(1, targetDirectory.GetFiles("File.ext").Count(), "File not moved");
                Assert.AreEqual(targetDirectory.FullName, file.DirectoryName, "File not moved");
                Assert.AreEqual(0, testDirectory.Directory.GetFiles("File.ext").Count(), "Original file not removed");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void FileInfo_Rename_Succeeds()
        {
            using (var testDirectory = fixture.CreateTestDirectory()) {
                var file = testDirectory.CreateFile("File.ext", new byte[1000]);

                file.MoveTo(testDirectory.Directory.FullName + Path.DirectorySeparatorChar + "RenamedFile.ext");

                Assert.AreEqual(1, testDirectory.Directory.GetFiles("RenamedFile.ext").Count(), "File not moved");
                Assert.AreEqual(0, testDirectory.Directory.GetFiles("File.ext").Count(), "Original file not removed");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
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

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
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
        }
    }
}
