using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    public interface IWebSocketClient
    {
        bool IsOpen { get; }

        event Action OnClosed;
        event Action<string> OnError;
        event Action<string> OnLog;
        event Action<string> OnMessage;
        event Action OnOpened;

        void Close();
        void Open(string url, string protocol = null);
        void Send(string message);
    }
}
