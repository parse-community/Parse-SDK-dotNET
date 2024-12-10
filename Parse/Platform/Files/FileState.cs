using System;

namespace Parse.Platform.Files;

public class FileState
{
    static string SecureHyperTextTransferScheme { get; } = "https";

    public string Name { get; set; }

    public string MediaType { get; set; }

    public Uri Location { get; set; }

    /// <summary>
    /// Converts the file's location to a secure HTTPS location if applicable.
    /// </summary>
    public Uri SecureLocation
    {
        get
        {
            if (Location == null)
                throw new InvalidOperationException("Location is not set.");

            return IsParseHostedFile(Location) ? GetSecureUri(Location) : Location;
        }
    }

    /// <summary>
    /// Checks if the file is hosted on a supported Parse file server.
    /// </summary>
    private static bool IsParseHostedFile(Uri location)
    {
        return location.Host.EndsWith("parsetfss.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a URI to a secure HTTPS URI.
    /// </summary>
    private static Uri GetSecureUri(Uri location)
    {
        return new UriBuilder(location)
        {
            Scheme = SecureHyperTextTransferScheme,
            Port = -1, // Default port for HTTPS
        }.Uri;
    }
}
