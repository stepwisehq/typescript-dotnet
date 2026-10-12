#!/usr/bin/env python3
"""Source-copy original x/text locale-worker input/expected tables, without executing Go."""
import argparse,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source-root',type=Path,default=Path.home()/'go/pkg/mod/golang.org/x/text@v0.42.0')
args=parser.parse_args()
source=args.source_root
files=['internal/language/language_test.go','internal/language/match_test.go','internal/language/parse_test.go','internal/language/compose_test.go','language/language_test.go','language/match_test.go','language/testdata/CLDRLocaleMatcherTest.txt','language/testdata/GoLocaleMatcherTest.txt']
texts={name:(source/name).read_text() for name in files}
def quote(value): return json.dumps(value,ensure_ascii=False)
def strip(text): return re.sub(r'//[^\n]*','',text)
def function(name,text):
 start=text.index('func '+name+'(')
 next_=text.find('\nfunc ',start+1)
 return text[start:next_ if next_!=-1 else len(text)]
header='''// Copyright 2013, 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Source-generated exact original x/text v0.42.0 assertion inputs/expected values.
namespace LanguageServices.Checks;
public static partial class LocaleMatcherChecks
{
'''
for name in ['TestAddLikelySubtags','TestMinimize']:
 rows=re.findall(r'\{"([^"\\]*)", "([^"\\]*)"\}',strip(function(name,texts['internal/language/match_test.go'])))
 header+='    private static readonly (string Input, string Expected)[] '+name+'Cases =\n    [\n'+''.join('        ('+quote(a)+', '+quote(b)+'),\n' for a,b in rows)+'    ];\n'
body=strip(function('parseTests',texts['internal/language/parse_test.go']))
rows=re.findall(r'\{in: "((?:\\.|[^"\\])*)"(.*?)\}(?=,?\s*\n)',body,re.S)
parsed=[]
for input_,rest in rows:
 d={key:json.loads('"'+value+'"') for key,value in re.findall(r'(lang|script|region|variants|ext): "((?:\\.|[^"\\])*)"',rest)}
 extlist=re.search(r'extList: \[\]string\{([^}]+)',rest)
 if extlist: d['ext']='-'.join(json.loads(x) for x in re.findall(r'"(?:\\.|[^"\\])*"',extlist[1]))
 parsed.append([json.loads('"'+input_+'"')]+[d.get(key,'') for key in ['lang','script','region','variants','ext']])
header+='    private static readonly ParseParts[] BuilderCases =\n    [\n'+''.join('        new('+', '.join(quote(x) for x in row)+'),\n' for row in parsed)+'    ];\n'
rows=re.findall(r'\{"([^"\\]*)", "([^"\\]*)", ([^}]+)\}',strip(function('TestCanonicalize',texts['language/language_test.go'])))
header+='    private static readonly (string Input, string Expected, LanguageParser.CanonType Option)[] CanonicalizeCases =\n    [\n'+''.join('        ('+quote(a)+', '+quote(b)+', '+re.sub(r'\b(\w+)\b',r'LanguageParser.CanonType.\1',flag.strip())+'),\n' for a,b,flag in rows)+'    ];\n'
canonicalizeCount=len(rows)
skiptext=texts['language/match_test.go'].split('var skip = map[string]bool{',1)[1].split('\n}',1)[0]
skips=set(re.findall(r'"([^"\\]*)":\s*true',skiptext))
compliance=[]; skipped=[]
for path in files[-2:]:
 for lineNumber,line in enumerate(texts[path].splitlines(),1):
  line=line.split('#',1)[0].strip()
  if not line or line.startswith('@'): continue
  cells=[s.strip() for s in line.split(';')]
  assert len(cells)>=3,(path,lineNumber,line)
  name=cells[0].replace(' ','')+'/'+cells[1].replace(' ','')
  if name in skips: skipped.append({'file':path,'line':lineNumber,'name':name}); continue
  compliance.append([path.split('/')[-1]+':'+str(lineNumber)]+cells[:3]+[cells[3] if len(cells)>3 else ''])
header+='    private static readonly ComplianceCase[] ComplianceCases =\n    [\n'+''.join('        new('+', '.join(quote(x) for x in row)+'),\n' for row in compliance)+'    ];\n'
rows=re.findall(r'\{"([^"\\]*)", "([^"\\]*)", "([^"\\]*)", "([^"\\]*)", (true|false)\}',strip(function('TestSetTypeForKey',texts['internal/language/language_test.go'])))
header+='    private static readonly (string Key, string Value, string Input, string Expected, bool Error)[] SetTypeCases =\n    [\n'+''.join('        ('+', '.join(quote(x) for x in row[:4])+', '+row[4]+'),\n' for row in rows)+'    ];\n'
rows=re.findall(r'\{"([^"\\]*)", "([^"\\]*)", (true|false)\}',strip(function('TestContains',texts['internal/language/language_test.go'])))
header+='    private static readonly (string Enclosing, string Contained, bool Expected)[] ContainsCases =\n    [\n'+''.join('        ('+quote(a)+', '+quote(b)+', '+flag+'),\n' for a,b,flag in rows)+'    ];\n'
rows=re.findall(r'\{"([^"\\]*)", "([^"\\]*)", (\w+)\}',strip(function('TestScript',texts['language/language_test.go'])))
header+='    private static readonly (string Input, string Script, LanguageParser.Confidence Confidence)[] ScriptCases =\n    [\n'+''.join('        ('+quote(a)+', '+quote(b)+', LanguageParser.Confidence.'+flag+'),\n' for a,b,flag in rows)+'    ];\n'
header+='}\n'
header=header.replace('namespace LanguageServices.Checks;','using TypeScript.Locale.XText;\nnamespace LanguageServices.Checks;')
(ROOT/'validation/LanguageServices.Checks/LocaleMatcherChecks.Generated.cs').write_text(header)
(ROOT/'docs/progress/lsp-localization-test-provenance.json').write_text(json.dumps({'module':'golang.org/x/text','version':'v0.42.0','sources':{name:hashlib.sha256((source/name).read_bytes()).hexdigest() for name in files},'generatedSha256':hashlib.sha256(header.encode()).hexdigest(),'counts':{'likelyRows':len(re.findall(r'\{"',strip(function('TestAddLikelySubtags',texts['internal/language/match_test.go'])))),'builderRowsPerFunction':len(parsed),'canonicalizeRows':canonicalizeCount,'complianceRows':len(compliance)},'originalComplianceOmittedRows':skipped,'complianceDisposition':'Eight source skip-map rows return before t.Run;275 admitted subtests,zero test skips.'},indent=2)+'\n')
