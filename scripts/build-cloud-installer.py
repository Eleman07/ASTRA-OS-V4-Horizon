"""Build the account-scoped cloud setup from an exact, audited portable payload.

No application/prerequisite/setup is executed by this builder. NSIS is verified
against its pinned official archive before compilation. Personal vaults and
runtime credentials are never read.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import uuid
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist"
NSIS = ROOT / "downloads/installer-tools/nsis-3.13"
NSIS_ARCHIVE = NSIS.parent / (NSIS.name + ".zip")
NSIS_SHA = "ba63dffc4410ee89193e1cb5a41989991bd77c61068da17e3156d136b7b0b3d8"


def sha(path: Path) -> str:
    with path.open("rb") as source:
        return hashlib.file_digest(source, "sha256").hexdigest()


def quote(value: object) -> str:
    return '"' + str(value).replace("$", "$$").replace('"', '$\\"') + '"'


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--payload", type=Path, default=DIST / "AstraOS-Cloud-4.2.0-Final")
    parser.add_argument("--output", type=Path, default=DIST / "ASTRA-OS-V4-HORIZON-Setup-4.2.0-cloud-win-x64.exe")
    args = parser.parse_args()
    source = args.payload.resolve(strict=True)
    assert source.is_relative_to(DIST.resolve()) and not source.is_symlink()
    manifest = json.loads((source / "cloud-build-manifest.json").read_text(encoding="utf-8-sig"))
    expected: dict[str, str] = {}
    for record in manifest["Files"]:
        relative = PurePosixPath(record["Path"].replace("\\", "/"))
        assert not relative.is_absolute() and ".." not in relative.parts and ":" not in str(relative)
        name = relative.as_posix()
        assert name not in expected
        file = source / name
        assert file.resolve().is_relative_to(source) and file.is_file() and not file.is_symlink()
        assert file.stat().st_size == record["Bytes"] and sha(file).lower() == record["Sha256"].lower(), name
        assert file.suffix.lower() not in {".gguf", ".dpapi"} and ".obsidian" not in relative.parts
        expected[name] = sha(file)
    expected["cloud-build-manifest.json"] = sha(source / "cloud-build-manifest.json")
    actual = {p.relative_to(source).as_posix() for p in source.rglob("*") if p.is_file()}
    assert actual == set(expected), "Payload includes unexplained additions or missing files"
    assert sha(NSIS_ARCHIVE) == NSIS_SHA, "Unverified NSIS compiler archive"
    with zipfile.ZipFile(NSIS_ARCHIVE) as package:
        for entry in package.infolist():
            if entry.is_dir():
                continue
            relative = PurePosixPath(entry.filename)
            assert not relative.is_absolute() and ".." not in relative.parts
            file = NSIS.parent / str(relative)
            assert file.is_file() and sha(file) == hashlib.sha256(package.read(entry)).hexdigest(), str(relative)

    stage = DIST / "installer-cloud-stage" / uuid.uuid4().hex / "payload"
    stage.mkdir(parents=True)
    for name in expected:
        target = stage / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source / name, target)
        assert sha(target) == expected[name]
    installer_license = stage / "licenses/installer/NSIS-COPYING.txt"
    installer_license.parent.mkdir(parents=True, exist_ok=True)
    installer_license.write_bytes((NSIS / "COPYING").read_bytes())
    expected[installer_license.relative_to(stage).as_posix()] = sha(installer_license)
    compiler_source = installer_license.with_name("SOURCE.txt")
    compiler_source.write_text("NSIS 3.13 official portable compiler.\nhttps://nsis.sourceforge.io/\nCompiler archive SHA-256: " + NSIS_SHA + "\n", encoding="utf8")
    expected[compiler_source.relative_to(stage).as_posix()] = sha(compiler_source)
    installer_files = stage / "INSTALLER-FILES.sha256"
    installer_files.write_text("".join(digest + "  " + name + "\n" for name, digest in sorted(expected.items())), encoding="utf8")
    expected[installer_files.name] = sha(installer_files)

    lines = ["!macro ASTRA_INSTALL_PAYLOAD"]
    current_directory = None
    directories: set[str] = set()
    for name in sorted(expected):
        path = PurePosixPath(name)
        parent = path.parent.as_posix()
        if current_directory != parent:
            current_directory = parent
            suffix = "\\" + parent.replace("/", "\\") if parent != "." else ""
            lines.append('SetOutPath "$INSTDIR' + suffix + '"')
        stored = (stage / name).stat().st_size > 64 * 1024**2 or path.suffix.lower() in {".glb", ".png", ".jpg", ".zip", ".exe"}
        lines.extend(["SetCompress " + ("off" if stored else "auto"), "ClearErrors", "File " + quote("/oname=" + path.name) + " " + quote(stage / name), "IfErrors 0 +3", "SetErrorLevel 1", 'Abort "A file could not be installed. Close ASTRA and retry."'])
        directories.update(p.as_posix() for p in path.parents if p.as_posix() != ".")
    lines.extend(["!macroend", "!macro ASTRA_UNINSTALL_PAYLOAD"])
    for name in sorted(expected):
        target = "$INSTDIR\\" + name.replace("/", "\\")
        lines.extend(['${If} ${FileExists} "' + target + '"', "ClearErrors", 'Delete "' + target + '"', "IfErrors UninstallFailed", "${EndIf}"])
    for directory in sorted(directories, key=lambda p: (-p.count("/"), p)):
        lines.append('RMDir "$INSTDIR\\' + directory.replace("/", "\\") + '"')
    lines.append("!macroend")
    generated = stage.parent / "payload.nsh"
    generated.write_text("\n".join(lines) + "\n", encoding="utf8")
    output = args.output.resolve()
    assert output.parent == DIST.resolve()
    total = sum((stage / name).stat().st_size for name in expected)
    command = [str(NSIS / "makensis.exe"), "/NOCONFIG", "/V2", "/DGENERATED_PAYLOAD=" + str(generated), "/DOUTPUT_EXE=" + str(output), "/DAPP_ICON=" + str(stage / "Assets/astra.ico"), "/DAPP_LICENSE=" + str(stage / "LICENSE"), "/DINSTALLED_KB=" + str((total + 1023) // 1024), "/DPRODUCT_VERSION=4.2.0.0", str(ROOT / "installer/AstraCloud.nsi")]
    print("Compiling verified cloud installer:", len(expected), "files,", total, "bytes", flush=True)
    subprocess.run(command, check=True, cwd=ROOT)
    assert output.is_file() and output.stat().st_size < 2 * 1024**3
    digest = sha(output)
    output.with_suffix(".exe.sha256").write_text(digest + "  " + output.name + "\n", encoding="utf8")
    report = {"generated": datetime.now(timezone.utc).isoformat(), "installer": output.name, "version": "4.2.0", "bytes": output.stat().st_size, "sha256": digest, "compiler": "NSIS 3.13", "compilerArchiveSha256": NSIS_SHA, "payloadManifestSha256": sha(source / "cloud-build-manifest.json"), "payloadRoot": stage.relative_to(DIST).as_posix(), "payloadCount": len(expected), "payloadHashes": expected, "installationTested": False, "signed": False, "stableCertification": False, "localModelWeights": False, "personalVaultIncluded": False}
    report_path = output.with_name(output.stem + "-build.json")
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf8")
    print(json.dumps({k: v for k, v in report.items() if k != "payloadHashes"}, indent=2), flush=True)


if __name__ == "__main__":
    main()
