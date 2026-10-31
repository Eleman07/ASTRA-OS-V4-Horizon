"""Package the startup fix with separate evidence; keep older release files intact."""
from pathlib import Path
import importlib.util

ROOT=Path(__file__).resolve().parents[1]

def module(name,file):
    spec=importlib.util.spec_from_file_location(name,ROOT/'scripts'/file)
    result=importlib.util.module_from_spec(spec);spec.loader.exec_module(result);return result

record=module('astra_ai_fix_evidence','record-assistant-verification.py')
record.APP=ROOT/'dist/AstraOS-AI-Path-Fix'
record.REPORT=ROOT/'docs/evidence/qa-ai-path-fix-it/assistant-system-smoke.json'
record.EVIDENCE=ROOT/'docs/evidence/ai-path-fix-artifact.json'
record.TEST_LOGS=['ai-path-fix-core.log','ai-path-fix-static.log']
package=module('astra_ai_fix_package','package-dock-update.py')
package.APP=record.APP
package.NAME='ASTRA-OS-V4-HORIZON-ai-startup-fix-win-x64'
package.EVIDENCE=record.EVIDENCE
package.RELEASE_NOTES=ROOT/'docs/AI-STARTUP-FIX.md'
package.VERIFICATION_NAME='AI-STARTUP-VERIFICATION.json'
package.REQUIRE_MODEL_TEST=True
package.EXTRA_DOCS={'ASSISTANT-SYSTEM-GUIDE.md':ROOT/'docs/ASSISTANT-SYSTEM-UPDATE.md','AI-STARTUP-FIX.md':ROOT/'docs/AI-STARTUP-FIX.md','GITHUB-UPDATE.md':ROOT/'docs/ASSISTANT-RELEASE-GITHUB.md'}
for file in (ROOT/'sdk/assistant-skills').iterdir():
    if file.is_file():package.EXTRA_DOCS['sdk/assistant-skills/'+file.name]=file

if __name__=='__main__':record.main();package.binary_update()
