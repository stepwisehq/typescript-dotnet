# Foundation completion campaign — user mandate

Okay, let's commit the work and move forward. We are going to shift gears a bit and try out completing each subsystem a layer at a time working our way up from the foundation:

Make the next campaign completion and verification of the scanner, parser, and syntax-AST foundations.

We have established feasibility through several successful capability campaigns. The objective now is to reduce foundational uncertainty and repeated downstream rework by completing the agreed foundation surface and systematically incorporating upstream tests.

If callable work is already underway, preserve it in a coherent checkpoint before switching. Do not discard completed work or mix partially verified changes into the foundation baseline.

Retain all established requirements: Astra coordination, GPT-6.1 Sol implementation and review subagents, close upstream fidelity, direct Go-to-C# file mapping, clean and readable C#, appropriate .NET libraries, xUnit, and mandatory Native AOT verification.

**Define the foundation boundary**

Inspect the pinned upstream revision and existing port. Establish a completion contract for scanning, parsing, syntax-node representation, and their required supporting behavior.

Determine the exact boundary yourself. Distinguish syntax-AST foundations from binder/checker-owned state, transformation machinery, and other later-stage functionality. Include necessary foundation dependencies without silently expanding into later compiler stages.

Account for the upstream files, functions, modes, and generated structures within the boundary. Identify complete implementations, partial implementations, missing behavior, and necessary language adaptations.

Share the contract in a concise update and proceed without waiting for approval unless a material scope decision requires my input. Do not define the boundary merely around what already passes.

**Incorporate upstream tests systematically**

Inventory all applicable upstream tests at the pinned revision. Port direct foundation tests into xUnit, preserving their behavioral assertions and recording their upstream provenance.

Distinguish:

- Original upstream tests whose intended assertions are preserved.
- Adapted tests with documented changes to what they establish.
- Upstream compiler inputs reused for foundation comparisons.
- Additional tests and differential comparisons created for this port.

Do not report a full compiler test as passing when only its input has been scanned or parsed.

Account explicitly for exclusions, skipped tests, unsupported cases, and tests blocked by later-stage dependencies. Explain each category sufficiently for independent review. Do not silently weaken assertions or change expected behavior to obtain agreement.

Develop the concrete test organization, execution strategy, and comparison mechanisms yourself.

**Close implementation and evidence gaps**

Reconcile implementation coverage with test coverage. Passing every available direct test is necessary where applicable, but does not by itself establish that every in-scope behavior was ported.

Carry forward historical limitations, including unobserved diagnostics, parsing-mode gaps, source-position behavior, and identity or helper behavior relevant to the foundation. Resolve them where they fall within the contract; otherwise retain their justification and downstream implications explicitly.

Use the pinned Go implementation as the behavioral reference. Establish broad agreement for valid and malformed inputs, syntax structure, diagnostics, and other observable foundation behavior. Make comparison omissions explicit rather than treating them as equivalence.

Avoid speculative redesign, fixture-specific behavior, permissive placeholders, or replacement algorithms that break upstream traceability.

**Verify and review**

Actual Native AOT publishing and execution are mandatory acceptance gates. The native executable must pass the applicable foundation verification. State the verified platform and distinguish it from any unverified platforms.

Preserve existing binder and checker regressions so foundation changes do not invalidate accepted capabilities.

Use independent Sol review to challenge both correctness and the completeness claim: missing upstream functionality, weakened test assertions, unjustified exclusions, and blind spots in comparisons.

Keep review and validation proportional to risk while performing comprehensive campaign acceptance before declaring completion.

**Execute efficiently and finish**

Own internal milestones, delegation, and strategy adjustments. Minimize repeated investigation, unnecessary context transfer, redundant validation, and coordination overhead. Maintain concise resumable state and improve the process using observed results.

Continue until the completion contract is met or a material blocker requires a decision. Do not stop simply because an internal slice is complete.

Deliver committed changes, a clean working tree, reproducible verification instructions, updated source mapping, and a contract-based acceptance assessment.

Report upstream test inventory totals by disposition, implementation completeness, Native AOT results, remaining limitations, and observed effort. Explain precisely what downstream work may now assume about these foundations and recommend where to resume capability development.

Call the campaign complete only when its contract is satisfied. Distinguish that result from exhaustive correctness or full compiler parity.

## Subsequent user emphasis

Just to put extra emphasis; I feel we should let the upstream subsystem tests guide us. If we haven't ported and satisfied them for these layers then that's likely a good place to start.
