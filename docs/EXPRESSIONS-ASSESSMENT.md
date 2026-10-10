# TypeScript expressions, statements and checker-flow assessment

**Accepted E1–E6 contract; actual Linux x64 Native AOT and retained gates passed.** Baseline `5efa4e5`,
unchanged upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`. The
[contract](EXPRESSIONS-CONTRACT.md) defines E1–E6, and the
[checkpoint](EXPRESSIONS-STATUS.md) records delivery state. The [durable proof](../validation/results/expressions-verified.json)
binds the accepted source, verification assets, binaries and results. The delivery
commit contains this assessment; this is not whole-checker completion.

## Responsibility closure

| Domain | Implemented and independently reviewed source responsibilities |
| --- | --- |
| E1 runtime binding | Object/array/nested/default/rest bindings, contextual parameters, pattern inference, assignment targets and recursive assignment validation; demanded unused diagnostics and real iterable/tuple helpers. |
| E2 composite/optional expressions | Object/array spread/rest merging and diagnostics, computed/shorthand/method properties, optional property/element/call chains and signature/return markers, contextual/callable interactions. |
| E3 expressions | Bigint/regex/template/tagged/omitted expressions, non-null/satisfies/assertions, delete/in/instanceof/private-brand/operators/comma/meta-properties, selected public type/symbol query and deferred-checking callers. |
| E4 statements | Synchronous loops, for-in/for-of patterns, switch/fallthrough, labels/jumps, try/catch/finally/throw, return/if/block/with/debugger/empty handling, genuine option and recovery diagnostics. |
| E5 checker flow | Switch discriminant/typeof/true/exhaustive/bypass analysis, property-presence/private-in/instanceof/optional narrowing, binding/default/destructuring/delete assignment transfers, predicate/assertion effects and the retained source loop/capture/cache/reentrancy algorithms. |
| E6 integration | Source-derived comparisons, inferred/narrowed observations, unchanged libraries and ordinary disk example; actual Linux x64 native, retained regression and independent review gates passed. |

The changes stay in their original Go-to-C# counterparts. Whole Checker, Flow and
supporting files retain explicit external responsibilities; selected synchronous
closure must not be read as whole-checker completion. See [source mapping](../source-map.json)
and independent [core](progress/expressions-core.md)/[support](progress/expressions-support.md)
notes. Existing declaration/Program/foundation algorithms are reused rather than
re-audited or replaced.

## Genuine use and boundaries

The [order-fulfillment project](../examples/expressions/README.md) combines relative
modules, discriminated data types, generic collections/callbacks, predicates, runtime
destructuring/defaults/rest, spread updates, optional calls/access, labeled iteration,
switch exhaustion and try/catch/finally. Its actual strict ES2022/ESNext/Bundler
configuration leaves skipLibCheck false and uses unchanged upstream libraries. Go and
managed and actual Linux x64 native ordinary CLI checking agree: valid exits 0 without errors; the invalid project
exits 1 with five complete diagnostics (2322, 2339, 2462, 2698, 2322). Reproduction is in [the guide](EXPRESSIONS-VERIFY.md).

Implemented synchronous behavior can be used with the existing accepted declaration,
callable, data-type and module capabilities. This does not imply support for ordinary
applications using every TypeScript feature or library API. Async/await, generators,
for-await, dynamic import and disposal protocols remain external; general checked
JS/JSDoc/JSX/decorators, broad module interoperability and remaining advanced-type
operations likewise remain unfinished. Emit/build/watch/services and enabled tracing
are outside the no-emit semantic campaign. Unsupported consumers are distinct from
source diagnostics and successful checks; library bytes/options are not altered to
hide those boundaries. Native acceptance is Linux x64 only.

## Evidence and its limits

The settled candidate corpus has 85 complete pinned-Go records: 59 authored source-family
cases, two disk/environment witnesses and 24 variants from 20 upstream compiler inputs.
Among the 24 fixtures inventoried for this campaign, 20 are reused and four excluded for checked JavaScript,
async iteration, dynamic import or disposal. These are selected fixtures, not an
exhaustive inventory of the compiler corpus. Reuse preserves source/options and
provenance; it does not execute emit baselines or full upstream type/symbol traversals.

Two original checker API assertion bodies and three original mapper bodies are retained,
not newly ported. Their five shared groups plus one authored external-consumer group
bring the new xUnit inventory to 91 cases. The upstream checker benchmark and enabled
tracer test are separately classified, with no correctness-pass claim from this work.
See [test dispositions](expressions-tests.json) for exact assertions and provenance.

New observations include complete config/Program/syntax/bind/global/semantic diagnostics,
locations/related information, bulk/per-file/repeated publication and selected public
inferred/narrowed type/symbol queries. They cover branch/loop/assignment effects and
invalid inputs, not just successful parsing or binder graphs. They do not constitute
full AST-node query traversal, all compiler options or exhaustive correctness. There
is no new incremental-update/lifecycle sequence in this corpus; earlier Program
lifecycle evidence remains in retained regressions. No new private-state instrumentation
or weakened diagnostic comparison was introduced. Existing public property display
and identity limitations remain as documented in the declaration campaign.

Final managed and actual Linux x64 native verification pass all 85 complete records,
six shared groups, 26 order/repeat executions per runtime and both ordinary CLI cases.
All 91 new xUnit cases and retained gates pass. The selected-suite TRX aggregate is
24,765 passed case executions, zero failures/skips; it is not a distinct-test inventory
or the total of every repeated execution. All five drift checks are clear and 242
historical fixture/proof files are unchanged. The proof records 19 inspected native
executable paths across the new and retained gates, without a CoreCLR dependency.
Independent source, assertion, tooling and acceptance reviews have no unresolved
substantive in-contract finding. The first failed gate and its repairs remain
explicitly recorded below.

## Adaptations and corrected integration gaps

Go map zero-value reads use the corresponding C# missing-key behavior rather than
throwing dictionary indexers. This differs from the existing null-safe SymbolTable
wrapper and was reviewed for newly added pattern/signature caches. Raw internal Go-byte
strings and public Unicode diagnostic strings retain distinct roles; new diagnostic
arguments were corrected at their actual source boundaries. Genuine Program module
format lookup remains intact, while named historical hostless test adapters use their
captured module option. That is compatibility support, not a substitute semantic path.

The port reuses its scanner for regex syntax/target/flag diagnostics rather than the
.NET regex engine, whose language differs. Required template evaluation, optional
property display and AST-query/deferred dispatch are restored from upstream. Source
no-op branches and real error recovery are preserved; missing selected consumers do
not silently return success. No optimization or source-file fragmentation was added.

## Observed effort and lessons

Three GPT-6.1 Sol agents were reused, with one Checker writer/build owner, one disjoint
Flow/support writer and independent reviewer, and one verifier. Source packets, immutable
DLLs and targeted failed representatives reduced repeated integration work. Five coherent
snapshots took ten library build invocations (73.23 seconds) and four separate CLI
builds (7.55 seconds) before final acceptance. These are recorded build measurements,
not token/cost/total model-effort estimates or compiler-performance measurements.

First targeted replay was 7/10 exact while later source families were still pending.
The first broad replay was 74/83 exact; nine exceptions reduced to four source causes
and one mixed-JS applicability error. Subsequent targeted replay exposed a missing
lazy tagged-template consumer; surrounding deferred-family review also restored
instanceof/cancellation branches. These failures occurred before frozen acceptance,
and no expectations were weakened to hide them. Review of only a direct producer is
insufficient: selected public-query and deferred consumers must be followed too.

An authored generator quote mistake was corrected before preserving its seven newly
added expectations; the failed packet and unchanged earlier prefix remain recorded.
The mixed controlFlowInstanceof fixture includes checked uglify.js; its full request,
reference and failed observation are preserved and its whole input is classified
external after independent review. No JS erasure or option weakening was used.
Two switch witnesses and one finite public-query witness were added for concrete review
risks, using the existing observer. Copied proof wording was corrected to distinguish
new repeated diagnostics from retained incremental lifecycle evidence.

The first frozen gate stopped before native publication after 165.99 seconds, all
five drift checks clear. New 91 xUnit cases/current 85 records and Binder 163 passed;
Checker passed 814/831. Thirteen failures arose from loss of an existing passive
Collections observer hook. Restoring that named-adapter-only bookkeeping is a narrow
compatibility repair, not evidence that compiler semantics require private-state equality.
Four public-symbol differences were a genuine expression-versus-type/namespace dispatch
regression, repaired by restoring the original predicate nesting. Expectations remain
unchanged. All 17 failed complete records and all 136 affected DataTypes/Collections
records match after the repairs. Corrected final acceptance then passed. The user reinforced
that private observations are diagnostic; no exhaustive cache/identity work was added.

The one retained cascade still invokes nested historical scripts that repeat some
builds and suites. This is observed verification overhead, not additional distinct
coverage. The current run is preserved; a future orchestration change should remove
proven duplicate executions only when touching that active bottleneck, while retaining
the same behavioral gates. No broad cleanup or new process framework is required.

The successful frozen gate took 1,398.790 seconds (23 minutes 19 seconds), including
its retained cascade. That is verification elapsed time, not compiler performance
or total implementation effort. No new package installation or user scope decision
was required. Unavailable token/cost telemetry is not estimated.

## Next dependency-aware campaign

Complete remaining type-system responsibilities next: operational intrinsic string
mapping, key remapping and remaining template/type construction, inference/relations/
constraints, generic variadic/rest closure and assertion-predicate gaps. Some of these
algorithms already operate; distinguish source omissions from incomplete combination
coverage. This would make the current synchronous body checker useful across a wider
range of real library types before async/generator/protocol and JS/JSX/module-option
campaigns. Establish the next contract from the actual source, not an allowed-syntax list.

The [whole-checker roadmap](CHECKER-COMPLETION-ROADMAP.md) retains its no-emit completion
gate. Large-project correctness and performance comparisons remain deferred until
that checker/infrastructure gate passes.
