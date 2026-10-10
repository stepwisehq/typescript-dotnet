#!/usr/bin/env python3
"""Current data-type admission; historical controls preserved with explicit retirement reasons."""
import argparse,hashlib,json,os,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--executable',required=True);parser.add_argument('--report',required=True);args=parser.parse_args()
def run(argument):return json.loads(subprocess.check_output([args.executable,argument],text=True))
controls=json.loads((root/'validation/checker-corpus/admission-controls.json').read_text())
retired=json.loads((root/'validation/data-types-corpus/retired-admission-controls.json').read_text())
retired_by_corpus={}
for item in retired:retired_by_corpus.setdefault(item['corpus'],set()).add(item['name'])
controls=[]
for corpus in ['checker-corpus','object-checker-corpus','data-types-corpus']:
 controls.extend(c for c in json.loads((root/('validation/'+corpus+'/admission-controls.json')).read_text()) if c['name'] not in retired_by_corpus.get(corpus,set()))
current_retirement=None
context=os.environ.get('DECLARATIONS_CURRENT_CHECKING')
if context is not None:
 if context!='1':raise RuntimeError('Unreviewed declaration checking context')
 manifest=json.loads((root/'validation/declarations-corpus/manifest.json').read_text())
 binding=manifest['authoredControlRetirement'];provenance=root/binding['provenance']
 if binding['provenance']!='validation/declarations-corpus/authored-control-retirement-provenance.json' or hashlib.sha256(provenance.read_bytes()).hexdigest()!=binding['provenanceSha256']:raise RuntimeError('Current authored retirement provenance differs')
 current_retirement=json.loads(provenance.read_text())
 if current_retirement['classification']!='authored-capability-restriction-retirement-with-genuine-source-positive-replacement' or current_retirement['replacement']['name']!='global-this-read-current-capability':raise RuntimeError('Unreviewed current admission retirement')
 if hashlib.sha256((root/current_retirement['historicalAsset']).read_bytes()).hexdigest()!=current_retirement['historicalSha256']:raise RuntimeError('Historical admission evidence changed')
 replacement=[c for c in controls if c['name']=='builtin-globalThis-read']
 if len(replacement)!=1 or replacement[0]!=current_retirement['retiredControls'][0]['previous']:raise RuntimeError('Retired authored input differs')
 controls=[c for c in controls if c['name']!='builtin-globalThis-read']
process=subprocess.Popen([args.executable],stdin=subprocess.PIPE,stdout=subprocess.PIPE,text=True)
for control in controls:
 process.stdin.write(json.dumps(control)+'\n');process.stdin.flush();actual=json.loads(process.stdout.readline())
 feature=control['expectedFeature']
 if actual['admitted']!=(feature is None) or actual['feature']!=feature:raise RuntimeError(control['name']+': admission result differs: '+str(actual))
 if feature is not None and not (-1<=actual['pos']<=actual['end']<=len(control['text'].encode())):raise RuntimeError('Invalid rejection byte span')
process.stdin.close()
if process.stdout.read() or process.wait():raise RuntimeError('Admission process failed or extra output')
utilities=run('--utilities');expected=json.loads((root/'validation/checker-corpus/utility-expected.json').read_text())
if utilities!=expected:raise RuntimeError('Go/native utility observations differ')
self_tests=run('--self-tests')
if current_retirement is None:
 if len(self_tests)!=10 or not all(item.get('boolValue') is True for item in self_tests):raise RuntimeError('Public API self-tests incomplete')
else:
 names={'api.foreign-type-node','api.foreign-symbol-node','api.orphan-node','api.cyclic-foreign-parent','api.global-table-alias','api.global-readonly','api.no-library-errors-visible','api.repeat-diagnostics','api.repeat-type-identity','api.builtin-object-read-admitted','api.builtin-object-missing-globals-visible','api.builtin-object-symbol','api.builtin-object-type','api.builtin-object-type-identity'}
 if len(self_tests)!=len(names) or {item.get('name') for item in self_tests}!=names or not all(item.get('boolValue') is True for item in self_tests):raise RuntimeError('Current public API self-tests incomplete')
report=dict(retiredHistoricalAdmissionControls=retired,admissionControls=len(controls),admitted=sum(x['expectedFeature'] is None for x in controls),rejected=sum(x['expectedFeature'] is not None for x in controls),goUtilityObservations=utilities,authoredPublicApiAssertions=self_tests)
if current_retirement is not None:report['currentAuthoredControlRetirement']=dict(environment={'DECLARATIONS_CURRENT_CHECKING':'1'},provenance=binding['provenance'],provenanceSha256=binding['provenanceSha256'],retiredName='builtin-globalThis-read',positiveReplacement=current_retirement['replacement']['name'])
pathlib.Path(args.report).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if isinstance(v,int)}))
