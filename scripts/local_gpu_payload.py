"""Strict, shared audit for the user-selected local GPU release (no downloads)."""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist"
MANIFEST = "local-gpu-build-manifest.json"
RUNTIME_SHA = "aa3452dfa16cdf7d902b44e5445e43e13023c64fb179ca7a13d60611d4371691"
AUXILIARY_WEIGHTS = {
    "Ai/vision/vision-model.gguf": (175054528, "2a31195d3769c0b0fd0a4906201666108834848db768af11de1d2cef7cd35e65"),
    "Ai/vision/vision-projector.gguf": (190031616, "0802360aca1748f112ea510b8ff277c65b1361c8ef30ed89b83c9c7a60d08e96"),
    "Ai/components/ggml-tiny.bin": (77691713, "be07e048e1e599ad46341c8d2a135645097a538221678b7acdd1b1919c6e1b21"),
    "Ai/components/ggml-small.bin": (487601967, "1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b"),
}
REQUIRED = {"Astra.Desktop.exe", "Astra.Desktop.dll", "Astra.Core.dll", "Astra.Desktop.pri", "App.xbf", "MainWindow.xbf",
            "Astra.AgentWorker.exe", "Astra.AgentWorker.dll", "Astra.AgentSupervisor.exe", "Astra.AgentSupervisor.dll",
            "BrainGraph/index.html", "BrainGraph/graph.js", "README.md", "THIRD-PARTY-NOTICES.md",
            "docs/LOCAL-GPU-README.md", "docs/LOCAL-GPU-4.2.0-VERIFICATION.md", "docs/LOCAL-GPU-MODELS.md",
            "docs/local-gpu-model-pins.json", "Ai/gpu/llama-server.exe",
            "Ai/gpu/manifest.json", "Ai/vision/manifest.json", "Ai/vision/LICENSE.txt", "Ai/components/manifest.json",
            "LICENSE", "Assets/astra.ico", "prerequisites/sources.json", "prerequisites/README.md",
            "licenses/local-gpu/llama.cpp/LICENSE.txt"} | AUXILIARY_WEIGHTS.keys()
PRIVATE_DIRS = {".obsidian", ".git", ".trash", "logs", "downloads", "ebwebview", "webview2", "__pycache__"}
SECRET = re.compile(rb"sk-or-v1-[A-Za-z0-9_-]{24,}")


