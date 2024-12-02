using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Parse.Infrastructure.Utilities;

namespace Parse
{
    /// <summary>
    ///  Represents this app installed on this device. Use this class to track information you want
    ///  to sample from (i.e. if you update a field on app launch, you can issue a query to see
    ///  the number of devices which were active in the last N hours).
    /// </summary>
    [ParseClassName("_Installation")]
    public partial class ParseInstallation : ParseObject
    {
        static HashSet<string> ImmutableKeys { get; } = new HashSet<string> { "deviceType", "deviceUris", "installationId", "timeZone", "localeIdentifier", "parseVersion", "appName", "appIdentifier", "appVersion", "pushType" };

        /// <summary>
        /// Constructs a new ParseInstallation. Generally, you should not need to construct
        /// ParseInstallations yourself. Instead use <see cref="CurrentInstallation"/>.
        /// </summary>
        public ParseInstallation() : base() { }

        /// <summary>
        /// A GUID that uniquely names this app installed on this device.
        /// </summary>
        [ParseFieldName("installationId")]
        public Guid InstallationId
        {
            get
            {
                string installationIdString = GetProperty<string>(nameof(InstallationId));
                Guid? installationId = null;

                try
                {
                    installationId = new Guid(installationIdString);
                }
                catch (Exception)
                {
                    // Do nothing.
                }

                return installationId.Value;
            }
            internal set
            {
                Guid installationId = value;
                SetProperty(installationId.ToString(), nameof(InstallationId));
            }
        }

        /// <summary>
        /// The runtime target of this installation object.
        /// </summary>
        [ParseFieldName("deviceType")]
        public string DeviceType
        {
            get => GetProperty<string>(nameof(DeviceType));
            internal set => SetProperty(value, nameof(DeviceType));
        }

        /// <summary>
        /// The user-friendly display name of this application.
        /// </summary>
        [ParseFieldName("appName")]
        public string AppName
        {
            get => GetProperty<string>(nameof(AppName));
            internal set => SetProperty(value, nameof(AppName));
        }

        /// <summary>
        /// A version string consisting of Major.Minor.Build.Revision.
        /// </summary>
        [ParseFieldName("appVersion")]
        public string AppVersion
        {
            get => GetProperty<string>(nameof(AppVersion));
            internal set => SetProperty(value, nameof(AppVersion));
        }

        /// <summary>
        /// The system-dependent unique identifier of this installation. This identifier should be
        /// sufficient to distinctly name an app on stores which may allow multiple apps with the
        /// same display name.
        /// </summary>
        [ParseFieldName("appIdentifier")]
        public string AppIdentifier
        {
            get => GetProperty<string>(nameof(AppIdentifier));
            internal set => SetProperty(value, nameof(AppIdentifier));
        }

        /// <summary>
        /// The time zone in which this device resides. This string is in the tz database format
        /// Parse uses for local-time pushes. Due to platform restrictions, the mapping is less
        /// granular on Windows than it may be on other systems. E.g. The zones
        /// America/Vancouver America/Dawson America/Whitehorse, America/Tijuana, PST8PDT, and
        /// America/Los_Angeles are all reported as America/Los_Angeles.
        /// </summary>
        [ParseFieldName("timeZone")]
        public string TimeZone
        {
            get => GetProperty<string>(nameof(TimeZone));
            private set => SetProperty(value, nameof(TimeZone));
        }

        /// <summary>
        /// The users locale. This field gets automatically populated by the SDK.
        /// Can be null (Parse Push uses default language in this case).
        /// </summary>
        [ParseFieldName("localeIdentifier")]
        public string LocaleIdentifier
        {
            get => GetProperty<string>(nameof(LocaleIdentifier));
            private set => SetProperty(value, nameof(LocaleIdentifier));
        }

