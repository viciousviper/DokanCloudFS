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
using System.IO;
using IgorSoft.DokanCloudFS.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests.Nodes
{
    [TestClass]
    public sealed partial class UnionCloudFileNodeTests
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
            const string fileName = "testFile.ext";

            var fileInfo = Fixture.GetUnionFileInfo(Fixture.DefaultConfig, fileName);

            var sut = new UnionCloudFileNode(fileInfo, fixture.DefaultDrive);

            Assert.AreEqual(fileInfo, sut.FileSystemInfo, "Unexpected FileSystemInfo");
        }

        [TestMethod]
        public void UnionCloudFileNode_GetContent_Succeeds()
        {
            const string fileName = "testFile.ext";
            var config = Fixture.DefaultConfig;

            var fileInfo = Fixture.GetUnionFileInfo(config, fileName);
            var content = new MemoryStream();

            fixture.SetupGetContent(fileInfo, config, content);

            var sut = new UnionCloudFileNode(fileInfo, fixture.DefaultDrive);

            var result = sut.GetContent(config);

            fixture.VerifyAll();
        }

        [TestMethod]
        public void UnionCloudFileNode_SetContent_Succeeds()
        {
            const string fileName = "testFile.ext";
            var config = Fixture.DefaultConfig;

            var fileInfo = Fixture.GetUnionFileInfo(config, fileName);
            var content = new MemoryStream();

            fixture.SetupSetContent(fileInfo, config, content);

            var sut = new UnionCloudFileNode(fileInfo, fixture.DefaultDrive);

            sut.SetContent(config, content);

            fixture.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void UnionCloudFileNode_Truncate_Throws()
        {
            const string fileName = "testFile.ext";
            var config = Fixture.DefaultConfig;

            var fileInfo = Fixture.GetUnionFileInfo(config, fileName);

            var sut = new UnionCloudFileNode(fileInfo, fixture.DefaultDrive);

            sut.Truncate(config);
        }
    }
}
