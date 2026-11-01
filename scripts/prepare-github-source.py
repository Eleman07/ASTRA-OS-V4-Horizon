"""Prepare the complete application source for an initial GitHub import.

No download, Git staging, commit, push, binary execution or deletion. An explicit
--install path may populate a clean README-only clone, preserving its .git data.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import zipfile

ROOT=Path(__file__).resolve().parents[1]
DIST=ROOT/'dist'
EXCLUDED_PARTS={'bin','obj','__pycache__','.git','.vs','node_modules'}
ROOT_FILES={'.gitignore','.gitattributes','AstraOS.sln','LICENSE','README.md',
    'GUIDA-COMPILAZIONE-LOCALE-WINDOWS.md'}
DIRECTORIES={'.github','src','sdk','tests','scripts','tools','installer','design-preview'}
EXCLUDED_SCRIPTS={'build-social-trailer.py','capture-trailer-clip.py','package-dock-update.py'}

def source_files():
    files=[]
    for path in ROOT.rglob('*'):
        relative=path.relative_to(ROOT)
        if not path.is_file() or path.is_symlink():continue
        if any(p.lower() in EXCLUDED_PARTS for p in relative.parts):continue
        allowed=(len(relative.parts)==1 and relative.name in ROOT_FILES) or relative.parts[0] in DIRECTORIES
        if relative.parts[0]=='docs':
            allowed=(len(relative.parts)==2 and path.suffix=='.md' and not path.name.startswith(('TRAILER','SCREENSHOT','NEXT-EXPANSION')))
            allowed|=relative.as_posix()=='docs/evidence/release-evidence.json'
            allowed|=(len(relative.parts)>2 and relative.parts[1] in {'obsidian-cloud','obsidian-local'} and 'astrox' not in relative.parts and path.suffix.lower() in {'.md','.json'})
        if not allowed:continue
        if relative.parts[0]=='scripts' and path.name in EXCLUDED_SCRIPTS:continue
        if 'Ai/components' in relative.as_posix() and path.suffix.lower() in {'.exe','.dll','.gguf','.bin'}:continue
        if path.suffix.lower() in {'.log','.dpapi','.pyc','.pdb','.tmp','.gguf','.bin','.exe','.dll'}:continue
        if path.stat().st_size>=50*1024**2:raise ValueError('Unexpected large source file: '+str(relative))
        files.append(path)
    return sorted(files)

def git(directory,*arguments):
    return subprocess.run(['git','-C',str(directory),*arguments],check=True,capture_output=True,text=True).stdout.strip()

def install(files,destination):
    destination=destination.resolve(strict=True)
    if destination==ROOT or destination.is_relative_to(ROOT) or ROOT.is_relative_to(destination):
        raise ValueError('Destination must be a separate clean source clone.')
    if not (destination/'.git').is_dir() or (destination/'.git').is_symlink():
        raise ValueError('Destination must have its own normal Git directory.')
    if git(destination,'status','--porcelain'):raise ValueError('Destination has local changes; no files copied.')
    tracked=set(git(destination,'ls-files').splitlines())
    if not tracked or not tracked <= {'README.md','LICENSE','.gitignore'}:
        raise ValueError('This installer only accepts an initial README-only clone.')
    # Validate every destination before copying; never follow a link outside it.
    for source in files:
        target=destination/source.relative_to(ROOT)
        if not target.resolve().is_relative_to(destination):raise ValueError('Destination link escapes clone.')
        if target.exists() and target.relative_to(destination).as_posix() not in tracked:
            raise ValueError('An untracked or ignored destination would be overwritten: '+str(target))
    backup=DIST/'github-initial-files-backup';backup.mkdir(parents=True,exist_ok=True)
    for name in tracked:
        before=destination/name
        if before.is_file():
            target=backup/name
            if target.exists() and target.read_bytes()!=before.read_bytes():raise ValueError('Existing backup differs: '+name)
            shutil.copy2(before,target)
    for source in files:
        target=destination/source.relative_to(ROOT)
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
    assert all((destination/p.relative_to(ROOT)).read_bytes()==p.read_bytes() for p in files)
    print('Populated and verified clone:',destination,flush=True)
    print('Branch:',git(destination,'branch','--show-current'),flush=True)
    print('No files staged; no commit or push performed.',flush=True)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install',type=Path,help='Populate a clean README-only Git clone.')
    args=parser.parse_args();files=source_files();DIST.mkdir(exist_ok=True)
    archive=DIST/'ASTRA-OS-V4-HORIZON-complete-source.zip'
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as package:
        for path in files:package.write(path,path.relative_to(ROOT).as_posix())
    with zipfile.ZipFile(archive) as package:
        assert len(package.infolist())==len(files)
        for path in files:assert package.read(path.relative_to(ROOT).as_posix())==path.read_bytes()
    digest=hashlib.sha256(archive.read_bytes()).hexdigest()
    (DIST/(archive.name+'.sha256')).write_text(digest+'  '+archive.name+'\n',encoding='utf8')
    result={'archive':archive.name,'files':len(files),'bytes':archive.stat().st_size,'sha256':digest,
        'largestFileBytes':max(p.stat().st_size for p in files),'aiBinariesIncluded':False,'personalQaProfilesIncluded':False}
    (DIST/'github-initial-source-report.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf8')
    print(json.dumps(result,indent=2),flush=True)
    if args.install:install(files,args.install)

if __name__=='__main__':main()
