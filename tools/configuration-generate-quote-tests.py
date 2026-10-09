"""Preserve selected locked strconv test assertions without .NET Unicode data.

The full IsPrint expected domain comes from locked Go unicode.IsPrint, which is
independent of the ported strconv print tables. Quote rows remain literal source
expectations; the unused AppendQuote operation is explicitly not claimed.
"""
from pathlib import Path
import base64, hashlib, json, re, subprocess, sys, tempfile
ROOT = Path(__file__).resolve().parents[1]
GO = ROOT / 'tools/.cache/go/bin/go'
SRC = ROOT / 'tools/.cache/go/src'
QUOTE = SRC / 'strconv/quote_test.go'
source = QUOTE.read_text()
match = re.search(r'var quotetests = \[\]quoteTest\{.*?\n\}', source, re.S)
if not match:
    raise ValueError('Original quote rows not found')
program = '''package main
import("encoding/base64";"encoding/json";"os";"unicode")
type quoteTest struct {in,out,ascii,graphic string}
''' + match[0] + '''
func main(){
 bits:=make([]byte,(unicode.MaxRune+1)/8)
 for r:=rune(0);r<=unicode.MaxRune;r++ {if unicode.IsPrint(r){bits[r/8]|=1<<uint(r%8)}}
 rows:=[][2]string{}
 for _,r:=range quotetests {rows=append(rows,[2]string{base64.StdEncoding.EncodeToString([]byte(r.in)),base64.StdEncoding.EncodeToString([]byte(r.out))})}
 json.NewEncoder(os.Stdout).Encode(map[string]any{"bitmap":base64.StdEncoding.EncodeToString(bits),"rows":rows})
}
'''
with tempfile.TemporaryDirectory(prefix='configuration-quote-tests-') as temp:
    file = Path(temp)/'main.go';file.write_text(program)
    expected = json.loads(subprocess.check_output([str(GO), 'run', str(file)], cwd=ROOT))
if len(expected['rows']) != 8 or len(base64.b64decode(expected['bitmap'])) != 0x110000//8:
    raise ValueError('Original domain mismatch')
chunks = [expected['bitmap'][i:i+120] for i in range(0,len(expected['bitmap']),120)]
output = '''// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Authored static assertion adaptation of locked strconv/quote_test.go.
namespace Configuration.Checks;

public static partial class QuoteChecks
{
    private static readonly byte[] expectedPrint = Convert.FromBase64String(
''' + '\n'.join('        '+json.dumps(s)+(' +' if i<len(chunks)-1 else ');') for i,s in enumerate(chunks)) + '''

    private static readonly (string Input, string Expected)[] quoteRows =
    [
''' + ''.join('        ('+json.dumps(i)+', '+json.dumps(o)+'),\n' for i,o in expected['rows']) + '''    ];
}
'''
provenance = {'goVersion': subprocess.check_output([str(GO),'version']).decode().strip(),
 'sources':{str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [QUOTE,SRC/'strconv/quote.go',SRC/'strconv/isprint.go',SRC/'unicode/tables.go']},
 'TestIsPrint':{'disposition':'assertion-preserving-port','domain':'rune 0 through unicode.MaxRune inclusive','assertions':0x110000,'expectedPredicate':'locked Go unicode.IsPrint, independent of strconv tables','groupCount':1},
 'TestQuote':{'disposition':'adapted-selected-operation-assertions','rows':8,'preserved':'All eight original Quote assertions with unchanged literal expected bytes','omitted':'Eight AppendQuote assertions; append API not selected or implemented','groupCount':1}}
products={ROOT/'validation/Configuration.Checks/QuoteChecks.Generated.cs':output, ROOT/'validation/Configuration.Checks/quote-test-provenance.json':json.dumps(provenance,indent=2)+'\n'}
for path,text in products.items():
    if '--check' in sys.argv:
        if path.read_text()!=text:raise SystemExit('Generated drift: '+str(path.relative_to(ROOT)))
    else:path.write_text(text)
print(json.dumps({'originalBodies':1,'adaptedOperationGroups':1,'quoteRows':8,'IsPrintDomain':0x110000}))
