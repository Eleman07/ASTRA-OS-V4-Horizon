$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.IO.Compression.FileSystem

$releaseZipName = 'ASTRA-OS-V4-HORIZON-openrouter-gaming-creator-win-x64.zip'
$releaseZip = Join-Path $PSScriptRoot $releaseZipName
$parent = Join-Path $env:LOCALAPPDATA 'Programs'
[System.IO.Directory]::CreateDirectory($parent) | Out-Null
$folderDialog = New-Object System.Windows.Forms.FolderBrowserDialog
$folderDialog.Description = 'Choose the parent folder for ASTRA OS Horizon. The installer creates an ASTRA OS Horizon subfolder.'
$folderDialog.SelectedPath = $parent
$folderDialog.ShowNewFolderButton = $true
if ($folderDialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { exit 0 }
$installRoot = Join-Path $folderDialog.SelectedPath 'ASTRA OS Horizon'
if (Test-Path -LiteralPath $installRoot) {
    $answer = [System.Windows.Forms.MessageBox]::Show(
        "ASTRA OS Horizon already exists here:`r`n$installRoot`r`n`r`nInstall this version into the same folder? Existing extra files will be kept.",
        'ASTRA OS Horizon Setup', [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Question)
    if ($answer -ne [System.Windows.Forms.DialogResult]::Yes) { exit 0 }
}

$tempRoot = Join-Path $env:TEMP ('ASTRA-Horizon-Setup-' + [guid]::NewGuid().ToString('N'))
try {
    if (-not (Test-Path -LiteralPath $releaseZip -PathType Leaf)) { throw 'The ASTRA package archive is missing from the setup payload.' }
    [System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
    Write-Progress -Activity 'Installing ASTRA OS Horizon' -Status 'Extracting the application files…'
    Expand-Archive -LiteralPath $releaseZip -DestinationPath $tempRoot -Force
    $sourceRoot = Join-Path $tempRoot 'ASTRA-OS-V4-HORIZON'
    if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot 'Astra.Desktop.exe') -PathType Leaf)) { throw 'The extracted package is incomplete.' }
    [System.IO.Directory]::CreateDirectory($installRoot) | Out-Null
    Copy-Item -Path (Join-Path $sourceRoot '*') -Destination $installRoot -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'iexpress-uninstall.ps1') -Destination (Join-Path $installRoot 'Uninstall-ASTRA.ps1') -Force

    $startFolder = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\ASTRA OS Horizon'
    [System.IO.Directory]::CreateDirectory($startFolder) | Out-Null
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut((Join-Path $startFolder 'ASTRA OS Horizon.lnk'))
    $shortcut.TargetPath = Join-Path $installRoot 'Astra.Desktop.exe'
    $shortcut.WorkingDirectory = $installRoot
    $shortcut.IconLocation = (Join-Path $installRoot 'Assets\astra.ico') + ',0'
    $shortcut.Save()
    $uninstallShortcut = $shell.CreateShortcut((Join-Path $startFolder 'Uninstall ASTRA OS Horizon.lnk'))
    $uninstallShortcut.TargetPath = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
    $uninstallShortcut.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installRoot 'Uninstall-ASTRA.ps1') + '"'
    $uninstallShortcut.WorkingDirectory = $installRoot
    $uninstallShortcut.Save()

    $desktop = [System.Windows.Forms.MessageBox]::Show('Create an ASTRA shortcut on the desktop?', 'ASTRA OS Horizon Setup', [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Question)
    if ($desktop -eq [System.Windows.Forms.DialogResult]::Yes) {
        $desktopShortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'ASTRA OS Horizon.lnk'))
        $desktopShortcut.TargetPath = Join-Path $installRoot 'Astra.Desktop.exe'
        $desktopShortcut.WorkingDirectory = $installRoot
        $desktopShortcut.IconLocation = (Join-Path $installRoot 'Assets\astra.ico') + ',0'
        $desktopShortcut.Save()
    }

    $uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ASTRAOSHorizon'
    New-Item -Path $uninstallKey -Force | Out-Null
    Set-ItemProperty -Path $uninstallKey -Name DisplayName -Value 'ASTRA OS V4 Horizon'
    Set-ItemProperty -Path $uninstallKey -Name DisplayVersion -Value '4.0.0.5'
    Set-ItemProperty -Path $uninstallKey -Name Publisher -Value 'Emanuele Magno and ASTRA contributors'
    Set-ItemProperty -Path $uninstallKey -Name InstallLocation -Value $installRoot
    Set-ItemProperty -Path $uninstallKey -Name UninstallString -Value ('"' + "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" + '" -NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installRoot 'Uninstall-ASTRA.ps1') + '"')
    New-ItemProperty -Path $uninstallKey -Name NoModify -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name NoRepair -Value 1 -PropertyType DWord -Force | Out-Null
    $sizeKb = [int][Math]::Min(2147483647, [Math]::Round(((Get-ChildItem -LiteralPath $installRoot -File -Recurse | Measure-Object -Property Length -Sum).Sum / 1KB)))
    New-ItemProperty -Path $uninstallKey -Name EstimatedSize -Value $sizeKb -PropertyType DWord -Force | Out-Null

    $start = [System.Windows.Forms.MessageBox]::Show('ASTRA OS Horizon was installed. Start it now?', 'ASTRA OS Horizon Setup', [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Information)
    if ($start -eq [System.Windows.Forms.DialogResult]::Yes) { Start-Process -FilePath (Join-Path $installRoot 'Astra.Desktop.exe') -WorkingDirectory $installRoot }
}
catch {
    [System.Windows.Forms.MessageBox]::Show("Installation could not be completed.`r`n`r`n$($_.Exception.Message)", 'ASTRA OS Horizon Setup', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}
finally {
    Write-Progress -Activity 'Installing ASTRA OS Horizon' -Completed
    if ($tempRoot -and (Test-Path -LiteralPath $tempRoot)) { Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
