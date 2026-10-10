# Declaration/object model: core progress

Final delivery status: D1–D6 accepted after snapshot 13 and the successful frozen
Linux x64 Native AOT/retained gate. See [assessment](../DECLARATIONS-ASSESSMENT.md)
and [proof](../../validation/results/declarations-verified.json). Pending labels
below describe historical implementation checkpoints, not the final acceptance.

Active under [D1–D6](../DECLARATIONS-CONTRACT.md), baseline `e7b81a2`, pinned
upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Core alone edits the live
[Checker](../../src/TypeScript.Scanner/Checker/Checker.cs),
[Types](../../src/TypeScript.Scanner/Checker/Types.cs),
[Links](../../src/TypeScript.Scanner/Checker/Links.cs),
[Flow](../../src/TypeScript.Scanner/Checker/Flow.cs), and Program acquisition.
Support owns grammar/relation/display and stages shared-checker method packets.
Verifier owns original assertions and genuine Program observations. Root owns
contract, source-map and acceptance. No final completeness or native claim.

## Current source families

| Source family | Implementation and remaining closure |
| --- | --- |
| NewChecker / initializeChecker, effective options, intrinsic/global state | Genuine entry binds actual host/files and calls a profile-free common initializer. Named historical adapters provide fixture options before the same initializer. Ordered script/global augmentation, builtin, ambient/pattern and nonglobal augmentation phases are restored. Source serialization diagnostic guards and erasable syntax option are restored. Broad lazy consumers remain subject to their real implementation status. |
| Diagnostic lifecycle, checkSourceFile, unused registration/checks | Actual cancellation context, sticky canceled state, separate type/unused phases and suggestion collection; support's 21 source unused methods integrated. Program incremental collector uses actual checker diagnostics/deferred globals. Empty pooled globals preserve source slices.Concat nil behavior. |
| Class / enum / accessor / computed models | Class instance/static/this/base/construct signatures, mixins, enum evaluation/literals/relation state, accessors, private lexical lookup, late-bound members and computed names staged. Support's 45 source declaration checks and demanded grammar/relation/display producers are integrated. Further callers and previously authored guards are being closed against actual inputs. |
| Constructor/static flow and visibility | Source constructor/static return flow and post-super algorithms, property inference, private/protected/abstract accessibility staged. Main # access and super-call source branches are staged after snapshot 2. General excluded body families remain explicit. |
| Module/augmentation targets and helper diagnostics | Source external-module lookup/target/mode diagnostic logic replaces old relative-only checker branches. Imported tslib helper lookup/export/arity diagnostics included for selected class/private semantics; no emission. TS library @param matching source consumer restored without changing library bytes/options. |
| Program/ordinary integration | Actual NewChecker/pool synchronization and semantic host methods, public incremental collection and filename roles staged. Ordinary --config uses genuine Program; named --project remains the compatibility adapter. Source consumers of public Project scalar filenames explicitly use FileNameGoBytes. |

## Evidence and current work

Snapshots 1–13 compile with zero warnings/errors. The two complete original
checker bodies pass on actual default bundled libraries/options. Snapshot 5 also
passes all restored mapper semantic groups and the authored external-consumer group;
four previously failing constructor/mixin/tslib records now match complete pinned
Go responses. Snapshot 2's retained Checker TRX is 830 passed / 2 failed / 0 skipped;
both failures were the same foreign-node exception-category adaptation, repaired
and focused-green in 3.

The verifier uses one representative per distinct failure while genuine library
checking has a common blocker. No fixture/library/comment/option substitutions are
used. Snapshot 6 restores intrinsic alias declaration validation exactly: accepted
source names and arities, invalid-intrinsic diagnostics, and the leaf return. This
does not claim operational intrinsic string mapping; that consumer remains explicitly
unsupported pending the actual library replay. Other snapshot-6 source closure includes
internal import-equals/export-assignment diagnostics, actual module-helper conditions,
private/exact-optional assignments, computed getter annotation lookup, actual indexed
access options and static-property diagnostics.

