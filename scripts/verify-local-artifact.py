"""Verify each published byte against the local distribution archive."""
from pathlib import Path
import hashlib
import json
import zipfile
from datetime import datetime, timezone

root=Path(__file__).resolve().parents[1]
directory=root/'dist/AstraOS-Local'
archive=root/'dist/ASTRA-OS-V4-HORIZON-win-x64-LOCAL.zip'
def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream,'sha256').hexdigest().upper()
files={p.relative_to(directory).as_posix():p for p in directory.rglob('*') if p.is_file()}
with zipfile.ZipFile(archive) as package:
    entries={entry.filename.replace('\\','/'):entry for entry in package.infolist() if not entry.is_dir()}
    if files.keys()!=entries.keys():raise RuntimeError('Archive/publication file lists differ')
    for name,path in files.items():
        with package.open(entries[name]) as stream:
            if hashlib.file_digest(stream,'sha256').hexdigest().upper()!=digest(path):raise RuntimeError('Hash mismatch: '+name)
    if any('ebwebview' in name.lower() for name in entries):raise RuntimeError('Runtime browser cache included')
report=dict(generated=datetime.now(timezone.utc).isoformat(),archive=str(archive),sha256=digest(archive),
            executableSha256=digest(directory/'Astra.Desktop.exe'),fileCount=len(files),archiveBytes=archive.stat().st_size,
            distributionBytes=sum(p.stat().st_size for p in files.values()),allFilesMatch=True,browserCacheExcluded=True)
(root/'docs/evidence/expansion-artifact-hashes.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
