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
using System.Threading;
using IgorSoft.DokanCloudFS.Parameters;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.DokanCloudFS
{
    internal abstract class CloudDriveBase : IDisposable
    {
        protected readonly RootName rootName;

        protected readonly string apiKey;

        protected readonly string encryptionKey;

        protected DriveInfoContract drive;

        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public string DisplayRoot { get; }

        public long? Free => ExecuteInSemaphore(() => GetDrive().FreeSpace, $"get_{nameof(Free)}".ToString(CultureInfo.InvariantCulture));

        public long? Used => ExecuteInSemaphore(() => GetDrive().UsedSpace, $"get_{nameof(Used)}".ToString(CultureInfo.InvariantCulture));

        protected CloudDriveBase(RootName rootName, CloudDriveParameters parameters)
        {
            this.rootName = rootName;
            DisplayRoot = rootName.Value;
            if (parameters != null) {
                apiKey = parameters.ApiKey;
                encryptionKey = parameters.EncryptionKey;
            }
            if (string.IsNullOrEmpty(encryptionKey))
                DisplayRoot = DisplayRoot.Insert(0, "*");
        }

        protected void ExecuteInSemaphore(Action action, string methodName, bool invalidateDrive = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            semaphore.Wait();
            try {
                action();
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            } finally {
                if (invalidateDrive)
                    drive = null;
                semaphore.Release();
            }
        }

        protected T ExecuteInSemaphore<T>(Func<T> func, string methodName, bool invalidateDrive = false)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            semaphore.Wait();
            try {
                return func();
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            } finally {
                if (invalidateDrive)
                    drive = null;
                semaphore.Release();
            }
        }

        protected abstract DriveInfoContract GetDrive();

        public void Dispose()
        {
            semaphore.Dispose();
            semaphore = null;
            GC.SuppressFinalize(this);
        }
    }
}
