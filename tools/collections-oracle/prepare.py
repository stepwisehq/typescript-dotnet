"""Independent multi-file oracle reuses immutable historical serializers.
Generated copies are untracked instrumentation; upstream semantic code is unchanged.
"""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
out = root / 'upstream/TypeScript/tsc/cmd/collections-oracle'
out.mkdir(parents=True, exist_ok=True)
source = (root / 'tools/parser-oracle/main.go').read_text()
source = source[:source.index('func run(r Request) Response {')]
for name in ['bufio', 'encoding/json', 'os', 'github.com/microsoft/TypeScript/tsc/internal/tspath', 'github.com/microsoft/TypeScript/tsc/internal/scanner']:
    source = source.replace('\t"'+name+'"\n', '')
# Collections-specific checker-created node Type is observed in storedGraph,
# not mistaken for a syntax scalar. All other original syntax fields stay exact.
source=source.replace('\t\tname := strings.ToUpper(sf.Name[:1]) + sf.Name[1:]', '\t\tif t.Name()=="SyntheticExpression" && sf.Name=="Type" { continue }\n\t\tname := strings.ToUpper(sf.Name[:1]) + sf.Name[1:]')
(out/'parser_graph.go').write_text(source)
source = (root/'tools/binder-oracle/main.go').read_text()
source = source[:source.index('func parseFile(r Request) *ast.SourceFile {')]
for name in ['bufio', 'encoding/base64', 'encoding/json', 'github.com/microsoft/TypeScript/tsc/internal/binder', 'github.com/microsoft/TypeScript/tsc/internal/core', 'github.com/microsoft/TypeScript/tsc/internal/parser', 'github.com/microsoft/TypeScript/tsc/internal/tspath', 'os', 'sync', 'sync/atomic']:
    source = source.replace('\t"'+name+'"\n', '')
# core remains used for exact ambient pattern name validation.
source = source.replace('"fmt"', '"fmt"\n"github.com/microsoft/TypeScript/tsc/internal/core"')
source = source.replace('var lastSyntaxEncoder *encoder\n', '')
source = source.replace('Flags            uint32 `json:"flags"`', 'Flags            uint32 `json:"flags"`\nCheckFlags uint32 `json:"checkFlags"`')
source = source.replace('\tif s.CheckFlags != 0 {\n\t\tpanic("Unexpected checker-owned symbol flags")\n\t}\n', '')
source = source.replace('Flags: uint32(s.Flags),', 'Flags: uint32(s.Flags), CheckFlags: uint32(s.CheckFlags),')
(out/'binding_graph.go').write_text(source)
(out/'foundation.go').write_bytes((root/'tools/parser-oracle/foundation.go').read_bytes())
for file in (root/'tools/collections-oracle').glob('*.go'):
    (out/file.name).write_bytes(file.read_bytes())
for file in (root/'tools/collections-oracle').glob('*.go.inc'):
    package = 'compiler' if file.name == 'program_access.go.inc' else 'checker'
    target = root/'upstream/TypeScript/tsc/internal'/package/('oracle_collections_'+file.name.removesuffix('.inc'))
    target.write_bytes(file.read_bytes())

# Reuse the accepted read-only union/intersection membership helper exactly.
(root/'upstream/TypeScript/tsc/internal/checker/oracle_data_access.go').write_bytes((root/'tools/checker-oracle/data_access.go').read_bytes())
