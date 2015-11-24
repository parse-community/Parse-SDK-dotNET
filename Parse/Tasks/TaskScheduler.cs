using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.Threading.Tasks {
  internal class TaskScheduler {
    private static SynchronizationContext defaultContext = new SynchronizationContext();
    private SynchronizationContext context;
    public TaskScheduler(SynchronizationContext context) {
      this.context = context ?? defaultContext;
    }

    public void Post(Action action) {
      context.Post(o => action(), null);
    }

    public static TaskScheduler FromCurrentSynchronizationContext() {
      return new TaskScheduler(SynchronizationContext.Current);
    }
  }
}
