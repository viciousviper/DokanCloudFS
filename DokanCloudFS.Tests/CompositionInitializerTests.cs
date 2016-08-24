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
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class CompositionInitializerTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            Fixture.ResetCompositionInitializer();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CompositionInitializer_Preload_WhereTypeIsNull_Throws()
        {
            CompositionInitializer.Preload(null);
        }

        [TestMethod]
        public void CompositionInitializer_Preload_Succeeds()
        {
            CompositionInitializer.Preload(typeof(CompositionInitializerTests));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CompositionInitializer_InitializeByAssemblies_WhereAssembliesAreNull_Throws()
        {
            CompositionInitializer.Initialize((IEnumerable<Assembly>)null);
        }

        [TestMethod]
        public void CompositionInitializer_InitializeByAssemblies_WhereAssembliesAreSpecified_Succeeds()
        {
            var onHostInitializedHandled = false;
            EventHandler hostInitializedHandler = (s, e) => onHostInitializedHandled = true;

            CompositionInitializer.HostInitialized += hostInitializedHandler;
            try {
                CompositionInitializer.Initialize(new[] { typeof(CompositionInitializerTests).Assembly });
            } finally {
                CompositionInitializer.HostInitialized -= hostInitializedHandler;
            }

            Assert.IsTrue(onHostInitializedHandled, "HostInitialized event not handled");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CompositionInitializer_InitializeByAssemblies_MoreThanOnce_Throws()
        {
            var onHostInitializedHandled = false;
            EventHandler hostInitializedHandler = (s, e) => onHostInitializedHandled = true;

            CompositionInitializer.HostInitialized += hostInitializedHandler;
            try {
                CompositionInitializer.Initialize(Enumerable.Empty<Assembly>());
            } finally {
                CompositionInitializer.HostInitialized -= hostInitializedHandler;
            }

            Assert.IsTrue(onHostInitializedHandled, "HostInitialized event not handled");

            CompositionInitializer.Initialize(Enumerable.Empty<Assembly>());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CompositionInitializer_InitializeByPath_WherePathIsNull_Throws()
        {
            CompositionInitializer.Initialize(null, null);
        }

        [TestMethod]
        public void CompositionInitializer_InitializeByPath_WherePathIsSpecified_Succeeds()
        {
            var onHostInitializedHandled = false;
            EventHandler hostInitializedHandler = (s, e) => onHostInitializedHandled = true;

            CompositionInitializer.HostInitialized += hostInitializedHandler;
            try {
                CompositionInitializer.Initialize(".", "Missing.dll");
            } finally {
                CompositionInitializer.HostInitialized -= hostInitializedHandler;
            }

            Assert.IsTrue(onHostInitializedHandled, "HostInitialized event not handled");
        }

        [TestMethod]
        public void CompositionInitializer_InitializeByPath_WherePathIsSpecifiedWithoutSearchPattern_Succeeds()
        {
            var onHostInitializedHandled = false;
            EventHandler hostInitializedHandler = (s, e) => onHostInitializedHandled = true;

            CompositionInitializer.HostInitialized += hostInitializedHandler;
            try {
                CompositionInitializer.Initialize(".");
            } finally {
                CompositionInitializer.HostInitialized -= hostInitializedHandler;
            }

            Assert.IsTrue(onHostInitializedHandled, "HostInitialized event not handled");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CompositionInitializer_InitializeByPath_MoreThanOnce_Throws()
        {
            var onHostInitializedHandled = false;
            EventHandler hostInitializedHandler = (s, e) => onHostInitializedHandled = true;

            CompositionInitializer.HostInitialized += hostInitializedHandler;
            try {
                CompositionInitializer.Initialize(".", "Missing.dll");
            } finally {
                CompositionInitializer.HostInitialized -= hostInitializedHandler;
            }

            Assert.IsTrue(onHostInitializedHandled, "HostInitialized event not handled");

            CompositionInitializer.Initialize(".", "Missing.dll");
        }

        [TestMethod]
        public void CompositionInitializer_InitializeByPathAndAssemblies_Succeeds()
        {
            var onHostInitializedHandled = false;
            EventHandler hostInitializedHandler = (s, e) => onHostInitializedHandled = true;

            CompositionInitializer.HostInitialized += hostInitializedHandler;
            try
            {
                CompositionInitializer.Initialize(Enumerable.Empty<Assembly>(), ".", "Missing.dll");
            }
            finally
            {
                CompositionInitializer.HostInitialized -= hostInitializedHandler;
            }

            Assert.IsTrue(onHostInitializedHandled, "HostInitialized event not handled");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CompositionInitializer_SatisfyImports_WithoutInitialization_Throws()
        {
            var composablePart = Fixture.GetComposablePart();

            CompositionInitializer.SatisfyImports(composablePart);
        }

        [TestMethod]
        public void CompositionInitializer_SatisfyImports_Succeeds()
        {
            CompositionInitializer.Initialize(new[] { typeof(CompositionInitializerTests).Assembly });

            var composablePart = Fixture.GetComposablePart();

            CompositionInitializer.SatisfyImports(composablePart);

            Assert.IsNotNull(composablePart.Component, "Composition of component failed");
        }
    }
}
