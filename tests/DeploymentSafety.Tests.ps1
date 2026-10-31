#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$fixture = Join-Path $root ('docs\evidence\safety-fixture-' + [guid]::NewGuid().ToString('N'))
$source = Join-Path $fixture 'media'
try {
    foreach ($relative in @('setup.exe','Astra.Desktop.exe','sources\boot.wim','sources\install.wim','boot\etfsboot.com','efi\microsoft\boot\efisys.bin')) {
        $file = Join-Path $source $relative
        New-Item -ItemType Directory -Path (Split-Path $file) -Force | Out-Null
        [IO.File]::WriteAllText($file, 'QA fixture; not Windows installation media')
    }
    $before = (Get-FileHash -LiteralPath (Join-Path $source 'setup.exe')).Hash
    $rejected = $false
    try {
        & (Join-Path $root 'scripts\New-AstraIso.ps1') -SourceMediaDirectory $source -AstraDistributionDirectory $source -OutputIso (Join-Path $fixture 'test.iso')
    } catch { if ($_.Exception.Message -match 'sovrapposti') { $rejected = $true } else { throw } }
    if (-not $rejected) { throw 'FAIL: ISO builder accepted identical source and payload directories.' }
    if (Test-Path -LiteralPath (Join-Path $fixture 'test.iso')) { throw 'FAIL: invalid plan created output.' }
    if ((Get-FileHash -LiteralPath (Join-Path $source 'setup.exe')).Hash -ne $before) { throw 'FAIL: source changed.' }
    Write-Output 'PASS: ISO overlap rejected before ADK/copy; source preserved; no output generated.'
} finally {
    if (Test-Path -LiteralPath $fixture) {
        $resolved = (Resolve-Path -LiteralPath $fixture).Path
        $parent = [IO.Path]::GetFullPath((Join-Path $root 'docs\evidence'))
        if ([IO.Path]::GetDirectoryName($resolved) -ne $parent -or [IO.Path]::GetFileName($resolved) -notmatch '^safety-fixture-[a-f0-9]{32}$') { throw 'Unsafe fixture cleanup refused.' }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
