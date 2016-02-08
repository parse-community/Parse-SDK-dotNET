using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AssemblyLister {
  /// <summary>
  /// A class that lets you list all loaded assemblies in a PCL-compliant way.
  /// </summary>
  public static class Lister {
    /// <summary>
    /// Get all of the assemblies used by this application.
    /// </summary>
    public static IEnumerable<Assembly> AllAssemblies {
      get {
         throw new Exception("Cannot use the portable version of AssemblyLister in an end-program. Please add a reference to AssemblyLister via NuGet to your final Application.");
      }
    }
  }
}
