# copy_playwright_screenshot.ps1
# Kopiuje screenshot z VM DEV

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

$session = New-PSSession -VMName "DEV" -Credential $cred

$destPath = "C:\Users\maciek\ai_experiments\tzar_bot\demo_results\playwright_test.png"
Copy-Item -FromSession $session -Path "C:\TzarBot\Screenshots\playwright_test.png" -Destination $destPath

Remove-PSSession $session

Write-Host "Screenshot copied to: $destPath"
