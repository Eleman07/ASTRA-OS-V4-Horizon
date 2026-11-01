"""Original educational Ariane 64 mesh. Public dimensions, never an ESA CAD asset.

Sources: ESA Ariane 6 overview and 'what's it made of?' (28 Oct 2026).
62 m total, 5.4 m central diameter, 32 m main stage, 20 m fairing.
Four P120C motor bodies: 13.5 m x 3.4 m. All local detail is illustrative.
"""
import json,math,struct,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'src/Astra.Desktop/Graphics3D/web/models'
buffer=bytearray();views=[];accessors=[];meshes=[];nodes=[]
materials=[dict(name=name,pbrMetallicRoughness=dict(baseColorFactor=color,metallicFactor=metal,roughnessFactor=rough),doubleSided=True) for name,color,metal,rough in [
 ('Ceramic white',[.91,.93,.94,1],.12,.4),('Carbon casing',[.032,.043,.055,1],.45,.52),('Engine alloy',[.16,.19,.23,1],.88,.26),('Thermal insulation',[.6,.42,.19,1],.5,.5),('Blue identification',[.03,.16,.36,1],.2,.38)]]
def array(data,kind,components):
    while len(buffer)%4:buffer.append(0)
    start=len(buffer);fmt='f' if kind==5126 else 'I';buffer.extend(struct.pack('<'+str(len(data))+fmt,*data));view=len(views);views.append(dict(buffer=0,byteOffset=start,byteLength=len(buffer)-start))
    a=dict(bufferView=view,componentType=kind,count=len(data)//components,type={1:'SCALAR',3:'VEC3'}[components])
    if components==3:a.update(min=[min(data[i::3]) for i in range(3)],max=[max(data[i::3]) for i in range(3)])
    accessors.append(a);return len(accessors)-1
def lathe(name,profile,material,x=0,z=0,segments=64):
    positions=[];normals=[];indices=[]
    for i,(y,r) in enumerate(profile):
        prev=profile[max(0,i-1)];nxt=profile[min(len(profile)-1,i+1)];dy=nxt[0]-prev[0];dr=nxt[1]-prev[1];length=math.hypot(dy,dr) or 1
        for j in range(segments+1):
            a=j*2*math.pi/segments;c=math.cos(a);s=math.sin(a);positions.extend([x+r*c,y,z+r*s]);normals.extend([dy/length*c,-dr/length,dy/length*s])
    for i in range(len(profile)-1):
        for j in range(segments):
            a=i*(segments+1)+j;b=a+segments+1;indices.extend([a,b,a+1,a+1,b,b+1])
    mesh=dict(name=name,primitives=[dict(attributes=dict(POSITION=array(positions,5126,3),NORMAL=array(normals,5126,3)),indices=array(indices,5125,1),material=material)])
    nodes.append(dict(name=name,mesh=len(meshes)));meshes.append(mesh)
def band(name,y,r,material=1,x=0,z=0,height=.16):lathe(name,[(y,r),(y+height,r)],material,x,z)
# Main shell, insulation sections, interstage and long fairing.
lathe('Main cryogenic stage',[(2,2.4),(3,2.7),(31.8,2.7),(32,2.68)],0)
lathe('Upper stage shroud',[(32,2.7),(41.8,2.7),(42,2.7)],0)
profile=[(42,2.7),(52.3,2.7)]
for i in range(1,49):
    t=i/48;profile.append((52.3+9.7*t,2.7*math.cos(t*math.pi/2)))
lathe('Twenty metre payload fairing',profile,0,segments=96)
for y in [3,9.2,17.8,27,31.8,32.2,40.6,41.85]:band('Stage joint',y,2.718,2,height=.10)
for y in [30.8,32.5,40.8]:band('Interstage composite ring',y,2.73,1,height=.32)
band('European blue identification band',45,2.72,4,height=.85)
# Vulcain bell: shaped nozzle, lip, cooling-channel rings; dimensions illustrative.
lathe('Vulcain 2.1 nozzle',[(.1,1.05),(.3,1.02),(.65,.91),(1.1,.71),(1.5,.44),(1.8,.29),(2.3,.32),(2.6,.6)],2)
for y in [0.15,.35,.55,.75,.95,1.15,1.35]:band('Vulcain nozzle cooling ring',y,1.05-.48*y,2,height=.045)
lathe('Core thrust structure',[(2,2.3),(2.8,2.7)],1)
# Four complete booster assemblies around the central core. Motor dimensions from ESA.
for n in range(4):
    angle=(n+.5)*math.pi/2;x=4.38*math.cos(angle);z=4.38*math.sin(angle)
    lathe(f'P120C motor {n+1}',[(3.2,1.7),(16.7,1.7)],0,x,z)
    tip=[(16.7,1.7),(18.2,1.62),(20,1.28),(21.5,.68),(22.5,.04)]
    lathe(f'Booster ogive {n+1}',tip,0,x,z)
    lathe(f'Booster aft skirt {n+1}',[(1.6,1.46),(2.4,1.7),(3.2,1.7)],1,x,z)
    lathe(f'Booster nozzle {n+1}',[(0,.83),(.4,.74),(1.1,.4),(1.7,.36)],2,x,z)
    for y in [3.3,4.4,10.1,15.9,16.6]:band('Booster segment interface',y,1.715,2,x,z,height=.09)
    for y in [5.2,13.5]:band('Booster tracking band',y,1.72,1,x,z,height=.24)
    # Attachments modeled as compact radial tie-rods using a lathe with transformed nodes.
    for y in [5,17]:
        lathe('Booster attachment',[(0,.19),(1.2,.19)],2,segments=16)
        nodes[-1].update(translation=[2.5*math.cos(angle),y,2.5*math.sin(angle)],rotation=[math.sin(angle/2),0,-math.cos(angle/2),0])
# Feed/umbilical raceway; elongated small pipes and flanges.
lathe('Core external service conduit',[(5,.10),(39,.10)],2,2.68,.28,segments=16)
for y in range(5,40,3):band('Service conduit clamp',y,.15,2,2.68,.28,height=.08)
doc=dict(asset=dict(version='2.0',generator='ASTRA OS educational parametric reconstruction',copyright='Original mesh: ASTRA OS. Vehicle and factual dimensions: ESA / ArianeGroup. No endorsement.'),scene=0,scenes=[dict(nodes=list(range(len(nodes))))],nodes=nodes,meshes=meshes,materials=materials,buffers=[dict(byteLength=len(buffer))],bufferViews=views,accessors=accessors,extras=dict(units='metres',vehicle='Ariane 64 with P120C, long fairing',height=62,limitations='External educational reconstruction; auxiliary details inferred. No official CAD, internal engineering, mass properties, certified launch data or ESA endorsement.',sources=['https://www.esa.int/Enabling_Support/Space_Transportation/Launch_vehicles/Ariane_6_overview','https://www.esa.int/Enabling_Support/Space_Transportation/Ariane/Ariane_6_what_s_it_made_of']))
js=json.dumps(doc,separators=(',',':')).encode();js+=b' ' *(-len(js)%4);buffer.extend(b'\0'*(-len(buffer)%4));glb=struct.pack('<III',0x46546c67,2,28+len(js)+len(buffer))+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(buffer),0x004e4942)+buffer
OUT.mkdir(parents=True,exist_ok=True);path=OUT/'ariane64.glb';path.write_bytes(glb)
catalog=json.loads((OUT/'catalog.json').read_text(encoding='utf8'));catalog=[m for m in catalog if m['id']!='ariane64'];catalog.append(dict(id='ariane64',name='Ariane 6 / Ariane 64 · ESA',file=path.name,tools=['gnc','orbit'],author='Mesh: ASTRA OS · Dati: ESA / ArianeGroup',license='Original educational reconstruction; ASTRA project license',source=doc['extras']['sources'][1],licenseUrl='https://www.esa.int/Terms_and_conditions',download='generated by scripts/build-ariane6-model.py',sha256=hashlib.sha256(glb).hexdigest()))
(OUT/'catalog.json').write_text(json.dumps(catalog,indent=2,ensure_ascii=False),encoding='utf8')
(OUT/'licenses/ariane64.md').write_text('# Ariane 6 / Ariane 64\n\nRicostruzione geometrica originale ASTRA OS, generata da scripts/build-ariane6-model.py e distribuita con la licenza del progetto. Non è un modello CAD ufficiale ESA. Nessun logo ESA viene riprodotto e non è implicata approvazione da parte di ESA, CNES, ArianeGroup o altri enti.\n\nFonti dei dati dimensionali e della configurazione:\n'+''.join('- '+url+'\n' for url in doc['extras']['sources'])+'\nAltezza 62 m; diametro centrale 5,4 m; stadio principale 32 m; coiffe 20 m; quattro booster con corpo motore 13,5 × 3,4 m. Profili di transizione, posizioni delle giunzioni, tubazioni, ugelli e attacchi sono approssimati a scopo visivo. La geometria non fornisce masse, inerzie o prestazioni del vero lanciatore. I risultati visualizzati derivano esclusivamente dal solutore e dai parametri mostrati da ASTRA.\n',encoding='utf8')
print('Ariane64:',len(glb),'bytes,',len(meshes),'meshes')

credits=OUT/'CREDITS.md'
text=credits.read_text(encoding='utf8').split('\n## Ariane 6 / Ariane 64')[0]
credits.write_text(text+'\n## Ariane 6 / Ariane 64\n\nMesh originale ASTRA OS: ricostruzione didattica della configurazione a quattro booster P120C, non CAD ufficiale. Dimensioni e configurazione: ESA / ArianeGroup. Dettagli non pubblicati approssimati. Nessun logo ESA riprodotto; nessuna approvazione implicita. [Fonte ESA]('+doc['extras']['sources'][1]+'). Specifiche, limiti e licenza in `licenses/ariane64.md`.\n',encoding='utf8')
