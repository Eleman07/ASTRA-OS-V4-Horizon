'use strict';
let astraStrings={};
function astraT(source){return astraStrings[source]||source;}
function astraF(source,...values){return astraT(source).replace(/\{(\d+)[^{}]*\}/g,(_,i)=>String(values[Number(i)]??''));}
const astraNodes=[],astraAttributes=[];
const walker=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT);
while(walker.nextNode()){const node=walker.currentNode;if(node.textContent.trim()&&!['SCRIPT','STYLE'].includes(node.parentElement.tagName))astraNodes.push([node,node.textContent]);}
for(const node of document.querySelectorAll('[aria-label],[placeholder],[title]'))for(const key of ['aria-label','placeholder','title'])if(node.hasAttribute(key))astraAttributes.push([node,key,node.getAttribute(key)]);
window.chrome?.webview?.addEventListener('message',event=>{
 const d=event.data;if(!d?.locale||!d.strings)return;astraStrings=d.strings;document.documentElement.lang=d.locale;
 for(const [node,source] of astraNodes){if(node.isConnected)node.textContent=source.replace(source.trim(),astraT(source.trim()));}
 for(const [node,key,source] of astraAttributes)node.setAttribute(key,astraT(source));
});
