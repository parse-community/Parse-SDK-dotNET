using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Runtime.ExceptionServices {
  internal class ExceptionDispatchInfo {
    public static ExceptionDispatchInfo Capture(Exception ex) {
      return new ExceptionDispatchInfo(ex);
    }
    private ExceptionDispatchInfo(Exception ex) {
      SourceException = ex;
    }
    public Exception SourceException {
      get;
      private set;
    }
    public void Throw() {
      throw SourceException;
    }
  }
}
