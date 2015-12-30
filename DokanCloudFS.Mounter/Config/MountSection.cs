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
using System.Configuration;

namespace IgorSoft.DokanCloudFS.Mounter.Config
{
    public sealed class MountSection : ConfigurationSection
    {
        private const string drivesPropertyName = "drives";
        private const string threadsPropertyName = "threads";

        [ConfigurationProperty(drivesPropertyName)]
        public DriveElementCollection Drives
        {
            get { return (DriveElementCollection)this[drivesPropertyName]; }
            set { this[drivesPropertyName] = value; }
        }

        [ConfigurationProperty(threadsPropertyName)]
        //[IntegerValidator(MinValue = 1, MaxValue = 10)]
        public int Threads
        {
            get { return (int)this[threadsPropertyName]; }
            set { this[threadsPropertyName] = value; }
        }
    }
}
