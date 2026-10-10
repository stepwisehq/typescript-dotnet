# Program construction and environment assembly contract

**Fixed campaign boundary; accepted with actual Linux x64 Native AOT evidence.** Baseline `1d6bd61`,
upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`. See the
[user mandate](PROGRAM-PROMPT.md), [assessment](PROGRAM-ASSESSMENT.md) and
[checkpoint](PROGRAM-STATUS.md).

## Completion definition

Complete the pinned compiler's Program construction/environment responsibilities,
including its local single-file update/reuse lifecycle. This is not completion of all
of `program.go`, the entire Compiler package, the checker, or the incremental builder.
Selected and external functions must be discoverable in the source mapping and source
responsibility record. Being inventoried, implemented, source-reviewed and behaviorally
exercised are distinct states.

The boundary follows actual NewProgram/processAllProgramFiles and their consumers,
not whichever files the existing demonstrations happen to load. Preserve one Go file
to one C# counterpart where practical; preserve original algorithms, flow, naming and
license notices. Additional authored entry/admission/observation code is identified.

| Gate | Required behavior |
| --- | --- |
| P1: Program and host lifecycle | Actual ProgramConfig/Hosts/Factories, NewProgram, source acquisition, local UpdateProgram/ReuseProgram and fallback, host/resolver factory forwarding, source identity and independent per-Program state required by original invariants. Lazy CheckerPool construction, partition/association algorithms and public pool interfaces follow source; no eager or fabricated checker evaluation. |
| P2: complete source environment | Consume genuine parsed configuration/discovery/resolution, source forms, missing and duplicate files, module metadata, ordering and include reasons. Load default/explicit libraries, transitive lib references, replacements, noLib/no-default-lib behavior, automatic and explicit type references/type roots, and all declared dependencies according to actual options/hosts. No declaration simplification or checker-driven file omission. |
| P3: project-reference environment | Resolve/read referenced configurations and original source/output mappings, include source/prebuilt declaration redirects and dts-faking read host behavior, preserve cycles/missing states and diagnostics. Include local reference state needed by update/reuse. No project build scheduler, declaration generation or pretending missing generated content was emitted. |
| P4: diagnostics and queries | Full Program-owned compiler-option, project-reference and output-path collision validation, config/syntactic/bind/include/environment diagnostics and their original attribution/order/dedup/directive policy. Source/reference/resolution/library/common-directory and other construction-owned query APIs needed by downstream stages. Preserve stored module/type diagnostic publication, including automatic type tasks; revisit the previously unexercised type-publication path through actual Program producers/factories. |
| P5: mapped-source integration | Original mapper host/project callback/result/error interfaces, in-process TransformAndParse/ParseResult/collision validation, SpanMap unit and required AST mapped-source metadata. Preserve Program supplemental roots, mappings, format and diagnostics when a genuine host Project callback is supplied. Ordinary host-unavailable behavior must follow source; never ignore configured mappers or replace their declarations. |
| P6: acceptance and delivery | Systematic original assertions/provenance plus meaningful comparisons against unmodified upstream NewProgram under equivalent hosts/options/libraries, actual Linux x64 Native AOT execution, all accepted regression gates, independent source/assertion/omission review, ordinary reproducible invocation, updated mapping/state/assessment, checker completion roadmap and a clean committed delivery. |

The production disposition record must account for all relevant Program/Host/CheckerPool,
FileLoader/FilesParser/FileInclude/IncludeProcessor/ProcessingDiagnostic, three
project-reference source files, and selected dependency responsibilities. Do not mark
all of a source file complete because its selected construction functions are complete.

## Required supporting boundaries

Include genuine bundled library bytes from the pinned revision, with a reproducible
manifest/loading location. The three embedded libraries used by the earlier semantic
profile are not a replacement for default ESNext/DOM/etc environment selection. Loading
and binding libraries does not establish full checking of their bodies/APIs.

Include required read-side cached VFS, KnownSymlinks, original synchronization collection
APIs, output-path enumeration and common-directory helpers. Managed locks/collections/
lazy initialization may adapt Go runtime facilities while preserving source guarantees.
Cached-VFS write forwarding and new writable OS infrastructure remain outside this
read-side campaign. Explicitly account for their original test exclusions.

Output-path collision diagnostics are included even for configurations that would emit.
No output is written. Program's local update/reuse is included because original Program
tests exercise it; full incremental build-info execution, watch and project build
scheduling remain external.

Content-mapper subprocess launching, JSON-RPC/IPC and external mapper lifecycle
(`hostimpl.go`) are external. The actual host callback boundary and in-process validation
are included; supplied transformations are not substituted by a miniature mapper. An
ordinary host without a mapper Project reports the upstream unavailable/failure result.
Mapped diagnostic behavior that requires missing checker algorithms remains explicitly
blocked at semantic entry; it must not become an empty successful diagnostic result.

Host diagnostic tracing is included. Enabled performance trace-file recording,
checker type-tracer construction and tracing file lifecycle remain external. A disabled
trace value follows actual guarded source behavior; unsupported enabled operations
reject explicitly rather than pretend to record a trace.

## Checker and API separation

Program inspection must construct and expose the actual environment without acquiring
an unsupported checker. It must distinguish construction diagnostics from semantic
checking that was not requested or is unsupported. Existing accepted semantic profiles
and commands retain regression protection and their named environment policies.

Do not rewrite options, suppress missing globals or omit files to fit those profiles.
Where Program semantic entry is connected to the existing checker, require genuine
supported algorithms/admission and complete diagnostics; otherwise report unsupported
semantics. General GetSemantic/Global/Declaration/Suggestion checker computation,
generators/yield and other missing language features are not implicitly authorized by
an original environment test that happens to request them. Port Program-owned collection/
filter/directive logic where applicable, retaining explicit consumer boundaries.

Ordinary API/CLI inspection will report loaded files/libraries/references, relevant
relationships and complete in-scope diagnostic categories, with reliable error exits.
The concrete command and schema are chosen during integration and documented before
acceptance. This is not a full tsc command or an assertion of successful type checking.

## Verification and test provenance

Inventory applicable original Program/CheckerPool/content-mapper and newly required
dependency tests. Preserve complete assertions when executable within the contract.
Mark whole original tests blocked by external checker/process/build responsibilities;
label any independently executable subsets as adapted tests, never as full original
passes. Compiler-input reuse supplies differential evidence, not full compiler baseline
success. Retain original expected behavior; no fixture-specific implementation.

Use unmodified upstream NewProgram for environment comparisons, without the prior
resolver graph's library/reference/automatic-type omissions. Compare actual file and
library closure, metadata, reference relationships, inclusion reasons, diagnostics and
relevant update/reuse outcomes. Exercise valid/invalid configurations and sources,
missing/cyclic/reference interactions, all library-selection mechanisms, type packages,
case/symlink identity, factories, mapped source validation and failures. Use focused
behavioral witnesses for consequential state/invariant risks; do not require exhaustive
private storage equivalence or expand observation merely because it is possible.

Source-review and a genuine Program factory publication witness can distinguish correct
handling of a nonempty type ResolutionDiagnostics result from its reachability through
the default resolver. Report that distinction; do not fabricate a default-resolver claim.

Cheap original/consumer managed checks precede costly acceptance stages. After API
changes use normal incremental dependency builds to avoid stale adapters. Final frozen
acceptance includes actual Linux x64 native publishing/execution and the complete retained
resolver/configuration/project/semantic/binder/foundation cascade. Prior proofs/expected
fixtures remain immutable. Virtual Windows/UNC/case hosts do not establish other native
platforms. A failed/partial gate is not acceptance.

## Checker completion roadmap

During implementation, produce a source-grounded roadmap of remaining checker and
required infrastructure. Separate absent/partial algorithms from implemented but
unverified paths, identify actual source dependencies, group work into substantial
campaigns, and define a checker-wide completion gate. Avoid unsupported percentages,
token/cost estimates or API-by-API fixture work as a substitute for source completion.

The eventual gate must require real environments, explicit supported compiler modes,
full relevant diagnostic comparisons (including invalid inputs and file locations),
applicable upstream assertions, algorithm coverage reconciliation, independent omission
review and actual Native AOT. Unsupported-path exceptions cannot count as successful
checks. Record remaining standard-library/JS/JSX/module/control-flow/type-system and
Program consumer barriers as the source shows them. Benchmarking large projects against
Go and JavaScript tsc with emit disabled follows checker/infrastructure completion;
this campaign does not start those comparisons or optimization work.

## Execution

Reuse three GPT-6.1 Sol owners with disjoint code ownership and peer review. Core owns
Program/CheckerPool and central builds; support owns loading/host/reference and selected
dependencies; verification owns original tests/oracle/evidence and roadmap investigation.
Astra owns scope and acceptance. Reuse accepted findings; investigate foundations only
when a concrete dependency or discrepancy demands it. Keep state concise and resumable.
Continue until this contract is met or a material boundary/access blocker requires a
decision; do not stop at an internal milestone or narrow the contract to declare success.
