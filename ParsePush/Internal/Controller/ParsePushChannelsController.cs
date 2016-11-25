// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace LeanCloud.Push.Internal {
  internal class ParsePushChannelsController : IParsePushChannelsController {
    public Task SubscribeAsync(IEnumerable<string> channels, CancellationToken cancellationToken) {
      AVInstallation installation = AVInstallation.CurrentInstallation;
      installation.AddRangeUniqueToList("channels", channels);
      return installation.SaveAsync(cancellationToken);
    }

    public Task UnsubscribeAsync(IEnumerable<string> channels, CancellationToken cancellationToken) {
      AVInstallation installation = AVInstallation.CurrentInstallation;
      installation.RemoveAllFromList("channels", channels);
      return installation.SaveAsync(cancellationToken);
    }
  }
}
