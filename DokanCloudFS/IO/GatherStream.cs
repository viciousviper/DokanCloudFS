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
using System.Threading;

namespace IgorSoft.DokanCloudFS.IO
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class GatherStream : MemoryStream
    {
        private readonly BlockMap assignedBlocks;

        private readonly TimeSpan timeout;

        public override bool CanWrite => false;

        public override int Capacity
        {
            get {
                lock (assignedBlocks) {
                    return assignedBlocks.Capacity;
                }
            }
            set {
                throw new NotSupportedException();
            }
        }

        public override long Length
        {
            get {
                lock (assignedBlocks) {
                    return assignedBlocks.Capacity;
                }
            }
        }

        public override long Position
        {
            get {
                lock (assignedBlocks) {
                    return base.Position;
                }
            }
            set {
                lock (assignedBlocks) {
                    base.Position = value;
                    Monitor.Pulse(assignedBlocks);
                }
            }
        }

        internal GatherStream(byte[] buffer, BlockMap assignedBlocks, TimeSpan timeout) : base(buffer, false)
        {
            if (assignedBlocks == null)
                throw new ArgumentNullException(nameof(assignedBlocks));
            if (buffer.Length != assignedBlocks.Capacity)
                throw new ArgumentException($"{nameof(assignedBlocks)} capacity does not match {nameof(buffer)} length".ToString(CultureInfo.CurrentCulture));

            this.assignedBlocks = assignedBlocks;
            this.timeout = timeout;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (assignedBlocks) {
                do {
                    var bytes = assignedBlocks.GetAvailableBytes((int)base.Position, count);
                    var read = base.Read(buffer, offset, bytes);
                    if (read > 0 || base.Position == Capacity)
                        return read;
                    if (!Monitor.Wait(assignedBlocks, timeout))
                        throw new TimeoutException($"{nameof(Read)} exceeded timeout {timeout}".ToString(CultureInfo.CurrentCulture));
                } while (true);
            }
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            lock (assignedBlocks) {
                var position = base.Seek(offset, loc);
                Monitor.Pulse(assignedBlocks);
                return position;
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(GatherStream)}[{Capacity}] {nameof(Length)} = {base.Length}, {nameof(Position)} = {base.Position}".ToString(CultureInfo.CurrentCulture);
    }
}
