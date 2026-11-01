(() => {
  'use strict';
  const $ = id => document.getElementById(id), canvas = $('graph'), ctx = canvas.getContext('2d');
  const palette = {note:'#68d8d0',memory:'#68d8d0',concept:'#f1bd73',agent:'#b0a0fa',skill:'#8884d9',task:'#77b6f2',document:'#76c2ef',project:'#86cda3',web:'#70b6d4',simulation:'#f1bd73','tool-error':'#e59797'};
  let data = {nodes:[],edges:[]}, visible = [], links = [], positions = new Map(), selected = null;
  let working=false, activeRoles=[], reduced=false, frames=24, pixelRatio=1.25, animation=0, timer=0;
  let width = 600, height = 400, scale = .7, panX = 0, panY = 0, limit = 300, pointer = null;
  function resize() { const rect=canvas.getBoundingClientRect();width=rect.width;height=rect.height;const dpr=Math.min(window.devicePixelRatio||1,pixelRatio);canvas.width=width*dpr;canvas.height=height*dpr;ctx.setTransform(dpr,0,0,dpr,0,0);draw(); }
  function layout() {
    positions=new Map();const groups=new Map();
    for(const node of visible){if(!groups.has(node.kind))groups.set(node.kind,[]);groups.get(node.kind).push(node);}
    const agents=groups.get("agent")||[];agents.forEach((node,i)=>{const a=i*Math.PI*2/Math.max(1,agents.length)-Math.PI/2;positions.set(node.id,{x:Math.cos(a)*230,y:Math.sin(a)*210});});
    const total=groups.size;let index=0;
    for(const [kind,nodes] of groups){if(kind==="agent")continue;const angle=index++*Math.PI*2/Math.max(1,total)-Math.PI/2;const cx=Math.cos(angle)*300,cy=Math.sin(angle)*260;
      nodes.forEach((node,i)=>{const parent=kind==="skill"?data.edges.find(e=>e.target===node.id&&positions.has(e.source)):null;const base=parent?positions.get(parent.source):{x:cx,y:cy};const r=nodes.length===1?0:(parent?22:35)*Math.sqrt(i%8+1),a=i*2.399963;positions.set(node.id,{x:base.x+Math.cos(a)*r,y:base.y+Math.sin(a)*r});});}
  }
  function filter() {
    const q=$('search').value.toLocaleLowerCase(),kind=$('kind').value;
    const matching=data.nodes.filter(n=>(!kind||n.kind===kind)&&(!q||(n.label+' '+n.detail).toLocaleLowerCase().includes(q)));
    visible=matching.slice(0,limit);const ids=new Set(visible.map(n=>n.id));links=data.edges.filter(e=>ids.has(e.source)&&ids.has(e.target));
    $('count').textContent=`${visible.length} / ${matching.length} matching · ${data.nodes.length} stored nodes`;
    $('more').disabled=visible.length>=matching.length;$('empty').hidden=visible.length!==0;
    const list=$('nodes');list.replaceChildren();for(const n of visible){const option=document.createElement('option');option.value=n.id;option.textContent=n.label;list.append(option);}
    layout();fit();
  }
  function fit(){if(!positions.size){draw();return;}const values=[...positions.values()];const xs=values.map(p=>p.x),ys=values.map(p=>p.y);const minX=Math.min(...xs),maxX=Math.max(...xs),minY=Math.min(...ys),maxY=Math.max(...ys);scale=Math.min(1.6,(width-90)/(maxX-minX+100),(height-70)/(maxY-minY+100));scale=Math.max(.03,scale);panX=-(minX+maxX)/2;panY=-(minY+maxY)/2;draw();}
  function screen(p){return{x:width/2+(p.x+panX)*scale,y:height/2+(p.y+panY)*scale};}
  function draw(){ctx.clearRect(0,0,width,height);
    const now=performance.now(),hub=screen({x:0,y:0}),pulse=working&&!reduced?Math.sin(now*.002):0;
    const glow=ctx.createRadialGradient(hub.x,hub.y,5,hub.x,hub.y,90+pulse*5);glow.addColorStop(0,'#5084bb55');glow.addColorStop(1,'#152b4800');ctx.fillStyle=glow;ctx.fillRect(hub.x-100,hub.y-100,200,200);
    ctx.strokeStyle=working?'#99d5f4':'#5b84ae';ctx.lineWidth=1.2;ctx.beginPath();ctx.ellipse(hub.x,hub.y,39+pulse*2,39+pulse*2,0,0,Math.PI*2);ctx.stroke();
    ctx.save();ctx.translate(hub.x,hub.y);ctx.rotate(working&&!reduced?now*.00015:.25);ctx.strokeStyle='#6db8d17a';ctx.beginPath();ctx.ellipse(0,0,54,20,.4,0,Math.PI*2);ctx.stroke();ctx.restore();
    ctx.fillStyle='#d9edff';ctx.font='600 12px Segoe UI';ctx.textAlign='center';ctx.fillText('ASTRA',hub.x,hub.y+4);ctx.textAlign='left';const neighbours=new Set();if(selected){neighbours.add(selected);for(const e of links){if(e.source===selected)neighbours.add(e.target);if(e.target===selected)neighbours.add(e.source);}}
    ctx.lineWidth=.7;for(const edge of links){const a=screen(positions.get(edge.source)),b=screen(positions.get(edge.target));const active=edge.source===selected||edge.target===selected;ctx.strokeStyle=active?'#79cada99':'#526d8a38';ctx.lineWidth=active?1.6:.7;ctx.beginPath();ctx.moveTo(a.x,a.y);ctx.lineTo(b.x,b.y);ctx.stroke();}
    for(const node of visible){const p=screen(positions.get(node.id)),active=node.id===selected;const live=node.kind==='agent'&&activeRoles.includes(node.id.replace('agent-',''));const radius=active?8:node.kind==='agent'?7:5;if(live){ctx.strokeStyle='#c1eaff';ctx.lineWidth=1.5;ctx.beginPath();ctx.arc(p.x,p.y,12+(working&&!reduced?3*Math.sin(performance.now()*.004):0),0,Math.PI*2);ctx.stroke();}ctx.globalAlpha=selected&&!neighbours.has(node.id)?.35:1;ctx.fillStyle=palette[node.kind]||'#93b7d5';if(active){ctx.shadowColor=ctx.fillStyle;ctx.shadowBlur=15;}ctx.beginPath();ctx.arc(p.x,p.y,radius,0,Math.PI*2);ctx.fill();ctx.shadowBlur=0;
      if(scale>.95||active||node.kind==='agent'){ctx.font=`${active?'600 ':''}${active?12:10}px Segoe UI`;ctx.fillStyle=active?'#ffffff':'#c3d3e3';const label=node.kind==="agent"?node.label.split(" · ")[0]:node.label;ctx.fillText(label.length>36?label.slice(0,35)+'…':label,p.x+radius+5,p.y+4);}}
    ctx.globalAlpha=1;
  }
  function inspect(id){document.querySelector('aside').hidden=false;selected=id;const n=data.nodes.find(n=>n.id===id);if(!n)return;$('title').textContent=n.label;$('type').textContent=n.kind;$('detail').textContent=n.detail||'No additional data.';
    const connections=data.edges.filter(e=>e.source===id||e.target===id);$('relations').textContent=connections.slice(0,20).map(e=>`${e.relation} → ${data.nodes.find(n=>n.id===(e.source===id?e.target:e.source))?.label||''}`).join('\n');$('relations').style.whiteSpace='pre-line';$('nodes').value=id;draw();}
  function hit(x,y){return [...visible].reverse().find(n=>{const p=screen(positions.get(n.id));return Math.hypot(x-p.x,y-p.y)<12;});}
  canvas.addEventListener('pointerdown',e=>{const rect=canvas.getBoundingClientRect(),x=e.clientX-rect.left,y=e.clientY-rect.top;pointer={x,y,startX:x,startY:y,node:hit(x,y)?.id};canvas.setPointerCapture(e.pointerId);});
  canvas.addEventListener('pointermove',e=>{if(!pointer)return;const rect=canvas.getBoundingClientRect(),x=e.clientX-rect.left,y=e.clientY-rect.top,dx=(x-pointer.x)/scale,dy=(y-pointer.y)/scale;if(pointer.node){const p=positions.get(pointer.node);p.x+=dx;p.y+=dy;}else{panX+=dx;panY+=dy;}pointer.x=x;pointer.y=y;draw();});
  canvas.addEventListener('pointerup',()=>{if(pointer?.node&&Math.hypot(pointer.x-pointer.startX,pointer.y-pointer.startY)<6)inspect(pointer.node);pointer=null;});canvas.addEventListener('pointercancel',()=>pointer=null);
  canvas.addEventListener('wheel',e=>{e.preventDefault();const rect=canvas.getBoundingClientRect(),x=e.clientX-rect.left-width/2,y=e.clientY-rect.top-height/2,old=scale;scale=Math.max(.03,Math.min(5,scale*Math.exp(-e.deltaY*.001)));panX+=x/scale-x/old;panY+=y/scale-y/old;draw();},{passive:false});
  canvas.addEventListener('keydown',e=>{if(e.key==='+'||e.key==='=')scale=Math.min(5,scale*1.2);else if(e.key==='-')scale=Math.max(.03,scale/1.2);else if(e.key==='0'){fit();return;}else if(e.key==='ArrowLeft')panX+=30/scale;else if(e.key==='ArrowRight')panX-=30/scale;else if(e.key==='ArrowUp')panY+=30/scale;else if(e.key==='ArrowDown')panY-=30/scale;else return;e.preventDefault();draw();});
  $('nodes').addEventListener('change',e=>inspect(e.target.value));$('search').addEventListener('input',filter);$('kind').addEventListener('change',filter);$('fit').addEventListener('click',fit);$('more').addEventListener('click',()=>{limit+=300;filter();});
  function animate(){cancelAnimationFrame(animation);clearTimeout(timer);animation=0;if(!working||reduced||document.hidden||frames<=0){draw();return;}timer=setTimeout(()=>{animation=requestAnimationFrame(()=>{draw();animate();});},1000/frames);}
  document.addEventListener('visibilitychange',animate);
  $('closeInspector').addEventListener('click',()=>document.querySelector('aside').hidden=true);
  $('focusNode').addEventListener('click',()=>{const node=data.nodes.find(n=>n.id===selected);if(node)window.chrome?.webview?.postMessage(JSON.stringify({type:'focus-node',id:node.id,label:node.label,detail:node.detail}));});
  function receive(graph){
    if(graph?.type==='activity'){working=graph.working===true;activeRoles=Array.isArray(graph.roles)?graph.roles:[];reduced=graph.motion===false;frames=graph.frames||0;$('phase').textContent=graph.phase||'Pronto';$('phase').classList.toggle('working',working&&!reduced);animate();return;}
    if(graph?.budget){limit=graph.budget.GraphNodes||300;pixelRatio=graph.budget.PixelRatio||1.25;frames=graph.budget.FramesPerSecond??24;resize();}
    if(!graph||!Array.isArray(graph.nodes)||!Array.isArray(graph.edges))return;data=graph;const old=$('kind').value;$('kind').replaceChildren(new Option('All types',''));for(const kind of [...new Set(data.nodes.map(n=>n.kind))].sort())$('kind').append(new Option(kind,kind));$('kind').value=old;filter();}
  window.astraGraph={receive,inspect,get state(){return{nodes:data.nodes.length,visible:visible.length,edges:links.length,selected,working,animation:animation!==0,pixelRatio,frames};}};
  if(window.chrome?.webview){window.chrome.webview.addEventListener('message',e=>receive(e.data));window.chrome.webview.postMessage('ready');}
  new ResizeObserver(resize).observe(canvas);resize();
})();
