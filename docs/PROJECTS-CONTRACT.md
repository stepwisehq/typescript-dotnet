# Bounded multi-file ES-module checking contract

**Accepted P1–P5 on Linux x64.** See [assessment](./PROJECTS-ASSESSMENT.md) and
[evidence](../validation/results/projects-verified.json). Baseline `2598ceb`; upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e` is unchanged. See the
[mandate](./PROJECTS-PROMPT.md). Completion means the rows below pass, not whole
Program/module/checker parity. Do not narrow this contract to declare success.

## Useful target and external boundary

Check a small project from disk using one genuine shared checker over distinct
source-file ASTs. Preserve module-local scopes, alias targets, exports, re-exports,
type/value meanings and cycles. Compose the accepted collection capabilities across
files, with complete applicable diagnostics and meaningful semantic observations.
No concatenation, per-file substitute checker or manufactured import symbols.

The demonstration splits record processing into model types, generic collection
functions, a barrel and an application. It combines recursive records, contextual
callbacks, generic inference, mapped utilities, array/tuple operations and loops.
Add a meaningful recursive type relationship across modules. Deliberately invalid
variants exercise imported generic constraints, wrong argument/return/property types,
missing/non-exported names, type-only value misuse and resolution failures. Targeted
fixtures additionally cover module cycles and ambiguous/circular re-exports.

Package resolution, Node/package-format behavior, project references, incremental
state, emit, services and broad compiler infrastructure are outside this campaign.
JS/declaration emission is not implied by successful type checking. Retain prior
language exclusions (classes/new, destructuring, optional/async syntax, custom
iterators, broader libraries, namespaces/augmentation and others documented in the
[collection contract](./COLLECTIONS-CONTRACT.md)), except imports/exports and multiple
user source files expressly authorized here. Internal checker/printer representations
required by admitted semantics must work even if their equivalent source syntax is
excluded. An unsupported dependency must fail explicitly, never silently become any.

## Configuration, resolution and source forms

Provide a documented `tscheck --project <config>` entry and public project API. The
configuration is an explicitly authored strict JSON subset with required nonempty
`files` array and optional `compilerOptions` containing only recognized fixed values.
No full tsconfig parser/CLI claim: reject unknown or duplicate properties, wrong types,
unsupported option values, extends/references/include/exclude and other unsupported
configuration. File paths are relative to the configuration directory, which bounds
the project. Normalize dot segments; reject outside-root and symlink-dependent paths.
Linux case-sensitive identity is the verified platform policy. Missing/invalid roots
must cause a checking failure rather than an empty successful project.

The .NET disk host requires paths representable as Unicode scalar values. Escaped
lone surrogates remain valid scanner/string values, but cannot become filesystem
paths through replacement decoding. Reject that path-encoding boundary explicitly;
valid surrogate pairs and real replacement-character filenames remain supported.
This is the same representability limit as the strict configuration's filename
strings, not a claim of arbitrary Unix byte-filename support.

Use upstream **Bundler resolution and ESNext modules/target**. Classic and Node10
are unsuitable because this pin reports their removed-option diagnostics. Port the
selected upstream relative loading/extension search algorithms and preserve search
precedence. Support extensionless relative imports, `.js` to `.ts` substitution,
explicit `.ts` specifiers and directory `index.ts`, including normalized duplicate
paths and `../` paths remaining inside the project. Relative resolution remains
relative to the importing file, not the configuration directory.

User source files are `.ts` implementations. The existing three genuine embedded
libraries are the only `.d.ts` environment. If upstream search selects another file
form or needs package metadata, report unsupported rather than bypassing that candidate
and choosing a later `.ts` file. Bare/absolute package specifiers, path mappings,
rootDirs, custom extensions, `.mts`/`.cts`, JS/JSX/TSX/JSON, user declaration files,
import attributes, `import =`, `export =`, dynamic import, `import.meta`, source
import-type expressions and ambient module declarations/augmentations remain excluded.
Module scripts and side-effect dependencies retain original script/global semantics;
importing a script as a module must produce the upstream non-module diagnostic.
Malformed source is explicitly outside semantic checking; expose its parse diagnostics
and failure status rather than claiming that its semantic diagnostics are complete.

Required ES-module forms: named/default/namespace/side-effect imports; declaration-
and specifier-level type-only imports/exports; exported variables, functions, aliases
and interfaces; local named exports and renames; default expressions and named/anonymous
default functions/interfaces; named re-exports, export-star and namespace re-exports.
Include string export names supported by the pinned ESNext semantics. Preserve
duplicate/conflicting exports, missing defaults/members, readonly imported bindings,
type/value restrictions and re-export collision diagnostics.

The pinned parser reports 1003 for an anonymous default interface. Its required
coverage is therefore that original syntax failure and CLI failure status, under
the malformed-input rule above. Named default interfaces and named/anonymous
default functions remain semantic cases; an invalid anonymous interface is not
counted as successfully checked input.

## Options and authentic library environment

Load the complete byte-identical ES5, decorators and decorators.legacy declarations
already embedded at the accepted collection checkpoint. Their hashes, notices and
relative order remain unchanged. They are explicit roots and not default libraries.
User roots/dependencies follow upstream program source discovery/order. Parse/bind
libraries and resolve demanded declarations normally; `skipLibCheck=true` skips their
whole declaration-body checking. Loading these files does not establish every API.

Carry the accepted fixed strict options: strictNullChecks, strictFunctionTypes,
noImplicitAny and noImplicitThis true; strictBindCallApply, noImplicitReturns,
exactOptionalPropertyTypes and noUncheckedIndexedAccess false. The effective
downlevelIteration behavior is false, but its removed configuration spelling is
rejected: the original Program reports diagnostic 5102 even for explicit false.
Project additions: module ESNext, moduleResolution Bundler, noEmit=true,
allowImportingTsExtensions=true and noUncheckedSideEffectImports=true. noLib=true
with explicit fixed libraries, noResolve=false for genuine module discovery. Automatic
external type acquisition is disabled with `types=[]`; `allowJs=false` and
`resolveJsonModule=false` are explicit (Bundler would otherwise default the latter
to true at this pin). A directory package.json is rejected only when the original
relative search would consult it after file candidates fail; unrelated ancestor
package files are not a reason to reject a supported project. Port applicable Program
diagnostic policy, including noEmit filtering, rather than reusing the historical
single-source adapter's assumptions. Record the exact options with oracle evidence.

Necessary diagnostic formatting must perform original ancestor package lookup when
reached, including transitive types with no direct import alias. If generating a
specifier demands an actual package imports mapping, that operation is explicitly
unsupported under the existing package-resolution exclusion. Do not replace required
metadata with nil or disable an option merely to bypass this dependency. Plain
ancestor type/name/version metadata is not itself grounds for rejection.

The CLI distinguishes success (exit 0), compiler diagnostic failure (exit 1), and
unsupported/configuration/host failure (exit 2). Report file paths, diagnostic codes,
messages/chains/related locations, byte ranges and user-readable line/column positions.
The ordinary native invocation must need neither Go nor an upstream checkout.

## Verification and observable acceptance

| Row | Required capability and evidence |
| --- | --- |
| P1 disk/config/resolution | Strict documented configuration; real selected upstream resolution/loading; file identity and search precedence; missing/unsupported controls; direct upstream assertions with provenance where applicable |
| P2 module semantics | All listed imports/exports, aliases/type-only/value meanings, module-local scopes and genuine global library merge; successful and failing cross-file generic/object/callable operations |
| P3 cycles/identity/state | Cyclic types/functions, star/alias cycles and collisions, shared file/symbol targets, cache repeated-query behavior, entry/query order controls and independent fresh-program isolation |
| P4 diagnostics/demo | Real disk demonstration succeeds without missing globals; invalid variants match complete upstream diagnostics including file/location/related information; CLI reliably fails for invalid/unsupported projects |
| P5 independent acceptance | Reviewed source fidelity and omissions; test inventory/dispositions; all retained regressions; actual Linux x64 Native AOT project/demonstration verification; reproducible instructions, committed clean delivery |

Use the pinned original Go **Program and default module resolver** with equivalent
files/options/libraries as reference. Keep original-Go expected outputs independent
of C# output. Compare loaded file identities and resolution edges, module/export
tables, alias declaration/ultimate-target relationships, selected types/signatures/
call resolutions and cross-file diagnostics. Counts or display strings alone do not
establish semantic identity. Record exactly which lazy/cache fields are compared and
which remain unobserved; use fail-closed observations for selected new structures.
Prior comprehensive collection cache coverage remains a regression, not a reason to
reimplement a whole private-state census for this campaign.

Exercise valid/invalid interactions beyond the demonstration, reordered roots/queries,
repeated use, fresh project instances, unresolved paths, path aliases/case, cycles,
ambiguities and unsupported controls. Distinguish direct original assertions, adapted
assertions, compiler-input reuse and authored comparisons. Do not report binder-only or
input-only processing as full compiler test success. Preserve accepted oracle fixture
and evidence bytes. Native AOT publishing plus actual Linux x64 execution is mandatory;
no other platform is claimed. Preserve scanner/parser/binder and accepted semantic
regressions. Unsupported cases cannot count as successful checking.

## Ownership, milestones and efficiency

Reuse three explicit GPT-6.1 Sol owners. Core owns checker algorithms, module alias/
model state and demanded name-resolution/formatter dependencies. Support owns the
source-mapped compiler/program, resolution/path/config support and authored disk/CLI
adapters. Verification owns admission extensions, upstream oracle, semantic projection,
corpus/example/xUnit and acceptance tooling. Root owns scope, contract, mapping,
coordination, acceptance and commits. Agree factory/host APIs directly before edits.
Keep one owner per shared file and transfer ownership explicitly for review repairs.

Internal milestones: (1) configuration/loading and a genuine two-file generic call;
(2) complete import/export/type-only/re-export/cycle families; (3) interaction corpus,
demo and independent review closure; (4) frozen comprehensive native/regression
acceptance and committed delivery. Do not stop after an internal milestone.

Core coordinates central library builds after useful integrated changes; others use
shared outputs and avoid concurrent rebuilds. Use targeted probes first, affected
regressions after substantive families, and one frozen comprehensive gate unless a
failure or code change requires repeating it. Capture concrete build/test durations
and coordination lessons, not invented token/cost figures. Per-owner checkpoints and
this contract are sufficient recovery state; no new workflow framework is needed.
