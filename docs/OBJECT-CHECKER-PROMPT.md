# Object checker phase mandate

Preserved verbatim from the user request authorizing this phase:

Great! Let's commit the changes and continue:

Proceed to the next bounded checker phase: object types, property access, and structural assignability.

Retain all established requirements and the Astra coordinator / GPT-6.1 Sol subagent arrangement, including upstream fidelity, direct source-file mapping, readable C#, appropriate .NET libraries, xUnit, and mandatory Native AOT verification.

Use the completed scalar checker milestone as the foundation. Investigate upstream dependencies and choose a coherent, useful boundary within this next area. Develop the implementation sequence, delegation, and verification strategy yourself.

The objective is meaningful agreement with upstream when constructing object types, resolving their properties, and determining compatibility between them. Include both successful operations and semantic failures. Select coverage that tests interactions between features, rather than only isolated happy paths.

Assess previously unexercised binder identity and table behaviors where the selected checker paths depend on them. Address relevant gaps without broadly reopening accepted work.

Continue porting actual upstream algorithms. Avoid fixture-specific behavior, substitute checker implementations, or permissive placeholders. Unsupported paths must remain distinguishable from successfully checked inputs.

Keep generics, broad standard-library support, module resolution, emit, and language services outside scope unless a concrete dependency requires reconsideration. If library declarations are necessary, propose a bounded approach faithful to upstream and explain its implications before expanding scope.

Keep the existing missing-global diagnostics separately identified in verification results. Do not suppress diagnostic categories broadly to obtain agreement.

Require meaningful semantic and diagnostic comparison against the pinned Go implementation, including execution under Native AOT. Preserve existing scanner, parser, binder, and checker regression coverage.

Continue refining execution efficiency using observed results. At the checkpoint, report supported behavior, verification evidence, unsupported paths, remaining risks, and observed effort. Recommend the next slice based on the dependencies and uncertainties actually encountered.
