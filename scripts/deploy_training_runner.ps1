# Deploy TrainingRunner and ONNX models to VM DEV
# Run from HOST machine

param(
    [switch]$BuildFirst = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== Deploy TrainingRunner to VM DEV ===" -ForegroundColor Cyan

# Paths
$ProjectRoot = "C:\Users\maciek\ai_experiments\tzar_bot"
$TrainingRunnerPath = "$ProjectRoot\tools\TrainingRunner"
$OnnxModelsPath = "$ProjectRoot\training\generation_0\onnx"
$VMName = "DEV"
$VMDestination = "C:\TzarBot"

# Build if requested
if ($BuildFirst) {
    Write-Host "Building TrainingRunner..." -ForegroundColor Yellow
    dotnet build "$TrainingRunnerPath\TrainingRunner.csproj" -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        exit 1
    }
}

# Publish as self-contained
Write-Host "Publishing TrainingRunner..." -ForegroundColor Yellow
dotnet publish "$TrainingRunnerPath\TrainingRunner.csproj" -c Release -o "$TrainingRunnerPath\publish" --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed!"
    exit 1
}

# VM credentials
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

# Create destination directories on VM
Write-Host "Creating directories on VM..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    New-Item -ItemType Directory -Force -Path "C:\TzarBot\TrainingRunner" | Out-Null
    New-Item -ItemType Directory -Force -Path "C:\TzarBot\Models\generation_0" | Out-Null
    New-Item -ItemType Directory -Force -Path "C:\TzarBot\Results" | Out-Null
}

# Copy TrainingRunner files
Write-Host "Copying TrainingRunner to VM..." -ForegroundColor Yellow
$session = New-PSSession -VMName $VMName -Credential $cred
Copy-Item -ToSession $session -Path "$TrainingRunnerPath\publish\*" -Destination "C:\TzarBot\TrainingRunner\" -Recurse -Force
Remove-PSSession $session

# Copy first 5 ONNX models for testing (full 20 would take too long)
Write-Host "Copying ONNX models to VM (network_00 to network_04)..." -ForegroundColor Yellow
$session = New-PSSession -VMName $VMName -Credential $cred
for ($i = 0; $i -lt 5; $i++) {
    $modelName = "network_{0:D2}.onnx" -f $i
    $modelPath = "$OnnxModelsPath\$modelName"
    if (Test-Path $modelPath) {
        Write-Host "  Copying $modelName..." -ForegroundColor Gray
        Copy-Item -ToSession $session -Path $modelPath -Destination "C:\TzarBot\Models\generation_0\" -Force
    }
}
Remove-PSSession $session

# Verify deployment
Write-Host "Verifying deployment..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    Write-Host "TrainingRunner files:"
    Get-ChildItem "C:\TzarBot\TrainingRunner" | Select-Object Name, Length | Format-Table

    Write-Host "ONNX models:"
    Get-ChildItem "C:\TzarBot\Models\generation_0\*.onnx" | Select-Object Name, @{N='SizeMB';E={[math]::Round($_.Length/1MB, 1)}} | Format-Table
}

Write-Host ""
Write-Host "=== Deployment Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "To run training on VM DEV:" -ForegroundColor Cyan
Write-Host '  TrainingRunner.exe <model_path> <map_path> <duration_seconds> [output_json]' -ForegroundColor White
Write-Host ""
Write-Host "Example:" -ForegroundColor Cyan
Write-Host '  C:\TzarBot\TrainingRunner\TrainingRunner.exe C:\TzarBot\Models\generation_0\network_00.onnx C:\TzarBot\Maps\training-0.tzared 300 C:\TzarBot\Results\network_00.json' -ForegroundColor White
