#!/usr/bin/env python3
"""Observed semantic contracts and honest nonzero-field coverage, not a quota."""
import collections,hashlib,json,pathlib
root=pathlib.Path(__file__).resolve().parent.parent;folder=root/'validation/data-types-corpus'
rows=[];groups=[];contract_groups=collections.defaultdict(list)
for group in ['tiny','matrix']:
    request=folder/(group+'-requests.jsonl');expected=folder/(group+'-expected.jsonl')
    requests=[json.loads(s) for s in request.read_text().splitlines()];values=[json.loads(s) for s in expected.read_text().splitlines()]
    if len(requests)!=len(values) or any(not r['dataObservations'] for r in requests):raise RuntimeError('Missing richer observations')
    if any(r['parser']['parseDiagnostics'] for r in values):raise RuntimeError('Semantic specimen malformed; preserve/report upstream recovery separately')
    rows.extend(values)
    groups.append(dict(name=group,cases=len(values),requests=request.relative_to(root).as_posix(),expected=expected.relative_to(root).as_posix(),requestsSha256=hashlib.sha256(request.read_bytes()).hexdigest(),expectedSha256=hashlib.sha256(expected.read_bytes()).hexdigest()))
    for item in json.loads((folder/(group+'-manifest.json')).read_text())['groups']:contract_groups[item['contract']].append(item)
def counts(items):return dict(sorted(collections.Counter(str(x) for x in items).items()))
def walk_diagnostics(items):
    for x in items:yield x;yield from walk_diagnostics(x['chain']);yield from walk_diagnostics(x['related'])
types=[t for r in rows for t in r['types']];symbols=[s for r in rows for s in r['symbols']]
records=[t for r in rows for t in r['dataGraph']['types']]
links=[l for r in rows for l in r['dataGraph']['links']];fields=[f for t in records for f in t['fields']]
aliases=[a for r in rows for a in r['dataGraph']['aliases']];mappers=[m for r in rows for m in r['dataGraph']['mappers']];indexes=[i for r in rows for i in r['dataGraph']['indexes']]
allfields=fields+[f for t in aliases+mappers+indexes for f in t['fields']]+[f for l in links for f in l['fields']]
keys=[k for r in rows for k in r['dataGraph']['keys']]
properties_cache=[entry for record in records for entry in record['propertiesCache']]
field_stats={}
for name in sorted({f['name'] for f in allfields}):
    values=[f for f in allfields if f['name']==name]
    field_stats[name]=dict(observations=len(values),kinds=counts(f['kind'] for f in values),null=sum(f['null'] for f in values),nonNullRefs=sum(f['ref'] is not None for f in values),listEdges=sum(len(f['refs']) for f in values),mapEdges=sum(len(f['entries'])+len(f['namedEntries']) for f in values),true=sum(f['bool'] for f in values),nonzeroNumbers=sum(f['number']!=0 for f in values),nonemptyBytes=sum(bool(f['bytes']) for f in values))
