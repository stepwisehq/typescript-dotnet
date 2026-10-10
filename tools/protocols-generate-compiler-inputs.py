#!/usr/bin/env python3
"""Reuse source-selected pinned protocol compiler inputs; no original assertion claim."""
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT/'validation/protocols-corpus'
BASE = 'upstream/TypeScript/tsc/testdata/tests/cases/'
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
if subprocess.check_output(['git', '-C', str(ROOT/'upstream/TypeScript'), 'rev-parse', 'HEAD'], text=True).strip() != PIN:
 raise ValueError('Upstream pin changed')
SELECTED = [
    ('compiler/asyncFunctionReturnNonPromiseThenable.ts', 'P1 malformed callable thenables and async annotation'),
    ('compiler/unresolvableSelfReferencingAwaitedUnion.ts', 'P1 recursive awaited union/cycle errors'),
    ('compiler/awaitedTypeNoLib.ts', 'P1 thenable constraints with original absent libraries'),
    ('compiler/asyncFunctionReturnType.ts', 'P1 contextual tuples/indexed/generic async returns'),
    ('compiler/asyncFunctionReturnExpressionErrorSpans.ts', 'P1 nested async-return diagnostic locations'),
    ('compiler/asyncFunctionsAndStrictNullChecks.ts', 'P1 overloaded nullable thenable callbacks'),
    ('compiler/contextuallyTypeAsyncFunctionReturnTypeFromUnion.ts', 'P1 union-filtered async contextual inference'),
    ('compiler/noImplicitReturnsInAsync1.ts', 'P1 source noImplicitReturns policy'),
    ('compiler/awaitInNonAsyncFunction.ts', 'P1 await context grammar'),
    ('compiler/awaitObjectLiteral.ts', 'P1 top-level await suggestion and source module context'),
    ('compiler/modulePreserveTopLevelAwait1.ts', 'P1 original module Preserve await policy'),
    ('compiler/generatorReturnExpressionIsChecked.ts', 'P2 checked generator return expression'),
    ('compiler/yieldStarContextualType.ts', 'P2 delegated yield contextual return/next inference'),
    ('compiler/asyncYieldStarContextualType.ts', 'P2 async delegation/generic yielded result context'),
    ('compiler/typeOfYieldWithUnionInContextualReturnType.ts', 'P2 synchronous/asynchronous union context'),
    ('compiler/contextualTypeOnYield1.ts', 'P2 yield-operand generic contextual inference'),
    ('compiler/customAsyncIterator.ts', 'P3 custom AsyncIterator method/result model'),
    ('compiler/asyncIteratorExtraParameters.ts', 'P3 malformed async iterable required parameter'),
    ('compiler/iteratorExtraParameters.ts', 'P3 malformed synchronous iterable required parameter'),
    ('compiler/forAwaitForUnion.ts', 'P3 for-await sync/async union'),
    ('compiler/forAwaitForIntersection1.ts', 'P3 separate sync/async lookup order/cache behavior'),
    ('compiler/forAwaitInAsyncAccessor.ts', 'P3 invalid async accessor/for-await context'),
    ('compiler/iteratorsAndStrictNullChecks.ts', 'P3 nullable iterator method/results'),
    ('compiler/usingAsyncDisposable.ts', 'P4 disposable/async-disposable union and suggestion chain'),
    ('conformance/es6/yieldExpressions/generatorTypeCheck2.ts', 'P2 no-yield annotated generator'),
    ('conformance/es6/yieldExpressions/generatorTypeCheck45.ts', 'P2 contextual generic generator callbacks'),
    ('conformance/es6/yieldExpressions/generatorTypeCheck48.ts', 'P2 noImplicitAny yield/next'),
    ('conformance/es6/yieldExpressions/generatorOverloads1.ts', 'P2 generator overload grammar'),
    ('conformance/es6/yieldExpressions/generatorInAmbientContext1.ts', 'P2 ambient generator grammar'),
    ('conformance/es6/yieldExpressions/generatorNoImplicitReturns.ts', 'P2 generator return-path policy'),
    ('conformance/statements/VariableStatements/usingDeclarations/usingDeclarations.1.ts', 'P4 using scopes/targets/functions/classes/loops'),
    ('conformance/statements/VariableStatements/usingDeclarations/usingDeclarations.9.ts', 'P4 genuine missing Disposable global'),
    ('conformance/statements/VariableStatements/usingDeclarations/usingDeclarations.13.ts', 'P4 exported/ambient using grammar'),
    ('conformance/statements/VariableStatements/usingDeclarations/usingDeclarationsInForIn.ts', 'P4 for-in using rejection'),
    ('conformance/statements/VariableStatements/usingDeclarations/awaitUsingDeclarations.1.ts', 'P4 await-using scopes/targets'),
    ('conformance/statements/VariableStatements/usingDeclarations/awaitUsingDeclarations.4.ts', 'P4 await-using binding grammar'),
    ('conformance/statements/VariableStatements/usingDeclarations/awaitUsingDeclarations.16.ts', 'P4 ambient await-using grammar'),
    ('conformance/statements/VariableStatements/usingDeclarations/awaitUsingDeclarationsInForIn.ts', 'P4 for-in await-using rejection'),
    ('conformance/statements/VariableStatements/usingDeclarations/awaitUsingDeclarationsTopLevelOfModule.1.ts', 'P4 module/target top-level await-using'),
    ('conformance/statements/VariableStatements/usingDeclarations/usingDeclarationsWithImportHelpers.ts', 'P4 genuine missing disposal helper diagnostics'),
    ('conformance/statements/VariableStatements/usingDeclarations/awaitUsingDeclarationsWithImportHelpers.ts', 'P4 genuine missing async-disposal helper diagnostics'),
    ('conformance/statements/VariableStatements/usingDeclarations/awaitUsingDeclarationsWithAsyncIteratorObject.ts', 'P3/P4 built-in async iterator disposal'),
]
EXCLUDED = [
    ('compiler/jsFileCompilationAwaitModifier.ts', 'Whole JS file/declaration producer is subsequent JS campaign; original source/settings preserved, no JS erasure.'),
    ('conformance/statements/VariableStatements/usingDeclarations/usingDeclarationsWithESClassDecorators.1.ts', 'Whole decorator/resource interaction is subsequent decorator campaign; no decorator stripping or option replacement.'),
    ('compiler/asyncImportNestedYield.ts', 'Whole nested dynamic-import consumer is subsequent module/options campaign; await/yield/import source retained without consumer suppression.'),
    ('compiler/importHelpersNoHelpersForAsyncGenerators.ts', 'Whole input requests original removed moduleResolution classic; no rewrite to another mode. Original option disposition remains explicit, not a pass.'),
]
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
for index,(relative,domain) in enumerate(SELECTED):
 source=BASE+relative;data=(ROOT/source).read_bytes()
 units,links,options,cwd=helper.extract(data,source)
 unknown=set(options)-helper.HARNESS_ONLY-set(names)
 if unknown: raise ValueError('Unaccounted compiler directive '+repr(unknown))
 if any(Path(name).name in ('tsconfig.json','jsconfig.json') for name,_ in units):
  raise ValueError('Selected finite inputs must not need post-discovery config option overlay')
 entry=dict(source=source,sourceSha256=sha(data),domain=domain,disposition='reused-compiler-input-semantic-differential',
            originalAssertionsClaimed=0,requestNames=[],variants=[])
 baseline_dir=ROOT/'upstream/TypeScript/tsc/testdata/baselines/reference'/Path(relative).parts[0]
 entry['baselineSourceFiles']={path.relative_to(ROOT).as_posix():sha(path.read_bytes()) for path in sorted(baseline_dir.glob(Path(source).stem+'*')) if path.name.startswith((Path(source).stem+'.',Path(source).stem+'(')) and (path.suffix in ('.types','.symbols') or path.name.endswith('.errors.txt'))}
 for variant_index,variant in enumerate(helper.variations(options,varying)):
  values={'newLine':'crlf','skipDefaultLibCheck':True,'noErrorTruncation':True}
  values.update({names[key]:helper.option_value(key,value,cwd) for key,value in variant.items() if key not in helper.HARNESS_ONLY})
  last_only=bool(variant.get('noimplicitreferences','')) or 'require(' in units[-1][1] or re.search(r'reference\spath',units[-1][1]) is not None
  roots=[(path,text) for path,text in (units[-1:] if last_only else units) if not path.endswith(('.json','.tsbuildinfo'))]
  files=[{'Path':name,'Text':text} for name,text in units]
  files += [{'Path':name,'Symlink':target} for name,target in links.items()]
  config=cwd+'/__protocol_compiler_input__.json'
  files.append({'Path':config,'Text':dumps({'compilerOptions':values,'files':[name for name,_ in roots]})})
  name=f'protocol-input-{index+1:02d}-{variant_index+1:02d}-{Path(source).stem}'
  request=dict(Name=name,ConfigPath=config,Files=files,Bind=True,SingleThreaded=True,Semantic=dict(PerFile=True,Repeat=True))
  requests.append(request);entry['requestNames'].append(name)
  entry['variants'].append(dict(name=name,compilerDirectives=variant,requestSha256=sha((dumps(request)+'\n').encode())))
 entry['adaptations']={'harnessOnly': {key:value for key,value in options.items() if key in helper.HARNESS_ONLY}, 'extraction':'Existing source-reviewed regular compiler parser removes directives and joins CRLF/LF with LF; all extracted source bytes retained.',
  'options':'Original regular compiler harness defaults plus actual directive variants serialized into authored config. Real libraries loaded; no noEmit override, library reduction or checker diagnostic filter.',
  'configMetadata':'Authored configuration file seeds ConfigFilePath and source metadata; not original harness direct option ingestion. No selected input uses embedded configuration or output remapping.',
  'externalOperations':'Original declaration-diagnostic/output, Emit-induced diagnostics and pre/post-emit invariant, JS/source-map output and full type/symbol baseline traversal not executed. Genuine pre-emit config/Program/syntactic/semantic/global records compared; original baseline bytes are source provenance, not baseline-pass claims.'}
 entry['execution']={'original':'reference recorded separately in compiler-inputs-reference-provenance.json','managed':'pending','native':'pending'};entries.append(entry)
for relative,reason in EXCLUDED:
 source=BASE+relative;entries.append(dict(source=source,sourceSha256=sha((ROOT/source).read_bytes()),
    disposition='excluded-whole-input-external-body-consumer',boundary='original-removed-option' if 'removed moduleResolution' in reason else 'subsequent-language-consumer',reason=reason,requestNames=[],originalAssertionsClaimed=0))
source_files=['tools/protocols-generate-compiler-inputs.py','tools/resolution-generate-compiler-inputs.py','upstream/TypeScript/tsc/internal/testrunner/test_case_parser.go',
 'upstream/TypeScript/tsc/internal/testrunner/compiler_runner.go','upstream/TypeScript/tsc/internal/testrunner/options_generated.go',
 'upstream/TypeScript/tsc/internal/testutil/harnessutil/harnessutil.go','upstream/TypeScript/tsc/internal/tsoptions/declarations_generated.go']
if len({request['Name'] for request in requests}) != len(requests):
 raise ValueError('Duplicate request name')
payload=''.join(dumps(row)+'\n' for row in requests).encode()
provenance=dict(baseline='bb035b3',upstream='fed0bf24149fb1ed36039212648bafdafc1ea10e',
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
