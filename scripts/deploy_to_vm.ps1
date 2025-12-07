# Deploy updated files to VM DEV
$ErrorActionPreference = "Stop"

$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "Cleaning VM directory..." -ForegroundColor Yellow
Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Remove-Item "C:\TzarBot" -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path "C:\TzarBot" -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\TzarBot\scripts\demo" -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\TzarBot-Demo\Phase0" -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\TzarBot-Demo\Phase1" -Force | Out-Null
}

Write-Host "Copying files to VM..." -ForegroundColor Yellow
$basePath = "C:\Users\maciek\ai_experiments\tzar_bot"

# Copy ZIP files
Copy-VMFile -Name "DEV" -SourcePath "$basePath\src.zip" -DestinationPath "C:\TzarBot\src.zip" -CreateFullPath -FileSource Host -Force
Copy-VMFile -Name "DEV" -SourcePath "$basePath\tests.zip" -DestinationPath "C:\TzarBot\tests.zip" -CreateFullPath -FileSource Host -Force
Copy-VMFile -Name "DEV" -SourcePath "$basePath\scripts.zip" -DestinationPath "C:\TzarBot\scripts.zip" -CreateFullPath -FileSource Host -Force
Copy-VMFile -Name "DEV" -SourcePath "$basePath\TzarBot.sln" -DestinationPath "C:\TzarBot\TzarBot.sln" -CreateFullPath -FileSource Host -Force

Write-Host "Extracting and running demo on VM..." -ForegroundColor Yellow
Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

    # Extract archives
    Expand-Archive -Path "C:\TzarBot\src.zip" -DestinationPath "C:\TzarBot\src" -Force
    Expand-Archive -Path "C:\TzarBot\tests.zip" -DestinationPath "C:\TzarBot\tests" -Force
    Expand-Archive -Path "C:\TzarBot\scripts.zip" -DestinationPath "C:\TzarBot\scripts" -Force

    Remove-Item "C:\TzarBot\*.zip" -Force

    Write-Host "Project structure:" -ForegroundColor Cyan
    Get-ChildItem "C:\TzarBot" -Recurse -Depth 2 | Where-Object { -not $_.PSIsContainer } | Select-Object FullName

    # Run demos
    Set-Location "C:\TzarBot\scripts\demo"
    & ".\Run-AllDemos.ps1" -ProjectPath "C:\TzarBot" -SkipScreenshots
}

Write-Host "Demo complete!" -ForegroundColor Green
