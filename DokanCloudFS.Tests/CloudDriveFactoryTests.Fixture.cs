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
using Moq;
using IgorSoft.CloudFS.Interface.Composition;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class CloudDriveFactoryTests
    {
        internal class Fixture
        {
            private readonly Mock<IGatewayManager> gatewayManager;

            public IGatewayManager GetGatewayManager() => gatewayManager.Object;

            internal static Fixture Initialize() => new Fixture();

            private Fixture()
            {
                gatewayManager = new Mock<IGatewayManager>(MockBehavior.Strict);
            }

            internal void SetupTryGetAsyncCloudGatewayForSchema(string schema, bool result = true)
            {
                var asyncGateway = new Mock<IAsyncCloudGateway>().Object;
                gatewayManager
                    .Setup(g => g.TryGetAsyncCloudGatewayForSchema(schema, out asyncGateway))
                    .Returns(result);
            }

            internal void SetupTryGetCloudGatewayForSchema(string schema, bool result = true)
            {
                var syncGateway = new Mock<ICloudGateway>().Object;
                gatewayManager
                    .Setup(g => g.TryGetCloudGatewayForSchema(schema, out syncGateway))
                    .Returns(result);
            }
        }
    }
}
