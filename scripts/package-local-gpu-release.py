"""Audit and ZIP the local GPU build; text-model weights remain user-selected."""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import stat
import uuid
import zipfile
from local_gpu_payload import AUXILIARY_WEIGHTS, DIST, MANIFEST, audit, no_links, safe_name, sha

TOP = "ASTRA-OS-V4-HORIZON"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, default=DIST / "AstraOS-LocalGPU-4.2.0-Final")
    parser.add_argument("--output", type=Path, default=DIST / "ASTRA-OS-V4-HORIZON-4.2.0-local-gpu-win-x64.zip")
    args = parser.parse_args()
    build = args.build.absolute()
    output = args.output.absolute()
    no_links(output.parent)
    if output.parent.resolve() != DIST.resolve() or output.suffix.lower() != ".zip" or output.is_symlink() or output.is_junction():
        raise ValueError("Only an ordinary ZIP destination directly below dist is allowed")
    files, hashes, generic = audit(build)
    temporary = output.with_name(output.name+"."+uuid.uuid4().hex+".tmp")
    try:
        with zipfile.ZipFile(temporary, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as package:
            for name in sorted(files):
                package.write(files[name], TOP+"/"+name)
        with zipfile.ZipFile(temporary) as package:
            entries = package.infolist()
            if len(entries) != len(files) or {entry.filename for entry in entries} != {TOP+"/"+name for name in files}:
                raise ValueError("ZIP inventory differs from the audited local GPU payload")
            for entry in entries:
                safe_name(entry.filename)
                if stat.S_ISLNK(entry.external_attr >> 16):
                    raise ValueError("ZIP contains a link")
                relative = entry.filename[len(TOP)+1:]
                with package.open(entry) as source:
                    if hashlib.file_digest(source,"sha256").hexdigest() != hashes[relative]:
                        raise ValueError("ZIP byte/hash mismatch: "+relative)
        os.replace(temporary,output)
    finally:
        if temporary.exists():
            temporary.unlink()
    digest = sha(output)
    no_links(output.with_suffix(".zip.sha256"))
    no_links(output.with_suffix(".verification.json"))
    output.with_suffix(".zip.sha256").write_text(digest+"  "+output.name+"\n",encoding="utf8")
    report = {"artifact":output.name,"bytes":output.stat().st_size,"sha256":digest,"flavour":"local-gpu",
              "topLevelFolder":TOP,"archiveFiles":len(files),"manifestFilesVerified":len(files)-1,
              "manifestSha256":hashes[MANIFEST],"genericKitFilesCompared":generic,"unexplainedExtras":[],
              "textModelWeightsIncluded":False,"bundledAuxiliaryWeights":list(AUXILIARY_WEIGHTS),
              "noPersonalVault":True,"noCredentialFiles":True,"archivePathsAndEveryEntryHashVerified":True}
    output.with_suffix(".verification.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf8")
    print(json.dumps(report,indent=2),flush=True)


if __name__ == "__main__":
    main()
