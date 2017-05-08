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
using System.IO;
using Moq;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Configuration;
using IgorSoft.DokanCloudFS.Drives;
using IgorSoft.DokanCloudFS.Nodes;

namespace IgorSoft.DokanCloudFS.Tests.Nodes
{
    public partial class UnionCloudFileNodeTests
    {
        private class Fixture
        {
            private const string defaultRootName = "default_root";

            private readonly Mock<IUnionCloudDrive> defaultDriveMock = new Mock<IUnionCloudDrive>();

            public static CloudDriveConfiguration DefaultConfig { get; } = GetCloudDriveConfiguration(defaultRootName);

            public IUnionCloudDrive DefaultDrive => defaultDriveMock.Object;

            public static CloudDriveConfiguration GetCloudDriveConfiguration(string rootName) => new CloudDriveConfiguration(new RootName(rootName));

            public static UnionFileInfo GetUnionFileInfo(CloudDriveConfiguration config, string name) => new UnionFileInfo(new Dictionary<CloudDriveConfiguration, FileInfoContract>()
            {
                [config] = new FileInfoContract($"\\{name}", name, DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0), (FileSize)100, name.ToHash())
            });

            public void SetupGetContent(UnionFileInfo source, CloudDriveConfiguration config, Stream content)
            {
                defaultDriveMock
                    .Setup(d => d.GetContent(source, config))
                    .Returns(content)
                    .Verifiable();
            }

            public void SetupSetContent(UnionFileInfo target, CloudDriveConfiguration config, Stream content)
            {
                defaultDriveMock
                    .Setup(d => d.SetContent(target, config, content))
                    .Verifiable();
            }

            public void SetupTruncate(UnionFileInfo target, CloudDriveConfiguration config)
            {
                defaultDriveMock
                    .Setup(d => d.SetContent(target, config, It.Is<Stream>(s => s.Length == 0)))
                    .Verifiable();
            }

            public void VerifyAll()
            {
                defaultDriveMock.VerifyAll();
            }
        }
    }
}
