# Type-system support checkpoint

Baseline `09630d6`; unchanged upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Assigned GPT-6.1 Sol support role owns the existing Inference/Relater counterparts
and agreed diagnostic-display/utility dependencies. Core remains the sole live
Checker/Types writer and central builder. No production edits to those core files,
independent builds, private-state census or broad foundation audit are planned.

## Source responsibility closure

The pinned [inference unit](../../upstream/TypeScript/tsc/internal/checker/inference.go)
defines 79 functions. All have represented counterparts; the default-type helper is
in Checker. This name reconciliation is a starting point, not a branch-fidelity or
completion claim. Existing priority/variance, conditional, generic mapped/reverse
mapped, variadic tuple, inference context/fixing and constraint/default engines must
be reconciled against their full source responsibilities, rather than replaced or
inferred complete from earlier fixtures. The initial missing arms were same-symbol intrinsic string-mapping inference and
constrained template-placeholder selection; both are restored in the current packet.

The pinned [relation unit](../../upstream/TypeScript/tsc/internal/checker/relater.go)
defines 188 functions. The initially absent selected helper `applyTargetStringMappingToSource` is now ported.
Two absent JSX-only helpers and enabled-tracing publication are separate external
consumers. Existing structured relations, conditional/mapped/indexed/variadic paths,
variance measurement, predicate/signature comparison and diagnostics need full
source-family reconciliation. Three structured string-mapping relation arms and membership are restored. The
superseded finite-object rejection is replaced by source conditions while actual
JSX consumer boundaries remain explicit.

The fixed [T1–T6 contract](../TYPESYSTEM-CONTRACT.md) authorizes packets for full inference-family reconciliation, full selected relation
family reconciliation, then required type-display/comparison and diagnostic consumers.
These are comprehensive source responsibilities for this campaign, not a syntax list
or selected-fixture completion boundary. Async/generator/checked-JS/JSX execution,
enabled tracing, service hover expansion and emit execution remain distinct, while
shared type algorithms used by those consumers remain in scope when required here.
Core owns intrinsic constructors/model/cache, key-remapping/type-node/instantiation,
constraint/default and predicate producer closure. Support hands off source findings
and disjoint helper requirements, then independently reviews core's saved families.

## Concrete dependency handoff

Core supplies source `StringMappingType.target`/`AsStringMappingType`,
`getStringMappingType(Symbol, Type)` and original constructor/alias/instantiation/
base-constraint arms. Support owns its inference/relation consumers, the missing
mapping-composition helper, intrinsic type-node serialization and actual type-order
comparison. Existing mapped/conditional/template and tuple models must be reused.

Root explicitly authorized the demanded [JS casing unit](../../upstream/TypeScript/tsc/internal/stringutil/js_case.go)
and its [pinned generated Unicode data](../../upstream/TypeScript/tsc/internal/stringutil/js_case_generated.go).
Support ports them to StringUtil/JsCase.cs and JsCaseGenerated.cs, retaining byte-string
decoding, multi-rune mappings, final-sigma context, lone-surrogate sentinels and pinned
Unicode 15.1 property/range data. Host culture or simple Unicode casing is not equivalent.
No upstream generator or network data update is used. Verifier owns the complete
original TestJSCasing body and all 26 assertions, plus behavioral Program/type/relation
comparisons. No type-system acceptance or runtime qualification is claimed yet.

## Current saved packet

The casing counterpart now has all eight source functions, 2,927 raw-byte mappings,
147 Cased ranges and 377 CaseIgnorable ranges. Core independently compared every
entry/range and the algorithm; its review evidence is in the campaign artifact folder.
The first coherent snapshot compiled and early verifier checks passed, without any
support-owned build. Runtime acceptance remains pending.

Intrinsic inference, composed membership and structured relations, type-node display
and intrinsic/instantiation ordering are saved. Source-family comparison also closed
the earlier constant-null object-literal target recovery and found/restored generic
target-signature canonicalization, mapped-template/string-index comparison and
private-conflicting intersection explanation. Unmatched-property display now keeps
source branch-local evaluation and nil-slice lengths.

