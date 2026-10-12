# Exact fixture literal transport only; assertions remain handwritten source counterparts.
import re,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'upstream/TypeScript/tsc/internal/project/ata/ata_test.go').read_text()
pattern=r't\.Run\("([^"\n]+)", func\(t \*testing.T\) \{'
starts=list(re.finditer(pattern,s))
def balanced(at):
 depth=0;quote=None;i=at
 while i<len(s):
  c=s[i]
  if quote:
   if c==quote:quote=None
   elif c=='\\' and quote=='"':i+=1
  elif c in '`"':quote=c
  elif c=='{':depth+=1
  elif c=='}':
   depth-=1
   if depth==0:return s[at:i+1]
  i+=1
 raise ValueError(at)
def literal(token):return token[1:-1] if token[0]=='`' else json.loads(token)
def pairs(body):
 token=r'(?:`[^`]*`|"(?:\\.|[^"\\])*")'
 return [(m.group(1).replace('projecttestutil.TestTypingsLocation + ',''),m.group(2)) for m in re.finditer(r'((?:projecttestutil.TestTypingsLocation\s*\+\s*)?'+token+r')\s*:\s*('+token+r')',body)]
rows=[]
for index,m in enumerate(starts):
 body=balanced(m.end()-1);at=body.index('files := map[string]any{');prefix=s.index(body)+at+len('files := map[string]any');files=balanced(prefix)
 fs=[]
 for k,v in pairs(files):
  originalk=k
  # Detect typed cache-location prefix before each source string separately.
  rawmatch=re.search(r'(projecttestutil.TestTypingsLocation\s*\+\s*)?'+re.escape(k)+r'\s*:',files)
  fs.append((("/home/src/Library/Caches/typescript" if rawmatch and rawmatch.group(1) else '')+literal(k),literal(v)))
 packages=[]
 if 'PackageToFile: map[string]string{' in body:
  pat='PackageToFile: map[string]string';absat=s.index(body)+body.index(pat)+len(pat);packages=[(literal(k),literal(v)) for k,v in pairs(balanced(absat))]
 reg=re.search(r'TypesRegistry: \[\]string\{([^}]+)\}',body)
 registry=re.findall(r'"([^"\n]+)"',reg.group(1)) if reg else []
 rows.append({'name':m.group(1),'line':s[:m.start()].count('\n')+1,'files':fs,'registry':registry,'packages':packages,'skip':re.search(r't.Skip\("([^"\n]+)"\)',body).group(1) if 't.Skip(' in body else None})
# Extract the actual loop-defined fixture independently; its raw literal indentation
# differs from the following re-enable case and must remain byte-identical.
loop_start=s.index('t.Run("ATA disabled via "+tc.name')
loop_body=balanced(s.index('{',loop_start))
file_start=s.index(loop_body)+loop_body.index('files := map[string]any')+len('files := map[string]any')
loop_files=[(literal(k),literal(v)) for k,v in pairs(balanced(file_start))]
package_marker='PackageToFile: map[string]string'
package_start=s.index(loop_body)+loop_body.index(package_marker)+len(package_marker)
loop_packages=[(literal(k),literal(v)) for k,v in pairs(balanced(package_start))]
for name,config in [('unified setting','unified'),('deprecated setting','deprecated')]:
 rows.insert(-1,{'name':'ATA disabled via '+name,'config':config,'line':s[:loop_start].count('\n')+1,'files':loop_files,'registry':[],'packages':loop_packages,'skip':None})
(root/'validation/LanguageServices.Checks/ProjectAtaInputs.provenance.json').write_text(json.dumps(rows,indent=2)+'\n')
lines=['using Input = Program.Checks.CompilerBaselineFileSystem.Input;','// Exact original ATA literal inputs; generated from pinned source, no changed expectations.','namespace LanguageServices.Checks;','public static partial class ProjectAtaChecks','{',' private static readonly AtaCase[] Cases = [']
for row in rows:
 q=lambda v:json.dumps(v,ensure_ascii=True)
 lines.append(' new('+q(row['name'])+', ['+', '.join('new Input('+q(k)+', '+q(v)+')' for k,v in row['files'])+'], ['+', '.join(map(q,row['registry']))+'], new() {'+', '.join('['+q(k)+'] = '+q(v) for k,v in row['packages'])+'}),')
lines+=[' ];','}']
(root/'validation/LanguageServices.Checks/ProjectAtaChecks.Inputs.cs').write_text('\n'.join(lines)+'\n')
print(len(rows),'groups',sum(bool(r['skip']) for r in rows),'original skips')
