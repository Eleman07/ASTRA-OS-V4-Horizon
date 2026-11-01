param([string]$OutputDirectory = 'dist\AstraOS-Online-4.4.0')
$ErrorActionPreference = 'Stop'
$astraRoot = Split-Path -Parent $PSScriptRoot
$astraOutput = [IO.Path]::GetFullPath((Join-Path $astraRoot $OutputDirectory))
$astraDist = Join-Path $astraRoot 'dist'
if (-not $astraOutput.StartsWith($astraDist + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be inside dist.' }
if (Test-Path -LiteralPath $astraOutput) { throw 'Choose a fresh output directory.' }
New-Item -ItemType Directory -Path $astraOutput | Out-Null
foreach ($project in @('Astra.Desktop','Astra.AgentWorker','Astra.AgentSupervisor')) {
    & dotnet publish (Join-Path $astraRoot "src\$project\$project.csproj") -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:AstraCloudOnly=true -p:Version=4.4.0 -p:DebugType=None -p:DebugSymbols=false -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -o $astraOutput
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $project" }
}
$astraSpeech = Join-Path $astraOutput 'Ai\components'
New-Item -ItemType Directory -Path $astraSpeech -Force | Out-Null
foreach ($name in @('ggml-small.bin','ggml-tiny.bin','LICENSE-Whisper-model.txt','LICENSE-whisper.cpp.txt','LICENSE-whisper.net.txt','LICENSE-NAudio.txt','LICENSE-PdfPig.txt','LICENSE-llama.cpp.txt')) {
    Copy-Item -LiteralPath (Join-Path $astraRoot ('src\Astra.Desktop\Ai\components\'+$name)) -Destination (Join-Path $astraSpeech $name)
}
Copy-Item -LiteralPath (Join-Path $astraRoot 'src\Astra.Desktop\Ai\components\whisper-manifest.json') -Destination (Join-Path $astraSpeech 'manifest.json')
if ((Get-FileHash -LiteralPath (Join-Path $astraSpeech 'ggml-small.bin')).Hash -ne '1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b') { throw 'Whisper integrity mismatch.' }
foreach ($name in @('README.md','LICENSE')) { Copy-Item -LiteralPath (Join-Path $astraRoot $name) -Destination (Join-Path $astraOutput $name) }
New-Item -ItemType Directory -Path (Join-Path $astraOutput 'docs') -Force | Out-Null
foreach ($name in @('UNIFIED-4.4.0-VERIFICATION.md','CODEX-4.3.0-VERIFICATION.md','AGENT-BRAIN-GUIDE.md','ONLINE-PROVIDERS.md','SECOND-BRAIN-GUIDE.md')) { Copy-Item -LiteralPath (Join-Path $astraRoot ('docs\'+$name)) -Destination (Join-Path $astraOutput ('docs\'+$name)) }
# KnowledgeKit is already published by the desktop project. Include the docs
# location too, so README links also work in a portable installation.
Copy-Item -LiteralPath (Join-Path $astraRoot 'docs\obsidian-cloud') -Destination (Join-Path $astraOutput 'docs') -Recurse
# The desktop project already publishes python-sdk, excluding Python caches.
# Downloaded Microsoft installers and their provenance are prepared separately.
$astraPrerequisites = Join-Path $astraRoot 'dist\release-prerequisites'
if (-not (Test-Path -LiteralPath $astraPrerequisites)) { throw 'Run scripts/fetch-release-prerequisites.ps1 first.' }
Copy-Item -LiteralPath $astraPrerequisites -Destination (Join-Path $astraOutput 'prerequisites') -Recurse
Copy-Item -LiteralPath (Join-Path $astraRoot 'docs\PREREQUISITES-README.md') -Destination (Join-Path $astraOutput 'prerequisites\README.md') -Force
Copy-Item -LiteralPath (Join-Path $astraRoot 'docs\CODEX-THIRD-PARTY-NOTICES.md') -Destination (Join-Path $astraOutput 'THIRD-PARTY-NOTICES.md')
& python (Join-Path $PSScriptRoot 'collect-release-licenses.py') --output $astraOutput
if ($LASTEXITCODE -ne 0) { throw 'License collection failed.' }
if (Get-ChildItem -LiteralPath $astraOutput -Filter '*.gguf' -Recurse) { throw 'Unexpected local language-model weights.' }
$astraFiles = @(Get-ChildItem -LiteralPath $astraOutput -File -Recurse | ForEach-Object { [pscustomobject]@{Path=[IO.Path]::GetRelativePath($astraOutput,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName).Hash} })
[pscustomobject]@{Version='4.4.0';Provider='Codex ChatGPT plan';Alternative='OpenRouter zero-price routes, explicitly selected';LocalTextOrVisionWeights=$false;Speech='OpenAI Whisper small/tiny CPU';Files=$astraFiles} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $astraOutput 'cloud-build-manifest.json') -Encoding utf8
Write-Output "Release payload ready: $astraOutput"
