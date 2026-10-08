#!/usr/bin/env python3
"""Bounded parser comparison: verified fixtures, canonical preflight, alternating samples."""
import argparse, base64, datetime, hashlib, json, os, pathlib, platform, statistics, subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent
ACCEPTED_HASH = 'd441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d'
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
parser = argparse.ArgumentParser()
parser.add_argument('--rounds', type=int, default=5)
parser.add_argument('--sample-ms', type=int, default=300)
parser.add_argument('--warmup-ms', type=int, default=150)
parser.add_argument('--prepare-only', action='store_true')
args = parser.parse_args()
if args.rounds < 1 or args.sample_ms < 1 or args.warmup_ms < 0:
    raise ValueError('Invalid benchmark duration/rounds')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def source_hash():
    return subprocess.check_output(['python3', str(ROOT/'tools/parser-source-hash.py')], text=True).strip()

before_hash = source_hash()
if before_hash != ACCEPTED_HASH:
    raise RuntimeError(f'Accepted parser source changed: {before_hash}')
if json.loads((ROOT/'upstream.lock.json').read_text())['revision'] != PIN:
    raise RuntimeError('Pinned upstream changed')
cache = ROOT/'tools/.cache/parser-benchmark-data'
cache.mkdir(parents=True, exist_ok=True)
output = ROOT/'artifacts/parser-benchmarks'
output.mkdir(parents=True, exist_ok=True)

upstream_requests = [json.loads(line) for line in (ROOT/'validation/parser-corpus/upstream-requests.jsonl').read_text().splitlines()]
fuzz_requests = [json.loads(line) for line in (ROOT/'validation/parser-corpus/fuzz-requests.jsonl').read_text().splitlines()]
real = [r for r in upstream_requests if '/packages/typescript/src/ast/' in r['name'] or '/RealWorld/parserharness.ts::' in r['name']]
tsx = [r for r in upstream_requests if r['scriptKind'] == 4 and '/jsxUnclosedParserRecovery.ts::' not in r['name']]
jsdoc = [r for r in upstream_requests if r['scriptKind'] == 1 and '/conformance/jsdoc/' in r['name']]
malformed = [r for r in upstream_requests if '/ErrorRecovery/IncompleteMemberVariables/' in r['name'] or '/jsxUnclosedParserRecovery.ts::' in r['name']] + fuzz_requests
workloads = {'real-typescript': real, 'tsx-microfixtures': tsx, 'eager-jsdoc-js-microfixtures': jsdoc, 'malformed-recovery': malformed}
manifest = {'upstreamCommit': PIN, 'acceptedSourceSha256': ACCEPTED_HASH,
            'mode': 'parse-source-file-plus-counters-and-diagnostics',
            'timedResolveJSDoc': False, 'workloads': []}
preflight = cache/'preflight'
preflight.mkdir(exist_ok=True)
primary_requests, supplemental_requests = [], []
for name, requests in workloads.items():
    if not requests:
        raise RuntimeError('Empty selected group: '+name)
    folder = cache/name
    folder.mkdir(exist_ok=True)
    inputs, files = [], []
    for i, original in enumerate(requests):
        data = base64.b64decode(original['textBytesBase64']) if original.get('textBytesBase64') is not None else original.get('text', '').encode()
        path = folder/f'{i:03}.bytes'
        path.write_bytes(data)
        file_name = original.get('fileName') or {1:'/index.js', 2:'/index.jsx', 3:'/index.ts', 4:'/index.tsx', 6:'/index.json'}[original['scriptKind']]
        item = dict(path=str(path), fileName=file_name, scriptKind=original['scriptKind'],
                    externalModuleJSX=original.get('externalModuleJSX', False), externalModuleForce=original.get('externalModuleForce', False))
        inputs.append(item)
        files.append(dict(corpusCase=original['name'], path=path.relative_to(ROOT).as_posix(), fileName=file_name,
                          scriptKind=item['scriptKind'], externalModuleJSX=item['externalModuleJSX'], externalModuleForce=item['externalModuleForce'],
                          bytes=len(data), sha256=sha(path)))
        request = dict(original, name=f'{name}-{i:03}', fileName=file_name, resolveJSDoc=False, textProbeIds=[])
        primary_requests.append(request)
        # Extra outside-timing reads expose already-eager JS JSDoc graphs; they do not force TS lazy parsing.
        if name == 'eager-jsdoc-js-microfixtures':
            supplemental_requests.append(dict(request, name=request['name']+'-eager-docs', resolveJSDoc=True))
    (folder/'workload.json').write_text(json.dumps(dict(files=inputs), indent=2)+'\n')
    manifest['workloads'].append(dict(name=name, inputBytes=sum(f['bytes'] for f in files), inputFiles=len(files),
                                      modeCounts={str(k):sum(f['scriptKind']==k for f in files) for k in sorted({f['scriptKind'] for f in files})}, files=files))
