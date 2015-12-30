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
using System.IO;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.IO;
using IgorSoft.DokanCloudFS.Parameters;

namespace IgorSoft.DokanCloudFS
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class AsyncCloudDrive : CloudDriveBase, ICloudDrive
    {
        private const int MAX_BULKDOWNLOAD_SIZE = 1 << 29;

        private IAsyncCloudGateway gateway;

        public AsyncCloudDrive(RootName rootName, IAsyncCloudGateway gateway, CloudDriveParameters parameters) : base(rootName, parameters)
        {
            this.gateway = gateway;
        }

        protected override DriveInfoContract GetDrive()
        {
            try {
                if (drive == null) {
                    drive = gateway.GetDriveAsync(rootName, apiKey).Result;
                    drive.Name = DisplayRoot + Path.VolumeSeparatorChar;
                }
                return drive;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            }
        }

        public RootDirectoryInfoContract GetRoot()
        {
            return ExecuteInSemaphore(() => {
                var root = gateway.GetRootAsync(rootName, apiKey).Result;
                root.Drive = GetDrive();
                return root;
            }, nameof(GetRoot));
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
        {
            return ExecuteInSemaphore(() => {
                return gateway.GetChildItemAsync(rootName, parent.Id).Result;
            }, nameof(GetChildItem));
        }

        public Stream GetContent(FileInfoContract source)
        {
            return ExecuteInSemaphore(() => {
                var result = gateway.GetContentAsync(rootName, source.Id).Result;

                if (!result.CanSeek) {
                    var bufferStream = new MemoryStream();
                    result.CopyTo(bufferStream, MAX_BULKDOWNLOAD_SIZE);
                    bufferStream.Seek(0, SeekOrigin.Begin);
                    result.Dispose();
                    result = bufferStream;
                }

                if (!string.IsNullOrEmpty(encryptionKey))
                    result = result.Decrypt(encryptionKey);

#if DEBUG
                result = new TraceStream(nameof(GetContent), source.Name, result);
#endif
                return result;
            }, nameof(GetContent));
        }

        public void SetContent(FileInfoContract target, Stream content)
        {
            ExecuteInSemaphore(() => {
                if (!string.IsNullOrEmpty(encryptionKey))
                    content = content.Encrypt(encryptionKey);

#if DEBUG
                content = new TraceStream(nameof(SetContent), target.Name, content);
#endif
                Func<FileSystemInfoLocator> locator = () => new FileSystemInfoLocator(target);
                var result = gateway.SetContentAsync(rootName, target.Id, content, null, locator).Result;
                target.Size = content.Length;
            }, nameof(SetContent), true);
        }

        public FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination)
        {
            return ExecuteInSemaphore(() => {
                Func<FileSystemInfoLocator> locator = () => new FileSystemInfoLocator(source);
                return gateway.MoveItemAsync(rootName, source.Id, movePath, destination.Id, locator).Result;
            }, nameof(MoveItem), true);
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name)
        {
            return ExecuteInSemaphore(() => {
                return gateway.NewDirectoryItemAsync(rootName, parent.Id, name).Result;
            }, nameof(NewDirectoryItem), true);
        }

        public FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content)
        {
            return ExecuteInSemaphore(() => {
                if (content != null && !string.IsNullOrEmpty(encryptionKey))
                    content = content.Encrypt(encryptionKey);

                return gateway.NewFileItemAsync(rootName, parent.Id, name, content, null).Result;
            }, nameof(NewFileItem), true);
        }

        public void RemoveItem(FileSystemInfoContract target, bool recurse)
        {
            ExecuteInSemaphore(() => {
                gateway.RemoveItemAsync(rootName, target.Id, recurse).Wait();
            }, nameof(RemoveItem), true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(AsyncCloudDrive)} {DisplayRoot}";
    }
}
