/*
The MIT License(MIT)

Copyright(c) 2017 IgorSoft

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
using System.Globalization;
using System.Linq;
using IgorSoft.CloudFS.Interface;

namespace IgorSoft.DokanCloudFS.Configuration
{
    /// <summary>
    /// A CloudDrive's configuration options.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class CloudDriveConfiguration
    {
        /// <summary>
        /// Gets the root name.
        /// </summary>
        /// <value>The root name.</value>
        public RootName RootName { get; }

        /// <summary>
        /// Gets the API key.
        /// </summary>
        /// <value>The API key.</value>
        public string ApiKey { get; }

        /// <summary>
        /// Gets the encryption key.
        /// </summary>
        /// <value>The encryption key.</value>
        public string EncryptionKey { get; internal set; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public IDictionary<string, string> Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudDriveConfiguration{TGateway}" /> class.
        /// </summary>
        /// <param name="rootName">The root name.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <param name="parameters">The parameters.</param>
        /// <exception cref="System.ArgumentNullException">rootName</exception>
        public CloudDriveConfiguration(RootName rootName, string apiKey = null, string encryptionKey = null, IDictionary<string, string> parameters = null)
        {
            RootName = rootName ?? throw new ArgumentNullException(nameof(rootName));
            ApiKey = apiKey;
            EncryptionKey = encryptionKey;
            Parameters = parameters;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(CloudDriveConfiguration)} {nameof(RootName)}={RootName} {nameof(ApiKey)}='{ApiKey}' {nameof(EncryptionKey)}='{EncryptionKey}' {nameof(Parameters)}=[{string.Join(",", Parameters?.Select(p => $"{p.Key}={p.Value}") ?? Enumerable.Empty<string>())}]".ToString(CultureInfo.CurrentCulture);
    }
}
