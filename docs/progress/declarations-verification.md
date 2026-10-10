# Declaration/object-model verification assessment

Active under [D1–D6](../DECLARATIONS-CONTRACT.md). Baseline
`e7b81a2`, pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
The [checker roadmap](../CHECKER-COMPLETION-ROADMAP.md) and accepted
[Program proof](../../validation/results/program-verified.json) supply the starting
environment, not evidence of completed declaration checking. The initial source
assessment below predates implementation; the current execution checkpoint separates
reference evidence from managed/native acceptance. Core remains the central library
builder; verification has made no production edits or private observation expansion.

## Current execution checkpoint

Both actual pinned checker original bodies passed unmodified Go, zero skips, in
1.567 seconds; [the report](../../artifacts/declarations-validation/original-checker-tests/report.json)
binds the source and full event log. Complete shared C# bodies and thin xUnit wrappers
are staged in [Declarations.Checks](../../validation/Declarations.Checks/) and
[Declarations.Tests](../../tests/Declarations.Tests/). Both complete shared originals
passed immutable snapshot2; [the managed report](../../artifacts/declarations-validation/build2/checker-checks/report.json)
binds executable and library hashes. Native and xUnit remain pending.
[Source-qualified applicability](../declarations-tests.json) keeps the two originals,
compiler-input reuse and mapper semantic debts distinct.

The materialized [warehouse](../../examples/declarations/warehouse/tsconfig.json)
reference loads 62 genuine files and has zero global/semantic diagnostics. Its
[invalid consumer](../../examples/declarations/warehouse-invalid/tsconfig.json)
loads 61 and reports six original semantic errors (2345, 2341, 2445, 2322, 2741, 2511).
The default-options interface/ambient input loads 91 files and has zero errors.
[All three reference records](../../artifacts/declarations-validation/original-vertical/report.json)
use actual Program global-before, bulk, per-file, global-after and repeated semantic
queries with unchanged libraries/options. The compatible observer extension runs only
when Semantic is explicitly requested; accepted Program inputs/expectations are unchanged.
No managed, native or ordinary semantic acceptance follows from these reference results.

The [source-family requests](../../validation/declarations-corpus/requests-families.jsonl)
now provide 41 authored D1–D5 inputs, all completed by genuine Go in 0.620 seconds.
They compare actual global/bulk/per-file/repeated/incremental diagnostic channels and
33 finite public symbol/type/property/base/constructor displays. These public queries
address concrete diagnostic blind spots: successful inherited signatures, class-expression
inference, enum unions and unique-symbol property names. No private type census is added.
Missing globals and wrong Array arity preserve actual errors; constructor branches,
early return, pre-super access, malformed declarations and original directives retain
their full diagnostic owners/spans. [The reference report](../../artifacts/declarations-validation/original-families41/report.json)
binds actual source and executable bytes. Five additional authentic tslib/downlevel
controls preserve missing-module/export/wrong-arity diagnostics and valid helper cases.
Snapshot2 is 3/41 exact; 38 cases fail explicitly, chiefly on genuine library tuple
normalization passing a negative signed range count to the managed runtime.
No options or libraries were reduced to conceal that failure; native remains pending.

The [compiler-input provenance](../../validation/declarations-corpus/requests-compiler-inputs-provenance.json)
accounts for 23 inspected upstream fixtures: 20 reused unchanged after the original
regular-harness extraction, across 21 option variants; three whole inputs excluded
for destructuring, async and decorators. The reused strict-property-initialization
fixture retains all original branch/return/#/ambient/static/computed source cases,
and malformed auto-accessor modifiers retain parser recovery. Original harness defaults
and directives are encoded in an authored config, without a noEmit/library override;
the config metadata difference is explicit. Error/type/symbol baseline bytes are
source-hashed, but their full baseline traversal/emit assertions are not claimed.
[All 21 Go observations](../../artifacts/declarations-validation/original-compiler-inputs21/report.json)
completed, zero process failures. Snapshot2 is 1/21 exact; remaining actual failures
are guarded class/enum collision, class name use, or TypeQuery consumers. The
[complete 65-case managed replay](../../artifacts/declarations-validation/build2/paired/report.json)
records four exact outputs and 61 explicit failures; all three vertical cases are
currently blocked by the same library tuple failure. No final acceptance is claimed.

