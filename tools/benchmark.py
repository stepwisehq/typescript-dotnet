#!/usr/bin/env python3
"""Build reproducible workloads and run alternating scanner throughput samples."""
import argparse,datetime,hashlib,json,os,pathlib,platform,statistics,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser()
parser.add_argument('--rounds',type=int,default=5)
parser.add_argument('--sample-ms',type=int,default=300)
parser.add_argument('--warmup-ms',type=int,default=150)
parser.add_argument('--prepare-only',action='store_true')
args=parser.parse_args()
cache=root/'tools/.cache/benchmark-data';cache.mkdir(parents=True,exist_ok=True)
upstream=root/'upstream/TypeScript'
real=sorted((upstream/'packages/vscode-typescript/src').rglob('*.ts'))
if sum(p.stat().st_size for p in real)<100*1024: real+=sorted((upstream/'packages/vscode-typescript/test').rglob('*.ts'))
if not real: raise RuntimeError('No pinned upstream TypeScript source inputs')

def synthetic(name,unicode):
 chunks=[];size=0;i=0
 while size<256*1024:
  if unicode:
   text=f'const α{i} = "中文🦀\\uD800\\u{{1F980}}"; let café{i} = 0x1fffffffffffff + 9007199254740993 + {i}.125e-7; const 𐐀{i} = `line\\nΩ`;\n'
  else:
   text=f'const value{i} = {i}; // ordinary comment\nif (value{i} >= 100) value{i} += 1;\n'
  encoded=text.encode();chunks.append(encoded);size+=len(encoded);i+=1
 path=cache/(name+'.ts');path.write_bytes(b''.join(chunks));return path

workloads={"real-typescript":real,"ascii-synthetic":[synthetic('ascii',False)],"unicode-string-number":[synthetic('unicode',True)]}
manifest={"upstreamCommit":"fed0bf24149fb1ed36039212648bafdafc1ea10e","workloads":[]}
for name,files in workloads.items():
 listing=cache/(name+'.paths');listing.write_text(''.join(str(p)+'\n' for p in files))
 manifest['workloads'].append(dict(name=name,inputBytes=sum(p.stat().st_size for p in files),inputFiles=len(files),files=[dict(path=p.relative_to(root).as_posix(),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in files]))
(root/'benchmarks/workloads.json').write_text(json.dumps(manifest,indent=2)+'\n')
if args.prepare_only:
 print(json.dumps({w['name']:dict(bytes=w['inputBytes'],files=w['inputFiles']) for w in manifest['workloads']}));raise SystemExit()

executables={'go':root/'tools/.cache/scanner-bench','csharp-native-aot':root/'artifacts/benchmarks/native/Scanner.Benchmarks'}
allowed=sorted(os.sched_getaffinity(0)) if hasattr(os,'sched_getaffinity') else []
cpu=allowed[0] if allowed else None
environment=os.environ.copy();environment.update(GOMAXPROCS='1',DOTNET_PROCESSOR_COUNT='1',GOGC='100',GOMEMLIMIT='off')
benchmark_sources=[root/path for path in ['benchmarks/Scanner.Benchmarks/Program.cs','benchmarks/Scanner.Benchmarks/Scanner.Benchmarks.csproj','tools/benchmark/main.go','tools/benchmark.py','tools/benchmark.sh']]
def file_hashes(paths): return {p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
benchmark_source_hashes=file_hashes(benchmark_sources)
executable_hashes={name:hashlib.sha256(path.read_bytes()).hexdigest() for name,path in executables.items()}
workload_hashes=file_hashes([path for files in workloads.values() for path in files])
raw=[];expected={}
source_hash=subprocess.check_output(['python3',str(root/'tools/source-hash.py')],text=True).strip()
for round_number in range(args.rounds):
 for name in workloads:
  order=['go','csharp-native-aot'] if round_number%2==0 else ['csharp-native-aot','go']
  for implementation in order:
   command=[str(executables[implementation]),str(cache/(name+'.paths')),str(args.sample_ms),str(args.warmup_ms)]
   if cpu is not None: command=['taskset','-c',str(cpu)]+command
   completed=subprocess.run(command,env=environment,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,check=True)
   sample=json.loads(completed.stdout)
   signature=(sample['inputBytes'],sample['inputFiles'],sample['tokensPerPass'],sample['checksum'])
   if name in expected and expected[name]!=signature: raise RuntimeError(f'{name}: checksum/token/input mismatch: {expected[name]} vs {signature}')
   expected[name]=signature
   sample.update(workload=name,round=round_number+1,order=order.index(implementation)+1)
   sample['mibPerSecond']=sample['inputBytes']*sample['iterations']/sample['elapsedSeconds']/1024**2
   sample['allocatedBytesPerInputByte']=sample['allocatedBytes']/(sample['inputBytes']*sample['iterations'])
   raw.append(sample)
   print(f"{name} round {round_number+1} {implementation}: {sample['mibPerSecond']:.2f} MiB/s, {sample['allocatedBytesPerInputByte']:.3f} allocated bytes/input byte",flush=True)
source_hash_after=subprocess.check_output(['python3',str(root/'tools/source-hash.py')],text=True).strip()
if source_hash!=source_hash_after: raise RuntimeError('Library/verification source changed during benchmarking')
if file_hashes(benchmark_sources)!=benchmark_source_hashes: raise RuntimeError('Benchmark source changed during measurement')
if {name:hashlib.sha256(path.read_bytes()).hexdigest() for name,path in executables.items()}!=executable_hashes: raise RuntimeError('Benchmark executable changed during measurement')
if file_hashes([path for files in workloads.values() for path in files])!=workload_hashes: raise RuntimeError('Workload input changed during measurement')
summary=[]
for name in workloads:
 metrics={}
 for implementation in executables:
  matching=[s for s in raw if s['workload']==name and s['implementation']==implementation]
  throughputs=[s['mibPerSecond'] for s in matching];allocations=[s['allocatedBytesPerInputByte'] for s in matching]
  metrics[implementation]=dict(medianMiBPerSecond=statistics.median(throughputs),minMiBPerSecond=min(throughputs),maxMiBPerSecond=max(throughputs),medianAllocatedBytesPerInputByte=statistics.median(allocations))
 metrics['csharpToGoThroughputRatio']=metrics['csharp-native-aot']['medianMiBPerSecond']/metrics['go']['medianMiBPerSecond']
 summary.append(dict(workload=name,**metrics))
cpu_model=next((line.split(':',1)[1].strip() for line in pathlib.Path('/proc/cpuinfo').read_text().splitlines() if line.startswith('model name')),'unknown')
report=dict(measuredAtUtc=datetime.datetime.now(datetime.UTC).isoformat(),mode='reset-settext-scan',rounds=args.rounds,sampleMilliseconds=args.sample_ms,warmupMilliseconds=args.warmup_ms,cpuAffinity=cpu,allowedCpus=allowed,cpuModel=cpu_model,system=platform.platform(),environment=dict(GOMAXPROCS='1',DOTNET_PROCESSOR_COUNT='1',GOGC='100',GOMEMLIMIT='off'),inheritedDotnetOverrides={key:value for key,value in environment.items() if key.startswith(('COMPlus_','DOTNET_')) and key not in ['DOTNET_ROOT','DOTNET_ROOT_X64','DOTNET_PROCESSOR_COUNT']},benchmarkSourceSha256=benchmark_source_hashes,sourceTreeSha256=source_hash,executableSha256=executable_hashes,workloads=manifest['workloads'],samples=raw,summary=summary)
(root/'benchmarks/results.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(summary,indent=2))
