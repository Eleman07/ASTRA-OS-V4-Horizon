"""Package the dock update, with tested binary hashes and a reviewable source patch.

The native QA report and artifact hash report must describe this publish. No GUI,
git push, installer execution or personal-profile access is performed here.
"""
from pathlib import Path
import argparse
import difflib
import hashlib
import importlib.util
import json
import shutil
import subprocess
import uuid
import zipfile

ROOT=Path(__file__).resolve().parents[1]
DIST=ROOT/'dist'
BASE=ROOT/'docs/evidence/dock-update-baseline'
APP=DIST/'AstraOS-Horizon-Update'
NAME='ASTRA-OS-V4-HORIZON-desktop-second-brain-update-win-x64'
TOP='ASTRA-OS-V4-HORIZON'
EVIDENCE=ROOT/'docs/evidence/dock-update-artifact.json'
RELEASE_NOTES=ROOT/'docs/RELEASE-NOTES-DOCK-UPDATE.md'
EXTRA_DOCS={}
VERIFICATION_NAME='DOCK-VERIFICATION.json'
REQUIRE_MODEL_TEST=False
CHANGED=[
    '.github/workflows/build.yml','README.md','src/Astra.Core/Models.cs',
    'src/Astra.Desktop/WindowWorkspace.cs','src/Astra.Desktop/DockWindowRules.cs',
    'src/Astra.Desktop/RunningAppButton.cs','src/Astra.Desktop/NativeShellIcon.cs','src/Astra.Desktop/ShellIconBitmap.cs',
    'src/Astra.Desktop/DesktopIconService.cs','src/Astra.Desktop/MainWindow.Files.cs',
    'src/Astra.Core/SecondBrain/EncryptedDocumentStore.cs','src/Astra.Core/SecondBrain/DocumentSummarizer.cs','src/Astra.Core/SecondBrain/BrainCommands.cs',
    'src/Astra.Desktop/Ai/BrainPdfReader.cs','src/Astra.Desktop/Ai/BundledAi.cs','src/Astra.Desktop/MainWindow.BrainDocuments.cs',
    'src/Astra.Desktop/MainWindow.Qa.Brain.cs',
    'src/Astra.Desktop/AssistantWindow.cs','src/Astra.Desktop/MainWindow.Assistant.cs','src/Astra.Desktop/MainWindow.Extensions.cs','src/Astra.Desktop/Ai/LocalLlmSession.cs',
    'src/Astra.Desktop/MainWindow.RunningApps.cs','src/Astra.Desktop/MainWindow.Qa.Dock.cs',
    'src/Astra.Desktop/MainWindow.Motion.cs','src/Astra.Desktop/MainWindow.xaml',
    'src/Astra.Desktop/MainWindow.xaml.cs','src/Astra.Desktop/Localization/en.json',
    'src/Astra.Desktop/Localization/sources.json','src/Astra.Desktop/Localization/sources-csharp.json',
    'tests/Astra.Desktop.DiagnosticsTests/Astra.Desktop.DiagnosticsTests.csproj',
    'tests/Astra.Desktop.DiagnosticsTests/Program.cs','tests/Astra.Desktop.DiagnosticsTests/DockFixture.cs',
    'tests/Astra.Desktop.DiagnosticsTests/BrainDocumentTests.cs',
    'docs/DOCK-UPDATE-GITHUB.md','docs/RELEASE-NOTES-DOCK-UPDATE.md','scripts/package-dock-update.py'
    ,'docs/SECOND-BRAIN-GUIDE.md'
]


def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()


def encode(obj):return (json.dumps(obj,indent=2,ensure_ascii=False)+'\n').encode('utf8')

def correction_patch():
    previous=ROOT/'docs/evidence/dock-v1-sources'
    paths=set(CHANGED)|{p.relative_to(previous).as_posix() for p in previous.rglob('*') if p.is_file()}
    parts=[];changes=[];before_files={}
    for relative in sorted(paths):
        old_file=previous/relative
        if not old_file.exists():old_file=BASE/relative
        new_file=ROOT/relative
        old=old_file.read_text(encoding='utf-8-sig') if old_file.is_file() else ''
        new=new_file.read_text(encoding='utf-8-sig') if new_file.is_file() else ''
        if old==new:continue
        header=f'diff --git a/{relative} b/{relative}\n'
        if not old_file.is_file():header+='new file mode 100644\n'
        if not new_file.is_file():header+='deleted file mode 100644\n'
        diff=difflib.unified_diff(old.splitlines(keepends=True),new.splitlines(keepends=True),fromfile='a/'+relative if old_file.is_file() else '/dev/null',tofile='b/'+relative if new_file.is_file() else '/dev/null')
        parts.append(header+''.join(line if line.endswith('\n') else line+'\n\\ No newline at end of file\n' for line in diff));changes.append(relative)
        if old_file.is_file():before_files[relative]=old_file
    patch=DIST/'ASTRA-OS-V4-HORIZON-correction-from-dock-v1.patch';patch.write_text(''.join(parts),encoding='utf8',newline='\n')
    stage=DIST/'dock-patch-check'/uuid.uuid4().hex;stage.mkdir(parents=True)
    for relative,file in before_files.items():
        target=stage/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(file,target)
    for extra in (['--check'],[]):subprocess.run(['git','apply',*extra,str(patch)],cwd=stage,check=True,capture_output=True,text=True)
    assert all((stage/p).read_text(encoding='utf-8-sig')==(ROOT/p).read_text(encoding='utf-8-sig') if (ROOT/p).is_file() else not (stage/p).exists() for p in changes)
    print(f'Incremental correction patch verified: {len(changes)} paths',flush=True)


