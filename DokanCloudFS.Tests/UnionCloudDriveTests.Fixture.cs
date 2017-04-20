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
using IgorSoft.DokanCloudFS.IO;
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

            public const long ASYNC_DRIVE_FREE_SPACE = 1 << 30;

            public const long DRIVE_FREE_SPACE = 1 << 25;

            public const long ASYNC_DRIVE_USED_SPACE = 1 << 20;

            public const long DRIVE_USED_SPACE = 1 << 15;

            private static readonly DateTimeOffset defaultTime = "2017-01-01 00:00:00".ToDateTime();

            private readonly Mock<IAsyncCloudGateway>[] asyncGateways;

            private readonly Mock<ICloudGateway>[] gateways;

            private readonly IDictionary<Mock, RootDirectoryInfoContract> rootDirectories;

            private readonly RootName rootName = new RootName(SCHEMA, USER_NAME, MOUNT_POINT);

            private readonly MockRepository mockRepository = new MockRepository(MockBehavior.Strict);

            private readonly Func<string, FileSystemInfoContract[]> driveRootDirectoryItems = id => new FileSystemInfoContract[] {
                new DirectoryInfoContract("\\SharedDir", "SharedDir", "2017-01-01 10:11:12".ToDateTime(), "2017-01-01 20:21:22".ToDateTime()),
                new DirectoryInfoContract($"\\IndividualDir_{id}", $"IndividualDir_{id}", "2017-01-01 13:14:15".ToDateTime(), "2017-01-01 23:24:25".ToDateTime()),
                new FileInfoContract("\\SharedFile.ext", "SharedFile.ext", "2017-01-02 10:11:12".ToDateTime(), "2017-01-02 20:21:22".ToDateTime(), new FileSize("16kB"), "16384".ToHash()),
                new FileInfoContract($"\\IndividualFile_{id}.ext", $"IndividualFile_{id}.ext", "2017-01-03 10:11:12".ToDateTime(), "2017-01-03 20:21:22".ToDateTime(), new FileSize("32kB"), "32768".ToHash())
            };

            public IDictionary<Mock, FileSystemInfoContract[]> RootDirectoryItems { get; } = new Dictionary<Mock, FileSystemInfoContract[]>();

            public static Fixture Initialize(int numAsyncGateways, int numGateways) => new Fixture(numAsyncGateways, numGateways);

            private Fixture(int numAsyncGateways, int numGateways)
            {
                asyncGateways = Enumerable.Range(0, numAsyncGateways).Select(i => mockRepository.Create<IAsyncCloudGateway>()).ToArray();
                gateways = Enumerable.Range(0, numGateways).Select(i => mockRepository.Create<ICloudGateway>()).ToArray();
                rootDirectories = new Dictionary<Mock, RootDirectoryInfoContract>();
                for (var i = 0; i < asyncGateways.Length; ++i) {
                    var mock = asyncGateways[i];
                    var driveName = $"AsyncDrive_{i}";
                    rootDirectories.Add(mock, new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), defaultTime, defaultTime)
                    {
                        Drive = new DriveInfoContract(driveName, ASYNC_DRIVE_FREE_SPACE << i, ASYNC_DRIVE_USED_SPACE << i)
                    });
                    RootDirectoryItems.Add(mock, driveRootDirectoryItems(driveName));
                }
                for (var i = 0; i < gateways.Length; ++i) {
                    var mock = gateways[i];
                    var driveName = $"Drive_{i}";
                    rootDirectories.Add(mock, new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), defaultTime, defaultTime)
                    {
                        Drive = new DriveInfoContract(driveName, DRIVE_FREE_SPACE << i, DRIVE_USED_SPACE << i)
                    });
                    RootDirectoryItems.Add(mock, driveRootDirectoryItems(driveName));
                }
            }

            public CloudDriveConfiguration[] CreateConfigurations(int count, string gatewayKind, string rootNamePattern, string apiKeyPattern, string encryptionKeyPattern)
            {
                return Enumerable.Range(0, count).Select(i => new CloudDriveConfiguration(
                    new RootName(string.Format(CultureInfo.InvariantCulture, rootNamePattern, gatewayKind, i)),
                    string.Format(CultureInfo.InvariantCulture, apiKeyPattern, gatewayKind, i),
                    string.Format(CultureInfo.InvariantCulture, encryptionKeyPattern, gatewayKind, i),
                    default(IDictionary<string, string>)
                )).ToArray();
            }

            public DirectoryInfoContract[] GetUniqueDirectories(IDictionary<Mock, FileSystemInfoContract[]> fileSystems)
            {
                return fileSystems.Values
                    .SelectMany(f => f.OfType<DirectoryInfoContract>()).GroupBy(f => f.Name)
                    .Where(d => d.Count() == 1).Select(d => d.Single())
                    .ToArray();
            }

            public DirectoryInfoContract[] GetSharedDirectories(IDictionary<Mock, FileSystemInfoContract[]> fileSystems)
            {
                return fileSystems.Values
                    .SelectMany(f => f.OfType<DirectoryInfoContract>()).GroupBy(f => f.Name)
                    .Where(d => d.Count() > 1).Select(d => d.First())
                    .ToArray();
            }

            public FileInfoContract[] GetUniqueFiles(IDictionary<Mock, FileSystemInfoContract[]> fileSystems)
            {
                return fileSystems.Values
                    .SelectMany(f => f.OfType<FileInfoContract>()).GroupBy(f => f.Name)
                    .Where(f => f.Count() == 1).Select(f => f.Single())
                    .ToArray();
            }

            public FileInfoContract[] GetSharedFiles(IDictionary<Mock, FileSystemInfoContract[]> fileSystems)
            {
                return fileSystems
                    .SelectMany(k => k.Value.OfType<FileInfoContract>().Select(v => new KeyValuePair<Mock, FileInfoContract>(k.Key, v))).GroupBy(k => k.Value.Name)
                    .Where(g => g.Count() > 1).SelectMany(f => f.Select(v => new FileInfoContract(v.Value.Id.Value, $"{v.Value.Name} [{v.Key}]", v.Value.Created, v.Value.Updated, v.Value.Size, v.Value.Hash)))
                    .ToArray();
            }

            public UnionCloudDrive Create(CloudDriveConfiguration[] asyncConfigs, CloudDriveConfiguration[] configs)
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                var asyncDrives = new Dictionary<CloudDriveConfiguration, IAsyncCloudGateway>();
                asyncGateways.Zip(asyncConfigs, (gw, cfg) => { asyncDrives.Add(cfg, gw.Object); return true; }).ToArray();
                var drives = new Dictionary<CloudDriveConfiguration, ICloudGateway>();
                gateways.Zip(configs, (gw, cfg) => { drives.Add(cfg, gw.Object); return true; }).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects

                return new UnionCloudDrive(new RootName(SCHEMA, USER_NAME, MOUNT_POINT), asyncDrives, drives);
            }

            public void SetupTryAuthenticate(CloudDriveConfiguration[] asyncConfigs, CloudDriveConfiguration[] configs, bool result = true)
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(asyncConfigs, (gw, cfg) =>
                    gw
                        .Setup(g => g.TryAuthenticateAsync(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(Task.FromResult(result))
                ).ToArray();
                gateways.Zip(configs, (gw, cfg) =>
                    gw
                        .Setup(g => g.TryAuthenticate(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(result)
                ).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }

            public void SetupGetDrive(CloudDriveConfiguration[] asyncConfigs, CloudDriveConfiguration[] configs)
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(asyncConfigs, (gw, cfg) =>
                    gw
                        .Setup(g => g.GetDriveAsync(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(Task.FromResult(rootDirectories[gw].Drive))
                ).ToArray();
                gateways.Zip(configs, (gw, cfg) =>
                    gw
                        .Setup(g => g.GetDrive(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(rootDirectories[gw].Drive)
                ).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }

            public void SetupGetDriveThrows<TException>(CloudDriveConfiguration[] asyncConfigs, CloudDriveConfiguration[] configs)
                where TException : Exception, new()
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(asyncConfigs, (gw, cfg) =>
                    gw
                        .Setup(g => g.GetDriveAsync(rootName, cfg.ApiKey, cfg.Parameters))
                        .Throws(new AggregateException(Activator.CreateInstance<TException>()))
                ).ToArray();
                gateways.Zip(configs, (gw, cfg) =>
                    gw
                        .Setup(g => g.GetDrive(rootName, cfg.ApiKey, cfg.Parameters))
                        .Throws(new AggregateException(Activator.CreateInstance<TException>()))
                ).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }

            public void SetupGetRoot(CloudDriveConfiguration[] asyncConfigs, CloudDriveConfiguration[] configs)
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(asyncConfigs, (gw, cfg) =>
                    gw
                        .Setup(g => g.GetRootAsync(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(Task.FromResult(rootDirectories[gw]))
                ).ToArray();
                gateways.Zip(configs, (gw, cfg) =>
                    gw
                        .Setup(g => g.GetRoot(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(rootDirectories[gw])
                ).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }

            public void SetupGetRootDirectoryItems(CloudDriveConfiguration[] asyncConfigs, CloudDriveConfiguration[] configs)
            {
#pragma warning disable S2201 // Return values should not be ignored when function calls don't have any side effects
                asyncGateways.Zip(asyncConfigs, (gw, cfg) => {
                    gw
                        .Setup(g => g.GetChildItemAsync(rootName, new DirectoryId(Path.DirectorySeparatorChar.ToString())))
                        .Returns(Task.FromResult(RootDirectoryItems[gw].AsEnumerable()));
                    if (!string.IsNullOrEmpty(cfg.EncryptionKey))
                        foreach (var fileInfo in RootDirectoryItems[gw].OfType<FileInfoContract>())
                            using (var rawStream = new MemoryStream(Enumerable.Repeat<byte>(0, (int)fileInfo.Size).ToArray()))
                                gw
                                    .SetupSequence(g => g.GetContentAsync(rootName, fileInfo.Id))
                                    .Returns(Task.FromResult(rawStream.EncryptOrPass(cfg.EncryptionKey)));

                    return true;
                }).ToArray();
                gateways.Zip(configs, (gw, cfg) => {
                    gw
                        .Setup(g => g.GetChildItem(rootName, new DirectoryId(Path.DirectorySeparatorChar.ToString())))
                        .Returns(RootDirectoryItems[gw]);
                    if (!string.IsNullOrEmpty(cfg.EncryptionKey))
                        foreach (var fileInfo in RootDirectoryItems[gw].OfType<FileInfoContract>())
                            using (var rawStream = new MemoryStream(Enumerable.Repeat<byte>(0, (int)fileInfo.Size).ToArray()))
                                gw
                                    .SetupSequence(g => g.GetContent(rootName, fileInfo.Id))
                                    .Returns(rawStream.EncryptOrPass(cfg.EncryptionKey));

                    return true;
                }).ToArray();
#pragma warning restore S2201 // Return values should not be ignored when function calls don't have any side effects
            }
        }
    }
}
