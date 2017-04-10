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

namespace IgorSoft.DokanCloudFS.Configuration
{
    /// <summary>
    /// A CloudDrive gateway and its associated configuration parameters.
    /// </summary>
    /// <typeparam name="TGateway">The type of the gateway.</typeparam>
    internal class GatewayConfiguration<TGateway>
        where TGateway : class
    {
        /// <summary>
        /// Gets the gateway.
        /// </summary>
        /// <value>The gateway.</value>
        public TGateway Gateway { get; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public CloudDriveParameters Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GatewayConfiguration{TGateway}" /> class.
        /// </summary>
        /// <param name="gateway">The gateway.</param>
        /// <param name="parameters">The parameters.</param>
        public GatewayConfiguration(TGateway gateway, CloudDriveParameters parameters)
        {
            Gateway = gateway;
            Parameters = parameters;
        }
    }
}
