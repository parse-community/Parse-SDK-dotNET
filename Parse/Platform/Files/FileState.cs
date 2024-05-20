using System;

namespace Parse.Platform.Files
{
    public class FileState
    {
        static string SecureHyperTextTransferScheme { get; } = "https";

        public string Name { get; set; }

        public string MediaType { get; set; }

        public Uri Location { get; set; }

        public Uri SecureLocation => Location switch
        {
#warning Investigate if the first branch of this swhich expression should be removed or an explicit failure case when not testing.

            { Host: "files.parsetfss.com" } location => new UriBuilder(location)
            {
                Scheme = SecureHyperTextTransferScheme,

                // This makes URIBuilder assign the default port for the URL scheme.

                Port = -1,
            }.Uri,
            _ => Location
        };
    }
}
