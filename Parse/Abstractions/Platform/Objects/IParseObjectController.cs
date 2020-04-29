// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure;

namespace Parse.Abstractions.Platform.Objects
{
    public interface IParseObjectController
    {
        Task<IObjectState> FetchAsync(IObjectState state, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

        Task<IObjectState> SaveAsync(IObjectState state, IDictionary<string, IParseFieldOperation> operations, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

        IList<Task<IObjectState>> SaveAllAsync(IList<IObjectState> states, IList<IDictionary<string, IParseFieldOperation>> operationsList, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

        Task DeleteAsync(IObjectState state, string sessionToken, CancellationToken cancellationToken = default);

        IList<Task> DeleteAllAsync(IList<IObjectState> states, string sessionToken, CancellationToken cancellationToken = default);
    }
}
