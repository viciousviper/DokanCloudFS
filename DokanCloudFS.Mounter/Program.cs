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
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DokanNet;
using NLog;
using IgorSoft.DokanCloudFS;
using IgorSoft.DokanCloudFS.Mounter.Config;
using IgorSoft.DokanCloudFS.Parameters;

namespace DokanCloudFS.Mounter
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            CompositionInitializer.Preload(typeof(IgorSoft.CloudFS.Interface.Composition.ICloudGateway));
            CompositionInitializer.Initialize("Gateways", "IgorSoft.CloudFS.Gateways.*.dll");
            var factory = new CloudDriveFactory();
            CompositionInitializer.SatisfyImports(factory);

            var mountSection = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).Sections[MountSection.Name] as MountSection;
            if (mountSection == null)
                throw new ConfigurationErrorsException("Mount configuration missing");

            try {
                var logger = new LogFactory().GetCurrentClassLogger();
                var tokenSource = new CancellationTokenSource();
                var tasks = new List<Task>();
                foreach (var drive in mountSection.Drives.Cast<DriveElement>()) {
                    var operations = new CloudOperations(factory.CreateCloudDrive(drive.Schema, drive.UserName, drive.Root, new CloudDriveParameters() { EncryptionKey = drive.EncryptionKey }), logger);

                    tasks.Add(Task.Run(() => operations.Mount(drive.Root, DokanOptions.RemovableDrive, mountSection.Threads, 800, TimeSpan.FromSeconds(drive.Timeout != 0 ? drive.Timeout : 20)), tokenSource.Token));
                }

                Console.ReadKey(true);

                tokenSource.Cancel();
            } finally {
                foreach (var drive in mountSection.Drives.Cast<DriveElement>())
                    Dokan.Unmount(drive.Root[0]);
            }
        }
    }
}