All source lazy initializer factories, sync/async iteration resolver setup, constructor
map storage and intrinsic allocation order are restored. Target/module/resolution modes
are captured once from actual options. The source atomic checker ID is retained; enabled
type/event tracing remains external and explicitly rejects nonnull tracing requests.
The genuine disabled path does not fabricate a tracer. The source object-literal and
assertion producers replace narrowed authored implementations; spread/destructuring
and checked JS remain explicit external consumers.

Independent core review of support's 45 declaration-check methods covers property/
method/accessor/constructor checks; super placement; duplicate static/instance/#
state; class/static/mixin/implements; overrides including null-declaration aggregation;
strict initialization references and flow; merged enums; namespace augmentation and
attributes; and admitted variable/function branches. No new body omission was found.
Their explicit JS/decorator/generator/destructuring/resource-management exclusions
remain external, and passing fixtures do not imply their underlying callers complete.
Ordinary `--config` now uses genuine Program/Bundled checking; `--project` remains
named legacy. Current copied-3 CLI controls are configuration 14/14 and resolution
11/11 under explicitly revised genuine-environment/boundary assertions; historical
algorithm corpus and proof bytes remain unchanged.

Central logs/snapshots are retained under ignored `artifacts/declarations-validation/`.
Library invocation times by packet: 1 = 1.77, 6.63, 7.35, 7.20 (22.95 seconds);
2 = 1.45, 7.50, 1.49, 8.07 (18.51); 3 = 0.75, 3.12, 3.34 (7.21);
4 = 0.58, 3.26 (3.84); 5 = 2.94, 3.38 (6.32);
6 = 2.93, 3.22 (6.15); 7 = 3.11, 3.29 (6.40); 8 = 3.30.
9 = 3.26; 10 = 3.19; 11 = 3.81; 12 = 3.26; 13 = 3.38. Twenty-five library invocations total 91.58 seconds.
CLI incremental builds are 0.79 seconds in 4, 0.72 in 8, 0.70 in 11, 0.68 in 12 and 0.73 in 13, recorded separately from these
library invocations. Consumer builds/tests are separate. Prefer normal incremental
references after API changes to avoid stale retained adapters.

Independent review of support's bounded relation changes confirms source import-related
retry conditions/related-information order, NoCheck and const-assertion elaboration,
and accessor/computed object diagnostics. The explicit spread/JSX exclusions remain.

D1 setup has been compared with the pinned constructor/initializer: captured effective
options, intrinsic allocation order, lazy factory arities/error policies, sync/async
resolver tables, global merge phases, and disabled tracing are source-reviewed.
D2–D4 class/constructor/private/enum/namespace model and validation families are
implemented and under independent branch review; representative constructor/mixin/
merge/unique-symbol records pass, while aggregate semantic closure remains pending.
D5 grammar/relation/display/unused families are staged and source-reviewed in bounded
packets; genuine bundled-library checking still exposes demanded dependencies before
an aggregate verdict. D6 genuine Program CLI migration has current managed controls;
NativeAOT and final retained acceptance have not run for this campaign.

Snapshot 6 intrinsic declaration checks clear the common library blocker. The next
actual declaration is `next(...[value]: [] | [TNext])` in the bundled iterator libraries.
Root approved its source binding-element/signature dependency and the synchronous
protocol-type helper family as moved D5 scope: checkVariableLikeDeclaration's real
binding/pattern descent and ambient/type-node return; source element typing/synthetic
references; iterable/iterator method signatures; next/return/throw and done/value result
extraction; original iteration diagnostics/relations. Existing tuple/index/resolver/cache
families are reused. Runtime body destructuring, unannotated pattern inference, object
rest/default initializer flow, generator statements and async iterator execution remain
explicit external consumers. There is no tuple-only shortcut or library option change.
This family compiles and passes the genuine ES2022 library replay in snapshot 10.
Independent support review confirms signature boundary, helper control flow and buffered
iterator diagnostic publication. Original NoCheck module lookup policy and project-reference
synthetic-default conditions are also restored after removal of stale fixed-host assumptions.

