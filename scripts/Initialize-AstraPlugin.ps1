# Generate SHA-256 pinned v2 manifest AFTER publishing an executable.
# This does not sign the executable or sandbox it; obtain binaries from trusted sources.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$PluginDirectory)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($PluginDirectory)
$manifest = Join-Path $root 'plugin.json'
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw 'plugin.json mancante' }
$data = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
if ($data.apiVersion -ne 2 -or $data.entryPoint -notmatch '^[a-zA-Z0-9._-]{1,80}\.exe$' -or $data.entryPoint.Contains('..')) { throw 'Manifesto non valido' }
$exe = Join-Path $root $data.entryPoint
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Eseguibile mancante' }
$data.sha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
$data | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $manifest -Encoding UTF8
Write-Host "Plugin pinned SHA-256: $($data.sha256)"
