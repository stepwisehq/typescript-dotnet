#!/usr/bin/env python3
"""Generate supplemental expected helpers/numbers from the unchanged pinned Go APIs."""
import hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];UP=ROOT/'upstream/TypeScript/tsc';PIN='fed0bf24149fb1ed36039212648bafdafc1ea10e'
assert subprocess.check_output(['git','-C',str(UP),'rev-parse','HEAD'],text=True).strip()==PIN
source=ROOT/'tools/foundation-helpers/main.go';dest=UP/'cmd/foundation-helpers';dest.mkdir(parents=True,exist_ok=True);
for helper in source.parent.glob("*.go"):(dest/helper.name).write_bytes(helper.read_bytes())
binary=ROOT/'tools/.cache/foundation-helpers'
subprocess.run([str(ROOT/'tools/.cache/go/bin/go'),'build','-o',str(binary),'./cmd/foundation-helpers'],cwd=UP,check=True)
output=ROOT/'validation/foundation-corpus';output.mkdir(parents=True,exist_ok=True)
assets={}
for mode,name in [('helpers','scanner-helper-fixtures.json'),('numbers','scanner-number-patterns.json'),('syntax','syntax-helper-fixtures.json')]:
 raw=subprocess.check_output([str(binary),mode]);data=json.loads(raw);p=output/name;p.write_text(json.dumps(data,ensure_ascii=True,separators=(',',':'))+'\n');assets[name]=hashlib.sha256(p.read_bytes()).hexdigest()
 if mode=='numbers':print('numeric samples:',len(data))
manifest={'upstreamRevision':PIN,'authoredHelperSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'authoredSources':{str(helper.relative_to(ROOT)):hashlib.sha256(helper.read_bytes()).hexdigest() for helper in source.parent.glob('*.go')},'goVersion':subprocess.check_output([str(ROOT/'tools/.cache/go/bin/go'),'version'],text=True).strip(),'expectedAssets':assets,'classification':'Additional differential evidence; not original upstream test cases.','numberFamilies':'All representable binary powers and immediate neighbors/both signs; presentation thresholds and4096fixed recurrence bits.','sources':{p:hashlib.sha256((UP/'internal'/p).read_bytes()).hexdigest() for p in ['scanner/scanner.go','scanner/utilities.go','ast/utilities.go','ast/precedence.go','core/core.go','jsnum/string.go','jsnum/pseudobigint.go']}}
(output/'scanner-helper-provenance.json').write_text(json.dumps(manifest,indent=2)+'\n')
