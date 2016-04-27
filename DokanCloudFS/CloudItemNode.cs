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
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS
{
    internal abstract class CloudItemNode
    {
        public FileSystemInfoContract Contract { get; private set; }

        protected CloudDirectoryNode Parent { get; private set; }

        public string Name => Contract.Name;

        public bool IsResolved => !(Contract is ProxyFileInfoContract);

        protected CloudItemNode(FileSystemInfoContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            Contract = contract;
        }

        public static CloudItemNode CreateNew(FileSystemInfoContract fileSystemInfo)
        {
            var fileInfoContract = fileSystemInfo as FileInfoContract;
            if (fileInfoContract != null)
                return new CloudFileNode(fileInfoContract);

            var directoryInfoContract = fileSystemInfo as DirectoryInfoContract;
            if (directoryInfoContract != null)
                return new CloudDirectoryNode(directoryInfoContract);

            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownItemType, fileSystemInfo.GetType().Name));
        }

        protected void ResolveContract(FileInfoContract contract)
        {
            if (IsResolved)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidNonProxyResolution, Contract.GetType().Name, Contract.Name));
            if (Contract.Name != contract.Name)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidProxyResolution, Contract.Name, contract.Name));

            Contract = contract;
        }

        public virtual void SetParent(CloudDirectoryNode parent)
        {
            Parent = parent;
        }

        public void Move(ICloudDrive drive, string newName, CloudDirectoryNode destinationDirectory)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));
            if (destinationDirectory == null)
                throw new ArgumentNullException(nameof(destinationDirectory));
            if (Parent == null)
                throw new InvalidOperationException($"{nameof(Parent)} of {GetType().Name} '{Name}' is null".ToString(CultureInfo.CurrentCulture));

            var moveItem = CreateNew(drive.MoveItem(Contract, newName, destinationDirectory.Contract));
            if (destinationDirectory.children != null) {
                destinationDirectory.children.Add(moveItem.Name, moveItem);
                moveItem.SetParent(destinationDirectory);
            } else {
                destinationDirectory.GetChildItems(drive);
            }
            Parent.children.Remove(Name);
            SetParent(null);
        }

        public void Remove(ICloudDrive drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            Parent.children.Remove(Name);
            drive.RemoveItem(Contract, false);
            SetParent(null);
        }
    }
}