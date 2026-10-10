# Reproduce shared type-system verification

Current acceptance status is reported in [TYPESYSTEM-STATUS.md](TYPESYSTEM-STATUS.md), the [assessment](TYPESYSTEM-ASSESSMENT.md), and the durable [proof](../validation/results/typesystem-verified.json). This guide records the reproducible acceptance inputs; it does not independently assert a successful final run.

Run from the repository root with the pinned Go toolchain and .NET 10 SDK. The [contract](TYPESYSTEM-CONTRACT.md) defines T1–T6 responsibilities, independently of the finite examples and compiler inputs. The [test catalog](typesystem-tests.json) separates complete original assertions, retained assertions, compiler input reuse, authored families, and external consumers.

```sh
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/run-typesystem-gate.py --preflight
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/run-typesystem-gate.py
# Record only after every stage passes and all drift flags are false.
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/record-typesystem-evidence.py
```

The gate rebuilds the genuine Go Program and semantic CLI observers, replays the complete original checker/mapper/casing bodies, runs new managed xUnit and full-record comparisons, and publishes actual Linux x64 Native AOT validation and ordinary CLI executables. It executes complete source records, shared assertions, two fresh reversed/repeated orders, and both ordinary disk projects. After new managed checks, and before new native publication, exactly one unchanged [expressions cascade](../tools/verify-expressions.sh) retains prior declarations, Program environments, resolution, configuration, and checker foundations. No extra new full retained-suite pass is added; historical scripts may already repeat suites, and execution totals are not distinct test counts.

The new validation xUnit project is [TypeSystem.Tests](../tests/TypeSystem.Tests/TypeSystem.Tests.csproj). All fixed project paths are checked before the gate; project controls remain in Checker.Tests, with no invented Projects.Tests project. Expectations come exclusively from the pinned Go observer. Public diagnostics retain owners, actual filenames, byte ranges, message arguments, chains and related information; type and signature displays use actual public APIs. Optional finite `Relations` records select actual owned identifier types and call public `IsTypeAssignableTo` on both implementations. Invalid selectors reject input; absent-field output remains unchanged. No private relation graph is required.

After the gate publishes the CLI, ordinary use requires no observer:

```sh
artifacts/typesystem-validation/cli-native/tscheck --config examples/typesystem/event-client/tsconfig.json --json
# exit 0
artifacts/typesystem-validation/cli-native/tscheck --config examples/typesystem/event-client-invalid/tsconfig.json --json
# exit 1, six complete source diagnostics
```

The configs use actual strict ES2022 options and bundled ES2022 libraries, with default `skipLibCheck:false`; no hidden library/profile substitution occurs. Native publication uses .NET 10, for example:

```sh
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release -r linux-x64 -p:PublishAot=true -o artifacts/typesystem-validation/cli-native
```

The gate already performs this publication, so reproducing a successful gate needs no additional build. A cached C compiler is selected when the host has no clang.

The finite compiler selection preserves extracted source bytes and actual directive variants using the source-reviewed regular compiler parser. Config ingestion is an explicit adaptation from the original harness direct-option path. Emit outputs and complete types/symbol baseline traversals are not claimed. New shared casing coverage preserves the entire `TestJSCasing` body and all 26 table assertions, with source-qualified UTF-8/WTF-8 handling and exact pinned Unicode mapping/range data. The two checker and three mapper bodies are retained originals, not new ports.

The gate binds source, corpus/provenance, harness/tests, the contract/guide, and the completed core/support source reconciliation inventories. Runtime-only catalog fields use a stable obligation projection; root narrative assessment/status/STATE/README/roadmap/source-map documents remain outside that fingerprint. Historical corpora/proofs must remain byte-identical. The fixed current retained CLI context is `DECLARATIONS_CURRENT_CHECKING=1`; explicit prior authored profile-control retirements and their source-grounded replacements remain recorded, without silent skips or weakened diagnostics.

Final artifacts are under [typesystem-validation](../artifacts/typesystem-validation): the complete gate log/report, original/managed/native full-record comparisons and raw streams, selected xUnit TRX, shared/order controls, ordinary CLI evidence and native binary hashes. The recorder checks every stage, source/asset/history/upstream/obligation drift, and actual Linux x64 ELF executables before writing a new proof. It never overwrites historical proof.
