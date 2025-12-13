$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)
Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    hostname
    whoami
    ffmpeg -version 2>&1 | Select-Object -First 2
}
