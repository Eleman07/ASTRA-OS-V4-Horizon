"""Package the tested local agent build, with a complete per-file SHA-256 verification."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / 'dist/AstraOS-Agent-Brain-Gpu-4.1.0'
NAME = 'ASTRA-OS-V4-HORIZON-agent-brain-win-x64'
TOP = 'ASTRA-OS-V4-HORIZON'

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def main():
    for name in ('Astra.Desktop.exe', 'Astra.Desktop.dll', 'Astra.Desktop.pri', 'MainWindow.xbf', 'Astra.AgentWorker.exe', 'Astra.AgentWorker.dll', 'Astra.AgentSupervisor.exe', 'BrainGraph/index.html', 'BrainGraph/graph.js', 'Ai/embeddings/nomic-embed-text-v1.5.Q8_0.gguf'):
        assert (APP / name).is_file(), 'Missing distribution component: ' + name
    qa = json.loads((ROOT / 'docs/evidence/agent-brain-native.json').read_text(encoding='utf-8-sig'))
    assert qa['passed'], 'Native QA failed'
    complex_qa=json.loads((ROOT/'docs/evidence/agent-brain-complex.json').read_text(encoding='utf-8-sig'))
    assert complex_qa['passed'], 'Complex task validation failed'
    payload = {}
    for file in sorted(APP.rglob('*')):
        if not file.is_file():
            continue
        relative = file.relative_to(APP)
        if file.is_symlink() or any(p.lower() in {'webview2', 'ebwebview', 'logs', '__pycache__'} for p in relative.parts):
            raise RuntimeError('Unexpected cache or symlink: ' + str(relative))
        if file.suffix.lower() in {'.pdb', '.log', '.dpapi'} or file.name == 'preferences.json':
            continue
        payload[relative.as_posix()] = file
    payload['README.md'] = ROOT / 'README.md'
    payload['AGENT-BRAIN-GUIDE.md'] = ROOT / 'docs/AGENT-BRAIN-GUIDE.md'
    payload['RELEASE-NOTES.md'] = ROOT / 'docs/RELEASE-NOTES-AGENT-BRAIN.md'
    hashes = {name: digest(file) for name, file in payload.items()}
    manifest = ''.join(value + '  ' + name + '\n' for name, value in sorted(hashes.items()))
    archive = ROOT / 'dist' / (NAME + '.zip')
    temporary = archive.with_suffix('.zip.tmp')
    with zipfile.ZipFile(temporary, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for name, file in payload.items():
            z.write(file, TOP + '/' + name)
        z.writestr(TOP + '/FILES.sha256', manifest)
    assert temporary.stat().st_size < 2 * 1024 ** 3, 'GitHub asset exceeds 2 GiB'
    with zipfile.ZipFile(temporary) as z:
        assert len(z.infolist()) == len(payload) + 1
        for name, expected in hashes.items():
            with z.open(TOP + '/' + name) as stream:
                assert hashlib.file_digest(stream, 'sha256').hexdigest() == expected, name
    temporary.replace(archive)
    sha = digest(archive)
    archive.with_suffix('.zip.sha256').write_text(sha + '  ' + archive.name + '\n', encoding='utf-8')
    report = {'generatedUtc': datetime.now(timezone.utc).isoformat(), 'archive': archive.name, 'sha256': sha, 'bytes': archive.stat().st_size, 'fileCount': len(payload), 'allPayloadHashesMatch': True, 'nativeChecksPassed': True, 'nativeEvidence': qa, 'complexTaskEvidence': complex_qa, 'signed': False, 'desktopDllSha256': digest(APP / 'Astra.Desktop.dll'), 'workerDllSha256': digest(APP / 'Astra.AgentWorker.dll')}
    (ROOT / 'dist' / (NAME + '-verification.json')).write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in report.items() if k != 'nativeEvidence'}, indent=2))

if __name__ == '__main__':
    main()
