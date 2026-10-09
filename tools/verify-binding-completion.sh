#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
out=artifacts/binding-completion-validation
mkdir -p "$out"
./tools/build-binding-completion-oracle.sh
tools/.cache/binding-completion-oracle --support-fixtures > "$out/support-current.json"
cmp validation/binding-completion-corpus/support-fixtures.json "$out/support-current.json"
tools/.cache/binding-completion-oracle --helper-fixtures > "$out/helpers-current.json"
cmp validation/binding-completion-corpus/helper-fixtures.json "$out/helpers-current.json"
tools/.cache/binding-completion-oracle < validation/binding-completion-corpus/targeted-requests.jsonl > "$out/targeted-current.jsonl"
cmp validation/binding-completion-corpus/targeted-expected.jsonl "$out/targeted-current.jsonl"
# One retained cascade: all current xUnit, checker/data types, historical binder,
# original Go subsystem assertions, foundation shared bodies/finite seeds and
# full ordinary scan/syntax streams. No second cascading regression execution.
bash tools/verify-foundation.sh
# Complete both original numeric finite baseline argument sets. Foundation's
# frozen 1x calls are smoke only; these bounded calls are separately counted.
python3 - <<'PY'
import datetime,hashlib,json,pathlib,subprocess,time
out=pathlib.Path('artifacts/binding-completion-validation');old=json.loads(pathlib.Path('validation/results/foundation-go-numeric-fuzz-verified.json').read_text());start=time.monotonic();rows=[]
with (out/'original-go-numeric-finite.log').open('w') as log:
    for row in old['commands']:
        command=row['command'];log.write('Command: '+repr(command)+'\n');log.flush()
        result=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT)
        if result.returncode:raise SystemExit(result.returncode)
        rows.append(dict(command=command,expectedDeduplicatedBaselineSeeds=row['expectedDeduplicatedBaselineSeeds'],exitCode=0))
text=(out/'original-go-numeric-finite.log').read_text()
for row in rows:
    count=row['expectedDeduplicatedBaselineSeeds']
    if f'{count}/{count} completed' not in text:raise RuntimeError('Original Go numeric baseline incomplete')
(out/'original-go-numeric-finite.json').write_text(json.dumps(dict(commands=rows,elapsedSeconds=round(time.monotonic()-start,3),logSha256=hashlib.sha256(text.encode()).hexdigest(),logText=text,scope='Complete original deduplicated finite argument baselines; no unbounded mutation fuzz campaign.'),indent=2)+'\n')
PY
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc"); fi
dotnet publish validation/BindingCompletion.Validation/BindingCompletion.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/native"
sha256sum "$out/native/BindingCompletion.Validation" > "$out/native-sha256-before-execution.txt"
native="$repo_root/$out/native/BindingCompletion.Validation"
"$native" --counts > "$out/native-counts.json"
"$native" --support-checks > "$out/native-support.txt"
"$native" --helper-checks > "$out/native-helpers.txt"
targeted_count="$(python3 -c 'import json;print(json.load(open("validation/binding-completion-corpus/manifest.json"))["targetedCases"])')"
python3 tools/compare-binding-completion.py --executable "$native" --requests validation/binding-completion-corpus/targeted-requests.jsonl --expected-count "$targeted_count" --output "$out/targeted-native"
python3 tools/compare-binding-completion.py --executable "$native" --expected-count 17723 --output "$out/broad-native"
python3 - <<'PY'
import collections,hashlib,json,pathlib,xml.etree.ElementTree as ET
root=pathlib.Path('artifacts/binding-completion-validation');manifest=json.loads(pathlib.Path('validation/binding-completion-corpus/manifest.json').read_text())
counts=json.loads((root/'native-counts.json').read_text())
if counts!={'support':8,'helpers':7}:raise RuntimeError('Unexpected shared registry counts')
for group,key in [('support','support'),('helpers','helpers')]:
    lines=(root/('native-'+group+'.txt')).read_text().splitlines()
    if len([line for line in lines if line.startswith('PASS ')])!=counts[key] or lines[-1]!='TOTAL '+str(counts[key]):raise RuntimeError('Incomplete shared native assertions')
for group,want in [('targeted',manifest['targetedCases']),('broad',17723)]:
    report=json.loads((root/(group+'-native')/'report.json').read_text())
    if report['limited'] or report['incomplete'] or report['counts']['requests']!=want or report['counts']['exact']!=want or report['failures']:raise RuntimeError('Incomplete/failed binding '+group)
ns='{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}';tree=ET.parse('artifacts/binder-validation/binder.trx');methods=collections.Counter()
for row in tree.findall('.//'+ns+'UnitTest'):
    method=row.find(ns+'TestMethod')
    if method is not None:methods[method.attrib['className'].split('.')[-1]]+=1
for cls,want in [('BindingSupportTests',8),('BindingHelperTests',7),('CompletionOracleTests',manifest['targetedCases'])]:
    if methods[cls]!=want:raise RuntimeError('Incomplete shared xUnit '+cls+': '+str(methods[cls]))
binary=root/'native/BindingCompletion.Validation'
if hashlib.sha256(binary.read_bytes()).hexdigest()!=(root/'native-sha256-before-execution.txt').read_text().split()[0]:raise RuntimeError('Native binary drift')
PY
