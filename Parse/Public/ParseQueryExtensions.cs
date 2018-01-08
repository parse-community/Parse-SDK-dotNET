// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Common.Internal;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Parse {
  /// <summary>
  /// Provides extension methods for <see cref="ParseQuery{T}"/> to support
  /// Linq-style queries.
  /// </summary>
  public static class ParseQueryExtensions {
    private static readonly MethodInfo getMethod;
    private static readonly MethodInfo stringContains;
    private static readonly MethodInfo stringStartsWith;
    private static readonly MethodInfo stringEndsWith;
    private static readonly MethodInfo containsMethod;
    private static readonly MethodInfo notContainsMethod;
    private static readonly MethodInfo containsKeyMethod;
    private static readonly MethodInfo notContainsKeyMethod;
    private static readonly Dictionary<MethodInfo, MethodInfo> functionMappings;
    static ParseQueryExtensions() {
      getMethod = GetMethod<ParseObject>(obj => obj.Get<int>(null)).GetGenericMethodDefinition();
      stringContains = GetMethod<string>(str => str.Contains(null));
      stringStartsWith = GetMethod<string>(str => str.StartsWith(null));
      stringEndsWith = GetMethod<string>(str => str.EndsWith(null));
      functionMappings = new Dictionary<MethodInfo, MethodInfo> {
        {
          stringContains,
          GetMethod<ParseQuery<ParseObject>>(q => q.WhereContains(null, null))
        },
        {
          stringStartsWith,
          GetMethod<ParseQuery<ParseObject>>(q => q.WhereStartsWith(null, null))
        },
        {
          stringEndsWith,
          GetMethod<ParseQuery<ParseObject>>(q => q.WhereEndsWith(null,null))
        },
      };
      containsMethod = GetMethod<object>(
          o => ParseQueryExtensions.ContainsStub<object>(null, null)).GetGenericMethodDefinition();
      notContainsMethod = GetMethod<object>(
          o => ParseQueryExtensions.NotContainsStub<object>(null, null))
              .GetGenericMethodDefinition();

      containsKeyMethod = GetMethod<object>(o => ParseQueryExtensions.ContainsKeyStub(null, null));
      notContainsKeyMethod = GetMethod<object>(
        o => ParseQueryExtensions.NotContainsKeyStub(null, null));
    }

    /// <summary>
    /// Gets a MethodInfo for a top-level method call.
    /// </summary>
    private static MethodInfo GetMethod<T>(Expression<Action<T>> expression) {
      return (expression.Body as MethodCallExpression).Method;
    }

    /// <summary>
    /// When a query is normalized, this is a placeholder to indicate we should
    /// add a WhereContainedIn() clause.
    /// </summary>
    private static bool ContainsStub<T>(object collection, T value) {
      throw new NotImplementedException(
          "Exists only for expression translation as a placeholder.");
    }

    /// <summary>
    /// When a query is normalized, this is a placeholder to indicate we should
    /// add a WhereNotContainedIn() clause.
    /// </summary>
    private static bool NotContainsStub<T>(object collection, T value) {
      throw new NotImplementedException(
          "Exists only for expression translation as a placeholder.");
    }

    /// <summary>
    /// When a query is normalized, this is a placeholder to indicate that we should
    /// add a WhereExists() clause.
    /// </summary>
    private static bool ContainsKeyStub(ParseObject obj, string key) {
      throw new NotImplementedException(
          "Exists only for expression translation as a placeholder.");
    }

    /// <summary>
    /// When a query is normalized, this is a placeholder to indicate that we should
    /// add a WhereDoesNotExist() clause.
    /// </summary>
    private static bool NotContainsKeyStub(ParseObject obj, string key) {
      throw new NotImplementedException(
          "Exists only for expression translation as a placeholder.");
    }

    /// <summary>
    /// Evaluates an expression and throws if the expression has components that can't be
    /// evaluated (e.g. uses the parameter that's only represented by an object on the server).
    /// </summary>
    private static object GetValue(Expression exp) {
      try {
        return Expression.Lambda(
            typeof(Func<>).MakeGenericType(exp.Type), exp).Compile().DynamicInvoke();
      } catch (Exception e) {
        throw new InvalidOperationException("Unable to evaluate expression: " + exp, e);
      }
    }

    /// <summary>
    /// Checks whether the MethodCallExpression is a call to ParseObject.Get(),
    /// which is the call we normalize all indexing into the ParseObject to.
    /// </summary>
    private static bool IsParseObjectGet(MethodCallExpression node) {
      if (node == null || node.Object == null) {
        return false;
      }
      if (!typeof(ParseObject).GetTypeInfo().IsAssignableFrom(node.Object.Type.GetTypeInfo())) {
        return false;
      }
      return node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == getMethod;
    }


    /// <summary>
    /// Visits an Expression, converting ParseObject.Get/ParseObject[]/ParseObject.Property,
    /// and nested indices into a single call to ParseObject.Get() with a "field path" like
    /// "foo.bar.baz"
    /// </summary>
    private class ObjectNormalizer : ExpressionVisitor {
      protected override Expression VisitIndex(IndexExpression node) {
        var visitedObject = Visit(node.Object);
        var indexer = visitedObject as MethodCallExpression;
        if (IsParseObjectGet(indexer)) {
          var indexValue = GetValue(node.Arguments[0]) as string;
          if (indexValue == null) {
            throw new InvalidOperationException("Index must be a string");
          }
          var newPath = GetValue(indexer.Arguments[0]) + "." + indexValue;
          return Expression.Call(indexer.Object,
              getMethod.MakeGenericMethod(node.Type),
              Expression.Constant(newPath, typeof(string)));
        }
        return base.VisitIndex(node);
      }

      /// <summary>
      /// Check for a ParseFieldName attribute and use that as the path component, turning
      /// properties like foo.ObjectId into foo.Get("objectId")
      /// </summary>
      protected override Expression VisitMember(MemberExpression node) {
        var fieldName = node.Member.GetCustomAttribute<ParseFieldNameAttribute>();
        if (fieldName != null &&
            typeof(ParseObject).GetTypeInfo().IsAssignableFrom(node.Expression.Type.GetTypeInfo())) {
          var newPath = fieldName.FieldName;
          return Expression.Call(node.Expression,
              getMethod.MakeGenericMethod(node.Type),
              Expression.Constant(newPath, typeof(string)));
        }
        return base.VisitMember(node);
      }

      /// <summary>
      /// If a ParseObject.Get() call has been cast, just change the generic parameter.
      /// </summary>
      protected override Expression VisitUnary(UnaryExpression node) {
        var methodCall = Visit(node.Operand) as MethodCallExpression;
        if ((node.NodeType == ExpressionType.Convert ||
            node.NodeType == ExpressionType.ConvertChecked) &&
            IsParseObjectGet(methodCall)) {
          return Expression.Call(methodCall.Object,
              getMethod.MakeGenericMethod(node.Type),
              methodCall.Arguments);
        }
        return base.VisitUnary(node);
      }

      protected override Expression VisitMethodCall(MethodCallExpression node) {
        // Turn parseObject["foo"] into parseObject.Get<object>("foo")
        if (node.Method.Name == "get_Item" && node.Object is ParameterExpression) {
          var indexPath = GetValue(node.Arguments[0]) as string;
          return Expression.Call(node.Object,
              getMethod.MakeGenericMethod(typeof(object)),
              Expression.Constant(indexPath, typeof(string)));
        }

        // Turn parseObject.Get<object>("foo")["bar"] into parseObject.Get<object>("foo.bar")
        if (node.Method.Name == "get_Item" || IsParseObjectGet(node)) {
          var visitedObject = Visit(node.Object);
          var indexer = visitedObject as MethodCallExpression;
          if (IsParseObjectGet(indexer)) {
            var indexValue = GetValue(node.Arguments[0]) as string;
            if (indexValue == null) {
              throw new InvalidOperationException("Index must be a string");
            }
            var newPath = GetValue(indexer.Arguments[0]) + "." + indexValue;
            return Expression.Call(indexer.Object,
                getMethod.MakeGenericMethod(node.Type),
                Expression.Constant(newPath, typeof(string)));
          }
        }
        return base.VisitMethodCall(node);
      }
    }

    /// <summary>
    /// Normalizes Where expressions.
    /// </summary>
    private class WhereNormalizer : ExpressionVisitor {

      /// <summary>
      /// Normalizes binary operators. &lt;, &gt;, &lt;=, &gt;= !=, and ==
      /// This puts the ParseObject.Get() on the left side of the operation
      /// (reversing it if necessary), and normalizes the ParseObject.Get()
      /// </summary>
      protected override Expression VisitBinary(BinaryExpression node) {
        var leftTransformed = new ObjectNormalizer().Visit(node.Left) as MethodCallExpression;
        var rightTransformed = new ObjectNormalizer().Visit(node.Right) as MethodCallExpression;

        MethodCallExpression objectExpression;
        Expression filterExpression;
        bool inverted;
        if (leftTransformed != null) {
          objectExpression = leftTransformed;
          filterExpression = node.Right;
          inverted = false;
        } else {
          objectExpression = rightTransformed;
          filterExpression = node.Left;
          inverted = true;
        }

        try {
          switch (node.NodeType) {
            case ExpressionType.GreaterThan:
              if (inverted) {
                return Expression.LessThan(objectExpression, filterExpression);
              } else {
                return Expression.GreaterThan(objectExpression, filterExpression);
              }
            case ExpressionType.GreaterThanOrEqual:
              if (inverted) {
                return Expression.LessThanOrEqual(objectExpression, filterExpression);
              } else {
                return Expression.GreaterThanOrEqual(objectExpression, filterExpression);
              }
            case ExpressionType.LessThan:
              if (inverted) {
                return Expression.GreaterThan(objectExpression, filterExpression);
              } else {
                return Expression.LessThan(objectExpression, filterExpression);
              }
            case ExpressionType.LessThanOrEqual:
              if (inverted) {
                return Expression.GreaterThanOrEqual(objectExpression, filterExpression);
              } else {
                return Expression.LessThanOrEqual(objectExpression, filterExpression);
              }
            case ExpressionType.Equal:
              return Expression.Equal(objectExpression, filterExpression);
            case ExpressionType.NotEqual:
              return Expression.NotEqual(objectExpression, filterExpression);
          }
        } catch (ArgumentException) {
          throw new InvalidOperationException("Operation not supported: " + node);
        }
        return base.VisitBinary(node);
      }

      /// <summary>
      /// If a ! operator is used, this removes the ! and instead calls the equivalent
      /// function (so e.g. == becomes !=, &lt; becomes &gt;=, Contains becomes NotContains)
      /// </summary>
      protected override Expression VisitUnary(UnaryExpression node) {
        // Normalizes inversion
        if (node.NodeType == ExpressionType.Not) {
          var visitedOperand = Visit(node.Operand);
          var binaryOperand = visitedOperand as BinaryExpression;
          if (binaryOperand != null) {
            switch (binaryOperand.NodeType) {
              case ExpressionType.GreaterThan:
                return Expression.LessThanOrEqual(binaryOperand.Left, binaryOperand.Right);
              case ExpressionType.GreaterThanOrEqual:
                return Expression.LessThan(binaryOperand.Left, binaryOperand.Right);
              case ExpressionType.LessThan:
                return Expression.GreaterThanOrEqual(binaryOperand.Left, binaryOperand.Right);
              case ExpressionType.LessThanOrEqual:
                return Expression.GreaterThan(binaryOperand.Left, binaryOperand.Right);
              case ExpressionType.Equal:
                return Expression.NotEqual(binaryOperand.Left, binaryOperand.Right);
              case ExpressionType.NotEqual:
                return Expression.Equal(binaryOperand.Left, binaryOperand.Right);
            }
          }

          var methodCallOperand = visitedOperand as MethodCallExpression;
          if (methodCallOperand != null) {
            if (methodCallOperand.Method.IsGenericMethod) {
              if (methodCallOperand.Method.GetGenericMethodDefinition() == containsMethod) {
                var genericNotContains = notContainsMethod.MakeGenericMethod(
                    methodCallOperand.Method.GetGenericArguments());
                return Expression.Call(genericNotContains, methodCallOperand.Arguments.ToArray());
              }
              if (methodCallOperand.Method.GetGenericMethodDefinition() == notContainsMethod) {
                var genericContains = containsMethod.MakeGenericMethod(
                    methodCallOperand.Method.GetGenericArguments());
                return Expression.Call(genericContains, methodCallOperand.Arguments.ToArray());
              }
            }
            if (methodCallOperand.Method == containsKeyMethod) {
              return Expression.Call(notContainsKeyMethod, methodCallOperand.Arguments.ToArray());
            }
            if (methodCallOperand.Method == notContainsKeyMethod) {
              return Expression.Call(containsKeyMethod, methodCallOperand.Arguments.ToArray());
            }
          }
        }
        return base.VisitUnary(node);
      }

      /// <summary>
      /// Normalizes .Equals into == and Contains() into the appropriate stub.
      /// </summary>
      protected override Expression VisitMethodCall(MethodCallExpression node) {
        // Convert .Equals() into ==
        if (node.Method.Name == "Equals" &&
            node.Method.ReturnType == typeof(bool) &&
            node.Method.GetParameters().Length == 1) {
          var obj = new ObjectNormalizer().Visit(node.Object) as MethodCallExpression;
          var parameter = new ObjectNormalizer().Visit(node.Arguments[0]) as MethodCallExpression;
          if ((IsParseObjectGet(obj) && (obj.Object is ParameterExpression)) ||
              (IsParseObjectGet(parameter) && (parameter.Object is ParameterExpression))) {
            return Expression.Equal(node.Object, node.Arguments[0]);
          }
        }

        // Convert the .Contains() into a ContainsStub
        if (node.Method != stringContains &&
            node.Method.Name == "Contains" &&
            node.Method.ReturnType == typeof(bool) &&
            node.Method.GetParameters().Length <= 2) {
          var collection = node.Method.GetParameters().Length == 1 ?
              node.Object :
              node.Arguments[0];
          var parameterIndex = node.Method.GetParameters().Length - 1;
          var parameter = new ObjectNormalizer().Visit(node.Arguments[parameterIndex])
              as MethodCallExpression;
          if (IsParseObjectGet(parameter) && (parameter.Object is ParameterExpression)) {
            var genericContains = containsMethod.MakeGenericMethod(parameter.Type);
            return Expression.Call(genericContains, collection, parameter);
          }
          var target = new ObjectNormalizer().Visit(collection) as MethodCallExpression;
          var element = node.Arguments[parameterIndex];
          if (IsParseObjectGet(target) && (target.Object is ParameterExpression)) {
            var genericContains = containsMethod.MakeGenericMethod(element.Type);
            return Expression.Call(genericContains, target, element);
          }
        }

        // Convert obj["foo.bar"].ContainsKey("baz") into obj.ContainsKey("foo.bar.baz")
        if (node.Method.Name == "ContainsKey" &&
            node.Method.ReturnType == typeof(bool) &&
            node.Method.GetParameters().Length == 1) {
          var getter = new ObjectNormalizer().Visit(node.Object) as MethodCallExpression;
          Expression target = null;
          string path = null;
          if (IsParseObjectGet(getter) && getter.Object is ParameterExpression) {
            target = getter.Object;
            path = GetValue(getter.Arguments[0]) + "." + GetValue(node.Arguments[0]);
            return Expression.Call(containsKeyMethod, target, Expression.Constant(path));
          } else if (node.Object is ParameterExpression) {
            target = node.Object;
            path = GetValue(node.Arguments[0]) as string;
          }
          if (target != null && path != null) {
            return Expression.Call(containsKeyMethod, target, Expression.Constant(path));
          }
        }
        return base.VisitMethodCall(node);
      }
    }

    /// <summary>
    /// Converts a normalized method call expression into the appropriate ParseQuery clause.
    /// </summary>
    private static ParseQuery<T> WhereMethodCall<T>(
        this ParseQuery<T> source, Expression<Func<T, bool>> expression, MethodCallExpression node)
        where T : ParseObject {
      if (IsParseObjectGet(node) && (node.Type == typeof(bool) || node.Type == typeof(bool?))) {
        // This is a raw boolean field access like 'where obj.Get<bool>("foo")'
        return source.WhereEqualTo(GetValue(node.Arguments[0]) as string, true);
      }

      MethodInfo translatedMethod;
      if (functionMappings.TryGetValue(node.Method, out translatedMethod)) {
        var objTransformed = new ObjectNormalizer().Visit(node.Object) as MethodCallExpression;
        if (!(IsParseObjectGet(objTransformed) &&
            objTransformed.Object == expression.Parameters[0])) {
          throw new InvalidOperationException(
            "The left-hand side of a supported function call must be a ParseObject field access.");
        }
        var fieldPath = GetValue(objTransformed.Arguments[0]);
        var containedIn = GetValue(node.Arguments[0]);
        var queryType = translatedMethod.DeclaringType.GetGenericTypeDefinition()
            .MakeGenericType(typeof(T));
        translatedMethod = ReflectionHelpers.GetMethod(queryType,
            translatedMethod.Name,
            translatedMethod.GetParameters().Select(p => p.ParameterType).ToArray());
        return translatedMethod.Invoke(source, new[] { fieldPath, containedIn }) as ParseQuery<T>;
      }

      if (node.Arguments[0] == expression.Parameters[0]) {
        // obj.ContainsKey("foo") --> query.WhereExists("foo")
        if (node.Method == containsKeyMethod) {
          return source.WhereExists(GetValue(node.Arguments[1]) as string);
        }
        // !obj.ContainsKey("foo") --> query.WhereDoesNotExist("foo")
        if (node.Method == notContainsKeyMethod) {
          return source.WhereDoesNotExist(GetValue(node.Arguments[1]) as string);
        }
      }

      if (node.Method.IsGenericMethod) {
        if (node.Method.GetGenericMethodDefinition() == containsMethod) {
          // obj.Get<IList<T>>("path").Contains(someValue)
          if (IsParseObjectGet(node.Arguments[0] as MethodCallExpression)) {
            return source.WhereEqualTo(
                GetValue(((MethodCallExpression)node.Arguments[0]).Arguments[0]) as string,
                GetValue(node.Arguments[1]));
          }
          // someList.Contains(obj.Get<T>("path"))
          if (IsParseObjectGet(node.Arguments[1] as MethodCallExpression)) {
            var collection = GetValue(node.Arguments[0]) as System.Collections.IEnumerable;
            return source.WhereContainedIn(
                GetValue(((MethodCallExpression)node.Arguments[1]).Arguments[0]) as string,
                collection.Cast<object>());
          }
        }

        if (node.Method.GetGenericMethodDefinition() == notContainsMethod) {
          // !obj.Get<IList<T>>("path").Contains(someValue)
          if (IsParseObjectGet(node.Arguments[0] as MethodCallExpression)) {
            return source.WhereNotEqualTo(
                GetValue(((MethodCallExpression)node.Arguments[0]).Arguments[0]) as string,
                GetValue(node.Arguments[1]));
          }
          // !someList.Contains(obj.Get<T>("path"))
          if (IsParseObjectGet(node.Arguments[1] as MethodCallExpression)) {
            var collection = GetValue(node.Arguments[0]) as System.Collections.IEnumerable;
            return source.WhereNotContainedIn(
                GetValue(((MethodCallExpression)node.Arguments[1]).Arguments[0]) as string,
                collection.Cast<object>());
          }
        }
      }
      throw new InvalidOperationException(node.Method + " is not a supported method call in a where expression.");
    }

    /// <summary>
    /// Converts a normalized binary expression into the appropriate ParseQuery clause.
    /// </summary>
    private static ParseQuery<T> WhereBinaryExpression<T>(
        this ParseQuery<T> source, Expression<Func<T, bool>> expression, BinaryExpression node)
        where T : ParseObject {
      var leftTransformed = new ObjectNormalizer().Visit(node.Left) as MethodCallExpression;

      if (!(IsParseObjectGet(leftTransformed) &&
          leftTransformed.Object == expression.Parameters[0])) {
        throw new InvalidOperationException(
          "Where expressions must have one side be a field operation on a ParseObject.");
      }

      var fieldPath = GetValue(leftTransformed.Arguments[0]) as string;
      var filterValue = GetValue(node.Right);

      if (filterValue != null && !ParseEncoder.IsValidType(filterValue)) {
        throw new InvalidOperationException(
          "Where clauses must use types compatible with ParseObjects.");
      }

      switch (node.NodeType) {
        case ExpressionType.GreaterThan:
          return source.WhereGreaterThan(fieldPath, filterValue);
        case ExpressionType.GreaterThanOrEqual:
          return source.WhereGreaterThanOrEqualTo(fieldPath, filterValue);
        case ExpressionType.LessThan:
          return source.WhereLessThan(fieldPath, filterValue);
        case ExpressionType.LessThanOrEqual:
          return source.WhereLessThanOrEqualTo(fieldPath, filterValue);
        case ExpressionType.Equal:
          return source.WhereEqualTo(fieldPath, filterValue);
        case ExpressionType.NotEqual:
          return source.WhereNotEqualTo(fieldPath, filterValue);
        default:
          throw new InvalidOperationException(
            "Where expressions do not support this operator.");
      }
    }

    /// <summary>
    /// Filters a query based upon the predicate provided.
    /// </summary>
    /// <typeparam name="TSource">The type of ParseObject being queried for.</typeparam>
    /// <param name="source">The base <see cref="ParseQuery{TSource}"/> to which
    /// the predicate will be added.</param>
    /// <param name="predicate">A function to test each ParseObject for a condition.
    /// The predicate must be able to be represented by one of the standard Where
    /// functions on ParseQuery</param>
    /// <returns>A new ParseQuery whose results will match the given predicate as
    /// well as the source's filters.</returns>
    public static ParseQuery<TSource> Where<TSource>(
        this ParseQuery<TSource> source, Expression<Func<TSource, bool>> predicate)
        where TSource : ParseObject {
      // Handle top-level logic operators && and ||
      var binaryExpression = predicate.Body as BinaryExpression;
      if (binaryExpression != null) {
        if (binaryExpression.NodeType == ExpressionType.AndAlso) {
          return source
              .Where(Expression.Lambda<Func<TSource, bool>>(
                  binaryExpression.Left, predicate.Parameters))
              .Where(Expression.Lambda<Func<TSource, bool>>(
                  binaryExpression.Right, predicate.Parameters));
        }
        if (binaryExpression.NodeType == ExpressionType.OrElse) {
          var left = source.Where(Expression.Lambda<Func<TSource, bool>>(
                  binaryExpression.Left, predicate.Parameters));
          var right = source.Where(Expression.Lambda<Func<TSource, bool>>(
                  binaryExpression.Right, predicate.Parameters));
          return left.Or(right);
        }
      }

      var normalized = new WhereNormalizer().Visit(predicate.Body);

      var methodCallExpr = normalized as MethodCallExpression;
      if (methodCallExpr != null) {
        return source.WhereMethodCall(predicate, methodCallExpr);
      }

      var binaryExpr = normalized as BinaryExpression;
      if (binaryExpr != null) {
        return source.WhereBinaryExpression(predicate, binaryExpr);
      }

      var unaryExpr = normalized as UnaryExpression;
      if (unaryExpr != null && unaryExpr.NodeType == ExpressionType.Not) {
        var node = unaryExpr.Operand as MethodCallExpression;
        if (IsParseObjectGet(node) && (node.Type == typeof(bool) || node.Type == typeof(bool?))) {
          // This is a raw boolean field access like 'where !obj.Get<bool>("foo")'
          return source.WhereNotEqualTo(GetValue(node.Arguments[0]) as string, true);
        }
      }

      throw new InvalidOperationException(
        "Encountered an unsupported expression for ParseQueries.");
    }

    /// <summary>
    /// Normalizes an OrderBy's keySelector expression and then extracts the path
    /// from the ParseObject.Get() call.
    /// </summary>
    private static string GetOrderByPath<TSource, TSelector>(
          Expression<Func<TSource, TSelector>> keySelector) {
      string result = null;
      var normalized = new ObjectNormalizer().Visit(keySelector.Body);
      var callExpr = normalized as MethodCallExpression;
      if (IsParseObjectGet(callExpr) && callExpr.Object == keySelector.Parameters[0]) {
        // We're operating on the parameter
        result = GetValue(callExpr.Arguments[0]) as string;
      }
      if (result == null) {
        throw new InvalidOperationException(
          "OrderBy expression must be a field access on a ParseObject.");
      }
      return result;
    }

    /// <summary>
    /// Orders a query based upon the key selector provided.
    /// </summary>
    /// <typeparam name="TSource">The type of ParseObject being queried for.</typeparam>
    /// <typeparam name="TSelector">The type of key returned by keySelector.</typeparam>
    /// <param name="source">The query to order.</param>
    /// <param name="keySelector">A function to extract a key from the ParseObject.</param>
    /// <returns>A new ParseQuery based on source whose results will be ordered by
    /// the key specified in the keySelector.</returns>
    public static ParseQuery<TSource> OrderBy<TSource, TSelector>(
        this ParseQuery<TSource> source, Expression<Func<TSource, TSelector>> keySelector)
        where TSource : ParseObject {
      return source.OrderBy(GetOrderByPath(keySelector));
    }

    /// <summary>
    /// Orders a query based upon the key selector provided.
    /// </summary>
    /// <typeparam name="TSource">The type of ParseObject being queried for.</typeparam>
    /// <typeparam name="TSelector">The type of key returned by keySelector.</typeparam>
    /// <param name="source">The query to order.</param>
    /// <param name="keySelector">A function to extract a key from the ParseObject.</param>
    /// <returns>A new ParseQuery based on source whose results will be ordered by
    /// the key specified in the keySelector.</returns>
    public static ParseQuery<TSource> OrderByDescending<TSource, TSelector>(
        this ParseQuery<TSource> source, Expression<Func<TSource, TSelector>> keySelector)
        where TSource : ParseObject {
      return source.OrderByDescending(GetOrderByPath(keySelector));
    }

    /// <summary>
    /// Performs a subsequent ordering of a query based upon the key selector provided.
    /// </summary>
    /// <typeparam name="TSource">The type of ParseObject being queried for.</typeparam>
    /// <typeparam name="TSelector">The type of key returned by keySelector.</typeparam>
    /// <param name="source">The query to order.</param>
    /// <param name="keySelector">A function to extract a key from the ParseObject.</param>
    /// <returns>A new ParseQuery based on source whose results will be ordered by
    /// the key specified in the keySelector.</returns>
    public static ParseQuery<TSource> ThenBy<TSource, TSelector>(
        this ParseQuery<TSource> source, Expression<Func<TSource, TSelector>> keySelector)
        where TSource : ParseObject {
      return source.ThenBy(GetOrderByPath(keySelector));
    }

    /// <summary>
    /// Performs a subsequent ordering of a query based upon the key selector provided.
    /// </summary>
    /// <typeparam name="TSource">The type of ParseObject being queried for.</typeparam>
    /// <typeparam name="TSelector">The type of key returned by keySelector.</typeparam>
    /// <param name="source">The query to order.</param>
    /// <param name="keySelector">A function to extract a key from the ParseObject.</param>
    /// <returns>A new ParseQuery based on source whose results will be ordered by
    /// the key specified in the keySelector.</returns>
    public static ParseQuery<TSource> ThenByDescending<TSource, TSelector>(
        this ParseQuery<TSource> source, Expression<Func<TSource, TSelector>> keySelector)
        where TSource : ParseObject {
      return source.ThenByDescending(GetOrderByPath(keySelector));
    }

    /// <summary>
    /// Correlates the elements of two queries based on matching keys.
    /// </summary>
    /// <typeparam name="TOuter">The type of ParseObjects of the first query.</typeparam>
    /// <typeparam name="TInner">The type of ParseObjects of the second query.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector
    /// functions.</typeparam>
    /// <typeparam name="TResult">The type of the result. This must match either
    /// TOuter or TInner</typeparam>
    /// <param name="outer">The first query to join.</param>
    /// <param name="inner">The query to join to the first query.</param>
    /// <param name="outerKeySelector">A function to extract a join key from the results of
    /// the first query.</param>
    /// <param name="innerKeySelector">A function to extract a join key from the results of
    /// the second query.</param>
    /// <param name="resultSelector">A function to select either the outer or inner query
    /// result to determine which query is the base query.</param>
    /// <returns>A new ParseQuery with a WhereMatchesQuery or WhereMatchesKeyInQuery
    /// clause based upon the query indicated in the <paramref name="resultSelector"/>.</returns>
    public static ParseQuery<TResult> Join<TOuter, TInner, TKey, TResult>(
        this ParseQuery<TOuter> outer,
        ParseQuery<TInner> inner,
        Expression<Func<TOuter, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<TOuter, TInner, TResult>> resultSelector)
      where TOuter : ParseObject
      where TInner : ParseObject
      where TResult : ParseObject {
      // resultSelector must select either the inner object or the outer object. If it's the inner
      // object, reverse the query.
      if (resultSelector.Body == resultSelector.Parameters[1]) {
        // The inner object was selected.
        return inner.Join<TInner, TOuter, TKey, TInner>(
            outer,
            innerKeySelector,
            outerKeySelector,
            (i, o) => i) as ParseQuery<TResult>;
      }
      if (resultSelector.Body != resultSelector.Parameters[0]) {
        throw new InvalidOperationException("Joins must select either the outer or inner object.");
      }

      // Normalize both selectors
      Expression outerNormalized = new ObjectNormalizer().Visit(outerKeySelector.Body);
      Expression innerNormalized = new ObjectNormalizer().Visit(innerKeySelector.Body);
      MethodCallExpression outerAsGet = outerNormalized as MethodCallExpression;
      MethodCallExpression innerAsGet = innerNormalized as MethodCallExpression;
      if (IsParseObjectGet(outerAsGet) && outerAsGet.Object == outerKeySelector.Parameters[0]) {
        var outerKey = GetValue(outerAsGet.Arguments[0]) as string;

        if (IsParseObjectGet(innerAsGet) && innerAsGet.Object == innerKeySelector.Parameters[0]) {
          // Both are key accesses, so treat this as a WhereMatchesKeyInQuery
          var innerKey = GetValue(innerAsGet.Arguments[0]) as string;
          return outer.WhereMatchesKeyInQuery(outerKey, innerKey, inner) as ParseQuery<TResult>;
        }

        if (innerKeySelector.Body == innerKeySelector.Parameters[0]) {
          // The inner selector is on the result of the query itself, so treat this as a
          // WhereMatchesQuery
          return outer.WhereMatchesQuery(outerKey, inner) as ParseQuery<TResult>;
        }
        throw new InvalidOperationException(
            "The key for the joined object must be a ParseObject or a field access " +
            "on the ParseObject.");
      }

      // TODO (hallucinogen): If we ever support "and" queries fully and/or support a "where this object
      // matches some key in some other query" (as opposed to requiring a key on this query), we
      // can add support for even more types of joins.

      throw new InvalidOperationException(
        "The key for the selected object must be a field access on the ParseObject.");
    }
  }
}
