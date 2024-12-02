using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Internal;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Utilities;
using Parse.Infrastructure.Data;
using System.Diagnostics;

namespace Parse;

public static class ObjectServiceExtensions
{
    /// <summary>
    /// Registers a custom subclass type with the Parse SDK, enabling strong-typing of those ParseObjects whenever
    /// they appear. Subclasses must specify the ParseClassName attribute, have a default constructor, and properties
    /// backed by ParseObject fields should have ParseFieldName attributes supplied.
    /// </summary>
    /// <param name="serviceHub">The target <see cref="IServiceHub"/> instance.</param>
    /// <typeparam name="T">The ParseObject subclass type to register.</typeparam>
    public static void AddValidClass<T>(this IServiceHub serviceHub) where T : ParseObject, new()
    {
        serviceHub.ClassController.AddValid(typeof(T));
    }

    /// <summary>
    /// Registers a custom subclass type with the Parse SDK, enabling strong-typing of those ParseObjects whenever
    /// they appear. Subclasses must specify the ParseClassName attribute, have a default constructor, and properties
    /// backed by ParseObject fields should have ParseFieldName attributes supplied.
    /// </summary>
    /// <param name="type">The ParseObject subclass type to register.</param>
    /// <param name="serviceHub">The target <see cref="IServiceHub"/> instance.</param>
    public static void RegisterSubclass(this IServiceHub serviceHub, Type type)
    {
        if (typeof(ParseObject).IsAssignableFrom(type))
        {
            serviceHub.ClassController.AddValid(type);
        }
    }

    /// <summary>
    /// Unregisters a previously-registered sub-class of <see cref="ParseObject"/> with the subclassing controller.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serviceHub"></param>
    public static void RemoveClass<T>(this IServiceHub serviceHub) where T : ParseObject, new()
    {
        serviceHub.ClassController.RemoveClass(typeof(T));
    }

    /// <summary>
    /// Unregisters a previously-registered sub-class of <see cref="ParseObject"/> with the subclassing controller.
    /// </summary>
    /// <param name="subclassingController"></param>
    /// <param name="type"></param>
    public static void RemoveClass(this IParseObjectClassController subclassingController, Type type)
    {
        if (typeof(ParseObject).IsAssignableFrom(type))
        {
            subclassingController.RemoveClass(type);
        }
    }

    /// <summary>
    /// Creates a new ParseObject based upon a class name. If the class name is a special type (e.g.
    /// for <see cref="ParseUser"/>), then the appropriate type of ParseObject is returned.
    /// </summary>
    /// <param name="className">The class of object to create.</param>
    /// <returns>A new ParseObject for the given class name.</returns>
    public static ParseObject CreateObject(this IServiceHub serviceHub, string className)
    {
        return serviceHub.ClassController.Instantiate(className, serviceHub);
    }

    /// <summary>
    /// Creates a new ParseObject based upon a given subclass type.
    /// </summary>
    /// <returns>A new ParseObject for the given class name.</returns>
    public static T CreateObject<T>(this IServiceHub serviceHub) where T : ParseObject
    {
        return (T) serviceHub.ClassController.CreateObject<T>(serviceHub);
    }

    /// <summary>
    /// Creates a new ParseObject based upon a given subclass type.
    /// </summary>
    /// <returns>A new ParseObject for the given class name.</returns>
    public static T CreateObject<T>(this IParseObjectClassController classController, IServiceHub serviceHub) where T : ParseObject
    {
        return (T) classController.Instantiate(classController.GetClassName(typeof(T)), serviceHub);
    }

    /// <summary>
    /// Creates a reference to an existing ParseObject for use in creating associations between
    /// ParseObjects. Calling <see cref="IsDataAvailable"/> on this object will return
    /// <c>false</c> until <see cref="ParseExtensions.FetchIfNeededAsync{T}(T)"/> has been called.
    /// No network request will be made.
    /// </summary>
    /// <param name="className">The object's class.</param>
    /// <param name="objectId">The object id for the referenced object.</param>
    /// <returns>A ParseObject without data.</returns>
    public static ParseObject CreateObjectWithoutData(this IServiceHub serviceHub, string className, string objectId)
    {
        return serviceHub.ClassController.CreateObjectWithoutData(className, objectId, serviceHub);
    }

