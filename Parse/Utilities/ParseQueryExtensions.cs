using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Parse.Infrastructure.Data;

namespace Parse.Abstractions.Internal
{
#pragma warning disable CS1030 // #warning directive
#warning Fully refactor at some point.

    /// <summary>
    /// So here's the deal. We have a lot of internal APIs for ParseObject, ParseUser, etc.
    ///
    /// These cannot be 'internal' anymore if we are fully modularizing things out, because
    /// they are no longer a part of the same library, especially as we create things like
    /// Installation inside push library.
    ///
    /// So this class contains a bunch of extension methods that can live inside another
    /// namespace, which 'wrap' the intenral APIs that already exist.
    /// </summary>
    public static class ParseQueryExtensions
#pragma warning restore CS1030 // #warning directive
    {
        static MethodInfo ParseObjectGetMethod { get; }

        static MethodInfo StringContainsMethod { get; }

        static MethodInfo StringStartsWithMethod { get; }

        static MethodInfo StringEndsWithMethod { get; }

        static MethodInfo ContainsMethod { get; }

        static MethodInfo NotContainsMethod { get; }

        static MethodInfo ContainsKeyMethod { get; }

        static MethodInfo NotContainsKeyMethod { get; }

        static Dictionary<MethodInfo, MethodInfo> Mappings { get; }

        static ParseQueryExtensions()
        {
            ParseObjectGetMethod = GetMethod<ParseObject>(target => target.Get<int>(null)).GetGenericMethodDefinition();
            StringContainsMethod = GetMethod<string>(text => text.Contains(null));
            StringStartsWithMethod = GetMethod<string>(text => text.StartsWith(null));
            StringEndsWithMethod = GetMethod<string>(text => text.EndsWith(null));

            Mappings = new Dictionary<MethodInfo, MethodInfo>
            {
                [StringContainsMethod] = GetMethod<ParseQuery<ParseObject>>(query => query.WhereContains(null, null)),
                [StringStartsWithMethod] = GetMethod<ParseQuery<ParseObject>>(query => query.WhereStartsWith(null, null)),
                [StringEndsWithMethod] = GetMethod<ParseQuery<ParseObject>>(query => query.WhereEndsWith(null, null)),
            };

            ContainsMethod = GetMethod<object>(o => ContainsStub<object>(null, null)).GetGenericMethodDefinition();
            NotContainsMethod = GetMethod<object>(o => NotContainsStub<object>(null, null)).GetGenericMethodDefinition();

            ContainsKeyMethod = GetMethod<object>(o => ContainsKeyStub(null, null));
            NotContainsKeyMethod = GetMethod<object>(o => NotContainsKeyStub(null, null));
        }

        /// <summary>
        /// Gets a MethodInfo for a top-level method call.
        /// </summary>
        static MethodInfo GetMethod<T>(Expression<Action<T>> expression)
        {
            return (expression.Body as MethodCallExpression).Method;
        }

        /// <summary>
        /// When a query is normalized, this is a placeholder to indicate we should
        /// add a WhereContainedIn() clause.
        /// </summary>
        static bool ContainsStub<T>(object collection, T value)
        {
            throw new NotImplementedException("Exists only for expression translation as a placeholder.");
        }

        /// <summary>
        /// When a query is normalized, this is a placeholder to indicate we should
        /// add a WhereNotContainedIn() clause.
        /// </summary>
        static bool NotContainsStub<T>(object collection, T value)
        {
            throw new NotImplementedException("Exists only for expression translation as a placeholder.");
        }

        /// <summary>
        /// When a query is normalized, this is a placeholder to indicate that we should
        /// add a WhereExists() clause.
        /// </summary>
        static bool ContainsKeyStub(ParseObject obj, string key)
        {
            throw new NotImplementedException("Exists only for expression translation as a placeholder.");
        }

        /// <summary>
        /// When a query is normalized, this is a placeholder to indicate that we should
        /// add a WhereDoesNotExist() clause.
        /// </summary>
        static bool NotContainsKeyStub(ParseObject obj, string key)
        {
            throw new NotImplementedException("Exists only for expression translation as a placeholder.");
        }

        /// <summary>
        /// Evaluates an expression and throws if the expression has components that can't be
        /// evaluated (e.g. uses the parameter that's only represented by an object on the server).
        /// </summary>
        static object GetValue(Expression exp)
        {
            try
            {
                return Expression.Lambda(typeof(Func<>).MakeGenericType(exp.Type), exp).Compile().DynamicInvoke();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Unable to evaluate expression: " + exp, e);
            }
        }

        /// <summary>
        /// Checks whether the MethodCallExpression is a call to ParseObject.Get(),
        /// which is the call we normalize all indexing into the ParseObject to.
        /// </summary>
        static bool IsParseObjectGet(MethodCallExpression node)
        {
            return node is { Object: { } } && typeof(ParseObject).GetTypeInfo().IsAssignableFrom(node.Object.Type.GetTypeInfo()) && node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == ParseObjectGetMethod;
        }

        /// <summary>
        /// Visits an Expression, converting ParseObject.Get/ParseObject[]/ParseObject.Property,
        /// and nested indices into a single call to ParseObject.Get() with a "field path" like
        /// "foo.bar.baz"
        /// </summary>
        class ObjectNormalizer : ExpressionVisitor
        {
            protected override Expression VisitIndex(IndexExpression node)
            {
                Expression visitedObject = Visit(node.Object);
                MethodCallExpression indexer = visitedObject as MethodCallExpression;

                if (IsParseObjectGet(indexer))
                {
                    if (!(GetValue(node.Arguments[0]) is string indexValue))
                    {
                        throw new InvalidOperationException("Index must be a string");
                    }

                    return Expression.Call(indexer.Object, ParseObjectGetMethod.MakeGenericMethod(node.Type), Expression.Constant($"{GetValue(indexer.Arguments[0])}.{indexValue}", typeof(string)));
                }

                return base.VisitIndex(node);
            }

            /// <summary>
            /// Check for a ParseFieldName attribute and use that as the path component, turning
            /// properties like foo.ObjectId into foo.Get("objectId")
            /// </summary>
            protected override Expression VisitMember(MemberExpression node)
            {
                return node.Member.GetCustomAttribute<ParseFieldNameAttribute>() is { } fieldName && typeof(ParseObject).GetTypeInfo().IsAssignableFrom(node.Expression.Type.GetTypeInfo()) ? Expression.Call(node.Expression, ParseObjectGetMethod.MakeGenericMethod(node.Type), Expression.Constant(fieldName.FieldName, typeof(string))) : base.VisitMember(node);
            }

            /// <summary>
            /// If a ParseObject.Get() call has been cast, just change the generic parameter.
            /// </summary>
            protected override Expression VisitUnary(UnaryExpression node)
            {
                MethodCallExpression methodCall = Visit(node.Operand) as MethodCallExpression;
                return (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked) && IsParseObjectGet(methodCall) ? Expression.Call(methodCall.Object, ParseObjectGetMethod.MakeGenericMethod(node.Type), methodCall.Arguments) : base.VisitUnary(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                // Turn parseObject["foo"] into parseObject.Get<object>("foo")

                if (node.Method.Name == "get_Item" && node.Object is ParameterExpression)
                {
                    return Expression.Call(node.Object, ParseObjectGetMethod.MakeGenericMethod(typeof(object)), Expression.Constant(GetValue(node.Arguments[0]) as string, typeof(string)));
                }

                // Turn parseObject.Get<object>("foo")["bar"] into parseObject.Get<object>("foo.bar")

                if (node.Method.Name == "get_Item" || IsParseObjectGet(node))
                {
                    Expression visitedObject = Visit(node.Object);
                    MethodCallExpression indexer = visitedObject as MethodCallExpression;

                    if (IsParseObjectGet(indexer))
                    {
                        if (!(GetValue(node.Arguments[0]) is string indexValue))
                        {
                            throw new InvalidOperationException("Index must be a string");
                        }

                        return Expression.Call(indexer.Object, ParseObjectGetMethod.MakeGenericMethod(node.Type), Expression.Constant($"{GetValue(indexer.Arguments[0])}.{indexValue}", typeof(string)));
                    }
                }

                return base.VisitMethodCall(node);
            }
        }

        /// <summary>
        /// Normalizes Where expressions.
        /// </summary>
        class WhereNormalizer : ExpressionVisitor
        {

            /// <summary>
            /// Normalizes binary operators. &lt;, &gt;, &lt;=, &gt;= !=, and ==
            /// This puts the ParseObject.Get() on the left side of the operation
            /// (reversing it if necessary), and normalizes the ParseObject.Get()
            /// </summary>
            protected override Expression VisitBinary(BinaryExpression node)
            {
                MethodCallExpression rightTransformed = new ObjectNormalizer().Visit(node.Right) as MethodCallExpression, objectExpression;
                Expression filterExpression;
                bool inverted;

                if (new ObjectNormalizer().Visit(node.Left) is MethodCallExpression leftTransformed)
                {
                    objectExpression = leftTransformed;
                    filterExpression = node.Right;
                    inverted = false;
                }
                else
                {
                    objectExpression = rightTransformed;
                    filterExpression = node.Left;
                    inverted = true;
                }

                try
                {
                    switch (node.NodeType)
                    {
                        case ExpressionType.GreaterThan:
                            return inverted ? Expression.LessThan(objectExpression, filterExpression) : Expression.GreaterThan(objectExpression, filterExpression);
                        case ExpressionType.GreaterThanOrEqual:
                            return inverted ? Expression.LessThanOrEqual(objectExpression, filterExpression) : Expression.GreaterThanOrEqual(objectExpression, filterExpression);
                        case ExpressionType.LessThan:
                            return inverted ? Expression.GreaterThan(objectExpression, filterExpression) : Expression.LessThan(objectExpression, filterExpression);
                        case ExpressionType.LessThanOrEqual:
                            return inverted ? Expression.GreaterThanOrEqual(objectExpression, filterExpression) : Expression.LessThanOrEqual(objectExpression, filterExpression);
                        case ExpressionType.Equal:
                            return Expression.Equal(objectExpression, filterExpression);
                        case ExpressionType.NotEqual:
                            return Expression.NotEqual(objectExpression, filterExpression);
                    }
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException("Operation not supported: " + node);
                }

                return base.VisitBinary(node);
            }

            /// <summary>
            /// If a ! operator is used, this removes the ! and instead calls the equivalent
            /// function (so e.g. == becomes !=, &lt; becomes &gt;=, Contains becomes NotContains)
            /// </summary>
            protected override Expression VisitUnary(UnaryExpression node)
            {
                // This is incorrect because control is supposed to be able to flow out of the binaryOperand case if the value of NodeType is not matched against an ExpressionType value, which it will not do.
                //
                // return node switch
                // {
                //     { NodeType: ExpressionType.Not, Operand: var operand } => Visit(operand) switch
                //     {
                //         BinaryExpression { Left: var left, Right: var right, NodeType: var type } binaryOperand => type switch
                //         {
                //             ExpressionType.GreaterThan => Expression.LessThanOrEqual(left, right),
                //             ExpressionType.GreaterThanOrEqual => Expression.LessThan(left, right),
                //             ExpressionType.LessThan => Expression.GreaterThanOrEqual(left, right),
                //             ExpressionType.LessThanOrEqual => Expression.GreaterThan(left, right),
                //             ExpressionType.Equal => Expression.NotEqual(left, right),
                //             ExpressionType.NotEqual => Expression.Equal(left, right),
                //         },
                //         _ => base.VisitUnary(node)
                //     },
                //     _ => base.VisitUnary(node)
                // };

                // Normalizes inversion

                if (node.NodeType == ExpressionType.Not)
                {
                    Expression visitedOperand = Visit(node.Operand);
                    if (visitedOperand is BinaryExpression binaryOperand)
                    {
                        switch (binaryOperand.NodeType)
                        {
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

                    if (visitedOperand is MethodCallExpression methodCallOperand)
                    {
                        if (methodCallOperand.Method.IsGenericMethod)
                        {
                            if (methodCallOperand.Method.GetGenericMethodDefinition() == ContainsMethod)
                            {
                                return Expression.Call(NotContainsMethod.MakeGenericMethod(methodCallOperand.Method.GetGenericArguments()), methodCallOperand.Arguments.ToArray());
                            }
                            if (methodCallOperand.Method.GetGenericMethodDefinition() == NotContainsMethod)
                            {
                                return Expression.Call(ContainsMethod.MakeGenericMethod(methodCallOperand.Method.GetGenericArguments()), methodCallOperand.Arguments.ToArray());
                            }
                        }
                        if (methodCallOperand.Method == ContainsKeyMethod)
                        {
                            return Expression.Call(NotContainsKeyMethod, methodCallOperand.Arguments.ToArray());
                        }
                        if (methodCallOperand.Method == NotContainsKeyMethod)
                        {
                            return Expression.Call(ContainsKeyMethod, methodCallOperand.Arguments.ToArray());
                        }
                    }
                }
                return base.VisitUnary(node);
            }

            /// <summary>
            /// Normalizes .Equals into == and Contains() into the appropriate stub.
            /// </summary>
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                // Convert .Equals() into ==

                if (node.Method.Name == "Equals" && node.Method.ReturnType == typeof(bool) && node.Method.GetParameters().Length == 1)
                {
                    MethodCallExpression obj = new ObjectNormalizer().Visit(node.Object) as MethodCallExpression, parameter = new ObjectNormalizer().Visit(node.Arguments[0]) as MethodCallExpression;

                    if (IsParseObjectGet(obj) && obj.Object is ParameterExpression || IsParseObjectGet(parameter) && parameter.Object is ParameterExpression)
                    {
                        return Expression.Equal(node.Object, node.Arguments[0]);
                    }
                }

                // Convert the .Contains() into a ContainsStub

                if (node.Method != StringContainsMethod && node.Method.Name == "Contains" && node.Method.ReturnType == typeof(bool) && node.Method.GetParameters().Length <= 2)
                {
                    Expression collection = node.Method.GetParameters().Length == 1 ? node.Object : node.Arguments[0];
                    int parameterIndex = node.Method.GetParameters().Length - 1;

                    if (new ObjectNormalizer().Visit(node.Arguments[parameterIndex]) is MethodCallExpression { } parameter && IsParseObjectGet(parameter) && parameter.Object is ParameterExpression)
                    {
                        return Expression.Call(ContainsMethod.MakeGenericMethod(parameter.Type), collection, parameter);
                    }

                    if (new ObjectNormalizer().Visit(collection) is MethodCallExpression { } target && IsParseObjectGet(target) && target.Object is ParameterExpression)
                    {
                        Expression element = node.Arguments[parameterIndex];
                        return Expression.Call(ContainsMethod.MakeGenericMethod(element.Type), target, element);
                    }
                }

                // Convert obj["foo.bar"].ContainsKey("baz") into obj.ContainsKey("foo.bar.baz").

                if (node.Method.Name == "ContainsKey" && node.Method.ReturnType == typeof(bool) && node.Method.GetParameters().Length == 1)
                {
                    Expression target = null;
                    string path = null;

                    if (new ObjectNormalizer().Visit(node.Object) is MethodCallExpression { } getter && IsParseObjectGet(getter) && getter.Object is ParameterExpression)
                    {
                        return Expression.Call(ContainsKeyMethod, getter.Object, Expression.Constant($"{GetValue(getter.Arguments[0])}.{GetValue(node.Arguments[0])}"));
                    }
                    else if (node.Object is ParameterExpression)
                    {
                        target = node.Object;
                        path = GetValue(node.Arguments[0]) as string;
                    }

                    if (target is { } && path is { })
                    {
                        return Expression.Call(ContainsKeyMethod, target, Expression.Constant(path));
                    }
                }
                return base.VisitMethodCall(node);
            }
        }

        /// <summary>
        /// Converts a normalized method call expression into the appropriate ParseQuery clause.
        /// </summary>
        static ParseQuery<T> WhereMethodCall<T>(this ParseQuery<T> source, Expression<Func<T, bool>> expression, MethodCallExpression node) where T : ParseObject
        {
            if (IsParseObjectGet(node) && (node.Type == typeof(bool) || node.Type == typeof(bool?)))
            {
                // This is a raw boolean field access like 'where obj.Get<bool>("foo")'.

                return source.WhereEqualTo(GetValue(node.Arguments[0]) as string, true);
            }

            if (Mappings.TryGetValue(node.Method, out MethodInfo translatedMethod))
            {
                MethodCallExpression objTransformed = new ObjectNormalizer().Visit(node.Object) as MethodCallExpression;

                if (!(IsParseObjectGet(objTransformed) && objTransformed.Object == expression.Parameters[0]))
                {
                    throw new InvalidOperationException("The left-hand side of a supported function call must be a ParseObject field access.");
                }

                return translatedMethod.DeclaringType.GetGenericTypeDefinition().MakeGenericType(typeof(T)).GetRuntimeMethod(translatedMethod.Name, translatedMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray()).Invoke(source, new[] { GetValue(objTransformed.Arguments[0]), GetValue(node.Arguments[0]) }) as ParseQuery<T>;
            }

            if (node.Arguments[0] == expression.Parameters[0])
            {
                // obj.ContainsKey("foo") --> query.WhereExists("foo")

                if (node.Method == ContainsKeyMethod)
                {
                    return source.WhereExists(GetValue(node.Arguments[1]) as string);
                }

                // !obj.ContainsKey("foo") --> query.WhereDoesNotExist("foo")

                if (node.Method == NotContainsKeyMethod)
                {
                    return source.WhereDoesNotExist(GetValue(node.Arguments[1]) as string);
                }
            }

            if (node.Method.IsGenericMethod)
            {
                if (node.Method.GetGenericMethodDefinition() == ContainsMethod)
                {
                    // obj.Get<IList<T>>("path").Contains(someValue)

                    if (IsParseObjectGet(node.Arguments[0] as MethodCallExpression))
                    {
                        return source.WhereEqualTo(GetValue(((MethodCallExpression) node.Arguments[0]).Arguments[0]) as string, GetValue(node.Arguments[1]));
                    }

                    // someList.Contains(obj.Get<T>("path"))

                    if (IsParseObjectGet(node.Arguments[1] as MethodCallExpression))
                    {
                        return source.WhereContainedIn(GetValue(((MethodCallExpression) node.Arguments[1]).Arguments[0]) as string, (GetValue(node.Arguments[0]) as IEnumerable).Cast<object>());
                    }
                }

                if (node.Method.GetGenericMethodDefinition() == NotContainsMethod)
                {
                    // !obj.Get<IList<T>>("path").Contains(someValue)

                    if (IsParseObjectGet(node.Arguments[0] as MethodCallExpression))
                    {
                        return source.WhereNotEqualTo(GetValue(((MethodCallExpression) node.Arguments[0]).Arguments[0]) as string, GetValue(node.Arguments[1]));
                    }

                    // !someList.Contains(obj.Get<T>("path"))

                    if (IsParseObjectGet(node.Arguments[1] as MethodCallExpression))
                    {
                        return source.WhereNotContainedIn(GetValue(((MethodCallExpression) node.Arguments[1]).Arguments[0]) as string, (GetValue(node.Arguments[0]) as IEnumerable).Cast<object>());
                    }
                }
            }
            throw new InvalidOperationException(node.Method + " is not a supported method call in a where expression.");
        }

        /// <summary>
        /// Converts a normalized binary expression into the appropriate ParseQuery clause.
        /// </summary>
        static ParseQuery<T> WhereBinaryExpression<T>(this ParseQuery<T> source, Expression<Func<T, bool>> expression, BinaryExpression node) where T : ParseObject
        {
            MethodCallExpression leftTransformed = new ObjectNormalizer().Visit(node.Left) as MethodCallExpression;

            if (!(IsParseObjectGet(leftTransformed) && leftTransformed.Object == expression.Parameters[0]))
            {
                throw new InvalidOperationException("Where expressions must have one side be a field operation on a ParseObject.");
            }

            string fieldPath = GetValue(leftTransformed.Arguments[0]) as string;
            object filterValue = GetValue(node.Right);

            if (filterValue != null && !ParseDataEncoder.Validate(filterValue))
            {
                throw new InvalidOperationException("Where clauses must use types compatible with ParseObjects.");
            }

            return node.NodeType switch
            {
                ExpressionType.GreaterThan => source.WhereGreaterThan(fieldPath, filterValue),
                ExpressionType.GreaterThanOrEqual => source.WhereGreaterThanOrEqualTo(fieldPath, filterValue),
                ExpressionType.LessThan => source.WhereLessThan(fieldPath, filterValue),
                ExpressionType.LessThanOrEqual => source.WhereLessThanOrEqualTo(fieldPath, filterValue),
                ExpressionType.Equal => source.WhereEqualTo(fieldPath, filterValue),
                ExpressionType.NotEqual => source.WhereNotEqualTo(fieldPath, filterValue),
                _ => throw new InvalidOperationException("Where expressions do not support this operator."),
            };
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
        public static ParseQuery<TSource> Where<TSource>(this ParseQuery<TSource> source, Expression<Func<TSource, bool>> predicate) where TSource : ParseObject
        {
            // Handle top-level logic operators && and ||

            if (predicate.Body is BinaryExpression binaryExpression)
            {
                if (binaryExpression.NodeType == ExpressionType.AndAlso)
                {
                    return source.Where(Expression.Lambda<Func<TSource, bool>>(binaryExpression.Left, predicate.Parameters)).Where(Expression.Lambda<Func<TSource, bool>>(binaryExpression.Right, predicate.Parameters));
                }

                if (binaryExpression.NodeType == ExpressionType.OrElse)
                {
                    return source.Services.ConstructOrQuery(source.Where(Expression.Lambda<Func<TSource, bool>>(binaryExpression.Left, predicate.Parameters)), (ParseQuery<TSource>) source.Where(Expression.Lambda<Func<TSource, bool>>(binaryExpression.Right, predicate.Parameters)));
                }
            }

            Expression normalized = new WhereNormalizer().Visit(predicate.Body);

            if (normalized is MethodCallExpression methodCallExpr)
            {
                return source.WhereMethodCall(predicate, methodCallExpr);
            }

            if (normalized is BinaryExpression binaryExpr)
            {
                return source.WhereBinaryExpression(predicate, binaryExpr);
            }

            if (normalized is UnaryExpression { NodeType: ExpressionType.Not, Operand: MethodCallExpression { } node, Type: var type } unaryExpr && IsParseObjectGet(node) && (type == typeof(bool) || type == typeof(bool?)))
            {
                // This is a raw boolean field access like 'where !obj.Get<bool>("foo")'.

                return source.WhereNotEqualTo(GetValue(node.Arguments[0]) as string, true);
            }

            throw new InvalidOperationException("Encountered an unsupported expression for ParseQueries.");
        }

        /// <summary>
        /// Normalizes an OrderBy's keySelector expression and then extracts the path
        /// from the ParseObject.Get() call.
        /// </summary>
        static string GetOrderByPath<TSource, TSelector>(Expression<Func<TSource, TSelector>> keySelector)
        {
            string result = null;
            Expression normalized = new ObjectNormalizer().Visit(keySelector.Body);
            MethodCallExpression callExpr = normalized as MethodCallExpression;

            if (IsParseObjectGet(callExpr) && callExpr.Object == keySelector.Parameters[0])
            {
                // We're operating on the parameter

                result = GetValue(callExpr.Arguments[0]) as string;
            }

            if (result == null)
            {
                throw new InvalidOperationException("OrderBy expression must be a field access on a ParseObject.");
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
        public static ParseQuery<TSource> OrderBy<TSource, TSelector>(this ParseQuery<TSource> source, Expression<Func<TSource, TSelector>> keySelector) where TSource : ParseObject
        {
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
        public static ParseQuery<TSource> OrderByDescending<TSource, TSelector>(this ParseQuery<TSource> source, Expression<Func<TSource, TSelector>> keySelector) where TSource : ParseObject
        {
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
        public static ParseQuery<TSource> ThenBy<TSource, TSelector>(this ParseQuery<TSource> source, Expression<Func<TSource, TSelector>> keySelector) where TSource : ParseObject
        {
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
        public static ParseQuery<TSource> ThenByDescending<TSource, TSelector>(this ParseQuery<TSource> source, Expression<Func<TSource, TSelector>> keySelector) where TSource : ParseObject
        {
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
        public static ParseQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this ParseQuery<TOuter> outer, ParseQuery<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector) where TOuter : ParseObject where TInner : ParseObject where TResult : ParseObject
        {
            // resultSelector must select either the inner object or the outer object. If it's the inner object, reverse the query.

            if (resultSelector.Body == resultSelector.Parameters[1])
            {
                // The inner object was selected.

                return inner.Join(outer, innerKeySelector, outerKeySelector, (i, o) => i) as ParseQuery<TResult>;
            }

            if (resultSelector.Body != resultSelector.Parameters[0])
            {
                throw new InvalidOperationException("Joins must select either the outer or inner object.");
            }

            // Normalize both selectors
            Expression outerNormalized = new ObjectNormalizer().Visit(outerKeySelector.Body), innerNormalized = new ObjectNormalizer().Visit(innerKeySelector.Body);
            MethodCallExpression outerAsGet = outerNormalized as MethodCallExpression, innerAsGet = innerNormalized as MethodCallExpression;

            if (IsParseObjectGet(outerAsGet) && outerAsGet.Object == outerKeySelector.Parameters[0])
            {
                string outerKey = GetValue(outerAsGet.Arguments[0]) as string;

                if (IsParseObjectGet(innerAsGet) && innerAsGet.Object == innerKeySelector.Parameters[0])
                {
                    // Both are key accesses, so treat this as a WhereMatchesKeyInQuery.

                    return outer.WhereMatchesKeyInQuery(outerKey, GetValue(innerAsGet.Arguments[0]) as string, inner) as ParseQuery<TResult>;
                }

                if (innerKeySelector.Body == innerKeySelector.Parameters[0])
                {
                    // The inner selector is on the result of the query itself, so treat this as a WhereMatchesQuery.

                    return outer.WhereMatchesQuery(outerKey, inner) as ParseQuery<TResult>;
                }

                throw new InvalidOperationException("The key for the joined object must be a ParseObject or a field access on the ParseObject.");
            }

            // TODO (hallucinogen): If we ever support "and" queries fully and/or support a "where this object
            // matches some key in some other query" (as opposed to requiring a key on this query), we
            // can add support for even more types of joins.

            throw new InvalidOperationException("The key for the selected object must be a field access on the ParseObject.");
        }

        public static string GetClassName<T>(this ParseQuery<T> query) where T : ParseObject
        {
            return query.ClassName;
        }

        public static IDictionary<string, object> BuildParameters<T>(this ParseQuery<T> query) where T : ParseObject
        {
            return query.BuildParameters(false);
        }

        public static object GetConstraint<T>(this ParseQuery<T> query, string key) where T : ParseObject
        {
            return query.GetConstraint(key);
        }
    }


    
}