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
using System.Linq;
using FsCheck;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public partial class ScatterGatherStreamRandomizedTests
    {
        [TestMethod, TestCategory(nameof(TestCategories.Manual)), System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void SamplePartitions()
        {
            foreach (var size in new[] { 10, 20, 50, 100 }) {
                Console.WriteLine($"Size {size}:");
                foreach (var partition in Fixture.PartitionsGen().Sample(size, 10)) {
                    Assert.AreEqual(size, partition.Sum(p => p.Count));
                    Console.WriteLine($"\t{string.Join(", ", partition.Select(p => p.ToString()))}");
                }
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void LinearReadFromRandomlyScatteredWrite_ReturnsContent()
        {
            Prop.ForAll(Fixture.Partitions(), partition =>
            {
                var size = partition.Max(p => p.Offset + p.Count);
                var buffer = Enumerable.Range(0, size).Select(i => (byte)(i % 251)).ToArray();

                return Fixture.CopyBufferConcurrentlyByPermutation(buffer, partition, null).SequenceEqual(buffer);
            }).Check(Fixture.QuickConfig);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void RandomlyGatheredReadFromLinearWrite_ReturnsContent()
        {
            Prop.ForAll(Fixture.Partitions(), partition =>
            {
                var size = partition.Max(p => p.Offset + p.Count);
                var buffer = Enumerable.Range(0, size).Select(i => (byte)(i % 251)).ToArray();

                return Fixture.CopyBufferConcurrentlyByPermutation(buffer, null, partition).SequenceEqual(buffer);
            }).Check(Fixture.QuickConfig);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void RandomlyGatheredReadFromRandomlyScatteredWrite_ReturnsContent()
        {
            Prop.ForAll(Fixture.Partitions(), Fixture.Partitions(), (writePartition, readPartition) =>
            {
                var writeSize = writePartition.Max(p => p.Offset + p.Count);
                var readSize = readPartition.Max(p => p.Offset + p.Count);

                Assert.IsTrue(writeSize == readSize, $"Partition size mismatch: write={writeSize}, read={readSize}");

                var buffer = Enumerable.Range(0, writeSize).Select(i => (byte)(i % 251)).ToArray();

                return Fixture.CopyBufferConcurrentlyByPermutation(buffer, writePartition, readPartition).SequenceEqual(buffer);
            }).Check(Fixture.QuickConfig);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void RandomlyGatheredReadsFromRandomlyScatteredWrite_ReturnContent()
        {
            Prop.ForAll(Fixture.Partitions(), Fixture.PartitionsList(5), (writePartition, readPartitions) =>
            {
                var writeSize = writePartition.Max(p => p.Offset + p.Count);
                var readSizes = readPartitions.Select(r => r.Max(p => p.Offset + p.Count));

                Assert.IsTrue(readSizes.All(r => writeSize == r), $"Partition size mismatch: write={writeSize}, read=[{string.Join(",", readSizes)}]");

                var buffer = Enumerable.Range(0, writeSize).Select(i => (byte)(i % 251)).ToArray();

                return Fixture.CopyBuffersConcurrentlyByPermutation(buffer, writePartition, readPartitions.ToArray()).All(b => b.SequenceEqual(buffer));
            }).Check(Fixture.QuickConfig);
        }
    }
}
