#!/usr/bin/env python3
"""Inventory independent object observations; do not mutate historical scalar assets."""
import collections, hashlib, json, pathlib
root = pathlib.Path(__file__).resolve().parent.parent
corpus = root/'validation/object-checker-corpus'
groups = []
rows = []
for request in sorted(corpus.glob('*-requests.jsonl')):
    expected = request.with_name(request.name.replace('-requests.jsonl', '-expected.jsonl'))
    values = [json.loads(line) for line in expected.read_text().splitlines()]
    if len(values) != len(request.read_text().splitlines()): raise RuntimeError('Incomplete fixture group')
    groups.append(dict(name=request.name.removesuffix('-requests.jsonl'), cases=len(values), requests=request.relative_to(root).as_posix(), expected=expected.relative_to(root).as_posix(), requestsSha256=hashlib.sha256(request.read_bytes()).hexdigest(), expectedSha256=hashlib.sha256(expected.read_bytes()).hexdigest()))
    rows.extend(values)
def count(values): return dict(sorted(collections.Counter(str(value) for value in values).items()))
def bits(values):
    values = list(values)
    return [i for i in range(32) if any(value & (1 << i) for value in values)]
def diagnostics(values):
    for value in values:
        yield value
        yield from diagnostics(value['chain'])
        yield from diagnostics(value['related'])
types = [value for row in rows for value in row['types']]
symbols = [value for row in rows for value in row['symbols']]
objects = [value for row in rows for value in row['objectGraph']['types']]
properties = [value for obj in objects for value in obj['properties']]
links = [value for row in rows for value in row['objectGraph']['propertyLinks']]
caches = [value for row in rows for value in row['objectGraph']['caches']]
wide = [value for row in rows for value in row['objectGraph']['widenings']]
key_checks = [value for row in rows for value in row['objectGraph']['relationKeys']]
if len(rows) != 43 or any(len(row['globalDiagnostics']) != 8 for row in rows): raise RuntimeError('Unexpected object corpus/environment')
report = dict(upstreamRevision='fed0bf24149fb1ed36039212648bafdafc1ea10e', cases=len(rows),
    astNodes=sum(len(row['parser']['nodes']) for row in rows), types=len(types), symbols=len(symbols),
    tables=sum(len(row['tables']) for row in rows), queries=sum(len(row['queries']) for row in rows),
    relations=sum(len(row['relations']) for row in rows), relationTrue=sum(rel['assignable'] for row in rows for rel in row['relations']),
    fileDiagnostics=sum(len(row['checkerDiagnostics']) for row in rows), globalDiagnostics=sum(len(row['globalDiagnostics']) for row in rows),
    topDiagnosticCodes=count(d['code'] for row in rows for d in row['checkerDiagnostics']),
    nestedDiagnosticCodes=count(d['code'] for row in rows for d in diagnostics(row['checkerDiagnostics'])),
    typePayloads=count(t['dataType'] for t in types), typeFlagBits=bits(t['flags'] for t in types), objectFlagBits=bits(obj['objectFlags'] for obj in objects),
    symbolFlagBits=bits(s['flags'] for s in symbols), checkFlagBits=bits(s['checkFlags'] for s in symbols),
    structuredTypes=len(objects), propertyEdges=len(properties), propertyModifierFlags=count(p['modifierFlags'] for p in properties),
    propertyDeclarationSymbolEdges=sum(len(p['declarationSymbols']) for p in properties),
    transientVsDeclarationSymbolDifferences=sum(s != p['symbol'] for p in properties for s in p['declarationSymbols'] if s is not None),
    rawPropertiesNull=sum(o['rawPropertiesNull'] for o in objects), publicPropertiesNull=sum(o['propertiesNull'] for o in objects),
    memberTablesNull=sum(o['members'] is None for o in objects),
    allocatedEmptyTables=sum(not table['entries'] for row in rows for table in row['tables']),
    sharedMemberTables=sum(len([obj for obj in row['objectGraph']['types'] if obj['members'] is not None])-len({obj['members'] for obj in row['objectGraph']['types'] if obj['members'] is not None}) for row in rows),
    boundSymbolMemberTableComparisons=sum(row['types'][o['type']]['symbol'] is not None for row in rows for o in row['objectGraph']['types']),
    boundSymbolMemberTableMatchesIncludingNull=sum(row['symbols'][row['types'][o['type']]['symbol']]['members'] == o['members'] for row in rows for o in row['objectGraph']['types'] if row['types'][o['type']]['symbol'] is not None),
    boundSymbolMemberTableNonNullAliases=sum(o['members'] is not None and row['symbols'][row['types'][o['type']]['symbol']]['members'] == o['members'] for row in rows for o in row['objectGraph']['types'] if row['types'][o['type']]['symbol'] is not None),
    globalsExportAliases=sum(row['symbols'][row['globalThisSymbol']]['exports'] == row['globals'] for row in rows),
    objectTargetEdges=sum(o['target'] is not None for o in objects), propertyLinks=len(links), absentPropertyLinks=sum(not link['present'] for link in links),
    propertyLinkEdges={key:sum(link[key] is not None for link in links) for key in ['target','resolvedType','writeType','nameType','containingType']},
    wideningObservations=len(wide), stableRepeatedWidenings=sum(w['repeatedIdentity'] for w in wide),
    distinctWidenedIdentities=sum(w['source'] != w['widened'] for w in wide),
    existingWidenedCacheEdges=sum(c['widened'] is not None for c in caches), existingRegularObjectCacheEdges=sum(c['regular'] is not None for c in caches),
    actualRelationKeyEqualityObservations=len(key_checks), relationKeyNames=count(k['name'] for k in key_checks),
    limits=['No named aliases/interfaces/type references/generic types/source unions, calls/signatures/indexes, writes, spread/computed property expressions, arrays or source control-flow extension.',
      'Single-file TS script noLib/noResolve; eight missing-library global diagnostics remain separate in every case.',
      'Public property discovery/type formatting/relations intentionally populate some caches in identical observation order; selected existing Widened/RegularObjectLiteral cache entries are read without forcing them.',
      'RegularObjectLiteral cache unhit if zero; union regularType and other unselected mutable caches remain unverified.',
      'Zero ObjectType.target/shared member table/property writeType/nameType/containingType edges mean those nonzero paths remain unexercised despite field comparison.',
      'Actual admitted object signature/index arrays must be empty; original Go mapper/instantiation presence asserts excluded generic payloads absent.',
      'Binder graph identities have earlier independent regression coverage; semantic graph adds property targets/types but does not repeat full flow JSON.',
      'Malformed parse recovery rejected; unsupported source syntax/API controls tested separately from original Go semantic agreement.'])
(corpus/'coverage.json').write_text(json.dumps(report, indent=2)+'\n')
manifest = dict(upstreamRevision=report['upstreamRevision'], cases=len(rows), groups=groups,
    retiredHistoricalAdmissionControls=['object','object-type'], historicalScalarEvidence='validation/results/checker-verified.json',
    observationOrder=json.loads((corpus/'tiny-manifest.json').read_text())['observationOrder'],
    instrumentation='Authored tools/checker-oracle/object_access.go copied as untracked inside original checker package; reads actual links/cache storage and invokes actual relation key. No tracked upstream modifications.',
    newAuthoredAdmissionControls=len(json.loads((corpus/'admission-controls.json').read_text())), currentHistoricalControls=33)
(corpus/'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
print(json.dumps({key:report[key] for key in ['cases','types','symbols','queries','relations','fileDiagnostics','globalDiagnostics','structuredTypes','propertyEdges','existingWidenedCacheEdges','existingRegularObjectCacheEdges']}))
