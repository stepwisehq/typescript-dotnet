# Original user prompt

The text below is preserved verbatim from the initial task. Later clarifications
are recorded in [START-HERE.md](../START-HERE.md).

---

You are the planner and coordinator for an early feasibility study: porting the Go implementation of the TypeScript compiler’s scanner to C#.

The longer-term possibility is a full compiler port, but this phase is limited to the scanner and the supporting code necessary to evaluate it. Do not expand into a full compiler implementation.

Your objective is to produce a working, verifiable port and evidence about the practicality and cost of continuing.

**Your role and delegation**

The main Astra session owns planning, scope, coordination, significant technical decisions, and assessment of results.

Delegate substantive implementation, investigation, testing, and review work to GPT-6.1 Sol subagents. You decide how to divide tasks, when parallel work helps, and when to reuse or replace agents. Keep delegation proportional to the work; creating more agents is not itself progress.

Use the requested models explicitly where supported. If model selection or subagents are unavailable, report that limitation rather than silently substituting a different arrangement.

**Porting constraints**

Keep the port as close to the upstream Go implementation as reasonably possible. Preserve algorithms, control flow, function boundaries, names, and organization wherever practical in C#.

Maintain a direct, discoverable mapping from each upstream Go source file to its C# counterpart. Prefer one source file to one destination file. Develop a simple mapping convention and a way to track completeness, partial ports, and necessary exceptions. Choose the mechanism yourself.

Make language adaptations where required for correctness or the requirements below, while avoiding unnecessary redesign and speculative optimization. Record meaningful departures from upstream and their reasons. Distinguish ported code from new harnesses or compatibility support.

Pin the upstream revision so implementation and tests refer to a stable source. Preserve applicable license notices.

**C# and .NET requirements**

.NET Native AOT compatibility is a mandatory acceptance requirement. The port must successfully publish with Native AOT, and the resulting native executable must pass the applicable behavioral verification. A successful JIT build or an assessment that the code appears AOT-compatible is insufficient.

If the environment prevents Native AOT verification, treat that as a blocker to acceptance. Continue useful work where possible, but do not mark the phase complete or relax the requirement.

Use .NET libraries and standard facilities where appropriate, provided their semantics match the upstream behavior. Avoid unnecessarily recreating functionality already available in .NET. Where behavior differs, preserve compatibility and document the adaptation.

Use xUnit for automated tests. The port and its executable validation target must support Native AOT; the xUnit runner itself does not need to be AOT-published.

Produce clean, readable C#. Favor clear naming, straightforward control flow, consistent formatting, and comments that explain non-obvious behavior or porting decisions. Avoid both awkward mechanical translation and cosmetic refactoring that makes comparison with upstream harder.

Choose concrete implementation techniques yourself. Resolve tensions between readability, .NET conventions, and structural fidelity with the smallest justified adaptation, keeping correctness and traceability intact.

**Efficiency**

Optimize for verified progress per unit of total effort, including model usage, coordination overhead, elapsed time, and human intervention.

Minimizing turns and tokens matters, but correctness and useful evidence are constraints. Do not achieve apparent efficiency by skipping necessary verification, hiding uncertainty, or declaring incomplete behavior finished.

Develop your own execution and delegation strategy. As work progresses, identify where effort is being wasted and improve the strategy using observed results. You may change task boundaries, context distribution, review practices, validation methods, or other working arrangements when evidence supports doing so.

Keep this optimization lightweight. Avoid building elaborate process infrastructure before it is useful. Track actual usage when available; clearly distinguish measurements from estimates. Do not infer subscription cost from unsupported assumptions about tokens.

Maintain enough concise project state that work can resume without reconstructing prior decisions or repeating investigations.

**Verification and feasibility**

Investigate the available upstream tests and determine how best to verify the scanner independently. Existing tests are valuable evidence, but passing a small suite alone does not establish completeness.

Develop acceptance criteria appropriate to this bounded phase, including the mandatory requirements above. Use upstream behavior as the reference, and assess relevant differences between Go and C# semantics.

Never change expected behavior merely to make the port pass. Make unsupported behavior, exclusions, and incomplete dependencies visible. Distinguish implementation defects from harness defects and intentional scope limits.

Use Sol subagents for independent review where it materially improves confidence, including review of behavioral fidelity, readability, and Native AOT compatibility. You remain responsible for resolving findings and deciding whether the evidence supports acceptance.

**Autonomy and stopping**

Begin by inspecting the workspace and upstream source, then establish a bounded plan and proceed with implementation. Choose routine technical and organizational details yourself.

Do not require me to manage the agents or approve every step. Ask only when a material ambiguity, access limitation, or scope decision genuinely requires my input.

Establish a practical checkpoint and stopping condition for this feasibility phase. If scanner isolation requires unexpectedly broad compiler work, report the dependency problem rather than quietly expanding scope.

Reaching a checkpoint or stopping condition does not mean the acceptance requirements have been met. Clearly distinguish completed, incomplete, and blocked outcomes.

At the checkpoint, deliver the implementation, reproducible build and verification instructions, source mapping, and a concise assessment covering:

- What works and what remains incomplete.
- Test results, Native AOT verification, and their limitations.
- Significant language adaptations, .NET library choices, and dependency costs.
- Observed usage and human intervention, where measurable.
- Which working strategies helped or wasted effort.
- Whether another bounded phase is justified, and what uncertainty it should resolve.
Treat successful scanner work as evidence about the scanner port and the working process—not proof that the full compiler will be equally tractable.
