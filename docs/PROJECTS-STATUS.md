# Project campaign status

**Accepted bounded P1–P5 contract on Linux x64.** Baseline `2598ceb`.
See the [contract](./PROJECTS-CONTRACT.md), [assessment](./PROJECTS-ASSESSMENT.md),
[guide](./PROJECTS.md), and [durable proof](../validation/results/projects-verified.json).
The delivery commit containing this status records the accepted implementation.

| Row | Implemented scope | Accepted verification |
| --- | --- | --- |
| P1 disk/config/resolution | Selected upstream loading, task identity, relative resolver and diagnostics; strict authored config/disk host | Native differential corpus plus shared path/config/identity assertions and ordinary CLI controls |
| P2 module semantics | Contracted imports, exports, re-exports, type-only and cross-file checking | 48/48 native comparisons exact against pinned original Go |
| P3 cycles/identity/state | Recursive types/functions, alias/star cycles, reached symbol relationships, shared file/program state | 14 selected order cases; 30 executions per runtime, exact |
| P4 diagnostics/demo | Five-module valid/invalid projects and ordinary user CLI; complete file/range/chained diagnostics | CLI 10/10, lexical projection 14/14; valid exit 0, invalid exit 1 with eight original diagnostics |
| P5 acceptance/delivery | Independent source, observation, tooling and final runtime review closed | Full gate exit 0, 780.934 seconds; 23,542 xUnit passes, no skips; nine native binaries and all retained regressions; durable proof recorded |

Three explicit GPT-6.1 Sol owners handled implementation and independent cross-review:
[core](./progress/projects-core.md), [support](./progress/projects-support.md), and
[verification](./progress/projects-verification.md). Coordinator owns contract,
source mapping, acceptance, and commit. No next campaign is authorized.

The first gate stopped with exit 143 after a confirmed surrogate-path false success.
Strict module-path decoding repaired it; unrepresentable filesystem paths are explicit
unsupported host errors, not claimed diagnostic parity. The fresh successful gate
above is the acceptance evidence. Protected historical fixtures/proofs and canonical
48 expectations remain unchanged. Observation omissions and exclusions remain explicit
in the assessment and [departures](./PROJECTS-DEPARTURES.md).

Measured central Debug library work: 11 invocations, 59.76 seconds reported build time,
three failed. This excludes other builds and overall effort; tokens/cost are unavailable.
The chronological notes below describe intermediate states, not remaining acceptance work.

## Historical decisions and integration observations

- Bundler/ESNext avoids removed Classic/Node10 option diagnostics at the pin.
- Effective downlevelIteration=false must be represented by omitted configuration;
  explicitly false triggers original Program diagnostic 5102.
- Fix resolveJsonModule=false explicitly because Bundler defaults it true; allowJs=false
  and types=[] make file forms and automatic external type discovery unambiguous.
- Reject package metadata only at an actual relative directory lookup, preserving
  upstream file-before-directory search precedence.
- Original-Go ancestor package.json module/commonjs controls have identical selected
  file/resolution/export/alias/type/diagnostic outputs. Private PackageJsonDirectory
  is explicitly not observed; no general package-format parity is inferred.
- Namespace imports may produce real SyntheticType cache entries containing the
  original type even without CommonJS wrappers. Preserve the actual path.
- Internal diagnostic type printing may require import-type representation despite
  source import-type syntax remaining excluded.
- Strict JSON configuration is an authored subset, not a full tsconfig implementation.
- Accepted collection source/native fingerprints were checked before changes. Broad
  old regressions are deferred to a useful integration point and final acceptance.
- Early coordinator fidelity review rejected an authored recursive loader inside the
  Project wrapper. Support is using mapped FileLoader/FilesParser task-dedup/load and
  separate final ordering functions, including original single-threaded LIFO workgroup
  semantics. Matching a basic DFS file order alone would not establish this behavior.
- New path.go functions extend the existing Path.cs counterpart rather than creating
  another campaign-specific port file.
- Central compile runs: initial duplicate-helper pass failed, method-body dependency
  pass failed, then central 2 passed with zero warnings/errors (5.73 s). The verifier
  consumes an immutable output snapshot; this is not runtime or native acceptance.
- After host integration, the concrete imported-symbol accessibility dependency was
  transferred as one exclusive source file to support to reduce the checker owner's
  serial work. Core retains other formatter files; independent review will cross owners.
- Original observer discovery found indistinguishable unrooted exportTypeLinks module
  clones. This is a comparison-identity question, not a demonstrated C# defect. Inspect
  the actual producer; preserve exact reached identities and document any omitted
  cache-only clone census. Do not add allocation provenance merely to maximize totals.
- First runtime failures were delivered to core as one packet. Fix common export-check
  and demanded formatter dependencies before repeating the whole packet. Central 3
  and 3b both compiled cleanly; compiling alone did not establish project semantics.
- Export-check failure confirmed as indexing nil Exports on an import-only module.
  The source repair restores original nullable lookup without allocating an empty table.
  Cross-file diagnostics also demand real module-specifier generation; support owns
  its bounded upstream counterpart, with explicit escalation for broader dependencies.
- Formatter dependency assessment found a previously unsampled ancestor imports-map
  lookup for transitive types without a direct alias. Genuine lookup/read is required;
  actual package mapping remains explicitly unsupported. Earlier type-only package
  controls are sample evidence, not a general metadata independence proof.
- Distinguish defects: positive-case graph registration ordering is harness-owned;
  import-only nil-map access and root StringLiteral printer dispatch are compiler
  defects. Expected Go behavior remains unchanged for each repair.
- Central 5 closes the original 11-case packet. Independent review separately found
  nil Exports lookup in reportNonExportedMember, repaired source-wise and queued for
  next build with a two-file witness. Focused new-map scan found no further instance.
  Central 6 compiles the nil-map, namespace invocation recovery, original 1409 argument and read-only index-shadow fixes. Its first finite module packet passes 12/12, shared dispatcher 26 groups; remaining interaction/full diagnostic packets are pending.
