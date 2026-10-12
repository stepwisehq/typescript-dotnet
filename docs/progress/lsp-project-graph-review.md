# Project graph independent source review

Status: clear, source review only. Execution and complete Session/service integration
remain separate from this review. The reviewed source pin is
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

The scope covers all144 upstream functions: Project44, compiler host10, collection20,
config registry builder26 and collection builder44. It includes the caller-grounded
diagnostic equality adapter and authored graph ownership assertions. Accepted
low-level dirty/cache/overlay owners were not re-audited.

Reviewed constructor/default state, ID admission/canonicalization, shallow snapshot
clones and fresh once owners, command-line memoization/reset, actual Program.UpdateProgram
reuse, parse/content-map acquire/ref/deref, binding, speculative acquire rollback,
checker-pool discard, resolver cleanup and compiler-host freeze. Collection ordering,
API refcounts/error rollback, opened/closed roots, configured/inferred/synthetic
selection, BFS level filtering/shared visited state/deterministic retained paths and
ancestor fallback match the original algorithms. Config reload/extended dependencies,
watch patterns/ignored directories, manifest invalidation, file-event policy and
project-reference release retain source control flow and ownership.

The concrete review findings were corrected by the source owner: five zero-value
Set producers now allocate lazily at their first Add/Union (extended config paths,
API retain, reconfigured IDs, removal candidates, surviving references), and the BFS
Core.Map replacement retains nil output for nil resolved references. Required
CopyMapInto nil/empty clone behavior was verified directly; no change was demanded.

Established ParsedCommandLine List boundaries require documented detached copies
where Go slice headers can shorten independently. Required upstream Clone operations
remain. The authored diagnostic equality is limited to actual inferred/synthetic
callers: preserved config diagnostics have retained SourceFile owners, reconstructed
API diagnostics have nil files. It compares all concrete diagnostic/message/related
fields and nil versus empty slices, with cycle guards; it does not claim general Go
reflect.DeepEqual over unrelated reconstructed ASTs. Authored assertions exercise
reconstructed nil-file equality, text/location/related changes and nil/empty errors.

No implementation or execution was performed by this reviewer. This is not full
project/session test acceptance or language-service/server completion.

## Bound files

- [Project.cs](../../src/TypeScript.Scanner/ProjectSystem/Project.cs): `6981d60771d5fd2df74f117a5d01bff3a85375f4623f3db29ff6bbd46a4375e6`
- [ProjectCompilerHost.cs](../../src/TypeScript.Scanner/ProjectSystem/ProjectCompilerHost.cs): `1e8c6900a4ddcc07d93efe7f316129af9e91ade54eff43bc265a3f88666bc58b`
- [ProjectCollection.cs](../../src/TypeScript.Scanner/ProjectSystem/ProjectCollection.cs): `8c70cd08e4e5dcc119071d9c890bc58a92cd9db11da099486c34b6d2dc85f1c0`
- [ConfigFileRegistryBuilder.cs](../../src/TypeScript.Scanner/ProjectSystem/ConfigFileRegistryBuilder.cs): `94994864b851daac5226e0799b6684cd1c1cfd37ec704b118391225e1ce8e8bf`
- [ProjectCollectionBuilder.cs](../../src/TypeScript.Scanner/ProjectSystem/ProjectCollectionBuilder.cs): `795ca0c2cc7262aea0a09afffcbad4d7270728184bca29c854dbbed8d987e060`
- [ProjectDiagnosticEquality.cs](../../src/TypeScript.Scanner/ProjectSystem/ProjectDiagnosticEquality.cs): `b4d210f691cc753f3ecd65ebb9790189531aa12ae131892822ad0f56b6f6d8a3`
- [ProjectGraphOwnershipChecks.cs](../../validation/LanguageServices.Checks/ProjectGraphOwnershipChecks.cs): `bcb53e4cce240d0955be5a8fb9872fa0a04cdf56afb1527533abcc001ca320d7`
