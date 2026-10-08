#!/usr/bin/env python3
"""Select pinned syntax sources, using the Go upstream test-unit splitter."""
import collections,hashlib,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
upstream=root/'upstream/TypeScript'
base=upstream/'tsc/testdata/tests/cases/conformance'
by_directory=collections.defaultdict(list)
for path in (base/'parser').rglob('*'):
 if path.is_file() and path.suffix in ['.ts','.tsx','.js','.jsx','.json']:by_directory[path.parent].append(path)
selected=set()
for paths in by_directory.values():
 selected.add(sorted(paths)[0])
 # Add a median-sized representative where the first lexical file is tiny.
 selected.add(sorted(paths,key=lambda p:(p.stat().st_size,str(p)))[len(paths)//2])
for pattern in ['jsx/*Parse*.tsx','jsx/*Parser*.ts','jsdoc/*Parse*.ts','jsdoc/*Parse*.js']:
 selected.update(base.glob(pattern))
selected.update(upstream/'packages/typescript/src'/p for p in ['ast/visitor.ts','ast/modifiers.ts','ast/utils.ts'])
paths=sorted(selected)
manifest=[dict(path=p.relative_to(upstream).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),bytes=p.stat().st_size,license='Apache-2.0') for p in paths]
output=root/'validation/parser-corpus'
selection=output/'upstream-files.json'
selection.write_text(json.dumps([p.relative_to(root).as_posix() for p in paths],indent=2)+'\n')
requests=output/'upstream-requests.jsonl'
with requests.open('w') as stream:subprocess.run([str(root/'tools/.cache/parser-oracle'),'--extract-test-files',str(selection)],cwd=root,stdout=stream,check=True)
summary=dict(upstreamCommit='fed0bf24149fb1ed36039212648bafdafc1ea10e',selection='First lexical and median-size source per parser conformance directory; JSX/JSDoc parsing tests; three real TypeScript AST utility sources',files=manifest,units=len(requests.read_text().splitlines()),requestsSha256=hashlib.sha256(requests.read_bytes()).hexdigest())
(output/'upstream-manifest.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps(dict(files=len(paths),units=summary['units'],sourceBytes=sum(p.stat().st_size for p in paths),requestsSha256=summary['requestsSha256'])))
