'use strict';
// Desktop metadata is supplied by the native host. Names are text, never markup.
const canvas=document.getElementById('scene'),status=document.getElementById('status'),labels=document.getElementById('labels'),list=document.getElementById('list');
const query=new URLSearchParams(location.search),fps=Number(query.get('fps')),motion=query.get('motion')!=='0';
if(!motion)document.body.classList.add('reduced');
const post=data=>window.chrome?.webview?.postMessage(data);
let renderer,items=[],selected=null,labelNodes=[],pending=false;
function select(id){selected=items.find(x=>x.id===id)||null;document.getElementById('name').textContent=selected?.name||astraT("Seleziona una voce");document.getElementById('meta').textContent=selected?astraF("{0} · {1}\nModifica: {2}\n{3}",selected.folder?astraT("Cartella"):selected.kind,selected.size,selected.modified,selected.location):astraT("Nessuna voce selezionata.");for(const key of ['open','explorer'])document.getElementById(key).disabled=!selected;for(const node of document.querySelectorAll('[data-id]'))node.classList.toggle('selected',node.dataset.id===selected?.id);}
function visible(){const search=document.getElementById('search').value.toLocaleLowerCase();return items.filter(i=>i.name.toLocaleLowerCase().includes(search));}
function renderDesktop(){
  const entries=visible();list.replaceChildren();labels.replaceChildren();labelNodes=[];
  document.getElementById('count').textContent=astraF("{0} voci · {1} cartelle · {2} file",entries.length,entries.filter(i=>i.folder).length,entries.filter(i=>!i.folder).length);
  const rings=Math.max(3,Math.min(6,Math.ceil(entries.length/12))),scene={series:[]},groups=[{name:astraT("Cartelle"),kind:'points',points:[],ids:[],color:renderer.palette.ice},{name:astraT("File"),kind:'points',points:[],ids:[],color:renderer.palette.snow}];
  for(let r=0;r<rings;r++){const radius=1.3+r*.45,tilt=(r%3-1)*.45,p=[];for(let j=0;j<=128;j++){const a=j*Math.PI*2/128;p.push([radius*Math.cos(a),radius*Math.sin(a)*Math.cos(tilt),radius*Math.sin(a)*Math.sin(tilt)]);}scene.series.push({name:astraT("Orbita"),kind:'line',points:p,color:renderer.palette.blue});}
  entries.forEach((item,i)=>{
    const ring=i%rings,slot=Math.floor(i/rings),inRing=Math.ceil((entries.length-ring)/rings),angle=slot*2*Math.PI/Math.max(1,inRing)+ring*.4,radius=1.3+ring*.45,tilt=(ring%3-1)*.45;
    const p=[radius*Math.cos(angle),radius*Math.sin(angle)*Math.cos(tilt),radius*Math.sin(angle)*Math.sin(tilt)],group=groups[item.folder?0:1];group.points.push(p);group.ids.push(item.id);
    const button=document.createElement('button');button.textContent=(item.folder?'▱ ':'• ')+item.name;button.dataset.id=item.id;button.title=item.name;button.onclick=()=>select(item.id);button.ondblclick=()=>post({type:'astra-desktop-open',id:item.id});list.append(button);
    if(i<60){const label=button.cloneNode(true);label.className='node'+(item.folder?' folder':'');label.onclick=()=>select(item.id);label.ondblclick=()=>post({type:'astra-desktop-open',id:item.id});labels.append(label);labelNodes.push({node:label,id:item.id});}
  });
  scene.series.push(...groups.filter(g=>g.points.length));renderer.axes=false;renderer.pointSize=10;pending=true;renderer.setScene(scene,false);select(selected?.id);
  status.textContent=entries.length?'Desktop reale · clic seleziona · doppio clic apre':astraT("Desktop vuoto o nessuna voce corrispondente");
}
try{
  renderer=new AstraRenderer(canvas,{fps,motion,quality:query.get('quality'),onPick:p=>{if(p?.id)select(p.id);},pickFilter:p=>!!p.id,onError:message=>{status.textContent=message;post({type:'astra-orbit-ready',ready:false});},onDraw:r=>{
    const positions=new Map(r.points.filter(p=>p.id).map(p=>[p.id,r.project(p.position)]));
    for(const {node,id} of labelNodes){const p=positions.get(id);node.hidden=!p||!document.getElementById('showLabels').checked;if(p)node.style.transform=`translate(${p.x}px,${p.y}px) translate(-50%,-50%)`;}
    const center=r.project([0,0,0]);if(center)document.getElementById('center').style.transform=`translate(${center.x}px,${center.y}px) translate(-50%,-50%)`;
    if(pending){pending=false;post({type:'astra-orbit-ready',ready:true,count:items.length});}
  }});
  renderer.setCoreSphere(true);renderer.pitch=1.1;renderer.yaw=.1;renderer.distance=2.9;
}catch(error){status.textContent=error.message;post({type:'astra-orbit-ready',ready:false});}
window.chrome?.webview?.addEventListener('message',event=>{
  try{const data=event.data;if(!renderer||!data)return;
    if(data.type==='astra-activity'){renderer.setActive(data.active);return;}
    if(data.type==='astra-theme'){renderer.setPalette(data);if(items.length)renderDesktop();}
    else if(data.type==='astra-desktop'){if(!Array.isArray(data.items)||data.items.length>500)throw Error(astraT("Elenco Desktop non valido"));items=data.items.filter(i=>typeof i.id==='string'&&/^[a-f0-9]{64}$/i.test(i.id)&&typeof i.name==='string'&&i.name.length<=260);renderDesktop();}
  }catch(error){status.textContent=astraT("Desktop non disponibile: ")+error.message;post({type:'astra-orbit-ready',ready:false});}
});
document.getElementById('search').addEventListener('input',()=>renderDesktop());
document.getElementById('showLabels').addEventListener('change',()=>renderer?.requestDraw());
document.getElementById('refresh').onclick=()=>post({type:'astra-desktop-refresh'});
document.getElementById('open').onclick=()=>{if(selected)post({type:'astra-desktop-open',id:selected.id});};
document.getElementById('explorer').onclick=()=>{if(selected)post({type:'astra-desktop-explorer',id:selected.id});};
