"""Build an account-scoped self-extracting Windows setup with the inbox IExpress tool."""
from __future__ import annotations
from datetime import datetime,timezone
import hashlib
import json
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[1]
DIST=ROOT/'dist'
ARCHIVE=DIST/'ASTRA-OS-V4-HORIZON-openrouter-gaming-creator-win-x64.zip'
OUTPUT=DIST/'ASTRA-OS-V4-HORIZON-Setup-4.0.0.5-win-x64.exe'
STAGE=DIST/'iexpress-installer-stage'
SED=STAGE/'ASTRA-OS-Horizon.sed'
IEXPRESS=Path(r'C:\Windows\System32\iexpress.exe')
LIMIT=2*1024**3

def sha(path:Path)->str:
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def main():
    verification=json.loads((DIST/(ARCHIVE.stem+'-verification.json')).read_text(encoding='utf-8'))
    if not verification['allPayloadHashesMatch'] or sha(ARCHIVE)!=verification['sha256']:
        raise RuntimeError('The portable release ZIP did not pass its file-integrity verification.')
    if not IEXPRESS.is_file():raise FileNotFoundError('Windows IExpress is not available.')
    STAGE.mkdir(parents=True,exist_ok=True)
    for source in (ARCHIVE,ROOT/'installer/iexpress-install.ps1',ROOT/'installer/iexpress-uninstall.ps1'):
        target=STAGE/source.name
        if source.resolve()!=target.resolve():target.write_bytes(source.read_bytes())
    sed=f'''[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=1
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=ASTRA OS Horizon setup has finished.
TargetName={OUTPUT}
FriendlyName=ASTRA OS V4 Horizon Setup 4.0.0.5
AppLaunched=powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File iexpress-install.ps1
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[SourceFiles]
SourceFiles0={STAGE}
[SourceFiles0]
%FILE0%=
%FILE1%=
%FILE2%=
[Strings]
FILE0="{ARCHIVE.name}"
FILE1="iexpress-install.ps1"
FILE2="iexpress-uninstall.ps1"
'''
    SED.write_text(sed,encoding='ascii')
    print('Building self-extracting setup with the Windows inbox IExpress tool…',flush=True)
    subprocess.run([str(IEXPRESS),'/N','/Q',str(SED)],check=True,cwd=STAGE)
    if not OUTPUT.is_file() or OUTPUT.stat().st_size>=LIMIT:raise RuntimeError('Setup output is missing or exceeds GitHub asset limit.')
    checksum=sha(OUTPUT)
    OUTPUT.with_suffix('.exe.sha256').write_text(f'{checksum}  {OUTPUT.name}\n',encoding='utf-8')
    evidence={'generatedUtc':datetime.now(timezone.utc).isoformat(),'installer':OUTPUT.name,'sha256':checksum,'bytes':OUTPUT.stat().st_size,'portableArchiveSha256':verification['sha256'],'installerBuilder':'Windows inbox IExpress','installScope':'current Windows account','installationTested':False,'signed':False}
    (DIST/(OUTPUT.stem+'-build.json')).write_text(json.dumps(evidence,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(evidence,indent=2),flush=True)

if __name__=='__main__':main()
