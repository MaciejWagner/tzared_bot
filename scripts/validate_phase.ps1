<#
.SYNOPSIS
    Validates a specific phase or task of the TzarBot project.

.DESCRIPTION
    Runs tests and checks for the specified phase. Can validate
    an entire phase or a specific task within a phase.

.PARAMETER Phase
    The phase number to validate (1-6).

.PARAMETER Task
    Optional. The task number within the phase to validate.

.PARAMETER Verbose
    Show detailed output.

.EXAMPLE
    .\validate_phase.ps1 -Phase 1
    Validates all tasks in Phase 1.

.EXAMPLE
    .\validate_phase.ps1 -Phase 1 -Task 2
    Validates only task F1.T2.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 6)]
    [int]$Phase,

    [Parameter(Mandatory = $false)]
    [int]$Task,

    [switch]$Detailed
)

$ErrorActionPreference = "Stop"
$Script:TestsFailed = 0
$Script:TestsPassed = 0
$Script:Warnings = @()

# Configuration
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ReportsDir = Join-Path $ProjectRoot "reports"
$TestResultsDir = Join-Path $ReportsDir "test_results"

# Ensure directories exist
New-Item -ItemType Directory -Path $ReportsDir -Force | Out-Null
New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null

function Write-Status {
    param([string]$Message, [string]$Status)

    $color = switch ($Status) {
        "OK" { "Green" }
        "FAIL" { "Red" }
        "WARN" { "Yellow" }
        "INFO" { "Cyan" }
        default { "White" }
    }

    Write-Host "[$Status] " -ForegroundColor $color -NoNewline
    Write-Host $Message
}

function Test-DotnetBuild {
    param([string]$Project)

    Write-Host "Building $Project..." -ForegroundColor Cyan

    $output = dotnet build $Project --no-restore 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        $Script:TestsPassed++
        Write-Status "Build successful: $Project" "OK"
        return $true
    } else {
        $Script:TestsFailed++
        Write-Status "Build failed: $Project" "FAIL"
        if ($Detailed) {
            Write-Host $output -ForegroundColor Red
        }
        return $false
    }
}

function Test-DotnetTests {
    param([string]$Filter)

    Write-Host "Running tests with filter: $Filter..." -ForegroundColor Cyan

    $testProject = Join-Path $ProjectRoot "tests\TzarBot.Tests\TzarBot.Tests.csproj"

    if (-not (Test-Path $testProject)) {
        $Script:Warnings += "Test project not found: $testProject"
        Write-Status "Test project not found" "WARN"
        return $true
    }

    $output = dotnet test $testProject --filter $Filter --no-build 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        $Script:TestsPassed++
        Write-Status "Tests passed: $Filter" "OK"
        return $true
    } else {
        $Script:TestsFailed++
        Write-Status "Tests failed: $Filter" "FAIL"
        if ($Detailed) {
            Write-Host $output -ForegroundColor Red
        }
        return $false
    }
}

function Test-FileExists {
    param([string]$Path, [string]$Description)

    $fullPath = Join-Path $ProjectRoot $Path

    if (Test-Path $fullPath) {
        $Script:TestsPassed++
        Write-Status "File exists: $Description" "OK"
        return $true
    } else {
        $Script:TestsFailed++
        Write-Status "File missing: $Description ($Path)" "FAIL"
        return $false
    }
}

function Test-ProjectExists {
    param([string]$ProjectPath)

    $fullPath = Join-Path $ProjectRoot $ProjectPath

    if (Test-Path $fullPath) {
        $Script:TestsPassed++
        Write-Status "Project exists: $ProjectPath" "OK"
        return $true
    } else {
        $Script:TestsFailed++
        Write-Status "Project missing: $ProjectPath" "FAIL"
        return $false
    }
}

