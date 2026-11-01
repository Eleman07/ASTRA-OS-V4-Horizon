# Read-only smoke diagnostics on actual Windows 11, without elevation.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Continue'
Write-Host 'ASTRA OS V2 RC2 — Windows read-only checks'
Write-Host ('OS: {0}' -f [Environment]::OSVersion.VersionString)
$exe = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path 'dist\AstraOS\Astra.Desktop.exe'
Write-Host ('Desktop output: {0}' -f $(if(Test-Path -LiteralPath $exe){'present'}else{'not compiled'}))
try { $mp = Get-MpComputerStatus -ErrorAction Stop; Write-Host ('Defender realtime: {0}' -f $mp.RealTimeProtectionEnabled) }
catch { Write-Host 'Defender: unavailable (not necessarily disabled)' }
try { Get-NetFirewallProfile -ErrorAction Stop | ForEach-Object { Write-Host ('Firewall {0}: {1}' -f $_.Name,$_.Enabled) } }
catch { Write-Host 'Firewall: unavailable' }
$wsl = Join-Path $env:windir 'System32\wsl.exe'
if (Test-Path -LiteralPath $wsl) {
    Write-Host 'WSL executable: present (not proof that Ubuntu is installed)'
} else { Write-Host 'WSL executable: not found' }
Write-Host 'No settings changed. Manual tests still required.'
