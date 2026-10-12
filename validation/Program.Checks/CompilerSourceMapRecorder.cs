// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of internal/testutil/harnessutil/sourcemap_recorder.go and
// harnessutil.go:909 GetSourceMapRecord. Test observation only, never compiler output.
using System.Globalization;
using System.Text;
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.SourceMap;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace Program.Checks;
public sealed partial class CompilerEmitCompilation
{
    // harnessutil.go:909.
    public string GetSourceMapRecord()
    {
        if (Result is null || Result.SourceMaps.Count == 0)
            return "";
        var recorder = new SourceMapWriterAggregator();
        foreach (var data in Result.SourceMaps)
        {
            SourceFile? previousSource = null;
            var generated = TsPath.IsDeclarationFileName(data.GeneratedFile) ? DTS : JS;
            var generatedPath = TsPath.ToRootedFilePath(data.GeneratedFile, CurrentDirectory());
            var file = generated.FirstOrDefault(item => TsPath.ToRootedFilePath(item.UnitName, CurrentDirectory()) == generatedPath) ?? throw new InvalidOperationException("Source map generated file was not emitted.");
            var writer = new SourceMapSpanWriter(recorder, data.SourceMap, file);
            var mappings = SourceMaps.DecodeMappings(data.SourceMap.Mappings);
            for (var(mapping, done) = mappings.Next(); !done; (mapping, done) = mappings.Next())
            {
                if (!mapping.IsSourceMapping())
                {
                    writer.RecordSourceMapSpan(mapping);
                    continue;
                }

                var source = Program.GetSourceFile(TsPath.ToRootedFilePath(data.InputSourceFileNames[checked((int)mapping.SourceIndex)], CurrentDirectory()));
                if (!ReferenceEquals(source, previousSource))
                {
                    if (source is not null)
                        writer.RecordNewSourceFileSpan(mapping, source.OriginalText());
                    previousSource = source;
                }
                else
                    writer.RecordSourceMapSpan(mapping);
            }

            writer.Close();
        }

        return recorder.ToString();
    }
}

// sourcemap_recorder.go:16 writerAggregator. Raw Go strings, CRLF record lines.
internal sealed class SourceMapWriterAggregator
{
    private readonly StringBuilder builder = new();
    internal void WriteString(string text) => builder.Append(text);
    // sourcemap_recorder.go:20. All original callsites use supported %s/%d verbs.
    internal void WriteStringf(string format, params object[] arguments) => builder.Append(Fmt.Sprintf(format, arguments));
    // sourcemap_recorder.go:29.
    internal void WriteLinef(string format, params object[] arguments) => WriteStringf(format + "\r\n", arguments);
    internal void WriteLine(string text)
    {
        builder.Append(text);
        builder.Append("\r\n");
    }

    public override string ToString() => builder.ToString();
}

internal readonly record struct SourceMapSpanWithDecodeErrors(Mapping Span, string[]? DecodeErrors);
internal readonly record struct DecodedSourceMapMapping(Mapping Span, Exception? Error);
// sourcemap_recorder.go:46 sourceMapDecoder.
internal sealed class SourceMapRecordDecoder
{
    private readonly ByteStringValue encoded;
    private readonly MappingsDecoder mappings;
    internal SourceMapRecordDecoder(RawSourceMap sourceMap)
    {
        encoded = sourceMap.Mappings;
        mappings = SourceMaps.DecodeMappings(encoded);
    }

    internal DecodedSourceMapMapping DecodeNextEncodedSourceMapSpan()
    {
        var(value, done) = mappings.Next();
        return done ? new(mappings.State(), mappings.Error() ?? new InvalidOperationException("No encoded entry found")) : new(value, null);
    }

    internal bool HasCompletedDecoding() => mappings.Pos() == encoded.Length;
    internal string GetRemainingDecodeString() => encoded.Slice(mappings.Pos()).Materialize()!;
}

