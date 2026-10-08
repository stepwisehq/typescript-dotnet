"""Reuse independent parser discovery without modifying historical oracle sources."""
import pathlib
root = pathlib.Path(__file__).resolve().parents[2]
source = (root/'tools/parser-oracle/main.go').read_text()
start = source.index('func run(r Request) Response {')
end = source.index('\te := encoder{', start)
source = source[:start]+'func runParser(r Request, file *ast.SourceFile) Response {\n'+source[end:]
source = source[:source.index('func main() {')]
for import_name in ['bufio', 'encoding/json', 'os', 'github.com/microsoft/TypeScript/tsc/internal/tspath']:
    source = source.replace('\t"'+import_name+'"\n', '')
source = source.replace('\te.node(file.AsNode())', '\tlastSyntaxEncoder = &e\n\te.node(file.AsNode())')
output = root/'upstream/TypeScript/tsc/cmd/checker-oracle'
output.mkdir(parents=True, exist_ok=True)
(output/'parser_graph.go').write_text(source)
