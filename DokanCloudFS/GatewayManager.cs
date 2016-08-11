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
using System.Composition;
using IgorSoft.CloudFS.Interface.Composition;

namespace IgorSoft.DokanCloudFS
{
    [Export(typeof(IGatewayManager))]
    internal sealed class GatewayManager : IGatewayManager
    {
        private readonly IDictionary<string, ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>> asyncGateways = new Dictionary<string, ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>>();

        private readonly IDictionary<string, ExportFactory<ICloudGateway, CloudGatewayMetadata>> gateways = new Dictionary<string, ExportFactory<ICloudGateway, CloudGatewayMetadata>>();

        [ImportingConstructor]
        public GatewayManager([ImportMany] IEnumerable<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>> asyncGateways, [ImportMany] IEnumerable<ExportFactory<ICloudGateway, CloudGatewayMetadata>> syncGateways)
        {
            foreach (var asyncGateway in asyncGateways)
                this.asyncGateways.Add(asyncGateway.Metadata.CloudService, asyncGateway);
            foreach (var gateway in syncGateways)
                this.gateways.Add(gateway.Metadata.CloudService, gateway);
        }

        public bool TryGetAsyncCloudGatewayForSchema(string cloudService, out IAsyncCloudGateway asyncGateway)
        {
            var result = default(ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>);
            if (asyncGateways.TryGetValue(cloudService, out result)) {
                using (var export = result.CreateExport()) {
                    asyncGateway = export.Value;
                    return true;
                }
            } else {
                asyncGateway = null;
                return false;
            }
        }

        public bool TryGetCloudGatewayForSchema(string cloudService, out ICloudGateway gateway)
        {
            var result = default(ExportFactory<ICloudGateway, CloudGatewayMetadata>);
            if (gateways.TryGetValue(cloudService, out result)) {
                using (var export = result.CreateExport()) {
                    gateway = export.Value;
                    return true;
                }
            } else {
                gateway = null;
                return false;
            }
        }
    }
}