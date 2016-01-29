// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Parse.Common.Internal;

namespace Parse {
  /// <summary>
  /// A common base class for ParseRelations.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public abstract class ParseRelationBase : IJsonConvertible {
    private ParseObject parent;
    private string key;
    private string targetClassName;

    internal ParseRelationBase(ParseObject parent, string key) {
      EnsureParentAndKey(parent, key);
    }

    internal ParseRelationBase(ParseObject parent, string key, string targetClassName)
      : this(parent, key) {
      this.targetClassName = targetClassName;
    }

    internal static IObjectSubclassingController SubclassingController {
      get {
        return ParseCorePlugins.Instance.SubclassingController;
      }
    }

    internal void EnsureParentAndKey(ParseObject parent, string key) {
      this.parent = this.parent ?? parent;
      this.key = this.key ?? key;
      Debug.Assert(this.parent == parent, "Relation retrieved from two different objects");
      Debug.Assert(this.key == key, "Relation retrieved from two different keys");
    }

    internal void Add(ParseObject obj) {
      var change = new ParseRelationOperation(new[] { obj }, null);
      parent.PerformOperation(key, change);
      targetClassName = change.TargetClassName;
    }

    internal void Remove(ParseObject obj) {
      var change = new ParseRelationOperation(null, new[] { obj });
      parent.PerformOperation(key, change);
      targetClassName = change.TargetClassName;
    }

    IDictionary<string, object> IJsonConvertible.ToJSON() {
      return new Dictionary<string, object> {
        {"__type", "Relation"},
        {"className", targetClassName}
      };
    }

    internal ParseQuery<T> GetQuery<T>() where T : ParseObject {
      if (targetClassName != null) {
        return new ParseQuery<T>(targetClassName)
          .WhereRelatedTo(parent, key);
      }

      return new ParseQuery<T>(parent.ClassName)
        .RedirectClassName(key)
        .WhereRelatedTo(parent, key);
    }

    internal string TargetClassName {
      get {
        return targetClassName;
      }
      set {
        targetClassName = value;
      }
    }

    /// <summary>
    /// Produces the proper ParseRelation&lt;T&gt; instance for the given classname.
    /// </summary>
    internal static ParseRelationBase CreateRelation(ParseObject parent,
        string key,
        string targetClassName) {
      var targetType = SubclassingController.GetType(targetClassName) ?? typeof(ParseObject);

      Expression<Func<ParseRelation<ParseObject>>> createRelationExpr =
          () => CreateRelation<ParseObject>(parent, key, targetClassName);
      var createRelationMethod =
          ((MethodCallExpression)createRelationExpr.Body)
          .Method
          .GetGenericMethodDefinition()
          .MakeGenericMethod(targetType);
      return (ParseRelationBase)createRelationMethod.Invoke(null, new object[] { parent, key, targetClassName });
    }

    private static ParseRelation<T> CreateRelation<T>(ParseObject parent, string key, string targetClassName)
        where T : ParseObject {
      return new ParseRelation<T>(parent, key, targetClassName);
    }
  }

  /// <summary>
  /// Provides access to all of the children of a many-to-many relationship. Each instance of
  /// ParseRelation is associated with a particular parent and key.
  /// </summary>
  /// <typeparam name="T">The type of the child objects.</typeparam>
  public sealed class ParseRelation<T> : ParseRelationBase where T : ParseObject {

    internal ParseRelation(ParseObject parent, string key) : base(parent, key) { }

    internal ParseRelation(ParseObject parent, string key, string targetClassName)
      : base(parent, key, targetClassName) { }

    /// <summary>
    /// Adds an object to this relation. The object must already have been saved.
    /// </summary>
    /// <param name="obj">The object to add.</param>
    public void Add(T obj) {
      base.Add(obj);
    }

    /// <summary>
    /// Removes an object from this relation. The object must already have been saved.
    /// </summary>
    /// <param name="obj">The object to remove.</param>
    public void Remove(T obj) {
      base.Remove(obj);
    }

    /// <summary>
    /// Gets a query that can be used to query the objects in this relation.
    /// </summary>
    public ParseQuery<T> Query {
      get {
        return base.GetQuery<T>();
      }
    }
  }
}