## Source responsibilities and first dependency

Pinned [NewChecker/initializeChecker](../../upstream/TypeScript/tsc/internal/checker/checker.go)
(lines 913/1309) bind the actual Program, consume its options/files, establish real
global types, and merge global declarations, ambient modules and augmentations in a
specific order. Strict options use their source fallback rules. Missing or wrong-arity
globals produce real diagnostics. Declaration-only Programs are a valid source domain.
Current [CreateForProgram/CreateCore](../../src/TypeScript.Scanner/Checker/Checker.cs)
and [Program.CreateActualChecker](../../src/TypeScript.Scanner/Compiler/Program.cs)
still impose authored profiles, direct strict-field interpretation, nonempty user-source
requirements and the exact three-library declaration environment. Those are concrete
initialization/admission gaps; simply admitting class nodes cannot close them.

After genuine initialization, follow the declared class/interface and base-type producers
(checker.go lines 17706/19624/19902), constructor/accessor/class checks
(2873/2979/4337), enum/module checks (5181/5245), symbol merging (14380/14417),
and enum/accessor type producers (24409/18918). Their demanded
[grammar](../../upstream/TypeScript/tsc/internal/checker/grammarchecks.go),
[relations](../../upstream/TypeScript/tsc/internal/checker/relater.go) and
[flow](../../upstream/TypeScript/tsc/internal/checker/flow.go) branches include
constructor accessibility, private/protected origin, super/this and definite property
initialization. Diagnostic type/symbol/construct-signature formatting in
[printer](../../upstream/TypeScript/tsc/internal/checker/printer.go),
[nodebuilder](../../upstream/TypeScript/tsc/internal/checker/nodebuilder.go) and
[symbolaccessibility](../../upstream/TypeScript/tsc/internal/checker/symbolaccessibility.go)
is a no-emit consumer dependency, not automatically an emit exclusion.
Class checking also directly calls decorator checks and demands computed/unique-name,
generic-base and static-side relation behavior. D1–D6 explicitly excludes decorator
syntax while selecting the other dependencies; an undecorated demo alone would not
justify either decision.

## Original assertions versus compiler inputs

The tracked pinned checker package has two behavioral tests in
[checker_test.go](../../upstream/TypeScript/tsc/internal/checker/checker_test.go):
`TestGetSymbolAtLocation` constructs a genuine default-library Program and asserts
non-null interface/ambient-variable/property symbols; `TestGetTypeAtLocationOfTypeOnlyImportClause`
retains both non-null and actual type-identity assertions for named/namespace/default
import clauses. Preserve their full original options, source, bundled environment and
assertions. The parallel scheduling setup may be adapted explicitly. The separate
[tracer test](../../upstream/TypeScript/tsc/internal/checker/tracer_test.go) is tracing
coverage, not a declaration unit. `BenchmarkNewChecker` is not a correctness body and
is outside this campaign's initial assessment. Existing ignored oracle-access/test
files in the upstream checkout are authored harness material, not pinned originals.

