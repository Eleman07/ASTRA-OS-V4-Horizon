# Simplified Windows smoke benchmark. Window visible is NOT proof of ready-to-interact.
param(
  [Parameter(Mandatory=$true)][string]$ExePath,
  [ValidateRange(3,30)][int]$Runs=5,
  [ValidateRange(5,120)][int]$IdleSeconds=15,
  [string]$OutputPath = 'astra-benchmark.csv'
)
$ErrorActionPreference = 'Stop'
$fullPath = (Resolve-Path $ExePath).Path
$records = @()
for ($i=1; $i -le $Runs; $i++) {
  $clock = [System.Diagnostics.Stopwatch]::StartNew()
  $p = Start-Process -FilePath $fullPath -PassThru
  try {
    $visible = $false
    while ($clock.Elapsed.TotalSeconds -lt 20) {
      Start-Sleep -Milliseconds 50
      $p.Refresh()
      if ($p.HasExited) { break }
      if ($p.MainWindowHandle -ne [IntPtr]::Zero -and $p.Responding) { $visible=$true; break }
    }
    if (-not $visible) { throw "Window did not become visible/responding for run $i" }
    $openMs = [Math]::Round($clock.Elapsed.TotalMilliseconds)
    Start-Sleep -Seconds $IdleSeconds
    $p.Refresh()
    $beforeCpu = $p.TotalProcessorTime.TotalSeconds
    $beforeWall = [System.Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds 4
    $beforeWall.Stop()
    $p.Refresh()
    $cpuDelta = $p.TotalProcessorTime.TotalSeconds - $beforeCpu
    $cpuPercent = [Math]::Round(100 * $cpuDelta / $beforeWall.Elapsed.TotalSeconds / [Environment]::ProcessorCount, 3)
    $ramMb = [Math]::Round($p.WorkingSet64/1MB,1)
    $records += [pscustomobject]@{Run=$i;WindowVisibleMs=$openMs;AppWorkingSetMB=$ramMb;IdleCpuPercent=$cpuPercent}
  } finally {
    if (-not $p.HasExited) {
      [void]$p.CloseMainWindow()
      if (-not $p.WaitForExit(5000)) { throw 'ASTRA did not close normally; benchmark leaves it open for inspection.' }
    }
    $p.Dispose()
  }
}
$records | Format-Table -AutoSize
$records | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding utf8
Write-Host 'NOTE: WindowVisibleMs is a proxy; interactive startup / frame times require WPA/ETW profiling.'
Write-Host "Output: $OutputPath"
