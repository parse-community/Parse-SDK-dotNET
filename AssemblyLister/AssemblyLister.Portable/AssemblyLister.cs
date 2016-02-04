using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AssemblyLister {
  public static class Lister {
    public static IEnumerable<Assembly> AllAssemblies {
      get {
         throw new Exception("Cannot use the portable version of AssemblyLister in an end-program. Please add a reference to AssemblyLister via NuGet to your final Application.");
      }
    }
  }
}
