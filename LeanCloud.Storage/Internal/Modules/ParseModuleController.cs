using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AssemblyLister;

namespace LeanCloud.Storage.Internal {
  /// <summary>
  /// The class which controls the loading of other ParseModules
  /// </summary>
  public class AVModuleController {
    private static readonly AVModuleController instance = new AVModuleController();
    public static AVModuleController Instance {
      get { return instance; }
    }

    private readonly object mutex = new object();
    private readonly List<IAVModule> modules = new List<IAVModule>();

    private bool isParseInitialized = false;

    public void RegisterModule(IAVModule module) {
      if (module == null) {
        return;
      }

      lock (mutex) {
        modules.Add(module);
        module.OnModuleRegistered();

        if (isParseInitialized) {
          module.OnParseInitialized();
        }
      }
    }

    public void ScanForModules() {
      var moduleTypes = Lister.AllAssemblies
        .SelectMany(asm => asm.GetCustomAttributes<AVModuleAttribute>())
        .Select(attr => attr.ModuleType)
        .Where(type => type != null && type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IAVModule)));

      lock (mutex) {
        foreach (Type moduleType in moduleTypes) {
          try {
            ConstructorInfo constructor = moduleType.FindConstructor();
            if (constructor != null) {
              var module = constructor.Invoke(new object[] {}) as IAVModule;
              RegisterModule(module);
            }
          } catch (Exception) {
            // Ignore, either constructor threw or was private.
          }
        }
      }
    }

    public void Reset() {
      lock (mutex) {
        modules.Clear();
        isParseInitialized = false;
      }
    }

    public void LeanCloudDidInitialize() {
      lock (mutex) {
        foreach (IAVModule module in modules) {
          module.OnParseInitialized();
        }
        isParseInitialized = true;
      }
    }
  }
}