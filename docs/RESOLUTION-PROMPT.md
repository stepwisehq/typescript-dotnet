# Source-import module-resolution campaign mandate

User clarification: full module resolution may resolve dependencies that the checker
or other higher-level subsystems cannot yet handle. That is acceptable; those systems
will be addressed later.

Proceed with completion of the remaining source-import module-resolution subsystem,
using configuration checkpoint `37bd57d` as the baseline.

Continue the subsystem-first strategy. The objective is faithful upstream resolution
behavior that subsequent project work can rely on, extending beyond the needs of
existing demonstration projects.

Retain all established requirements: Astra coordination, GPT-6.1 Sol implementation
and independent review, upstream fidelity, direct Go-to-C# source mapping, readable
C#, appropriate .NET libraries, xUnit, and mandatory Native AOT verification.

## Establish the completion contract

Inspect the pinned upstream resolver, existing implementation, configuration
integration, and required host services. Account for the remaining files, functions,
resolution modes, and behavior.

Define the contract from upstream responsibilities rather than a selected set of
imports that currently pass. Assess package metadata, configuration-driven resolution,
supported file forms, filesystem behavior, and resolver state wherever they belong to
the subsystem.

Determine implementation order, dependency boundaries, and internal milestones yourself.
Include proportionate supporting dependencies. Explain material expansion into adjacent
subsystems before undertaking it.

Share the contract and proceed autonomously. Distinguish functions merely inventoried
from functions implemented and verified.

## Complete upstream behavior

Port the actual upstream resolution algorithms and integrate them with the completed
configuration and project-loading work.

Remove temporary resolver restrictions that the completed subsystem makes obsolete.
Do not substitute package-specific shortcuts, simplified search rules, or permissive
fallback behavior.

Preserve upstream source mapping and document necessary .NET adaptations.

Separate resolution capability from semantic capability. A dependency may resolve
correctly while containing unsupported checker behavior. Report that distinction
accurately; do not distort resolution or silently omit files to accommodate checker
limitations.

## Verify independently

Systematically incorporate applicable upstream tests, preserving assertions and
provenance. Distinguish original tests, adapted tests, reused fixtures, and new
differential evidence.

Compare meaningful resolution results against pinned Go under equivalent configuration,
package metadata, files, and host assumptions. Assess success, failure, ambiguity,
ordering, and cache behavior as applicable to the contract.

Make platform assumptions explicit. Claim cross-platform behavior only where verified.

Use realistic project graphs as integration gates while retaining resolver-level
verification independent of downstream checker support. Establish that configuration
is consumed correctly and resolved dependencies reach the project graph as intended.

Have independent Sol review challenge missing upstream behavior, unjustified exclusions,
host mismatches, comparison blind spots, and incorrect completion claims.

Require actual Linux x64 Native AOT execution and preserve all accepted configuration,
project, semantic, and foundation regression gates.

## Execute efficiently and finish

Own delegation, agent reuse, internal milestones, and validation frequency. Apply prior
process lessons to reduce repeated investigation, redundant builds, unnecessary context,
and coordination overhead.

Continue until the contract is satisfied or a material blocker requires a decision.
Do not stop at an intermediate feature milestone.

Deliver committed changes, a clean working tree, reproducible verification, updated
source mapping, and a completion assessment.

Report implementation completeness and verification limitations separately. Identify
which project restrictions were removed and which remaining obstacles belong to program
construction, checker semantics, libraries, or other subsystems.

Recommend the next high-value subsystem based on remaining dependencies and concrete
project failures.
