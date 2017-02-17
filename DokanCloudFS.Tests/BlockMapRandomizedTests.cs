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
using FsCheck;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.DokanCloudFS.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public partial class BlockMapRandomizedTests
    {
        [TestMethod, TestCategory(nameof(TestCategories.Manual)), System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void GenerateBlockMaps()
        {
            foreach (var size in new[] { 10, 20, 50, 100 }) {
                Console.WriteLine($"Size {size}:");
                foreach (var blockMap in Fixture.BlockMapsGen().Sample(size, 10))
                    Console.WriteLine($"\t{blockMap}");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Manual)), System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void GenerateFreeBlocks()
        {
            foreach (var size in new[] { 10, 20, 50, 100 }) {
                Console.WriteLine($"Size {size}:");
                foreach (var block in Fixture.FreeBlocksGen(new BlockMap(size)).Sample(size, 10))
                    Console.WriteLine($"\t{block}");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void AssignBytes_ToFreeArea_ProducesAvailableBytes()
        {
            Prop.ForAll(Fixture.BlockMaps(), bm => Prop.ForAll(Fixture.FreeBlocks(bm), b => {
                bm.AssignBytes(b.Offset, b.Count);
                return bm.GetAvailableBytes(b.Offset, b.Count) >= b.Count;
            })).Check(new Configuration() { Runner = Config.QuickThrowOnFailure.Runner, MaxNbOfTest = 10000 });
        }
    }
}
