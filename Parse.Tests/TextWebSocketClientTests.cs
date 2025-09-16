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

        client.Dispose();
    }

    [TestMethod]
    public async Task TestSendAndReceive()
    {
        TextWebSocketClient client = new TextWebSocketClient(32);
        await client.OpenAsync("wss://echo.websocket.org", CancellationToken.None);

        TaskCompletionSource ConnectionSignal = new TaskCompletionSource();
        CancellationTokenSource cts = new CancellationTokenSource();

        client.MessageReceived += (_, e) =>
        {
            if (e.Message.StartsWith("Request served by"))
            {
                return;
            }
            Assert.AreEqual("Hello world, WebSocket listening!", e.Message);
            ConnectionSignal?.TrySetResult();
        };

        await client.SendAsync("Hello world, WebSocket listening!", cts.Token);
        cts.CancelAfter(5000);
        await ConnectionSignal.Task.WaitAsync(cts.Token);
        ConnectionSignal = null;
        await client.CloseAsync();
        client.Dispose();
    }

    
}