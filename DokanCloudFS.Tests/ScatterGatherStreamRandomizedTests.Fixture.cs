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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;
using IgorSoft.DokanCloudFS.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    public partial class ScatterGatherStreamRandomizedTests
    {
        private static class Fixture
        {
            private static readonly TimeSpan initialWriteDelay = TimeSpan.FromMilliseconds(20);
            private static readonly TimeSpan writeDelay = TimeSpan.FromMilliseconds(10);
            private static readonly TimeSpan initialReadDelay = TimeSpan.Zero;
            private static readonly TimeSpan readDelay = TimeSpan.FromMilliseconds(2);

            public static Configuration QuickConfig => new Configuration() { Runner = Config.QuickThrowOnFailure.Runner, StartSize = 3 };

            public static Configuration VerboseConfig => new Configuration() { Runner = Config.VerboseThrowOnFailure.Runner, StartSize = 3 };

            public static byte[] CopyBufferConcurrentlyByPermutation(byte[] sourceBuffer, BlockMap.Block[] writePermutation, BlockMap.Block[] readPermutation)
            {
                var targetBuffer = new byte[sourceBuffer.Length];

                var scatterStream = default(Stream);
                var gatherStream = default(Stream);
                ScatterGatherStreamFactory.CreateScatterGatherStreams(sourceBuffer.Length, out scatterStream, out gatherStream);

                var writeTask = WriteAsync(scatterStream, writePermutation, sourceBuffer);
                var readTask = ReadAsync(gatherStream, readPermutation, targetBuffer);

                Task.WaitAll(new Task[] { writeTask, readTask });

                return targetBuffer;
            }

            public static byte[][] CopyBuffersConcurrentlyByPermutation(byte[] sourceBuffer, BlockMap.Block[] writePermutation, BlockMap.Block[][] readPermutations)
            {
                var targetBuffers = Enumerable.Range(0, readPermutations.Length).Select(i => new byte[sourceBuffer.Length]).ToArray();

                var scatterStream = default(Stream);
                var gatherStreams = new Stream[readPermutations.Length];
                ScatterGatherStreamFactory.CreateScatterGatherStreams(sourceBuffer.Length, out scatterStream, gatherStreams);

                var writeTask = WriteAsync(scatterStream, writePermutation, sourceBuffer);
                var readTasks = Enumerable.Range(0, readPermutations.Length).Select(i => ReadAsync(gatherStreams[i], readPermutations[i], targetBuffers[i]));

                Task.WaitAll(new Task[] { writeTask }.Concat(readTasks).ToArray());

                return targetBuffers;
            }

            public static async Task<bool> ReadAsync(Stream stream, BlockMap.Block[] readPermutation, byte[] buffer)
            {
                await Task.Delay(initialReadDelay);

                if (readPermutation == null) {
                    stream.Position = 0;
                    while (stream.Position < buffer.Length) {
                        var position = (int)stream.Position;
                        await stream.ReadAsync(buffer, position, buffer.Length - position);
                        await Task.Delay(readDelay);
                    }
                } else {
                    var completed = 0;
                    for (var i = 0; completed < buffer.Length; ++i) {
                        var bytesRead = 0;
                        stream.Position = readPermutation[i].Offset;
                        do {
                            bytesRead += await stream.ReadAsync(buffer, readPermutation[i].Offset + bytesRead, readPermutation[i].Count - bytesRead);
                            await Task.Delay(readDelay);
                        } while (bytesRead < readPermutation[i].Count);
                        completed += bytesRead;
                    }
                }

                return (int)stream.Position == buffer.Length;
            }

            public static async Task<bool> WriteAsync(Stream stream, BlockMap.Block[] writePermutation, byte[] buffer)
            {
                await Task.Delay(initialWriteDelay);

                if (writePermutation == null) {
                    stream.Position = 0;
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                    await Task.Delay(writeDelay);
                } else {
                    foreach (var block in writePermutation) {
                        stream.Position = block.Offset;
                        await stream.WriteAsync(buffer, block.Offset, block.Count);
                        await Task.Delay(writeDelay);
                    }
                }

                await stream.FlushAsync();

                return true;
            }

            private static BlockMap.Block[] ToPartition(int size, int[] offsets)
            {
                var blocks = new BlockMap.Block[offsets.Length + 1];
                blocks[0] = new BlockMap.Block(0, offsets[0]);
                for (int i = 0; i < offsets.Length - 1; ++i)
                    blocks[i + 1] = new BlockMap.Block(offsets[i], offsets[i + 1] - offsets[i]);
                blocks[offsets.Length] = new BlockMap.Block(offsets[offsets.Length - 1], size - offsets[offsets.Length - 1]);
                return blocks.ToArray();
            }

            private static T[] ApplyPermutation<T>(T[] partition, int[] permutation) => permutation.Select(p => partition[p]).ToArray();

            public static Converter<IList<int>, IList<int>> Order => l => l.OrderBy(i => i).ToList();

            private static Converter<IEnumerable<int>, bool> ByAcceptableSize(int size) => l => l.Any() && l.Count() <= Convert.ToInt32(Math.Sqrt(size) + 1);

            public static Gen<BlockMap.Block[]> PartitionsGen() => Gen.Sized(size =>
                from offsets in Gen.Map<IList<int>, IList<int>>(Order, Gen.Filter(ByAcceptableSize(size), Gen.SubListOf(Enumerable.Range(1, size - 1))))
                from permutation in Gen.Shuffle(Enumerable.Range(0, offsets.Count + 1))
                select ApplyPermutation(ToPartition(size, offsets.ToArray()), permutation));

            public static Arbitrary<BlockMap.Block[]> Partitions() => Arb.From(PartitionsGen());

            public static Arbitrary<IList<BlockMap.Block[]>> PartitionsList(int count) => Arb.From(PartitionsGen().ListOf(count));
        }
    }
}
