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
using System.Configuration;
using System.Linq;

namespace IgorSoft.DokanCloudFS.Mounter.Config
{
    [ConfigurationCollection(typeof(DriveElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class DriveElementCollection : ConfigurationElementCollection, IEnumerable<DriveElement>
    {
        private const string driveElementName = "drive";

        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        protected override ConfigurationElement CreateNewElement() => new DriveElement();

        protected override ConfigurationElement CreateNewElement(string elementName) => new DriveElement() { Root = elementName };

        protected override string ElementName => driveElementName;

        protected override object GetElementKey(ConfigurationElement element) => (element as DriveElement)?.Root ?? null;

        protected override bool IsElementName(string elementName) => BaseGetAllKeys().Contains(elementName);

        IEnumerator<DriveElement> IEnumerable<DriveElement>.GetEnumerator()
        {
            foreach (var element in this)
                yield return (DriveElement)element;
        }
    }
}
