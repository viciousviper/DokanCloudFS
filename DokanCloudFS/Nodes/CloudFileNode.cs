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
using System.IO;
using System.Globalization;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Drives;

namespace IgorSoft.DokanCloudFS.Nodes
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class CloudFileNode : CloudItemNode, ICloudFileNode
    {
        public new FileInfoContract FileSystemInfo => (FileInfoContract)base.FileSystemInfo;

        public FileSize Size => FileSystemInfo.Size;

        public override bool IsResolved => !(FileSystemInfo is ProxyFileInfoContract);

        public CloudFileNode(FileInfoContract fileInfo, ICloudDrive drive) : base(fileInfo, drive)
        {
        }

        protected internal void ResolveContract(FileInfoContract fileInfo)
        {
            if (IsResolved)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidNonProxyResolution, FileSystemInfo.GetType().Name, FileSystemInfo.Name));
            if (fileInfo is ProxyFileInfoContract || FileSystemInfo.Name != fileInfo.Name)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidProxyResolution, FileSystemInfo.Name, fileInfo.GetType().Name, fileInfo.Name));

            base.FileSystemInfo = fileInfo;
        }

        public override void SetParent(CloudDirectoryNode parent)
        {
            base.SetParent(parent);

            FileSystemInfo.Directory = parent?.FileSystemInfo;
        }

        public Stream GetContent()
        {
            return Drive.GetContent(FileSystemInfo);
        }

        public void SetContent(Stream stream)
        {
            var proxyFileInfoContract = FileSystemInfo as ProxyFileInfoContract;
            if (proxyFileInfoContract != null)
                ResolveContract(Drive.NewFileItem(Parent.FileSystemInfo, proxyFileInfoContract.Name, stream));
             else
                Drive.SetContent(FileSystemInfo, stream);
        }

        public void Truncate()
        {
            Drive.SetContent(FileSystemInfo, Stream.Null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(CloudFileNode)} {Name} Size={FileSystemInfo.Size}".ToString(CultureInfo.CurrentCulture);
    }
}
