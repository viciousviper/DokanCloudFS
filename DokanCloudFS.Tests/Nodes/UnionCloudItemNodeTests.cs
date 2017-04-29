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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface;
using IgorSoft.DokanCloudFS.Configuration;
using IgorSoft.DokanCloudFS.Drives;
using IgorSoft.DokanCloudFS.Nodes;

namespace IgorSoft.DokanCloudFS.Tests.Nodes
{
    [TestClass]
    public sealed partial class UnionCloudItemNodeTests
    {
        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void UnionCloudItemNode_Create_WhereFileSystemInfosAreMissing_Throws()
        {
            var sut = new TestUnionCloudItemNode(null, fixture.DefaultDrive);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void UnionCloudItemNode_Create_WhereCloudDriveIsMissing_Throws()
        {
            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, null);
        }

        [TestMethod]
        public void UnionCloudItemNode_Create_WhereFileSystemInfosAndCloudDriveAreSpecified_Succeeds()
        {
            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, fixture.DefaultDrive);

            Assert.IsNotNull(sut, "Item node creation failed");
        }

        [TestMethod]
        public void UnionCloudItemNode_CreateNew_ForUnionDirectoryInfo_CreatesNewInstanceOnSameDrive()
        {
            const string directoryName = "testDirectory";

            var directoryInfo = Fixture.GetUnionDirectoryInfo(fixture.DefaultConfig, directoryName);

            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, fixture.DefaultDrive);

            var itemNode = sut.CreateNew(directoryInfo);

            Assert.IsInstanceOfType(itemNode, typeof(UnionCloudDirectoryNode), "Unexpected node type");
            Assert.AreEqual(directoryName, itemNode.Name, "Unexpected node name");
            Assert.AreSame(fixture.DefaultDrive, itemNode.Drive, "Invalid node drive");
            Assert.IsNull(((UnionCloudDirectoryNode)itemNode).Parent, "Unexpected node parent");
        }

        [TestMethod]
        public void UnionCloudItemNode_CreateNew_ForUnionFileInfo_CreatesNewInstanceOnSameDrive()
        {
            const string fileName = "testFile.ext";

            var fileInfo = Fixture.GetUnionFileInfo(fixture.DefaultConfig, fileName);

            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, fixture.DefaultDrive);

            var itemNode = sut.CreateNew(fileInfo);

            Assert.IsInstanceOfType(itemNode, typeof(UnionCloudFileNode), "Unexpected node type");
            Assert.AreEqual(fileName, itemNode.Name, "Unexpected node name");
            Assert.AreSame(fixture.DefaultDrive, itemNode.Drive, "Invalid node drive");
            Assert.IsNull(((UnionCloudFileNode)itemNode).Parent, "Unexpected node parent");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UnionCloudItemNode_CreateNew_ForUnknownFileSystemInfo_Throws()
        {
            var unknownInfo = new UnknownUnionFileSystemInfo();

            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, fixture.DefaultDrive);

            var itemNode = sut.CreateNew(unknownInfo);
        }

        [TestMethod]
        public void UnionCloudItemNode_SetParent_WithMatchingConfiguration_Succeeds()
        {
            const string parentName = "parentDirectory";

            var parentDirectory = new UnionCloudDirectoryNode(Fixture.GetUnionDirectoryInfo(fixture.DefaultConfig, parentName), fixture.DefaultDrive);

            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, fixture.DefaultDrive);

            sut.SetParent(parentDirectory);

            Assert.AreSame(parentDirectory, sut.Parent, "Invalid node parent");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UnionCloudItemNode_SetParent_WithMismatchedConfiguration_Throws()
        {
            const string parentName = "parentDirectory";

            var otherConfig = new CloudDriveConfiguration(new RootName("other_root"));
            var parentDirectory = new UnionCloudDirectoryNode(Fixture.GetUnionDirectoryInfo(otherConfig, parentName), fixture.DefaultDrive);

            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, fixture.DefaultDrive);

            sut.SetParent(parentDirectory);

            Assert.AreSame(parentDirectory, sut.Parent, "Invalid node parent");
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void UnionCloudItemNode_Move_Throws()
        {
            const string directoryName = "targetDirectory";

            var targetDirectory = new UnionCloudDirectoryNode(Fixture.GetUnionDirectoryInfo(fixture.DefaultConfig, directoryName), fixture.DefaultDrive);

            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, fixture.DefaultDrive);

            sut.Move(sut.Name, targetDirectory);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void UnionCloudItemNode_Remove_Throws()
        {
            var sut = new TestUnionCloudItemNode(fixture.DefaultDirectory, fixture.DefaultDrive);

            sut.Remove();
        }
    }
}
