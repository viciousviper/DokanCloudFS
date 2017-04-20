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

namespace IgorSoft.DokanCloudFS
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class UnionCloudDrive : CloudDriveBase
    {
        private IDictionary<CloudDriveConfiguration, IAsyncCloudGateway> asyncConfigs;

        private IDictionary<CloudDriveConfiguration, ICloudGateway> configs;

        private IDictionary<CloudDriveConfiguration, DriveInfoContract> asyncDrives = new Dictionary<CloudDriveConfiguration, DriveInfoContract>();

        private IDictionary<CloudDriveConfiguration, DriveInfoContract> drives = new Dictionary<CloudDriveConfiguration, DriveInfoContract>();

        public (IPersistGatewaySettings, CloudDriveConfiguration)[] PersistSettings => ApplyToConfigurations((c, g) => Task.FromResult(g as IPersistGatewaySettings), (c, g) => g as IPersistGatewaySettings);

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

        private (TResult Result, CloudDriveConfiguration Configuration)[] ApplyToConfigurations<TResult>(Func<CloudDriveConfiguration, IAsyncCloudGateway, Task<TResult>> asyncConfigFunc, Func<CloudDriveConfiguration, ICloudGateway, TResult> configFunc)
        {
            var asyncResults = asyncConfigs.Select(p => asyncConfigFunc(p.Key, p.Value)).ToArray();
            var results = configs.Select(p => configFunc(p.Key, p.Value)).ToArray();

            Task.WaitAll(asyncResults);

            return asyncResults.Zip(asyncConfigs, (t, c) => (t.Result, c.Key)).Concat(results.Zip(configs, (r, c) => (r, c.Key))).ToArray();
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

        public RootDirectoryInfoContract GetRoot()
        {
            return ExecuteInSemaphore(() => {
                GetDrive();
                var roots = ApplyToConfigurations(
                    (c, g) => g.GetRootAsync(rootName, c.ApiKey, c.Parameters),
                    (c, g) => g.GetRoot(rootName, c.ApiKey, c.Parameters)
                );

                return new RootDirectoryInfoContract(@"\", roots.Min(r => r.Result.Created), roots.Max(r => r.Result.Updated)) { Drive = drive };
            }, nameof(GetRoot));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
        {
            return ExecuteInSemaphore(() => {
                var allChildItems = ApplyToConfigurations(
                    (c, g) => g.GetChildItemAsync(rootName, parent.Id),
                    (c, g) => g.GetChildItem(rootName, parent.Id)
                ).SelectMany(r => r.Result.Select(f => (f, r.Configuration))).ToArray();

                var directories = allChildItems.Where(i => i.Item1 is DirectoryInfoContract).GroupBy(i => i.Item1.Name).ToArray();
                var files = allChildItems.Where(i => i.Item1 is FileInfoContract).GroupBy(i => i.Item1.Name).ToArray();

                return directories.Select(g => g.First().Item1)
                    .Concat(files.SelectMany(g => {
                        return g.Count() == 1
                            ? g.Select(f => f.Item1)
                            : g.Select(f => new FileInfoContract(f.Item1.Id.Value, $"{f.Item1.Name} [{f.Item2.RootName}]", f.Item1.Created, f.Item1.Updated, ((FileInfoContract)f.Item1).Size, ((FileInfoContract)f.Item1).Hash));
                    }));
            }, nameof(GetChildItem));
        }

        public Stream GetContent(FileInfoContract source)
        {
            throw new NotImplementedException();
        }

        public FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination)
        {
            throw new NotImplementedException();
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name)
        {
            throw new NotImplementedException();
        }

        public FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content)
        {
            throw new NotImplementedException();
        }

        public void RemoveItem(FileSystemInfoContract target, bool recurse)
        {
            throw new NotImplementedException();
        }

        public void SetContent(FileInfoContract target, Stream content)
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(UnionCloudDrive)} {DisplayRoot}".ToString(CultureInfo.CurrentCulture);
    }
}
