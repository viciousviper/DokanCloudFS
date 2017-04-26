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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Drives;

namespace IgorSoft.DokanCloudFS.Nodes
{
    internal abstract class UnionCloudItemNode
    {
        protected UnionCloudDirectoryNode Parent { get; private set; }

        public UnionFileSystemInfo FileSystemInfo { get; }

        public string Name => FileSystemInfo.Name;

        public virtual bool IsResolved => true;

        public UnionCloudDrive Drive { get; private set; }

        protected UnionCloudItemNode(UnionFileSystemInfo fileSystemInfo, UnionCloudDrive drive)
        {
            FileSystemInfo = fileSystemInfo ?? throw new ArgumentNullException(nameof(fileSystemInfo));
            Drive = drive ?? throw new ArgumentNullException(nameof(drive));
        }

        protected internal UnionCloudItemNode CreateNew(UnionFileSystemInfo fileSystemInfo)
        {
            var fileInfoContract = fileSystemInfo as UnionFileInfo;
            if (fileInfoContract != null)
                return new UnionCloudFileNode(fileInfoContract, Drive);

            var directoryInfoContract = fileSystemInfo as UnionDirectoryInfo;
            if (directoryInfoContract != null)
                return new UnionCloudDirectoryNode(directoryInfoContract, Drive);

            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownItemType, fileSystemInfo.GetType().Name));
        }

        protected void EnsureCompatibleDrives(UnionCloudItemNode otherNode, [CallerMemberName] string operation = null)
        {
            if (FileSystemInfo.FileSystemInfos.Keys.Except(otherNode.FileSystemInfo.FileSystemInfos.Keys).Any())
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.CrossDriveOperation, operation, otherNode.Drive.DisplayRoot, Drive.DisplayRoot));
        }

        public virtual void SetParent(UnionCloudDirectoryNode parent)
        {
            if (parent != null)
                EnsureCompatibleDrives(parent);

            Parent = parent;
        }

        public void Move(string newName, UnionCloudDirectoryNode destinationDirectory)
        {
            throw new NotImplementedException();
        }

        public void Remove()
        {
            throw new NotImplementedException();
        }
    }
}
