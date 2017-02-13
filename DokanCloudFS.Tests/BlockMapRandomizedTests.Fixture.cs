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
using System.Linq;
using FsCheck;
using IgorSoft.DokanCloudFS.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    public partial class BlockMapRandomizedTests
    {
        private static class Fixture
        {
            private static BlockMap.Block[] GetFreeBlocks(BlockMap blockMap)
            {
                var blocks = blockMap.Blocks;
                var freeBlocks = new List<BlockMap.Block>();

                if (blocks.Any()) {
                    if (blocks.First().Offset > 0)
                        freeBlocks.Add(new BlockMap.Block(0, blocks[0].Offset));
                    for (int i = 0; i < blocks.Count - 1; ++i) {
                        var freeBlockOffset = blocks[i].Offset + blocks[i].Count;
                        freeBlocks.Add(new BlockMap.Block(freeBlockOffset, blocks[i + 1].Offset - freeBlockOffset));
                    }
                    var lastByteIndex = blocks.Last().Offset + blocks.Last().Count;
                    if (lastByteIndex < blockMap.Capacity)
                        freeBlocks.Add(new BlockMap.Block(lastByteIndex, blockMap.Capacity - lastByteIndex));
                } else {
                    freeBlocks.Add(new BlockMap.Block(0, blockMap.Capacity));
                }

                return freeBlocks.ToArray();
            }

            private static BlockMap ToBlockMap(bool[] model)
            {
                var result = new BlockMap(model.Length);

                var currentOffset = -1;
                var currentLength = 0;
                for (int i = 0; i < model.Length; ++i) {
                    if (model[i]) {
                        if (currentOffset < 0)
                            currentOffset = i;
                        ++currentLength;
                    } else {
                        if (currentOffset >= 0) {
                            result.AssignBytes(currentOffset, currentLength);
                            currentOffset = -1;
                            currentLength = 0;
                        }
                    }
                }
                if (currentOffset >= 0)
                    result.AssignBytes(currentOffset, currentLength);

                return result;
            }

            internal static Gen<BlockMap> BlockMapsGen() => Gen.Sized(s => Gen.Map<bool[], BlockMap>((Converter<bool[], BlockMap>)ToBlockMap, Gen.Filter((Converter<bool[], bool>)(a => a.Any(b => !b)), Gen.ArrayOf(s, Gen.Elements(true, false)))));

            internal static Arbitrary<BlockMap> BlockMaps() => Arb.From(BlockMapsGen());

            internal static Gen<BlockMap.Block> FreeBlocksGen(BlockMap bm) =>
                from block in Gen.Elements(GetFreeBlocks(bm))
                from offset in Gen.Choose(block.Offset, block.Offset + block.Count - 1)
                from count in Gen.Choose(1, block.Count - (offset - block.Offset))
                select new BlockMap.Block(offset, count);

            internal static Arbitrary<BlockMap.Block> FreeBlocks(BlockMap bm) => Arb.From(FreeBlocksGen(bm));
        }
    }
}
