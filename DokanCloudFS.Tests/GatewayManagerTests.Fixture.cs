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
using IgorSoft.CloudFS.Interface.Composition;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class GatewayManagerTests
    {
        internal static class Fixture
        {
            public static Tuple<IAsyncCloudGateway, Action> GetAsyncCreator() => new Tuple<IAsyncCloudGateway, Action>(null, () => { });

            public static Tuple<ICloudGateway, Action> GetSyncCreator() => new Tuple<ICloudGateway, Action>(null, () => { });

            public static CloudGatewayMetadata GetAsyncGatewayMetadata() => new CloudGatewayMetadata(new Dictionary<string, object>() {
                { "CloudService", "testAsync" }
            });

            public static CloudGatewayMetadata GetSyncGatewayMetadata() => new CloudGatewayMetadata(new Dictionary<string, object>() {
                { "CloudService", "testSync" }
            });
        }
    }
}
