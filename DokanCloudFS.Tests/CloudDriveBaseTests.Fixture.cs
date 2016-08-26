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
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.DokanCloudFS.Parameters;

namespace IgorSoft.DokanCloudFS.Tests
{
    public sealed partial class CloudDriveBaseTests
    {
        private static class Fixture
        {
            private class FakeCloudDrive : CloudDriveBase
            {
                public FakeCloudDrive(RootName rootName, CloudDriveParameters parameters) : base(rootName, parameters)
                {
                }

                protected override DriveInfoContract GetDrive()
                {
                    throw new NotImplementedException();
                }

                public void ExecuteInSemaphore(Action action, string methodName)
                {
                    base.ExecuteInSemaphore(action, methodName);
                }

                public T ExecuteInSemaphore<T>(Func<T> func, string methodName)
                {
                    return base.ExecuteInSemaphore(func, methodName);
                }
            }

            private static FakeCloudDrive CreateCloudDrive() => new FakeCloudDrive(new RootName("fake", "FakeUser", "FakeRoot"), null);

            public static void ExecuteInSemaphore(Action action, string methodName)
            {
                using (var drive = CreateCloudDrive())
                    drive.ExecuteInSemaphore(action, methodName);
            }

            public static T ExecuteInSemaphore<T>(Func<T> func, string methodName)
            {
                using (var drive = CreateCloudDrive())
                    return drive.ExecuteInSemaphore(func, methodName);
            }
        }
    }
}
