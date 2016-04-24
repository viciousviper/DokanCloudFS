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

namespace IgorSoft.DokanCloudFS
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class CloudFileNode : CloudItemNode
    {
        public new FileInfoContract Contract => (FileInfoContract)base.Contract;

        public CloudFileNode(FileInfoContract contract) : base(contract)
        {
        }

        public override void SetParent(CloudDirectoryNode parent)
        {
            base.SetParent(parent);

            Contract.Directory = parent?.Contract;
        }

        public Stream GetContent(ICloudDrive drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            return drive.GetContent(Contract);
        }

        public void SetContent(ICloudDrive drive, Stream stream)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            var proxyFileInfoContract = Contract as ProxyFileInfoContract;
            if (proxyFileInfoContract != null)
                ResolveContract(drive.NewFileItem(Parent.Contract, proxyFileInfoContract.Name, stream));
             else
                drive.SetContent(Contract, stream);
        }

        public void Truncate(ICloudDrive drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            drive.SetContent(Contract, Stream.Null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(CloudFileNode)} {Name} Size={Contract.Size}".ToString(CultureInfo.CurrentCulture);
    }
}
