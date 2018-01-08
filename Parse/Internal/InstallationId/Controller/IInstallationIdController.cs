// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Core.Internal {
  public interface IInstallationIdController {
    /// <summary>
    /// Sets current <code>installationId</code> and saves it to local storage.
    /// </summary>
    /// <param name="installationId">The <code>installationId</code> to be saved.</param>
    Task SetAsync(Guid? installationId);

    /// <summary>
    /// Gets current <code>installationId</code> from local storage. Generates a none exists.
    /// </summary>
    /// <returns>Current <code>installationId</code>.</returns>
    Task<Guid?> GetAsync();

    /// <summary>
    /// Clears current installationId from memory and local storage.
    /// </summary>
    Task ClearAsync();
  }
}
