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
        private GatewayConfiguration<IAsyncCloudGateway>[] asyncGateways;

        private GatewayConfiguration<ICloudGateway>[] gateways;

        private IDictionary<IAsyncCloudGateway, DriveInfoContract> asyncDrives = new Dictionary<IAsyncCloudGateway, DriveInfoContract>();

        private IDictionary<ICloudGateway, DriveInfoContract> drives = new Dictionary<ICloudGateway, DriveInfoContract>();

        public IPersistGatewaySettings[] PersistSettings => ApplyToGateways(g => Task.FromResult(g as IPersistGatewaySettings), g => g as IPersistGatewaySettings);

        public UnionCloudDrive(RootName rootName, GatewayConfiguration<IAsyncCloudGateway>[] asyncGateways, GatewayConfiguration<ICloudGateway>[] gateways) : base(rootName, GetUnionParameters(asyncGateways, gateways))
        {
            this.asyncGateways = asyncGateways ?? Enumerable.Empty<GatewayConfiguration<IAsyncCloudGateway>>().ToArray();
            this.gateways = gateways ?? Enumerable.Empty<GatewayConfiguration<ICloudGateway>>().ToArray();
        }

        private static CloudDriveParameters GetUnionParameters(IEnumerable<GatewayConfiguration<IAsyncCloudGateway>> asyncGateways, IEnumerable<GatewayConfiguration<ICloudGateway>> gateways)
        {
            return (asyncGateways?.All(g => !string.IsNullOrEmpty(g.Parameters.EncryptionKey)) ?? false) &&
                (gateways?.All(g => !string.IsNullOrEmpty(g.Parameters.EncryptionKey)) ?? false)
                ? new CloudDriveParameters() { EncryptionKey = "." }
                : null;
        }

        private TResult[] ApplyToConfigurations<TResult>(Func<GatewayConfiguration<IAsyncCloudGateway>, Task<TResult>> asyncConfigFunc, Func<GatewayConfiguration<ICloudGateway>, TResult> configFunc)
        {
            var asyncResults = asyncGateways.Select(asyncConfigFunc).ToArray();
            var results = gateways.Select(configFunc).ToArray();

            Task.WaitAll(asyncResults);

            return asyncResults.Select(r => r.Result).Concat(results).ToArray();
        }

        private TResult[] ApplyToGateways<TResult>(Func<IAsyncCloudGateway, Task<TResult>> asyncGatewayFunc, Func<ICloudGateway, TResult> gatewayFunc)
        {
            var asyncResults = asyncGateways.Select(c => asyncGatewayFunc(c.Gateway)).ToArray();
            var results = gateways.Select(c => gatewayFunc(c.Gateway)).ToArray();

            Task.WaitAll(asyncResults);

            return asyncResults.Select(r => r.Result).Concat(results).ToArray();
        }

        protected override DriveInfoContract GetDrive()
        {
            if (drive == null) {
                ApplyToConfigurations(c => {
                    if (!asyncDrives.ContainsKey(c.Gateway))
                        asyncDrives.Add(c.Gateway, c.Gateway.GetDriveAsync(rootName, c.Parameters.ApiKey, c.Parameters.Parameters).Result);
                    return Task.FromResult(true);
                }, c => {
                    if (!drives.ContainsKey(c.Gateway))
                        drives.Add(c.Gateway, c.Gateway.GetDrive(rootName, c.Parameters.ApiKey, c.Parameters.Parameters));
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
                c => c.Gateway.TryAuthenticateAsync(rootName, c.Parameters.ApiKey, c.Parameters.Parameters),
                c => c.Gateway.TryAuthenticate(rootName, c.Parameters.ApiKey, c.Parameters.Parameters)
            ).All(b => b);
        }

        public RootDirectoryInfoContract GetRoot()
        {
            return ExecuteInSemaphore(() => {
                GetDrive();
                var roots = ApplyToConfigurations(
                    c => c.Gateway.GetRootAsync(rootName, c.Parameters.ApiKey, c.Parameters.Parameters),
                    c => c.Gateway.GetRoot(rootName, c.Parameters.ApiKey, c.Parameters.Parameters)
                );

                return new RootDirectoryInfoContract(rootName.Value, roots.Min(r => r.Created), roots.Max(r => r.Updated)) { Drive = drive };
            }, nameof(GetRoot));
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
        {
            throw new NotImplementedException();
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
