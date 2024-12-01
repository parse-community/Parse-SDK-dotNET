using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Control;

namespace Parse
{
    public static class RelationServiceExtensions
    {
        /// <summary>
        /// Produces the proper ParseRelation&lt;T&gt; instance for the given classname.
        /// </summary>
        internal static ParseRelationBase CreateRelation(this IServiceHub serviceHub, ParseObject parent, string key, string targetClassName)
        {
            return serviceHub.ClassController.CreateRelation(parent, key, targetClassName);
        }

        internal static ParseRelationBase CreateRelation(this IParseObjectClassController classController, ParseObject parent, string key, string targetClassName)
        {
            Expression<Func<ParseRelation<ParseObject>>> createRelationExpr = () => CreateRelation<ParseObject>(parent, key, targetClassName);
            return (createRelationExpr.Body as MethodCallExpression).Method.GetGenericMethodDefinition().MakeGenericMethod(classController.GetType(targetClassName) ?? typeof(ParseObject)).Invoke(default, new object[] { parent, key, targetClassName }) as ParseRelationBase;
        }

        static ParseRelation<T> CreateRelation<T>(ParseObject parent, string key, string targetClassName) where T : ParseObject
        {
            return new ParseRelation<T>(parent, key, targetClassName);
        }
    }

    /// <summary>
    /// A common base class for ParseRelations.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ParseRelationBase : IJsonConvertible
    {
        ParseObject Parent { get; set; }

        string Key { get; set; }

        internal ParseRelationBase(ParseObject parent, string key) => EnsureParentAndKey(parent, key);

        internal ParseRelationBase(ParseObject parent, string key, string targetClassName) : this(parent, key) => TargetClassName = targetClassName;

        internal void EnsureParentAndKey(ParseObject parent, string key)
        {
            Parent ??= parent;
            Key ??= key;

            Debug.Assert(Parent == parent, "Relation retrieved from two different objects");
            Debug.Assert(Key == key, "Relation retrieved from two different keys");
        }

        internal void Add(ParseObject entity)
        {
            ParseRelationOperation change = new ParseRelationOperation(Parent.Services.ClassController, new[] { entity }, default);

            Parent.PerformOperation(Key, change);
            TargetClassName = change.TargetClassName;
        }

        internal void Remove(ParseObject entity)
        {
            ParseRelationOperation change = new ParseRelationOperation(Parent.Services.ClassController, default, new[] { entity });

            Parent.PerformOperation(Key, change);
            TargetClassName = change.TargetClassName;
        }

        IDictionary<string, object> IJsonConvertible.ConvertToJSON()
        {
            return new Dictionary<string, object>
            {
                ["__type"] = "Relation",
                ["className"] = TargetClassName
            };
        }

        internal ParseQuery<T> GetQuery<T>() where T : ParseObject
        {
            return TargetClassName is { } ? new ParseQuery<T>(Parent.Services, TargetClassName).WhereRelatedTo(Parent, Key) : new ParseQuery<T>(Parent.Services, Parent.ClassName).RedirectClassName(Key).WhereRelatedTo(Parent, Key);
        }

        internal string TargetClassName { get; set; }
    }

    /// <summary>
    /// Provides access to all of the children of a many-to-many relationship. Each instance of
    /// ParseRelation is associated with a particular parent and key.
    /// </summary>
    /// <typeparam name="T">The type of the child objects.</typeparam>
    public sealed class ParseRelation<T> : ParseRelationBase where T : ParseObject
    {
        internal ParseRelation(ParseObject parent, string key) : base(parent, key) { }

        internal ParseRelation(ParseObject parent, string key, string targetClassName) : base(parent, key, targetClassName) { }

        /// <summary>
        /// Adds an object to this relation. The object must already have been saved.
        /// </summary>
        /// <param name="obj">The object to add.</param>
        public void Add(T obj)
        {
            base.Add(obj);
        }

        /// <summary>
        /// Removes an object from this relation. The object must already have been saved.
        /// </summary>
        /// <param name="obj">The object to remove.</param>
        public void Remove(T obj)
        {
            base.Remove(obj);
        }

        /// <summary>
        /// Gets a query that can be used to query the objects in this relation.
        /// </summary>
        public ParseQuery<T> Query => GetQuery<T>();
    }
}
