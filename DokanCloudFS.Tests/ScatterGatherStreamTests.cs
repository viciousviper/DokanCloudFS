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
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.DokanCloudFS.IO;

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
        [ExpectedException(typeof(ArgumentNullException))]
        public void GatherStream_Create_WhereBufferIsNull_Throws()
        {
            fixture.CreateGatherStream(null, new BlockMap(1));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GatherStream_Create_WhereBlockMapIsNull_Throws()
        {
            fixture.CreateGatherStream(Array.Empty<byte>(), null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentException))]
        public void GatherStream_Create_WhereBlockMapCapacityDiffersFromBufferSize_Throws()
        {
            fixture.CreateGatherStream(fixture.InitializeBuffer(20), new BlockMap(10));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GatherStream_CanWrite_ReturnsFalse()
        {
            var sut = fixture.CreateGatherStream(1);

            Assert.IsFalse(sut.CanWrite);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GatherStream_SeekFromBegin_ReturnsCorrectResult()
        {
            const int size = 100;
            var sut = fixture.CreateGatherStream(size);

            var result = sut.Seek(size / 4, SeekOrigin.Begin);

            Assert.AreEqual(size / 4, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GatherStream_SeekFromEnd_ReturnsCorrectResult()
        {
            const int size = 100;
            var sut = fixture.CreateGatherStream(size);

            var result = sut.Seek(-size / 4, SeekOrigin.End);

            Assert.AreEqual(size * 3 / 4, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void GatherStream_SetCapacity_Throws()
        {
            const int size = 100;
            var sut = fixture.CreateGatherStream(size);

            sut.Capacity = size * 3 / 4;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void GatherStream_SetLength_Throws()
        {
            const int size = 100;
            var sut = fixture.CreateGatherStream(size);

            sut.SetLength(size * 3 / 4);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void GatherStream_Write_Throws()
        {
            var sut = fixture.CreateGatherStream(1);

            sut.Write(Array.Empty<byte>(), 0, 0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ScatterStream_Create_WhereBufferIsNull_Throws()
        {
            fixture.CreateScatterStream(null, new BlockMap(1));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentException))]
        public void ScatterStream_Create_WhereBlockMapCapacityDiffersFromBufferSize_Throws()
        {
            fixture.CreateScatterStream(Enumerable.Repeat<byte>(0, 20).ToArray(), new BlockMap(10));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ScatterStream_Create_WhereBlockMapIsNull_Throws()
        {
            fixture.CreateScatterStream(Array.Empty<byte>(), null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ScatterStream_CanRead_ReturnsFalse()
        {
            var sut = fixture.CreateScatterStream(1);

            Assert.IsFalse(sut.CanRead);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void ScatterStream_Read_Throws()
        {
            var sut = fixture.CreateScatterStream(1);

            sut.Read(Array.Empty<byte>(), 0, 0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ScatterStream_SeekFromBegin_ReturnsCorrectResult()
        {
            const int size = 100;
            var sut = fixture.CreateScatterStream(size);

            var result = sut.Seek(size / 4, SeekOrigin.Begin);

            Assert.AreEqual(size / 4, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ScatterStream_SeekFromEnd_ReturnsCorrectResult()
        {
            const int size = 100;
            var sut = fixture.CreateScatterStream(size);

            var result = sut.Seek(-size / 4, SeekOrigin.End);

            Assert.AreEqual(size * 3 / 4, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ScatterStream_SetLength_Succeeds()
        {
            const int size = 100;
            var sut = fixture.CreateScatterStream(size);

            sut.SetLength(size * 3 / 4);

            Assert.AreEqual(size * 3 / 4, sut.Length);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ScatterGatherStreamFactory_CreateScatterGatherStream_WhereCapacityIsNegative_Throws()
        {
            var scatterStream = default(Stream);
            var gatherStream = default(Stream);

            ScatterGatherStreamFactory.CreateScatterGatherStreams(-1, out scatterStream, out gatherStream);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ScatterGatherStreamFactory_CreateScatterGatherStream_WhereTimeoutIsNegative_Throws()
        {
            var scatterStream = default(Stream);
            var gatherStream = default(Stream);

            ScatterGatherStreamFactory.CreateScatterGatherStreams(100, TimeSpan.FromMilliseconds(-2), out scatterStream, out gatherStream);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ScatterGatherStreamFactory_CreateScatterGatherStreams_WhereCapacityIsNegative_Throws()
        {
            var scatterStream = default(Stream);
            var gatherStreams = new Stream[5];

            ScatterGatherStreamFactory.CreateScatterGatherStreams(-1, out scatterStream, gatherStreams);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ScatterGatherStreamFactory_CreateScatterGatherStreams_WhereTimeoutIsNegative_Throws()
        {
            var scatterStream = default(Stream);
            var gatherStreams = new Stream[5];

            ScatterGatherStreamFactory.CreateScatterGatherStreams(100, TimeSpan.FromMilliseconds(-2), out scatterStream, gatherStreams);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ScatterGatherStreamFactory_CreateScatterGatherStreams_WhereGatherStreamsIsNumm_Throws()
        {
            var scatterStream = default(Stream);
            var gatherStreams = default(Stream[]);

            ScatterGatherStreamFactory.CreateScatterGatherStreams(100, out scatterStream, gatherStreams);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ScatterStream_SetCapacity_ToLowerValue_Succeeds()
        {
            var size = 1000;

            var scatterStream = fixture.CreateScatterStream(size);

            scatterStream.Capacity -= 1;

            Assert.AreEqual(size - 1, scatterStream.Capacity);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ScatterStream_SetCapacity_ToHigherValue_Succeeds()
        {
            var size = 1000;

            var scatterStream = fixture.CreateScatterStream(size);

            scatterStream.Capacity += 1;

            Assert.AreEqual(size + 1, scatterStream.Capacity);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ScatterStream_SetCapacity_SetsMatchingCapacityOnGatherStream()
        {
            var size = 1000;

            var scatterStream = default(Stream);
            var gatherStream = default(Stream);
            ScatterGatherStreamFactory.CreateScatterGatherStreams(size, out scatterStream, out gatherStream);

            var changedSize = size / 4;
            ((ScatterStream)scatterStream).Capacity = changedSize;

            Assert.AreEqual(changedSize, ((GatherStream)gatherStream).Capacity);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ScatterStream_SetCapacity_SetsMatchingCapacityOnAllGatherStreams()
        {
            var size = 1000;

            var scatterStream = default(Stream);
            var gatherStreams = new Stream[5];
            ScatterGatherStreamFactory.CreateScatterGatherStreams(size, out scatterStream, gatherStreams);

            var changedSize = size / 4;
            ((ScatterStream)scatterStream).Capacity = changedSize;

            Assert.IsTrue(gatherStreams.Cast<GatherStream>().All(s => s.Capacity == changedSize));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ScatterStream_SetCapacity_WhereBlockMapWouldBeTruncated_Throws()
        {
            var size = 256;
            var buffer = fixture.InitializeBuffer(size);
            var assignedBlocks = new BlockMap(size);
            assignedBlocks.AssignBytes(0, size / 2);

            var scatterStream = fixture.CreateScatterStream(buffer, assignedBlocks);
            var gatherStream = fixture.CreateGatherStream(buffer, assignedBlocks);

            var changedSize = size / 2 - 1;
            ((ScatterStream)scatterStream).Capacity = changedSize;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBufferConcurrently_WhereReaderIsFlooded_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrently(source, TimeSpan.Zero, 100, TimeSpan.Zero, TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(10));

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBufferConcurrently_WhereReaderIsSynced_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrently(source, TimeSpan.Zero, 50, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(5));

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBufferConcurrently_WhereReaderIsStarved_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrently(source, TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(10), TimeSpan.Zero, 100, TimeSpan.Zero);

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBufferConcurrently_WhereLimitIsSpecified_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrently(source, TimeSpan.Zero, 50, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(5), false);

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBufferConcurrently_WithExplicitPermutationRetrograde_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrentlyByPermutation(source, TimeSpan.FromMilliseconds(10), 200, TimeSpan.FromMilliseconds(10), new int[] { 4, 3, 2, 1, 0 }, TimeSpan.Zero, 200, TimeSpan.Zero, new int[] { 0, 1, 2, 3, 4 });

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBufferConcurrently_WithExplicitPermutationInterleaved_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var target = fixture.CopyBufferConcurrentlyByPermutation(source, TimeSpan.FromMilliseconds(10), 200, TimeSpan.FromMilliseconds(10), new int[] { 4, 2, 0, 3, 1 }, TimeSpan.Zero, 200, TimeSpan.Zero, new int[] { 0, 2, 4, 1, 3 });

            CollectionAssert.AreEqual(source, target, "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBuffersConcurrently_WhereReaderIsFlooded_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var targets = fixture.CopyBuffersConcurrently(source, 5, TimeSpan.Zero, 100, TimeSpan.Zero, TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(10));

            Assert.IsTrue(targets.All(t => t.SequenceEqual(source)), "Unexpected result");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyBuffersConcurrently_WhereReaderIsStarved_ReturnsCorrectResult()
        {
            var source = fixture.InitializeBuffer(1000);
            var targets = fixture.CopyBuffersConcurrently(source, 5, TimeSpan.FromMilliseconds(10), 50, TimeSpan.FromMilliseconds(10), TimeSpan.Zero, 100, TimeSpan.Zero);

            Assert.IsTrue(targets.All(t => t.SequenceEqual(source)), "Unexpected result");
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
                Console.WriteLine($"Exception thrown: {ex.InnerException.Message}".ToString(CultureInfo.CurrentCulture));
                Assert.AreEqual(1, ex.InnerExceptions.Count, "Excessive number of Exceptions");
                Assert.IsInstanceOfType(ex.InnerException, typeof(TimeoutException), $"Unexpected Exception type {ex.InnerException.GetType().Name}".ToString(CultureInfo.CurrentCulture));
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
                Console.WriteLine($"Exception thrown: {ex.InnerException.Message}".ToString(CultureInfo.CurrentCulture));
                Assert.AreEqual(1, ex.InnerExceptions.Count, "Excessive number of Exceptions");
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentOutOfRangeException), $"Unexpected Exception type {ex.InnerException.GetType().Name}".ToString(CultureInfo.CurrentCulture));
            }
        }
    }
}
