"""Assign stable names to static XAML strings; user-provided data never enters the catalog."""
import json,re
from pathlib import Path
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
X='http://schemas.microsoft.com/winfx/2006/xaml'
UI='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
file=ROOT/'src/Astra.Desktop/MainWindow.xaml'
text=file.read_text(encoding='utf8')
properties={'Text','Content','Header','PlaceholderText','OnContent','OffContent','ToolTipService.ToolTip','AutomationProperties.Name','CloseButtonText','PrimaryButtonText','SecondaryButtonText'}
manifest={};counter=max([int(x) for x in re.findall(r'x:Name="L10n(\d+)"',text)]+[0])
def replace(match):
    global counter
    tag=match.group(0)
    if tag.startswith(('</','<?','<!--')):return tag
    values={}
    element=re.match(r"<([A-Za-z0-9_:]+)",tag).group(1)
    for prop,value in re.findall(r'([\w.:]+)="([^"]*)"',tag):
        if prop=="Text" and element in {"TextBox","NumberBox","RichEditBox"}:continue
        if prop=="Content" and element=="ComboBoxItem":continue
        if prop in properties and value and not value.startswith('{') and re.search(r'[^\W\d_]',value):
            values[prop]=value.replace('&amp;','&').replace('&quot;','"').replace('&apos;',"'").replace('&lt;','<').replace('&gt;','>')
    if not values:return tag
    name=re.search(r'x:Name="([^"]+)"',tag)
    if name:name=name.group(1)
    else:
        counter+=1;name=f'L10n{counter:04}';ending='/>' if tag.endswith('/>') else '>';tag=tag[:-len(ending)]+f' x:Name="{name}"'+ending
    manifest[name]=values;return tag
text=re.sub(r'<[^>]+>',replace,text)
# Re-running keeps existing named objects and cannot introduce duplicate names.
file.write_text(text,encoding='utf8')
directory=ROOT/'src/Astra.Desktop/Localization';directory.mkdir(exist_ok=True)
(directory/'xaml.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
sources=set(json.loads((directory/'sources-csharp.json').read_text(encoding='utf8')))
sources.update(value for item in manifest.values() for value in item.values())
for path in list((ROOT/'src/Astra.Desktop/profiles').glob('*.json'))+list((ROOT/'sdk/declarative').glob('*/recipe.json')):
    data=json.loads(path.read_text(encoding='utf8'))
    for key in ['name','description','category']:
        if isinstance(data.get(key),str):sources.add(data[key])
for model in json.loads((ROOT/'src/Astra.Desktop/Graphics3D/web/models/catalog.json').read_text(encoding='utf8')):sources.add(model['name'])
from html.parser import HTMLParser
class WebLabels(HTMLParser):
    skip=False
    def handle_starttag(self,tag,attrs):
        self.skip=tag in {'script','style'}
        for k,v in attrs:
            if k in {'aria-label','placeholder','title'} and v:sources.add(v)
    def handle_endtag(self,tag):
        if tag in {'script','style'}:self.skip=False
    def handle_data(self,data):
        if not self.skip and data.strip() and re.search(r'[^\W\d_]',data):sources.add(data.strip())
for name in ['index.html','stem.html']:WebLabels().feed((ROOT/'src/Astra.Desktop/Graphics3D/web'/name).read_text(encoding='utf8'))
for name in ['scene.js','stem.js','model-viewer.js']:
    script=(ROOT/'src/Astra.Desktop/Graphics3D/web'/name).read_text(encoding='utf8')
    for literal in re.findall(r'astra[TF]\(("(?:[^"\\]|\\.)*")',script):sources.add(json.loads(literal))
(directory/'sources.json').write_text(json.dumps(sorted(sources),ensure_ascii=False,indent=2),encoding='utf8')
print('Static XAML objects:',len(manifest),'catalog strings:',len(sources))