# Phase validation functions
function Validate-Phase1 {
    param([int]$TaskNum)

    Write-Host "`n=== Validating Phase 1: Game Interface ===" -ForegroundColor Magenta

    $tasks = @{
        1 = @{
            Name = "Project Setup"
            Tests = @(
                { Test-FileExists "TzarBot.sln" "Solution file" }
                { Test-ProjectExists "src\TzarBot.GameInterface\TzarBot.GameInterface.csproj" }
                { Test-ProjectExists "src\TzarBot.Common\TzarBot.Common.csproj" }
                { Test-ProjectExists "tests\TzarBot.Tests\TzarBot.Tests.csproj" }
            )
        }
        2 = @{
            Name = "Screen Capture"
            Tests = @(
                { Test-FileExists "src\TzarBot.GameInterface\Capture\IScreenCapture.cs" "IScreenCapture interface" }
                { Test-FileExists "src\TzarBot.GameInterface\Capture\DxgiScreenCapture.cs" "DxgiScreenCapture implementation" }
                { Test-DotnetTests "FullyQualifiedName~Phase1.ScreenCapture" }
            )
        }
        3 = @{
            Name = "Input Injection"
            Tests = @(
                { Test-FileExists "src\TzarBot.GameInterface\Input\IInputInjector.cs" "IInputInjector interface" }
                { Test-FileExists "src\TzarBot.GameInterface\Input\Win32InputInjector.cs" "Win32InputInjector implementation" }
                { Test-DotnetTests "FullyQualifiedName~Phase1.InputInjector" }
            )
        }
        4 = @{
            Name = "IPC Named Pipes"
            Tests = @(
                { Test-FileExists "src\TzarBot.GameInterface\IPC\PipeServer.cs" "PipeServer" }
                { Test-FileExists "src\TzarBot.GameInterface\IPC\PipeClient.cs" "PipeClient" }
                { Test-DotnetTests "FullyQualifiedName~Phase1.Ipc" }
            )
        }
        5 = @{
            Name = "Window Detection"
            Tests = @(
                { Test-FileExists "src\TzarBot.GameInterface\Window\WindowDetector.cs" "WindowDetector" }
                { Test-DotnetTests "FullyQualifiedName~Phase1.WindowDetector" }
            )
        }
        6 = @{
            Name = "Integration Tests"
            Tests = @(
                { Test-DotnetTests "FullyQualifiedName~Phase1.Integration" }
            )
        }
    }

    if ($TaskNum) {
        if ($tasks.ContainsKey($TaskNum)) {
            Write-Host "`n--- Task F1.T$TaskNum : $($tasks[$TaskNum].Name) ---" -ForegroundColor Yellow
            foreach ($test in $tasks[$TaskNum].Tests) {
                & $test
            }
        } else {
            Write-Status "Invalid task number: $TaskNum" "FAIL"
        }
    } else {
        foreach ($t in $tasks.Keys | Sort-Object) {
            Write-Host "`n--- Task F1.T$t : $($tasks[$t].Name) ---" -ForegroundColor Yellow
            foreach ($test in $tasks[$t].Tests) {
                & $test
            }
        }
    }
}

function Validate-Phase2 {
    param([int]$TaskNum)

    Write-Host "`n=== Validating Phase 2: Neural Network ===" -ForegroundColor Magenta

    $tasks = @{
        1 = @{
            Name = "NetworkGenome & Serialization"
            Tests = @(
                { Test-ProjectExists "src\TzarBot.NeuralNetwork\TzarBot.NeuralNetwork.csproj" }
                { Test-FileExists "src\TzarBot.NeuralNetwork\Genome\NetworkGenome.cs" "NetworkGenome" }
                { Test-DotnetTests "FullyQualifiedName~Phase2.Genome" }
            )
        }
        2 = @{
            Name = "Image Preprocessor"
            Tests = @(
                { Test-FileExists "src\TzarBot.NeuralNetwork\Preprocessing\ImagePreprocessor.cs" "ImagePreprocessor" }
                { Test-DotnetTests "FullyQualifiedName~Phase2.Preprocessor" }
            )
        }
        3 = @{
            Name = "ONNX Network Builder"
            Tests = @(
                { Test-FileExists "src\TzarBot.NeuralNetwork\Builder\OnnxNetworkBuilder.cs" "OnnxNetworkBuilder" }
                { Test-DotnetTests "FullyQualifiedName~Phase2.NetworkBuilder" }
            )
        }
        4 = @{
            Name = "Inference Engine"
            Tests = @(
                { Test-FileExists "src\TzarBot.NeuralNetwork\Inference\OnnxInferenceEngine.cs" "OnnxInferenceEngine" }
                { Test-DotnetTests "FullyQualifiedName~Phase2.Inference" }
            )
        }
        5 = @{
            Name = "Integration Tests"
            Tests = @(
                { Test-DotnetTests "FullyQualifiedName~Phase2.Integration" }
            )
        }
    }

    if ($TaskNum) {
        if ($tasks.ContainsKey($TaskNum)) {
            Write-Host "`n--- Task F2.T$TaskNum : $($tasks[$TaskNum].Name) ---" -ForegroundColor Yellow
            foreach ($test in $tasks[$TaskNum].Tests) {
                & $test
            }
        }
    } else {
        foreach ($t in $tasks.Keys | Sort-Object) {
            Write-Host "`n--- Task F2.T$t : $($tasks[$t].Name) ---" -ForegroundColor Yellow
            foreach ($test in $tasks[$t].Tests) {
                & $test
            }
        }
    }
}

