# VMCredentials.ps1
# Biblioteka do tworzenia credentiali VM bez problemu z modulem Security
# Uzycie: . .\lib\VMCredentials.ps1; $cred = Get-VMCredential

function Get-VMCredential {
    param(
        [string]$Username = "test",
        [string]$Password = "password123"
    )

    # Metoda 1: Bezposrednie tworzenie SecureString przez .NET
    $securePassword = New-Object System.Security.SecureString
    $Password.ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
    $securePassword.MakeReadOnly()

    return New-Object System.Management.Automation.PSCredential($Username, $securePassword)
}

# Eksportuj funkcje
Export-ModuleMember -Function Get-VMCredential -ErrorAction SilentlyContinue