selected = primary_requests+supplemental_requests
requests_path = preflight/'selected-requests.jsonl'
requests_path.write_text(''.join(json.dumps(r, separators=(',', ':'))+'\n' for r in selected))
(ROOT/'benchmarks/parser-workloads.json').write_text(json.dumps(manifest, indent=2)+'\n')
if args.prepare_only:
    print(json.dumps({w['name']:dict(bytes=w['inputBytes'], files=w['inputFiles'], modes=w['modeCounts']) for w in manifest['workloads']}))
    raise SystemExit()

# Require existing accepted native validation evidence; no edits/rebuilds to accepted validation sources.
native_validator = ROOT/'artifacts/parser-validation/native/Parser.Validation'
accepted_report = json.loads((ROOT/'artifacts/parser-validation/native-result.json').read_text())
if accepted_report['sourceSha256'] != ACCEPTED_HASH or accepted_report['nativeSha256'] != sha(native_validator) or accepted_report['failures']:
    raise RuntimeError('Accepted native parser validator/evidence mismatch')
expected_path = preflight/'selected-expected.jsonl'
with requests_path.open('rb') as source, expected_path.open('wb') as destination:
    subprocess.run([str(ROOT/'tools/.cache/parser-oracle')], stdin=source, stdout=destination, check=True)
validation_report = output/'canonical-preflight.json'
subprocess.run(['python3', str(ROOT/'tools/compare-parser.py'), '--executable', str(native_validator), '--corpus', str(preflight), '--report', str(validation_report)], check=True)
preflight_data = json.loads(validation_report.read_text())
if preflight_data['cases'] != len(selected) or preflight_data['failures']:
    raise RuntimeError('Incomplete canonical preflight')

# Tether the timed observer to canonical oracle metadata, not merely agreement between adapters.
canonical_rows = [json.loads(line) for line in expected_path.read_text().splitlines()]
expected_signatures = {}
row_index = 0
mask = (1 << 64)-1
for name, cases in workloads.items():
    checksum = 14695981039346656037
    nodes = identifiers = texts = parse_diags = js_diags = jsdoc_diags = 0
    def mix(value):
        global checksum
        checksum = ((checksum ^ (value & mask))*1099511628211)&mask
    for _ in cases:
        row = canonical_rows[row_index]
        row_index += 1
        node, metadata = row['nodes'][0], row['metadata']
        for value in [node['kind'], node['pos'], node['end'], node['flags'], metadata['nodeCount'], metadata['identifierCount'], metadata['textCount']]:
            mix(value)
        nodes += metadata['nodeCount']
        identifiers += metadata['identifierCount']
        texts += metadata['textCount']
        for channel, key in enumerate(['parseDiagnostics', 'jsDiagnostics', 'jsDocDiagnostics']):
            diags = row[key]
            mix(channel)
            mix(len(diags))
            for diagnostic in diags:
                for key in ['code', 'pos', 'end', 'category']:
                    mix(diagnostic[key])
            if channel == 0: parse_diags += len(diags)
            elif channel == 1: js_diags += len(diags)
            else: jsdoc_diags += len(diags)
    definition = next(w for w in manifest['workloads'] if w['name'] == name)
    expected_signatures[name] = (definition['inputBytes'], len(cases), nodes, identifiers, texts, parse_diags, js_diags, jsdoc_diags, f'{checksum:016x}')

