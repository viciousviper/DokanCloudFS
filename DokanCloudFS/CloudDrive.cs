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
using System.Globalization;
using System.IO;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.IO;
using IgorSoft.DokanCloudFS.Parameters;

namespace IgorSoft.DokanCloudFS
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class CloudDrive : CloudDriveBase, ICloudDrive
    {
        private readonly ICloudGateway gateway;

        private readonly IDictionary<string, string> parameters;

        public CloudDrive(RootName rootName, ICloudGateway gateway, CloudDriveParameters parameters) : base(rootName, parameters)
        {
            this.gateway = gateway;
            this.parameters = parameters.Parameters;
        }

        public IPersistGatewaySettings PersistSettings => gateway as IPersistGatewaySettings;

        protected override DriveInfoContract GetDrive()
        {
            if (drive == null) {
                drive = gateway.GetDrive(rootName, apiKey, parameters);
                drive.Name = DisplayRoot + Path.VolumeSeparatorChar;
            }
            return drive;
        }

        public bool TryAuthenticate()
        {
            return gateway.TryAuthenticate(rootName, apiKey, parameters);
        }

        public RootDirectoryInfoContract GetRoot()
        {
            return ExecuteInSemaphore(() => {
                GetDrive();
                var root = gateway.GetRoot(rootName, apiKey, parameters);
                root.Drive = drive;
                return root;
            }, nameof(GetRoot));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
        {
            return ExecuteInSemaphore(() => {
                return gateway.GetChildItem(rootName, parent.Id);
            }, nameof(GetChildItem));
        }

        public Stream GetContent(FileInfoContract source)
        {
            return ExecuteInSemaphore(() => {
                var gatewayContent = gateway.GetContent(rootName, source.Id).ToSeekableStream();

                var content = gatewayContent.DecryptOrPass(encryptionKey);
                if (content != gatewayContent)
                    gatewayContent.Close();
                source.Size = (FileSize)content.Length;

#if DEBUG
                CompositionInitializer.SatisfyImports(content = new TraceStream(nameof(source), source.Name, content));
#endif
                return content;
            }, nameof(GetContent));
        }

        public void SetContent(FileInfoContract target, Stream content)
        {
            ExecuteInSemaphore(() => {
                var gatewayContent = content.EncryptOrPass(encryptionKey);
                target.Size = (FileSize)content.Length;

#if DEBUG
                CompositionInitializer.SatisfyImports(gatewayContent = new TraceStream(nameof(target), target.Name, gatewayContent));
#endif
                gateway.SetContent(rootName, target.Id, gatewayContent, null);
                if (content != gatewayContent)
                    gatewayContent.Close();
            }, nameof(SetContent), true);
        }

        public FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination)
        {
            return ExecuteInSemaphore(() => {
                return !(source is ProxyFileInfoContract) ? gateway.MoveItem(rootName, source.Id, movePath, destination.Id) : new ProxyFileInfoContract(movePath);
            }, nameof(MoveItem), true);
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name)
        {
            return ExecuteInSemaphore(() => {
                return gateway.NewDirectoryItem(rootName, parent.Id, name);
            }, nameof(NewDirectoryItem), true);
        }

        public FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content)
        {
            return ExecuteInSemaphore(() => {
                if (content.Length == 0)
                    return new ProxyFileInfoContract(name);

                var gatewayContent = content.EncryptOrPass(encryptionKey);

                var result = gateway.NewFileItem(rootName, parent.Id, name, gatewayContent, null);
                result.Size = (FileSize)content.Length;
                return result;
            }, nameof(NewFileItem),true);
        }

        public void RemoveItem(FileSystemInfoContract target, bool recurse)
        {
            ExecuteInSemaphore(() => {
                if (!(target is ProxyFileInfoContract))
                    gateway.RemoveItem(rootName, target.Id, recurse);
            }, nameof(RemoveItem), true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(CloudDrive)} {DisplayRoot}".ToString(CultureInfo.CurrentCulture);
    }
}
