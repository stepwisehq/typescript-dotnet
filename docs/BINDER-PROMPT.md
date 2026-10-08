# Binding phase user mandate

The following user instruction is preserved verbatim after the parser milestone
and its performance comparison:

> Awesome! Let's get the work committed and then move on:
>
> Proceed to the next bounded phase: port source-file binding and its necessary supporting structures.
>
> Retain all established requirements: Astra coordination, GPT-6.1 Sol implementation and review subagents, close upstream fidelity, direct Go-to-C# file mapping, appropriate .NET library use, readable C#, xUnit tests, and mandatory Native AOT verification.
>
> Before extending the implementation, review the parser milestone’s evidence. Confirm what the comparison actually establishes about AST structure and diagnostics. Assess the 29 unexercised parser diagnostic codes, address gaps that could undermine binding, and keep remaining exclusions visible. Avoid reopening completed work without a concrete reason.
>
> The objective is agreement with upstream on source-file binding: declaration and symbol relationships, scope organization, binder diagnostics, and control-flow structures needed by subsequent compiler stages.
>
> Determine the precise dependency boundary, internal milestones, and verification strategy yourself. Type checking, cross-file semantic resolution, module resolution, and code generation remain outside this phase. Do not silently broaden scope to accommodate dependencies.
>
> Verification must assess meaningful structural and behavioral agreement, not merely matching counts or successful execution. Identify which binder outputs are compared and which remain unverified. Keep expected behavior grounded in the pinned upstream implementation.
>
> Require the Native AOT executable to pass the applicable verification and preserve scanner and parser regression coverage.
>
> Continue improving delegation and execution efficiency using evidence from the completed phases. Minimize repeated investigation, unnecessary context, and coordination overhead while retaining sufficient independent review.
>
> At the checkpoint, report implemented scope, verification results, unresolved discrepancies, coverage limitations, and observed effort. Explain whether the result provides a sound foundation for an initial type-checker slice. Distinguish a completed bounded milestone from full binder parity.
