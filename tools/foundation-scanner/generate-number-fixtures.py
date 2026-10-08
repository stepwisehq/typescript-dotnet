#!/usr/bin/env python3
"""Evaluate the pinned original Go number-test expressions, preserving IEEE bits.

Only copied declaration/data sections are compiled. This does not replace or run
original assertions; shared C#/NativeAOT checks retain those independently.
"""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[2]
PIN='fed0bf24149fb1ed36039212648bafdafc1ea10e'
UP=ROOT/'upstream/TypeScript/tsc'
if subprocess.check_output(['git','-C',str(UP),'rev-parse','HEAD'],text=True).strip()!=PIN:
    raise SystemExit('Upstream revision differs from pin')
s=(UP/'internal/jsnum/string_test.go').read_text()
ryu=(UP/'internal/jsnum/ryu_test.go').read_text()
part=s[s.index('type stringTest struct'):s.index('func TestString(')]
part+=s[s.index('var fromStringTests ='):s.index('func TestFromString(')]
part+=ryu[ryu.index('func ieeeParts2Double'):]
head='''package main
import("encoding/json";"fmt";"math";"slices";"os";"github.com/microsoft/TypeScript/tsc/internal/jsnum")
type Number = jsnum.Number
const MaxSafeInteger=jsnum.MaxSafeInteger
const MinSafeInteger=jsnum.MinSafeInteger
var negativeZero=Number(math.Copysign(0,-1))
func NaN() Number{return jsnum.NaN()}
func Inf(sign int) Number{return jsnum.Inf(sign)}
func numberFromBits(bits uint64) Number{return Number(math.Float64frombits(bits))}
'''
main='''
func main(){
 type row struct{Bits string;Text string}
 out:=map[string][]row{}
 for _,group:=range []struct{name string;data []stringTest}{{"format",stringTests},{"parse",fromStringTests}}{
  for _,v:=range group.data{out[group.name]=append(out[group.name],row{fmt.Sprintf("%016x",math.Float64bits(float64(v.number))),v.str})}
 }
 if err:=json.NewEncoder(os.Stdout).Encode(out);err!=nil{panic(err)}
}
'''
dest=UP/'cmd/foundation-number-fixtures'
dest.mkdir(parents=True,exist_ok=True)
(dest/'main.go').write_text(head+part+main)
raw=subprocess.check_output([str(ROOT/'tools/.cache/go/bin/go'),'run','./cmd/foundation-number-fixtures'],cwd=UP)
data=json.loads(raw)
assert len(data['format'])==153 and len(data['parse'])==95
out=ROOT/'validation/Foundation.Checks/NumberFixtures.Generated.cs'
lines=ryu[ryu.index('// Copyright'):ryu.index('func ieeeParts2Double')].strip().splitlines()+['// Copyright Microsoft Corporation.','// Apache-2.0: licenses/TypeScript-LICENSE.txt. Ryu fixtures may alternatively use Boost-1.0.',
 '// Ryu original notice: upstream tsc/internal/jsnum/ryu_test.go lines3-20.',
 '// Generated from pinned original string_test.go/ryu_test.go expressions; no expected value changes.',
 '// Upstream '+PIN, 'namespace TypeScript.Foundation;', 'internal static class NumberFixtures','{',
 '    internal readonly record struct Row(ulong Bits, string Text);']
for key,name in [('format','Format'),('parse','Parse')]:
 lines+=['    internal static readonly Row[] '+name+' =','    [']
 for row in data[key]:lines.append('        new(0x'+row['Bits']+'UL, '+json.dumps(row['Text'],ensure_ascii=True)+'),')
 lines+=['    ];']
lines+=['}']
out.parent.mkdir(parents=True,exist_ok=True)
out.write_text('\n'.join(lines)+'\n')
manifest={'upstreamRevision':PIN,'sources':{p:hashlib.sha256((UP/p).read_bytes()).hexdigest() for p in ['internal/jsnum/string_test.go','internal/jsnum/ryu_test.go']},'formatRows':153,'parseRows':95,'outputSha256':hashlib.sha256(out.read_bytes()).hexdigest(),'method':'Original Go expressions evaluated as float64 then stored as uint64 bits and unchanged expected strings.'}
(ROOT/'validation/Foundation.Checks/number-fixture-provenance.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Wrote153formatting/95parsing original rows with exact IEEE bits')