def source_update():
    pieces=[];changed=[]
    for relative in CHANGED:
        current=ROOT/relative;before=BASE/relative
        new=current.read_text(encoding='utf-8-sig');old=before.read_text(encoding='utf-8-sig') if before.exists() else ''
        if old==new:continue
        changed.append(relative)
        header=f'diff --git a/{relative} b/{relative}\n'
        if not before.exists():header+='new file mode 100644\n'
        diff=difflib.unified_diff(old.splitlines(keepends=True),new.splitlines(keepends=True),
            fromfile='a/'+relative if before.exists() else '/dev/null',tofile='b/'+relative)
        pieces.append(header+''.join(line if line.endswith('\n') else line+'\n\\ No newline at end of file\n' for line in diff))
    patch=DIST/'ASTRA-OS-V4-HORIZON-dock-update.patch';patch.write_text(''.join(pieces),encoding='utf8',newline='\n')
    check=DIST/'dock-patch-check'/uuid.uuid4().hex;check.mkdir(parents=True)
    # Copy only the baseline files touched by this patch. Never delete a checkout.
    for relative in changed:
        if (BASE/relative).is_file():
            target=check/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(BASE/relative,target)
    for extra in (['--check'],[]):
        subprocess.run(['git','apply',*extra,str(patch)],cwd=check,check=True,capture_output=True,text=True)
    assert all((check/p).read_text(encoding='utf-8-sig')==(ROOT/p).read_text(encoding='utf-8-sig') for p in changed)
    source_zip=DIST/'ASTRA-OS-V4-HORIZON-dock-source-update.zip'
    with zipfile.ZipFile(source_zip,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
        for relative in changed:archive.write(ROOT/relative,relative)
        archive.writestr('SOURCE-CHANGES.txt','\n'.join(changed)+'\n')
    print(f'Source patch verified on baseline: {len(changed)} files; {source_zip.name}',flush=True)
    if (ROOT/'docs/evidence/dock-v1-sources').is_dir():correction_patch()
    return changed,patch,source_zip


def binary_update():
    evidence=json.loads(EVIDENCE.read_text(encoding='utf8'))
    if not evidence.get('releaseReady'):
        raise RuntimeError('Binary release is pending verification: '+evidence.get('blocker','final native checks are required'))
    report=json.loads((ROOT/evidence['qaReport']).read_text(encoding='utf8'))
    if not report.get('passed'):raise RuntimeError('The native integration checks have not all passed.')
    if REQUIRE_MODEL_TEST and report.get('modelTested') is not True:raise RuntimeError('Actual local inference must pass before packaging.')
    runtime=json.loads((APP/'Astra.Desktop.runtimeconfig.json').read_text(encoding='utf8'))['runtimeOptions']
    if not runtime.get('includedFrameworks') or runtime.get('frameworks'):raise RuntimeError('The publish must include a self-contained runtime configuration.')
    for relative,expected in evidence['testedFiles'].items():
        if sha(APP/relative)!=expected:raise RuntimeError('Publish changed since native QA: '+relative)
    # Validate every bundled AI component, including the launcher and backend
    # DLLs, so an incomplete incremental publish cannot become a release.
    ai=APP/'Ai/components'
    components=json.loads((ai/'manifest.json').read_text(encoding='utf8'))
    for name,expected in components['files'].items():
        if Path(name).name!=name or not (ai/name).is_file() or sha(ai/name)!=expected.lower():
            raise RuntimeError('Bundled AI component missing or changed: '+name)
    if REQUIRE_MODEL_TEST:
        embedded=json.loads((APP/'Ai/embedded-runtime.json').read_text(encoding='utf8'))
        for name,expected in embedded['files'].items():
            file=APP/name
            if not file.resolve().is_relative_to(APP.resolve()) or not file.is_file() or sha(file)!=expected.lower():raise RuntimeError('Embedded runtime missing or changed: '+name)
        if any((ai/name).is_file() for name in ['llama-server.exe','llama-server-impl.dll']):raise RuntimeError('Legacy server files must not be included in the embedded distribution.')
    for required in ['Astra.Desktop.exe','Astra.Desktop.dll','Astra.Core.dll','Astra.Desktop.pri','MainWindow.xbf',
                     'App.xbf','Assets/astra.ico','Ai/components/astra-local-model.gguf','Localization/en.json',
                     'coreclr.dll','hostfxr.dll','hostpolicy.dll']:
        if not (APP/required).is_file():raise RuntimeError('Required publish file missing: '+required)
    payload={}
    for file in APP.rglob('*'):
        if not file.is_file():continue
        relative=file.relative_to(APP).as_posix()
        if file.is_symlink():raise RuntimeError('Symlink in publish: '+relative)
        parts={p.lower() for p in file.relative_to(APP).parts}
        if parts&{'webview2','ebwebview','logs','__pycache__','.git'} or file.suffix.lower() in {'.log','.dpapi'} or file.name=='preferences.json':raise RuntimeError('User/cache data in publish: '+relative)
        if file.suffix.lower()=='.pdb' or relative=='INSTALLAZIONE.md':continue
        payload[relative]=file
    payload.update({'README.md':ROOT/'README.md','RELEASE-NOTES.md':RELEASE_NOTES,
        'SECOND-BRAIN-GUIDE.md':ROOT/'docs/SECOND-BRAIN-GUIDE.md',
        'GITHUB-UPDATE.md':ROOT/'docs/DOCK-UPDATE-GITHUB.md','THIRD-PARTY-NOTICES.md':ROOT/'docs/THIRD-PARTY-NOTICES.md',
        'prerequisites/README.md':ROOT/'docs/PREREQUISITES-README.md'})
    payload.update(EXTRA_DOCS)
    spec=importlib.util.spec_from_file_location('astra_release_helpers',ROOT/'scripts/package-release.py')
    helpers=importlib.util.module_from_spec(spec);spec.loader.exec_module(helpers);helpers.APP=APP
    payload.update(helpers.collect_licenses())
    provenance=json.loads((DIST/'release-prerequisites/sources.json').read_text(encoding='utf-8-sig'))
    for entry in provenance['installers']:
        file=DIST/'release-prerequisites'/entry['file']
        if entry['signatureStatus']!='Valid' or 'O=Microsoft Corporation,' not in entry['publisher'] or sha(file)!=entry['sha256'].lower():raise RuntimeError('Prerequisite validation failed')
        payload['prerequisites/'+entry['file']]=file
    payload['prerequisites/sources.json']=encode(provenance)
    payload[VERIFICATION_NAME]=encode({'artifact':evidence,'nativeChecks':report})
    expected={name:sha(source) if isinstance(source,Path) else hashlib.sha256(source).hexdigest() for name,source in payload.items()}
    manifest=''.join(f'{value}  {name}\n' for name,value in sorted(expected.items())).encode('utf8')
    payload['FILES.sha256']=manifest;expected['FILES.sha256']=hashlib.sha256(manifest).hexdigest()
    archive=DIST/(NAME+'.zip')
    print(f'Packaging {NAME}: {len(payload)} files',flush=True)
    with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=3,allowZip64=True) as package:
        for name,source in sorted(payload.items()):
            if isinstance(source,Path):
                mode=zipfile.ZIP_STORED if source.stat().st_size>64*1024**2 or source.suffix.lower() in {'.glb','.png','.jpg','.zip'} else zipfile.ZIP_DEFLATED
                package.write(source,TOP+'/'+name,compress_type=mode)
            else:package.writestr(TOP+'/'+name,source)
    if archive.stat().st_size>=2*1024**3:raise RuntimeError('Release asset is too large for GitHub.')
    with zipfile.ZipFile(archive) as package:
        assert len(package.namelist())==len(payload)
        for entry in package.infolist():
            with package.open(entry) as stream:actual=hashlib.file_digest(stream,'sha256').hexdigest()
            if actual!=expected[entry.filename.removeprefix(TOP+'/')]:raise RuntimeError('ZIP integrity failure: '+entry.filename)
    checksum=sha(archive);(DIST/(NAME+'.zip.sha256')).write_text(checksum+'  '+archive.name+'\n',encoding='utf8')
    shutil.copy2(ROOT/'README.md',DIST/(NAME+'-README.md'))
    result={'archive':archive.name,'bytes':archive.stat().st_size,'sha256':checksum,'fileCount':len(payload),
            'allPayloadHashesMatch':True,'nativeChecksPassed':True,'appBinariesChanged':True}
    (DIST/(NAME+'-verification.json')).write_bytes(encode(result));print(json.dumps(result,indent=2),flush=True)


if __name__=='__main__':
    parser=argparse.ArgumentParser();group=parser.add_mutually_exclusive_group();group.add_argument('--source-only',action='store_true');group.add_argument('--binary-only',action='store_true');args=parser.parse_args()
    if not args.binary_only:source_update()
    if not args.source_only:binary_update()
