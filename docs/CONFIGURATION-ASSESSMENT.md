# Configuration subsystem assessment

**Accepted: the fixed configuration interpretation and discovery contract S1–S6.**
Baseline `2c850db`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
The delivery commit containing this report preserves the implementation and evidence.
The [contract](CONFIGURATION-CONTRACT.md) was not narrowed. See the
[reproducible guide](CONFIGURATION.md), [durable proof](../validation/results/configuration-verified.json)
and [resumption state](CONFIGURATION-STATUS.md).

## Completion target and implementation

The selected subsystem is upstream TsOptions configuration interpretation and file
discovery: all 17 production files, 210 function boundaries and the required option
model. It extends beyond the current demonstration through raw-object/file/JSONC APIs,
CLI and response/build arguments, package-based inheritance, caches, references,
mapper metadata, wildcard discovery, reload/query behavior and show-config.

The baseline had no TsOptions counterparts. A complete source-derived option model
also removes repeated field-by-field additions to Core. Required dependencies include
the complete matcher/walker, package-JSON and semver units; selected read-side VFS,
typed-path and config-resolution algorithms; original diagnostic formatting and
bounded Go JSON/x/text dependencies. The [source map](../source-map.json) distinguishes
these complete units from selected dependencies and authored compatibility support.
All TsOptions function boundaries are translated, compiled and independently reviewed.
There is no identified unimplemented behavior inside the fixed contract. This does
not complete the broader subsystems supplying its selected dependencies.

The implemented ordinary entry points are documented in the [guide](CONFIGURATION.md).
Inspection returns interpreted settings, discovered files and configuration diagnostics.
Checking applies a separate explicit admission policy to the actual result. The older
strict project format remains a named compatibility adapter. Genuine inheritance and
discovery are no longer approximated by a demonstration-specific configuration parser.

## Implementation and evidence are separate

All 17 TsOptions files / 210 functions are implemented and compiled, with independent
source review. The complete units and selected dependency paths are itemized in the
[source map](../source-map.json). This is a source-completeness assessment for the fixed
contract, not a claim that every branch has an execution witness.

Actual current-source Linux x64 Native AOT passes 319 differential cases, 217 shared
assertion groups, 38 order executions per runtime and 14 ordinary CLI cases plus
14 lexical controls. All 536 new configuration xUnit cases pass without failure/skip.
The original 265 differential expectations remain unchanged. All 56 original TsOptions
test bodies are included, with 89 unchanged configuration baseline files.
The complete repaired-source gate passed in **860.209 seconds**, followed by independent
Sol evidence review. All **24,078 xUnit cases** pass with no failures/skips: configuration
536, checker 832, binder 163, parser 18,709 and scanner 3,838. All retained native
foundation, binding, semantic and project gates pass. Eleven native physical paths
represent ten targets because retained tooling publishes the ordinary CLI a second time;
all are Linux x64 ELF executables without a CoreCLR dependency.

Source, verification assets, historical fixtures/proofs, upstream sources and reviewed
assertion obligations are unchanged across the successful gate. Source SHA256:
`1354fd8ba087038cf4d952314203b52b2184fdf24367718de7bc082bdb7dc49d`.
Verification asset SHA256:
`868f4203227396b0c63d9cc5f3d318e6e5cfca2a66c05e1cc4065ef38eca142e`.

| Contract row | Accepted capability and evidence |
| --- | --- |
| S1 | Complete option model and generated metadata: 132 CompilerOptions, four TypeAcquisition and six BuildOptions fields; original clone/equality/merge/tag assertions, static Native AOT dispatch. |
| S2 | All TsOptions parsing, argument/response/build metadata, inheritance/cache and show-config responsibilities; original baselines and complete diagnostic/value comparisons. |
| S3 | Discovery, wildcard metadata, reload and parsed-project queries; original matcher/walker/query assertions, virtual case/path modes and actual disk integration. |
| S4 | Required dependency closure reviewed and exercised; complete units and selected external-library/read-OS/resolver paths mapped separately. |
| S5 | Actual native inspect/config/legacy entries preserve interpreted results and reject unsupported checking; valid example exits 0, invalid example exits 1 with eight original diagnostics. |
| S6 | Original-test inventory, independent source/assertion/omission/tooling/evidence review, comprehensive Native AOT/regression gate, reproducible guide and committed delivery. |

