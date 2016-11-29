using System;
using Websockets;
using System.Net.WebSockets;
using LeanCloud.Realtime.Internal;

namespace LeanCloud.Realtime
{
    public class WebSocketClient: IWebSocketClient
    {
        internal readonly IWebSocketConnection connection;
        public WebSocketClient()
        {
            Websockets.Net.WebsocketConnection.Link();
            connection = WebSocketFactory.Create();
        }

        public event Action OnClosed;
        public event Action<string> OnError;
        public event Action<string> OnLog;

        public event Action OnOpened
        {
            add
            {
                connection.OnOpened += value;
            }
            remove
            {
                connection.OnOpened -= value;
            }
        }

        public event Action<string> OnMessage
        {
            add
            {
                connection.OnMessage += value;
            }
            remove
            {
                connection.OnMessage -= value;
            }
        }

        public bool IsOpen
        {
            get
            {
                return connection.IsOpen;
            }
        }

        public void Close()
        {
            if (connection != null)
            {
                connection.Close();
            }
        }

        public void Open(string url, string protocol = null)
        {
            if (connection != null)
            {
                connection.Open(url, protocol);
            }
        }

        public void Send(string message)
        {
            if (connection != null)
            {
                if (this.IsOpen)
                {
                    connection.Send(message);
                }
            }
        }
    }
}
