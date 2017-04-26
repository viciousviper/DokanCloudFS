/*
The MIT License(MIT)

Copyright(c) 2017 IgorSoft

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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Configuration;
using IgorSoft.DokanCloudFS.Nodes;
using System.Collections.Generic;

namespace IgorSoft.DokanCloudFS.Tests.Drives
{
    [TestClass]
    public partial class UnionCloudDriveTests
    {
        private const string rootNamePattern = "my{0}schema{1}@My{0}User{1}";

        private const string apiKeyPattern = "<My{0}ApiKey>[{1}]";

        private const string encryptionKeyPattern = "<My{0}EncryptionKey>[{1}]";

        private const int NUM_ASYNC_CONFIGURATIONS = 5;

        private const int NUM_CONFIGURATIONS = 5;

        private Fixture fixture;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            ExportProvider.ResetComposition();
        }

        [TestInitialize]
        public void Initialize()
        {
            fixture = Fixture.Initialize(NUM_ASYNC_CONFIGURATIONS, NUM_CONFIGURATIONS, rootNamePattern, apiKeyPattern, encryptionKeyPattern);
        }

        [TestMethod]
        public void UnionCloudDrive_Create_Succeeds()
        {
            using (var result = fixture.Create()) {
                Assert.IsNotNull(result, "Missing result");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_TryAuthenticate_Succeeds()
        {
            fixture.SetupTryAuthenticate();

            using (var sut = fixture.Create()) {
                var result = sut.TryAuthenticate();

                Assert.IsTrue(result, "Unexpected result");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_TryAuthenticate_WhereGatewayAuthenticationFails_Fails()
        {
            fixture.SetupTryAuthenticate(false);

            using (var sut = fixture.Create()) {
                var result = sut.TryAuthenticate();

                Assert.IsFalse(result, "Unexpected result");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetFree_Succeeds()
        {
            fixture.SetupGetDrive();

            using (var sut = fixture.Create()) {
                var result = sut.Free;

                Assert.AreEqual(Enumerable.Range(0, NUM_ASYNC_CONFIGURATIONS).Sum(i => Fixture.ASYNC_DRIVE_FREE_SPACE << i) + Enumerable.Range(0, NUM_CONFIGURATIONS).Sum(i => Fixture.DRIVE_FREE_SPACE << i), result, "Unexpected Free value");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetUsed_Succeeds()
        {
            fixture.SetupGetDrive();

            using (var sut = fixture.Create()) {
                var result = sut.Used;

                Assert.AreEqual(Enumerable.Range(0, NUM_ASYNC_CONFIGURATIONS).Sum(i => Fixture.ASYNC_DRIVE_USED_SPACE << i) + Enumerable.Range(0, NUM_CONFIGURATIONS).Sum(i => Fixture.DRIVE_USED_SPACE << i), result, "Unexpected Used value");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void UnionCloudDrive_GetFree_WhereGetDriveFails_Throws()
        {
            fixture.SetupGetDriveThrows<ApplicationException>();

            using (var sut = fixture.Create()) {
                var result = sut.Free;
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetRoot_Succeeds()
        {
            fixture.SetupGetDrive();
            fixture.SetupGetRoot();

            using (var sut = fixture.Create()) {
                var result = sut.GetRoot();

                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}{Path.VolumeSeparatorChar}{Path.DirectorySeparatorChar}".ToString(CultureInfo.CurrentCulture), result.FullName, "Unexpected root name");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetDisplayRoot_Succeeds()
        {
            fixture.SetupGetDrive();
            fixture.SetupGetRoot();

            using (var sut = fixture.Create()) {
                var result = sut.DisplayRoot;

                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}".ToString(CultureInfo.CurrentCulture), result, "Unexpected DisplayRoot value");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetChildItem_WhereEncryptionKeyIsEmpty_Succeeds()
        {
            fixture.ForEachConfiguration(cfg => cfg.EncryptionKey = null);

            fixture.SetupGetDrive();
            fixture.SetupGetRoot();
            fixture.SetupGetRootDirectoryItems();

            using (var sut = fixture.Create()) {
                var result = sut.GetChildItem(sut.GetRoot()).ToList();

                var resultNames = result.Select(f => f.Name).ToArray();
                CollectionAssert.AreEqual(resultNames.Distinct().ToArray(), resultNames, "Unexpected duplicates");

                var expectedNames = fixture.GetUniqueFiles(fixture.RootDirectoryItems).Concat(fixture.GetSharedFiles(fixture.RootDirectoryItems)).Select(f => f.Name)
                    .Concat(fixture.GetUniqueDirectories(fixture.RootDirectoryItems).Concat(fixture.GetSharedDirectories(fixture.RootDirectoryItems)).Select(d => d.Name))
                    .ToArray();
                CollectionAssert.AreEquivalent(expectedNames, resultNames);
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetChildItem_WhereEncryptionKeyIsSet_Succeeds()
        {
            fixture.SetupGetDrive();
            fixture.SetupGetRoot();
            fixture.SetupGetRootDirectoryItems();

            using (var sut = fixture.Create()) {
                var result = sut.GetChildItem(sut.GetRoot()).ToList();

                var resultNames = result.Select(f => f.Name).ToArray();
                CollectionAssert.AreEqual(resultNames.Distinct().ToArray(), resultNames, "Unexpected duplicates");

                var expectedNames = fixture.GetUniqueFiles(fixture.RootDirectoryItems).Concat(fixture.GetSharedFiles(fixture.RootDirectoryItems)).Select(f => f.Name)
                    .Concat(fixture.GetUniqueDirectories(fixture.RootDirectoryItems).Concat(fixture.GetSharedDirectories(fixture.RootDirectoryItems)).Select(d => d.Name))
                    .ToArray();
                CollectionAssert.AreEquivalent(expectedNames, resultNames);
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetContent_Succeeds()
        {
            var fileContents = fixture.SelectByConfiguration(cfg => Encoding.Default.GetBytes($"Why did the chicken cross the road?:{cfg.RootName}"));
            var encryptionKeys = fixture.SelectByConfiguration(cfg => cfg.EncryptionKey);
            var sutContract = new UnionFileInfo(fixture.SelectByConfiguration(cfg => fixture.RootDirectoryItems[cfg].OfType<FileInfoContract>().First()));

            fixture.SetupGetContent(sutContract, fileContents, encryptionKeys);

            var buffers = new Dictionary<CloudDriveConfiguration, byte[]>();
            using (var sut = fixture.Create()) {
                fixture.ForEachConfiguration(cfg => {
                    using (var stream = sut.GetContent(sutContract, cfg)) {
                        var buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        buffers.Add(cfg, buffer);
                    }
                });
            }

            fixture.ForEachConfiguration(cfg => {
                var fileContent = fileContents[cfg];
                var buffer = buffers[cfg];
                Assert.AreEqual(fileContent.Length, buffer.Length, $"Invalid content size in configuration '{cfg.RootName}'");
                CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");
            });

            fixture.VerifyAll();
        }

        [TestMethod]
        public void UnionCloudDrive_GetContent_WhereContentIsUnencrypted_Succeeds()
        {
            var fileContents = fixture.SelectByConfiguration(cfg => Encoding.Default.GetBytes($"Why did the chicken cross the road?:{cfg.RootName}"));
            var sutContract = new UnionFileInfo(fixture.SelectByConfiguration(cfg => fixture.RootDirectoryItems[cfg].OfType<FileInfoContract>().First()));

            fixture.SetupGetContent(sutContract, fileContents);

            var buffers = new Dictionary<CloudDriveConfiguration, byte[]>();
            using (var sut = fixture.Create()) {
                fixture.ForEachConfiguration(cfg => {
                    using (var stream = sut.GetContent(sutContract, cfg)) {
                        var buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        buffers.Add(cfg, buffer);
                    }
                });
            }

            fixture.ForEachConfiguration(cfg => {
                var fileContent = fileContents[cfg];
                var buffer = buffers[cfg];
                Assert.AreEqual(fileContent.Length, buffer.Length, $"Invalid content size in configuration '{cfg.RootName}'");
                CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");
            });

            fixture.VerifyAll();
        }

        [TestMethod]
        public void UnionCloudDrive_GetContent_WhereContentIsNotSeekable_Succeeds()
        {
            var fileContents = fixture.SelectByConfiguration(cfg => Encoding.Default.GetBytes($"Why did the chicken cross the road?:{cfg.RootName}"));
            var encryptionKeys = fixture.SelectByConfiguration(cfg => cfg.EncryptionKey);
            var sutContract = new UnionFileInfo(fixture.SelectByConfiguration(cfg => fixture.RootDirectoryItems[cfg].OfType<FileInfoContract>().First()));

            fixture.SetupGetContent(sutContract, fileContents, encryptionKeys, false);

            var buffers = new Dictionary<CloudDriveConfiguration, byte[]>();
            using (var sut = fixture.Create()) {
                fixture.ForEachConfiguration(cfg => {
                    using (var stream = sut.GetContent(sutContract, cfg)) {
                        var buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        buffers.Add(cfg, buffer);
                    }
                });
            }

            fixture.ForEachConfiguration(cfg => {
                var fileContent = fileContents[cfg];
                var buffer = buffers[cfg];
                Assert.AreEqual(fileContent.Length, buffer.Length, $"Invalid content size in configuration '{cfg.RootName}'");
                CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");
            });

            fixture.VerifyAll();
        }
    }
}
