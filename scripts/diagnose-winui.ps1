#requires -Version 5.1
param(
  [ValidateSet("Minimal", "Full", "Probe")]
  [string]$Mode = "Minimal",
  [string]$ExecutablePath,
  [string]$ProbeXaml,
  [string]$DataDirectory,
  [switch]$RunSmoke,
  [ValidateRange(3,60)][int]$ObserveSeconds = 15,
  [switch]$CloseAfterCheck,
  [string]$ReportPath
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$exe = if ($ExecutablePath) { (Resolve-Path -LiteralPath $ExecutablePath).Path } else { Join-Path $root 'dist\AstraOS-Local\Astra.Desktop.exe' }
$log = Join-Path $env:LOCALAPPDATA 'AstraOS\Logs\startup.log'
if (-not (Test-Path -LiteralPath $exe)) { throw "Eseguibile non trovato: $exe. Esegui prima scripts\build-local.ps1." }
$oldMinimal = $env:ASTRA_DIAGNOSTIC_MINIMAL
$oldProbe = $env:ASTRA_DIAGNOSTIC_XAML
$oldData = $env:ASTRA_DATA_DIRECTORY
$oldSmoke = $env:ASTRA_RUN_SMOKE
try {
$env:ASTRA_DIAGNOSTIC_MINIMAL = if ($Mode -eq 'Minimal') { '1' } else { $null }
$env:ASTRA_DIAGNOSTIC_XAML = if ($Mode -eq 'Probe') { (Resolve-Path -LiteralPath $ProbeXaml).Path } else { $null }
if ($DataDirectory) { $env:ASTRA_DATA_DIRECTORY = [IO.Path]::GetFullPath($DataDirectory) }
if ($RunSmoke -and -not $DataDirectory) { throw 'RunSmoke richiede un DataDirectory QA separato.' }
$env:ASTRA_RUN_SMOKE = if ($RunSmoke) { '1' } else { $null }
Write-Host "[ASTRA] Modalita': $Mode" -ForegroundColor Cyan
Write-Host "[ASTRA] Eseguibile: $exe"
$start = Get-Date
$offset = if (Test-Path -LiteralPath $log) { (Get-Item -LiteralPath $log).Length } else { 0 }
$process = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
Start-Sleep -Seconds $ObserveSeconds
$process.Refresh()
$running = -not $process.HasExited
$responsive = $running -and $process.MainWindowHandle -ne 0 -and $process.Responding
if (-not $running) {
    Write-Host "[ASTRA] Processo terminato: $($process.ExitCode)" -ForegroundColor Yellow
} else {
    Write-Host '[ASTRA] Processo ancora in esecuzione; verifico risposta e marcatori della sessione.' -ForegroundColor Cyan
}
$sessionLog = ''
if (Test-Path -LiteralPath $log) {
    Write-Host "`n[ASTRA] Ultime righe startup.log:" -ForegroundColor Cyan
    $stream = [IO.File]::Open($log, 'Open', 'Read', 'ReadWrite')
    try {
        if ($stream.Length -ge $offset) { [void]$stream.Seek($offset, 'Begin') }
        $reader = New-Object IO.StreamReader($stream)
        $sessionLog = $reader.ReadToEnd()
    } finally { $stream.Dispose() }
    Write-Output $sessionLog
}
Write-Host "`n[ASTRA] Nuovi eventi di crash Windows, se presenti:" -ForegroundColor Cyan
$events = @(Get-WinEvent -FilterHashtable @{ LogName='Application'; StartTime=$start } -ErrorAction SilentlyContinue |
  Where-Object { $_.Id -in 1000,1001,1026 -and $_.Message -match 'Astra.Desktop|Microsoft.UI.Xaml' } |
  Select-Object TimeCreated,Id,ProviderName,Message)
$expected = switch ($Mode) {
  'Minimal' { @('MinimalWindow.create.complete','MinimalWindow.activate.complete') }
  'Full' { @('MainWindow.create.complete','MainWindow.activate.complete') }
  'Probe' { @('XamlProbe.complete') }
}
$stagesComplete = @($expected | Where-Object { -not $sessionLog.Contains($_) }).Count -eq 0
$result = [ordered]@{
  mode=$Mode; started=$start.ToString('o'); executable=$exe; pid=$process.Id
  running=$running; responsive=$responsive; stagesComplete=$stagesComplete
  exitCode=$(if ($running) { $null } else { $process.ExitCode })
  startupLog=$sessionLog; crashEvents=$events
  passed=($responsive -and $stagesComplete -and $events.Count -eq 0)
}
if ($RunSmoke) {
  $smokeReport = Join-Path $DataDirectory 'desktop-smoke.json'
  if ((Test-Path -LiteralPath $smokeReport) -and (Get-Item -LiteralPath $smokeReport).LastWriteTime -ge $start) {
    $smoke = Get-Content -LiteralPath $smokeReport -Raw | ConvertFrom-Json
    $result.smokeChecks = @($smoke.checks).Count
    $result.smokePassed = @($smoke.checks | Where-Object status -ne 'PASS').Count -eq 0
    $result.passed = $result.passed -and $result.smokePassed
  } else { $result.smokePassed = $false; $result.passed = $false }
}
if ($CloseAfterCheck -and $running) {
  $result.closeRequested = $process.CloseMainWindow()
  $result.closedNormally = $process.WaitForExit(5000)
  if (-not $result.closedNormally) { $result.passed = $false }
}
$json = $result | ConvertTo-Json -Depth 5
if ($ReportPath) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath), $json) }
Write-Output $json
if (-not $result.passed) { exit 1 }
} finally {
  $env:ASTRA_DIAGNOSTIC_MINIMAL = $oldMinimal
  $env:ASTRA_DIAGNOSTIC_XAML = $oldProbe
  $env:ASTRA_DATA_DIRECTORY = $oldData
  $env:ASTRA_RUN_SMOKE = $oldSmoke
}
