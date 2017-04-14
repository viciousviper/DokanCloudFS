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
using IgorSoft.DokanCloudFS.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public partial class UnionCloudDriveTests
    {
        private const string apiKeyPattern = "<MyApiKey>[{0}]";

        private const string encryptionKeyPattern = "<MyEncryptionKey>[{0}]";

        private const int NUM_ASYNC_GATEWAYS = 5;

        private const int NUM_GATEWAYS = 5;

        private Fixture fixture;

        private CloudDriveParameters[] asyncParameters;

        private CloudDriveParameters[] parameters;

        [TestInitialize]
        public void Initialize()
        {
            fixture = Fixture.Initialize(NUM_ASYNC_GATEWAYS, NUM_GATEWAYS);

            asyncParameters = fixture.CreateParameters(NUM_ASYNC_GATEWAYS, apiKeyPattern, encryptionKeyPattern);
            parameters = fixture.CreateParameters(NUM_ASYNC_GATEWAYS, apiKeyPattern, encryptionKeyPattern);
        }

        [TestMethod]
        public void UnionCloudDrive_TryAuthenticate_Succeeds()
        {
            fixture.SetupGetDrive(asyncParameters, parameters);
            fixture.SetupTryAuthenticate(asyncParameters, parameters);

            using (var sut = fixture.Create(asyncParameters, parameters)) {
                var result = sut.TryAuthenticate();

                Assert.IsTrue(result, "Unexpected result");
            }
        }

        [TestMethod]
        public void UnionCloudDrive_TryAuthenticate_WhereGatewayAuthenticationFails_Fails()
        {
            fixture.SetupGetDrive(asyncParameters, parameters);
            fixture.SetupTryAuthenticate(asyncParameters, parameters, false);

            using (var sut = fixture.Create(asyncParameters, parameters)) {
                var result = sut.TryAuthenticate();

                Assert.IsFalse(result, "Unexpected result");
            }
        }
    }
}
