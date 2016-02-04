// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse {
  /// <summary>
  /// Specifies a field name for a property on a ParseObject subclass.
  /// </summary>
  [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
  public sealed class ParseFieldNameAttribute : Attribute {
    /// <summary>
    /// Constructs a new ParseFieldName attribute.
    /// </summary>
    /// <param name="fieldName">The name of the field on the ParseObject that the
    /// property represents.</param>
    public ParseFieldNameAttribute(string fieldName) {
      FieldName = fieldName;
    }

    /// <summary>
    /// Gets the name of the field represented by this property.
    /// </summary>
    public string FieldName { get; private set; }
  }
}
