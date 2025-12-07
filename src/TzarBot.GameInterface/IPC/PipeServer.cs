using System.Buffers;
using System.IO.Pipes;
using MessagePack;
using TzarBot.Common.Models;

namespace TzarBot.GameInterface.IPC;

/// <summary>
/// Named pipe server implementation for IPC communication.
/// </summary>
public sealed class PipeServer : IPipeServer
{
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc />
    public event Action<GameAction>? OnActionReceived;

    /// <inheritdoc />
    public event Action? OnClientConnected;

    /// <inheritdoc />
    public event Action? OnClientDisconnected;

    /// <inheritdoc />
    public bool IsClientConnected => _pipeServer?.IsConnected ?? false;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _pipeServer = new NamedPipeServerStream(
            Protocol.PipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        await _pipeServer.WaitForConnectionAsync(_cts.Token);
        OnClientConnected?.Invoke();

        _readTask = ReadLoopAsync(_cts.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
        }

        if (_readTask != null)
        {
            try
            {
                await _readTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _pipeServer?.Disconnect();
    }

    /// <inheritdoc />
    public async Task SendFrameAsync(ScreenFrame frame, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_pipeServer == null || !_pipeServer.IsConnected)
        {
            throw new InvalidOperationException("Client not connected");
        }

        await _writeLock.WaitAsync(ct);
        try
        {
            var data = MessagePackSerializer.Serialize(frame);
            await WriteMessageAsync(Protocol.MSG_FRAME, data, ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var headerBuffer = new byte[5]; // 4 bytes length + 1 byte type

        try
        {
            while (!ct.IsCancellationRequested && _pipeServer!.IsConnected)
            {
                // Read header
                var headerRead = await ReadExactlyAsync(_pipeServer, headerBuffer, ct);
                if (!headerRead)
                {
                    break; // Connection closed
                }

                var length = BitConverter.ToInt32(headerBuffer, 0);
                var messageType = headerBuffer[4];

                if (length > Protocol.MaxMessageSize)
                {
                    throw new InvalidOperationException($"Message too large: {length}");
                }

                // Read payload
                var payloadBuffer = ArrayPool<byte>.Shared.Rent(length);
                try
                {
                    var payloadRead = await ReadExactlyAsync(_pipeServer, payloadBuffer.AsMemory(0, length), ct);
                    if (!payloadRead)
                    {
                        break;
                    }

                    ProcessMessage(messageType, payloadBuffer.AsSpan(0, length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(payloadBuffer);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (IOException)
        {
            // Client disconnected
        }
        finally
        {
            OnClientDisconnected?.Invoke();
        }
    }

    private void ProcessMessage(byte messageType, ReadOnlySpan<byte> payload)
    {
        switch (messageType)
        {
            case Protocol.MSG_ACTION:
                var action = MessagePackSerializer.Deserialize<GameAction>(payload.ToArray());
                OnActionReceived?.Invoke(action);
                break;

            case Protocol.MSG_HEARTBEAT:
                // Heartbeat received - connection is alive
                break;

            default:
                // Unknown message type - ignore
                break;
        }
    }

    private async Task WriteMessageAsync(byte messageType, byte[] data, CancellationToken ct)
    {
        if (_pipeServer == null) return;

        // Write header: [Length:4][Type:1]
        var header = new byte[5];
        BitConverter.TryWriteBytes(header.AsSpan(0, 4), data.Length);
        header[4] = messageType;

        await _pipeServer.WriteAsync(header, ct);
        await _pipeServer.WriteAsync(data, ct);
        await _pipeServer.FlushAsync(ct);
    }

    private static async Task<bool> ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[totalRead..], ct);
            if (read == 0)
            {
                return false; // EOF
            }
            totalRead += read;
        }
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _pipeServer?.Dispose();
        _writeLock.Dispose();
        _cts?.Dispose();
    }
}
