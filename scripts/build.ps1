param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
Set-Location (Resolve-Path (Join-Path $PSScriptRoot ".."))
Write-Host "[ASTRA] .NET Core smoke tests"
dotnet run --project tests/Astra.Core.SmokeTests/Astra.Core.SmokeTests.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Core tests failed" }
Write-Host "[ASTRA] Building unpackaged WinUI 3 desktop (win-x64)"
dotnet publish src/Astra.Desktop/Astra.Desktop.csproj -c $Configuration -p:Platform=x64 -r win-x64 --self-contained true -o dist/AstraOS
if ($LASTEXITCODE -ne 0) { throw "WinUI publish failed" }
Write-Host "[ASTRA] Publish output: dist/AstraOS"
Write-Host "[ASTRA] Building and hashing public SDK sample plugin"
dotnet publish sdk/Astra.SamplePlugin/Astra.SamplePlugin.csproj -c $Configuration -r win-x64 --self-contained true -o dist/SamplePlugin
if ($LASTEXITCODE -ne 0) { throw "Sample plugin build failed" }
Copy-Item sdk/Astra.SamplePlugin/plugin.json dist/SamplePlugin/plugin.json -Force
& "$PSScriptRoot/Initialize-AstraPlugin.ps1" -PluginDirectory "dist/SamplePlugin"
Write-Host "[ASTRA] Sample plugin available at dist/SamplePlugin (manual opt-in install)"
Write-Host "[ASTRA] Optional: compile installer/AstraOS.iss using Inno Setup 6 ISCC.exe"
