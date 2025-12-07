using TzarBot.Common.Models;

namespace TzarBot.GameInterface.IPC;

/// <summary>
/// Interface for the IPC pipe server.
/// </summary>
public interface IPipeServer : IDisposable
{
    /// <summary>
    /// Starts the server and waits for client connections.
    /// </summary>
    Task StartAsync(CancellationToken ct);

    /// <summary>
    /// Stops the server and disconnects all clients.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Sends a screen frame to the connected client.
    /// </summary>
    Task SendFrameAsync(ScreenFrame frame, CancellationToken ct);

    /// <summary>
    /// Event raised when an action is received from the client.
    /// </summary>
    event Action<GameAction>? OnActionReceived;

    /// <summary>
    /// Event raised when a client connects.
    /// </summary>
    event Action? OnClientConnected;

    /// <summary>
    /// Event raised when the client disconnects.
    /// </summary>
    event Action? OnClientDisconnected;

    /// <summary>
    /// Indicates whether a client is currently connected.
    /// </summary>
    bool IsClientConnected { get; }
}
