using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;

namespace Parse.Infrastructure
{
    /// <summary>
    /// Represents the configuration of the Parse SDK.
    /// </summary>
    public struct ServerConnectionData : IServerConnectionData
    {
        // TODO: Consider simplification of names: ApplicationID => Application | Target, ServerURI => Server, MasterKey => Master.
        // TODO: Move Test property elsewhere.

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

        // ALTERNATE NAME: AuxiliaryHeaders, AdditionalHeaders

        /// <summary>
        /// Additional HTTP headers to be sent with network requests from the SDK.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }
    }
}
