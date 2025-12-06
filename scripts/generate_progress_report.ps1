<#
.SYNOPSIS
    Generates a progress report for the TzarBot project.

.DESCRIPTION
    Analyzes the project state and generates a progress report
    showing completed tasks, current status, and next steps.

.EXAMPLE
    .\generate_progress_report.ps1
#>

[CmdletBinding()]
param()

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ReportsDir = Join-Path $ProjectRoot "reports"
$ProgressFile = Join-Path $ReportsDir "progress.json"

# Task definitions
$AllTasks = @{
    "F1.T1" = @{ Name = "Project Setup"; Phase = 1 }
    "F1.T2" = @{ Name = "Screen Capture"; Phase = 1 }
    "F1.T3" = @{ Name = "Input Injection"; Phase = 1 }
    "F1.T4" = @{ Name = "IPC Named Pipes"; Phase = 1 }
    "F1.T5" = @{ Name = "Window Detection"; Phase = 1 }
    "F1.T6" = @{ Name = "Integration Tests"; Phase = 1 }
    "F2.T1" = @{ Name = "NetworkGenome"; Phase = 2 }
    "F2.T2" = @{ Name = "Image Preprocessor"; Phase = 2 }
    "F2.T3" = @{ Name = "ONNX Builder"; Phase = 2 }
    "F2.T4" = @{ Name = "Inference Engine"; Phase = 2 }
    "F2.T5" = @{ Name = "Integration Tests"; Phase = 2 }
    "F3.T1" = @{ Name = "GA Engine Core"; Phase = 3 }
    "F3.T2" = @{ Name = "Mutation Operators"; Phase = 3 }
    "F3.T3" = @{ Name = "Crossover Operators"; Phase = 3 }
    "F3.T4" = @{ Name = "Selection & Elitism"; Phase = 3 }
    "F3.T5" = @{ Name = "Fitness & Persistence"; Phase = 3 }
    "F4.T1" = @{ Name = "Template VM"; Phase = 4 }
    "F4.T2" = @{ Name = "VM Cloning Scripts"; Phase = 4 }
    "F4.T3" = @{ Name = "VM Manager"; Phase = 4 }
    "F4.T4" = @{ Name = "Orchestrator Service"; Phase = 4 }
    "F4.T5" = @{ Name = "Communication Protocol"; Phase = 4 }
    "F4.T6" = @{ Name = "Integration Tests"; Phase = 4 }
    "F5.T1" = @{ Name = "Template Capture Tool"; Phase = 5 }
    "F5.T2" = @{ Name = "GameStateDetector"; Phase = 5 }
    "F5.T3" = @{ Name = "GameMonitor"; Phase = 5 }
    "F5.T4" = @{ Name = "Stats Extraction"; Phase = 5 }
    "F6.T1" = @{ Name = "Training Loop"; Phase = 6 }
    "F6.T2" = @{ Name = "Curriculum Manager"; Phase = 6 }
    "F6.T3" = @{ Name = "Checkpoint Manager"; Phase = 6 }
    "F6.T4" = @{ Name = "Tournament System"; Phase = 6 }
    "F6.T5" = @{ Name = "Blazor Dashboard"; Phase = 6 }
    "F6.T6" = @{ Name = "Full Integration"; Phase = 6 }
}

# Load or create progress file
if (Test-Path $ProgressFile) {
    $progress = Get-Content $ProgressFile | ConvertFrom-Json
} else {
    $progress = @{
        lastUpdated = (Get-Date).ToString("o")
        phases = @{}
    }
}

# Check file existence for basic status
function Get-TaskStatus {
    param([string]$TaskId)

    # Map task to expected files
    $fileChecks = @{
        "F1.T1" = "TzarBot.sln"
        "F1.T2" = "src\TzarBot.GameInterface\Capture\DxgiScreenCapture.cs"
        "F1.T3" = "src\TzarBot.GameInterface\Input\Win32InputInjector.cs"
        "F1.T4" = "src\TzarBot.GameInterface\IPC\PipeServer.cs"
        "F1.T5" = "src\TzarBot.GameInterface\Window\WindowDetector.cs"
        "F2.T1" = "src\TzarBot.NeuralNetwork\Genome\NetworkGenome.cs"
        "F2.T2" = "src\TzarBot.NeuralNetwork\Preprocessing\ImagePreprocessor.cs"
        "F2.T3" = "src\TzarBot.NeuralNetwork\Builder\OnnxNetworkBuilder.cs"
        "F2.T4" = "src\TzarBot.NeuralNetwork\Inference\OnnxInferenceEngine.cs"
        "F3.T1" = "src\TzarBot.GeneticAlgorithm\Core\GeneticAlgorithmEngine.cs"
        "F4.T2" = "scripts\vm\New-TzarWorkerVM.ps1"
        "F5.T2" = "src\TzarBot.StateDetection\Detection\TemplateMatchingDetector.cs"
        "F6.T1" = "src\TzarBot.Training\Core\TrainingPipeline.cs"
    }

    if ($fileChecks.ContainsKey($TaskId)) {
        $path = Join-Path $ProjectRoot $fileChecks[$TaskId]
        if (Test-Path $path) {
            return "completed"
        }
    }

    return "pending"
}

