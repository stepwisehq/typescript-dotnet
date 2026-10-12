// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/sourcemap/decoder.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
namespace TypeScript.SourceMap;
internal readonly record struct Mapping(long GeneratedLine, long GeneratedCharacter, long SourceIndex, long SourceLine, long SourceCharacter, long NameIndex)
{
    internal bool IsSourceMapping() => SourceIndex != SourceMaps.MissingSource && SourceLine != SourceMaps.MissingLineOrColumn && SourceCharacter != SourceMaps.MissingUTF16Column;
}
internal static partial class SourceMaps
{
    internal const long MissingSource = -1, MissingName = -1, MissingLineOrColumn = -1, MissingUTF16Column = -1;
    internal static MappingsDecoder DecodeMappings(string mappings) => DecodeMappings(ByteStringValue.FromString(mappings));
    internal static MappingsDecoder DecodeMappings(ByteStringValue mappings) => new(mappings);
}
internal sealed class MappingsDecoder
{
    private readonly ByteStringValue mappings;
    private bool done;
    private int pos;
    private long generatedLine, generatedCharacter, sourceIndex, sourceLine, sourceCharacter, nameIndex;
    private Exception? error;
    internal MappingsDecoder(ByteStringValue mappings) => this.mappings = mappings.Snapshot();
    internal ByteStringValue MappingsString() => mappings;
    internal int Pos() => pos;
    internal Exception? Error() => error;
    internal Mapping State() => captureMapping(true, true);
    internal IEnumerable<Mapping> Values()
    {
        for (var (value, done) = Next(); !done; (value, done) = Next()) yield return value!;
    }
    internal (Mapping Value, bool Done) Next()
    {
        while (!done && pos < mappings.Length)
        {
            int ch = mappings[pos];
            if (ch == ';') { generatedLine++; generatedCharacter = 0; pos++; continue; }
            if (ch == ',') { pos++; continue; }
            bool hasSource = false, hasName = false;
            generatedCharacter = unchecked(generatedCharacter + base64VLQFormatDecode());
            if (hasReportedError()) return stopIterating();
            if (generatedCharacter < 0) return setErrorAndStopIterating("Invalid generatedCharacter found");
            if (!isSourceMappingSegmentEnd())
            {
                hasSource = true;
                sourceIndex = unchecked(sourceIndex + base64VLQFormatDecode());
                if (hasReportedError()) return stopIterating();
                if (sourceIndex < 0) return setErrorAndStopIterating("Invalid sourceIndex found");
                if (isSourceMappingSegmentEnd()) return setErrorAndStopIterating("Unsupported Format: No entries after sourceIndex");
                sourceLine = unchecked(sourceLine + base64VLQFormatDecode());
                if (hasReportedError()) return stopIterating();
                if (sourceLine < 0) return setErrorAndStopIterating("Invalid sourceLine found");
                if (isSourceMappingSegmentEnd()) return setErrorAndStopIterating("Unsupported Format: No entries after sourceLine");
                sourceCharacter = unchecked(sourceCharacter + base64VLQFormatDecode());
                if (hasReportedError()) return stopIterating();
                if (sourceCharacter < 0) return setErrorAndStopIterating("Invalid sourceCharacter found");
                if (!isSourceMappingSegmentEnd())
                {
                    hasName = true;
                    nameIndex = unchecked(nameIndex + base64VLQFormatDecode());
                    if (hasReportedError()) return stopIterating();
                    if (nameIndex < 0) return setErrorAndStopIterating("Invalid nameIndex found");
                    if (!isSourceMappingSegmentEnd()) return setErrorAndStopIterating("Unsupported Error Format: Entries after nameIndex");
                }
            }
            return (captureMapping(hasSource, hasName), false);
        }
        return stopIterating();
    }
    // Each returned mapping remains independent and retained, matching arena.New.
    private Mapping captureMapping(bool hasSource, bool hasName) => new(generatedLine, generatedCharacter, hasSource ? sourceIndex : SourceMaps.MissingSource, hasSource ? sourceLine : SourceMaps.MissingLineOrColumn, hasSource ? sourceCharacter : SourceMaps.MissingUTF16Column, hasName ? nameIndex : SourceMaps.MissingName);
    private (Mapping, bool) stopIterating() { done = true; return (default, true); }
    private void setError(string err) => error = new InvalidOperationException(err);
    private (Mapping, bool) setErrorAndStopIterating(string err) { setError(err); return stopIterating(); }
    private bool hasReportedError() => error != null;
    private bool isSourceMappingSegmentEnd() => pos == mappings.Length || mappings[pos] is ',' or ';';
    private long base64VLQFormatDecode()
    {
        bool moreDigits = true;
        int shiftCount = 0;
        long value = 0;
        for (; moreDigits; pos++)
        {
            if (pos >= mappings.Length) { setError("Error in decoding base64VLQFormatDecode, past the mapping string"); return -1; }
            int currentByte = base64FormatDecode(mappings[pos]);
            if (currentByte == -1) { setError("Invalid character in VLQ"); return -1; }
            moreDigits = (currentByte & 32) != 0;
            // Go variable shifts >= word width produce zero; .NET masks shifts.
            value |= shiftCount < 64 ? (long)(currentByte & 31) << shiftCount : 0;
            if (shiftCount < 64) shiftCount += 5;
        }
        if ((value & 1) == 0) value >>= 1;
        else { value >>= 1; value = unchecked(-value); }
        return value;
    }
    private static int base64FormatDecode(int ch) => ch switch { >= 'A' and <= 'Z' => ch-'A', >= 'a' and <= 'z' => ch-'a'+26, >= '0' and <= '9' => ch-'0'+52, '+' => 62, '/' => 63, _ => -1 };
}
