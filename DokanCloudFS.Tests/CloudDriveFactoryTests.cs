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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using IgorSoft.DokanCloudFS.Parameters;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class CloudDriveFactoryTests
    {
        private const string schema = "test";

        private const string user = "testUser";

        private const string root = "Z";

        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = Fixture.Initialize();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CreateCloudDrive_WhereGatewayManagerIsNotInitialized_Throws()
        {
            var sut = new CloudDriveFactory();

            sut.CreateCloudDrive(schema, user, root, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void CreateCloudDrive_WhereGatewayIsNotRegistered_Throws()
        {
            fixture.SetupTryGetAsyncCloudGatewayForSchema(schema, false);
            fixture.SetupTryGetCloudGatewayForSchema(schema, false);
            var sut = new CloudDriveFactory() { GatewayManager = fixture.GetGatewayManager() };

            sut.CreateCloudDrive(schema, user, root, new CloudDriveParameters());
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateCloudDrive_WhereAsyncGatewayIsRegistered_Succeeds()
        {
            fixture.SetupTryGetAsyncCloudGatewayForSchema(schema);

            var sut = new CloudDriveFactory() { GatewayManager = fixture.GetGatewayManager() };

            using (var result = sut.CreateCloudDrive(schema, user, root, new CloudDriveParameters())) {
                Assert.IsInstanceOfType(result, typeof(AsyncCloudDrive), "Unexpected result type");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateCloudDrive_WhereGatewayIsRegistered_Succeeds()
        {
            const string schema = "test";
            fixture.SetupTryGetAsyncCloudGatewayForSchema(schema, false);
            fixture.SetupTryGetCloudGatewayForSchema(schema);

            var sut = new CloudDriveFactory() { GatewayManager = fixture.GetGatewayManager() };

            using (var result = sut.CreateCloudDrive(schema, user, root, new CloudDriveParameters())) {
                Assert.IsInstanceOfType(result, typeof(CloudDrive), "Unexpected result type");
            }
        }
    }
}
