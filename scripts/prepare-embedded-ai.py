"""Pin the exact CPU library bytes from repository-signed NuGet packages.

Run after restoring ASTRA. No binary is executed. Does not restore or modify
quarantined files, antivirus settings or exclusions.
"""
from pathlib import Path
import base64
import hashlib
import json
import subprocess

ROOT=Path(__file__).resolve().parents[1]
VERSION='0.30.0'
PINS={
    'llamasharp':'kH7o9VqvQoFvqm167iWITcKEX8QKo6XFyDRlvLxU+xdyBwKYHkAed9btIEkeSN/okF+bJS1yzQmzSz28Xi468w==',
    'llamasharp.backend.cpu':'6j8qUfsVmNwTWBoLUVij//EptyoGZpwwjTSPHBOzehIyinnk6QKtwg5/+2aOWNh5rXkbpPUuuQYchW36UTM5ig=='
}
ARCHIVE_PINS={
    'llamasharp':'xD6xH0eWuwTLwi0tErUNPeAPDqmT4lV084JWp3ayef0r9EiqMmeoSHI7SNTCOvgW2qTsR5z9l+qViCClOXVGHQ==',
    'llamasharp.backend.cpu':'MR7HaL4diwFvKpSSeyzrDa6G90HqwgrsJaE001yno2z9BgMyF1zGzMoAn+2gpo2DIX7TCwNyRFM7o7WgFJDbGQ=='
}

def main():
    assets=json.loads((ROOT/'src/Astra.Desktop/obj/project.assets.json').read_text(encoding='utf-8-sig'))
    package_roots=[Path(p) for p in assets['packageFolders']]
    folders={}
    for name,expected in PINS.items():
        folder=next(p/name/VERSION for p in package_roots if (p/name/VERSION).is_dir())
        archive=folder/f'{name}.{VERSION}.nupkg'
        # NuGet content hashes normalize package-signature bytes; verify both the
        # restored content identity and the repository signature with NuGet itself.
        metadata=json.loads((folder/'.nupkg.metadata').read_text(encoding='utf8'))
        if metadata['contentHash']!=expected:raise RuntimeError('Unexpected NuGet content identity: '+name)
        with archive.open('rb') as stream:actual=base64.b64encode(hashlib.file_digest(stream,'sha512').digest()).decode('ascii')
        if actual!=ARCHIVE_PINS[name]:raise RuntimeError('Unexpected NuGet package bytes: '+name)
        subprocess.run(['dotnet','nuget','verify',str(archive),'--all'],check=True)
        folders[name]=folder
    files={'LLamaSharp.dll':hashlib.file_digest((folders['llamasharp']/'lib/net10.0/LLamaSharp.dll').open('rb'),'sha256').hexdigest()}
    for file in (folders['llamasharp.backend.cpu']/'LLamaSharpRuntimes/win-x64').rglob('*.dll'):
        relative=file.relative_to(folders['llamasharp.backend.cpu']/'LLamaSharpRuntimes').as_posix()
        files['runtimes/'+relative]=hashlib.file_digest(file.open('rb'),'sha256').hexdigest()
    output=ROOT/'src/Astra.Desktop/Ai/embedded-runtime.json'
    output.write_text(json.dumps({'provider':'LLamaSharp CPU in-process','version':VERSION,'source':'https://github.com/SciSharp/LLamaSharp','nugetContentHashes':PINS,'files':files},indent=2)+'\n',encoding='utf8')
    old=ROOT/'src/Astra.Desktop/Ai/components/manifest.json'
    manifest=json.loads(old.read_text(encoding='utf8'))
    manifest['runtimeProvider']='LLamaSharp.Backend.Cpu/'+VERSION
    manifest['runtimeSource']='https://www.nuget.org/packages/LLamaSharp.Backend.Cpu/'+VERSION
    manifest.pop('runtimeTag',None)
    manifest['files']={name:sha for name,sha in manifest['files'].items() if name.endswith(('.gguf','.bin')) or name.startswith('LICENSE')}
    old.write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf8')
    print('Pinned embedded runtime:',len(files),'files; no server executable.')

if __name__=='__main__':main()
