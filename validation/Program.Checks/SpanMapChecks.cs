// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Table/subtest assertions from pinned spanmap/spanmap_test.go; literal inputs
// and expected values unchanged. xUnit/dispatcher supplies outer scheduling.
#nullable disable
using Configuration.Checks;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using Kind = TypeScript.SpanMap.Kind;
namespace Program.Checks;
public static partial class SpanMapChecks
{
    private static void TestVirtualToOriginalPosition()
    {
        var map = SpanMaps.New([new(0, 10, 100, 110, Kind.Verbatim, Feature.All), new(20, 30, 200, 210, Kind.Atom, Feature.All)]);
        (string name, int pos, int want, Fidelity fidelity)[] rows = [("verbatim interpolates", 3, 103, Fidelity.Exact), ("atom maps to its start", 25, 200, Fidelity.Atom), ("gap maps to insertion point", 15, 110, Fidelity.None)];
        foreach (var row in rows)
        {
            var (got, fidelity) = map.VirtualToOriginalPosition(row.pos);
            Check.Equal(got, row.want, row.name);
            Check.Equal(fidelity, row.fidelity, row.name);
            var (span, spanFidelity) = map.VirtualToOriginalSpan(new(row.pos, row.pos));
            Check.Equal(got, span.Pos(), row.name);
            Check.Equal(fidelity, spanFidelity, row.name);
        }
    }
    private static void TestVirtualToOriginalPositionExact()
    {
        var map = SpanMaps.New([new(0, 10, 100, 110, Kind.Verbatim, Feature.All), new(10, 20, 110, 120, Kind.Atom, Feature.All), new(20, 30, 120, 130, Kind.Verbatim, Feature.All)]);
        (int pos, int want, bool ok)[] rows = [(5, 105, true), (10, 110, false), (15, 110, false), (20, 120, false), (25, 125, true)];
        foreach (var row in rows)
        {
            var (got, ok) = map.VirtualToOriginalPositionExact(row.pos);
            Check.Equal(got, row.want, nameof(TestVirtualToOriginalPositionExact));
            Check.Equal(ok, row.ok, nameof(TestVirtualToOriginalPositionExact));
        }
    }
    private static void TestZeroLengthSpansAtSegmentEnds()
    {
        var map = SpanMaps.New([new(0, 10, 100, 110, Kind.Verbatim, Feature.Hover), new(20, 30, 200, 210, Kind.Verbatim, Feature.Hover)]);
        var (position, fidelity) = map.VirtualToOriginalPosition(30);
        Check.Equal(position, 210, nameof(TestZeroLengthSpansAtSegmentEnds));
        Check.Equal(fidelity, Fidelity.Exact, nameof(TestZeroLengthSpansAtSegmentEnds));
        var (virtualSpan, spanFidelity) = map.VirtualToOriginalSpan(new(30, 30));
        Check.Equal(virtualSpan, new TextRange(210, 210), nameof(TestZeroLengthSpansAtSegmentEnds));
        Check.Equal(spanFidelity, Fidelity.Exact, nameof(TestZeroLengthSpansAtSegmentEnds));
        foreach (var row in new[] { (name: "before gap", originalEnd: 110), (name: "final", originalEnd: 210) })
        {
            var positions = map.OriginalToVirtualPositions(row.originalEnd, Feature.Hover);
            var spans = map.OriginalToVirtualSpans(new(row.originalEnd, row.originalEnd), Feature.Hover);
            Check.Equal(Len(positions), 1, row.name);
            Check.Equal(Len(spans), 1, row.name);
            Check.Equal(spans[0].Span, new TextRange(positions[0].Position, positions[0].Position), row.name);
            Check.Equal(spans[0].Fidelity, positions[0].Fidelity, row.name);
        }
    }
    private static void TestOriginalToVirtualPositions()
    {
        var map = SpanMaps.New([new(0, 10, 100, 110, Kind.Verbatim, Feature.All), new(20, 30, 200, 210, Kind.Atom, Feature.All)]);
        (string name, int pos, int want, Fidelity fidelity)[] rows = [("verbatim interpolates", 103, 3, Fidelity.Exact), ("atom maps to its start", 205, 20, Fidelity.Atom), ("gap has no projection", 150, 0, Fidelity.None)];
        foreach (var row in rows)
        {
            var positions = map.OriginalToVirtualPositions(row.pos, Feature.All);
            var spans = map.OriginalToVirtualSpans(new(row.pos, row.pos), Feature.All);
            if (row.fidelity.IsNone())
            {
                Check.Equal(Len(positions), 0, row.name);
                Check.Equal(Len(spans), 0, row.name);
                continue;
            }
            Check.Equal(Len(positions), 1, row.name);
            Check.Equal(positions[0].Position, row.want, row.name);
            Check.Equal(positions[0].Fidelity, row.fidelity, row.name);
            Check.Equal(Len(spans), 1, row.name);
            Check.Equal(spans[0].Span.Pos(), row.want, row.name);
            Check.Equal(spans[0].Fidelity, row.fidelity, row.name);
        }
    }
    private static void TestValidate()
    {
        const string transformed = "const greeting = 1;\n", original = "<x>const greeting = 1;\n</x>";
        const int scriptStart = 3;
        (string name, List<Segment> segments, MappingErrorKind kind, bool valid)[] rows =
        [
            ("valid verbatim", [new(0, transformed.Length, scriptStart, scriptStart + transformed.Length, Kind.Verbatim, Feature.None)], default, true),
            ("empty is valid", null, default, true),
            ("gap is allowed", [new(3, transformed.Length, 0, 0, Kind.Atom, Feature.None)], default, true),
            ("overlap", [new(0, 10, 0, 0, Kind.Atom, Feature.None), new(5, transformed.Length, 0, 0, Kind.Atom, Feature.None)], MappingErrorKind.Overlap, false),
            ("original out of bounds", [new(0, transformed.Length, 0, original.Length + 10, Kind.Atom, Feature.None)], MappingErrorKind.OutOfBounds, false),
            ("verbatim text mismatch", [new(0, transformed.Length, 0, transformed.Length, Kind.Verbatim, Feature.None)], MappingErrorKind.VerbatimMismatch, false),
            ("unknown kind", [new(0, 1, 0, 1, (Kind)3, Feature.None)], MappingErrorKind.Kind, false)
        ];
        foreach (var row in rows)
        {
            var problem = SpanMaps.New(row.segments).Validate(transformed, original);
            if (row.valid) { Check.True(problem is null, row.name); continue; }
            Check.True(problem is not null, row.name);
            Check.Equal(problem.Kind, row.kind, row.name);
        }
    }
    private static void TestValidateOriginalOverlapAndFeatures()
    {
        (string name, List<Segment> segments, MappingErrorKind kind, bool valid)[] rows =
        [
            ("identical duplicate group", [new(0, 3, 0, 3, Kind.Verbatim, Feature.Definition), new(3, 6, 0, 3, Kind.Verbatim, Feature.Hover)], default, true),
            ("partial original overlap is valid", [new(0, 3, 0, 3, Kind.Atom, Feature.None), new(3, 6, 2, 5, Kind.Atom, Feature.None)], default, true),
            ("nested original overlap is valid", [new(0, 5, 0, 5, Kind.Atom, Feature.None), new(5, 6, 1, 4, Kind.Atom, Feature.None)], default, true),
            ("duplicate without explicit features is tolerant", [new(0, 3, 0, 3, Kind.Atom, Feature.None), new(3, 6, 0, 3, Kind.Atom, Feature.Definition)], default, true),
            ("duplicate with shared feature members is tolerant", [new(0, 3, 0, 3, Kind.Atom, Feature.Hover), new(3, 6, 0, 3, Kind.Atom, Feature.Hover | Feature.Definition)], default, true),
            ("features on sole cover are valid", [new(0, 3, 0, 3, Kind.Atom, Feature.Definition)], default, true),
            ("unknown feature flag", [new(0, 3, 0, 3, Kind.Atom, (Feature)(1 << 22))], MappingErrorKind.Feature, false)
        ];
        foreach (var row in rows)
        {
            var problem = SpanMaps.New(row.segments).Validate("abcabc", "abcdef");
            if (row.valid) { Check.True(problem is null, row.name); continue; }
            Check.True(problem is not null, row.name);
            Check.Equal(problem.Kind, row.kind, row.name);
        }
    }
}
