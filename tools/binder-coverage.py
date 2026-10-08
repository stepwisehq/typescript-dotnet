#!/usr/bin/env python3
"""Inventory graph structure and invariants, without reducing acceptance to counts."""
import collections,hashlib,json,pathlib,re
root=pathlib.Path(__file__).resolve().parent.parent
upstream=root/'upstream/TypeScript/tsc/internal'
messages={name:int(code) for name,code in re.findall(r'var (\w+) = &Message\{code: (\d+)',(upstream/'diagnostics/diagnostics_generated.go').read_text())}
binder_source=(upstream/'binder/binder.go').read_text()
referenced={messages[name] for name in re.findall(r'diagnostics\.(\w+)',binder_source) if name in messages}
symbols=flows=lists=tables=nodes=cases=diagnostics=0
symbol_bits=collections.Counter();flow_bits=collections.Counter();bound_fields=collections.Counter();nonnull=collections.Counter();payloads=collections.Counter();codes=set();modes=collections.Counter();shared_tables=flow_cycles=flow_list_sharing=export_local_splits=next_container_links=0
groups=[]
features=collections.Counter()
flag_names=lambda filename,prefix: {bit:name for name,bit in re.findall(prefix+r'(\w+)\s+'+prefix+r'\s*=\s*1\s*<<\s*(\d+)',(upstream/'ast'/filename).read_text())}
symbol_names=flag_names('symbolflags.go','SymbolFlags');flow_names=flag_names('flow.go','FlowFlags')
def cycle(edges):
 active=set();done=set()
 def visit(n):
  if n in active:return True
  if n in done:return False
  active.add(n)
  if any(visit(child) for child in edges.get(n,[])):return True
  active.remove(n);done.add(n);return False
 return any(visit(n) for n in edges)
def diagnostic(d):
 codes.add(d['code'])
 for child in d['related']+d['chain']:diagnostic(child)
