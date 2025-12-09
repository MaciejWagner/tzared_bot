using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenCvSharp;
using TzarBot.Common.Models;
using TzarBot.StateDetection.Detection;

namespace TzarBot.StateDetection.Monitoring;

/// <summary>
/// Monitors game state in real-time and detects game outcomes.
///
/// The monitor tracks:
/// - State transitions (menu -> loading -> in-game -> victory/defeat)
/// - Game duration and timeouts
/// - Window responsiveness (crash detection)
/// - Screen activity (stuck detection)
///
/// Uses a state machine with debouncing to prevent false positives
/// from single-frame detection errors.
/// </summary>
public sealed class GameMonitor : IGameMonitor
{
    private readonly IGameStateDetector _detector;
    private readonly GameMonitorConfig _config;
    private readonly Stopwatch _gameTimer = new();
    private readonly List<StateTransition> _stateHistory = new();

    private GameState _currentState = GameState.Unknown;
    private GameState _pendingState = GameState.Unknown;
    private int _pendingStateCount;
    private DateTime _lastActivityTime = DateTime.UtcNow;
    private DateTime? _notRespondingSince;
    private DateTime? _loadingStartTime;
    private Mat? _previousFrame;
    private int _idleFrameCount;
    private int _activeFrameCount;
    private bool _isMonitoring;
    private bool _disposed;

    public GameState CurrentState => _currentState;
    public bool IsMonitoring => _isMonitoring;

    public event EventHandler<StateChangedEventArgs>? StateChanged;
    public event EventHandler<GameEndedEventArgs>? GameEnded;

    /// <summary>
    /// Creates a new game monitor with the specified detector and configuration.
    /// </summary>
    /// <param name="detector">Game state detector to use.</param>
    /// <param name="config">Monitor configuration.</param>
    public GameMonitor(IGameStateDetector detector, GameMonitorConfig? config = null)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _config = config ?? GameMonitorConfig.Default();

