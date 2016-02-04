using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

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
        Task<List<Assembly>> assembliesTask = Task.Run(async () => {
          List<Assembly> results = new List<Assembly>();

          var folder = Package.Current.InstalledLocation;
          foreach (var file in await folder.GetFilesAsync().AsTask().ConfigureAwait(false)) {
            if (file.FileType == ".dll") {
              var assemblyName = new AssemblyName(file.DisplayName);

              try {
                var assembly = Assembly.Load(assemblyName);
                results.Add(assembly);
              } catch (Exception) {
                // Ignore...
              }
            }
          }
         return results;
        });

        // While we asynchronously load, give back the one assembly we know to exist (i.e. the one with the main application).
        var currentAssembly = Application.Current.GetType().GetTypeInfo().Assembly;
        yield return currentAssembly;

        assembliesTask.Wait();
        foreach (Assembly asm in assembliesTask.Result) {
          yield return asm;
        }
      }
    }
  }
}