Most declaration obligations reside in compiler inputs and error/type/symbol baselines,
executed by the actual [compiler runner](../../upstream/TypeScript/tsc/internal/testrunner/compiler_runner.go)
and [TestLocal](../../upstream/TypeScript/tsc/internal/testrunner/compiler_runner_test.go).
Early source families include [constructor/super cases](../../upstream/TypeScript/tsc/testdata/tests/cases/conformance/classes/constructorDeclarations/),
[private/protected members](../../upstream/TypeScript/tsc/testdata/tests/cases/conformance/types/members/),
[accessors](../../upstream/TypeScript/tsc/testdata/tests/cases/conformance/classes/propertyMemberDeclarations/memberAccessorDeclarations/),
[strict initialization](../../upstream/TypeScript/tsc/testdata/tests/cases/conformance/classes/propertyMemberDeclarations/strictPropertyInitialization.ts),
[enum merging/errors](../../upstream/TypeScript/tsc/testdata/tests/cases/conformance/enums/enumMergingErrors.ts),
[namespace/class/enum merges](../../upstream/TypeScript/tsc/testdata/tests/cases/conformance/internalModules/DeclarationMerging/)
and [cross-file ambient/global/module augmentation](../../upstream/TypeScript/tsc/testdata/tests/cases/conformance/interfaces/declarationMerging/mergedInterfaceAugmentationDiagnostics.ts).
These are candidate source families, not an executed or exhaustive corpus. Preserve
fixture bytes, original harness file/root extraction, option variants and original
baseline hashes. Separating emit output never permits dropping declaration-related
checker errors from a fixture that also requests declaration emit. A no-emit adaptation
must be explicit and identically applied on both runtimes; it is not a full original
emit baseline pass.

Source provenance anchors: checker_test.go SHA256
`b4bd82d0cfb311742e7092e33b367f18c72d0100ccd15377f694646d70ece50b`;
compiler/contentmapper_test.go SHA256
`630f10ec32eb34f500aad399f6b7a54122a11921c1a23ee72e51acb982a8f334`.

## Behavioral observer and execution proposal

Reuse the [Program observer](../../tools/program-oracle/main.go) and its static managed
counterpart with a new explicit semantic request, leaving accepted Program requests
unchanged. Construct unmodified upstream NewProgram with actual parsed options and
Bundled.WrapFS; no profile override, injected three-lib roots, declaration simplification
or fake globals. The older [semantic project observer](../../tools/resolution-cli-oracle/main.go)
illustrates diagnostic projection, but its explicit fixed checking libraries are not
the new campaign environment. Coordinate the source checker.Program/Host contract
with core; the current [bounded host interface](../../src/TypeScript.Scanner/Compiler/CheckerHost.cs)
does not represent all demanded source methods.

Compare config/Program/syntax/bind/global/semantic diagnostic categories separately,
including complete code/text/arguments/chains/related information, byte locations and
actual owner files. Use actual Program.GetGlobalDiagnostics and bulk/per-file semantic
calls, retaining noEmit, noCheck, skipLibCheck/default-lib, preceding directives and
mapped-directive policy. Zero diagnostics is meaningful only after genuine checking;
unsupported acquisition is a distinct failure. Finite public type/symbol, base-member
and construct-signature queries at source-marked nodes supplement diagnostics where
they expose a concrete inference/visibility/merge risk. Preserve original identity
assertions, without adding a private cache/storage census.

Run original direct bodies and cheap managed consumers first, then substantive positive/
negative original-Go comparisons, then one focused actual Linux x64 AOT execution of
the same shared/query bodies. The final frozen gate runs new managed/native/xUnit/CLI
evidence before one retained Program cascade. Preserve prior proof/corpus bytes.

## Proposed early cross-file demonstration

Select a small warehouse stock model: an abstract base stock item with private identity,
protected quantity, constructor parameter properties and a public accessor; an exported
derived Product overrides its price method and calls super; an enum models stock state;
a merged Product namespace provides a factory. Two ambient declaration files merge an
AuditRecord interface, consumed by a Warehouse class in another module. A separate main
module constructs and uses the model through base and derived types. Use a genuine
strict ES2022 configuration and unchanged pinned `lib.es2022` closure, with no runtime
execution required. Keep advanced JSX/generator/decorator syntax out of this first demo.

