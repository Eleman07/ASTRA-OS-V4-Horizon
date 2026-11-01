# ASTRA OS V4 — Fix GitHub Actions (Pillow / PIL)

## Diagnosis
The Windows runner halted before compiling the app: `tests/static/test_v4_contracts.py` imports `from PIL import Image`, but the CI pipeline did not install `Pillow`.

## Applied fix
- Remove the external Pillow dependency from the V4 PNG asset test.
- Validate expected PNG signature, chunks, per-chunk CRC32, IHDR and decompressed image stream using Python's standard library.
- Add a regression check confirming corrupted asset bytes are rejected.
- Pin Python 3.12 in GitHub Actions and run static tests with `python -S` (site-packages disabled).

## Test results obtained in this environment
- `python -S -m unittest discover -s tests/static -v` — PASS, 25/25.
- `python -S scripts/validate_source.py` — PASS.
- `python -S scripts/lint_cs_strings.py` — PASS.
- `node --check design-preview/preview.js` — PASS.
- `node --check src/Astra.Desktop/Graphics3D/web/scene.js` — PASS.

## To use on your existing GitHub repository
You can apply only the patch to your existing V4 branch:

```bash
git apply --check ASTRA-OS-V4-FIX-PIL.patch
git apply ASTRA-OS-V4-FIX-PIL.patch
git add .github/workflows/build.yml tests/static/test_v4_contracts.py
git commit -m "Fix V4 CI: validate PNG assets without Pillow"
git push
```

Alternatively, replace those two files using the corresponding files from the fixed source archive. Do not overwrite unrelated changes.

## Limitations
This repairs the reported test import failure; GitHub Actions must still rerun on Windows. It does NOT confirm WinUI compilation or Windows app runtime compatibility, and V4 is still a release candidate.
