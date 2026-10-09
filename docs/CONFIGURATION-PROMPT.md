# User mandate: upstream subsystem completion

Baseline: `2c850db`. Exact campaign instruction:

> Change the next campaign’s organizing principle from bounded feature delivery to completion of a high-value upstream subsystem.
>
> Use checkpoint 2c850db as the baseline. We now have a working bounded project checker and substantially verified scanner/parser/AST and binder foundations. The objective is to reduce repeated dependency work and integration effort as the remaining compiler is ported.
>
> Retain all established requirements: Astra coordination, GPT-6.1 Sol implementation and independent review, close upstream fidelity, direct Go-to-C# source mapping, clean and readable C#, appropriate .NET libraries, xUnit, and mandatory Native AOT verification.
>
> **Select the subsystem**
>
> Begin with a short assessment of the existing source mapping, implementation gaps, and recent campaign history.
>
> Identify which partially ported subsystems have repeatedly required changes, where dependency boundaries have caused rework, and which completion would most benefit subsequent work. Distinguish implementation effort from verification and coordination overhead. Use available evidence without inventing unavailable measurements.
>
> Configuration parsing and file discovery are the leading candidate. Assess whether they form a coherent completion target and identify their required dependencies. You may select a different subsystem if the evidence clearly favors it.
>
> Keep this assessment lightweight. State your selection and rationale, then proceed with implementation. Do not stop after producing a plan or require me to choose routine technical boundaries.
>
> **Define completion from upstream**
>
> Establish a completion contract based on the pinned upstream subsystem’s responsibilities, entry points, behavior, and supporting dependencies.
>
> The contract must extend beyond the needs of the current demonstration project. Do not define completion as implementing whichever subset makes selected fixtures pass.
>
> Account for upstream files and functions, partial implementations, adaptations, generated material, and platform-sensitive behavior. Preserve the direct source mapping.
>
> Identify genuine external subsystem boundaries. If required functionality lives elsewhere, assess whether completing that dependency now will avoid repeated partial ports. Include proportionate dependencies and explain material scope expansion before undertaking it.
>
> Keep implementation completeness separate from verification completeness. An implemented but unexercised path and an unimplemented path are different limitations.
>
> **Complete the implementation**
>
> Port the actual upstream behavior and algorithms. Use .NET facilities where their semantics fit, while preserving traceability and documenting necessary adaptations.
>
> Replace temporary restrictions or scaffolding that the completed subsystem makes obsolete. Preserve explicit rejection of behavior that remains outside supported downstream capabilities.
>
> Do not substitute a simplified project-specific implementation for the upstream subsystem. Do not weaken the completion contract after discovering difficult behavior.
>
> Keep the already completed foundations stable unless a concrete defect or required dependency justifies changes.
>
> **Verify subsystem behavior and integration**
>
> Systematically account for applicable upstream tests, preserving their assertions and provenance. Distinguish original tests, adapted tests, reused compiler inputs, and new differential verification.
>
> Compare behavior with pinned Go across representative success, failure, and boundary cases. Use project demonstrations as integration gates, not as the sole definition of completeness.
>
> For configuration and discovery, if selected, distinguish correctly interpreting and discovering a project from having semantic support for every feature in that project. Do not silently ignore settings or files merely because downstream checking is incomplete.
>
> Require actual Linux x64 Native AOT verification and preserve existing subsystem, semantic, and project regressions. State platform limitations explicitly.
>
> Have independent Sol review challenge missing upstream behavior, unjustified exclusions, weakened assertions, and incorrect completion claims.
>
> **Optimize and finish autonomously**
>
> Own internal milestones, delegation, agent reuse, and validation strategy. Minimize repeated exploration, unnecessary context transfer, overlapping edits, and avoidable rebuilds. Keep process infrastructure proportional to its demonstrated value.
>
> Continue until the contract is satisfied or a material blocker requires a decision. Do not stop merely because an internal milestone or demonstration passes.
>
> Deliver committed changes, a clean working tree, reproducible verification, updated source mapping, and a subsystem completion assessment.
>
> Report remaining implementation and evidence gaps separately. Explain which recurring dependencies or restrictions were eliminated, what subsequent campaigns may now rely on, and whether observed results support continuing this subsystem-first strategy.
>
> Recommend the next high-value subsystem based on the dependency structure and remaining work.