The negative counterpart preserves the shared model and changes the consumer: wrong
constructor argument, inaccessible private/protected access, wrong accessor assignment
and an incompatible AuditRecord implementation. Add invalid override/missing super and
private-origin incompatibility as focused source-family controls rather than crowding
the demo. Exact diagnostics and type displays come only from pinned Go; no guessed
diagnostic codes or target-derived expected values. The model and invalid consumer
are now materialized; additional source-family controls are in progress. Original
reference agreement is not completed managed coverage.

## Existing mapper semantic debts

The [original mapper tests](../../upstream/TypeScript/tsc/internal/compiler/contentmapper_test.go)
`TestCompositeProjectContentMapperSupplementalRoots`, `TestContentMapperInvalidMappings`
and `TestContentMapperSourceFileState` must regain their original
collectContentMapperDiagnostics semantic call when the genuine environment supports it.
Their accepted construction-channel assertions are retained foundation, not proof of
that publication path. The helper now invokes the original syntactic/semantic/Program
concat channels with all fixtures/assertions unchanged: the three targeted Go bodies
and all three shared snapshot2 bodies passed. [New restoration provenance](../../validation/Declarations.Checks/mapper-semantic-restoration-provenance.json)
separates this execution from the immutable historical Program metadata; native/xUnit
qualification remains pending. `TestContentMapperDirectivesPreserveIncrementalGlobals` additionally
requires original bulk-global and incremental diagnostic assertions over unchanged
generator/yield source and missing IterableIterator behavior. That remaining generator
algorithm debt must stay explicit unless the new contract selects its closure; do not
replace source, libraries, diagnostics or the expected global count to make it pass.

## Current ordinary checking environment migration

The genuine ordinary checking path now consumes the actual parsed options and bundled
Program environment. [The shared checking base](../../examples/configuration/checking-base.json)
explicitly selects `noLib:false` and `lib:["es5"]`; all TS example bytes are unchanged.
Its earlier `noLib:true` controls relied on an authored host injecting three libraries.
[Migration provenance](../../validation/declarations-corpus/checking-environment-migration.json)
preserves the complete old base bytes/hash and identifies unchanged historical
corpora/proofs. New current controls will establish complete source diagnostics and
exits; the old authored capability-rejection observations are explicitly retired or
replaced, rather than silently skipped. The authentic reference already gives valid
exit0 and invalid exit1 with the same eight complete source diagnostics.

The existing authored Program acquisition control has similarly replaced declaration-only
and later-library rejection with actual symbol/global behavior. Snapshot2 passes both
its scheduling and acquisition bodies. A focused signed `range(-1/0/3)` runtime
assertion is added to that existing authored group for the concrete tuple failure;
it is not counted as an upstream checker original body.

Snapshot3 closes the concrete signed-range failure: the expanded authored acquisition
group, both original checker bodies, restored mapper3 and the migrated disk integration
group all pass. [Current complete replay](../../artifacts/declarations-validation/build3/paired/report.json)
is 9/65 exact; [triage](../../artifacts/declarations-validation/build3/paired/triage.json)
retains all 56 explicit failures. Actual library tagged-documentation checking now
blocks 40 cases. Remaining compiler inputs expose constructor overload dictionary
zero-value behavior, class initialization, identifier visitation, nonpublic mixin
properties, source new expressions, nonscalar enum unions, assertions and private
reference accounting. These are source implementation gaps, not adjusted expectations.

The [current CLI generator](../../tools/declarations-generate-cli-requests.py) preserves
14 explicit controls: four shipped-demo inputs now use the real ES5 configuration;
one authored obsolete reference/profile rejection is replaced by genuine declaration-only
checking; nine other inputs remain byte-equivalent. The complete previous requests and
expected streams stay in their historical corpus. The checking producer is now the
same actual bundled Program/parsed options, including bind diagnostics, with no injected
roots. Expected source outputs and native execution remain pending publication.

