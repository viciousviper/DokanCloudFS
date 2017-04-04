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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.DokanCloudFS.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class BlockMapTests
    {
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Create_WhereCapacityIsZero_Throws()
        {
            var sut = new BlockMap(0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetAvailableBytes_WhereOffsetIsNegative_Throws()
        {
            var sut = Fixture.CreateMap();

            sut.GetAvailableBytes(-1, 1);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetAvailableBytes_WhereOffsetIsGreaterThanCapacity_Throws()
        {
            var sut = Fixture.CreateMap();

            sut.GetAvailableBytes(sut.Capacity + 1, 1);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetAvailableBytes_WhereCountIsNegative_Throws()
        {
            var sut = Fixture.CreateMap();

            sut.GetAvailableBytes(0, -1);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereCountZero_ReturnsZero()
        {
            var sut = Fixture.CreateMap();

            Assert.AreEqual(0, sut.GetAvailableBytes(10, 0));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Assign_WhereBoundariesAreValid_AssignsBlockCorrectly()
        {
            var sut = Fixture.CreateMap();

            sut.AssignBytes(8, 4);

            CollectionAssert.AreEqual(new[] { new BlockMap.Block(8, 4) }, sut.Blocks);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Assign_WhereBoundariesAreInvalid_WithNegativeOffset_Throws()
        {
            var sut = Fixture.CreateMap();

            sut.AssignBytes(-1, 4);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Assign_WhereBoundariesAreInvalid_WithZeroCount_Throws()
        {
            var sut = Fixture.CreateMap();

            sut.AssignBytes(4, 0);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Assign_WhereBoundariesAreInvalid_WithExcessiveOffset_Throws()
        {
            var sut = Fixture.CreateMap();

            sut.AssignBytes(20, 1);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Assign_WhereBoundariesAreInvalid_WithExcessiveCount_Throws()
        {
            var sut = Fixture.CreateMap();

            sut.AssignBytes(19, 2);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Assign_WherePredecessorExists_AssignsBlockCorrectly()
        {
            var sut = Fixture.CreateMap().WithPredecessor(8, 4);

            sut.AssignBytes(8, 4);

            CollectionAssert.AreEqual(new[] { new BlockMap.Block(3, 4), new BlockMap.Block(8, 4) }, sut.Blocks);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Assign_WhereImmediatePredecessorExists_MergesBlocksCorrectly()
        {
            var sut = Fixture.CreateMap().WithImmediatePredecessor(8, 4);

            sut.AssignBytes(8, 4);

            CollectionAssert.AreEqual(new[] { new BlockMap.Block(4, 8) }, sut.Blocks);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Assign_WhereImmediateSuccessorExists_MergesBlocksCorrectly()
        {
            var sut = Fixture.CreateMap().WithImmediateSuccessor(8, 4);

            sut.AssignBytes(8, 4);

            CollectionAssert.AreEqual(new[] { new BlockMap.Block(8, 8) }, sut.Blocks);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Assign_WhereSuccessorExists_DoesNotMergeBlocks()
        {
            var sut = Fixture.CreateMap().WithSuccessor(8, 4);

            sut.AssignBytes(8, 4);

            CollectionAssert.AreEqual(new[] { new BlockMap.Block(8, 4), new BlockMap.Block(13, 4) }, sut.Blocks);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Assign_WhereImmediatePredecessorAndSuccessorExist_MergesBlocksCorrectly()
        {
            var sut = Fixture.CreateMap().WithImmediatePredecessor(8, 4).WithImmediateSuccessor(8, 4);

            sut.AssignBytes(8, 4);

            CollectionAssert.AreEqual(new[] { new BlockMap.Block(4, 12) }, sut.Blocks);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Assign_WherePredecessorAndSuccessorExist_DoesNotMergeBlocks()
        {
            var sut = Fixture.CreateMap().WithPredecessor(8, 4).WithSuccessor(8, 4);

            sut.AssignBytes(8, 4);

            CollectionAssert.AreEqual(new[] { new BlockMap.Block(3, 4), new BlockMap.Block(8, 4), new BlockMap.Block(13, 4) }, sut.Blocks);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Assign_WherePredecessorIntersects_Throws()
        {
            var sut = Fixture.CreateMap().WithIntersectingPredecessor(8, 4);

            sut.AssignBytes(8, 4);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Assign_WhereSucccessorIntersects_Throws()
        {
            var sut = Fixture.CreateMap().WithIntersectingSuccessor(8, 4);

            sut.AssignBytes(8, 4);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Assign_WherePredecessorAndSucccessorIntersects_Throws()
        {
            var sut = Fixture.CreateMap().WithIntersectingPredecessor(8, 4).WithIntersectingSuccessor(8, 4);

            sut.AssignBytes(8, 4);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesBlock_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4);

            Assert.AreEqual(4, sut.GetAvailableBytes(8, 4));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesMergedBlockAndImmediatePredecessor_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4).WithImmediatePredecessor(8, 4);

            Assert.AreEqual(6, sut.GetAvailableBytes(5, 6));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesBlockAndImmediatePredecessor_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4).WithImmediatePredecessor(8, 4);

            Assert.AreEqual(6, sut.GetAvailableBytes(5, 6));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesMergedBlockImmediatePredecessorAndSuccessor_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4).WithImmediatePredecessor(8, 4).WithImmediateSuccessor(8, 4);

            Assert.AreEqual(10, sut.GetAvailableBytes(5, 10));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesBlockImmediatePredecessorAndSuccessor_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4).WithImmediatePredecessor(8, 4).WithImmediateSuccessor(8, 4);

            Assert.AreEqual(10, sut.GetAvailableBytes(5, 10));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesBlockAndLeadingSpace_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4);

            Assert.AreEqual(0, sut.GetAvailableBytes(7, 5));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesBlockAndTrailingSpace_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4);

            Assert.AreEqual(4, sut.GetAvailableBytes(8, 5));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesBlockAndPredecessor_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4).WithPredecessor(8, 4);

            Assert.AreEqual(3, sut.GetAvailableBytes(4, 6));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetAvailableBytes_WhereTargetMatchesBlockPredecessorAndSuccessor_ReturnsCorrectResult()
        {
            var sut = Fixture.CreateMap().WithBlock(8, 4).WithPredecessor(8, 4).WithSuccessor(8, 4);

            Assert.AreEqual(3, sut.GetAvailableBytes(4, 12));
        }
    }
}
