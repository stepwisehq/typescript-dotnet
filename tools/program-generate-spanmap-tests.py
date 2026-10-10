import re,pathlib,json,hashlib,argparse
parser=argparse.ArgumentParser();parser.add_argument("--check",action="store_true");cli=parser.parse_args()
root=pathlib.Path(__file__).resolve().parents[1]; src=root/'upstream/TypeScript/tsc/internal/spanmap/spanmap_test.go'; text=src.read_text()
# Literal body translation; special table/subtest bodies are handled separately.
special={'TestVirtualToOriginalPosition','TestVirtualToOriginalPositionExact','TestZeroLengthSpansAtSegmentEnds','TestOriginalToVirtualPositions','TestValidate','TestValidateOriginalOverlapAndFeatures'}
def close(s,start,op='{',cl='}'):
 depth=0;quoted=False;escaped=False
 for i in range(start,len(s)):
  c=s[i]
  if quoted:
   if escaped:escaped=False
   elif c=='\\':escaped=True
   elif c=='"':quoted=False
   continue
  if c=='"':quoted=True
  elif c==op:depth+=1
  elif c==cl:
   depth-=1
   if depth==0:return i
 raise ValueError('unclosed')
def lists(s):
 p=re.compile(r'\[\](?:spanmap\.)?(Segment|MappedPosition|MappedSpan|core.TextRange|spanmap.Feature)\{')
 while (m:=p.search(s)):
  end=close(s,m.end()-1); body=s[m.end():end]; typ=m.group(1).replace('core.TextRange','TextRange').replace('spanmap.Feature','Feature')
  if typ in ['Segment','MappedPosition','MappedSpan']:
   body=re.sub(r'\{([^{}]*)\}',lambda m:'new() {'+re.sub(r'\b(\w+):',r'\1 =',m.group(1))+'}',body)
  s=s[:m.start()]+'new List<'+typ+'> {'+re.sub(r'\s+', ' ', body).strip()+'}'+s[end+1:]
 s=re.sub(r'spanmap.MappedSpan\{([^{}]*)\}',lambda m:'new MappedSpan {'+re.sub(r'\b(\w+):',r'\1 =',m.group(1))+'}',s)
 return s
def args(s):
 out=[];start=0;d=0;quote=False;escape=False
 for i,c in enumerate(s):
  if quote:
   if escape:escape=False
   elif c=='\\':escape=True
   elif c=='"':quote=False
   continue
  if c=='"':quote=True
  elif c in '([{':d+=1
  elif c in ')]}':d-=1
  elif c==',' and d==0:out.append(s[start:i].strip());start=i+1
 out.append(s[start:].strip());return out
