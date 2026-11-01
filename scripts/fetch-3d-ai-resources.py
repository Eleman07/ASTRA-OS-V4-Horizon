"""Fetch pinned upstream assets, retain licenses and record every delivered hash."""
import concurrent.futures,hashlib,json,urllib.request,urllib.parse,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
WEB=ROOT/'src/Astra.Desktop/Graphics3D/web'
HEADERS={'User-Agent':'ASTRA-Horizon-local-build'}
def fetch(url,path,expected=None,limit=1_300_000_000):
    path.parent.mkdir(parents=True,exist_ok=True)
    if path.exists() and (not expected or hashlib.file_digest(path.open('rb'),'sha256').hexdigest()==expected):return path
    temp=path.with_name(path.name+'.partial');h=hashlib.sha256();size=0
    try:
        with urllib.request.urlopen(urllib.request.Request(url,headers=HEADERS),timeout=90) as source,temp.open('wb') as target:
            while chunk:=source.read(1024*1024):
                size+=len(chunk)
                if size>limit:raise ValueError('Resource size limit: '+url)
                target.write(chunk);h.update(chunk)
        if expected and h.hexdigest()!=expected:raise ValueError('SHA256 mismatch: '+url)
        temp.replace(path)
    finally:
        if temp.exists():temp.unlink()
    print('OK',path.relative_to(ROOT),size,flush=True);return path
def raw(repo,ref,path):return 'https://raw.githubusercontent.com/'+repo+'/'+ref+'/'+urllib.parse.quote(path,safe='/')
def revision(repo,branch):
    with urllib.request.urlopen(urllib.request.Request(f'https://api.github.com/repos/{repo}/commits/{branch}',headers=HEADERS),timeout=30) as f:return json.load(f)['sha']
