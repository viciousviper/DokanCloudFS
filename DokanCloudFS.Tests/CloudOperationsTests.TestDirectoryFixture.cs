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
using System.Linq;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class CloudOperationsTests
    {
        internal class TestDirectoryFixture : IDisposable
        {
            private readonly DirectoryInfo rootDirectory;

            public DirectoryInfo Directory { get; }

            public TestDirectoryFixture(DirectoryInfo rootDirectory, string path)
            {
                this.rootDirectory = rootDirectory;

                var residualDirectory = rootDirectory.GetDirectories().SingleOrDefault(d => d.Name == path);
                residualDirectory?.Delete(true);

                Directory = rootDirectory.CreateSubdirectory(path);
            }

            public DirectoryInfo CreateSubdirectory(string directoryName) => Directory.CreateSubdirectory(directoryName);

            public DirectoryInfo CreateDirectory(string directoryName)
            {
                var directory = new DirectoryInfo(Path.Combine(Directory.FullName, directoryName));
                directory.Create();
                return directory;
            }

            public FileInfo CreateFile(string fileName, byte[] content, DirectoryInfo parentDirectory = null)
            {
                var file = new FileInfo(Path.Combine(parentDirectory?.FullName ?? Directory.FullName, fileName));
                if (content != null)
                    using (var fileStream = file.Create()) {
                        fileStream.WriteAsync(content, 0, content.Length).Wait();
                        fileStream.Close();
                    }
                return file;
            }

            public void Dispose()
            {
                Directory.Delete(true);
            }
        }
    }
}
