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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.DokanCloudFS.Tests
{
    [TestClass]
    public sealed partial class CloudDriveBaseTests
    {
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExecuteInSemaphor_WhereActionIsNull_Throws()
        {
            Fixture.ExecuteInSemaphore((Action)null, string.Empty);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ExecuteInSemaphor_WhereActionSucceeds_Succeeds()
        {
            var executed = false;
            Action action = () => executed = true;

            Fixture.ExecuteInSemaphore(action, string.Empty);

            Assert.IsTrue(executed, "Expected Action not executed");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ApplicationException))]
        public void ExecuteInSemaphor_WhereActionThrowsAggregateException_ThrowsInnerException()
        {
            Action action = () => { throw new AggregateException(new ApplicationException()); };

            Fixture.ExecuteInSemaphore(action, string.Empty);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExecuteInSemaphor_WhereFuncIsNull_Throws()
        {
            Fixture.ExecuteInSemaphore((Func<object>)null, string.Empty);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ExecuteInSemaphor_WhereFuncSucceeds_ReturnsFunctionResult()
        {
            var @object = new object();
            Func<object> func = () => @object;

            var result = Fixture.ExecuteInSemaphore(func, string.Empty);

            Assert.AreSame(@object, result, "Expected result not returned");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ApplicationException))]
        public void ExecuteInSemaphor_WhereFuncThrowsAggregateException_ThrowsInnerException()
        {
            Func<object> func = () => { throw new AggregateException(new ApplicationException()); };

            var result = Fixture.ExecuteInSemaphore(func, string.Empty);
        }
    }
}