Current ordinary migrated controls are independently reviewed and all green on immutable3:
[Configuration14](../../artifacts/declarations-validation/build3/current-configuration-cli/report.json)
is 13 complete genuine Go diagnostic/exit comparisons plus one unchanged raw-path host
boundary; [Resolution11](../../artifacts/declarations-validation/build3/current-resolution-cli-replacements/report.json)
is nine genuine source comparisons plus two exact external JSON checking failures.
The latter replace obsolete fixed-profile option-ban assertions using identical graph
fixtures; unsupported results do not count as Go semantic parity. Original graph fields,
sequences and resolution assertions remain unchanged.

Retained gates use only the explicit `DECLARATIONS_CURRENT_CHECKING=1` declaration
context. Fixed shadow manifests reject all changes except the reviewed CLI block;
canonical319/216 corpus inputs/results and original assertion registries remain identical.
Current shipped demo bytes are checked for the new CLI controls, while canonical
interpretation-only demo rows retain their captured old base bytes, verified against
the migration record. Evidence records the environment and exact retired/replacement
controls. No acceptance gate has run: selected source failures remain open.

## Snapshot5 representative checkpoint

All six current shared groups pass on the immutable fifth snapshot: [original checker2](../../artifacts/declarations-validation/build5/focused/checker-checks/report.json), [restored mapper semantic3](../../artifacts/declarations-validation/build5/focused/mapper-semantic-checks/report.json), and [authored external consumer1](../../artifacts/declarations-validation/build5/focused/external-semantic-boundary-checks/report.json). The latter exercises actual checked JS, owned direct JS queries, JSX and decorator consumers; it does not claim Go parity for unsupported semantics. Three separate unchanged-option [natural JS skip-policy records](../../artifacts/declarations-validation/build4/representative/report.json) already match complete source results for noCheck, @ts-nocheck and checkJs:false.

Current genuine Go references contain 68 records: 44 authored source-family inputs, three vertical witnesses, and 21 reused compiler-input variants. The appended three skip records preserve the original first41 request bytes. The last complete replay remains the historical snapshot3 65-record run; there has been no complete68 target replay yet. Native and xUnit acceptance remain pending.

The [snapshot5 declaration representative](../../artifacts/declarations-validation/build5/representative/report.json) matches all four complete original records for derived default constructors, mixin access modifiers and instance/static private helper diagnostics. The [real-library representative](../../artifacts/declarations-validation/build5/focused/library-report.json) progresses past the previous ThisType dispatch failure but encounters an obsolete intrinsic type-alias throw. Original checkTypeAliasDeclaration recognizes compiler intrinsic leaf declarations and returns without child checking; operational string mapping has a separate producer. Library bytes, options and diagnostics are unchanged. The full68 replay is held while this common producer blocker is repaired, rather than repeatedly recording the same exception across families.

## Reproducible wiring and snapshot8 checkpoint

[The declaration gate](../../tools/run-declarations-gate.py), [shell](../../tools/verify-declarations.sh), [recorder](../../tools/record-declarations-evidence.py), [manifest](../../validation/declarations-corpus/manifest.json) and [reproduction guide](../DECLARATIONS-VERIFY.md) are written and preflight passes. Corpus/shared xUnit consumers compile against the immutable snapshot6 without a production rebuild. The current inventory is 70 source records and six shared groups, yielding 76 new xUnit cases. Support's finite tool/provenance review identified no blocker. This is wiring evidence, not full semantic/native acceptance.

Two ambient declaration dependency witnesses extend the first44 unchanged inputs: a typed rest-tuple member signature and invalid ambient binding grammar. The negative is enriched with inner rest-last/trailing-comma assertions before any freeze, with the initial46 request/output artifacts preserved. [Current source references](../../artifacts/declarations-validation/original-families46-binding-grammar/report.json) establish complete original records, including positive public next-member type and negative codes1014/2371/2462/1013. No generator/runtime binding-body parity is claimed.

