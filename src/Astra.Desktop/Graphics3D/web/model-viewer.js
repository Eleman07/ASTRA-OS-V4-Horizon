import * as THREE from './vendor/three/build/three.module.js';
import {GLTFLoader} from './vendor/three/examples/jsm/loaders/GLTFLoader.js';
import {DRACOLoader} from './vendor/three/examples/jsm/loaders/DRACOLoader.js';
import {OBJLoader} from './vendor/three/examples/jsm/loaders/OBJLoader.js';
import {STLLoader} from './vendor/three/examples/jsm/loaders/STLLoader.js';
import {OrbitControls} from './vendor/three/examples/jsm/controls/OrbitControls.js';

const $=id=>document.getElementById(id),post=value=>window.chrome?.webview?.postMessage(value);
let stage,revision=0,nativeActive=true,stageSettings={fps:30,quality:"balanced",palette:{navy:"#0A1931"}};
class ModelStage{
 constructor(){
  this.renderer=new THREE.WebGLRenderer({antialias:stageSettings.quality!=="low",alpha:false});this.renderer.setPixelRatio(stageSettings.quality==="low"?1:Math.min(devicePixelRatio,1.5));this.renderer.setClearColor('#0A1931');this.renderer.toneMapping=THREE.ACESFilmicToneMapping;this.renderer.toneMappingExposure=1.5;this.renderer.localClippingEnabled=true;$('model-canvas').append(this.renderer.domElement);
  this.scene=new THREE.Scene();this.camera=new THREE.PerspectiveCamera(40,1,.001,1000);this.camera.position.set(2.6,1.7,3.2);this.controls=new OrbitControls(this.camera,this.renderer.domElement);this.controls.addEventListener('change',()=>this.request());
  this.scene.add(new THREE.HemisphereLight(0xe8f3ff,0x35485a,3));const light=new THREE.DirectionalLight(0xffffff,4);light.position.set(3,5,4);this.scene.add(light);const fill=new THREE.DirectionalLight(0xb3cfe5,3);fill.position.set(-4,2,-3);this.scene.add(fill);
  this.grid=new THREE.GridHelper(4,20,0x4a7fa7,0x203c57);this.grid.position.y=-.8;this.scene.add(this.grid);this.axes=new THREE.AxesHelper(1.2);this.scene.add(this.axes);this.pose=new THREE.Group();this.alignment=new THREE.Group();this.pose.add(this.alignment);this.scene.add(this.pose);
  this.clip=new THREE.Plane(new THREE.Vector3(-1,0,0),10);this.progress=0;this.playing=false;this.fps=stageSettings.fps;this.pending=false;this.visible=nativeActive&&!document.hidden;this.speed=1;this.last=0;
  this.draco=new DRACOLoader();this.draco.setDecoderPath('./vendor/three/examples/jsm/libs/draco/gltf/');this.draco.setWorkerLimit(stageSettings.quality==="low"?1:2);
  this.manager=new THREE.LoadingManager();this.manager.setURLModifier(url=>{if(url.startsWith('blob:')||url.startsWith('data:'))return url;throw Error(astraT("Risorsa esterna non incorporata nel modello"));});
  this.resize=new ResizeObserver(()=>this.request());this.resize.observe($('model-canvas'));
  document.addEventListener('visibilitychange',()=>{this.visible=nativeActive&&!document.hidden;if(this.visible)this.request();else{cancelAnimationFrame(this.raf);clearTimeout(this.timer);this.pending=false;}});
  $('play-model').onclick=()=>{this.playing=!this.playing;$('play-model').textContent=this.playing?astraT("Pausa"):astraT("Riproduci");this.last=0;this.request();};
  $('model-timeline').oninput=e=>{this.progress=Number(e.target.value)/1000;this.last=0;this.apply();this.request();};
  $('model-speed').onchange=e=>this.speed=Number(e.target.value);
  $('model-wire').onchange=e=>{this.meshes?.forEach(m=>{for(const mat of [].concat(m.material))mat.wireframe=e.target.checked;});this.request();};
  $('model-section').oninput=e=>{this.clip.constant=Number(e.target.value)/100;this.meshes?.forEach(m=>{for(const mat of [].concat(m.material)){mat.clippingPlanes=this.clip.constant<1?[this.clip]:[];mat.needsUpdate=true;}});this.request();};
  $('model-gain').onchange=e=>{this.gain=Number(e.target.value);this.apply();this.request();};this.gain=1;
  $('model-axis').onchange=e=>{this.alignment.rotation.set(0,0,0);if(e.target.value==='z')this.alignment.rotation.y=Math.PI/2;if(e.target.value==='y')this.alignment.rotation.z=-Math.PI/2;this.apply();this.request();};
  $('model-center').onclick=()=>{this.camera.position.set(2.6,1.7,3.2);this.controls.target.set(0,0,0);this.controls.update();this.request();};
 }
 disposeRoot(){if(!this.root)return;this.alignment.remove(this.root);this.root.traverse(o=>{o.geometry?.dispose();for(const m of [].concat(o.material||[])){for(const v of Object.values(m))if(v?.isTexture){v.source?.data?.close?.();v.dispose();}m.dispose();}});this.root=null;}
 async load(data,credit,simulation,trajectory){
  if(!simulation&&trajectory?.length>1)simulation={tool:"python",columns:["sample","x","y","z"],rows:trajectory.map((p,i)=>[i,...p])};
  const token=++revision;if(!['glb','gltf','obj','stl'].includes(data.format)||typeof data.data!=='string'||data.data.length>86_000_000)throw Error(astraT("Modello non valido"));
  const raw=atob(data.data),bytes=new Uint8Array(raw.length);for(let i=0;i<raw.length;i++)bytes[i]=raw.charCodeAt(i);let root;
  if(data.format==='glb'||data.format==='gltf'){const loader=new GLTFLoader(this.manager).setDRACOLoader(this.draco);const gltf=await loader.parseAsync(data.format==='glb'?bytes.buffer:new TextDecoder().decode(bytes),'');root=gltf.scene;this.clips=gltf.animations;}
  else if(data.format==='obj')root=new OBJLoader().parse(new TextDecoder().decode(bytes));
  else{const geometry=new STLLoader().parse(bytes.buffer);geometry.computeVertexNormals();root=new THREE.Group();root.add(new THREE.Mesh(geometry,new THREE.MeshStandardMaterial({color:0xb3cfe5,metalness:.25,roughness:.48})));}
  if(token!==revision)return;this.disposeRoot();this.root=root;this.simulation=simulation;this.pathBounds=null;if(this.path){this.scene.remove(this.path);this.path.geometry.dispose();this.path.material.dispose();this.path=null;}this.progress=0;this.playing=false;$('play-model').textContent=astraT("Riproduci");this.pose.position.set(0,0,0);this.pose.quaternion.identity();this.pose.scale.setScalar(1);
  root.updateMatrixWorld(true);const box=new THREE.Box3().setFromObject(root),size=box.getSize(new THREE.Vector3()),center=box.getCenter(new THREE.Vector3()),span=Math.max(size.x,size.y,size.z);
  if(!Number.isFinite(span)||span<=0||span>1e12)throw Error(astraT("Ingombro della mesh non valido"));this.originalSize=size.toArray();root.scale.multiplyScalar(1.8/span);root.position.sub(center.multiplyScalar(1.8/span));this.alignment.add(root);this.scene.updateMatrixWorld(true);
  this.meshes=[];let count=0;root.traverse(o=>{if(o.isMesh){this.meshes.push(o);count+=o.geometry.getAttribute('position')?.count||0;o.frustumCulled=false;for(const mat of [].concat(o.material)){mat.side=THREE.DoubleSide;mat.clippingPlanes=[];mat.wireframe=false;}}});
  if(count>2_000_000||this.meshes.length>512)throw Error(astraT("Mesh troppo complessa per il visualizzatore"));
  this.base=this.meshes.map(mesh=>({mesh,positions:mesh.geometry.attributes.position.array.slice(),colors:mesh.geometry.attributes.color?.array.slice(),matrix:mesh.matrixWorld.clone()}));
  this.heatFrames=null;this.lastDeformKey=null;this.lastHeatFrame=null;
  if(simulation?.tool==='heat'){const frames=new Map();for(const row of simulation.rows){if(!frames.has(row[0]))frames.set(row[0],[]);frames.get(row[0]).push(row[2]);}this.heatFrames=[...frames].map(([time,temperatures])=>({time,temperatures}));}
  $('model-title').textContent=data.name;$('model-credit').textContent=credit;$('model-stat').textContent=astraF("{0} vertici · {1} mesh · ingombro originale {2} (unità del file)",count.toLocaleString(),this.meshes.length,this.originalSize.map(n=>n.toPrecision(4)).join(' × '));
  $('model-pane').hidden=false;$('views').classList.add('with-model');$('model-wire').checked=false;$('model-section').value='100';
  this.apply();this.request();post({type:'astra-model-ready',ready:true,message:data.name+' · materiali e geometria caricati · '+count+astraT(" vertici")});
 }
 row(){const rows=this.simulation?.rows;if(!rows?.length)return null;const p=this.progress*(rows.length-1),i=Math.floor(p),j=Math.min(i+1,rows.length-1),a=p-i;return rows[i].map((v,k)=>typeof v==='number'?v+(rows[j][k]-v)*a:v);}
 apply(){
  if(!this.root)return;const s=this.simulation,r=this.row();this.pose.position.set(0,0,0);this.pose.quaternion.identity();this.pose.scale.setScalar(1);let detail='Modello geometrico · trascina per ruotare',axisLabel='';
  if(s&&r){const values=Object.fromEntries(s.columns.map((c,i)=>[c,r[i]]));detail=Object.entries(values).slice(0,7).map(([k,v])=>k+': '+Number(v).toPrecision(5)).join(' · ');const t=s.tool;
   if(t==='gnc'){const q=new THREE.Quaternion(values.qx||0,values.qy||0,values.qz||0,values.qw??1).normalize();this.pose.quaternion.copy(q);axisLabel=astraT("Assetto ECI: quaternione del corpo rigido");}
   else if(t==='orbit'||t==='flight'||t==='lorenz'||t==='python'){
    const indices=t==='orbit'?[1,3,2]:t==='flight'?[1,2,-1]:t==='python'?[1,2,3]:[1,3,2],rows=s.rows;
    if(!this.pathBounds||this.pathTool!==t){const lo=[Infinity,Infinity,Infinity],hi=[-Infinity,-Infinity,-Infinity];for(const row of rows)indices.forEach((k,j)=>{const v=k<0?0:row[k];lo[j]=Math.min(lo[j],v);hi[j]=Math.max(hi[j],v);});this.pathBounds={lo,hi,span:Math.max(...hi.map((v,j)=>v-lo[j]))||1};this.pathTool=t;this.drawPath(rows,indices);}
    const b=this.pathBounds;this.pose.position.set(...indices.map((k,j)=>((k<0?0:r[k])-(b.lo[j]+b.hi[j])/2)/b.span*2.6));this.pose.scale.setScalar(.16);
    if(t==='flight')this.pose.rotation.z=values.gamma_deg*Math.PI/180;
    axisLabel='Traiettoria calcolata · dimensione della mesh amplificata per leggibilità';
   }
   else if(t==='oscillator'){this.pose.position.x=values.displacement_m;axisLabel='Traslazione lungo X in metri · modello rigido';}
   else if(t==='aero'){this.pose.rotation.z=values.alpha_deg*Math.PI/180;axisLabel='Incidenza α applicata alla mesh · forze dal modello di ala finita';}
   else if(t==='beam'){this.deformBeam(this.progress);detail=astraF("Carico {0}% · δ punta {1} m · amplificazione ×{2}",(this.progress*100).toFixed(1),(s.rows.at(-1)[1]*this.progress).toPrecision(5),this.gain);axisLabel='Deformazione di Euler–Bernoulli proiettata sull’asse X della mesh; non FEM';}
   else if(t==='heat'){detail=this.paintHeat();axisLabel=astraT("Campo FTCS 1D proiettato lungo X della mesh");}
   else if(t==='reactor'){this.tint(Math.max(0,Math.min(1,values.CB_mol_m3/s.parameters.initial)));axisLabel='Colore: frazione di conversione chimica calcolata · volume ideale';}
   else if(t==='rlc'){this.tint(Math.max(0,Math.min(1,Math.abs(values.voltage_C_V)/(s.parameters.v||1))));axisLabel=astraT("Colore: tensione del condensatore normalizzata al gradino");}
   else if(t==='signal'){this.tint(Math.max(0,Math.min(1,values.amplitude)));axisLabel='Scansione dei bin DFT · colore proporzionale all’ampiezza';}
   else if(t==='queue'){const n=Math.round(values.users_n);this.pose.position.x=Math.min(2,n/100);axisLabel=astraF("Stato n={0}; probabilità stazionaria {1}%. Il veicolo rappresenta un utente, non una simulazione del traffico.",n,(values.probability*100).toPrecision(4));}
   else if(t==='ecology'){this.pose.scale.setScalar(.5+Math.min(1,values.prey/20));axisLabel='Scala del modello proporzionale alle prede del sistema Lotka–Volterra; non crescita anatomica';}
  }
  $('model-metrics').textContent=detail;$('model-assumptions').textContent=axisLabel;$('model-timeline').value=String(Math.round(this.progress*1000));
 }
 drawPath(rows,indices){if(this.path){this.scene.remove(this.path);this.path.geometry.dispose();this.path.material.dispose();}const b=this.pathBounds,points=rows.filter((_,i)=>i%Math.max(1,Math.ceil(rows.length/4000))===0).map(r=>new THREE.Vector3(...indices.map((k,j)=>((k<0?0:r[k])-(b.lo[j]+b.hi[j])/2)/b.span*2.6)));this.path=new THREE.Line(new THREE.BufferGeometry().setFromPoints(points),new THREE.LineBasicMaterial({color:0x4a7fa7}));this.scene.add(this.path);}
 deformBeam(factor){const key=Math.round(factor*100)+":"+this.gain;if(this.lastDeformKey===key)return;this.lastDeformKey=key;const p=this.simulation.parameters,L=p.length,load=p.load,E=p.young*1e9,I=p.inertia;for(const {mesh,positions,matrix} of this.base){if(mesh.isSkinnedMesh)continue;const attribute=mesh.geometry.attributes.position,inv=matrix.clone().invert(),v=new THREE.Vector3();for(let i=0;i<attribute.count;i++){v.fromArray(positions,i*3).applyMatrix4(matrix);const x=Math.max(0,Math.min(1,(v.x+.9)/1.8))*L;v.y-=load*x*x*(3*L-x)/(6*E*I)/L*1.8*factor*this.gain;v.applyMatrix4(inv);attribute.setXYZ(i,v.x,v.y,v.z);}attribute.needsUpdate=true;mesh.geometry.computeVertexNormals();}}
 paintHeat(){if(!this.heatFrames?.length)return ''; const frame=this.heatFrames[Math.round(this.progress*(this.heatFrames.length-1))];if(this.lastHeatFrame===frame)return this.heatDetail;this.lastHeatFrame=frame;for(const {mesh,positions,matrix} of this.base){const count=mesh.geometry.attributes.position.count,colors=mesh.geometry.attributes.color?.array||new Float32Array(count*3),v=new THREE.Vector3(),color=new THREE.Color();for(let i=0;i<count;i++){v.fromArray(positions,i*3).applyMatrix4(matrix);const index=Math.max(0,Math.min(50,Math.round((v.x+.9)/1.8*50))),t=frame.temperatures[index]/100;color.setRGB(.12+.82*t,.28+.68*t,.48+.5*t);color.toArray(colors,i*3);}if(!mesh.geometry.attributes.color)mesh.geometry.setAttribute('color',new THREE.BufferAttribute(colors,3));mesh.geometry.attributes.color.needsUpdate=true;for(const m of [].concat(mesh.material)){if(!m.vertexColors){m.vertexColors=true;m.color.set(0xffffff);m.needsUpdate=true;}}}return `t=${frame.time.toFixed(3)} s · min ${Math.min(...frame.temperatures).toFixed(2)} °C · max ${Math.max(...frame.temperatures).toFixed(2)} °C`;}
 tint(f){for(const {mesh} of this.base)for(const m of [].concat(mesh.material)){if(m.emissive){m.emissive.setRGB(.05*f,.2*f,.4*f);m.emissiveIntensity=.8;}}}
 request(){if(this.pending||!this.visible)return;this.pending=true;const delay=Math.max(0,1000/this.fps-(performance.now()-(this.lastDraw||0)));clearTimeout(this.timer);this.timer=setTimeout(()=>this.raf=requestAnimationFrame(t=>this.draw(t)),delay);}
 draw(time){this.pending=false;if(!this.visible)return;this.lastDraw=time;if(this.playing){if(this.last)this.progress=(this.progress+(time-this.last)/10000*this.speed)%1;this.last=time;this.apply();}const box=$('model-canvas').getBoundingClientRect();if(box.width&&box.height){this.renderer.setSize(box.width,box.height,false);this.camera.aspect=box.width/box.height;this.camera.updateProjectionMatrix();this.renderer.render(this.scene,this.camera);}if(this.playing)this.request();}
 dispose(){this.visible=false;clearTimeout(this.timer);cancelAnimationFrame(this.raf);this.resize.disconnect();this.controls.dispose();this.disposeRoot();this.draco.dispose();this.renderer.dispose();this.renderer.forceContextLoss();}
}
window.chrome?.webview?.addEventListener('message',async event=>{const d=event.data;try{if(d?.type==='astra-activity'){nativeActive=!!d.active;if(stage){stage.visible=nativeActive&&!document.hidden;stage.last=0;if(stage.visible)stage.request();else{clearTimeout(stage.timer);cancelAnimationFrame(stage.raf);stage.pending=false;}}return;}if(d?.type==='astra-model'){stage??=new ModelStage();stage.renderer.setClearColor(stageSettings.palette.navy);await stage.load(d.model,d.credit,d.simulation,d.trajectory);}else if(d?.type==='astra-scene'){stageSettings=d;if(!stage)return;stage.fps=[15,30,60].includes(d.fps)?d.fps:30;stage.renderer.setClearColor(d.palette.navy);stage.renderer.setPixelRatio(d.quality==='low'?1:Math.min(devicePixelRatio,1.5));stage.draco.setWorkerLimit(d.quality==='low'?1:2);if(!d.motion&&document.hidden)stage.playing=false;stage.request();}}catch(error){$('model-metrics').textContent=error.message;post({type:'astra-model-ready',ready:false,message:error.message});}});
window.addEventListener('pagehide',()=>stage?.dispose());
