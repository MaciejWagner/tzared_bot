using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TzarBot.Orchestrator.VM;

/// <summary>
/// Manages Hyper-V virtual machines using PowerShell cmdlets
/// </summary>
public class HyperVManager : IVMManager
{
    private readonly ILogger<HyperVManager> _logger;
    private readonly HyperVManagerOptions _options;
    private readonly SemaphoreSlim _runspaceLock = new(1, 1);
    private Runspace? _runspace;
    private bool _disposed;

    public HyperVManager(ILogger<HyperVManager> logger, IOptions<HyperVManagerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    private async Task<Runspace> GetRunspaceAsync()
    {
        if (_runspace is { RunspaceStateInfo.State: RunspaceState.Opened })
            return _runspace;

        await _runspaceLock.WaitAsync();
        try
        {
            if (_runspace is { RunspaceStateInfo.State: RunspaceState.Opened })
                return _runspace;

            var iss = InitialSessionState.CreateDefault();

            // Import Hyper-V module
            iss.ImportPSModule("Hyper-V");

            _runspace = RunspaceFactory.CreateRunspace(iss);
            _runspace.Open();

            _logger.LogInformation("PowerShell runspace opened with Hyper-V module");
            return _runspace;
        }
        finally
        {
            _runspaceLock.Release();
        }
    }

    private async Task<(IReadOnlyList<PSObject> Results, bool HasErrors, string ErrorText)> ExecutePowerShellAsync(
        string script,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var runspace = await GetRunspaceAsync();

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddScript(script);

        if (parameters != null)
        {
            foreach (var (key, value) in parameters)
            {
                ps.AddParameter(key, value);
            }
        }

        var results = new List<PSObject>();
        var errors = new List<string>();

        var asyncResult = ps.BeginInvoke();

        while (!asyncResult.IsCompleted)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
        }

        foreach (var result in ps.EndInvoke(asyncResult))
        {
            results.Add(result);
        }

        foreach (var error in ps.Streams.Error)
        {
            errors.Add(error.ToString());
            _logger.LogWarning("PowerShell error: {Error}", error);
        }

        return (results, errors.Count > 0, string.Join(Environment.NewLine, errors));
    }

    public async Task<IReadOnlyList<VMInfo>> GetAllWorkersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all worker VMs with prefix: {Prefix}", _options.VMPrefix);

        var script = $@"
            Get-VM | Where-Object {{ $_.Name -like '{_options.VMPrefix}*' }} |
            Select-Object Name, State, Heartbeat, MemoryAssigned, ProcessorCount, Uptime,
                @{{N='VhdPath'; E={{($_.HardDrives | Select-Object -First 1).Path}}}}
        ";

        var (results, hasErrors, _) = await ExecutePowerShellAsync(script, cancellationToken: cancellationToken);

        if (hasErrors || results.Count == 0)
        {
            return Array.Empty<VMInfo>();
        }

        var vms = new List<VMInfo>();
        foreach (var result in results)
        {
            var vm = ParseVMInfo(result);
            if (vm != null)
                vms.Add(vm);
        }

