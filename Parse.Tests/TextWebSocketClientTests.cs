using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure.Execution;

namespace Parse.Tests;

[TestClass]
public class TextWebSocketClientTests
{
    [TestMethod]
    public void TestConstructor()
    {
        TextWebSocketClient client = new TextWebSocketClient(4096);
        Assert.IsNotNull(client);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [Timeout(10000)]
    public async Task TestSendAndReceive()
    {
        TextWebSocketClient client = new TextWebSocketClient(32);
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        TaskCompletionSource receiveTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await client.OpenAsync("wss://echo.websocket.org", cts.Token);

        string receivedMessage = null;
        EventHandler<MessageReceivedEventArgs> handler = (_, e) =>
        {
            if (e.Message.StartsWith("Request served by"))
            {
                return;
            }
            receivedMessage = e.Message;
            receiveTcs?.TrySetResult();
        };
        client.MessageReceived += handler;

        await client.SendAsync("Hello world, WebSocket listening!", cts.Token);
        cts.CancelAfter(5000);
        await receiveTcs.Task.WaitAsync(cts.Token);
        Assert.AreEqual("Hello world, WebSocket listening!", receivedMessage);

        client.MessageReceived -= handler;
        await client.CloseAsync(cts.Token);
    }
}