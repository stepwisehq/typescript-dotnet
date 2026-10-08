#!/usr/bin/env python3
"""Describe observed lexical corpus coverage without claiming exhaustive correctness."""
import collections, hashlib, json, pathlib, re
root=pathlib.Path(__file__).resolve().parent.parent
up=root/'upstream/TypeScript/tsc'
requests=[json.loads(x) for x in (root/'validation/corpus/requests.jsonl').read_text().splitlines()]
expected=[json.loads(x) for x in (root/'validation/corpus/expected.jsonl').read_text().splitlines()]
references=set(re.findall(r'diagnostics\.([A-Za-z_0-9]+)', ''.join((up/f'internal/scanner/{f}').read_text() for f in ['scanner.go','regexp.go'])))-{'Message'}
mapping={name:int(code) for name,code in re.findall(r'var (\w+) = &Message\{code: (\d+)',(up/'internal/diagnostics/diagnostics_generated.go').read_text())}
codes=set(d['code'] for x in expected for d in x['diagnostics'])
report=dict(cases=len(requests),tokens=sum(len(x['tokens']) for x in expected),diagnostics=sum(len(x['diagnostics']) for x in expected),distinctTokenKinds=len(set(t['kind'] for x in expected for t in x['tokens'])),distinctDiagnosticCodes=len(codes),referencedLexicalDiagnostics=len(references),unobservedDiagnostics=[dict(name=name,code=mapping[name]) for name in sorted(references) if mapping[name] not in codes],utilityOperations=dict(sorted(collections.Counter(o['op'] for x in requests for o in x.get('utilityOperations',[])).items())),utilityResults=sum(len(x.get('utilityResults',[])) for x in expected),explicitOperations=dict(sorted(collections.Counter(o['op'] for x in requests for o in x.get('operations',[])).items())),requestsSha256=hashlib.sha256((root/'validation/corpus/requests.jsonl').read_bytes()).hexdigest(),expectedSha256=hashlib.sha256((root/'validation/corpus/expected.jsonl').read_bytes()).hexdigest())
(root/'validation/corpus/coverage.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,separators=(',',':')))
