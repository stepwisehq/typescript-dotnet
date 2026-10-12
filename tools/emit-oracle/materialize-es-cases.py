#!/usr/bin/env python3
"""Materialize captured authored ES cases as shared xUnit/AOT constants; no compiler run."""
import json, sys
from pathlib import Path
source, destination, class_name = sys.argv[1:]
cases = json.loads(Path(source).read_text())
def literal(value): return json.dumps(value, ensure_ascii=True)
def tri(item, field):
    value=item.get(field)
    return 'Tristate.Unknown' if value is None else 'Tristate.True' if value else 'Tristate.False'
rows=[]
for item in cases:
    assert isinstance(item['Output'],str) and isinstance(item['FullDiagnostics'],dict)
    args=[literal(item['Name']),literal(item['Input']),literal(item.get('Other','')),str(item.get('JSX',False)).lower(),'ScriptTarget.'+item['Target'],'ModuleKind.'+(item.get('Module') or 'ESNext'),tri(item,'UseDefineForClassFields'),tri(item,'ExperimentalDecorators'),tri(item,'EmitDecoratorMetadata'),str(item.get('StrictNullChecks',True) is not False).lower(),literal(item['Output']),literal(json.dumps(item['FullDiagnostics'],separators=(',',':'),ensure_ascii=True))]
    rows.append('        new('+', '.join(args)+'),')
text='''// Authored exact pinned-Go TS→ES target-chain observations, not original corpus tests.
// Generated from SOURCE by tools/emit-oracle/materialize-es-cases.py; assertions shared xUnit/AOT.
using TypeScript.Scanner;
namespace Emit.Checks;
public static class CLASS
{
    public static IEnumerable<string> Names => Cases.Select(item => item.Name);
    public static void Run(string name)
    {
        foreach (var item in Cases)
            if (item.Name == name) { item.Run(); return; }
        throw new ArgumentException("Unknown authored ES case", nameof(name));
    }
    public static int RunAll()
    {
        foreach (var item in Cases) item.Run();
        return Cases.Length;
    }
    private static readonly EsTransformCase[] Cases =
    [
ROWS
    ];
}
'''.replace('SOURCE',source).replace('CLASS',class_name).replace('ROWS','\n'.join(rows))
Path(destination).write_text(text)
print(f'{class_name}: {len(cases)} authored cases')