Static adapters replace Go reflection for selected JSON models and generated option
access; `StrongBox<long>` preserves 64-bit Go integer-pointer sharing. Standard .NET
collections, synchronization and file IO are used where semantics match. Selected Go
JSON, quoting and x/text algorithms and narrow Linux interop are required where they
do not. The [adaptation record](CONFIGURATION-DEPARTURES.md) gives the reasons and limits.

## Upstream test dispositions

The [main inventory](configuration-tests.json) contains 358 entries. A function can
contain many data rows/assertions; those are not separate upstream test functions.

| Main disposition | Entries |
| --- | ---: |
| New complete original subsystem/dependency tests | 110 |
| Newly selected complete path dependency tests | 34 |
| Retained complete original tests | 15 |
| Adapted finite-seed domains | 2 |
| Adapted selected dependency assertions | 2 |
| Seed domain already covered by a retained original unit | 1 |
| Performance excluded | 14 |
| Excluded operations outside selected dependency closure | 7 |
| Test-host framework excluded | 18 |
| External host adapters excluded | 2 |
| Cache-mutation framework excluded | 9 |
| Watch execution excluded | 144 |
| **Total** | **358** |

Linked dependency inventories refine or extend that catalog; they overlap and must
not be added into a fictitious total of unique upstream tests:

- [Locale](progress/configuration-locale-tests.json): 99 original functions;
  eight preserve full assertions, 19 adapt selected assertions, 72 are excluded
  from the parser/canonicalization closure.
- [JSON/quoting](configuration-json-tests.json): 74 entries; one full assertion
  domain, 21 selected-operation adaptations, 52 external-operation/performance
  exclusions. The jsonwire and strconv same-named rune tests have distinct provenance.
- [Module](progress/configuration-module-tests.json): 38 functions; two selected
  complete helper tests and 36 tests requiring external resolver APIs.
- [Ordered map](../validation/Configuration.Checks/ordered-map-test-provenance.json):
  four complete storage tests, one selected raw-decoder adaptation, one excluded
  allocation-budget test. [Metadata](../validation/Configuration.Checks/dependency-metadata-test-provenance.json)
  adds two complete original helper functions.
- [Native paths](../validation/Configuration.Checks/nativepath-test-provenance.json):
  seven selected POSIX assertion groups, adapted to the actual rooted Linux caller;
  relative/chdir and other-platform domains are explicitly excluded.

The original-Go replay registry contains 110 main plus 42 dependency functions.
The shared managed/native dispatcher has 217 groups: 161 assertion-preserving source
domains, 51 adapted source domains and five authored groups. Separately, the final 319
source-derived differential requests are authored comparisons. None are claimed as
whole compiler-test success. Inventory reasons identify the omitted assertions;
applicability and runtime evidence remain distinct.

## Material limitations

Configuration completion does not imply full Program or checker completion. Program-
owned option-combination validation, source package resolution, project references,
watch/incremental execution, mapper processes, emit and language services remain
external. Configuration may correctly describe a project that the current checker
rejects. Existing language/library restrictions are unchanged and remain prominent in
the guide. No full compiler tests are claimed from configuration-only input reuse.

Actual native execution is verified on Linux x64 only. Virtual Windows/UNC and case
modes do not establish Windows platform verification. Encountered unrepresentable Unix directory names or successfully resolved target bytes
are explicitly rejected at the real OS boundary rather than replacement-decoded into
another file; missing targets retain the original lookup failure. Pure configuration and virtual-host operations preserve original bytes.
Opaque OS metadata identity and sub-.NET timestamp precision are representation
limits; broad filesystem and external Go-library parity are not claimed.

Observation covers actual values, diagnostics, order and selected sharing/cache
behavior; it does not census all private allocations or synchronization objects.
JSON SemanticError comparison permits only the original typed modal variation; raw
evidence is retained. Ordinary multi-key Go map traversal and its correlated error
prefix are not asserted deterministic. These are evidence/representation limits,
separate from the external functionality listed above. Function source coverage is
complete for TsOptions; exhaustive branch/input coverage is not claimed. Frozen
generation provenance can retain earlier snapshot runtime notes: the final proof
supersedes those notes without rewriting the generation artifacts.

## Execution observations

Three explicit GPT-6.1 Sol owners were reused. Stable source ownership and one central
library builder produced immutable snapshots for independent test authors. When
original test translation became the constraint, matcher/walker, metadata and locale
test families were assigned to existing owners rather than creating more agents.
Core and support reviewed each other's production code; test authors received separate
assertion/provenance review. Gate tooling and corpus/observation work were later split
with exclusive files so those independent tasks could proceed concurrently.

