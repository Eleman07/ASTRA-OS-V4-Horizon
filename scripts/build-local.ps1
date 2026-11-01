#requires -Version 5.1
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipCoreTests,
    [switch]$SkipZip,
    [switch]$RunWindowsTests
)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$desktop = Join-Path $root "src\Astra.Desktop\Astra.Desktop.csproj"
$outDir = Join-Path $root "dist\AstraOS-Local"
$zipFile = Join-Path $root "dist\ASTRA-OS-V4-HORIZON-win-x64-LOCAL.zip"
$distRoot = [IO.Path]::GetFullPath((Join-Path $root 'dist'))
$stage = Join-Path $distRoot ('.astra-publish-' + [guid]::NewGuid().ToString('N'))
$previous = Join-Path $distRoot ('.astra-previous-' + [guid]::NewGuid().ToString('N'))
$promoted = $false
function Assert-KnownOutput([string]$Path) {
    $absolute = [IO.Path]::GetFullPath($Path)
    if ([IO.Path]::GetDirectoryName($absolute) -ne $distRoot -or
        [IO.Path]::GetFileName($absolute) -notmatch '^(AstraOS-Local|\.astra-(publish|previous)-[a-f0-9]{32})$') {
        throw "Percorso output non sicuro: $absolute"
    }
    if ((Test-Path -LiteralPath $absolute) -and ((Get-Item -LiteralPath $absolute).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Output reparse point non consentito: $absolute"
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet non trovato. Installa Visual Studio con WinUI e SDK .NET 10 (x64)."
}
$sdkList = @(dotnet --list-sdks)
if (-not ($sdkList -match '^10\.')) {
    throw "SDK .NET 10 non rilevato. SDK installati: $($sdkList -join ', ')"
}
if (-not (Test-Path $desktop)) { throw "Sorgente desktop mancante: $desktop" }
New-Item -ItemType Directory -Path (Join-Path $root 'dist') -Force | Out-Null

Push-Location $root
try {
    if (-not $SkipCoreTests) {
        Write-Host "[ASTRA] Test Astra.Core (.NET 10)" -ForegroundColor Cyan
        & dotnet run --project 'tests/Astra.Core.SmokeTests/Astra.Core.SmokeTests.csproj' -c $Configuration
        if ($LASTEXITCODE -ne 0) { throw "Core tests falliti: codice $LASTEXITCODE" }
    }
    if ($RunWindowsTests) {
        & dotnet run --project 'tests/Astra.Desktop.DiagnosticsTests/Astra.Desktop.DiagnosticsTests.csproj' -c $Configuration -- (Join-Path $root 'docs\evidence\windows-services.json')
        if ($LASTEXITCODE -ne 0) { throw 'Windows service tests failed.' }
        & (Join-Path $root 'tests\DeploymentSafety.Tests.ps1')
    }
    # Publish into a fresh isolated directory. Keep the previous distribution
    # available if restore, compile or resource validation fails.
    Assert-KnownOutput $stage
    Write-Host "[ASTRA] Publish WinUI 3 unpackaged self-contained - $Configuration" -ForegroundColor Cyan
    & dotnet publish $desktop -c $Configuration -r win-x64 --self-contained true `
        -p:Platform=x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true `
        -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false `
        -o $stage
    if ($LASTEXITCODE -ne 0) { throw "Compilazione ASTRA fallita: codice $LASTEXITCODE" }
    & dotnet publish (Join-Path $root 'src\Astra.AgentWorker\Astra.AgentWorker.csproj') -c $Configuration -r win-x64 --self-contained true -o $stage
    if ($LASTEXITCODE -ne 0) { throw "Compilazione agent worker fallita: codice $LASTEXITCODE" }
    & dotnet publish (Join-Path $root 'src\Astra.AgentSupervisor\Astra.AgentSupervisor.csproj') -c $Configuration -r win-x64 --self-contained true -o $stage
    if ($LASTEXITCODE -ne 0) { throw "Compilazione supervisor fallita: codice $LASTEXITCODE" }
    $exe = Join-Path $stage 'Astra.Desktop.exe'
    if (-not (Test-Path $exe)) { throw "Pubblicazione terminata senza Astra.Desktop.exe" }
    & (Join-Path $PSScriptRoot 'Test-AstraDistribution.ps1') -Path $stage
    Assert-KnownOutput $outDir
    Assert-KnownOutput $previous
    if (Test-Path -LiteralPath $outDir) { Move-Item -LiteralPath $outDir -Destination $previous }
    try {
        Move-Item -LiteralPath $stage -Destination $outDir
        $promoted = $true
    } catch {
        if ((Test-Path -LiteralPath $previous) -and -not (Test-Path -LiteralPath $outDir)) { Move-Item -LiteralPath $previous -Destination $outDir }
        throw
    }
    $exe = Join-Path $outDir 'Astra.Desktop.exe'
    Write-Host "[ASTRA] Eseguibile pronto: $exe" -ForegroundColor Green
    if (-not $SkipZip) {
        if (Test-Path $zipFile) { Remove-Item $zipFile -Force }
        Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zipFile -CompressionLevel Optimal
        Write-Host "[ASTRA] ZIP binario: $zipFile" -ForegroundColor Green
    }
    Write-Host "[ASTRA] Avvio manuale: & '$exe'" -ForegroundColor Green
    Write-Host "[ASTRA] Diagnostica: $env:LOCALAPPDATA\AstraOS\Logs\startup.log"
}
finally {
    if (Test-Path -LiteralPath $stage) { Assert-KnownOutput $stage; Remove-Item -LiteralPath $stage -Recurse -Force }
    if ($promoted -and (Test-Path -LiteralPath $previous)) { Assert-KnownOutput $previous; Remove-Item -LiteralPath $previous -Recurse -Force }
    Pop-Location
}
