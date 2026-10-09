"""Compose finite metadata-only global cache hooks with an existing Go overlay.
Actual type-list arguments, complete indexed-access flags and widened source owners are erased by results;
all other selected key terms are retained in typed keys or actual payload fields.
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
    before = Path(merged.get(original_key, original_key)).read_text()
    after = before
    patches = [
        ("func (c *Checker) getSignatureInstantiationWithoutFillingInTypeArguments(",
         "\t\tc.cachedSignatures[key] = instantiation",
         "\t\tc.cachedSignatures[key] = instantiation\n\t\toracleCollectionsCaptureSignatureArguments(c, key, typeArguments)"),
        ("func (c *Checker) getIndexedAccessTypeOrUndefined(",
         "\t\t\tc.indexedAccessTypes[key] = t",
         "\t\t\tc.indexedAccessTypes[key] = t\n\t\t\toracleCollectionsCaptureIndexedInput(c, key, objectType, indexType, accessFlags, alias)"),
        ("func (c *Checker) getWidenedTypeWithContext(",
         "\t\t\tc.cachedTypes[CachedTypeKey{kind: CachedTypeKindWidened, typeId: t.id}] = result",
         "\t\t\tc.cachedTypes[CachedTypeKey{kind: CachedTypeKindWidened, typeId: t.id}] = result\n\t\t\toracleCollectionsCaptureWidenedOwner(c, t, result)"),
    ]
    for signature, old, new in patches:
        start = after.index(signature)
        end = after.index("\nfunc ", start + 5)
        function = after[start:end]
        if function.count(old) != 1 or new in function:
            raise RuntimeError("Original global cache publication changed or already instrumented: " + signature)
        after = after[:start] + function.replace(old, new) + after[end:]
    output.mkdir(parents=True, exist_ok=True)
    target = output / "checker-global-caches.go"
    target.write_text(after)
    merged[original_key] = str(target.resolve())
    diff = "".join(difflib.unified_diff(before.splitlines(True), after.splitlines(True),
        fromfile="existing-overlay/checker.go" if replacements else "pinned/checker.go",
        tofile="global-cache-metadata-overlay/checker.go"))
    (output / "global-caches.diff").write_text(diff)
    (output / "overlay.json").write_text(json.dumps({"Replace": merged}, indent=2) + "\n")
    sha = lambda data: hashlib.sha256(data).hexdigest()
    (output / "global-caches-manifest.json").write_text(json.dumps({
        "upstream": PIN, "originalSha256": sha(original.read_bytes()),
        "baseSha256": sha(before.encode()), "overlaySha256": sha(after.encode()),
        "diffSha256": sha(diff.encode()), "producerHooks": len(patches),
        "sites": [signature for signature, _, _ in patches],
        "semanticPolicy": "After original cache writes, retain computed key and inputs only. No rehash, semantic getter, type-argument filling, mapper, constraint or allocating ID call. Type-list length-zero headers normalized to encoded empty sequence; indexed key retains complete accessFlags while result keeps Persistent only; Widened metadata retains the actual source and published value when the value erases its source owner.",
    }, indent=2) + "\n")
    return merged


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--base-overlay", type=Path)
    args = parser.parse_args()
    replacements = json.loads(args.base_overlay.read_text())["Replace"] if args.base_overlay else None
    generate(args.output, replacements)
