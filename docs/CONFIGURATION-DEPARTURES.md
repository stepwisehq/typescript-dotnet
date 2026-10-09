# Configuration subsystem adaptations and boundaries

Status: fixed configuration contract accepted on Linux x64 Native AOT. The
[contract](CONFIGURATION-CONTRACT.md), [function inventory](configuration-functions.json)
and [source map](../source-map.json) define scope independently of passing fixtures.

| Area | Adaptation and reason | Required behavior / boundary |
| --- | --- | --- |
| Integer option pointers | `StrongBox<long>` represents Go `*int` on the selected 64-bit platform | Preserve nil versus allocated zero, shallow-clone pointer sharing and value equality. CLI integer parsing and raw JSON conversion retain their different original overflow behavior. |
| Generated option access | Static source-derived field dispatch replaces reflection | All 132 CompilerOptions fields, four TypeAcquisition fields and six BuildOptions fields remain represented. Original full-field test domains are generated independently from upstream sources. No runtime reflection requirement under Native AOT. |
| Collections and lazy state | .NET dictionaries/lists, ordered wrappers, locks and explicit once state | Preserve nil/empty distinctions, insertion order, selected table sharing and reset behavior. Go.Once completes after a throwing invocation; copied parsed results reset the original lazy caches while retaining intended references. Private synchronization-object topology is not compared. |
| JSON | Selected pinned Go 1.27.1 grammar/error paths plus authored static value adapters | .NET facilities alone do not preserve original duplicate typed-field mutation, skipped unknown values, error priority, partial output, numeric and invalid-byte behavior. Iterative container handling retains the upstream 10,000-depth limit. No general streaming/reflection JSON library port is claimed. |
| Locale | Selected x/text v0.42.0 parser, canonicalization and generated registry | Installed .NET cultures have different acceptance and canonicalization behavior. Preserve original partial values/errors and selected comparable value semantics; unrelated matching/collation/Compose APIs remain external. License and source hashes are retained. |
| Quoting and semver | Source-derived strconv quoting/print tables and adapted .NET regex syntax | Retain Go byte quoting, ASCII grammar, end anchors, case-fold behavior and overflow/comparison rules. JSON quoting and culture-sensitive matching are not substitutes. |
| Typed paths | Value wrappers distinguish rooted paths, directory paths, patterns and keys | Pure operations use original Go byte strings. Unicode conversion is explicit at the OS boundary; virtual Windows/UNC and case modes remain observable independently of host platform. |
| Linux read-side filesystem | Standard .NET IO for representable files; narrow statx, raw readdir/readlink and O_PATH interop where needed | Reject encountered unrepresentable raw directory names or successfully resolved targets before replacement decoding can alias a real filename; missing targets retain original lookup failure. No writable filesystem port or non-Linux execution claim. File timestamps expose .NET precision; opaque syscall metadata identity is not preserved. |
| Realpath | Direct Linux nativepath fast path, selected POSIX Go filepath fallback | Preserve O_PATH/proc behavior and fallback ordering/link limits. The selected caller consumes failures rather than exposing every Go PathError detail. Windows filepath branches are excluded. |
| Diagnostic formatting | Original generic formatter plus ordinary source adapter | Preserve exact upstream configuration baseline bytes, colors, line endings, positions and related diagnostics. Content-mapped span execution and translated diagnostic resources remain external. |
| Configuration resolver | Original reachable ResolveConfig/ResolvePackageDirectory paths | Package-based inheritance and mapper metadata are included. This does not complete source-import resolution, type-root discovery, project redirection or Program execution. |
| Ordinary command | Authored inspection and semantic-admission entry points around upstream interpretation | Preserve all interpreted options/files in inspection. Checking rejects unsupported behavior explicitly and never coerces options to make a project pass. Legacy strict configuration remains a named compatibility entry. |

The raw JSON dependency is recorded in
[configuration-json-provenance.json](configuration-json-provenance.json). Linux ABI
and nativepath source details are in
[configuration-os-provenance.json](configuration-os-provenance.json). The direct
Go-to-C# production mapping is retained; authored adapters, generated material and
the few selected external-file combinations are identified separately.

## Corrections discovered during verification

