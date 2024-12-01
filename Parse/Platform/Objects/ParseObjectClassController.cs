using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Objects
{
    internal class ParseObjectClassController : IParseObjectClassController
    {
        // Class names starting with _ are documented to be reserved. Use this one here to allow us to "inherit" certain properties.
        static string ReservedParseObjectClassName { get; } = "_ParseObject";

        ReaderWriterLockSlim Mutex { get; } = new ReaderWriterLockSlim { };

        IDictionary<string, ParseObjectClass> Classes { get; } = new Dictionary<string, ParseObjectClass> { };

        Dictionary<string, Action> RegisterActions { get; set; } = new Dictionary<string, Action> { };

        public ParseObjectClassController() => AddValid(typeof(ParseObject));

        public string GetClassName(Type type)
        {
            return type == typeof(ParseObject) ? ReservedParseObjectClassName : type.GetParseClassName();
        }

        public Type GetType(string className)
        {
            Mutex.EnterReadLock();
            Classes.TryGetValue(className, out ParseObjectClass info);
            Mutex.ExitReadLock();

            return info?.TypeInfo.AsType();
        }

        public bool GetClassMatch(string className, Type type)
        {
            Mutex.EnterReadLock();
            Classes.TryGetValue(className, out ParseObjectClass subclassInfo);
            Mutex.ExitReadLock();

            return subclassInfo is { } ? subclassInfo.TypeInfo == type.GetTypeInfo() : type == typeof(ParseObject);
        }

        public void AddValid(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();

            if (!typeof(ParseObject).GetTypeInfo().IsAssignableFrom(typeInfo))
                throw new ArgumentException("Cannot register a type that is not a subclass of ParseObject");

            string className = GetClassName(type);

            try
            {
                // Perform this as a single independent transaction, so we can never get into an
                // intermediate state where we *theoretically* register the wrong class due to a
                // TOCTTOU bug.

                Mutex.EnterWriteLock();

                if (Classes.TryGetValue(className, out ParseObjectClass previousInfo))
                    if (typeInfo.IsAssignableFrom(previousInfo.TypeInfo))
                        // Previous subclass is more specific or equal to the current type, do nothing.

                        return;
                    else if (previousInfo.TypeInfo.IsAssignableFrom(typeInfo))
                    {
                        // Previous subclass is parent of new child, fallthrough and actually register this class.
                        /* Do nothing */
                    }
                    else
                        throw new ArgumentException($"Tried to register both {previousInfo.TypeInfo.FullName} and {typeInfo.FullName} as the ParseObject subclass of {className}. Cannot determine the right class to use because neither inherits from the other.");

#pragma warning disable CS1030 // #warning directive
#warning Constructor detection may erroneously find a constructor which should not be used.

                ConstructorInfo constructor = type.FindConstructor() ?? type.FindConstructor(typeof(string), typeof(IServiceHub));
#pragma warning restore CS1030 // #warning directive

                if (constructor is null)
                    throw new ArgumentException("Cannot register a type that does not implement the default constructor!");

                Classes[className] = new ParseObjectClass(type, constructor);
            }
            finally
            {
                Mutex.ExitWriteLock();
            }

            Mutex.EnterReadLock();
            RegisterActions.TryGetValue(className, out Action toPerform);
            Mutex.ExitReadLock();

            toPerform?.Invoke();
        }

        public void RemoveClass(Type type)
        {
            Mutex.EnterWriteLock();
            Classes.Remove(GetClassName(type));
            Mutex.ExitWriteLock();
        }

        public void AddRegisterHook(Type type, Action action)
        {
            Mutex.EnterWriteLock();
            RegisterActions.Add(GetClassName(type), action);
            Mutex.ExitWriteLock();
        }

        public ParseObject Instantiate(string className, IServiceHub serviceHub)
        {
            Mutex.EnterReadLock();
            Classes.TryGetValue(className, out ParseObjectClass info);
            Mutex.ExitReadLock();

            if (info is { })
            {
                var obj = info.Instantiate().Bind(serviceHub);
                return obj;

            }
            else
            {

                return  new ParseObject(className, serviceHub);

            }
        }

        public IDictionary<string, string> GetPropertyMappings(string className)
        {
            Mutex.EnterReadLock();
            Classes.TryGetValue(className, out ParseObjectClass info);

            if (info is null)
                Classes.TryGetValue(ReservedParseObjectClassName, out info);

            Mutex.ExitReadLock();
            return info.PropertyMappings;
        }

        bool SDKClassesAdded { get; set; }

        // ALTERNATE NAME: AddObject, AddType, AcknowledgeType, CatalogType

        public void AddIntrinsic()
        {
            if (!(SDKClassesAdded, SDKClassesAdded = true).SDKClassesAdded)
            {
                AddValid(typeof(ParseUser));
                AddValid(typeof(ParseRole));
                AddValid(typeof(ParseSession));
                AddValid(typeof(ParseInstallation));
            }
        }
    }
}
