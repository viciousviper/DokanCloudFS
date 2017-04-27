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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Configuration;
using IgorSoft.DokanCloudFS.Nodes;

namespace IgorSoft.DokanCloudFS.Tests.Nodes
{
    [TestClass]
    public sealed class UnionFileInfoTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UnionFileInfo_SetParent_WithIncompleteCloudDriveConfigurations_Throws()
        {
            var configs = Enumerable.Range(0, 5).Select(i => new CloudDriveConfiguration(new RootName($"root_{i}"))).ToArray();
            var fileInfos = Enumerable.Range(0, 5).ToDictionary(
                i => configs[i],
                i => new FileInfoContract("\\FI", "FI", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0), (FileSize)100, i.ToString().ToHash())
            );
            var parentInfos = Enumerable.Range(0, 4).ToDictionary(
                i => configs[i],
                i => new DirectoryInfoContract("\\Parent", "Parent", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0))
            );
            var parent = new UnionDirectoryInfo(parentInfos);

            var sut = new UnionFileInfo(fileInfos);

            sut.SetDirectory(parent);
        }

        [TestMethod]
        public void UnionFileInfo_GetFullName_WhereParentIsRoot_ReturnsFullName()
        {
            const string name = "FI";

            var configs = Enumerable.Range(0, 5).Select(i => new CloudDriveConfiguration(new RootName($"root_{i}"))).ToArray();
            var rootInfos = Enumerable.Range(0, 5).ToDictionary(
                i => configs[i],
                i => new RootDirectoryInfoContract(@"\", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0)) { Drive = new DriveInfoContract($"drive{i}", 1 << 20, 1 << 10) }
            );
            var fileInfos = Enumerable.Range(0, 5).ToDictionary(
                i => configs[i],
                i => new FileInfoContract($"\\{name}", $"{name}", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0), (FileSize)100, i.ToString().ToHash())
            );
            var root = new UnionRootDirectoryInfo(rootInfos);

            var sut = new UnionFileInfo(fileInfos);

            sut.SetDirectory(root);

            Assert.AreEqual($"\\{name}", sut.FullName);
        }

        [TestMethod]
        public void UnionFileInfo_GetFullName_WhereParentIsDirectory_ReturnsFullName()
        {
            const string name = "FI";
            const string parentName = "Parent";

            var configs = Enumerable.Range(0, 5).Select(i => new CloudDriveConfiguration(new RootName($"root_{i}"))).ToArray();
            var rootInfos = Enumerable.Range(0, 5).ToDictionary(
                i => configs[i],
                i => new RootDirectoryInfoContract(@"\", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0)) { Drive = new DriveInfoContract($"drive{i}", 1 << 20, 1 << 10) }
            );
            var parentInfos = Enumerable.Range(0, 5).ToDictionary(
                i => configs[i],
                i => new DirectoryInfoContract($"\\{parentName}", $"{parentName}", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0))
            );
            var fileInfos = Enumerable.Range(0, 5).ToDictionary(
                i => configs[i],
                i => new FileInfoContract($"\\{name}", $"{name}", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0), (FileSize)100, i.ToString().ToHash())
            );
            var root = new UnionRootDirectoryInfo(rootInfos);
            var parent = new UnionDirectoryInfo(parentInfos);
            parent.SetParent(root);

            var sut = new UnionFileInfo(fileInfos);

            sut.SetDirectory(parent);

            Assert.AreEqual($"\\{parentName}\\{name}", sut.FullName);
        }
    }
}
