#!/usr/bin/env python3
"""Inventory actual independent semantic observations, without claiming coverage by count."""
import collections,json,pathlib
root=pathlib.Path(__file__).resolve().parent.parent
rows=[json.loads(line) for path in sorted((root/'validation/checker-corpus').glob('*-expected.jsonl')) for line in path.read_text().splitlines()]
def count(values):return dict(sorted(collections.Counter(str(v) for v in values).items()))
def bits(values):
 values=list(values)
 return [i for i in range(32) if any(v&(1<<i) for v in values)]
types=[t for r in rows for t in r['types']];symbols=[s for r in rows for s in r['symbols']]
report=dict(upstreamRevision='fed0bf24149fb1ed36039212648bafdafc1ea10e',cases=len(rows),astNodes=sum(len(r['parser']['nodes']) for r in rows),types=len(types),symbols=len(symbols),tables=sum(len(r['tables']) for r in rows),queries=sum(len(r['queries']) for r in rows),relations=sum(len(r['relations']) for r in rows),relationTrue=sum(x['assignable'] for r in rows for x in r['relations']),fileDiagnostics=sum(len(r['checkerDiagnostics']) for r in rows),globalDiagnostics=sum(len(r['globalDiagnostics']) for r in rows),diagnosticCodes=count(d['code'] for r in rows for d in r['checkerDiagnostics']),globalDiagnosticCodes=count(d['code'] for r in rows for d in r['globalDiagnostics']),bindDiagnosticCodes=count(d['code'] for r in rows for d in r['bindDiagnostics']),typePayloads=count(t['dataType'] for t in types),typeFlags=count(t['flags'] for t in types),typeFlagBits=bits(t['flags'] for t in types),objectFlagBits=bits(t['objectFlags'] for t in types),symbolFlagBits=bits(s['flags'] for s in symbols),checkFlagBits=bits(s['checkFlags'] for s in symbols),queryKinds=count(r['parser']['nodes'][q['nodeId']]['kind'] for r in rows for q in r['queries']),freshTypeEdges=sum(t['freshType'] is not None for t in types),regularTypeEdges=sum(t['regularType'] is not None for t in types),unionConstituentEdges=sum(len(t['types']) for t in types),globalsExportAliases=sum(r['symbols'][r['globalThisSymbol']]['exports']==r['globals'] for r in rows),observedAllocatedEmptyTables=sum(not t['entries'] for r in rows for t in r['tables']),relatedDiagnostics=sum(len(d['related']) for r in rows for d in r['checkerDiagnostics']),chainDiagnostics=sum(len(d['chain']) for r in rows for d in r['checkerDiagnostics']),independentAdmissionControls=35,goUtilityObservations=20,authoredApiAssertions=10,limits=[
 'Single-file TS scripts under fixed noLib/noResolve scalar admission; eight global2318 errors retained.',
 'No objects/calls/functions/controlflow/module/project/library/emit semantic acceptance.',
 'Selected public type graph observables, not every mutable cache field; union regularType omitted.',
 'Parser structural snapshot before semantic admission/checking; lazy documentation cache not serialized afterward.',
 'Assigned symbol/flow binder graph covered by preserved binder regression, not repeated in semantic JSON.',
 'No malformed semantic recovery admission; parse errors explicitly rejected.',
 'No diagnostic chains observed in accepted scalar grammar; related information observed.',
 'Allocated-empty tables covered by separate actual Go utility observations, not scalar graph corpus.'])
path=root/'validation/checker-corpus/coverage.json';path.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ['cases','astNodes','types','symbols','queries','relations','fileDiagnostics','globalDiagnostics']}))
