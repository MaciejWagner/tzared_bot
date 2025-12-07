#
# Run Demo on VM DEV with Screenshots Collection
# This script deploys project, runs demos, and collects screenshots via Enhanced Session
#
# IMPORTANT: For screenshots to work, this script should be run while RDP/Enhanced Session
# is connected to the VM, OR use the scheduled task approach.
#

param(
    [string]$VMName = "DEV",
    [string]$VMUsername = "test",
    [string]$VMPassword = "password123",
    [string]$LocalProjectPath = "C:\Users\maciek\ai_experiments\tzar_bot",
    [string]$VMProjectPath = "C:\TzarBot",
    [string]$LocalOutputDir = "C:\Users\maciek\ai_experiments\tzar_bot\project_management\demo"
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host @"

===============================================================================
                     TzarBot Demo Runner with Screenshots
===============================================================================
VM:           $VMName
Timestamp:    $timestamp
Local Output: $LocalOutputDir
===============================================================================

"@ -ForegroundColor Cyan

# Create credential
$secPassword = ConvertTo-SecureString $VMPassword -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($VMUsername, $secPassword)

# -----------------------------------------
# Step 1: Clean and prepare VM
# -----------------------------------------
Write-Host "[1/6] Preparing VM..." -ForegroundColor Yellow

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    # Set execution policy
    Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
    Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser -Force

    Remove-Item "C:\TzarBot" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\TzarBot-Demo" -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path "C:\TzarBot" -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\TzarBot\scripts\demo" -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\TzarBot-Demo\Phase0" -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\TzarBot-Demo\Phase1" -Force | Out-Null
    Write-Host "VM directories prepared"
}

# -----------------------------------------
# Step 2: Create archives and copy to VM
# -----------------------------------------
Write-Host "[2/6] Deploying project to VM..." -ForegroundColor Yellow

Push-Location $LocalProjectPath

# Create ZIP archives
Compress-Archive -Path "src\*" -DestinationPath "src.zip" -Force
Compress-Archive -Path "tests\*" -DestinationPath "tests.zip" -Force
Compress-Archive -Path "scripts\*" -DestinationPath "scripts.zip" -Force

# Copy to VM
Copy-VMFile -Name $VMName -SourcePath "$LocalProjectPath\src.zip" -DestinationPath "C:\TzarBot\src.zip" -CreateFullPath -FileSource Host -Force
Copy-VMFile -Name $VMName -SourcePath "$LocalProjectPath\tests.zip" -DestinationPath "C:\TzarBot\tests.zip" -CreateFullPath -FileSource Host -Force
Copy-VMFile -Name $VMName -SourcePath "$LocalProjectPath\scripts.zip" -DestinationPath "C:\TzarBot\scripts.zip" -CreateFullPath -FileSource Host -Force
Copy-VMFile -Name $VMName -SourcePath "$LocalProjectPath\TzarBot.sln" -DestinationPath "C:\TzarBot\TzarBot.sln" -CreateFullPath -FileSource Host -Force

# Cleanup local ZIPs
Remove-Item "src.zip", "tests.zip", "scripts.zip" -Force -ErrorAction SilentlyContinue

Pop-Location
Write-Host "Project files copied to VM"

# -----------------------------------------
# Step 3: Extract and prepare on VM
# -----------------------------------------
Write-Host "[3/6] Extracting files on VM..." -ForegroundColor Yellow

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    Expand-Archive -Path "C:\TzarBot\src.zip" -DestinationPath "C:\TzarBot\src" -Force
    Expand-Archive -Path "C:\TzarBot\tests.zip" -DestinationPath "C:\TzarBot\tests" -Force
    Expand-Archive -Path "C:\TzarBot\scripts.zip" -DestinationPath "C:\TzarBot\scripts" -Force
    Remove-Item "C:\TzarBot\*.zip" -Force
    Write-Host "Files extracted successfully"
}

# -----------------------------------------
# Step 4: Run Phase 0 Demo
# -----------------------------------------
Write-Host "[4/6] Running Phase 0 Demo..." -ForegroundColor Yellow

$phase0Result = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
    Set-Location "C:\TzarBot\scripts\demo"
    $result = & ".\Run-Phase0Demo.ps1" -OutputDir "C:\TzarBot-Demo\Phase0" -SkipScreenshots
    return $result
}

Write-Host "Phase 0 Result: Pass=$($phase0Result.PassCount), Fail=$($phase0Result.FailCount)" -ForegroundColor $(if ($phase0Result.Success) { "Green" } else { "Red" })

# -----------------------------------------
# Step 5: Run Phase 1 Demo
# -----------------------------------------
Write-Host "[5/6] Running Phase 1 Demo..." -ForegroundColor Yellow

$phase1Result = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
    Set-Location "C:\TzarBot\scripts\demo"
    $result = & ".\Run-Phase1Demo.ps1" -OutputDir "C:\TzarBot-Demo\Phase1" -ProjectPath "C:\TzarBot" -SkipScreenshots
    return $result
}

Write-Host "Phase 1 Result: Tests Passed=$($phase1Result.PassedTests), Failed=$($phase1Result.FailedTests)" -ForegroundColor $(if ($phase1Result.Success) { "Green" } else { "Yellow" })

# -----------------------------------------
# Step 6: Copy results from VM
# -----------------------------------------
Write-Host "[6/6] Copying results from VM..." -ForegroundColor Yellow

# Create local evidence directories
$phase0Evidence = Join-Path $LocalOutputDir "phase_0_evidence"
$phase1Evidence = Join-Path $LocalOutputDir "phase_1_evidence"
New-Item -ItemType Directory -Path $phase0Evidence -Force | Out-Null
New-Item -ItemType Directory -Path $phase1Evidence -Force | Out-Null

# Create PS Session for copying
$session = New-PSSession -VMName $VMName -Credential $cred

# Copy Phase 0 results
Copy-Item -FromSession $session -Path "C:\TzarBot-Demo\Phase0\*" -Destination $phase0Evidence -Recurse -Force

# Copy Phase 1 results
Copy-Item -FromSession $session -Path "C:\TzarBot-Demo\Phase1\*" -Destination $phase1Evidence -Recurse -Force

Remove-PSSession $session

# -----------------------------------------
# Summary
# -----------------------------------------
Write-Host @"

===============================================================================
                           Demo Execution Complete
===============================================================================
Phase 0: $(if ($phase0Result.Success) { "PASSED" } else { "FAILED" }) - $($phase0Result.PassCount)/$($phase0Result.PassCount + $phase0Result.FailCount) tests
Phase 1: $(if ($phase1Result.Success) { "PASSED" } else { "PARTIAL" }) - $($phase1Result.PassedTests) passed, $($phase1Result.FailedTests) failed

Results saved to:
- Phase 0: $phase0Evidence
- Phase 1: $phase1Evidence
===============================================================================

"@ -ForegroundColor $(if ($phase0Result.Success -and $phase1Result.Success) { "Green" } else { "Yellow" })

# Return summary
return @{
    Timestamp = $timestamp
    Phase0 = $phase0Result
    Phase1 = $phase1Result
    OutputDir = $LocalOutputDir
}
