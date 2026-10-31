from pathlib import Path
import sys
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1] / 'src'
errors=[]
for f in root.rglob('*.cs'):
 s=f.read_text(encoding='utf8');i=0;n=len(s);line=1;state='normal';raw_n=0;start=0
 while i<n:
  c=s[i];two=s[i:i+2]
  if state=='normal':
   if two=='//':state='linecomment';i+=2;continue
   if two=='/*':state='blockcomment';i+=2;continue
   if c=='"':
    j=i
    while j<n and s[j]=='"':j+=1
    if j-i>=3:state='raw';raw_n=j-i;start=line;i=j;continue
    if i>0 and s[i-1]=='@':state='verbatim'
    else:state='string'
    start=line
   elif c=="'":state='char';start=line
  elif state=='linecomment':
   if c=='\n':state='normal'
  elif state=='blockcomment':
   if two=='*/':state='normal';i+=2;continue
  elif state in ('string','char'):
   if c=='\\':i+=2;continue
   if c=='"' and state=='string':state='normal'
   elif c=="'" and state=='char':state='normal'
   elif c=='\n': errors.append((f,start,state,'new line in ordinary literal'));state='normal'
  elif state=='verbatim':
   if two=='""':i+=2;continue
   if c=='"':state='normal'
  elif state=='raw':
   if c=='"':
    j=i
    while j<n and s[j]=='"':j+=1
    if j-i>=raw_n:state='normal';i=j;continue
  if c=='\n':line+=1
  i+=1
 if state in ('string','char'):errors.append((f,start,state,'unclosed literal'))
for f,line,k,issue in errors:print(f.relative_to(root),line,k,issue)
print('ordinary literal scanner:', 'PASS' if not errors else f'FAIL {len(errors)}')
sys.exit(bool(errors))
