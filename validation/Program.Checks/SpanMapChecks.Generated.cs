// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Literal assertion-preserving port of pinned spanmap/spanmap_test.go.
// Reproduce: python3 tools/program-generate-spanmap-tests.py --check
#nullable disable
using Configuration.Checks;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using SpanMapValue = TypeScript.SpanMap.SpanMap;
using Kind = TypeScript.SpanMap.Kind;
namespace Program.Checks;
public static partial class SpanMapChecks
{
    public static IEnumerable<object[]> Cases => Names.Select(name => new object[] { name });
    public static readonly string[] Names = [
        "TestVirtualToOriginalSpanVerbatim",
        "TestVirtualToOriginalSpanAtom",
        "TestVirtualAlias",
        "TestVirtualToOriginalSpanSynthesizedGap",
        "TestOriginalToVirtualIntersectingSpansAllowsUncoveredEndpoints",
        "TestVirtualToOriginalSpanEmptyIsSynthesized",
        "TestVirtualToOriginalSpanCrossingSegments",
        "TestVirtualToOriginalSpanNilIdentity",
        "TestVirtualToOriginalPosition",
        "TestVirtualToOriginalPositionExact",
        "TestVirtualToOriginalPositionExactRejectsDiscontinuousBoundary",
        "TestZeroLengthSpansAtSegmentEnds",
        "TestMapPositionNilIdentity",
        "TestOriginalToVirtualSpanVerbatim",
        "TestOriginalToVirtualSpanAtom",
        "TestOriginalToVirtualSpanGap",
        "TestOriginalToVirtualSpanNilIdentity",
        "TestOriginalToVirtualPositions",
        "TestOriginalToVirtualPositionsAtEndpoint",
        "TestOriginalToVirtualDuplicateGroup",
        "TestOriginalToVirtualOverlappingSpans",
        "TestOriginalToVirtualPositionFindsEarlyCoveringSegment",
        "TestOriginalToVirtualOverlapFallsBackFromDisabledContainer",
        "TestOriginalToVirtualCrossGroupProjections",
        "TestOriginalToVirtualExplicitZeroFeatures",
        "TestFeatureParticipationOriginalAndVirtual",
        "TestOriginalToVirtualSpanRoundTrip",
        "TestMarshalRoundTrip",
        "TestValidate",
        "TestValidateOriginalOverlapAndFeatures",
        "TestValidateNilIsValid",
    ];
    public static void Run(string name)
    {
        switch (name)
        {
            case "TestVirtualToOriginalSpanVerbatim": TestVirtualToOriginalSpanVerbatim(); break;
            case "TestVirtualToOriginalSpanAtom": TestVirtualToOriginalSpanAtom(); break;
            case "TestVirtualAlias": TestVirtualAlias(); break;
            case "TestVirtualToOriginalSpanSynthesizedGap": TestVirtualToOriginalSpanSynthesizedGap(); break;
            case "TestOriginalToVirtualIntersectingSpansAllowsUncoveredEndpoints": TestOriginalToVirtualIntersectingSpansAllowsUncoveredEndpoints(); break;
            case "TestVirtualToOriginalSpanEmptyIsSynthesized": TestVirtualToOriginalSpanEmptyIsSynthesized(); break;
            case "TestVirtualToOriginalSpanCrossingSegments": TestVirtualToOriginalSpanCrossingSegments(); break;
            case "TestVirtualToOriginalSpanNilIdentity": TestVirtualToOriginalSpanNilIdentity(); break;
            case "TestVirtualToOriginalPosition": TestVirtualToOriginalPosition(); break;
            case "TestVirtualToOriginalPositionExact": TestVirtualToOriginalPositionExact(); break;
            case "TestVirtualToOriginalPositionExactRejectsDiscontinuousBoundary": TestVirtualToOriginalPositionExactRejectsDiscontinuousBoundary(); break;
            case "TestZeroLengthSpansAtSegmentEnds": TestZeroLengthSpansAtSegmentEnds(); break;
            case "TestMapPositionNilIdentity": TestMapPositionNilIdentity(); break;
            case "TestOriginalToVirtualSpanVerbatim": TestOriginalToVirtualSpanVerbatim(); break;
            case "TestOriginalToVirtualSpanAtom": TestOriginalToVirtualSpanAtom(); break;
            case "TestOriginalToVirtualSpanGap": TestOriginalToVirtualSpanGap(); break;
            case "TestOriginalToVirtualSpanNilIdentity": TestOriginalToVirtualSpanNilIdentity(); break;
            case "TestOriginalToVirtualPositions": TestOriginalToVirtualPositions(); break;
            case "TestOriginalToVirtualPositionsAtEndpoint": TestOriginalToVirtualPositionsAtEndpoint(); break;
            case "TestOriginalToVirtualDuplicateGroup": TestOriginalToVirtualDuplicateGroup(); break;
            case "TestOriginalToVirtualOverlappingSpans": TestOriginalToVirtualOverlappingSpans(); break;
            case "TestOriginalToVirtualPositionFindsEarlyCoveringSegment": TestOriginalToVirtualPositionFindsEarlyCoveringSegment(); break;
            case "TestOriginalToVirtualOverlapFallsBackFromDisabledContainer": TestOriginalToVirtualOverlapFallsBackFromDisabledContainer(); break;
            case "TestOriginalToVirtualCrossGroupProjections": TestOriginalToVirtualCrossGroupProjections(); break;
            case "TestOriginalToVirtualExplicitZeroFeatures": TestOriginalToVirtualExplicitZeroFeatures(); break;
            case "TestFeatureParticipationOriginalAndVirtual": TestFeatureParticipationOriginalAndVirtual(); break;
            case "TestOriginalToVirtualSpanRoundTrip": TestOriginalToVirtualSpanRoundTrip(); break;
            case "TestMarshalRoundTrip": TestMarshalRoundTrip(); break;
            case "TestValidate": TestValidate(); break;
            case "TestValidateOriginalOverlapAndFeatures": TestValidateOriginalOverlapAndFeatures(); break;
            case "TestValidateNilIsValid": TestValidateNilIsValid(); break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
    private static int Len<T>(IReadOnlyCollection<T> values) => values?.Count ?? 0;
    private static void TestVirtualToOriginalSpanVerbatim()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 10, OriginalStart = 100, OriginalEnd = 110, Kind = Kind.Verbatim, Features = Feature.All},});
	var (got, fidelity) = m.VirtualToOriginalSpan(new TextRange(3, 7));
	Check.Equal(got.Pos(), 103, "TestVirtualToOriginalSpanVerbatim");
	Check.Equal(got.End(), 107, "TestVirtualToOriginalSpanVerbatim");
	Check.Equal(fidelity, Fidelity.Exact, "TestVirtualToOriginalSpanVerbatim");
    }
    private static void TestVirtualToOriginalSpanAtom()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 3, VirtualEnd = 14, OriginalStart = 60, OriginalEnd = 71, Kind = Kind.Atom, Features = Feature.All},});
	var (got, fidelity) = m.VirtualToOriginalSpan(new TextRange(5, 9));
	Check.Equal(got.Pos(), 60, "TestVirtualToOriginalSpanAtom");
	Check.Equal(got.End(), 71, "TestVirtualToOriginalSpanAtom");
	Check.Equal(fidelity, Fidelity.Atom, "TestVirtualToOriginalSpanAtom");
    }
    private static void TestVirtualAlias()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 3, VirtualEnd = 6, OriginalStart = 10, OriginalEnd = 11, Kind = Kind.Alias, Features = Feature.All},});
	var (got, fidelity) = m.VirtualToOriginalSpan(new TextRange(3, 6));
	Check.Equal(got, new TextRange(10, 11), "TestVirtualAlias");
	Check.Equal(fidelity, Fidelity.Atom, "TestVirtualAlias");
	var (alias, ok) = m.AliasForVirtualSpan(new TextRange(3, 6));
	Check.True(ok, "TestVirtualAlias");
	Check.Equal(alias.Kind, Kind.Alias, "TestVirtualAlias");
	var (_, partial) = m.AliasForVirtualSpan(new TextRange(4, 6));
	Check.True(!partial, "TestVirtualAlias");
	var (data, err) = m.Marshal();
	Check.True(err is null, "TestVirtualAlias");
	var (decoded, errNext) = SpanMaps.Unmarshal(data);
	Check.True(errNext is null, "TestVirtualAlias");
	Check.Equal(decoded.Segments()[0].Kind, Kind.Alias, "TestVirtualAlias");
    }
    private static void TestVirtualToOriginalSpanSynthesizedGap()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 10, OriginalStart = 100, OriginalEnd = 110, Kind = Kind.Verbatim, Features = Feature.All}, new() {VirtualStart = 20, VirtualEnd = 30, OriginalStart = 200, OriginalEnd = 210, Kind = Kind.Verbatim, Features = Feature.All},});
	var (got, fidelity) = m.VirtualToOriginalSpan(new TextRange(12, 15));
	Check.Equal(got.Pos(), 110, "TestVirtualToOriginalSpanSynthesizedGap");
	Check.Equal(got.End(), 110, "TestVirtualToOriginalSpanSynthesizedGap");
	Check.Equal(fidelity, Fidelity.None, "TestVirtualToOriginalSpanSynthesizedGap");
    }
    private static void TestOriginalToVirtualIntersectingSpansAllowsUncoveredEndpoints()
    {
	var m = SpanMaps.New(new List<Segment> {new() { VirtualStart = 10, VirtualEnd = 20, OriginalStart = 100, OriginalEnd = 110, Kind = Kind.Verbatim, Features = Feature.SemanticTokens | Feature.InlayHints, }});
	foreach (var feature in new List<Feature> {Feature.SemanticTokens, Feature.InlayHints}) {
		var got = m.OriginalToVirtualIntersectingSpans(new TextRange(90, 120), feature);
		Check.Equal(Len(got), 1, "TestOriginalToVirtualIntersectingSpansAllowsUncoveredEndpoints");
		Check.Equal(got[0].Span, new TextRange(10, 20), "TestOriginalToVirtualIntersectingSpansAllowsUncoveredEndpoints");
		Check.Equal(got[0].Fidelity, Fidelity.Exact, "TestOriginalToVirtualIntersectingSpansAllowsUncoveredEndpoints");
	}
    }
    private static void TestVirtualToOriginalSpanEmptyIsSynthesized()
    {
	var m = SpanMaps.New(null);
	var (got, fidelity) = m.VirtualToOriginalSpan(new TextRange(5, 10));
	Check.Equal(got.Pos(), 0, "TestVirtualToOriginalSpanEmptyIsSynthesized");
	Check.Equal(got.End(), 0, "TestVirtualToOriginalSpanEmptyIsSynthesized");
	Check.Equal(fidelity, Fidelity.None, "TestVirtualToOriginalSpanEmptyIsSynthesized");
    }
    private static void TestVirtualToOriginalSpanCrossingSegments()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 10, OriginalStart = 100, OriginalEnd = 110, Kind = Kind.Verbatim}, new() {VirtualStart = 10, VirtualEnd = 20, OriginalStart = 200, OriginalEnd = 210, Kind = Kind.Verbatim},});
	var (got, fidelity) = m.VirtualToOriginalSpan(new TextRange(5, 15));
	Check.Equal(got.Pos(), 105, "TestVirtualToOriginalSpanCrossingSegments");
	Check.Equal(got.End(), 205, "TestVirtualToOriginalSpanCrossingSegments");
	Check.Equal(fidelity, Fidelity.Approximate, "TestVirtualToOriginalSpanCrossingSegments");
    }
    private static void TestVirtualToOriginalSpanNilIdentity()
    {
SpanMapValue m = null;;
	var (got, fidelity) = m.VirtualToOriginalSpan(new TextRange(3, 7));
	Check.Equal(got.Pos(), 3, "TestVirtualToOriginalSpanNilIdentity");
	Check.Equal(got.End(), 7, "TestVirtualToOriginalSpanNilIdentity");
	Check.Equal(fidelity, Fidelity.Exact, "TestVirtualToOriginalSpanNilIdentity");
    }
    private static void TestVirtualToOriginalPositionExactRejectsDiscontinuousBoundary()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 10, OriginalStart = 0, OriginalEnd = 10, Kind = Kind.Verbatim}, new() {VirtualStart = 10, VirtualEnd = 20, OriginalStart = 100, OriginalEnd = 110, Kind = Kind.Verbatim},});
	var (mapped, ok) = m.VirtualToOriginalPositionExact(10);
	Check.Equal(mapped, (100), "TestVirtualToOriginalPositionExactRejectsDiscontinuousBoundary");
	Check.True(!ok, "TestVirtualToOriginalPositionExactRejectsDiscontinuousBoundary");
    }
    private static void TestMapPositionNilIdentity()
    {
SpanMapValue m = null;;
	var (got, fidelity) = m.VirtualToOriginalPosition(7);
	Check.Equal(got, (7), "TestMapPositionNilIdentity");
	Check.Equal(fidelity, Fidelity.Exact, "TestMapPositionNilIdentity");
    }
    private static void TestOriginalToVirtualSpanVerbatim()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 10, OriginalStart = 100, OriginalEnd = 110, Kind = Kind.Verbatim, Features = Feature.All},});
	var results = m.OriginalToVirtualSpans(new TextRange(103, 107), Feature.All);
	Check.Equal(Len(results), 1, "TestOriginalToVirtualSpanVerbatim");
	Check.Equal(results[0].Span.Pos(), 3, "TestOriginalToVirtualSpanVerbatim");
	Check.Equal(results[0].Span.End(), 7, "TestOriginalToVirtualSpanVerbatim");
	Check.Equal(results[0].Fidelity, Fidelity.Exact, "TestOriginalToVirtualSpanVerbatim");
    }
    private static void TestOriginalToVirtualSpanAtom()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 3, VirtualEnd = 14, OriginalStart = 60, OriginalEnd = 71, Kind = Kind.Atom, Features = Feature.All},});
	var results = m.OriginalToVirtualSpans(new TextRange(63, 67), Feature.All);
	Check.Equal(Len(results), 1, "TestOriginalToVirtualSpanAtom");
	Check.Equal(results[0].Span.Pos(), 3, "TestOriginalToVirtualSpanAtom");
	Check.Equal(results[0].Span.End(), 14, "TestOriginalToVirtualSpanAtom");
	Check.Equal(results[0].Fidelity, Fidelity.Atom, "TestOriginalToVirtualSpanAtom");
    }
    private static void TestOriginalToVirtualSpanGap()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 10, OriginalStart = 100, OriginalEnd = 110, Kind = Kind.Verbatim, Features = Feature.All}, new() {VirtualStart = 20, VirtualEnd = 30, OriginalStart = 200, OriginalEnd = 210, Kind = Kind.Verbatim, Features = Feature.All},});
	Check.Equal(Len(m.OriginalToVirtualSpans(new TextRange(150, 160), Feature.All)), 0, "TestOriginalToVirtualSpanGap");
    }
    private static void TestOriginalToVirtualSpanNilIdentity()
    {
SpanMapValue m = null;;
	var results = m.OriginalToVirtualSpans(new TextRange(3, 7), Feature.All);
	Check.Equal(Len(results), 1, "TestOriginalToVirtualSpanNilIdentity");
	Check.Equal(results[0].Span.Pos(), 3, "TestOriginalToVirtualSpanNilIdentity");
	Check.Equal(results[0].Span.End(), 7, "TestOriginalToVirtualSpanNilIdentity");
	Check.Equal(results[0].Fidelity, Fidelity.Exact, "TestOriginalToVirtualSpanNilIdentity");
    }
    private static void TestOriginalToVirtualPositionsAtEndpoint()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 2, VirtualEnd = 5, OriginalStart = 10, OriginalEnd = 13, Kind = Kind.Verbatim, Features = Feature.Completion}, new() {VirtualStart = 8, VirtualEnd = 11, OriginalStart = 13, OriginalEnd = 16, Kind = Kind.Verbatim, Features = Feature.Completion}, new() {VirtualStart = 20, VirtualEnd = 23, OriginalStart = 30, OriginalEnd = 35, Kind = Kind.Atom, Features = Feature.Completion},});
	Check.Sequence(m.OriginalToVirtualPositions(13, Feature.Completion), new List<MappedPosition> {new() {Position = 5, Fidelity = Fidelity.Exact}, new() {Position = 8, Fidelity = Fidelity.Exact},}, "TestOriginalToVirtualPositionsAtEndpoint");
	Check.Sequence(m.OriginalToVirtualPositions(35, Feature.Completion), new List<MappedPosition> {new() {Position = 23, Fidelity = Fidelity.Atom},}, "TestOriginalToVirtualPositionsAtEndpoint");
	var filtered = SpanMaps.New(new List<Segment> {new() {VirtualStart = 20, VirtualEnd = 23, OriginalStart = 10, OriginalEnd = 13, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 2, VirtualEnd = 5, OriginalStart = 13, OriginalEnd = 16, Kind = Kind.Verbatim, Features = Feature.Completion},});
	Check.Sequence(filtered.OriginalToVirtualPositions(13, Feature.All), new List<MappedPosition> {new() {Position = 2, Fidelity = Fidelity.Exact}, new() {Position = 23, Fidelity = Fidelity.Exact},}, "TestOriginalToVirtualPositionsAtEndpoint");
	Check.Sequence(filtered.OriginalToVirtualPositions(13, Feature.Completion), new List<MappedPosition> {new() {Position = 2, Fidelity = Fidelity.Exact},}, "TestOriginalToVirtualPositionsAtEndpoint");
    }
    private static void TestOriginalToVirtualDuplicateGroup()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 3, OriginalStart = 10, OriginalEnd = 13, Kind = Kind.Verbatim, Features = Feature.Definition}, new() {VirtualStart = 10, VirtualEnd = 13, OriginalStart = 10, OriginalEnd = 13, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 20, VirtualEnd = 25, OriginalStart = 10, OriginalEnd = 13, Kind = Kind.Atom, Features = Feature.Definition},});
	var semantic = m.OriginalToVirtualPositions(11, Feature.Hover);
	Check.Equal(Len(semantic), 1, "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(semantic[0].Position, (11), "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(semantic[0].Fidelity, Fidelity.Exact, "TestOriginalToVirtualDuplicateGroup");
	var navigation = m.OriginalToVirtualPositions(11, Feature.Definition);
	Check.Equal(Len(navigation), 2, "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(navigation[0].Position, (1), "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(navigation[0].Fidelity, Fidelity.Exact, "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(navigation[1].Position, (20), "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(navigation[1].Fidelity, Fidelity.Atom, "TestOriginalToVirtualDuplicateGroup");
	var spans = m.OriginalToVirtualSpans(new TextRange(10, 13), Feature.Definition);
	Check.Equal(Len(spans), 2, "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(spans[0].Span.Pos(), 0, "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(spans[0].Span.End(), 3, "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(spans[1].Span.Pos(), 20, "TestOriginalToVirtualDuplicateGroup");
	Check.Equal(spans[1].Span.End(), 25, "TestOriginalToVirtualDuplicateGroup");
    }
    private static void TestOriginalToVirtualOverlappingSpans()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 6, OriginalStart = 0, OriginalEnd = 6, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 10, VirtualEnd = 12, OriginalStart = 2, OriginalEnd = 4, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 20, VirtualEnd = 24, OriginalStart = 3, OriginalEnd = 7, Kind = Kind.Verbatim, Features = Feature.Hover},});
	Check.Sequence(m.OriginalToVirtualPositions(3, Feature.Hover), new List<MappedPosition> {new() {Position = 3, Fidelity = Fidelity.Exact}, new() {Position = 11, Fidelity = Fidelity.Exact}, new() {Position = 20, Fidelity = Fidelity.Exact},}, "TestOriginalToVirtualOverlappingSpans");
	var spans = m.OriginalToVirtualSpans(new TextRange(3, 4), Feature.Hover);
	var wantSpans = new List<MappedSpan> {new() {Span = new TextRange(3, 4), Fidelity = Fidelity.Exact}, new() {Span = new TextRange(11, 12), Fidelity = Fidelity.Exact}, new() {Span = new TextRange(20, 21), Fidelity = Fidelity.Exact},};
	Check.Equal(Len(spans), Len(wantSpans), "TestOriginalToVirtualOverlappingSpans");
	for (int i = 0; i < Len(spans); i++) {
		Check.Equal(spans[i], wantSpans[i], "TestOriginalToVirtualOverlappingSpans");
	}
    }
    private static void TestOriginalToVirtualPositionFindsEarlyCoveringSegment()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 100, OriginalStart = 0, OriginalEnd = 100, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 100, VirtualEnd = 105, OriginalStart = 80, OriginalEnd = 85, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 105, VirtualEnd = 110, OriginalStart = 90, OriginalEnd = 95, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 110, VirtualEnd = 113, OriginalStart = 100, OriginalEnd = 103, Kind = Kind.Verbatim, Features = Feature.Hover},});
	Check.Sequence(m.OriginalToVirtualPositions(97, Feature.Hover), new List<MappedPosition> {new() {Position = 97, Fidelity = Fidelity.Exact},}, "TestOriginalToVirtualPositionFindsEarlyCoveringSegment");
	var spans = m.OriginalToVirtualSpans(new TextRange(97, 98), Feature.Hover);
	Check.Equal(Len(spans), 1, "TestOriginalToVirtualPositionFindsEarlyCoveringSegment");
	Check.Equal(spans[0], new MappedSpan {Span = new TextRange(97, 98), Fidelity = Fidelity.Exact}, "TestOriginalToVirtualPositionFindsEarlyCoveringSegment");
	Check.Sequence(m.OriginalToVirtualPositions(100, Feature.Hover), new List<MappedPosition> {new() {Position = 100, Fidelity = Fidelity.Exact}, new() {Position = 110, Fidelity = Fidelity.Exact},}, "TestOriginalToVirtualPositionFindsEarlyCoveringSegment");
