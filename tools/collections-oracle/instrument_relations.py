"""Capture the original completed relation-key bytes via a metadata-only overlay.
Accept an existing Replace map so this precise hook composes with the independent
instantiation/comparer producers. Never modifies the pinned upstream source.
"""
from pathlib import Path
import argparse
import difflib
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
PIN = "fed0bf24149fb1ed36039212648bafdafc1ea10e"


def generate(output: Path, replacements: dict[str, str] | None = None) -> dict[str, str]:
    original = ROOT / "upstream/TypeScript/tsc/internal/checker/checker.go"
    original_key = str(original.resolve())
    merged = dict(replacements or {})
    base = Path(merged.get(original_key, original_key))
    before = base.read_text()
    start = before.index("func getRelationKey(")
    end = before.index("\nfunc getNodeListKey(", start)
    function = before[start:end]
    old = "\treturn b.hash(), constrained"
    if function.count(old) != 1:
        raise RuntimeError("Original getRelationKey completed-hash return changed")
    replacement = (
        "\toracleCollectionsKey := b.hash()\n"
        "\toracleCollectionsCaptureRelationKey(source, target, &b, oracleCollectionsKey)\n"
        "\treturn oracleCollectionsKey, constrained"
    )
    after = before[:start] + function.replace(old, replacement) + before[end:]
    output.mkdir(parents=True, exist_ok=True)
    target = output / "checker-relations.go"
    target.write_text(after)
    merged[original_key] = str(target.resolve())
    diff = "".join(difflib.unified_diff(
        before.splitlines(True), after.splitlines(True),
        fromfile="existing-overlay/checker.go" if replacements else "pinned/checker.go",
        tofile="relation-metadata-overlay/checker.go",
    ))
    (output / "relations.diff").write_text(diff)
    (output / "overlay.json").write_text(json.dumps({"Replace": merged}, indent=2) + "\n")
    sha = lambda data: hashlib.sha256(data).hexdigest()
    (output / "relations-manifest.json").write_text(json.dumps({
        "upstream": PIN,
        "originalSha256": sha(original.read_bytes()),
        "baseSha256": sha(before.encode()),
        "overlaySha256": sha(after.encode()),
        "diffSha256": sha(diff.encode()),
        "producerHooks": 1,
        "site": "checker.go:getRelationKey completed hash return",
        "semanticPolicy": "Hash computed once at original return; capture copies existing overflow+inline bytes after that hash. No extra constraints, type arguments, semantic getters, mapper or allocating ID calls.",
    }, indent=2) + "\n")
    return merged


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--base-overlay", type=Path)
    args = parser.parse_args()
    replacements = None
    if args.base_overlay:
        replacements = json.loads(args.base_overlay.read_text())["Replace"]
    generate(args.output, replacements)
