#!/usr/bin/env python3
"""Reuse finite pinned declaration compiler inputs; no original assertion claim."""
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT/'validation/declarations-corpus'
BASE = 'upstream/TypeScript/tsc/testdata/tests/cases/conformance/'
selected = [
 ('classes/constructorDeclarations/classConstructorOverloadsAccessibility.ts', 'D2/D3 accessibility and signature consistency'),
 ('classes/constructorDeclarations/constructorWithExpressionLessReturn.ts', 'D3 constructor return flow'),
 ('classes/constructorDeclarations/automaticConstructors/derivedClassWithoutExplicitConstructor.ts', 'D2 inherited generic default signatures'),
 ('classes/constructorDeclarations/superCalls/derivedClassConstructorWithoutSuperCall.ts', 'D3 actual super-flow validation including nested scopes'),
 ('classes/constructorDeclarations/superCalls/superPropertyInConstructorBeforeSuperCall.ts', 'D3 pre-super property use'),
 ('classes/constructorDeclarations/constructorParameters/readonlyConstructorAssignment.ts', 'D3 readonly parameter-property redeclaration and assignment'),
 ('classes/propertyMemberDeclarations/strictPropertyInitialization.ts', 'D3 full branch/return/#/ambient/static/computed initialization input'),
 ('classes/propertyMemberDeclarations/autoAccessorDisallowedModifiers.ts', 'D3 malformed auto-accessor modifier/parser recovery'),
 ('classes/classExpression.ts', 'D2 anonymous/named/contextual class expressions'),
 ('classes/mixinClassesAnonymous.ts', 'D2 generic anonymous mixin and spread constructor'),
 ('classes/mixinAccessModifiers.ts', 'D2/D3 private/protected intersection mixin origins'),
 ('classes/members/privateNames/privateNameUnused.ts', 'D3/D5 lexical-private unused lifecycle'),
 ('enums/enumConstantMembers.ts', 'D4 numeric/string/nonfinite const and ambient enum values'),
 ('enums/enumMergingErrors.ts', 'D4 full merged enum numbering/errors'),
 ('internalModules/DeclarationMerging/ModuleAndClassWithSameNameAndCommonRoot.ts', 'D4 cross-file namespace/class ordering and merged roots'),
 ('internalModules/DeclarationMerging/ModuleAndEnumWithSameNameAndCommonRoot.ts', 'D4 enum/namespace merge relationships'),
 ('interfaces/declarationMerging/mergedInterfaceAugmentationDiagnostics.ts', 'D1/D4 actual global/ambient/external incompatible augmentation'),
 ('types/uniqueSymbol/uniqueSymbolsPropertyNames.ts', 'D5 unique symbol property declarations'),
 ('classes/members/privateNames/privateNameEmitHelpers.ts', 'D3/D5 original old tslib imported private get/set helper arity'),
 ('classes/members/privateNames/privateNameStaticEmitHelpers.ts', 'D3/D5 original old tslib imported static private helper arity')]
excluded = [
 ('classes/members/accessibility/privateProtectedMembersAreNotAccessibleDestructuring.ts', 'Contains selected member visibility plus contract-external destructuring consumers; whole original input excluded, authored property-access origins covered separately.'),
 ('classes/members/privateNames/privateNameStaticMethodAsync.ts', 'Async/protocol body consumers external under D1-D6; no source rewrite.'),
 ('decorators/class/accessor/decoratorOnClassAccessor1.ts', 'Decorator execution external under D1-D6; no decorator erasure or expected suppression.')]
helper_path = ROOT/'tools/resolution-generate-compiler-inputs.py'
spec = importlib.util.spec_from_file_location('retained_compiler_input_extraction', helper_path)
helper = importlib.util.module_from_spec(spec);spec.loader.exec_module(helper)
sha = lambda data: hashlib.sha256(data).hexdigest()
dumps = lambda value: json.dumps(value,ensure_ascii=False,separators=(',', ':'))
generated = (ROOT/'upstream/TypeScript/tsc/internal/testrunner/options_generated.go').read_text()
varying = set(re.findall(r'"([a-z]+)"', generated))
declarations = (ROOT/'upstream/TypeScript/tsc/internal/tsoptions/declarations_generated.go').read_text()
names = {name.lower():name for name in re.findall(r'Name:\s*"([^"]+)"', declarations)}
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
 baseline_dir=ROOT/'upstream/TypeScript/tsc/testdata/baselines/reference/conformance'
 entry['baselineSourceFiles']={path.relative_to(ROOT).as_posix():sha(path.read_bytes()) for path in sorted(baseline_dir.glob(Path(source).stem+'.*')) if path.suffix in ('.types','.symbols') or path.name.endswith('.errors.txt')}
 for variant_index,variant in enumerate(helper.variations(options,varying)):
  values={'newLine':'crlf','skipDefaultLibCheck':True,'noErrorTruncation':True}
  values.update({names[key]:helper.option_value(key,value,cwd) for key,value in variant.items() if key not in helper.HARNESS_ONLY})
  last_only=bool(variant.get('noimplicitreferences','')) or 'require(' in units[-1][1] or re.search(r'reference\spath',units[-1][1]) is not None
  roots=units[-1:] if last_only else units
  files=[{'Path':name,'Text':text} for name,text in units]
  files += [{'Path':name,'Symlink':target} for name,target in links.items()]
  config=cwd+'/__declarations_compiler_input__.json'
  files.append({'Path':config,'Text':dumps({'compilerOptions':values,'files':[name for name,_ in roots]})})
  name=f'declaration-input-{index+1:02d}-{variant_index+1:02d}-{Path(source).stem}'
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
provenance=dict(baseline='e7b81a2',upstream='fed0bf24149fb1ed36039212648bafdafc1ea10e',
    classification='pinned-compiler-input-reuse-not-original-test-body-or-baseline-pass',
    fixtures=len(entries),reusedFixtures=len(selected),excludedFixtures=len(excluded),requests=len(requests),
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
