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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IgorSoft.DokanCloudFS.Tests
{
    internal static class StreamExtensions
    {
        public static bool Contains(this Stream stream, byte[] content)
        {
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[stream.Length];
            var result = (stream.Read(buffer, 0, buffer.Length) == stream.Length && buffer.SequenceEqual(content));
            stream.Seek(position, SeekOrigin.Begin);
            return result;
        }

        public static void FindDifferences(this Stream stream, byte[] content, ICollection<Tuple<int, int, byte[], byte[]>> differences)
        {
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[stream.Length];
            if (stream.Read(buffer, 0, buffer.Length) != stream.Length)
                throw new InvalidOperationException("Failure reading from Stream");
            stream.Seek(position, SeekOrigin.Begin);

            for (int i = 0; i < buffer.Length;)
                if (buffer[i] == content[i]) {
                    ++i;
                } else {
                    var j = i + 1;
                    while (j < buffer.Length && buffer[j] != content[j])
                        ++j;
                    var length = j - i;
                    differences.Add(new Tuple<int, int, byte[], byte[]>(i, length, buffer.Skip(i).Take(length).ToArray(), content.Skip(i).Take(length).ToArray()));
                    i = j;
                }
        }
    }
}