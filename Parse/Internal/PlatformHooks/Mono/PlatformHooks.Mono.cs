// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.IO;

using Parse.Internal;

namespace Parse {
  static class MonoHelpers {
    static Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.Factory.FromAsync(
          (callback, obj) => stream.BeginRead(buffer, offset, count, callback, obj),
          (Func<IAsyncResult, int>)stream.EndRead,
          TaskCreationOptions.None);
    }
  }

  partial class PlatformHooks : IPlatformHooks {
    /// <summary>
    /// Wraps an IsolatedStorageFile as an object for Parse so that it can be exposed as ApplicationSettings.
    /// </summary>
    private class SettingsWrapper : IDictionary<string, object> {
      private static readonly IsolatedStorageFile isolatedStore = IsolatedStorageFile.GetUserStoreForAssembly();
      private object mutex = new object();
      private readonly IDictionary<string, object> data;
      private readonly string fileName;
      internal SettingsWrapper(string fileName) {
        this.fileName = fileName;
        if (!isolatedStore.FileExists(fileName)) {
          data = new Dictionary<string, object>();
          Save();
        } else {
          data = ParseClient.DeserializeJsonString(ReadFile()) ?? new Dictionary<string, object>();
        }
      }
      private string ReadFile() {
        lock (mutex) {
          using (var reader = new StreamReader(isolatedStore.OpenFile(fileName, FileMode.Open, FileAccess.Read))) {
            return reader.ReadToEnd();
          }
        }
      }
      private void Save() {
        lock (mutex) {
          using (var writer = new StreamWriter(isolatedStore.OpenFile(fileName, FileMode.Create, FileAccess.Write))) {
            writer.Write(ParseClient.SerializeJsonString(data));
          }
        }
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

    private readonly Lazy<SettingsWrapper> settings =
        new Lazy<SettingsWrapper>(() => new SettingsWrapper("ApplicationSettings"), true);
    public IDictionary<string, object> ApplicationSettings {
      get { return settings.Value; }
    }
  }
}
