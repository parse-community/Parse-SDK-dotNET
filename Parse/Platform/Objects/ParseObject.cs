using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Internal;
using Parse.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Utilities;
using Parse.Platform.Objects;
using Parse.Infrastructure.Data;
using System.Diagnostics;

namespace Parse;

/// <summary>
/// The ParseObject is a local representation of data that can be saved and
/// retrieved from the Parse cloud.</summary>
/// <remarks>
/// <para>
/// The basic workflow for creating new data is to construct a new ParseObject,
/// use the indexer to fill it with data, and then use SaveAsync() to persist to the
/// database.
/// </para>
/// <para>
/// The basic workflow for accessing existing data is to use a ParseQuery
/// to specify which existing data to retrieve.
/// </para>
/// </remarks>
public class ParseObject : IEnumerable<KeyValuePair<string, object>>, INotifyPropertyChanged
{
    internal static string AutoClassName { get; } = "_Automatic";

    internal static ThreadLocal<bool> CreatingPointer { get; } = new ThreadLocal<bool>(() => false);

    internal TaskQueue TaskQueue { get; } = new TaskQueue { };

    /// <summary>
    /// The <see cref="ParseClient"/> instance being targeted. This should generally not be set except when an object is being constructed, as otherwise race conditions may occur. The preferred method to set this property is via calling <see cref="Bind(IServiceHub)"/>.
    /// </summary>
    public IServiceHub Services { get; set; }

    /// <summary>
    /// Constructs a new ParseObject with no data in it. A ParseObject constructed in this way will
    /// not have an ObjectId and will not persist to the database until <see cref="SaveAsync(CancellationToken)"/>
    /// is called.
    /// </summary>
    /// <remarks>
    /// Class names must be alphanumerical plus underscore, and start with a letter. It is recommended
    /// to name classes in PascalCase.
    /// </remarks>
    /// <param name="className">The className for this ParseObject.</param>
    /// <param name="serviceHub">The <see cref="IServiceHub"/> implementation instance to target for any resources. This paramater can be effectively set after construction via <see cref="Bind(IServiceHub)"/>.</param>
    public ParseObject(string className, IServiceHub serviceHub = default)
    {
        // Validate serviceHub
        if (serviceHub == null && ParseClient.Instance == null)
        {
            Debug.WriteLine("Warning: Both serviceHub and ParseClient.Instance are null. ParseObject requires explicit initialization via Bind(IServiceHub).");

            //throw new InvalidOperationException("A valid IServiceHub or ParseClient.Instance must be available to construct a ParseObject.");
        }

        Services = serviceHub ?? ParseClient.Instance;

        // Validate and set className
        if (string.IsNullOrWhiteSpace(className))
        {
            throw new ArgumentException("You must specify a Parse class name when creating a new ParseObject.");
        }

        if (AutoClassName.Equals(className))
        {
            className = GetType().GetParseClassName() ?? throw new ArgumentException("Unable to determine class name for ParseObject.");
        }
        if (Services is not null)
        {
            // Validate against factory requirements
            if (!Services.ClassController.GetClassMatch(className, GetType()) && GetType() != typeof(ParseObject))
            {
                throw new ArgumentException("You must create this type of ParseObject using ParseObject.Create() or the proper subclass.");
            }
        }

        // Initialize state
        State = new MutableObjectState { ClassName = className };
        OnPropertyChanged(nameof(ClassName));
        OperationSetQueue.AddLast(new Dictionary<string, IParseFieldOperation>());

        // Handle pointer creation
        bool isPointer = CreatingPointer.Value;
        CreatingPointer.Value = false;

        Fetched = !isPointer;
        IsDirty = !isPointer;

        if (!isPointer)
        {
            SetDefaultValues();
        }
    }


    #region ParseObject Creation

    /// <summary>
    /// Constructor for use in ParseObject subclasses. Subclasses must specify a ParseClassName attribute. Subclasses that do not implement a constructor accepting <see cref="IServiceHub"/> will need to be bond to an implementation instance via <see cref="Bind(IServiceHub)"/> after construction.
    /// </summary>
    protected ParseObject(IServiceHub serviceHub = default) : this(AutoClassName, serviceHub) { }

