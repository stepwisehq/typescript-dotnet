# Bounded multi-file project campaign mandate

User mandate, preserved verbatim:

Proceed with bounded multi-file ES-module project checking, using checkpoint 2598ceb as the baseline.

The objective is the first useful project-checking workflow: load a small project from disk, resolve its supported dependencies, perform cross-file semantic checking, and report diagnostics through a reproducible Native AOT invocation.

Retain all established requirements: Astra coordination, GPT-6.1 Sol implementation and independent review, upstream fidelity, direct Go-to-C# source mapping, readable C#, appropriate .NET libraries, xUnit, and mandatory Native AOT verification.

**Define the project contract**

Investigate upstream program construction, configuration, module resolution, and cross-file checker dependencies. Establish a coherent bounded contract covering relative ES-module imports, exports, cycles, and cross-file diagnostics.

Determine the exact supported resolution mode, configuration options, file forms, and import/export behavior yourself. Use upstream semantics and choose a boundary that enables a useful project.

Support a documented project configuration and entry invocation. Explicitly reject unsupported options or behavior rather than silently ignoring them. A full compiler CLI is not required.

Package resolution, broad platform-specific module behavior, project references, incremental builds, emit, and language services remain outside scope unless a material dependency requires a decision.

Share the contract and proceed autonomously through internal milestones.

**Implement genuine project semantics**

Port the necessary upstream loading, resolution, program, and checker behavior. Do not substitute concatenated files, flattened scopes, or fixture-specific import handling for genuine multi-file semantics.

Assess file identity, shared program state, cross-file symbol relationships, cycles, and cache behavior. Include required dependencies while preserving existing source mapping and explicit unsupported boundaries.

Use the genuine pinned library environment established by the preceding campaign. Preserve the distinction between loading library declarations with skipLibCheck and verifying their bodies or supporting every library API.

**Demonstrate and verify**

Select a small representative project early and check it from disk under actual Linux x64 Native AOT. It should compose capabilities from earlier campaigns across module boundaries.

Require agreement with pinned Go under equivalent files, configuration, and libraries. Include successful checking and intentionally invalid variants that exercise cross-file failures. Compare complete diagnostics, including file attribution and locations, and meaningful semantic relationships.

Ensure the user-facing invocation communicates checking failure reliably. Document the supported command and configuration without requiring users to understand the verification harness.

Use independent review to challenge incorrect acceptance, scope leakage, resolution shortcuts, cycle behavior, and order-dependent results. Retain existing regressions and explicit unsupported-input controls.

**Execute efficiently and finish**

Own implementation order, delegation, internal milestones, and validation strategy. Apply prior process lessons and investigate avoidable rebuild or coordination overhead without weakening acceptance.

Continue until the contract is satisfied or a material blocker requires a decision.

Deliver committed changes, a clean working tree, reproducible project-checking and verification instructions, and a contract-based assessment.

State which projects can now be checked, the exact configuration and library boundaries, remaining limitations, and observed effort. Recommend the next campaign based on concrete barriers encountered in project use.

This campaign establishes type checking. JavaScript and declaration emit remain separate milestones.