report=dict(upstreamRevision='fed0bf24149fb1ed36039212648bafdafc1ea10e',cases=len(rows),
    astNodes=sum(len(r['parser']['nodes']) for r in rows),types=len(types),symbols=len(symbols),tables=sum(len(r['tables']) for r in rows),queries=sum(len(r['queries']) for r in rows),relations=sum(len(r['relations']) for r in rows),
    relationTrue=sum(q['assignable'] for r in rows for q in r['relations']),
    diagnosticChannels={c:dict(count=sum(len(r[c]) for r in rows),codes=counts(d['code'] for r in rows for d in walk_diagnostics(r[c]))) for c in ['bindDiagnostics','checkerDiagnostics','globalDiagnostics']},
    globalDiagnosticCounts=counts(len(r['globalDiagnostics']) for r in rows),typePayloads=counts(t['dataType'] for t in types),
    aliasIdentities=len(aliases),retainedMapperIdentities=len(mappers),internalIndexIdentities=len(indexes),
    declaredTypeObservations=sum(len(r['dataGraph']['declared']) for r in rows),stableRepeatedDeclaredTypes=sum(x['repeatedIdentity'] for r in rows for x in r['dataGraph']['declared']),
    propertyEdges=sum(len(t['properties']) for t in records),
    propertiesTypesCache=dict(mapNullObservations=sum(r['dataGraph']['propertiesCacheNull'] for r in rows),ownersWithEntries=sum(bool(t['propertiesCache']) for t in records),entries=len(properties_cache),includeFlags=counts(e['include'] for e in properties_cache),includeOrigin=sum(e['includeOrigin'] for e in properties_cache),valueEdges=sum(e['value'] is not None for e in properties_cache)),
    linkPresence={name:dict(observed=sum(l['name']==name for l in links),present=sum(l['name']==name and l['present'] for l in links)) for name in sorted({l['name'] for l in links})},
    resolvedNodeSymbols=sum(x['symbol'] is not None for r in rows for x in r['dataGraph']['resolvedSymbols']),
    allocatedEmptyTables=sum(not t['entries'] for r in rows for t in r['tables']),
    fieldObservations=field_stats,
    keyObservations={name:dict(observed=sum(k['name']==name for k in keys),true=sum(k['name']==name and k['boolValue'] for k in keys)) for name in sorted({k['name'] for k in keys})},
    limits=[
        'No checker/project/library/cross-file/module/emit claim beyond D1-D7 single-file admitted source; unsupported syntax tested independently.',
        'Source generics/this/index signatures/signatures excluded, while real hidden-this references/type parameters and any-base internal IndexInfo remain represented.',
        'Physical untouched paged symbolNodeLinks slot presence excluded because global AST-ID page placement differs; actual resolvedSymbol content preserved and legacy20 fixed-ID store observations retained.',
        'Go payload fields independently flattened with unknown-type failure; intrinsic/literal payloads remain authoritative in existing base graph, Type owner/id/data backreferences explicitly omitted.',
        'Opaque hash bits intentionally excluded under managed complete-preimage adaptation; actual map presence/cardinality/value identities plus populated reference key-to-instance and composite intern membership/equality checks retained. Other unpopulated associations remain unverified.',
        'Cache-only values seed by unique flags/text/raw-symbol/declaration/argument descriptor; collision fails instead of merging identities.',
        'Selected node/type-node/value/type-alias/declared/deferred/mapped/reference/module/merged stores, actual propertiesTypes entries for queued rooted types (include/includeOrigin/value association), and Widened/RegularObjectLiteral caches observed; other checker stores/caches remain unverified.',
        'Retained Mapper records may remain zero; hidden-this arguments/targets/results and actual generic-reference key paths are observed, source-excluded mapper variants unverified.',
        'Lists and diagnostic ordering retained; unordered tables/maps sorted canonically, nil distinct from allocated empty.',
    ])
(folder/'coverage.json').write_text(json.dumps(report,indent=2)+'\n')
manifest=dict(upstreamRevision=report['upstreamRevision'],cases=len(rows),groups=groups,contractGroups=dict(sorted(contract_groups.items())),
    historicalBaseIntegrationCases=14,historicalBaseIntegrationAcceptance=False,
    retiredHistoricalAdmissionControls=json.loads((folder/'retired-admission-controls.json').read_text()),
    observationOrder='Parser graph before admission/checking; file diagnostics; ascending selected type/symbol queries with formatting and type graph registration; ordered directed relations; globals/diagnostics; selected declared types repeated; selected widening repeated; logical node caches and resolved-symbol contents; indexed type work queue resolves public properties then snapshots raw payload/alias/index/mapper fields, existing propertiesTypes cache entries, and object caches; indexed symbol-link queue may add types, repeat queues to closure; final type flags; actual key membership/equality reads. Fields are snapshots at their visit, no absent caches forced.',
    independentDiscovery='Original Go reflection discovers all reachable payload fields, not a C# model manifest. Authored original-package data_access.go reads actual private stores and executes original key builders; tracked upstream unchanged.',
    staticMirror='C# DataGraph uses handwritten typed fields and source-generated JSON, no runtime reflection.',globals='All actual file/global diagnostic channels retained, no constant missing-global count.')
(folder/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ['cases','types','symbols','queries','relations','aliasIdentities','retainedMapperIdentities','internalIndexIdentities']}))
