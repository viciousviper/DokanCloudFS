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
using DokanNet;
using FileAccess = DokanNet.FileAccess;
using IgorSoft.DokanCloudFS.Extensions;

namespace IgorSoft.DokanCloudFS
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal partial class CloudOperations
    {
        private NtStatus AsTrace(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            logger?.Trace($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));

            return result;
        }

        private NtStatus AsTrace(string method, string fileName, DokanFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, NtStatus result)
        {
            logger?.Trace($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));

            return result;
        }

        private NtStatus AsDebug(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            logger?.Debug($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));

            return result;
        }

        private NtStatus AsDebug(string method, string fileName, DokanFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, NtStatus result)
        {
            logger?.Debug($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));

            return result;
        }

        private NtStatus AsWarn(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            logger?.Warn($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));

            return result;
        }

        private NtStatus AsError(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            logger?.Error($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));

            return result;
        }

        private NtStatus AsError(string method, string fileName, DokanFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, NtStatus result)
        {
            logger?.Error($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));

            return result;
        }
   }
}