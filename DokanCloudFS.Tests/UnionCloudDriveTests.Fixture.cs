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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Configuration;
using Moq;

namespace IgorSoft.DokanCloudFS.Tests
{
    public partial class UnionCloudDriveTests
    {
        internal class Fixture
        {
            public const string MOUNT_POINT = "Z";

            public const string SCHEMA = "mock";

            public const string USER_NAME = "IgorDev";

            public const long FREE_SPACE = 64 * 1 << 20;

            public const long USED_SPACE = 36 * 1 << 20;

            private static readonly DateTimeOffset defaultTime = "2017-01-01 00:00:00".ToDateTime();

            private readonly Mock<IAsyncCloudGateway>[] asyncGateways;

            private readonly Mock<ICloudGateway>[] gateways;

            private readonly IDictionary<Mock, RootDirectoryInfoContract> rootDirectories;

            private readonly RootName rootName = new RootName(SCHEMA, USER_NAME, MOUNT_POINT);

            private readonly MockRepository mockRepository = new MockRepository(MockBehavior.Strict);

            public static Fixture Initialize(int numAsyncGateways, int numGateways) => new Fixture(numAsyncGateways, numGateways);

            private Fixture(int numAsyncGateways, int numGateways)
            {
                asyncGateways = Enumerable.Range(0, numAsyncGateways).Select(i => mockRepository.Create<IAsyncCloudGateway>()).ToArray();
                gateways = Enumerable.Range(0, numGateways).Select(i => mockRepository.Create<ICloudGateway>()).ToArray();
                rootDirectories = new Dictionary<Mock, RootDirectoryInfoContract>();
                foreach (var mock in asyncGateways.Cast<Mock>().Concat(gateways.Cast<Mock>()))
                    rootDirectories.Add(mock, new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), defaultTime, defaultTime)
                    {
                        Drive = new DriveInfoContract(MOUNT_POINT, FREE_SPACE, USED_SPACE)
                    });
            }

            public CloudDriveParameters[] CreateParameters(int count, string apiKeyPattern, string encryptionKeyPattern)
            {
                return Enumerable.Range(0, count).Select(i => new CloudDriveParameters()
                {
                    ApiKey = string.Format(CultureInfo.InvariantCulture, apiKeyPattern, i),
                    EncryptionKey = string.Format(CultureInfo.InvariantCulture, encryptionKeyPattern, i)
                }).ToArray();
            }

            public UnionCloudDrive Create(CloudDriveParameters[] asyncParameters, CloudDriveParameters[] parameters)
            {
                return new UnionCloudDrive(new RootName(SCHEMA, USER_NAME, MOUNT_POINT),
                    asyncGateways.Zip(asyncParameters, (gw, ps) => new GatewayConfiguration<IAsyncCloudGateway>(gw.Object, ps)).ToArray(),
                    gateways.Zip(parameters, (pw, ps) => new GatewayConfiguration<ICloudGateway>(pw.Object, ps)).ToArray()
                );
            }

            public void SetupTryAuthenticate(CloudDriveParameters[] asyncParameters, CloudDriveParameters[] parameters, bool result = true)
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(parameters, (gw, ps) =>
                    gw
                        .Setup(g => g.TryAuthenticateAsync(rootName, ps.ApiKey, ps.Parameters))
                        .Returns(Task.FromResult(result))
                ).ToArray();
                gateways.Zip(parameters, (gw, ps) =>
                    gw
                        .Setup(g => g.TryAuthenticate(rootName, ps.ApiKey, ps.Parameters))
                        .Returns(result)
                ).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }

            public void SetupGetDrive(CloudDriveParameters[] asyncParameters, CloudDriveParameters[] parameters)
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(parameters, (gw, ps) =>
                    gw
                        .Setup(g => g.GetDriveAsync(rootName, ps.ApiKey, ps.Parameters))
                        .Returns(Task.FromResult(rootDirectories[gw].Drive))
                ).ToArray();
                gateways.Zip(parameters, (gw, ps) =>
                    gw
                        .Setup(g => g.GetDrive(rootName, ps.ApiKey, ps.Parameters))
                        .Returns(rootDirectories[gw].Drive)
                ).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }

            public void SetupGetDriveThrows<TException>(CloudDriveParameters[] asyncParameters, CloudDriveParameters[] parameters)
                where TException : Exception, new()
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(parameters, (gw, ps) =>
                    gw
                        .Setup(g => g.GetDriveAsync(rootName, ps.ApiKey, ps.Parameters))
                        .Throws(new AggregateException(Activator.CreateInstance<TException>()))
                ).ToArray();
                gateways.Zip(parameters, (gw, ps) =>
                    gw
                        .Setup(g => g.GetDrive(rootName, ps.ApiKey, ps.Parameters))
                        .Throws(new AggregateException(Activator.CreateInstance<TException>()))
                ).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }

            public void SetupGetRoot(CloudDriveParameters[] asyncParameters, CloudDriveParameters[] parameters)
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(parameters, (gw, ps) =>
                    gw
                        .Setup(g => g.GetRootAsync(rootName, ps.ApiKey, ps.Parameters))
                        .Returns(Task.FromResult(rootDirectories[gw]))
                ).ToArray();
                gateways.Zip(parameters, (gw, ps) =>
                    gw
                        .Setup(g => g.GetRoot(rootName, ps.ApiKey, ps.Parameters))
                        .Returns(rootDirectories[gw])
                ).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }
        }
    }
}
