using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AssemblyLister;

namespace Parse.Common.Internal
{
    /// <summary>
    /// The class which controls the loading of other ParseModules
    /// </summary>
    public class ParseModuleController
    {
        public static ParseModuleController Instance { get; } = new ParseModuleController { };

        object Mutex { get; } = new object { };

        List<IParseModule> Modules { get; } = new List<IParseModule> { };

        bool Initialized { get; set; } = false;

        public void RegisterModule(IParseModule module)
        {
            if (module == null)
            {
                return;
            }

            lock (Mutex)
            {
                Modules.Add(module);
                module.ExecuteModuleRegistrationHook();

                if (Initialized)
                {
                    module.ExecuteLibraryInitializationHook();
                }
            }
        }

        public void ScanForModules()
        {
            IEnumerable<Type> moduleTypes = Lister.AllAssemblies.SelectMany(asm => asm.GetCustomAttributes<ParseModuleAttribute>()).Select(attr => attr.ModuleType).Where(type => type != null && type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IParseModule)));

            lock (Mutex)
            {
                foreach (Type moduleType in moduleTypes)
                {
                    try
                    {
                        if (moduleType.FindConstructor() is { } constructor)
                        {
                            RegisterModule(constructor.Invoke(new object[] { }) as IParseModule);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore, either constructor threw or was private.
                    }
                }
            }
        }

        public void Reset()
        {
            lock (Mutex)
            {
                Modules.Clear();
                Initialized = false;
            }
        }

        public void BroadcastParseInitialization()
        {
            lock (Mutex)
            {
                foreach (IParseModule module in Modules)
                {
                    module.ExecuteLibraryInitializationHook();
                }

                Initialized = true;
            }
        }
    }
}