The main cost was not compilation alone. Verification uncovered observable JSON
partial-state/error semantics, a real Linux replacement-name alias, inherited-root
admission mistakes and missing assertions in adapted tests. Those findings justified
dependency work. Some apparent differences were harness errors, including raw override
forwarding, typed-nil metadata and a source fixture host's inability to construct UNC
paths. Corrections are classified by owner; original expected behavior was retained.

Early Native AOT caught reflection-dependent observer overloads that managed execution
did not reveal. After static construction repaired the harness, publishing succeeded.
Copied build inputs removed an unnecessary hold on test authoring. The final gate is
ordered with new configuration checks first and the expensive retained cascade last,
so a new failure does not repeatedly consume full regression time.

One avoidable sequencing problem, also raised by the user, remains visible: the initial external-dependency
catalog listed selected JSON assertions but their translation was still pending when
gate tooling began. Authored differential coverage did not close that obligation.
The work was reassigned explicitly before source freeze. Future subsystem campaigns
should resolve assertion-level applicability and assign those bodies alongside each
new dependency, rather than letting an inventory entry appear more complete than its
actual implementation. Readiness was communicated prematurely before that closure.
These include genuine fidelity mistakes, rather than unavoidable language differences.
The typed-path review similarly found real containment and
normalization defects through applicable original assertions. JSON API correspondence
needed particular care: generic unmarshal, parent ReadValue validation and raw-value
encoding have different observable errors. The last also exposed a supported raw
metadata path that appended bytes without original validation/compaction; its bounded
producer repair is part of the fixed completion obligation, not a general JSON port.

Two initial final-gate attempts did not reach the retained cascade. The first stopped
intentionally after 37.556 seconds to correct a source-qualified test-name attribution;
534 configuration xUnit cases had passed but native publishing was incomplete. The
second stopped after 3.436 seconds because an authored ordering test constructed an
ordinary randomized Go map. Two source-correct ordered-map fixtures and two additional
single-key ordinary-map controls now distinguish these producers. Earlier failed
artifacts are retained, no original expected behavior is normalized, and the final
corpus has 319 cases. These are verification/inventory errors, not compiler defects.

The third gate passed the new Native AOT/configuration checks, then failed one retained
checker test after 224.649 seconds (831 passed, one failed). The legacy authored
configuration adapter dereferenced the nil property list of an empty JSON object.
A null-safe iteration restores the original missing-files rejection without changing
parser behavior or expected tests. All 27 retained project shared groups are now part
of the focused repair check. This was avoidable validation sequencing: the cheap
existing tests for the directly changed adapter should have preceded the full gate,
even though ordinary CLI integration already passed. The repaired source then passed the fresh fourth comprehensive gate reported above;
the earlier native pass was not substituted for its proof.

The first recorded coordinator timestamp is 2026-10-09 18:03:13 UTC, after work began.
It is not an exact start or total model-effort measurement. The central ledger records 19 library/CLI compiler invocations
(12 successful, seven failed), totaling 85.811 seconds. Adapter/oracle builds and AOT publishing
are separate. This is not total effort or a controlled comparison with earlier
campaign build counts. The successful gate took 860.209 seconds; the three stopped or
failed attempts took 37.556, 3.436 and 224.649 seconds. Token and monetary telemetry
are unavailable. No user unblock or system package installation was required. The user supplied useful
process feedback; that is distinct from having to manage the workers or resolve access.

## Next high-value subsystem

The reached dependency structure points to the remaining source-import module
resolver, followed by full Program construction/validation, as the next candidates.
They can reuse genuine option, package, semver, path and configuration machinery.
Assess their separate upstream completion boundaries before combining them: completing
the resolver does not itself supply missing checker language features or project build
execution. Classes, destructuring and broader libraries remain independent barriers
to ordinary application checking.

Downstream work can now rely on genuine configuration interpretation/discovery,
complete option metadata, package/semver machinery and the documented query/cache
behavior, with explicit OS and downstream admission boundaries. This removes the
recurring strict-config/explicit-files scaffolding and narrow option-model additions.
It does not make arbitrary discovered projects semantically checkable.

The result supports continuing the subsystem-first strategy, with a concrete process
correction: assign source-derived dependency assertions alongside the port, qualify
provenance by source identity, and run cheap tests for directly affected accepted
adapters before broad integration. It does not establish a measured token saving or
whole-checker tractability. Completing the remaining source-import resolver is the
next recommended campaign; assess full Program construction as its subsequent boundary.
