namespace TzarBot.GameInterface.IPC;

/// <summary>
/// Protocol constants for IPC communication.
/// </summary>
public static class Protocol
{
    /// <summary>
    /// Name of the named pipe for communication.
    /// </summary>
    public const string PipeName = "TzarBot";

    /// <summary>
    /// Message type: Screen frame (Server -> Client).
    /// </summary>
    public const byte MSG_FRAME = 0x01;

    /// <summary>
    /// Message type: Action to perform (Client -> Server).
    /// </summary>
    public const byte MSG_ACTION = 0x02;

    /// <summary>
    /// Message type: Keep-alive heartbeat (Bidirectional).
    /// </summary>
    public const byte MSG_HEARTBEAT = 0x03;

    /// <summary>
    /// Message type: Game status update (Server -> Client).
    /// </summary>
    public const byte MSG_STATUS = 0x04;

    /// <summary>
    /// Message type: Acknowledgment.
    /// </summary>
    public const byte MSG_ACK = 0x05;

    /// <summary>
    /// Maximum size of a single message in bytes (10MB).
    /// </summary>
    public const int MaxMessageSize = 10 * 1024 * 1024;

    /// <summary>
    /// Heartbeat interval in milliseconds.
    /// </summary>
    public const int HeartbeatIntervalMs = 1000;

    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    public const int ConnectionTimeoutMs = 5000;
}
