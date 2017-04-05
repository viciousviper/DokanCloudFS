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
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using IgorSoft.AppDomainResolver;

namespace IgorSoft.DokanCloudFS
{
    internal static class CompositionInitializer
    {
        private static CompositionHost host;

        private static readonly string assemblyFileSearchPattern = typeof(CompositionInitializer).Namespace + ".*.dll";

        internal static ContainerConfiguration ConfigurationPreset { get; set; }

        private static CompositionHost InitializeHost(IEnumerable<Assembly> assemblies)
        {
            var configuration = ConfigurationPreset ?? new ContainerConfiguration();

            if (assemblies != null) {
                AssemblyResolver.Initialize();
                foreach (var assembly in assemblies) {
                    configuration.WithAssembly(assembly);
                    AssemblyResolver.RegisterAssembly(assembly);
                }
            }

            configuration.WithAssembly(typeof(CompositionInitializer).Assembly);

            return configuration.CreateContainer();
        }

        private static void OnHostInitialized()
        {
            var handler = HostInitialized;
            handler?.Invoke(typeof(CompositionInitializer), EventArgs.Empty);
        }

        public static event EventHandler HostInitialized;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "name", Justification = "Required for composition initialization")]
        public static void Preload(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var name = type.Name;
        }

        public static void Initialize(string path)
        {
            Initialize(path, assemblyFileSearchPattern);
        }

        public static void Initialize(string path, string searchPattern)
        {
            Initialize(Enumerable.Empty<Assembly>(), path, searchPattern);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        public static void Initialize(IEnumerable<Assembly> assemblies, string path, string searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (host != null)
                throw new InvalidOperationException(Resources.CompositionHostAlreadyInitialized);

            var combinedAssemblies = new List<Assembly>(assemblies);

            foreach (var part in path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
                var directory = new DirectoryInfo(part);
                if (directory.Exists)
                    foreach (var file in directory.EnumerateFiles(searchPattern))
                        combinedAssemblies.Add(Assembly.LoadFrom(file.FullName));
            }

            Initialize(combinedAssemblies);
        }

        public static void Initialize(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));
            if (host != null)
                throw new InvalidOperationException(Resources.CompositionHostAlreadyInitialized);

            host = InitializeHost(assemblies);
            OnHostInitialized();
        }

        public static void SatisfyImports(object part)
        {
            if (host == null)
                throw new InvalidOperationException(Resources.CompositionHostNotInitialized);

            host.SatisfyImports(part);
        }
    }
}