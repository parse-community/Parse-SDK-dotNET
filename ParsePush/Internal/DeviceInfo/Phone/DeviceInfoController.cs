using System;
using System.Threading.Tasks;
using System.Xml;
using LeanCloud.Core.Internal;
using System.Collections.Generic;
using Microsoft.Phone.Notification;

namespace LeanCloud.Push.Internal {
  public class DeviceInfoController : IDeviceInfoController {
    public string DeviceType {
      get {
        return "winphone";
      }
    }

    public string DeviceTimeZone {
      get {
        // We need the system string to be in english so we'll have the proper key in our lookup table.
        // If it's not in english then we will attempt to fallback to the closest Time Zone we can find.
        TimeZoneInfo tzInfo = TimeZoneInfo.Local;

        string deviceTimeZone = null;
        if (AVInstallation.TimeZoneNameMap.TryGetValue(tzInfo.StandardName, out deviceTimeZone)) {
          return deviceTimeZone;
        }

        TimeSpan utcOffset = tzInfo.BaseUtcOffset;

        // If we have an offset that is not a round hour, then use our second map to see if we can
        // convert it or not.
        if (AVInstallation.TimeZoneOffsetMap.TryGetValue(utcOffset, out deviceTimeZone)) {
          return deviceTimeZone;
        }

        // NOTE: Etc/GMT{+/-} format is inverted from the UTC offset we use as normal people -
        // a negative value means ahead of UTC, a positive value means behind UTC.
        bool negativeOffset = utcOffset.Ticks < 0;
        return String.Format("Etc/GMT{0}{1}", negativeOffset ? "+" : "-", Math.Abs(utcOffset.Hours));
      }
    }

    public string AppName {
      get {
        return GetAppAttribute("Title");
      }
    }

    public string AppBuildVersion {
      get {
        return GetAppAttribute("Version");
      }
    }

    public string AppDisplayVersion {
      get {
        return GetAppAttribute("Version");
      }
    }

    public string AppIdentifier {
      get {
        return GetAppAttribute("ProductID");
      }
    }

    public Task ExecuteParseInstallationSaveHookAsync(AVInstallation installation) {
      return getToastUriTask.Value.ContinueWith(t => {
        installation.SetIfDifferent("deviceUris", t.Result == null ? null :
          new Dictionary<string, string> {
          { toastChannelTag, t.Result }
        });
      });
    }

    public void Initialize() {
    }

    /// <summary>
    /// Gets an attribute from the Windows Phone App Manifest App element
    /// </summary>
    /// <param name="attributeName">the attribute name</param>
    /// <returns>the attribute value</returns>
    /// This is a duplicate of what we have in AVInstallation. We do it because
    /// it's easier to maintain this way (rather than referencing <c>PlatformHooks</c> everywhere).
    private string GetAppAttribute(string attributeName) {
      string appManifestName = "WMAppManifest.xml";
      string appNodeName = "App";

      var settings = new XmlReaderSettings();
      settings.XmlResolver = new XmlXapResolver();

      using (XmlReader rdr = XmlReader.Create(appManifestName, settings)) {
        rdr.ReadToDescendant(appNodeName);
        if (!rdr.IsStartElement()) {
          throw new System.FormatException(appManifestName + " is missing " + appNodeName);
        }

        return rdr.GetAttribute(attributeName);
      }
    }

    /// <summary>
    /// Future proofing: Right now there's only one valid channel for the app, but we will likely
    /// want to allow additional channels for auxiliary tiles (i.e. a contacts app can have a new
    /// channel for each contact and the UI needs to pop up on the right tile). The expansion job
    /// generically has one _Installation field it passes to device-specific code, so we store a map
    /// of tag -> channel URI. Right now, there is only one valid tag and it is automatic.
    /// Unused variable warnings are suppressed because this const is used in WinRT and WinPhone but not NetFx.
    /// </summary>
    static readonly string toastChannelTag = "_Toast";

    private static Lazy<Task<HttpNotificationChannel>> getToastChannelTask = new Lazy<Task<HttpNotificationChannel>>(() =>
      Task.Run(() => {
        try {
          HttpNotificationChannel toastChannel = HttpNotificationChannel.Find(toastChannelTag);
          if (toastChannel == null) {
            toastChannel = new HttpNotificationChannel(toastChannelTag);

            // Note: We could bind to the ChannelUriUpdated event & automatically save instead of checking
            // whether the channel has changed on demand. This is more seamless but adds API requests for a
            // feature that may not be in use. Maybe we should build an auto-update feature in the future?
            // Or maybe Push.subscribe calls will always be a save & that should be good enough for us.
            toastChannel.Open();
          }

          // You cannot call BindToShellToast willy nilly across restarts. This was somehow built in a non-idempotent way.
          if (!toastChannel.IsShellToastBound) {
            toastChannel.BindToShellToast();
          }
          return toastChannel;

          // If the app manifest does not declare ID_CAP_PUSH_NOTIFICATION
        } catch (UnauthorizedAccessException) {
          return null;
        }
      })
    );

    private static Lazy<Task<string>> getToastUriTask = new Lazy<Task<string>>(async () => {
      var channel = await getToastChannelTask.Value;
      if (channel == null) {
        return null;
      }

      var source = new TaskCompletionSource<string>();
      EventHandler<NotificationChannelUriEventArgs> handler = null;
      EventHandler<NotificationChannelErrorEventArgs> errorHandler = null;
      handler = (sender, args) => {
        // Prevent NullReferenceException
        if (args.ChannelUri == null) {
          source.TrySetResult(null);
        } else {
          source.TrySetResult(args.ChannelUri.AbsoluteUri);
        }
      };
      errorHandler = (sender, args) => {
        source.TrySetException(new ApplicationException(args.Message));
      };

      channel.ChannelUriUpdated += handler;
      channel.ErrorOccurred += errorHandler;

      // Sometimes the channel isn't ready yet. Sometimes it is.
      if (channel.ChannelUri != null && !source.Task.IsCompleted) {
        source.TrySetResult(channel.ChannelUri.AbsoluteUri);
      }

      return await source.Task.ContinueWith(t => {
        // Cleanup the handler.
        channel.ChannelUriUpdated -= handler;
        channel.ErrorOccurred -= errorHandler;

        return t;
      }).Unwrap();
    });

    internal static Task<HttpNotificationChannel> GetToastChannelTask {
      get {
        return getToastChannelTask.Value;
      }
    }

    static DeviceInfoController() {
      var _ = GetToastChannelTask;
    }
  }
}