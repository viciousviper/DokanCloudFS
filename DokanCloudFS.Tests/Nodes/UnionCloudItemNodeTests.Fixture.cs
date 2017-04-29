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
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Configuration;
using IgorSoft.DokanCloudFS.Drives;
using IgorSoft.DokanCloudFS.Nodes;

namespace IgorSoft.DokanCloudFS.Tests.Nodes
{
    public partial class UnionCloudItemNodeTests
    {
        private class TestUnionCloudItemNode: UnionCloudItemNode
        {
            public TestUnionCloudItemNode(UnionFileSystemInfo fileSystemInfo, UnionCloudDrive drive) : base(fileSystemInfo, drive)
            {
            }
        }

        private class UnknownFileSystemId : FileSystemId
        {
            public UnknownFileSystemId(string id) : base(id)
            {
            }
        }

        private class UnknownFileSystemInfoContract : FileSystemInfoContract
        {
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            public override string FullName => throw new NotImplementedException();

            [Obsolete]
            public override string Mode => throw new NotImplementedException();

            public UnknownFileSystemInfoContract(string name) : base(new UnknownFileSystemId($"\\{name}"), name, DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0))
            {
            }
        }

        private class UnknownUnionFileSystemInfo: UnionFileSystemInfo
        {
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            public override string FullName => throw new NotImplementedException();

            public UnknownUnionFileSystemInfo() : base(new Dictionary<CloudDriveConfiguration, FileSystemInfoContract>() {
                [new CloudDriveConfiguration(new RootName("root_unknown"))] = new UnknownFileSystemInfoContract("unknown")
            })
            {
            }
        }

        private static class Fixture
        {
            public static CloudDriveConfiguration GetCloudDriveConfiguration(string rootName) => new CloudDriveConfiguration(new RootName(rootName));

            public static UnionDirectoryInfo GetUnionDirectoryInfo(CloudDriveConfiguration config, string name) => new UnionDirectoryInfo(new Dictionary<CloudDriveConfiguration, DirectoryInfoContract>() {
                [config] = new DirectoryInfoContract($"\\{name}", name, DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0))
            });

            public static UnionFileInfo GetUnionFileInfo(CloudDriveConfiguration config, string name) => new UnionFileInfo(new Dictionary<CloudDriveConfiguration, FileInfoContract>()
            {
                [config] = new FileInfoContract($"\\{name}", name, DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0), (FileSize)100, name.ToHash())
            });

            public static UnionCloudDrive GetUnionCloudDrive(string rootName) => new UnionCloudDrive(new RootName(rootName), new Dictionary<CloudDriveConfiguration, IAsyncCloudGateway>(), new Dictionary<CloudDriveConfiguration, ICloudGateway>());
        }
    }
}
