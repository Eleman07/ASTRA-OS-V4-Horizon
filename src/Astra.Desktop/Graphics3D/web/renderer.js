'use strict';
// Shared local WebGL engine. Coordinates are normalized before Float32 conversion.
// No imports, URLs, eval, network, filesystem or host-object access.
class AstraRenderer {
  constructor(canvas, options = {}) {
    this.canvas=canvas; this.options=options; this.parts=[];this.points=[];this.scene={series:[]};
    this.palette={navy:'#0A1931',deep:'#1A3D63',blue:'#4A7FA7',ice:'#B3CFE5',snow:'#F6FAFD'};
    this.fit='equal';this.pointSize=6;this.axes=true;this.orthographic=false;
    this.fps=[15,30,60].includes(options.fps)?options.fps:30;this.motion=!!options.motion;this.active=true;
    this.gl=canvas.getContext('webgl',{alpha:false,antialias:options.quality==='high',powerPreference:'low-power'});
    if(!this.gl)throw Error('WebGL non disponibile');
    const gl=this.gl;
    const compile=(type,source)=>{const s=gl.createShader(type);gl.shaderSource(s,source);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw Error('Shader non disponibile');return s;};
    this.program=gl.createProgram();const vs=compile(gl.VERTEX_SHADER,'attribute vec3 p;attribute vec3 color;uniform mat4 m;uniform float size;varying vec3 c;void main(){c=color;gl_Position=m*vec4(p,1.);gl_PointSize=size;}');
    const fs=compile(gl.FRAGMENT_SHADER,'precision mediump float;varying vec3 c;uniform bool points;void main(){if(points&&distance(gl_PointCoord,vec2(.5))>.5)discard;gl_FragColor=vec4(c,1.);}');
    gl.attachShader(this.program,vs);gl.attachShader(this.program,fs);gl.linkProgram(this.program);gl.deleteShader(vs);gl.deleteShader(fs);
    if(!gl.getProgramParameter(this.program,gl.LINK_STATUS))throw Error('Programma WebGL non disponibile');
    this.ap=gl.getAttribLocation(this.program,'p');this.ac=gl.getAttribLocation(this.program,'color');this.um=gl.getUniformLocation(this.program,'m');this.us=gl.getUniformLocation(this.program,'size');this.uPoints=gl.getUniformLocation(this.program,'points');
    gl.enable(gl.DEPTH_TEST);this.resetCamera();
    this.down=e=>{if(e.button!==0)return;this.drag={x:e.clientX,y:e.clientY,moved:0,pan:e.shiftKey};canvas.setPointerCapture(e.pointerId);};
    this.move=e=>{if(!this.drag)return;const dx=e.clientX-this.drag.x,dy=e.clientY-this.drag.y;this.drag.moved+=Math.abs(dx)+Math.abs(dy);if(this.drag.pan){this.pan[0]+=dx/160;this.pan[1]-=dy/160;}else{this.yaw+=dx*.008;this.pitch=Math.max(-1.55,Math.min(1.55,this.pitch+dy*.008));}this.drag.x=e.clientX;this.drag.y=e.clientY;this.requestDraw();};
    this.up=e=>{if(this.drag&&this.drag.moved<5)this.options.onPick?.(this.pick(e.clientX,e.clientY));this.drag=null;};
    this.wheel=e=>{e.preventDefault();this.distance=Math.max(1.3,Math.min(15,this.distance*Math.exp(e.deltaY*.001)));this.requestDraw();};
    this.key=e=>{const k=e.key;if(k==='ArrowLeft')this.yaw-=.12;else if(k==='ArrowRight')this.yaw+=.12;else if(k==='ArrowUp')this.pitch=Math.min(1.55,this.pitch+.12);else if(k==='ArrowDown')this.pitch=Math.max(-1.55,this.pitch-.12);else if(k==='+'||k==='=')this.distance=Math.max(1.3,this.distance*.9);else if(k==='-')this.distance=Math.min(15,this.distance/ .9);else if(k==='Home')this.resetCamera();else return;e.preventDefault();this.requestDraw();};
    this.hostActive=true;this.visibility=()=>{this.active=!document.hidden&&this.hostActive;if(this.active)this.requestDraw();else{cancelAnimationFrame(this.raf);clearTimeout(this.timer);this.pending=false;}};
    this.lost=e=>{e.preventDefault();this.active=false;this.options.onError?.('Contesto GPU perso. Riattiva la scena.');};
    canvas.addEventListener('pointerdown',this.down);canvas.addEventListener('pointermove',this.move);canvas.addEventListener('pointerup',this.up);canvas.addEventListener('pointercancel',()=>this.drag=null);canvas.addEventListener('wheel',this.wheel,{passive:false});canvas.addEventListener('keydown',this.key);canvas.addEventListener('webglcontextlost',this.lost);document.addEventListener('visibilitychange',this.visibility);
    canvas.tabIndex=0;this.resize=new ResizeObserver(()=>this.requestDraw());this.resize.observe(canvas);this.setScene({series:[]});
  }
  static rgb(hex){return [1,3,5].map(i=>parseInt(hex.slice(i,i+2),16)/255);}
  static multiply(a,b){const r=new Float32Array(16);for(let c=0;c<4;c++)for(let row=0;row<4;row++)for(let k=0;k<4;k++)r[c*4+row]+=a[k*4+row]*b[c*4+k];return r;}
  static rx(a){const c=Math.cos(a),s=Math.sin(a);return new Float32Array([1,0,0,0,0,c,s,0,0,-s,c,0,0,0,0,1]);}
  static ry(a){const c=Math.cos(a),s=Math.sin(a);return new Float32Array([c,0,-s,0,0,1,0,0,s,0,c,0,0,0,0,1]);}
  resetCamera(){this.yaw=.6;this.pitch=.35;this.distance=4.2;this.pan=[0,0];this.requestDraw();}
  setView(view){this.pan=[0,0];this.yaw=view==='side'?Math.PI/2:view==='iso'?0.6:0;this.pitch=view==='top'?Math.PI/2:view==='iso'?0.35:0;this.requestDraw();}
  setPalette(p){for(const k of Object.keys(this.palette))if(/^#[0-9a-f]{6}$/i.test(p[k]||'')){this.palette[k]=p[k];document.documentElement.style.setProperty('--'+k,p[k]);}this.setScene(this.scene,false);}
  setFit(mode){this.fit=mode==='axes'?'axes':'equal';this.setScene(this.scene,false);}
  setActive(active){this.hostActive=!!active;this.visibility();}
  setCoreSphere(enabled){this.coreSphere=!!enabled;this.setScene(this.scene,false);}
  setScene(scene,reset=true){
    if(!scene||!Array.isArray(scene.series)||scene.series.length>24)throw Error('Serie non valide');
    let total=0,scale=0;const min=[Infinity,Infinity,Infinity],max=[-Infinity,-Infinity,-Infinity];
    for(const s of scene.series){if(!['points','line','surface'].includes(s.kind)||!Array.isArray(s.points)||(total+=s.points.length)>50000)throw Error('Scena oltre i limiti');for(const p of s.points){if(!Array.isArray(p)||p.length!==3||!p.every(v=>typeof v==='number'&&Number.isFinite(v)&&Math.abs(v)<=1e100))throw Error('Coordinate non valide');for(const v of p)scale=Math.max(scale,Math.abs(v));}if(s.kind==='surface'&&(!Number.isInteger(s.rows)||!Number.isInteger(s.columns)||s.rows<2||s.columns<2||s.rows*s.columns!==s.points.length))throw Error('Griglia non valida');}
    scale=scale||1;for(const s of scene.series)for(const p of s.points)for(let a=0;a<3;a++){min[a]=Math.min(min[a],p[a]/scale);max[a]=Math.max(max[a],p[a]/scale);}
    if(!total){min.fill(0);max.fill(0);}const span=max.map((v,i)=>v-min[i]),center=min.map((v,i)=>(v+max[i])/2),big=Math.max(...span)||2;
    this.bounds={min:min.map(v=>v*scale),max:max.map(v=>v*scale)};
    const normalize=p=>{const n=p.map((v,i)=>(v/scale-center[i])/(this.fit==='axes'?(span[i]||2):big)*2);return [n[0],n[2],-n[1]];};
    const gl=this.gl;for(const p of this.parts)gl.deleteBuffer(p.buffer);this.parts=[];this.points=[];this.scene=scene;
    const add=(vertices,mode)=>{const buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,buffer);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(vertices),gl.STATIC_DRAW);this.parts.push({buffer,mode,count:vertices.length/6});};
    const ice=AstraRenderer.rgb(this.palette.ice),blue=AstraRenderer.rgb(this.palette.blue);
    for(let si=0;si<scene.series.length;si++){
      const s=scene.series[si],color=/^#[0-9a-f]{6}$/i.test(s.color||'')?AstraRenderer.rgb(s.color):(si%2?ice:blue);const vertices=[];
      const vertex=index=>{const raw=s.points[index],n=normalize(raw);const t=span[2]?(raw[2]/scale-min[2])/span[2]:.5;const c=s.kind==='surface'?blue.map((v,i)=>v+(ice[i]-v)*t):color;vertices.push(...n,...c);};
      for(let i=0;i<s.points.length;i++)this.points.push({series:si,index:i,point:s.points[i],position:normalize(s.points[i]),id:s.ids?.[i]});
      if(s.kind==='surface'){for(let r=0;r<s.rows-1;r++)for(let c=0;c<s.columns-1;c++){let a=r*s.columns+c,b=a+s.columns;for(const i of [a,b,a+1,a+1,b,b+1])vertex(i);}add(vertices,gl.TRIANGLES);}
      else{for(let i=0;i<s.points.length;i++)vertex(i);add(vertices,s.kind==='line'?gl.LINE_STRIP:gl.POINTS);}
    }
    this._axesPart=this.parts.length;const grid=[],gc=blue.map(v=>v*.55);const line=(a,b,c=gc)=>grid.push(...a,...c,...b,...c);
    for(let i=-5;i<=5;i++){let v=i/5;line([-1,-1,v],[1,-1,v]);line([v,-1,-1],[v,-1,1]);}line([-1.15,0,0],[1.15,0,0],ice);line([0,-1.15,0],[0,1.15,0],ice);line([0,0,-1.15],[0,0,1.15],ice);add(grid,gl.LINES);
    if(this.coreSphere){
      const vertices=[],colors=['navy','deep','blue','ice','snow'].map(k=>AstraRenderer.rgb(this.palette[k]));
      const vertex=(lat,lon)=>{const n=[Math.sin(lat)*Math.cos(lon),Math.cos(lat),Math.sin(lat)*Math.sin(lon)];let f=Math.max(0,Math.min(.99999,(n[0]*.35+n[1]*.65+n[2]*.55+1)/2));f=Math.min(3.99999,f*4);const a=Math.floor(f),blend=f-a,c=colors[a].map((v,i)=>v*(1-blend)+colors[a+1][i]*blend);vertices.push(...n.map(v=>v*.31),...c);};
      for(let r=0;r<32;r++)for(let c=0;c<64;c++){const a=r*Math.PI/32,b=(r+1)*Math.PI/32,x=c*Math.PI/32,y=(c+1)*Math.PI/32;vertex(a,x);vertex(b,x);vertex(a,y);vertex(a,y);vertex(b,x);vertex(b,y);}
      add(vertices,gl.TRIANGLES);this.parts[this.parts.length-1].core=true;
    }
    if(reset)this.resetCamera();this.options.onData?.(this.bounds,total);this.requestDraw();
  }
  requestDraw(){if(!this.gl||!this.active||this.pending)return;this.pending=true;clearTimeout(this.timer);const delay=Math.max(0,1000/this.fps-(performance.now()-(this.lastDraw||0)));const queue=()=>{this.raf=requestAnimationFrame(t=>{this.pending=false;this.draw(t);});};if(delay>1)this.timer=setTimeout(queue,delay);else queue();}
  draw(time){
    if(!this.active)return;this.lastDraw=time;const gl=this.gl,c=this.canvas,dpr=Math.min(devicePixelRatio||1,this.options.quality==='low'?1:this.options.quality==='high'?1.75:1.25),w=Math.max(1,Math.floor(c.clientWidth*dpr)),h=Math.max(1,Math.floor(c.clientHeight*dpr));
    if(c.width!==w||c.height!==h){c.width=w;c.height=h;}gl.viewport(0,0,w,h);gl.clearColor(...AstraRenderer.rgb(this.palette.navy),1);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);gl.useProgram(this.program);
    const f=1/Math.tan(.83/2),aspect=w/h,near=.1,far=50;
    const projection=this.orthographic?new Float32Array([2/this.distance/aspect,0,0,0,0,2/this.distance,0,0,0,0,-2/(far-near),0,0,0,-(far+near)/(far-near),1]):new Float32Array([f/aspect,0,0,0,0,f,0,0,0,0,(far+near)/(near-far),-1,0,0,2*far*near/(near-far),0]);
    const view=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,this.pan[0],this.pan[1],-this.distance,1]);
    const spin=this.motion?time*.00004:0;this.matrix=AstraRenderer.multiply(projection,AstraRenderer.multiply(view,AstraRenderer.multiply(AstraRenderer.rx(this.pitch),AstraRenderer.ry(this.yaw+spin))));
    gl.uniformMatrix4fv(this.um,false,this.matrix);gl.enableVertexAttribArray(this.ap);gl.enableVertexAttribArray(this.ac);gl.uniform1f(this.us,this.pointSize*dpr);
    this.parts.forEach((p,i)=>{if(!this.axes&&i===this._axesPart)return;let matrix=this.matrix;if(p.core&&this.motion){const pulse=1+.025*Math.sin(time*.0012),scale=new Float32Array([pulse,0,0,0,0,pulse,0,0,0,0,pulse,0,0,0,0,1]);matrix=AstraRenderer.multiply(this.matrix,AstraRenderer.multiply(AstraRenderer.ry(time*.00022),AstraRenderer.multiply(AstraRenderer.rx(.3),scale)));}gl.uniformMatrix4fv(this.um,false,matrix);gl.bindBuffer(gl.ARRAY_BUFFER,p.buffer);gl.vertexAttribPointer(this.ap,3,gl.FLOAT,false,24,0);gl.vertexAttribPointer(this.ac,3,gl.FLOAT,false,24,12);gl.uniform1i(this.uPoints,p.mode===gl.POINTS?1:0);gl.drawArrays(p.mode,0,p.count);});
    this.options.onDraw?.(this);if(this.motion){clearTimeout(this.timer);this.timer=setTimeout(()=>this.requestDraw(),1000/this.fps);}
  }
  project(position){if(!this.matrix)return null;const m=this.matrix,p=[...position,1],v=[0,0,0,0];for(let row=0;row<4;row++)for(let col=0;col<4;col++)v[row]+=m[col*4+row]*p[col];if(v[3]<=0||Math.abs(v[2]/v[3])>1)return null;return {x:(v[0]/v[3]+1)*this.canvas.clientWidth/2,y:(1-v[1]/v[3])*this.canvas.clientHeight/2,depth:v[3]};}
  pick(clientX,clientY){const b=this.canvas.getBoundingClientRect(),x=clientX-b.left,y=clientY-b.top;let best=null,distance=20*20;for(const point of this.points){if(this.options.pickFilter&&!this.options.pickFilter(point))continue;const q=this.project(point.position);if(!q)continue;const d=(q.x-x)**2+(q.y-y)**2;if(d<distance){distance=d;best=point;}}return best;}
  dispose(){this.active=false;cancelAnimationFrame(this.raf);clearTimeout(this.timer);this.resize.disconnect();document.removeEventListener('visibilitychange',this.visibility);for(const [name,fn] of [['pointerdown',this.down],['pointermove',this.move],['pointerup',this.up],['wheel',this.wheel],['keydown',this.key],['webglcontextlost',this.lost]])this.canvas.removeEventListener(name,fn);for(const p of this.parts)this.gl.deleteBuffer(p.buffer);this.gl.deleteProgram(this.program);}
}
window.AstraRenderer=AstraRenderer;
