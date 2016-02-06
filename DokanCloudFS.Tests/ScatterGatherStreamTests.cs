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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class ScatterGatherStreamTests
    {
        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture();
        }

        [TestCleanup]
        public void Cleanup()
        {
            fixture = null;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBuffersConcurrently_WhereReaderIsFlooded_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrently(source, TimeSpan.Zero, 100, TimeSpan.Zero, TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(10));

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBuffersConcurrently_WhereReaderIsSynced_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrently(source, TimeSpan.Zero, 50, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(5));

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBuffersConcurrently_WhereReaderIsStarved_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrently(source, TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(10), TimeSpan.Zero, 100, TimeSpan.Zero);

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBuffersConcurrently_WhereLimitIsSpecified_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrently(source, TimeSpan.Zero, 50, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(5), false);

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBuffersConcurrently_WithExplicitPermutationRetrograde_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrentlyByPermutation(source, TimeSpan.FromMilliseconds(10), 200, TimeSpan.FromMilliseconds(10), new int[] { 4, 3, 2, 1, 0 }, TimeSpan.Zero, 200, TimeSpan.Zero, new int[] { 0, 1, 2, 3, 4 });

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBuffersConcurrently_WithExplicitPermutationInterleaved_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrentlyByPermutation(source, TimeSpan.FromMilliseconds(10), 200, TimeSpan.FromMilliseconds(10), new int[] { 4, 2, 0, 3, 1 }, TimeSpan.Zero, 200, TimeSpan.Zero, new int[] { 0, 2, 4, 1, 3 });

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ReadAsync_WhereReaderTimesOut_Throws()
        {
            var source = fixture.InitializeBuffer(1000);

            try {
                var scatterStream = default(Stream);
                fixture.ReadBufferConcurrently(source, 100, TimeSpan.Zero, TimeSpan.FromMilliseconds(100), out scatterStream);

                Assert.Fail("Expected Exception is missing");
            } catch (AggregateException ex) {
                Console.WriteLine($"Exception thrown: {ex.InnerException.Message}");
                Assert.AreEqual(1, ex.InnerExceptions.Count, $"Excessive number of Exceptions");
                Assert.IsInstanceOfType(ex.InnerException, typeof(TimeoutException), $"Unexpected Exception type {ex.InnerException.GetType().Name}");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void WriteAsync_WhereCountExceedsLimit_Throws()
        {
            var source = fixture.InitializeBuffer(1000);

            try {
                var gatherStream = default(Stream);
                var target = new byte[source.Length / 2];
                fixture.WriteBufferConcurrently(source, target, 100, TimeSpan.Zero, TimeSpan.FromMilliseconds(100), out gatherStream);

                Assert.Fail("Expected Exception is missing");
            } catch (AggregateException ex) {
                Console.WriteLine($"Exception thrown: {ex.InnerException.Message}");
                Assert.AreEqual(1, ex.InnerExceptions.Count, $"Excessive number of Exceptions");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException), $"Unexpected Exception type {ex.InnerException.GetType().Name}");
            }
        }
    }
}