def assets():
    for item in ['build/three.module.js','build/three.core.js','examples/jsm/loaders/GLTFLoader.js','examples/jsm/loaders/DRACOLoader.js','examples/jsm/libs/draco/gltf/draco_wasm_wrapper.js','examples/jsm/libs/draco/gltf/draco_decoder.wasm','examples/jsm/loaders/OBJLoader.js','examples/jsm/loaders/STLLoader.js','examples/jsm/utils/BufferGeometryUtils.js','examples/jsm/controls/OrbitControls.js','LICENSE']:
        fetch(raw('mrdoob/three.js','r180',item),WEB/'vendor/three'/item)
    nasa='11ebb4ee043715aefbba6aeec8a61746fad67fa7';khronos='8f492390ddc7a2ae221a1369ef560589cf8c70fb'
    models=[
        ('cubesat','CubeSat 1 RU','3D Models/CubeSat - 1 RU Generic/CubeSat - 1 RU Generic.glb',['orbit','gnc','lorenz']),
        ('global-hawk','Global Hawk','3D Models/Global Hawk/Global Hawk.glb',['flight','aero']),
        ('dish','Antenna 70 metri','3D Models/70-meter Dish/70 meter dish.glb',['signal']),
        ('habitat','Habitat Demonstration Unit','3D Models/Habitat Demonstration Unit/Habitat Demonstration Unit (part 1).glb',['beam','oscillator'])]
    catalog=[]
    for key,name,path,tools in models:
        url=raw('nasa/NASA-3D-Resources',nasa,path);target=fetch(url,WEB/'models'/f'{key}.glb',limit=40_000_000)
        catalog.append(dict(id=key,name=name,file=target.name,tools=tools,author='NASA / NASA 3D Resources',license='NASA media usage guidelines',source='https://github.com/nasa/NASA-3D-Resources/tree/'+nasa+'/'+urllib.parse.quote(path.rsplit('/',1)[0]),licenseUrl='https://www.nasa.gov/nasa-brand-center/images-and-media/',download=url,sha256=hashlib.file_digest(target.open('rb'),'sha256').hexdigest()))
    for key,name,upstream,tools,author in [
        ('radio','Radio portatile','BoomBox',['rlc','signal'],'Microsoft'),
        ('bottle','Contenitore PBR','WaterBottle',['reactor','heat'],'Microsoft'),
        ('fish','Barramundi','BarramundiFish',['ecology'],'Microsoft'),
        ('avocado','Avocado','Avocado',['heat','reactor'],'Microsoft'),
        ('car','Automobile','ToyCar',['queue','oscillator'],'Guido Odendahl; Eric Chadwick')]:
        path=f'Models/{upstream}/glTF-Binary/{upstream}.glb';url=raw('KhronosGroup/glTF-Sample-Assets',khronos,path);target=fetch(url,WEB/'models'/f'{key}.glb',limit=40_000_000)
        fetch(raw('KhronosGroup/glTF-Sample-Assets',khronos,f'Models/{upstream}/README.md'),WEB/'models/licenses'/f'{key}.md')
        catalog.append(dict(id=key,name=name,file=target.name,tools=tools,author=author,license='CC0-1.0',source=f'https://github.com/KhronosGroup/glTF-Sample-Assets/tree/{khronos}/Models/{upstream}',licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/',download=url,sha256=hashlib.file_digest(target.open('rb'),'sha256').hexdigest()))
    fetch(raw('nasa/NASA-3D-Resources',nasa,'README.md'),WEB/'models/licenses/NASA.md')
    (WEB/'models/catalog.json').write_text(json.dumps(catalog,indent=2,ensure_ascii=False),encoding='utf8')
    credits='# Modelli 3D inclusi\n\nMesh originali, senza modifiche ai file. Normalizzazione, animazione e colorazione sono applicate a runtime da ASTRA. Nessun ente o autore sponsorizza ASTRA. I modelli illustrano i risultati dei solutori a parametri ridotti; la mesh non costituisce un modello CFD/FEM validato.\n\n'
    for m in catalog:credits+=f"- **{m['name']}** â€” {m['author']}; {m['license']}. [Fonte]({m['source']}) Â· [Condizioni]({m['licenseUrl']}). SHA256 `{m['sha256']}`.\n"
    credits+='\nRenderer: Three.js r180, MIT; licenza integrale in vendor/three/LICENSE.\n'
    (WEB/'models/CREDITS.md').write_text(credits,encoding='utf8')
def ai():
    d=json.loads((ROOT/'scripts/resources/ai-cpu-release.json').read_text());a=d['asset']
    destination=ROOT/'src/Astra.Desktop/Ai/components';destination.mkdir(parents=True,exist_ok=True)
    # Speech dependencies only. GPU text models use fetch-local-model.py.
    fetch(raw('ggml-org/llama.cpp',d['tag_name'],'LICENSE'),destination/'LICENSE-llama.cpp.txt')
    fetch('https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin',destination/'ggml-tiny.bin','be07e048e1e599ad46341c8d2a135645097a538221678b7acdd1b1919c6e1b21',80_000_000)
    fetch(raw('ggml-org/whisper.cpp','master','LICENSE'),destination/'LICENSE-whisper.cpp.txt')
    for name,url in {
        'LICENSE-whisper.net.txt':'https://raw.githubusercontent.com/sandrohanea/whisper.net/main/LICENSE',
        'LICENSE-NAudio.txt':'https://raw.githubusercontent.com/naudio/NAudio/master/license.txt',
        'LICENSE-PdfPig.txt':'https://raw.githubusercontent.com/UglyToad/PdfPig/master/LICENSE',
        'LICENSE-Whisper-model.txt':'https://raw.githubusercontent.com/openai/whisper/main/LICENSE'}.items():fetch(url,destination/name)
    manifest=dict(runtimeProvider='LLamaSharp.Backend.Cpu/0.30.0',files={p.name:hashlib.file_digest(p.open('rb'),'sha256').hexdigest() for p in destination.iterdir() if p.is_file() and (p.name=='ggml-tiny.bin' or (p.name.startswith('LICENSE') and 'Qwen' not in p.name))},speechModelSource='https://huggingface.co/ggerganov/whisper.cpp')
    (destination/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
def finalize():
    # ES modules use file-relative imports in the offline WebView host.
    import re,runpy
    for path in (WEB/'vendor/three/examples/jsm').rglob('*.js'):
        if 'libs' in path.parts:continue
        text=path.read_text(encoding='utf8');path.write_text(text.replace("from 'three'","from '../../../build/three.module.js'"),encoding='utf8')
    runpy.run_path(str(ROOT/'scripts/build-ariane6-model.py'),run_name='__main__')
if __name__=='__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        for result in pool.map(lambda fn:fn(),[assets,ai]):pass
    finalize()