// sourcemap_recorder.go:78 sourceMapSpanWriter.
internal sealed class SourceMapSpanWriter
{
    internal readonly SourceMapWriterAggregator Recorder;
    private readonly ByteStringValue[] sources, names;
    internal readonly CompilerEmitTestFile JSFile;
    internal readonly Slice<int> JSLineMap;
    internal string TSCode = "";
    internal ByteStringView TSCodeView;
    internal Slice<int> TSLineMap;
    internal Slice<SourceMapSpanWithDecodeErrors> SpansOnSingleLine = Slice<SourceMapSpanWithDecodeErrors>.Make(0);
    internal int PreviousWrittenSourcePosition;
    private int nextJSLineToWrite;
    internal bool SpanMarkerContinues;
    private readonly SourceMapRecordDecoder decoder;
    // sourcemap_recorder.go:93.
    internal SourceMapSpanWriter(SourceMapWriterAggregator recorder, RawSourceMap map, CompilerEmitTestFile file)
    {
        Recorder = recorder;
        sources = map.Sources;
        names = map.Names;
        JSFile = file;
        JSLineMap = Core.ComputeECMALineStarts(file.Content);
        decoder = new(map);
        recorder.WriteLine("===================================================================");
        recorder.WriteLinef("JsFile: %s", map.File.Materialize() ?? "");
        recorder.WriteLine("mapUrl: " + SourceMaps.TryGetSourceMappingURL(ECMALineInfo.CreateECMALineInfo(file.Content, JSLineMap)).Materialize());
        recorder.WriteLinef("sourceRoot: %s", map.SourceRoot.Materialize() ?? "");
        recorder.WriteLine("sources: " + string.Join(",", map.Sources.Select(static source => source.Materialize())));
        if (map.SourcesContent is { Length: > 0 })
        {
            var content = JsonCodec.Marshal(map.SourcesContent.Select(static text => text.HasValue ? (object)text.Value : null).ToArray());
            recorder.WriteLine("sourcesContent: " + GoString.FromBytes(content));
        }

        recorder.WriteLine("===================================================================");
    }

    // sourcemap_recorder.go:123.
    internal string GetSourceMapSpanString(Mapping mapping, bool getAbsentNameIndex)
    {
        var text = new StringBuilder();
        text.Append(CultureInvariant($"Emitted({mapping.GeneratedLine + 1}, {mapping.GeneratedCharacter + 1})"));
        if (mapping.IsSourceMapping())
        {
            text.Append(CultureInvariant($" Source({mapping.SourceLine + 1}, {mapping.SourceCharacter + 1}) + SourceIndex({mapping.SourceIndex})"));
            if (mapping.NameIndex >= 0 && mapping.NameIndex < names.Length)
                text.Append(" name (" + names[checked((int)mapping.NameIndex)].Materialize() + ")");
            else if (mapping.NameIndex != SourceMaps.MissingName || getAbsentNameIndex)
                text.Append(CultureInvariant($" nameIndex ({mapping.NameIndex})"));
        }

        return text.ToString();
    }

    // sourcemap_recorder.go:139.
    internal void RecordSourceMapSpan(Mapping span)
    {
        var decoded = decoder.DecodeNextEncodedSourceMapSpan();
        string[]? errors = null;
        if (decoded.Error is not null || decoded.Span != span)
            errors = [decoded.Error is not null ? "!!^^ !!^^ There was decoding error in the sourcemap at this location: " + decoded.Error.Message : "!!^^ !!^^ The decoded span from sourcemap's mapping entry does not match what was encoded for this span:", "!!^^ !!^^ Decoded span from sourcemap's mappings entry: " + GetSourceMapSpanString(decoded.Span, true) + " Span encoded by the emitter:" + GetSourceMapSpanString(span, true)];
        if (SpansOnSingleLine.Count > 0 && SpansOnSingleLine[0].Span.GeneratedLine != span.GeneratedLine)
        {
            WriteRecordedSpans();
            SpansOnSingleLine = default;
        }

        SpansOnSingleLine = SpansOnSingleLine.Append(new SourceMapSpanWithDecodeErrors(span, errors));
    }

