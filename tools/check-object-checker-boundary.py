#!/usr/bin/env python3
"""Run authored admission controls and original Go utility expectations separately."""
import argparse,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--executable',required=True);parser.add_argument('--report',required=True);args=parser.parse_args()
def run(argument):return json.loads(subprocess.check_output([args.executable,argument],text=True))
controls=json.loads((root/'validation/checker-corpus/admission-controls.json').read_text())
retired=['object','object-type']
controls=[control for control in controls if control['name'] not in retired]+json.loads((root/'validation/object-checker-corpus/admission-controls.json').read_text())
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
if len(self_tests)!=10 or not all(item.get('boolValue') is True for item in self_tests):raise RuntimeError('Public API self-tests incomplete')
report=dict(retiredHistoricalAdmissionControls=retired,admissionControls=len(controls),admitted=sum(x['expectedFeature'] is None for x in controls),rejected=sum(x['expectedFeature'] is not None for x in controls),goUtilityObservations=utilities,authoredPublicApiAssertions=self_tests)
pathlib.Path(args.report).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if isinstance(v,int)}))
