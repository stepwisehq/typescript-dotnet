# Remaining TypeScript type-system completion contract

Baseline `09630d6`; pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
The [mandate](TYPESYSTEM-PROMPT.md) authorizes completion of the shared TypeScript
semantic type system left partial by earlier campaigns. The [checkpoint](TYPESYSTEM-STATUS.md)
tracks delivery; the [roadmap](CHECKER-COMPLETION-ROADMAP.md) retains the whole-checker gate.
This contract covers source responsibilities, including existing partial algorithms;
passing selected examples or removing explicit guards does not establish completion.

## Required responsibility closure

| Domain | Pinned responsibilities and observable acceptance |
| --- | --- |
| T1 Generic models, instantiation and constraints | Reconcile and complete type/mapper/signature/object/alias instantiation, substitutions, defaults, constraints and recursive/circular/depth handling, generic parameter validation and explicit variance annotations. Preserve upstream canonicalization, mapper combination and recursion/cache algorithms where they affect checking. Validate both accepted relationships and actual constraint/default/variance/depth diagnostics. |
| T2 Type construction, reduction and advanced operators | Reconcile union/intersection, keyof/index/indexed access, mapped and key-remapped types, modifiers/optionality, conditional/distributive/constrained-infer types, template literals and intrinsic string mappings. Complete missing producers and consumers, not a substitute solver. Include numeric/string/unique-symbol keys, filtering/remapping collisions, recursive aliases and the interactions that these original algorithms permit. |
| T3 Generic inference | Complete the shared synchronous TypeScript responsibilities of inference.go and demanded checker callers: priorities, contextual/callback/return inference, covariant/contravariant candidates, constraints/defaults/NoInfer, template/string inference, reverse mapped inference, generic overload/signature inference, tuple/rest/variadic inference and recursive source/target handling. Preserve source recovery and inference failure diagnostics. |
| T4 Type relationships | Complete shared TypeScript responsibilities in relater.go and demanded helpers: assignability/identity/subtyping, generic variance, structural/signature/predicate comparisons, unions/intersections, mapped/conditional/indexed/template/string types, tuple/rest relations, recursion and diagnostic elaboration. Compare public relationship results as well as complete errors; do not infer relations solely from successful program checking. |
| T5 Callable, tuple and flow consumers | Reconcile generic and variadic tuple construction/normalization, labels/readonly/optional/rest, signature expansion/instantiation/calls, explicit and inferred predicates/assertions and their synchronous narrowing consumers. Complete demanded public type/symbol/display paths and genuine declaration/callable/body interactions. Reuse accepted flow/declaration algorithms; reopen only concrete partial branches or defects exposed by this closure. |
| T6 Shared dependencies and acceptance | Port the missing upstream JavaScript-string casing algorithms and pinned generated data used by intrinsic types, preserving full casing and surrogate semantics. Reconcile source families and applicable tests independently of corpus results. Genuine unchanged Program/library/options, complete diagnostics and inferred/relationship observations on valid/invalid inputs, actual Linux x64 Native AOT, retained regressions, independent omission review, updated source/test inventories, reproducible disk use and committed clean delivery are mandatory. |

Source authorities are pinned [checker.go](../upstream/TypeScript/tsc/internal/checker/checker.go),
[types.go](../upstream/TypeScript/tsc/internal/checker/types.go),
[inference.go](../upstream/TypeScript/tsc/internal/checker/inference.go),
[relater.go](../upstream/TypeScript/tsc/internal/checker/relater.go), demanded grammar/
display/utility counterparts, and [stringutil](../upstream/TypeScript/tsc/internal/stringutil/).
The source-family reconciliation must account for absent functions and partial branches,
not just unsupported exceptions. Preserve one Go file to one discoverable C# counterpart;
record language adaptations, generated material and authored harnesses separately.

Much generic/mapped/conditional/tuple code already executes through genuine Program.
That is implementation to reconcile, not evidence of full coverage. Initial concrete
omissions include the StringMappingType producer/model and its inference/relation/
constraint/display consumers and deferred variance validation. The historical function
catalog and named-profile admission lists are not current completeness authority.
Remaining genuine-path shortcuts must be replaced by actual upstream algorithms;
historical test adapters must be explicitly identified rather than mistaken for
limitations of the ordinary checker.

## External boundaries

Async/await/generator/yield/for-await/disposal protocol execution and JavaScript/JSDoc/
JSX/decorator-specific checking remain later campaigns. Their shared generic/type
algorithms belong here; excluding a caller does not justify omitting shared TypeScript
semantics. Broader module interoperability/import attributes and JSON-specific checking
remain separate consumer responsibilities. Include proportionate demanded dependencies
and report material expansion before undertaking it; never narrow this contract to
make difficult cases pass.

Program loading, resolution, configuration, binding and syntax remain accepted inputs.
No broad repeat completeness audit is required, but fix concrete exposed defects.
Library declarations/options remain genuine and unchanged. No fake declarations,
hidden globals, diagnostic suppression, permissive placeholders or fixture special
cases. Upstream noCheck/skip/check policy remains genuine; unsupported consumers are
explicitly different from valid checking. Emit, watch/build scheduling, services and
enabled tracing remain outside this no-emit campaign. Other native platforms are
unverified. Large-project correctness/performance and .NET upgrade/optimization remain
deferred until checker/infrastructure completion.

## Execution and verification

Astra owns the contract and acceptance; three reused GPT-6.1 Sol agents implement,
review and verify. Core owns Checker/Types and central builds; support owns inference/
relation/display/utility and casing, with cross-owner source review; verifier owns
corpus/oracle/shared original assertions/xUnit/native and retained evidence. Avoid
concurrent writers and builds. Use coherent packets: mapping model/dependencies and
consumers; generic/operator/variance reconciliation; tuple/callable/predicate closure;
independent comprehensive acceptance. Move between them autonomously.

Inventory preserved original assertions, adaptations, reused compiler inputs and new
comparisons separately. The original casing body has 26 table assertions, not 26
separate upstream test functions. Existing original checker bodies remain applicable.
Select compiler-input coverage from source responsibilities, retain invalid diagnostics
and exclusions, and do not claim full compiler test success from reused inputs.

Compare full config/Program/syntax/bind/global/semantic diagnostic publication with
file locations, related chains and directives, public inferred/narrowed type and
relationship results, and focused repeat/order behavior for concrete risks. Private
cache/identity/storage observation is diagnostic, not an automatic completion condition.
No new private-state census is required. Source implementation/review status and
behavioral coverage status must remain distinct.

Use focused representatives after coherent builds and cheap affected regression checks
before Native AOT. A final frozen run must preserve every accepted regression gate.
Remove proven duplicate orchestration only where this safely reduces the current
bottleneck without altering assertions, historical evidence or acceptance scope; do not
build a new process framework. Record actual effort/build/run evidence and limitations;
do not invent unavailable model-token or monetary measurements. Completing T1–T6 does
not establish exhaustive correctness or completion of external checker responsibilities.
