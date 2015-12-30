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
using IgorSoft.DokanCloudFS.IO;

namespace IgorSoft.DokanCloudFS.Tests
{
    internal static class BlockMapExtensions
    {
        public static BlockMap WithBlock(this BlockMap map, int offset, int count)
        {
            map.AssignBytes(offset, count);
            return map;
        }

        public static BlockMap WithPredecessor(this BlockMap map, int offset, int count)
        {
            map.AssignBytes(offset - count - 1, count);
            return map;
        }

        public static BlockMap WithImmediatePredecessor(this BlockMap map, int offset, int count)
        {
            map.AssignBytes(offset - count, count);
            return map;
        }

        public static BlockMap WithIntersectingPredecessor(this BlockMap map, int offset, int count)
        {
            map.AssignBytes(offset - count + 1, count);
            return map;
        }

        public static BlockMap WithIntersectingSuccessor(this BlockMap map, int offset, int count)
        {
            map.AssignBytes(offset + count - 1, count);
            return map;
        }

        public static BlockMap WithImmediateSuccessor(this BlockMap map, int offset, int count)
        {
            map.AssignBytes(offset + count, count);
            return map;
        }

        public static BlockMap WithSuccessor(this BlockMap map, int offset, int count)
        {
            map.AssignBytes(offset + count + 1, count);
            return map;
        }
    }
}