The [snapshot8 real-library representative](../../artifacts/declarations-validation/build8/focused/library-report.json) progresses beyond intrinsic leaf declarations and synchronous rest-binding/iterator helpers. It now fails at an obsolete checkIdentifier object-value guard reached through a library typeof conditional-type annotation. No request, option or library bytes are changed to bypass it. Complete70 managed and native acceptance remain pending. The final runner requires exact disk warehouse exit0/1 and full diagnostics against genuine Go, affected managed consumer suites before AOT, then exactly one retained Program cascade with the explicitly reviewed current CLI context.

## Snapshot11 source and observer checkpoint

The [complete snapshot10 replay](../../artifacts/declarations-validation/build10/paired/report.json) is 63/70 exact, with seven explicit failures preserved. After source repairs, the [snapshot11 focused packet](../../artifacts/declarations-validation/build11/focused/report.json) is six of eight exact: the genuine ES2022 environment and full default-library interface record, both public property-display cases, and the semicolon/private-unused compiler input now match complete Go records. All six shared assertion groups pass. Two source causes remain: mixin override iteration and printing an ambient binding-pattern parameter. Full acceptance and Native AOT remain pending; another full70 replay waits for this deduplicated packet.

The prior property observer used raw escaped storage names, which expose unrelated compiler allocation IDs for private and unique-symbol members. The [explicit observation correction](../../validation/declarations-corpus/semantic-observation-provenance.json) calls actual public SymbolToString on the same returned property in both runtimes, with a distinct displayNameBase64 field. Property ordering and each property/type association, all semantic diagnostics and other query records stay exact. Display names do not prove arbitrary cross-property identity. No text normalization or production ID adjustment occurs. [Previous observer/goldens](../../artifacts/declarations-validation/observer-property-display-correction/before/files.json) and complete raw mismatches remain preserved; [fresh pinned-Go references](../../artifacts/declarations-validation/original-property-display/families/report.json) provide the current expectations. Vertical and compiler-input output bytes are unchanged.

## Frozen candidate12

[Immutable12 complete replay](../../artifacts/declarations-validation/build12/paired/report.json) matches all70 complete genuine Go records: 46 authored source-family observations, three vertical witnesses and21 reused compiler-input variants. [All six shared assertion groups](../../artifacts/declarations-validation/build12/shared/report.json) pass, including two full original checker bodies, three restored original mapper semantic channels and one authored external-consumer boundary. The prior two source causes close without request/options/library changes. [Disk warehouse checking](../../artifacts/declarations-validation/build11/warehouse-cli/report.json) already matches complete genuine source diagnostics and exits0/1 on the coherent prior source; final native ordinary checking repeats it on frozen current source.

The coordinated final gate is authorized after finite independent source/tool/observation review. [Twelve negative validators](../../artifacts/declarations-validation/gate-review/negative-validators.json) all reject invalid counts/hashes/context/CLI/original/display provenance. Contract/guide, production, tests, observers, corpus and fixtures freeze for affected managed suites before Native AOT, then one retained Program cascade with explicit current checking context. No successful Native AOT/full retained declaration acceptance is claimed before that run finishes.

## Preserved gate1 and focused13 correction

[Gate1](../../artifacts/declarations-validation/gate-attempt1/final-gate.json) stops before Native AOT at the cheap retained Checker suite: 829/832 pass, all five source/asset/historical/upstream/obligation drift checks are false. The new76 xUnit cases, complete70 managed records, shared6, order and genuine disk warehouse CLI are green. Three failures are retained separately: two authored old globalThis capability bans and the named legacy Project foreign-node exception category. The foreign file was correctly rejected; the source13 repair preserves its previously asserted ArgumentException category without changing membership or genuine/scalar rejection behavior.

