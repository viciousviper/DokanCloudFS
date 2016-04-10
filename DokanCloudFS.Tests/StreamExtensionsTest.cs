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
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed class StreamExtensionsTests
    {
        private IList<Tuple<int, int, byte[], byte[]>> differences;

        [TestInitialize]
        public void Initialize()
        {
            differences = new List<Tuple<int, int, byte[], byte[]>>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            differences = null;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FindDifferences_WhereArraysAreEqual_ReturnsEmptyDifferences()
        {
            var array1 = Enumerable.Repeat((byte)0, 10).ToArray();
            var array2 = Enumerable.Repeat((byte)0, 10).ToArray();

            new MemoryStream(array1).FindDifferences(array2, differences);

            Assert.IsFalse(differences.Any(), "Unexpected difference");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FindDifferences_WhereArraysAreComplementary_ReturnsSingleDifferences()
        {
            var array1 = Enumerable.Repeat((byte)0, 10).ToArray();
            var array2 = Enumerable.Repeat((byte)1, 10).ToArray();

            new MemoryStream(array1).FindDifferences(array2, differences);

            Assert.AreEqual(1, differences.Count, "Unexpected number of differences");
            var difference = differences[0];
            Assert.AreEqual(0, difference.Item1, "Unexpected start index");
            Assert.AreEqual(array1.Length, difference.Item2, "Unexpected length");
            CollectionAssert.AreEqual(array1, difference.Item3, "Mismatched Stream content");
            CollectionAssert.AreEqual(array2, difference.Item4, "Mismatched content");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FindDifferences_WhereAlternatingArrayItemsDiffer_ReturnsCorrectDifferences()
        {
            var array1 = Enumerable.Repeat((byte)0, 10).ToArray();
            var array2 = Enumerable.Range(0, 10).Select(i => (byte)(i % 2)).ToArray();

            new MemoryStream(array1).FindDifferences(array2, differences);

            Assert.AreEqual(array1.Length / 2, differences.Count, "Unexpected number of differences");
            for (int i = 0; i < differences.Count; ++i) {
                var difference = differences[i];
                Assert.AreEqual(2 * i + 1, difference.Item1, $"Unexpected start index ({i})".ToString(CultureInfo.CurrentCulture));
                Assert.AreEqual(1, difference.Item2, $"Unexpected length ({i})".ToString(CultureInfo.CurrentCulture));
                CollectionAssert.AreEqual(new[] { (byte)0 }, difference.Item3, $"Mismatched Stream content ({i})".ToString(CultureInfo.CurrentCulture));
                CollectionAssert.AreEqual(new[] { (byte)1 }, difference.Item4, $"Mismatched content ({i})".ToString(CultureInfo.CurrentCulture));
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FindDifferences_WhereContiguousArrayItemsDiffer_ReturnsCorrectDifferences()
        {
            var array1 = Enumerable.Repeat((byte)0, 20).ToArray();
            var array2 = Enumerable.Range(0, 20).Select(i => (byte)((i / 4) % 2)).ToArray();

            new MemoryStream(array1).FindDifferences(array2, differences);

            Assert.AreEqual(2, differences.Count, "Unexpected number of differences");
            for (int i = 0; i < differences.Count; ++i) {
                var difference = differences[i];
                Assert.AreEqual(8 * i + 4 , difference.Item1, $"Unexpected start index ({i})".ToString(CultureInfo.CurrentCulture));
                Assert.AreEqual(4, difference.Item2, $"Unexpected length ({i})".ToString(CultureInfo.CurrentCulture));
                CollectionAssert.AreEqual(Enumerable.Repeat((byte)0, 4).ToArray(), difference.Item3, $"Mismatched Stream content ({i})".ToString(CultureInfo.CurrentCulture));
                CollectionAssert.AreEqual(Enumerable.Repeat((byte)1, 4).ToArray(), difference.Item4, $"Mismatched content ({i})".ToString(CultureInfo.CurrentCulture));
            }
        }
    }
}
