<#
.SYNOPSIS
    Runs all tests for the TzarBot project.

.DESCRIPTION
    Builds the solution and runs all test categories.

.PARAMETER Category
    Optional. Filter tests by category (Phase1, Phase2, etc.)

.PARAMETER Coverage
    Generate code coverage report.

.EXAMPLE
    .\run_all_tests.ps1
    Runs all tests.

.EXAMPLE
    .\run_all_tests.ps1 -Category Phase1
    Runs only Phase 1 tests.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Phase1", "Phase2", "Phase3", "Phase4", "Phase5", "Phase6", "Integration", "All")]
    [string]$Category = "All",

    [switch]$Coverage
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TzarBot Test Runner" -ForegroundColor Cyan
Write-Host "  Category: $Category" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Restore and build
Write-Host "`nRestoring packages..." -ForegroundColor Cyan
dotnet restore (Join-Path $ProjectRoot "TzarBot.sln")

Write-Host "`nBuilding solution..." -ForegroundColor Cyan
dotnet build (Join-Path $ProjectRoot "TzarBot.sln") --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Determine filter
$filter = switch ($Category) {
    "Phase1" { "FullyQualifiedName~Phase1" }
    "Phase2" { "FullyQualifiedName~Phase2" }
    "Phase3" { "FullyQualifiedName~Phase3" }
    "Phase4" { "FullyQualifiedName~Phase4" }
    "Phase5" { "FullyQualifiedName~Phase5" }
    "Phase6" { "FullyQualifiedName~Phase6" }
    "Integration" { "FullyQualifiedName~Integration" }
    default { $null }
}

# Run tests
Write-Host "`nRunning tests..." -ForegroundColor Cyan

$testProject = Join-Path $ProjectRoot "tests\TzarBot.Tests\TzarBot.Tests.csproj"

if (-not (Test-Path $testProject)) {
    Write-Host "Test project not found: $testProject" -ForegroundColor Yellow
    Write-Host "No tests to run." -ForegroundColor Yellow
    exit 0
}

$testArgs = @(
    "test"
    $testProject
    "--no-build"
    "--verbosity", "normal"
)

if ($filter) {
    $testArgs += "--filter", $filter
}

if ($Coverage) {
    $testArgs += "--collect", "XPlat Code Coverage"
}

& dotnet @testArgs

$exitCode = $LASTEXITCODE

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
if ($exitCode -eq 0) {
    Write-Host "  All tests passed!" -ForegroundColor Green
} else {
    Write-Host "  Some tests failed." -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Cyan

exit $exitCode