CallableBoundaryTests and ObjectOracleTests against snapshot 6 passed all 90 tests,
zero skipped. Final semantic/native acceptance remains pending; passing these witnesses
does not claim every declaration function individually executed or a whole checker.

Focused obsolete-assumption scan of Checker/Relater found three genuine Program
consumer debts in the module family: NoCheck module-error suppression, synthetic
default decisions using project-reference source/output redirects, and NoCheck missing
module-member diagnostics. The first two are in snapshot 8; all three are now restored. Remaining profile fields/callers are named historical
fixture capability/ownership adapters, and the unchecked-JS false arm is an explicit
external consumer rather than an options override. No further fixed-host branch was
found in this focused scan.

Snapshot 10 closes the demanded identifier/type-query family: original linked-reference
marking and contextual-binding recursion rule; actual globalThis value/module type;
generic T[K] constraint-position exception; declaration binding correlation with source
circularity/guard-reset/reentrant-flow rules. Independent source review covers both the
producer and adjacent query/narrowing helpers. Implementation destructuring correlation
remains explicit at its consumer. No library/comment/options changes or operational
string-mapping expansion. Snapshot 8 CLI incremental build was 0.72 seconds.

Snapshot 10's full differential packet is 63/70 exact. Snapshot 11 then clears the
actual default 91-library environment through the source template-node producer,
private-unused semicolon dispatch, and public property display through actual
SymbolToString rather than incidental internal allocation IDs. Original/shared six
groups pass. Two remaining focused causes are override validation's outer nil
property-list range and binding-parameter printing; the exact nil-range repair is
included in clean snapshot 12, with the support-owned recursive binding printer
independently reviewed against the source name/element/list algorithms. Snapshot 12
passes the complete 70-record packet: 46 authored family records, three genuine
environments/demonstration records and 21 upstream compiler-input variants. All six
shared assertion groups pass. Disjoint finite source reviews are closed for the
selected initializer/model/validation/flow/relation/display responsibilities, with
the listed external consumers retained. Source is stable for the single final gate;
NativeAOT and complete retained acceptance are still pending.
These runtime witnesses are distinct from source-family review and do not claim
every function exercised. The verifier owns one cheap full retained managed stage
before NativeAOT; core will not duplicate Checker832 on the identical candidate.

Independent assertion review of CheckerOriginalChecks against pinned checker_test.go
confirms both full bodies: unchanged fixture/config bytes, case-insensitive bundled
host with real default options, all three symbol non-null checks, both named/namespace
type-only import identities, and default-import/reference identity. Sequential execution
and try/finally checker release adapt t.Parallel/defer; no library/profile reduction.
The upstream benchmark is outside this behavioral campaign.

Read-only generic-rest boundary clarification: the resolved-rest capability guard
applies only to named historical Collections/Projects profiles. Genuine acquisition
executes existing source rest-type construction, variadic tuple normalization,
effective signature slicing, spread-argument inference and tuple relations; it does
not return placeholder success or reject every generic-rest form. Whole advanced
generic-rest combinations are not completion-qualified by this campaign. Dependent
unfinished intrinsic string mapping, body binding and other listed consumers remain
explicit failures at their own paths.

Final gate attempt 1 stopped before NativeAOT with three authored retained failures:
two obsolete globalThis-read negatives and the legacy Project foreign-node exception
category (rejection itself remained correct). All five drift checks were false; failed
reports remain archived. The source repair restores only the original named Project
ArgumentException category, preserving genuine/scalar foreignNode feature exceptions
and all ownership assertions. Final acceptance remains pending the corrected candidate.
Snapshot 13 compiles cleanly with that exact boundary repair. Independent core review
of the two authored builtin controls confirms same-input positive symbol/type/cache and
visible-global diagnostic checks; only the obsolete historical negative applicability
is retired, without changing captured corpus bytes. Verifier owns the genuine pinned-Go
positive record and focused ownership/foreign-node replay before the next gate.
