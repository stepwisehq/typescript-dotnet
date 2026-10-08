# Foundation campaign assessment

**Accepted against the fixed foundation contract on Linux x64.** The frozen native
gate passed with no source or verification-asset drift. See the
[durable evidence](../validation/results/foundation-verified.json) and separate
[complete Go numeric seed replay](../validation/results/foundation-go-numeric-fuzz-verified.json).
Committed delivery completes the campaign; this is not full compiler parity.

The [fixed contract](./FOUNDATION-CONTRACT.md) covers scanning, initial parsing in
JS/JSX/TS/TSX/JSON modes, ordinary syntax-node representation and operations, source
positions and default-English syntax diagnostics. It is deliberately broader than
the earlier feasibility samples. It does not complete binder/checker-owned state,
transformations, project configuration, module resolution, emit or language services.

## Implementation and source fidelity

All 158 scanner functions and all six parser files have explicit dispositions.
The syntax inventory accounts for 2,752 upstream function rows across 27 files;
these are inventory rows, not independently exercised branches. The utility file
accounts for 401 implemented bodies, five language adaptations, seven retained
shared-support functions and eleven later-stage exclusions. All six precedence
functions are included. Generated syntax fields, factories, updates, visitors,
full subtree facts and lazy child iterators retain upstream structure.

Required support now includes complete PositionMap and scanner coordinate helpers,
source caches and typed data keys, ordinary diagnostic constructors and all 2,222
English diagnostic metadata records. Full facts supersede the earlier private JSX
projection. The [source map](../source-map.json), detailed inventories and
[adaptations](./FOUNDATION-DEPARTURES.md) distinguish ported code from authored
harnesses and managed compatibility support.

The direct tests and broader comparisons exposed real defects: floating formatting
roundtrip and BigInt separators, token bounds, permissive accessors, declaration-map
symbol access and overload grouping, arbitrary declaration filenames, equal-key
reparsed-node ordering and callback slice-length behavior. Repairs preserve upstream
expectations. The ordering repair required 326 formatted C# lines from pinned Go's
comparator sort closure; .NET sorting does not preserve its observable tie order.

## Evidence and limits

The final gate passed in **231.230 seconds** on .NET SDK 10.0.401, Go 1.27.1
and Node 24.21.0. All five published executables are native Linux x64 ELF binaries,
linked to libc/libm rather than CoreCLR. Foundation's 15,960,016-byte executable
includes embedded verification corpora and assertions; its size is not a library
footprint or performance measurement.

| Evidence | Result | What the count means |
| --- | ---: | --- |
| xUnit original finite parser seeds | 17,633 passed | One data-driven case per seed; original no-panic assertion |
| Other xUnit cases | 5,297 passed | Original table rows, historical differentials and authored checks |
| Total xUnit | 22,930 passed, zero skipped | Executed cases, not handwritten upstream test functions |
| Shared assertions under Native AOT | 3,153 passed | Also represented in xUnit; do not add to its total |
| Original parser seeds under Native AOT | 17,633 passed | The same seed population, executed natively |
| Native syntax graphs versus Go | 17,723 exact | Separate stream comparison, 2,647,487 nodes and 4,118 root diagnostics |
| Native ordinary scans versus Go | 17,723 exact | Separate stream comparison, 3,894,564 tokens and 2,384 diagnostics |

The native regression cascade also preserved 1,276 scanner, 471 parser, 118 binder,
45 scalar, 43 anonymous-object and 92 named/composite-data cases, with semantic
boundary controls. These overlap other evidence populations; there is no meaningful
single grand total. Original Go subsystem tests passed. Separate complete numeric
fuzz baseline replays passed 156 and 236 deduplicated inputs in 9.239 seconds;
the gate's shorter numeric fuzz calls alone are only smoke checks.

The [verification document](./FOUNDATION-VERIFICATION.md) and
[test catalog](./foundation-test-catalog.json) distinguish original assertions,
adapted assertions, compiler inputs reused only for foundation comparisons and
authored comparisons. The original localization test preserves only its three
default-English rows out of thirteen and is explicitly adapted. Benchmarks and
later-stage assertions are not counted as correctness passes.