spans = m.OriginalToVirtualSpans(new TextRange(100, 101), Feature.Hover);
	Check.Equal(Len(spans), 1, "TestOriginalToVirtualPositionFindsEarlyCoveringSegment");
	Check.Equal(spans[0], new MappedSpan {Span = new TextRange(110, 111), Fidelity = Fidelity.Exact}, "TestOriginalToVirtualPositionFindsEarlyCoveringSegment");
    }
    private static void TestOriginalToVirtualOverlapFallsBackFromDisabledContainer()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 6, OriginalStart = 0, OriginalEnd = 6, Kind = Kind.Verbatim, Features = Feature.Definition}, new() {VirtualStart = 10, VirtualEnd = 13, OriginalStart = 0, OriginalEnd = 3, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 13, VirtualEnd = 16, OriginalStart = 3, OriginalEnd = 6, Kind = Kind.Verbatim, Features = Feature.Hover},});
	var spans = m.OriginalToVirtualSpans(new TextRange(1, 5), Feature.Hover);
	Check.Equal(Len(spans), 1, "TestOriginalToVirtualOverlapFallsBackFromDisabledContainer");
	Check.Equal(spans[0], new MappedSpan {Span = new TextRange(11, 15), Fidelity = Fidelity.Approximate}, "TestOriginalToVirtualOverlapFallsBackFromDisabledContainer");
    }
    private static void TestOriginalToVirtualCrossGroupProjections()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 2, OriginalStart = 0, OriginalEnd = 2, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 2, VirtualEnd = 4, OriginalStart = 2, OriginalEnd = 4, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 10, VirtualEnd = 12, OriginalStart = 0, OriginalEnd = 2, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 12, VirtualEnd = 14, OriginalStart = 2, OriginalEnd = 4, Kind = Kind.Verbatim, Features = Feature.Hover},});
	var spans = m.OriginalToVirtualSpans(new TextRange(1, 3), Feature.Hover);
	Check.Equal(Len(spans), 2, "TestOriginalToVirtualCrossGroupProjections");
	Check.Equal(spans[0].Span, new TextRange(1, 3), "TestOriginalToVirtualCrossGroupProjections");
	Check.Equal(spans[1].Span, new TextRange(11, 13), "TestOriginalToVirtualCrossGroupProjections");
	foreach (var mapped in spans) {
		Check.Equal(mapped.Fidelity, Fidelity.Approximate, "TestOriginalToVirtualCrossGroupProjections");
	}
    }
    private static void TestOriginalToVirtualExplicitZeroFeatures()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 3, OriginalStart = 10, OriginalEnd = 13, Kind = Kind.Verbatim, Features = Feature.None},});
	Check.Equal(Len(m.OriginalToVirtualPositions(11, Feature.Hover)), 0, "TestOriginalToVirtualExplicitZeroFeatures");
	Check.Equal(Len(m.OriginalToVirtualPositions(11, Feature.Definition)), 0, "TestOriginalToVirtualExplicitZeroFeatures");
	Check.Equal(Len(m.OriginalToVirtualSpans(new TextRange(10, 13), Feature.Hover)), 0, "TestOriginalToVirtualExplicitZeroFeatures");
	var (data, err) = m.Marshal();
	Check.True(err is null, "TestOriginalToVirtualExplicitZeroFeatures");
	Check.Equal(GoString.FromBytes(data!), "[[0,3,10,3,0,0]]", "TestOriginalToVirtualExplicitZeroFeatures");
	var (decoded, errNext) = SpanMaps.Unmarshal(data);
	Check.True(errNext is null, "TestOriginalToVirtualExplicitZeroFeatures");
	var segments = decoded.Segments();
	Check.Equal(segments[0].Features, Feature.None, "TestOriginalToVirtualExplicitZeroFeatures");
	var (legacy, errNext2) = SpanMaps.Unmarshal(GoString.Bytes("[[0,3,10,3,0]]"));
	Check.True(errNext2 is null, "TestOriginalToVirtualExplicitZeroFeatures");
	Check.Equal(legacy.Segments()[0].Features, Feature.All, "TestOriginalToVirtualExplicitZeroFeatures");
	Check.Equal(Len(legacy.OriginalToVirtualPositions(11, Feature.Hover)), 1, "TestOriginalToVirtualExplicitZeroFeatures");
    }
    private static void TestFeatureParticipationOriginalAndVirtual()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 3, OriginalStart = 10, OriginalEnd = 13, Kind = Kind.Verbatim, Features = Feature.Hover}, new() {VirtualStart = 3, VirtualEnd = 6, OriginalStart = 20, OriginalEnd = 23, Kind = Kind.Verbatim, Features = Feature.Completion},});
	Check.Equal(Len(m.OriginalToVirtualPositions(11, Feature.Hover)), 1, "TestFeatureParticipationOriginalAndVirtual");
	Check.Equal(Len(m.OriginalToVirtualPositions(11, Feature.Completion)), 0, "TestFeatureParticipationOriginalAndVirtual");
	var (mapped, fidelity) = m.VirtualToOriginalSpanForFeature(new TextRange(0, 3), Feature.Hover);
	Check.Equal(mapped, new TextRange(10, 13), "TestFeatureParticipationOriginalAndVirtual");
	Check.Equal(fidelity, Fidelity.Exact, "TestFeatureParticipationOriginalAndVirtual");
