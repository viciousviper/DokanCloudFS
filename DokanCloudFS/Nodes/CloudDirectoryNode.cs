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
using IgorSoft.DokanCloudFS.Drives;

namespace IgorSoft.DokanCloudFS.Nodes
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class CloudDirectoryNode : CloudItemNode, ICloudDirectoryNode
    {
        internal IDictionary<string, CloudItemNode> children;

        public new DirectoryInfoContract FileSystemInfo => (DirectoryInfoContract)base.FileSystemInfo;

        public CloudDirectoryNode(DirectoryInfoContract directoryInfo, ICloudDrive drive) : base(directoryInfo, drive)
        {
        }

        public override void SetParent(CloudDirectoryNode parent)
        {
            base.SetParent(parent);

            FileSystemInfo.Parent = parent?.FileSystemInfo;
        }

        public IEnumerable<ICloudItemNode> GetChildItems()
        {
            if (children == null) {
                lock (Drive) {
                    if (children == null) {
                        children = Drive.GetChildItem(FileSystemInfo).Select(CreateNew).ToDictionary(i => i.Name);

                        foreach (var child in children.Values)
                            child.SetParent(this);
                    }
                }
            }

            return children.Values;
        }

        public ICloudItemNode GetChildItemByName(string fileName)
        {
            GetChildItems();

            children.TryGetValue(fileName, out CloudItemNode result);
            return result;
        }

        public ICloudDirectoryNode NewDirectoryItem(string directoryName)
        {
            var newItem = new CloudDirectoryNode(Drive.NewDirectoryItem(FileSystemInfo, directoryName), Drive);
            children.Add(newItem.Name, newItem);
            newItem.SetParent(this);
            return newItem;
        }

        public ICloudFileNode NewFileItem(string fileName)
        {
            var newItem = new CloudFileNode(Drive.NewFileItem(FileSystemInfo, fileName, Stream.Null), Drive);
            children.Add(newItem.Name, newItem);
            newItem.SetParent(this);
            return newItem;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(CloudDirectoryNode)} {Name} [{children?.Count ?? 0}]".ToString(CultureInfo.CurrentCulture);
    }
}
