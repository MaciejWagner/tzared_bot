# Check VM DEV status for training
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "=== Checking VM DEV Status ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "`n=== Map Files ===" -ForegroundColor Yellow
    if (Test-Path "C:\TzarBot\Maps") {
        Get-ChildItem "C:\TzarBot\Maps\*.tzared" -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "  $($_.Name) - $([math]::Round($_.Length/1KB, 0)) KB"
        }
    } else {
        Write-Host "  C:\TzarBot\Maps not found!"
    }

    Write-Host "`n=== TrainingRunner ===" -ForegroundColor Yellow
    if (Test-Path "C:\TzarBot\TrainingRunner\TrainingRunner.dll") {
        Write-Host "  TrainingRunner.dll: EXISTS"
        $files = (Get-ChildItem "C:\TzarBot\TrainingRunner" -File).Count
        Write-Host "  Total files: $files"
    } else {
        Write-Host "  TrainingRunner NOT deployed"
    }

    Write-Host "`n=== ONNX Models ===" -ForegroundColor Yellow
    if (Test-Path "C:\TzarBot\Models\generation_0") {
        $models = Get-ChildItem "C:\TzarBot\Models\generation_0\*.onnx" -ErrorAction SilentlyContinue
        Write-Host "  ONNX models: $($models.Count)"
        $models | ForEach-Object {
            Write-Host "    $($_.Name) - $([math]::Round($_.Length/1MB, 1)) MB"
        }
    } else {
        Write-Host "  No ONNX models deployed"
    }

    Write-Host "`n=== Results ===" -ForegroundColor Yellow
    if (Test-Path "C:\TzarBot\Results") {
        $results = Get-ChildItem "C:\TzarBot\Results\*.json" -ErrorAction SilentlyContinue
        Write-Host "  Result files: $($results.Count)"
    } else {
        Write-Host "  Results folder not found"
    }
}
