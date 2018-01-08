// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Common.Internal {
  /// <summary>
  /// Represents an event handler that calls back from the synchronization context
  /// that subscribed.
  /// <typeparam name="T">Should look like an EventArgs, but may not inherit EventArgs if T is implemented by the Windows team.</typeparam>
  /// </summary>
  public class SynchronizedEventHandler<T> {
    private LinkedList<Tuple<Delegate, TaskFactory>> delegates =
        new LinkedList<Tuple<Delegate, TaskFactory>>();
    public void Add(Delegate del) {
      lock (delegates) {
        TaskFactory factory;
        if (SynchronizationContext.Current != null) {
          factory =
              new TaskFactory(CancellationToken.None,
                  TaskCreationOptions.None,
                  TaskContinuationOptions.ExecuteSynchronously,
                  TaskScheduler.FromCurrentSynchronizationContext());
        } else {
          factory = Task.Factory;
        }
        foreach (var d in del.GetInvocationList()) {
          delegates.AddLast(new Tuple<Delegate, TaskFactory>(d, factory));
        }
      }
    }

    public void Remove(Delegate del) {
      lock (delegates) {
        if (delegates.Count == 0) {
          return;
        }
        foreach (var d in del.GetInvocationList()) {
          var node = delegates.First;
          while (node != null) {
            if (node.Value.Item1 == d) {
              delegates.Remove(node);
              break;
            }
            node = node.Next;
          }
        }
      }
    }

    public Task Invoke(object sender, T args) {
      IEnumerable<Tuple<Delegate, TaskFactory>> toInvoke;
      var toContinue = new[] { Task.FromResult(0) };
      lock (delegates) {
        toInvoke = delegates.ToList();
      }
      var invocations = toInvoke
          .Select(p => p.Item2.ContinueWhenAll(toContinue,
              _ => p.Item1.DynamicInvoke(sender, args)))
          .ToList();
      return Task.WhenAll(invocations);
    }
  }
}
