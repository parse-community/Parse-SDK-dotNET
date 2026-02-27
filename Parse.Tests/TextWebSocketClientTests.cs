using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure.Execution;

namespace Parse.Tests;

[TestClass]
public class TextWebSocketClientTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        TextWebSocketClient client = new TextWebSocketClient(1024);
        client.Dispose();
        client.Dispose();
        // No exception expected
    }

    [TestMethod]
    public async Task SendAsync_ThrowsIfNotOpen()
    {
        TextWebSocketClient client = new TextWebSocketClient(1024);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await client.SendAsync("msg", CancellationToken.None));
    }

    [TestMethod]
    public async Task CloseAsync_DoesNothingIfNotConnected()
    {
        TextWebSocketClient client = new TextWebSocketClient(1024);
        await client.CloseAsync();
        // Should not throw
    }

    [TestMethod]
    public void StartListening_DoesNotStartMultipleListeners()
    {
        TextWebSocketClient client = new TextWebSocketClient(1024);
        MethodInfo method = typeof(TextWebSocketClient).GetMethod("StartListening", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        CancellationTokenSource cts = new CancellationTokenSource();
        method.Invoke(client, new object[] { cts.Token });
        method.Invoke(client, new object[] { cts.Token });
        // Should not throw or start multiple listeners
    }
    [TestMethod]
    public void TestConstructor()
    {
        TextWebSocketClient client = new TextWebSocketClient(4096);
        Assert.IsNotNull(client);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task TestSendAndReceive()
    {
        // obtain endpoint from environment/config
        string wsUrl = Environment.GetEnvironmentVariable("TEST_WEBSOCKET_ECHO_URL")?.Trim();
        if (string.IsNullOrEmpty(wsUrl))
        {
            // if not provided we cannot run the live integration; mark inconclusive
            // users/CI can set TEST_WEBSOCKET_ECHO_URL to a reachable echo service
            // e.g. wss://echo.example.org or localhost during local development
            Assert.Inconclusive("TEST_WEBSOCKET_ECHO_URL not configured; skipping WebSocket integration test.");
        }

        TextWebSocketClient client = new TextWebSocketClient(32);
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        TaskCompletionSource receiveTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await client.OpenAsync(wsUrl, cts.Token);

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