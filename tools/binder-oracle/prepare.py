"""Reuse the independent parser serializer without changing historical assets."""
import pathlib
root=pathlib.Path(__file__).resolve().parents[2]
source=(root/'tools/parser-oracle/main.go').read_text()
start=source.index('func run(r Request) Response {')
end=source.index('\te := encoder{',start)
source=source[:start]+'func runParser(r Request, file *ast.SourceFile) Response {\n'+source[end:]
source=source.replace('func main() {','func parserMain() {').replace('out.Encode(run(r))','out.Encode(runParser(r, parseFile(r)))')
source=source.replace('"github.com/microsoft/TypeScript/tsc/internal/tspath"\n','')
source=source.replace('\te.node(file.AsNode())','\tlastSyntaxEncoder = &e\n\te.node(file.AsNode())')
source=source.replace('value.Type().String() == "*ast.FlowNode"','value.Type().String() == "*ast.FlowNode" || value.Type().String() == "*ast.FlowList"')
output=root/'upstream/TypeScript/tsc/cmd/binder-oracle'
(output/'parser_graph.go').write_text(source)
for file in (root/'tools/parser-oracle').glob('*.go'):
    if file.name!='main.go':(output/file.name).write_bytes(file.read_bytes())
