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
using System.Globalization;

namespace IgorSoft.DokanCloudFS.Mounter.Config
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class DriveElement : ConfigurationElement
    {
        private const string schemaPropertyName = "schema";
        private const string userNamePropertyName = "userName";
        private const string rootPropertyName = "root";
        private const string apiKeyPropertyName = "apiKey";
        private const string encryptionKeyPropertyName = "encryptionKey";
        private const string parametersPropertyName = "parameters";
        private const string timeoutPropertyName = "timeout";

        [ConfigurationProperty(schemaPropertyName, IsRequired = true)]
        public string Schema
        {
            get { return (string)this[schemaPropertyName]; }
            set { this[schemaPropertyName] = value; }
        }

        [ConfigurationProperty(userNamePropertyName)]
        public string UserName
        {
            get { return (string)this[userNamePropertyName]; }
            set { this[userNamePropertyName] = value; }
        }

        [ConfigurationProperty(rootPropertyName, IsKey = true, IsRequired = true)]
        public string Root
        {
            get { return (string)this[rootPropertyName]; }
            set { this[rootPropertyName] = value; }
        }

        [ConfigurationProperty(apiKeyPropertyName)]
        public string ApiKey
        {
            get { return (string)this[apiKeyPropertyName]; }
            set { this[apiKeyPropertyName] = value; }
        }

        [ConfigurationProperty(encryptionKeyPropertyName)]
        public string EncryptionKey
        {
            get { return (string)this[encryptionKeyPropertyName]; }
            set { this[encryptionKeyPropertyName] = value; }
        }

        [ConfigurationProperty(parametersPropertyName)]
        public string Parameters
        {
            get { return (string)this[parametersPropertyName]; }
            set { this[parametersPropertyName] = value; }
        }

        [ConfigurationProperty(timeoutPropertyName)]
        public int Timeout
        {
            get { return (int)this[timeoutPropertyName]; }
            set { this[timeoutPropertyName] = value; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(DriveElement)} schema='{Schema}', userName='{UserName}', root='{Root}', apiKey='{ApiKey}', encryptionKey='{EncryptionKey}', timeout='{Timeout}'".ToString(CultureInfo.CurrentCulture);
    }
}