    // sourcemap_recorder.go:169. Preserve pinned char==line comparison, including its upstream TODO.
    internal void RecordNewSourceFileSpan(Mapping span, string source)
    {
        bool continuesLine = false;
        if (SpansOnSingleLine.Count > 0 && SpansOnSingleLine[0].Span.GeneratedCharacter == span.GeneratedLine)
        {
            WriteRecordedSpans();
            SpansOnSingleLine = default;
            nextJSLineToWrite--;
            continuesLine = true;
        }

        RecordSourceMapSpan(span);
        if (SpansOnSingleLine.Count != 1)
            throw new InvalidOperationException("expected a single span");
        Recorder.WriteLine("-------------------------------------------------------------------");
        Recorder.WriteLine(continuesLine ? "emittedFile:" + JSFile.UnitName + CultureInvariant($" ({span.GeneratedLine + 1}, {span.GeneratedCharacter + 1})") : "emittedFile:" + JSFile.UnitName);
        Recorder.WriteLine("sourceFile:" + sources[checked((int)SpansOnSingleLine[0].Span.SourceIndex)].Materialize());
        Recorder.WriteLine("-------------------------------------------------------------------");
        TSLineMap = Core.ComputeECMALineStarts(source);
        TSCode = source;
        TSCodeView = ByteStringView.FromOwnedBytes(GoString.Bytes(source));
        PreviousWrittenSourcePosition = 0;
    }

    // sourcemap_recorder.go:198.
    internal void Close()
    {
        WriteRecordedSpans();
        if (!decoder.HasCompletedDecoding())
        {
            Recorder.WriteLine("!!!! **** There are more source map entries in the sourceMap's mapping than what was encoded");
            Recorder.WriteLine("!!!! **** Remaining decoded string: " + decoder.GetRemainingDecodeString());
        }

        WriteJSFileLines(JSLineMap.Count);
    }

    // sourcemap_recorder.go:211. Positions index raw UTF8 bytes, never UTF16 CLR characters.
    internal string GetTextOfLine(int line, Slice<int> lineMap, string code)
    {
        int start = lineMap[line];
        int end = line + 1 < lineMap.Count ? lineMap[line + 1] : code.Length;
        string text = code[start..end];
        if (line != 0)
            return text;
        // stringutil/util.go:182–212, all three raw-byte BOMs.
        if (text.StartsWith("\xEF\xBB\xBF", StringComparison.Ordinal))
            return text[3..];
        if (text.StartsWith("\xFE\xFF", StringComparison.Ordinal) || text.StartsWith("\xFF\xFE", StringComparison.Ordinal))
            return text[2..];
        return text;
    }

    // sourcemap_recorder.go:227.
    internal void WriteJSFileLines(int endLine)
    {
        for (; nextJSLineToWrite < endLine; nextJSLineToWrite++)
            Recorder.WriteString(">>>" + GetTextOfLine(nextJSLineToWrite, JSLineMap, JSFile.Content));
    }

    // sourcemap_recorder.go:233.
    private void WriteRecordedSpans() => new RecordedSourceMapSpanWriter(this).WriteRecordedSpans();
    internal static string CultureInvariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}

// sourcemap_recorder.go:238 recordedSpanWriter, one synchronous owner per recorded line.
internal sealed class RecordedSourceMapSpanWriter
{
    private Slice<string> markerIds;
    private int previousEmittedColumn;
    private readonly SourceMapSpanWriter writer;
    private readonly Action<SourceMapSpanWithDecodeErrors, int> writeMarker, writeSourceText, writeDetails;
    internal RecordedSourceMapSpanWriter(SourceMapSpanWriter writer)
    {
        this.writer = writer;
        writeMarker = WriteSourceMapMarker;
        writeSourceText = WriteSourceMapSourceText;
        writeDetails = WriteSpanDetails;
    }

    // sourcemap_recorder.go:244.
    private string GetMarkerId(int index)
    {
        if (writer.SpanMarkerContinues)
        {
            if (index != 0)
                throw new InvalidOperationException("expected markerIndex to be 0");
            return "1->";
        }

        string id = (index + 1).ToString(CultureInfo.InvariantCulture);
        if (id.Length < 2)
            id += " ";
        return id + ">";
    }

