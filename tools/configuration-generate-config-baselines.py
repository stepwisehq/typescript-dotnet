"""Extract complete original config test inputs and unchanged baseline bytes."""
from pathlib import Path
import base64
import hashlib
import json
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
UPSTREAM = ROOT / 'upstream/TypeScript/tsc'
TEST = UPSTREAM / 'internal/tsoptions/tsconfigparsing_test.go'
source = TEST.read_text()
# These exact original declarations contain only fixture literals and option
# initializers. No TypeScript parser/serializer/checker is run for expectations.
declarations = source[source.index('type testConfig'):source.index('func TestParseConfigFileTextToJson')]
declarations += source[source.index('type parseJsonConfigTestCase'):source.index('func TestParseJsonConfigFileContent(')]
acquisition_start = source.index('func TestParseTypeAcquisition(')
acquisition_body = source[source.index('cases :=', acquisition_start):source.index('\tfor _, test := range cases', acquisition_start)]
declarations += '\nfunc acquisition() []any {\n' + acquisition_body + '\nout:=[]any{};for _,r:=range cases{out=append(out,map[string]any{"title":r.title,"config":r.config,"configName":r.configName})};return out}\n'
driver = '''package main
import("encoding/json";"os";"github.com/microsoft/TypeScript/tsc/internal/core";"github.com/microsoft/TypeScript/tsc/internal/tspath")
''' + declarations + '''
func main(){out:=map[string]any{};text:=[]any{};for _,r:=range parseConfigFileTextToJsonTests{text=append(text,map[string]any{"title":r.title,"input":r.input})};out["text"]=text;cases:=[]any{};for _,r:=range parseJsonConfigFileTests{input:=[]any{};for _,c:=range r.input{input=append(input,map[string]any{"text":c.jsonText,"file":c.configFileName,"base":c.basePath,"files":c.allFileList,"existing":c.existingOptions})};cases=append(cases,map[string]any{"title":r.title,"includeOptions":r.includeCompilerOptions,"input":input})};out["config"]=cases;out["acquisition"]=acquisition();if err:=json.NewEncoder(os.Stdout).Encode(out);err!=nil{panic(err)}}
'''
with tempfile.TemporaryDirectory(prefix='config-test-extract-', dir=UPSTREAM / 'cmd') as directory:
    (Path(directory) / 'main.go').write_text(driver)
    data = json.loads(subprocess.check_output([str(ROOT / 'tools/.cache/go/bin/go'), 'run', './cmd/' + Path(directory).name], cwd=UPSTREAM))
if len(data['config']) != 33 or len(data['text']) != 7 or len(data['acquisition']) != 8:
    raise ValueError('Original source fixture domain changed')
digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
sources = {str(TEST.relative_to(ROOT)): digest(TEST)}
quote = lambda value: json.dumps(value, ensure_ascii=True)
baseline_directory = UPSTREAM / 'testdata/baselines/reference/config/tsconfigParsing'

def baseline(name):
    path = baseline_directory / name
    sources[str(path.relative_to(ROOT))] = digest(path)
    return quote(base64.b64encode(path.read_bytes()).decode())

lines = ['// Source-extracted original fixtures and unchanged pinned baseline assertions.',
         'namespace Configuration.Checks;', 'public static partial class ConfigBaselineChecks', '{',
         '    private sealed record Input(string Text, string File, string Base, (string Name, string Content)[] Files);',
         '    private sealed record ConfigCase(string Title, bool IncludeOptions, Input[] Inputs, string JsonBaseline, string SourceBaseline);',
         '    private sealed record TextCase(string Title, string[] Inputs, string Baseline);',
         '    private static readonly ConfigCase[] configCases =', '    [']
for row in data['config']:
    inputs = []
    for item in row['input']:
        if item['existing'] is not None:
            raise ValueError('New existing-options fixture requires an explicit static typed adapter')
        files = '[' + ', '.join('(' + quote(key) + ', ' + quote(value) + ')' for key, value in sorted((item['files'] or {}).items())) + ']'
        inputs.append('new(' + quote(item['text']) + ', ' + quote(item['file']) + ', ' + quote(item['base']) + ', ' + files + ')')
    expected = [baseline(row['title'] + suffix) for suffix in [' with json api.js', ' with jsonSourceFile api.js']]
    lines.append('        new(' + quote(row['title']) + ', ' + str(row['includeOptions']).lower() + ', [' + ', '.join(inputs) + '], ' + ', '.join(expected) + '),')
lines += ['    ];', '    private static readonly ConfigCase[] acquisitionCases =', '    [']
for row in data['acquisition']:
    expected = [baseline(row['title'] + suffix) for suffix in [' with json api.js', ' with jsonSourceFile api.js']]
    item = 'new(' + quote(row['config']) + ', ' + quote(row['configName']) + ', "/apath", [("/apath/a.ts", ""), ("/apath/b.ts", "")])'
    lines.append('        new(' + quote(row['title']) + ', true, [' + item + '], ' + ', '.join(expected) + '),')
lines += ['    ];', '    private static readonly TextCase[] textCases =', '    [']
for row in data['text']:
    lines.append('        new(' + quote(row['title']) + ', [' + ', '.join(quote(value) for value in row['input']) + '], ' + baseline(row['title'] + ' jsonParse.js') + '),')
lines += ['    ];', '}']
products = {
    ROOT / 'validation/Configuration.Checks/ConfigBaselineChecks.Generated.cs': '\n'.join(lines) + '\n',
    ROOT / 'validation/Configuration.Checks/config-baseline-test-provenance.json': json.dumps({
        'sources': sources, 'originalFunctions': ['TestParseConfigFileTextToJson', 'TestParseJsonConfigFileContent', 'TestParseJsonSourceFileConfigFileContent', 'TestParseTypeAcquisition'],
        'configScenarioRows': 33, 'textScenarioGroups': 7, 'acquisitionScenarioRows': 8, 'unchangedBaselineFiles': 89,
        'preservation': 'All original fixture inputs and full exact pinned baseline bytes; sequential scheduling and finite read-side VFS fixture construction. Test indentation is whitespace-only over actual compact codec bytes, preserving every primitive and member order.'
    }, indent=2) + '\n'
}
for path, contents in products.items():
    if '--check' in sys.argv:
        if path.read_text() != contents:
            raise SystemExit('Generated drift: ' + str(path))
    else:
        path.write_text(contents)
print(json.dumps({'configRows': 33, 'textGroups': 7, 'acquisitionRows': 8, 'baselines': 89}))
