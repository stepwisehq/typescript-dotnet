// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/lsconv/converters.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.Lsp.Protocol;
using LspRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.LanguageServices.Lsconv;
internal sealed class Converters
{
    private readonly Func<RootedFilePath, LSPLineMap> getLineMap;
    private readonly PositionEncodingKind positionEncoding;
    private Converters(PositionEncodingKind encoding, Func<RootedFilePath, LSPLineMap> getLineMap)
    {
        positionEncoding = encoding;
        this.getLineMap = getLineMap;
    }

    internal static Converters NewConverters(PositionEncodingKind encoding, Func<RootedFilePath, LSPLineMap> getLineMap) => new(encoding, getLineMap);
    internal (LspRange Range, Fidelity Fidelity) ToLSPRange(IScript script, TextRange range) => ToLSPRangeWorker(script, range, null);
    internal (LspRange Range, Fidelity Fidelity) ToLSPRangeForFeature(IScript script, TextRange range, Feature feature) => ToLSPRangeWorker(script, range, feature);
    private (LspRange, Fidelity) ToLSPRangeWorker(IScript script, TextRange range, Feature? feature)
    {
        var(original, mapped, fidelity) = Lsconv.VirtualRangeToOriginal(script, range, feature);
        return (new LspRange { Start = PositionToLineAndCharacter(original, mapped.Pos()), End = PositionToLineAndCharacter(original, mapped.End()) }, fidelity);
    }

    internal (Position Position, Fidelity Fidelity) ToLSPPosition(IScript script, int position) => ToLSPPositionWorker(script, position, null);
    internal (Position Position, Fidelity Fidelity) ToLSPPositionForFeature(IScript script, int position, Feature feature) => ToLSPPositionWorker(script, position, feature);
    private (Position, Fidelity) ToLSPPositionWorker(IScript script, int position, Feature? feature)
    {
        var(original, mapped, fidelity) = Lsconv.VirtualPositionToOriginal(script, position, feature);
        return (PositionToLineAndCharacter(original, mapped), fidelity);
    }

    internal (Location Location, Fidelity Fidelity) ToLSPLocation(IScript script, TextRange range)
    {
        var(mapped, fidelity) = ToLSPRange(script, range);
        return (new Location { Uri = Lsconv.FileNameToDocumentURI(script.OriginalFileName()), Range = mapped }, fidelity);
    }

    internal (Location Location, Fidelity Fidelity) ToLSPLocationForFeature(IScript script, TextRange range, Feature feature)
    {
        var(mapped, fidelity) = ToLSPRangeForFeature(script, range, feature);
        return (new Location { Uri = Lsconv.FileNameToDocumentURI(script.OriginalFileName()), Range = mapped }, fidelity);
    }

    internal Slice<MappedSpan<T>> FromLSPRange<T>(T script, LspRange range, Feature feature)
        where T : IScript => LspRangeToVirtualForScripts(Slice<T>.FromArray([script]), range, feature);
    internal Slice<MappedSpan<SourceFile>> FromLSPRangeForSourceFile(SourceFile file, LspRange range, Feature feature) => LspRangeToVirtualForScripts(Lsconv.SourceFileProjections(file), range, feature);
    internal Slice<MappedSpan<SourceFile>> FromLSPRangeIntersectingForSourceFile(SourceFile file, LspRange range, Feature feature)
    {
        var files = Lsconv.SourceFileProjections(file);
        var result = Slice<MappedSpan<SourceFile>>.Make(0, files.Count);
        foreach (var script in files)
        {
            var spans = script.SpanMap();
            if (spans is null)
            {
                result = result.Append(new MappedSpan<SourceFile>(script, new(TextRangeForScript(script, range), Fidelity.Exact)));
                continue;
            }

            var original = new OriginalTextScript(script.OriginalFileName(), ((IScript)script).OriginalTextValue());
            var mappings = spans.OriginalToVirtualIntersectingSpans(TextRangeForScript(original, range), feature);
            if (mappings is not null)
                foreach (var mapping in mappings)
                    result = result.Append(new MappedSpan<SourceFile>(script, mapping));
        }

        return result;
    }

    private Slice<MappedSpan<T>> LspRangeToVirtualForScripts<T>(Slice<T> scripts, LspRange range, Feature feature)
        where T : IScript
    {
        var result = Slice<MappedSpan<T>>.Make(0, scripts.Count);
        foreach (var script in scripts)
        {
            var mappings = LspRangeToVirtual(script, range, feature);
            if (mappings is not null)
                foreach (var mapping in mappings)
                    result = result.Append(new MappedSpan<T>(script, mapping));
        }

        return result;
    }