Original assertions and Go expectations remain authoritative. Verification exposed
real adaptation defects in typed JSON duplicate/error ordering, inherited raw values,
name-map nil handling, package resolution and absolute discovered-root admission.
The implementations were repaired rather than changing original expected behavior.
The retained strict-configuration test also found an authored legacy adapter null-list
dereference on `{}`. Iterating an absent AST property list as empty restores the existing
missing-files error; it does not allocate a replacement AST node or change parser semantics.

The selected typed-path originals exposed three concrete boundary errors: equal-path
containment was accepted, a supposedly normalized path could contain repeated slashes,
and containment-only `RelativeTo` could return a `..` path for an unrelated directory.
The exact original conditions/helpers were restored. These were implementation defects;
the original rejected-input assertions were not weakened.

A Linux directory containing both an invalid UTF8 filename and a valid replacement
character filename exposed a real .NET enumeration collision. Strict encoding after
`FileSystemInfo.Name` was too late. The raw directory reader now inspects the actual
name bytes once and rejects the unrepresentable entry. Virtual filesystem byte values
remain unrestricted. This is an explicit OS representation limit, not Go equivalence
for every possible Unix filename.

Some authored harness assumptions were also wrong: typed-nil interface metadata,
raw override forwarding and finite typed package setup needed correction. The real
Linux nativepath producer uses a kernel fast path rather than always applying the
Go fallback's 255-link limit. A stronger authored expectation for a missing invalid
symlink target was withdrawn after checking that producer. These changes repair
observations or authored controls; they do not rewrite original Go expectations.

The original `vfstest.FromMap` fixture cannot construct the selected UNC roots and
its delegated file reader rejects non-UTF8 paths. UNC differential cases therefore
use a narrow reversible oracle backing-host adapter, with original UNC values still
passed to the compiler algorithms. Raw-byte configuration-owner tests use the source
API. They do not establish arbitrary-byte filesystem equivalence between that
original fixture host and the managed virtual host. Typed-path byte assertions and
real OS rejection controls are separate evidence.

Early actual Native AOT publishing caught generic JSON observer overloads that
required runtime reflection. Static node construction fixed the observer without
warning suppression. Passing managed tests was insufficient to expose that issue.

The pinned Go JSON library intentionally chooses either `cannot` or `unable to`
once per process when rendering `SemanticError`. Forty fresh original oracle
processes confirmed both forms for an identical NaN marshal request. The port uses
the permitted `cannot` form. Verification must qualify any equivalent rendering by
the actual typed JSON error producer and preserve the original raw variation evidence;
it must not rewrite arbitrary error strings, user text or compiler diagnostics. The
observer now qualifies direct original SemanticError instances, copies their fields
and projects only that first modal phrase (including directly typed nested causes).
Plain or wrapped user-lookalike causes remain unchanged. Raw error bytes and actual
kind/type/pointer/offset are retained separately. Six controls and forty fresh Go
processes pass; all265 original observations reproduce existing unchanged golden
outputs. Independent review closed; the final native and retained regression gates pass.

Two authored raw-value prior-prefix tests initially constructed ordinary Go maps,
whose traversal order is unspecified. Forty fresh processes produced two correlated
partial-output/error-offset records. The corrected ordering assertions invoke actual
OrderedMap.MarshalJSONTo; separate single-key ordinary-map malformed inputs retain
default-map producer coverage. Failed original evidence is preserved. No offsets,
partial output or compiler diagnostics are normalized. Default multi-key map traversal
order and its resulting error prefix are not claimed as deterministic equivalence.

## Test adaptations and observation limits

Shared assertion bodies serve xUnit and the native dispatcher. Scheduling and finite
test fixture hosts may differ while preserving original assertions. Original bodies,
adapted dependency assertions, reused inputs and authored comparisons are inventoried
separately; a test function with an excluded assertion is not reported as fully ported.

The comparison observes options/raw values, ordered file/spec/reference results,
diagnostic structure and formatting, selected cache/identity behavior, reload and
serialization. It does not establish equality of every private synchronization object
or allocation graph. Original clone/merge/cache tests and targeted mutation checks
retain the selected identity obligations. Compact locale IDs, allocation counters,
unselected x/text APIs and writable/platform-specific filesystem assertions retain
explicit dispositions rather than being silently counted as successes.

Configuration interpretation does not include Program-owned option-combination
validation. Content-mapper manifests, references and build options remain metadata
until their execution subsystems are ported. The accepted checker/library restrictions
are unchanged; interpreting a setting does not establish its semantic implementation.
