# Configuration interpretation and file-discovery completion contract

Status: **fixed contract accepted on Linux x64 Native AOT**.
See the [acceptance assessment](CONFIGURATION-ASSESSMENT.md) and
[durable proof](../validation/results/configuration-verified.json).
Baseline `2c850db`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
See the [mandate](./CONFIGURATION-PROMPT.md). Acceptance does not follow from a
successful project demonstration, a build, or an agent finishing its implementation.

## Selection and rationale

Complete the upstream **TsOptions configuration interpretation and discovery
subsystem**, not an authored tsconfig subset. The 17 production Go files contain
210 functions and 7,631 physical lines, including 3,224 generated lines. The baseline
has no TsOptions counterparts: its strict project configuration adapter and selected
Core option fields are not a subsystem implementation.

The latest three capability commits repeatedly changed Checker, diagnostic formatting,
Core and option metadata. Those larger semantic families are valuable future targets,
but completing the entire checker or formatter now would entangle many still-excluded
language features. Configuration offers a coherent independent API, direct upstream
tests, and a reusable boundary between project interpretation and semantic support.
It eliminates explicit-file-only/project-specific configuration scaffolding and supplies
complete option data for future work instead of repeated field-by-field additions.

The recent project assessment records 11 central library builds (59.76 seconds of
reported build time), separate from dependency analysis, oracle work and comprehensive
verification (780.934 seconds for its successful gate). Its first gate was stopped
for a real encoding defect. The collection assessment records 30 numbered central
build logs; an earlier project comparison mentioned 24. These counts have different
reported scopes and are not a controlled efficiency comparison. Tokens, monetary cost
and total model effort are unavailable. The strategy here is stable subsystem/API
ownership, independent source/test inventories, and early boundary witnesses before
one comprehensive frozen acceptance run.

## Required completion surface

| Row | Upstream responsibility | Required completion |
| --- | --- | --- |
| S1 | Option declarations and representations | All TsOptions generated declarations/options and required Core option fields, enums, parsing, defaults, cloning, equality, paths and config-directory substitution; ordered and nil/empty behavior preserved. Static generated access is permitted for Native AOT. |
| S2 | Configuration and argument parsing | All production TsOptions entry points: JSONC syntax and raw-object APIs, complete diagnostics/locations, command-line and response files, build argument interpretation, compiler/type-acquisition options, references, compileOnSave, inheritance including arrays/cycles/cache replay, show-config serialization. |
| S3 | Discovery and parsed-project queries | Literal/default/include/exclude discovery, extension precedence and all upstream file forms, wildcard directories, matched specs, reload behavior, case-sensitive/insensitive virtual hosts, common source/output/reference metadata and parsed-project helpers. |
| S4 | Necessary independent dependencies | Complete VFS matcher and directory walker; required read-side FS/OS/BOM and typed paths; complete package-JSON and semantic-version production units; genuine ResolveConfig and ResolvePackageDirectory reachable algorithms; content-mapper declaration/manifest metadata, output-path and glob dependencies. |
| S5 | Usable integration | Standalone disk configuration inspection reports genuine interpreted options, discovered files and diagnostics independently of checker support. A checking entry consumes the result and explicitly reports unsupported downstream settings/files; it must not drop or silently override them. Existing project examples remain integration gates. Obsolete project-specific parsing is replaced or isolated as a named compatibility adapter, not presented as the completed subsystem. |
| S6 | Evidence and delivery | Systematic upstream assertion provenance, implementation/function inventory separate from evidence inventory, original-Go agreement, independent omission/correctness review, Linux x64 Native AOT execution, all retained regressions, reproducible instructions, committed clean delivery. |

Every production file/function in TsOptions must have a disposition. Implementation
completeness includes behavior absent from selected fixtures. Dependencies retain their
own full/partial status; completing TsOptions does not make all of Module, VFS, Core,
ContentMapper, compiler execution or the checker complete. Generated code needs a
reproducible source-derived generation or an explicit checked mapping with source hashes.
One Go source file maps to one counterpart where practical; meaningful exceptions and
language adaptations are recorded in the source map/departures.

## External boundaries and material dependency expansion

Package-based extends and content-mapper manifest lookup are actual TsOptions
responsibilities. Their package lookup is **included**, despite source package imports
remaining excluded from the checker. Complete package-JSON and semantic-version units
avoid repeated partial extraction; resolver code covers the genuine reachable config/
package-directory paths, including conditional targets and symlink behavior. Other
resolver branches remain explicitly mapped as outside this subsystem, not permissive
fallbacks. A branch declared unreachable must be justified from original API options.

Locale parsing requires the pinned Go dependency `golang.org/x/text v0.42.0`: the
selected BCP47 grammar/canonicalization/partial-tag-and-error path and complete
registry/alias data are included. Preserve its source/version hashes and license.
Installed .NET cultures are not an equivalent substitute. Unrelated language matching
and collation are external; the selected closure is explicit in the source inventory.

