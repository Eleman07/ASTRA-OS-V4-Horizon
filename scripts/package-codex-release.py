"""Audit the public online payload and create a verified portable ZIP.

Only the pinned CPU Whisper checkpoints may be included as model weights.
No personal profile, vault, credential or test output is an allowed payload.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / 'dist'
def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def audit(build):
    assert build.resolve().is_relative_to(DIST.resolve())
    files = {}
    for directory, dirs, names in os.walk(build, followlinks=False):
        folder = Path(directory)
        for item in [folder, *(folder / name for name in dirs + names)]:
            assert not item.is_symlink() and not item.is_junction(), 'Linked payload entry'
        assert not {d.casefold() for d in dirs} & {'.obsidian', '.git', 'webview2', 'ebwebview', 'logs', '__pycache__', 'jobs', 'settings'}, 'Private runtime directory'
        for name in names:
            path = folder / name
            relative = path.relative_to(build).as_posix()
            assert path.suffix.lower() not in {'.dpapi', '.key', '.gguf', '.safetensors', '.onnx', '.pdb'}, relative
            if path.suffix.lower() == '.bin':
                pinned = {'Ai/components/ggml-small.bin': '1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b', 'Ai/components/ggml-tiny.bin': 'be07e048e1e599ad46341c8d2a135645097a538221678b7acdd1b1919c6e1b21'}
                assert relative in pinned and sha(path) == pinned[relative], 'Unapproved model weights: ' + relative
            if path.suffix.lower() in {'.md', '.json', '.txt', '.xml', '.ps1', '.config', '.yml'}:
                data = path.read_bytes()
                assert not re.search(rb'sk-or-v1-[A-Za-z0-9_-]{24,}|gh[pousr]_[A-Za-z0-9]{30,}', data), 'Credential pattern: ' + relative
                personal_prefixes = [str(Path.home()).encode(), str(Path.home()).replace('\\', '/').encode()]
                assert not any(prefix in data for prefix in personal_prefixes), 'Personal path: ' + relative
            files[relative] = path
    manifest = json.loads(files['cloud-build-manifest.json'].read_text(encoding='utf-8-sig'))
    assert manifest['Version'] == '4.3.0' and manifest['Provider'] == 'Codex ChatGPT plan'
    expected = {record['Path'].replace('\\', '/'): record for record in manifest['Files']}
    assert len(expected) == len(manifest['Files']) and set(files) == set(expected) | {'cloud-build-manifest.json'}
    assert {'README.md', 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'licenses/dependencies.json', 'prerequisites/sources.json', 'Astra.Desktop.exe', 'Astra.AgentWorker.exe', 'Astra.AgentSupervisor.exe', 'BrainGraph/graph.js'} <= set(files)
    assert len([n for n in files if n.startswith('AssistantSkills/') and n.endswith('.json')]) == 39
    for name, record in expected.items():
        assert sha(files[name]) == record['Sha256'].lower() and files[name].stat().st_size == record['Bytes'], name
        if name.startswith('KnowledgeKit/'):
            assert files[name].read_bytes() == (ROOT / 'docs/obsidian-cloud' / name.removeprefix('KnowledgeKit/')).read_bytes(), 'Non-generic kit'
    prerequisites = json.loads(files['prerequisites/sources.json'].read_text(encoding='utf-8-sig'))
    for record in prerequisites['installers']:
        assert sha(files['prerequisites/' + record['file']]) == record['sha256'].lower()
    return files

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--build', type=Path, default=DIST / 'AstraOS-Online-4.3.0')
    parser.add_argument('--audit-only', action='store_true')
    args = parser.parse_args()
    files = audit(args.build.resolve(strict=True))
    output = DIST / 'ASTRA-OS-V4-HORIZON-4.3.0-online-win-x64.zip'
    if not args.audit_only:
        assert not output.exists(), 'Preserve existing release ZIP'
        with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as package:
            for name, file in sorted(files.items()):
                package.write(file, 'ASTRA-OS-V4-HORIZON/' + name)
        with zipfile.ZipFile(output) as package:
            assert len(package.infolist()) == len(files)
            for name, file in files.items():
                with package.open('ASTRA-OS-V4-HORIZON/' + name) as stream:
                    assert hashlib.file_digest(stream, 'sha256').hexdigest() == sha(file)
        assert output.stat().st_size < 2 * 1024**3
        output.with_suffix('.zip.sha256').write_text(sha(output) + '  ' + output.name + '\n')
    report = {'utc': datetime.now(timezone.utc).isoformat(), 'payloadFiles': len(files), 'payloadAudited': True, 'personalVaultIncluded': False, 'credentialsIncluded': False, 'localTextVisionModelIncluded': False, 'cpuWhisperIncluded': True, 'zip': None if args.audit_only else output.name, 'zipSha256': None if args.audit_only else sha(output)}
    (DIST / 'online-4.3-package-audit.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report, indent=2))

if __name__ == '__main__':
    main()
