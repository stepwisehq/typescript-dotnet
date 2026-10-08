# Foundation adaptations and boundaries

Current campaign implementation notes; acceptance remains pending in the
[status](./FOUNDATION-STATUS.md). The fixed [contract](./FOUNDATION-CONTRACT.md)
and [source map](../source-map.json) govern scope. Earlier phase adaptations
remain applicable unless explicitly superseded here.

## Text and positions

Internal strings retain one managed character per UTF8 byte, preserving Go byte
indexing and malformed input. Public Unicode and byte-array adapters make conversion
explicit. PositionMap follows the original JS-string rune decoding and binary-search
algorithms. Ordinary UTF8 length and scanner coordinate helpers intentionally use
their different upstream decoding rules; surrogate sentinel bytes must not be
normalized indiscriminately. Empty private managed collections may replace nil
storage where no public distinction exists; observable nullable syntax lists and
identity remain preserved.

## Numeric facilities

The port uses invariant .NET floating-point formatting and BigInteger rather than
reimplementing the Go standard library. Original assertions exposed a shortest-format
roundtrip defect for one binary64 value: invariant R produced digits that parsed to
the preceding value. The formatter now checks roundtrip identity and uses G17 if
needed, retaining upstream fixed/exponent presentation thresholds. This is a general
guard, not a fixture-specific spelling. Original assertions and supplemental actual
Go/Node comparisons provide finite evidence; G17 alone does not prove shortestness
for every binary64 value. See the [scanner audit](./progress/foundation-scanner-audit.md).

Nondecimal BigInt parsing now retains the original allowed underscore positions.
BigInteger supplies exact arithmetic for conversion. Unrelated numeric arithmetic,
bitwise and exponentiation operations remain outside scanner/parser dependencies.

## Syntax state and runtime facilities

Managed references represent pointer identity; canonical graph identifiers are
comparison labels, not upstream allocation numbers. Lazy per-payload child iterators
retain field order, early termination and original slice observation behavior.
Subtree facts now implement the complete pure syntax computations and cache masks;
the earlier private JSX-only projection is superseded.

SourceFile caches, typed data keys and declaration maps follow upstream algorithms.
The typed data cell preserves Go sync.Once failure behavior: a failed first compute
completes the cell and later calls return its default value without retry. Standard
Lazy exception caching would differ. General concurrency guarantees are not an
acceptance claim. Declaration-map operations that inspect symbols retain their
upstream bound/unbound preconditions; they do not add binding algorithms.

Go panics become managed exceptions. Failure versus success is preserved for invalid
inputs; runtime exception class names and generic panic formatting are not claimed
identical. Pooling and arena allocation use managed ownership, with syntax identity
and observable caches preserved.

## Required sort and filename dependencies

The parser exposes its sorted ReparsedClones array. .NET List.Sort produced different
permutations for equal source ranges on five broad inputs. The required comparator
SortFunc and PDQ sort helper closure are therefore ported from pinned Go 1.27.1,
with one source file mapped to each C# counterpart and the Go license retained.
This is a compatibility dependency, not a performance optimization or a general
slices-library port. Stable sorting would also change the upstream output.

Declaration filenames use the actual upstream path/extension helpers, including
arbitrary `.d.*.ts` forms. A suffix-only approximation missed 21 broad cases. This
adds filename processing, not filesystem access or module resolution.

## Diagnostics and tests

All 2,222 pinned English diagnostic records are available for general key lookup and
serialization. This data expansion does not implement the checker/project features
that emit those messages. Translated locale catalogs remain excluded. The original
localization test is explicitly adapted to its three default-English rows out of
thirteen; it is not reported as a complete original test port.

Original assertion bodies are shared between xUnit and the Native AOT validation
executable. Test dispatch and JSON graph adapters are authored harnesses, separate
from ported compiler algorithms. Byte-array equality can represent original Go
byte-string equality without weakening the assertion. Finite FuzzParser seeds retain
the original no-panic assertion; graph comparisons establish separate, stronger
evidence. Compiler input reuse does not establish full compiler test success.

Default-message debug failures are ported where syntax helpers need them; general
Go formatting/debug facilities are not a new runtime subsystem. Later-stage AST
state, content mapping, transformations, project-option derivation and locale services
retain explicit per-function exclusions in the foundation inventories.

Test-file mapping exception: small upstream direct test files are grouped into shared
assertion classes so xUnit and Native AOT execute identical bodies. The per-function
test inventories retain original file, line, source hash, assertion scope and C#
evidence path; authored probes are separately identified. Production source mapping
continues to use individual Go-to-C# counterparts.