Content-mapper metadata is parsed and resolved without starting mapper processes.
External mapper execution, IPC, transformations and virtual-source processing are outside.
References/build options/output filenames are interpreted; reference graph execution,
watch scheduling, incremental caches and emit are outside. Configuration may describe
unsupported semantic/compiler operations: the configuration result must preserve them;
only the downstream checking entry may reject them. No new language/library parity is
implied. Genuine existing libraries and accepted checker boundaries remain unchanged.
TsOptions interpretation/discovery diagnostics are distinct from Program-owned
option-combination validation. Inspection does not certify that arbitrary settings
form an executable compiler invocation; the checking entry retains explicit admission.

Original configuration baselines also assert colored diagnostic text with source
context and exact line endings. Include source-faithful diagnosticwriter formatting
and the ordinary configuration-source adapter to preserve those assertions. Generic
formatting is reusable; transformed/canonical content-mapped source adapters and
span-map runtime are separate external dependencies. Default-English localization
retains the foundation boundary; locale-tag interpretation does not imply complete
translated diagnostic resources. Any adapter omissions remain explicit.

Raw option and package-JSON APIs expose decoder errors. The .NET raw-value adapter
therefore includes a selected pinned Go 1.27.1 jsonwire/jsontext/v2 grammar and error
formatting dependency for compatible offsets, JSON pointers and error text. This is
not a general Go JSON framework port: streaming IO, reflection-based encoding and
unrelated option combinations remain external. Returned values, acceptance and
partial mutation must still agree; matching error messages alone is insufficient.

The parser accepts all original config values and file forms; host accessibility and
supported semantic checking are separate. Virtual FS verification exercises upstream
case modes, Windows/UNC path syntax, ordering and symlink identities. Actual native
platform acceptance is **Linux x64 only**. Any .NET/OS inability to represent raw Go
filesystem names must be explicit at the OS adapter boundary, tested against collision/
replacement mistakes, and not silently alter pure configuration values or virtual paths.
Linux directory names and symlink targets require raw native inspection before .NET
decoding: a confirmed invalid-UTF8/replacement-name collision makes lossy enumeration
unacceptable. Include narrow AOT-safe raw-name/readlink interop for explicit rejection,
alongside statx; ordinary representable file IO still uses .NET. No writable native
filesystem or additional verified platform is implied.

## Verification obligations

- Inventory every direct upstream test in TsOptions and newly included complete units;
  preserve original assertions in xUnit. Explicitly classify originals, adaptations,
  reused compiler inputs, authored comparisons, fuzz/benchmarks and external execution
  tests. Tests omitted for downstream dependencies need independent justification.
- Use an independent original-Go configuration oracle, not the prior strict-project
  observer's limited option projection. Observe complete option/raw values, null/empty
  distinctions, ordered files/references/extends data, specs/wildcards, diagnostics with
  files/ranges/chains, relevant parsed-project queries and show-config/argument results.
- Exercise valid, invalid and malformed inputs, inheritance and response cycles,
  package resolution precedence, reload/cache reuse, caller overrides, Unicode/BOM,
  nil/empty arrays/maps, virtual platform behavior and state/order sensitivity.
- Execute original tests where practical as provenance confirmation. Test totals do not
  substitute for function/branch disposition or original assertion preservation.
- Publish and run an actual Linux x64 Native AOT validation executable and ordinary
  disk inspection/checking invocation. Keep prior proofs/fixtures immutable and run
  retained foundation, binder, semantic and project regression gates on current code.
- Independent Sol review must challenge omissions, generated mappings, platform
  adaptations, weakened assertions, accidental successful checking and claims of
  full completion. Source/asset drift checks link the frozen gate to delivered code.

## Ownership and milestones

Three existing explicit GPT-6.1 Sol owners are reused. Core owns TsOptions, full option
model and metadata dependencies, plus central shared-library builds. Support owns
VFS/matching/paths, package JSON/semver and configuration resolver dependencies.
Locale/x-text implementation was subsequently transferred exclusively to support
to parallelize the independent dependency; core retains its consumer API ownership.
Verification owns original tests/oracle, native/shared validation and provenance.
Support also owns disk inspection/checking integration and the named legacy adapter;
this assignment follows the stable parser API and does not overlap core source edits.
Root owns contract, dependency/scope decisions, integration assignment, source map and
acceptance. Cross-owner independent review is mandatory. No overlapping source edits.

First stabilize API/data seams and port complete units; then integrate upstream tests
and fix differential failures; then integrate disk inspection/checking and independent
completeness review; finally freeze executable assets and run comprehensive acceptance.
Use focused checks at meaningful risk boundaries; do not rebuild centrally for each
small helper or repeat the full old gate during intermediate work.

A material dependency beyond these external boundaries requires reporting the concrete
obstacle and choices. Difficulty, test volume or an internal milestone alone is not a
stopping condition. Do not narrow this contract to make a partial result appear complete.
