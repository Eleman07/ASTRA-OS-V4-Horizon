"""Package only the tested portable Assistant/System release; no remote writes."""
from pathlib import Path
import importlib.util

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('astra_binary_package',ROOT/'scripts/package-dock-update.py')
package=importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)
package.APP=ROOT/'dist/AstraOS-Assistant-System-Update'
package.NAME='ASTRA-OS-V4-HORIZON-assistant-system-update-win-x64'
package.EVIDENCE=ROOT/'docs/evidence/assistant-system-artifact.json'
package.RELEASE_NOTES=ROOT/'docs/RELEASE-NOTES-ASSISTANT-SYSTEM.md'
package.VERIFICATION_NAME='ASSISTANT-SYSTEM-VERIFICATION.json'
package.REQUIRE_MODEL_TEST=True
package.EXTRA_DOCS={'ASSISTANT-SYSTEM-GUIDE.md':ROOT/'docs/ASSISTANT-SYSTEM-UPDATE.md','GITHUB-UPDATE.md':ROOT/'docs/ASSISTANT-RELEASE-GITHUB.md'}
for file in (ROOT/'sdk/assistant-skills').iterdir():
    if file.is_file():package.EXTRA_DOCS['sdk/assistant-skills/'+file.name]=file

if __name__=='__main__':package.binary_update()
