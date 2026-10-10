// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/spanmap/spanmap.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Strings are original Go byte strings.
// Nullable receiver extensions preserve Go's nil identity mapping; a nonnil
// map with a nil/empty segment slice instead describes synthesized content.
using TypeScript.Json;
using TypeScript.Scanner;

namespace TypeScript.SpanMap;
public enum Kind { Verbatim, Atom, Alias }
[Flags]
public enum Feature
{
    None = 0, Hover = 1 << 0, SignatureHelp = 1 << 1, Completion = 1 << 2,
    Definition = 1 << 3, TypeDefinition = 1 << 4, Implementation = 1 << 5,
    References = 1 << 6, DocumentHighlights = 1 << 7, Rename = 1 << 8,
    CallHierarchy = 1 << 9, CodeActions = 1 << 10, Formatting = 1 << 11,
    InlayHints = 1 << 12, SemanticTokens = 1 << 13, FoldingRanges = 1 << 14,
    SelectionRanges = 1 << 15, LinkedEditing = 1 << 16, AutoInsert = 1 << 17,
    DocumentSymbols = 1 << 18, CodeLens = 1 << 19, All = (CodeLens << 1) - 1
}
public enum Fidelity { Exact, Atom, Approximate, None }
public readonly record struct Segment(int VirtualStart, int VirtualEnd, int OriginalStart, int OriginalEnd, Kind Kind, Feature Features);
public readonly record struct MappedPosition(int Position, Fidelity Fidelity);
public readonly record struct MappedSpan(TextRange Span, Fidelity Fidelity);
public enum MappingErrorKind { Overlap, OutOfBounds, VerbatimMismatch, Kind, Feature }
public sealed class MappingError : Exception
{
    public MappingErrorKind Kind;
    public int VirtualPos, OriginalPos;
    public override string Message => Kind switch
    {
        MappingErrorKind.Overlap => $"content mapper position mappings overlap or are out of order near virtual offset {VirtualPos}",
        MappingErrorKind.OutOfBounds => $"content mapper position mapping points outside the original content at original offset {OriginalPos}",
        MappingErrorKind.VerbatimMismatch => $"content mapper verbatim mapping does not match the original content at virtual offset {VirtualPos}, original offset {OriginalPos}",
        MappingErrorKind.Kind => $"content mapper position mapping has an invalid kind at virtual offset {VirtualPos}",
        MappingErrorKind.Feature => $"content mapper position mappings have invalid features near original offset {OriginalPos}",
        _ => "content mapper produced an invalid position mapping"
    };
}
public sealed class SpanMap
{
    internal readonly List<Segment>? segments;
    private readonly Lazy<OriginalIndex> originalIndex;
    internal SpanMap(List<Segment>? segments)
    {
        this.segments = segments;
        // The source sync.Once callback only builds immutable values and cannot
        // fail from ordinary input. Lazy publishes the same single index.
        originalIndex = new(() => OriginalIndex.Create(this.segments));
    }
    internal OriginalIndex OrigIndex() => originalIndex.Value;
}
public static class SpanMaps
{
    public static bool IsExact(this Fidelity fidelity) => fidelity == Fidelity.Exact;
    public static bool IsSingleSegment(this Fidelity fidelity) => fidelity is Fidelity.Exact or Fidelity.Atom;
    public static bool IsNone(this Fidelity fidelity) => fidelity == Fidelity.None;
    public static SpanMap New(List<Segment>? segments)
    {
        var sorted = GoSlices.Clone(segments);
        GoSlices.SortFunc(sorted, (a, b) => unchecked(a.VirtualStart - b.VirtualStart));
        return new(sorted);
    }
    public static List<Segment>? Segments(this SpanMap? map) => GoSlices.Clone(map?.segments);
    public static MappingError? Validate(this SpanMap? map, string virtualText, string original)
    {
        if (map is null) return null;
        int previousVirtualEnd = 0;
        foreach (var segment in map.segments ?? [])
        {
            if (segment.VirtualStart < previousVirtualEnd || segment.VirtualEnd < segment.VirtualStart || segment.VirtualEnd > virtualText.Length)
                return new() { Kind = MappingErrorKind.Overlap, VirtualPos = segment.VirtualStart };
            previousVirtualEnd = segment.VirtualEnd;
            if (segment.OriginalStart < 0 || segment.OriginalEnd < segment.OriginalStart || segment.OriginalEnd > original.Length)
                return new() { Kind = MappingErrorKind.OutOfBounds, VirtualPos = segment.VirtualStart, OriginalPos = segment.OriginalEnd };
            if (segment.Kind is not (Kind.Verbatim or Kind.Atom or Kind.Alias))
                return new() { Kind = MappingErrorKind.Kind, VirtualPos = segment.VirtualStart, OriginalPos = segment.OriginalStart };
            if (segment.Kind == Kind.Verbatim &&
                (unchecked(segment.VirtualEnd - segment.VirtualStart) != unchecked(segment.OriginalEnd - segment.OriginalStart) ||
                virtualText[segment.VirtualStart..segment.VirtualEnd] != original[segment.OriginalStart..segment.OriginalEnd]))
                return new() { Kind = MappingErrorKind.VerbatimMismatch, VirtualPos = segment.VirtualStart, OriginalPos = segment.OriginalStart };
            if ((segment.Features & ~Feature.All) != 0)
                return new() { Kind = MappingErrorKind.Feature, VirtualPos = segment.VirtualStart, OriginalPos = segment.OriginalStart };
        }
        return null;
    }
    public static (TextRange Span, Fidelity Fidelity) VirtualToOriginalSpan(this SpanMap? map, TextRange range)
    {
        if (map is null) return (range, Fidelity.Exact);
        int start = range.Pos(), end = Math.Max(range.End(), start);
        if (start == end)
        {
            var (position, fidelity) = map.VirtualToOriginalPosition(start);
            return (new(position, position), fidelity);
        }
        var (startIndex, startInside) = SegmentIndexAt(map, start);
        var (endIndex, endInside) = SegmentIndexAt(map, end - 1);
        if (startIndex == endIndex && startInside == endInside)
        {
            if (startInside)
            {
                var segment = map.segments![startIndex];
                if (segment.Kind == Kind.Verbatim)
                {
                    int originalStart = Clamp(unchecked(segment.OriginalStart + start - segment.VirtualStart), segment.OriginalStart, segment.OriginalEnd);
                    int originalEnd = Clamp(unchecked(segment.OriginalStart + end - segment.VirtualStart), originalStart, segment.OriginalEnd);
                    return (new(originalStart, originalEnd), Fidelity.Exact);
                }
                return (new(segment.OriginalStart, segment.OriginalEnd), Fidelity.Atom);
            }
            int position = InsertionPoint(map, startIndex);
            return (new(position, position), Fidelity.None);
        }
        int mappedStart = MapLow(map, start, startIndex, startInside);
        int mappedEnd = Math.Max(MapHigh(map, end, endIndex, endInside), mappedStart);
        return (new(mappedStart, mappedEnd), Fidelity.Approximate);
    }
    public static (TextRange Span, Fidelity Fidelity) VirtualToOriginalSpanForFeature(this SpanMap? map, TextRange range, Feature feature)
    {
        var (mapped, fidelity) = map.VirtualToOriginalSpan(range);
        return (mapped, map is null || VirtualSpanSupportsFeature(map, range, feature) ? fidelity : Fidelity.None);
    }
    internal static bool VirtualSpanSupportsFeature(SpanMap map, TextRange range, Feature feature)
    {
        int start = range.Pos(), end = Math.Max(range.End(), start);
        var (index, inside) = SegmentIndexAt(map, start);
        if (start == end) return inside && SupportsFeature(map.segments![index], feature);
        if (!inside) return false;
        int coveredThrough = start;
        while (index < (map.segments?.Count ?? 0) && coveredThrough < end)
        {
            var segment = map.segments![index];
            if (segment.VirtualStart > coveredThrough || segment.VirtualEnd <= coveredThrough || !SupportsFeature(segment, feature)) return false;
            coveredThrough = segment.VirtualEnd;
            index++;
        }
        return coveredThrough >= end;
    }
    public static (int Position, Fidelity Fidelity) VirtualToOriginalPosition(this SpanMap? map, int position)
    {
        if (map is null) return (position, Fidelity.Exact);
        var (index, inside) = SegmentIndexAt(map, position);
        if (!inside) return (InsertionPoint(map, index), Fidelity.None);
        var segment = map.segments![index];
        return segment.Kind == Kind.Verbatim
            ? (Clamp(unchecked(segment.OriginalStart + position - segment.VirtualStart), segment.OriginalStart, segment.OriginalEnd), Fidelity.Exact)
            : (segment.OriginalStart, Fidelity.Atom);
    }
    public static (int Position, bool Ok) VirtualToOriginalPositionExact(this SpanMap? map, int position)
    {
        var (mapped, fidelity) = map.VirtualToOriginalPosition(position);
        if (fidelity != Fidelity.Exact || map is null) return (mapped, fidelity == Fidelity.Exact);
        var (index, inside) = SegmentIndexAt(map, position);
        if (!inside || map.segments![index].Kind != Kind.Verbatim) return (mapped, false);
        if (index > 0)
        {
            var previous = map.segments![index - 1];
            if (previous.VirtualEnd == position && (previous.Kind != Kind.Verbatim || previous.OriginalEnd != map.segments[index].OriginalStart)) return (mapped, false);
        }
        return (mapped, true);
    }
    public static (int Position, Fidelity Fidelity) VirtualToOriginalPositionForFeature(this SpanMap? map, int position, Feature feature)
    {
        var (mapped, fidelity) = map.VirtualToOriginalPosition(position);
        if (map is null) return (mapped, fidelity);
        var (index, inside) = SegmentIndexAt(map, position);
        return (mapped, !inside || !SupportsFeature(map.segments![index], feature) ? Fidelity.None : fidelity);
    }
    public static (Segment Segment, bool Ok) AliasForVirtualSpan(this SpanMap? map, TextRange range)
    {
        if (map is null) return (default, false);
        var (index, inside) = SegmentIndexAt(map, range.Pos());
        if (!inside) return (default, false);
        var segment = map.segments![index];
        return (segment, segment.Kind == Kind.Alias && range.Pos() == segment.VirtualStart && range.End() == segment.VirtualEnd);
    }
    internal static (int Index, bool Inside) SegmentIndexAt(SpanMap map, int position)
    {
        int index = Search(map.segments?.Count ?? 0, i => unchecked(map.segments![i].VirtualStart - position) >= 0);
        if (index < (map.segments?.Count ?? 0) && unchecked(map.segments![index].VirtualStart - position) == 0) return (index, true);
        int previous = index - 1;
        if (previous >= 0 && (position < map.segments![previous].VirtualEnd || previous == map.segments!.Count - 1 && position == map.segments[previous].VirtualEnd)) return (previous, true);
        return (previous, false);
    }
    private static int InsertionPoint(SpanMap map, int previous) => previous < 0 ? 0 : map.segments![previous].OriginalEnd;
    private static int MapLow(SpanMap map, int position, int index, bool inside)
    {
        if (!inside) return InsertionPoint(map, index);
        var segment = map.segments![index];
        return segment.Kind == Kind.Verbatim ? Clamp(unchecked(segment.OriginalStart + position - segment.VirtualStart), segment.OriginalStart, segment.OriginalEnd) : segment.OriginalStart;
    }
    private static int MapHigh(SpanMap map, int position, int index, bool inside)
    {
        if (!inside) return InsertionPoint(map, index);
        var segment = map.segments![index];
        return segment.Kind == Kind.Verbatim ? Clamp(unchecked(segment.OriginalStart + position - segment.VirtualStart), segment.OriginalStart, segment.OriginalEnd) : segment.OriginalEnd;
    }
    public static List<MappedPosition>? OriginalToVirtualPositions(this SpanMap? map, int position, Feature feature)
    {
        if (map is null) return [new(position, Fidelity.Exact)];
        var groups = map.OrigIndex().SegmentGroupsAtOriginalPosition(position);
        if (groups is null || groups.Count == 0) return null;
        List<MappedPosition>? results = null;
        foreach (var group in groups)
        {
            foreach (var segment in group.Segments)
            {
                if (!SupportsFeature(segment, feature)) continue;
                var mapped = segment.Kind == Kind.Verbatim
                    ? new MappedPosition(Clamp(unchecked(segment.VirtualStart + position - segment.OriginalStart), segment.VirtualStart, segment.VirtualEnd), Fidelity.Exact)
                    : new MappedPosition(group.AtEnd ? segment.VirtualEnd : segment.VirtualStart, Fidelity.Atom);
                if (!(results?.Contains(mapped) ?? false)) (results ??= []).Add(mapped);
            }
        }
        GoSlices.SortFunc(results, (a, b) => unchecked(a.Position - b.Position));
        return results;
    }
    public static List<MappedSpan>? OriginalToVirtualSpans(this SpanMap? map, TextRange range, Feature feature)
    {
        if (map is null) return [new(range, Fidelity.Exact)];
        int start = range.Pos(), end = Math.Max(range.End(), start);
        if (start == end)
        {
            var positions = map.OriginalToVirtualPositions(start, feature);
            return positions?.Select(position => new MappedSpan(new(position.Position, position.Position), position.Fidelity)).ToList();
        }
        var originalIndex = map.OrigIndex();
        var (startSegments, startInside) = originalIndex.SegmentsAtOriginalPosition(start);
        var (endSegments, endInside) = originalIndex.SegmentsAtOriginalPosition(end - 1);
        if (!startInside || !endInside) return null;
        List<Segment>? containing = null;
        foreach (var segment in startSegments!)
            if (end <= segment.OriginalEnd) (containing ??= []).Add(segment);
        if (containing is { Count: > 0 })
        {
            var results = OriginalToVirtualSpansInSegments(containing, start, end, feature);
            if (results.Count > 0)
            {
                GoSlices.SortFunc(results, (a, b) => unchecked(a.Span.Pos() - b.Span.Pos()));
                return results;
            }
        }
        var starts = OriginalStartProjections(startSegments!, start, feature);
        var ends = OriginalEndProjections(endSegments!, end, feature);
        if (starts.Count == 0 || ends.Count == 0) return null;
        starts.Sort();
        ends.Sort();
        var projections = new List<MappedSpan>(Math.Min(starts.Count, ends.Count));
        for (int i = 0; i < starts.Count; i++)
        {
            int virtualStart = starts[i];
            int endIndex = Search(ends.Count, index => ends[index] >= virtualStart);
            if (endIndex == ends.Count || i + 1 < starts.Count && starts[i + 1] <= ends[endIndex]) continue;
            projections.Add(new(new(virtualStart, ends[endIndex]), Fidelity.Approximate));
        }
        return projections;
    }
    public static List<MappedSpan>? OriginalToVirtualIntersectingSpans(this SpanMap? map, TextRange range, Feature feature)
    {
        if (map is null) return [new(range, Fidelity.Exact)];
        if (range.Pos() == range.End()) return map.OriginalToVirtualSpans(range, feature);
        List<MappedSpan> results = [];
        foreach (var segment in map.segments ?? [])
        {
            if (!SupportsFeature(segment, feature)) continue;
            int start = Math.Max(range.Pos(), segment.OriginalStart), end = Math.Min(range.End(), segment.OriginalEnd);
            if (start >= end) continue;
            results.Add(segment.Kind == Kind.Verbatim
                ? new(new(unchecked(segment.VirtualStart + start - segment.OriginalStart), unchecked(segment.VirtualStart + end - segment.OriginalStart)), Fidelity.Exact)
                : new(new(segment.VirtualStart, segment.VirtualEnd), Fidelity.Atom));
        }
        return results;
    }
    internal static List<int> OriginalStartProjections(List<Segment> segments, int start, Feature feature)
    {
        var results = new List<int>(segments.Count);
        foreach (var segment in segments)
            if (SupportsFeature(segment, feature)) results.Add(segment.Kind == Kind.Verbatim ? Clamp(unchecked(segment.VirtualStart + start - segment.OriginalStart), segment.VirtualStart, segment.VirtualEnd) : segment.VirtualStart);
        return results;
    }
    internal static List<int> OriginalEndProjections(List<Segment> segments, int end, Feature feature)
    {
        var results = new List<int>(segments.Count);
        foreach (var segment in segments)
            if (SupportsFeature(segment, feature)) results.Add(segment.Kind == Kind.Verbatim ? Clamp(unchecked(segment.VirtualStart + end - segment.OriginalStart), segment.VirtualStart, segment.VirtualEnd) : segment.VirtualEnd);
        return results;
    }
    internal static List<MappedSpan> OriginalToVirtualSpansInSegments(List<Segment> segments, int start, int end, Feature feature)
    {
        var results = new List<MappedSpan>(segments.Count);
        foreach (var segment in segments)
        {
            if (!SupportsFeature(segment, feature)) continue;
            if (segment.Kind == Kind.Verbatim)
            {
                int virtualStart = Clamp(unchecked(segment.VirtualStart + start - segment.OriginalStart), segment.VirtualStart, segment.VirtualEnd);
                int virtualEnd = Clamp(unchecked(segment.VirtualStart + end - segment.OriginalStart), virtualStart, segment.VirtualEnd);
                results.Add(new(new(virtualStart, virtualEnd), Fidelity.Exact));
            }
            else results.Add(new(new(segment.VirtualStart, segment.VirtualEnd), Fidelity.Atom));
        }
        return results;
    }
    internal static bool SameOriginalRange(Segment left, Segment right) => left.OriginalStart == right.OriginalStart && left.OriginalEnd == right.OriginalEnd;
    internal static bool SupportsFeature(Segment segment, Feature feature) => (segment.Features & feature) != 0;
    internal static int Clamp(int value, int low, int high) => Math.Max(low, Math.Min(value, high));
    // sort.Search: first true in [0,count), including count when none is true.
    internal static int Search(int count, Func<int, bool> predicate)
    {
        int low = 0, high = count;
        while (low < high)
        {
            int middle = (int)((uint)(low + high) >> 1);
            if (!predicate(middle)) low = middle + 1;
            else high = middle;
        }
        return low;
    }
    public static (SpanMap? Map, Exception? Error) Unmarshal(byte[] bytes)
    {
        var (tuples, error) = JsonCodec.UnmarshalInt32Matrix(bytes);
        if (error is not null) return (null, error);
        var segments = new List<Segment>(tuples?.Count ?? 0);
        for (int i = 0; i < (tuples?.Count ?? 0); i++)
        {
            var tuple = tuples![i];
            int count = tuple?.Count ?? 0;
            if (count is not (5 or 6)) return (null, new Exception($"span map segment {i}: expected 5 or 6 values, got {count}"));
            var feature = count == 6 ? (Feature)tuple![5] : Feature.All;
            segments.Add(new(tuple![0], unchecked(tuple[0] + tuple[1]), tuple[2], unchecked(tuple[2] + tuple[3]), (Kind)tuple[4], feature));
        }
        return (New(segments), null);
    }
    public static (byte[]? Bytes, Exception? Error) Marshal(this SpanMap map)
    {
        var tuples = new List<object?>(map.segments?.Count ?? 0);
        foreach (var segment in map.segments ?? [])
        {
            List<object?> tuple = [segment.VirtualStart, unchecked(segment.VirtualEnd - segment.VirtualStart), segment.OriginalStart, unchecked(segment.OriginalEnd - segment.OriginalStart), (int)segment.Kind];
            if (segment.Features != Feature.All) tuple.Add((int)segment.Features);
            tuples.Add(tuple);
        }
        return JsonCodec.TryMarshal(tuples);
    }
}
internal sealed class OriginalIndex
{
    private readonly List<Segment>? segments;
    private readonly int leafCount;
    private readonly int[] maxEnds;
    private OriginalIndex(List<Segment>? segments, int leafCount, int[] maxEnds) => (this.segments, this.leafCount, this.maxEnds) = (segments, leafCount, maxEnds);
    internal static OriginalIndex Create(List<Segment>? input)
    {
        var segments = GoSlices.Clone(input);
        GoSlices.SortFunc(segments, (a, b) =>
        {
            int start = unchecked(a.OriginalStart - b.OriginalStart);
            if (start != 0) return start;
            int end = unchecked(a.OriginalEnd - b.OriginalEnd);
            return end != 0 ? end : unchecked(a.VirtualStart - b.VirtualStart);
        });
        int leafCount = 1;
        while (leafCount < (segments?.Count ?? 0)) leafCount *= 2;
        var maxEnds = new int[2 * leafCount];
        for (int i = 0; i < (segments?.Count ?? 0); i++) maxEnds[leafCount + i] = segments![i].OriginalEnd;
        for (int i = leafCount - 1; i > 0; i--) maxEnds[i] = Math.Max(maxEnds[2 * i], maxEnds[2 * i + 1]);
        return new(segments, leafCount, maxEnds);
    }
    internal (List<Segment>? Segments, bool Inside) SegmentsAtOriginalPosition(int position)
    {
        int start = SpanMaps.Search(segments?.Count ?? 0, i => segments![i].OriginalStart >= position);
        var results = SegmentsEndingAfterPosition(start, position);
        int end = SpanMaps.Search(segments?.Count ?? 0, i => segments![i].OriginalStart > position);
        if (end > start) (results ??= []).AddRange(segments!.GetRange(start, end - start));
        return (results, results is { Count: > 0 });
    }
    private List<Segment>? SegmentsEndingAfterPosition(int limit, int position)
    {
        List<Segment>? results = null;
        CollectSegmentsEndingAfter(1, 0, leafCount, limit, position, false, ref results);
        return results;
    }
    private void CollectSegmentsEndingAfter(int node, int start, int end, int limit, int position, bool includeEnd, ref List<Segment>? results)
    {
        if (start >= limit || maxEnds[node] < position || !includeEnd && maxEnds[node] == position) return;
        if (end - start == 1)
        {
            (results ??= []).Add(segments![start]);
            return;
        }
        int middle = start + (end - start) / 2;
        CollectSegmentsEndingAfter(2 * node, start, middle, limit, position, includeEnd, ref results);
        CollectSegmentsEndingAfter(2 * node + 1, middle, end, limit, position, includeEnd, ref results);
    }
    internal List<OriginalSegmentGroup>? SegmentGroupsAtOriginalPosition(int position)
    {
        int limit = SpanMaps.Search(segments?.Count ?? 0, i => segments![i].OriginalStart > position);
        List<Segment>? matches = null;
        CollectSegmentsEndingAfter(1, 0, leafCount, limit, position, true, ref matches);
        List<OriginalSegmentGroup>? groups = null;
        for (int i = 0; i < (matches?.Count ?? 0);)
        {
            int end = i + 1;
            while (end < matches!.Count && SpanMaps.SameOriginalRange(matches[i], matches[end])) end++;
            var segment = matches[i];
            if (position <= segment.OriginalEnd) (groups ??= []).Add(new(matches.GetRange(i, end - i), position == segment.OriginalEnd && position != segment.OriginalStart));
            i = end;
        }
        return groups;
    }
}
internal readonly record struct OriginalSegmentGroup(List<Segment> Segments, bool AtEnd);
