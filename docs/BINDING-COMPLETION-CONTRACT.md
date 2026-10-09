# Source-file binding completion contract

Status: fixed target satisfied, B1–B6 accepted; see the
[assessment](./BINDING-COMPLETION-ASSESSMENT.md) and
[proof](../validation/results/binding-completion-verified.json).
Baseline `2886d47`; upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Governing
[mandate](./BINDING-COMPLETION-PROMPT.md). Historical binder and semantic milestones
remain accepted within their own boundaries. Do not narrow this contract to declare
completion or silently reinterpret a reference failure as successful binding.

## Capability

Bind a freshly parsed source file, inspect the full published binder graph and use
its file-bound symbol/scope/flow relationships without running the checker or setting
up a project. Include every initial binding path in the pinned binder, not only the
old 118-case corpus. The five parsing modes are JS, JSX, TS, TSX and JSON, with
declaration filenames and caller-supplied source parse/module options. Preserve
malformed-input recovery and distinctions between syntax and binder diagnostics.

B1. Account for all 167 binder.go functions, their state and required dependency
closure. Preserve algorithms, branches, names and file mapping; fresh managed binder
allocation and arenas may replace pooling/allocation mechanics. All in-scope bodies
must be implemented with no success stubs or permissive unsupported paths. Include
the four other exported binder helpers: GetSymbolNameForPrivateIdentifier,
FindUseStrictPrologue, SetValueDeclaration and GetContainerFlags, preserving raw
byte-string names and their actual input preconditions.

B2. Complete declarations, symbols and scopes within a file: merge/conflict rules,
ordered declarations, value declarations, symbol ownership/parents, local/export
links, members/exports/locals, container chains, default/export-equals/import-alias
declarations, namespaces/local module classification, private/computed names,
destructuring, ambient patterns, JS/CommonJS/expando and JSDoc binding. Preserve
every binder-produced flag and source metadata even when only a later stage consumes
it. Imports record declarations; they do not resolve files or target symbols.

B3. Complete binder-produced control flow: attachments, node/list identity and
sharing, ordered antecedents, cycles, narrowing-expression classification, reachable
and unreachable states, branch/loop/label/switch/try-catch-finally/return/throw flow,
IIFE/constructor behavior, synthetic switch data and reduce-label snapshots.
Building flow is included; using it to narrow semantic types is checker work.

B4. Exact binder diagnostics and state lifecycle. Compare source ownership, byte
spans, code/category/key/text, raw arguments, flags, related information, message
chains, ordering and nil/empty states. Match repeated binding on the same instance,
isolation of separately parsed files (including equal filenames), and bound-once
success, concurrent publication and failed-callback no-retry behavior. Already-bound
instances preserve upstream no-op semantics; no incremental invalidation or edited
AST repair is promised. Include file-bound symbol ownership/name/flag helpers and
the required graph support; checker-transient controls must stay distinct from
binder-produced symbols.

B5. Systematic upstream evidence and identity-gap closure. Inventory original
binder/support tests and benchmarks; retain all applicable original assertions in
xUnit and native shared bodies. The pinned binder package currently has only
BenchmarkBind, with no correctness assertions; this is not a test-port pass.
Reuse all 17,723 accepted foundation text/options requests for independent binding
comparison, retaining original input provenance and the 25 already classified
non-source units. Do not re-extract the same physical corpus without a concrete
reason. The four explicit ancillary-mode adaptations retain their classifications.
Add targeted valid/malformed and interacting behavior cases where inventories show
gaps; do not derive expectations from the C# implementation.

Primary comparisons bind fresh parser output before lazy JSDoc, subtree-fact, symbol
ownership or other observation helpers run. Any pre-bind syntax snapshot must avoid
input-changing getters or use an independent parse. Specify the actual observation
order and test selected cold/prewarmed interactions separately. Preserve historical
oracle outputs with an opt-in protocol rather than editing accepted fixtures.

Resolve natural-producer reachability for allocated empty tables, shared FlowList
tails and aliases between distinct published table fields. Exercise reachable
relationships. For any impossible production state, require an independently
reviewed write/dependency-closure proof and retain that absence as unobserved
production coverage. Authored graph alias/null-empty sensitivity controls
may verify the observer but must not masquerade as naturally produced binder output.
Reconcile all binder-referenced diagnostics, including 1250/1251/1252 in the unused
strict-mode helper, with witnesses or independently reviewed reachability reasons.

B6. Actual Linux x64 Native AOT execution, comprehensive foundation and accepted
semantic regressions, independent correctness/omission/provenance review, durable
source/asset/native evidence, reproducible instructions and committed clean delivery.
No unresolved defined-output discrepancy may remain. Enumerate graph omissions and
unsupported operations explicitly. Process-global ID values may be canonicalized
only while preserving their actual owner and alias relationships; unordered map
iteration may be normalized, but declaration/flow/diagnostic order remains compared.

## Boundaries and exceptional inputs

Name/reference resolution performed by checker-facing sibling files, type inference,
flow-based semantic checking, checker-created transient symbols/caches, cross-file
and global merging, module resolution, project configuration, emit, transformations,
incremental compilation and language services are outside this campaign. Existing
accepted semantic support remains regression-tested. A binder-produced state field
cannot be excluded merely because one of those later stages consumes it.

All original input and outcome dispositions must be visible. If pinned Go itself
fails on a source case, retain the exact input and failure, investigate its cause and
separate reference-defined binding from unsupported/malformed-AST preconditions or
resource limits. A matching failure is not a successful bound graph. Broad or
unexpected dependency/resource pressure requires coordinator assessment and a user
decision if the agreed boundary cannot be met. Do not silently omit difficult cases.
General arbitrary synthetic ASTs, resource-exhaustion equivalence, Go backing-array
capacity semantics and a concurrent whole-compiler API are not completion claims.

## Execution and gates

1. Inventory original source/tests and historical gaps; fix this contract and
   observation order. Reuse accepted foundation evidence instead of rerunning it
   before any relevant change.
2. Complete source/support inventories and targeted relationship/lifecycle/mode
   probes. Implement concrete defects with independent upstream witnesses.
3. Run one broad managed comparison for triage. Replay affected cases and add
   meaningful regression assertions after fixes; do not repeat unchanged suites.
4. Independent reviews challenge source closure, graph fidelity, exclusions and
   provenance. Review all new files, including whitespace, before freezing them.
5. Freeze source and verification assets; run actual Native AOT broad/shared checks
   and the complete foundation/semantic regression cascade. Include the full finite
   Go numeric baseline supplement required by foundation acceptance. Distinguish
   test functions, table/seed cases, graph inputs and repeated platform executions.
6. Record contract acceptance or a specific material blocker, update mapping/state,
   commit reviewed work and confirm a clean tree. Do not start callable checking
   automatically; report whether this binding foundation supports that next mandate.

Reuse the three explicitly selected GPT-6.1 Sol agents with disjoint ownership:
scanner agent now owns binder algorithms, support owns AST/runtime closure and
shared library builds, verification owns independent oracles/corpora/gates. Root
owns scope, significant decisions, integration, assessment and commits. Small owner
checkpoints and source/test inventories suffice; no new workflow framework. Record
measured effort only; token totals or monetary cost cannot be inferred from elapsed
time. Existing main-session model-metadata limitations remain disclosed.
