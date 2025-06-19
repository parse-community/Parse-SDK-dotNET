using System.Net.WebSockets;
using System.Threading;

namespace Parse;

public class ParseLiveQueryClient
{
    private ClientWebSocket clientWebSocket;

    async void connect()
    {
        if (clientWebSocket is not null)
        {
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
        clientWebSocket = new ClientWebSocket();
    }
}