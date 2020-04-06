// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Library;

namespace Parse.Push.Internal
{
    internal class ParsePushChannelsController : IParsePushChannelsController
    {
        IParseCurrentInstallationController CurrentInstallationController { get; }

        public ParsePushChannelsController(IParseCurrentInstallationController currentInstallationController) => CurrentInstallationController = currentInstallationController;

        public Task SubscribeAsync(IEnumerable<string> channels, CancellationToken cancellationToken) => CurrentInstallationController.GetAsync(cancellationToken).ContinueWith(task =>
        {
            task.Result.AddRangeUniqueToList(nameof(channels), channels);
            return task.Result.SaveAsync(cancellationToken);
        });

        public Task UnsubscribeAsync(IEnumerable<string> channels, CancellationToken cancellationToken) => CurrentInstallationController.GetAsync().ContinueWith(task =>
        {
            task.Result.RemoveAllFromList(nameof(channels), channels);
            return task.Result.SaveAsync(cancellationToken);
        });
    }
}