def sha(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def safe_name(value: str) -> str:
    value = value.replace("\\", "/")
    path = PurePosixPath(value)
    if path.is_absolute() or not value or any(part in {"", ".", ".."} for part in value.split("/")) or ":" in value or any(ord(c) < 32 for c in value):
        raise ValueError("Unsafe package path: " + repr(value))
    return value


def no_links(path: Path) -> None:
    for item in [path, *path.parents]:
        if item.exists() or item.is_symlink() or item.is_junction():
            info = item.lstat()
            if item.is_symlink() or getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
                raise ValueError("Linked package path is not allowed: " + str(item))


def inventory(root: Path) -> dict[str, Path]:
    no_links(root)
    result: dict[str, Path] = {}
    pending = [root]
    while pending:
        with os.scandir(pending.pop()) as children:
            for entry in children:
                path = Path(entry.path)
                info = entry.stat(follow_symlinks=False)
                if entry.is_symlink() or getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
                    raise ValueError("Linked build entry: " + str(path))
                if entry.is_dir(follow_symlinks=False):
                    if entry.name.lower() in PRIVATE_DIRS:
                        raise ValueError("Private/cache directory in release: " + str(path))
                    pending.append(path)
                elif entry.is_file(follow_symlinks=False):
                    name = safe_name(path.relative_to(root).as_posix())
                    lower = name.lower()
                    if lower.endswith((".dpapi", ".key", ".pyc", ".pyo", ".part", ".chunks.json")):
                        raise ValueError("Private/runtime data in release: " + name)
                    if lower == "ai/components/license-model.txt":
                        raise ValueError("Obsolete bundled text-model license in release")
                    if (lower.endswith((".gguf", ".safetensors", ".onnx", ".pt", ".pth"))
                        or (path.name.lower().startswith("ggml-") and lower.endswith(".bin"))) and name not in AUXILIARY_WEIGHTS:
                        raise ValueError("Unapproved/text model weights in release: " + name)
                    if "license" in path.name.lower() and any(word in lower for word in ("qwen", "deepseek", "baichuan", "internlm", "chatglm")):
                        raise ValueError("Excluded model license in release: " + name)
                    if lower.endswith((".json", ".md", ".txt", ".ps1", ".xml", ".config", ".yml", ".yaml")):
                        text = path.read_bytes()
                        if SECRET.search(text):
                            raise ValueError("API credential pattern in release: " + name)
                        if "license" in path.name.lower() and b"qwen" in text.lower():
                            raise ValueError("Excluded model license in release: " + name)
                    result[name] = path
                else:
                    raise ValueError("Unsupported release entry: " + str(path))
    if len({name.casefold() for name in result}) != len(result):
        raise ValueError("Case-colliding release paths")
    return result


def verify_auxiliary_weights(files: dict[str, Path]) -> None:
    for name, (length, digest) in AUXILIARY_WEIGHTS.items():
        if name not in files:
            raise ValueError("Missing pinned auxiliary weight: " + name)
        if files[name].stat().st_size != length or sha(files[name]) != digest:
            raise ValueError("Unapproved auxiliary model bytes: " + name)


def verify_sources(files: dict[str, Path]) -> int:
    if not REQUIRED <= files.keys():
        raise ValueError("Missing local GPU components: " + str(sorted(REQUIRED-files.keys())))
    verify_auxiliary_weights(files)
    if sha(files["licenses/local-gpu/llama.cpp/LICENSE.txt"]) != "94f29bbed6a22c35b992c5c6ebf0e7c92f13b836b90f36f461c9cf2f0f1d010d":
        raise ValueError("llama.cpp MIT license text mismatch")
    vision = json.loads(files["Ai/vision/manifest.json"].read_text(encoding="utf-8-sig"))
    if vision.get("repository") != "ggml-org/SmolVLM-256M-Instruct-GGUF" or vision.get("revision") != "b9e4379657e1450d04d02eec8e345667265b0a00":
        raise ValueError("Unexpected bundled vision-model provenance")
    for name in ("vision-model.gguf", "vision-projector.gguf"):
        size, digest = AUXILIARY_WEIGHTS["Ai/vision/"+name]
        if vision["files"][name]["sha256"] != digest or vision["files"][name]["bytes"] != size:
            raise ValueError("Vision provenance differs from the pinned allowlist")
    components = json.loads(files["Ai/components/manifest.json"].read_text(encoding="utf-8-sig"))
    for name in ("ggml-tiny.bin", "ggml-small.bin"):
        if components["files"].get(name) != AUXILIARY_WEIGHTS["Ai/components/"+name][1]:
            raise ValueError("Unexpected Whisper provenance: " + name)
    expected_components = {"Ai/components/manifest.json"}
    for name, digest in components["files"].items():
        safe_name(name)
        if "/" in name or not (name in {"ggml-tiny.bin", "ggml-small.bin"} or name.startswith("LICENSE-")):
            raise ValueError("Unexpected component provenance entry: " + name)
        path = "Ai/components/"+name
        expected_components.add(path)
        if path not in files or sha(files[path]) != digest:
            raise ValueError("Speech component/license hash mismatch: " + name)
    if {name for name in files if name.startswith("Ai/components/")} != expected_components:
        raise ValueError("Unexplained bundled component files")
    archive = ROOT / "downloads/llama-b12020-bin-win-vulkan-x64.zip"
    if not archive.is_file() or sha(archive) != RUNTIME_SHA:
        raise ValueError("Verified official llama.cpp b12020 archive is required; run fetch-gpu-runtime.py")
    expected_runtime = {}
    with zipfile.ZipFile(archive) as package:
        for entry in package.infolist():
            if entry.is_dir():
                continue
            name = PurePosixPath(safe_name(entry.filename)).name
            if name.lower().endswith(".dll") or name == "llama-server.exe" or "LICENSE" in name.upper():
                expected_runtime[name] = hashlib.sha256(package.read(entry)).hexdigest()
    runtime = json.loads(files["Ai/gpu/manifest.json"].read_text(encoding="utf-8-sig"))
    if runtime.get("version") != "b12020" or runtime.get("archiveSha256") != RUNTIME_SHA or runtime.get("files") != expected_runtime:
        raise ValueError("GPU runtime manifest differs from the pinned official archive")
    for name, digest in expected_runtime.items():
        if "Ai/gpu/"+name not in files or sha(files["Ai/gpu/"+name]) != digest:
            raise ValueError("GPU runtime byte mismatch: " + name)
    if {name for name in files if name.startswith("Ai/gpu/")} != {"Ai/gpu/manifest.json"} | {"Ai/gpu/"+name for name in expected_runtime}:
        raise ValueError("Unexplained GPU runtime files")
    generic_docs = 0
    for name, path in files.items():
        for prefix in ("KnowledgeKit/", "docs/obsidian-cloud/"):
            if name.startswith(prefix):
                original = ROOT / "docs/obsidian-cloud" / name[len(prefix):]
                if not original.is_file() or original.read_bytes() != path.read_bytes():
                    raise ValueError("Non-generic/modified workspace kit: " + name)
                generic_docs += 1
    return generic_docs


def audit(root: Path) -> tuple[dict[str, Path], dict[str, str], int]:
    root = root.absolute()
    no_links(root)
    if not root.resolve().is_relative_to(DIST.resolve()):
        raise ValueError("Build must be below this workspace's dist directory")
    files = inventory(root)
    generic = verify_sources(files)
    manifest = json.loads(files[MANIFEST].read_text(encoding="utf-8-sig"))
    if manifest.get("Version") != "4.2.0" or manifest.get("Provider") != "UserSelectedLocalGpu" or manifest.get("TextModelWeightsIncluded") is not False:
        raise ValueError("Unexpected local GPU build identity")
    entries = {safe_name(item["Path"]): item for item in manifest["Files"]}
    if len(entries) != len(manifest["Files"]) or set(files) != set(entries) | {MANIFEST}:
        raise ValueError("Missing, duplicate, or unexplained local GPU build files")
    hashes = {}
    for name, path in files.items():
        digest = sha(path)
        if name in entries and (digest != entries[name]["Sha256"].lower() or path.stat().st_size != entries[name]["Bytes"]):
            raise ValueError("Build manifest mismatch: " + name)
        hashes[name] = digest
    return files, hashes, generic


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--payload", type=Path, required=True)
    parser.add_argument("--write-manifest", action="store_true")
    args = parser.parse_args()
    root = args.payload.absolute()
    no_links(root)
    if not root.resolve().is_relative_to(DIST.resolve()):
        raise ValueError("Build must be below dist")
    if args.write_manifest:
        files = inventory(root)
        verify_sources(files)
        if MANIFEST in files:
            raise ValueError("Refusing to overwrite an existing build manifest")
        manifest = {"Version":"4.2.0", "Provider":"UserSelectedLocalGpu", "GeneratedUtc":datetime.now(timezone.utc).isoformat(),
                    "TextModelWeightsIncluded":False, "BundledAuxiliaryWeights":list(AUXILIARY_WEIGHTS),
                    "GpuRuntime":"llama.cpp b12020 Vulkan", "Files":[{"Path":name, "Bytes":path.stat().st_size, "Sha256":sha(path)} for name,path in sorted(files.items())]}
        with (root/MANIFEST).open("x", encoding="utf8") as output:
            json.dump(manifest, output, indent=2); output.write("\n")
    files, hashes, generic = audit(root)
    print(json.dumps({"auditedFiles":len(files), "manifestSha256":hashes[MANIFEST], "genericKitFilesCompared":generic,
                      "textModelWeightsIncluded":False, "bundledAuxiliaryWeights":list(AUXILIARY_WEIGHTS)}, indent=2))


if __name__ == "__main__":
    main()
