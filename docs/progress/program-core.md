# Program construction: initial core assessment

Baseline `1d6bd61`; original revision remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Initial assessment is followed by the implementation checkpoint below; neither
translation nor compilation alone is acceptance.

Current status: the complete campaign is accepted by the independently reviewed
[Program proof](../../validation/results/program-verified.json). The assessment
below preserves the initial findings; final gate and build metrics appear at the end.

The four requested producer files contain 3,116 physical lines and 178 Go function
declarations: [program.go](../../upstream/TypeScript/tsc/internal/compiler/program.go)
2,506/151; [host.go](../../upstream/TypeScript/tsc/internal/compiler/host.go) 113/9;
[checkerpool.go](../../upstream/TypeScript/tsc/internal/compiler/checkerpool.go)
495/18; [pkg.go](../../upstream/TypeScript/tsc/internal/compiler/pkg.go) is a
2-line package declaration. Whole-file counts include emit/checker/lifecycle APIs
and are not a proposed construction-only completeness claim.

## Coherent responsibility boundary

Complete `NewProgram` construction and its resulting environment: genuine parsed
options, roots, default/explicit/noLib libraries and overrides, automatic types,
imports/references, reference source/output redirects, metadata/modes, missing-file
and include explanations, config/program/syntactic/bind diagnostics, option/reference
verification and output-path collision metadata. Retain source host/factory callbacks
and typed Go-byte paths; do not seed fixed checking options or inject three fixed
libraries into this environment. Program construction initializes a lazy checker
pool, not semantic execution. Read/query APIs should expose actual source results.

The current [Program.cs](../../src/TypeScript.Scanner/Compiler/Program.cs) is only
fixed-host diagnostic policy. The selected loader deliberately omits default/global
library and automatic-type environments; its graph edges cannot replace the source
per-file resolution tables, automatic pseudo-task, LibFile map or reference maps.
`verifyCompilerOptions` is about 500 lines and performs genuine output-path validation
although actual emit can stay outside this campaign. Existing OutputPaths metadata
is therefore a real dependency, not grounds to discard emit-related options.

Update/Reuse are separate from fresh construction, but the original
`TestProgramHostsAndFactories` and `TestClonedProgramProjectReferenceResolution`
exercise them directly. Including their roughly 220-line control/copy family is a
coherent modest extension if the coordinator wants those complete original bodies;
a fresh-only contract must explicitly classify lifecycle assertion subsets instead.
Actual emit/build/watch and unported semantic/declaration/suggestion checker execution
remain external unless explicitly authorized. Construction itself must not impose
checker syntax/file admission on declarations, JS, JSON or other loader-supported input.

## Necessary dependency seams and pressures

- Exact `ICompilerHost`: FS, default library directory, diagnostic trace, source-file
  parse callback, mapped-source callback/project and resolved-reference callback.
  `ProcessAllProgramFiles(ProgramOptions, singleThreaded)` returns processed source
  data, actual ResolutionData and the error channel. Hosts/factories stay separate
  from retained ProgramConfig/shared data.
- Complete selected loader/library/automatic-type scheduling, final include diagnostics
  and all three project-reference producer files (583 lines): parser, mapper and
  declaration-faking resolver host. References need real config recursion/order,
  source/output maps and realpath/symlink behavior, without executing referenced builds.
- CachedVfs, KnownSymlinks and authoritative bundled library resources are missing
  direct dependencies. Original TestProgram asserts actual bundled library file
  membership/order; the old three declaration files are not an equivalent fixture.
- Content mapper construction is genuine pressure: default host calls TransformAndParse,
  checks supplemental filename collisions, then loader publishes failures/mapping
  diagnostics and source identities. Coordinator must choose actual injected host
  callback/result lifecycle versus proportionate transform/span-map runtime closure.
  Mapper descriptors cannot silently disappear or become ordinary untransformed TS.
  External process execution is a distinct boundary from consuming actual host results.
- Built-in CheckerPool has source weighted graph partitioning, lazy checker creation
  and exclusive release semantics. Its pure policy/original assertions are feasible;
  existing Checker.CreateForProgram has admission restrictions and no all-declaration
  construction, so invoking semantic APIs requires an explicit downstream boundary.
  One mapper original assertion uses generator/yield and incremental global diagnostics;
  this is a real unported checker dependency, not permission to change the source input.
- Preserve raw filenames/path keys in source APIs. Existing named legacy checking
  scalar-filename adapters must remain explicit; do not infer a filename's byte role.

## Suggested ownership and early validation

Core: Program counterpart/config/API models, CheckerPool, option and construction
control flow; sole grouped library/CLI builds. Support: Host, loader/parser/include
five files, project-reference three files, read cache/symlink/bundled dependencies
and host/CLI integration. Verifier: original assertions/fixtures/oracle and targeted
consumer checks. Root fixes the written contract and external dependency decisions.

