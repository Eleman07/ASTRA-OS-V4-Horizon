"""Compile the offline setup from a fully verified portable release archive.

The NSIS compiler is a portable official ZIP pinned to the publisher's checksum.
No application, prerequisite installer or generated setup is executed here.
"""
from pathlib import Path,PurePosixPath
from datetime import datetime,timezone
import argparse
import hashlib
import json
import subprocess
import uuid
import zipfile

ROOT=Path(__file__).resolve().parents[1]
DIST=ROOT/'dist'
DEFAULT_ARCHIVE=DIST/'ASTRA-OS-V4-HORIZON-openrouter-gaming-creator-win-x64.zip'
NSIS_ARCHIVE=ROOT/'downloads/installer-tools/nsis-3.13.zip'
NSIS=ROOT/'downloads/installer-tools/nsis-3.13'
NSIS_SHA='ba63dffc4410ee89193e1cb5a41989991bd77c61068da17e3156d136b7b0b3d8'

def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def quote(value):return '"'+str(value).replace('$','$$').replace('"','$\\"')+'"'

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--archive',type=Path,default=DEFAULT_ARCHIVE)
    parser.add_argument('--version',default='4.0.0.5')
    parser.add_argument('--output',type=Path)
    args=parser.parse_args()
    archive_path=args.archive.resolve()
    artifact_path=archive_path.with_name(archive_path.stem+'-verification.json')
    artifact=json.loads(artifact_path.read_text(encoding='utf8'))
    assert artifact['allPayloadHashesMatch'] and sha(archive_path)==artifact['sha256']
    assert sha(NSIS_ARCHIVE)==NSIS_SHA,'Unverified installer compiler archive'
    # Verify executable/header/plugin bytes against the official archive before
    # compilation, instead of trusting mutable extracted tools.
    with zipfile.ZipFile(NSIS_ARCHIVE) as archive:
        for entry in archive.infolist():
            if entry.is_dir():continue
            relative=PurePosixPath(entry.filename)
            assert not relative.is_absolute() and '..' not in relative.parts
            file=NSIS.parent/str(relative)
            assert file.is_file() and sha(file)==hashlib.sha256(archive.read(entry)).hexdigest(),'Compiler file changed: '+str(relative)
    stage=DIST/'installer-stage'/uuid.uuid4().hex/'payload';stage.mkdir(parents=True)
    expected={}
    with zipfile.ZipFile(archive_path) as archive:
        manifest=archive.read('ASTRA-OS-V4-HORIZON/FILES.sha256').decode('utf8')
        hashes=dict((line.split('  ',1)[1],line.split('  ',1)[0]) for line in manifest.splitlines())
        for entry in archive.infolist():
            relative=PurePosixPath(entry.filename)
            assert relative.parts[0]=='ASTRA-OS-V4-HORIZON' and '..' not in relative.parts and not relative.is_absolute()
            name=PurePosixPath(*relative.parts[1:]).as_posix()
            assert name not in expected and name!='.' and ':' not in name
            destination=stage/name;destination.parent.mkdir(parents=True,exist_ok=True)
            with archive.open(entry) as source,destination.open('wb') as target:
                digest=hashlib.sha256()
                while data:=source.read(1024*1024):target.write(data);digest.update(data)
            actual=digest.hexdigest()
            if name!='FILES.sha256':assert hashes[name]==actual,'Payload hash mismatch: '+name
            expected[name]=actual
    license_file=stage/'licenses/installer/NSIS-COPYING.txt';license_file.parent.mkdir(parents=True)
    license_file.write_bytes((NSIS/'COPYING').read_bytes());expected[license_file.relative_to(stage).as_posix()]=sha(license_file)
    sources=stage/'licenses/installer/SOURCE.txt'
    sources.write_text('NSIS 3.13, official portable distribution.\nhttps://nsis.sourceforge.io/\nhttps://sourceforge.net/projects/nsis/files/NSIS%203/3.13/nsis-3.13.zip/download\nSHA-256: '+NSIS_SHA+'\n',encoding='utf8');expected[sources.relative_to(stage).as_posix()]=sha(sources)
    manifest_file=stage/'INSTALLER-FILES.sha256'
    manifest_file.write_text(''.join(value+'  '+name+'\n' for name,value in sorted(expected.items())),encoding='utf8');expected[manifest_file.name]=sha(manifest_file)
    lines=['!macro ASTRA_INSTALL_PAYLOAD']
    directory=None
    directories=set()
    for name in sorted(expected):
        path=PurePosixPath(name);parent=path.parent.as_posix()
        if parent!=directory:
            directory=parent;target='$INSTDIR'+('\\'+parent.replace('/','\\') if parent!='.' else '')
            lines.append('SetOutPath "'+target+'"')
        stored=(stage/name).stat().st_size>64*1024**2 or path.suffix.lower() in {'.glb','.png','.jpg','.zip'}
        lines+=['SetCompress '+('off' if stored else 'auto'),'ClearErrors','File '+quote('/oname='+path.name)+' '+quote(stage/name),'IfErrors 0 +3','SetErrorLevel 1','Abort "A file could not be installed. Close ASTRA and retry."']
        directories.update(p.as_posix() for p in path.parents if p.as_posix()!='.')
    lines+=['!macroend','!macro ASTRA_UNINSTALL_PAYLOAD']
    for name in sorted(expected):
        target='$INSTDIR\\'+name.replace('/','\\')
        lines+=['${If} ${FileExists} "'+target+'"','ClearErrors','Delete "'+target+'"','IfErrors UninstallFailed','${EndIf}']
    for directory in sorted(directories,key=lambda p:(-p.count('/'),p),reverse=False):lines.append('RMDir "$INSTDIR\\'+directory.replace('/','\\')+'"')
    lines+=['!macroend']
    include=stage.parent/'payload.nsh';include.write_text('\n'.join(lines)+'\n',encoding='utf8')
    total=sum((stage/name).stat().st_size for name in expected)
    output=(args.output or (DIST/f'ASTRA-OS-V4-HORIZON-Setup-{args.version}-win-x64.exe')).resolve()
    command=[str(NSIS/'makensis.exe'),'/NOCONFIG','/V2','/DGENERATED_PAYLOAD='+str(include),'/DOUTPUT_EXE='+str(output),'/DAPP_ICON='+str(stage/'Assets/astra.ico'),'/DAPP_LICENSE='+str(stage/'LICENSE'),'/DINSTALLED_KB='+str((total+1023)//1024),'/DPRODUCT_VERSION='+args.version,str(ROOT/'installer/AstraHorizon.nsi')]
    print('Compiling offline installer:',len(expected),'files,',total,'installed bytes',flush=True)
    subprocess.run(command,check=True,cwd=ROOT)
    assert output.is_file() and output.stat().st_size<2*1024**3
    checksum=sha(output);output.with_suffix('.exe.sha256').write_text(checksum+'  '+output.name+'\n',encoding='utf8')
    evidence={'generated':datetime.now(timezone.utc).isoformat(),'installer':output.name,'sha256':checksum,'bytes':output.stat().st_size,'version':args.version,'compiler':'NSIS 3.13','compilerArchiveSha256':NSIS_SHA,'portableArchiveSha256':artifact['sha256'],'payloadRoot':str(stage),'payloadCount':len(expected),'payloadHashes':expected,'installBytes':total,'installationTested':False,'releaseReady':False,'signed':False}
    (DIST/(output.stem+'-build.json')).write_text(json.dumps(evidence,indent=2)+'\n',encoding='utf8')
    print(json.dumps({k:v for k,v in evidence.items() if k not in ['payloadHashes']},indent=2),flush=True)

if __name__=='__main__':main()
