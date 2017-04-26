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
using System.Runtime.CompilerServices;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Drives;

namespace IgorSoft.DokanCloudFS.Nodes
{
    internal abstract class CloudItemNode
    {
        protected CloudDirectoryNode Parent { get; private set; }

        public FileSystemInfoContract FileSystemInfo { get; set; }

        public string Name => FileSystemInfo.Name;

        public virtual bool IsResolved => true;

        public ICloudDrive Drive { get; private set; }

        protected CloudItemNode(FileSystemInfoContract fileSystemInfo, ICloudDrive drive)
        {
            FileSystemInfo = fileSystemInfo ?? throw new ArgumentNullException(nameof(fileSystemInfo));
            Drive = drive ?? throw new ArgumentNullException(nameof(drive));
        }

        protected internal CloudItemNode CreateNew(FileSystemInfoContract fileSystemInfo)
        {
            var fileInfoContract = fileSystemInfo as FileInfoContract;
            if (fileInfoContract != null)
                return new CloudFileNode(fileInfoContract, Drive);

            var directoryInfoContract = fileSystemInfo as DirectoryInfoContract;
            if (directoryInfoContract != null)
                return new CloudDirectoryNode(directoryInfoContract, Drive);

            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownItemType, fileSystemInfo.GetType().Name));
        }

        protected void EnsureSameDrive(CloudItemNode otherNode, [CallerMemberName] string operation = null)
        {
            if (otherNode.Drive != Drive)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.CrossDriveOperation, operation, otherNode.Drive, Drive));
        }

        public virtual void SetParent(CloudDirectoryNode parent)
        {
            if (parent != null)
                EnsureSameDrive(parent);

            Parent = parent;
        }

        public void Move(string newName, CloudDirectoryNode destinationDirectory)
        {
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));
            if (destinationDirectory == null)
                throw new ArgumentNullException(nameof(destinationDirectory));
            EnsureSameDrive(destinationDirectory);
            if (Parent == null)
                throw new InvalidOperationException($"{nameof(Parent)} of {GetType().Name} '{Name}' is null".ToString(CultureInfo.CurrentCulture));

            var moveItem = CreateNew(Drive.MoveItem(FileSystemInfo, newName, destinationDirectory.FileSystemInfo));
            if (destinationDirectory.children != null) {
                destinationDirectory.children.Add(moveItem.Name, moveItem);
                moveItem.SetParent(destinationDirectory);
            } else {
                destinationDirectory.GetChildItems();
            }
            Parent.children.Remove(Name);
            SetParent(null);
        }

        public void Remove()
        {
            Parent.children.Remove(Name);
            Drive.RemoveItem(FileSystemInfo, false);
            SetParent(null);
        }
    }
}
