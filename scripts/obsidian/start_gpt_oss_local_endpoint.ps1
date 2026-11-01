param(
    [switch]$DryRun,
    [int]$MinimumFreeVramMiB = 13800,
    [int]$ContextSize = 32768,
    [string]$ServerPath,
    [string]$ModelPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$serverCandidates = @(
    (Join-Path $repoRoot 'dist\AstraOS-Agent-Brain-Gpu-4.1.0-FinalCandidate\Ai\gpu\llama-server.exe'),
    (Join-Path $repoRoot 'dist\AstraOS-Agent-Brain-Gpu-4.1.0\Ai\gpu\llama-server.exe'),
    (Join-Path $repoRoot 'dist\AstraOS-Agent-Brain\Ai\gpu\llama-server.exe')
)
$modelPath = if ([string]::IsNullOrWhiteSpace($ModelPath)) {
    Join-Path $repoRoot 'downloads\models\gpt-oss-20b-MXFP4.gguf'
} else {
    (Resolve-Path -LiteralPath $ModelPath).Path
}

if ([string]::IsNullOrWhiteSpace($ServerPath)) {
    $serverPath = $serverCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
} else {
    $serverPath = (Resolve-Path -LiteralPath $ServerPath).Path
}
$port = 11435

$existingListener = @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
if ($existingListener.Count -gt 0) {
    $loopbackListener = @($existingListener | Where-Object { $_.LocalAddress -in @('127.0.0.1', '::1') })
    if ($loopbackListener.Count -gt 0) {
        $modelIds = @()
        $existingContext = 0
        try {
            $existingModels = Invoke-RestMethod -Uri "http://127.0.0.1:$port/v1/models" -TimeoutSec 3
            $modelIds = @($existingModels.data | ForEach-Object { [string]$_.id })
            $existingProps = Invoke-RestMethod -Uri "http://127.0.0.1:$port/props" -TimeoutSec 3
            $existingContext = [int]$existingProps.default_generation_settings.n_ctx
        } catch {
            # A listener that cannot prove its model identity is treated as occupied.
        }

        if ($modelIds -contains 'gpt-oss:20b') {
            if ($existingContext -lt $ContextSize) {
                throw "GPT-OSS is already running on loopback with a $existingContext-token context; this setup needs at least $ContextSize for the configured agent prompt. Stop that server, then rerun this launcher. No duplicate was started."
            }

            Write-Output "Reusing the already-running loopback endpoint at http://127.0.0.1:$port/v1 (model gpt-oss:20b, context $existingContext). No second model process was started and no additional VRAM was requested."
            if ($DryRun) { Write-Output 'Dry run passed: existing local model is ready.' }
            exit 0
        }
    }

    throw "Port $port is already listening, but it did not identify as the expected loopback GPT-OSS endpoint. No second model server was started."
}

if (-not (Test-Path -LiteralPath $serverPath -PathType Leaf)) {
    throw "Bundled llama-server was not found. Build ASTRA or pass -ServerPath. Tried: $($serverCandidates -join ', ')"
}

if (-not (Test-Path -LiteralPath $modelPath -PathType Leaf)) {
    throw "Local GPT-OSS model was not found: $modelPath. Download it or pass -ModelPath."
}

$nvidiaSmi = Get-Command nvidia-smi -ErrorAction SilentlyContinue
if ($null -eq $nvidiaSmi) {
    throw 'nvidia-smi is not available. Install/update the NVIDIA driver and check GPU memory before starting the model.'
}

$freeMemoryOutput = & $nvidiaSmi.Source --query-gpu=memory.free --format=csv,noheader,nounits 2>$null
if ($LASTEXITCODE -ne 0 -or $null -eq $freeMemoryOutput) {
    throw 'Could not read free NVIDIA GPU memory; no model server was started.'
}

$freeMemoryMiB = [int](($freeMemoryOutput | Select-Object -First 1).Trim())
if ($freeMemoryMiB -lt $MinimumFreeVramMiB) {
    throw "Only $freeMemoryMiB MiB of GPU memory is free; at least $MinimumFreeVramMiB MiB is required to load this 20B model with a $ContextSize-token context. Close GPU workloads, then retry. No process was started."
}

if ($DryRun) {
    Write-Output "Ready: $freeMemoryMiB MiB free; the GPU model server was not started."
    exit 0
}

$logDirectory = Join-Path $repoRoot 'logs\obsidian-gpt-oss'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$stdoutPath = Join-Path $logDirectory 'server.stdout.log'
$stderrPath = Join-Path $logDirectory 'server.stderr.log'

$serverArguments = @(
    '--host', '127.0.0.1',
    '--port', "$port",
    '--n-gpu-layers', '999',
    '--ctx-size', "$ContextSize",
    '--parallel', '1',
    '--alias', 'gpt-oss:20b',
    '-m', $modelPath
)

$serverProcess = Start-Process `
    -FilePath $serverPath `
    -ArgumentList $serverArguments `
    -WindowStyle Hidden `
    -PassThru `
    -RedirectStandardOutput $stdoutPath `
    -RedirectStandardError $stderrPath

Write-Output "Started local GPU endpoint (PID $($serverProcess.Id)); waiting for model readiness..."

$deadline = [DateTime]::UtcNow.AddMinutes(2)
$ready = $false
while ([DateTime]::UtcNow -lt $deadline) {
    $serverProcess.Refresh()
    if ($serverProcess.HasExited) {
        $tail = if (Test-Path -LiteralPath $stderrPath) { (Get-Content -LiteralPath $stderrPath -Tail 20) -join [Environment]::NewLine } else { '' }
        throw "llama-server exited before becoming ready. Check $stderrPath. $tail"
    }

    try {
        $readyModels = Invoke-RestMethod -Uri "http://127.0.0.1:$port/v1/models" -TimeoutSec 3
        $readyProps = Invoke-RestMethod -Uri "http://127.0.0.1:$port/props" -TimeoutSec 3
        $readyIds = @($readyModels.data | ForEach-Object { [string]$_.id })
        $readyContext = [int]$readyProps.default_generation_settings.n_ctx
        if (($readyIds -contains 'gpt-oss:20b') -and ($readyContext -ge $ContextSize)) {
            $ready = $true
            break
        }
    } catch {
        # The server may still be loading its model; retry until the deadline.
    }

    Start-Sleep -Seconds 2
}

if (-not $ready) {
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    throw "llama-server did not expose gpt-oss:20b with a $ContextSize-token context within two minutes. Its logs are in $logDirectory. The process started by this script was stopped."
}

Write-Output "Started local GPU endpoint (PID $($serverProcess.Id), context $readyContext)."
Write-Output "OpenAI-compatible base URL: http://127.0.0.1:$port/v1"
Write-Output "Model ID: gpt-oss:20b"
Write-Output "Logs: $logDirectory"
