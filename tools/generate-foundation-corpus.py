#!/usr/bin/env python3
"""Extract tracked upstream units through the original splitter, preserving provenance."""
import base64, collections, gzip, hashlib, json, pathlib, subprocess, tempfile
ROOT=pathlib.Path(__file__).resolve().parent.parent
CORPUS=ROOT/'validation/foundation-corpus'
def line(value): return json.dumps(value,ensure_ascii=True,separators=(',',':'))+'\n'
def main():
    catalog=json.loads((ROOT/'docs/foundation-test-catalog.json').read_text())
    inputs=[json.loads(s) for s in (CORPUS/'source-inputs.jsonl').read_text().splitlines()]
    inputs += [dict(path=row['path'],suite='fuzz-fixture') for row in catalog['finiteFuzzFixtureSeeds']]
    counts=collections.Counter(); modes=collections.Counter(); failures=[]; physical=set(); request_index=0
    with tempfile.NamedTemporaryFile(mode='w',suffix='.jsonl') as source:
        for row in inputs: source.write(line(row))
        source.flush()
        process=subprocess.Popen([str(ROOT/'tools/.cache/parser-oracle'),'--extract-foundation-files',source.name],cwd=ROOT,stdout=subprocess.PIPE,text=True)
        with (CORPUS/'source-requests.jsonl').open('w') as requests,(CORPUS/'source-units.jsonl').open('w') as units:
            for output in process.stdout:
                row=json.loads(output); physical.add(row['sourcePath']); counts['extractedUnits']+=1
                if row['extraction']=='failed':
                    failures.append(row); units.write(line(row)); continue
                raw=base64.b64decode(row.pop('textBytesBase64')); row['textSha256']=hashlib.sha256(raw).hexdigest();row['textBytes']=len(raw)
                kind=row['scriptKind'];extension=row['extension']; row['requestIndices']=[]
                suffix=pathlib.PurePosixPath(row['fileName']).suffix.lower();row['actualFilenameSuffix']=suffix
                # All JSON source units, including config/package inputs, are syntax inputs.
                # Source maps and build-info are JSON syntax assets, explicitly adapted.
                if kind==0 and suffix=='.tsbuildinfo':
                    # Original filename inference does not recognize this asset.
                    # Its actual content is serialized JSON; no build-info semantics.
                    json.loads(raw);kind=6;row['modeAdaptation']='Authored JSON syntax of original serialized build-info bytes; no build-info semantics'
                if kind==0 and suffix=='.map' and 'jsFileCompilationWithMapFileAsJs' in row['sourcePath']:
                    kind=1;row['modeAdaptation']='Authored JS-mode parse of original function source deliberately named .map; not original filename inference or compiler pass'
                if kind==0:
                    row['disposition']='excluded-asset'
                    row['reason']=('Custom content-mapper source requires an excluded host transform to interpret this filename/text.' if 'contentMapper' in row['sourcePath'] else 'Non-source module-resolution asset has no original filename-inferred syntax mode; actual suffix '+repr(suffix)+' and text hash retained.')
                    counts['excludedAssets']+=1
                else:
                    row['disposition']='input-only-reuse'; counts['parsedUnits']+=1
                    variants=row.get('parseOptionVariants') or [dict(jsx=False,force=False)]
                    for variant_index,variant in enumerate(variants):
                        name=f"{row['sourcePath']}::unit{row['unitIndex']}::config{variant_index}"
                        seed=row['suite'] in ('compiler','conformance','fuzz-fixture') and row['originalFuzzEligible'] and variant_index==0
                        request=dict(name=name,fileName=row['fileName'],scriptKind=kind,textBytesBase64=base64.b64encode(raw).decode(),externalModuleJSX=variant['jsx'],externalModuleForce=variant['force'],resolveJSDoc=True,foundationObservations=True,
                                     extension=extension,originalFuzzSeed=seed,originalFuzzJSX=False,originalFuzzForce=False)
                        if 'modeAdaptation' in row:request['modeAdaptation']=row['modeAdaptation'];counts['authoredModeAdaptationRequests']+=1
                        requests.write(line(request));row['requestIndices'].append(request_index); request_index+=1; modes[str(kind)]+=1
                        counts['originalFuzzSeeds']+=int(seed)
                units.write(line(row))
        if process.wait()!=0:raise RuntimeError('Original extraction process failed')
    fuzz_dir=ROOT/'upstream/TypeScript/tsc/internal/parser/testdata/fuzz/FuzzParser'
    output=subprocess.check_output([str(ROOT/'tools/.cache/parser-oracle'),'--extract-fuzz',str(fuzz_dir)],cwd=ROOT,text=True)
    with (CORPUS/'source-requests.jsonl').open('a') as requests:
        for encoded in output.splitlines():
            request=json.loads(encoded);request.update(foundationObservations=True,resolveJSDoc=True,originalFuzzSeed=True,extension=request['fileName'][6:],originalFuzzJSX=request['externalModuleJSX'],originalFuzzForce=request['externalModuleForce'])
            requests.write(line(request));request_index+=1;counts['committedFuzzSeeds']+=1;counts['originalFuzzSeeds']+=1;modes[str(request['scriptKind'])]+=1
    requests_path=CORPUS/'source-requests.jsonl'
    requests_path.with_suffix('.jsonl.gz').write_bytes(gzip.compress(requests_path.read_bytes(),compresslevel=9,mtime=0))
    units_path=CORPUS/'source-units.jsonl'
    units_path.with_suffix('.jsonl.gz').write_bytes(gzip.compress(units_path.read_bytes(),compresslevel=9,mtime=0))
    manifest=dict(upstreamRevision=catalog['upstreamRevision'],physicalInputs=len(inputs),physicalSourcesExtracted=len(physical),counts=dict(counts),requests=request_index,modeCounts=dict(modes),extractionFailures=failures,
                  scope='Original assertions are finite FuzzParser seed bodies only; compiler/transpile inputs are syntax/scanner input-only reuse.',
                  moduleDetectionMetadata='Original helper with zero source-file project/package metadata; actual JSX/Force flag variants deduplicated.',
                  expandedRequestsSha256=hashlib.sha256(requests_path.read_bytes()).hexdigest(),
                  files={name:hashlib.sha256((CORPUS/name).read_bytes()).hexdigest() for name in ('source-inputs.jsonl','source-units.jsonl.gz','source-requests.jsonl.gz')})
    (CORPUS/'source-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n'); print(json.dumps(manifest,separators=(',',':')))
    if failures:raise RuntimeError('Extraction failures require explicit disposition; retained in manifest')
if __name__=='__main__':main()