        /// <summary>
        /// Gets the locale identifier in the format: [language code]-[COUNTRY CODE].
        /// </summary>
        /// <returns>The locale identifier in the format: [language code]-[COUNTRY CODE].</returns>
        private string GetLocaleIdentifier()
        {
            string languageCode = null;
            string countryCode = null;

            if (CultureInfo.CurrentCulture != null)
            {
                languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            }
            if (RegionInfo.CurrentRegion != null)
            {
                countryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            if (String.IsNullOrEmpty(countryCode))
            {
                return languageCode;
            }
            else
            {
                return String.Format("{0}-{1}", languageCode, countryCode);
            }
        }

        /// <summary>
        /// The version of the Parse SDK used to build this application.
        /// </summary>
        [ParseFieldName("parseVersion")]
        public Version ParseVersion
        {
            get
            {
                string versionString = GetProperty<string>(nameof(ParseVersion));
                Version version = null;
                try
                {
                    version = new Version(versionString);
                }
                catch (Exception)
                {
                    // Do nothing.
                }

                return version;
            }
            private set
            {
                Version version = value;
                SetProperty(version.ToString(), nameof(ParseVersion));
            }
        }

        /// <summary>
        /// A sequence of arbitrary strings which are used to identify this installation for push notifications.
        /// By convention, the empty string is known as the "Broadcast" channel.
        /// </summary>
        [ParseFieldName("channels")]
        public IList<string> Channels
        {
            get => GetProperty<IList<string>>(nameof(Channels));
            set => SetProperty(value, nameof(Channels));
        }

        protected override bool CheckKeyMutable(string key)
        {
            return !ImmutableKeys.Contains(key);
        }

        protected override async Task SaveAsync(Task toAwait, CancellationToken cancellationToken)
        {
            if (Services.CurrentInstallationController.IsCurrent(this))
#pragma warning disable CS1030 // #warning directive
            {
                SetIfDifferent("deviceType", Services.MetadataController.EnvironmentData.Platform);
                SetIfDifferent("timeZone", Services.MetadataController.EnvironmentData.TimeZone);
                SetIfDifferent("localeIdentifier", GetLocaleIdentifier());
                SetIfDifferent("parseVersion", ParseClient.Version);
                SetIfDifferent("appVersion", Services.MetadataController.HostManifestData.Version);
                SetIfDifferent("appIdentifier", Services.MetadataController.HostManifestData.Identifier);
                SetIfDifferent("appName", Services.MetadataController.HostManifestData.Name);

#warning InstallationDataFinalizer needs to be injected here somehow or removed.

                //platformHookTask = Client.InstallationDataFinalizer.FinalizeAsync(this);
            }
#pragma warning restore CS1030 // #warning directive
            Task platformHookTask = ParseClient.Instance.InstallationDataFinalizer.FinalizeAsync(this); 

            // Wait for the platform task, then proceed with saving the main task.
            try
            {
                _ = platformHookTask.Safe().ConfigureAwait(false);
                _ = base.SaveAsync(toAwait, cancellationToken).ConfigureAwait(false);
                if (!Services.CurrentInstallationController.IsCurrent(this))
                {
                    _ = Services.CurrentInstallationController.SetAsync(this, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                // You can log it or rethrow if necessary
                Console.Error.WriteLine(ex);
            }

        }

        /// <summary>
        /// This mapping of Windows names to a standard everyone else uses is maintained
        /// by the Unicode consortium, which makes this officially the first helpful
        /// interaction between Unicode and Microsoft.
        /// Unfortunately this is a little lossy in that we only store the first mapping in each zone because
        /// Microsoft does not give us more granular location information.
        /// Built from http://unicode.org/repos/cldr-tmp/trunk/diff/supplemental/zone_tzid.html
        /// </summary>
        internal static Dictionary<string, string> TimeZoneNameMap { get; } = new Dictionary<string, string>
        {
            ["Dateline Standard Time"] = "Etc/GMT+12",
            ["UTC-11"] = "Etc/GMT+11",
            ["Hawaiian Standard Time"] = "Pacific/Honolulu",
            ["Alaskan Standard Time"] = "America/Anchorage",
            ["Pacific Standard Time (Mexico)"] = "America/Santa_Isabel",
            ["Pacific Standard Time"] = "America/Los_Angeles",
            ["US Mountain Standard Time"] = "America/Phoenix",
            ["Mountain Standard Time (Mexico)"] = "America/Chihuahua",
            ["Mountain Standard Time"] = "America/Denver",
            ["Central America Standard Time"] = "America/Guatemala",
            ["Central Standard Time"] = "America/Chicago",
            ["Central Standard Time (Mexico)"] = "America/Mexico_City",
            ["Canada Central Standard Time"] = "America/Regina",
            ["SA Pacific Standard Time"] = "America/Bogota",
            ["Eastern Standard Time"] = "America/New_York",
            ["US Eastern Standard Time"] = "America/Indianapolis",
            ["Venezuela Standard Time"] = "America/Caracas",
            ["Paraguay Standard Time"] = "America/Asuncion",
            ["Atlantic Standard Time"] = "America/Halifax",
            ["Central Brazilian Standard Time"] = "America/Cuiaba",
            ["SA Western Standard Time"] = "America/La_Paz",
            ["Pacific SA Standard Time"] = "America/Santiago",
            ["Newfoundland Standard Time"] = "America/St_Johns",
            ["E. South America Standard Time"] = "America/Sao_Paulo",
            ["Argentina Standard Time"] = "America/Buenos_Aires",
            ["SA Eastern Standard Time"] = "America/Cayenne",
            ["Greenland Standard Time"] = "America/Godthab",
            ["Montevideo Standard Time"] = "America/Montevideo",
            ["Bahia Standard Time"] = "America/Bahia",
            ["UTC-02"] = "Etc/GMT+2",
            ["Azores Standard Time"] = "Atlantic/Azores",
            ["Cape Verde Standard Time"] = "Atlantic/Cape_Verde",
            ["Morocco Standard Time"] = "Africa/Casablanca",
            ["UTC"] = "Etc/GMT",
            ["GMT Standard Time"] = "Europe/London",
            ["Greenwich Standard Time"] = "Atlantic/Reykjavik",
            ["W. Europe Standard Time"] = "Europe/Berlin",
            ["Central Europe Standard Time"] = "Europe/Budapest",
            ["Romance Standard Time"] = "Europe/Paris",
            ["Central European Standard Time"] = "Europe/Warsaw",
            ["W. Central Africa Standard Time"] = "Africa/Lagos",
            ["Namibia Standard Time"] = "Africa/Windhoek",
            ["GTB Standard Time"] = "Europe/Bucharest",
            ["Middle East Standard Time"] = "Asia/Beirut",
            ["Egypt Standard Time"] = "Africa/Cairo",
            ["Syria Standard Time"] = "Asia/Damascus",
            ["E. Europe Standard Time"] = "Asia/Nicosia",
            ["South Africa Standard Time"] = "Africa/Johannesburg",
            ["FLE Standard Time"] = "Europe/Kiev",
            ["Turkey Standard Time"] = "Europe/Istanbul",
            ["Israel Standard Time"] = "Asia/Jerusalem",
            ["Jordan Standard Time"] = "Asia/Amman",
            ["Arabic Standard Time"] = "Asia/Baghdad",
            ["Kaliningrad Standard Time"] = "Europe/Kaliningrad",
            ["Arab Standard Time"] = "Asia/Riyadh",
            ["E. Africa Standard Time"] = "Africa/Nairobi",
            ["Iran Standard Time"] = "Asia/Tehran",
            ["Arabian Standard Time"] = "Asia/Dubai",
            ["Azerbaijan Standard Time"] = "Asia/Baku",
            ["Russian Standard Time"] = "Europe/Moscow",
            ["Mauritius Standard Time"] = "Indian/Mauritius",
            ["Georgian Standard Time"] = "Asia/Tbilisi",
            ["Caucasus Standard Time"] = "Asia/Yerevan",
            ["Afghanistan Standard Time"] = "Asia/Kabul",
            ["Pakistan Standard Time"] = "Asia/Karachi",
            ["West Asia Standard Time"] = "Asia/Tashkent",
            ["India Standard Time"] = "Asia/Calcutta",
            ["Sri Lanka Standard Time"] = "Asia/Colombo",
            ["Nepal Standard Time"] = "Asia/Katmandu",
            ["Central Asia Standard Time"] = "Asia/Almaty",
            ["Bangladesh Standard Time"] = "Asia/Dhaka",
            ["Ekaterinburg Standard Time"] = "Asia/Yekaterinburg",
            ["Myanmar Standard Time"] = "Asia/Rangoon",
            ["SE Asia Standard Time"] = "Asia/Bangkok",
            ["N. Central Asia Standard Time"] = "Asia/Novosibirsk",
            ["China Standard Time"] = "Asia/Shanghai",
            ["North Asia Standard Time"] = "Asia/Krasnoyarsk",
            ["Singapore Standard Time"] = "Asia/Singapore",
            ["W. Australia Standard Time"] = "Australia/Perth",
            ["Taipei Standard Time"] = "Asia/Taipei",
            ["Ulaanbaatar Standard Time"] = "Asia/Ulaanbaatar",
            ["North Asia East Standard Time"] = "Asia/Irkutsk",
            ["Tokyo Standard Time"] = "Asia/Tokyo",
            ["Korea Standard Time"] = "Asia/Seoul",
            ["Cen. Australia Standard Time"] = "Australia/Adelaide",
            ["AUS Central Standard Time"] = "Australia/Darwin",
            ["E. Australia Standard Time"] = "Australia/Brisbane",
            ["AUS Eastern Standard Time"] = "Australia/Sydney",
            ["West Pacific Standard Time"] = "Pacific/Port_Moresby",
            ["Tasmania Standard Time"] = "Australia/Hobart",
            ["Yakutsk Standard Time"] = "Asia/Yakutsk",
            ["Central Pacific Standard Time"] = "Pacific/Guadalcanal",
            ["Vladivostok Standard Time"] = "Asia/Vladivostok",
            ["New Zealand Standard Time"] = "Pacific/Auckland",
            ["UTC+12"] = "Etc/GMT-12",
            ["Fiji Standard Time"] = "Pacific/Fiji",
            ["Magadan Standard Time"] = "Asia/Magadan",
            ["Tonga Standard Time"] = "Pacific/Tongatapu",
            ["Samoa Standard Time"] = "Pacific/Apia"
        };

        /// <summary>
        /// This is a mapping of odd TimeZone offsets to their respective IANA codes across the world.
        /// This list was compiled from painstakingly pouring over the information available at
        /// https://en.wikipedia.org/wiki/List_of_tz_database_time_zones.
        /// </summary>
        internal static Dictionary<TimeSpan, string> TimeZoneOffsetMap { get; } = new Dictionary<TimeSpan, string>
        {
            [new TimeSpan(12, 45, 0)] = "Pacific/Chatham",
            [new TimeSpan(10, 30, 0)] = "Australia/Lord_Howe",
            [new TimeSpan(9, 30, 0)] = "Australia/Adelaide",
            [new TimeSpan(8, 45, 0)] = "Australia/Eucla",
            [new TimeSpan(8, 30, 0)] = "Asia/Pyongyang", // Parse in North Korea confirmed.
            [new TimeSpan(6, 30, 0)] = "Asia/Rangoon",
            [new TimeSpan(5, 45, 0)] = "Asia/Kathmandu",
            [new TimeSpan(5, 30, 0)] = "Asia/Colombo",
            [new TimeSpan(4, 30, 0)] = "Asia/Kabul",
            [new TimeSpan(3, 30, 0)] = "Asia/Tehran",
            [new TimeSpan(-3, 30, 0)] = "America/St_Johns",
            [new TimeSpan(-4, 30, 0)] = "America/Caracas",
            [new TimeSpan(-9, 30, 0)] = "Pacific/Marquesas",
        };
    }
}