executables = {'go': ROOT/'tools/.cache/parser-bench', 'csharp-native-aot': output/'native/Parser.Benchmarks'}
source_paths = [ROOT/p for p in ['benchmarks/Parser.Benchmarks/Program.cs', 'benchmarks/Parser.Benchmarks/Parser.Benchmarks.csproj',
                              'tools/parser-benchmark/main.go', 'tools/parser-benchmark.py', 'tools/parser-benchmark.sh']]
validator_sources = list((ROOT/'tools/parser-oracle').glob('*.go'))+[ROOT/'tools/compare-parser.py', ROOT/'validation/Parser.Validation/Runner.cs', ROOT/'validation/Parser.Validation/Contracts.cs']
def hashes(paths):
    return {p.relative_to(ROOT).as_posix():sha(p) for p in paths}
source_hashes = hashes(source_paths)
validator_hashes = hashes(validator_sources)
binary_hashes = {name:sha(path) for name,path in executables.items()}
validator_binary_hashes = {'go':sha(ROOT/'tools/.cache/parser-oracle'), 'native':sha(native_validator)}
input_paths = [ROOT/f['path'] for w in manifest['workloads'] for f in w['files']]
workload_hashes = hashes(input_paths)
config_paths = [cache/name/'workload.json' for name in workloads]+[ROOT/'benchmarks/parser-workloads.json', requests_path, expected_path, validation_report]
config_hashes = hashes(config_paths)
allowed = sorted(os.sched_getaffinity(0)) if hasattr(os, 'sched_getaffinity') else []
cpu = allowed[0] if allowed else None
if cpu is None:
    raise RuntimeError('CPU affinity unavailable')
environment = os.environ.copy()
environment.update(GOMAXPROCS='1', DOTNET_PROCESSOR_COUNT='1', GOGC='100', GOMEMLIMIT='off')
raw, signatures = [], {}
for round_number in range(args.rounds):
    for name in workloads:
        order = ['go', 'csharp-native-aot'] if round_number % 2 == 0 else ['csharp-native-aot', 'go']
        for implementation in order:
            command = ['taskset', '-c', str(cpu), str(executables[implementation]), str(cache/name/'workload.json'), str(args.sample_ms), str(args.warmup_ms)]
            result = subprocess.run(command, env=environment, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, check=True)
            sample = json.loads(result.stdout)
            fields = ['inputBytes', 'inputFiles', 'nodesPerPass', 'identifiersPerPass', 'textsPerPass', 'parseDiagnosticsPerPass', 'jsDiagnosticsPerPass', 'jsDocDiagnosticsPerPass', 'checksum']
            signature = tuple(sample[f] for f in fields)
            if signature != expected_signatures[name]:
                raise RuntimeError(f'{name}: timed observer differs from canonical preflight {signature} != {expected_signatures[name]}')
            if name in signatures and signatures[name] != signature:
                raise RuntimeError(f'{name}: observer/input mismatch {signatures[name]} != {signature}')
            signatures[name] = signature
            if sample['implementation'] != implementation or sample['mode'] != manifest['mode']:
                raise RuntimeError('Benchmark implementation/mode mismatch')
            sample.update(workload=name, round=round_number+1, order=order.index(implementation)+1)
            sample['mibPerSecond'] = sample['inputBytes']*sample['iterations']/sample['elapsedSeconds']/1024**2
            sample['millisecondsPerPass'] = sample['elapsedSeconds']*1000/sample['iterations']
            sample['allocatedBytesPerInputByte'] = sample['allocatedBytes']/(sample['inputBytes']*sample['iterations'])
            raw.append(sample)
            print(f"{name} round{round_number+1} {implementation}: {sample['mibPerSecond']:.2f}MiB/s, {sample['allocatedBytesPerInputByte']:.3f}allocated bytes/input byte", flush=True)
