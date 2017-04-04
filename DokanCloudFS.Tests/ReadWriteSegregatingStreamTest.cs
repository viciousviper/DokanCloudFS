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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.DokanCloudFS.IO;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class ReadWriteSegregatingStreamTest
    {
        private Fixture fixture;

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateNew_WhereWriteStreamIsNull_Throws()
        {
            var sut = new ReadWriteSegregatingStream(null, fixture.CreateStream());
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateNew_WhereReadStreamIsNull_Throws()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ReadStreamGetter_ReturnsReadStream()
        {
            var stream = default(Stream);

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), stream = fixture.CreateStream());

            Assert.AreSame(stream, sut.ReadStream);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void WriteStreamGetter_ReturnsWriteStream()
        {
            var stream = default(Stream);

            var sut = new ReadWriteSegregatingStream(stream = fixture.CreateStream(), fixture.CreateStream());

            Assert.AreSame(stream, sut.WriteStream);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CanRead_IsDelegatedToReadStream_ForCanReadAsTrue()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForCanRead());

            Assert.IsTrue(sut.CanRead);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CanRead_IsDelegatedToReadStream_ForCanReadAsFalse()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForCanRead(false));

            Assert.IsFalse(sut.CanRead);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CanTimeout_IsDelegatedToWriteStream_ForCanTimeoutAsFalseFalse()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForCanTimeout(false), fixture.CreateStream());

            Assert.IsFalse(sut.CanTimeout);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CanTimeout_IsDelegatedToWriteStream_ForCanTimeoutAsFalseTrue()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForCanTimeout(false), fixture.CreateStream());

            Assert.IsFalse(sut.CanTimeout);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CanTimeout_IsDelegatedToWriteAndReadStream_ForCanTimeoutAsTrueFalse()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForCanTimeout(), fixture.CreateStream_ForCanTimeout(false));

            Assert.IsFalse(sut.CanTimeout);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CanTimeout_IsDelegatedToWriteAndReadStream_ForCanTimeoutAsTrueTrue()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForCanTimeout(), fixture.CreateStream_ForCanTimeout());

            Assert.IsTrue(sut.CanTimeout);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CanWrite_IsDelegatedToWriteStream_ForCanWriteAsTrue()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForCanWrite(), fixture.CreateStream());

            Assert.IsTrue(sut.CanWrite);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CanWrite_IsDelegatedToWriteStream_ForCanWriteAsFalse()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForCanWrite(false), fixture.CreateStream());

            Assert.IsFalse(sut.CanWrite);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetReadTimeout_IsDelegatedToReadStream()
        {
            var timeout = 42;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForGetReadTimeout(timeout));

            Assert.AreEqual(timeout, sut.ReadTimeout);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SetReadTimeout_IsDelegatedToReadStream()
        {
            var timeout = 42;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForSetReadTimeout(timeout));

            sut.ReadTimeout = timeout;

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void GetWriteTimeout_IsDelegatedToWriteStream()
        {
            var timeout = 42;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForGetWriteTimeout(timeout), fixture.CreateStream());

            Assert.AreEqual(timeout, sut.WriteTimeout);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SetWriteTimeout_IsDelegatedToWriteStream()
        {
            var timeout = 42;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForSetWriteTimeout(timeout), fixture.CreateStream());

            sut.WriteTimeout = timeout;

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void BeginRead_IsDelegatedToReadStream()
        {
            var buffer = new byte[100];
            AsyncCallback callback = ar => { };
            var state = new object();

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForBeginRead(buffer, callback, state));

            var asyncResult = sut.BeginRead(buffer, 0, buffer.Length, callback, state);

            Assert.IsInstanceOfType(asyncResult, typeof(Task<int>));
            Assert.AreEqual(buffer.Length, ((Task<int>)asyncResult).Result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void BeginWrite_IsDelegatedToWriteStream()
        {
            var buffer = new byte[100];
            AsyncCallback callback = ar => { };
            var state = new object();

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForBeginWrite(buffer, callback, state), fixture.CreateStream());

            var asyncResult = sut.BeginWrite(buffer, 0, buffer.Length, callback, state);

            Assert.IsInstanceOfType(asyncResult, typeof(Task<int>));
            Assert.AreEqual(buffer.Length, ((Task<int>)asyncResult).Result);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CopyToAsync_IsDelegatedToReadStream()
        {
            using (var cts = new CancellationTokenSource()) {
                var destination = Stream.Null;
                var bufferSize = 4711;
                var cancellationToken = cts.Token;

                var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForCopyToAsync(destination, bufferSize, cancellationToken));

                var task = sut.CopyToAsync(destination, bufferSize, cancellationToken);

                Assert.IsInstanceOfType(task, typeof(Task));
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void EndRead_IsDelegatedToReadStream()
        {
            var bufferSize = 4711;
            var asyncResult = Task.FromResult(bufferSize);

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForEndRead(asyncResult));

            Assert.AreEqual(bufferSize, sut.EndRead(asyncResult));

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void EndWrite_IsDelegatedToWriteStream()
        {
            var bufferSize = 4711;
            var asyncResult = Task.FromResult(bufferSize);

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForEndWrite(asyncResult), fixture.CreateStream());

            sut.EndWrite(asyncResult);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Flush_IsDelegatedToWriteStreamAndReadStream()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForFlush(), fixture.CreateStream_ForFlush());

            sut.Flush();

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FlushAsync_IsDelegatedToWriteStreamAndReadStream()
        {
            using (var cts = new CancellationTokenSource()) {
                var cancellationToken = cts.Token;

                var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForFlushAsync(cancellationToken), fixture.CreateStream_ForFlushAsync(cancellationToken));

                var task = sut.FlushAsync(cancellationToken);

                Assert.IsInstanceOfType(task, typeof(Task));
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(NotSupportedException))]
        public void InitializeLifetimeService_Throws()
        {
            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream());

            sut.InitializeLifetimeService();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Read_IsDelegatedToReadStream()
        {
            var buffer = new byte[100];

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForRead(buffer, 0, buffer.Length));

            Assert.AreEqual(buffer.Length, sut.Read(buffer, 0, buffer.Length));

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ReadAsync_IsDelegatedToReadStream()
        {
            var buffer = new byte[100];

            using (var cts = new CancellationTokenSource()) {
                var cancellationToken = cts.Token;

                var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForReadAsync(buffer, 0, buffer.Length, cancellationToken));

                var task = sut.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                Assert.IsInstanceOfType(task, typeof(Task));
                Assert.AreEqual(buffer.Length, task.Result);
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ReadByte_IsDelegatedToReadStream()
        {
            var value = 42;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream(), fixture.CreateStream_ForReadByte(value));

            Assert.AreEqual(value, sut.ReadByte());

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Write_IsDelegatedToWriteStream()
        {
            var buffer = new byte[100];

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForWrite(buffer, 0, buffer.Length), fixture.CreateStream());

            sut.Write(buffer, 0, buffer.Length);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Seek_IsDelegatedToWriteStreamAndReadStream()
        {
            var position = 42;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForSeek(position, SeekOrigin.Begin, position), fixture.CreateStream_ForSeek(position, SeekOrigin.Begin, position));

            Assert.AreEqual(position, sut.Seek(position, SeekOrigin.Begin));

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Seek_WherePositionsDiffer_Throws()
        {
            var position = 42;
            var differingPosition = 17;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForSeek(position, SeekOrigin.Begin, position), fixture.CreateStream_ForSeek(position, SeekOrigin.Begin, differingPosition));

            sut.Seek(position, SeekOrigin.Begin);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SetLength_IsDelegatedToWriteStreamAndReadStream()
        {
            var value = 100;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForSetLength(value), fixture.CreateStream_ForSetLength(value));

            sut.SetLength(value);

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void WriteAsync_IsDelegatedToWriteStream()
        {
            var buffer = new byte[100];

            using (var cts = new CancellationTokenSource()) {
                var cancellationToken = cts.Token;

                var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForWriteAsync(buffer, 0, buffer.Length, cancellationToken), fixture.CreateStream());

                var task = sut.WriteAsync(buffer, 0, buffer.Length, cancellationToken);

                Assert.IsInstanceOfType(task, typeof(Task));
            }

            fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void WriteByte_IsDelegatedToWriteStream()
        {
            var value = (byte)42;

            var sut = new ReadWriteSegregatingStream(fixture.CreateStream_ForWriteByte(value), fixture.CreateStream());

            sut.WriteByte(value);

            fixture.Verify();
        }
    }
}
