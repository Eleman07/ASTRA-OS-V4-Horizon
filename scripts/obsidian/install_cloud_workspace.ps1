[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$VaultPath,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$packageRoot = (Resolve-Path -LiteralPath (Join-Path $repoRoot 'docs\obsidian-cloud\vault\Astra Command Center')).Path.TrimEnd('\')
$vaultRoot = (Resolve-Path -LiteralPath $VaultPath).Path.TrimEnd('\')

if ($vaultRoot -eq [IO.Path]::GetPathRoot($vaultRoot).TrimEnd('\')) {
    throw 'Select a vault directory rather than a filesystem volume root.'
}
if (-not (Test-Path -LiteralPath $vaultRoot -PathType Container)) {
    throw 'VaultPath must name an existing directory.'
}
if (-not (Test-Path -LiteralPath (Join-Path $vaultRoot '.obsidian') -PathType Container)) {
    throw 'VaultPath must be an existing Obsidian vault with an .obsidian directory.'
}

$targetRoot = [IO.Path]::GetFullPath((Join-Path $vaultRoot 'Astra Command Center'))
$vaultPrefix = $vaultRoot + '\'
$targetPrefix = $targetRoot.TrimEnd('\') + '\'
if (-not $targetRoot.StartsWith($vaultPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The install target is outside the selected vault.'
}

function Assert-NoReparseAncestors {
    param([string]$Path, [string]$Boundary)
    $resolvedCandidate = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    $resolvedBoundary = [IO.Path]::GetFullPath($Boundary).TrimEnd('\')
    if ($resolvedCandidate -ne $resolvedBoundary -and -not $resolvedCandidate.StartsWith($resolvedBoundary + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes its checked boundary: $resolvedCandidate"
    }
    $cursor = $resolvedCandidate
    while ($true) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Refusing a reparse point or symbolic link: $cursor"
            }
        }
        if ($cursor -eq $resolvedBoundary) { break }
        $cursor = Split-Path -Path $cursor -Parent
        if ([string]::IsNullOrEmpty($cursor)) { throw 'Could not verify all path ancestors.' }
    }
}

function Assert-InstallDestination {
    param([string]$Destination)
    if (-not $Destination.StartsWith($targetPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Destination escapes Astra Command Center: $Destination"
    }
    Assert-NoReparseAncestors -Path $Destination -Boundary $vaultRoot
    if (Test-Path -LiteralPath $Destination -PathType Container) {
        throw "A directory conflicts with a package file: $Destination"
    }
    $cursor = Split-Path -Path $Destination -Parent
    while ($true) {
        if ((Test-Path -LiteralPath $cursor) -and -not (Test-Path -LiteralPath $cursor -PathType Container)) {
            throw "A file conflicts with a required directory: $cursor"
        }
        if ($cursor -eq $vaultRoot) { break }
        $cursor = Split-Path -Path $cursor -Parent
    }
}

function Assert-RealDirectoryAncestry {
    param([string]$DirectoryPath)
    $directory = [IO.DirectoryInfo]::new([IO.Path]::GetFullPath($DirectoryPath))
    while ($null -ne $directory) {
        if (($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Select a real directory path without linked ancestors: $($directory.FullName)"
        }
        $directory = $directory.Parent
    }
}

Assert-RealDirectoryAncestry -DirectoryPath $vaultRoot
Assert-RealDirectoryAncestry -DirectoryPath $packageRoot
Assert-NoReparseAncestors -Path $packageRoot -Boundary $repoRoot

# Enumerate without following source directory links. Only the generic Markdown
# subtree is eligible; plugin settings, attachments and private vault data are not.
$pending = [Collections.Generic.Stack[string]]::new()
$pending.Push($packageRoot)
$sourceFiles = [Collections.Generic.List[IO.FileInfo]]::new()
while ($pending.Count -gt 0) {
    $current = $pending.Pop()
    foreach ($entry in Get-ChildItem -LiteralPath $current -Force) {
        if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "The package contains a reparse point: $($entry.FullName)"
        }
        if ($entry.PSIsContainer) { $pending.Push($entry.FullName) }
        elseif ($entry.Extension -eq '.md') { $sourceFiles.Add($entry) }
        else { throw "Unexpected non-Markdown package file: $($entry.FullName)" }
    }
}
if ($sourceFiles.Count -eq 0) { throw 'No package notes were found.' }

$plan = @(
    foreach ($file in $sourceFiles | Sort-Object FullName) {
        $relative = $file.FullName.Substring($packageRoot.Length + 1)
        $destination = [IO.Path]::GetFullPath((Join-Path $targetRoot $relative))
        Assert-InstallDestination -Destination $destination
        [pscustomobject]@{ Source = $file.FullName; Destination = $destination; Exists = (Test-Path -LiteralPath $destination -PathType Leaf) }
    }
)

$preserved = @($plan | Where-Object Exists).Count
if ($DryRun) {
    [pscustomobject]@{
        DryRun = $true
        Target = $targetRoot
        WouldCreate = $plan.Count - $preserved
        Preserved = $preserved
        Overwritten = 0
        Home = (Join-Path $targetRoot '00 Home.md')
    }
    return
}

$created = 0
foreach ($item in $plan) {
    Assert-InstallDestination -Destination $item.Destination
    if (Test-Path -LiteralPath $item.Destination -PathType Leaf) { continue }
    $parent = Split-Path -Path $item.Destination -Parent
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    Assert-InstallDestination -Destination $item.Destination
    $tempPath = Join-Path $parent ('.astra-package-' + [Guid]::NewGuid().ToString('N') + '.tmp')
    $stream = $null
    try {
        $stream = [IO.File]::Open($tempPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        $bytes = [IO.File]::ReadAllBytes($item.Source)
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Dispose()
        $stream = $null
        Assert-InstallDestination -Destination $item.Destination
        try {
            # Two-argument File.Move is atomic on the same volume and refuses
            # an existing destination; it cannot replace an edited user note.
            [IO.File]::Move($tempPath, $item.Destination)
            $created++
        }
        catch [IO.IOException] {
            Assert-InstallDestination -Destination $item.Destination
            if (-not (Test-Path -LiteralPath $item.Destination -PathType Leaf)) { throw }
        }
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
        if ([IO.File]::Exists($tempPath)) { [IO.File]::Delete($tempPath) }
    }
}

[pscustomobject]@{
    DryRun = $false
    Target = $targetRoot
    Created = $created
    Preserved = $plan.Count - $created
    Overwritten = 0
    Home = (Join-Path $targetRoot '00 Home.md')
}
