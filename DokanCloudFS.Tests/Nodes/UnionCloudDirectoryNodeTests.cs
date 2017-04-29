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
using IgorSoft.DokanCloudFS.Nodes;

namespace IgorSoft.DokanCloudFS.Tests.Nodes
{
    [TestClass]
    public sealed partial class UnionCloudDirectoryNodeTests
    {
        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture();
        }

        [TestMethod]
        public void UnionCloudFileNode_GetFileSystemInfo_Succeeds()
        {
            const string directoryName = "testDirectory";

            var directoryInfo = Fixture.GetUnionDirectoryInfo(Fixture.DefaultConfig, directoryName);

            var sut = new UnionCloudDirectoryNode(directoryInfo, fixture.DefaultDrive);

            Assert.AreEqual(directoryInfo, sut.FileSystemInfo, "Unexpected FileSystemInfo");
        }

        [TestMethod]
        public void UnionCloudFileNode_GetChildItems_ReturnsChildItems()
        {
            const string directoryName = "testDirectory";

            var directoryInfo = Fixture.GetUnionDirectoryInfo(Fixture.DefaultConfig, directoryName);
            var childItems = Fixture.GetChildItems();

            fixture.SetupGetChildItems(directoryInfo, childItems);

            var sut = new UnionCloudDirectoryNode(directoryInfo, fixture.DefaultDrive);

            var result = sut.GetChildItems();

            Assert.IsNotNull(result, "GetChildItems() returned null");
            CollectionAssert.AreEqual(childItems, result.Select(i => i.FileSystemInfo).ToList(), "Mismatched result items");
            Assert.IsTrue(result.All(i => i.Parent == sut), "Invalid parent assignment in result items");

            fixture.VerifyAll();
        }

        [TestMethod]
        public void UnionCloudFileNode_GetChildItemByName_WhereChildItemExists_ReturnsItem()
        {
            const string directoryName = "testDirectory";

            var directoryInfo = Fixture.GetUnionDirectoryInfo(Fixture.DefaultConfig, directoryName);
            var childItems = Fixture.GetChildItems();
            var itemName = childItems[childItems.Length - 2].Name;

            fixture.SetupGetChildItems(directoryInfo, childItems);

            var sut = new UnionCloudDirectoryNode(directoryInfo, fixture.DefaultDrive);

            var result = sut.GetChildItemByName(itemName);

            Assert.IsNotNull(result, "Unexpected result");
            Assert.AreEqual(itemName, result.Name, "Mismatched result name");

            fixture.VerifyAll();
        }

        [TestMethod]
        public void UnionCloudFileNode_GetChildItemByName_WhereChildItemDoesNotExist_ReturnsNull()
        {
            const string directoryName = "testDirectory";

            var directoryInfo = Fixture.GetUnionDirectoryInfo(Fixture.DefaultConfig, directoryName);
            var childItems = Fixture.GetChildItems();
            var itemName = "unknown";

            fixture.SetupGetChildItems(directoryInfo, childItems);

            var sut = new UnionCloudDirectoryNode(directoryInfo, fixture.DefaultDrive);

            var result = sut.GetChildItemByName(itemName);

            Assert.IsNull(result, "Unexpected result");

            fixture.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void UnionCloudFileNode_NewDirectoryItem_Throws()
        {
            const string directoryName = "testDirectory";
            const string newDirectoryName = "newDirectory";

            var directoryInfo = Fixture.GetUnionDirectoryInfo(Fixture.DefaultConfig, directoryName);

            var sut = new UnionCloudDirectoryNode(directoryInfo, fixture.DefaultDrive);

            var result = sut.NewDirectoryItem(newDirectoryName);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void UnionCloudFileNode_NewFileItem_Throws()
        {
            const string directoryName = "testDirectory";
            const string newFileName = "newFile.ext";

            var directoryInfo = Fixture.GetUnionDirectoryInfo(Fixture.DefaultConfig, directoryName);

            var sut = new UnionCloudDirectoryNode(directoryInfo, fixture.DefaultDrive);

            var result = sut.NewFileItem(newFileName);
        }
    }
}
