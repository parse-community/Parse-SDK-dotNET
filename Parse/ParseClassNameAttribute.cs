// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse {
  /// <summary>
  /// Defines the class name for a subclass of ParseObject.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
  public sealed class ParseClassNameAttribute : Attribute {
    /// <summary>
    /// Constructs a new ParseClassName attribute.
    /// </summary>
    /// <param name="className">The class name to associate with the ParseObject subclass.</param>
    public ParseClassNameAttribute(string className) {
      this.ClassName = className;
    }

    /// <summary>
    /// Gets the class name to associate with the ParseObject subclass.
    /// </summary>
    public string ClassName { get; private set; }
  }
}
