// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

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

namespace Parse
{
    public static class ObjectServiceExtensions
    {
        /// <summary>
        /// Registers a custom subclass type with the Parse SDK, enabling strong-typing of those ParseObjects whenever
        /// they appear. Subclasses must specify the ParseClassName attribute, have a default constructor, and properties
        /// backed by ParseObject fields should have ParseFieldName attributes supplied.
        /// </summary>
        /// <param name="serviceHub">The target <see cref="IServiceHub"/> instance.</param>
        /// <typeparam name="T">The ParseObject subclass type to register.</typeparam>
        public static void AddValidClass<T>(this IServiceHub serviceHub) where T : ParseObject, new() => serviceHub.ClassController.AddValid(typeof(T));

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
        public static void RemoveClass<T>(this IServiceHub serviceHub) where T : ParseObject, new() => serviceHub.ClassController.RemoveClass(typeof(T));

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
        public static ParseObject CreateObject(this IServiceHub serviceHub, string className) => serviceHub.ClassController.Instantiate(className, serviceHub);

        /// <summary>
        /// Creates a new ParseObject based upon a given subclass type.
        /// </summary>
        /// <returns>A new ParseObject for the given class name.</returns>
        public static T CreateObject<T>(this IServiceHub serviceHub) where T : ParseObject => (T) serviceHub.ClassController.CreateObject<T>(serviceHub);

        /// <summary>
        /// Creates a new ParseObject based upon a given subclass type.
        /// </summary>
        /// <returns>A new ParseObject for the given class name.</returns>
        public static T CreateObject<T>(this IParseObjectClassController classController, IServiceHub serviceHub) where T : ParseObject => (T) classController.Instantiate(classController.GetClassName(typeof(T)), serviceHub);

