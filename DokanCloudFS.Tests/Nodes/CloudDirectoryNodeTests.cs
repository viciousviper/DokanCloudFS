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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.DokanCloudFS.Nodes;

namespace IgorSoft.DokanCloudFS.Tests.Nodes
{
    [TestClass]
    public sealed partial class CloudDirectoryNodeTests
    {
        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = Fixture.Initialize();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudDirectoryNode_Create_WhereContractIsMissing_Throws()
        {
            fixture.GetDirectory(null, fixture.Drive);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_Create_WhereContractIsSpecified_StoresContract()
        {
            var directory = fixture.TargetDirectory;

            var sut = fixture.GetDirectory(directory, fixture.Drive);

            Assert.AreEqual(directory, sut.FileSystemInfo);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudDirectoryNode_Create_WhereDriveIsMissing_Throws()
        {
            fixture.GetDirectory(fixture.TargetDirectory, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_Create_WhereDriveIsSpecified_StoresContract()
        {
            var drive = fixture.Drive;

            var sut = fixture.GetDirectory(fixture.TargetDirectory, drive);

            Assert.AreEqual(drive, sut.Drive);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_GetChildItemByName_CallsDriveCorrectly()
        {
            var fileName = fixture.SubDirectoryItems.First().Name;
            var directory = fixture.TargetDirectory;

            fixture.SetupGetChildItems(directory, fixture.SubDirectoryItems);

            var sut = fixture.GetDirectory(directory, fixture.Drive);
            var result = sut.GetChildItemByName(fileName);

            Assert.AreEqual(fileName, result.Name, "Mismatched result");

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_GetChildItems_CallsDriveCorrectly()
        {
            var directory = fixture.TargetDirectory;

            fixture.SetupGetChildItems(directory, fixture.SubDirectoryItems);

            var sut = fixture.GetDirectory(directory, fixture.Drive);
            var result = sut.GetChildItems();

            CollectionAssert.AreEqual(fixture.SubDirectoryItems, result.Select(i => i.FileSystemInfo).ToArray(), "Mismatched result");

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_Move_Succeeds()
        {
            var contract = fixture.TestDirectory;
            var directory = fixture.TargetDirectory;

            fixture.SetupGetChildItems(directory, fixture.SubDirectoryItems);
            fixture.SetupMove(contract, contract.Name, directory);

            var sut = fixture.GetDirectory(contract, fixture.Drive);
            sut.Move(contract.Name, new CloudDirectoryNode(directory, fixture.Drive));

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_MoveAndRename_Succeeds()
        {
            const string newName = "RenamedDirectory";
            var contract = fixture.TestDirectory;
            var directory = fixture.TargetDirectory;

            fixture.SetupGetChildItems(directory, fixture.SubDirectoryItems);
            fixture.SetupMove(contract, newName, directory);

            var sut = fixture.GetDirectory(contract, fixture.Drive);
            sut.Move(newName, new CloudDirectoryNode(directory, fixture.Drive));

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_NewDirectoryItem_Succeeds()
        {
            const string newName = "NewDirectory";
            var contract = fixture.TestDirectory;

            fixture.SetupGetChildItems(contract, fixture.SubDirectoryItems);
            fixture.SetupNewDirectoryItem(contract, newName);

            var sut = fixture.GetDirectory(contract, fixture.Drive);
            sut.GetChildItems();
            var result = sut.NewDirectoryItem(newName);

            Assert.IsNotNull(result, "DirectoryNode not created");

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_NewFileItem_Succeeds()
        {
            const string newName = "NewFile.ext";
            var contract = fixture.TestDirectory;

            fixture.SetupGetChildItems(contract, fixture.SubDirectoryItems);
            fixture.SetupNewFileItem(contract, newName);

            var sut = fixture.GetDirectory(contract, fixture.Drive);
            sut.GetChildItems();
            var result = sut.NewFileItem(newName);

            Assert.IsNotNull(result, "FileNode not created");

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudDirectoryNode_Remove_Succeeds()
        {
            var contract = fixture.TestDirectory;

            fixture.SetupRemove(contract);

            var sut = fixture.GetDirectory(contract, fixture.Drive);
            sut.Remove();

            fixture.VerifyAll();
        }
    }
}
