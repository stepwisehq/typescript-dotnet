# Async/generator/runtime protocol support checkpoint

Baseline `bb035b3`; pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
GPT-6.1 Sol support owns existing Flow/GrammarChecks and demanded utility/display
counterparts. Core alone writes Checker/Types and performs central builds; verifier
owns original assertions, public behavioral comparisons and native acceptance. Prior
source inventories, corpora and proofs remain immutable. No new private instrumentation,
independent builds or production fragments are introduced.

The recommended complete source responsibility boundary is Promise/thenable awaited
resolution, async contextual/return checking; generator and async-generator yield,
return and next inference/validation; delegated-yield sent types; synchronous and
asynchronous iterable/iterator method/result resolution, async-from-sync and for-await;
using/await-using initializer and scope validation; actual target/module/source-format
restrictions and no-emit helper diagnostics. Shared accepted type/flow algorithms are
reused. Emit execution, JS/JSX/decorators and service consumers remain separate; a
protocol helper's name does not determine its semantic reachability.

## Saved first support packet

[Flow](../../src/TypeScript.Scanner/Checker/Flow.cs) restores the original
`getExplicitTypeOfSymbol` for-of annotation branch: select `IterationUse.ForAwaitOf`
when the actual statement has an await modifier, otherwise `ForOf`, then use the
existing original iteration consumer. No alternate flow algorithm is added.

[GrammarChecks](../../src/TypeScript.Scanner/Checker/GrammarChecks.cs) restores
`checkGrammarAsyncModifier`, `checkGrammarAwaitOrAwaitUsing`,
`checkGrammarYieldExpression`, and the partial modifier, declaration-list and
for-in/for-of branches. The source branches preserve static-block/parameter-initializer
errors even with parse diagnostics, while contextual/top-level diagnostics retain
source parse-error policy. Top-level Node module formats query actual host metadata;
module/target fallthrough, effective external-module checks and related async-function
suggestions remain original. Using declarations retain for-in, ambient and unblocked
case/default restrictions. C# `goto case/default` expresses Go switch fallthrough;
metadata lookup uses an explicit Go-byte PathKey adapter.

[Utilities](../../src/TypeScript.Scanner/Checker/Utilities.cs) adds exact
`hasAsyncModifier`, `forEachYieldExpression` and `expressionResultIsUnused` source helpers. Yield traversal includes
operands and computed names evaluated in the containing generator, while excluding
nested function bodies and type declarations as upstream. Existing scanner/token spans,
AST flags, diagnostic builders, host metadata and genuine bundled libraries suffice;
no new subsystem pressure was identified in this initial support packet. Unused-result
classification retains original parentheses/comma traversal and expression-statement,
void and for-initializer/incrementer cases; it does not assume every yield result is used.

These source families are saved and diff-clean. Core independently compared the support
source branches without a substantive finding. The first central compiler found only
six support bindings (the actual Ast diagnostic factory and represented ResolutionMode
metadata), now corrected, plus the source unused-result helper now supplied. A clean
coherent compile and runtime qualification are still pending; support performed no build.

## Independent core source review

The first saved core packet was compared with pinned await/thenable/Awaited and cycle
families, actual return/yield/next aggregation and contextual widening, generator
construction/annotation/assignability, contextual await/yield/return, yield delegation,
iteration fast/slow/member/result and async-from-sync fallback, for-await helper targets,
using initializer diagnostic chains, signature checks and return-path policy. Source
lazy global resolver callbacks, negative iteration cache policy, buffered/deferred
diagnostics, diagnostic byte strings and actual target/module options remain intact.
The LanguageFeatureMinimumTarget values and iteration-kind selector match the source.
No substantive omission was found in this saved packet. This is source review, not
runtime acceptance or a whole-checker completion claim. Other mixed-function obligations
remain caller-led; accepted source audits are not repeated.

## Original assertion debt

Verifier owns restoration of pinned compiler
`TestContentMapperDirectivesPreserveIncrementalGlobals`, preserving the same mapped
generator source, ES5 library selection, SkipLibCheck and both Ignore/Expect policies.
The original initial global query must have zero diagnostics. Incremental checking
must publish exactly one fileless missing-global diagnostic for `IterableIterator`;
the file channel must retain actual owner, source `vue`, code 2578, and unused count
zero for Ignore or one for Expect. Construction-only assertions cannot establish this
semantic/delayed-global behavior. Historical prior evidence stays unchanged; current
full-body taxonomy and runtime evidence remain verifier-owned.

