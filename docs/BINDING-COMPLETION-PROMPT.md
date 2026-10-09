# Source-file binding completion mandate

User instruction following foundation delivery `2886d47`, preserved verbatim:

> Awesome! Let's continue:
>
> Proceed with completion and systematic verification of source-file binding before resuming callable-type checking.
>
> Use the committed foundation campaign at 2886d47 and the accepted semantic campaigns as the baseline. Retain all established requirements: Astra coordination, GPT-6.1 Sol implementation and independent review, upstream fidelity, direct Go-to-C# file mapping, readable C#, appropriate .NET libraries, xUnit, and mandatory Native AOT verification.
>
> **Define completion**
>
> Inspect the pinned upstream binder implementation, its dependencies, existing port, and historical limitations. Establish a completion contract for source-file binding.
>
> Account for declarations, symbols, scopes, declaration merging within a file, binder diagnostics, control-flow structures, and other binder-produced state needed by downstream stages. Determine the exact boundary and internal milestones yourself.
>
> Distinguish binder-owned behavior from checker-owned resolution, cross-file/global merging, transformations, module resolution, and emit. Preserve binder-produced information even where its downstream consumer remains outside scope.
>
> Share the contract and proceed autonomously. Do not limit it merely to the behavior already covered by existing fixtures.
>
> **Complete implementation and upstream coverage**
>
> Inventory applicable upstream tests and relevant compiler fixtures. Preserve original assertions where executable within the boundary, and distinguish direct tests from adapted tests and inputs reused for differential verification.
>
> Account explicitly for missing implementation, partial behavior, excluded tests, and later-stage dependencies. Do not claim full compiler-test success from binder-only verification.
>
> Revisit historical binder gaps, including shared table identities, shared linked-flow-list identities, allocated empty tables, sparsely exercised script modes, and applicable unobserved diagnostics. Resolve in-scope gaps or provide independently reviewed reasons they remain unexercised.
>
> Choose the implementation and verification methods yourself. Keep actual upstream behavior as the reference; avoid fixture-specific shortcuts, permissive placeholders, or weakened comparisons.
>
> **Establish acceptance**
>
> Require meaningful agreement with pinned Go on binder outputs and their relationships. Counts alone are insufficient. Make omissions in structural, identity, diagnostic, and control-flow comparisons explicit.
>
> Exercise both valid and malformed inputs, relevant parsing modes, and interactions between binding behaviors. Assess state reuse or repeated binding where those operations belong to the contract.
>
> Require actual Linux x64 Native AOT execution to pass the applicable verification. Preserve foundation and existing checker regressions. Do not imply verification of additional platforms without evidence.
>
> Use independent Sol review to challenge correctness, missing behavior, test provenance, exclusions, and the completion claim.
>
> **Execute efficiently and finish**
>
> Own internal milestones and continue between them without requesting another assignment. Apply the process lessons from the foundation campaign, adapting task boundaries, agent reuse, context sharing, and validation frequency to reduce wasted work.
>
> Maintain concise resumable state. Keep broad compiler infrastructure and checker expansion outside scope unless a material dependency requires a decision.
>
> At completion, deliver committed changes, a clean working tree, reproducible verification, updated source mapping, and a contract-based assessment.
>
> Report implementation completeness, test inventory dispositions, Native AOT results, remaining limitations, and observed effort. State precisely what the checker may now assume about bound source files and whether callable-type checking is ready to resume.
>
> Distinguish completion of this contract from exhaustive correctness or full compiler parity.