Pair host/factory/reference/library/option families with their original assertions
before integration. Use normal incremental dependency builds after API changes,
then cheap retained managed consumers before the comprehensive gate. Do not expand
private observation merely because retained data exists; upstream invariants and
concrete correctness risks determine useful identity/concurrency witnesses.

## First producer checkpoint

The fixed [contract](../PROGRAM-CONTRACT.md) includes fresh construction, local
Update/Reuse, lazy CheckerPool, selected diagnostic/output metadata and in-process
mapped-source consumption. Core counterparts and supporting APIs are staged; support
has supplied the source-shaped loader/reference/cache/library family. The first
coherent library compile reached declaration binding and reported two mechanical
errors (public include-reason model visibility and a reserved tuple element name).
Its actual log is under ignored `artifacts/program-validation/build1/library.log`;
measured invocation was 2.78 seconds. Body binding and independent runtime tests are
still pending. No source function has been individually execution-qualified.

The mapper fingerprint uses System.IO.Hashing 10.0.8 XXH3-128, canonical numeric
Hi/Lo ordering matching the original big-endian Bytes method; actual Go vectors
remain required. Static source-generated option descriptors replace reflection for
MarshalDeclaredOptions. The SpanMap JSON dependency is a fixed int32-matrix decoder
retaining partial destination state; generic JSON APIs are unchanged. Semantic
acquisition explicitly admits only the accepted checker options/declaration profile;
construction and inspection retain authentic unrestricted environments.

The consolidated declaration/body repairs now compile cleanly in snapshot 3b:
zero warnings/errors, 4.42 seconds. The immutable DLL SHA-256 is
`b380fba012324d6d35793a68f182fadfd2e301e7d475b0f5b06271da1d38f588`.
The actual central ledger records five invocations (four mechanical failures, one
clean compile), 26.54 seconds total. Verification owns consumer assertions and
actual original comparisons on that copied artifact; no runtime acceptance is claimed.

Independent assertion read-through compared all seven cached-VFS operation/count
sequences, SyncMap missing/present-nil operations, and all seven CheckerPool source
bodies with their actual managed table domains. No missing source assertion found;
Go t.Parallel scheduling and bounded managed indices are explicit adaptations.

## Coherent snapshot 4b

The library-only 4 build passed with zero warnings/errors in 3.80 seconds. The
ordinary inspection binder channel was then included in a normal incremental CLI
build, which rebuilt its library dependency and passed in 4.22 seconds. Snapshot
4b is copied at `artifacts/program-validation/build4b/snapshot.json`; library SHA
`829b1a9074e6c3e837f4cd33b4e9f427c2085819390509977e0d9a3725c65025`.
The central ledger now has seven invocations, three clean/four failed, 34.56 seconds.

Verification's actual copied-3b shared report passed all 68 staged original bodies.
The finite decoder/hash comparison identified one typed matrix delimiter error
and missing static serialization for ordered raw option values; those source
repairs are included in 4b and require actual original replay. Hash replacement
was not inferred from the previous wrong-input hash values. The ordinary smoke
now includes bind diagnostics and actual global/per-source inclusion diagnostics;
it loaded the genuine configured libraries without checker acquisition.

[Independent assertion review](review-program-assertions.json) records exact
source/body hashes for Program7, pool7, cache7, SyncMap nil and the two separately
authored scheduling/semantic controls. [Selected dependency review](review-program-support.md)
records source and lifetime boundaries, including retained reference-mapper
publication and generation-local include locations. Remaining source reviews,
actual native execution and the final campaign proof stay separate.

The function inventory merges 125 new support identities with the initial 178;
nine Host functions overlap and are not counted twice. Its 303 declarations have
291 selected responsibility dispositions and 12 external APIs, including four
reference-faking host write operations; translated does not mean accepted or
individually exercised. Broader dependency unit mappings remain separately
recorded by the coordinator.

## Coherent snapshot 6 and source review

Snapshots 5 and 6 passed normal incremental library/CLI builds in 4.59 and
4.16 seconds. The actual [central ledger](../../artifacts/program-validation/build-ledger.json)
records nine invocations, five clean/four mechanical failures, 43.31 seconds total.
Snapshot 6 [copy](../../artifacts/program-validation/build6/snapshot.json) has
library SHA `61a9babbb274b08dc164df4a9dab548b63c3e17e5aa193e7b99b6304dbd3dfa0`.

The four independently identified parser/diagnostic source branches are repaired
and compiled: redirected child scheduling, original redirected-input reason
selection, one duplicate casing record per acquisition, and allocated empty include
details for a nonempty diagnostic owner. Focused behavioral consequences remain
verifier-owned. The two raw typed config-source producers now mark their explicit
Go-byte filename role; the actual Unicode owner comparison exposed double encoding
through the newly reached diagnostic-path consumer. Public filenames/path keys and
legacy scalar/null filename semantics are unchanged. Support independently approved
these producer markers and the diagnostic-path role selection.

