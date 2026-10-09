#!/usr/bin/env python3
"""Preserve literal original jsnum tables/IEEE expected values for shared assertions."""
import hashlib,json,pathlib,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[1];UP=ROOT/'upstream/TypeScript/tsc';PIN=json.loads((ROOT/'upstream.lock.json').read_text())['revision']
if subprocess.check_output(['git','-C',str(UP),'rev-parse','HEAD'],text=True).strip()!=PIN:raise RuntimeError('Upstream pin mismatch')
source=UP/'internal/jsnum/jsnum_test.go';text=source.read_text();names=['ToInt32','BitwiseNOT','BitwiseAND','BitwiseOR','BitwiseXOR','SignedRightShift','UnsignedRightShift','LeftShift','Remainder','Exponentiate'];parts=[text[text.index('var toInt32Tests ='):text.index('func TestToInt32(')]]
for name in names[1:]:
 body=text[text.index('func Test'+name+'('):];start=body.index('tests := []struct');end=body.index('\n\txs :=',start);parts.append(body[start:end].replace('tests :=','var tests'+name+' =',1))
head='''package main
import("encoding/json";"fmt";"math";"os";"github.com/microsoft/TypeScript/tsc/internal/jsnum")
type Number=jsnum.Number
const MaxSafeInteger=jsnum.MaxSafeInteger
const MinSafeInteger=jsnum.MinSafeInteger
var negativeZero=Number(math.Copysign(0,-1))
func NaN() Number{return jsnum.NaN()}
func Inf(sign int) Number{return jsnum.Inf(sign)}
func numberFromBits(bits uint64) Number{return Number(math.Float64frombits(bits))}
func bits(n Number)string{return fmt.Sprintf("%016x",math.Float64bits(float64(n)))}
'''
main='''func main(){
 type Row struct{Function string `json:"function"`; Index int `json:"index"`; X string `json:"x"`; Y string `json:"y"`; Want string `json:"want"`}
 out:=[]Row{}
 for i,v:=range toInt32Tests{out=append(out,Row{"ToInt32",i,bits(v.input),bits(0),bits(Number(v.want))})}
'''
for name in names[1:]:main+='for i,v:=range tests'+name+'{out=append(out,Row{"'+name+'",i,bits(v.x),bits('+('0' if name=='BitwiseNOT' else 'v.y')+'),bits(v.want)})}\n'
main+='if err:=json.NewEncoder(os.Stdout).Encode(out);err!=nil{panic(err)}\n}\n';dest=UP/'cmd/callable-numeric-fixtures';dest.mkdir(parents=True,exist_ok=True);(dest/'main.go').write_text(head+'\n'.join(parts)+main)
raw=subprocess.check_output([str(ROOT/'tools/.cache/go/bin/go'),'run','./cmd/callable-numeric-fixtures'],cwd=UP);rows=json.loads(raw);OUT=ROOT/'validation/callables-corpus';(OUT/'numeric-fixtures.json').write_bytes(raw)
counts={name:sum(r['function']==name for r in rows) for name in names};record=dict(upstreamRevision=PIN,source=source.relative_to(UP).as_posix(),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),fixtureSha256=hashlib.sha256(raw).hexdigest(),originalFunctions=10,tableRows=len(rows),nodeAssertions=len(rows),functionRowCounts=counts,method='Original Go literal/data expressions evaluated to IEEE bits; original literal assertion and independent Node assertion both retained. Node exponent comparison preserves original one-ULP allowance; literal exponent expectations remain exact.',benchmarks='BenchmarkToInt32 and BenchmarkExponentiate are excluded performance loops, not correctness assertions.')
(OUT/'numeric-provenance.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(record))
