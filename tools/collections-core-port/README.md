# Historical collections port scaffolding

These are one-time investigation and integration aids from the collections campaign,
starting at baseline `cfa6d2f` and pinned Go revision
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. They are retained to explain how selected
original method bodies were initially extracted and adapted. They do **not** regenerate
the completed, reviewed compiler.

[main.go](./main.go) emits selected Go AST method bodies as C# scratch text. The
`patch-*.py` files adapt particular historical scratch batches. Their order and input
assumptions reflect intermediate integration stages; they are not a repeatable final
port pipeline. Some intermediate guards were subsequently replaced by real algorithms.
[install.py](./install.py) replaces matched methods or appends partial declarations and
can overwrite later manual corrections. It is not an idempotent update command.

The authoritative implementations are the reviewed
[Checker.cs](../../src/TypeScript.Scanner/Checker/Checker.cs),
[Inference.cs](../../src/TypeScript.Scanner/Checker/Inference.cs),
[Utilities.cs](../../src/TypeScript.Scanner/Checker/Utilities.cs) and
[GrammarChecks.cs](../../src/TypeScript.Scanner/Checker/GrammarChecks.cs), with ownership,
origin mappings and exclusions recorded in the repository source map and
[contract](../../docs/COLLECTIONS-CONTRACT.md). Future source comparisons may use this
scaffolding to emit disposable scratch output, followed by method-level review; do not
reapply the historical patch/install sequence to those production files.

Reproducing verification uses the separate
[original-Go oracle builder](../build-collections-oracle.sh) and
[collection gate](../verify-collections.sh), as described in the
[practical guide](../../docs/COLLECTIONS.md). Those commands observe the pinned source
and validate the existing hand-reviewed port; they do not regenerate its C# bodies.
