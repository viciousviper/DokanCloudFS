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
using Moq;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Configuration;
using IgorSoft.DokanCloudFS.Drives;
using IgorSoft.DokanCloudFS.IO;
using IgorSoft.DokanCloudFS.Nodes;
using IgorSoft.DokanCloudFS.Tests.IO;

namespace IgorSoft.DokanCloudFS.Tests.Drives
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

            private readonly IDictionary<CloudDriveConfiguration, Mock<IAsyncCloudGateway>> asyncConfigurations;

            private readonly IDictionary<CloudDriveConfiguration, Mock<ICloudGateway>> configurations;

            private readonly IDictionary<CloudDriveConfiguration, RootDirectoryInfoContract> rootDirectories;

            private readonly RootName rootName = new RootName(SCHEMA, USER_NAME, MOUNT_POINT);

            private readonly MockRepository mockRepository = new MockRepository(MockBehavior.Strict);

            private readonly Func<RootDirectoryInfoContract, DirectoryInfoContract> targetDirectoryFactory = rootDirectory => new DirectoryInfoContract(@"\SubDir", "SubDir", "2015-01-01 10:11:12".ToDateTime(), "2015-01-01 20:21:22".ToDateTime()) { Parent = rootDirectory };

            private readonly Func<string, FileSystemInfoContract[]> driveRootDirectoryItems = id => new FileSystemInfoContract[] {
                new DirectoryInfoContract("\\SharedDir", "SharedDir", "2017-01-01 10:11:12".ToDateTime(), "2017-01-01 20:21:22".ToDateTime()),
                new DirectoryInfoContract($"\\IndividualDir_{id}", $"IndividualDir_{id}", "2017-01-01 13:14:15".ToDateTime(), "2017-01-01 23:24:25".ToDateTime()),
                new FileInfoContract("\\SharedFile.ext", "SharedFile.ext", "2017-01-02 10:11:12".ToDateTime(), "2017-01-02 20:21:22".ToDateTime(), new FileSize("16kB"), "16384".ToHash()),
                new FileInfoContract($"\\IndividualFile_{id}.ext", $"IndividualFile_{id}.ext", "2017-01-03 10:11:12".ToDateTime(), "2017-01-03 20:21:22".ToDateTime(), new FileSize("32kB"), "32768".ToHash())
            };

            public UnionDirectoryInfo TargetDirectory { get; }

            public IDictionary<CloudDriveConfiguration, FileSystemInfoContract[]> RootDirectoryItems { get; } = new Dictionary<CloudDriveConfiguration, FileSystemInfoContract[]>();

            public static Fixture Initialize(int numAsyncGateways, int numGateways, string rootNamePattern, string apiKeyPattern, string encryptionKeyPattern) => new Fixture(numAsyncGateways, numGateways, rootNamePattern, apiKeyPattern, encryptionKeyPattern);

            private Fixture(int numAsyncGateways, int numGateways, string rootNamePattern, string apiKeyPattern, string encryptionKeyPattern)
            {
                asyncConfigurations = Enumerable.Range(0, numAsyncGateways)
                    .Select(i => (CreateConfiguration(i, "async", rootNamePattern, apiKeyPattern, encryptionKeyPattern), mockRepository.Create<IAsyncCloudGateway>()))
                    .ToDictionary(t => t.Item1, t => t.Item2);
                configurations = Enumerable.Range(0, numGateways)
                    .Select(i => (CreateConfiguration(i, string.Empty, rootNamePattern, apiKeyPattern, encryptionKeyPattern), mockRepository.Create<ICloudGateway>()))
                    .ToDictionary(t => t.Item1, t => t.Item2);

                rootDirectories = new Dictionary<CloudDriveConfiguration, RootDirectoryInfoContract>();
                int asyncIndex = 0;
                foreach (var asyncConfig in asyncConfigurations.Keys) {
                    var driveName = $"AsyncDrive_{asyncIndex}";
                    var rootDirectory = new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), defaultTime, defaultTime) {
                        Drive = new DriveInfoContract(driveName, ASYNC_DRIVE_FREE_SPACE << asyncIndex, ASYNC_DRIVE_USED_SPACE << asyncIndex)
                    };
                    rootDirectories.Add(asyncConfig, rootDirectory);
                    RootDirectoryItems.Add(asyncConfig, driveRootDirectoryItems(driveName));
                    ++asyncIndex;
                }
                int index = 0;
                foreach (var config in configurations.Keys) {
                    var driveName = $"Drive_{index}";
                    var rootDirectory = new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), defaultTime, defaultTime) {
                        Drive = new DriveInfoContract(driveName, DRIVE_FREE_SPACE << index, DRIVE_USED_SPACE << index)
                    };
                    rootDirectories.Add(config, rootDirectory);
                    RootDirectoryItems.Add(config, driveRootDirectoryItems(driveName));
                    ++index;
                }

                TargetDirectory = new UnionDirectoryInfo(asyncConfigurations.Keys.Concat(configurations.Keys).Select(c => (c, targetDirectoryFactory(rootDirectories[c]))).ToDictionary(t => t.Item1, t => t.Item2));
            }

            private CloudDriveConfiguration CreateConfiguration(int index, string gatewayKind, string rootNamePattern, string apiKeyPattern, string encryptionKeyPattern)
            {
                return new CloudDriveConfiguration(
                    new RootName(string.Format(CultureInfo.InvariantCulture, rootNamePattern, gatewayKind, index)),
                    string.Format(CultureInfo.InvariantCulture, apiKeyPattern, gatewayKind, index),
                    string.Format(CultureInfo.InvariantCulture, encryptionKeyPattern, gatewayKind, index)
                );
            }

            public void ForEachConfiguration(Action<CloudDriveConfiguration> action)
            {
                asyncConfigurations.ForEach((cfg, gateway) => action(cfg));
                configurations.ForEach((cfg, gateway) => action(cfg));
            }

            public IDictionary<CloudDriveConfiguration, TValue> SelectByConfiguration<TValue>(Func<CloudDriveConfiguration, TValue> func)
            {
                return asyncConfigurations.Keys.Concat(configurations.Keys).Select(c => (c, func(c))).ToDictionary(t => t.Item1, t => t.Item2);
            }

            public DirectoryInfoContract[] GetUniqueDirectories(IDictionary<CloudDriveConfiguration, FileSystemInfoContract[]> fileSystems)
            {
                return fileSystems.Values
                    .SelectMany(f => f.OfType<DirectoryInfoContract>()).GroupBy(f => f.Name)
                    .Where(d => d.Count() == 1).Select(d => d.Single())
                    .ToArray();
            }

            public DirectoryInfoContract[] GetSharedDirectories(IDictionary<CloudDriveConfiguration, FileSystemInfoContract[]> fileSystems)
            {
                return fileSystems.Values
                    .SelectMany(f => f.OfType<DirectoryInfoContract>()).GroupBy(f => f.Name)
                    .Where(d => d.Count() > 1).Select(d => d.First())
                    .ToArray();
            }

            public FileInfoContract[] GetUniqueFiles(IDictionary<CloudDriveConfiguration, FileSystemInfoContract[]> fileSystems)
            {
                return fileSystems.Values
                    .SelectMany(f => f.OfType<FileInfoContract>()).GroupBy(f => f.Name)
                    .Where(f => f.Count() == 1).Select(f => f.Single())
                    .ToArray();
            }

            public FileInfoContract[] GetSharedFiles(IDictionary<CloudDriveConfiguration, FileSystemInfoContract[]> fileSystems)
            {
                //return fileSystems
                //    .SelectMany(k => k.Value.OfType<FileInfoContract>().Select(v => new KeyValuePair<Mock, FileInfoContract>(k.Key, v))).GroupBy(k => k.Value.Name)
                //    .Where(g => g.Count() > 1).SelectMany(f => f.Select(v => new FileInfoContract(v.Value.Id.Value, $"{v.Value.Name} [{v.Key}]", v.Value.Created, v.Value.Updated, v.Value.Size, v.Value.Hash)))
                //    .ToArray();
                return fileSystems.Values
                    .SelectMany(f => f.OfType<FileInfoContract>()).GroupBy(f => f.Name)
                    .Where(d => d.Count() > 1).Select(d => d.First())
                    .ToArray();
            }

            public UnionCloudDrive Create()
            {
                var asyncDrives = asyncConfigurations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Object);
                var drives = configurations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Object);

                return new UnionCloudDrive(new RootName(SCHEMA, USER_NAME, MOUNT_POINT), asyncDrives, drives);
            }

            public void SetupTryAuthenticate(bool result = true)
            {
                asyncConfigurations.ForEach((cfg, gateway) =>
                    gateway
                        .Setup(g => g.TryAuthenticateAsync(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(Task.FromResult(result))
                );
                configurations.ForEach((cfg, gateway) =>
                    gateway
                        .Setup(g => g.TryAuthenticate(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(result)
                );
            }

            public void SetupGetDrive()
            {
                asyncConfigurations.ForEach((cfg, gateway) =>
                    gateway
                        .Setup(g => g.GetDriveAsync(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(Task.FromResult(rootDirectories[cfg].Drive))
                );
                configurations.ForEach((cfg, gateway) =>
                    gateway
                        .Setup(g => g.GetDrive(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(rootDirectories[cfg].Drive)
                );
            }

            public void SetupGetDriveThrows<TException>()
                where TException : Exception, new()
            {
                asyncConfigurations.ForEach((cfg, gateway) =>
                    gateway
                        .Setup(g => g.GetDriveAsync(rootName, cfg.ApiKey, cfg.Parameters))
                        .Throws(new AggregateException(Activator.CreateInstance<TException>()))
                );
                configurations.ForEach((cfg, gateway) =>
                    gateway
                        .Setup(g => g.GetDrive(rootName, cfg.ApiKey, cfg.Parameters))
                        .Throws(new AggregateException(Activator.CreateInstance<TException>()))
                );
            }

            public void SetupGetRoot()
            {
                asyncConfigurations.ForEach((cfg, gateway) =>
                    gateway
                        .Setup(g => g.GetRootAsync(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(Task.FromResult(rootDirectories[cfg]))
                );
                configurations.ForEach((cfg, gateway) =>
                    gateway
                        .Setup(g => g.GetRoot(rootName, cfg.ApiKey, cfg.Parameters))
                        .Returns(rootDirectories[cfg])
                );
            }

            public void SetupGetRootDirectoryItems()
            {
                asyncConfigurations.ForEach((cfg, gateway) => {
                    gateway
                        .Setup(g => g.GetChildItemAsync(rootName, new DirectoryId(Path.DirectorySeparatorChar.ToString())))
                        .Returns(Task.FromResult(RootDirectoryItems[cfg].AsEnumerable()));
                    if (!string.IsNullOrEmpty(cfg.EncryptionKey))
                        foreach (var fileInfo in RootDirectoryItems[cfg].OfType<FileInfoContract>())
                            using (var rawStream = new MemoryStream(Enumerable.Repeat<byte>(0, (int)fileInfo.Size).ToArray()))
                                gateway
                                    .SetupSequence(g => g.GetContentAsync(rootName, fileInfo.Id))
                                    .Returns(Task.FromResult(rawStream.EncryptOrPass(cfg.EncryptionKey)));
                });
                configurations.ForEach((cfg, gateway) => {
                    gateway
                        .Setup(g => g.GetChildItem(rootName, new DirectoryId(Path.DirectorySeparatorChar.ToString())))
                        .Returns(RootDirectoryItems[cfg]);
                    if (!string.IsNullOrEmpty(cfg.EncryptionKey))
                        foreach (var fileInfo in RootDirectoryItems[cfg].OfType<FileInfoContract>())
                            using (var rawStream = new MemoryStream(Enumerable.Repeat<byte>(0, (int)fileInfo.Size).ToArray()))
                                gateway
                                    .SetupSequence(g => g.GetContent(rootName, fileInfo.Id))
                                    .Returns(rawStream.EncryptOrPass(cfg.EncryptionKey));
                });
            }

            public void SetupGetContent(UnionFileInfo source, IDictionary<CloudDriveConfiguration, byte[]> contents, IDictionary<CloudDriveConfiguration, string> encryptionKeys = null, bool canSeek = true)
            {
                Func<string, byte[], Stream> createStream = (encryptionKey, content) => {
                    var stream = new MemoryStream(content);
                    if (!string.IsNullOrEmpty(encryptionKey))
                    {
                        var buffer = new MemoryStream();
                        SharpAESCrypt.SharpAESCrypt.Encrypt(encryptionKey, stream, buffer);
                        buffer.Seek(0, SeekOrigin.Begin);
                        stream = buffer;
                    }
                    if (!canSeek)
                        stream = new LinearReadMemoryStream(stream);
                    return stream;
                };
                asyncConfigurations.ForEach((cfg, gateway) => {
                    gateway
                        .Setup(g => g.GetContentAsync(rootName, ((FileInfoContract)source.FileSystemInfos[cfg]).Id))
                        .Returns(Task.FromResult(createStream(encryptionKeys?[cfg], contents[cfg])));
                });
                configurations.ForEach((cfg, gateway) => {
                    gateway
                        .Setup(g => g.GetContent(rootName, ((FileInfoContract)source.FileSystemInfos[cfg]).Id))
                        .Returns(createStream(encryptionKeys?[cfg], contents[cfg]));
                });
            }

            public void SetupSetContent(UnionFileInfo target, IDictionary<CloudDriveConfiguration, byte[]> contents, IDictionary<CloudDriveConfiguration, string> encryptionKeys = null)
            {
                Func<Stream, string, byte[], bool> checkContent = (stream, encryptionKey, content) => {
                    if (!string.IsNullOrEmpty(encryptionKey)) {
                        var buffer = new MemoryStream();
                        SharpAESCrypt.SharpAESCrypt.Decrypt(encryptionKey, stream, buffer);
                        buffer.Seek(0, SeekOrigin.Begin);
                        return buffer.Contains(content);
                    }
                    return stream.Contains(content);
                };
                asyncConfigurations.ForEach((cfg, gateway) => {
                    gateway
                        .Setup(g => g.SetContentAsync(rootName, ((FileInfoContract)target.FileSystemInfos[cfg]).Id, It.Is<Stream>(s => checkContent(s, encryptionKeys[cfg], contents[cfg])), It.IsAny<IProgress<ProgressValue>>(), It.IsAny<Func<FileSystemInfoLocator>>()))
                        .Returns(Task.FromResult(true));
                });
                configurations.ForEach((cfg, gateway) => {
                    gateway
                        .Setup(g => g.SetContent(rootName, ((FileInfoContract)target.FileSystemInfos[cfg]).Id, It.Is<Stream>(s => checkContent(s, encryptionKeys[cfg], contents[cfg])), It.IsAny<IProgress<ProgressValue>>()));
                });
            }

            public void VerifyAll()
            {
                mockRepository.VerifyAll();
            }
        }
    }
}
