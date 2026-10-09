"""Ordinary configuration CLI inputs; expectations come only from pinned Go."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'validation/configuration-corpus'
rows=[]
sub=[json.loads(line)for line in (OUT/'subsystem-requests.jsonl').read_text().splitlines()]
for r in sub:
 if r['name'].startswith('checking-demo-'):
  for action in ['inspect','check']:
   rows.append({**r,'name':r['name']+'-'+action,'cliAction':action,'logicalRoot':'/repo','exitCode':0 if action=='inspect' or r['name'].endswith('valid') and not r['name'].endswith('invalid') else 1})
# Hand-authored checking exclusions are separate from original-Go semantics.
config={'compilerOptions':{'allowJs':True,'jsx':'preserve','noEmit':False,'module':'commonjs','target':'es5'},'references':[{'path':'other'}],'include':['src']}
base={'mode':'file','logicalRoot':'/project','configPath':'/project/tsconfig.json','files':[{'path':'/project/tsconfig.json','text':json.dumps(config)},{'path':'/project/src/component.tsx','text':'export const x=<div/>;'},{'path':'/project/src/a.js','text':'let x=1;'}]}
rows +=[{**base,'name':'inspect-unsupported-checker-settings','cliAction':'inspect','exitCode':0}, {**base,'name':'check-unsupported-settings','cliAction':'check','exitCode':2,'unsupportedError':'Project-reference execution is not supported. Use --inspect-config to inspect this configuration without checking.'}]
for name,configtext in [('invalid-option-config','{"compilerOptions":{"target":"wrong"},"files":["a.ts"]}'),('invalid-jsonc-config','{"files":["a.ts"],,,}')]:
 for action in ['inspect','check']:
  rows.append({'name':name+'-'+action,'mode':'file','logicalRoot':'/project','configPath':'/project/tsconfig.json','files':[{'path':'/project/tsconfig.json','text':configtext},{'path':'/project/a.ts','text':''}],'cliAction':action,'exitCode':1})
for action in ['inspect','check']:
 rows.append({'name':'missing-config-'+action,'mode':'file','logicalRoot':'/project','configPath':'/project/missing.json','files':[{'path':'/project/a.ts','text':''}],'cliAction':action,'exitCode':1})
rows.append({'name':'raw-native-directory-name-rejected','mode':'file','logicalRoot':'/project','configPath':'/project/tsconfig.json','files':[{'path':'/project/tsconfig.json','text':'{"include":["src/**/*"]}'},{'path':'/project/src/�.ts','text':'export {};'}],'cliAction':'inspect','exitCode':2,'unsupportedError':'The .NET filesystem requires a scalar Unicode path; the original Go byte string is not representable.','rawEntries':[{'pathBase64':'c3JjL/8udHM=','text':'export {};'}]})
rows.append({'name':'valid-replacement-supplementary-filenames','mode':'file','logicalRoot':'/project','configPath':'/project/tsconfig.json','files':[{'path':'/project/tsconfig.json','text':'{"include":["src/**/*"]}'},{'path':'/project/src/�.ts','text':''},{'path':'/project/src/𐀀.ts','text':''},{'path':'/project/src/é.ts','text':''}],'cliAction':'inspect','exitCode':0})
(OUT/'cli-requests.jsonl').write_text(''.join(json.dumps(row,separators=(',',':'),ensure_ascii=True)+'\n'for row in rows))
print(len(rows))
