using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    interface IAVIMPlatformHooks
    {
        IWebSocketClient WebSocketClient { get; }

        string ua { get; }
    }
}
