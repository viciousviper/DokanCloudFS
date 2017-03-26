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
using System.Threading.Tasks;

namespace IgorSoft.DokanCloudFS.IO
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ReadWriteSegregatingStream : Stream
    {
        private Stream readStream;

        private Stream writeStream;

        public Stream ReadStream => readStream;

        public Stream WriteStream => writeStream;

        public override bool CanRead => readStream.CanRead;

        public override bool CanSeek => writeStream.CanSeek && readStream.CanSeek;

        public override bool CanTimeout => writeStream.CanTimeout && readStream.CanTimeout;

        public override bool CanWrite => writeStream.CanWrite;

        public override long Length => writeStream.Length;

        public override long Position
        {
            get { return writeStream.Position; }
            set { readStream.Position = writeStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return readStream.ReadTimeout; }
            set { readStream.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return writeStream.WriteTimeout; }
            set { writeStream.WriteTimeout = value; }
        }

        public ReadWriteSegregatingStream(Stream writeStream, Stream readStream)
        {
            if (writeStream == null)
                throw new ArgumentNullException(nameof(writeStream));
            if (readStream == null)
                throw new ArgumentNullException(nameof(readStream));

            this.writeStream = writeStream;
            this.readStream = readStream;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return readStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return writeStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            writeStream.Close();
            readStream.Close();

            base.Close();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return readStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            writeStream.Dispose();
            readStream.Dispose();

            base.Dispose(disposing);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return readStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            writeStream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            writeStream.Flush();
            readStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(writeStream.FlushAsync(cancellationToken), readStream.FlushAsync(cancellationToken));
        }

        public override object InitializeLifetimeService()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return readStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return readStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return readStream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var writePosition = writeStream.Seek(offset, origin);
            var readPosition = readStream.Seek(offset, origin);

            if (writePosition != readPosition)
                throw new InvalidOperationException();

            return writePosition;
        }

        public override void SetLength(long value)
        {
            writeStream.SetLength(value);
            readStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            writeStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return writeStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            writeStream.WriteByte(value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(ReadWriteSegregatingStream)} Read={readStream.GetType().Name} Write={writeStream.GetType().Name} Position=[{writeStream.Position}/{readStream.Position}]".ToString(CultureInfo.CurrentCulture);
    }
}
