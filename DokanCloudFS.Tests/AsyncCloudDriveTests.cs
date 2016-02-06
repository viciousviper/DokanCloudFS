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
    public sealed partial class AsyncCloudDriveTests
    {
        private Fixture fixture;

        private string apiKey = "<MyApiKey>";

        private string encryptionKey = "<MyEncryptionKey>";

        private IDictionary<string, string> parameters = null;

        [TestInitialize]
        public void Initialize()
        {
            fixture = Fixture.Initialize();
        }

        [TestMethod]
        public void CloudDrive_Create_Succeeds()
        {
            using (var result = fixture.Create(apiKey, encryptionKey)) {
                Assert.IsNotNull(result, "Missing result");
            }
        }

        [TestMethod]
        public void CloudDrive_GetFree_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.Free;

                Assert.AreEqual(Fixture.FREE_SPACE, result, "Unexpected Free value");
            }
        }

        [TestMethod]
        public void CloudDrive_GetUsed_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.Used;

                Assert.AreEqual(Fixture.USED_SPACE, result, "Unexpected Used value");
            }
        }

        [TestMethod]
        public void CloudDrive_GetRoot_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);
            fixture.SetupGetRootAsync(apiKey);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.GetRoot();

                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}{Path.VolumeSeparatorChar}{Path.DirectorySeparatorChar}", result.FullName, "Unexpected root name");
            }
        }

        [TestMethod]
        public void CloudDrive_GetDisplayRoot_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);
            fixture.SetupGetRootAsync(apiKey);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.DisplayRoot;

                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}", result, "Unexpected DisplayRoot value");
            }
        }

        [TestMethod]
        public void CloudDrive_GetChildItem_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);
            fixture.SetupGetRootAsync(apiKey);
            fixture.SetupGetRootDirectoryItemsAsync();

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.GetChildItem(sut.GetRoot());

                Assert.AreEqual(fixture.RootDirectoryItems, result, "Mismatched result");
            }
        }

        [TestMethod]
        public void CloudDrive_GetContent_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetContentAsync(sutContract, fileContent, encryptionKey);

            var buffer = default(byte[]);
            using (var sut = fixture.Create(apiKey, encryptionKey))
            using (var stream = sut.GetContent(sutContract)) {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            Assert.AreEqual(fileContent.Length, buffer.Length, "Invalid content size");
            CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_GetContent_WhereContentIsUnencrypted_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetContentAsync(sutContract, fileContent);

            var buffer = default(byte[]);
            using (var sut = fixture.Create(apiKey, encryptionKey))
            using (var stream = sut.GetContent(sutContract)) {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            Assert.AreEqual(fileContent.Length, buffer.Length, "Invalid content size");
            CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_GetContent_WhereContentIsNotSeekable_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupGetContentAsync(sutContract, fileContent, encryptionKey, false);

            var buffer = default(byte[]);
            using (var sut = fixture.Create(apiKey, encryptionKey))
            using (var stream = sut.GetContent(sutContract)) {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            Assert.AreEqual(fileContent.Length, buffer.Length, "Invalid content size");
            CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_MoveDirectoryItem_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();
            var directory = fixture.TargetDirectory;

            fixture.SetupMoveDirectoryOrFileAsync(sutContract, directory);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                sut.MoveItem(sutContract, sutContract.Name, directory);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_MoveFileItem_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().Last();
            var directory = fixture.TargetDirectory;

            fixture.SetupMoveDirectoryOrFileAsync(sutContract, directory);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                sut.MoveItem(sutContract, sutContract.Name, directory);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_NewDirectoryItem_Succeeds()
        {
            var newName = "NewFile.ext";
            var directory = fixture.TargetDirectory;

            fixture.SetupNewDirectoryItemAsync(directory, newName);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                sut.NewDirectoryItem(directory, newName);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_NewFileItem_Succeeds()
        {
            var newName = "NewDirectory";
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var directory = fixture.TargetDirectory;

            fixture.SetupNewFileItemAsync(directory, newName, fileContent, encryptionKey);

            using (var sut = fixture.Create(apiKey, encryptionKey))
            using (var stream = new MemoryStream(fileContent)) {
                sut.NewFileItem(directory, newName, stream);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_RemoveDirectoryItem_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            fixture.SetupRemoveDirectoryOrFileAsync(sutContract, true);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                sut.RemoveItem(sutContract, true);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_RemoveFileItem_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupRemoveDirectoryOrFileAsync(sutContract, false);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                sut.RemoveItem(sutContract, false);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void CloudDrive_SetContent_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupSetContentAsync(sutContract, fileContent, encryptionKey);

            using (var sut = fixture.Create(apiKey, encryptionKey))
            using (var stream = new MemoryStream(fileContent)) {
                sut.SetContent(sutContract, stream);
            }

            fixture.VerifyAll();
        }
    }
}
