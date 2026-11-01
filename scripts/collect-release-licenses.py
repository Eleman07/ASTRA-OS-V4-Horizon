"""Collect notices from the actual restored packages; never read account data."""
import argparse
import importlib.util
from pathlib import Path
import shutil

spec = importlib.util.spec_from_file_location('release_notices', Path(__file__).with_name('package-release.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
target = parser.parse_args().output.resolve(strict=True)
assert target.is_relative_to(module.DIST.resolve())
module.APP = target
notices = module.collect_licenses()
for name, source in notices.items():
    destination = target / name
    assert destination.resolve().is_relative_to(target)
    destination.parent.mkdir(parents=True, exist_ok=True)
    if isinstance(source, bytes):
        destination.write_bytes(source)
    else:
        shutil.copy2(source, destination)
print('Collected', len(notices), 'license/provenance files')
