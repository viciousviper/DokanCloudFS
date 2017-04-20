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
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.DokanCloudFS.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public partial class UnionCloudDriveTests
    {
        private const string rootNamePattern = "my{0}schema{1}@My{0}User{1}";

        private const string apiKeyPattern = "<My{0}ApiKey>[{1}]";

        private const string encryptionKeyPattern = "<My{0}EncryptionKey>[{1}]";

        private const int NUM_ASYNC_GATEWAYS = 5;

        private const int NUM_GATEWAYS = 5;

        private Fixture fixture;

        private CloudDriveConfiguration[] asyncConfigs;

        private CloudDriveConfiguration[] configs;

        [TestInitialize]
        public void Initialize()
        {
            fixture = Fixture.Initialize(NUM_ASYNC_GATEWAYS, NUM_GATEWAYS);

            asyncConfigs = fixture.CreateConfigurations(NUM_ASYNC_GATEWAYS, "async", rootNamePattern, apiKeyPattern, encryptionKeyPattern);
            configs = fixture.CreateConfigurations(NUM_ASYNC_GATEWAYS, string.Empty, rootNamePattern, apiKeyPattern, encryptionKeyPattern);
        }

        [TestMethod]
        public void UnionCloudDrive_Create_Succeeds()
        {
            using (var result = fixture.Create(asyncConfigs, configs)) {
                Assert.IsNotNull(result, "Missing result");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_TryAuthenticate_Succeeds()
        {
            fixture.SetupTryAuthenticate(asyncConfigs, configs);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.TryAuthenticate();

                Assert.IsTrue(result, "Unexpected result");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_TryAuthenticate_WhereGatewayAuthenticationFails_Fails()
        {
            fixture.SetupTryAuthenticate(asyncConfigs, configs, false);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.TryAuthenticate();

                Assert.IsFalse(result, "Unexpected result");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetFree_Succeeds()
        {
            fixture.SetupGetDrive(asyncConfigs, configs);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.Free;

                Assert.AreEqual(Enumerable.Range(0, asyncConfigs.Length).Sum(i => Fixture.ASYNC_DRIVE_FREE_SPACE << i) + Enumerable.Range(0, configs.Length).Sum(i => Fixture.DRIVE_FREE_SPACE << i), result, "Unexpected Free value");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetUsed_Succeeds()
        {
            fixture.SetupGetDrive(asyncConfigs, configs);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.Used;

                Assert.AreEqual(Enumerable.Range(0, asyncConfigs.Length).Sum(i => Fixture.ASYNC_DRIVE_USED_SPACE << i) + Enumerable.Range(0, configs.Length).Sum(i => Fixture.DRIVE_USED_SPACE << i), result, "Unexpected Used value");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void UnionCloudDrive_GetFree_WhereGetDriveFails_Throws()
        {
            fixture.SetupGetDriveThrows<ApplicationException>(asyncConfigs, configs);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.Free;
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetRoot_Succeeds()
        {
            fixture.SetupGetDrive(asyncConfigs, configs);
            fixture.SetupGetRoot(asyncConfigs, configs);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.GetRoot();

                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}{Path.VolumeSeparatorChar}{Path.DirectorySeparatorChar}".ToString(CultureInfo.CurrentCulture), result.FullName, "Unexpected root name");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_GetDisplayRoot_Succeeds()
        {
            fixture.SetupGetDrive(asyncConfigs, configs);
            fixture.SetupGetRoot(asyncConfigs, configs);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.DisplayRoot;

                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.USER_NAME}|{Fixture.MOUNT_POINT}".ToString(CultureInfo.CurrentCulture), result, "Unexpected DisplayRoot value");
            }
        }

        [TestMethod]
        public void CloudDrive_GetChildItem_WhereEncryptionKeyIsEmpty_Succeeds()
        {
            foreach (var p in asyncConfigs.Concat(configs))
                p.EncryptionKey = null;

            fixture.SetupGetDrive(asyncConfigs, configs);
            fixture.SetupGetRoot(asyncConfigs, configs);
            fixture.SetupGetRootDirectoryItems(asyncConfigs, configs);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.GetChildItem(sut.GetRoot()).ToList();

                var resultNames = result.Select(f => f.Name).ToArray();
                //CollectionAssert.AreEqual(fixture.RootDirectoryItems.Values.SelectMany(f => f).ToArray(), result, "Mismatched result");
                CollectionAssert.AreEqual(resultNames.Distinct().ToArray(), resultNames, "Unexpected duplicates");
            }
        }

        [TestMethod]
        public void CloudDrive_GetChildItem_WhereEncryptionKeyIsSet_Succeeds()
        {
            fixture.SetupGetDrive(asyncConfigs, configs);
            fixture.SetupGetRoot(asyncConfigs, configs);
            fixture.SetupGetRootDirectoryItems(asyncConfigs, configs);

            using (var sut = fixture.Create(asyncConfigs, configs)) {
                var result = sut.GetChildItem(sut.GetRoot()).ToList();

                CollectionAssert.AreEqual(fixture.RootDirectoryItems.Values.SelectMany(f => f).ToArray(), result, "Mismatched result");
            }
        }
    }
}