    /// <summary>
    /// Attaches the given <see cref="IServiceHub"/> implementation instance to this <see cref="ParseObject"/> or <see cref="ParseObject"/>-derived class instance.
    /// </summary>
    /// <param name="serviceHub">The serviceHub to use for all operations.</param>
    /// <returns>The instance which was mutated.</returns>
    public ParseObject Bind(IServiceHub serviceHub)
    {
        return (Instance: this, Services = ParseClient.Instance).Instance;
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged
    {
        add
        {
            PropertyChangedHandler.Add(value);
        }
        remove
        {
            PropertyChangedHandler.Remove(value);
        }
    }

    /// <summary>
    /// Gets or sets the ParseACL governing this object.
    /// </summary>
    [ParseFieldName("ACL")]
    public ParseACL ACL
    {
        get => GetProperty<ParseACL>(default, nameof(ACL));
        set => SetProperty(value, nameof(ACL));
    }

    /// <summary>
    /// Gets the class name for the ParseObject.
    /// </summary>
    public string ClassName => State.ClassName;

    /// <summary>
    /// Gets the first time this object was saved as the server sees it, so that if you create a
    /// ParseObject, then wait a while, and then call <see cref="SaveAsync()"/>, the
    /// creation time will be the time of the first <see cref="SaveAsync()"/> call rather than
    /// the time the object was created locally.
    /// </summary>
    [ParseFieldName("createdAt")]
    public DateTime? CreatedAt => State.CreatedAt;

    /// <summary>
    /// Gets whether the ParseObject has been fetched.
    /// </summary>
    public bool IsDataAvailable
    {
        get
        {
            lock (Mutex)
            {
                return Fetched;
            }
        }
    }

    /// <summary>
    /// Indicates whether this ParseObject has unsaved changes.
    /// </summary>
    public bool IsDirty
    {
        get
        {
            lock (Mutex)
            {
                return CheckIsDirty(true);
            }
        }
        internal set
        {
            lock (Mutex)
            {
                Dirty = value;
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    /// <summary>
    /// Returns true if this object was created by the Parse server when the
    /// object might have already been there (e.g. in the case of a Facebook
    /// login)
    /// </summary>
    public bool IsNew
    {
        get => State.IsNew;
        internal set
        {
            MutateState(mutableClone => mutableClone.IsNew = value);
            OnPropertyChanged(nameof(IsNew));
        }
    }

    /// <summary>
    /// Gets a set view of the keys contained in this object. This does not include createdAt,
    /// updatedAt, or objectId. It does include things like username and ACL.
    /// </summary>
    public ICollection<string> Keys
    {
        get
        {
            lock (Mutex)
            {
                return EstimatedData.Keys;
            }
        }
    }

    /// <summary>
    /// Gets or sets the object id. An object id is assigned as soon as an object is
    /// saved to the server. The combination of a <see cref="ClassName"/> and an
    /// <see cref="ObjectId"/> uniquely identifies an object in your application.
    /// </summary>
    [ParseFieldName("objectId")]
    public string ObjectId
    {
        get => State.ObjectId;
        set
        {
            IsDirty = true;
            SetObjectIdInternal(value);
        }
    }

    /// <summary>
    /// Gets the last time this object was updated as the server sees it, so that if you make changes
    /// to a ParseObject, then wait a while, and then call <see cref="SaveAsync()"/>, the updated time
    /// will be the time of the <see cref="SaveAsync()"/> call rather than the time the object was
    /// changed locally.
    /// </summary>
    [ParseFieldName("updatedAt")]
    public DateTime? UpdatedAt => State.UpdatedAt;

    public IDictionary<string, IParseFieldOperation> CurrentOperations
    {
        get
        {
            lock (Mutex)
            {
                return OperationSetQueue.Last.Value;
            }
        }
    }

    internal object Mutex { get; } = new object { };

    public IObjectState State { get; private set; }

    internal bool CanBeSerialized
    {
        get
        {
            // This method is only used for batching sets of objects for saveAll
            // and when saving children automatically. Since it's only used to
            // determine whether or not save should be called on them, it only
            // needs to examine their current values, so we use estimatedData.

            lock (Mutex)
            {
                return Services.CanBeSerializedAsValue(EstimatedData);
            }
        }
    }

    bool Dirty { get; set; }

    internal IDictionary<string, object> EstimatedData { get; } = new Dictionary<string, object> { };

    internal bool Fetched { get; set; }

    bool HasDirtyChildren
    {
        get
        {
            lock (Mutex)
            {
                return FindUnsavedChildren().FirstOrDefault() != null;
            }
        }
    }

    LinkedList<IDictionary<string, IParseFieldOperation>> OperationSetQueue { get; } = new LinkedList<IDictionary<string, IParseFieldOperation>>();

    SynchronizedEventHandler<PropertyChangedEventArgs> PropertyChangedHandler { get; } = new SynchronizedEventHandler<PropertyChangedEventArgs>();

    /// <summary>
    /// Gets or sets a value on the object. It is recommended to name
    /// keys in partialCamelCaseLikeThis.
    /// </summary>
    /// <param name="key">The key for the object. Keys must be alphanumeric plus underscore
    /// and start with a letter.</param>
    /// <exception cref="KeyNotFoundException">The property is
    /// retrieved and <paramref name="key"/> is not found.</exception>
    /// <returns>The value for the key.</returns>
    virtual public object this[string key]
    {
        get
        {
            lock (Mutex)
            {
                CheckGetAccess(key);
                object value = EstimatedData[key];
                //TODO THIS WILL THROW, MAKE IT END GRACEFULLY
                // A relation may be deserialized without a parent or key. Either way,
                // make sure it's consistent.

                if (value is ParseRelationBase relation)
                {
                    relation.EnsureParentAndKey(this, key);
                }

                return value;
            }
        }
        set
        {
            lock (Mutex)
            {
                CheckKeyIsMutable(key);
                Set(key, value);
            }
        }
    }

    /// <summary>
    /// Adds a value for the given key, throwing an Exception if the key
    /// already has a value.
    /// </summary>
    /// <remarks>
    /// This allows you to use collection initialization syntax when creating ParseObjects,
    /// such as:
    /// <code>
    /// var obj = new ParseObject("MyType")
    /// {
    ///     {"name", "foo"},
    ///     {"count", 10},
    ///     {"found", false}
    /// };
    /// </code>
    /// </remarks>
    /// <param name="key">The key for which a value should be set.</param>
    /// <param name="value">The value for the key.</param>
    public void Add(string key, object value)
    {
        lock (Mutex)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException("Key already exists", key);
            }

            this[key] = value;
        }
    }

    /// <summary>
    /// Atomically adds objects to the end of the list associated with the given key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="values">The objects to add.</param>
    public void AddRangeToList<T>(string key, IEnumerable<T> values)
    {
        lock (Mutex)
        {
            CheckKeyIsMutable(key);
            PerformOperation(key, new ParseAddOperation(values.Cast<object>()));
        }
    }

    /// <summary>
    /// Atomically adds objects to the end of the list associated with the given key,
    /// only if they are not already present in the list. The position of the inserts are not
    /// guaranteed.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="values">The objects to add.</param>
    public void AddRangeUniqueToList<T>(string key, IEnumerable<T> values)
    {
        lock (Mutex)
        {
            CheckKeyIsMutable(key);
            PerformOperation(key, new ParseAddUniqueOperation(values.Cast<object>()));
        }
    }

    #endregion

    /// <summary>
    /// Atomically adds an object to the end of the list associated with the given key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The object to add.</param>
    public void AddToList(string key, object value)
    {
        AddRangeToList(key, new[] { value });
    }

    /// <summary>
    /// Atomically adds an object to the end of the list associated with the given key,
    /// only if it is not already present in the list. The position of the insert is not
    /// guaranteed.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The object to add.</param>
    public void AddUniqueToList(string key, object value)
    {
        AddRangeUniqueToList(key, new object[] { value });
    }

    /// <summary>
    /// Returns whether this object has a particular key.
    /// </summary>
    /// <param name="key">The key to check for</param>
    public bool ContainsKey(string key)
    {
        lock (Mutex)
        {
            return EstimatedData.ContainsKey(key);
        }
    }

    /// <summary>
    /// Gets a value for the key of a particular type.
    /// <typeparam name="T">The type to convert the value to. Supported types are
    /// ParseObject and its descendents, Parse types such as ParseRelation and ParseGeopoint,
    /// primitive types,IList&lt;T&gt;, IDictionary&lt;string, T&gt;, and strings.</typeparam>
    /// <param name="key">The key of the element to get.</param>
    /// <exception cref="KeyNotFoundException">The property is
    /// retrieved and <paramref name="key"/> is not found.</exception>
    /// </summary>
    public T Get<T>(string key)
    {
        return Conversion.To<T>(this[key]);
    }

    /// <summary>
    /// Access or create a Relation value for a key.
    /// </summary>
    /// <typeparam name="T">The type of object to create a relation for.</typeparam>
    /// <param name="key">The key for the relation field.</param>
    /// <returns>A ParseRelation for the key.</returns>
    public ParseRelation<T> GetRelation<T>(string key) where T : ParseObject
    {
        // All the sanity checking is done when add or remove is called.

        TryGetValue(key, out ParseRelation<T> relation);
        return relation ?? new ParseRelation<T>(this, key);
    }

    /// <summary>
    /// A helper function for checking whether two ParseObjects point to
    /// the same object in the cloud.
    /// </summary>
    public bool HasSameId(ParseObject other)
    {
        lock (Mutex)
        {
            return other is { } && Equals(ClassName, other.ClassName) && Equals(ObjectId, other.ObjectId);
        }
    }

    #region Atomic Increment

    /// <summary>
    /// Atomically increments the given key by 1.
    /// </summary>
    /// <param name="key">The key to increment.</param>
    public void Increment(string key)
    {
        Increment(key, 1);
    }

    /// <summary>
    /// Atomically increments the given key by the given number.
    /// </summary>
    /// <param name="key">The key to increment.</param>
    /// <param name="amount">The amount to increment by.</param>
    public void Increment(string key, long amount)
    {
        lock (Mutex)
        {
            CheckKeyIsMutable(key);
            PerformOperation(key, new ParseIncrementOperation(amount));
        }
    }

    /// <summary>
    /// Atomically increments the given key by the given number.
    /// </summary>
    /// <param name="key">The key to increment.</param>
    /// <param name="amount">The amount to increment by.</param>
    public void Increment(string key, double amount)
    {
        lock (Mutex)
        {
            CheckKeyIsMutable(key);
            PerformOperation(key, new ParseIncrementOperation(amount));
        }
    }

    /// <summary>
    /// Indicates whether key is unsaved for this ParseObject.
    /// </summary>
    /// <param name="key">The key to check for.</param>
    /// <returns><c>true</c> if the key has been altered and not saved yet, otherwise
    /// <c>false</c>.</returns>
    public bool IsKeyDirty(string key)
    {
        lock (Mutex)
        {
            return CurrentOperations.ContainsKey(key);
        }
    }

    /// <summary>
    /// Removes a key from the object's data if it exists.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    public virtual void Remove(string key)
    {
        lock (Mutex)
        {
            CheckKeyIsMutable(key);
            PerformOperation(key, ParseDeleteOperation.Instance);
        }
    }

    /// <summary>
    /// Atomically removes all instances of the objects in <paramref name="values"/>
    /// from the list associated with the given key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="values">The objects to remove.</param>
    public void RemoveAllFromList<T>(string key, IEnumerable<T> values)
    {
        lock (Mutex)
        {
            CheckKeyIsMutable(key);
            PerformOperation(key, new ParseRemoveOperation(values.Cast<object>()));
        }
    }

    /// <summary>
    /// Clears any changes to this object made since the last call to <see cref="SaveAsync()"/>.
    /// </summary>
    public void Revert()
    {
        lock (Mutex)
        {
            if (CurrentOperations.Count > 0)
            {
                CurrentOperations.Clear();
                RebuildEstimatedData();
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    /// <summary>
    /// Saves this object to the server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return TaskQueue.Enqueue(toAwait => SaveAsync(toAwait, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Populates result with the value for the key, if possible.
    /// </summary>
    /// <typeparam name="T">The desired type for the value.</typeparam>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <param name="result">The value for the given key, converted to the
    /// requested type, or null if unsuccessful.</param>
    /// <returns>true if the lookup and conversion succeeded, otherwise
    /// false.</returns>
    public bool TryGetValue<T>(string key, out T result)
    {
        lock (Mutex)
        {
            if (ContainsKey(key))
            {
                try
                {
                    T temp = Conversion.To<T>(this[key]);
                    result = temp;
                    return true;
                }
                catch
                {
                    result = default;
                    return false;
                }
            }

            result = default;
            return false;
        }
    }

    #endregion

    #region Delete Object

    /// <summary>
    /// Deletes this object on the server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        return TaskQueue.Enqueue(async toAwait =>
        {
            await DeleteAsyncInternal(cancellationToken).ConfigureAwait(false);
            return toAwait;  // Ensure the task is returned to the queue
        }, cancellationToken);
    }

    internal async Task DeleteAsyncInternal(CancellationToken cancellationToken)
    {
        if (ObjectId == null)
        {
            return; // No need to delete if the object doesn't have an ID
        }

        var sessionToken = Services.GetCurrentSessionToken();
        await Services.ObjectController.DeleteAsync(State, sessionToken, cancellationToken).ConfigureAwait(false);
        IsDirty = true;
    }


    #region Fetch Object(s)
    /// <summary>
    /// Fetches this object with the data from the server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal Task<ParseObject> FetchAsync(CancellationToken cancellationToken)
    {
        return TaskQueue.Enqueue(async toAwait =>
        {
            await FetchAsyncInternal(cancellationToken).ConfigureAwait(false);
            return this;  // Ensures the task is returned to the queue after fetch
        }, cancellationToken);
    }

    internal async Task<ParseObject> FetchIfNeededAsync(CancellationToken cancellationToken)
    {
        if (!IsDataAvailable)
        {
            return await FetchAsyncInternal(cancellationToken).ConfigureAwait(false);
        }
        return this;
    }
    internal async Task<ParseObject> FetchIfNeededAsyncInternal(Task toAwait, CancellationToken cancellationToken)
    {
        if (IsDataAvailable)
        {
            return this;
        }

        await toAwait.ConfigureAwait(false);
        return await FetchAsyncInternal(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// If this ParseObject has not been fetched (i.e. <see cref="IsDataAvailable"/> returns
    /// false), fetches this object with the data from the server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal Task<ParseObject> FetchIfNeededAsyncInternal(CancellationToken cancellationToken)
    {
        return TaskQueue.Enqueue(async toAwait =>
        {
            return await FetchIfNeededAsyncInternal(toAwait, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    internal virtual async Task<ParseObject> FetchAsyncInternal(CancellationToken cancellationToken)
    {
        if (ObjectId == null)
        {
            throw new InvalidOperationException("Cannot refresh an object that hasn't been saved to the server.");
        }

        var sessionToken = Services.GetCurrentSessionToken();
        var result = await Services.ObjectController.FetchAsync(State, sessionToken, Services, cancellationToken).ConfigureAwait(false);
        HandleFetchResult(result);
        return this;
    }

    #endregion


    internal void HandleFailedSave(IDictionary<string, IParseFieldOperation> operationsBeforeSave)
    {
        lock (Mutex)
        {
            // Attempt to find the node in the OperationSetQueue
            LinkedListNode<IDictionary<string, IParseFieldOperation>> opNode = OperationSetQueue.Find(operationsBeforeSave);
            if (opNode == null)
            {
                // If not found, gracefully exit or perform cleanup as needed
                return; // Gracefully exit
            }

            IDictionary<string, IParseFieldOperation> nextOperations = opNode.Next.Value;
            bool wasDirty = nextOperations.Count > 0;
            OperationSetQueue.Remove(opNode);

            // Merge the data from the failed save into the next save.

            foreach (KeyValuePair<string, IParseFieldOperation> pair in operationsBeforeSave)
            {
                IParseFieldOperation operation1 = pair.Value;

                nextOperations.TryGetValue(pair.Key, out IParseFieldOperation operation2);
                nextOperations[pair.Key] = operation2 is { } ? operation2.MergeWithPrevious(operation1) : operation1;
            }

            if (!wasDirty && nextOperations == CurrentOperations && operationsBeforeSave.Count > 0)
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public virtual void HandleFetchResult(IObjectState serverState)
    {
        lock (Mutex)
        {
            MergeFromServer(serverState);
        }
    }

    internal virtual void HandleSave(IObjectState serverState)
    {
        lock (Mutex)
        {
            IDictionary<string, IParseFieldOperation> operationsBeforeSave = OperationSetQueue.First.Value;
            OperationSetQueue.RemoveFirst();

            // Merge the data from the save and the data from the server into serverData.

            MutateState(mutableClone => mutableClone.Apply(operationsBeforeSave));
            MergeFromServer(serverState);
        }
    }

    internal void MergeFromObject(ParseObject other)
    {
        // If they point to the same instance, we don't need to merge

        lock (Mutex)
        {
            if (this == other)
            {
                return;
            }
        }

        // Clear out any changes on this object.

        if (OperationSetQueue.Count != 1)
        {
            throw new InvalidOperationException("Attempt to MergeFromObject during save.");
        }

        OperationSetQueue.Clear();

        foreach (IDictionary<string, IParseFieldOperation> operationSet in other.OperationSetQueue)
        {
            OperationSetQueue.AddLast(operationSet.ToDictionary(entry => entry.Key, entry => entry.Value));
        }

        lock (Mutex)
        {
            State = other.State;
        }

        RebuildEstimatedData();
    }

    internal virtual void MergeFromServer(IObjectState serverState)
    {
        // Make a new serverData with fetched values.

        Dictionary<string, object> newServerData = serverState.ToDictionary(t => t.Key, t => t.Value);

        lock (Mutex)
        {
            // Trigger handler based on serverState

            if (serverState.ObjectId != null)
            {
                // If the objectId is being merged in, consider this object to be fetched.

                Fetched = true;
                OnPropertyChanged(nameof(IsDataAvailable));
            }

            if (serverState.UpdatedAt != null)
            {
                OnPropertyChanged(nameof(UpdatedAt));
            }

            if (serverState.CreatedAt != null)
            {
                OnPropertyChanged(nameof(CreatedAt));
            }

            // We cache the fetched object because subsequent Save operation might flush the fetched objects into Pointers.

            IDictionary<string, ParseObject> fetchedObject = CollectFetchedObjects();

            foreach (KeyValuePair<string, object> pair in serverState)
            {
                object value = pair.Value;

                if (value is ParseObject)
                {
                    // Resolve fetched object.

                    ParseObject entity = value as ParseObject;

                    if (fetchedObject.ContainsKey(entity.ObjectId))
                    {
                        value = fetchedObject[entity.ObjectId];
                    }
                }
                newServerData[pair.Key] = value;
            }

            IsDirty = false;
            var s = serverState.MutatedClone(mutableClone => mutableClone.ServerData = newServerData);
            MutateState(mutableClone => mutableClone.Apply(s));
        }
    }

    internal void MutateState(Action<MutableObjectState> mutator)
    {
        lock (Mutex)
        {
            State = State.MutatedClone(mutator);

            // Refresh the estimated data.

            RebuildEstimatedData();
        }
    }

    /// <summary>
    /// Override to run validations on key/value pairs. Make sure to still
    /// call the base version.
    /// </summary>
    internal virtual void OnSettingValue(ref string key, ref object value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
    }

    /// <summary>
    /// PerformOperation is like setting a value at an index, but instead of
    /// just taking a new value, it takes a ParseFieldOperation that modifies the value.
    /// </summary>
    internal void PerformOperation(string key, IParseFieldOperation operation)
    {
        lock (Mutex)
        {
            EstimatedData.TryGetValue(key, out object oldValue);
            object newValue = operation.Apply(oldValue, key);

            if (newValue != ParseDeleteOperation.Token)
            {
                EstimatedData[key] = newValue;
            }
            else
            {
                EstimatedData.Remove(key);
            }

            bool wasDirty = CurrentOperations.Count > 0;
            CurrentOperations.TryGetValue(key, out IParseFieldOperation oldOperation);
            IParseFieldOperation newOperation = operation.MergeWithPrevious(oldOperation);
            CurrentOperations[key] = newOperation;

            if (!wasDirty)
            {
                OnPropertyChanged(nameof(IsDirty));
            }

            OnFieldsChanged(new[] { key });
        }
    }

    /// <summary>
    /// Regenerates the estimatedData map from the serverData and operations.
    /// </summary>
    internal void RebuildEstimatedData()
    {
        lock (Mutex)
        {
            EstimatedData.Clear();

            foreach (KeyValuePair<string, object> item in State)
            {
                EstimatedData.Add(item);
            }
            foreach (IDictionary<string, IParseFieldOperation> operations in OperationSetQueue)
            {
                ApplyOperations(operations, EstimatedData);
            }

            // We've just applied a bunch of operations to estimatedData which
            // may have changed all of its keys. Notify of all keys and properties
            // mapped to keys being changed.

            OnFieldsChanged(default);
        }
    }

    public IDictionary<string, object> ServerDataToJSONObjectForSerialization()
    {
        return PointerOrLocalIdEncoder.Instance.Encode(State.ToDictionary(pair => pair.Key, pair => pair.Value), Services) as IDictionary<string, object>;
    }

    /// <summary>
    /// Perform Set internally which is not gated by mutability check.
    /// </summary>
    /// <param name="key">key for the object.</param>
    /// <param name="value">the value for the key.</param>
    public void Set(string key, object value)
    {
        lock (Mutex)
        {
            OnSettingValue(ref key, ref value);

            if (!ParseDataEncoder.Validate(value))
            {
                throw new ArgumentException("Invalid type for value: " + value.GetType().ToString());
            }

            PerformOperation(key, new ParseSetOperation(value));
        }
    }

    /// <summary>
    /// Allows subclasses to set values for non-pointer construction.
    /// </summary>
    internal virtual void SetDefaultValues() { }

    public void SetIfDifferent<T>(string key, T value)
    {
        bool hasCurrent = TryGetValue(key, out T current);

        if (value == null)
        {
            if (hasCurrent)
            {
                PerformOperation(key, ParseDeleteOperation.Instance);
            }
            return;
        }

        if (!hasCurrent || !value.Equals(current))
        {
            Set(key, value);
        }
    }

    #endregion

    #region Save Object(s)

    /// <summary>
    /// Pushes new operations onto the queue and returns the current set of operations.
    /// </summary>
    internal IDictionary<string, IParseFieldOperation> StartSave()
    {
        lock (Mutex)
        {
            IDictionary<string, IParseFieldOperation> currentOperations = CurrentOperations;
            OperationSetQueue.AddLast(new Dictionary<string, IParseFieldOperation>());
            OnPropertyChanged(nameof(IsDirty));
            return currentOperations;
        }
    }

    #endregion

    /// <summary>
    /// Gets the value of a property based upon its associated ParseFieldName attribute.
    /// </summary>
    /// <returns>The value of the property.</returns>
    /// <param name="propertyName">The name of the property.</param>
    /// <typeparam name="T">The return type of the property.</typeparam>
    protected T GetProperty<T>([CallerMemberName] string propertyName = null)
    {
        return GetProperty(default(T), propertyName);
    }

    /// <summary>
    /// Gets the value of a property based upon its associated ParseFieldName attribute.
    /// </summary>
    /// <returns>The value of the property.</returns>
    /// <param name="defaultValue">The value to return if the property is not present on the ParseObject.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <typeparam name="T">The return type of the property.</typeparam>
    protected T GetProperty<T>(T defaultValue, [CallerMemberName] string propertyName = null)
    {
        return TryGetValue(Services.GetFieldForPropertyName(ClassName, propertyName), out T result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a relation for a property based upon its associated ParseFieldName attribute.
    /// </summary>
    /// <returns>The ParseRelation for the property.</returns>
    /// <param name="propertyName">The name of the property.</param>
    /// <typeparam name="T">The ParseObject subclass type of the ParseRelation.</typeparam>
    protected ParseRelation<T> GetRelationProperty<T>([CallerMemberName] string propertyName = null) where T : ParseObject
    {
        return GetRelation<T>(Services.GetFieldForPropertyName(ClassName, propertyName));
    }

    protected virtual bool CheckKeyMutable(string key)
    {
        return true;
    }

    /// <summary>
    /// Raises change notifications for all properties associated with the given
    /// field names. If fieldNames is null, this will notify for all known field-linked
    /// properties (e.g. this happens when we recalculate all estimated data from scratch)
    /// </summary>
    protected void OnFieldsChanged(IEnumerable<string> fields)
    {
        IDictionary<string, string> mappings = Services.ClassController.GetPropertyMappings(ClassName);

        foreach (string property in mappings is { } ? fields is { } ? from mapping in mappings join field in fields on mapping.Value equals field select mapping.Key : mappings.Keys : Enumerable.Empty<string>())
        {
            OnPropertyChanged(property);
        }

        OnPropertyChanged("Item[]");
    }

    /// <summary>
    /// Raises change notifications for a property. Passing null or the empty string
    /// notifies the binding framework that all properties/indexes have changed.
    /// Passing "Item[]" tells the binding framework that all indexed values
    /// have changed (but not all properties)
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedHandler.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual async Task SaveAsync(Task toAwait, CancellationToken cancellationToken)
    {
        if (!IsDirty)
        {
            // No need to save if the object is not dirty
            return;
        }

        // Get the session token and prepare the save operation
        var currentOperations = StartSave();
        var sessionToken = Services.GetCurrentSessionToken();

        // Perform the deep save asynchronously
        try
        {
            // Await the deep save operation
            await Services.DeepSaveAsync(EstimatedData, sessionToken, cancellationToken).ConfigureAwait(false);

            // Proceed with the object save
            await Services.ObjectController.SaveAsync(State, currentOperations, sessionToken, Services, cancellationToken).ConfigureAwait(false);

            // Handle successful save
            HandleSave(State);
        }
        catch (OperationCanceledException)
        {
            // Handle the cancellation case
            HandleFailedSave(currentOperations);
        }
        catch (Exception ex)
        {
            // Log or handle unexpected errors
            HandleFailedSave(currentOperations);
            Console.Error.WriteLine($"Error during save: {ex.Message}");
        }
    }


    /// <summary>
    /// Sets the value of a property based upon its associated ParseFieldName attribute.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <typeparam name="T">The type for the property.</typeparam>
    protected void SetProperty<T>(T value, [CallerMemberName] string propertyName = null)
    {
        this[Services.GetFieldForPropertyName(ClassName, propertyName)] = value;
    }

    void ApplyOperations(IDictionary<string, IParseFieldOperation> operations, IDictionary<string, object> map)
    {
        lock (Mutex)
        {
            foreach (KeyValuePair<string, IParseFieldOperation> pair in operations)
            {
                map.TryGetValue(pair.Key, out object oldValue);
                object newValue = pair.Value.Apply(oldValue, pair.Key);

                if (newValue != ParseDeleteOperation.Token)
                {
                    map[pair.Key] = newValue;
                }
                else
                {
                    map.Remove(pair.Key);
                }
            }
        }
    }
    void CheckGetAccess(string key)
    {
        lock (Mutex)
        {
            if (!CheckIsDataAvailable(key))
            {
                Debug.WriteLine($"Warning: ParseObject has no data for key '{key}'. Ensure FetchIfNeededAsync() is called before accessing data.");
                // Optionally, set a flag or return early to signal the issue.
                return;
            }
        }
    }


    bool CheckIsDataAvailable(string key)
    {
        lock (Mutex)
        {
            return IsDataAvailable || EstimatedData.ContainsKey(key);
        }
    }

    internal bool CheckIsDirty(bool considerChildren)
    {
        lock (Mutex)
        {
            return Dirty || CurrentOperations.Count > 0 || considerChildren && HasDirtyChildren;
        }
    }

    void CheckKeyIsMutable(string key)
    {
        if (!CheckKeyMutable(key))
        {
            throw new InvalidOperationException($@"Cannot change the ""{key}"" property of a ""{ClassName}"" object.");
        }
    }

    /// <summary>
    /// Deep traversal of this object to grab a copy of any object referenced by this object.
    /// These instances may have already been fetched, and we don't want to lose their data when
    /// refreshing or saving.
    /// </summary>
    /// <returns>Map of objectId to ParseObject which have been fetched.</returns>
    IDictionary<string, ParseObject> CollectFetchedObjects()
    {
        return Services.TraverseObjectDeep(EstimatedData).OfType<ParseObject>().Where(o => o.ObjectId != null && o.IsDataAvailable).GroupBy(o => o.ObjectId).ToDictionary(group => group.Key, group => group.Last());
    }

    IEnumerable<ParseObject> FindUnsavedChildren()
    {
        return Services.TraverseObjectDeep(EstimatedData).OfType<ParseObject>().Where(o => o.IsDirty);
    }

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        lock (Mutex)
        {
            return EstimatedData.GetEnumerator();
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        lock (Mutex)
        {
            return ((IEnumerable<KeyValuePair<string, object>>) this).GetEnumerator();
        }
    }
    /// <summary>
    /// Sets the objectId without marking dirty.
    /// </summary>
    /// <param name="objectId">The new objectId</param>
    void SetObjectIdInternal(string objectId)
    {
        lock (Mutex)
        {
            MutateState(mutableClone => mutableClone.ObjectId = objectId);
            OnPropertyChanged(nameof(ObjectId));
        }
    }
}