        return vms;
    }

    public async Task<VMInfo?> GetVMAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting VM info for: {VMName}", vmName);

        var script = @"
            param($VMName)
            $vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
            if ($vm) {
                $vm | Select-Object Name, State, Heartbeat, MemoryAssigned, ProcessorCount, Uptime,
                    @{N='VhdPath'; E={($_.HardDrives | Select-Object -First 1).Path}}
            }
        ";

        var (results, _, _) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object> { ["VMName"] = vmName },
            cancellationToken);

        return results.Count > 0 ? ParseVMInfo(results[0]) : null;
    }

    public async Task<bool> StartVMAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting VM: {VMName}", vmName);

        var script = @"
            param($VMName)
            $vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
            if (-not $vm) { throw ""VM not found: $VMName"" }
            if ($vm.State -eq 'Running') { return $true }
            Start-VM -Name $VMName -ErrorAction Stop
            return $true
        ";

        var (_, hasErrors, errorText) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object> { ["VMName"] = vmName },
            cancellationToken);

        if (hasErrors)
        {
            _logger.LogError("Failed to start VM {VMName}: {Error}", vmName, errorText);
            return false;
        }

        _logger.LogInformation("VM started: {VMName}", vmName);
        return true;
    }

    public async Task<bool> StopVMAsync(string vmName, bool force = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping VM: {VMName} (force={Force})", vmName, force);

        var script = force
            ? @"param($VMName) Stop-VM -Name $VMName -Force -TurnOff -ErrorAction Stop"
            : @"param($VMName) Stop-VM -Name $VMName -Force -ErrorAction Stop";

        var (_, hasErrors, errorText) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object> { ["VMName"] = vmName },
            cancellationToken);

        if (hasErrors)
        {
            _logger.LogError("Failed to stop VM {VMName}: {Error}", vmName, errorText);
            return false;
        }

        _logger.LogInformation("VM stopped: {VMName}", vmName);
        return true;
    }

    public async Task<bool> RestartVMAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restarting VM: {VMName}", vmName);

        var script = @"
            param($VMName)
            Restart-VM -Name $VMName -Force -ErrorAction Stop
        ";

        var (_, hasErrors, errorText) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object> { ["VMName"] = vmName },
            cancellationToken);

        if (hasErrors)
        {
            _logger.LogError("Failed to restart VM {VMName}: {Error}", vmName, errorText);
            return false;
        }

        _logger.LogInformation("VM restarted: {VMName}", vmName);
        return true;
    }

    public async Task<bool> WaitForHealthyAsync(string vmName, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Waiting for VM {VMName} to become healthy (timeout: {Timeout}s)", vmName, timeout.TotalSeconds);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var vm = await GetVMAsync(vmName, cancellationToken);
            if (vm?.IsReady == true)
            {
                _logger.LogInformation("VM {VMName} is healthy after {Elapsed}s", vmName, stopwatch.Elapsed.TotalSeconds);
                return true;
            }

            await Task.Delay(_options.HeartbeatCheckIntervalMs, cancellationToken);
        }

        _logger.LogWarning("VM {VMName} did not become healthy within timeout", vmName);
        return false;
    }

    public async Task<bool> CreateWorkerVMAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating worker VM: {VMName}", vmName);

        var vhdPath = Path.Combine(_options.WorkersPath, $"{vmName}.vhdx");

        var script = @"
            param($VMName, $VhdPath, $TemplatePath, $SwitchName, $MemoryStartup, $MemoryMin, $MemoryMax, $ProcessorCount)

            # Create differencing disk
            New-VHD -Path $VhdPath -ParentPath $TemplatePath -Differencing | Out-Null

            # Create VM
            New-VM -Name $VMName -Generation 2 -MemoryStartupBytes $MemoryStartup -VHDPath $VhdPath -SwitchName $SwitchName | Out-Null

            # Configure VM
            Set-VM -Name $VMName -ProcessorCount $ProcessorCount -DynamicMemory -MemoryMinimumBytes $MemoryMin -MemoryMaximumBytes $MemoryMax -AutomaticStartAction Start -AutomaticStopAction ShutDown

            # Disable Secure Boot
            Set-VMFirmware -VMName $VMName -EnableSecureBoot Off -ErrorAction SilentlyContinue

            return $true
        ";

        var (_, hasErrors, errorText) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object>
            {
                ["VMName"] = vmName,
                ["VhdPath"] = vhdPath,
                ["TemplatePath"] = _options.TemplatePath,
                ["SwitchName"] = _options.SwitchName,
                ["MemoryStartup"] = _options.MemoryStartupBytes,
                ["MemoryMin"] = _options.MemoryMinBytes,
                ["MemoryMax"] = _options.MemoryMaxBytes,
                ["ProcessorCount"] = _options.ProcessorCount
            },
            cancellationToken);

        if (hasErrors)
        {
            _logger.LogError("Failed to create VM {VMName}: {Error}", vmName, errorText);
            return false;
        }

        _logger.LogInformation("VM created: {VMName}", vmName);
        return true;
    }

    public async Task<bool> RemoveWorkerVMAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing worker VM: {VMName}", vmName);

        var script = @"
            param($VMName)

            $vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
            if (-not $vm) { return $true }

            # Stop if running
            if ($vm.State -ne 'Off') {
                Stop-VM -Name $VMName -Force -TurnOff -ErrorAction Stop
            }

            # Get VHD path
            $vhdPath = $vm.HardDrives | Select-Object -First 1 -ExpandProperty Path

            # Remove VM
            Remove-VM -Name $VMName -Force -ErrorAction Stop

            # Remove VHD
            if ($vhdPath -and (Test-Path $vhdPath)) {
                Remove-Item $vhdPath -Force -ErrorAction Stop
            }

            return $true
        ";

        var (_, hasErrors, errorText) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object> { ["VMName"] = vmName },
            cancellationToken);

        if (hasErrors)
        {
            _logger.LogError("Failed to remove VM {VMName}: {Error}", vmName, errorText);
            return false;
        }

        _logger.LogInformation("VM removed: {VMName}", vmName);
        return true;
    }

    public async Task<bool> ResetToCheckpointAsync(string vmName, string checkpointName = "Clean", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resetting VM {VMName} to checkpoint: {Checkpoint}", vmName, checkpointName);

        var script = @"
            param($VMName, $CheckpointName)

            # Stop VM if running
            $vm = Get-VM -Name $VMName -ErrorAction Stop
            if ($vm.State -ne 'Off') {
                Stop-VM -Name $VMName -Force -TurnOff -ErrorAction Stop
            }

            # Restore checkpoint
            $checkpoint = Get-VMSnapshot -VMName $VMName -Name $CheckpointName -ErrorAction SilentlyContinue
            if ($checkpoint) {
                Restore-VMSnapshot -VMName $VMName -Name $CheckpointName -Confirm:$false -ErrorAction Stop
            }

            return $true
        ";

        var (_, hasErrors, errorText) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object>
            {
                ["VMName"] = vmName,
                ["CheckpointName"] = checkpointName
            },
            cancellationToken);

        if (hasErrors)
        {
            _logger.LogError("Failed to reset VM {VMName}: {Error}", vmName, errorText);
            return false;
        }

        _logger.LogInformation("VM reset: {VMName}", vmName);
        return true;
    }

    public async Task<string?> GetVMIPAddressAsync(string vmName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting IP address for VM: {VMName}", vmName);

        var script = @"
            param($VMName)
            $adapter = Get-VMNetworkAdapter -VMName $VMName -ErrorAction SilentlyContinue
            if ($adapter -and $adapter.IPAddresses) {
                $adapter.IPAddresses | Where-Object { $_ -match '^\d+\.\d+\.\d+\.\d+$' } | Select-Object -First 1
            }
        ";

        var (results, _, _) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object> { ["VMName"] = vmName },
            cancellationToken);

        var ip = results.FirstOrDefault()?.BaseObject as string;
        return ip;
    }

    public async Task<bool> SendFileToVMAsync(string vmName, string localPath, string remotePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending file to VM {VMName}: {LocalPath} -> {RemotePath}", vmName, localPath, remotePath);

        var script = @"
            param($VMName, $LocalPath, $RemotePath, $Username, $Password)

            $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
            $credential = New-Object System.Management.Automation.PSCredential($Username, $securePassword)

            $session = New-PSSession -VMName $VMName -Credential $credential -ErrorAction Stop
            try {
                Copy-Item -Path $LocalPath -Destination $RemotePath -ToSession $session -ErrorAction Stop
                return $true
            }
            finally {
                Remove-PSSession $session -ErrorAction SilentlyContinue
            }
        ";

        var (_, hasErrors, errorText) = await ExecutePowerShellAsync(
            script,
            new Dictionary<string, object>
            {
                ["VMName"] = vmName,
                ["LocalPath"] = localPath,
                ["RemotePath"] = remotePath,
                ["Username"] = _options.VMUsername,
                ["Password"] = _options.VMPassword
            },
            cancellationToken);

        if (hasErrors)
        {
            _logger.LogError("Failed to send file to VM {VMName}: {Error}", vmName, errorText);
            return false;
        }

        _logger.LogInformation("File sent to VM {VMName}", vmName);
        return true;
    }

    public async Task<(bool Success, string Output, string Error)> ExecuteOnVMAsync(
        string vmName,
        string command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing command on VM {VMName}: {Command}", vmName, command);

        var script = @"
            param($VMName, $Command, $Username, $Password)

            $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
            $credential = New-Object System.Management.Automation.PSCredential($Username, $securePassword)

            $session = New-PSSession -VMName $VMName -Credential $credential -ErrorAction Stop
            try {
                $result = Invoke-Command -Session $session -ScriptBlock ([ScriptBlock]::Create($Command)) -ErrorAction Stop
                return @{
                    Success = $true
                    Output = ($result | Out-String).Trim()
                    Error = ''
                }
            }
            catch {
                return @{
                    Success = $false
                    Output = ''
                    Error = $_.Exception.Message
                }
            }
            finally {
                Remove-PSSession $session -ErrorAction SilentlyContinue
            }
        ";

        using var cts = timeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (timeout.HasValue)
            cts.CancelAfter(timeout.Value);

        try
        {
            var (results, hasErrors, errorText) = await ExecutePowerShellAsync(
                script,
                new Dictionary<string, object>
                {
                    ["VMName"] = vmName,
                    ["Command"] = command,
                    ["Username"] = _options.VMUsername,
                    ["Password"] = _options.VMPassword
                },
                cts.Token);

            if (hasErrors)
            {
                return (false, string.Empty, errorText);
            }

            if (results.Count > 0 && results[0].BaseObject is System.Collections.Hashtable ht)
            {
                return (
                    (bool)(ht["Success"] ?? false),
                    (string)(ht["Output"] ?? string.Empty),
                    (string)(ht["Error"] ?? string.Empty)
                );
            }

            return (true, string.Empty, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return (false, string.Empty, "Operation timed out");
        }
    }

    private VMInfo? ParseVMInfo(PSObject psObject)
    {
        try
        {
            var name = psObject.Properties["Name"]?.Value?.ToString();
            if (string.IsNullOrEmpty(name))
                return null;

            var stateValue = psObject.Properties["State"]?.Value;
            var state = VMState.Unknown;
            if (stateValue != null)
            {
                // Hyper-V returns Microsoft.HyperV.PowerShell.VMState enum
                var stateStr = stateValue.ToString();
                state = stateStr switch
                {
                    "Off" => VMState.Off,
                    "Running" => VMState.Running,
                    "Saved" => VMState.Saved,
                    "Paused" => VMState.Paused,
                    "Starting" => VMState.Starting,
                    "Saving" => VMState.Saving,
                    "Stopping" => VMState.Stopping,
                    "Resuming" => VMState.Resuming,
                    "Critical" => VMState.Critical,
                    "Pausing" => VMState.Pausing,
                    _ => VMState.Unknown
                };
            }

            var heartbeat = psObject.Properties["Heartbeat"]?.Value?.ToString();
            var healthStatus = heartbeat switch
            {
                "OkApplicationsHealthy" => VMHealthStatus.Healthy,
                "OkApplicationsUnknown" => VMHealthStatus.Healthy,
                "NoContact" => VMHealthStatus.NoHeartbeat,
                _ => VMHealthStatus.Unknown
            };

            var memoryAssigned = Convert.ToInt64(psObject.Properties["MemoryAssigned"]?.Value ?? 0);
            var processorCount = Convert.ToInt32(psObject.Properties["ProcessorCount"]?.Value ?? 0);
            var uptime = psObject.Properties["Uptime"]?.Value as TimeSpan? ?? TimeSpan.Zero;
            var vhdPath = psObject.Properties["VhdPath"]?.Value?.ToString();

            return new VMInfo
            {
                Name = name,
                State = state,
                HealthStatus = healthStatus,
                MemoryAssignedBytes = memoryAssigned,
                ProcessorCount = processorCount,
                Uptime = uptime,
                VhdPath = vhdPath
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse VM info");
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _runspace?.Close();
        _runspace?.Dispose();
        _runspaceLock.Dispose();
    }
}

/// <summary>
/// Configuration options for HyperVManager
/// </summary>
public class HyperVManagerOptions
{
    public const string SectionName = "HyperV";

    /// <summary>
    /// Path to the template VHD file
    /// </summary>
    public string TemplatePath { get; set; } = @"C:\VMs\TzarBot-Template.vhdx";

    /// <summary>
    /// Directory for worker VM VHD files
    /// </summary>
    public string WorkersPath { get; set; } = @"C:\VMs\Workers";

    /// <summary>
    /// Prefix for worker VM names
    /// </summary>
    public string VMPrefix { get; set; } = "TzarBot-Worker-";

    /// <summary>
    /// Name of the virtual switch
    /// </summary>
    public string SwitchName { get; set; } = "TzarBotSwitch";

    /// <summary>
    /// Memory startup in bytes (default 2GB for workers to respect 10GB limit)
    /// </summary>
    public long MemoryStartupBytes { get; set; } = 2L * 1024 * 1024 * 1024;

    /// <summary>
    /// Minimum memory in bytes (1GB)
    /// </summary>
    public long MemoryMinBytes { get; set; } = 1L * 1024 * 1024 * 1024;

    /// <summary>
    /// Maximum memory in bytes (3GB)
    /// </summary>
    public long MemoryMaxBytes { get; set; } = 3L * 1024 * 1024 * 1024;

    /// <summary>
    /// Number of virtual CPUs
    /// </summary>
    public int ProcessorCount { get; set; } = 2;

    /// <summary>
    /// Interval for heartbeat checks in milliseconds
    /// </summary>
    public int HeartbeatCheckIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Username for PowerShell Direct
    /// </summary>
    public string VMUsername { get; set; } = "test";

    /// <summary>
    /// Password for PowerShell Direct
    /// </summary>
    public string VMPassword { get; set; } = "password123";
}