function Validate-Phase3 {
    param([int]$TaskNum)

    Write-Host "`n=== Validating Phase 3: Genetic Algorithm ===" -ForegroundColor Magenta

    # Similar structure for Phase 3 tasks
    $tasks = @{
        1 = @{ Name = "GA Engine Core"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase3.GAEngine" }) }
        2 = @{ Name = "Mutation Operators"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase3.Mutation" }) }
        3 = @{ Name = "Crossover Operators"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase3.Crossover" }) }
        4 = @{ Name = "Selection & Elitism"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase3.Selection" }) }
        5 = @{ Name = "Fitness & Persistence"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase3.Fitness|Phase3.Persistence" }) }
    }

    if ($TaskNum -and $tasks.ContainsKey($TaskNum)) {
        Write-Host "`n--- Task F3.T$TaskNum : $($tasks[$TaskNum].Name) ---" -ForegroundColor Yellow
        foreach ($test in $tasks[$TaskNum].Tests) { & $test }
    } else {
        foreach ($t in $tasks.Keys | Sort-Object) {
            Write-Host "`n--- Task F3.T$t : $($tasks[$t].Name) ---" -ForegroundColor Yellow
            foreach ($test in $tasks[$t].Tests) { & $test }
        }
    }
}

function Validate-Phase4 {
    param([int]$TaskNum)

    Write-Host "`n=== Validating Phase 4: Hyper-V Infrastructure ===" -ForegroundColor Magenta

    $tasks = @{
        1 = @{ Name = "Template VM (Manual)"; Tests = @({ Write-Status "Manual validation required" "WARN"; $true }) }
        2 = @{ Name = "VM Cloning Scripts"; Tests = @({ Test-FileExists "scripts\vm\New-TzarWorkerVM.ps1" "VM creation script" }) }
        3 = @{ Name = "VM Manager"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase4.VMManager" }) }
        4 = @{ Name = "Orchestrator Service"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase4.Orchestrator" }) }
        5 = @{ Name = "Communication Protocol"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase4.Communication" }) }
        6 = @{ Name = "Integration Tests"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase4.Integration" }) }
    }

    if ($TaskNum -and $tasks.ContainsKey($TaskNum)) {
        Write-Host "`n--- Task F4.T$TaskNum : $($tasks[$TaskNum].Name) ---" -ForegroundColor Yellow
        foreach ($test in $tasks[$TaskNum].Tests) { & $test }
    } else {
        foreach ($t in $tasks.Keys | Sort-Object) {
            Write-Host "`n--- Task F4.T$t : $($tasks[$t].Name) ---" -ForegroundColor Yellow
            foreach ($test in $tasks[$t].Tests) { & $test }
        }
    }
}

function Validate-Phase5 {
    param([int]$TaskNum)

    Write-Host "`n=== Validating Phase 5: Game State Detection ===" -ForegroundColor Magenta

    $tasks = @{
        1 = @{ Name = "Template Capture Tool"; Tests = @({ Test-FileExists "tools\TemplateCapturer\TemplateCapturer.csproj" "Template Capturer project" }) }
        2 = @{ Name = "GameStateDetector"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase5.GameStateDetector" }) }
        3 = @{ Name = "GameMonitor"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase5.GameMonitor" }) }
        4 = @{ Name = "Stats Extraction"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase5.StatsExtraction" }) }
    }

    if ($TaskNum -and $tasks.ContainsKey($TaskNum)) {
        Write-Host "`n--- Task F5.T$TaskNum : $($tasks[$TaskNum].Name) ---" -ForegroundColor Yellow
        foreach ($test in $tasks[$TaskNum].Tests) { & $test }
    } else {
        foreach ($t in $tasks.Keys | Sort-Object) {
            Write-Host "`n--- Task F5.T$t : $($tasks[$t].Name) ---" -ForegroundColor Yellow
            foreach ($test in $tasks[$t].Tests) { & $test }
        }
    }
}

function Validate-Phase6 {
    param([int]$TaskNum)

    Write-Host "`n=== Validating Phase 6: Training Pipeline ===" -ForegroundColor Magenta

    $tasks = @{
        1 = @{ Name = "Training Loop Core"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase6.TrainingPipeline" }) }
        2 = @{ Name = "Curriculum Manager"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase6.Curriculum" }) }
        3 = @{ Name = "Checkpoint Manager"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase6.Checkpoint" }) }
        4 = @{ Name = "Tournament System"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase6.Tournament" }) }
        5 = @{ Name = "Blazor Dashboard"; Tests = @({ Test-DotnetBuild "src\TzarBot.Dashboard\TzarBot.Dashboard.csproj" }) }
        6 = @{ Name = "Full Integration"; Tests = @({ Test-DotnetTests "FullyQualifiedName~Phase6.Integration" }) }
    }

    if ($TaskNum -and $tasks.ContainsKey($TaskNum)) {
        Write-Host "`n--- Task F6.T$TaskNum : $($tasks[$TaskNum].Name) ---" -ForegroundColor Yellow
        foreach ($test in $tasks[$TaskNum].Tests) { & $test }
    } else {
        foreach ($t in $tasks.Keys | Sort-Object) {
            Write-Host "`n--- Task F6.T$t : $($tasks[$t].Name) ---" -ForegroundColor Yellow
            foreach ($test in $tasks[$t].Tests) { & $test }
        }
    }
}

# Main execution
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TzarBot Phase Validation" -ForegroundColor Cyan
Write-Host "  Phase: $Phase $(if ($Task) { "Task: $Task" })" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# First, try to restore packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Cyan
$solutionPath = Join-Path $ProjectRoot "TzarBot.sln"
if (Test-Path $solutionPath) {
    dotnet restore $solutionPath 2>&1 | Out-Null
}

# Run appropriate validation
switch ($Phase) {
    1 { Validate-Phase1 -TaskNum $Task }
    2 { Validate-Phase2 -TaskNum $Task }
    3 { Validate-Phase3 -TaskNum $Task }
    4 { Validate-Phase4 -TaskNum $Task }
    5 { Validate-Phase5 -TaskNum $Task }
    6 { Validate-Phase6 -TaskNum $Task }
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  VALIDATION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Passed: $Script:TestsPassed" -ForegroundColor Green
Write-Host "  Failed: $Script:TestsFailed" -ForegroundColor $(if ($Script:TestsFailed -gt 0) { "Red" } else { "Green" })

if ($Script:Warnings.Count -gt 0) {
    Write-Host "  Warnings:" -ForegroundColor Yellow
    foreach ($warning in $Script:Warnings) {
        Write-Host "    - $warning" -ForegroundColor Yellow
    }
}

# Generate report
$reportPath = Join-Path $ReportsDir "phase_${Phase}_validation.md"
$reportContent = @"
# Phase $Phase Validation Report

**Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Task:** $(if ($Task) { "F$Phase.T$Task" } else { "All tasks" })

## Results

- **Passed:** $Script:TestsPassed
- **Failed:** $Script:TestsFailed

## Status

$(if ($Script:TestsFailed -eq 0) { "All validations passed." } else { "Some validations failed. Please review." })

## Warnings

$(if ($Script:Warnings.Count -gt 0) { $Script:Warnings -join "`n- " } else { "None" })

---
*Generated by validate_phase.ps1*
"@

Set-Content -Path $reportPath -Value $reportContent
Write-Host "`nReport saved to: $reportPath" -ForegroundColor Cyan

# Exit code
if ($Script:TestsFailed -gt 0) {
    exit 1
} else {
    exit 0
}
