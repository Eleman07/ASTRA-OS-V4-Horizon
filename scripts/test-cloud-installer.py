"""Exercise setup/uninstall with a unique, workspace-contained QA identity.

This verifies package installation only. It does not launch ASTRA, execute the
Microsoft prerequisites, contact a model provider, or certify live agent tasks.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time
import uuid
import winreg

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist"


def sha(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def registry_exists(key: str) -> bool:
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, key, 0,
                            winreg.KEY_READ | winreg.KEY_WOW64_64KEY):
            return True
    except FileNotFoundError:
        return False


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, default=DIST / "ASTRA-OS-V4-HORIZON-Setup-4.2.0-cloud-win-x64-build.json")
    args = parser.parse_args()
    build = json.loads(args.build.read_text(encoding="utf8"))
    setup = DIST / build["installer"]
    assert setup.resolve().parent == DIST.resolve()
    assert sha(setup) == build["sha256"]
    token = uuid.uuid4().hex
    qa_root = DIST / "installer-cloud-qa"
    target = qa_root / token / "AstraCloud"
    assert target.resolve().is_relative_to(qa_root.resolve())
    key = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\ASTRAOSHorizonCloud-QA-" + token
    menu = Path(os.environ["APPDATA"]) / "Microsoft/Windows/Start Menu/Programs" / ("ASTRA OS Horizon Cloud QA-" + token)
    desktop_name = "ASTRA OS Horizon Cloud QA-" + token + ".lnk"
    checks: list[dict[str, str]] = []

    def check(value: object, name: str) -> None:
        if not value:
            raise RuntimeError("FAIL: " + name)
        checks.append({"name": name, "status": "PASS"})
        print("PASS:", name, flush=True)

    check(not target.exists() and not registry_exists(key) and not menu.exists(),
          "QA installation uses an unused folder, registry identity and Start menu")
    state = {"token": token, "directory": str(target), "registryKey": key,
             "menu": str(menu), "desktopName": desktop_name,
             "installerSha256": build["sha256"]}
    state_path = qa_root / token / "state.json"
    state_path.parent.mkdir(parents=True)
    state_path.write_text(json.dumps(state, indent=2) + "\n", encoding="utf8")
    command = '"' + str(setup) + '" /S /ASTRA-QA=' + token + ' /D=' + str(target)
    installed = subprocess.run(command, shell=False, timeout=240)
    check(installed.returncode == 0, "Real silent setup exits successfully")
    for name, expected in build["payloadHashes"].items():
        file = target / name
        check_path = file.resolve()
        if not check_path.is_relative_to(target.resolve()) or not file.is_file() or sha(file) != expected:
            raise RuntimeError("Installed payload mismatch: " + name)
    check(True, "Every installed payload file matches the release build")
    check((target / "Uninstall.exe").is_file() and (target / ".astra-installation.ini").is_file(),
          "Uninstaller and installation identity are created")
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, key, 0,
                        winreg.KEY_READ | winreg.KEY_WOW64_64KEY) as handle:
        check(winreg.QueryValueEx(handle, "InstallLocation")[0] == str(target),
              "Per-user uninstall registration points to the isolated folder")
        check(winreg.QueryValueEx(handle, "UninstallString")[0] == '"' + str(target / "Uninstall.exe") + '"',
              "Registered uninstall command is quoted")
        check(winreg.QueryValueEx(handle, "DisplayVersion")[0] == "4.2.0.0",
              "Windows installed-app version is 4.2.0.0")
    check(all((menu / name).is_file() for name in ["ASTRA OS Horizon Cloud.lnk", "Prerequisites.lnk", "Uninstall.lnk"]),
          "Isolated Start menu shortcuts are created")
    keep = target / "keep-my-file.txt"
    keep.write_text("Unknown user-owned files must survive uninstall.\n", encoding="utf8")
    keep_sha = sha(keep)
    removed = subprocess.run([str(target / "Uninstall.exe"), "/S"], timeout=180)
    check(removed.returncode == 0, "Real silent uninstaller exits successfully")
    for _ in range(60):
        if not registry_exists(key) and not (target / "Uninstall.exe").exists():
            break
        time.sleep(1)
    check(all(not (target / name).exists() for name in build["payloadHashes"]),
          "Uninstall removes every installed payload file")
    check(not registry_exists(key) and not menu.exists() and not (target / ".astra-installation.ini").exists(),
          "Uninstall removes its isolated integration and marker")
    check(keep.is_file() and sha(keep) == keep_sha,
          "Unknown user-owned file remains unchanged after uninstall")
    report = {"verifiedAt": datetime.now(timezone.utc).isoformat(),
              "installer": setup.name, "bytes": setup.stat().st_size,
              "sha256": sha(setup), "installationTested": True,
              "uninstallationTested": True, "allPayloadHashesMatch": True,
              "installedPayloadCount": build["payloadCount"],
              "applicationLaunched": False, "prerequisitesExecuted": False,
              "liveProviderTested": False, "stableCertification": False,
              "signed": False, "checks": checks}
    output = setup.with_name(setup.stem + "-verification.json")
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf8")
    print("Verification report:", output, flush=True)


if __name__ == "__main__":
    main()
