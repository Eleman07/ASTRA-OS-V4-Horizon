"""Build the account-scoped cloud/local GPU setup from an audited payload.

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
    parser.add_argument("--flavour", choices=["cloud", "local-gpu", "codex"], default="cloud")
    parser.add_argument("--payload", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--version", default=None)
    args = parser.parse_args()
    version = args.version or ("4.4.0" if args.flavour == "codex" else "4.2.0")
    assert len(version.split(".")) == 3 and all(p.isdigit() for p in version.split("."))
    local = args.flavour == "local-gpu"
    args.payload = args.payload or DIST / ("AstraOS-LocalGPU-4.2.0-Final" if local else "AstraOS-Cloud-4.2.0-Final")
    args.output = args.output or DIST / ("ASTRA-OS-V4-HORIZON-Setup-4.2.0-" + args.flavour + "-win-x64.exe")
    source = args.payload.absolute()
    from local_gpu_payload import no_links
    no_links(source)
    assert source.resolve().is_relative_to(DIST.resolve())
    manifest_name = "local-gpu-build-manifest.json" if local else "cloud-build-manifest.json"
    manifest = json.loads((source / manifest_name).read_text(encoding="utf-8-sig"))
    expected: dict[str, str] = {}
    records = manifest["Files"] if not local else []
    if local:
        from local_gpu_payload import audit
        _, expected, _ = audit(source)
    for record in records:
        relative = PurePosixPath(record["Path"].replace("\\", "/"))
        assert not relative.is_absolute() and ".." not in relative.parts and ":" not in str(relative)
        name = relative.as_posix()
        assert name not in expected
        file = source / name
        assert file.resolve().is_relative_to(source) and file.is_file() and not file.is_symlink()
        assert file.stat().st_size == record["Bytes"] and sha(file).lower() == record["Sha256"].lower(), name
        assert file.suffix.lower() not in {".gguf", ".dpapi"} and ".obsidian" not in relative.parts
        expected[name] = sha(file)
    expected[manifest_name] = sha(source / manifest_name)
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

    stage = DIST / ("installer-" + args.flavour + "-stage") / uuid.uuid4().hex / "payload"
    no_links(stage)
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
    output = args.output.absolute()
    no_links(output)
    assert output.parent.resolve() == DIST.resolve()
    total = sum((stage / name).stat().st_size for name in expected)
    template = ROOT / "installer" / ("AstraLocalGpu.nsi" if local else "AstraCodex.nsi" if args.flavour == "codex" else "AstraCloud.nsi")
    command = [str(NSIS / "makensis.exe"), "/NOCONFIG", "/V2", "/DGENERATED_PAYLOAD=" + str(generated), "/DOUTPUT_EXE=" + str(output), "/DAPP_ICON=" + str(stage / "Assets/astra.ico"), "/DAPP_LICENSE=" + str(stage / "LICENSE"), "/DINSTALLED_KB=" + str((total + 1023) // 1024), "/DPRODUCT_VERSION=" + version + ".0", str(template)]
    print("Compiling verified " + args.flavour + " installer:", len(expected), "files,", total, "bytes", flush=True)
    subprocess.run(command, check=True, cwd=ROOT)
    assert output.is_file() and output.stat().st_size < 2 * 1024**3
    digest = sha(output)
    no_links(output.with_suffix(".exe.sha256"))
    output.with_suffix(".exe.sha256").write_text(digest + "  " + output.name + "\n", encoding="utf8")
    report = {"generated": datetime.now(timezone.utc).isoformat(), "installer": output.name, "version": version, "flavour":args.flavour, "bytes": output.stat().st_size, "sha256": digest, "compiler": "NSIS 3.13", "compilerArchiveSha256": NSIS_SHA, "payloadManifestSha256": sha(source / manifest_name), "payloadRoot": stage.relative_to(DIST).as_posix(), "payloadCount": len(expected), "payloadHashes": expected, "installationTested": False, "signed": False, "stableCertification": False, "localModelWeights": local, "textModelWeightsIncluded":False, "personalVaultIncluded": False}
    report_path = output.with_name(output.stem + "-build.json")
    no_links(report_path)
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf8")
    print(json.dumps({k: v for k, v in report.items() if k != "payloadHashes"}, indent=2), flush=True)


if __name__ == "__main__":
    main()
