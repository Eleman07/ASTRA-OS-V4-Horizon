"""Exercise the real setup using isolated folder, Start menu and registry names.

The /ASTRA-QA identity does not skip installer behavior. It isolates integration
names so testing never replaces an existing user's ASTRA shortcuts/registration.
"""
from pathlib import Path
from datetime import datetime,timezone
import argparse
import hashlib
import json
import os
import subprocess
import time
import uuid
import winreg

ROOT=Path(__file__).resolve().parents[1]
STATE=ROOT/'docs/evidence/installer-test-state.json'
REPORT=ROOT/'docs/evidence/installer-verification.json'
BUILD=ROOT/'docs/evidence/installer-build.json'

def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def registry_exists(key):
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,key,0,winreg.KEY_READ|winreg.KEY_WOW64_64KEY):return True
    except FileNotFoundError:return False

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--uninstall',action='store_true');parser.add_argument('--build',type=Path,default=BUILD);parser.add_argument('--native',type=Path,default=ROOT/'docs/evidence/qa-installed-ai-it/assistant-system-smoke.json');args=parser.parse_args()
    build=json.loads(args.build.read_text(encoding='utf8'))
    setup=ROOT/'dist'/build['installer'];assert sha(setup)==build['sha256']
    checks=[]
    def check(value,name):
        if not value:raise RuntimeError('FAIL: '+name)
        checks.append({'name':name,'status':'PASS'});print('PASS:',name,flush=True)
    if not args.uninstall:
        token=uuid.uuid4().hex
        target=ROOT/'dist/installer-qa'/token/'ASTRA OS Horizon'
        key='Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\ASTRAOSHorizon-QA-'+token
        menu=Path(os.environ['APPDATA'])/'Microsoft/Windows/Start Menu/Programs'/('ASTRA OS Horizon QA-'+token)
        check(not registry_exists(key) and not menu.exists() and not target.exists(),'QA installation identity is isolated and unused')
        command='"'+str(setup)+'" /S /ASTRA-QA='+token+' /D='+str(target)
        (ROOT/'docs/evidence/installer-attempt.json').write_text(json.dumps({'token':token,'directory':str(target),'registryKey':key,'menu':str(menu)},indent=2)+'\n',encoding='utf8')
        result=subprocess.run(command,shell=False,timeout=180)
        print('Setup exit code:',result.returncode,hex(result.returncode&0xffffffff),flush=True)
        check(result.returncode==0,'Real setup completes successfully')
        for name,expected in build['payloadHashes'].items():
            file=target/name
            if not file.is_file() or sha(file)!=expected:raise RuntimeError('Installed payload mismatch: '+name)
        check(True,'Every installed payload file matches the tested release bytes')
        check((target/'Uninstall.exe').is_file() and (target/'.astra-installation.ini').is_file(),'Uninstaller and installation marker are created')
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,key,0,winreg.KEY_READ|winreg.KEY_WOW64_64KEY) as handle:
            check(winreg.QueryValueEx(handle,'InstallLocation')[0]==str(target),'Windows per-user uninstall registration points to this installation')
            check(winreg.QueryValueEx(handle,'UninstallString')[0]=='"'+str(target/'Uninstall.exe')+'"','Registered uninstall command is quoted correctly')
        check(all((menu/name).is_file() for name in ['ASTRA OS Horizon.lnk','Prerequisites.lnk','Uninstall.lnk']),'Start menu app, prerequisite and uninstall shortcuts are created')
        keep=target/'keep-my-file.txt';keep.write_text('User-owned file must survive application removal.\n',encoding='utf8')
        state={'token':token,'directory':str(target),'registryKey':key,'menu':str(menu),'keepSha256':sha(keep),'checks':checks,'installerSha256':build['sha256']}
        STATE.write_text(json.dumps(state,indent=2)+'\n',encoding='utf8')
        print('Installed QA folder:',target,flush=True)
    else:
        state=json.loads(STATE.read_text(encoding='utf8'));target=Path(state['directory'])
        assert target.resolve().is_relative_to((ROOT/'dist/installer-qa').resolve()) and len(state['token'])==32
        check(state['installerSha256']==build['sha256'],'Installed fixture belongs to this exact setup build')
        smoke=args.native
        native=json.loads(smoke.read_text(encoding='utf8'))
        check(native.get('passed') is True and native.get('modelTested') is True,'Installed application passes native UI checks with real local inference')
        profile=smoke.parent;before={p.relative_to(profile).as_posix():sha(p) for p in profile.rglob('*') if p.is_file()}
        result=subprocess.run([str(target/'Uninstall.exe'),'/S'],timeout=120)
        check(result.returncode==0,'Real uninstaller completes successfully')
        for _ in range(60):
            if not registry_exists(state['registryKey']) and not (target/'Uninstall.exe').exists():break
            time.sleep(1)
        check(all(not (target/name).exists() for name in build['payloadHashes']),'Uninstall removes every installed application payload file')
        check(not registry_exists(state['registryKey']) and not Path(state['menu']).exists(),'Uninstall removes only its isolated registration and shortcuts')
        check(sha(target/'keep-my-file.txt')==state['keepSha256'],'Unknown user-owned files in the installation folder are preserved')
        check(all((profile/name).is_file() and sha(profile/name)==expected for name,expected in before.items()),'Account notes, PDFs, chats and preferences survive uninstall unchanged')
        result={'verifiedAt':datetime.now(timezone.utc).isoformat(),'installer':setup.name,'bytes':setup.stat().st_size,'sha256':sha(setup),'releaseReady':True,'installationTested':True,'allPayloadHashesMatch':True,'installedPayloadCount':build['payloadCount'],'nativeChecks':len(native['checks']),'signed':False,'checks':state['checks']+checks}
        REPORT.write_text(json.dumps(result,indent=2)+'\n',encoding='utf8')
        print('PASS: installer ready for the private GitHub release',flush=True)

if __name__=='__main__':main()
