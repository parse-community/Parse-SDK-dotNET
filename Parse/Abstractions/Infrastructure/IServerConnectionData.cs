using System.Collections.Generic;

namespace Parse.Abstractions.Infrastructure
{
    public interface IServerConnectionData
    {
        /// <summary>
        /// The App ID of your app.
        /// </summary>
        string ApplicationID { get; set; }

        /// <summary>
        /// A URI pointing to the target Parse Server instance hosting the app targeted by <see cref="ApplicationID"/>.
        /// </summary>
        string ServerURI { get; set; }

        /// <summary>
        /// The .NET Key for the Parse app targeted by <see cref="ServerURI"/>.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// The Master Key for the Parse app targeted by <see cref="Key"/>.
        /// </summary>
        string MasterKey { get; set; }

        /// <summary>
        /// Additional HTTP headers to be sent with network requests from the SDK.
        /// </summary>
        IDictionary<string, string> Headers { get; set; }
    }
}
