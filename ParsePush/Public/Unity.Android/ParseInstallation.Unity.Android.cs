// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Parse {
  public partial class ParseInstallation : ParseObject {
    /// <summary>
    /// iOS Badge.
    /// </summary>
    [ParseFieldName("badge")]
    public int Badge {
      get {
        return GetProperty<int>("Badge");
      }
      set {
        int badge = value;
        SetProperty<int>(badge, "Badge");
      }
    }
  }
}
