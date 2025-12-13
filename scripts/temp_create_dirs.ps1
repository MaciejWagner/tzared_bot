$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    New-Item -ItemType Directory -Force -Path "C:\TzarBot\TrainingRunner" | Out-Null
    New-Item -ItemType Directory -Force -Path "C:\TzarBot\Models\generation_0" | Out-Null
    New-Item -ItemType Directory -Force -Path "C:\TzarBot\Results" | Out-Null
    Write-Output "Directories created on VM DEV"
}
