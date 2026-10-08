# Binder implementation checkpoint

- Status: accepted bounded source-file binder; independent reviews and final actual Native AOT gate passed. No active work or executable edits pending. Evidence: [binder-verified.json](../../validation/results/binder-verified.json).
- Pin: maintained TypeScript `fed0bf24149fb1ed36039212648bafdafc1ea10e`, Apache-2.0. Previous accepted parser/benchmark checkpoint `de25b66`.
- Scope: complete source-file `Binder.BindSourceFile(SourceFile)` algorithms. Symbols, declarations, local/member/export tables, local/export associations, container chains, ordered binding diagnostics and cyclic/shared control-flow graphs are retained.
- Excluded: `nameresolver.go` and `referenceresolver.go` (binder never calls them), checker/type evaluation, cross-file/global/name/reference resolution, module resolution, emit, incremental rebinding, services and general transform-facts implementation.
- Function inventory: binder.go 167 functions = 163 emitted, getBinder plus two ActiveLabel accessors adapted manually, putBinder omitted with pool return. No grammar/dispatch handlers are stubbed.
- Source mapping: [Binder.cs](../../src/TypeScript.Scanner/Binder/Binder.cs) maps binder.go alone, including originating enums/state declarations. Separate [binder-port](../../tools/binder-port) scaffold/templates/patch preserve regeneration. AST owner maps symbols/flow/base state/utility closure to originating counterparts.
- Owned additions: Binder/Binder.cs; Runtime/BinderCompat.cs; TsPath/Extension.cs. Bounded existing-file changes: Scanner/Scanner.cs, Scanner/Utilities.cs, Runtime/ParserCompat.cs (GoSlices.Index), TsPath/Path.cs (partial class).
- Scanner.go additions: GetScannerForSourceFile, ScanTokenAtPosition, GetRangeOfTokenAtPosition, getErrorRangeForArrowFunction, findOriginatingJSDocSatisfiesTag, GetErrorRangeForNode, GetECMALineStarts, GetECMALineOfPosition, GetECMAEndLinePosition. GetTokenPosOfNode was already included.
- Scanner/utilities.go adds DeclarationNameToString with internal raw byte-string and public decoded/byte wrappers. Still excluded: IdentifierToKeywordKind, GetTextOfJSDocComment, IsIdentifierText, IsIntrinsicJsxName. Handwritten bridge helpers are explicit adapters.
- Required SourceFile.ECMALineMap lazy ECMA line-start cache is included by AST owner. Full SourceFile.GetPositionMap and broader source-file position-map APIs remain excluded.
- TsPath/extension.go maps only RemoveFileExtension and extensionsToRemove. Typed rooted-path/FileNameStem wrappers collapse at this boundary; no path resolution is imported.
- AST state: exact embedded declaration/export/local/flow bases flattened into existing typed payload interfaces; compiler state separated from syntax metadata. Symbols/tables/flow/list classes retain shared object identity; nil/empty table/declaration distinctions and flow edge ordering remain observable.
- Byte contract: AST text and symbol/table names are Latin1 strings representing Go UTF8 bytes, including invalid FE internal-name prefix. Public filename options remain .NET strings; bindSourceFileAsExternalModule encodes filename once, coalescing default C# null to Go empty, before known-extension removal.
- IDs: atomic ulong node/symbol counters follow increment/CAS. Private-name signed integer formatting preserves the pinned Go platform's 64-bit int. Oracle normalizes only known generated counter components after asserting actual owner identity, and sorts those unordered map keys by syntax-owner identity.
- Publication: BindOnce is synchronized/non-reentrant, done after callback failure and IsBound published only after success. Repeated binding preserves syntax/symbol/table/flow/list references and diagnostics.
- Allocation adaptation: a fresh managed Binder replaces Go sync.Pool; GoObjectArena.New uses static new() with no reflection. Managed slice append/slice behavior retains earlier documented header-copy adaptation. No optimization or performance investigation performed.
- Integration reason: bind/container/children dispatch is mutually recursive across most functions. Root approved compiling the actual component together after a clean low-level symbol/flow/model milestone, avoiding fake partial handlers.
- Resolved fidelity issues: AST utility enum-prefix mapping, Visitor callback type, nil Go-map reads, raw diagnostic text, default filename and full-width private IDs. Fixes are in owning generators/templates/patches or narrow handwritten bridges.
- Final build/gate: [verify-binder.sh](../../tools/verify-binder.sh) exited zero on 2026-10-08, rebuilding the frozen sources and Native AOT harness. Ten binder scaffold sanity tests and byte-identical scratch Binder.cs regeneration passed before freeze.
- Final differential evidence: 118/118 Go/Native AOT graphs matched (5,182 nodes, 858 symbols, 430 tables, 700 flows, 150 FlowLists, 41 diagnostics); 122/122 binder xUnit checks passed, including concurrent publication, failed Once no-retry, default-options names and concurrent repeated binding identity. Parser regression 471 native/475 xUnit; scanner regression 1,276 native/1,287 xUnit plus 178 helper results. Accepted source SHA256 `0386f000d85382e8331fa97dee83e50dd469bdca4b90bead795075a18436bf18`.
- Reviews: [review-binder-support.md](./review-binder-support.md) covers AST/model/helpers plus independent Go/native oracle; [review-binder.md](./review-binder.md) is support owner's independent algorithm/emitter/scanner-span review. No unresolved algorithm blocker.
- Reproducibility: final Binder.cs scratch emit + patch + LF formatting is byte-identical to reviewed source. Scanner/TsPath/runtime bridges are handwritten, not generated. Unsupported scaffold forms fail fast; resolver emission is outside its scope.
- No active work. Root records the accepted bounded assessment/state; [source-map.json](../../source-map.json) retains full versus partial source scopes, runtime adaptations and resolver exclusions. Sources, tools, tests and corpus remain frozen.

Regenerate to scratch first:

```sh
GO111MODULE=off tools/.cache/go/bin/go test ./tools/binder-port
tools/.cache/go/bin/go build -o /tmp/binder-port tools/binder-port/main.go
/tmp/binder-port upstream/TypeScript/tsc/internal/binder/binder.go /scratch/Binder.cs
python3 tools/binder-port/patch.py /scratch/Binder.cs
dotnet run --project tools/CSharpFormat/CSharpFormat.csproj -- /scratch/Binder.cs
cmp src/TypeScript.Scanner/Binder/Binder.cs /scratch/Binder.cs
```
