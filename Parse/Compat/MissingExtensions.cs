using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Internal {
  internal static class MissingExtensions {
    public static Type GetTypeInfo(this Type t) {
      return t;
    }

    public static bool HasFlag(this Enum enumValue, Enum flag) {
      var eInt = Convert.ToInt64(enumValue);
      var flagInt = Convert.ToInt64(flag);
      return (eInt & flagInt) == flagInt;
    }

    internal static T GetCustomAttribute<T>(this PropertyInfo prop, bool inherit)
        where T : Attribute {
      return (T)prop.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
    }

    internal static T GetCustomAttribute<T>(this PropertyInfo prop) where T : Attribute {
      return prop.GetCustomAttribute<T>(true);
    }

    internal static T GetCustomAttribute<T>(this Type type, bool inherit) where T : Attribute {
      return (T)type.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
    }

    internal static T GetCustomAttribute<T>(this Type type) where T : Attribute {
      return type.GetCustomAttribute<T>(true);
    }

    internal static Task<string> ReadToEndAsync(this StreamReader reader) {
      return Task.Run(() => reader.ReadToEnd());
    }

    internal static Task CopyToAsync(this Stream stream, Stream destination) {
      return stream.CopyToAsync(destination, 2048, CancellationToken.None);
    }

    internal static Task CopyToAsync(this Stream stream,
        Stream destination,
        int bufferSize,
        CancellationToken cancellationToken) {
      byte[] buffer = new byte[bufferSize];
      int bytesRead = 0;
      return InternalExtensions.WhileAsync(() => {
        return stream.ReadAsync(buffer, 0, bufferSize, cancellationToken).OnSuccess(readTask => {
          bytesRead = readTask.Result;
          return bytesRead > 0;
        });
      }, () => {
        cancellationToken.ThrowIfCancellationRequested();
        return destination.WriteAsync(buffer, 0, bytesRead, cancellationToken)
          .OnSuccess(_ => cancellationToken.ThrowIfCancellationRequested());
      });
    }

    internal static Task<int> ReadAsync(this Stream stream,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) {
      if (cancellationToken.IsCancellationRequested) {
        var tcs = new TaskCompletionSource<int>();
        tcs.SetCanceled();
        return tcs.Task;
      }
      return Task.Factory.FromAsync<byte[], int, int, int>(stream.BeginRead,
          stream.EndRead,
            buffer,
            offset,
            count,
            null);
    }

    internal static Task WriteAsync(this Stream stream,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) {
      if (cancellationToken.IsCancellationRequested) {
        var tcs = new TaskCompletionSource<object>();
        tcs.SetCanceled();
        return tcs.Task;
      }
      return Task.Factory.FromAsync<byte[], int, int>(stream.BeginWrite,
          stream.EndWrite,
          buffer,
          offset,
          count,
          null);
    }

    internal static IEnumerable<TResult> Zip<T1, T2, TResult>(this IEnumerable<T1> list1,
        IEnumerable<T2> list2,
        Func<T1, T2, TResult> zipper) {
      var e1 = list1.GetEnumerator();
      var e2 = list2.GetEnumerator();
      while (e1.MoveNext() && e2.MoveNext()) {
        yield return zipper(e1.Current, e2.Current);
      }
    }
  }
}
