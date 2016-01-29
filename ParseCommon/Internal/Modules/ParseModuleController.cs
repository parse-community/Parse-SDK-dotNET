using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AssemblyLister;

namespace Parse.Common.Internal {
  /// <summary>
  /// The class which controls the loading of other ParseModules
  /// </summary>
  public class ParseModuleController {
    private static readonly ParseModuleController instance = new ParseModuleController();
    public static ParseModuleController Instance {
      get { return instance; }
    }

    private readonly object mutex = new object();
    private readonly List<IParseModule> modules = new List<IParseModule>();

    private bool isParseInitialized = false;

    public void RegisterModule(IParseModule module) {
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
        .SelectMany(asm => asm.GetCustomAttributes<ParseModuleAttribute>())
        .Select(attr => attr.ModuleType)
        .Where(type => type != null && type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IParseModule)));

      lock (mutex) {
        foreach (Type moduleType in moduleTypes) {
          try {
            ConstructorInfo constructor = moduleType.FindConstructor();
            if (constructor != null) {
              var module = constructor.Invoke(new object[] {}) as IParseModule;
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

    public void ParseDidInitialize() {
      lock (mutex) {
        foreach (IParseModule module in modules) {
          module.OnParseInitialized();
        }
        isParseInitialized = true;
      }
    }
  }
}