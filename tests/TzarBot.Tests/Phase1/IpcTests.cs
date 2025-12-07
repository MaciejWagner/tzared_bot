using FluentAssertions;
using TzarBot.Common.Models;
using TzarBot.GameInterface.IPC;

namespace TzarBot.Tests.Phase1;

/// <summary>
/// Tests for IPC Named Pipes implementation.
/// </summary>
public class IpcTests : IAsyncLifetime
{
    private PipeServer _server = null!;
    private PipeClient _client = null!;

    public Task InitializeAsync()
    {
        _server = new PipeServer();
        _client = new PipeClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _server.StopAsync();
        _server.Dispose();
    }

    [Fact]
    public async Task Server_AcceptsConnection()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var serverTask = _server.StartAsync(cts.Token);
        await _client.ConnectAsync(TimeSpan.FromSeconds(5), cts.Token);

        // Assert
        _server.IsClientConnected.Should().BeTrue();
    }

    [Fact]
    public async Task Client_Connects()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        _ = _server.StartAsync(cts.Token);
        await _client.ConnectAsync(TimeSpan.FromSeconds(5), cts.Token);

        // Assert
        _client.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task OnClientConnected_EventRaised()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var connected = false;
        _server.OnClientConnected += () => connected = true;

        // Act
        _ = _server.StartAsync(cts.Token);
        await _client.ConnectAsync(TimeSpan.FromSeconds(5), cts.Token);
        await Task.Delay(100);

        // Assert
        connected.Should().BeTrue();
    }

    [Fact]
    public async Task SendReceiveFrame_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = _server.StartAsync(cts.Token);
        await _client.ConnectAsync(TimeSpan.FromSeconds(5), cts.Token);

        ScreenFrame? receivedFrame = null;
        var frameReceived = new TaskCompletionSource<bool>();
        _client.OnFrameReceived += frame =>
        {
            receivedFrame = frame;
            frameReceived.TrySetResult(true);
        };

        var testFrame = new ScreenFrame
        {
            Data = new byte[100],
            Width = 10,
            Height = 10,
            TimestampTicks = DateTime.UtcNow.Ticks,
            Format = PixelFormat.BGRA32
        };

        // Act
        await _server.SendFrameAsync(testFrame, cts.Token);
        await Task.WhenAny(frameReceived.Task, Task.Delay(1000));

        // Assert
        receivedFrame.Should().NotBeNull();
        receivedFrame!.Width.Should().Be(testFrame.Width);
        receivedFrame.Height.Should().Be(testFrame.Height);
        receivedFrame.Data.Length.Should().Be(testFrame.Data.Length);
    }

    [Fact]
    public async Task SendReceiveAction_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = _server.StartAsync(cts.Token);
        await _client.ConnectAsync(TimeSpan.FromSeconds(5), cts.Token);

        GameAction? receivedAction = null;
        var actionReceived = new TaskCompletionSource<bool>();
        _server.OnActionReceived += action =>
        {
            receivedAction = action;
            actionReceived.TrySetResult(true);
        };

        var testAction = new GameAction
        {
            Type = ActionType.LeftClick,
            MouseDeltaX = 10,
            MouseDeltaY = 20
        };

        // Act
        await _client.SendActionAsync(testAction, cts.Token);
        await Task.WhenAny(actionReceived.Task, Task.Delay(1000));

        // Assert
        receivedAction.Should().NotBeNull();
        receivedAction!.Type.Should().Be(ActionType.LeftClick);
        receivedAction.MouseDeltaX.Should().Be(10);
        receivedAction.MouseDeltaY.Should().Be(20);
    }

    [Fact]
    public async Task MultipleFrames_AllReceived()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = _server.StartAsync(cts.Token);
        await _client.ConnectAsync(TimeSpan.FromSeconds(5), cts.Token);

        var receivedFrames = new List<ScreenFrame>();
        _client.OnFrameReceived += frame => receivedFrames.Add(frame);

        // Act
        for (var i = 0; i < 5; i++)
        {
            var testFrame = new ScreenFrame
            {
                Data = new byte[100],
                Width = 10 + i,
                Height = 10,
                TimestampTicks = DateTime.UtcNow.Ticks,
                Format = PixelFormat.BGRA32
            };
            await _server.SendFrameAsync(testFrame, cts.Token);
        }

        await Task.Delay(500);

        // Assert
        receivedFrames.Count.Should().Be(5);
    }

    [Fact]
    public async Task Client_DisconnectDoesNotThrow()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = _server.StartAsync(cts.Token);
        await _client.ConnectAsync(TimeSpan.FromSeconds(5), cts.Token);

        // Act
        var exception = await Record.ExceptionAsync(() => _client.DisconnectAsync());

        // Assert
        exception.Should().BeNull();
        _client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task SendAction_WhenNotConnected_Throws()
    {
        // Arrange - don't connect

        // Act
        var action = new GameAction { Type = ActionType.LeftClick };
        var act = async () => await _client.SendActionAsync(action, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendFrame_WhenNotConnected_Throws()
    {
        // Arrange - don't start server

        // Act
        var frame = new ScreenFrame
        {
            Data = [],
            Width = 1,
            Height = 1,
            TimestampTicks = 0,
            Format = PixelFormat.BGRA32
        };
        var act = async () => await _server.SendFrameAsync(frame, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
