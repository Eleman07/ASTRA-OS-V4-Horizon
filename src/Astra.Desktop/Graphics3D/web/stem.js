'use strict';
let renderer,pending=false;
const post=value=>window.chrome?.webview?.postMessage(value);
try{
 renderer=new AstraRenderer(document.getElementById('scene'),{fps:30,motion:false,onPick:p=>{if(p)document.getElementById('status').textContent=renderer.scene.series[p.series].name+' · ['+p.point.map(v=>v.toPrecision(6)).join(', ')+']';},onDraw:()=>{if(pending){pending=false;post({type:'astra-stem-ready',ready:true});}},onError:message=>{document.getElementById('status').textContent=message;post({type:'astra-stem-ready',ready:false});}});
 window.chrome?.webview?.addEventListener('message',event=>{
  const d=event.data;if(d?.type==='astra-activity'){renderer.setActive(d.active);return;}if(d?.type!=='astra-scene')return;
  try{renderer.fps=[15,30,60].includes(d.fps)?d.fps:30;renderer.setPalette(d.palette||{});pending=true;renderer.setScene(d.scene);document.getElementById('title').textContent=d.scene.title;document.getElementById('count').textContent=d.scene.series.reduce((n,s)=>n+s.points.length,0)+astraT(" punti");
   const legend=document.getElementById('legend');legend.replaceChildren();for(const s of d.scene.series){const row=document.createElement('div');row.textContent=s.name+' · '+s.kind;legend.append(row);}const axes=document.createElement('div');axes.textContent=Object.entries(d.scene.axes||{}).map(([k,v])=>k.toUpperCase()+': '+v).join(' / ');legend.append(axes);for(let i=0;i<3;i++){const range=document.createElement('div');range.textContent=['X','Y','Z'][i]+': '+renderer.bounds.min[i].toPrecision(5)+' → '+renderer.bounds.max[i].toPrecision(5);legend.append(range);}
  }catch(error){document.getElementById('status').textContent=error.message;post({type:'astra-stem-ready',ready:false});}
 });
 document.querySelectorAll('[data-view]').forEach(b=>b.addEventListener('click',()=>renderer.setView(b.dataset.view)));document.getElementById('reset').onclick=()=>renderer.resetCamera();document.getElementById('axes').onchange=e=>{renderer.axes=e.target.checked;renderer.setScene(renderer.scene,false);};document.getElementById('ortho').onchange=e=>{renderer.orthographic=e.target.checked;renderer.requestDraw();};document.getElementById('fit').onchange=e=>renderer.setFit(e.target.checked?'axes':'equal');
}catch(error){document.getElementById('status').textContent=error.message;post({type:'astra-stem-ready',ready:false});}
window.addEventListener('pagehide',()=>renderer?.dispose());