    /// <summary>
    /// Creates a reference to an existing ParseObject for use in creating associations between
    /// ParseObjects. Calling <see cref="IsDataAvailable"/> on this object will return
    /// <c>false</c> until <see cref="ParseExtensions.FetchIfNeededAsync{T}(T)"/> has been called.
    /// No network request will be made.
    /// </summary>
    /// <param name="className">The object's class.</param>
    /// <param name="objectId">The object id for the referenced object.</param>
    /// <returns>A ParseObject without data.</returns>
    public static ParseObject CreateObjectWithoutData(this IParseObjectClassController classController, string className, string objectId, IServiceHub serviceHub)
    {
        ParseObject.CreatingPointer.Value = true;
        try
        {
            ParseObject result = classController.Instantiate(className, serviceHub);
            result.ObjectId = objectId;

            // Left in because the property setter might be doing something funky.

            result.IsDirty = false;
            if (result.IsDirty)
                throw new InvalidOperationException("A ParseObject subclass default constructor must not make changes to the object that cause it to be dirty.");
            else
                return  result;
        }
        finally
        {
            ParseObject.CreatingPointer.Value = false;
        }
    }

    /// <summary>
    /// Creates a reference to an existing ParseObject for use in creating associations between
    /// ParseObjects. Calling <see cref="IsDataAvailable"/> on this object will return
    /// <c>false</c> until <see cref="ParseExtensions.FetchIfNeededAsync{T}(T)"/> has been called.
    /// No network request will be made.
    /// </summary>
    /// <param name="objectId">The object id for the referenced object.</param>
    /// <returns>A ParseObject without data.</returns>
    public static T CreateObjectWithoutData<T>(this IServiceHub serviceHub, string objectId) where T : ParseObject
    {
        return (T) serviceHub.CreateObjectWithoutData(serviceHub.ClassController.GetClassName(typeof(T)), objectId);
    }
    /// <summary>
    /// Creates a reference to a new ParseObject with the specified initial data.
    /// This can be used for creating new objects with predefined values.
    /// No network request will be made until the object is saved.
    /// </summary>
    /// <param name="initialData">A dictionary containing the initial key-value pairs for the object.</param>
    /// <returns>A new ParseObject with the specified initial data.</returns>
    public static T CreateObjectWithData<T>(this IServiceHub serviceHub, IDictionary<string, object> initialData) where T : ParseObject
    {
        if (initialData == null)
        {
            throw new ArgumentNullException(nameof(initialData), "Initial data cannot be null.");
        }

        // Create a new instance of the specified ParseObject type
        var parseObject = (T) serviceHub.CreateObject(serviceHub.ClassController.GetClassName(typeof(T)));

        // Set initial data properties
        foreach (var kvp in initialData)
        {
            parseObject[kvp.Key] = kvp.Value;
        }

        return parseObject;
    }

