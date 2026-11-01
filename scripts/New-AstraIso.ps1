# ASTRA Image Builder v2.0 - local, explicit, does not distribute Windows binaries.
# PLAN is default. Use -Build to copy supplied Windows media + ASTRA payload into a local ISO.
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$SourceMediaDirectory,
  [Parameter(Mandatory=$true)][string]$AstraDistributionDirectory,
  [Parameter(Mandatory=$true)][string]$OutputIso,
  [switch]$Build
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function FullPath([string]$p) { [IO.Path]::GetFullPath($p) }
function IsChild([string]$child,[string]$parent) {
  $p = (FullPath $parent).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
  return (FullPath $child).StartsWith($p,[StringComparison]::OrdinalIgnoreCase)
}
$source = FullPath $SourceMediaDirectory
$astra = FullPath $AstraDistributionDirectory
$output = FullPath $OutputIso
if (-not [IO.Directory]::Exists($source)) { throw 'Cartella media Windows inesistente.' }
if (-not [IO.Directory]::Exists($astra)) { throw 'Directory ASTRA pubblicata inesistente.' }
# Reject reparse points anywhere in supplied trees: avoid copying outside intended directories.
# Deliberately fail closed instead of following symlinks/junctions from third-party media.
foreach ($root in @($source, $astra)) {
  if (((Get-Item -LiteralPath $root -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "Sorgente con reparse point non ammessa: $root"
  }
  foreach ($entry in (Get-ChildItem -LiteralPath $root -Force -Recurse -ErrorAction Stop)) {
    if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
      throw "Media contiene un reparse point non ammesso: $($entry.FullName)"
    }
  }
}
if (-not [IO.File]::Exists((Join-Path $astra 'Astra.Desktop.exe'))) { throw 'Astra.Desktop.exe non trovato nella directory ASTRA.' }
if (-not [IO.File]::Exists((Join-Path $source 'setup.exe')) -or
    -not [IO.File]::Exists((Join-Path $source 'sources\boot.wim')) -or
    -not ([IO.File]::Exists((Join-Path $source 'sources\install.wim')) -or [IO.File]::Exists((Join-Path $source 'sources\install.esd')))) {
  throw 'Media Windows non riconosciuto: attesi setup.exe e sources\boot.wim/install.wim oppure install.esd.'
}
$biosRelative = 'boot\etfsboot.com'
$uefiRelative = 'efi\microsoft\boot\efisys.bin'
foreach ($r in @($biosRelative,$uefiRelative)) {
  if (-not [IO.File]::Exists((Join-Path $source $r))) { throw "File boot Microsoft mancante: $r" }
}
if ([IO.File]::Exists($output)) { throw 'ISO di output già esistente: nessuna sovrascrittura.' }
foreach ($candidate in @($source, $astra)) {
  $attrs = (Get-Item -LiteralPath $candidate -Force).Attributes
  if (($attrs -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Path reparse/link non supportato: $candidate" }
}
if ([IO.Path]::GetExtension($output) -ne '.iso') { throw 'Output deve essere un file .iso.' }
if ([String]::Equals($source,$astra,[StringComparison]::OrdinalIgnoreCase) -or
    (IsChild $output $source) -or (IsChild $output $astra) -or (IsChild $source $astra) -or (IsChild $astra $source)) {
  throw 'Percorsi sovrapposti non consentiti.'
}
$oscdimgCandidates = @(
  (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe'),
  (Join-Path $env:ProgramFiles 'Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe')
)
$oscdimg = $oscdimgCandidates | Where-Object { $_ -and [IO.File]::Exists($_) } | Select-Object -First 1
if (-not $oscdimg) { throw 'oscdimg.exe non trovato: installa Windows ADK / Deployment Tools.' }
$dism = Join-Path $env:windir 'System32\dism.exe'
if (-not [IO.File]::Exists($dism)) { throw 'DISM non disponibile nel percorso Windows standard.' }
$installImage = Join-Path $source 'sources\install.wim'
if (-not [IO.File]::Exists($installImage)) { $installImage = Join-Path $source 'sources\install.esd' }
foreach ($image in @((Join-Path $source 'sources\boot.wim'), $installImage)) {
  Write-Output "Verifica metadata immagine in sola lettura: $image"
  & $dism '/English' '/Get-WimInfo' "/WimFile:$image"
  if ($LASTEXITCODE -ne 0) { throw "DISM non ha validato l'immagine (exit $LASTEXITCODE). Verifica il media e gli eventuali privilegi richiesti." }
}
Write-Output 'ASTRA Image Builder · verifica completata'
Write-Output "Media Windows locali: $source"
Write-Output "Directory ASTRA: $astra"
Write-Output "Destinazione privata: $output"
Write-Output "OSCDIMG: $oscdimg"
Write-Output 'La ISO NON contiene licenze Windows e NON viene pubblicata da ASTRA.'
Write-Output 'ASTRA andrà avviato manualmente dalla cartella ASTRA-OS dopo installazione Windows.'
if (-not $Build) { Write-Output 'MODALITÀ PIANO: nessun file modificato. Usa -Build dopo verifica.'; exit 0 }
# V3 refuses reparse entries *inside* both trees before any copy: prevent link escape
# from attacker-controlled source directories. Not a substitute for Windows media licensing.
foreach ($root in @($source, $astra)) {
  $linked = Get-ChildItem -LiteralPath $root -Recurse -Force -ErrorAction Stop |
    Where-Object { ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 } |
    Select-Object -First 1
  if ($linked) { throw "Collegamento/symlink non autorizzato nella sorgente: $($linked.FullName)" }
}
$sourceBytes = (Get-ChildItem -LiteralPath $source -File -Recurse -Force -ErrorAction Stop | Measure-Object -Property Length -Sum).Sum
$astraBytes = (Get-ChildItem -LiteralPath $astra -File -Recurse -Force -ErrorAction Stop | Measure-Object -Property Length -Sum).Sum
$destinationDrive = [IO.Path]::GetPathRoot($output)
$temporaryDrive = [IO.Path]::GetPathRoot([IO.Path]::GetTempPath())
$required = [long]($sourceBytes + $astraBytes) + 2GB
if ([IO.DriveInfo]::new($destinationDrive).AvailableFreeSpace -lt $required) { throw 'Spazio insufficiente sul disco di destinazione.' }
if ([IO.DriveInfo]::new($temporaryDrive).AvailableFreeSpace -lt $required) { throw 'Spazio insufficiente sul disco temporaneo.' }
if ([String]::Equals($destinationDrive,$temporaryDrive,[StringComparison]::OrdinalIgnoreCase) -and
    [IO.DriveInfo]::new($destinationDrive).AvailableFreeSpace -lt (2 * $required)) {
  throw 'Sorgente temporanea e output condividono un disco: spazio insufficiente per entrambe le copie.'
}
$workspace = Join-Path ([IO.Path]::GetTempPath()) ('astra-image-' + [Guid]::NewGuid().ToString('N'))
$partial = $output + '.partial'
if ([IO.File]::Exists($partial)) { throw 'ISO parziale già presente: rimuovila manualmente prima di procedere.' }
if ((IsChild $workspace $source) -or (IsChild $workspace $astra)) { throw 'Directory temporanea non sicura.' }
New-Item -ItemType Directory -Path (Split-Path -Path $output -Parent) -Force | Out-Null
New-Item -ItemType Directory -Path $workspace | Out-Null
try {
  Write-Output 'Copia media di installazione locale in directory temporanea...'
  # Original Microsoft file hashes are preserved; no Windows kernel/system files are patched.
  $copy = Start-Process -FilePath 'robocopy.exe' -ArgumentList @('"' + $source + '"','"' + $workspace + '"','/E','/XJ','/R:1','/W:1','/NFL','/NDL') -PassThru -Wait -NoNewWindow
  if ($copy.ExitCode -gt 7) { throw "Copia del media Windows fallita: robocopy exit=$($copy.ExitCode)" }
  $dest = Join-Path $workspace 'ASTRA-OS'
  New-Item -ItemType Directory -Path $dest | Out-Null
  $astraCopy = Start-Process -FilePath 'robocopy.exe' -ArgumentList @('"' + $astra + '"','"' + $dest + '"','/E','/XJ','/R:1','/W:1','/NFL','/NDL') -PassThru -Wait -NoNewWindow
  if ($astraCopy.ExitCode -gt 7) { throw "Copia ASTRA fallita: robocopy exit=$($astraCopy.ExitCode)" }
  @'
ASTRA OS is a third-party open-source desktop application.
Install Windows 11 with a valid Microsoft license first.
Then open ASTRA-OS\Astra.Desktop.exe from this media manually.
ASTRA OS is not affiliated with or endorsed by Microsoft.
Do not redistribute this ISO without applicable rights to its Windows content.
'@ | Set-Content -LiteralPath (Join-Path $workspace 'ASTRA-README.txt') -Encoding UTF8
  $bios = Join-Path $workspace $biosRelative
  $uefi = Join-Path $workspace $uefiRelative
  $bootArgs = '-bootdata:2#p0,e,b' + $bios + '#pEF,e,b' + $uefi
  Write-Output 'Generazione ISO UEFI/BIOS con oscdimg...'
  & $oscdimg -m -o -u2 -udfver102 '-lASTRA_LOCAL' $bootArgs $workspace $partial
  if ($LASTEXITCODE -ne 0 -or -not [IO.File]::Exists($partial)) { throw 'oscdimg fallito o ISO non creata.' }
  [IO.File]::Move($partial,$output)
  Write-Output "ISO creata: $output"
  $hash = Get-FileHash -LiteralPath $output -Algorithm SHA256
  Write-Output "SHA256: $($hash.Hash)"
}
finally {
  if ([IO.File]::Exists($partial)) { Remove-Item -LiteralPath $partial -Force }
  if ([IO.Directory]::Exists($workspace)) {
    $resolvedWorkspace = (Resolve-Path -LiteralPath $workspace).Path
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')
    if ([IO.Path]::GetDirectoryName($resolvedWorkspace) -ne $resolvedTemp -or
        [IO.Path]::GetFileName($resolvedWorkspace) -notmatch '^astra-image-[a-f0-9]{32}$' -or
        ((Get-Item -LiteralPath $resolvedWorkspace).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
      throw "Pulizia rifiutata: directory temporanea inattesa $resolvedWorkspace"
    }
    Remove-Item -LiteralPath $resolvedWorkspace -Force -Recurse
  }
}
