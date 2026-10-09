#!/usr/bin/env python3
"""Materialize authored example bytes for genuine resolver API observations.
These queries are an early search witness; only the loader observer proves graph closure.
"""
from pathlib import Path
import json
import sys
ROOT = Path(__file__).resolve().parents[2]
output = Path(sys.argv[1])
output.mkdir(parents=True, exist_ok=True)
rows = []
for name in ('package-graph', 'package-graph-invalid', 'package-checking', 'package-checking-invalid'):
    directory = ROOT / 'examples/resolution' / name
    logical = '/examples/resolution/' + name
    files = [{'Path': logical + '/' + str(p.relative_to(directory)), 'Text': p.read_text()}
             for p in sorted(directory.rglob('*')) if p.is_file()]
    files.append({'Path': '/examples/configuration/checking-base.json',
                  'Text': (ROOT / 'examples/configuration/checking-base.json').read_text()})
    if 'graph' in name:
        queries = [
            ('record-kit', 'src/main.ts', 99), ('record-kit', 'src/common.cts', 1),
            ('#local', 'src/main.ts', 99), ('@models/record', 'src/main.ts', 99),
            ('./cycle/a', 'src/main.ts', 99), ('./b', 'src/cycle/a.ts', 99),
            ('./a', 'src/cycle/b.ts', 99), ('./extra.js', 'src/main.ts', 99),
            ('./data.json', 'src/main.ts', 99), ('./view', 'src/main.ts', 99),
            ('react/jsx-runtime', 'src/view.tsx', 99), ('record-kit', 'src/main.ts', 99),
        ]
        if name.endswith('invalid'):
            queries += [('record-kit/missing', 'src/main.ts', 99), ('record-kit/blocked', 'src/main.ts', 99)]
    else:
        queries = [('record-kit', 'src/main.ts', 99), ('record-kit', 'src/main.ts', 99)]
    rows.append({'Name': name + '-resolver-api', 'Cwd': logical,
                 'ConfigPath': logical + '/tsconfig.json',
                 'ConfigText': (directory / 'tsconfig.json').read_text(), 'Files': files,
                 'Queries': [{'Kind': 'module', 'Name': module, 'Containing': logical + '/' + source, 'Mode': mode}
                             for module, source, mode in queries]})
(output / 'requests.jsonl').write_text(''.join(json.dumps(row, ensure_ascii=False) + '\n' for row in rows))