        /// <summary>
        /// Creates a reference to an existing ParseObject for use in creating associations between
        /// ParseObjects. Calling <see cref="IsDataAvailable"/> on this object will return
        /// <c>false</c> until <see cref="ParseExtensions.FetchIfNeededAsync{T}(T)"/> has been called.
        /// No network request will be made.
        /// </summary>
        /// <param name="className">The object's class.</param>
        /// <param name="objectId">The object id for the referenced object.</param>
        /// <returns>A ParseObject without data.</returns>
        public static ParseObject CreateObjectWithoutData(this IServiceHub serviceHub, string className, string objectId) => serviceHub.ClassController.CreateObjectWithoutData(className, objectId, serviceHub);

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
                return result.IsDirty ? throw new InvalidOperationException("A ParseObject subclass default constructor must not make changes to the object that cause it to be dirty.") : result;
            }
            finally { ParseObject.CreatingPointer.Value = false; }
        }

        /// <summary>
        /// Creates a reference to an existing ParseObject for use in creating associations between
        /// ParseObjects. Calling <see cref="IsDataAvailable"/> on this object will return
        /// <c>false</c> until <see cref="ParseExtensions.FetchIfNeededAsync{T}(T)"/> has been called.
        /// No network request will be made.
        /// </summary>
        /// <param name="objectId">The object id for the referenced object.</param>
        /// <returns>A ParseObject without data.</returns>
        public static T CreateObjectWithoutData<T>(this IServiceHub serviceHub, string objectId) where T : ParseObject => (T) serviceHub.CreateObjectWithoutData(serviceHub.ClassController.GetClassName(typeof(T)), objectId);

        /// <summary>
        /// Deletes each object in the provided list.
        /// </summary>
        /// <param name="objects">The objects to delete.</param>
        public static Task DeleteObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects) where T : ParseObject => DeleteObjectsAsync(serviceHub, objects, CancellationToken.None);

        /// <summary>
        /// Deletes each object in the provided list.
        /// </summary>
        /// <param name="objects">The objects to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task DeleteObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, CancellationToken cancellationToken) where T : ParseObject
        {
            HashSet<ParseObject> unique = new HashSet<ParseObject>(objects.OfType<ParseObject>().ToList(), new IdentityEqualityComparer<ParseObject> { });

            return EnqueueForAll(unique, toAwait => toAwait.OnSuccess(_ => Task.WhenAll(serviceHub.ObjectController.DeleteAllAsync(unique.Select(task => task.State).ToList(), serviceHub.GetCurrentSessionToken(), cancellationToken))).Unwrap().OnSuccess(task =>
            {
                // Dirty all objects in memory.

                foreach (ParseObject obj in unique)
                {
                    obj.IsDirty = true;
                }

                return default(object);
            }), cancellationToken);
        }

        /// <summary>
        /// Fetches all of the objects in the provided list.
        /// </summary>
        /// <param name="objects">The objects to fetch.</param>
        /// <returns>The list passed in for convenience.</returns>
        public static Task<IEnumerable<T>> FetchObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects) where T : ParseObject => FetchObjectsAsync(serviceHub, objects, CancellationToken.None);

        /// <summary>
        /// Fetches all of the objects in the provided list.
        /// </summary>
        /// <param name="objects">The objects to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list passed in for convenience.</returns>
        public static Task<IEnumerable<T>> FetchObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, CancellationToken cancellationToken) where T : ParseObject => EnqueueForAll(objects.Cast<ParseObject>(), (Task toAwait) => serviceHub.FetchAllInternalAsync(objects, true, toAwait, cancellationToken), cancellationToken);

        /// <summary>
        /// Fetches all of the objects that don't have data in the provided list.
        /// </summary>
        /// <param name="objects">todo: describe objects parameter on FetchAllIfNeededAsync</param>
        /// <returns>The list passed in for convenience.</returns>
        public static Task<IEnumerable<T>> FetchObjectsIfNeededAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects) where T : ParseObject => FetchObjectsIfNeededAsync(serviceHub, objects, CancellationToken.None);

        /// <summary>
        /// Fetches all of the objects that don't have data in the provided list.
        /// </summary>
        /// <param name="objects">The objects to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list passed in for convenience.</returns>
        public static Task<IEnumerable<T>> FetchObjectsIfNeededAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, CancellationToken cancellationToken) where T : ParseObject => EnqueueForAll(objects.Cast<ParseObject>(), (Task toAwait) => serviceHub.FetchAllInternalAsync(objects, false, toAwait, cancellationToken), cancellationToken);

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
        public static Task SaveObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects) where T : ParseObject => SaveObjectsAsync(serviceHub, objects, CancellationToken.None);

        /// <summary>
        /// Saves each object in the provided list.
        /// </summary>
        /// <param name="objects">The objects to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task SaveObjectsAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, CancellationToken cancellationToken) where T : ParseObject => DeepSaveAsync(serviceHub, objects.ToList(), serviceHub.GetCurrentSessionToken(), cancellationToken);

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
        internal static T GenerateObjectFromState<T>(this IServiceHub serviceHub, IObjectState state, string defaultClassName) where T : ParseObject => serviceHub.ClassController.GenerateObjectFromState<T>(state, defaultClassName, serviceHub);

        internal static T GenerateObjectFromState<T>(this IParseObjectClassController classController, IObjectState state, string defaultClassName, IServiceHub serviceHub) where T : ParseObject
        {
            T obj = (T) classController.CreateObjectWithoutData(state.ClassName ?? defaultClassName, state.ObjectId, serviceHub);
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
        internal static bool CanBeSerializedAsValue(this IServiceHub serviceHub, object value) => TraverseObjectDeep(serviceHub, value, yieldRoot: true).OfType<ParseObject>().All(entity => entity.ObjectId is { });

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
        static void CollectDirtyChildren(this IServiceHub serviceHub, object node, IList<ParseObject> dirtyChildren) => CollectDirtyChildren(serviceHub, node, dirtyChildren, new HashSet<ParseObject>(new IdentityEqualityComparer<ParseObject>()), new HashSet<ParseObject>(new IdentityEqualityComparer<ParseObject>()));

        internal static Task DeepSaveAsync(this IServiceHub serviceHub, object target, string sessionToken, CancellationToken cancellationToken)
        {
            List<ParseObject> objects = new List<ParseObject>();
            CollectDirtyChildren(serviceHub, target, objects);

            HashSet<ParseObject> uniqueObjects = new HashSet<ParseObject>(objects, new IdentityEqualityComparer<ParseObject>());
            List<Task> saveDirtyFileTasks = TraverseObjectDeep(serviceHub, target, true).OfType<ParseFile>().Where(file => file.IsDirty).Select(file => file.SaveAsync(serviceHub, cancellationToken)).ToList();

            return Task.WhenAll(saveDirtyFileTasks).OnSuccess(_ =>
            {
                IEnumerable<ParseObject> remaining = new List<ParseObject>(uniqueObjects);
                return InternalExtensions.WhileAsync(() => Task.FromResult(remaining.Any()), () =>
                {
                    // Partition the objects into two sets: those that can be saved immediately,
                    // and those that rely on other objects to be created first.

                    List<ParseObject> current = (from item in remaining where item.CanBeSerialized select item).ToList(), nextBatch = (from item in remaining where !item.CanBeSerialized select item).ToList();
                    remaining = nextBatch;

                    if (current.Count == 0)
                    {
                        // We do cycle-detection when building the list of objects passed to this
                        // function, so this should never get called. But we should check for it
                        // anyway, so that we get an exception instead of an infinite loop.

                        throw new InvalidOperationException("Unable to save a ParseObject with a relation to a cycle.");
                    }

                    // Save all of the objects in current.

                    return EnqueueForAll(current, toAwait => toAwait.OnSuccess(__ =>
                    {
                        List<IObjectState> states = (from item in current select item.State).ToList();
                        List<IDictionary<string, IParseFieldOperation>> operationsList = (from item in current select item.StartSave()).ToList();

                        IList<Task<IObjectState>> saveTasks = serviceHub.ObjectController.SaveAllAsync(states, operationsList, sessionToken, serviceHub, cancellationToken);

                        return Task.WhenAll(saveTasks).ContinueWith(task =>
                        {
                            if (task.IsFaulted || task.IsCanceled)
                            {
                                foreach ((ParseObject item, IDictionary<string, IParseFieldOperation> ops) pair in current.Zip(operationsList, (item, ops) => (item, ops)))
                                {
                                    pair.item.HandleFailedSave(pair.ops);
                                }
                            }
                            else
                            {
                                foreach ((ParseObject item, IObjectState state) pair in current.Zip(task.Result, (item, state) => (item, state)))
                                {
                                    pair.item.HandleSave(pair.state);
                                }
                            }

                            cancellationToken.ThrowIfCancellationRequested();
                            return task;
                        }).Unwrap();
                    }).Unwrap().OnSuccess(t => (object) null), cancellationToken);
                });
            }).Unwrap();
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
        static Task<IEnumerable<T>> FetchAllInternalAsync<T>(this IServiceHub serviceHub, IEnumerable<T> objects, bool force, Task toAwait, CancellationToken cancellationToken) where T : ParseObject => toAwait.OnSuccess(_ =>
        {
            if (objects.Any(obj => obj.State.ObjectId == null))
            {
                throw new InvalidOperationException("You cannot fetch objects that haven't already been saved.");
            }

            List<T> objectsToFetch = (from obj in objects where force || !obj.IsDataAvailable select obj).ToList();

            if (objectsToFetch.Count == 0)
            {
                return Task.FromResult(objects);
            }

            // Do one Find for each class.

            Dictionary<string, Task<IEnumerable<ParseObject>>> findsByClass = (from obj in objectsToFetch group obj.ObjectId by obj.ClassName into classGroup where classGroup.Count() > 0 select (ClassName: classGroup.Key, FindTask: new ParseQuery<ParseObject>(serviceHub, classGroup.Key).WhereContainedIn("objectId", classGroup).FindAsync(cancellationToken))).ToDictionary(pair => pair.ClassName, pair => pair.FindTask);

            // Wait for all the Finds to complete.

            return Task.WhenAll(findsByClass.Values.ToList()).OnSuccess(__ =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return objects;
                }

                // Merge the data from the Finds into the input objects.
                foreach ((T obj, ParseObject result) in from obj in objectsToFetch from result in findsByClass[obj.ClassName].Result where result.ObjectId == obj.ObjectId select (obj, result))
                {
                    obj.MergeFromObject(result);
                    obj.Fetched = true;
                }

                return objects;
            });
        }).Unwrap();

        internal static string GetFieldForPropertyName(this IServiceHub serviceHub, string className, string propertyName) => serviceHub.ClassController.GetPropertyMappings(className).TryGetValue(propertyName, out string fieldName) ? fieldName : fieldName;
    }
}
