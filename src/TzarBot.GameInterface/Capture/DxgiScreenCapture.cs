using System.Runtime.InteropServices;
using TzarBot.Common.Models;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TzarBot.GameInterface.Capture;

/// <summary>
/// Screen capture implementation using DXGI Desktop Duplication API.
/// Provides high-performance screen capture with minimal CPU overhead.
/// </summary>
public sealed class DxgiScreenCapture : IScreenCapture
{
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly IDXGIOutputDuplication _duplication;
    private readonly ID3D11Texture2D _stagingTexture;

    private readonly int _width;
    private readonly int _height;
    private bool _disposed;

    /// <inheritdoc />
    public int Width => _width;

    /// <inheritdoc />
    public int Height => _height;

    /// <inheritdoc />
    public bool IsInitialized => !_disposed && _duplication != null;

    /// <summary>
    /// Creates a new DXGI screen capture instance for the primary monitor.
    /// </summary>
    /// <param name="adapterIndex">Index of the graphics adapter (0 = primary).</param>
    /// <param name="outputIndex">Index of the output/monitor (0 = primary).</param>
    /// <exception cref="ScreenCaptureException">Thrown when initialization fails.</exception>
    public DxgiScreenCapture(int adapterIndex = 0, int outputIndex = 0)
    {
        try
        {
            // Create D3D11 device
            var result = D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                [FeatureLevel.Level_11_0],
                out _device!,
                out _context!);

            if (result.Failure)
            {
                throw new ScreenCaptureException($"Failed to create D3D11 device: {result}");
            }

            // Get DXGI device and adapter
            using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
            using var adapter = dxgiDevice.GetAdapter();

            // Get the output (monitor) - use EnumOutputs for Vortice API
            var enumResult = adapter.EnumOutputs((uint)outputIndex, out var output);
            if (enumResult.Failure || output == null)
            {
                throw new ScreenCaptureException($"Failed to get output {outputIndex}");
            }

            using (output)
            {
                using var output1 = output.QueryInterface<IDXGIOutput1>();

                // Get output description for dimensions
                var outputDesc = output.Description;
                _width = outputDesc.DesktopCoordinates.Right - outputDesc.DesktopCoordinates.Left;
                _height = outputDesc.DesktopCoordinates.Bottom - outputDesc.DesktopCoordinates.Top;

                // Create output duplication
                _duplication = output1.DuplicateOutput(_device);
            }

            // Create staging texture for CPU access
            var stagingDesc = new Texture2DDescription
            {
                Width = (uint)_width,
                Height = (uint)_height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None
            };

            _stagingTexture = _device.CreateTexture2D(stagingDesc);
        }
        catch (Exception ex) when (ex is not ScreenCaptureException)
        {
            Dispose();
            throw new ScreenCaptureException("Failed to initialize DXGI screen capture", ex);
        }
    }

    /// <inheritdoc />
    public ScreenFrame? CaptureFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            // Try to acquire next frame (timeout 100ms)
            var result = _duplication.AcquireNextFrame(100, out var frameInfo, out var desktopResource);

            if (result.Failure)
            {
                // DXGI_ERROR_WAIT_TIMEOUT - no new frame available
                if (result == Vortice.DXGI.ResultCode.WaitTimeout)
                {
                    return null;
                }

                // DXGI_ERROR_ACCESS_LOST - need to reinitialize
                if (result == Vortice.DXGI.ResultCode.AccessLost)
                {
                    throw new ScreenCaptureException("Desktop duplication access lost. Reinitialization required.");
                }

                throw new ScreenCaptureException($"Failed to acquire frame: {result}");
            }

            try
            {
                // Get the desktop texture
                using var desktopTexture = desktopResource.QueryInterface<ID3D11Texture2D>();

                // Copy to staging texture
                _context.CopyResource(_stagingTexture, desktopTexture);

                // Map staging texture for CPU read
                var mappedResource = _context.Map(_stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

                try
                {
                    // Copy pixel data
                    var dataSize = _width * _height * 4;
                    var data = new byte[dataSize];

                    // Handle pitch (stride) difference
                    var sourcePtr = mappedResource.DataPointer;
                    var sourcePitch = (int)mappedResource.RowPitch;
                    var destPitch = _width * 4;

                    if (sourcePitch == destPitch)
                    {
                        // Fast path: no pitch adjustment needed
                        Marshal.Copy(sourcePtr, data, 0, dataSize);
                    }
                    else
                    {
                        // Copy row by row to handle pitch difference
                        for (var y = 0; y < _height; y++)
                        {
                            var sourceRow = sourcePtr + (y * sourcePitch);
                            Marshal.Copy(sourceRow, data, y * destPitch, destPitch);
                        }
                    }

                    return new ScreenFrame
                    {
                        Data = data,
                        Width = _width,
                        Height = _height,
                        TimestampTicks = DateTime.UtcNow.Ticks,
                        Format = PixelFormat.BGRA32
                    };
                }
                finally
                {
                    _context.Unmap(_stagingTexture, 0);
                }
            }
            finally
            {
                desktopResource?.Dispose();
                _duplication.ReleaseFrame();
            }
        }
        catch (Exception ex) when (ex is not ScreenCaptureException)
        {
            throw new ScreenCaptureException("Failed to capture frame", ex);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _stagingTexture?.Dispose();
        _duplication?.Dispose();
        _context?.Dispose();
        _device?.Dispose();
    }
}
