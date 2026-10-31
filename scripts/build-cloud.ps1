#requires -Version 5.1
param([switch]$SkipCoreTests,[switch]$OfflineCache)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$output=Join-Path $root 'dist\AstraOS-Cloud-4.2.0-Final'
if(Test-Path -LiteralPath $output){if((Get-Item -LiteralPath $output).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Refusing linked build output.'}}
$restore=@();if($OfflineCache){$restore=@('--no-restore')}
Push-Location $root
try {
    if(-not $SkipCoreTests){& dotnet run --project tests/Astra.Core.SmokeTests/Astra.Core.SmokeTests.csproj -c Release @restore;if($LASTEXITCODE -ne 0){throw 'Core tests failed.'}}
    foreach($project in @('src/Astra.Desktop/Astra.Desktop.csproj','src/Astra.AgentWorker/Astra.AgentWorker.csproj','src/Astra.AgentSupervisor/Astra.AgentSupervisor.csproj')){
        & dotnet publish $project -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:AstraCloudOnly=true -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -o $output @restore
        if($LASTEXITCODE -ne 0){throw "Cloud publish failed: $project"}
    }
    foreach($file in @('Astra.Desktop.exe','Astra.Desktop.dll','Astra.Core.dll','Astra.Desktop.pri','App.xbf','MainWindow.xbf','Astra.AgentWorker.exe','Astra.AgentSupervisor.exe','BrainGraph\index.html','KnowledgeKit\README.md')){
        if(-not(Test-Path -LiteralPath (Join-Path $output $file) -PathType Leaf)){throw "Missing cloud distribution component: $file"}
    }
    foreach($doc in Get-ChildItem -LiteralPath (Join-Path $root 'docs\obsidian-cloud') -Recurse -File){$destination=Join-Path $output $doc.FullName.Substring($root.Length+1);New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null;Copy-Item -LiteralPath $doc.FullName -Destination $destination -Force}
    New-Item -ItemType Directory -Path (Join-Path $output 'scripts\obsidian') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'docs\CLOUD-4.2.0-VERIFICATION.md') -Destination (Join-Path $output 'docs\CLOUD-4.2.0-VERIFICATION.md') -Force
    Copy-Item -LiteralPath (Join-Path $root 'scripts\obsidian\install_cloud_workspace.ps1') -Destination (Join-Path $output 'scripts\obsidian\install_cloud_workspace.ps1') -Force
    $prerequisites=Join-Path $root 'dist\release-prerequisites'
    $prerequisiteManifest=Join-Path $prerequisites 'sources.json'
    if(-not(Test-Path -LiteralPath $prerequisiteManifest)){throw 'Run scripts/fetch-release-prerequisites.ps1 first; the release README requires the optional signed Microsoft installers.'}
    $prerequisiteTarget=Join-Path $output 'prerequisites';New-Item -ItemType Directory -Path $prerequisiteTarget -Force | Out-Null
    foreach($entry in (Get-Content -LiteralPath $prerequisiteManifest -Raw | ConvertFrom-Json).installers){
        $file=Join-Path $prerequisites $entry.file
        if((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.sha256){throw "Prerequisite hash mismatch: $($entry.file)"}
        $signature=Get-AuthenticodeSignature -LiteralPath $file
        if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation'){throw "Invalid Microsoft prerequisite signature: $($entry.file)"}
        Copy-Item -LiteralPath $file -Destination (Join-Path $prerequisiteTarget $entry.file) -Force
    }
    Copy-Item -LiteralPath $prerequisiteManifest -Destination (Join-Path $prerequisiteTarget 'sources.json') -Force
    Copy-Item -LiteralPath (Join-Path $root 'docs\PREREQUISITES-README.md') -Destination (Join-Path $prerequisiteTarget 'README.md') -Force
    if(Get-ChildItem -LiteralPath $output -Recurse -File | Where-Object { $_.Extension -eq '.gguf' -or $_.Name -match '^ggml-.*\.bin$' }){throw 'Local model weights must not be included in the cloud distribution.'}
    $files=@(Get-ChildItem -LiteralPath $output -Recurse -File | Where-Object Name -ne 'cloud-build-manifest.json' | ForEach-Object {[pscustomobject]@{Path=$_.FullName.Substring($output.Length+1);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
    [pscustomobject]@{Version='4.2.0';Provider='OpenRouter';LocalModelWeights=$false;Files=$files} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output 'cloud-build-manifest.json') -Encoding UTF8
    Write-Output "Cloud distribution verified: $output ($($files.Count) files; no local model weights)."
} finally {Pop-Location}
