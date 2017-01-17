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
using System.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DokanNet;
using Microsoft.Extensions.CommandLineUtils;
using NLog;
using IgorSoft.CloudFS.Authentication;
using IgorSoft.CloudFS.Interface;
using IgorSoft.DokanCloudFS.Mounter.Config;
using IgorSoft.DokanCloudFS.Parameters;

namespace IgorSoft.DokanCloudFS.Mounter
{
    internal sealed class Program
    {
        private static string settingsPassPhrase;

        private static ILogger logger;

        public sealed class ExportProvider
        {
            /// <summary>
            /// Gets the passphrase used in the encryption of privacy sensitive gateway settings.
            /// </summary>
            /// <value>The settings encryption passphrase.</value>
            [Export(CloudFS.Interface.Composition.ExportContracts.SettingsPassPhrase)]
            public string SettingsPassPhrase => settingsPassPhrase;

            /// <summary>
            /// Gets the logger.
            /// </summary>
            /// <value>The logger.</value>
            [Export]
            public ILogger Logger => logger;
        }

        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <exception cref="ConfigurationErrorsException">Mount configuration missing</exception>
        /// <remarks>
        /// IgorSoft.DokanCloudFS.Mounter [mount [<userNames>] [-p|--passPhrase <passPhrase>]]
        ///                               [reset [<userNames>]]
        ///                               [-?|-h|--help]
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
        internal static void Main(string[] args)
        {
            new Program().ParseCommandLine(args);
        }

        private void ParseCommandLine(string[] args)
        {
            var commandLine = new CommandLineApplication() {
                Name = "Mounter", FullName = "IgorSoft.DokanCloudFS.Mounter", Description = "A mount manager for cloud drives",
                ShortVersionGetter = () => GetFileVersion(typeof(Program).Assembly, 3), LongVersionGetter = () => GetFileVersion(typeof(Program).Assembly, 4)
            };
            commandLine.HelpOption("-?|-h|--help");

            commandLine.Command("mount", c => {
                var userNames = c.Argument("<userNames>", "If specified, mount the drives associated with the specified users; otherwise, mount all configured drives.", true);
                var passPhrase = c.Option("-p|--passPhrase", "The pass phrase used to encrypt persisted user credentials and access tokens", CommandOptionType.SingleValue);
                c.HelpOption("-?|-h|--help");
                c.OnExecute(() => Mount(passPhrase.Value(), userNames.Values));
            });

            commandLine.Command("reset", c => {
                var userNames = c.Argument("<userNames>", "If specified, purge the persisted settings of the drives associated with the specified users; otherwise, purge the persisted settings of all configured drives.", true);
                c.HelpOption("-?|-h|--help");
                c.OnExecute(() => Reset(userNames.Values));
            });

            commandLine.Execute(args);
        }

        private string GetFileVersion(Assembly assembly, int components)
        {
            var fileVersion = (string)assembly.CustomAttributes.Single(c => c.AttributeType == typeof(AssemblyFileVersionAttribute)).ConstructorArguments[0].Value;
            var versionComponents = fileVersion.Split('.');
            return string.Join(".", versionComponents.Take(components).ToArray());
        }

        private CloudDriveFactory InitializeCloudDriveFactory(string libPath)
        {
            CompositionInitializer.Preload(typeof(CloudFS.Interface.Composition.ICloudGateway));
            CompositionInitializer.Initialize(new[] { typeof(Program).Assembly }, libPath, "IgorSoft.CloudFS.Gateways.*.dll");
            var factory = new CloudDriveFactory();
            CompositionInitializer.SatisfyImports(factory);
            return factory;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private int Mount(string passPhrase, IList<string> userNames)
        {
            var mountSection = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).Sections[MountSection.Name] as MountSection;
            if (mountSection == null)
                throw new ConfigurationErrorsException("Mount configuration missing");

            settingsPassPhrase = passPhrase;

            try {
                using (var logFactory = new LogFactory()) {
                    logger = logFactory.GetCurrentClassLogger();
                    var factory = InitializeCloudDriveFactory(mountSection.LibPath);
                    using (var tokenSource = new CancellationTokenSource()) {
                        var tasks = new List<Task>();
                        foreach (var driveElement in mountSection.Drives.Where(d => !userNames.Any() || userNames.Contains(d.UserName))) {
                            var drive = factory.CreateCloudDrive(driveElement.Schema, driveElement.UserName, driveElement.Root, new CloudDriveParameters() { ApiKey = driveElement.ApiKey, EncryptionKey = driveElement.EncryptionKey, Parameters = driveElement.GetParameters() });
                            if (!drive.TryAuthenticate()) {
                                var displayRoot = drive.DisplayRoot;
                                drive.Dispose();
                                logger.Warn($"Authentication failed for drive '{displayRoot}'");
                                continue;
                            }

                            var operations = new CloudOperations(drive, logger);

                            // HACK: handle non-unique parameter set of DokanOperations.Mount() by explicitely specifying AllocationUnitSize and SectorSize
                            tasks.Add(Task.Run(() => operations.Mount(driveElement.Root, DokanOptions.RemovableDrive | DokanOptions.MountManager | DokanOptions.CurrentSession, mountSection.Threads, 1100, TimeSpan.FromSeconds(driveElement.Timeout != 0 ? driveElement.Timeout : 20), null, 512, 512), tokenSource.Token));

                            var driveInfo = new DriveInfo(driveElement.Root);
                            while (!driveInfo.IsReady)
                                Thread.Sleep(10);
                            logger.Info($"Drive '{drive.DisplayRoot}' mounted successfully.");
                        }

                        Console.WriteLine("Press CTRL-BACKSPACE to clear log, any other key to unmount drives");
                        while (true) {
                            var keyInfo = Console.ReadKey(true);
                            if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) && keyInfo.Key == ConsoleKey.Backspace)
                                Console.Clear();
                            else
                                break;
                        }

                        tokenSource.Cancel();

                        return 0;
                    }
                }
            } catch (Exception ex) {
                Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                return -1;
            } finally {
                foreach (var driveElement in mountSection.Drives.Cast<DriveElement>())
                    Dokan.Unmount(driveElement.Root[0]);
                UIThread.Shutdown();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private int Reset(IList<string> userNames)
        {
            var mountSection = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).Sections[MountSection.Name] as MountSection;
            if (mountSection == null)
                throw new ConfigurationErrorsException("Mount configuration missing");

            var factory = InitializeCloudDriveFactory(mountSection.LibPath);

            try
            {
                foreach (var driveElement in mountSection.Drives.Where(d => !userNames.Any() || userNames.Contains(d.UserName))) {
                    using (var drive = factory.CreateCloudDrive(driveElement.Schema, driveElement.UserName, driveElement.Root, new CloudDriveParameters())) {
                        drive.PersistSettings?.PurgeSettings(new RootName(driveElement.Schema, driveElement.UserName, driveElement.Root));
                    }
                }

                return 0;
            } catch (Exception ex) {
                Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                return -1;
            }
        }
    }
}