for request_path in sorted((root/'validation/binder-corpus').glob('*-requests.jsonl')):
 expected=request_path.with_name(request_path.name.replace('-requests','-expected'));count=0
 for raw,line in zip(request_path.open(),expected.open(),strict=True):
  request=json.loads(raw);result=json.loads(line);binding=result['binding'];syntax=result['after']['nodes'];cases+=1;count+=1
  if not result['idempotent'] or not binding['isBound']:raise RuntimeError('Binding completion/idempotence missing')
  modes[str(request.get('scriptKind',3))]+=1;nodes+=len(syntax);symbols+=len(binding['symbols']);flows+=len(binding['flows']);lists+=len(binding['flowLists']);tables+=len(binding['tables']);diagnostics+=len(binding['diagnostics'])
  refs=collections.Counter();list_refs=collections.Counter();flow_refs=collections.Counter();container_edges={}
  bound_by_node={entry["nodeId"]:{field["name"]:field for field in entry["fields"]} for entry in binding["nodes"]}
  def valid(ref,values):
   if ref is not None and not 0<=ref<len(values):raise RuntimeError("Invalid graph reference")
  for entry in binding['nodes']:
   for field in entry['fields']:
    valid(field['ref'],{'node':syntax,'symbol':binding['symbols'],'table':binding['tables'],'flow':binding['flows'],'flowList':binding['flowLists']}[field['type']])
    bound_fields[field['name']]+=1
    if field['type']=='table':features['nilTableFields']+=field['ref'] is None
    if field['type']=='flow' and field['ref'] is not None:flow_refs[field['ref']]+=1
    if field['ref'] is not None:nonnull[field['name']]+=1
    if field['type']=='table' and field['ref'] is not None:refs[field['ref']]+=1
    if field['name']=='NextContainer' and field['ref'] is not None:container_edges[entry['nodeId']]=[field['ref']];next_container_links+=1
  if cycle(container_edges):raise RuntimeError('NextContainer cycle in upstream graph')
  for node in syntax:payloads[node['dataType']]+=1
  for symbol in binding['symbols']:
   for bit in range(32):
    if symbol['flags']&(1<<bit):symbol_bits[str(bit)]+=1
   export_local_splits+=symbol['exportSymbol'] is not None
   for key in ['members','exports']:
    if symbol[key] is not None:refs[symbol[key]]+=1
   features['mergedSymbols']+=len(symbol['declarations'])>1
   features['nilDeclarationLists']+=symbol['declarationsNull']
   for key in ['parent','exportSymbol']:valid(symbol[key],binding['symbols'])
   valid(symbol['valueDeclaration'],syntax)
   for declaration in symbol['declarations']:
    if not 0<=declaration<len(syntax):raise RuntimeError('Invalid declaration node ID')
  for table in binding['tables']:
   features['emptyTables']+=not table['entries']
   for entry in table['entries']:valid(entry['symbolId'],binding['symbols'])
  if binding['globalExports'] is not None:refs[binding['globalExports']]+=1
  shared_tables+=sum(value>1 for value in refs.values())
  flow_edges={}
  for flow in binding['flows']:
   for bit in range(32):
    if flow['flags']&(1<<bit):flow_bits[str(bit)]+=1
   edges=[]
   valid(flow['node'],syntax);valid(flow['antecedent'],binding['flows']);valid(flow['antecedents'],binding['flowLists'])
   if flow['antecedent'] is not None:edges.append(flow['antecedent']);flow_refs[flow['antecedent']]+=1
   listing=flow['antecedents'];seen=set()
   while listing is not None:
    if listing in seen:raise RuntimeError('Unexpected linked FlowList cycle')
    seen.add(listing);list_refs[listing]+=1;item=binding['flowLists'][listing]
    valid(item['flow'],binding['flows']);valid(item['next'],binding['flowLists'])
    if item['flow'] is not None:edges.append(item['flow']);flow_refs[item['flow']]+=1
    listing=item['next']
   flow_edges[flow['id']]=edges
  # Reduce-label payloads contain additional graph edges through target/list fields.
  for entry in binding['nodes']:
   if syntax[entry['nodeId']]['dataType']=='FlowReduceLabelData':
    for field in entry['fields']:
     if field['type']=='flowList' and field['ref'] is not None:list_refs[field['ref']]+=1
  flow_cycles+=cycle(flow_edges);flow_list_sharing+=sum(value>1 for value in list_refs.values())
  features['sharedFlowIdentities']+=sum(value>1 for value in flow_refs.values())
  for d in binding['diagnostics']:diagnostic(d)
 groups.append(dict(name=request_path.stem,cases=count,requestsSha256=hashlib.sha256(request_path.read_bytes()).hexdigest(),expectedSha256=hashlib.sha256(expected.read_bytes()).hexdigest()))
report=dict(upstreamRevision='fed0bf24149fb1ed36039212648bafdafc1ea10e',cases=cases,nodes=nodes,symbols=symbols,tables=tables,flows=flows,flowLists=lists,diagnostics=diagnostics,modeCounts=dict(modes),boundFieldCounts=dict(bound_fields),nonnullBoundFieldCounts=dict(nonnull),symbolFlagBitCounts=dict(symbol_bits),flowFlagBitCounts=dict(flow_bits),payloadCounts=dict(sorted(payloads.items())),observedBinderDiagnosticCodes=sorted(codes),binderReferencedDiagnosticCodes=sorted(referenced),unobservedBinderDiagnosticCodes=sorted(referenced-codes),graphFeatures=dict(casesWithFlowCycles=flow_cycles,sharedTableIdentities=shared_tables,sharedFlowListIdentities=flow_list_sharing,localExportSymbolLinks=export_local_splits,nextContainerLinks=next_container_links),groups=groups)
report['symbolFlagNamesByBit']=symbol_names;report['flowFlagNamesByBit']=flow_names
report['unobservedSymbolFlags']=[name for bit,name in symbol_names.items() if bit not in symbol_bits]
report['unobservedFlowFlags']=[name for bit,name in flow_names.items() if bit not in flow_bits]
report['graphFeatures'].update(features)
report['diagnosticLimits']={'1250/1251/1252':'Referenced only by getStrictModeBlockScopeFunctionDeclarationMessage, which has no callsites in the pinned binder.'}
path=root/'validation/binder-corpus/coverage.json';path.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ['cases','nodes','symbols','tables','flows','flowLists','diagnostics','boundFieldCounts','nonnullBoundFieldCounts','symbolFlagBitCounts','flowFlagBitCounts','unobservedBinderDiagnosticCodes','graphFeatures']}))
