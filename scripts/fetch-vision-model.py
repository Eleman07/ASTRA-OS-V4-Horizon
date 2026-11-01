"""Fetch pinned Apache-2.0 vision weights; verify official LFS SHA-256 before use."""
from pathlib import Path
import hashlib,json,urllib.request
ROOT=Path(__file__).resolve().parents[1]
REPO='ggml-org/SmolVLM-256M-Instruct-GGUF'
REV='b9e4379657e1450d04d02eec8e345667265b0a00'
FILES={'SmolVLM-256M-Instruct-Q8_0.gguf':'vision-model.gguf','mmproj-SmolVLM-256M-Instruct-f16.gguf':'vision-projector.gguf'}
def digest(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def main():
    target=ROOT/'src/Astra.Desktop/Ai/vision';target.mkdir(parents=True,exist_ok=True)
    with urllib.request.urlopen(f'https://huggingface.co/api/models/{REPO}/tree/{REV}?expand=true') as response:entries=json.load(response)
    manifest={'repository':REPO,'revision':REV,'license':'Apache-2.0','files':{}}
    for remote,local in FILES.items():
        item=next(x for x in entries if x['path']==remote);expected=item['lfs']['oid'];destination=target/local
        if not destination.is_file() or digest(destination)!=expected:
            temporary=destination.with_suffix('.tmp');print('Downloading '+remote,flush=True)
            urllib.request.urlretrieve(f'https://huggingface.co/{REPO}/resolve/{REV}/{remote}',temporary)
            assert digest(temporary)==expected,'Vision checksum mismatch';temporary.replace(destination)
        manifest['files'][local]={'sha256':expected,'bytes':destination.stat().st_size,'source':f'https://huggingface.co/{REPO}/blob/{REV}/{remote}'}
        print(local+': verified '+expected,flush=True)
    (target/'LICENSE.txt').write_bytes(urllib.request.urlopen('https://www.apache.org/licenses/LICENSE-2.0.txt').read())
    (target/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
if __name__=='__main__':main()
