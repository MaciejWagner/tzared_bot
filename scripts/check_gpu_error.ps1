$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

Invoke-Command -VMName DEV -Credential $cred -ScriptBlock {
    Write-Host "=== GPU Device Status ===" -ForegroundColor Cyan

    $gpu = Get-PnpDevice | Where-Object { $_.FriendlyName -like "*NVIDIA*" }

    Write-Host "Name: $($gpu.FriendlyName)"
    Write-Host "Status: $($gpu.Status)"
    Write-Host "Problem: $($gpu.Problem)"
    Write-Host "ConfigManagerErrorCode: $($gpu.ConfigManagerErrorCode)"
    Write-Host "InstanceId: $($gpu.InstanceId)"

    Write-Host "`n=== Device Properties ===" -ForegroundColor Cyan
    Get-PnpDeviceProperty -InstanceId $gpu.InstanceId | Where-Object { $_.KeyName -like "*Error*" -or $_.KeyName -like "*Status*" } | Format-Table KeyName, Data

    Write-Host "`n=== D3D Feature Level Test ===" -ForegroundColor Cyan
    # Quick D3D test
    Add-Type -AssemblyName System.Runtime.WindowsRuntime

    try {
        # Try to enumerate DXGI adapters
        $dxgiOutput = & {
            Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public class DXGITest {
    [DllImport("dxgi.dll", PreserveSig = false)]
    public static extern void CreateDXGIFactory1([In] ref Guid riid, [Out] out IntPtr ppFactory);

    public static string[] GetAdapters() {
        Guid factoryGuid = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
        IntPtr factory;
        try {
            CreateDXGIFactory1(ref factoryGuid, out factory);
            return new string[] { "DXGI Factory created successfully" };
        } catch (Exception ex) {
            return new string[] { "DXGI Error: " + ex.Message };
        }
    }
}
"@
            [DXGITest]::GetAdapters()
        }
        Write-Host $dxgiOutput
    }
    catch {
        Write-Host "DXGI test failed: $_"
    }
}
