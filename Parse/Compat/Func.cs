using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System {
  internal delegate TResult Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(
      TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);
}
