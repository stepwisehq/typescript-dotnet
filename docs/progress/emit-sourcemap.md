# Emit source-map package

Baseline: `e3131a5b`; pinned Go source revision
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

Source implementation is complete and awaiting coordinated integration build,
independent review and the integrated managed/Native AOT acceptance. No acceptance
is inferred from source presence. No build or behavioral run was performed by this
lane. The coordinator owns those runs.

## Source mapping

| Upstream internal/sourcemap | C# source | Responsibility |
| --- | --- | --- |
| generator.go | ../../src/TypeScript.Scanner/SourceMap/Generator.cs | Generator, RawSourceMap, source/name/content ownership, pending mappings, VLQ emission, ordered JSON and inline data URL |
| decoder.go | ../../src/TypeScript.Scanner/SourceMap/Decoder.cs | Mapping, MappingsDecoder, DecodeMappings, exact decoder state/errors and Go64 VLQ decoding |
| lineinfo.go | ../../src/TypeScript.Scanner/SourceMap/LineInfo.cs | ECMALineInfo and byte line slicing |
| source.go | ../../src/TypeScript.Scanner/SourceMap/Source.cs | Source text/file/line-map interface |
| util.go | ../../src/TypeScript.Scanner/SourceMap/Utilities.cs | trailing sourceMappingURL comment discovery |
| source_mapper.go | ../../src/TypeScript.Scanner/SourceMap/SourceMapper.cs | Host, DocumentPosition, complete document mapper, typed source-map parsing, external/inline lookup |

All upstream functions are represented. Package-level functions live in the
partial `SourceMaps` static class; `NewGenerator` and `CreateECMALineInfo` live on
their corresponding types. There are no deferred package branches or stubs.
Printer/compiler output wiring belongs to subsequent integration and is not
represented as completed by this package checkpoint.

## Adaptations and ownership

Names, paths, source content and mapping bytes use `ByteStringValue`; retained keys
use `ByteStringKey`. Source text/line slicing borrow immutable `ByteStringView`.
Generator owner arrays use established `Slice<T>` headers, preserving returned
`Sources()` header lengths when the generator appends. RawSourceMap retains required
independent source/name/content array clones, including nil content versus present
empty content and null content elements. JSON materialization is an actual external
serialization/parsing boundary using the existing ordered AOT-safe JsonCodec.
Explicit ordered fields preserve upstream JSON field order and omission rules;
Known fields decode through the typed raw-field adapter; unknown fields are skipped
lexically, including huge numeric values, while duplicate names are rejected even
in skipped objects. A small JsonDocument numeric check preserves int token spelling
(`3.0` is rejected).
The generated JSON itself uses no reflection or source-generated serializer.

Go source/name/line/UTF16 numeric deltas are retained as signed 64-bit values for the
pinned platform. Decoder shifts >=64 yield zero, rather than .NET's masked shift.
Managed buffer/index limits are 32-bit: source-map columns/lines are saturated only
when invoking `allowEdits` position conversion, preserving its clamping behavior.
An exact borrowed `ReadOnlySpan<int>`/`ByteStringView` Scanner overload forwards the
existing private algorithm, avoiding source-map host whole-buffer copies/encoding.
Byte positions and UTF16 columns remain separate throughout.

`Mapping` and `MappedPosition` are readonly record structs. Upstream decoder arena
pointers are never mutated after capture; neither pointer identity nor arena address
is observed by any package consumer. Value captures preserve independently retained
mappings without a separate CLR heap object for every arena element. Mapper records
still copy on source-mapping production, as upstream does. Mapper arrays/lists remain
ordinary managed storage, and sorting uses the existing exact GoSliceSort implementation
to preserve equal-position ordering before deduplication. No speculative pool or
alternate mapping algorithm was introduced.

## Test dispositions

The shared xUnit/Native assertion source is
[SourceMapChecks.cs](../../validation/Emit.Checks/SourceMapChecks.cs).
It ports all 32 generator functions and all 10 mapper functions with original
expected objects, strings, errors and position assertions. `t.Parallel` becomes
sequential execution in the shared static AOT assertion harness; the mapper's one
nested table still executes all four source variants by both absent/empty roots.
Report **42 original functions / 49 original leaf rows**, separately from **19
authored controls**. The xUnit Theory exposes each original function by name and invokes the same
bodies the Native harness registers. Authored controls have a separate Fact and
a local returned count, with no mutable shared assertion state.

Authored controls cover all ten decoder failure categories, independent retained
mapping/state capture, >=64 VLQ shifts, required RawSourceMap clones/content/JSON
omission and base64 output, astral UTF8/UTF16 position conversion, malformed typed
JSON/duplicate names/unsupported embedded content, skipped unknown numeric fields, actual inline lookup, and Unicode
space/trailing-comment stopping. Query-position comparisons widen before subtraction
to preserve Go64 behavior at signed32 boundaries. These controls supplement upstream assertions;
they are not counted as additional upstream test functions.

## Independent review checkpoint

Independent Sol review is clear with no open findings:
[bound review](../../artifacts/emit/source-review/sourcemap.json). It covers all six
production files, the exact borrowed scanner overload, original shared assertions
and the thin xUnit Theory wrapper. All recorded reviewed file hashes match the
source at this checkpoint. Two concrete findings were fixed: skipped unknown huge
JSON numbers and signed32 query-position subtraction overflow. Their exact witnesses
remain in the authored controls.

The reviewer executed pinned Go's complete `internal/sourcemap` original suite:
42 original functions / 49 leaf rows passed. That confirms preserved upstream
expectations; it is separate from the coordinated managed/Native AOT runs and does
not establish whole-emitter acceptance.

## Accepted prerequisite gate

The coordinated managed90-case emit suite and actual Linux x64 Native executable
passed, executing all42 original map functions and19 authored map controls within
the shared78-original/30-authored suite. Retained checker/type-system/selected
no-emit regressions passed. [Durable evidence](../../validation/results/emit-foundation-verified.json).
Printer/Program map integration remains a later whole-emit obligation.
