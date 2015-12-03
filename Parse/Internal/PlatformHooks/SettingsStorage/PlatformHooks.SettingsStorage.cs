// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Internal;
using Parse.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Parse {
  partial class PlatformHooks : IPlatformHooks {
    private static readonly IDictionary<string, object> settings;
    static PlatformHooks() {
      try {
        settings = SettingsWrapper.Wrapper;
      } catch (ConfigurationException) {
        settings = new Dictionary<string, object>();
      }
    }
    /// <summary>
    /// Wraps the custom settings object for Parse so that it can be exposed as ApplicationSettings.
    /// </summary>
    private class SettingsWrapper : IDictionary<string, object> {
      private static SettingsWrapper wrapper;
      public static SettingsWrapper Wrapper {
        get {
          wrapper = wrapper ?? new SettingsWrapper();
          return wrapper;
        }
      }
      private readonly IDictionary<string, object> data;
      private SettingsWrapper() {
        if (string.IsNullOrEmpty(Settings.Default.ApplicationSettings)) {
          data = new Dictionary<string, object>();
          Save();
        } else {
          data = ParseClient.DeserializeJsonString(Settings.Default.ApplicationSettings);
        }
      }
      private void Save() {
        Settings.Default.ApplicationSettings = ParseClient.SerializeJsonString(data);
        Settings.Default.Save();
      }
      public void Add(string key, object value) {
        data.Add(key, value);
        Save();
      }

      public bool ContainsKey(string key) {
        return data.ContainsKey(key);
      }

      public ICollection<string> Keys {
        get { return data.Keys; }
      }

      public bool Remove(string key) {
        if (data.Remove(key)) {
          Save();
          return true;
        }
        return false;
      }

      public bool TryGetValue(string key, out object value) {
        return data.TryGetValue(key, out value);
      }

      public ICollection<object> Values {
        get { return data.Values; }
      }

      public object this[string key] {
        get {
          return data[key];
        }
        set {
          data[key] = value;
          Save();
        }
      }

      public void Add(KeyValuePair<string, object> item) {
        data.Add(item);
        Save();
      }

      public void Clear() {
        data.Clear();
        Save();
      }

      public bool Contains(KeyValuePair<string, object> item) {
        return data.Contains(item);
      }

      public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
        data.CopyTo(array, arrayIndex);
      }

      public int Count {
        get { return data.Count; }
      }

      public bool IsReadOnly {
        get { return data.IsReadOnly; }
      }

      public bool Remove(KeyValuePair<string, object> item) {
        if (data.Remove(item)) {
          Save();
          return true;
        }
        return false;
      }

      public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
        return data.GetEnumerator();
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return data.GetEnumerator();
      }
    }

    /// <summary>
    /// Provides a dictionary that gets persisted on the filesystem between runs of the app.
    /// This is analogous to NSUserDefaults in iOS.
    /// </summary>
    public IDictionary<string, object> ApplicationSettings {
      get {
        return settings;
      }
    }
  }
}
