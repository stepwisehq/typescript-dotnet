"""Install the authored resolver oracle; never replace upstream algorithm files."""
from pathlib import Path
import argparse
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "tools/resolution-oracle"
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--pristine", action="store_true")
args = parser.parse_args()
OUTPUT = ROOT / "upstream/TypeScript/tsc/cmd" / ("resolution-pristine-oracle" if args.pristine else "resolution-oracle")
OUTPUT.mkdir(parents=True, exist_ok=True)
for path in OUTPUT.glob("*.go"):
    path.unlink()
for path in SOURCE.glob("*.go"):
    if args.pristine and path.name == "graph.go":
        continue
    (OUTPUT / path.name).write_bytes(path.read_bytes())

if args.pristine:
    # This companion cannot observe graphs. Ordinary APIs compile against the
    # unchanged pinned source without any compiler overlay or replacements.
    (OUTPUT / "graph_unavailable.go").write_text('''package main
import "github.com/microsoft/TypeScript/tsc/internal/tsoptions"
func (o *observer) graph(parsed *tsoptions.ParsedCommandLine, h *host) any {
    panic("Graph observer unavailable in pristine companion")
}
''')
    proof = ROOT / "artifacts/resolution-validation/pristine-oracle-provenance.json"
    proof.parent.mkdir(parents=True, exist_ok=True)
    proof.write_text(json.dumps({
        "noProductionOverlay": True,
        "observerOnlyGraphEntry": "explicit rejection; ordinary APIs unchanged",
        "files": {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in sorted(OUTPUT.glob("*.go"))},
    }, indent=2) + "\n")
    print(proof)
else:
    from instrument_graph import generate
    print(generate(ROOT / "artifacts/resolution-validation/original-graph-overlay"))
