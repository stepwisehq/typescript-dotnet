#!/usr/bin/env python3
"""Reuse source-selected pinned type-system compiler inputs; no original assertion claim."""
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT/'validation/typesystem-corpus'
BASE = 'upstream/TypeScript/tsc/testdata/tests/cases/'
selected = [('compiler/stringMappingSpecialCasing.ts', 'T2 intrinsic/string templates'), ('compiler/stringMappingAssignability.ts', 'T2 intrinsic/string templates'), ('compiler/intrinsicStringMappingLoneSurrogate.ts', 'T2 intrinsic/string templates'), ('compiler/templateLiteralInferenceSupplementarySplit.ts', 'T2 intrinsic/string templates'), ('compiler/templateLiteralInferenceLoneSurrogate.ts', 'T2 intrinsic/string templates'), ('compiler/keyRemappingKeyofResult.ts', 'T2 key remapping/indexed types'), ('compiler/keyRemappingKeyofResult2.ts', 'T2 key remapping/indexed types'), ('compiler/mappedTypeRecursiveInference.ts', 'T1/T2 recursive mapped instantiation'), ('compiler/mappedTypeRecursiveInference2.ts', 'T1/T2 recursive mapped instantiation'), ('compiler/reverseMappedTypeConditionalTemplateInference.ts', 'T3 reverse-mapped/contextual inference'), ('compiler/reverseMappedContravariantInference.ts', 'T3 reverse-mapped/contextual inference'), ('compiler/reverseMappedPartiallyInferableTypes.ts', 'T3 reverse-mapped/contextual inference'), ('compiler/reverseMappedUnionInference.ts', 'T3 reverse-mapped/contextual inference'), ('compiler/reverseMappedIntersectionInference1.ts', 'T3 reverse-mapped/contextual inference'), ('compiler/reverseMappedTupleContext.ts', 'T3 reverse-mapped/contextual inference'), ('compiler/reverseMappedTypeAssignableToIndex.ts', 'T3 reverse-mapped/contextual inference'), ('compiler/inferFromTupleRestAndVariadic.ts', 'T1/T3/T5 generic tuple/constraints/variance'), ('compiler/varianceAnnotationValidation.ts', 'T1/T3/T5 generic tuple/constraints/variance'), ('compiler/inferTypeConstraintInstantiationCircularity.ts', 'T1/T3/T5 generic tuple/constraints/variance'), ('compiler/noInferCommonPropertyCheck1.ts', 'T3 NoInfer/inference priorities'), ('compiler/noInferUnionExcessPropertyCheck1.ts', 'T3 NoInfer/inference priorities'), ('compiler/conditionalTypeAnyUnion.ts', 'T1/T2/T4 conditional relation/recursion'), ('compiler/conditionalTypeAssignabilityWhenDeferred.ts', 'T1/T2/T4 conditional relation/recursion'), ('compiler/conditionalTypeGenericInSignatureTypeParameterConstraint.ts', 'T1/T2/T4 conditional relation/recursion'), ('compiler/conditionalTypeRelaxingConstraintAssignability.ts', 'T1/T2/T4 conditional relation/recursion'), ('compiler/recursiveConditionalTypes.ts', 'T1/T2/T4 conditional relation/recursion'), ('compiler/assertionFunctionsCanNarrowByDiscriminant.ts', 'T3/T5 generic callable/predicate inference'), ('compiler/genericInferenceDefaultTypeParameter.ts', 'T3/T5 generic callable/predicate inference'), ('compiler/genericCallInferenceWithGenericLocalFunction.ts', 'T3/T5 generic callable/predicate inference'), ('compiler/genericFunctionsAndConditionalInference.ts', 'T3/T5 generic callable/predicate inference'), ('conformance/types/typeRelationships/typeInference/genericCallTypeArgumentInference.ts', 'T3 generic call inference'), ('conformance/types/typeRelationships/typeInference/genericCallWithConstraintsTypeArgumentInference.ts', 'T1/T3 call constraints')]
excluded = [('compiler/genericInferenceDefaultTypeParameterJsxReact.tsx', 'Whole JSX/React consumer external under T1-T6; preserve input, no JSX erasure.'), ('conformance/statements/for-await-ofStatements/emitter.forAwait.ts', 'Async iteration execution external under T1-T6; no await erasure.'), ('conformance/dynamicImport/importCallExpressionAsyncES6CJS.ts', 'Dynamic import/async execution external under T1-T6; no consumer suppression.')]
helper_path = ROOT/'tools/resolution-generate-compiler-inputs.py'
spec = importlib.util.spec_from_file_location('retained_compiler_input_extraction', helper_path)
helper = importlib.util.module_from_spec(spec);spec.loader.exec_module(helper)
sha = lambda data: hashlib.sha256(data).hexdigest()
dumps = lambda value: json.dumps(value,ensure_ascii=False,separators=(',', ':'))
generated = (ROOT/'upstream/TypeScript/tsc/internal/testrunner/options_generated.go').read_text()
varying = set(re.findall(r'"([a-z]+)"', generated))
expressions = (ROOT/'upstream/TypeScript/tsc/internal/tsoptions/declarations_generated.go').read_text()
names = {name.lower():name for name in re.findall(r'Name:\s*"([^"]+)"', expressions)}
requests=[];entries=[]
for index,(relative,domain) in enumerate(selected):
 source=BASE+relative;data=(ROOT/source).read_bytes()
 units,links,options,cwd=helper.extract(data,source)
 unknown=set(options)-helper.HARNESS_ONLY-set(names)
 if unknown: raise ValueError('Unaccounted compiler directive '+repr(unknown))
 if any(Path(name).name in ('tsconfig.json','jsconfig.json') for name,_ in units):
  raise ValueError('Selected finite inputs must not need post-discovery config option overlay')
 entry=dict(source=source,sourceSha256=sha(data),domain=domain,disposition='reused-compiler-input-semantic-differential',
            originalAssertionsClaimed=0,requestNames=[],variants=[])
 baseline_dir=ROOT/'upstream/TypeScript/tsc/testdata/baselines/reference'/Path(relative).parts[0]
 entry['baselineSourceFiles']={path.relative_to(ROOT).as_posix():sha(path.read_bytes()) for path in sorted(baseline_dir.glob(Path(source).stem+'.*')) if path.suffix in ('.types','.symbols') or path.name.endswith('.errors.txt')}
 for variant_index,variant in enumerate(helper.variations(options,varying)):
  values={'newLine':'crlf','skipDefaultLibCheck':True,'noErrorTruncation':True}
  values.update({names[key]:helper.option_value(key,value,cwd) for key,value in variant.items() if key not in helper.HARNESS_ONLY})
  last_only=bool(variant.get('noimplicitreferences','')) or 'require(' in units[-1][1] or re.search(r'reference\spath',units[-1][1]) is not None
  roots=units[-1:] if last_only else units
  files=[{'Path':name,'Text':text} for name,text in units]
  files += [{'Path':name,'Symlink':target} for name,target in links.items()]
  config=cwd+'/__typesystem_compiler_input__.json'
  files.append({'Path':config,'Text':dumps({'compilerOptions':values,'files':[name for name,_ in roots]})})
  name=f'typesystem-input-{index+1:02d}-{variant_index+1:02d}-{Path(source).stem}'
  request=dict(Name=name,ConfigPath=config,Files=files,Bind=True,SingleThreaded=True,Semantic=dict(PerFile=True,Repeat=True))
  requests.append(request);entry['requestNames'].append(name)
  entry['variants'].append(dict(name=name,compilerDirectives=variant,requestSha256=sha((dumps(request)+'\n').encode())))
 entry['adaptations']={'extraction':'Existing source-reviewed regular compiler parser removes directives and joins CRLF/LF with LF; all extracted source bytes retained.',
  'options':'Original regular compiler harness defaults plus actual directive variants serialized into authored config. Real libraries loaded; no noEmit override, library reduction or checker diagnostic filter.',
  'configMetadata':'Authored configuration file seeds ConfigFilePath and source metadata; not original harness direct option ingestion. No selected input uses embedded configuration or output remapping.',
  'externalOperations':'Original emit/declaration-output and full type/symbol baseline traversal not executed. Baseline bytes hashed only as original source provenance, not falsely claimed matching.'}
 entry['execution']={'original':'pending','managed':'pending','native':'pending'};entries.append(entry)
