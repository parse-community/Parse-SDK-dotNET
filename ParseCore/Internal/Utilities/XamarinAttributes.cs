using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Parse;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  /// <summary>
  /// A reimplementation of Xamarin's PreserveAttribute.
  /// This allows us to support AOT and linking for Xamarin platforms.
  /// </summary>
  [AttributeUsage(AttributeTargets.All)]
  internal class PreserveAttribute : Attribute {
    public bool AllMembers;
    public bool Conditional;
  }

  [AttributeUsage(AttributeTargets.All)]
  internal class LinkerSafeAttribute : Attribute {
    public LinkerSafeAttribute() { }
  }

  [Preserve(AllMembers = true)]
  internal class PreserveWrapperTypes {
    /// <summary>
    /// Exists to ensure that generic types are AOT-compiled for the conversions we support.
    /// Any new value types that we add support for will need to be registered here.
    /// The method itself is never called, but by virtue of the Preserve attribute being set
    /// on the class, these types will be AOT-compiled.
    ///
    /// This also applies to Unity.
    /// </summary>
    private static List<object> CreateWrapperTypes() {
      return new List<object> {
				(Action)(() => ParseCloud.CallFunctionAsync<object>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<bool>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<byte>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<sbyte>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<short>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<ushort>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<int>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<uint>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<long>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<ulong>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<char>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<double>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<float>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<IDictionary<string, object>>(null, null, CancellationToken.None)),
				(Action)(() => ParseCloud.CallFunctionAsync<IList<object>>(null, null, CancellationToken.None)),

        typeof(FlexibleListWrapper<object, ParseGeoPoint>),
				typeof(FlexibleListWrapper<ParseGeoPoint, object>),
				typeof(FlexibleDictionaryWrapper<object, ParseGeoPoint>),
				typeof(FlexibleDictionaryWrapper<ParseGeoPoint, object>),
			};
    }
  }
}
