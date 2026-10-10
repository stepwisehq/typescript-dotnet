# Genuine initialization and declaration/object completion contract

Completion requirements; current acceptance status is in the
[assessment](DECLARATIONS-ASSESSMENT.md) and [checkpoint](DECLARATIONS-STATUS.md).
Baseline `e7b81a2`; pinned upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. The [mandate](DECLARATIONS-PROMPT.md)
and [reviewed roadmap](CHECKER-COMPLETION-ROADMAP.md) govern this campaign.
The accepted Program, resolution, configuration, foundation and semantic proofs are
inputs. This contract completes the following upstream responsibility closure, not
the whole checker. It must not be narrowed to obtain a passing checkpoint.

## Responsibility and acceptance domains

| Domain | Required implementation and observable acceptance |
| --- | --- |
| D1 Genuine initialization | Source NewChecker/initializeChecker over actual Program files/options: effective strict options, intrinsics/global types, declaration-only/noLib/missing-global environments, ordered global/ambient/pattern/augmentation merges, lazy semantic acquisition and per-file/global diagnostic lifecycle. Remove the exact-three-library acquisition restriction without modifying library bytes or options. Test genuine default and later library environments, invalid globals, cross-file merging, repeated acquisition and relevant order controls. |
| D2 Class/object producers | Class declarations/expressions, instance/static/constructor/prototype sides, generic this/base/implements relationships, inherited members and signatures, default/overloaded/abstract constructors and new, contextual class expressions, mixins, extends null and recursive/cyclic heritage. Compare inferred/public types and relationships as well as full valid/invalid diagnostics. |
| D3 Members and initialization | Methods, fields, parameter properties, accessors/auto-accessors, static blocks, abstract/override rules, private/protected and lexical # members, this/super, constructor accessibility and strict property initialization. Actual constructor/static return-flow and post-super algorithms are required; lexical assignment approximations are forbidden. Verify visibility origins, static/instance differences, super ordering and path-dependent initialization. |
| D4 Enums, namespaces and declarations | Numeric/string/computed/const/ambient enum values and member types, numbering/cycles/invalid constants, namespaces and ambient declarations, global/external augmentations, declaration merging and export consistency. Include TS internal import aliases and export-equals target resolution where required by these source producers. Preserve real target identities across files and meaningful invalid-merge diagnostics. |
| D5 Shared semantic closure | Computed declaration names, unique-symbol/well-known-symbol keys and late binding; demanded class/enum/constructor relation, inference, grammar, diagnostic display and accessibility algorithms. Complete declaration unused-identifier registration/check lifecycle for admitted forms and applicable noUnused options, rather than ignoring them. Preserve lazy unsupported dependencies explicitly. |
| D6 Integrated acceptance | Ordinary disk project semantic invocation with genuine Program libraries/options; cross-file warehouse/domain demonstration and invalid variants, original applicable checker assertions, upstream compiler-input comparisons, independent correctness/omission review, actual Linux x64 Native AOT and all retained regressions. Restore the three mapper semantic-channel debts when this domain permits their original operations. |

Source families and concrete current gaps are grounded in the short
[core](progress/declarations-core.md), [support](progress/declarations-support.md)
and [verification](progress/declarations-verification.md) assessments. Track each
selected family as partial/implemented/reviewed/verified as work proceeds; inventory
alone is not implementation. Preserve direct existing Go/C# counterparts and license
notices, recording meaningful adaptations in the source map and final assessment.

## External boundaries and genuine environments

TS and declaration TS are the selected declaration language. Decorator syntax,
JS/JSDoc/JSX, async/generator/protocol execution, remaining general expression/statement
and advanced-type families remain later roadmap campaigns. Existing supported bodies
remain supported; required class constructor/static initialization flow and declaration
computed-name checking are included. Destructuring, switch/try, optional-chain and
other unfinished body consumers must reject explicitly when reached/admitted, never
silently skip checks. Namespace/augmentation dependencies do not imply completing all
runtime CommonJS interop. Emit/build/watch, service serializers, project infrastructure
expansion and optimization are external. Source tracing/process boundaries remain as
previously recorded. Material dependency expansion must be reported before proceeding.

The actual Program chooses libraries and options, including noLib, explicit lib,
skipLibCheck/default-lib and diagnostic directives, according to upstream rules.
All supplied library bytes remain genuine. skipLibCheck is source behavior, not
permission to return any for a demanded unsupported declaration. Initialization of a
library is distinct from complete checking of all its APIs/bodies. Unsupported options
or semantics are explicit failures distinct from diagnostics and successful checking;
no categories, missing globals or declaration errors may be suppressed to match.
Legacy named semantic profiles may remain explicitly authored regression adapters,
not alternative production checker algorithms. As a responsibility is completed,
replace vertical-slice scaffolding, profile-conditioned semantics and obsolete guards
with the comprehensive upstream implementation. Preserve meaningful test assertions,
not temporary architecture. Necessary C#/.NET adaptations remain documented; untouched
unfinished responsibilities retain explicit boundaries rather than permissive defaults.

Genuine library replay requires typed ambient binding-element parameter validation
and the synchronous iterable/iterator type-model helpers it calls. Include the original
signature, next-parameter, return-result and done/value discrimination algorithms with
their existing caches and diagnostics. This proportionate declaration dependency
extends beyond tuple indexing; a tuple-only shortcut would not preserve upstream.
It does not complete runtime destructuring, generator/yield bodies or async protocol
execution. Those unfinished consumers remain explicit. This is an additive dependency
clarification, not a relaxation of the original library or acceptance requirements.

Malformed supported declarations must reach source grammar/semantic diagnostics.
External syntax guards must not preempt ordinary invalid class/enum/namespace cases.
Unused/destructuring branches for excluded forms remain explicit source exclusions;
the supported declaration lifecycle must honor requested unused checks. Generator
mapper global/incremental assertions remain a documented later protocol dependency.

## Execution and verification

Root Astra owns contract/acceptance; reused GPT-6.1 Sol agents own substantive work.
Core is the sole live writer of Checker, Types, Links, Flow and Program/host acquisition,
and owns central builds. Support owns existing GrammarChecks, Relater, Inference and
diagnostic-display/accessibility counterparts, supplying bounded Checker method patches
for core integration. Verifier owns original provenance, oracle/tests and demonstration.
No arbitrary production fragments, overlapping whole-file edits or per-leaf rebuilds.

Internal sequence: genuine initialization; declaration models and producers; demanded
validation/relations/flow/display and admission; integrated review and acceptance.
Move between these without user approval. Group builds around coherent dependencies;
run cheap targeted tests before one frozen final retained cascade. Existing private
instrumentation may remain; new observation needs a concrete correctness reason.

The two pinned checker behavioral unit tests retain their original assertions and
actual environments. Compiler test inputs/baselines, adapted tests and authored cases
are reported separately, including explicit exclusions. Compare complete relevant
config/Program/syntax/bind/global/semantic diagnostics with file/position/related data
under equivalent options and unchanged declarations. Public type/symbol/base/constructor
observations supplement diagnostics for concrete blind spots; exhaustive private-state
identity is not a gate. Original required identity assertions are preserved.

Completion requires D1–D6 implemented, reviewed and behaviorally verified; actual
Linux x64 native execution, retained regressions, reproducible guide, separate remaining
implementation/evidence limitations, refreshed roadmap, commit and clean tree. No other
platform or exhaustive correctness claim follows. Large-project benchmarks await the
whole-checker no-emit gate. Record measured builds/gate effort only; token/cost telemetry
is unavailable unless subsequently supplied.