The inventory covers 144 upstream functions in the selected subsystem/support and
runner files: 121 tests, six fuzz targets and 17 benchmarks. Their dispositions are:

| Disposition | Functions | Meaning |
| --- | ---: | --- |
| Original assertions preserved | 39 | Includes finite seed bodies for three fuzz targets; no unbounded-fuzz claim |
| Adapted assertions | 1 | Default-English localization subset |
| Excluded | 102 | Unneeded shared APIs, translated locale/build support, unrelated numeric operations and benchmarks |
| Later-stage blocked | 2 | Compiler and transpile runner assertions; their source inputs are reused separately |
| Unresolved | 0 | No unknown disposition remains |

Of these, 67 functions are direct subsystem inventory entries: 39 preserved, one
adapted and 27 excluded. The other entries account for shared-package and runner
boundaries. These counts are not a census of every test in the upstream repository.
The catalog separately inventories compiler inputs and language-service suites.

The final corpus contains 17,723 requests across the five declared modes, extracted
from 13,017 physical inputs with the original splitter. One build-info JSON and
three JavaScript bodies with map filenames have explicit authored mode adaptations;
25 non-source units are excluded with provenance. Original finite FuzzParser
membership remains 17,633 seeds. Its no-panic assertion is weaker than, and separate
from, the graph comparison.

Structural comparisons include named syntax fields and scalars, nullable lists,
parents, child order and identity, diagnostics and related information, actual full
facts/cache state and source-position mapping. Additional probes exercise clone,
update, factory, visitor and cache identities. Exact diagnostics and recovery are
compared on malformed inputs, not merely successful parsing.

Four defensive diagnostic codes remain unobserved: scanner 1513 and parser 1140,
1179 and 1439. Independent source review explains their public unreachability or,
for 1179, suppression before publication. These are not counted as exercised
diagnostics. See the [scanner audit](./progress/foundation-scanner-audit.md) and
[parser arguments](./foundation-parser-unreachable-diagnostics.md).

Finite test coverage is not exhaustive correctness. Numeric shortest-decimal
equivalence for every binary64 value, arbitrary Go backing-array aliasing, general
concurrency, resource exhaustion, compiler-created synthetic trees and excluded
later-stage behavior are not established. The native acceptance platform is Linux
x64; other operating systems and architectures require their own publication and
execution evidence.

## Working process and observed effort

Three existing GPT-6.1 Sol agents were reused for implementation and review, with
disjoint ownership and one shared-library build owner. Shared assertion bodies let
xUnit and Native AOT execute the same intended tests. One broad managed comparison
identified repairs, followed by focused replays instead of repeated full runs.
Independent reviews examined both new and previously partial helper bodies.

The first direct native gate took 22.313 seconds. The initial broad managed parser
comparison took 107.799 seconds and ordinary scanning took 21.603 seconds. The final guarded gate took 231.230 seconds. These
are measured command durations, not total labor or performance benchmarks. Token
usage, model billing and active human/model labor were unavailable; no subscription
cost is inferred. No package installation or approval request was needed. The user
explicitly steered the campaign toward original upstream tests, which proved useful.

Avoidable work came from an authored Node script error, omitted probe dispatch and
incorrect classification of four ancillary assets. An overly broad path-test
exclusion also needed correction. Source inventories and original assertions caught
gaps that aggregate differential test counts had hidden. The durable state remains
small Markdown checkpoints plus source/test inventories and compressed reproducible
corpora; no database or workflow framework was necessary.

## Downstream implications

Downstream work can now rely on this defined scanning/parsing/syntax
surface and its regression corpus, including malformed-input recovery and ordinary
syntax operations. This does not imply full compiler parity or eliminate the need
for new tests when downstream paths exercise previously unobserved interactions.

Following the user's layer-first direction, the next substantial campaign should
complete and systematically verify source-file binding: original binder tests,
symbol-table and flow identity, allocated empty tables and the remaining documented
binder exclusions. That would provide a stronger base for resuming callable types
and functions. Neither foundation success nor the accepted data-shape checker proves
that whole-checker or representative-project support will have comparable cost.
