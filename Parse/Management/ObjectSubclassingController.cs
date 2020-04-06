using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Parse.Common.Internal;

namespace Parse.Core.Internal
{
    internal class ObjectSubclassingController : IParseObjectClassController
    {
        // Class names starting with _ are documented to be reserved. Use this one here to allow us to "inherit" certain properties.
        static string ReservedParseObjectClassName { get; } = "_ParseObject";

        ReaderWriterLockSlim Mutex { get; } = new ReaderWriterLockSlim { };

        IDictionary<string, ObjectSubclassInfo> RegisteredSubclasses { get; } = new Dictionary<string, ObjectSubclassInfo> { };

        Dictionary<string, Action> RegisterActions { get; set; } = new Dictionary<string, Action> { };

        public ObjectSubclassingController() => AddValid(typeof(ParseObject));

        public string GetClassName(Type type) => type == typeof(ParseObject) ? ReservedParseObjectClassName : type.GetParseClassName();

        public Type GetType(string className)
        {
            Mutex.EnterReadLock();
            RegisteredSubclasses.TryGetValue(className, out ObjectSubclassInfo info);
            Mutex.ExitReadLock();

            return info?.TypeInfo.AsType();
        }

        public bool GetClassMatch(string className, Type type)
        {
            Mutex.EnterReadLock();
            RegisteredSubclasses.TryGetValue(className, out ObjectSubclassInfo subclassInfo);
            Mutex.ExitReadLock();

            return subclassInfo == null ? type == typeof(ParseObject) : subclassInfo.TypeInfo == type.GetTypeInfo();
        }

        public void AddValid(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();

            if (!typeof(ParseObject).GetTypeInfo().IsAssignableFrom(typeInfo))
            {
                throw new ArgumentException("Cannot register a type that is not a subclass of ParseObject");
            }

            string className = GetClassName(type);

            try
            {
                // Perform this as a single independent transaction, so we can never get into an
                // intermediate state where we *theoretically* register the wrong class due to a
                // TOCTTOU bug.

                Mutex.EnterWriteLock();

                if (RegisteredSubclasses.TryGetValue(className, out ObjectSubclassInfo previousInfo))
                {
                    if (typeInfo.IsAssignableFrom(previousInfo.TypeInfo))
                    {
                        // Previous subclass is more specific or equal to the current type, do nothing.
                        return;
                    }
                    else if (previousInfo.TypeInfo.IsAssignableFrom(typeInfo))
                    {
                        // Previous subclass is parent of new child, fallthrough and actually register
                        // this class.
                        /* Do nothing */
                    }
                    else
                    {
                        throw new ArgumentException($"Tried to register both {previousInfo.TypeInfo.FullName} and {typeInfo.FullName} as the ParseObject subclass of {className}. Cannot determine the right class to use because neither inherits from the other.");
                    }
                }

                ConstructorInfo constructor = type.FindConstructor();

                if (constructor == null)
                {
                    throw new ArgumentException("Cannot register a type that does not implement the default constructor!");
                }

                RegisteredSubclasses[className] = new ObjectSubclassInfo(type, constructor);
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
            RegisteredSubclasses.Remove(GetClassName(type));
            Mutex.ExitWriteLock();
        }

        public void AddRegisterHook(Type t, Action action)
        {
            Mutex.EnterWriteLock();
            RegisterActions.Add(GetClassName(t), action);
            Mutex.ExitWriteLock();
        }

        public ParseObject Instantiate(string className)
        {
            Mutex.EnterReadLock();
            RegisteredSubclasses.TryGetValue(className, out ObjectSubclassInfo info);
            Mutex.ExitReadLock();

            return info is { } ? info.Instantiate() : new ParseObject(className);
        }

        public IDictionary<string, string> GetPropertyMappings(string className)
        {
            Mutex.EnterReadLock();
            RegisteredSubclasses.TryGetValue(className, out ObjectSubclassInfo info);

            if (info is null)
            {
                RegisteredSubclasses.TryGetValue(ReservedParseObjectClassName, out info);
            }

            Mutex.ExitReadLock();
            return info.PropertyMappings;
        }

        // ALTERNATE NAME: AddObject, AddType, AcknowledgeType, CatalogType

        public void AddIntrinsic()
        {
            AddValid(typeof(ParseUser));
            AddValid(typeof(ParseRole));
            AddValid(typeof(ParseSession));
            AddValid(typeof(ParseInstallation));
        }
    }
}
