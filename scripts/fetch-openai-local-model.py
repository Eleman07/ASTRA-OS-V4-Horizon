"""Download the exact free precision model used by the acceptance benchmark."""
from pathlib import Path
import hashlib,urllib.request,time,socket,json,threading,concurrent.futures
ROOT=Path(__file__).resolve().parents[1]
URL='https://huggingface.co/ggml-org/gpt-oss-20b-GGUF/resolve/15dcfc7e7c608c6161507211ed43c5b590801b4b/gpt-oss-20b-MXFP4.gguf'
SHA='27cd6c432c7672cb812a92f611cf3ba7bbc35928262bb1e1253ff4ee6ae35901'
def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def main():
    path=ROOT/'downloads/models/gpt-oss-20b-MXFP4.gguf';path.parent.mkdir(parents=True,exist_ok=True)
    if not path.is_file() or sha(path)!=SHA:
        temporary=path.with_suffix('.part');print('Downloading precision model (12.11 GB), no API key required',flush=True)
        socket.setdefaulttimeout(120)
        total=12109566624;block=16*1024*1024;state=temporary.with_suffix('.chunks.json')
        done=set(json.loads(state.read_text()) if state.exists() else [])
        if not temporary.exists() or temporary.stat().st_size!=total:
            with temporary.open('wb') as f:f.truncate(total)
            done=set()
        lock=threading.Lock()
        def fetch(index):
            start=index*block;end=min(total-1,start+block-1)
            for attempt in range(4):
                try:
                    request=urllib.request.Request(URL+f'?download=true&chunk={index}&astra={int(time.time())}',headers={'Range':f'bytes={start}-{end}'})
                    with urllib.request.urlopen(request,timeout=120) as response:
                        assert response.status==206 and response.headers['Content-Range'].startswith(f'bytes {start}-{end}/')
                        data=response.read();assert len(data)==end-start+1
                    with temporary.open('r+b') as f:f.seek(start);f.write(data)
                    with lock:
                        done.add(index);state.write_text(json.dumps(sorted(done)))
                        if len(done)%20==0:print(f'{len(done)}/{(total+block-1)//block} verified-size chunks downloaded',flush=True)
                    return
                except Exception:
                    if attempt==3:raise
                    time.sleep(1+attempt)
        with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:list(pool.map(fetch,[i for i in range((total+block-1)//block) if i not in done]))
        assert sha(temporary)==SHA;temporary.replace(path)
    print('Precision model verified:',path,flush=True)
if __name__=='__main__':main()
