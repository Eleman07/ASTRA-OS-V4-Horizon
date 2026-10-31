"""Read-only GitHub checks for this binary release and its six approved assets."""
from pathlib import Path
from datetime import datetime,timezone
import argparse
import hashlib
import json
import subprocess

ROOT=Path(__file__).resolve().parents[1]
REPO='Eleman07/ASTRA-OS-V4-Horizon'
TAG='v4.0.0-assistant-system.20261029'
MAIN='4e0a1f01514b25cf5f916baf2c8607134fc2969a'
NAME='ASTRA-OS-V4-HORIZON-assistant-system-update-win-x64'

def api(path):
    result=subprocess.run(['gh','api',path],check=True,capture_output=True,text=True,encoding='utf8')
    return json.loads(result.stdout)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--published',action='store_true');args=parser.parse_args()
    repository=api('repos/'+REPO)
    assert repository['private'] is True,'Repository visibility changed'
    assert api('repos/'+REPO+'/branches/main')['commit']['sha']==MAIN,'main changed'
    releases=api('repos/'+REPO+'/releases')
    release=next(r for r in releases if r['tag_name']==TAG)
    assert release['draft'] is (not args.published),'Unexpected publication state'
    assert release['prerelease'] is True,'Release must remain a pre-release'
    assert release['target_commitish']==MAIN,'Unexpected release target'
    paths=[ROOT/'dist'/(NAME+suffix) for suffix in ['.zip','.zip.sha256','-README.md','-verification.json']]
    paths+=[ROOT/'docs/ASSISTANT-SYSTEM-UPDATE.md',ROOT/'docs/FEATURE-CATALOG.md']
    remote={asset['name']:asset for asset in release['assets']}
    assert len(remote)==len(paths),'Unexpected release asset count'
    evidence=[]
    for path in paths:
        with path.open('rb') as stream:sha=hashlib.file_digest(stream,'sha256').hexdigest()
        asset=remote[path.name]
        assert asset['state']=='uploaded' and asset['size']==path.stat().st_size and asset['digest']=='sha256:'+sha,'Asset mismatch: '+path.name
        evidence.append({'name':path.name,'bytes':path.stat().st_size,'sha256':sha,'url':asset['browser_download_url']})
    assert '376 Core checks' in release['body'] and 'FEATURE-CATALOG.md' in release['body'],'Release description missing verification/catalog'
    report={'verifiedAt':datetime.now(timezone.utc).isoformat(),'repository':REPO,'private':True,'releaseId':release['id'],'tag':TAG,'url':release['html_url'],'draft':release['draft'],'prerelease':release['prerelease'],'publishedAt':release['published_at'],'allSixAssetsVerified':True,'mainUnchanged':True,'sourceCodeUploaded':False,'assets':evidence}
    name='github-assistant-published.json' if args.published else 'github-assistant-upload-verified.json'
    (ROOT/'docs/evidence'/name).write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
    print(json.dumps({k:v for k,v in report.items() if k!='assets'},indent=2))

if __name__=='__main__':main()
