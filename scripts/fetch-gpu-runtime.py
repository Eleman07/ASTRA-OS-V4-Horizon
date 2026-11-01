"""Fetch a pinned official llama.cpp Vulkan runtime and verify the release digest."""
from pathlib import Path,PurePosixPath
import hashlib,json,urllib.request,zipfile
ROOT=Path(__file__).resolve().parents[1]
URL='https://github.com/ggml-org/llama.cpp/releases/download/b12020/llama-b12020-bin-win-vulkan-x64.zip'
SHA='aa3452dfa16cdf7d902b44e5445e43e13023c64fb179ca7a13d60611d4371691'
def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def main():
    archive=ROOT/'downloads/llama-b12020-bin-win-vulkan-x64.zip';archive.parent.mkdir(exist_ok=True)
    if not archive.exists() or sha(archive)!=SHA:urllib.request.urlretrieve(URL,archive)
    assert sha(archive)==SHA,'Official GPU archive digest mismatch'
    folder=ROOT/'src/Astra.Desktop/Ai/gpu';folder.mkdir(parents=True,exist_ok=True)
    files={}
    with zipfile.ZipFile(archive) as z:
        for entry in z.infolist():
            name=PurePosixPath(entry.filename)
            if name.is_absolute() or '..' in name.parts:raise RuntimeError('Unsafe runtime path')
            if entry.is_dir() or not(name.suffix.lower()=='.dll' or name.name=='llama-server.exe' or 'LICENSE' in name.name.upper()):continue
            path=folder/name.name;path.write_bytes(z.read(entry));files[name.name]=sha(path)
    assert 'llama-server.exe' in files
    (folder/'manifest.json').write_text(json.dumps({'source':URL,'archiveSha256':SHA,'version':'b12020','license':'MIT','files':files},indent=2)+'\n',encoding='utf-8')
    print('Official local GPU runtime verified:',len(files),'files')
if __name__=='__main__':main()