The [compact inventory](typesystem-support-inventory.json) accounts for all 79 inference
and 188 relation source functions (185 selected relation functions; three external JSX/
tracing helpers). Definition triage, actual branch review and runtime qualification
are separate. Full selected family reconciliation and cross-owner review are closed; runtime
acceptance remains pending. There is no whole-checker or no-emit consumer completion claim.

The selected inference/relation source-family pass is now closed on the saved packet;
core's independent whole-family review found no substantive discrepancy. Full
behavioral/native acceptance remains pending. Changed display and casing packets
have separate core review ACKs. Required display
also restores exact variance `sub-`/`super-` naming, shadowed type-parameter usage and
reserved/global-Array alias branches. Service-configured hover expansion and external
JSX/trace consumers remain explicitly separate. Verifier independently confirmed all
casing data and original assertion bytes; its early managed comparisons passed.

Cross-owner review of core's mapping/variance, generic-tuple base constraints, pattern
intersection reduction and nonstrict nullable union construction found no discrepancy.
The next filter/intersection correction preserves Never as a real `everyType` predicate
input, and the Promise-named arity helper now performs original constructor-symbol
identity checks for ordinary synchronous generic callback callers as well. This is
shared diagnostic support, without Promise/async execution expansion.

The extended independent core review compared active-mapper instantiation/cache and
stack handling, inferred/deferred constraints and circular/default sentinels,
missing arguments, mapped keys and tuple/template modifiers, indexed-access
deferral/read-versus-write/property selection and diagnostic byte roles, conditional
distribution/tail/mapper order, and signature/predicate validation/contextual/body
production. No selected TypeScript discrepancy was identified in those inspected
families. A JS-only contextual flag difference remains outside the selected TS
consumer boundary; this is not a whole-checker review claim.

The draft behavioral inventory distinguishes six genuine original assertion bodies
(including the one new casing body), one authored external boundary, compiler-input
reuse and authored public comparisons. Its finite compiler-input extraction reuses
the accepted source parser and records configuration/default/emit-baseline adaptations;
the three whole JS/JSX/async inputs stay excluded without erasing their contents.
The requested mixed nested intrinsic membership case for the actual recursive
mapping-composition helper is included. The settled manifest currently has 77 records
(43 families, two demos and 32 compiler inputs); final runtime qualification remains
verifier-owned. Finite fixture success does not define source completion.

## Latest concrete display closure and review

Immutable3 compared 76 of 77 complete source records exactly. The remaining recursive
mapped-type diagnostic exposed real `NoErrorTruncation:true` handling: the source
printed 10,288 bytes while the old counterpart truncated at 288. No options or
expected bytes changed. The saved repair propagates the original option/flags, source
160-versus-1,000,000 builder thresholds and doubled printer cutoff, and exact selected
list/object/cyclic elision. Demanded synthetic trailing-comment storage, no-body
NotEmittedTypeElement printing, comment policy and line-start writer forwarding are
source-shaped partial display dependencies; service verbosity and emit execution
remain external. Core independently compared this packet with no finding. Coherent4
compiled clean; verifier owns the corrected runtime replay and final gate.

Core's independent full selected inference/relation review covers recursion and
priority state, mapped/reverse and variadic inference, contextual candidate fixing,
relation cache/error/variance transitions, conditional/index/template/intrinsic
structure and signature/property/index diagnostics. No substantive selected-source
finding remains. The source family accounting remains 79 inference and 185 selected
of 188 relation functions; name triage, actual source review and runtime evidence are
separate.

The optional public Relations observer has an explicit, hash-bound additive version
transition in the retained declarations guard. Historical public-display provenance,
requests, expected bytes and proofs remain unchanged. Read-only review confirmed
exact original or exact new source-pair acceptance and all ten finite rejection
controls. Practical new gate/recorder/CLI review found the original/managed/native
corpus, shared assertions, order and ordinary positive/negative outcomes, one retained
expressions cascade, and all drift/failure checks present. This is readiness review;
it does not qualify an unexecuted final Native AOT gate.
