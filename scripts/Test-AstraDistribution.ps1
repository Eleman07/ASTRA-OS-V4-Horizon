#requires -Version 5.1
param([Parameter(Mandatory=$true)][string]$Path)
$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $Path).Path
$required = @(
    'Astra.Desktop.exe', 'Astra.Desktop.dll', 'Astra.Core.dll',
    'Astra.AgentWorker.exe', 'Astra.AgentWorker.dll', 'Astra.AgentWorker.runtimeconfig.json', 'BrainGraph\index.html', 'BrainGraph\graph.js',
    'Astra.AgentSupervisor.exe', 'Astra.AgentSupervisor.dll',
    'Astra.Desktop.deps.json', 'Astra.Desktop.runtimeconfig.json',
    'Astra.Desktop.pri', 'App.xbf', 'MainWindow.xbf',
    'Microsoft.UI.Xaml.dll', 'Microsoft.UI.Xaml.Controls.pri', 'WebView2Loader.dll',
    'Assets\wallpaper_v4.png', 'Assets\banner_art_v4.png',
    'profiles\core.json', 'profiles\stem.json', 'profiles\gaming.json', 'profiles\creator.json',
    'Graphics3D\web\index.html', 'Graphics3D\web\scene.js',
    'Graphics3D\web\renderer.js', 'Graphics3D\web\stem.html', 'Graphics3D\web\stem.js',
    'python-sdk\astra_scene.py', 'python-sdk\examples.py', 'python-sdk\README.md', 'python-sdk\examples-data\mathematics.json',
    'Assets\astra.ico', 'Assets\astra-logo.png', 'Assets\astra-logo.svg',
    'Localization\en.json', 'Localization\it.json', 'Localization\sources.json', 'Localization\xaml.json',
    'Graphics3D\web\model-viewer.js', 'Graphics3D\web\l10n.js', 'Graphics3D\web\models\ariane64.glb',
    'Graphics3D\web\vendor\three\build\three.module.js',
    'LLamaSharp.dll', 'Ai\embedded-runtime.json', 'Ai\components\ggml-tiny.bin', 'Ai\components\manifest.json',
    'Ai\embeddings\nomic-embed-text-v1.5.Q8_0.gguf', 'Ai\gpu\llama-server.exe', 'Ai\components\LICENSE-Whisper-model.txt',
    'builder\New-AstraIso.ps1', 'recipes\org.astra.textkit\recipe.json'
)
foreach ($relative in $required) {
    $file = Join-Path $directory $relative
    if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -eq 0) {
        throw "Distribuzione incompleta: $relative"
    }
}
if (@(Get-ChildItem -LiteralPath (Join-Path $directory 'recipes') -Filter recipe.json -Recurse).Count -lt 30) { throw 'Meno di 30 plugin nella distribuzione.' }
$runtimeConfiguration=Get-Content -LiteralPath (Join-Path $directory 'Astra.Desktop.runtimeconfig.json') -Raw | ConvertFrom-Json
if (-not $runtimeConfiguration.runtimeOptions.includedFrameworks -or $runtimeConfiguration.runtimeOptions.frameworks) { throw 'Distribuzione non self-contained: pubblica con build completa e --self-contained true, senza --no-build dopo una build framework-dependent.' }
foreach($runtimeFile in @('coreclr.dll','hostfxr.dll','hostpolicy.dll')) { if(-not(Test-Path -LiteralPath (Join-Path $directory $runtimeFile))) { throw "Runtime locale incompleto: $runtimeFile" } }
$embeddedManifest=Get-Content -LiteralPath (Join-Path $directory 'Ai\embedded-runtime.json') -Raw | ConvertFrom-Json
foreach($entry in $embeddedManifest.files.PSObject.Properties) {
    $component=Join-Path $directory $entry.Name
    if(-not(Test-Path -LiteralPath $component -PathType Leaf) -or (Get-FileHash -LiteralPath $component -Algorithm SHA256).Hash -ne $entry.Value) { throw "Runtime AI integrato incompleto o modificato: $($entry.Name)" }
}
if(Test-Path -LiteralPath (Join-Path $directory 'Ai\components\llama-server.exe')) { throw 'Il vecchio server non appartiene alla distribuzione AI integrata.' }
Write-Output "PASS distribution: $($required.Count) required files, including application PRI and compiled XAML."
