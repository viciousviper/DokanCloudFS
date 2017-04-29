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
using System.Collections.Generic;
using System.Linq;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Configuration;
using IgorSoft.DokanCloudFS.Nodes;

namespace IgorSoft.DokanCloudFS.Tests.Nodes
{
    public partial class UnionFileSystemInfoTests
    {
        private class TestUnionFileSystemInfo : UnionFileSystemInfo
        {
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            public override string FullName => throw new NotImplementedException();

            public TestUnionFileSystemInfo(IDictionary<CloudDriveConfiguration, TestFileSystemInfoContract> testFileSystemInfos) : base(testFileSystemInfos.ToDictionary(i => i.Key, i => i.Value as FileSystemInfoContract))
            {
            }
        }

        private class TestFileSystemInfoContract : FileSystemInfoContract
        {
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            public override string FullName => throw new NotImplementedException();

            [Obsolete]
            public override string Mode => throw new NotImplementedException();

            public TestFileSystemInfoContract(FileSystemId id, string name, DateTimeOffset created, DateTimeOffset updated) : base(id, name, created, updated)
            {
            }
        }

        private class TestFileSystemId : FileSystemId
        {
            public TestFileSystemId(string id) : base(id)
            {
            }
        }
    }
}