Ordinary inspection now calls the source public inclusion diagnostic API per source,
preserving SkipTypeChecking and preceding-directive policy without acquiring a
checker; fileless inclusion diagnostics remain in Program diagnostics. Source
construction/options/read state remains authentic. The raw collection accessor is
a separately declared observation seam, not ordinary inspection policy.

Prior copied4b evidence passed 70 complete original shared groups plus two authored
controls and all 56 typed-matrix/declared-option-hash dependency records. Mapper
shared groups are being qualified separately as full bodies, adapted channels and
a construction-only subset; their total is not an all-original count. The 60 new
[core requests](../../validation/program-corpus/requests-core.jsonl) are authored
source-branch inputs, with expected records produced exclusively by the pinned Go
implementation. Their first comparison found 58 exact, one observer nil-versus-empty
owner issue and the repaired Unicode config owner. Final native and campaign
acceptance remain pending.

The function inventory now records the completed selected-boundary peer source
reviews separately from compilation and campaign runtime. The 303 declarations
retain 291 selected and 12 external dispositions; no translated/reviewed function
is claimed individually exercised.

Snapshot 6 retained filename consumers passed actual tests: default Binder empty
module-name assertion, Resolution graph/legacy Unicode byte-role control and
Scanner Unicode diagnostic arguments (one test each, no skips). The focused
[report](../../artifacts/program-validation/build6/retained-filename/report.json)
binds normal incremental dependency builds and actual TRX counters; it is not a
substitute for the later complete retained gate.

The source-status reconciliation explicitly classifies suggestion-checker producer
computation as external rather than translated: its no-op policy guard remains,
and actual acquisition otherwise rejects. The public wrapper retains actual
collection/admission control. This disposition is a contract-accounting correction,
not a source change or newly supported semantic computation. Finite dependency
scopes are recorded alongside, separately from the 303 compiler declarations.

## Targeted source repair and snapshot 7

The focused Linux Native AOT run on immutable6 passed all 78 registered shared
groups (72 assertion-preserving, four adapted channels/construction subsets and
two authored) and all 56 dependency records. This is the early metadata/resource/
package risk check, not the final retained acceptance gate. The 60 core records
also matched completely after the Unicode producer-role correction.

The genuine mapper corpus then exposed a supplemental ExplainFiles renderer that
bypassed source relative-name presentation. Support repaired that branch; independent
source review also corrected the three Program relative renderers to use actual
host case sensitivity instead of the parsed config's filesystem. Named legacy
parsed/null fallbacks remain. Snapshot 7 compiled this bounded source packet cleanly
in 3.77 seconds; library SHA
`f235751ab8dfc364fc06942a75843efca1eb10531d2017824c925b6cb384da4f`.
Actual central totals are ten invocations, six clean/four mechanical failures,
47.08 seconds. Targeted mapper21 replay and settled final manifest review precede
source freeze; no broad managed/native readiness replay is required.

The [Program gate](../../tools/run-program-gate.py),
[recorder](../../tools/record-program-evidence.py) and
[shell](../../tools/verify-program.sh) reuse accepted verification architecture.
Original/direct managed/ordinary inspection and affected filename consumers run
before native publication, followed by exactly one retained resolution cascade.
They passed final preflight and both independent tooling reviews before the frozen
full gate.

## Final accepted campaign

The complete gate passed in **982.578 seconds**, exit 0, with source, verification
assets, historical assets, upstream and assertion obligations unchanged during
execution. The [durable proof](../../validation/results/program-verified.json)
binds all 186 complete original/managed/native records, 78 shared groups (72 full,
four adapted and two authored), 76 order executions per runtime and two ordinary
CLI inspections. Program xUnit passed 264 cases; the retained cascade passed
another 24,334, yielding **24,598 passed, zero failed or skipped**. Three focused
filename consumers also passed separately. All 15 physical native paths were
inspected as Linux x64 ELF executables without a managed runtime dependency.

Proof recording initially failed before writing because the recorder followed one
extra `retainedExecution` layer when aggregating the retained test totals. The
permanent correction changes only that lookup line. An independently reviewed
one-off driver reproduced the successful gate's asset fingerprint with the exact
archived pre-correction recorder bytes substituted for that file alone; all other
assets, sources, obligations, binaries and historical proofs remained checked.
The proof records the old/new recorder hashes, driver hash and separately labeled
current asset hash. No runtime rerun or expected-output change was needed.

Final source snapshot remains **7**. Actual central production build totals remain
**ten invocations, six clean/four mechanical failures, 47.08 seconds**, separate
from full gate time. The function inventory now points to the accepted campaign
proof and refreshes actual counterpart hashes; this is aggregate qualification,
not a claim that every selected source function was individually exercised.
