# Parser phase user mandate

Verbatim user direction authorizing the second bounded phase; the original
[requirements](./INITIAL-PROMPT.md) continue to apply.

---

Okay. Let's commit the progress. Then:

Proceed to the next bounded phase: port the parser and the AST support necessary to produce syntax trees and syntax diagnostics from source text.

Retain all existing project requirements and the Astra coordinator / GPT-6.1 Sol subagent arrangement. Native AOT remains a mandatory acceptance requirement.

First, review the scanner phase’s completion evidence and remaining exclusions. Address any gaps that would undermine parser work. Use the observed effort and working patterns to improve your execution strategy.

Determine the parser’s dependency boundary and develop your own milestones, delegation strategy, and verification approach. Preserve the pinned upstream revision and direct Go-to-C# source-file mapping. Bring over supporting code as needed, while making partial ports and departures explicit.

This phase covers parsing and the required AST support. Binding, type checking, module resolution, and code generation remain outside scope. If dependencies threaten that boundary, assess and report the issue before expanding the phase.

Acceptance must establish meaningful agreement with upstream on syntax-tree structure and syntax diagnostics, including malformed-input handling. Define and document the supported parsing modes and exclusions. Do not treat “does not crash” or “accepts valid input” as sufficient evidence.

Exercise the completed behavior in the Native AOT executable and retain scanner regression coverage. Develop the concrete verification methods yourself.

At the checkpoint, report the implemented scope, verification evidence, remaining gaps, and what this phase reveals about the feasibility and efficiency of continuing. Distinguish a completed milestone from full parser parity.
