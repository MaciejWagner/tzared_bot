# run_on_vm.ps1
# Wrapper do uruchamiania skryptow na VM DEV z hosta
# Uzycie: .\run_on_vm.ps1 -ScriptName "install_ffmpeg.ps1"
#         .\run_on_vm.ps1 -Command "ffmpeg -version"

param(
    [string]$ScriptName,
    [string]$Command,
    [string]$VMName = "DEV",
    [string]$Username = "test",
    [string]$Password = "password123",
    [string]$VMScriptPath = "C:\TzarBot\Scripts"
)

Write-Host "=== Uruchamianie na VM: $VMName ===" -ForegroundColor Cyan

# Przygotuj credentials
$securePass = ConvertTo-SecureString $Password -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($Username, $securePass)

# Sprawdz czy VM dziala
$vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
if (-not $vm) {
    Write-Host "BLAD: VM '$VMName' nie istnieje" -ForegroundColor Red
    exit 1
}

if ($vm.State -ne "Running") {
    Write-Host "VM nie dziala. Uruchamiam..."
    Start-VM -Name $VMName
    Write-Host "Czekam na uruchomienie VM (60s)..."
    Start-Sleep -Seconds 60
}

# Tryb: Komenda bezposrednia
if ($Command) {
    Write-Host "Komenda: $Command"
    Write-Host ""
    Write-Host "=== OUTPUT ===" -ForegroundColor Yellow
    Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        param($cmd)
        Invoke-Expression $cmd
    } -ArgumentList $Command
    Write-Host "=== KONIEC ===" -ForegroundColor Cyan
    exit 0
}

# Tryb: Skrypt
if ($ScriptName) {
    Write-Host "Skrypt: $ScriptName"
    Write-Host ""

    # Sciezka do skryptu na hoscie
    $hostScriptPath = Join-Path $PSScriptRoot "vm\$ScriptName"

    if (-not (Test-Path $hostScriptPath)) {
        Write-Host "BLAD: Nie znaleziono skryptu: $hostScriptPath" -ForegroundColor Red
        exit 1
    }

    # Utworz katalog na VM jesli nie istnieje
    Write-Host "Tworzenie katalogu na VM: $VMScriptPath"
    Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        param($path)
        if (-not (Test-Path $path)) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
        }
    } -ArgumentList $VMScriptPath

    # Skopiuj skrypt na VM
    Write-Host "Kopiowanie skryptu na VM..."
    Copy-VMFile -Name $VMName -SourcePath $hostScriptPath -DestinationPath "$VMScriptPath\" -FileSource Host -Force -CreateFullPath

    # Uruchom skrypt na VM
    Write-Host ""
    Write-Host "=== OUTPUT ===" -ForegroundColor Yellow
    Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        param($path, $script)
        Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
        Set-Location $path
        & ".\$script"
    } -ArgumentList $VMScriptPath, $ScriptName

    Write-Host ""
    Write-Host "=== KONIEC ===" -ForegroundColor Cyan
    exit 0
}

Write-Host "Uzycie:"
Write-Host "  .\run_on_vm.ps1 -ScriptName 'install_ffmpeg.ps1'"
Write-Host "  .\run_on_vm.ps1 -Command 'ffmpeg -version'"
