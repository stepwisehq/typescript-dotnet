#!/usr/bin/env python3
"""Strict streamed original-Go/managed/native cold binding; failures remain dispositions."""
import argparse, collections, gzip, hashlib, importlib.util, json, pathlib, time
ROOT=pathlib.Path(__file__).resolve().parent.parent
spec=importlib.util.spec_from_file_location('foundation_comparison',ROOT/'tools/compare-foundation.py')
shared=importlib.util.module_from_spec(spec);spec.loader.exec_module(shared)

def request_rows(path):
    opener=gzip.open if path.suffix=='.gz' else open
    with opener(path,'rt') as stream:
        for line in stream:
            value=json.loads(line)
            # Preserve each source byte and parse option; only observation controls change.
            if 'parse' not in value:value={'parse':value,'warmBeforeBind':False,'resolveJSDocAfterBind':value.get('resolveJSDoc',False)}
            yield value

def bits(value):
    index=0
    while value:
        if value&1:yield str(1<<index)
        value>>=1;index+=1

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--executable',required=True);parser.add_argument('--requests',default='validation/foundation-corpus/source-requests.jsonl.gz');parser.add_argument('--output',default='artifacts/binding-completion-validation/broad-managed');parser.add_argument('--limit',type=int);parser.add_argument('--expected-count',type=int);args=parser.parse_args()
    out=ROOT/args.output;out.mkdir(parents=True,exist_ok=True);faildir=out/'mismatches';faildir.mkdir(exist_ok=True)
    oracle=shared.Process([str(ROOT/'tools/.cache/binding-completion-oracle')]);port=shared.Process([args.executable]);started=time.monotonic()
    counts=collections.Counter();modes=collections.Counter();options=collections.Counter();codes=collections.Counter();nodeflags=collections.Counter();symbolflags=collections.Counter();flowflags=collections.Counter();payloads=collections.Counter();boundfields=collections.Counter();metadata=collections.Counter();containerflags=collections.Counter();failures=[]
    with (out/'digests.jsonl').open('w') as digests:
        for index,request in enumerate(request_rows(ROOT/args.requests)):
            if args.limit is not None and index>=args.limit:break
            parse=request['parse'];name=parse.get('name','');modes[str(parse.get('scriptKind',3))]+=1
            options[json.dumps(dict(scriptKind=parse.get('scriptKind',3),externalModuleJSX=parse.get('externalModuleJSX',False),externalModuleForce=parse.get('externalModuleForce',False),declarationFile=parse.get('fileName','').endswith(('.d.ts','.d.mts','.d.cts')),warmBeforeBind=request.get('warmBeforeBind',False),resolveJSDocAfterBind=request.get('resolveJSDocAfterBind',False)),sort_keys=True,separators=(',',':'))]+=1
            line=json.dumps(request,separators=(',',':'))+'\n';want,go_error=oracle.request(line);got,port_error=port.request(line);counts['requests']+=1;entry=dict(index=index,name=name)
            if go_error or port_error:
                failure=dict(**entry,upstreamError=go_error,portError=port_error);failures.append(failure);counts['upstreamFailures']+=int(go_error is not None);counts['portFailures']+=int(port_error is not None)
                (faildir/f'{index:05d}-request.json').write_text(line);(faildir/f'{index:05d}-errors.json').write_text(json.dumps(failure,indent=2)+'\n');continue
            diff=shared.difference(want,got)
            if diff:
                failures.append(dict(**entry,difference=diff));counts['mismatches']+=1
                for label,value in [('request',request),('go',want),('csharp',got)]:(faildir/f'{index:05d}-{label}.json').write_text(json.dumps(value,separators=(',',':'))+'\n')
                if len(failures)<=25:print(name+': '+diff,flush=True)
            else:counts['exact']+=1
            for key,value in want['metadataNulls'].items():metadata[key+('=null' if value else '=allocated')]+=1
            for row in want['containerHelpers']:containerflags[str(row['flags'])]+=1
            graph=want['graph'];binding=graph['binding'];nodes=graph['after']['nodes']
            for key in ('symbols','tables','flows','flowLists'):counts[key]+=len(binding[key])
            counts['nodes']+=len(nodes);counts['allocatedEmptyTables']+=sum(not table['entries'] for table in binding['tables']);counts['nilBindingDiagnosticChannels']+=want['bindDiagnosticsNull'];counts['bindingDiagnostics']+=len(binding['diagnostics']);codes.update(shared.diagnostic_codes(binding['diagnostics']))
            for n in nodes:payloads[n['dataType']]+=1;nodeflags.update(bits(n['flags']))
            for s in binding['symbols']:symbolflags.update(bits(s['flags']))
            for f in binding['flows']:flowflags.update(bits(f['flags']))
            table_refs=collections.Counter();list_refs=collections.Counter()
            for node in binding['nodes']:
                for field in node['fields']:
                    boundfields[field['name']+':'+field['type']+('=null' if field['ref'] is None else '=reference')]+=1
                    if field['type']=='table' and field['ref'] is not None:table_refs[field['ref']]+=1
                    if field['type']=='flowList' and field['ref'] is not None:list_refs[field['ref']]+=1
            for symbol in binding['symbols']:
                for key in ('members','exports'):
                    if symbol[key] is not None:table_refs[symbol[key]]+=1
            if binding['globalExports'] is not None:table_refs[binding['globalExports']]+=1
            for flow in binding['flows']:
                if flow['antecedents'] is not None:list_refs[flow['antecedents']]+=1
            for row in binding['flowLists']:
                if row['next'] is not None:list_refs[row['next']]+=1
            counts['sharedDistinctTableSlots']+=sum(n>1 for n in table_refs.values());counts['sharedFlowListReferences']+=sum(n>1 for n in list_refs.values())
            entry.update(goSha256=shared.canonical_hash(want),csharpSha256=shared.canonical_hash(got),exact=diff is None);digests.write(json.dumps(entry,separators=(',',':'))+'\n')
            if (index+1)%1000==0:print(json.dumps(dict(completed=index+1,counts=dict(counts),elapsedSeconds=round(time.monotonic()-started,2))),flush=True)
    oracle.finish();port.finish()
    incomplete=args.expected_count is not None and counts['requests']!=args.expected_count
    report=dict(stage='binding-completion',limited=args.limit is not None,incomplete=incomplete,expectedCount=args.expected_count,requestsFile=args.requests,requestsSha256=hashlib.sha256((ROOT/args.requests).read_bytes()).hexdigest(),controlTransformation='Preserve original source and parse options; bind fresh before observer controls; resolve original requested JSDoc only after repeat bind.',counts=dict(counts),modeCounts=dict(modes),parseOptionCounts=dict(options),nodeFlagCounts=dict(sorted(nodeflags.items())),symbolFlagCounts=dict(sorted(symbolflags.items())),flowFlagCounts=dict(sorted(flowflags.items())),boundFieldCounts=dict(sorted(boundfields.items())),metadataNullCounts=dict(sorted(metadata.items())),containerFlagCounts=dict(sorted(containerflags.items())),payloadCounts=dict(sorted(payloads.items())),diagnosticCounts=dict(sorted(codes.items())),failures=failures,elapsedSeconds=round(time.monotonic()-started,3),digestsSha256=hashlib.sha256((out/'digests.jsonl').read_bytes()).hexdigest())
    (out/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(counts=dict(counts),failures=len(failures),elapsedSeconds=report['elapsedSeconds']),separators=(',',':')));raise SystemExit(bool(failures or incomplete))
if __name__=='__main__':main()
