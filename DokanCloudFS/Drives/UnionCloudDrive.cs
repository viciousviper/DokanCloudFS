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
using IgorSoft.DokanCloudFS.Nodes;

namespace IgorSoft.DokanCloudFS.Drives
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class UnionCloudDrive : CloudDriveBase, IUnionCloudDrive
    {
        private class PersistUnionGatewaySettings : IPersistGatewaySettings
        {
            private UnionCloudDrive drive;

            public PersistUnionGatewaySettings(UnionCloudDrive drive)
            {
                this.drive = drive;
            }

            public void PurgeSettings(RootName root)
            {
                drive.ApplyToConfigurations((c, g) => { (g as IPersistGatewaySettings).PurgeSettings(root); return Task.FromResult(new ValueTuple()); }, (c, g) => { (g as IPersistGatewaySettings).PurgeSettings(root); return new ValueTuple(); });
            }
        }

        private IDictionary<CloudDriveConfiguration, IAsyncCloudGateway> asyncConfigs;

        private IDictionary<CloudDriveConfiguration, ICloudGateway> configs;

        private IDictionary<CloudDriveConfiguration, DriveInfoContract> asyncDrives = new Dictionary<CloudDriveConfiguration, DriveInfoContract>();

        private IDictionary<CloudDriveConfiguration, DriveInfoContract> drives = new Dictionary<CloudDriveConfiguration, DriveInfoContract>();

        public IPersistGatewaySettings PersistSettings => new PersistUnionGatewaySettings(this);

        public UnionCloudDrive(RootName rootName, IDictionary<CloudDriveConfiguration, IAsyncCloudGateway> asyncConfigs, IDictionary<CloudDriveConfiguration, ICloudGateway> configs) : base(GetUnionConfiguration(rootName, asyncConfigs, configs))
        {
            this.asyncConfigs = asyncConfigs ?? new Dictionary<CloudDriveConfiguration, IAsyncCloudGateway>();
            this.configs = configs ?? new Dictionary<CloudDriveConfiguration, ICloudGateway>();
        }

        private static CloudDriveConfiguration GetUnionConfiguration(RootName rootName, IDictionary<CloudDriveConfiguration, IAsyncCloudGateway> asyncConfigs, IDictionary<CloudDriveConfiguration, ICloudGateway> configs)
        {
            if (rootName == null)
                throw new ArgumentNullException(nameof(rootName));

            var encryptionKey = (asyncConfigs?.Keys.All(c => !string.IsNullOrEmpty(c.EncryptionKey)) ?? false) &&
                                (configs?.Keys.All(c => !string.IsNullOrEmpty(c.EncryptionKey)) ?? false)
                ? "."
                : default(string);
            return new CloudDriveConfiguration(rootName, encryptionKey: encryptionKey);
        }

        private (CloudDriveConfiguration Configuration, TResult Result)[] ApplyToConfigurations<TResult>(Func<CloudDriveConfiguration, IAsyncCloudGateway, Task<TResult>> asyncConfigFunc, Func<CloudDriveConfiguration, ICloudGateway, TResult> configFunc)
        {
            var asyncResults = asyncConfigs.Select(p => asyncConfigFunc(p.Key, p.Value)).ToArray();
            var results = configs.Select(p => configFunc(p.Key, p.Value)).ToArray();

            Task.WaitAll(asyncResults);

            return asyncConfigs.Zip(asyncResults, (c, r) => (c.Key, r.Result)).Concat(configs.Zip(results, (c, r) => (c.Key, r))).ToArray();
        }

        protected override DriveInfoContract GetDrive()
        {
            if (drive == null) {
                ApplyToConfigurations((c, g) => {
                    if (!asyncDrives.ContainsKey(c))
                        asyncDrives.Add(c, g.GetDriveAsync(rootName, c.ApiKey, c.Parameters).Result);
                    return Task.FromResult(true);
                }, (c, g) => {
                    if (!drives.ContainsKey(c))
                        drives.Add(c, g.GetDrive(rootName, c.ApiKey, c.Parameters));
                    return true;
                });

                var free = asyncDrives.Sum(d => d.Value.FreeSpace) + drives.Sum(d => d.Value.FreeSpace);
                var used = asyncDrives.Sum(d => d.Value.UsedSpace) + drives.Sum(d => d.Value.UsedSpace);
                drive = new DriveInfoContract(rootName.Value, free, used) { Name = DisplayRoot + Path.VolumeSeparatorChar } ;
            }
            return drive;
        }

        public bool TryAuthenticate()
        {
            return ApplyToConfigurations(
                (c, g) => g.TryAuthenticateAsync(rootName, c.ApiKey, c.Parameters),
                (c, g) => g.TryAuthenticate(rootName, c.ApiKey, c.Parameters)
            ).All(t => t.Result);
        }

        public UnionRootDirectoryInfo GetRoot()
        {
            return ExecuteInSemaphore(() => {
                GetDrive();
                var roots = ApplyToConfigurations(
                    (c, g) => g.GetRootAsync(rootName, c.ApiKey, c.Parameters),
                    (c, g) => g.GetRoot(rootName, c.ApiKey, c.Parameters)
                );

                return new UnionRootDirectoryInfo(roots.ToDictionary(r => r.Configuration, r => r.Result)) { Drive = drive };
            }, nameof(GetRoot));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public IEnumerable<UnionFileSystemInfo> GetChildItem(UnionDirectoryInfo parent)
        {
            return ExecuteInSemaphore(() => {
                var allChildItems = ApplyToConfigurations(
                    (c, g) => g.GetChildItemAsync(rootName, (DirectoryId)parent.FileSystemInfos[c].Id),
                    (c, g) => g.GetChildItem(rootName, (DirectoryId)parent.FileSystemInfos[c].Id)
                ).SelectMany(r => r.Result.Select(i => (r.Configuration, i))).ToArray();

                var directories = allChildItems.Where(i => i.Item2 is DirectoryInfoContract).GroupBy(i => i.Item2.Name).ToArray();
                var files = allChildItems.Where(i => i.Item2 is FileInfoContract).GroupBy(i => i.Item2.Name).ToArray();

                return directories.Select(g => new UnionDirectoryInfo(g.ToDictionary(c => c.Item1, c => c.Item2 as DirectoryInfoContract))).Cast<UnionFileSystemInfo>()
                    .Concat(files.Select(g => new UnionFileInfo(g.ToDictionary(c => c.Item1, c => c.Item2 as FileInfoContract))));
            }, nameof(GetChildItem));
        }

        public Stream GetContent(UnionFileInfo source, CloudDriveConfiguration config)
        {
            return ExecuteInSemaphore(() => {
                var fileInfo = (FileInfoContract)source.FileSystemInfos[config];

                Stream gatewayContent;
                if (asyncConfigs.TryGetValue(config, out IAsyncCloudGateway asyncGateway))
                    gatewayContent = asyncGateway.GetContentAsync(rootName, fileInfo.Id).Result.ToSeekableStream();
                else if (configs.TryGetValue(config, out ICloudGateway gateway))
                    gatewayContent = gateway.GetContent(rootName, fileInfo.Id).ToSeekableStream();
                else
                    throw new InvalidOperationException(/*TODO: Declare message resource*/);

                var content = gatewayContent.DecryptOrPass(config.EncryptionKey);
                if (content != gatewayContent)
                    gatewayContent.Close();
                fileInfo.Size = (FileSize)content.Length;

#if DEBUG
                CompositionInitializer.SatisfyImports(content = new TraceStream(nameof(source), source.Name, content));
#endif
                return content;
            }, nameof(GetContent));
        }

        public void SetContent(UnionFileInfo target, CloudDriveConfiguration config, Stream content)
        {
            ExecuteInSemaphore(() => {
                var fileInfo = (FileInfoContract)target.FileSystemInfos[config];

                var gatewayContent = content.EncryptOrPass(config.EncryptionKey);
                fileInfo.Size = (FileSize)content.Length;

#if DEBUG
                CompositionInitializer.SatisfyImports(gatewayContent = new TraceStream(nameof(target), target.Name, gatewayContent));
#endif
                if (asyncConfigs.TryGetValue(config, out IAsyncCloudGateway asyncGateway))
                    asyncGateway.SetContentAsync(rootName, fileInfo.Id, gatewayContent, null, () => new FileSystemInfoLocator(fileInfo)).Wait();
                else if (configs.TryGetValue(config, out ICloudGateway gateway))
                    gateway.SetContent(rootName, fileInfo.Id, gatewayContent, null);
                else
                    throw new InvalidOperationException(/*TODO: Declare message resource*/);

                if (content != gatewayContent)
                    gatewayContent.Close();
            }, nameof(SetContent), true);
        }

        public UnionFileSystemInfo MoveItem(UnionFileSystemInfo source, string movePath, UnionDirectoryInfo destination)
        {
            throw new NotImplementedException();
        }

        public DirectoryInfoContract NewDirectoryItem(UnionDirectoryInfo parent, string name)
        {
            throw new NotImplementedException();
        }

        public FileInfoContract NewFileItem(UnionDirectoryInfo parent, CloudDriveConfiguration config, string name, Stream content)
        {
            throw new NotImplementedException();
        }

        public void RemoveItem(UnionFileSystemInfo target, bool recurse)
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(UnionCloudDrive)} {DisplayRoot}".ToString(CultureInfo.CurrentCulture);
    }
}
