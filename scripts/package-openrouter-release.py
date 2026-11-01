"""Package a freshly published ASTRA Windows build and verify its payload."""
from __future__ import annotations
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import uuid
import zipfile
from datetime import datetime, timezone

ROOT=Path(__file__).resolve().parents[1]
DIST=ROOT/'dist'
APP=DIST/'AstraOS-OpenRouter-Gaming-Creator'
PREREQUISITES=DIST/'release-prerequisites'
NAME='ASTRA-OS-V4-HORIZON-openrouter-gaming-creator-win-x64'
TOP='ASTRA-OS-V4-HORIZON'
LIMIT=2*1024**3

def digest(path:Path)->str:
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def main():
    required=['Astra.Desktop.exe','Astra.Desktop.pri','App.xbf','MainWindow.xbf','Localization/en.json','Ai/components/astra-local-model.gguf','Ai/components/manifest.json','Assets/astra.ico']
    for name in required:
        path=APP/name
        if not path.is_file() or path.stat().st_size==0:raise RuntimeError('Missing publish file: '+name)
    provenance=json.loads((PREREQUISITES/'sources.json').read_text(encoding='utf-8-sig'))
    signed={entry['file']:entry for entry in provenance['installers']}
    payload:dict[str,Path|bytes]={}
    for file in sorted(APP.rglob('*')):
        if file.is_symlink():raise RuntimeError('Symlink in publish output: '+str(file))
        if not file.is_file():continue
        name=file.relative_to(APP).as_posix();parts=[p.lower() for p in file.relative_to(APP).parts]
        if any(p in {'ebwebview','webview2','__pycache__','.git','logs'} or p.startswith('qa-') for p in parts):raise RuntimeError('Development/cache data in payload: '+name)
        if file.suffix.lower() in {'.dpapi','.log','.pdb'} or file.name.lower()=='preferences.json':continue
        payload[name]=file
    docs={
        'README.md':ROOT/'README.md',
        'RELEASE-NOTES.md':ROOT/'docs/RELEASE-NOTES-OPENROUTER-2026-10-30.md',
        'OPENROUTER-AI-GUIDE.md':ROOT/'docs/OPENROUTER-AI-GUIDE.md',
        'SECOND-BRAIN-GUIDE.md':ROOT/'docs/SECOND-BRAIN-GUIDE.md',
        'THIRD-PARTY-NOTICES.md':ROOT/'docs/THIRD-PARTY-NOTICES.md',
        'prerequisites/README.md':ROOT/'docs/PREREQUISITES-README.md',
    }
    payload.update(docs)
    for name in ('MicrosoftEdgeWebView2RuntimeInstallerX64.exe','vc_redist.x64.exe'):
        entry=signed[name];path=PREREQUISITES/name
        if entry.get('signatureStatus')!='Valid' or 'O=Microsoft Corporation,' not in entry.get('publisher','') or path.stat().st_size!=entry['bytes'] or digest(path).lower()!=entry['sha256'].lower():
            raise RuntimeError('Prerequisite signature/hash verification failed: '+name)
        payload['prerequisites/'+name]=path
    payload['prerequisites/sources.json']=(PREREQUISITES/'sources.json')
    hashes={}
    for name,source in sorted(payload.items()):
        path=PurePosixPath(name)
        if path.is_absolute() or '..' in path.parts or '\\' in name or ':' in name or '\n' in name:raise RuntimeError('Unsafe archive path: '+name)
        if name.lower() in hashes:raise RuntimeError('Duplicate archive path: '+name)
        hashes[name]=digest(source) if isinstance(source,Path) else hashlib.sha256(source).hexdigest()
    manifest=''.join(f'{value}  {name}\n' for name,value in sorted(hashes.items())).encode('utf-8')
    payload['FILES.sha256']=manifest
    archive=DIST/(NAME+'.zip');tmp=DIST/('.'+NAME+'-'+uuid.uuid4().hex+'.tmp')
    print(f'Creating {archive.name}: {len(payload)} files',flush=True)
    try:
        with zipfile.ZipFile(tmp,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6,allowZip64=True) as z:
            for i,(name,source) in enumerate(sorted(payload.items()),1):
                if isinstance(source,Path):z.write(source,TOP+'/'+name)
                else:z.writestr(TOP+'/'+name,source)
                if i%100==0:print(f'  {i}/{len(payload)}',flush=True)
        if tmp.stat().st_size>=LIMIT:raise RuntimeError('ZIP exceeds GitHub 2 GiB asset limit.')
        with zipfile.ZipFile(tmp) as z:
            expected={TOP+'/'+name for name in payload}
            if len(z.infolist())!=len(expected) or set(z.namelist())!=expected:raise RuntimeError('Archive inventory mismatch.')
            for entry in z.infolist():
                name=entry.filename.removeprefix(TOP+'/')
                with z.open(entry) as stream:actual=hashlib.file_digest(stream,'sha256').hexdigest()
                target=hashes[name] if name in hashes else hashlib.sha256(manifest).hexdigest()
                if actual!=target:raise RuntimeError('Archive payload hash mismatch: '+name)
        os.replace(tmp,archive)
    finally:
        if tmp.exists():tmp.unlink()
    sha=digest(archive)
    archive.with_suffix('.zip.sha256').write_text(f'{sha}  {archive.name}\n',encoding='utf-8')
    (DIST/(NAME+'-FILES.sha256')).write_bytes(manifest)
    result={'generatedUtc':datetime.now(timezone.utc).isoformat(),'archive':archive.name,'sha256':sha,'bytes':archive.stat().st_size,'fileCount':len(payload),'allPayloadHashesMatch':True,'microsoftOfflineInstallersVerified':True,'executableSha256':digest(APP/'Astra.Desktop.exe'),'signed':False}
    (DIST/(NAME+'-verification.json')).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result,indent=2),flush=True)

if __name__=='__main__':main()
