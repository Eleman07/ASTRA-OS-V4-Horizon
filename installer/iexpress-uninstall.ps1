$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$installRoot = [System.IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
$confirmation = [System.Windows.Forms.MessageBox]::Show(
    "Remove ASTRA OS Horizon from this Windows account?`r`n`r`nYour notes, PDFs, preferences, and other user data are stored separately and will be kept.",
    'Uninstall ASTRA OS Horizon', [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Question)
if ($confirmation -ne [System.Windows.Forms.DialogResult]::Yes) { exit 0 }

try {
    $manifest = Join-Path $installRoot 'FILES.sha256'
    if (Test-Path -LiteralPath $manifest -PathType Leaf) {
        $prefix = $installRoot + '\'
        foreach ($line in Get-Content -LiteralPath $manifest) {
            if ($line -notmatch '^[0-9a-fA-F]{64}  (.+)$') { continue }
            $relative = $Matches[1]
            if ([System.IO.Path]::IsPathRooted($relative) -or $relative.Split([char[]]@('/','\')) -contains '..') { continue }
            $target = [System.IO.Path]::GetFullPath((Join-Path $installRoot ($relative.Replace('/','\'))))
            if (-not $target.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { continue }
            if (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target -Force }
        }
        Remove-Item -LiteralPath $manifest -Force -ErrorAction SilentlyContinue
    }
    $startFolder = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\ASTRA OS Horizon'
    foreach ($shortcutName in @('ASTRA OS Horizon.lnk','Uninstall ASTRA OS Horizon.lnk')) {
        $shortcutPath = Join-Path $startFolder $shortcutName
        if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath -Force }
    }
    if ((Test-Path -LiteralPath $startFolder -PathType Container) -and -not (Get-ChildItem -LiteralPath $startFolder -Force | Select-Object -First 1)) { Remove-Item -LiteralPath $startFolder -Force }
    $desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'ASTRA OS Horizon.lnk'
    if (Test-Path -LiteralPath $desktopShortcut) { Remove-Item -LiteralPath $desktopShortcut -Force }
    Remove-Item -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ASTRAOSHorizon' -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath (Join-Path $installRoot 'Uninstall-ASTRA.ps1')) { Remove-Item -LiteralPath (Join-Path $installRoot 'Uninstall-ASTRA.ps1') -Force }
    foreach ($directory in Get-ChildItem -LiteralPath $installRoot -Directory -Recurse -ErrorAction SilentlyContinue | Sort-Object { $_.FullName.Length } -Descending) {
        if (-not (Get-ChildItem -LiteralPath $directory.FullName -Force -ErrorAction SilentlyContinue | Select-Object -First 1)) { Remove-Item -LiteralPath $directory.FullName -Force -ErrorAction SilentlyContinue }
    }
    if (-not (Get-ChildItem -LiteralPath $installRoot -Force -ErrorAction SilentlyContinue | Select-Object -First 1)) { Remove-Item -LiteralPath $installRoot -Force -ErrorAction SilentlyContinue }
    [System.Windows.Forms.MessageBox]::Show('ASTRA OS Horizon was removed. Your separate user data was kept.', 'Uninstall ASTRA OS Horizon', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
}
catch {
    [System.Windows.Forms.MessageBox]::Show("Some application files could not be removed.`r`n`r`n$($_.Exception.Message)", 'Uninstall ASTRA OS Horizon', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning) | Out-Null
    exit 1
}
