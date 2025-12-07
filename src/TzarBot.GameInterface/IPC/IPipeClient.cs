using TzarBot.Common.Models;

namespace TzarBot.GameInterface.IPC;

/// <summary>
/// Interface for the IPC pipe client.
/// </summary>
public interface IPipeClient : IDisposable
{
    /// <summary>
    /// Connects to the pipe server.
    /// </summary>
    Task ConnectAsync(TimeSpan timeout, CancellationToken ct);

    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends an action to the server.
    /// </summary>
    Task SendActionAsync(GameAction action, CancellationToken ct);

    /// <summary>
    /// Event raised when a screen frame is received from the server.
    /// </summary>
    event Action<ScreenFrame>? OnFrameReceived;

    /// <summary>
    /// Indicates whether the client is connected to the server.
    /// </summary>
    bool IsConnected { get; }
}
