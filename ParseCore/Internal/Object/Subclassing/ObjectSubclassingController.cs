using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  internal class ObjectSubclassingController : IObjectSubclassingController {
    // Class names starting with _ are documented to be reserved. Use this one
    // here to allow us to 'inherit' certain properties.
    private static readonly string parseObjectClassName = "_ParseObject";

    private readonly ReaderWriterLockSlim mutex;
    private readonly IDictionary<String, ObjectSubclassInfo> registeredSubclasses;
    private Dictionary<String, Action> registerActions;

    public ObjectSubclassingController() {
      mutex = new ReaderWriterLockSlim();
      registeredSubclasses = new Dictionary<String, ObjectSubclassInfo>();
      registerActions = new Dictionary<string, Action>();

      // Register the ParseObject subclass, so we get access to the ACL,
      // objectId, and other ParseFieldName properties.
      RegisterSubclass(typeof(ParseObject));
    }

    public String GetClassName(Type type) {
      return type == typeof(ParseObject)
        ? parseObjectClassName
        : ObjectSubclassInfo.GetClassName(type.GetTypeInfo());
    }

    public Type GetType(String className) {
      ObjectSubclassInfo info = null;
      mutex.EnterReadLock();
      registeredSubclasses.TryGetValue(className, out info);
      mutex.ExitReadLock();

      return info != null
        ? info.TypeInfo.AsType()
        : null;
    }

    public bool IsTypeValid(String className, Type type) {
      ObjectSubclassInfo subclassInfo = null;

      mutex.EnterReadLock();
      registeredSubclasses.TryGetValue(className, out subclassInfo);
      mutex.ExitReadLock();

      return subclassInfo == null
        ? type == typeof(ParseObject)
        : subclassInfo.TypeInfo == type.GetTypeInfo();
    }

    public void RegisterSubclass(Type type) {
      TypeInfo typeInfo = type.GetTypeInfo();
      if (!typeof(ParseObject).GetTypeInfo().IsAssignableFrom(typeInfo)) {
        throw new ArgumentException("Cannot register a type that is not a subclass of ParseObject");
      }

      String className = GetClassName(type);

      try {
        // Perform this as a single independent transaction, so we can never get into an
        // intermediate state where we *theoretically* register the wrong class due to a
        // TOCTTOU bug.
        mutex.EnterWriteLock();

        ObjectSubclassInfo previousInfo = null;
        if (registeredSubclasses.TryGetValue(className, out previousInfo)) {
          if (typeInfo.IsAssignableFrom(previousInfo.TypeInfo)) {
            // Previous subclass is more specific or equal to the current type, do nothing.
            return;
          } else if (previousInfo.TypeInfo.IsAssignableFrom(typeInfo)) {
            // Previous subclass is parent of new child, fallthrough and actually register
            // this class.
            /* Do nothing */
          } else {
            throw new ArgumentException(
              "Tried to register both " + previousInfo.TypeInfo.FullName + " and " + typeInfo.FullName +
              " as the ParseObject subclass of " + className + ". Cannot determine the right class " +
              "to use because neither inherits from the other."
            );
          }
        }

        ConstructorInfo constructor = type.FindConstructor();
        if (constructor == null) {
          throw new ArgumentException("Cannot register a type that does not implement the default constructor!");
        }

        registeredSubclasses[className] = new ObjectSubclassInfo(type, constructor);
      } finally {
        mutex.ExitWriteLock();
      }

      Action toPerform;

      mutex.EnterReadLock();
      registerActions.TryGetValue(className, out toPerform);
      mutex.ExitReadLock();

      if (toPerform != null) {
        toPerform();
      }
    }

    public void UnregisterSubclass(Type type) {
      mutex.EnterWriteLock();
      registeredSubclasses.Remove(GetClassName(type));
      mutex.ExitWriteLock();
    }

    public void AddRegisterHook(Type t, Action action) {
      mutex.EnterWriteLock();
      registerActions.Add(GetClassName(t), action);
      mutex.ExitWriteLock();
    }

    public ParseObject Instantiate(String className) {
      ObjectSubclassInfo info = null;

      mutex.EnterReadLock();
      registeredSubclasses.TryGetValue(className, out info);
      mutex.ExitReadLock();

      return info != null
        ? info.Instantiate()
        : new ParseObject(className);
    }

    public IDictionary<String, String> GetPropertyMappings(String className) {
      ObjectSubclassInfo info = null;
      mutex.EnterReadLock();
      registeredSubclasses.TryGetValue(className, out info);
      if (info == null) {
        registeredSubclasses.TryGetValue(parseObjectClassName, out info);
      }
      mutex.ExitReadLock();

      return info.PropertyMappings;
    }

  }
}
