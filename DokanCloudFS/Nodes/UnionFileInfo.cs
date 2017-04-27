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
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class UnionFileInfo : UnionFileSystemInfo
    {
        private UnionDirectoryInfo directory;

        public override string FullName => directory != null ? $"{directory.FullName.TrimEnd('\\')}\\{Name}" : Name;

        public UnionFileInfo(IDictionary<CloudDriveConfiguration, FileInfoContract> fileInfos) : base(fileInfos.ToDictionary(i => i.Key, i => i.Value as FileSystemInfoContract))
        {
        }

        public void SetDirectory(UnionDirectoryInfo directory)
        {
            this.directory = directory;

            foreach (var fileSystemInfo in FileSystemInfos) {
                var directoryByConfiguration = default(FileSystemInfoContract);
                if (!directory?.FileSystemInfos.TryGetValue(fileSystemInfo.Key, out directoryByConfiguration) ?? false)
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.ConfigurationEntryMissing, fileSystemInfo.Key.RootName.Value));

                ((FileInfoContract)fileSystemInfo.Value).Directory = (DirectoryInfoContract)directoryByConfiguration;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(UnionFileInfo)} '{Name}' {{{string.Join(",", FileSystemInfos.Select(i => i.Key.RootName))}}}".ToString(CultureInfo.CurrentCulture);
    }
}
