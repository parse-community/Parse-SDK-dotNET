using System;
using System.Collections.Generic;
using System.Text;

namespace Parse.Library
{
    /// <summary>
    /// Represents the configuration of the Parse SDK.
    /// </summary>
    public struct Configuration
    {
        internal bool Test { get; set; }

        /// <summary>
        /// The App ID of your app.
        /// </summary>
        public string ApplicationID { get; set; }

        /// <summary>
        /// A URI pointing to the target Parse Server instance hosting the app targeted by <see cref="ApplicationID"/>.
        /// </summary>
        public string ServerURI { get; set; }

        /// <summary>
        /// The .NET Key for the Parse app targeted by <see cref="ServerURI"/>.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The Master Key for the Parse app targeted by <see cref="Key"/>.
        /// </summary>
        public string MasterKey { get; set; }

        /// <summary>
        /// Additional HTTP headers to be sent with network requests from the SDK.
        /// </summary>
        public IDictionary<string, string> AuxiliaryHeaders { get; set; }
    }
}
