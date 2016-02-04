// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Common.Internal {
  public class LockSet {
    private static readonly ConditionalWeakTable<object, IComparable> stableIds =
        new ConditionalWeakTable<object, IComparable>();
    private static long nextStableId = 0;

    private readonly IEnumerable<object> mutexes;

    public LockSet(IEnumerable<object> mutexes) {
      this.mutexes = (from mutex in mutexes
                      orderby GetStableId(mutex)
                      select mutex).ToList();
    }

    public void Enter() {
      foreach (var mutex in mutexes) {
        Monitor.Enter(mutex);
      }
    }

    public void Exit() {
      foreach (var mutex in mutexes) {
        Monitor.Exit(mutex);
      }
    }

    private static IComparable GetStableId(object mutex) {
      lock (stableIds) {
        return stableIds.GetValue(mutex, k => nextStableId++);
      }
    }
  }
}
