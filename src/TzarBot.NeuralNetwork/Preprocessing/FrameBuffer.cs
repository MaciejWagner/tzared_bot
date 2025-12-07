using System.Runtime.CompilerServices;

namespace TzarBot.NeuralNetwork.Preprocessing;

/// <summary>
/// Thread-safe circular buffer for storing processed frames.
/// Provides temporal context by stacking multiple frames.
///
/// Thread safety:
/// - Readers and writers can operate concurrently
/// - GetStackedFrames returns a snapshot (copy) of the current state
/// - Write operations are serialized via lock
/// </summary>
public sealed class FrameBuffer : IDisposable
{
    private readonly object _lock = new();
    private readonly float[][] _frames;
    private readonly int _capacity;
    private readonly int _frameSize;
    private int _writeIndex;
    private int _count;
    private bool _disposed;

    /// <summary>
    /// Number of frames currently in the buffer.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// Maximum number of frames this buffer can hold.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Size of each frame in float elements.
    /// </summary>
    public int FrameSize => _frameSize;

    /// <summary>
    /// Whether the buffer has enough frames for a complete stack.
    /// </summary>
    public bool IsFull
    {
        get
        {
            lock (_lock)
            {
                return _count >= _capacity;
            }
        }
    }

    /// <summary>
    /// Creates a new frame buffer.
    /// </summary>
    /// <param name="capacity">Number of frames to store (typically 4 for temporal stacking)</param>
    /// <param name="frameSize">Size of each frame in float elements (height * width)</param>
    public FrameBuffer(int capacity, int frameSize)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
        if (frameSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameSize), "Frame size must be positive");

        _capacity = capacity;
        _frameSize = frameSize;
        _frames = new float[capacity][];

        for (int i = 0; i < capacity; i++)
        {
            _frames[i] = new float[frameSize];
        }
    }

    /// <summary>
    /// Adds a new frame to the buffer, overwriting the oldest frame if full.
    /// </summary>
    /// <param name="frame">Preprocessed frame data (must be exactly frameSize elements)</param>
    /// <exception cref="ArgumentException">If frame size doesn't match expected size</exception>
    public void AddFrame(ReadOnlySpan<float> frame)
    {
        ThrowIfDisposed();

        if (frame.Length != _frameSize)
        {
            throw new ArgumentException(
                $"Frame size mismatch. Expected {_frameSize}, got {frame.Length}",
                nameof(frame));
        }

        lock (_lock)
        {
            // Copy frame data to the current write position
            frame.CopyTo(_frames[_writeIndex].AsSpan());

            // Move to next position (circular)
            _writeIndex = (_writeIndex + 1) % _capacity;
            _count = Math.Min(_count + 1, _capacity);
        }
    }

    /// <summary>
    /// Gets stacked frames as a flat array, ordered from oldest to newest.
    /// Returns a copy of the data (thread-safe snapshot).
    ///
    /// If buffer is not full, fills missing frames with zeros (or copies oldest available).
    /// </summary>
    /// <returns>Flat array of shape [capacity * frameSize] with stacked frames</returns>
    public float[] GetStackedFrames()
    {
        ThrowIfDisposed();

        float[] result = new float[_capacity * _frameSize];
        GetStackedFrames(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Gets stacked frames into a pre-allocated buffer.
    /// More efficient than GetStackedFrames() for hot paths.
    /// </summary>
    /// <param name="destination">Destination buffer (must be at least capacity * frameSize)</param>
    public void GetStackedFrames(Span<float> destination)
    {
        ThrowIfDisposed();

        int totalSize = _capacity * _frameSize;
        if (destination.Length < totalSize)
        {
            throw new ArgumentException(
                $"Destination too small. Expected at least {totalSize}, got {destination.Length}",
                nameof(destination));
        }

        lock (_lock)
        {
            if (_count == 0)
            {
                // No frames yet, return zeros
                destination[..totalSize].Clear();
                return;
            }

            // Calculate starting index (oldest frame)
            // If buffer is full, oldest is at _writeIndex
            // If not full, oldest is at 0
            int startIndex = _count >= _capacity
                ? _writeIndex
                : 0;

            int destOffset = 0;

            // If we don't have enough frames, pad with copies of the oldest
            int framesToPad = _capacity - _count;
            if (framesToPad > 0)
            {
                var oldestFrame = _frames[0].AsSpan();
                for (int i = 0; i < framesToPad; i++)
                {
                    oldestFrame.CopyTo(destination.Slice(destOffset, _frameSize));
                    destOffset += _frameSize;
                }
            }

            // Copy actual frames from oldest to newest
            for (int i = 0; i < _count; i++)
            {
                int frameIndex = (startIndex + i) % _capacity;
                _frames[frameIndex].AsSpan().CopyTo(destination.Slice(destOffset, _frameSize));
                destOffset += _frameSize;
            }
        }
    }

    /// <summary>
    /// Gets the most recent frame (or null if empty).
    /// Returns a copy of the data.
    /// </summary>
    public float[]? GetLatestFrame()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_count == 0)
                return null;

            // Latest frame is at (_writeIndex - 1 + capacity) % capacity
            int latestIndex = (_writeIndex - 1 + _capacity) % _capacity;
            return (float[])_frames[latestIndex].Clone();
        }
    }

    /// <summary>
    /// Clears all frames from the buffer.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            for (int i = 0; i < _capacity; i++)
            {
                Array.Clear(_frames[i], 0, _frameSize);
            }
            _writeIndex = 0;
            _count = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FrameBuffer));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        // Note: arrays are managed, no explicit disposal needed
        // but we mark as disposed to prevent further use
    }
}
