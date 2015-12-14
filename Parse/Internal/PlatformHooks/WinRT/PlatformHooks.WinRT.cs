// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Networking.PushNotifications;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Parse {
  partial class PlatformHooks : IPlatformHooks {
    /// <summary>
    /// Future proofing: Right now there's only one valid channel for the app, but we will likely
    /// want to allow additional channels for auxiliary tiles (i.e. a contacts app can have a new
    /// channel for each contact and the UI needs to pop up on the right tile). The expansion job
    /// generically has one _Installation field it passes to device-specific code, so we store a map
    /// of tag -> channel URI. Right now, there is only one valid tag and it is automatic.
    /// Unused variable warnings are suppressed because this const is used in WinRT and WinPhone but not NetFx.
    /// </summary>
    private static readonly string defaultChannelTag = "_Default";

    // This must be wrapped in a property so other classes may continue on this task
    // during their static initialization.
    private static Lazy<Task<PushNotificationChannel>> getChannelTask = new Lazy<Task<PushNotificationChannel>>(() =>
      PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync().AsTask()
    );
    internal static Task<PushNotificationChannel> GetChannelTask {
      get {
        return getChannelTask.Value;
      }
    }

    static PlatformHooks() {
      var _ = GetChannelTask;
    }

    private IHttpClient httpClient = null;
    public IHttpClient HttpClient {
      get {
        httpClient = httpClient ?? new HttpClient();
        return httpClient;
      }
    }

    public string SDKName {
      get {
        return "winrt";
      }
    }

    public string AppName {
      get {
        var task = Package.Current.InstalledLocation.GetFileAsync("AppxManifest.xml").AsTask().OnSuccess(t => {
          return FileIO.ReadTextAsync(t.Result).AsTask();
        }).Unwrap().OnSuccess(t => {
          var doc = XDocument.Parse(t.Result);

          // Define the default namespace to be used
          var propertiesXName = XName.Get("Properties", "http://schemas.microsoft.com/appx/2010/manifest");
          var displayNameXName = XName.Get("DisplayName", "http://schemas.microsoft.com/appx/2010/manifest");

          return doc.Descendants(propertiesXName).Single().Descendants(displayNameXName).Single().Value;
        });
        task.Wait();
        return task.Result;
      }
    }

    public string AppBuildVersion {
      get {
        var version = Package.Current.Id.Version;
        return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
      }
    }

    public string AppDisplayVersion {
      get {
        return AppBuildVersion;
      }
    }

    public string AppIdentifier {
      get {
        return Package.Current.Id.Name;
      }
    }

    public string OSVersion {
      get {
        // It's impossible to do with WinRT.
        return "";
      }
    }

    public string DeviceType {
      get {
        return "winrt";
      }
    }

    public string DeviceTimeZone {
      get {
        string windowsName = TimeZoneInfo.Local.StandardName;
        if (ParseInstallation.TimeZoneNameMap.ContainsKey(windowsName)) {
          return ParseInstallation.TimeZoneNameMap[windowsName];
        } else {
          return null;
        }
      }
    }

    public void Initialize() {
      // Do nothing.
    }

    public Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation) {
      return GetChannelTask.ContinueWith(t => {
        installation.SetIfDifferent("deviceUris", new Dictionary<string, string> {
          { defaultChannelTag, t.Result.Uri }
        });
      });
    }

    /// <summary>
    /// Wraps the LocalSettings for Parse so that large strings can be stored in multiple keys.
    /// It accomplishes this by adding a __Count version of the field and then splits the value
    /// across __### fields.
    /// </summary>
    private class SettingsWrapper : IDictionary<string, object> {
      private static readonly object mutex = new object();
      private static readonly string Delimiter = "__";
      private static readonly string CountSuffix = Delimiter + "Count";
      private const int ShardSize = 1024;
      private static SettingsWrapper wrapper;
      public static SettingsWrapper Wrapper {
        get {
          lock (mutex) {
            wrapper = wrapper ?? new SettingsWrapper();
            return wrapper;
          }
        }
      }

      private readonly IDictionary<string, object> data;
      private SettingsWrapper() {
        var container = ApplicationData.Current.LocalSettings
            .CreateContainer("Parse", ApplicationDataCreateDisposition.Always);
        data = container.Values;
      }

      public void Add(string key, object value) {
        lock (mutex) {
          if (ContainsKey(key)) {
            throw new ArgumentException("Key already exists in dictionary.", "key");
          }
          this[key] = value;
        }
      }

      public bool ContainsKey(string key) {
        lock (mutex) {
          return data.ContainsKey(key) || data.ContainsKey(key + CountSuffix);
        }
      }

      public ICollection<string> Keys {
        get {
          lock (mutex) {
            return (from k in data.Keys
                    where k.EndsWith(CountSuffix) || !k.Contains(Delimiter)
                    select k.Split(new string[] { Delimiter },
                        1, StringSplitOptions.None)[0]).ToList();
          }
        }
      }

      public bool Remove(string key) {
        lock (mutex) {
          var suffixed = key + CountSuffix;
          if (data.ContainsKey(suffixed)) {
            int count = (int)data[suffixed];
            data.Remove(suffixed);
            var delimitedKey = key + Delimiter;
            for (int i = 0; i < count; i++) {
              data.Remove(delimitedKey + i);
            }
            return true;
          }
          if (data.Remove(key)) {
            return true;
          }
          return false;
        }
      }

      public bool TryGetValue(string key, out object value) {
        lock (mutex) {
          var suffixed = key + CountSuffix;
          if (data.ContainsKey(suffixed)) {
            // Reassemble the sharded string.
            int count = (int)data[suffixed];
            var builder = new StringBuilder(count * ShardSize);
            var delimitedKey = key + Delimiter;
            for (int i = 0; i < count; i++) {
              object shard;
              if (!data.TryGetValue(delimitedKey + i, out shard)) {
                value = null;
                return false;
              }
              builder.Append((string)shard);
            }
            value = builder.ToString();
            return true;
          }
          return data.TryGetValue(key, out value);
        }
      }

      public ICollection<object> Values {
        get {
          lock (mutex) {
            return (from k in Keys
                    select this[k]).ToList();
          }
        }
      }

      public object this[string key] {
        get {
          object value;
          if (TryGetValue(key, out value)) {
            return value;
          }
          throw new IndexOutOfRangeException();
        }
        set {
          lock (mutex) {
            this.Remove(key);
            // If the value is a large string (> 1k characters), split it into multiple shards.
            var stringValue = value as string;
            if (stringValue != null && stringValue.Length > ShardSize) {
              var delimitedKey = key + Delimiter;
              int count = 0;
              for (int start = 0; start < stringValue.Length; start += ShardSize) {
                string shard = stringValue.Substring(start,
                    Math.Min(ShardSize, stringValue.Length - start));
                data[delimitedKey + count] = shard;
                count++;
              }
              data[key + CountSuffix] = count;
            } else {
              data[key] = value;
            }
          }
        }
      }

      public void Add(KeyValuePair<string, object> item) {
        this.Add(item.Key, item.Value);
      }

      public void Clear() {
        lock (mutex) {
          data.Clear();
        }
      }

      public bool Contains(KeyValuePair<string, object> item) {
        lock (mutex) {
          return this.ContainsKey(item.Key) && this[item.Key] == item.Value;
        }
      }

      public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
        this.ToList().CopyTo(array, arrayIndex);
      }

      public int Count {
        get { return Keys.Count; }
      }

      public bool IsReadOnly {
        get { return false; }
      }

      public bool Remove(KeyValuePair<string, object> item) {
        lock (mutex) {
          if (!this.Contains(item)) {
            return false;
          }
          return this.Remove(item.Key);
        }
      }

      public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
        return (from k in this.Keys
                select new KeyValuePair<string, object>(k, this[k])).GetEnumerator();
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
      }
    }
    public IDictionary<string, object> ApplicationSettings {
      get {
        return SettingsWrapper.Wrapper;
      }
    }
  }
}
