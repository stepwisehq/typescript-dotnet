#!/usr/bin/env python3
"""Observe original Go stores through an untracked same-package test helper."""
import argparse,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--output',default=str(root/'validation/checker-corpus/utility-expected.json'));args=parser.parse_args()
source=root/'tools/checker-oracle/utilities_test.go'
target=root/'upstream/TypeScript/tsc/internal/checker/oracle_semantic_utilities_test.go'
if target.exists() and not target.read_text().startswith('// Independent observations'):raise RuntimeError('Refusing to replace unknown upstream helper')
target.write_bytes(source.read_bytes())
result=subprocess.run([str(root/'tools/.cache/go/bin/go'),'-C',str(root/'upstream/TypeScript/tsc'),'test','./internal/checker','-run','^TestOracleSemanticUtilities$','-count=1','-v'],text=True,capture_output=True,check=True)
rows=[line.removeprefix('UTILITY_RESULT:') for line in result.stdout.splitlines() if line.startswith('UTILITY_RESULT:')]
if len(rows)!=1:raise RuntimeError('Missing independent utility observation result')
value=json.loads(rows[0]);path=pathlib.Path(args.output);path.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps({'observations':len(value),'path':str(path)}))