    /// <summary>
    /// Deletes each object in the provided list.
    /// </summary>
    /// <param name="objects">The objects to delete.</param>
    public static Task DeleteObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects) where T : ParseObject
    {
        return DeleteObjectsAsync(serviceHub, objects, CancellationToken.None);
    }

    /// <summary>
    /// Deletes each object in the provided list.
    /// </summary>
    /// <param name="objects">The objects to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task DeleteObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, CancellationToken cancellationToken) where T : ParseObject
    {
        // Get a unique set of ParseObjects
        var uniqueObjects = new HashSet<ParseObject>(objects.OfType<ParseObject>(), new IdentityEqualityComparer<ParseObject>());

        await EnqueueForAll(uniqueObjects, async toAwait =>
        {
            // Wait for the preceding task (toAwait) to complete
            await toAwait.ConfigureAwait(false);

            // Perform the delete operation for all objects
            await Task.WhenAll(
                serviceHub.ObjectController.DeleteAllAsync(
                    uniqueObjects.Select(obj => obj.State).ToList(),
                    serviceHub.GetCurrentSessionToken(),
                    cancellationToken)
            ).ConfigureAwait(false);

            // Mark all objects as dirty
            foreach (var obj in uniqueObjects)
            {
                obj.IsDirty = true;
            }

            return true; // Return a meaningful result if needed
        }, cancellationToken).ConfigureAwait(false);
    }





    /// <summary>
    /// Fetches all of the objects in the provided list.
    /// </summary>
    /// <param name="objects">The objects to fetch.</param>
    /// <returns>The list passed in for convenience.</returns>
    public static Task<IEnumerable<T>> FetchObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects) where T : ParseObject
    {
        return FetchObjectsAsync(serviceHub, objects, CancellationToken.None);
    }

    /// <summary>
    /// Fetches all of the objects in the provided list.
    /// </summary>
    /// <param name="objects">The objects to fetch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list passed in for convenience.</returns>
    public static Task<IEnumerable<T>> FetchObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, CancellationToken cancellationToken) where T : ParseObject
    {
        return EnqueueForAll(objects.Cast<ParseObject>(), (Task toAwait) => serviceHub.FetchAllInternalAsync(objects, true, toAwait, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Fetches all of the objects that don't have data in the provided list.
    /// </summary>
    /// <param name="objects">todo: describe objects parameter on FetchAllIfNeededAsync</param>
    /// <returns>The list passed in for convenience.</returns>
    public static Task<IEnumerable<T>> FetchObjectsIfNeededAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects) where T : ParseObject
    {
        return FetchObjectsIfNeededAsync(serviceHub, objects, CancellationToken.None);
    }

    /// <summary>
    /// Fetches all of the objects that don't have data in the provided list.
    /// </summary>
    /// <param name="objects">The objects to fetch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list passed in for convenience.</returns>
    public static Task<IEnumerable<T>> FetchObjectsIfNeededAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, CancellationToken cancellationToken) where T : ParseObject
    {
        return EnqueueForAll(objects.Cast<ParseObject>(), (Task toAwait) => serviceHub.FetchAllInternalAsync(objects, false, toAwait, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Gets a <see cref="ParseQuery{ParseObject}"/> for the type of object specified by
    /// <paramref name="className"/>
    /// </summary>
    /// <param name="className">The class name of the object.</param>
    /// <returns>A new <see cref="ParseQuery{ParseObject}"/>.</returns>
    public static ParseQuery<ParseObject> GetQuery(this IServiceHub serviceHub, string className)
    {
        // Since we can't return a ParseQuery<ParseUser> (due to strong-typing with
        // generics), we'll require you to go through subclasses. This is a better
        // experience anyway, especially with LINQ integration, since you'll get
        // strongly-typed queries and compile-time checking of property names and
        // types.

        if (serviceHub.ClassController.GetType(className) is { })
        {
            throw new ArgumentException($"Use the class-specific query properties for class {className}", nameof(className));
        }
        return new ParseQuery<ParseObject>(serviceHub, className);
    }

    /// <summary>
    /// Saves each object in the provided list.
    /// </summary>
    /// <param name="objects">The objects to save.</param>
    public static Task SaveObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects) where T : ParseObject
    {
        return SaveObjectsAsync(serviceHub, objects, CancellationToken.None);
    }

    /// <summary>
    /// Saves each object in the provided list.
    /// </summary>
    /// <param name="objects">The objects to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task SaveObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, CancellationToken cancellationToken) where T : ParseObject
    {
        return DeepSaveAsync(serviceHub, objects.ToList(), serviceHub.GetCurrentSessionToken(), cancellationToken);
    }

    /// <summary>
    /// Flattens dictionaries and lists into a single enumerable of all contained objects
    /// that can then be queried over.
    /// </summary>
    /// <param name="root">The root of the traversal</param>
    /// <param name="traverseParseObjects">Whether to traverse into ParseObjects' children</param>
    /// <param name="yieldRoot">Whether to include the root in the result</param>
    /// <returns></returns>
    internal static IEnumerable<object> TraverseObjectDeep(this IServiceHub serviceHub, object root, bool traverseParseObjects = false, bool yieldRoot = false)
    {
        IEnumerable<object> items = DeepTraversalInternal(serviceHub, root, traverseParseObjects, new HashSet<object>(new IdentityEqualityComparer<object>()));
        return yieldRoot ? new[] { root }.Concat(items) : items;
    }

    // TODO (hallucinogen): add unit test
    internal static T GenerateObjectFromState<T>(this IServiceHub serviceHub, IObjectState state, string defaultClassName) where T : ParseObject
    {
        var obj = serviceHub.ClassController.GenerateObjectFromState<T>(state, defaultClassName, serviceHub);
        return obj;
    }

    internal static T GenerateObjectFromState<T>(
this IParseObjectClassController classController,
IObjectState state,
string defaultClassName,
IServiceHub serviceHub
) where T : ParseObject
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state), "The state cannot be null.");
        }

        if (string.IsNullOrEmpty(state.ClassName) && string.IsNullOrEmpty(defaultClassName))
        {
            throw new InvalidOperationException("Both state.ClassName and defaultClassName are null or empty. Unable to determine class name.");
        }

        // Use the provided class name from the state, or fall back to the default class name
        string className = state.ClassName ?? defaultClassName;
        state.ClassName = className;    //to make it so that user cl
        var obj = (T) ParseClient.Instance.CreateObject(className);
        
        
        obj.HandleFetchResult(state);

        return obj;
    }


    internal static IDictionary<string, object> GenerateJSONObjectForSaving(this IServiceHub serviceHub, IDictionary<string, IParseFieldOperation> operations)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();

        foreach (KeyValuePair<string, IParseFieldOperation> pair in operations)
        {
            result[pair.Key] = PointerOrLocalIdEncoder.Instance.Encode(pair.Value, serviceHub);
        }

        return result;
    }

    /// <summary>
    /// Returns true if the given object can be serialized for saving as a value
    /// that is pointed to by a ParseObject.
    /// </summary>
    internal static bool CanBeSerializedAsValue(this IServiceHub serviceHub, object value)
    {
        return TraverseObjectDeep(serviceHub, value, yieldRoot: true).OfType<ParseObject>().All(entity => entity.ObjectId is { });
    }

    static void CollectDirtyChildren(this IServiceHub serviceHub, object node, IList<ParseObject> dirtyChildren, ICollection<ParseObject> seen, ICollection<ParseObject> seenNew)
    {
        foreach (ParseObject target in TraverseObjectDeep(serviceHub, node).OfType<ParseObject>())
        {
            ICollection<ParseObject> scopedSeenNew;

            // Check for cycles of new objects. Any such cycle means it will be impossible to save
            // this collection of objects, so throw an exception.

            if (target.ObjectId != null)
            {
                scopedSeenNew = new HashSet<ParseObject>(new IdentityEqualityComparer<ParseObject>());
            }
            else
            {
                if (seenNew.Contains(target))
                {
                    throw new InvalidOperationException("Found a circular dependency while saving");
                }

                scopedSeenNew = new HashSet<ParseObject>(seenNew, new IdentityEqualityComparer<ParseObject>()) { target };
            }

            // Check for cycles of any object. If this occurs, then there's no problem, but
            // we shouldn't recurse any deeper, because it would be an infinite recursion.

            if (seen.Contains(target))
            {
                return;
            }

            seen.Add(target);

            // Recurse into this object's children looking for dirty children.
            // We only need to look at the child object's current estimated data,
            // because that's the only data that might need to be saved now.

            CollectDirtyChildren(serviceHub, target.EstimatedData, dirtyChildren, seen, scopedSeenNew);

            if (target.CheckIsDirty(false))
            {
                dirtyChildren.Add(target);
            }
        }
    }

    /// <summary>
    /// Helper version of CollectDirtyChildren so that callers don't have to add the internally
    /// used parameters.
    /// </summary>
    static void CollectDirtyChildren(this IServiceHub serviceHub, object node, IList<ParseObject> dirtyChildren)
    {
        CollectDirtyChildren(serviceHub, node, dirtyChildren, new HashSet<ParseObject>(new IdentityEqualityComparer<ParseObject>()), new HashSet<ParseObject>(new IdentityEqualityComparer<ParseObject>()));
    }

    internal static async Task DeepSaveAsync(this IServiceHub serviceHub, object target, string sessionToken, CancellationToken cancellationToken)
    {
        // Collect dirty objects
        var objects = new List<ParseObject>();
        CollectDirtyChildren(serviceHub, target, objects);

        var uniqueObjects = new HashSet<ParseObject>(objects, new IdentityEqualityComparer<ParseObject>());

        // Save all dirty files
        var saveDirtyFileTasks = TraverseObjectDeep(serviceHub, target, true)
            .OfType<ParseFile>()
            .Where(file => file.IsDirty)
            .Select(file => file.SaveAsync(serviceHub, cancellationToken))
            .ToList();

        await Task.WhenAll(saveDirtyFileTasks).ConfigureAwait(false);

        // Save remaining objects in batches
        var remaining = new List<ParseObject>(uniqueObjects);
        while (remaining.Any())
        {
            // Partition objects into those that can be saved immediately and those that cannot
            var current = remaining.Where(item => item.CanBeSerialized).ToList();
            var nextBatch = remaining.Where(item => !item.CanBeSerialized).ToList();
            remaining = nextBatch;

            if (!current.Any())
            {
                throw new InvalidOperationException("Unable to save a ParseObject with a relation to a cycle.");
            }

            // Save all objects in the current batch
            var states = current.Select(item => item.State).ToList();
            var operationsList = current.Select(item => item.StartSave()).ToList();

            try
            {
                // Await SaveAllAsync to get the collection of Task<IObjectState>
                var saveTasks = await serviceHub.ObjectController.SaveAllAsync(states, operationsList, sessionToken, serviceHub, cancellationToken).ConfigureAwait(false);

                // Await individual tasks in the result
                foreach (var (item, stateTask) in current.Zip(saveTasks, (item, stateTask) => (item, stateTask)))
                {
                    var state = await stateTask.ConfigureAwait(false); // Await the Task<IObjectState>
                    item.HandleSave(state); // Now state is IObjectState
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                foreach (var (item, ops) in current.Zip(operationsList, (item, ops) => (item, ops)))
                {
                    item.HandleFailedSave(ops);
                }

                throw; // Re-throw cancellation exceptions
            }
        }
    }

    static IEnumerable<object> DeepTraversalInternal(this IServiceHub serviceHub, object root, bool traverseParseObjects, ICollection<object> seen)
    {
        seen.Add(root);
        System.Collections.IEnumerable targets = ParseClient.IL2CPPCompiled ? null : null as IEnumerable<object>;

        if (Conversion.As<IDictionary<string, object>>(root) is { } rootDictionary)
        {
            targets = rootDictionary.Values;
        }
        else
        {
            if (Conversion.As<IList<object>>(root) is { } rootList)
            {
                targets = rootList;
            }
            else if (traverseParseObjects)
            {
                if (root is ParseObject entity)
                {
                    targets = entity.Keys.ToList().Select(key => entity[key]);
                }
            }
        }

        if (targets is { })
        {
            foreach (object item in targets)
            {
                if (!seen.Contains(item))
                {
                    yield return item;

                    foreach (object child in DeepTraversalInternal(serviceHub, item, traverseParseObjects, seen))
                    {
                        yield return child;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds a task to the queue for all of the given objects.
    /// </summary>
    static Task<T> EnqueueForAll<T>(IEnumerable<ParseObject> objects, Func<Task, Task<T>> taskStart, CancellationToken cancellationToken)
    {
        // The task that will be complete when all of the child queues indicate they're ready to start.

        TaskCompletionSource<object> readyToStart = new TaskCompletionSource<object>();

        // First, we need to lock the mutex for the queue for every object. We have to hold this
        // from at least when taskStart() is called to when obj.taskQueue enqueue is called, so
        // that saves actually get executed in the order they were setup by taskStart().
        // The locks have to be sorted so that we always acquire them in the same order.
        // Otherwise, there's some risk of deadlock.

        LockSet lockSet = new LockSet(objects.Select(o => o.TaskQueue.Mutex));

        lockSet.Enter();
        try
        {
            // The task produced by taskStart. By running this immediately, we allow everything prior
            // to toAwait to run before waiting for all of the queues on all of the objects.

            Task<T> fullTask = taskStart(readyToStart.Task);

            // Add fullTask to each of the objects' queues.

            List<Task> childTasks = new List<Task>();
            foreach (ParseObject obj in objects)
            {
                obj.TaskQueue.Enqueue((Task task) =>
                {
                    childTasks.Add(task);
                    return fullTask;
                }, cancellationToken);
            }

            // When all of the objects' queues are ready, signal fullTask that it's ready to go on.
            Task.WhenAll(childTasks.ToArray()).ContinueWith((Task task) => readyToStart.SetResult(default));
            return fullTask;
        }
        finally
        {
            lockSet.Exit();
        }
    }

    /// <summary>
    /// Fetches all of the objects in the list.
    /// </summary>
    /// <param name="objects">The objects to fetch.</param>
    /// <param name="force">If false, only objects without data will be fetched.</param>
    /// <param name="toAwait">A task to await before starting.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list passed in for convenience.</returns>
    static async Task<IEnumerable<T>> FetchAllInternalAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, bool force, Task toAwait, CancellationToken cancellationToken) where T : ParseObject
    {
        // Wait for the preceding task (toAwait) to complete
        await toAwait.ConfigureAwait(false);

        // Ensure all objects have an ObjectId
        if (objects.Any(obj => obj.State.ObjectId == null))
        {
            throw new InvalidOperationException("You cannot fetch objects that haven't already been saved.");
        }

        // Filter objects to fetch based on the force flag and data availability
        var objectsToFetch = objects.Where(obj => force || !obj.IsDataAvailable).ToList();

        if (objectsToFetch.Count == 0)
        {
            return objects; // No objects need to be fetched
        }

        // Group objects by ClassName and prepare queries
        var findsByClass = objectsToFetch
            .GroupBy(obj => obj.ClassName)
            .Where(group => group.Any())
            .ToDictionary(
                group => group.Key,
                group => new ParseQuery<ParseObject>(serviceHub, group.Key)
                            .WhereContainedIn("objectId", group.Select(obj => obj.State.ObjectId))
                            .FindAsync(cancellationToken)
            );

        // Execute all queries in parallel
        var findResults = await Task.WhenAll(findsByClass.Values).ConfigureAwait(false);

        // If the operation was canceled, return the original list
        if (cancellationToken.IsCancellationRequested)
        {
            return objects;
        }

        // Merge fetched data into the original objects
        foreach (var obj in objectsToFetch)
        {
            if (findsByClass.TryGetValue(obj.ClassName, out var resultsTask))
            {
                var results = await resultsTask.ConfigureAwait(false);
                var match = results.FirstOrDefault(result => result.ObjectId == obj.ObjectId);

                if (match != null)
                {
                    obj.MergeFromObject(match);
                    obj.Fetched = true;
                }
            }
        }

        return objects;
    }


    internal static string GetFieldForPropertyName(this IServiceHub serviceHub, string className, string propertyName)
    {
        if (serviceHub == null)
        {
            Debug.WriteLine("ServiceHub is null.");
            return null;
        }

        if (string.IsNullOrEmpty(className))
        {
            throw new ArgumentException("ClassName cannot be null or empty.", nameof(className));
        }

        if (string.IsNullOrEmpty(propertyName))
        {
            throw new ArgumentException("PropertyName cannot be null or empty.", nameof(propertyName));
        }

        var classController = serviceHub.ClassController;
        if (classController == null)
        {
            throw new InvalidOperationException("ClassController is null.");
        }

        var propertyMappings = classController.GetPropertyMappings(className);
        if (propertyMappings == null)
        {
            throw new InvalidOperationException($"Property mappings for class '{className}' are null.");
        }

        if (!propertyMappings.TryGetValue(propertyName, out string fieldName))
        {
            throw new KeyNotFoundException($"Property '{propertyName}' not found in class '{className}'.");
        }

        return fieldName;
    }

}
