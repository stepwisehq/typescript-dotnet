// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/sourcemap/source_mapper.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Text.Json;
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.Json;
using Path = TypeScript.Scanner.TsPath;
namespace TypeScript.SourceMap;
internal interface Host
{
    CaseSensitivity CaseSensitivity();
    ECMALineInfo? GetECMALineInfo(RootedFilePath fileName);
    (ByteStringValue Text, bool Ok) ReadFile(RootedFilePath fileName);
}
internal sealed record DocumentPosition(RootedFilePath FileName, int Pos);
internal readonly record struct MappedPosition(int generatedPosition, int sourcePosition, long sourceIndex, long nameIndex)
{
    internal bool isSourceMappedPosition() => sourceIndex != SourceMaps.MissingSource && sourcePosition != -1;
}
internal sealed class DocumentPositionMapper
{
    internal CaseSensitivity caseSensitivity;
    internal RootedFilePath[] sourceFileAbsolutePaths = [];
    internal Dictionary<PathKey,List<MappedPosition>> sourceMappingsByPath = [];
    internal RootedFilePath generatedAbsoluteFilePath;
    internal List<MappedPosition> generatedMappings = [];
    internal Dictionary<int,List<MappedPosition>> sourceMappings = [];
    internal DocumentPosition? GetSourcePosition(DocumentPosition loc)
    {
        if (generatedMappings.Count == 0) return null;
        int targetIndex = binarySearch(generatedMappings, loc.Pos, false);
        if (targetIndex >= generatedMappings.Count) return null;
        var mapping = generatedMappings[targetIndex];
        if (!mapping.isSourceMappedPosition()) return null;
        return new(sourceFileAbsolutePaths[mapping.sourceIndex], mapping.sourcePosition);
    }
    internal DocumentPosition? GetGeneratedPosition(DocumentPosition loc)
    {
        if (!sourceMappingsByPath.TryGetValue(caseSensitivity.PathKey(loc.FileName.AsPath()), out var mappings) || mappings.Count == 0) return null;
        int targetIndex = binarySearch(mappings, loc.Pos, true);
        if (targetIndex >= mappings.Count) return null;
        return new(generatedAbsoluteFilePath, mappings[targetIndex].generatedPosition);
    }
    private static int binarySearch(List<MappedPosition> mappings, int pos, bool source)
    {
        int low = 0, high = mappings.Count;
        while (low < high)
        {
            int middle = low + (high-low)/2;
            long comparison = (long)(source ? mappings[middle].sourcePosition : mappings[middle].generatedPosition) - pos;
            if (comparison < 0) low = middle+1; else high = middle;
        }
        return low;
    }
}
internal static partial class SourceMaps
{
    private sealed record parsedRawSourceMap(RawSourceMap sourceMap, ByteStringValue? sourceRoot, bool[] nullSources);
    private static int compareSourcePositions(MappedPosition left, MappedPosition right) => left.sourcePosition.CompareTo(right.sourcePosition);
    private static DocumentPositionMapper? createDocumentPositionMapper(Host host, RawSourceMap sourceMap, ByteStringValue? sourceRootField, bool[] nullSources, RootedFilePath mapPath)
    {
        var mapDirectory = mapPath.Directory();
        ByteStringValue sourceURLPrefix = default;
        if (sourceRootField.HasValue && sourceRootField.Value.Length != 0)
        {
            sourceURLPrefix = sourceRootField.Value;
            if (!sourceURLPrefix.EndsWith("/"u8)) sourceURLPrefix = ByteStringValue.Concat(sourceURLPrefix, ByteStringValue.FromString("/"));
        }
        var (generatedAbsoluteFilePath, ok) = tryResolveSourceMapPath(sourceMap.File, mapDirectory);
        if (!ok) return null;
        var unmappedSources = new bool[sourceMap.Sources.Length];
        Array.Copy(nullSources, unmappedSources, Math.Min(nullSources.Length, unmappedSources.Length));
        var sourceFileAbsolutePaths = new RootedFilePath[sourceMap.Sources.Length];
        for (int i = 0; i < sourceMap.Sources.Length; i++)
        {
            if (unmappedSources[i]) continue;
            var sourceWithPrefix = ByteStringValue.Concat(sourceURLPrefix, sourceMap.Sources[i]);
            RootedFilePath resolved;
            if (sourceWithPrefix.Length == 0) resolved = mapPath;
            else
            {
                (resolved, ok) = tryResolveSourceMapPath(sourceWithPrefix, mapDirectory);
                if (!ok) { unmappedSources[i] = true; continue; }
            }
            sourceFileAbsolutePaths[i] = resolved;
        }
        var caseSensitivity = host.CaseSensitivity();
        var sourceToSourceIndexMap = new Dictionary<PathKey,List<int>>(sourceFileAbsolutePaths.Length);
        for (int i = 0; i < sourceFileAbsolutePaths.Length; i++)
        {
            if (unmappedSources[i]) continue;
            var key = caseSensitivity.PathKey(sourceFileAbsolutePaths[i].AsPath());
            if (!sourceToSourceIndexMap.TryGetValue(key, out var indices)) sourceToSourceIndexMap[key] = indices = [];
            indices.Add(i);
        }
        var decodedMappings = new List<MappedPosition>();
        var sourceMappings = new Dictionary<int,List<MappedPosition>>();
        var decoder = DecodeMappings(sourceMap.Mappings);
        foreach (var mapping in decoder.Values())
        {
            int generatedPosition = -1;
            var lineInfo = host.GetECMALineInfo(generatedAbsoluteFilePath);
            if (lineInfo != null) generatedPosition = computePosition(lineInfo, mapping.GeneratedLine, mapping.GeneratedCharacter);
            int sourcePosition = -1;
            // Go ints are 64-bit. Retain invalid/out-of-range indices without
            // truncation; they never participate in a source position lookup.
            long sourceIndex = mapping.SourceIndex;
            if (mapping.IsSourceMapping() && sourceIndex >= 0 && sourceIndex < sourceFileAbsolutePaths.Length && !unmappedSources[sourceIndex])
            {
                lineInfo = host.GetECMALineInfo(sourceFileAbsolutePaths[sourceIndex]);
                if (lineInfo != null) sourcePosition = computePosition(lineInfo, mapping.SourceLine, mapping.SourceCharacter);
            }
            decodedMappings.Add(new(generatedPosition, sourcePosition, sourceIndex, mapping.NameIndex));
        }
        if (decoder.Error() != null) decodedMappings.Clear();
        foreach (var mapping in decodedMappings)
        {
            if (!mapping.isSourceMappedPosition()) continue;
            if (!sourceMappings.TryGetValue((int)mapping.sourceIndex, out var list)) sourceMappings[(int)mapping.sourceIndex] = list = [];
            // The upstream mapper allocates independent mapped-position records.
            list.Add(new(mapping.generatedPosition, mapping.sourcePosition, mapping.sourceIndex, mapping.nameIndex));
        }
        foreach (var list in sourceMappings.Values)
        {
            GoSliceSort.SortFunc(list, (a,b) => { Debug.Assert(a.sourceIndex == b.sourceIndex, "All source mappings should have the same source index"); return compareSourcePositions(a,b); });
            deduplicateSorted(list);
        }
        var sourceMappingsByPath = new Dictionary<PathKey,List<MappedPosition>>(sourceToSourceIndexMap.Count);
        foreach (var (path, sourceIndices) in sourceToSourceIndexMap)
        {
            var mappings = new List<MappedPosition>();
            foreach (int sourceIndex in sourceIndices) if (sourceMappings.TryGetValue(sourceIndex, out var list)) mappings.AddRange(list);
            GoSliceSort.SortFunc(mappings, compareSourcePositions); sourceMappingsByPath[path] = mappings;
        }
        var generatedMappings = decodedMappings;
        GoSliceSort.SortFunc(generatedMappings, (a,b) => a.generatedPosition.CompareTo(b.generatedPosition));
        deduplicateSorted(generatedMappings);
        return new() { caseSensitivity = caseSensitivity, sourceFileAbsolutePaths = sourceFileAbsolutePaths, sourceMappingsByPath = sourceMappingsByPath, generatedAbsoluteFilePath = generatedAbsoluteFilePath, generatedMappings = generatedMappings, sourceMappings = sourceMappings };
    }
    private static int computePosition(ECMALineInfo li, long line, long character)
    {
        // allowEdits clamps lines to the available source and characters to its
        // line end. Saturation preserves this for decoded 64-bit VLQ offsets.
        int line32 = (int)Math.Clamp(line, int.MinValue, int.MaxValue);
        int character32 = (int)Math.Clamp(character, int.MinValue, int.MaxValue);
        return TypeScript.Scanner.Scanner.ComputePositionOfLineAndUTF16Character(li.lineStarts.AsSpan(), line32, character32, li.text, true);
    }
    private static void deduplicateSorted(List<MappedPosition> list)
    {
        if (list.Count < 2) return;
        int write = 1;
        for (int read = 1; read < list.Count; read++)
        {
            var a = list[write-1]; var b = list[read];
            if (a.generatedPosition != b.generatedPosition || a.sourceIndex != b.sourceIndex || a.sourcePosition != b.sourcePosition) list[write++] = b;
        }
        if (write < list.Count) list.RemoveRange(write, list.Count-write);
    }
    private static (RootedFilePath Path, bool Ok) tryResolveSourceMapPath(ByteStringValue path, RootedDirectoryPath directory)
    {
        if (path.Length == 0) return (default, false);
        return Path.TryRootedFilePathFromAbsolute(Path.PathIsAbsolute(path) ? path : Path.CombinePaths(directory.AsValue(), path));
    }
    internal static DocumentPositionMapper? GetDocumentPositionMapper(Host host, RootedFilePath generatedFileName)
    {
        var mapFileName = tryGetSourceMappingURL(host, generatedFileName);
        if (mapFileName.Length != 0)
        {
            var (base64Object, matched) = tryParseBase64Url(mapFileName);
            if (matched)
            {
                if (base64Object.Length != 0)
                {
                    try
                    {
                        // Go StdEncoding ignores CR/LF, but the URL parser above
                        // already rejects them; do not accept .NET whitespace.
                        var bytes = Convert.FromBase64String(base64Object.Materialize()!);
                        return convertDocumentToSourceMapper(host, ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes)), generatedFileName);
                    }
                    catch (FormatException) { }
                }
                mapFileName = default;
            }
        }
        var possibleMapLocations = new List<ByteStringValue>();
        if (mapFileName.Length != 0) possibleMapLocations.Add(mapFileName);
        possibleMapLocations.Add(generatedFileName.AppendSuffix(".map").AsValue());
        foreach (var location in possibleMapLocations)
        {
            var (mapPath, ok) = tryResolveSourceMapPath(location, generatedFileName.Directory());
            if (!ok) continue;
            var (mapFileContents, found) = host.ReadFile(mapPath);
            if (found) return convertDocumentToSourceMapper(host, mapFileContents, mapPath);
        }
        return null;
    }
    internal static DocumentPositionMapper? convertDocumentToSourceMapper(Host host, string contents, RootedFilePath mapFileName) => convertDocumentToSourceMapper(host, ByteStringValue.FromString(contents), mapFileName);
    internal static DocumentPositionMapper? convertDocumentToSourceMapper(Host host, ByteStringValue contents, RootedFilePath mapFileName)
    {
        var parsed = tryParseRawSourceMap(contents);
        if (parsed == null || parsed.sourceMap.Sources.Length == 0 || parsed.sourceMap.File.Length == 0 || parsed.sourceMap.Mappings.Length == 0) return null;
        if (parsed.sourceMap.SourcesContent?.Any(s => s.HasValue) == true) return null;
        return createDocumentPositionMapper(host, parsed.sourceMap, parsed.sourceRoot, parsed.nullSources, mapFileName);
    }
    private static parsedRawSourceMap? tryParseRawSourceMap(ByteStringValue contents)
    {
        // Typed Go struct unmarshalling skips unknown values lexically; generic
        // float64 decoding would reject valid unknown numbers such as 1e1000.
        string input = contents.Materialize()!;
        if (TypeScript.Json.Internal.JsonDiagnosticReader.Analyze(input, false, validateNumbers: false) != null) return null;
        byte[] bytes = contents.IsBytes ? contents.ByteSpan.ToArray() : GoString.Bytes(input);
        var (fields, isNull, error) = JsonCodec.ParseObjectFields(bytes);
        if (error != null || isNull || fields == null) return null;
        try
        {
            var encoded = new OrderedMap<string,object?>();
            long number = 0;
            foreach (var (name, raw) in fields)
            {
                if (name == "version")
                {
                    using var document = JsonDocument.Parse(raw.Bytes!);
                    // Numeric spelling matters for an int field: JSON 3.0 is not 3.
                    if (document.RootElement.ValueKind == JsonValueKind.Null) number = 0;
                    else if (!document.RootElement.TryGetInt64(out number)) return null;
                }
                else if (name is "file" or "sourceRoot" or "sources" or "names" or "mappings" or "sourcesContent")
                {
                    var (value, fieldError) = JsonCodec.Parse(raw);
                    if (fieldError != null) return null;
                    encoded.Set(name, value);
                }
            }
            if (number != 3) return null;
            object? field(string name) => encoded.GetOrZero(name);
            ByteStringValue readString(object? item) => item == null ? default : item is string text ? ByteStringValue.FromString(text) : throw new FormatException();
            ByteStringValue? readNullableString(object? item) => item == null ? null : readString(item);
            ByteStringValue?[]? readArray(object? item) => item == null ? null : item is List<object?> items ? items.Select(readNullableString).ToArray() : throw new FormatException();
            var sourceEntries = readArray(field("sources")) ?? [];
            return new(new RawSourceMap { Version = 3, File = readString(field("file")), Sources = sourceEntries.Select(s => s ?? default).ToArray(), Names = (readArray(field("names")) ?? []).Select(s => s ?? default).ToArray(), Mappings = readString(field("mappings")), SourcesContent = readArray(field("sourcesContent")) }, readNullableString(field("sourceRoot")), sourceEntries.Select(s => !s.HasValue).ToArray());
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException) { return null; }
    }
    private static ByteStringValue tryGetSourceMappingURL(Host host, RootedFilePath fileName) => TryGetSourceMappingURL(host.GetECMALineInfo(fileName));
    private static (ByteStringValue ParseableUrl, bool IsBase64Url) tryParseBase64Url(ByteStringValue url)
    {
        if (!url.StartsWith("data:"u8)) return (default, false);
        url = url.Slice(5);
        if (!url.StartsWith("application/json;"u8)) return (default, true);
        url = url.Slice(17);
        if (url.StartsWith("charset="u8))
        {
            url = url.Slice(8);
            // Upstream slices without a length guard (short values panic).
            var charset = url.Slice(0, 6);
            if (!UnicodeCase.EqualFold(charset, ByteStringValue.FromString("utf-8;"))) return (default, true);
            url = url.Slice(6);
        }
        if (!url.StartsWith("base64,"u8)) return (default, true);
        url = url.Slice(7);
        for (int i = 0; i < url.Length; i++)
        {
            int r = url[i];
            if (!(StringUtil.IsASCIILetter(r) || StringUtil.IsDigit(r) || r is '+' or '/' or '=')) return (default, true);
        }
        return (url, true);
    }
}