# Generate status for each task
$taskStatuses = @{}
foreach ($taskId in $AllTasks.Keys) {
    $status = Get-TaskStatus $taskId
    $taskStatuses[$taskId] = @{
        name = $AllTasks[$taskId].Name
        phase = $AllTasks[$taskId].Phase
        status = $status
    }
}

# Calculate phase summaries
$phaseSummaries = @{}
for ($p = 1; $p -le 6; $p++) {
    $phaseTasks = $taskStatuses.GetEnumerator() | Where-Object { $_.Value.phase -eq $p }
    $completed = ($phaseTasks | Where-Object { $_.Value.status -eq "completed" }).Count
    $total = $phaseTasks.Count

    $phaseSummaries[$p] = @{
        name = switch ($p) {
            1 { "Game Interface" }
            2 { "Neural Network" }
            3 { "Genetic Algorithm" }
            4 { "Hyper-V Infrastructure" }
            5 { "Game State Detection" }
            6 { "Training Pipeline" }
        }
        completed = $completed
        total = $total
        percentage = if ($total -gt 0) { [math]::Round(($completed / $total) * 100) } else { 0 }
    }
}

# Overall progress
$totalCompleted = ($taskStatuses.Values | Where-Object { $_.status -eq "completed" }).Count
$totalTasks = $taskStatuses.Count
$overallPercentage = [math]::Round(($totalCompleted / $totalTasks) * 100)

# Update progress.json
$progress.lastUpdated = (Get-Date).ToString("o")
$progress.phases = @{}
for ($p = 1; $p -le 6; $p++) {
    $progress.phases["$p"] = @{
        name = $phaseSummaries[$p].name
        status = if ($phaseSummaries[$p].completed -eq $phaseSummaries[$p].total) { "completed" }
                 elseif ($phaseSummaries[$p].completed -gt 0) { "in_progress" }
                 else { "pending" }
        tasks = @{}
    }

    foreach ($task in ($taskStatuses.GetEnumerator() | Where-Object { $_.Value.phase -eq $p })) {
        $progress.phases["$p"].tasks[$task.Key] = @{
            name = $task.Value.name
            status = $task.Value.status
        }
    }
}

$progress | ConvertTo-Json -Depth 10 | Set-Content $ProgressFile

# Generate markdown report
$reportContent = @"
# TzarBot Progress Report

**Generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Overall Progress

**$totalCompleted / $totalTasks tasks completed ($overallPercentage%)**

``````
[$(('=' * [math]::Floor($overallPercentage / 5)) + (' ' * (20 - [math]::Floor($overallPercentage / 5))))] $overallPercentage%
``````

## Phase Summary

| Phase | Name | Progress | Status |
|-------|------|----------|--------|
"@

for ($p = 1; $p -le 6; $p++) {
    $s = $phaseSummaries[$p]
    $status = if ($s.completed -eq $s.total) { "Complete" }
              elseif ($s.completed -gt 0) { "In Progress" }
              else { "Not Started" }
    $reportContent += "| $p | $($s.name) | $($s.completed)/$($s.total) ($($s.percentage)%) | $status |`n"
}

$reportContent += @"

## Task Details

"@

for ($p = 1; $p -le 6; $p++) {
    $reportContent += "`n### Phase $p: $($phaseSummaries[$p].name)`n`n"

    foreach ($task in ($taskStatuses.GetEnumerator() | Where-Object { $_.Value.phase -eq $p } | Sort-Object { $_.Key })) {
        $icon = if ($task.Value.status -eq "completed") { "[x]" } else { "[ ]" }
        $reportContent += "- $icon $($task.Key): $($task.Value.name)`n"
    }
}

$reportContent += @"

## Next Steps

"@

# Find next pending task
$nextTask = $taskStatuses.GetEnumerator() |
    Where-Object { $_.Value.status -eq "pending" } |
    Sort-Object { $_.Key } |
    Select-Object -First 1

if ($nextTask) {
    $reportContent += "**Next task:** $($nextTask.Key) - $($nextTask.Value.name)`n`n"
    $reportContent += "Run the following prompt file:`n"
    $reportContent += "``prompts/phase_$($nextTask.Value.phase)/$($nextTask.Key)_*.md```n"
} else {
    $reportContent += "**All tasks completed!**`n"
}

$reportContent += @"

---
*Generated by generate_progress_report.ps1*
"@

# Save report
$reportPath = Join-Path $ReportsDir "daily_summary.md"
Set-Content -Path $reportPath -Value $reportContent

# Display summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TzarBot Progress Report" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Overall Progress: $totalCompleted / $totalTasks ($overallPercentage%)" -ForegroundColor $(if ($overallPercentage -eq 100) { "Green" } else { "Yellow" })
Write-Host ""

for ($p = 1; $p -le 6; $p++) {
    $s = $phaseSummaries[$p]
    $color = if ($s.completed -eq $s.total) { "Green" }
             elseif ($s.completed -gt 0) { "Yellow" }
             else { "Gray" }
    Write-Host "Phase $p : $($s.name)" -ForegroundColor $color -NoNewline
    Write-Host " - $($s.completed)/$($s.total)" -ForegroundColor $color
}

Write-Host ""
if ($nextTask) {
    Write-Host "Next: $($nextTask.Key) - $($nextTask.Value.name)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Reports saved to:" -ForegroundColor Gray
Write-Host "  - $reportPath" -ForegroundColor Gray
Write-Host "  - $ProgressFile" -ForegroundColor Gray
