using System.Buffers;
using System.IO.Pipes;
using MessagePack;
using TzarBot.Common.Models;

namespace TzarBot.GameInterface.IPC;

/// <summary>
/// Named pipe client implementation for IPC communication.
/// </summary>
public sealed class PipeClient : IPipeClient
{
    private NamedPipeClientStream? _pipeClient;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc />
    public event Action<ScreenFrame>? OnFrameReceived;

    /// <inheritdoc />
    public bool IsConnected => _pipeClient?.IsConnected ?? false;

    /// <inheritdoc />
    public async Task ConnectAsync(TimeSpan timeout, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _pipeClient = new NamedPipeClientStream(
            ".",
            Protocol.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await _pipeClient.ConnectAsync((int)timeout.TotalMilliseconds, _cts.Token);

        _readTask = ReadLoopAsync(_cts.Token);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
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

        _pipeClient?.Close();
    }

    /// <inheritdoc />
    public async Task SendActionAsync(GameAction action, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_pipeClient == null || !_pipeClient.IsConnected)
        {
            throw new InvalidOperationException("Not connected to server");
        }

        await _writeLock.WaitAsync(ct);
        try
        {
            var data = MessagePackSerializer.Serialize(action);
            await WriteMessageAsync(Protocol.MSG_ACTION, data, ct);
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
            while (!ct.IsCancellationRequested && _pipeClient!.IsConnected)
            {
                // Read header
                var headerRead = await ReadExactlyAsync(_pipeClient, headerBuffer, ct);
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
                    var payloadRead = await ReadExactlyAsync(_pipeClient, payloadBuffer.AsMemory(0, length), ct);
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
            // Server disconnected
        }
    }

    private void ProcessMessage(byte messageType, ReadOnlySpan<byte> payload)
    {
        switch (messageType)
        {
            case Protocol.MSG_FRAME:
                var frame = MessagePackSerializer.Deserialize<ScreenFrame>(payload.ToArray());
                OnFrameReceived?.Invoke(frame);
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
        if (_pipeClient == null) return;

        // Write header: [Length:4][Type:1]
        var header = new byte[5];
        BitConverter.TryWriteBytes(header.AsSpan(0, 4), data.Length);
        header[4] = messageType;

        await _pipeClient.WriteAsync(header, ct);
        await _pipeClient.WriteAsync(data, ct);
        await _pipeClient.FlushAsync(ct);
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
        _pipeClient?.Dispose();
        _writeLock.Dispose();
        _cts?.Dispose();
    }
}