The [same-input source-positive replacement](../../artifacts/declarations-validation/original-globalthis-retirement/report.json) appends exactly one record, preserving all first46 request/output bytes. Genuine noLib ES2022 Program checking retains ten missing-global2318 diagnostics and complete public x/globalThis type/symbol results; there is no library/global injection. The [immutable13 positive](../../artifacts/declarations-validation/build13/globalthis-positive/report.json) matches its entire original record. The [focused legacy/scalar ownership/API checks](../../artifacts/declarations-validation/build13/retained-boundary-focus/report.json) pass with all14 current assertions, including retained foreign/orphan/cycle and actual symbol/type cache behavior. Named legacy eight-global representation is explicitly separate from the genuine Program ten-global result.

[Retirement provenance](../../validation/declarations-corpus/authored-control-retirement-provenance.json) preserves historical negative JSON/proofs and archived earlier adapter bytes. Only the exact obsolete current negative is retired, replaced with the genuine positive running in all new channels. The actual downstream native data-boundary tool selects this same retirement and exact14 current API names only in the reviewed declaration context; default historical behavior and all other controls remain unchanged. Counts are now71 source records (47+3+21), six shared groups and77 new xUnit cases. No native/full retained acceptance is claimed until the next frozen complete run succeeds.

## Preserved gate2 harness correction

[Gate2](../../artifacts/declarations-validation/gate-attempt2/final-gate.json) stops before native publish after the new77 xUnit cases,71 complete managed records,shared6,order and warehouse CLI pass. Binder163,Checker831 and Scanner3838 also pass. All five drift checks are false. The next invocation names a nonexistent Projects.Tests project: this is an authored runner error, not a producer regression. [The archived diagnosis](../../artifacts/declarations-validation/gate-attempt2/diagnosis.json) and pre-repair scripts/guide retain that evidence.

The corrected gate removes only that duplicate stage/TRX expectation. Actual project assertions remain in Checker.Tests and the retained Projects native cascade. Preflight now validates every fixed test/publish project path across the complete cascade. No production code, original assertion, request, expectation or historical proof changes. Native and complete retained acceptance remain pending the corrected full run.

## Qualified final acceptance

[The complete frozen gate](../../artifacts/declarations-validation/final-gate.json) passes on source13 in1163.022 seconds, with all five drift checks false. [Durable proof](../../validation/results/declarations-verified.json) records five unchanged pinned original assertion bodies,77 new xUnit cases,71 complete semantic records in both managed and actual Linux x64 Native AOT, six shared groups,26 exact executions per runtime/order stage, and native warehouse checking with complete source diagnostics and exits0/1. Its17 physical native paths are ELF64 x86-64 executables without a CoreCLR dependency. One complete retained Program cascade passes under the reviewed current checking context. No further runtime was performed to qualify these saved results.

The final record binds exact source/observer/request/expectation/library/executable hashes. Independent saved-result review confirms full record comparison, actual source filenames/spans/chains/related diagnostics, public display-name association, nil/empty and repeated-result behavior selected by these inputs. The71 records comprise47 authored source-family requests, three vertical inputs and21 compiler-input variants; they are separate from the five original test bodies and do not establish every checker responsibility. Explicit checked JS/JSX/decorator/external body semantics and the generator mapper global/incremental debt remain outside this contract. Three mapper semantic diagnostic channels now execute their original calls and unchanged expectations.

The24674 figure is the sum of selected suite TRX case executions counted once per suite, not a distinct test inventory or a sum of every repeated invocation. Cheap affected Binder163/Checker831/Scanner3838/configuration-integration1 executions are recorded separately; retained scripts repeat some suites. Historical corpora/proofs remain unchanged, while the reviewed authored hidden-library, mixed-JSON and globalThis control migrations are explicit in the final proof. Public property display replaces incidental escaped allocation keys without implying arbitrary property identity.

The [owned test catalog](../declarations-tests.json) now links final runtime evidence only. Its original assertions/applicability and reviewed obligations are unchanged; source and frozen verification-asset fingerprints still match the proof after that update. Acceptance is recommended for D1–D6, with no known blocker within the selected contract.
