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
using System.IO;
using Moq;
using System.Threading;
using System.Threading.Tasks;

namespace IgorSoft.DokanCloudFS.Tests
{
    public partial class ReadWriteSegregatingStreamTest
    {
        public interface IStream
        {
            bool CanRead { get; }

            bool CanSeek { get; }

            bool CanTimeout { get; }

            bool CanWrite { get; }

            long Length { get; }

            long Position { get; set; }

            int ReadTimeout { get; set; }

            int WriteTimeout { get; set; }

            Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken);

            void Close();

            void Flush();

            Task FlushAsync(CancellationToken cancellationToken);

            long Seek(long offset, SeekOrigin origin);

            void SetLength(long value);

            int Read(byte[] buffer, int offset, int count);

            void Write(byte[] buffer, int offset, int count);
        }

        internal class Fixture
        {
            private class StreamFake : Stream
            {
                private IStream stream;

                public override bool CanRead => stream.CanRead;

                public override bool CanSeek => stream.CanSeek;

                public override bool CanWrite => stream.CanWrite;

                public override bool CanTimeout => stream.CanTimeout;

                public override long Length => stream.Length;

                public override long Position
                {
                    get { return stream.Position; }
                    set { stream.Position = value; }
                }

                public override int ReadTimeout
                {
                    get { return stream.ReadTimeout; }
                    set { stream.ReadTimeout = value; }
                }

                public override int WriteTimeout
                {
                    get { return stream.WriteTimeout; }
                    set { stream.WriteTimeout = value; }
                }

                public StreamFake(IStream stream)
                {
                    this.stream = stream;
                }

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
                {
                    return stream.CopyToAsync(destination, bufferSize, cancellationToken);
                }

                public override void Close()
                {
                    stream.Close();
                    base.Close();
                }

                public override void Flush()
                {
                    stream.Flush();
                }

                public override Task FlushAsync(CancellationToken cancellationToken)
                {
                    return stream.FlushAsync(cancellationToken);
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    return stream.Read(buffer, offset, count);
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    return stream.Seek(offset, origin);
                }

                public override void SetLength(long value)
                {
                    stream.SetLength(value);
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    stream.Write(buffer, offset, count);
                }
            }

            private MockRepository mockRepository = new MockRepository(MockBehavior.Strict);

            private Stream CreateStream(out Mock<IStream> streamMock)
            {
                streamMock = mockRepository.Create<IStream>();
                return new StreamFake(streamMock.Object);
            }

            public Stream CreateStream()
            {
                var streamMock = default(Mock<IStream>);
                return CreateStream(out streamMock);
            }

            public Stream CreateStream_ForCanRead(bool canRead = true)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .Setup(s => s.CanRead)
                    .Returns(canRead);

                return stream;
            }

            public Stream CreateStream_ForCanWrite(bool canWrite = true)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .Setup(s => s.CanWrite)
                    .Returns(canWrite);

                return stream;
            }

            public Stream CreateStream_ForCanTimeout(bool canTimeout = true)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .Setup(s => s.CanTimeout)
                    .Returns(canTimeout);

                return stream;
            }

            public Stream CreateStream_ForGetReadTimeout(int readTimeout)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .SetupGet(s => s.ReadTimeout)
                    .Returns(readTimeout);

                return stream;
            }

            public Stream CreateStream_ForSetReadTimeout(int readTimeout)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .SetupSet(s => s.ReadTimeout = readTimeout);

                return stream;
            }

            public Stream CreateStream_ForGetWriteTimeout(int writeTimeout)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .SetupGet(s => s.WriteTimeout)
                    .Returns(writeTimeout);

                return stream;
            }

            public Stream CreateStream_ForSetWriteTimeout(int writeTimeout)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .SetupSet(s => s.WriteTimeout = writeTimeout);

                return stream;
            }

            public Stream CreateStream_ForCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .Setup(s => s.CopyToAsync(destination, bufferSize, cancellationToken))
                    .Returns(Task.CompletedTask);

                return stream;
            }

            public Stream CreateStream_ForFlush()
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .Setup(s => s.Flush());

                return stream;
            }

            public Stream CreateStream_ForFlushAsync(CancellationToken cancellationToken)
            {
                var streamMock = default(Mock<IStream>);

                var stream = CreateStream(out streamMock);

                streamMock
                    .Setup(s => s.FlushAsync(cancellationToken))
                    .Returns(Task.CompletedTask);

                return stream;
            }

            public void Verify()
            {
                mockRepository.Verify();
            }
        }
    }
}
