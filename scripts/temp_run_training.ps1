$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "=== Running Training Test on VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "Starting TrainingRunner..."
    Set-Location "C:\TzarBot\TrainingRunner"

    $output = & .\TrainingRunner.exe "C:\TzarBot\Models\generation_0\network_00.onnx" "C:\TzarBot\Maps\training-0.tzared" 35 "C:\TzarBot\Results\network_00_test.json" 2>&1

    Write-Host $output

    if (Test-Path "C:\TzarBot\Results\network_00_test.json") {
        Write-Host "`n=== Results ===" -ForegroundColor Green
        Get-Content "C:\TzarBot\Results\network_00_test.json"
    }
}
