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
using System.Globalization;
using System.Linq;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Configuration;

namespace IgorSoft.DokanCloudFS.Nodes
{
    internal abstract class UnionFileSystemInfo
    {
        public string Name { get; }

        public abstract string FullName { get; }

        protected internal IDictionary<CloudDriveConfiguration, FileSystemInfoContract> FileSystemInfos { get; }

        protected UnionFileSystemInfo(IDictionary<CloudDriveConfiguration, FileSystemInfoContract> fileSystemInfos)
        {
            if (fileSystemInfos == null || !fileSystemInfos.Any())
                throw new ArgumentNullException(nameof(fileSystemInfos));

            var distinctNames = fileSystemInfos.Values.Select(f => f.Name).Distinct().ToArray();
            if (distinctNames.Length > 1)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.InconsistentNames, nameof(fileSystemInfos)));

            FileSystemInfos = fileSystemInfos;
            Name = distinctNames.Single();
        }
    }
}
