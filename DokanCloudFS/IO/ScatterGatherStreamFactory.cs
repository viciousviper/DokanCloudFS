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

namespace IgorSoft.DokanCloudFS.IO
{
    public static class ScatterGatherStreamFactory
    {
        private static readonly TimeSpan defaultTimeout = TimeSpan.FromMilliseconds(-1);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public static void CreateScatterGatherStreams(int capacity, out Stream scatterStream, out Stream gatherStream)
        {
            CreateScatterGatherStreams(capacity, defaultTimeout, out scatterStream, out gatherStream);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#")]
        public static void CreateScatterGatherStreams(int capacity, TimeSpan timeout, out Stream scatterStream, out Stream gatherStream)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), $"{nameof(capacity)} must be positive.".ToString(CultureInfo.CurrentCulture));
            if (timeout < defaultTimeout)
                throw new ArgumentOutOfRangeException(nameof(timeout), $"{nameof(timeout)} must be greater than {defaultTimeout:c}.".ToString(CultureInfo.CurrentCulture));

            var buffer = new byte[capacity];
            var assignedBlocks = new BlockMap(capacity);
            scatterStream = new ScatterStream(buffer, assignedBlocks, timeout);
            gatherStream = new GatherStream(buffer, assignedBlocks, timeout);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static void CreateScatterGatherStreams(int capacity, out Stream scatterStream, Stream[] gatherStreams)
        {
            CreateScatterGatherStreams(capacity, defaultTimeout, out scatterStream, gatherStreams);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public static void CreateScatterGatherStreams(int capacity, TimeSpan timeout, out Stream scatterStream, Stream[] gatherStreams)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), $"{nameof(capacity)} must be positive.".ToString(CultureInfo.CurrentCulture));
            if (timeout < defaultTimeout)
                throw new ArgumentOutOfRangeException(nameof(timeout), $"{nameof(timeout)} must be greater than {defaultTimeout:c}.".ToString(CultureInfo.CurrentCulture));
            if (gatherStreams == null)
                throw new ArgumentNullException(nameof(gatherStreams));

            var buffer = new byte[capacity];
            var assignedBlocks = new BlockMap(capacity);
            scatterStream = new ScatterStream(buffer, assignedBlocks, timeout);
            foreach (var i in Enumerable.Range(0, gatherStreams.Length))
                gatherStreams[i] = new GatherStream(buffer, assignedBlocks, timeout);
        }
    }
}