(_, fidelity) = m.VirtualToOriginalSpanForFeature(new TextRange(0, 3), Feature.Completion);
	Check.Equal(fidelity, Fidelity.None, "TestFeatureParticipationOriginalAndVirtual");
(mapped, fidelity) = m.VirtualToOriginalSpan(new TextRange(0, 3));
	Check.Equal(mapped, new TextRange(10, 13), "TestFeatureParticipationOriginalAndVirtual");
	Check.Equal(fidelity, Fidelity.Exact, "TestFeatureParticipationOriginalAndVirtual");
    }
    private static void TestOriginalToVirtualSpanRoundTrip()
    {
	var m = SpanMaps.New(new List<Segment> {new() {VirtualStart = 0, VirtualEnd = 10, OriginalStart = 200, OriginalEnd = 210, Kind = Kind.Verbatim, Features = Feature.All}, new() {VirtualStart = 10, VirtualEnd = 20, OriginalStart = 100, OriginalEnd = 110, Kind = Kind.Verbatim, Features = Feature.All},});
	foreach (var r in new List<TextRange> {new TextRange(2, 8), new TextRange(12, 18)}) {
		var (orig, fidelity) = m.VirtualToOriginalSpan(r);
		Check.Equal(fidelity, Fidelity.Exact, "TestOriginalToVirtualSpanRoundTrip");
		var back = m.OriginalToVirtualSpans(orig, Feature.All);
		Check.Equal(Len(back), 1, "TestOriginalToVirtualSpanRoundTrip");
		Check.Equal(back[0].Fidelity, Fidelity.Exact, "TestOriginalToVirtualSpanRoundTrip");
		Check.Equal(back[0].Span.Pos(), r.Pos(), "TestOriginalToVirtualSpanRoundTrip");
		Check.Equal(back[0].Span.End(), r.End(), "TestOriginalToVirtualSpanRoundTrip");
	}
    }
    private static void TestMarshalRoundTrip()
    {
	var original = SpanMaps.New(new List<Segment> {new() {VirtualStart = 3, VirtualEnd = 14, OriginalStart = 60, OriginalEnd = 71, Kind = Kind.Atom}, new() {VirtualStart = 14, VirtualEnd = 24, OriginalStart = 71, OriginalEnd = 81, Kind = Kind.Verbatim},});
	var (data, err) = original.Marshal();
	Check.True(err is null, "TestMarshalRoundTrip");
	var (decoded, errNext) = SpanMaps.Unmarshal(data);
	Check.True(errNext is null, "TestMarshalRoundTrip");
	foreach (var r in new List<TextRange> {new TextRange(1, 2), new TextRange(4, 10), new TextRange(16, 20)}) {
		var (wantRange, wantFidelity) = original.VirtualToOriginalSpan(r);
		var (gotRange, gotFidelity) = decoded.VirtualToOriginalSpan(r);
		Check.Equal(gotRange, wantRange, "TestMarshalRoundTrip");
		Check.Equal(gotFidelity, wantFidelity, "TestMarshalRoundTrip");
	}
    }
    private static void TestValidateNilIsValid()
    {
SpanMapValue m = null;;
	Check.True(m.Validate("abc", "abc") == null, "TestValidateNilIsValid");
    }
}
