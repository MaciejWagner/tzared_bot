# Tworzenie SecureString bez modulu Security (unika bledow)
$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()

$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "Laczenie z VM DEV..." -ForegroundColor Cyan

try {
    $result = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
        Write-Host "Hostname: $(hostname)" -ForegroundColor Green
        Write-Host "User: $(whoami)"

        $tzar = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
        if ($tzar) {
            Write-Host "Tzar dziala - PID: $($tzar.Id -join ', ')" -ForegroundColor Green
        } else {
            Write-Host "Tzar nie dziala" -ForegroundColor Yellow
        }

        return "Connected to $env:COMPUTERNAME at $(Get-Date)"
    }
    Write-Host "SUCCESS: $result" -ForegroundColor Green
} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Test zakonczony." -ForegroundColor Cyan
