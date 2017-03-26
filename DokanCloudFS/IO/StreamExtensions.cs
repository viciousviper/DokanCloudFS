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
using System.Security.Cryptography;

namespace IgorSoft.DokanCloudFS.IO
{
    internal static class StreamExtensions
    {
        private const int MAX_BULKDOWNLOAD_SIZE = 1 << 29;

        public static Stream EncryptOrPass(this Stream stream, string encryptionKey)
        {
            return !string.IsNullOrEmpty(encryptionKey)
                ? Process(stream, encryptionKey, SharpAESCrypt.OperationMode.Encrypt)
                : stream;
        }

        public static Stream DecryptOrPass(this Stream stream, string encryptionKey)
        {
            if (!string.IsNullOrEmpty(encryptionKey))
                try {
                    stream = Process(stream, encryptionKey, SharpAESCrypt.OperationMode.Decrypt);
                } catch (InvalidDataException) {
                    // Ignore InvalidDataException to enable reading of unencrypted content from cloud volumes
                    stream.Seek(0, SeekOrigin.Begin);
                }

            return stream;
        }

        public static Stream ToSeekableStream(this Stream stream)
        {
            if (!stream.CanSeek) {
                var bufferStream = new MemoryStream();
                stream.CopyTo(bufferStream, MAX_BULKDOWNLOAD_SIZE);
                bufferStream.Seek(0, SeekOrigin.Begin);
                stream.Dispose();
                stream = bufferStream;
            }

            return stream;
        }

        private static Stream Process(Stream stream, string encryptionKey, SharpAESCrypt.OperationMode mode)
        {
            var buffer = new MemoryStream();

            switch (mode) {
                case SharpAESCrypt.OperationMode.Encrypt:
                    SharpAESCrypt.SharpAESCrypt.Encrypt(encryptionKey, stream, buffer);
                    break;
                case SharpAESCrypt.OperationMode.Decrypt:
                    SharpAESCrypt.SharpAESCrypt.Decrypt(encryptionKey, stream, buffer);
                    break;
            }


            buffer.Seek(0, SeekOrigin.Begin);
            return buffer;
        }
    }
}
