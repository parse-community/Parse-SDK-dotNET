using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Platform.LiveQueries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Parse.Tests;

[TestClass]
public class LiveQueryControllerTests
{
    private readonly Mock<IWebSocketClient> _webSocketClientMock;
    private readonly Mock<IParseLiveQueryMessageParser> _messageParserMock;
    private readonly Mock<IParseLiveQueryMessageBuilder> _messageBuilderMock;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);

    public LiveQueryControllerTests()
    {
        _webSocketClientMock = new Mock<IWebSocketClient>();
        _messageParserMock = new Mock<IParseLiveQueryMessageParser>();
        _messageBuilderMock = new Mock<IParseLiveQueryMessageBuilder>();
    }

    [TestMethod]
    public void ProcessMessage_UnknownOperation_DoesNotThrow()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        Dictionary<string, object> message = new Dictionary<string, object> { { "op", "unknown" } };
        MethodInfo processMessage = controller.GetType().GetMethod("ProcessMessage", BindingFlags.NonPublic | BindingFlags.Instance);
        processMessage.Invoke(controller, new object[] { message });
        // No exception expected
    }

    [TestMethod]
    public void ProcessMessage_MissingOp_DoesNotThrow()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        Dictionary<string, object> message = new Dictionary<string, object>();
        MethodInfo processMessage = controller.GetType().GetMethod("ProcessMessage", BindingFlags.NonPublic | BindingFlags.Instance);
        processMessage.Invoke(controller, new object[] { message });
        // No exception expected
    }

    [TestMethod]
    public void ProcessConnectionMessage_SetsStateConnected()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        _messageParserMock.Setup(m => m.GetClientId(It.IsAny<IDictionary<string, object>>())).Returns("clientId");
        Dictionary<string, object> message = new Dictionary<string, object> { { "op", "connected" } };
        MethodInfo processConnectionMessage = controller.GetType().GetMethod("ProcessConnectionMessage", BindingFlags.NonPublic | BindingFlags.Instance);
        processConnectionMessage.Invoke(controller, new object[] { message });
        Assert.AreEqual(ParseLiveQueryController.ParseLiveQueryState.Connected, controller.State);
    }

    [TestMethod]
    public void ProcessErrorMessage_RaisesErrorEvent()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        IParseLiveQueryMessageParser.LiveQueryError error = new IParseLiveQueryMessageParser.LiveQueryError { Code = 99, Message = "err", Reconnect = false };
        _messageParserMock.Setup(m => m.GetError(It.IsAny<IDictionary<string, object>>())).Returns(error);
        bool raised = false;
        controller.Error += (s, e) => { raised = true; Assert.AreEqual(99, e.Code); };
        MethodInfo processErrorMessage = controller.GetType().GetMethod("ProcessErrorMessage", BindingFlags.NonPublic | BindingFlags.Instance);
        processErrorMessage.Invoke(controller, new object[] { new Dictionary<string, object>() });
        Assert.IsTrue(raised);
    }

    [TestMethod]
    public void Constructor_InitializesStateToClosed()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        Assert.AreEqual(ParseLiveQueryController.ParseLiveQueryState.Closed, controller.State);
    }

    [TestMethod]
    public async Task ConnectAsync_ThrowsIfDisposed()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        controller.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => controller.ConnectAsync());
    }

    [TestMethod]
    public async Task DisposeAsync_ClosesConnection()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        _webSocketClientMock.Setup(w => w.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        await controller.DisposeAsync();
        _webSocketClientMock.Verify(w => w.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public void Dispose_SetsDisposedFlag()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        controller.Dispose();
        Assert.Throws<ObjectDisposedException>(() => controller.ConnectAsync().GetAwaiter().GetResult());
    }

    [TestMethod]
    public void Error_Event_Is_Raised_On_Error_Message()
    {
        ParseLiveQueryController controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        IParseLiveQueryMessageParser.LiveQueryError error = new IParseLiveQueryMessageParser.LiveQueryError { Code = 42, Message = "Test error", Reconnect = true };
        _messageParserMock.Setup(m => m.GetError(It.IsAny<IDictionary<string, object>>())).Returns(error);
        bool eventRaised = false;
        controller.Error += (sender, args) =>
        {
            eventRaised = true;
            Assert.AreEqual(42, args.Code);
            Assert.AreEqual("Test error", args.Error);
            Assert.IsTrue(args.Reconnect);
        };
        Dictionary<string, object> message = new Dictionary<string, object> { { "op", "error" } };
        MethodInfo processMessage = controller.GetType().GetMethod("ProcessMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        processMessage.Invoke(controller, new object[] { message });
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void ValidateClientMessage_ReturnsFalse_WhenClientIdMissing()
    {
        var controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        MethodInfo validate = controller.GetType().GetMethod("ValidateClientMessage", BindingFlags.NonPublic | BindingFlags.Instance);

        _messageParserMock.Setup(m => m.GetClientId(It.IsAny<IDictionary<string, object>>())).Returns((string)null);
        _messageParserMock.Setup(m => m.GetRequestId(It.IsAny<IDictionary<string, object>>())).Returns(123);

        var message = new Dictionary<string, object>();
        object[] args = { message, 0 };
        bool result = (bool)validate.Invoke(controller, args);

        Assert.IsFalse(result, "Expected validation to fail when client id is null");
        // requestId should remain default since client id was invalid
        Assert.AreEqual(0, (int)args[1]);
    }

    [TestMethod]
    public void ValidateClientMessage_ReturnsFalse_WhenRequestIdZero()
    {
        var controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        MethodInfo validate = controller.GetType().GetMethod("ValidateClientMessage", BindingFlags.NonPublic | BindingFlags.Instance);

        _messageParserMock.Setup(m => m.GetClientId(It.IsAny<IDictionary<string, object>>())).Returns("cid");
        _messageParserMock.Setup(m => m.GetRequestId(It.IsAny<IDictionary<string, object>>())).Returns(0);

        var message = new Dictionary<string, object>();
        object[] args = { message, 0 };
        bool result = (bool)validate.Invoke(controller, args);

        Assert.IsFalse(result, "Expected validation to fail when request id is zero");
        Assert.AreEqual(0, (int)args[1]);
    }

    [TestMethod]
    public void ValidateClientMessage_ReturnsTrue_AndSetsFields_WhenValid()
    {
        var controller = new ParseLiveQueryController(_timeout, _webSocketClientMock.Object, _messageParserMock.Object, _messageBuilderMock.Object);
        MethodInfo validate = controller.GetType().GetMethod("ValidateClientMessage", BindingFlags.NonPublic | BindingFlags.Instance);

        _messageParserMock.Setup(m => m.GetClientId(It.IsAny<IDictionary<string, object>>())).Returns("cid");
        _messageParserMock.Setup(m => m.GetRequestId(It.IsAny<IDictionary<string, object>>())).Returns(999);

        var message = new Dictionary<string, object>();
        object[] args = { message, 0 };
        bool result = (bool)validate.Invoke(controller, args);

        Assert.IsTrue(result, "Expected validation to succeed with valid client id and request id");
        Assert.AreEqual(999, (int)args[1]);

        // verify controller's internal ClientId property was set appropriately
        PropertyInfo clientIdProp = controller.GetType().GetProperty("ClientId", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.AreEqual("cid", clientIdProp.GetValue(controller));
    }
}
