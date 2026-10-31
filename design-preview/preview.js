'use strict';
// This is solely a visual QA preview; the real WinUI app persists preferences and DPAPI-encrypted tasks.
const defaults={navy:'#0A1931',ice:'#B3CFE5',blue:'#4A7FA7',deep:'#1A3D63',snow:'#F6FAFD'};
const storageKey='astra-v4-visual-preview-palette';
const taskKey='astra-v4-visual-preview-tasks';
const $=id=>document.getElementById(id);
function read(key,fallback){try{return JSON.parse(localStorage.getItem(key))??fallback}catch{return fallback}}
let palette=read(storageKey,defaults);
const hex=x=>/^#[0-9a-f]{6}$/i.test(x);
const contrast=hexcolor=>{let c=[1,3,5].map(i=>parseInt(hexcolor.slice(i,i+2),16)/255).map(x=>x<=.04045?x/12.92:Math.pow((x+.055)/1.055,2.4));return .2126*c[0]+.7152*c[1]+.0722*c[2]>.179?'#0A1931':'#F6FAFD'};
function recolor(){Object.entries(palette).forEach(([key,val])=>document.documentElement.style.setProperty('--'+key,val));document.documentElement.style.setProperty('--ink',contrast(palette.snow));document.documentElement.style.setProperty('--light',contrast(palette.navy));document.querySelectorAll('.paletteform label').forEach(label=>{const key=label.dataset.token;label.querySelector('i').style.background=palette[key];label.querySelector('input').value=palette[key]})}
function view(name){$('dashboard').hidden=name!=='Home';$('settings').hidden=name!=='Settings';$('feature').hidden=name==='Home'||name==='Settings';$('featuretitle').textContent=name;document.querySelectorAll('[data-view]').forEach(b=>b.classList.toggle('selected',b.dataset.view===name&&b.closest('nav')));}
for(const b of document.querySelectorAll('[data-view]'))b.addEventListener('click',()=>view(b.dataset.view));
for(const token of Object.keys(defaults)){const label=document.createElement('label');label.dataset.token=token;label.innerHTML='<i></i>'+token.toUpperCase()+'<input type="text" maxlength="7" aria-label="Color '+token+'">';$('paletteform').appendChild(label)}
$('apply').addEventListener('click',()=>{const next=Object.fromEntries([...document.querySelectorAll('.paletteform label')].map(l=>[l.dataset.token,l.querySelector('input').value]));if(!Object.values(next).every(hex)){alert('Use #RRGGBB for every color');return}palette=next;localStorage.setItem(storageKey,JSON.stringify(palette));recolor()});
$('reset').addEventListener('click',()=>{palette={...defaults};localStorage.removeItem(storageKey);recolor()});
$('motion').addEventListener('change',e=>document.querySelector('.moving').style.animationPlayState=e.target.checked?'paused':'running');
$('focus').addEventListener('click',()=>document.querySelector('.app').classList.toggle('focus'));
$('back').addEventListener('click',()=>view('Home'));
$('search').addEventListener('keydown',e=>{if(e.key!=='Enter')return;const q=e.target.value.toLowerCase().trim();const btn=[...document.querySelectorAll('[data-view]')].find(b=>b.dataset.view.toLowerCase().includes(q));if(btn){view(btn.dataset.view);e.target.value=''}});
document.addEventListener('keydown',e=>{if(e.ctrlKey&&e.key.toLowerCase()==='k'){e.preventDefault();$('search').focus()}});
function clock(){$('clock').textContent=new Date().toLocaleString('it-IT',{weekday:'short',hour:'2-digit',minute:'2-digit',day:'2-digit',month:'short'})}clock();setInterval(clock,60000);
$('perfBtn').onclick=()=>{for(const id of ['cpu','ram','disk'])$(id).textContent='n/d';alert('Only the native ASTRA WinUI app can read Windows diagnostics. This is a design preview.')};
let tasks=read(taskKey,[]);
function render(){const root=$('tasklist');root.textContent='';for(const task of tasks){const label=document.createElement('label');const check=document.createElement('input');check.type='checkbox';check.checked=!!task.done;check.onchange=()=>{task.done=check.checked;task.date=check.checked?new Date().toISOString():null;save()};const text=document.createElement('span');text.textContent=task.text;const del=document.createElement('button');del.textContent='×';del.onclick=()=>{tasks=tasks.filter(t=>t.id!==task.id);save()};label.append(check,text,del);root.append(label)}let counts=Array.from({length:7},(_,i)=>{let d=new Date();d.setDate(d.getDate()-6+i);return tasks.filter(t=>t.date&&new Date(t.date).toDateString()===d.toDateString()).length});let m=Math.max(...counts,1);$('chart').replaceChildren(...counts.map(n=>{let bar=document.createElement('span');bar.style.height=(5+80*n/m)+'px';return bar}))}
function save(){localStorage.setItem(taskKey,JSON.stringify(tasks));render()}
$('taskform').onsubmit=e=>{e.preventDefault();const el=$('newtask'),t=el.value.trim();if(!t||t.length>120||tasks.length>=100)return;tasks.push({id:crypto.randomUUID(),text:t,done:false,date:null});el.value='';save()};
recolor();render();