if source_hash() != before_hash:
    raise RuntimeError('Accepted source changed during measurement')
if hashes(source_paths) != source_hashes or hashes(validator_sources) != validator_hashes:
    raise RuntimeError('Harness source changed during measurement')
if {name:sha(path) for name,path in executables.items()} != binary_hashes:
    raise RuntimeError('Benchmark executable changed during measurement')
if {'go':sha(ROOT/'tools/.cache/parser-oracle'), 'native':sha(native_validator)} != validator_binary_hashes:
    raise RuntimeError('Validator executable changed during measurement')
if hashes(config_paths) != config_hashes:
    raise RuntimeError('Workload/preflight options or config changed during measurement')
if hashes(input_paths) != workload_hashes:
    raise RuntimeError('Workload bytes changed during measurement')
summary = []
for name in workloads:
    metrics = {}
    for implementation in executables:
        matching = [s for s in raw if s['workload'] == name and s['implementation'] == implementation]
        throughputs = [s['mibPerSecond'] for s in matching]
        metrics[implementation] = dict(medianMiBPerSecond=statistics.median(throughputs), minMiBPerSecond=min(throughputs), maxMiBPerSecond=max(throughputs),
                                       medianMillisecondsPerPass=statistics.median(s['millisecondsPerPass'] for s in matching),
                                       medianAllocatedBytesPerInputByte=statistics.median(s['allocatedBytesPerInputByte'] for s in matching))
    metrics['csharpToGoThroughputRatio'] = metrics['csharp-native-aot']['medianMiBPerSecond']/metrics['go']['medianMiBPerSecond']
    summary.append(dict(workload=name, **metrics))
cpu_model = next((line.split(':',1)[1].strip() for line in pathlib.Path('/proc/cpuinfo').read_text().splitlines() if line.startswith('model name')), 'unknown')
report = dict(measuredAtUtc=datetime.datetime.now(datetime.UTC).isoformat(), mode=manifest['mode'], rounds=args.rounds,
              sampleMilliseconds=args.sample_ms, warmupMilliseconds=args.warmup_ms, cpuAffinity=cpu, allowedCpus=allowed, cpuModel=cpu_model,
              system=platform.platform(), environment={key:environment[key] for key in ['GOMAXPROCS','DOTNET_PROCESSOR_COUNT','GOGC','GOMEMLIMIT']},
              inheritedDotnetOverrides={key:value for key,value in environment.items() if key.startswith(('COMPlus_','DOTNET_')) and key not in ['DOTNET_ROOT','DOTNET_ROOT_X64','DOTNET_PROCESSOR_COUNT']},
              acceptedSourceSha256Before=before_hash, acceptedSourceSha256After=source_hash(), upstreamRevision=PIN,
              benchmarkSourceSha256=source_hashes, validatorSourceSha256=validator_hashes, executableSha256=binary_hashes, validatorExecutableSha256=validator_binary_hashes,
              canonicalPreflight=dict(cases=len(selected), timedOptionCases=len(primary_requests), supplementalEagerJSDocCases=len(supplemental_requests),
                                      reportSha256=sha(validation_report), requestsSha256=sha(requests_path), expectedSha256=sha(expected_path)),
              workloadManifestSha256=sha(ROOT/'benchmarks/parser-workloads.json'), workloadConfigSha256=config_hashes, expectedObserverSignatures={name:list(sig) for name,sig in expected_signatures.items()}, workloads=manifest['workloads'], samples=raw, summary=summary)
(ROOT/'benchmarks/parser-results.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(summary, indent=2))
