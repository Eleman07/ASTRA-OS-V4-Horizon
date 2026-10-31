"""Package the existing, verified Windows publish for GitHub Releases.

No compilation, application launch, installer execution, or user-profile access.
The existing local distribution and its LOCAL.zip are left intact.
"""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import uuid
import xml.etree.ElementTree as ET
import zipfile
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist"
APP = DIST / "AstraOS-Local"
PREREQUISITES = DIST / "release-prerequisites"
NAME = "ASTRA-OS-V4-HORIZON-win-x64"
TOP = "ASTRA-OS-V4-HORIZON"
GITHUB_ASSET_LIMIT = 2 * 1024**3


def digest(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def encoded(value: object) -> bytes:
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf8")


def collect_licenses() -> dict[str, Path | bytes]:
    """Use the actual restored package versions, without bundling their binaries twice."""
    assets = json.loads((ROOT / "src/Astra.Desktop/obj/project.assets.json").read_text(encoding="utf-8-sig"))
    package_roots = [Path(p) for p in assets["packageFolders"]]
    result: dict[str, Path | bytes] = {}
    inventory = []

    def find_package(relative: str) -> Path:
        folder = next((base / relative for base in package_roots if (base / relative).is_dir()), None)
        if folder is None:
            raise RuntimeError(f"Restored package unavailable for license collection: {relative}")
        return folder

    for identity, metadata in sorted(assets["libraries"].items()):
        if metadata["type"] != "package":
            continue
        folder = find_package(metadata["path"])
        specification = next(folder.glob("*.nuspec"))
        tree = ET.parse(specification)

        def field(name: str) -> str | None:
            node = tree.find(".//{*}" + name)
            return node.text if node is not None else None

        label = identity.replace("/", "-")
        copies = []
        for file in sorted(folder.rglob("*")):
            if not file.is_file() or file.stat().st_size > 2_000_000:
                continue
            if not any(token in file.name.lower() for token in ("license", "third-party", "thirdparty", "notice", "copying", "copyright")):
                continue
            relative = file.relative_to(folder).as_posix()
            name = f"licenses/nuget/{label}/{relative}"
            result[name] = file
            copies.append(name)
        # These family licenses are already distributed with ASTRA's components.
        if not any("license" in PurePosixPath(p).name.lower() for p in copies):
            fallback = None
            if identity.startswith("NAudio."):
                fallback = APP / "Ai/components/LICENSE-NAudio.txt"
            elif identity.startswith("LLamaSharp/"):
                fallback = ROOT / "docs/third-party/llamasharp/LICENSE.txt"
            elif identity.startswith("LLamaSharp.Backend.Cpu/"):
                fallback = APP / "Ai/components/LICENSE-llama.cpp.txt"
            elif identity.startswith("PdfPig/"):
                fallback = APP / "Ai/components/LICENSE-PdfPig.txt"
            elif identity.startswith("Whisper.net."):
                fallback = APP / "Ai/components/LICENSE-whisper.net.txt"
            elif identity.startswith(("Microsoft.Extensions.AI.", "System.Security.", "System.Speech/")):
                fallback = find_package("microsoft.netcore.app.runtime.win-x64/10.0.14") / "LICENSE.TXT"
            if fallback:
                name = f"licenses/nuget/{label}/LICENSE.txt"
                result[name] = fallback
                copies.append(name)
        inventory.append({
            "package": identity,
            "license": field("license"),
            "licenseUrl": field("licenseUrl"),
            "projectUrl": field("projectUrl"),
            "licenseFiles": copies,
        })

    runtime_config = json.loads((APP / "Astra.Desktop.runtimeconfig.json").read_text(encoding="utf8"))
    for framework in runtime_config["runtimeOptions"]["includedFrameworks"]:
        package = {
            "Microsoft.NETCore.App": "microsoft.netcore.app.runtime.win-x64",
            "Microsoft.WindowsDesktop.App": "microsoft.windowsdesktop.app.runtime.win-x64",
        }[framework["name"]]
        folder = find_package(package + "/" + framework["version"])
        for file in folder.iterdir():
            if file.is_file() and any(t in file.name.lower() for t in ("license", "notice")):
                result[f"licenses/dotnet-runtime/{framework['name']}/{file.name}"] = file

    for file in (ROOT / "docs/third-party/draco").iterdir():
        if file.is_file():
            result[f"licenses/draco/{file.name}"] = file
    result["licenses/draco/SOURCE.txt"] = (
        "Draco decoder distributed with Three.js r180.\n"
        "License and authors: https://github.com/google/draco/tree/1.5.7\n"
        "https://raw.githubusercontent.com/google/draco/1.5.7/LICENSE\n"
        "https://raw.githubusercontent.com/google/draco/1.5.7/AUTHORS\n"
    ).encode("utf8")
    result["licenses/dependencies.json"] = encoded({
        "scope": "NuGet packages restored for this build, including build-only dependencies; runtime framework licenses are supplied separately.",
        "packages": inventory,
    })
    return result


def main() -> None:
    if not (APP / "Astra.Desktop.exe").is_file():
        raise RuntimeError("Publish the app with scripts/build-local.ps1 first.")
    required = (
        "Astra.Desktop.exe", "Astra.Desktop.dll", "Astra.Core.dll", "Astra.Desktop.pri",
        "Astra.Desktop.deps.json", "Astra.Desktop.runtimeconfig.json", "App.xbf", "MainWindow.xbf",
        "coreclr.dll", "hostfxr.dll", "Microsoft.UI.Xaml.dll", "WebView2Loader.dll",
        "LLamaSharp.dll", "Ai/embedded-runtime.json", "Ai/components/astra-local-model.gguf",
        "Ai/components/ggml-tiny.bin", "Ai/components/manifest.json",
        "Graphics3D/web/models/CREDITS.md", "Localization/en.json", "Localization/it.json",
    )
    for name in required:
        if not (APP / name).is_file() or (APP / name).stat().st_size == 0:
            raise RuntimeError("Distribution file missing: " + name)
    evidence = json.loads((ROOT / "docs/evidence/new-expansion-artifact-hashes.json").read_text(encoding="utf-8-sig"))
    exe_hash = digest(APP / "Astra.Desktop.exe")
    if exe_hash.lower() != evidence["executableSha256"].lower():
        raise RuntimeError("The app executable differs from the verified October 28 build.")

    payload: dict[str, Path | bytes] = {}
    excluded = []
    for file in sorted(APP.rglob("*")):
        if file.is_symlink():
            raise RuntimeError("Symlinks are not package payload: " + str(file))
        if not file.is_file():
            continue
        name = file.relative_to(APP).as_posix()
        parts = [part.lower() for part in file.relative_to(APP).parts]
        if any(part in ("ebwebview", "webview2", "__pycache__", ".git", "logs") or part.startswith("qa-") for part in parts):
            raise RuntimeError("Runtime cache/development data found in distribution: " + name)
        if file.suffix.lower() in (".dpapi", ".log") or file.name.lower() == "preferences.json":
            raise RuntimeError("User data found in distribution: " + name)
        if file.suffix.lower() == ".pdb" or name == "INSTALLAZIONE.md":
            excluded.append(name)
            continue
        payload[name] = file

    documents = {
        "README.md": ROOT / "README.md",
        "RELEASE-NOTES.md": ROOT / "docs/RELEASE-NOTES-2026-10-28.md",
        "OPENROUTER-AI-GUIDE.md": ROOT / "docs/OPENROUTER-AI-GUIDE.md",
        "SECOND-BRAIN-GUIDE.md": ROOT / "docs/SECOND-BRAIN-GUIDE.md",
        "GITHUB-PUBLISHING.md": ROOT / "docs/GITHUB-PUBLISHING.md",
        "THIRD-PARTY-NOTICES.md": ROOT / "docs/THIRD-PARTY-NOTICES.md",
        "prerequisites/README.md": ROOT / "docs/PREREQUISITES-README.md",
    }
    payload.update(documents)
    payload.update(collect_licenses())

    provenance = json.loads((PREREQUISITES / "sources.json").read_text(encoding="utf-8-sig"))
    installers = {entry["file"]: entry for entry in provenance["installers"]}
    for name in ("MicrosoftEdgeWebView2RuntimeInstallerX64.exe", "vc_redist.x64.exe"):
        entry = installers[name]
        file = PREREQUISITES / name
        if entry["signatureStatus"] != "Valid" or "O=Microsoft Corporation," not in entry["publisher"]:
            raise RuntimeError("Missing Microsoft signature verification: " + name)
        if file.stat().st_size != entry["bytes"] or digest(file).lower() != entry["sha256"].lower():
            raise RuntimeError("Installer differs from verified Microsoft download: " + name)
        payload["prerequisites/" + name] = file
    payload["prerequisites/sources.json"] = encoded(provenance)

    seen = set()
    hashes = {}
    for name, source in sorted(payload.items()):
        path = PurePosixPath(name)
        if path.is_absolute() or ".." in path.parts or "\\" in name or "\n" in name or ":" in name:
            raise RuntimeError("Unsafe archive path: " + name)
        if name.casefold() in seen:
            raise RuntimeError("Duplicate Windows archive path: " + name)
        seen.add(name.casefold())
        hashes[name] = digest(source) if isinstance(source, Path) else hashlib.sha256(source).hexdigest()
    manifest = "".join(f"{value}  {name}\n" for name, value in sorted(hashes.items())).encode("utf8")
    payload["FILES.sha256"] = manifest
    expected = dict(hashes, **{"FILES.sha256": hashlib.sha256(manifest).hexdigest()})
    archive = DIST / (NAME + ".zip")
    temporary = DIST / ("." + NAME + "-" + uuid.uuid4().hex + ".tmp")
    print(f"Packaging {len(payload)} files into {archive.name}", flush=True)
    try:
        with zipfile.ZipFile(temporary, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as package:
            for index, (name, source) in enumerate(sorted(payload.items()), 1):
                target = TOP + "/" + name
                if isinstance(source, Path):
                    package.write(source, target)
                else:
                    package.writestr(target, source)
                if index % 100 == 0:
                    print(f"  Added {index}/{len(payload)} files", flush=True)
        if temporary.stat().st_size >= GITHUB_ASSET_LIMIT:
            raise RuntimeError("ZIP exceeds GitHub's per-asset limit of less than 2 GiB.")
        print("Reading and verifying every ZIP entry...", flush=True)
        with zipfile.ZipFile(temporary) as package:
            entries = package.infolist()
            if len(entries) != len(payload) or set(package.namelist()) != {TOP + "/" + n for n in payload}:
                raise RuntimeError("ZIP file inventory differs from expected payload.")
            for entry in entries:
                name = entry.filename.removeprefix(TOP + "/")
                with package.open(entry) as stream:
                    actual = hashlib.file_digest(stream, "sha256").hexdigest()
                if actual != expected[name]:
                    raise RuntimeError("ZIP payload hash mismatch: " + name)
        os.replace(temporary, archive)
    finally:
        if temporary.exists():
            temporary.unlink()

    archive_hash = digest(archive)
    (DIST / (NAME + ".zip.sha256")).write_text(f"{archive_hash}  {archive.name}\n", encoding="utf8")
    (DIST / (NAME + "-FILES.sha256")).write_bytes(manifest)
    shutil.copyfile(documents["README.md"], DIST / (NAME + "-README.md"))
    shutil.copyfile(documents["RELEASE-NOTES.md"], DIST / (NAME + "-RELEASE-NOTES.md"))
    uncompressed = sum(source.stat().st_size if isinstance(source, Path) else len(source) for source in payload.values())
    report = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "archive": archive.name,
        "archiveSha256": archive_hash,
        "executableSha256": exe_hash,
        "archiveBytes": archive.stat().st_size,
        "uncompressedBytes": uncompressed,
        "fileCount": len(payload),
        "allPayloadHashesMatch": True,
        "githubAssetUnder2GiB": True,
        "englishReadmeIncluded": True,
        "microsoftOfflineInstallersVerified": True,
        "excludedDevelopmentFiles": excluded,
        "appBinariesChanged": False,
    }
    (DIST / (NAME + "-verification.json")).write_bytes(encoded(report))
    (ROOT / "docs/evidence/github-release-package.json").write_bytes(encoded(report))
    print(json.dumps(report, indent=2), flush=True)


if __name__ == "__main__":
    main()
