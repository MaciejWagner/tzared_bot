using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TzarBot.Orchestrator.VM;

namespace TzarBot.Orchestrator.Communication;

/// <summary>
/// Communicates with VMs using PowerShell Direct (through Hyper-V)
/// </summary>
public class PowerShellCommunicator : IVMCommunicator
{
    private readonly ILogger<PowerShellCommunicator> _logger;
    private readonly IVMManager _vmManager;
    private readonly CommunicationOptions _options;

    public PowerShellCommunicator(
        ILogger<PowerShellCommunicator> logger,
        IVMManager vmManager,
        IOptions<CommunicationOptions> options)
    {
        _logger = logger;
        _vmManager = vmManager;
        _options = options.Value;
    }

    public async Task<bool> SendGenomeAsync(string vmName, byte[] genomeData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending genome to VM {VMName} ({Size} bytes)", vmName, genomeData.Length);

        // Write genome to temp file
        var tempFile = Path.Combine(Path.GetTempPath(), $"genome_{Guid.NewGuid()}.bin");
        try
        {
            await File.WriteAllBytesAsync(tempFile, genomeData, cancellationToken);

            // Send to VM
            var remotePath = Path.Combine(_options.RemoteGenomePath, "current_genome.bin");
            var success = await _vmManager.SendFileToVMAsync(vmName, tempFile, remotePath, cancellationToken);

            if (!success)
            {
                _logger.LogError("Failed to send genome to VM {VMName}", vmName);
                return false;
            }

            // Signal bot service to load the genome
            var (cmdSuccess, output, error) = await _vmManager.ExecuteOnVMAsync(
                vmName,
                $@"
                    $genomePath = '{remotePath}'
                    if (Test-Path $genomePath) {{
                        # Signal the bot service by creating a trigger file
                        Set-Content -Path '{_options.RemoteGenomePath}\load_genome.trigger' -Value (Get-Date).ToString()
                        Write-Output 'Genome signal sent'
                        return $true
                    }}
                    throw 'Genome file not found after transfer'
                ",
                TimeSpan.FromSeconds(30),
                cancellationToken);

            if (!cmdSuccess)
            {
                _logger.LogError("Failed to signal genome load on VM {VMName}: {Error}", vmName, error);
                return false;
            }

            _logger.LogInformation("Genome sent successfully to VM {VMName}", vmName);
            return true;
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public async Task<EvaluationResult?> ReceiveResultAsync(string vmName, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Waiting for evaluation result from VM {VMName} (timeout: {Timeout}s)", vmName, timeout.TotalSeconds);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var resultPath = Path.Combine(_options.RemoteResultPath, "evaluation_result.json");

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if result file exists
            var (success, output, error) = await _vmManager.ExecuteOnVMAsync(
                vmName,
                $@"
                    $resultPath = '{resultPath}'
                    if (Test-Path $resultPath) {{
                        $content = Get-Content $resultPath -Raw
                        Remove-Item $resultPath -Force  # Consume the result
                        Write-Output $content
                    }} else {{
                        Write-Output 'NO_RESULT'
                    }}
                ",
                TimeSpan.FromSeconds(10),
                cancellationToken);

            if (success && output != "NO_RESULT" && !string.IsNullOrWhiteSpace(output))
            {
                try
                {
                    var result = JsonSerializer.Deserialize<EvaluationResult>(output);
                    if (result != null)
                    {
                        _logger.LogInformation("Received evaluation result from VM {VMName}: Score={Score}, Outcome={Outcome}",
                            vmName, result.FitnessScore, result.Outcome);
                        return result;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse evaluation result from VM {VMName}", vmName);
                }
            }

            await Task.Delay(_options.ResultPollIntervalMs, cancellationToken);
        }

        _logger.LogWarning("Timeout waiting for result from VM {VMName}", vmName);
        return null;
    }

    public async Task<bool> HeartbeatAsync(string vmName, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending heartbeat to VM {VMName}", vmName);

        try
        {
            var (success, output, _) = await _vmManager.ExecuteOnVMAsync(
                vmName,
                "Write-Output 'PONG'",
                timeout,
                cancellationToken);

            var isAlive = success && output.Trim() == "PONG";
            _logger.LogDebug("Heartbeat result for VM {VMName}: {IsAlive}", vmName, isAlive);
            return isAlive;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Heartbeat failed for VM {VMName}", vmName);
            return false;
        }
    }

    public async Task<bool> StartBotServiceAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting bot service on VM {VMName}", vmName);

        var (success, _, error) = await _vmManager.ExecuteOnVMAsync(
            vmName,
            $@"
                $serviceName = '{_options.BotServiceName}'
                $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
                if ($service) {{
                    if ($service.Status -ne 'Running') {{
                        Start-Service -Name $serviceName -ErrorAction Stop
                    }}
                    Write-Output 'Service started'
                    return $true
                }} else {{
                    # Try starting as process if not a service
                    $botPath = '{_options.RemoteBotPath}'
                    if (Test-Path $botPath) {{
                        Start-Process -FilePath $botPath -WindowStyle Hidden
                        Write-Output 'Process started'
                        return $true
                    }}
                    throw 'Bot service/executable not found'
                }}
            ",
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (!success)
        {
            _logger.LogError("Failed to start bot service on VM {VMName}: {Error}", vmName, error);
            return false;
        }

        _logger.LogInformation("Bot service started on VM {VMName}", vmName);
        return true;
    }

    public async Task<bool> StopBotServiceAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping bot service on VM {VMName}", vmName);

        var (success, _, error) = await _vmManager.ExecuteOnVMAsync(
            vmName,
            $@"
                $serviceName = '{_options.BotServiceName}'
                $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
                if ($service) {{
                    if ($service.Status -ne 'Stopped') {{
                        Stop-Service -Name $serviceName -Force -ErrorAction Stop
                    }}
                    Write-Output 'Service stopped'
                    return $true
                }} else {{
                    # Try stopping process if not a service
                    $processes = Get-Process -Name 'TzarBot*' -ErrorAction SilentlyContinue
                    if ($processes) {{
                        $processes | Stop-Process -Force
                        Write-Output 'Processes stopped'
                    }}
                    return $true
                }}
            ",
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (!success)
        {
            _logger.LogError("Failed to stop bot service on VM {VMName}: {Error}", vmName, error);
            return false;
        }

        _logger.LogInformation("Bot service stopped on VM {VMName}", vmName);
        return true;
    }

    public async Task<BotServiceStatus> GetBotStatusAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting bot status from VM {VMName}", vmName);

        var (success, output, _) = await _vmManager.ExecuteOnVMAsync(
            vmName,
            $@"
                $serviceName = '{_options.BotServiceName}'
                $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

                if ($service) {{
                    Write-Output $service.Status.ToString()
                }} else {{
                    # Check for running process
                    $processes = Get-Process -Name 'TzarBot*' -ErrorAction SilentlyContinue
                    if ($processes) {{
                        Write-Output 'Running'
                    }} else {{
                        Write-Output 'Stopped'
                    }}
                }}
            ",
            TimeSpan.FromSeconds(10),
            cancellationToken);

        if (!success)
        {
            return BotServiceStatus.Unknown;
        }

        return output.Trim() switch
        {
            "Running" => BotServiceStatus.Running,
            "Stopped" => BotServiceStatus.Stopped,
            "Starting" or "StartPending" => BotServiceStatus.Starting,
            _ => BotServiceStatus.Unknown
        };
    }
}

/// <summary>
/// Configuration options for VM communication
/// </summary>
public class CommunicationOptions
{
    public const string SectionName = "Communication";

    /// <summary>
    /// Name of the bot Windows service
    /// </summary>
    public string BotServiceName { get; set; } = "TzarBotInterface";

    /// <summary>
    /// Path to the bot executable on the VM
    /// </summary>
    public string RemoteBotPath { get; set; } = @"C:\TzarBot\TzarBot.GameInterface.exe";

    /// <summary>
    /// Path for genome files on the VM
    /// </summary>
    public string RemoteGenomePath { get; set; } = @"C:\TzarBot\Genomes";

    /// <summary>
    /// Path for result files on the VM
    /// </summary>
    public string RemoteResultPath { get; set; } = @"C:\TzarBot\Results";

    /// <summary>
    /// Interval for polling result files
    /// </summary>
    public int ResultPollIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Maximum retries for communication
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retries
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}
