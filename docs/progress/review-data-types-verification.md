# Data-type contract, admission and oracle audit

Status: verification-owner audit complete; final frozen-source gate passed. This audit compares the
independently authored contract/corpus and original Go discovery against the
managed adapter. Core/support peer reviews remain separate.

## Evidence inspected

[Contract](../DATA-TYPES-CONTRACT.md), [case manifest](../../validation/data-types-corpus/manifest.json),
[coverage](../../validation/data-types-corpus/coverage.json),
[admission](../../src/TypeScript.Scanner/Checker/Admission.cs), original
[oracle](../../tools/checker-oracle/main.go), original private
[observations](../../tools/checker-oracle/data_access.go), original reflective
[discovery](../../tools/checker-oracle/data_graph.go), static
[mirror](../../validation/Checker.Validation/DataGraph.cs),
[runner](../../validation/Checker.Validation/Runner.cs), source-generated
[contracts](../../validation/Checker.Validation/Contracts.cs), and
[gate](../../tools/verify-data-types.sh).

Build18 isolated adapter matched all14 tiny and78 matrix rows (92total), including
every JSON key, type, list order, value and identity edge. Its library SHA256 was
`3d413f82ccac60a00bad643b5d369f988a2b7a539c108f2f69604ca1746b1304`;
adapter SHA256 `0235d39e93b730638b562ee8be114c941c92d3202ebcb630bce8d5846cded839`.
Binary and fixture hashes stayed stable during each run. Current78 admission
controls (7admitted,71rejected),20 actual Go utility observations and10 authored
public API assertions passed. These are managed integration results, not native
acceptance or proof for later source changes.

## Matrix and admission

| Row | Independent interaction checks | Disposition |
| --- | --- | --- |
| D1 | Forward/scoped/escaped aliases, legal type/value sharing, wrong value/type meanings, qualified first/second-component and globalThis namespace failures, suggestions and duplicate aliases | All compared; unknown/invalid names produce upstream diagnostics rather than Unsupported |
| D2 | Multiple compatible/conflicting bases, own override versus inherited comparison, reversed merges, optional/readonly conflicts, alias/intersection/any bases and derived hidden-this identity | All compared; synthetic reference/type-parameter/index dependencies retained |
| D3 | Direct/mutual property recursion and recursive compatible/mismatching relations versus naked alias and heritage cycles | Distinct actual diagnostics, shared/circular identities and repeated declared-type results compared |
| D4 | Common/absent/optional properties, nullable reads, fresh versus indirect union initialization, three-arm deferred fields, ten-arm discriminant map, Cartesian discriminant coverage/mismatch, scalar absorption | Actual property/link/cache/result structure compared; no source branching claim |
| D5 | Combined and conflicting members, never reduction, missing dot reads, distributed and nested composites, optional/required and scalar reductions | Exact constituents/flags/origin/reduction/cache fields and diagnostic chains compared |
| D6 | Named shorthand/context, width/freshness, readonly and weak types, nested nullable reads, object keyword and local globalThis shadow | Actual resolution identities and directed relations compared; builtin globalThis value reads remain explicitly excluded |
| D7 | Source generics/this/signatures/indexes/keyof/operators/classes/modules/writes/branching rejected; invalid supported names/cycles/bases admitted; builtin global type declarations | Authored rejection expectations independent of Go semantic oracle; exact per-case globals preserved |

Admission traverses the actual parser AST, including both heritage node shapes;
it does not use oracle success or diagnostic counts as a whitelist. Typed static
properties are the contract: untyped members, member initializers and methods are
outside it. Existing scalar/object controls remain byte-preserved; ten explicit
retirement entries exactly match current test selection (two historical object
changes, eight newly admitted data-type controls). A named xUnit regression checks
each retired request is now admitted and has a visible reason.

## Observation and omission audit

- The Go host builds the real pinned Program and Checker, asserts one source file,
  and uses fixed noLib/noResolve/ESNext/strictNullChecks options. Neither admission
  nor a managed field manifest filters original results. File, binder and global
  diagnostics are separate; source builtin-name declarations can change globals.
- Original payload fields are discovered reflectively and unknown fields/types
  fail. Managed fields are manually typed, JSON uses explicit generated type info,
  and reflection serialization is disabled. Type/symbol/table/alias/mapper/index
  identities are preregistered before recursion; ordered lists remain ordered.
- Both runtimes perform diagnostics, selected type/symbol queries with formatting,
  directed relations, globals, repeated declared types/widening, logical node
  cache reads, then the indexed type/symbol work queues in the manifest order.
  Queue reads can resolve public members; fields snapshot their queue visit, not
  an imaginary simultaneous final state. Absent caches are never forced merely
  for observation. Final type flags are refreshed after the queue.
- Alias inheritance preserves hidden this/reference/constraint/target/argument
  links. Any heritage preserves real IndexInfo and accepts a fresh extra property
  with an undeclared dot read of type any. Required finite-property-key origins
  are exposed through the existing propertiesTypes map: exact include/includeOrigin
  keys and canonical values are associated with their actual owner. Ten entries
  and four IndexType payloads across the corpus (two in the focused best-overlap
  case) are observed, without calling getIndexType to seed them.
- The actual internal globalThis export cache is populated in the qualified missing
  member case (2694), with table identity and nil type-only map retained. Physical
  untouched paged symbolNodeLinks slot presence is excluded because process-global
  AST-ID page placement differs; actual resolved symbols and logical node/type-node
  cache presence are compared. Earlier20 fixed-ID store probes remain in the gate.
- Opaque hash bits differ by the documented managed full-preimage adaptation.
  Populated reference keys are validated against actual cache values, including
  full association checks; composite intern membership and alias/origin/reference
  key equality use actual helpers. Unpopulated key families are not inferred covered.
  Cache-only descriptor collisions fail rather than merge identities.

## Findings and dispositions

The audit/integration exposed an authored heritage downcast, missing finite-key
cache roots, and two index-derived failure-label mistakes. Heritage validation now
handles both actual node forms, existing cache edges are observed, and failure
packets include exact paths plus actual owner/store/field names. No expected
semantic value or diagnostic was changed to repair an implementation difference.

Earlier blanket nil/empty prose was too broad: nullable model fields, tables/maps,
signature/index slots and per-diagnostic argsNull remain distinguished. Existing
top-level diagnostic channels normalize nil slices to empty result arrays; that
historical observation boundary is preserved and now documented. Type wrapper
owner/id/data backreferences are excluded; relevant payload fields are retained.

Actual retained Mapper, mapped-symbol and merged-symbol edges are zero. This does
not establish those variants; hidden mapping is evidenced by reference arguments,
targets/results and selected original call paths. Other checker stores, inference,
source-excluded signatures/generics, full source position maps, historical unobserved
diagnostic branches and shared FlowList aliasing remain explicitly unverified.

No unresolved admission/oracle omission blocker was found within D1–D7. Final
source/core/support review, formatting/inventory freeze and actual Native AOT
completed successfully. The 82.979 second frozen gate passed 92 rich comparisons,
all 262 checker xUnit tests, current boundary/API checks and every earlier native
regression. [Durable evidence](../../validation/results/data-types-verified.json)
ties those results to the exact final source/assets/binary. Historical 45 scalar/43 object
fresh Go outputs remain byte-identical; five historical evidence records remain
unchanged against baseline1f7c953. No stronger unverified-branch claim is added.