for relative,reason in excluded:
 source=BASE+relative;entries.append(dict(source=source,sourceSha256=sha((ROOT/source).read_bytes()),
    disposition='excluded-whole-input-external-body-consumer',reason=reason,requestNames=[],originalAssertionsClaimed=0))
source_files=['tools/resolution-generate-compiler-inputs.py','upstream/TypeScript/tsc/internal/testrunner/test_case_parser.go',
 'upstream/TypeScript/tsc/internal/testrunner/compiler_runner.go','upstream/TypeScript/tsc/internal/testrunner/options_generated.go',
 'upstream/TypeScript/tsc/internal/testutil/harnessutil/harnessutil.go','upstream/TypeScript/tsc/internal/tsoptions/declarations_generated.go']
payload=''.join(dumps(row)+'\n' for row in requests).encode()
provenance=dict(baseline='09630d6',upstream='fed0bf24149fb1ed36039212648bafdafc1ea10e',
    classification='pinned-compiler-input-reuse-not-original-test-body-or-baseline-pass',
    fixtures=len(entries),reusedFixtures=sum(e['disposition']=='reused-compiler-input-semantic-differential' for e in entries),
    excludedFixtures=sum(e['disposition']=='excluded-whole-input-external-body-consumer' for e in entries),requests=len(requests),
    requestsSha256=sha(payload),sourceFiles={name:sha((ROOT/name).read_bytes()) for name in source_files},inputs=entries)
provenance_bytes=(json.dumps(provenance,indent=2)+'\n').encode()
if '--check' in sys.argv:
 assert (OUT/'requests-compiler-inputs.jsonl').read_bytes()==payload
 assert (OUT/'requests-compiler-inputs-provenance.json').read_bytes()==provenance_bytes
else:
 OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'requests-compiler-inputs.jsonl').write_bytes(payload)
 (OUT/'requests-compiler-inputs-provenance.json').write_bytes(provenance_bytes)
print(json.dumps({key:provenance[key] for key in ('fixtures','reusedFixtures','excludedFixtures','requests','requestsSha256')}))