    private List<TypeScript.SpanMap.MappedSpan>? LspRangeToVirtual(IScript script, LspRange range, Feature feature)
    {
        var spans = script.SpanMap();
        if (spans is null)
            return[new(TextRangeForScript(script, range), Fidelity.Exact)];
        var original = new OriginalTextScript(script.OriginalFileName(), script.OriginalTextValue());
        return spans.OriginalToVirtualSpans(TextRangeForScript(original, range), feature);
    }

    internal Slice<MappedPosition<T>> FromLSPPosition<T>(T script, Position position, Feature feature)
        where T : IScript => LspPositionToVirtualForScripts(Slice<T>.FromArray([script]), position, feature);
    internal Slice<MappedPosition<SourceFile>> FromLSPPositionForSourceFile(SourceFile file, Position position, Feature feature) => LspPositionToVirtualForScripts(Lsconv.SourceFileProjections(file), position, feature);
    internal TextRange FromLSPRangeToOriginal(IScript script, LspRange range) => TextRangeForScript(new OriginalTextScript(script.OriginalFileName(), script.OriginalTextValue()), range);
    private TextRange TextRangeForScript(IScript script, LspRange range) => new(LineAndCharacterToPosition(script, range.Start), LineAndCharacterToPosition(script, range.End));
    private Slice<MappedPosition<T>> LspPositionToVirtualForScripts<T>(Slice<T> scripts, Position position, Feature feature)
        where T : IScript
    {
        var result = Slice<MappedPosition<T>>.Make(0, scripts.Count);
        foreach (var script in scripts)
        {
            var mappings = LspPositionToVirtual(script, position, feature);
            if (mappings is not null)
                foreach (var mapping in mappings)
                    result = result.Append(new MappedPosition<T>(script, mapping));
        }

        return result;
    }

    private List<TypeScript.SpanMap.MappedPosition>? LspPositionToVirtual(IScript script, Position position, Feature feature)
    {
        var spans = script.SpanMap();
        if (spans is null)
            return[new(LineAndCharacterToPosition(script, position), Fidelity.Exact)];
        var original = new OriginalTextScript(script.OriginalFileName(), script.OriginalTextValue());
        return spans.OriginalToVirtualPositions(LineAndCharacterToPosition(original, position), feature);
    }

    // core.TextPos is int32: preserve the source uint32 casts and wrapping
    // addition before clamping, including out-of-range editor coordinates.
    internal int LineAndCharacterToPosition(IScript script, Position coordinate)
    {
        Debug.Assert(script.SpanMap()is null, "raw coordinate conversion requires a non-content-mapped script");
        var map = getLineMap(script.FileName());
        var text = script.TextValue();
        int line = unchecked((int)coordinate.Line);
        int targetCharacter = unchecked((int)coordinate.Character);
        if (line >= map.LineStarts.Count)
            return text.Length;
        int start = map.LineStarts[line];
        int end = line + 1 < map.LineStarts.Count ? map.LineStarts[line + 1] : text.Length;
        if (map.AsciiOnly || positionEncoding == PositionEncodingKind.UTF8)
            return Math.Max(start, Math.Min(unchecked(start + targetCharacter), end));
        int character = 0;
        int pos = start;
        while (pos < end)
        {
            var(rune, size) = Lsconv.DecodeRune(text.Slice(pos));
            int units = rune > 0xffff ? 2 : 1;
            if (unchecked(character + units) > targetCharacter)
                break;
            character = unchecked(character + units);
            pos += size;
        }

        return pos;
    }

    internal Position PositionToLineAndCharacter(IScript script, int position)
    {
        Debug.Assert(script.SpanMap()is null, "raw coordinate conversion requires a non-content-mapped script");
        var text = script.TextValue();
        position = Math.Clamp(position, 0, text.Length);
        var map = getLineMap(script.FileName());
        var(line, found) = GoSlices.BinarySearchFunc(map.LineStarts, position, static (x, y) => x.CompareTo(y));
        if (!found)
            line--;
        line = Math.Clamp(line, 0, map.LineStarts.Count - 1);
        int start = map.LineStarts[line];
        int character = 0;
        if (map.AsciiOnly || positionEncoding == PositionEncodingKind.UTF8)
            character = position - start;
        else
        {
            var segment = text.Slice(start, position);
            for (int pos = 0; pos < segment.Length;)
            {
                var(rune, size) = Lsconv.DecodeRune(segment.Slice(pos));
                character += rune > 0xffff ? 2 : 1;
                pos += size;
            }
        }

        return new Position
        {
            Line = (uint)line,
            Character = (uint)character
        };
    }
}

internal readonly record struct OriginalTextScript(RootedFilePath Name, ByteStringValue Text) : IScript
{
    public RootedFilePath FileName() => Name;
    public RootedFilePath OriginalFileName() => Name;
    public ByteStringValue TextValue() => Text;
    public ByteStringValue OriginalTextValue() => Text;
    public TypeScript.SpanMap.SpanMap? SpanMap() => null;
}

