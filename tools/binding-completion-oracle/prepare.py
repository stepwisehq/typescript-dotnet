#!/usr/bin/env python3
"""Compose an opt-in independent cold-binding oracle from retained observers."""
import pathlib
ROOT = pathlib.Path(__file__).resolve().parents[2]
output = ROOT / 'upstream/TypeScript/tsc/cmd/binding-completion-oracle'
output.mkdir(parents=True, exist_ok=True)
source = (ROOT/'tools/parser-oracle/main.go').read_text()
start = source.index('func run(r Request) Response {')
end = source.index('\te := encoder{', start)
source = source[:start] + 'func runParser(r Request, file *ast.SourceFile) Response {\n' + source[end:]
source = source.replace('func main() {', 'func parserMain() {').replace('out.Encode(run(r))', 'out.Encode(runParser(r, parseFile(r)))')
source = source.replace('"github.com/microsoft/TypeScript/tsc/internal/tspath"\n', '')
source = source.replace('\te.node(file.AsNode())', '\tlastSyntaxEncoder = &e\n\te.node(file.AsNode())')
source = source.replace('value.Type().String() == "*ast.FlowNode"', 'value.Type().String() == "*ast.FlowNode" || value.Type().String() == "*ast.FlowList"')
(output/'parser_graph.go').write_text(source)
for path in (ROOT/'tools/parser-oracle').glob('*.go'):
    if path.name != 'main.go':
        (output/path.name).write_bytes(path.read_bytes())
source = (ROOT/'tools/binder-oracle/main.go').read_text().replace('func main() {', 'func historicalBinderMain() {')
# The binder generates private names from the lexical class even for malformed
# destructuring property declarations whose actual Symbol.Parent is another owner.
source = source.replace('owner := s.Parent', 'owner := ast.GetContainingClass(decl).Symbol()')
source = source.replace('if s.Parent == nil || len(s.Parent.Declarations) == 0 {', 'owner := ast.GetContainingClass(decl).Symbol()\n\t\t\tif owner == nil || len(owner.Declarations) == 0 {')
source = source.replace('g.syntax.ids[s.Parent.Declarations[0]]', 'g.syntax.ids[owner.Declarations[0]]')
source = source.replace('strconv.FormatUint(uint64(ast.GetSymbolId(owner)), 10)', 'strconv.Itoa(int(ast.GetSymbolId(owner)))')
(output/'binder_graph.go').write_text(source)
for path in (ROOT/'tools/binding-completion-oracle').glob('*.go'):
    (output/path.name).write_bytes(path.read_bytes())
