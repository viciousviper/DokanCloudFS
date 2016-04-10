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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class CloudDirectoryNode : CloudItemNode
    {
        internal IDictionary<string, CloudItemNode> children;

        public new DirectoryInfoContract Contract => (DirectoryInfoContract)base.Contract;

        public CloudDirectoryNode(DirectoryInfoContract contract) : base(contract)
        {
        }

        public override void SetParent(CloudDirectoryNode parent)
        {
            base.SetParent(parent);

            Contract.Parent = parent?.Contract;
        }

        public IEnumerable<CloudItemNode> GetChildItems(ICloudDrive drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            if (children == null) {
                lock (Contract) {
                    if (children == null) {
                        children = drive.GetChildItem(Contract).Select(f => CloudItemNode.CreateNew(f)).ToDictionary(i => i.Name);

                        foreach (var child in children.Values)
                            child.SetParent(this);
                    }
                }
            }

            return children.Values;
        }

        public CloudItemNode GetChildItemByName(ICloudDrive drive, string fileName)
        {
            GetChildItems(drive);

            var result = default(CloudItemNode);
            children.TryGetValue(fileName, out result);
            return result;
        }

        public CloudDirectoryNode NewDirectoryItem(ICloudDrive drive, string directoryName)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            var newItem = new CloudDirectoryNode(drive.NewDirectoryItem(Contract, directoryName));
            children.Add(newItem.Name, newItem);
            newItem.SetParent(this);
            return newItem;
        }

        public CloudFileNode NewFileItem(ICloudDrive drive, string fileName)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            var newItem = new CloudFileNode(drive.NewFileItem(Contract, fileName, Stream.Null));
            children.Add(newItem.Name, newItem);
            newItem.SetParent(this);
            return newItem;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(CloudDirectoryNode)} {Name} [{children?.Count ?? 0}]".ToString(CultureInfo.CurrentCulture);
    }
}
