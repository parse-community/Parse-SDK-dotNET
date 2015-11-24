// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Parse.Internal {
  internal class ParsePushController : IParsePushController {
    public Task SendPushNotificationAsync(IPushState state, String sessionToken, CancellationToken cancellationToken) {
      var command = new ParseCommand("push",
          method: "POST",
          sessionToken: sessionToken,
          data: ParsePushEncoder.Instance.Encode(state));

      return ParseClient.ParseCommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
    }
  }
}
