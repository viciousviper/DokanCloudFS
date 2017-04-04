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

namespace IgorSoft.DokanCloudFS {
    using System;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("IgorSoft.DokanCloudFS.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Composition host already initialized
        /// </summary>
        internal static string CompositionHostAlreadyInitialized {
            get {
                return ResourceManager.GetString("CompositionHostAlreadyInitialized", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Composition host not initialized
        /// </summary>
        internal static string CompositionHostNotInitialized {
            get {
                return ResourceManager.GetString("CompositionHostNotInitialized", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} &apos;{1}&apos; is not a resolvable FileSystemInfo type.
        /// </summary>
        internal static string InvalidNonProxyResolution {
            get {
                return ResourceManager.GetString("InvalidNonProxyResolution", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Cannot resolve ProxyFileInfo &apos;{0}&apos; with FileInfo &apos;{1}&apos;.
        /// </summary>
        internal static string InvalidProxyResolution {
            get {
                return ResourceManager.GetString("InvalidProxyResolution", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to No gateway is registered for schema &apos;{0}&apos;.
        /// </summary>
        internal static string NoGatewayForSchema {
            get {
                return ResourceManager.GetString("NoGatewayForSchema", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} must be non-negative.
        /// </summary>
        internal static string NonnegativeValueRequired {
            get {
                return ResourceManager.GetString("NonnegativeValueRequired", resourceCulture);
            }
        }
        /// <summary>
        ///   Looks up a localized string similar to Unknown item type &apos;{0}&apos;
        /// </summary>
        internal static string UnknownItemType {
            get {
                return ResourceManager.GetString("UnknownItemType", resourceCulture);
            }
        }
    }
}