Independent restoration review compared the full generator body to the pinned original:
both policies, original options/text/map, initial global query, exact incremental file,
all diagnostic identity/code/source/argument checks and counts are preserved. Only this
body and header changed; the earlier three restored semantic bodies and wrappers are
unchanged. The retained declaration guard accepts only the immutable exact four-file
source version or the fixed hash-bound restoration provenance with the same source set.
The saved ten positive/negative guard controls cover mixed versions, hashes, sets,
classification and function selection. No reviewer execution was performed.

The thin future no-emit inventory was reviewed separately against the original
compiler/conformance enumerator, settings/configuration expansion, unit parser and
skip list. Saved hashes/counts match: 12,824 inputs, 15,190 configurations and 42
pre-configuration skips (ten built-API environments, 32 removed-option inputs).
All remain unexecuted obligations, not passes. Two metadata corrections were handed to
verification: explicitly separate declaration/emit-induced diagnostic and pre/post-emit
invariants as external output-emitter operations, and hash the Python generator that
assigns those dispositions and baseline bindings. Genuine checking, suggestions and
type/symbol obligations remain required; input flags must not be changed to force noEmit.

## Compiler-input handoff

Support exclusively generated the finite [protocol compiler inputs](../../tools/protocols-generate-compiler-inputs.py)
and [request provenance](../../validation/protocols-corpus/requests-compiler-inputs-provenance.json):
46 inventoried whole fixtures, 42 reused TS inputs, four explicit whole-input dispositions,
52 actual directive variants. The selection covers malformed/recursive thenables,
generic/contextual async returns and return locations/options; generator overloads,
ambient/no-yield/noImplicitAny/return-path grammar and generic/delegated yield context;
custom/malformed async iterator methods and sync/async union/intersection lookup order;
using/await-using scopes, ambient/binding/for-in grammar, targets, true missing globals,
disposal helper diagnostics and built-in async iterator disposal. The four dispositions
retain whole JS, decorator, nested dynamic-import and removed-classic-option source
bytes/settings; none is stripped or recorded as a successful comparison.

The established source-reviewed fixture parser is reused. Its 42 selected extractions
were checked against the genuine Go full-inventory unit paths/text hashes/settings and
configuration variants, all exact. File-name directives become actual virtual Files
roles; there is no option-value normalization. Original regular harness defaults and
all compiler directive variants feed authored configs with genuine libraries, without
forcing noEmit or filtering diagnostics. Emit/declaration operations and full original
type/symbol baseline traversal remain separately external/unexecuted; these are reused
inputs, not full original assertion or baseline passes.

The unchanged compiled pinned Go Program oracle produced all 52 complete records in
0.862 seconds, exit zero, exact ordered names and unchanged oracle hash. The deterministic
[expected records](../../validation/protocols-corpus/expected-compiler-inputs.jsonl.gz) and
[reference provenance](../../validation/protocols-corpus/compiler-inputs-reference-provenance.json)
bind requests, output, oracle/source hashes and extraction evidence. Generator --check
and diff-check pass. No managed/native execution or production build was performed by
support; verification owns aggregate integration, independent corpus review and runtime.

## Final tooling review before the frozen gate

The retained Expressions caller now delegates only its duplicate mapper four-file hash
loop to the reviewed Declaration guard. That guard accepts the exact original source
version or the fixed full-generator restoration provenance; immutable earlier provenance
and obligation projections stay unchanged. The saved ten controls exercise both accepted
versions and reject changed sets, hashes, mixed versions, classification and function.

The new protocol shell, gate, CLI runner, corpus/shared adapters and recorder were reviewed
against the settled 94-record manifest and actual saved candidate-one report shapes:
40 authored families, two demos, 52 reused compiler variants, eight shared bodies,
22 order executions per runtime and two ordinary CLI outcomes. The authored families
exercise positive and negative P1–P5 behavior through complete bulk/per-file/incremental/
repeated diagnostics and finite public queries. Their comparison has no diagnostic or
private-state normalization. Support's own compiler selection is reviewed separately by
verification and the coordinator.

The order reader uses actual report fields; CLI proof checks executable hashes, argv,
full responses, expected exits and source bytes. Recorder predecessor nesting matches
the real return shapes and sums selected TRX records once. The shell runs one retained
type-system cascade before publishing and executing the new Linux x64 native targets.
Source/assets/historical/upstream/obligation drift and no-overwrite proof checks remain
required. No substantive tooling or assertion finding remains. The now-saved reproduction
guide matches those counts, execution order and boundaries. No runtime or build was
repeated by this reviewer; final NativeAOT/retained acceptance is still pending.
