// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Foundation;
using UIKit;

using Parse.Internal;

using PreserveAttribute = Foundation.PreserveAttribute;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Parse {
  [Preserve(AllMembers = true)]
  partial class PlatformHooks : IPlatformHooks {
    private IHttpClient httpClient = null;
    public IHttpClient HttpClient {
      get {
        httpClient = httpClient ?? new HttpClient();
        return httpClient;
      }
    }

    public string SDKName {
      get {
        return "xamarin-ios";
      }
    }

    public string AppName {
      get {
        return GetAppAttribute("CFBundleDisplayName");
      }
    }

    public string AppBuildVersion {
      get {
        return GetAppAttribute("CFBundleVersion");
      }
    }

    public string AppDisplayVersion {
      get {
        return GetAppAttribute("CFBundleShortVersionString");
      }
    }

    public string AppIdentifier {
      get {
        return GetAppAttribute("CFBundleIdentifier");
      }
    }

    public string OSVersion {
      get {
        return UIDevice.CurrentDevice.SystemVersion;
      }
    }

    public string DeviceType {
      get {
        return "ios";
      }
    }

    public string DeviceTimeZone {
      get {
        Foundation.NSTimeZone.ResetSystemTimeZone();
        return Foundation.NSTimeZone.SystemTimeZone.Name;
      }
    }

    public void Initialize() {
      // Do nothing.
    }

    public Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation) {
      return Task.Run(() => {
        installation.SetIfDifferent("badge", installation.Badge);
      });
    }

    /// <summary>
    /// Gets an attribute from the Info.plist.
    /// </summary>
    /// <param name="attributeName">the attribute name</param>
    /// <returns>the attribute value</returns>
    /// This is a duplicate of what we have in ParseInstallation. We do it because
    /// it's easier to maintain this way (rather than referencing <c>PlatformHooks</c> everywhere).
    private string GetAppAttribute(string attributeName) {
      var appAttributes = NSBundle.MainBundle;

      var attribute = appAttributes.ObjectForInfoDictionary(attributeName);
      return attribute == null ? null : attribute.ToString();
    }

    /// <summary>
    /// Exists to ensure that generic types are AOT-compiled for the conversions we support.
    /// Any new value types that we add support for will need to be registered here.
    /// The method itself is never called, but by virtue of the Preserve attribute being set
    /// on the PlatformHooks class, these types will be AOT-compiled.
    /// </summary>
    private static List<object> CreateWrapperTypes() {
      return new List<object> {
				(Action)(async () => await ParseCloud.CallFunctionAsync<object>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<bool>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<byte>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<sbyte>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<short>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<ushort>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<int>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<uint>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<long>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<ulong>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<char>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<double>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<float>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<IDictionary<string, object>>(null, null, CancellationToken.None)),
				(Action)(async () => await ParseCloud.CallFunctionAsync<IList<object>>(null, null, CancellationToken.None)),

				typeof(FlexibleListWrapper<object, object>),
				typeof(FlexibleListWrapper<object, bool>),
				typeof(FlexibleListWrapper<object, byte>),
				typeof(FlexibleListWrapper<object, sbyte>),
				typeof(FlexibleListWrapper<object, short>),
				typeof(FlexibleListWrapper<object, ushort>),
				typeof(FlexibleListWrapper<object, int>),
				typeof(FlexibleListWrapper<object, uint>),
				typeof(FlexibleListWrapper<object, long>),
				typeof(FlexibleListWrapper<object, ulong>),
				typeof(FlexibleListWrapper<object, char>),
				typeof(FlexibleListWrapper<object, double>),
				typeof(FlexibleListWrapper<object, float>),
				
				typeof(FlexibleListWrapper<bool, object>),
				typeof(FlexibleListWrapper<bool, bool>),
				typeof(FlexibleListWrapper<bool, byte>),
				typeof(FlexibleListWrapper<bool, sbyte>),
				typeof(FlexibleListWrapper<bool, short>),
				typeof(FlexibleListWrapper<bool, ushort>),
				typeof(FlexibleListWrapper<bool, int>),
				typeof(FlexibleListWrapper<bool, uint>),
				typeof(FlexibleListWrapper<bool, long>),
				typeof(FlexibleListWrapper<bool, ulong>),
				typeof(FlexibleListWrapper<bool, char>),
				typeof(FlexibleListWrapper<bool, double>),
				typeof(FlexibleListWrapper<bool, float>),
				
				typeof(FlexibleListWrapper<byte, object>),
				typeof(FlexibleListWrapper<byte, bool>),
				typeof(FlexibleListWrapper<byte, byte>),
				typeof(FlexibleListWrapper<byte, sbyte>),
				typeof(FlexibleListWrapper<byte, short>),
				typeof(FlexibleListWrapper<byte, ushort>),
				typeof(FlexibleListWrapper<byte, int>),
				typeof(FlexibleListWrapper<byte, uint>),
				typeof(FlexibleListWrapper<byte, long>),
				typeof(FlexibleListWrapper<byte, ulong>),
				typeof(FlexibleListWrapper<byte, char>),
				typeof(FlexibleListWrapper<byte, double>),
				typeof(FlexibleListWrapper<byte, float>),
				
				typeof(FlexibleListWrapper<sbyte, object>),
				typeof(FlexibleListWrapper<sbyte, bool>),
				typeof(FlexibleListWrapper<sbyte, byte>),
				typeof(FlexibleListWrapper<sbyte, sbyte>),
				typeof(FlexibleListWrapper<sbyte, short>),
				typeof(FlexibleListWrapper<sbyte, ushort>),
				typeof(FlexibleListWrapper<sbyte, int>),
				typeof(FlexibleListWrapper<sbyte, uint>),
				typeof(FlexibleListWrapper<sbyte, long>),
				typeof(FlexibleListWrapper<sbyte, ulong>),
				typeof(FlexibleListWrapper<sbyte, char>),
				typeof(FlexibleListWrapper<sbyte, double>),
				typeof(FlexibleListWrapper<sbyte, float>),
				
				typeof(FlexibleListWrapper<short, object>),
				typeof(FlexibleListWrapper<short, bool>),
				typeof(FlexibleListWrapper<short, byte>),
				typeof(FlexibleListWrapper<short, sbyte>),
				typeof(FlexibleListWrapper<short, short>),
				typeof(FlexibleListWrapper<short, ushort>),
				typeof(FlexibleListWrapper<short, int>),
				typeof(FlexibleListWrapper<short, uint>),
				typeof(FlexibleListWrapper<short, long>),
				typeof(FlexibleListWrapper<short, ulong>),
				typeof(FlexibleListWrapper<short, char>),
				typeof(FlexibleListWrapper<short, double>),
				typeof(FlexibleListWrapper<short, float>),
				
				typeof(FlexibleListWrapper<ushort, object>),
				typeof(FlexibleListWrapper<ushort, bool>),
				typeof(FlexibleListWrapper<ushort, byte>),
				typeof(FlexibleListWrapper<ushort, sbyte>),
				typeof(FlexibleListWrapper<ushort, short>),
				typeof(FlexibleListWrapper<ushort, ushort>),
				typeof(FlexibleListWrapper<ushort, int>),
				typeof(FlexibleListWrapper<ushort, uint>),
				typeof(FlexibleListWrapper<ushort, long>),
				typeof(FlexibleListWrapper<ushort, ulong>),
				typeof(FlexibleListWrapper<ushort, char>),
				typeof(FlexibleListWrapper<ushort, double>),
				typeof(FlexibleListWrapper<ushort, float>),
				
				typeof(FlexibleListWrapper<int, object>),
				typeof(FlexibleListWrapper<int, bool>),
				typeof(FlexibleListWrapper<int, byte>),
				typeof(FlexibleListWrapper<int, sbyte>),
				typeof(FlexibleListWrapper<int, short>),
				typeof(FlexibleListWrapper<int, ushort>),
				typeof(FlexibleListWrapper<int, int>),
				typeof(FlexibleListWrapper<int, uint>),
				typeof(FlexibleListWrapper<int, long>),
				typeof(FlexibleListWrapper<int, ulong>),
				typeof(FlexibleListWrapper<int, char>),
				typeof(FlexibleListWrapper<int, double>),
				typeof(FlexibleListWrapper<int, float>),
				
				typeof(FlexibleListWrapper<uint, object>),
				typeof(FlexibleListWrapper<uint, bool>),
				typeof(FlexibleListWrapper<uint, byte>),
				typeof(FlexibleListWrapper<uint, sbyte>),
				typeof(FlexibleListWrapper<uint, short>),
				typeof(FlexibleListWrapper<uint, ushort>),
				typeof(FlexibleListWrapper<uint, int>),
				typeof(FlexibleListWrapper<uint, uint>),
				typeof(FlexibleListWrapper<uint, long>),
				typeof(FlexibleListWrapper<uint, ulong>),
				typeof(FlexibleListWrapper<uint, char>),
				typeof(FlexibleListWrapper<uint, double>),
				typeof(FlexibleListWrapper<uint, float>),
				
				typeof(FlexibleListWrapper<long, object>),
				typeof(FlexibleListWrapper<long, bool>),
				typeof(FlexibleListWrapper<long, byte>),
				typeof(FlexibleListWrapper<long, sbyte>),
				typeof(FlexibleListWrapper<long, short>),
				typeof(FlexibleListWrapper<long, ushort>),
				typeof(FlexibleListWrapper<long, int>),
				typeof(FlexibleListWrapper<long, uint>),
				typeof(FlexibleListWrapper<long, long>),
				typeof(FlexibleListWrapper<long, ulong>),
				typeof(FlexibleListWrapper<long, char>),
				typeof(FlexibleListWrapper<long, double>),
				typeof(FlexibleListWrapper<long, float>),
				
				typeof(FlexibleListWrapper<ulong, object>),
				typeof(FlexibleListWrapper<ulong, bool>),
				typeof(FlexibleListWrapper<ulong, byte>),
				typeof(FlexibleListWrapper<ulong, sbyte>),
				typeof(FlexibleListWrapper<ulong, short>),
				typeof(FlexibleListWrapper<ulong, ushort>),
				typeof(FlexibleListWrapper<ulong, int>),
				typeof(FlexibleListWrapper<ulong, uint>),
				typeof(FlexibleListWrapper<ulong, long>),
				typeof(FlexibleListWrapper<ulong, ulong>),
				typeof(FlexibleListWrapper<ulong, char>),
				typeof(FlexibleListWrapper<ulong, double>),
				typeof(FlexibleListWrapper<ulong, float>),
				
				typeof(FlexibleListWrapper<char, object>),
				typeof(FlexibleListWrapper<char, bool>),
				typeof(FlexibleListWrapper<char, byte>),
				typeof(FlexibleListWrapper<char, sbyte>),
				typeof(FlexibleListWrapper<char, short>),
				typeof(FlexibleListWrapper<char, ushort>),
				typeof(FlexibleListWrapper<char, int>),
				typeof(FlexibleListWrapper<char, uint>),
				typeof(FlexibleListWrapper<char, long>),
				typeof(FlexibleListWrapper<char, ulong>),
				typeof(FlexibleListWrapper<char, char>),
				typeof(FlexibleListWrapper<char, double>),
				typeof(FlexibleListWrapper<char, float>),
				
				typeof(FlexibleListWrapper<double, object>),
				typeof(FlexibleListWrapper<double, bool>),
				typeof(FlexibleListWrapper<double, byte>),
				typeof(FlexibleListWrapper<double, sbyte>),
				typeof(FlexibleListWrapper<double, short>),
				typeof(FlexibleListWrapper<double, ushort>),
				typeof(FlexibleListWrapper<double, int>),
				typeof(FlexibleListWrapper<double, uint>),
				typeof(FlexibleListWrapper<double, long>),
				typeof(FlexibleListWrapper<double, ulong>),
				typeof(FlexibleListWrapper<double, char>),
				typeof(FlexibleListWrapper<double, double>),
				typeof(FlexibleListWrapper<double, float>),
				
				typeof(FlexibleListWrapper<float, object>),
				typeof(FlexibleListWrapper<float, bool>),
				typeof(FlexibleListWrapper<float, byte>),
				typeof(FlexibleListWrapper<float, sbyte>),
				typeof(FlexibleListWrapper<float, short>),
				typeof(FlexibleListWrapper<float, ushort>),
				typeof(FlexibleListWrapper<float, int>),
				typeof(FlexibleListWrapper<float, uint>),
				typeof(FlexibleListWrapper<float, long>),
				typeof(FlexibleListWrapper<float, ulong>),
				typeof(FlexibleListWrapper<float, char>),
				typeof(FlexibleListWrapper<float, double>),
				typeof(FlexibleListWrapper<float, float>),
				
				typeof(FlexibleListWrapper<object, DateTime>),
				typeof(FlexibleListWrapper<DateTime, object>),
				typeof(FlexibleListWrapper<object, ParseGeoPoint>),
				typeof(FlexibleListWrapper<ParseGeoPoint, object>),
				
				typeof(FlexibleDictionaryWrapper<object, object>),
				typeof(FlexibleDictionaryWrapper<object, bool>),
				typeof(FlexibleDictionaryWrapper<object, byte>),
				typeof(FlexibleDictionaryWrapper<object, sbyte>),
				typeof(FlexibleDictionaryWrapper<object, short>),
				typeof(FlexibleDictionaryWrapper<object, ushort>),
				typeof(FlexibleDictionaryWrapper<object, int>),
				typeof(FlexibleDictionaryWrapper<object, uint>),
				typeof(FlexibleDictionaryWrapper<object, long>),
				typeof(FlexibleDictionaryWrapper<object, ulong>),
				typeof(FlexibleDictionaryWrapper<object, char>),
				typeof(FlexibleDictionaryWrapper<object, double>),
				typeof(FlexibleDictionaryWrapper<object, float>),
				
				typeof(FlexibleDictionaryWrapper<bool, object>),
				typeof(FlexibleDictionaryWrapper<bool, bool>),
				typeof(FlexibleDictionaryWrapper<bool, byte>),
				typeof(FlexibleDictionaryWrapper<bool, sbyte>),
				typeof(FlexibleDictionaryWrapper<bool, short>),
				typeof(FlexibleDictionaryWrapper<bool, ushort>),
				typeof(FlexibleDictionaryWrapper<bool, int>),
				typeof(FlexibleDictionaryWrapper<bool, uint>),
				typeof(FlexibleDictionaryWrapper<bool, long>),
				typeof(FlexibleDictionaryWrapper<bool, ulong>),
				typeof(FlexibleDictionaryWrapper<bool, char>),
				typeof(FlexibleDictionaryWrapper<bool, double>),
				typeof(FlexibleDictionaryWrapper<bool, float>),
				
				typeof(FlexibleDictionaryWrapper<byte, object>),
				typeof(FlexibleDictionaryWrapper<byte, bool>),
				typeof(FlexibleDictionaryWrapper<byte, byte>),
				typeof(FlexibleDictionaryWrapper<byte, sbyte>),
				typeof(FlexibleDictionaryWrapper<byte, short>),
				typeof(FlexibleDictionaryWrapper<byte, ushort>),
				typeof(FlexibleDictionaryWrapper<byte, int>),
				typeof(FlexibleDictionaryWrapper<byte, uint>),
				typeof(FlexibleDictionaryWrapper<byte, long>),
				typeof(FlexibleDictionaryWrapper<byte, ulong>),
				typeof(FlexibleDictionaryWrapper<byte, char>),
				typeof(FlexibleDictionaryWrapper<byte, double>),
				typeof(FlexibleDictionaryWrapper<byte, float>),
				
				typeof(FlexibleDictionaryWrapper<sbyte, object>),
				typeof(FlexibleDictionaryWrapper<sbyte, bool>),
				typeof(FlexibleDictionaryWrapper<sbyte, byte>),
				typeof(FlexibleDictionaryWrapper<sbyte, sbyte>),
				typeof(FlexibleDictionaryWrapper<sbyte, short>),
				typeof(FlexibleDictionaryWrapper<sbyte, ushort>),
				typeof(FlexibleDictionaryWrapper<sbyte, int>),
				typeof(FlexibleDictionaryWrapper<sbyte, uint>),
				typeof(FlexibleDictionaryWrapper<sbyte, long>),
				typeof(FlexibleDictionaryWrapper<sbyte, ulong>),
				typeof(FlexibleDictionaryWrapper<sbyte, char>),
				typeof(FlexibleDictionaryWrapper<sbyte, double>),
				typeof(FlexibleDictionaryWrapper<sbyte, float>),
				
				typeof(FlexibleDictionaryWrapper<short, object>),
				typeof(FlexibleDictionaryWrapper<short, bool>),
				typeof(FlexibleDictionaryWrapper<short, byte>),
				typeof(FlexibleDictionaryWrapper<short, sbyte>),
				typeof(FlexibleDictionaryWrapper<short, short>),
				typeof(FlexibleDictionaryWrapper<short, ushort>),
				typeof(FlexibleDictionaryWrapper<short, int>),
				typeof(FlexibleDictionaryWrapper<short, uint>),
				typeof(FlexibleDictionaryWrapper<short, long>),
				typeof(FlexibleDictionaryWrapper<short, ulong>),
				typeof(FlexibleDictionaryWrapper<short, char>),
				typeof(FlexibleDictionaryWrapper<short, double>),
				typeof(FlexibleDictionaryWrapper<short, float>),
				
				typeof(FlexibleDictionaryWrapper<ushort, object>),
				typeof(FlexibleDictionaryWrapper<ushort, bool>),
				typeof(FlexibleDictionaryWrapper<ushort, byte>),
				typeof(FlexibleDictionaryWrapper<ushort, sbyte>),
				typeof(FlexibleDictionaryWrapper<ushort, short>),
				typeof(FlexibleDictionaryWrapper<ushort, ushort>),
				typeof(FlexibleDictionaryWrapper<ushort, int>),
				typeof(FlexibleDictionaryWrapper<ushort, uint>),
				typeof(FlexibleDictionaryWrapper<ushort, long>),
				typeof(FlexibleDictionaryWrapper<ushort, ulong>),
				typeof(FlexibleDictionaryWrapper<ushort, char>),
				typeof(FlexibleDictionaryWrapper<ushort, double>),
				typeof(FlexibleDictionaryWrapper<ushort, float>),
				
				typeof(FlexibleDictionaryWrapper<int, object>),
				typeof(FlexibleDictionaryWrapper<int, bool>),
				typeof(FlexibleDictionaryWrapper<int, byte>),
				typeof(FlexibleDictionaryWrapper<int, sbyte>),
				typeof(FlexibleDictionaryWrapper<int, short>),
				typeof(FlexibleDictionaryWrapper<int, ushort>),
				typeof(FlexibleDictionaryWrapper<int, int>),
				typeof(FlexibleDictionaryWrapper<int, uint>),
				typeof(FlexibleDictionaryWrapper<int, long>),
				typeof(FlexibleDictionaryWrapper<int, ulong>),
				typeof(FlexibleDictionaryWrapper<int, char>),
				typeof(FlexibleDictionaryWrapper<int, double>),
				typeof(FlexibleDictionaryWrapper<int, float>),
				
				typeof(FlexibleDictionaryWrapper<uint, object>),
				typeof(FlexibleDictionaryWrapper<uint, bool>),
				typeof(FlexibleDictionaryWrapper<uint, byte>),
				typeof(FlexibleDictionaryWrapper<uint, sbyte>),
				typeof(FlexibleDictionaryWrapper<uint, short>),
				typeof(FlexibleDictionaryWrapper<uint, ushort>),
				typeof(FlexibleDictionaryWrapper<uint, int>),
				typeof(FlexibleDictionaryWrapper<uint, uint>),
				typeof(FlexibleDictionaryWrapper<uint, long>),
				typeof(FlexibleDictionaryWrapper<uint, ulong>),
				typeof(FlexibleDictionaryWrapper<uint, char>),
				typeof(FlexibleDictionaryWrapper<uint, double>),
				typeof(FlexibleDictionaryWrapper<uint, float>),
				
				typeof(FlexibleDictionaryWrapper<long, object>),
				typeof(FlexibleDictionaryWrapper<long, bool>),
				typeof(FlexibleDictionaryWrapper<long, byte>),
				typeof(FlexibleDictionaryWrapper<long, sbyte>),
				typeof(FlexibleDictionaryWrapper<long, short>),
				typeof(FlexibleDictionaryWrapper<long, ushort>),
				typeof(FlexibleDictionaryWrapper<long, int>),
				typeof(FlexibleDictionaryWrapper<long, uint>),
				typeof(FlexibleDictionaryWrapper<long, long>),
				typeof(FlexibleDictionaryWrapper<long, ulong>),
				typeof(FlexibleDictionaryWrapper<long, char>),
				typeof(FlexibleDictionaryWrapper<long, double>),
				typeof(FlexibleDictionaryWrapper<long, float>),
				
				typeof(FlexibleDictionaryWrapper<ulong, object>),
				typeof(FlexibleDictionaryWrapper<ulong, bool>),
				typeof(FlexibleDictionaryWrapper<ulong, byte>),
				typeof(FlexibleDictionaryWrapper<ulong, sbyte>),
				typeof(FlexibleDictionaryWrapper<ulong, short>),
				typeof(FlexibleDictionaryWrapper<ulong, ushort>),
				typeof(FlexibleDictionaryWrapper<ulong, int>),
				typeof(FlexibleDictionaryWrapper<ulong, uint>),
				typeof(FlexibleDictionaryWrapper<ulong, long>),
				typeof(FlexibleDictionaryWrapper<ulong, ulong>),
				typeof(FlexibleDictionaryWrapper<ulong, char>),
				typeof(FlexibleDictionaryWrapper<ulong, double>),
				typeof(FlexibleDictionaryWrapper<ulong, float>),
				
				typeof(FlexibleDictionaryWrapper<char, object>),
				typeof(FlexibleDictionaryWrapper<char, bool>),
				typeof(FlexibleDictionaryWrapper<char, byte>),
				typeof(FlexibleDictionaryWrapper<char, sbyte>),
				typeof(FlexibleDictionaryWrapper<char, short>),
				typeof(FlexibleDictionaryWrapper<char, ushort>),
				typeof(FlexibleDictionaryWrapper<char, int>),
				typeof(FlexibleDictionaryWrapper<char, uint>),
				typeof(FlexibleDictionaryWrapper<char, long>),
				typeof(FlexibleDictionaryWrapper<char, ulong>),
				typeof(FlexibleDictionaryWrapper<char, char>),
				typeof(FlexibleDictionaryWrapper<char, double>),
				typeof(FlexibleDictionaryWrapper<char, float>),
				
				typeof(FlexibleDictionaryWrapper<double, object>),
				typeof(FlexibleDictionaryWrapper<double, bool>),
				typeof(FlexibleDictionaryWrapper<double, byte>),
				typeof(FlexibleDictionaryWrapper<double, sbyte>),
				typeof(FlexibleDictionaryWrapper<double, short>),
				typeof(FlexibleDictionaryWrapper<double, ushort>),
				typeof(FlexibleDictionaryWrapper<double, int>),
				typeof(FlexibleDictionaryWrapper<double, uint>),
				typeof(FlexibleDictionaryWrapper<double, long>),
				typeof(FlexibleDictionaryWrapper<double, ulong>),
				typeof(FlexibleDictionaryWrapper<double, char>),
				typeof(FlexibleDictionaryWrapper<double, double>),
				typeof(FlexibleDictionaryWrapper<double, float>),
				
				typeof(FlexibleDictionaryWrapper<float, object>),
				typeof(FlexibleDictionaryWrapper<float, bool>),
				typeof(FlexibleDictionaryWrapper<float, byte>),
				typeof(FlexibleDictionaryWrapper<float, sbyte>),
				typeof(FlexibleDictionaryWrapper<float, short>),
				typeof(FlexibleDictionaryWrapper<float, ushort>),
				typeof(FlexibleDictionaryWrapper<float, int>),
				typeof(FlexibleDictionaryWrapper<float, uint>),
				typeof(FlexibleDictionaryWrapper<float, long>),
				typeof(FlexibleDictionaryWrapper<float, ulong>),
				typeof(FlexibleDictionaryWrapper<float, char>),
				typeof(FlexibleDictionaryWrapper<float, double>),
				typeof(FlexibleDictionaryWrapper<float, float>),
				
				typeof(FlexibleDictionaryWrapper<object, DateTime>),
				typeof(FlexibleDictionaryWrapper<DateTime, object>),
				typeof(FlexibleDictionaryWrapper<object, ParseGeoPoint>),
				typeof(FlexibleDictionaryWrapper<ParseGeoPoint, object>),
			};
    }
  }

  static class MonoHelpersiOS {
    internal static BinaryExpression Update(this BinaryExpression expr,
                                            Expression left,
                                            LambdaExpression conversion,
                                            Expression right) {
      return Expression.MakeBinary(expr.NodeType, left, right, expr.IsLiftedToNull, expr.Method, conversion);
    }

    internal static ConditionalExpression Update(this ConditionalExpression expr,
                                                 Expression test,
                                                 Expression isTrue,
                                                 Expression isFalse) {
      return Expression.Condition(test, isTrue, isFalse);
    }

    internal static ElementInit Update(this ElementInit init, IEnumerable<Expression> arguments) {
      return Expression.ElementInit(init.AddMethod, arguments);
    }

    internal static LambdaExpression Update(this LambdaExpression expr,
                                            Expression body,
                                            IEnumerable<ParameterExpression> parameters) {
      return Expression.Lambda(expr.Type, body, parameters);
    }

    internal static ListInitExpression Update(this ListInitExpression expr,
                                              NewExpression newExpr,
                                              IEnumerable<ElementInit> initializers) {
      return Expression.ListInit(newExpr, initializers);
    }

    internal static MemberExpression Update(this MemberExpression expr, Expression obj) {
      return Expression.MakeMemberAccess(obj, expr.Member);
    }

    internal static MemberAssignment Update(this MemberAssignment assign, Expression expr) {
      return Expression.Bind(assign.Member, expr);
    }

    internal static InvocationExpression Update(this InvocationExpression expr,
                                                Expression root,
                                                IEnumerable<Expression> args) {
      return Expression.Invoke(root, args);
    }

    internal static MemberInitExpression Update(this MemberInitExpression expr,
                                                NewExpression newExpr,
                                                IEnumerable<MemberBinding> bindings) {
      return Expression.MemberInit(newExpr, bindings);
    }

    internal static MemberListBinding Update(this MemberListBinding binding, IEnumerable<ElementInit> initializers) {
      return Expression.ListBind(binding.Member, initializers);
    }

    internal static MemberMemberBinding Update(this MemberMemberBinding binding, IEnumerable<MemberBinding> bindings) {
      return Expression.MemberBind(binding.Member, bindings);
    }

    internal static MethodCallExpression Update(this MethodCallExpression expr,
                                                Expression root,
                                                IEnumerable<Expression> args) {
      return Expression.Call(root, expr.Method, args);
    }

    internal static NewExpression Update(this NewExpression expr, IEnumerable<Expression> args) {
      return Expression.New(expr.Constructor, args, expr.Members);
    }

    internal static NewArrayExpression Update(this NewArrayExpression expr, IEnumerable<Expression> args) {
      return Expression.NewArrayInit(expr.Type, args);
    }

    internal static TypeBinaryExpression Update(this TypeBinaryExpression expr, Expression body) {
      return Expression.TypeIs(body, expr.TypeOperand);
    }

    internal static UnaryExpression Update(this UnaryExpression expr, Expression body) {
      return Expression.MakeUnary(expr.NodeType, body, expr.Type, expr.Method);
    }
  }

  // TODO: Revisit this after discussing whether Xamarin can make this class public.
  internal abstract class ExpressionVisitor {
    public ExpressionVisitor() {
    }

    public virtual Expression Visit(Expression expr) {
      if (expr == null) {
        return null;
      }

      var bin = expr as BinaryExpression;
      if (bin != null) {
        return VisitBinary(bin);
      }

      var cond = expr as ConditionalExpression;
      if (cond != null) {
        return VisitConditional(cond);
      }

      var constant = expr as ConstantExpression;
      if (constant != null) {
        return VisitConstant(constant);
      }

      var lambda = expr as LambdaExpression;
      if (lambda != null) {
        var lambdaType = lambda.GetType().GetGenericArguments()[0];
        Func<Expression<object>, Expression> visitLambdaDelegate = VisitLambda<object>;
        MethodInfo method = visitLambdaDelegate.Method.GetGenericMethodDefinition().MakeGenericMethod(lambdaType);
        return (Expression)method.Invoke(this, new[] { lambda });
      }

      var listInit = expr as ListInitExpression;
      if (listInit != null) {
        return VisitListInit(listInit);
      }

      var member = expr as MemberExpression;
      if (member != null) {
        return VisitMember(member);
      }

      var memberInit = expr as MemberInitExpression;
      if (memberInit != null) {
        return VisitMemberInit(memberInit);
      }

      var methodCall = expr as MethodCallExpression;
      if (methodCall != null) {
        return VisitMethodCall(methodCall);
      }

      var newExpr = expr as NewExpression;
      if (newExpr != null) {
        return VisitNew(newExpr);
      }

      var newArrayExpr = expr as NewArrayExpression;
      if (newArrayExpr != null) {
        return VisitNewArray(newArrayExpr);
      }

      var param = expr as ParameterExpression;
      if (param != null) {
        return VisitParameter(param);
      }

      var typeBinary = expr as TypeBinaryExpression;
      if (typeBinary != null) {
        return VisitTypeBinary(typeBinary);
      }

      var unary = expr as UnaryExpression;
      if (unary != null) {
        return VisitUnary(unary);
      }

      var invocation = expr as InvocationExpression;
      if (invocation != null) {
        return VisitInvocation(invocation);
      }

      throw new NotSupportedException("Expressions of type " + expr.Type + " are not supported.");
    }

    protected virtual Expression VisitBinary(BinaryExpression expr) {
      return expr.Update(Visit(expr.Left), (LambdaExpression)Visit(expr.Conversion), Visit(expr.Right));
    }

    protected virtual Expression VisitConditional(ConditionalExpression expr) {
      return expr.Update(Visit(expr.Test), Visit(expr.IfTrue), Visit(expr.IfFalse));
    }

    protected virtual Expression VisitConstant(ConstantExpression expr) {
      return expr;
    }

    protected virtual ElementInit VisitElementInit(ElementInit init) {
      return init.Update(init.Arguments.Select(a => Visit(a)));
    }

    protected virtual Expression VisitLambda<T>(Expression<T> expr) {
      return expr.Update(Visit(expr.Body), expr.Parameters.Select(p => (ParameterExpression)VisitParameter(p)));
    }

    protected virtual Expression VisitListInit(ListInitExpression expr) {
      return expr.Update((NewExpression)Visit(expr.NewExpression), expr.Initializers.Select(i => VisitElementInit(i)));
    }

    protected virtual Expression VisitMember(MemberExpression expr) {
      return expr.Update(Visit(expr.Expression));
    }

    protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assign) {
      return assign.Update(Visit(assign.Expression));
    }

    protected virtual MemberBinding VisitMemberBinding(MemberBinding binding) {
      switch (binding.BindingType) {
        case MemberBindingType.Assignment:
          return VisitMemberAssignment((MemberAssignment)binding);
        case MemberBindingType.ListBinding:
          return VisitMemberListBinding((MemberListBinding)binding);
        case MemberBindingType.MemberBinding:
          return VisitMemberMemberBinding((MemberMemberBinding)binding);
        default:
          throw new NotSupportedException("Bad member binding type: " + binding);
      }
    }

    protected virtual Expression VisitInvocation(InvocationExpression expr) {
      return expr.Update(Visit(expr.Expression), expr.Arguments.Select(a => Visit(a)));
    }

    protected virtual Expression VisitMemberInit(MemberInitExpression expr) {
      return expr.Update((NewExpression)Visit(expr.NewExpression), expr.Bindings.Select(b => VisitMemberBinding(b)));
    }

    protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding) {
      return binding.Update(binding.Initializers.Select(i => VisitElementInit(i)));
    }

    protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding) {
      return binding.Update(binding.Bindings.Select(b => VisitMemberBinding(b)));
    }

    protected virtual Expression VisitMethodCall(MethodCallExpression expr) {
      return expr.Update(Visit(expr.Object), expr.Arguments.Select(a => Visit(a)));
    }

    protected virtual Expression VisitNew(NewExpression expr) {
      return expr.Update(expr.Arguments.Select(a => Visit(a)));
    }

    protected virtual Expression VisitNewArray(NewArrayExpression expr) {
      return expr.Update(expr.Expressions.Select(a => Visit(a)));
    }

    protected virtual Expression VisitParameter(ParameterExpression expr) {
      return expr;
    }

    protected virtual Expression VisitTypeBinary(TypeBinaryExpression expr) {
      return expr.Update(Visit(expr.Expression));
    }

    protected virtual Expression VisitUnary(UnaryExpression expr) {
      return expr.Update(Visit(expr.Operand));
    }
  }
}