names=[];bodies={}
for m in re.finditer(r'func (Test\w+)\(t \*testing.T\) \{',text):
 name=m.group(1);names.append(name);end=close(text,m.end()-1);body=text[m.end():end]
 if name in special:continue
 body=re.sub(r'//[^\n]*','',body).replace('t.Parallel()','');body=lists(body)
 body=re.sub(r'spanmap\.(Kind|Feature|Fidelity|MappingErrorKind)(\w+)',r'\1.\2',body).replace('spanmap.New(','SpanMaps.New(').replace('spanmap.Unmarshal(','SpanMaps.Unmarshal(').replace('core.NewTextRange(','new TextRange(').replace('core.TextPos(','(')
 body=body.replace('var m *spanmap.SpanMap','SpanMapValue m = null;').replace('nil','null').replace('int(','(').replace('string(data)','GoString.FromBytes(data!)').replace('[]byte(','GoString.Bytes(')
 # For-each original literal ranges and already-created arrays.
 body=re.sub(r'for _, (\w+) := range (.+) \{',r'foreach (var \1 in \2) {',body)
 body=re.sub(r'for (\w+) := range (\w+) \{',r'for (int \1 = 0; \1 < Len(\2); \1++) {',body)
 body=re.sub(r'len\(', 'Len(', body)
 body=re.sub(r'}\s*\)', '})', body)
 # Assertions preserve each source argument; diagnostic labels are adapted.
 p=re.compile(r'assert\.(Equal|DeepEqual|NilError|Assert)\(')
 while (m:=p.search(body)):
  end=close(body,m.end()-1,'(',')'); a=args(body[m.end():end])[1:]
  mode=m.group(1)
  replacement= {'Equal':lambda:f'Check.Equal({a[0]}, {a[1]}, "{name}");','DeepEqual':lambda:f'Check.Sequence({a[0]}, {a[1]}, "{name}");','NilError':lambda:f'Check.True({a[0]} is null, "{name}");','Assert':lambda:f'Check.True({a[0]}, "{name}");'}[mode]()
  body=body[:m.start()]+replacement+body[end+1:]
 # Go declarations/reassignments, avoiding old variables in partial declarations.
 seen=set(); renamed={}; lines=[]
 for line in body.splitlines():
  stripped=line.strip()
  if not stripped:continue
  if ':=' in line:
   lhs,rhs=line.split(':=',1); variables=[v.strip() for v in lhs.split(',')];indent=lhs[:len(lhs)-len(lhs.lstrip())]
   for old,new in renamed.items():rhs=re.sub(r'\b'+old+r'\b',new,rhs)
   declaration=[]
   for variable in variables:
    fresh=variable
    if variable in seen:
     suffix=1
     while fresh in seen:
      fresh=variable+'Next'+(str(suffix) if suffix>1 else '');suffix+=1
     renamed[variable]=fresh
    seen.add(fresh);declaration.append(fresh)
   if len(variables)>1:lines.append(indent+'var ('+', '.join(declaration)+') = '+rhs.strip()+';')
   else:lines.append(indent+'var '+declaration[0]+' = '+rhs.strip()+';')
   continue
  for old,new in renamed.items():line=re.sub(r'\b'+old+r'\b',new,line)
  stripped=line.strip()
  if stripped.startswith(('Check.','SpanMapValue?')) or stripped in ['{','}']:lines.append(line)
  elif stripped.startswith('foreach ') or stripped.startswith('for '):lines.append(line)
  elif re.match(r'^[\w_, ]+ = ',stripped):
   lhs,rhs=line.split('=',1);lhs=lhs.strip();lines.append(('('+lhs+')' if ',' in lhs else lhs)+' = '+rhs.strip()+';')
  else:lines.append(line)
 body='\n'.join(lines)
 bodies[name]=body
out='''// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Literal assertion-preserving port of pinned spanmap/spanmap_test.go.
// Reproduce: python3 tools/program-generate-spanmap-tests.py --check
#nullable disable
using Configuration.Checks;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using SpanMapValue = TypeScript.SpanMap.SpanMap;
using Kind = TypeScript.SpanMap.Kind;
namespace Program.Checks;
public static partial class SpanMapChecks
{
    public static IEnumerable<object[]> Cases => Names.Select(name => new object[] { name });
    public static readonly string[] Names = [
'''+''.join('        "'+n+'",\n' for n in names)+'''    ];
    public static void Run(string name)
    {
        switch (name)
        {
'''+''.join('            case "'+n+'": '+n+'(); break;\n' for n in names)+'''            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
    private static int Len<T>(IReadOnlyCollection<T> values) => values?.Count ?? 0;
'''
for name,body in bodies.items():out+='    private static void '+name+'()\n    {\n'+body+'\n    }\n'
out+='}\n';path=root/'validation/Program.Checks/SpanMapChecks.Generated.cs'
def emit(path,content):
 if cli.check:
  if not path.exists() or path.read_text()!=content:raise SystemExit('generated output drift: '+str(path.relative_to(root)))
 else:path.write_text(content)
emit(path,out)
emit(root/'validation/Program.Checks/spanmap-test-provenance.json',json.dumps({'upstreamRevision':'fed0bf24149fb1ed36039212648bafdafc1ea10e','sourceFile':str(src.relative_to(root)),'sourceSha256':hashlib.sha256(src.read_bytes()).hexdigest(),'functions':names,'disposition':'assertion-preserving-port','managedSource':str(path.relative_to(root)),'manualSource':'validation/Program.Checks/SpanMapChecks.cs','manualSourceSha256':hashlib.sha256((root/'validation/Program.Checks/SpanMapChecks.cs').read_bytes()).hexdigest(),'adaptations':['Go nil receiver becomes nullable extension call; Go len nil slice maps count zero.','Source t.Parallel/t.Run concurrency scheduling is supplied by shared/xUnit dispatcher; subtest input and assertions preserved.','Original byte JSON assertions retain byte-string conversion.','No benchmark performance/allocation claim.']},indent=2)+'\n')
print('generated',len(bodies),'simple bodies; special',len(special))
