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
using System.Composition;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface.Composition;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class GatewayManagerTests
    {
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Create_WhereGatewaysAreEmpty_Succeeds()
        {
            var asyncGateways = Enumerable.Empty<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>>();
            var syncGateways = Enumerable.Empty<ExportFactory<ICloudGateway, CloudGatewayMetadata>>();

            var sut = new GatewayManager(asyncGateways, syncGateways);

            Assert.IsNotNull(sut, "GatewayManager creation failed");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Create_WhereGatewaysAreSpecified_Succeeds()
        {
            var asyncGateways = new[] { new ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>(() => Fixture.GetAsyncCreator(), Fixture.GetAsyncGatewayMetadata()) };
            var syncGateways = new[] { new ExportFactory<ICloudGateway, CloudGatewayMetadata>(() => Fixture.GetSyncCreator(), Fixture.GetSyncGatewayMetadata()) };

            var sut = new GatewayManager(asyncGateways, syncGateways);

            Assert.IsNotNull(sut, "GatewayManager creation failed");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void TryGetAsyncCloudGatewayForSchema_WhereNoGatewaysAreDefined_Fails()
        {
            var asyncGateways = Enumerable.Empty<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>>();
            var syncGateways = Enumerable.Empty<ExportFactory<ICloudGateway, CloudGatewayMetadata>>();

            var sut = new GatewayManager(asyncGateways, syncGateways);
            IAsyncCloudGateway asyncGateway = null;
            var result = sut.TryGetAsyncCloudGatewayForSchema("testAsync", out asyncGateway);

            Assert.IsFalse(result, "Unconfigured AsyncCloudGateway returned");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void TryGetCloudGatewayForSchema_WhereNoGatewaysAreDefined_Fails()
        {
            var asyncGateways = Enumerable.Empty<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>>();
            var syncGateways = Enumerable.Empty<ExportFactory<ICloudGateway, CloudGatewayMetadata>>();

            var sut = new GatewayManager(asyncGateways, syncGateways);
            ICloudGateway syncGateway = null;
            var result = sut.TryGetCloudGatewayForSchema("testSync", out syncGateway);

            Assert.IsFalse(result, "Unconfigured CloudGateway returned");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void TryGetAsyncCloudGatewayForSchema_WhereGatewayIsUndefined_Fails()
        {
            var asyncGateways = new[] { new ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>(() => Fixture.GetAsyncCreator(), Fixture.GetAsyncGatewayMetadata()) };
            var syncGateways = Enumerable.Empty<ExportFactory<ICloudGateway, CloudGatewayMetadata>>();

            var sut = new GatewayManager(asyncGateways, syncGateways);
            IAsyncCloudGateway asyncGateway = null;
            var result = sut.TryGetAsyncCloudGatewayForSchema("undefinedAsync", out asyncGateway);

            Assert.IsFalse(result, "Unconfigured AsyncCloudGateway returned");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void TryGetCloudGatewayForSchema_WhereGatewayIsUndefined_Fails()
        {
            var asyncGateways = Enumerable.Empty<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>>();
            var syncGateways = new[] { new ExportFactory<ICloudGateway, CloudGatewayMetadata>(() => Fixture.GetSyncCreator(), Fixture.GetSyncGatewayMetadata()) };

            var sut = new GatewayManager(asyncGateways, syncGateways);
            ICloudGateway syncGateway = null;
            var result = sut.TryGetCloudGatewayForSchema("undefinedSync", out syncGateway);

            Assert.IsFalse(result, "Unconfigured CloudGateway returned");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void TryGetAsyncCloudGatewayForSchema_WhereGatewayIsDefined_Succeeds()
        {
            var asyncGateways = new[] { new ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>(() => Fixture.GetAsyncCreator(), Fixture.GetAsyncGatewayMetadata()) };
            var syncGateways = Enumerable.Empty<ExportFactory<ICloudGateway, CloudGatewayMetadata>>();

            var sut = new GatewayManager(asyncGateways, syncGateways);
            IAsyncCloudGateway asyncGateway = null;
            var result = sut.TryGetAsyncCloudGatewayForSchema("testAsync", out asyncGateway);

            Assert.IsTrue(result, "Configured AsyncCloudGateway not returned");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void TryGetCloudGatewayForSchema_WhereGatewayIsDefined_Succeeds()
        {
            var asyncGateways = Enumerable.Empty<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>>();
            var syncGateways = new[] { new ExportFactory<ICloudGateway, CloudGatewayMetadata>(() => Fixture.GetSyncCreator(), Fixture.GetSyncGatewayMetadata()) };

            var sut = new GatewayManager(asyncGateways, syncGateways);
            ICloudGateway syncGateway = null;
            var result = sut.TryGetCloudGatewayForSchema("testSync", out syncGateway);

            Assert.IsTrue(result, "Configured CloudGateway not returned");
        }
    }
}
