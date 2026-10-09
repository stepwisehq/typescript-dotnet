"""Source-extract CLI original inputs and embed unchanged paired baseline bytes."""
from pathlib import Path
import base64,hashlib,json,re,sys
ROOT=Path(__file__).resolve().parents[1]
TEST=ROOT/'upstream/TypeScript/tsc/internal/tsoptions/commandlineparser_test.go'
text=TEST.read_text();sources={str(TEST.relative_to(ROOT)):hashlib.sha256(TEST.read_bytes()).hexdigest()}
STR=r'"(?:\\.|[^"\\])*"|`[^`]*`'
def literal(token):return token[1:-1] if token.startswith('`') else json.loads(token)
def body(name):
 start=text.index('func '+name+'(');match=re.search(r'^func ',text[start+5:],re.M);return text[start:start+5+match.start()] if match else text[start:]
def extract(name):
 return [(literal(n),[literal(v) for v in re.findall(STR,args)]) for n,args in re.findall(r'\{('+STR+r'),\s*\[\]string\{([^}]*)\}\}',body(name))]
rows=[]
for test,scenario in [('TestCommandLineParseResult','parseCommandLine'),('TestParseBuildCommandLine','parseBuildOptions')]:
 for name,args in extract(test):rows.append((test,scenario,name,args,''))
rows.append(('TestParseCommandLineVerifyNull','parseCommandLine','allows setting option type boolean to false',['--composite','false','0.ts'],''))
for title,name,nonnull,kind in [('option of type boolean','composite','true',''),('option of type object','paths','',''),('option of type list','rootDirs','abc,xyz',''),('option of type string','optionName','hello','String'),('option of type number','optionName','10','Number')]:
 scenarios=[(title+' allows setting it to null',['--'+name,'null','0.ts'])]
 if nonnull:scenarios.append((title+' errors if non null value is passed',['--'+name,nonnull,'0.ts']))
 scenarios +=[(title+' errors if its followed by another option',['0.ts','--strictNullChecks','--'+name]),(title+' errors if its last option',['0.ts','--'+name])]
 for title,args in scenarios:rows.append(('TestParseCommandLineVerifyNull','parseCommandLine',title,args,kind))
if len(rows)!=63:raise ValueError('Full scenario rows '+str(len(rows)))
lines=['// Exact original scenario and baseline assertion data, no implementation-generated expectations.','namespace Configuration.Checks;','public static partial class CommandLineChecks','{','    private sealed record OriginalScenario(string Group, string Name, string[] Arguments, string ExtraKind, string OriginalBase64, string CurrentBase64);','    private static readonly OriginalScenario[] originalScenarios =','    [']
for group,scenario,name,args,kind in rows:
 current=ROOT/'upstream/TypeScript/tsc/testdata/baselines/reference/tsoptions/commandLineParsing'/scenario/(name+'.js')
 original=ROOT/'upstream/TypeScript/tsc/testdata/fixtures/typescript/tests/baselines/reference/config/commandLineParsing'/scenario/(name+'.js')
 new=current.read_bytes();sources[str(current.relative_to(ROOT))]=hashlib.sha256(new).hexdigest()
 old=original.read_bytes() if original.exists() else b''
 # Only five extra build scenarios intentionally omit the original TS baseline.
 if not old and not (group=='TestParseBuildCommandLine' and name in ['parse --builders','--singleThreaded and --builders together','reports error when --builders is 0','reports error when --builders is negative','reports error when --builders is invalid type']):raise ValueError('Missing required original '+str(original))
 if old:sources[str(original.relative_to(ROOT))]=hashlib.sha256(old).hexdigest()
 strings=[json.dumps(x) for x in [group,scenario+'/'+name]]
 lines.append('        new('+', '.join(strings)+', ['+', '.join(json.dumps(a) for a in args)+'], '+json.dumps(kind)+', '+json.dumps(base64.b64encode(old).decode())+', '+json.dumps(base64.b64encode(new).decode())+'),')
lines+=['    ];','}']
products={ROOT/'validation/Configuration.Checks/CommandLineScenarios.Generated.cs':'\n'.join(lines)+'\n',ROOT/'validation/Configuration.Checks/command-line-scenario-provenance.json':json.dumps({'sources':sources,'originalFunctions':['TestCommandLineParseResult','TestParseCommandLineVerifyNull','TestParseBuildCommandLine'],'rows':len(rows),'preservation':'All original source scenarios, original TypeScript baseline fields/options assertions and exact pinned tsc formatted baselines; five additional build scenarios omit only original TS comparison as source does.'},indent=2)+'\n'}
for p,s in products.items():
 if '--check' in sys.argv:
  if p.read_text()!=s:raise SystemExit('Generated drift '+str(p))
 else:p.write_text(s)
print(json.dumps({'rows':len(rows),'originalFunctions':3}))
