from pathlib import Path
import re,gzip,struct,json,hashlib
scope={};exec(Path('tools/generate-regex-checks.py').read_text().split('alltext=')[0].replace('root=Path(__file__).resolve().parents[1]','root=Path.cwd()'),scope)
source=Path('/home/greg/go/pkg/mod/golang.org/x/text@v0.42.0/unicode/norm/normalize_test.go');original=source.read_text();literals=[]
pattern=scope['strpat']+r"|'(?:[^'\\]|\\.)*'"
def protect(m):
 q=m.group()
 if q.startswith('//'):return ''
 literals.append(scope['raw']('"'+q[1:-1]+'"' if q.startswith("'") else q));return 'L'+str(len(literals)-1)
text=re.sub(r'//[^\n]*|'+pattern,protect,original);text=re.sub(r'//[^\n]*','',text)
known={f'L{i}':v for i,v in enumerate(literals)};known.update(maxNonStarters=30,segSize=128,cgj=chr(0x34f).encode())
def evaluate(expr):
 expr=expr.strip().replace('strings.Repeat','repeat')
 for name in re.findall(r'\b[A-Za-z_]\w*\b',expr):
  if name in {'repeat','rep','grave'}:continue
  if name not in known:
   definition=re.search(r'\b'+re.escape(name)+r'\s*=\s*([^\n]+)',text)
   assert definition,name
   known[name]=evaluate(definition[1])
 return eval(expr,{'__builtins__':{}},{**known,'repeat':lambda s,n:s*n,'rep':lambda r,n:(chr(r).encode() if isinstance(r,int) else r)*n,'grave':lambda n:chr(0x300).encode()*n})
rows=[];counts={}
for table in ['appendTestsNFC','appendTestsNFKC','appendTestsNFKD']:
 body=text.split('var '+table+' = []AppendTest{',1)[1].split('\n}',1)[0];start=len(rows)
 for m in re.finditer(r'\{([^{}]*)\}',body,re.S):
  expr=m[1];parts=[];depth=0;position=0
  for i,c in enumerate(expr):
   if c=='(':depth+=1
   elif c==')':depth-=1
   elif c==',' and depth==0:parts.append(expr[position:i]);position=i+1
  if expr[position:].strip():parts.append(expr[position:])
  assert len(parts)==3,(table,parts)
  rows.append((evaluate(parts[0]),evaluate(parts[1])))
 counts[table]=len(rows)-start
out=Path('validation/LanguageServices.Checks/RegexData/nfd-string-bootstrap.bin.gz')
with out.open('wb') as f,gzip.GzipFile(filename='',fileobj=f,mode='wb',mtime=0) as w:
 w.write(struct.pack('<i',len(rows)))
 for left,right in rows:
  for field in [left,right]:w.write(struct.pack('<i',len(field)));w.write(field)
Path('validation/LanguageServices.Checks/RegexData/nfd-string-bootstrap-provenance.json').write_text(json.dumps({'source':str(source),'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'resourceSha256':hashlib.sha256(out.read_bytes()).hexdigest(),'rows':len(rows),'tables':counts,'scope':'original TestString selected NFD cross-form bootstrap invariant; original empty NFD direct table, other forms excluded; empty AppendString setup replaced by equivalent NFD.String(left)','textAttribution':'Source fixture retains original txt_vn/kr/jp/cn CC attribution references at normalize_test.go:1244 onward'},indent=2)+'\n')
print(counts)
