namespace Parse.Abstractions.Library
{
    /// <summary>
    /// In the event that you would like to use the Parse SDK
    /// from a completely portable project, with no platform-specific library required,
    /// to get full access to all of our features available on Parse Dashboard
    /// (A/B testing, slow queries, etc.), you must set the values of this struct
    /// to be appropriate for your platform.
    ///
    /// Any values set here will overwrite those that are automatically configured by
    /// any platform-specific migration library your app includes.
    /// </summary>
    public interface IHostApplicationVersioningData
    {
        /// <summary>
        /// The build number of your app.
        /// </summary>
        string BuildVersion { get; }

        /// <summary>
        /// The human friendly version number of your app.
        /// </summary>
        string DisplayVersion { get; }

        /// <summary>
        /// The operating system version of the platform the SDK is operating in..
        /// </summary>
        string HostOSVersion { get; }

        /// <summary>
        /// Gets a value for whether or not this instance of <see cref="VersionInformation"/> is populated with default values.
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// Gets a value for whether or not this instance of <see cref="VersionInformation"/> can currently be used for the generation of <see cref="MetadataBasedStorageConfiguration.NoCompanyInferred"/>.
        /// </summary>
        bool CanBeUsedForInference { get; }
    }
}
