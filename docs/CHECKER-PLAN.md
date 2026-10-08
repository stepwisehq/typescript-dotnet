# First bounded semantic checking plan

Status: **accepted bounded checkpoint** on 2026-10-08; all four milestones complete.
See [verification](./CHECKER-VERIFICATION.md) and [assessment](./CHECKER-ASSESSMENT.md). Prior binder commit
`e94cc6f`; unchanged upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Governing [user mandate](./CHECKER-PROMPT.md).

## Selected boundary and pressure

Investigations confirm the general checker is a large mutually recursive component.
Even simple declarations call shared initialization, name resolution, type caches,
flow analysis and diagnostic type formatting. A conservative lexical dependency
scan estimates about 1,900 functions/40,000 lines in the unrestricted closure; this
is an investigation estimate, not a sound reachability proof or a port-size promise.
Porting that closure wholesale would defeat the requested bounded phase.

The candidate useful slice is one TypeScript script with scalar variable declarations,
literal/identifier initializers and nested lexical blocks. It must use upstream
name resolution, literal freshness/widening and initializer type relationships,
including semantic errors. General expressions, calls, objects, functions, aliases,
modules and branching control flow are initially outside admission. Exact accepted
node forms will be documented and tested before acceptance, not inferred from a
small positive corpus.

Partial upstream methods are permissible only when retained branches preserve
algorithms and omitted paths fail explicitly as unsupported. New input admission,
host and result-status adapters must be identified separately from ported logic.
No unconditional success stubs, fixture-name checks or replacement miniature type
checker. Scoped initialization is a meaningful departure that must enumerate
omitted unreachable state and preserve required upstream type identities. Boolean
is the upstream union of regular true/false literals, not a substitute intrinsic.

NameResolver is needed; ReferenceResolver's emit-only callsites remain outside
this path. Required primitive diagnostic formatting may bring small node-builder/
printer branches as support; this does not authorize a general emission pipeline.
The coordinator reported these dependency pressures before approving any extension.

## Standard library and options

Initial oracle work uses one in-memory TS script, ESNext target, noLib/noResolve,
SkipModuleResolution, strictNullChecks true, strictBindCallApply false and
noImplicitAny false. There are no supplied library declarations or external files.
The actual upstream checker still reports missing required global library types.
Keep these global diagnostics separate and exact alongside file semantic diagnostics;
do not suppress them or claim a complete standard-library program was checked.

Use real compiler.NewProgram only inside the independent Go oracle host. A broad
project host port is excluded. The C# entry adapter will parse, bind and invoke the
ported selected checker functions. Unsupported features/options/dependencies must
produce an explicit unsupported outcome, distinct from a completed check with
semantic diagnostics. Syntax and binder diagnostic channels remain visible.

## Foundation review and relevant gaps

The verifier confirmed source/assets/native hashes of the accepted binder checkpoint;
recorded native and xUnit results still apply. No source changed before that check.

Shared symbol tables become directly relevant: upstream globalThis.Exports aliases
the globals table. Allocated empty lazy tables need focused model/oracle probes.
Identifier initializers still traverse binder assignment flows even when explicit
assignment expressions are excluded. Shared FlowList graphs appear unnecessary
for straight-line declarations, but this conclusion must be checked against the
final admitted AST/flow inventory. Reject unported branching flow paths explicitly.
No direct source-position-map dependency was found in the selected checker/relater/
resolver paths; exact byte-span diagnostics can reuse the accepted scanner helpers.
Retain broader mapping exclusions unless concrete calls demonstrate a need.

## Milestones and delegation

1. Confirm binder evidence; select supported syntax/options, initialization and
   dependency boundary; settle shared C# API and independent Go oracle contract.
2. Port exact semantic model/name-resolution support and an end-to-end tiny
   declaration/identifier/assignment-compatibility path with both valid and invalid
   inputs. A partial compiling component is not semantic acceptance.
3. Expand differential cases across name shadowing/missing names, literal types,
   annotation compatibility, inference and supported recovery/error boundaries.
   Classify every observed discrepancy as implementation, harness or explicit scope.
4. Independent cross-review; freeze source; xUnit and actual Native AOT checker
   agreement plus complete scanner/parser/binder regressions. Record native/source
   hashes, coverage/exclusions and the next informative slice, then stop.

Reuse explicitly selected GPT-6.1 Sol agents: scanner owns checker algorithms and
shared library builds; support owns type/model/resolver support and cross-review;
verification owns independent Go oracle, corpus, xUnit/native harness and gate.
Main coordinates significant adaptations, boundary and acceptance. Persist concise
subsystem progress and exact next actions; do not create a database without need.

## Verification contract to finalize

Compare ordered exact file and global diagnostic records (including raw arguments,
related information and byte spans), resolved identifier/declaration relationships,
and meaningful primitive/literal/union type structure/flags and assignability
results where queried. Numeric allocation IDs are not an equivalence target.
Neither matching diagnostic counts nor absence of a crash establishes agreement.

The Go oracle must execute real upstream semantic APIs, independent of C# partial
function/admission manifests. Add independently authored unsupported-boundary tests
so shared filtering cannot mask omission. Invalid cases must demonstrably trigger
intended upstream semantic checks. Prior phases' unobserved diagnostics remain
visible; close only gaps relevant to this slice. No profiling or optimization.

## First independent Go probes

The real upstream oracle built successfully. Nine initial specimens establish
expected semantics before C# implementation: valid declarations/shadowing/undefined
produce no file diagnostics; mismatches and null assignments produce 2322, unknown
names 2304, typos 2552, forward let references 2448 plus 2454, and a self-reference
2448. All retain eight ordered global 2318 diagnostics for Array, Boolean, Function,
IArguments, Number, Object, RegExp and String. Literal fresh/regular cycles and the
boolean false/true union are included in type graph observations. These are Go
reference results, not evidence that the C# slice works yet.

Public adapter contract: `TypeScript.Checking.Checker.Create(SourceFile)` validates
the supported input before delegating to scoped `CreateCore`. It rejects syntax
diagnostics/malformed recovery, non-TS or declaration/module files and excluded
constructs explicitly. Checking malformed syntax is deferred; semantic-invalid but
syntactically valid sources are required acceptance cases. The Go oracle does not
consume this admission whitelist.

## Early review checkpoints

Coordinator review before integration identified these concrete fidelity concerns
for owners to resolve and record before acceptance:

- Use upstream diagnostic collection comparison/deduplication, not a simplified
  ordering over rendered messages.
- Retain the actual optionality/selected-modifier predicate in
  areDeclarationFlagsIdentical rather than replacing it with a broader flag test.
- Resolve globalThis through the real resolver; reject its unsupported object
  semantics after resolution, without banning a same-spelled local scalar binding.
- Input admission must not silently change lazy TS JSDoc observations or let
  no-check/external-reference directives create a falsely successful checked result.

These are review tasks on in-progress code, not unresolved failures in an accepted
checkpoint. Final acceptance must report their disposition.

## Final disposition

All early review findings were resolved before the stable final gate. The actual
Native AOT executable passed 45 semantic cases,35admission controls,20Go utility
observations and10API checks, with82checker xUnit tests and all prior phase
regressions. Exact evidence and remaining limits are in the verification report.
The first gate's inventory-tool drift was rejected and a full frozen repeat passed.
No remaining implementation action or next-phase authorization exists.
