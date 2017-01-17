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
using Moq;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class CloudDirectoryNodeTests
    {
        internal class Fixture
        {
            private const string mountPoint = "Z:";

            private const long freeSpace = 64 * 1 << 20;

            private const long usedSpace = 36 * 1 << 20;

            private readonly Mock<ICloudDrive> drive;

            private readonly CloudDirectoryNode root;

            public ICloudDrive Drive => drive.Object;

            public readonly DirectoryInfoContract TestDirectory = new DirectoryInfoContract(@"\Dir", "Dir", "2015-01-02 20:11:12".ToDateTime(), "2015-01-02 20:21:22".ToDateTime());

            public readonly DirectoryInfoContract TargetDirectory = new DirectoryInfoContract(@"\SubDir", "SubDir", "2015-01-01 10:11:12".ToDateTime(), "2015-01-01 20:21:22".ToDateTime());

            public FileSystemInfoContract[] SubDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir\SubSubDir", "SubSubDir", "2015-02-01 10:11:12".ToDateTime(), "2015-02-01 20:21:22".ToDateTime()),
                new FileInfoContract(@"\SubDir\SubFile.ext", "SubFile.ext", "2015-02-02 10:11:12".ToDateTime(), "2015-02-02 20:21:22".ToDateTime(), (FileSize)981256915, "981256915".ToHash()),
                new FileInfoContract(@"\SubDir\SecondSubFile.ext", "SecondSubFile.ext", "2015-02-03 10:11:12".ToDateTime(), "2015-02-03 20:21:22".ToDateTime(), (FileSize)30858025, "30858025".ToHash()),
                new FileInfoContract(@"\SubDir\ThirdSubFile.ext", "ThirdSubFile.ext", "2015-02-04 10:11:12".ToDateTime(), "2015-02-04 20:21:22".ToDateTime(), (FileSize)45357, "45357".ToHash())
            };

            public static Fixture Initialize() => new Fixture();

            private Fixture()
            {
                drive = new Mock<ICloudDrive>(MockBehavior.Strict);
                root = new CloudDirectoryNode(new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), "2015-01-01 00:00:00".ToDateTime(), "2015-01-01 00:00:00".ToDateTime()) {
                    Drive = new DriveInfoContract(mountPoint, freeSpace, usedSpace)
                }) { children = new Dictionary<string, CloudItemNode>() };
            }

            public CloudDirectoryNode GetDirectory(DirectoryInfoContract contract)
            {
                var result = new CloudDirectoryNode(contract);
                result.SetParent(root);
                return result;
            }

            public void SetupGetChildItems(DirectoryInfoContract parent, IEnumerable<FileSystemInfoContract> childItems)
            {
                drive
                    .Setup(d => d.GetChildItem(parent))
                    .Returns(childItems);
            }

            public void SetupMove(DirectoryInfoContract source, string movePath, DirectoryInfoContract destination)
            {
                var newName = !string.IsNullOrEmpty(movePath) ? movePath : source.Name;
                drive
                    .Setup(d => d.MoveItem(source, movePath, destination))
                    .Returns(new DirectoryInfoContract(destination.Id.Value + Path.DirectorySeparatorChar + newName, newName, source.Created, source.Updated));
            }

            public void SetupNewDirectoryItem(DirectoryInfoContract parent, string directoryName)
            {
                drive
                    .Setup(d => d.NewDirectoryItem(parent, directoryName))
                    .Returns(new DirectoryInfoContract(parent.Id + Path.DirectorySeparatorChar.ToString() + directoryName, directoryName, DateTimeOffset.Now, DateTimeOffset.Now));
            }

            public void SetupNewFileItem(DirectoryInfoContract parent, string fileName)
            {
                drive
                    .Setup(d => d.NewFileItem(parent, fileName, It.Is<Stream>(s => s.Length == 0)))
                    .Returns(new FileInfoContract(parent.Id + Path.DirectorySeparatorChar.ToString() + fileName, fileName, DateTimeOffset.Now, DateTimeOffset.Now, FileSize.Empty, string.Empty.ToHash()));
            }

            public void SetupRemove(DirectoryInfoContract target)
            {
                drive
                    .Setup(d => d.RemoveItem(target, false));
            }

            public void VerifyAll()
            {
                drive.VerifyAll();
            }
        }
    }
}