    // sourcemap_recorder.go:261.
    private void IterateSpans(Action<SourceMapSpanWithDecodeErrors, int> callback)
    {
        previousEmittedColumn = 0;
        for (int i = 0; i < writer.SpansOnSingleLine.Count; i++)
        {
            callback(writer.SpansOnSingleLine[i], i);
            previousEmittedColumn = checked((int)writer.SpansOnSingleLine[i].Span.GeneratedCharacter);
        }
    }

    // sourcemap_recorder.go:269.
    private void WriteSourceMapIndent(int length, string prefix)
    {
        writer.Recorder.WriteString(prefix);
        for (int i = 0; i < length; i++)
            writer.Recorder.WriteString(" ");
    }

    // sourcemap_recorder.go:276.
    private void WriteSourceMapMarker(SourceMapSpanWithDecodeErrors span, int index) => WriteSourceMapMarkerEx(index, checked((int)span.Span.GeneratedCharacter), false);
    // sourcemap_recorder.go:280. Original currentSpan is unused, so no fabricated mapping is needed.
    private void WriteSourceMapMarkerEx(int index, int endColumn, bool endContinues)
    {
        string id = GetMarkerId(index);
        markerIds = markerIds.Append(id);
        WriteSourceMapIndent(previousEmittedColumn, id);
        for (int i = previousEmittedColumn; i < endColumn; i++)
            writer.Recorder.WriteString("^");
        if (endContinues)
            writer.Recorder.WriteString("->");
        writer.Recorder.WriteLine("");
        writer.SpanMarkerContinues = endContinues;
    }

    // sourcemap_recorder.go:294.
    private void WriteSourceMapSourceText(SourceMapSpanWithDecodeErrors span, int index)
    {
        int sourcePosition = ScannerApi.ComputePositionOfLineAndUTF16Character(writer.TSLineMap.AsSpan(), checked((int)span.Span.SourceLine), checked((int)span.Span.SourceCharacter), writer.TSCodeView, true);
        string source = writer.PreviousWrittenSourcePosition < sourcePosition ? writer.TSCode[writer.PreviousWrittenSourcePosition..sourcePosition] : "";
        foreach (string error in span.DecodeErrors ?? [])
        {
            WriteSourceMapIndent(previousEmittedColumn, markerIds[index]);
            writer.Recorder.WriteLine(error);
        }

        var lineMap = Core.ComputeECMALineStarts(source);
        for (int i = 0; i < lineMap.Count; i++)
        {
            WriteSourceMapIndent(previousEmittedColumn, i == 0 ? markerIds[index] : "  >");
            writer.Recorder.WriteString(writer.GetTextOfLine(i, lineMap, source));
            if (i == lineMap.Count - 1)
                writer.Recorder.WriteLine("");
        }

        writer.PreviousWrittenSourcePosition = sourcePosition;
    }

    // sourcemap_recorder.go:331.
    private void WriteSpanDetails(SourceMapSpanWithDecodeErrors span, int index) => writer.Recorder.WriteLine(markerIds[index] + writer.GetSourceMapSpanString(span.Span, false));
    // sourcemap_recorder.go:335. Preserve pinned next-line lookup (documented Strada TODO).
    internal void WriteRecordedSpans()
    {
        if (writer.SpansOnSingleLine.Count == 0)
            return;
        int line = checked((int)writer.SpansOnSingleLine[0].Span.GeneratedLine);
        writer.WriteJSFileLines(line + 1);
        IterateSpans(writeMarker);
        string jsLine = writer.GetTextOfLine(line + 1, writer.JSLineMap, writer.JSFile.Content);
        if (previousEmittedColumn < jsLine.Length - 1)
            WriteSourceMapMarkerEx(writer.SpansOnSingleLine.Count, jsLine.Length - 1, true);
        IterateSpans(writeSourceText);
        IterateSpans(writeDetails);
        writer.Recorder.WriteLine("---");
    }
}
