# First bounded type-checking milestone: user mandate

The following user request is preserved verbatim.

> Okay, let's commit the work and then continue:
>
> Proceed toward the first bounded type-checking milestone: an end-to-end path from source text through the existing parser and binder to semantic diagnostics, using faithfully ported upstream checker logic.
>
> Retain all established requirements, including Astra coordination, GPT-6.1 Sol subagents, direct Go-to-C# file mapping, readable C#, appropriate .NET libraries, xUnit, and mandatory Native AOT verification.
>
> First confirm the binder milestone’s Native AOT and regression evidence. Carry forward its documented limitations. Assess whether shared symbol-table identities, linked-flow-list identities, allocated empty tables, or partial source-position mapping affect the selected semantic path. Address relevant gaps without automatically reopening unrelated work.
>
> Investigate the checker’s dependency structure and select the smallest useful semantic slice that meaningfully tests porting feasibility. Develop the concrete scope, milestones, delegation, and verification strategy yourself. Favor a boundary that exercises real upstream name resolution and type relationships on both valid and invalid inputs.
>
> Port necessary upstream functionality rather than creating a separate miniature checker or special-casing fixtures. Preserve structural fidelity and make incomplete dependencies explicit. Do not let placeholder behavior silently produce successful checks.
>
> Include reference/name resolution and other semantic support only as required for the selected slice. Broad module resolution, project infrastructure, emit, incremental compilation, and language services remain outside scope unless a concrete dependency makes reconsideration necessary. Report such scope pressure before broadening the phase.
>
> Define the treatment of standard-library declarations and external dependencies. Distinguish unsupported inputs from inputs that were successfully checked.
>
> Compare meaningful semantic results and diagnostics against the pinned Go implementation. Agreement on valid inputs alone is insufficient. Establish that the chosen invalid inputs exercise the intended checker behavior and that the Native AOT executable agrees with upstream.
>
> Continue scanner, parser, and binder regression verification. Improve execution efficiency based on observed results from prior phases.
>
> At the checkpoint, report the supported semantic behavior, verification evidence, remaining dependencies and exclusions, observed effort, and the next most informative slice. Explain whether progress supports a route toward checking a representative real project; do not extrapolate whole-checker feasibility from a narrow success.
