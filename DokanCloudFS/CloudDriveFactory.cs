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
using System.Globalization;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.DokanCloudFS.Parameters;

namespace IgorSoft.DokanCloudFS
{
    internal sealed class CloudDriveFactory
    {
        [Import]
        internal IGatewayManager GatewayManager { get; set; }

        internal ICloudDrive CreateCloudDrive(string schema, string userName, string root, CloudDriveParameters parameters)
        {
            if (GatewayManager == null)
                throw new InvalidOperationException($"{nameof(GatewayManager)} not initialized".ToString(CultureInfo.CurrentCulture));

            var rootName = new RootName(schema, userName, root);

            var asyncGateway = default(IAsyncCloudGateway);
            if (GatewayManager.TryGetAsyncCloudGatewayForSchema(rootName.Schema, out asyncGateway))
                return new AsyncCloudDrive(rootName, asyncGateway, parameters);

            var gateway = default(ICloudGateway);
            if (GatewayManager.TryGetCloudGatewayForSchema(rootName.Schema, out gateway))
                return new CloudDrive(rootName, gateway, parameters);

            throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.NoGatewayForSchema, rootName.Schema));
        }
    }
}
