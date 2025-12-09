using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using TzarBot.Orchestrator.VM;

namespace TzarBot.Orchestrator.Communication;

/// <summary>
/// Handles reliable genome transfer to VMs with verification
/// </summary>
public class GenomeTransfer
{
    private readonly ILogger<GenomeTransfer> _logger;
    private readonly IVMManager _vmManager;
    private readonly string _localTempPath;

    public GenomeTransfer(ILogger<GenomeTransfer> logger, IVMManager vmManager)
    {
        _logger = logger;
        _vmManager = vmManager;
        _localTempPath = Path.Combine(Path.GetTempPath(), "TzarBot_Genomes");
        Directory.CreateDirectory(_localTempPath);
    }

    /// <summary>
    /// Transfers a genome to a VM with verification
    /// </summary>
    public async Task<GenomeTransferResult> TransferGenomeAsync(
        string vmName,
        string genomeId,
        byte[] genomeData,
        string remotePath,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var localFile = Path.Combine(_localTempPath, $"{genomeId}.bin");
        var checksum = ComputeChecksum(genomeData);

        _logger.LogInformation("Transferring genome {GenomeId} to VM {VMName} ({Size} bytes, checksum: {Checksum})",
            genomeId, vmName, genomeData.Length, checksum);

        try
        {
            // Write to local temp file
            await File.WriteAllBytesAsync(localFile, genomeData, cancellationToken);

            // Transfer to VM
            var transferSuccess = await _vmManager.SendFileToVMAsync(vmName, localFile, remotePath, cancellationToken);
            if (!transferSuccess)
            {
                return new GenomeTransferResult
                {
                    Success = false,
                    GenomeId = genomeId,
                    VMName = vmName,
                    ErrorMessage = "Failed to transfer file to VM"
                };
            }

            // Verify checksum on VM
            var (verifySuccess, remoteChecksum, error) = await _vmManager.ExecuteOnVMAsync(
                vmName,
                $@"
                    $path = '{remotePath}'
                    if (Test-Path $path) {{
                        $bytes = [System.IO.File]::ReadAllBytes($path)
                        $hash = [System.Security.Cryptography.SHA256]::Create()
                        $hashBytes = $hash.ComputeHash($bytes)
                        Write-Output ([BitConverter]::ToString($hashBytes) -replace '-', '')
                    }} else {{
                        throw 'File not found after transfer'
                    }}
                ",
                TimeSpan.FromSeconds(30),
                cancellationToken);

            if (!verifySuccess)
            {
                return new GenomeTransferResult
                {
                    Success = false,
                    GenomeId = genomeId,
                    VMName = vmName,
                    ErrorMessage = $"Failed to verify checksum: {error}"
                };
            }

            var checksumMatch = string.Equals(checksum, remoteChecksum.Trim(), StringComparison.OrdinalIgnoreCase);
            if (!checksumMatch)
            {
                _logger.LogError("Checksum mismatch for genome {GenomeId}: local={Local}, remote={Remote}",
                    genomeId, checksum, remoteChecksum);

                return new GenomeTransferResult
                {
                    Success = false,
                    GenomeId = genomeId,
                    VMName = vmName,
                    ErrorMessage = "Checksum verification failed"
                };
            }

            stopwatch.Stop();
            _logger.LogInformation("Genome {GenomeId} transferred to VM {VMName} in {Elapsed}ms",
                genomeId, vmName, stopwatch.ElapsedMilliseconds);

            return new GenomeTransferResult
            {
                Success = true,
                GenomeId = genomeId,
                VMName = vmName,
                TransferDuration = stopwatch.Elapsed,
                Checksum = checksum,
                BytesTransferred = genomeData.Length
            };
        }
        finally
        {
            // Cleanup local temp file
            if (File.Exists(localFile))
            {
                try { File.Delete(localFile); }
                catch { /* ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Retrieves a genome file from a VM
    /// </summary>
    public async Task<byte[]?> RetrieveGenomeAsync(
        string vmName,
        string remotePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving genome from VM {VMName}: {RemotePath}", vmName, remotePath);

        // Read file content as base64 from VM
        var (success, output, error) = await _vmManager.ExecuteOnVMAsync(
            vmName,
            $@"
                $path = '{remotePath}'
                if (Test-Path $path) {{
                    $bytes = [System.IO.File]::ReadAllBytes($path)
                    Write-Output ([Convert]::ToBase64String($bytes))
                }} else {{
                    throw 'File not found'
                }}
            ",
            TimeSpan.FromSeconds(60),
            cancellationToken);

        if (!success)
        {
            _logger.LogError("Failed to retrieve genome from VM {VMName}: {Error}", vmName, error);
            return null;
        }

        try
        {
            var data = Convert.FromBase64String(output.Trim());
            _logger.LogInformation("Retrieved genome from VM {VMName}: {Size} bytes", vmName, data.Length);
            return data;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to decode genome data from VM {VMName}", vmName);
            return null;
        }
    }

    /// <summary>
    /// Cleans up genome files on a VM
    /// </summary>
    public async Task<bool> CleanupGenomesAsync(
        string vmName,
        string remoteDirectory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up genomes on VM {VMName}: {Directory}", vmName, remoteDirectory);

        var (success, _, error) = await _vmManager.ExecuteOnVMAsync(
            vmName,
            $@"
                $dir = '{remoteDirectory}'
                if (Test-Path $dir) {{
                    Get-ChildItem -Path $dir -Filter '*.bin' | Remove-Item -Force
                    Get-ChildItem -Path $dir -Filter '*.trigger' | Remove-Item -Force
                }}
                Write-Output 'Cleanup complete'
            ",
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Genome cleanup failed on VM {VMName}: {Error}", vmName, error);
            return false;
        }

        return true;
    }

    private static string ComputeChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }
}

/// <summary>
/// Result of a genome transfer operation
/// </summary>
public record GenomeTransferResult
{
    public bool Success { get; init; }
    public required string GenomeId { get; init; }
    public required string VMName { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan TransferDuration { get; init; }
    public string? Checksum { get; init; }
    public int BytesTransferred { get; init; }
}
