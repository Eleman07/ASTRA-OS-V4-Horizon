#requires -Version 5.1
param([switch]$SkipCoreTests,[switch]$OfflineCache)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$dist=[IO.Path]::GetFullPath((Join-Path $root 'dist'))
$final=Join-Path $dist 'AstraOS-LocalGPU-4.2.0-Final'
$stage=Join-Path $dist ('local-gpu-build-stage\'+[Guid]::NewGuid().ToString('N')+'\payload')
function Assert-DistPath([string]$path) {
    $full=[IO.Path]::GetFullPath($path)
    if(-not $full.StartsWith($dist+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Build path escapes dist.'}
    for($current=$full;$current;$current=Split-Path -Parent $current){
        if(Test-Path -LiteralPath $current){if((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Refusing linked build paths.'}}
        if($current -eq (Split-Path -Parent $current)){break}
    }
}
Assert-DistPath $stage
Assert-DistPath $final
New-Item -ItemType Directory -Path $stage | Out-Null
$restore=@();if($OfflineCache){$restore=@('--no-restore')}
Push-Location $root
try {
    if(-not $SkipCoreTests){& dotnet run --project tests/Astra.Core.SmokeTests/Astra.Core.SmokeTests.csproj -c Release @restore;if($LASTEXITCODE -ne 0){throw 'Core tests failed.'}}
    foreach($project in @('src/Astra.Desktop/Astra.Desktop.csproj','src/Astra.AgentWorker/Astra.AgentWorker.csproj','src/Astra.AgentSupervisor/Astra.AgentSupervisor.csproj')){
        & dotnet publish $project -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:AstraCloudOnly=true -p:AstraLocalGpu=true -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -o $stage @restore
        if($LASTEXITCODE -ne 0){throw "Local GPU publish failed: $project"}
    }
    foreach($doc in Get-ChildItem -LiteralPath (Join-Path $root 'docs\obsidian-cloud') -Recurse -File){
        $destination=Join-Path $stage $doc.FullName.Substring($root.Length+1)
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $doc.FullName -Destination $destination
    }
    Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $stage 'README.md') -Force
    Copy-Item -LiteralPath (Join-Path $root 'docs\LOCAL-GPU-THIRD-PARTY-NOTICES.md') -Destination (Join-Path $stage 'THIRD-PARTY-NOTICES.md')
    foreach($name in @('LOCAL-GPU-README.md','LOCAL-GPU-4.2.0-VERIFICATION.md','LOCAL-GPU-MODELS.md','local-gpu-model-pins.json')){
        Copy-Item -LiteralPath (Join-Path $root ('docs\'+$name)) -Destination (Join-Path $stage ('docs\'+$name))
    }
    & python (Join-Path $root 'scripts\collect-cloud-licenses.py') --write --payload $stage
    if($LASTEXITCODE -ne 0){throw 'Dependency license collection failed.'}
    $llamaLicense=Join-Path $root 'src\Astra.Desktop\Ai\components\LICENSE-llama.cpp.txt'
    if((Get-FileHash -LiteralPath $llamaLicense -Algorithm SHA256).Hash -ne '94f29bbed6a22c35b992c5c6ebf0e7c92f13b836b90f36f461c9cf2f0f1d010d'){throw 'llama.cpp MIT license mismatch.'}
    $llamaLicenseTarget=Join-Path $stage 'licenses\local-gpu\llama.cpp'
    New-Item -ItemType Directory -Path $llamaLicenseTarget -Force | Out-Null
    Copy-Item -LiteralPath $llamaLicense -Destination (Join-Path $llamaLicenseTarget 'LICENSE.txt')
    New-Item -ItemType Directory -Path (Join-Path $stage 'scripts\obsidian') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'scripts\obsidian\install_cloud_workspace.ps1') -Destination (Join-Path $stage 'scripts\obsidian\install_cloud_workspace.ps1')
    foreach($name in @('fetch-local-model.py','local-models.json')){
        Copy-Item -LiteralPath (Join-Path $root ('scripts\'+$name)) -Destination (Join-Path $stage ('scripts\'+$name))
    }
    $prerequisites=Join-Path $dist 'release-prerequisites'
    $prerequisiteManifest=Join-Path $prerequisites 'sources.json'
    if(-not(Test-Path -LiteralPath $prerequisiteManifest)){throw 'Run scripts/fetch-release-prerequisites.ps1 first.'}
    $prerequisiteTarget=Join-Path $stage 'prerequisites';New-Item -ItemType Directory -Path $prerequisiteTarget -Force | Out-Null
    foreach($entry in (Get-Content -LiteralPath $prerequisiteManifest -Raw | ConvertFrom-Json).installers){
        if($entry.file -ne [IO.Path]::GetFileName($entry.file)){throw 'Unsafe prerequisite filename.'}
        $file=Join-Path $prerequisites $entry.file
        if((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.sha256){throw "Prerequisite hash mismatch: $($entry.file)"}
        $signature=Get-AuthenticodeSignature -LiteralPath $file
        if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation'){throw "Invalid Microsoft signature: $($entry.file)"}
        Copy-Item -LiteralPath $file -Destination (Join-Path $prerequisiteTarget $entry.file)
    }
    Copy-Item -LiteralPath $prerequisiteManifest -Destination (Join-Path $prerequisiteTarget 'sources.json')
    Copy-Item -LiteralPath (Join-Path $root 'docs\PREREQUISITES-README.md') -Destination (Join-Path $prerequisiteTarget 'README.md')
    & python (Join-Path $root 'scripts\local_gpu_payload.py') --payload $stage --write-manifest
    if($LASTEXITCODE -ne 0){throw 'Local GPU manifest/privacy/model-source audit failed.'}
    # Preserve the previous build rather than recursively deleting any directory.
    Assert-DistPath $stage
    Assert-DistPath $final
    if(Test-Path -LiteralPath $final){
        $backup=Join-Path $dist ('AstraOS-LocalGPU-4.2.0-Final-previous-'+[Guid]::NewGuid().ToString('N'))
        Assert-DistPath $backup
        if(Test-Path -LiteralPath $backup){throw 'Backup destination already exists.'}
        Move-Item -LiteralPath $final -Destination $backup
    }
    Move-Item -LiteralPath $stage -Destination $final
    & python (Join-Path $root 'scripts\local_gpu_payload.py') --payload $final
    if($LASTEXITCODE -ne 0){throw 'Final local GPU distribution audit failed.'}
    Write-Output "Local GPU distribution verified: $final. User-selected text weights are not included."
} finally {Pop-Location}
