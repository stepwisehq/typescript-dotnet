# Source mapping convention

The authoritative source revision is in [upstream.lock.json](../upstream.lock.json).
Paths in the mapping are relative to that repository, including its `tsc/` prefix.
The local reference checkout is ignored by Git and can be recreated from the pin.
Entries carrying their own `repository` and `revision` refer to a separately
pinned dependency; currently these are the two Go 1.27.1 Unicode support slices.

Ported files live under `src/TypeScript.Scanner/`, retaining the upstream package
as a directory and using PascalCase file names. For example,
`tsc/internal/scanner/scanner.go` maps to
`src/TypeScript.Scanner/Scanner/Scanner.cs`. `Scanner.cs` and `Regexp.cs` use a
C# partial class to preserve upstream source boundaries. Generated upstream data
is copied into corresponding static C# tables rather than regenerated from the
machine's Unicode or compiler version.

The final machine-readable manifest is [source-map.json](../source-map.json).
Each entry records:

- `upstream`: original Go source path (null for newly written support).
- `destination`: C# or harness path, or null for an excluded upstream file.
- `status`: `ported`, `partial`, `excluded`, or `new`.
- `scope`: included behavior and explicit omissions or adaptations.

`ported` means implementation coverage, not proof of exhaustive behavioral
equivalence. A support file with only a few required definitions is `partial`
even if all currently required scanner behavior passes. Excluded AST-facing
functions are listed explicitly; no placeholder AST implementation is introduced.

New byte-string compatibility helpers, oracle protocol, test corpus, and command
line validation code are identified separately from translated upstream code.
Ported source headers identify origin and modification; applicable license and
notice copies are preserved under [licenses/](../licenses/).
