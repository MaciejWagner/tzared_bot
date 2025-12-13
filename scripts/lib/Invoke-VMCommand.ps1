# Invoke-VMCommand.ps1
# Uniwersalny wrapper do wykonywania komend na VM bez problemow z credentials
# Uzycie: .\lib\Invoke-VMCommand.ps1 -VMName "DEV" -ScriptBlock { hostname }

param(
    [Parameter(Mandatory=$true)]
    [string]$VMName = "DEV",

    [Parameter(Mandatory=$true)]
    [scriptblock]$ScriptBlock,

    [string]$Username = "test",
    [string]$Password = "password123",

    [array]$ArgumentList = @()
)

# Tworzenie SecureString bez uzycia ConvertTo-SecureString
$securePassword = New-Object System.Security.SecureString
$Password.ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()

$cred = New-Object System.Management.Automation.PSCredential($Username, $securePassword)

# Wykonaj komende na VM
if ($ArgumentList.Count -gt 0) {
    Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock $ScriptBlock -ArgumentList $ArgumentList
} else {
    Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock $ScriptBlock
}
