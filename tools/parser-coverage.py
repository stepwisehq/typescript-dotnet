#!/usr/bin/env python3
"""Inventory observed parser structures/channels against the pinned sources."""
import base64,collections,hashlib,json,pathlib,re
root=pathlib.Path(__file__).resolve().parent.parent
upstream=root/'upstream/TypeScript/tsc'
messages=dict((name,int(code)) for name,code in re.findall(r'var (\w+) = &Message\{code: (\d+)',(upstream/'internal/diagnostics/diagnostics_generated.go').read_text()))
refs=set()
for path in (upstream/'internal/parser').glob('*.go'):
 if not path.name.endswith('_test.go'):refs.update(re.findall(r'diagnostics\.(\w+)',path.read_text()))
referenced=sorted({messages[name] for name in refs if name in messages})
possible=set()
for path in (upstream/'internal/ast').glob('*.go'):
 possible.update(re.findall(r'func \(\w+ \*Node\) As\w+\(\) \*(\w+)',path.read_text()))
possible.discard('SyntheticExpression')
ast_source='\n'.join(path.read_text() for path in (upstream/'internal/ast').glob('*.go'))
arenas=dict(re.findall(r'(\w+)\s+core.Arena\[(\w+)\]',ast_source))
factory_payload={name:arenas[arena] for name,arena in re.findall(r'func \(\w+ \*NodeFactory\) (New\w+)\([^\n]*\) \*Node \{\s*data := \w+\.(\w+)\.New\(\)',ast_source) if arena in arenas}
factory_payload.update(dict(re.findall(r'func \(\w+ \*NodeFactory\) (New\w+)\([^\n]*\) \*Node \{\s*data := &(\w+)\{',ast_source)))
parser_source='\n'.join(path.read_text() for path in (upstream/'internal/parser').glob('*.go') if not path.name.endswith('_test.go'))
factory_calls=set(re.findall(r'factory\.(New\w+)\(',parser_source))
parser_payloads={factory_payload[name] for name in factory_calls&factory_payload.keys()}
kinds=collections.Counter();payloads=collections.Counter();channels=collections.Counter();codes=set();modes=collections.Counter();tristates=collections.Counter();missing=empty=nil_lists=trailing=cases=input_bytes=nodes=probe_count=0;groups=[]
def walk_diagnostic(d):
 codes.add(d['code'])
 for child in d['chain']+d['related']:walk_diagnostic(child)
for requests in sorted((root/'validation/parser-corpus').glob('*-requests.jsonl')):
 expected=requests.with_name(requests.name.replace('-requests','-expected'));count=0
 for row,result in zip(requests.open(),expected.open(),strict=True):
  request=json.loads(row);response=json.loads(result);cases+=1;count+=1
  modes[str(request.get('scriptKind',3))]+=1
  input_bytes+=len(base64.b64decode(request['textBytesBase64'])) if request.get('textBytesBase64') is not None else len(request.get('text','').encode())
  tristates[str(response['metadata']['usesUriStyleNodeCoreModules'])]+=1;probe_count+=len(response['textProbes'])
  for name in ['parseDiagnostics','jsDiagnostics','jsDocDiagnostics']:
   channels[name]+=len(response[name])
   for diagnostic in response[name]:walk_diagnostic(diagnostic)
  for node in response['nodes']:
   nodes+=1;kinds[str(node['kind'])]+=1;payloads[node['dataType']]+=1
   for field in node['fields']:
    if field['type']=='list' and not field['isNull']:
     listing=field['list'];missing+=listing['missing'];nil_lists+=listing['nodesNull'];empty+=not listing['items'];trailing+=listing['trailingComma']
 groups.append(dict(name=requests.stem,cases=count,requestsSha256=hashlib.sha256(requests.read_bytes()).hexdigest(),expectedSha256=hashlib.sha256(expected.read_bytes()).hexdigest()))
report=dict(upstreamCommit='fed0bf24149fb1ed36039212648bafdafc1ea10e',cases=cases,inputBytes=input_bytes,nodes=nodes,modeCounts=dict(modes),kindCounts=dict(sorted(kinds.items(),key=lambda p:int(p[0]))),payloadCounts=dict(sorted(payloads.items())),astAccessorTypes=len(possible),unobservedAstAccessorTypes=sorted(possible-set(payloads)),parserFactoryPayloadTypes=sorted(parser_payloads),unobservedParserFactoryPayloadTypes=sorted(parser_payloads-set(payloads)),unclassifiedParserFactoryCalls=sorted(factory_calls-factory_payload.keys()),diagnosticChannelCounts=dict(channels),observedDiagnosticCodes=sorted(codes),parserReferencedDiagnosticCodes=referenced,observedParserReferencedCodes=sorted(codes&set(referenced)),unobservedParserReferencedCodes=sorted(set(referenced)-codes),uriImportStyleCounts=dict(tristates),listCounts=dict(missing=missing,empty=empty,nilNodes=nil_lists,trailingComma=trailing),normalizedTextProbes=probe_count,groups=groups)
path=root/'validation/parser-corpus/coverage.json';path.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(dict(cases=cases,nodes=nodes,kinds=len(kinds),payloads=len(payloads),possibleAstPayloads=len(possible),diagnostics=dict(channels),codes=len(codes),parserReferencedObserved=len(codes&set(referenced)),parserReferencedTotal=len(referenced),lists=report['listCounts'])))
