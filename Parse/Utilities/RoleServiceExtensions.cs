// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Abstractions.Infrastructure;

namespace Parse
{
    public static class RoleServiceExtensions
    {
        /// <summary>
        /// Gets a <see cref="ParseQuery{ParseRole}"/> over the Role collection.
        /// </summary>
        public static ParseQuery<ParseRole> GetRoleQuery(this IServiceHub serviceHub) => serviceHub.GetQuery<ParseRole>();
    }
}
