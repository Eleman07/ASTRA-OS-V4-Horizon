"""Collect cached dependency licenses for a cloud payload, without downloading.

The default is a dry run. --write copies license/notice documents only; it does
not rebuild binaries, regenerate the payload manifest, or run any installer.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import stat
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist"
PROJECTS = ("Astra.Desktop", "Astra.Core", "Astra.AgentWorker", "Astra.AgentSupervisor")
TOKENS = ("license", "third-party", "thirdparty", "notice", "copying", "copyright")


def encoded(value: object) -> bytes:
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf8")


def safe(value: str) -> str:
    path = PurePosixPath(value)
    if path.is_absolute() or any(p in {"", ".", ".."} for p in value.split("/")) or ":" in value or "\\" in value or any(ord(c) < 32 for c in value):
        raise ValueError("Unsafe license destination")
    return value


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--payload", type=Path, default=DIST / "AstraOS-Cloud-4.2.0-Final")
    parser.add_argument("--write", action="store_true")
    args = parser.parse_args()
    payload = args.payload.resolve(strict=True)
    if not payload.is_relative_to(DIST.resolve()) or not payload.is_dir():
        raise ValueError("Payload must be a directory below this workspace's dist")

    packages: dict[str, dict] = {}
    package_roots: list[Path] = []
    for project in PROJECTS:
        assets = json.loads((ROOT / "src" / project / "obj/project.assets.json").read_text(encoding="utf-8-sig"))
        package_roots.extend(Path(p) for p in assets["packageFolders"])
        for identity, metadata in assets["libraries"].items():
            if metadata["type"] == "package":
                packages[identity] = metadata

    def package_folder(relative: str) -> Path:
        for base in package_roots:
            candidate = base / relative
            if candidate.is_dir():
                return candidate
        raise RuntimeError("Restored package unavailable: " + relative)

    runtime_packages: list[tuple[str, Path]] = []
    frameworks = json.loads((payload / "Astra.Desktop.runtimeconfig.json").read_text(encoding="utf8"))["runtimeOptions"]["includedFrameworks"]
    for framework in frameworks:
        family = {"Microsoft.NETCore.App": "microsoft.netcore.app.runtime.win-x64", "Microsoft.WindowsDesktop.App": "microsoft.windowsdesktop.app.runtime.win-x64"}[framework["name"]]
        runtime_packages.append((framework["name"], package_folder(family + "/" + framework["version"])))
    runtime_mit = next(folder / "LICENSE.TXT" for name, folder in runtime_packages if name == "Microsoft.NETCore.App")
    shipped: set[str] = set()
    for file in payload.glob("Astra.*.deps.json"):
        deps = json.loads(file.read_text(encoding="utf8"))
        shipped.update(identity for identity, info in deps["libraries"].items() if info.get("type") == "package")
    if not shipped <= packages.keys():
        raise RuntimeError("Shipped dependencies are missing restored metadata: " + ", ".join(sorted(shipped - packages.keys())))

    documents: dict[str, bytes] = {}

    def add(name: str, source: Path | bytes) -> str:
        name = safe(name)
        if isinstance(source, Path):
            if not source.is_file() or source.is_symlink() or source.stat().st_size > 2_000_000:
                raise RuntimeError("Invalid cached license document: " + source.name)
            data = source.read_bytes()
        else:
            data = source
        if name in documents and documents[name] != data:
            raise RuntimeError("Conflicting license destination: " + name)
        documents[name] = data
        return name

    inventory = []
    missing = []
    for identity, metadata in sorted(packages.items()):
        folder = package_folder(metadata["path"])
        specification = next(folder.glob("*.nuspec"))
        tree = ET.parse(specification)

        def field(name: str) -> str | None:
            node = tree.find(".//{*}" + name)
            return node.text if node is not None else None

        base = "licenses/nuget/" + identity.replace("/", "-") + "/"
        copies = []
        for file in sorted(folder.rglob("*")):
            if file.is_file() and file.stat().st_size <= 2_000_000 and any(token in file.name.lower() for token in TOKENS):
                if not file.resolve().is_relative_to(folder.resolve()):
                    raise RuntimeError("Linked cached license document")
                copies.append(add(base + file.relative_to(folder).as_posix(), file))
        fallback = None
        fallback_note = None
        if not any("license" in PurePosixPath(p).name.lower() for p in copies):
            if identity.startswith("LLamaSharp/"):
                fallback = ROOT / "docs/third-party/llamasharp/LICENSE.txt"
            elif identity.startswith("NAudio."):
                fallback = ROOT / "src/Astra.Desktop/Ai/components/LICENSE-NAudio.txt"
            elif identity.startswith("PdfPig/"):
                fallback = ROOT / "src/Astra.Desktop/Ai/components/LICENSE-PdfPig.txt"
            elif identity.startswith("Whisper.net."):
                fallback = ROOT / "src/Astra.Desktop/Ai/components/LICENSE-whisper.net.txt"
            elif field("license") == "MIT":
                fallback = runtime_mit
                fallback_note = "MIT text from the bundled .NET runtime; this package's own copyright and license declaration are retained in PACKAGE.nuspec and this inventory."
            if fallback:
                copies.append(add(base + "LICENSE.txt", fallback))
        add(base + "PACKAGE.nuspec", specification)
        if not copies:
            missing.append({"package": identity, "presentInApplicationDeps": identity in shipped, "licenseUrl": field("licenseUrl")})
        inventory.append({"package": identity, "license": field("license"), "licenseUrl": field("licenseUrl"), "projectUrl": field("projectUrl"), "copyright": field("copyright"), "presentInApplicationDeps": identity in shipped, "licenseFiles": copies, "fallbackNote": fallback_note})

    for name, folder in runtime_packages:
        for file in sorted(folder.iterdir()):
            if file.is_file() and any(token in file.name.lower() for token in TOKENS):
                add("licenses/dotnet-runtime/" + name + "/" + file.name, file)
    for file in sorted((ROOT / "docs/third-party/draco").iterdir()):
        if file.is_file():
            add("licenses/draco/" + file.name, file)
    add("licenses/draco/SOURCE.txt", b"Draco decoder distributed with Three.js r180.\nLicense and authors: https://github.com/google/draco/tree/1.5.7\n")
    add("licenses/dependencies.json", encoded({"scope": "Current restored Desktop, Core, Worker and Supervisor package identities, including build-only dependencies; application dependency membership is recorded separately. Runtime framework licenses are supplied separately. No user-profile paths are recorded.", "packages": inventory, "packagesWithoutAvailableLicenseText": missing}))
    if any(item["presentInApplicationDeps"] for item in missing):
        raise RuntimeError("A package in the application dependency graph has no cached license/notice text: " + json.dumps(missing))

    if args.write:
        # Check every destination component before any writes. Do not follow an
        # existing junction/symlink in an output tree shared with other tools.
        for name in documents:
            destination = payload / name
            for component in (destination, *destination.parents):
                if component == payload.parent:
                    break
                if component.exists() and (component.is_symlink() or getattr(component.stat(follow_symlinks=False), "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT):
                    raise RuntimeError("Linked license destination")
            if not destination.resolve().is_relative_to(payload):
                raise RuntimeError("License destination escapes the payload")
        for name, data in sorted(documents.items()):
            destination = payload / name
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(data)
            if hashlib.sha256(destination.read_bytes()).digest() != hashlib.sha256(data).digest():
                raise RuntimeError("License copy verification failed")
    print(json.dumps({"mode": "write" if args.write else "dry-run", "packages": len(packages), "licenseDocuments": len(documents), "bytes": sum(map(len, documents.values())), "packagesWithoutAvailableLicenseText": missing, "applicationBinariesChanged": False, "networkUsed": False}, indent=2))


if __name__ == "__main__":
    main()
