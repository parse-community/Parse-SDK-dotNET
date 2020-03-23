using System;
using System.Reflection;
using Parse.Abstractions.Library;
using Parse.Storage;

namespace Parse.Library
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
    public class HostApplicationVersioningData : IHostApplicationVersioningData
    {
        /// <summary>
        /// An instance of <see cref="HostApplicationVersioningData"/> with inferred values based on the entry assembly.
        /// </summary>
        /// <remarks>Should not be used with Unity.</remarks>
        public static HostApplicationVersioningData Inferred { get; } = new HostApplicationVersioningData
        {
            BuildVersion = Assembly.GetEntryAssembly().GetName().Version.Build.ToString(),
            DisplayVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
            HostOSVersion = Environment.OSVersion.ToString()
        };

        /// <summary>
        /// The build number of your app.
        /// </summary>
        public string BuildVersion { get; set; }

        /// <summary>
        /// The human friendly version number of your app.
        /// </summary>
        public string DisplayVersion { get; set; }

        /// <summary>
        /// The host operating system version of the platform the host application is operating in.
        /// </summary>
        public string HostOSVersion { get; set; }

        /// <summary>
        /// Gets a value for whether or not this instance of <see cref="HostApplicationVersioningData"/> is populated with default values.
        /// </summary>
        public bool IsDefault => BuildVersion is null && DisplayVersion is null && HostOSVersion is null;

        /// <summary>
        /// Gets a value for whether or not this instance of <see cref="HostApplicationVersioningData"/> can currently be used for the generation of <see cref="MetadataBasedCacheLocationConfiguration.NoCompanyInferred"/>.
        /// </summary>
        public bool CanBeUsedForInference => !(IsDefault || String.IsNullOrWhiteSpace(DisplayVersion));
    }
}
