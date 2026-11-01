$ErrorActionPreference = 'Stop'
$astraRoot = Split-Path -Parent $PSScriptRoot
$astraComponents = Join-Path $astraRoot 'src\Astra.Desktop\Ai\components'
$astraManifest = Get-Content -LiteralPath (Join-Path $astraComponents 'whisper-manifest.json') -Raw | ConvertFrom-Json
foreach ($astraName in @('ggml-small.bin','ggml-tiny.bin')) {
    $astraFile = Join-Path $astraComponents $astraName
    $astraExpected = $astraManifest.files.$astraName
    if (-not (Test-Path -LiteralPath $astraFile)) {
        $astraUrl = 'https://huggingface.co/ggerganov/whisper.cpp/resolve/' + $astraManifest.revision + '/' + $astraName
        Invoke-WebRequest -Uri $astraUrl -OutFile ($astraFile + '.partial')
        if ((Get-FileHash -LiteralPath ($astraFile + '.partial')).Hash -ne $astraExpected) { throw "Whisper download mismatch: $astraName" }
        Move-Item -LiteralPath ($astraFile + '.partial') -Destination $astraFile
    }
    if ((Get-FileHash -LiteralPath $astraFile).Hash -ne $astraExpected) { throw "Whisper integrity mismatch: $astraName" }
    Write-Output "Verified CPU speech checkpoint: $astraName"
}
