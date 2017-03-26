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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class AsyncCloudDriveTests
    {
        private Fixture fixture;

        private const string apiKey = "<MyApiKey>";

        private const string encryptionKey = "<MyEncryptionKey>";

#pragma warning disable 649
        private readonly IDictionary<string, string> parameters;
#pragma warning restore 649

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            ExportProvider.ResetComposition();
        }

        [TestInitialize]
        public void Initialize()
        {
            fixture = Fixture.Initialize();
        }

        [TestMethod]
        public void AsyncCloudDrive_Create_Succeeds()
        {
            using (var result = fixture.Create(apiKey, encryptionKey)) {
                Assert.IsNotNull(result, "Missing result");
            }
        }

        [TestMethod]
        public void AsyncCloudDrive_TryAuthenticate_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);
            fixture.SetupTryAuthenticate(apiKey, parameters);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.TryAuthenticate();

                Assert.IsTrue(result, "Unexpected result");
            }
        }

        [TestMethod]
        public void AsyncCloudDrive_GetFree_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.Free;

                Assert.AreEqual(Fixture.FREE_SPACE, result, "Unexpected Free value");
            }
        }

        [TestMethod]
        public void AsyncCloudDrive_GetUsed_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.Used;

                Assert.AreEqual(Fixture.USED_SPACE, result, "Unexpected Used value");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void AsyncCloudDrive_GetFree_WhereGetDriveFails_Throws()
        {
            fixture.SetupGetDriveAsyncThrows<ApplicationException>(apiKey, parameters);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.Free;
            }
        }

        [TestMethod]
        public void AsyncCloudDrive_GetRoot_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);
            fixture.SetupGetRootAsync(apiKey, parameters);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.GetRoot();

                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}{Path.VolumeSeparatorChar}{Path.DirectorySeparatorChar}".ToString(CultureInfo.CurrentCulture), result.FullName, "Unexpected root name");
            }
        }

        [TestMethod]
        public void AsyncCloudDrive_GetDisplayRoot_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);
            fixture.SetupGetRootAsync(apiKey, parameters);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.DisplayRoot;

                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}".ToString(CultureInfo.CurrentCulture), result, "Unexpected DisplayRoot value");
            }
        }

        [TestMethod]
        public void AsyncCloudDrive_GetChildItem_WhereEncryptionKeyIsEmpty_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);
            fixture.SetupGetRootAsync(apiKey, parameters);
            fixture.SetupGetRootDirectoryItemsAsync();

            using (var sut = fixture.Create(apiKey, string.Empty)) {
                var result = sut.GetChildItem(sut.GetRoot()).ToList();

                CollectionAssert.AreEqual(fixture.RootDirectoryItems, result, "Mismatched result");
            }
        }

        [TestMethod]
        public void AsyncCloudDrive_GetChildItem_WhereEncryptionKeyIsSet_Succeeds()
        {
            fixture.SetupGetDriveAsync(apiKey, parameters);
            fixture.SetupGetRootAsync(apiKey, parameters);
            fixture.SetupGetRootDirectoryItemsAsync(encryptionKey);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                var result = sut.GetChildItem(sut.GetRoot()).ToList();

                CollectionAssert.AreEqual(fixture.RootDirectoryItems, result, "Mismatched result");
            }
        }

        [TestMethod]
        public void AsyncCloudDrive_GetContent_Succeeds()
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
        public void AsyncCloudDrive_GetContent_WhereContentIsUnencrypted_Succeeds()
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
        public void AsyncCloudDrive_GetContent_WhereContentIsNotSeekable_Succeeds()
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
        public void AsyncCloudDrive_MoveDirectoryItem_Succeeds()
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
        public void AsyncCloudDrive_MoveFileItem_Succeeds()
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
        public void AsyncCloudDrive_NewDirectoryItem_Succeeds()
        {
            const string newName = "NewDirectory";
            var directory = fixture.TargetDirectory;

            fixture.SetupNewDirectoryItemAsync(directory, newName);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                sut.NewDirectoryItem(directory, newName);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void AsyncCloudDrive_NewFileItem_Succeeds()
        {
            const string newName = "NewFile.ext";
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
        public void AsyncCloudDrive_NewFileItem_WhereContentIsEmpty_Succeeds()
        {
            const string newName = "NewFile.ext";
            var directory = fixture.TargetDirectory;

            FileInfoContract contract;
            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                contract = sut.NewFileItem(directory, newName, Stream.Null);
            }

            Assert.IsInstanceOfType(contract, typeof(ProxyFileInfoContract));

            fixture.VerifyAll();
        }

        [TestMethod]
        public void AsyncCloudDrive_RemoveDirectoryItem_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            fixture.SetupRemoveDirectoryOrFileAsync(sutContract, true);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                sut.RemoveItem(sutContract, true);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void AsyncCloudDrive_RemoveFileItem_Succeeds()
        {
            var sutContract = fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            fixture.SetupRemoveDirectoryOrFileAsync(sutContract, false);

            using (var sut = fixture.Create(apiKey, encryptionKey)) {
                sut.RemoveItem(sutContract, false);
            }

            fixture.VerifyAll();
        }

        [TestMethod]
        public void AsyncCloudDrive_SetContent_Succeeds()
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
