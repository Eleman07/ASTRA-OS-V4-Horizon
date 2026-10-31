"""Record evidence for the native-tested portable build; does not run or publish it."""
from pathlib import Path
from datetime import datetime,timezone
import hashlib
import json

ROOT=Path(__file__).resolve().parents[1]
APP=ROOT/'dist/AstraOS-Assistant-System-Update'
REPORT=ROOT/'docs/evidence/qa-assistant-embedded-final/assistant-system-smoke.json'
EVIDENCE=ROOT/'docs/evidence/assistant-system-artifact.json'
TEST_LOGS=['assistant-system-core.log','assistant-system-static.log','assistant-system-brain-regression.log']

def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def main():
    report=json.loads(REPORT.read_text(encoding='utf8'))
    if report.get('passed') is not True or report.get('modelTested') is not True or any(c['status']!='PASS' for c in report['checks']):raise RuntimeError('Native checks with actual inference must all pass.')
    critical=['Astra.Desktop.exe','Astra.Desktop.dll','Astra.Core.dll','Astra.Desktop.pri','MainWindow.xbf','App.xbf','Astra.Desktop.runtimeconfig.json','Ai/embedded-runtime.json','Ai/components/manifest.json','Localization/en.json','Localization/sources.json','Localization/sources-csharp.json']
    if any((APP/name).stat().st_mtime>REPORT.stat().st_mtime for name in critical):raise RuntimeError('Publish files changed after native QA.')
    embedded=json.loads((APP/'Ai/embedded-runtime.json').read_text(encoding='utf8'))
    critical+=list(embedded['files'])
    critical+=[p.relative_to(APP).as_posix() for p in (APP/'AssistantSkills').glob('*.json')]
    for name,expected in embedded['files'].items():
        if sha(APP/name)!=expected:raise RuntimeError('Embedded library changed: '+name)
    tests={}
    for file in TEST_LOGS:
        path=ROOT/'docs/evidence'/file;text=path.read_text(encoding='utf-8-sig')
        if 'FAIL:' in text or 'FAILED (' in text:raise RuntimeError('Unresolved failure: '+file)
        tests[file]={'sha256':sha(path),'passLines':sum(line.startswith('PASS:') for line in text.splitlines())}
    value={'generated':datetime.now(timezone.utc).isoformat(),'releaseReady':True,'app':APP.relative_to(ROOT).as_posix(),'qaReport':REPORT.relative_to(ROOT).as_posix(),'qaReportSha256':sha(REPORT),'nativeChecks':len(report['checks']),'backend':'LLamaSharp.Backend.Cpu/0.30.0, in-process, no server executable','testedFiles':{name:sha(APP/name) for name in dict.fromkeys(critical)},'tests':tests}
    EVIDENCE.write_text(json.dumps(value,indent=2)+'\n',encoding='utf8')
    print('Release ready:',len(report['checks']),'native checks;',len(value['testedFiles']),'pinned app/resource files.')

if __name__=='__main__':main()
