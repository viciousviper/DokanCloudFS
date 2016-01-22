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
using System.IO;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;

namespace IgorSoft.DokanCloudFS.IO
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class TraceStream : Stream
    {
        private string name;

        private string fileName;

        private Stream baseStream;

        public TraceStream(string name, string fileName, Stream baseStream)
        {
            this.name = name;
            this.fileName = fileName;
            this.baseStream = baseStream;
        }

        private void Trace(string message)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {name}: '{fileName}' {message}");
        }

        private T Trace<T>(string message, T result)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {name}: '{fileName}' {message} => {result}");
            return result;
        }

        private void Trace<T>(T value, string message)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {name}: '{fileName}' {message}={value}");
        }

        public override long Length
        {
            get {
                lock (baseStream) {
                    return Trace($"{nameof(Length)}", baseStream.Length);
                }
            }
        }

        public override long Position
        {
            get {
                lock (baseStream) {
                    return Trace($"{nameof(Position)}", baseStream.Position);
                }
            }

            set {
                lock (baseStream) {
                    Trace(value, $"set{nameof(Position)}");
                    baseStream.Position = value;
                }
            }
        }

        public override int ReadTimeout
        {
            get {
                lock (baseStream) {
                    return Trace($"{nameof(ReadTimeout)}", baseStream.ReadTimeout);
                }
            }

            set {
                lock (baseStream) {
                    Trace(value, $"set{nameof(ReadTimeout)}");
                    baseStream.ReadTimeout = value;
                }
            }
        }

        public override int WriteTimeout
        {
            get {
                lock (baseStream) {
                    return Trace($"{nameof(WriteTimeout)}", baseStream.WriteTimeout);
                }
            }

            set {
                lock (baseStream) {
                    Trace(value, $"set{nameof(WriteTimeout)}");
                    baseStream.WriteTimeout = value;
                }
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            lock (baseStream) {
                Trace($"{nameof(BeginRead)}(buffer=[{buffer.Length}], offset={offset}, count={count})");
                return baseStream.BeginRead(buffer, offset, count, callback, state);
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            lock (baseStream) {
                Trace($"{nameof(BeginWrite)}(buffer=[{buffer.Length}], offset={offset}, count={count})");
                return baseStream.BeginWrite(buffer, offset, count, callback, state);
            }
        }

        public override bool CanRead
        {
            get {
                lock (baseStream) {
                    return Trace($"{nameof(CanRead)}", baseStream.CanRead);
                }
            }
        }

        public override bool CanSeek
        {
            get {
                lock (baseStream) {
                    return Trace($"{nameof(CanSeek)}", baseStream.CanSeek);
                }
            }
        }

        public override bool CanTimeout
        {
            get {
                lock (baseStream) {
                    return Trace($"{nameof(CanTimeout)}", baseStream.CanTimeout);
                }
            }
        }

        public override bool CanWrite
        {
            get {
                lock (baseStream) {
                    return Trace($"{nameof(CanWrite)}", baseStream.CanWrite);
                }
            }
        }

        public override void Close()
        {
            lock (baseStream) {
                Trace($"{nameof(Close)}()");
                baseStream.Close();
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            lock (baseStream) {
                Trace($"{nameof(CopyToAsync)}(destination={destination.GetType().Name}, bufferSize={bufferSize})");
                return baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
            }
        }

        public override ObjRef CreateObjRef(Type requestedType)
        {
            lock (baseStream) {
                Trace($"{nameof(CreateObjRef)}(requestedType={requestedType.Name})");
                return baseStream.CreateObjRef(requestedType);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (baseStream != null)
                lock (baseStream) {
                    Trace($"{nameof(Dispose)}(disposing={disposing})");

                    baseStream = null;
                }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            lock (baseStream) {
                return Trace($"{nameof(EndRead)}(asyncResult.IsCompleted={asyncResult.IsCompleted})", baseStream.EndRead(asyncResult));
            }
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            lock (baseStream) {
                Trace($"{nameof(EndWrite)}(asyncResult.IsCompleted={asyncResult.IsCompleted})");
                baseStream.EndWrite(asyncResult);
            }
        }

        public override void Flush()
        {
            lock (baseStream) {
                Trace($"{nameof(Flush)}()");
                baseStream.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            lock (baseStream) {
                Trace($"{nameof(FlushAsync)}()");
                return baseStream.FlushAsync(cancellationToken);
            }
        }

        public override object InitializeLifetimeService()
        {
            lock (baseStream) {
                Trace($"{nameof(InitializeLifetimeService)}()");
                return baseStream.InitializeLifetimeService();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (baseStream) {
                return Trace($"{nameof(Read)}(buffer=[{buffer.Length}], offset={offset}, count={count}) <Position={baseStream.Position}>", baseStream.Read(buffer, offset, count));
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            lock (baseStream) {
                Trace($"{nameof(ReadAsync)}(buffer=[{buffer.Length}], offset={offset}, count={count}) <Position={baseStream.Position}>");
                return baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            }
        }

        public override int ReadByte()
        {
            lock (baseStream) {
                return Trace($"{nameof(ReadByte)}()", baseStream.ReadByte());
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (baseStream) {
                return Trace($"{nameof(Seek)}(offset={offset}, origin={origin})", baseStream.Seek(offset, origin));
            }
        }

        public override void SetLength(long value)
        {
            lock (baseStream) {
                Trace($"{nameof(SetLength)}(value={value})");
                baseStream.SetLength(value);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (baseStream) {
                Trace($"{nameof(Write)}(buffer=[{buffer.Length}], offset={offset}, count={count}) <Position={baseStream.Position}>");
                baseStream.Write(buffer, offset, count);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            lock (baseStream) {
                Trace($"{nameof(WriteAsync)}(buffer=[{buffer.Length}], offset={offset}, count={count}) <Position={baseStream.Position}>");
                return baseStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        public override void WriteByte(byte value)
        {
            lock (baseStream) {
                Trace($"{nameof(WriteByte)}(value={value})");
                baseStream.WriteByte(value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(TraceStream)}[{baseStream.GetType().Name}]";
    }
}
