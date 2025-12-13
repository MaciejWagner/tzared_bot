# copy_nav_screenshots.ps1
$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

$session = New-PSSession -VMName "DEV" -Credential $cred

$destDir = "C:\Users\maciek\ai_experiments\tzar_bot\demo_results\nav_test"
New-Item -ItemType Directory -Path $destDir -Force | Out-Null

$files = Invoke-Command -Session $session -ScriptBlock {
    Get-ChildItem "C:\TzarBot\Screenshots\nav_*.png" | Select-Object -ExpandProperty FullName
}

foreach ($file in $files) {
    Copy-Item -FromSession $session -Path $file -Destination $destDir
    Write-Host "Copied: $file"
}

Remove-PSSession $session
Write-Host "All screenshots copied to: $destDir"