internal static partial class Lsconv
{
    internal static (int Rune, int Size) DecodeRune(ByteStringValue text) => text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan) : Utf8.DecodeRuneInString(text.CharSpan);
    internal static Slice<SourceFile> SourceFileProjections(SourceFile file)
    {
        var supplemental = file.SupplementalSourceFiles();
        var files = Slice<SourceFile>.Make(1, 1 + supplemental.Count);
        files[0] = file;
        return files.Append(supplemental);
    }

    internal static (IScript Script, TextRange Range, Fidelity Fidelity) VirtualRangeToOriginal(IScript script, TextRange range, Feature? feature)
    {
        if (script.SpanMap()is null)
            return (script, range, Fidelity.Exact);
        var(mapped, fidelity) = feature is null ? script.SpanMap().VirtualToOriginalSpan(range) : script.SpanMap().VirtualToOriginalSpanForFeature(range, feature.Value);
        return (new OriginalTextScript(script.OriginalFileName(), script.OriginalTextValue()), mapped, fidelity);
    }

    internal static (IScript Script, int Position, Fidelity Fidelity) VirtualPositionToOriginal(IScript script, int position, Feature? feature)
    {
        if (script.SpanMap()is null)
            return (script, position, Fidelity.Exact);
        var(mapped, fidelity) = feature is null ? script.SpanMap().VirtualToOriginalPosition(position) : script.SpanMap().VirtualToOriginalPositionForFeature(position, feature.Value);
        return (new OriginalTextScript(script.OriginalFileName(), script.OriginalTextValue()), mapped, fidelity);
    }

    internal static ScriptKind LanguageKindToScriptKind(LanguageKind language)
    {
        var value = language.Value;
        if (value.EqualsBytes("typescript"u8))
            return ScriptKind.TS;
        if (value.EqualsBytes("typescriptreact"u8))
            return ScriptKind.TSX;
        if (value.EqualsBytes("javascript"u8))
            return ScriptKind.JS;
        if (value.EqualsBytes("javascriptreact"u8))
            return ScriptKind.JSX;
        if (value.EqualsBytes("json"u8))
            return ScriptKind.JSON;
        return ScriptKind.Unknown;
    }

    internal static DocumentUri FileNameToDocumentURI(RootedFilePath path) => PathToDocumentURI(path.AsPath());
    internal static DocumentUri PathToDocumentURI(RootedPath rootedPath)
    {
        var path = rootedPath.AsValue();
        if (TypeScript.Bundled.Bundled.IsBundled(path))
            return new(path);
        if (rootedPath.IsDynamic())
            return ProtocolUris.DynamicFileNameToDocumentUri(rootedPath);
        var(volume, remainder, _) = TsPath.SplitVolumePath(path);
        path = remainder;
        if (volume.Length != 0)
            volume = ByteStringValue.Concat(ByteStringValue.FromString("/"), EscapeUriSegment(volume));
        if (path.StartsWith("//"u8))
            path = path.Slice(2);
        // strings.Split's owned header array; each source segment is borrowed.
        int count = 1;
        for (int i = 0; i < path.Length; i++)
            if (path[i] == '/')
                count++;
        var parts = Slice<ByteStringValue>.Make(count);
        int start = 0, partIndex = 0;
        for (int i = 0; i <= path.Length; i++)
        {
            if (i != path.Length && path[i] != '/')
                continue;
            parts[partIndex++] = EscapeUriSegment(path.Slice(start, i));
            start = i + 1;
        }

        var output = new GoBuilder();
        output.WriteString("file://");
        output.WriteString(volume);
        for (int i = 0; i < parts.Count; i++)
        {
            if (i != 0)
                output.WriteByte('/');
            output.WriteString(parts[i]);
        }

        return new(output.Value());
    }

    private static ByteStringValue EscapeUriSegment(ByteStringValue segment)
    {
        // url.PathEscape followed by extraEscapeReplacer escapes precisely all
        // non-unreserved bytes. Fuse only the two byte transforms; retain the
        // original unchanged header fast path and each changed segment owner.
        bool needsEscape = false;
        for (int i = 0; i < segment.Length; i++)
            if (!IsUriUnreserved(segment[i]))
            {
                needsEscape = true;
                break;
            }

        if (!needsEscape)
            return segment;
        var output = new GoBuilder();
        for (int i = 0; i < segment.Length; i++)
            EscapeUriByte(ref output, segment[i]);
        return output.Value();
    }

    private static bool IsUriUnreserved(int value) => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_' or '.' or '~';
    private static void EscapeUriByte(ref GoBuilder output, int value)
    {
        if (IsUriUnreserved(value))
            output.WriteByte((byte)value);
        else
        {
            output.WriteByte('%');
            output.WriteByte((byte)"0123456789ABCDEF"[value >> 4]);
            output.WriteByte((byte)"0123456789ABCDEF"[value & 15]);
        }
    }
}
