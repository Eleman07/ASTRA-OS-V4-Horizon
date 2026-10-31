#!/usr/bin/env python3
"""Verify the cloud build and create its portable, source-free GitHub ZIP."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import uuid
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist"
TOP = "ASTRA-OS-V4-HORIZON"
DEFAULT_BUILD = DIST / "AstraOS-Cloud-4.2.0-Final"
DEFAULT_ZIP = DIST / "ASTRA-OS-V4-HORIZON-4.2.0-cloud-win-x64.zip"
REQUIRED = {
    "Astra.Desktop.exe", "Astra.Desktop.dll", "Astra.Core.dll",
    "Astra.Desktop.pri", "App.xbf", "MainWindow.xbf",
    "Astra.AgentWorker.exe", "Astra.AgentWorker.dll",
    "Astra.AgentSupervisor.exe", "Astra.AgentSupervisor.dll",
    "BrainGraph/index.html", "BrainGraph/graph.js", "README.md",
}
PRIVATE_DIRS = {".obsidian", ".git", ".trash", "logs", "ebwebview", "webview2"}
SECRET = re.compile(rb"sk-or-v1-[A-Za-z0-9_-]{24,}")


def sha(path: Path) -> str:
    with path.open("rb") as source:
        return hashlib.file_digest(source, "sha256").hexdigest()


def safe_name(value: str) -> str:
    value = value.replace("\\", "/")
    path = PurePosixPath(value)
    if path.is_absolute() or not value or any(p in {"", ".", ".."} for p in value.split("/")) or ":" in value or any(ord(c) < 32 for c in value):
        raise ValueError("Unsafe package path: " + repr(value))
    return value


def inventory(root: Path) -> dict[str, Path]:
    result: dict[str, Path] = {}
    pending = [root]
    while pending:
        directory = pending.pop()
        with os.scandir(directory) as children:
            for entry in children:
                path = Path(entry.path)
                info = entry.stat(follow_symlinks=False)
                if entry.is_symlink() or getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
                    raise ValueError("Linked file/directory in build: " + str(path))
                if entry.is_dir(follow_symlinks=False):
                    if entry.name.lower() in PRIVATE_DIRS:
                        raise ValueError("Private/cache directory in build: " + str(path))
                    pending.append(path)
                elif entry.is_file(follow_symlinks=False):
                    name = safe_name(path.relative_to(root).as_posix())
                    lowered = name.lower()
                    if lowered.endswith((".gguf", ".safetensors", ".onnx", ".dpapi", ".key")) or Path(name).name.lower().startswith("ggml-") and lowered.endswith(".bin"):
                        raise ValueError("Model weights or private data in build: " + name)
                    if lowered.endswith((".json", ".md", ".txt", ".ps1", ".xml", ".config", ".yml", ".yaml")) and SECRET.search(path.read_bytes()):
                        raise ValueError("API credential pattern in build: " + name)
                    result[name] = path
                else:
                    raise ValueError("Unsupported filesystem entry in build: " + str(path))
    if len({name.casefold() for name in result}) != len(result):
        raise ValueError("Case-colliding package paths")
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, default=DEFAULT_BUILD)
    parser.add_argument("--output", type=Path, default=DEFAULT_ZIP)
    args = parser.parse_args()
    build = args.build.resolve(strict=True)
    output = args.output.resolve()
    if not build.is_relative_to(DIST.resolve()) or output.parent != DIST.resolve() or output.suffix.lower() != ".zip":
        raise ValueError("Only the task's dist build and dist ZIP destination are allowed")
    if build.is_symlink() or build.is_junction():
        raise ValueError("Linked build root is not allowed")
    files = inventory(build)
    manifest = json.loads(files["cloud-build-manifest.json"].read_text(encoding="utf-8-sig"))
    if manifest.get("Version") != "4.2.0" or manifest.get("Provider") != "OpenRouter" or manifest.get("LocalModelWeights") is not False:
        raise ValueError("Unexpected build manifest identity")
    entries = {safe_name(item["Path"]): item for item in manifest["Files"]}
    if len(entries) != len(manifest["Files"]):
        raise ValueError("Duplicate manifest paths")
    missing = set(entries) - files.keys()
    extras = files.keys() - set(entries) - {"cloud-build-manifest.json"}
    if missing or extras or not REQUIRED <= files.keys():
        raise ValueError(f"Incomplete or unexplained build inventory: missing={sorted(missing)}, extras={sorted(extras)}, required={sorted(REQUIRED-files.keys())}")
    digests = {}
    for name, path in files.items():
        actual = sha(path)
        if name in entries and (actual != entries[name]["Sha256"].lower() or path.stat().st_size != entries[name]["Bytes"]):
            raise ValueError("Build manifest mismatch: " + name)
        digests[name] = actual
    generic_docs = 0
    for name, path in files.items():
        for prefix in ("KnowledgeKit/", "docs/obsidian-cloud/"):
            if name.startswith(prefix):
                original = ROOT / "docs/obsidian-cloud" / name[len(prefix):]
                if not original.is_file() or original.read_bytes() != path.read_bytes():
                    raise ValueError("Non-generic/modified workspace-kit file: " + name)
                generic_docs += 1
    temp = output.with_name(output.name + "." + uuid.uuid4().hex + ".tmp")
    try:
        with zipfile.ZipFile(temp, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as package:
            for name in sorted(files):
                package.write(files[name], TOP + "/" + name)
        with zipfile.ZipFile(temp) as package:
            names = {entry.filename for entry in package.infolist()}
            if len(names) != len(files) or names != {TOP + "/" + name for name in files}:
                raise ValueError("Archive inventory differs from the audited build")
            for entry in package.infolist():
                safe_name(entry.filename)
                if stat.S_ISLNK(entry.external_attr >> 16):
                    raise ValueError("Archive contains a symlink")
                relative = entry.filename[len(TOP) + 1:]
                with package.open(entry) as source:
                    if hashlib.file_digest(source, "sha256").hexdigest() != digests[relative]:
                        raise ValueError("Archive byte/hash mismatch: " + relative)
        os.replace(temp, output)
    finally:
        if temp.exists():
            temp.unlink()
    digest = sha(output)
    checksum = output.with_suffix(output.suffix + ".sha256")
    checksum.write_text(digest + "  " + output.name + "\n", encoding="utf-8")
    report = {
        "artifact": output.name, "bytes": output.stat().st_size, "sha256": digest,
        "topLevelFolder": TOP, "archiveFiles": len(files), "manifestFilesVerified": len(entries),
        "unexplainedExtras": [], "genericKitFilesCompared": generic_docs,
        "noPersonalVault": True, "noCredentials": True, "noModelWeights": True,
        "archivePathsAndEveryEntryHashVerified": True,
    }
    output.with_suffix(".verification.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2), flush=True)


if __name__ == "__main__":
    main()