        // Initialize detector if needed
        if (_detector is IInitializableDetector initializable && !initializable.IsInitialized)
        {
            initializable.Initialize();
        }
    }

    /// <summary>
    /// Creates a game monitor with a default composite detector.
    /// </summary>
    /// <param name="config">Monitor configuration.</param>
    public GameMonitor(GameMonitorConfig? config = null)
        : this(CreateDefaultDetector(), config)
    {
    }

    private static CompositeGameStateDetector CreateDefaultDetector()
    {
        var detector = new CompositeGameStateDetector();
        detector.Initialize();
        return detector;
    }

    /// <inheritdoc />
    public async Task<MonitoringResult> StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_isMonitoring)
        {
            throw new InvalidOperationException("Monitoring is already active");
        }

        _isMonitoring = true;
        _gameTimer.Restart();
        _stateHistory.Clear();
        _idleFrameCount = 0;
        _activeFrameCount = 0;
        _currentState = GameState.Unknown;
        _pendingState = GameState.Unknown;
        _pendingStateCount = 0;
        _lastActivityTime = DateTime.UtcNow;
        _notRespondingSince = null;
        _loadingStartTime = null;

        var startTime = DateTime.UtcNow;

        Log($"Monitoring started at {startTime}");

        try
        {
            while (_isMonitoring && !cancellationToken.IsCancellationRequested)
            {
                // Check for timeout conditions
                var timeoutResult = CheckTimeouts();
                if (timeoutResult.HasValue)
                {
                    return CreateResult(timeoutResult.Value.Item1, startTime, timeoutResult.Value.Item2);
                }

                // Check if window is responding
                var crashResult = CheckWindowHealth();
                if (crashResult.HasValue)
                {
                    return CreateResult(crashResult.Value.Item1, startTime, crashResult.Value.Item2);
                }

                // Small delay between checks
                await Task.Delay(_config.CheckInterval, cancellationToken);

                // Note: In actual use, the ProcessFrame method should be called
                // by the screen capture loop. This loop handles timeouts and health checks.
            }

            // Monitoring was stopped externally
            return CreateResult(GameOutcome.Cancelled, startTime, "Monitoring cancelled");
        }
        catch (OperationCanceledException)
        {
            return CreateResult(GameOutcome.Cancelled, startTime, "Monitoring cancelled");
        }
        finally
        {
            _isMonitoring = false;
            _gameTimer.Stop();
        }
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        _isMonitoring = false;
    }

    /// <inheritdoc />
    public GameState ProcessFrame(ScreenFrame frame)
    {
        if (!_isMonitoring)
        {
            // Start monitoring if not active
            _isMonitoring = true;
            _gameTimer.Start();
        }

        try
        {
            // Detect state from frame
            var result = _detector.Detect(frame);

            // Track activity
            TrackActivity(frame);

            // Update last activity time
            _lastActivityTime = DateTime.UtcNow;
            _notRespondingSince = null; // Window is responding if we got a frame

            // Handle state transition with debouncing
            HandleStateDetection(result);

            return _currentState;
        }
        catch (Exception ex)
        {
            Log($"Error processing frame: {ex.Message}");
            return _currentState;
        }
    }

    private void HandleStateDetection(DetectionResult result)
    {
        var detectedState = result.State;
        var confidence = result.Confidence;

        // Ignore low-confidence detections
        if (confidence < _config.StateTransitionThreshold && detectedState != GameState.Unknown)
        {
            return;
        }

        // Same state as current - reset pending
        if (detectedState == _currentState)
        {
            _pendingState = GameState.Unknown;
            _pendingStateCount = 0;
            return;
        }

        // Same as pending state - increment count
        if (detectedState == _pendingState)
        {
            _pendingStateCount++;

            // Check if we have enough consecutive detections
            if (_pendingStateCount >= _config.ConsecutiveDetectionsRequired)
            {
                TransitionToState(detectedState, confidence);
            }
        }
        else
        {
            // New pending state
            _pendingState = detectedState;
            _pendingStateCount = 1;
        }
    }

    private void TransitionToState(GameState newState, float confidence)
    {
        var previousState = _currentState;
        _currentState = newState;
        _pendingState = GameState.Unknown;
        _pendingStateCount = 0;

        var elapsed = _gameTimer.Elapsed;

        // Record state transition
        var transition = new StateTransition
        {
            FromState = previousState,
            ToState = newState,
            Timestamp = DateTime.UtcNow,
            Confidence = confidence,
            ElapsedTime = elapsed
        };
        _stateHistory.Add(transition);

        Log($"State transition: {previousState} -> {newState} (confidence: {confidence:F2}, elapsed: {elapsed})");

        // Track loading state start time
        if (newState == GameState.Loading)
        {
            _loadingStartTime = DateTime.UtcNow;
        }
        else
        {
            _loadingStartTime = null;
        }

        // Raise event
        StateChanged?.Invoke(this, new StateChangedEventArgs
        {
            PreviousState = previousState,
            NewState = newState,
            Confidence = confidence,
            ElapsedTime = elapsed
        });

        // Check for game end conditions
        if (newState == GameState.Victory || newState == GameState.Defeat)
        {
            var outcome = newState == GameState.Victory ? GameOutcome.Victory : GameOutcome.Defeat;

            GameEnded?.Invoke(this, new GameEndedEventArgs
            {
                Outcome = outcome,
                FinalState = newState,
                Duration = elapsed
            });

            _isMonitoring = false;
        }
    }

    private void TrackActivity(ScreenFrame currentFrame)
    {
        if (!_config.EnableActivityTracking)
        {
            _activeFrameCount++;
            return;
        }

        using var currentMat = ConvertFrameToGray(currentFrame);

        if (_previousFrame == null)
        {
            _previousFrame = currentMat.Clone();
            _activeFrameCount++;
            return;
        }

        // Calculate frame difference
        using var diff = new Mat();
        Cv2.Absdiff(currentMat, _previousFrame, diff);

        // Calculate normalized difference
        var meanDiff = Cv2.Mean(diff).Val0 / 255.0;

        if (meanDiff < _config.ActivityThreshold)
        {
            _idleFrameCount++;
        }
        else
        {
            _activeFrameCount++;
            _lastActivityTime = DateTime.UtcNow;
        }

        // Update previous frame
        _previousFrame.Dispose();
        _previousFrame = currentMat.Clone();
    }

    private (GameOutcome, string)? CheckTimeouts()
    {
        var elapsed = _gameTimer.Elapsed;

        // Maximum game duration timeout
        if (elapsed > _config.MaxGameDuration)
        {
            Log($"Game timeout after {elapsed}");
            return (GameOutcome.Timeout, $"Game duration exceeded {_config.MaxGameDuration}");
        }

        // Idle timeout (no activity)
        if (_config.EnableActivityTracking)
        {
            var idleTime = DateTime.UtcNow - _lastActivityTime;
            if (idleTime > _config.IdleTimeout && _currentState == GameState.InGame)
            {
                Log($"Game stuck - no activity for {idleTime}");
                return (GameOutcome.Stuck, $"No activity for {idleTime}");
            }
        }

        // Loading timeout
        if (_loadingStartTime.HasValue)
        {
            var loadingTime = DateTime.UtcNow - _loadingStartTime.Value;
            if (loadingTime > _config.LoadingTimeout)
            {
                Log($"Loading timeout after {loadingTime}");
                return (GameOutcome.Stuck, $"Loading screen stuck for {loadingTime}");
            }
        }

        return null;
    }

    private (GameOutcome, string)? CheckWindowHealth()
    {
        // Check if game process exists
        var processes = Process.GetProcessesByName(_config.GameProcessName);
        if (processes.Length == 0)
        {
            Log("Game process not found");
            return (GameOutcome.Crashed, "Game process not found");
        }

        // Check if window is responding
        foreach (var process in processes)
        {
            try
            {
                if (!IsWindowResponding(process))
                {
                    if (_notRespondingSince == null)
                    {
                        _notRespondingSince = DateTime.UtcNow;
                    }
                    else if (DateTime.UtcNow - _notRespondingSince > _config.NotRespondingTimeout)
                    {
                        Log($"Game window not responding for {_config.NotRespondingTimeout}");
                        return (GameOutcome.Crashed, "Game window not responding");
                    }
                }
                else
                {
                    _notRespondingSince = null;
                }
            }
            catch
            {
                // Ignore errors checking process state
            }
            finally
            {
                process.Dispose();
            }
        }

        return null;
    }

    private static bool IsWindowResponding(Process process)
    {
        // On Windows, check if the main window is responding
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                return process.Responding;
            }
            catch
            {
                return true; // Assume responding if we can't check
            }
        }

        return true;
    }

    private MonitoringResult CreateResult(GameOutcome outcome, DateTime startTime, string? reason = null)
    {
        var endTime = DateTime.UtcNow;
        var duration = _gameTimer.Elapsed;

        Log($"Monitoring ended: {outcome}, duration: {duration}, reason: {reason}");

        return new MonitoringResult
        {
            Outcome = outcome,
            FinalState = _currentState,
            Duration = duration,
            StartTime = startTime,
            EndTime = endTime,
            EndReason = reason,
            StateChangeCount = _stateHistory.Count,
            IdleFrameCount = _idleFrameCount,
            ActiveFrameCount = _activeFrameCount,
            StateHistory = new List<StateTransition>(_stateHistory)
        };
    }

    private static Mat ConvertFrameToGray(ScreenFrame frame)
    {
        var matType = frame.Format switch
        {
            PixelFormat.BGRA32 => MatType.CV_8UC4,
            PixelFormat.RGB24 => MatType.CV_8UC3,
            PixelFormat.Grayscale8 => MatType.CV_8UC1,
            _ => throw new ArgumentException($"Unsupported pixel format: {frame.Format}")
        };

        var mat = new Mat(frame.Height, frame.Width, matType);

        unsafe
        {
            fixed (byte* dataPtr = frame.Data)
            {
                Buffer.MemoryCopy(
                    dataPtr,
                    (void*)mat.DataPointer,
                    frame.Data.Length,
                    frame.Data.Length);
            }
        }

        // Convert to grayscale
        Mat gray;
        if (frame.Format == PixelFormat.Grayscale8)
        {
            gray = mat;
        }
        else
        {
            gray = new Mat();
            var code = frame.Format == PixelFormat.BGRA32
                ? ColorConversionCodes.BGRA2GRAY
                : ColorConversionCodes.BGR2GRAY;
            Cv2.CvtColor(mat, gray, code);
            mat.Dispose();
        }

        return gray;
    }

    private void Log(string message)
    {
        if (_config.EnableLogging)
        {
            Console.WriteLine($"[GameMonitor] {DateTime.UtcNow:HH:mm:ss.fff} - {message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _isMonitoring = false;
        _previousFrame?.Dispose();
        _previousFrame = null;

        if (_detector is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}
