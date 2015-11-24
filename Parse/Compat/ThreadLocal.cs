using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.Threading {
  internal class ThreadLocal<T> : IDisposable {
    private static long lastId = -1;
    [ThreadStatic]
    private static IDictionary<long, T> threadLocalData;
    private static IList<WeakReference> allDataDictionaries = new List<WeakReference>();

    private static IDictionary<long, T> ThreadLocalData {
      get {
        if (threadLocalData == null) {
          threadLocalData = new Dictionary<long, T>();
          lock (allDataDictionaries) {
            allDataDictionaries.Add(new WeakReference(threadLocalData));
          }
        }
        return threadLocalData;
      }
    }

    private bool disposed = false;
    private readonly long id;
    private readonly Func<T> valueFactory;

    public ThreadLocal()
      : this(() => default(T)) {
    }

    public ThreadLocal(Func<T> valueFactory) {
      this.valueFactory = valueFactory;
      id = Interlocked.Increment(ref lastId);
    }

    public T Value {
      get {
        CheckDisposed();
        T result;
        if (ThreadLocalData.TryGetValue(id, out result)) {
          return result;
        }
        return ThreadLocalData[id] = valueFactory();
      }
      set {
        CheckDisposed();
        ThreadLocalData[id] = value;
      }
    }

    ~ThreadLocal() {
      if (!disposed) {
        Dispose();
      }
    }

    private void CheckDisposed() {
      if (disposed) {
        throw new ObjectDisposedException("ThreadLocal has been disposed.");
      }
    }

    public void Dispose() {
      lock (allDataDictionaries) {
        for (int i = 0; i < allDataDictionaries.Count; i++) {
          var data = allDataDictionaries[i].Target as IDictionary<object, T>;
          if (data == null) {
            allDataDictionaries.RemoveAt(i);
            i--;
            continue;
          }
          data.Remove(id);
        }
      }
      disposed = true;
    }
  }
}
