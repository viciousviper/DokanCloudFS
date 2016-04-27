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
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class CloudFileNodeTests
    {
        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = Fixture.Initialize();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudFileNode_Create_WhereContractIsMissing_Throws()
        {
            fixture.GetFile(null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudFileNode_Create_WhereContractIsSpecified_StoresContract()
        {
            var contract = fixture.TestFile;

            var sut = fixture.GetFile(contract);

            Assert.AreEqual(contract, sut.Contract);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Objekte nicht mehrmals verwerfen")]
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudFileNode_GetContent_WhereDriveIsNull_Throws()
        {
            var contract = fixture.TestFile;

            var sut = fixture.GetFile(contract);
            sut.GetContent(null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Objekte nicht mehrmals verwerfen")]
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudFileNode_GetContent_Succeeds()
        {
            const string fileContent = "Mary had a little lamb";
            var contract = fixture.TestFile;

            fixture.SetupGetContent(contract, fileContent);

            var sut = fixture.GetFile(contract);
            using (var stream = sut.GetContent(fixture.Drive))
            using (var reader = new StreamReader(stream)) {
                Assert.AreEqual(fileContent, reader.ReadToEnd(), "Mismatched result");
            }

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudFileNode_Move_Succeeds()
        {
            var contract = fixture.TestFile;
            var directory = fixture.TargetDirectory;

            fixture.SetupGetChildItems(directory, fixture.SubDirectoryItems);
            fixture.SetupMove(contract, contract.Name, directory);

            var sut = fixture.GetFile(contract);
            sut.Move(fixture.Drive, contract.Name, new CloudDirectoryNode(directory));

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudFileNode_MoveAndRename_Succeeds()
        {
            const string newName = "RenamedFile.ext";
            var contract = fixture.TestFile;
            var directory = fixture.TargetDirectory;

            fixture.SetupGetChildItems(directory, fixture.SubDirectoryItems);
            fixture.SetupMove(contract, newName, directory);

            var sut = fixture.GetFile(contract);
            sut.Move(fixture.Drive, newName, new CloudDirectoryNode(directory));

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudFileNode_Remove_Succeeds()
        {
            var contract = fixture.TestFile;

            fixture.SetupRemove(contract);

            var sut = fixture.GetFile(contract);
            sut.Remove(fixture.Drive);

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudFileNode_SetContent_WhereDriveIsNull_Throws()
        {
            var contract = fixture.TestFile;

            var sut = fixture.GetFile(contract);
            sut.SetContent(null, Stream.Null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudFileNode_SetContent_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Mary had a little lamb");
            var contract = fixture.TestFile;

            fixture.SetupSetContent(contract, fileContent);

            var sut = fixture.GetFile(contract);
            using (var stream = new MemoryStream(fileContent)) {
                sut.SetContent(fixture.Drive, stream);
            }

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudFileNode_SetContent_OnProxyFileInfo_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Mary had a little lamb");
            var contract = fixture.ProxyTestFile;

            fixture.SetupNewFileItem(fixture.ProxyParentDirectory, contract.Name, fileContent);

            var sut = fixture.GetFile(contract, fixture.ProxyParentDirectory);

            Assert.IsInstanceOfType(sut.Contract, typeof(CloudFS.Interface.IO.ProxyFileInfoContract));

            using (var stream = new MemoryStream(fileContent)) {
                sut.SetContent(fixture.Drive, stream);
            }

            Assert.IsInstanceOfType(sut.Contract, typeof(CloudFS.Interface.IO.FileInfoContract));

            fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudFileNode_Truncate_WhereDriveIsNull_Throws()
        {
            var contract = fixture.TestFile;

            var sut = fixture.GetFile(contract);
            sut.Truncate(null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudFileNode_Truncate_Succeeds()
        {
            var contract = fixture.TestFile;

            fixture.SetupTruncate(contract);

            var sut = fixture.GetFile(contract);
            sut.Truncate(fixture.Drive);

            fixture.VerifyAll();
        }
    }
}
