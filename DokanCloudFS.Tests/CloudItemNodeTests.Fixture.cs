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
using IgorSoft.CloudFS.Interface.IO;
using Moq;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class CloudItemNodeTests
    {
        internal class Fixture
        {
            internal sealed class TestCloudItemNode : CloudItemNode
            {
                public TestCloudItemNode(FileSystemInfoContract contract) : base(contract)
                {
                }

                public new void ResolveContract(FileInfoContract contract)
                {
                    base.ResolveContract(contract);
                }
            }

            private sealed class TestFileSystemInfoContract : FileSystemInfoContract
            {
                public TestFileSystemInfoContract(string id, string name, DateTimeOffset created, DateTimeOffset updated) : base(new TestFileSystemId(id), name, created, updated)
                {
                }

                public override string FullName => Name;

                [Obsolete("Unused property will be removed in a future version.")]
                public override string Mode => "t----";
            }

            private sealed class TestFileSystemId : FileSystemId
            {
                public TestFileSystemId(string id) : base(id)
                {
                }
            }

            private Mock<ICloudDrive> drive;

            public ICloudDrive Drive => drive?.Object ?? (drive = new Mock<ICloudDrive>(MockBehavior.Strict)).Object;

            public readonly FileSystemInfoContract TestItem = new TestFileSystemInfoContract(@"\Item.ext", "Item.ext", "2015-12-31 10:11:12".ToDateTime(), "2015-12-31 20:21:22".ToDateTime());

            public readonly FileInfoContract TestFile = new FileInfoContract(@"\File.ext", "File.ext", "2015-01-02 10:11:12".ToDateTime(), "2015-01-02 20:21:22".ToDateTime(), new FileSize("16kB"), "16384".ToHash());

            public readonly ProxyFileInfoContract MismatchedProxyTestFile = new ProxyFileInfoContract("MismatchedFile.ext");

            public readonly DirectoryInfoContract TargetDirectory = new DirectoryInfoContract(@"\SubDir", "SubDir", "2015-01-01 10:11:12".ToDateTime(), "2015-01-01 20:21:22".ToDateTime());

            public static Fixture Initialize() => new Fixture();

            public CloudItemNode GetItem(FileSystemInfoContract contract)
            {
                return new TestCloudItemNode(contract);
            }
        }
    }
}
