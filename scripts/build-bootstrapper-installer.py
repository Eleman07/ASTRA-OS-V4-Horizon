"""Compile a small WinForms bootstrapper and append the verified release ZIP payload."""
from __future__ import annotations
import hashlib
import json
from datetime import datetime,timezone
from pathlib import Path
import shutil
import struct
import subprocess

ROOT=Path(__file__).resolve().parents[1]
DIST=ROOT/'dist'
ARCHIVE=DIST/'ASTRA-OS-V4-HORIZON-openrouter-gaming-creator-win-x64.zip'
OUTPUT=DIST/'ASTRA-OS-V4-HORIZON-Setup-4.0.0.5-win-x64.exe'
STAGE=DIST/'bootstrapper-build'
STUB=STAGE/'AstraHorizonSetup-stub.exe'
CSC=Path(r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe')
ICON=ROOT/'src/Astra.Desktop/Assets/astra.ico'
UNINSTALL=ROOT/'installer/iexpress-uninstall.ps1'
SOURCE=ROOT/'installer/Bootstrapper.cs'
MAGIC=b'ASTRASETUPV2'.ljust(16,b'\0')
TRAILER=24
LIMIT=2*1024**3

def sha(path:Path)->str:
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def main():
    verification=json.loads((DIST/(ARCHIVE.stem+'-verification.json')).read_text(encoding='utf-8'))
    if not verification['allPayloadHashesMatch'] or sha(ARCHIVE)!=verification['sha256']:
        raise RuntimeError('The portable ZIP has not passed its payload hash verification.')
    if not CSC.is_file():raise FileNotFoundError('The Windows .NET Framework C# compiler is not available.')
    STAGE.mkdir(parents=True,exist_ok=True)
    references=[
        'System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll',
        'System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','Microsoft.CSharp.dll'
    ]
    command=[str(CSC),'/nologo','/target:winexe','/platform:x64','/optimize+','/warn:4','/out:'+str(STUB),'/win32icon:'+str(ICON),'/resource:'+str(UNINSTALL)+',AstraInstaller.Uninstall.ps1']
    command.extend('/reference:'+name for name in references)
    command.append(str(SOURCE))
    print('Compiling the account-scoped setup UI…',flush=True)
    subprocess.run(command,check=True,cwd=ROOT)
    stub_length=STUB.stat().st_size;payload_length=ARCHIVE.stat().st_size
    if stub_length+payload_length+TRAILER>=LIMIT:raise RuntimeError('The self-extracting setup exceeds GitHub asset limits.')
    temp=OUTPUT.with_name('.'+OUTPUT.name+'.tmp')
    try:
        with STUB.open('rb') as source,temp.open('xb') as output:
            shutil.copyfileobj(source,output,4*1024**2)
        with ARCHIVE.open('rb') as source,temp.open('ab') as output:
            shutil.copyfileobj(source,output,8*1024**2)
            output.write(MAGIC)
            output.write(struct.pack('<q',payload_length))
        with temp.open('rb') as stream:
            stream.seek(stub_length+payload_length)
            if stream.read(16)!=MAGIC or struct.unpack('<q',stream.read(8))[0]!=payload_length:
                raise RuntimeError('Self-extracting package footer verification failed.')
        temp.replace(OUTPUT)
    finally:
        if temp.exists():temp.unlink()
    checksum=sha(OUTPUT)
    OUTPUT.with_suffix('.exe.sha256').write_text(f'{checksum}  {OUTPUT.name}\n',encoding='utf-8')
    result={'generatedUtc':datetime.now(timezone.utc).isoformat(),'installer':OUTPUT.name,'sha256':checksum,'bytes':OUTPUT.stat().st_size,'bootstrapperBytes':stub_length,'embeddedArchive':ARCHIVE.name,'embeddedArchiveSha256':verification['sha256'],'installerFormat':'PE WinForms executable with appended verified ZIP payload','installScope':'current Windows account','installationTested':False,'signed':False}
    (DIST/(OUTPUT.stem+'-build.json')).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result,indent=2),flush=True)

if __name__=='__main__':main()
