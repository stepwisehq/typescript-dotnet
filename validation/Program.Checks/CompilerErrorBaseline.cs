// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete AST-diagnostic specialization of tsbaseline/error_baseline.go.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TypeScript.Scanner;
using TypeScript.DiagnosticWriter;
using Writer = TypeScript.DiagnosticWriter.DiagnosticWriter;

namespace Program.Checks;
public static partial class CompilerEmitBaselines
{
    private const string ErrorHarnessNewLine = "\r\n";
    private static readonly FormattingOptions ErrorFormattingOptions = new()
    {
        NewLine = ErrorHarnessNewLine
    };
    private static readonly Regex DiagnosticsLocationPrefix = new(@"^(lib.*\.d\.ts)\(\d+,\d+\)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex DiagnosticsLocationPattern = new(@"(lib.*\.d\.ts):\d+:\d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ErrorLineDelimiter = new("\r?\n", RegexOptions.CultureInvariant);
    // error_baseline.go:34 DoErrorBaseline, baseline filename/channel supplied by runner.
    public static string ObserveErrorBaseline(IReadOnlyList<CompilerEmitTestFile> inputFiles, Slice<Diagnostic> errors, bool pretty)
    {
        string output = errors.Count == 0 ? CompilerSourceMapBaselines.NoContent : GetErrorBaseline(inputFiles, errors, pretty);
        foreach (var error in errors)
            if (error.Code() == -1)
                throw new InvalidOperationException("Found diagnostic with code -1, which is used to log critical assertion violations in the baseline. Inspect and fix those failures.");
        return output;
    }

    // error_baseline.go:52 minimalDiagnosticsToString.
    private static string MinimalDiagnosticsToString(IReadOnlyList<IDiagnostic> diagnostics, bool pretty)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        if (pretty)
            Writer.FormatDiagnosticsWithColorAndContext(output, diagnostics, ErrorFormattingOptions);
        else
            Writer.WriteFormatDiagnostics(output, diagnostics, ErrorFormattingOptions);
        return output.ToString();
    }

    // error_baseline.go:61 GetErrorBaseline. Raw Go-byte string result.
    public static string GetErrorBaseline(IReadOnlyList<CompilerEmitTestFile> inputFiles, Slice<Diagnostic> diagnostics, bool pretty)
    {
        var output = IterateErrorBaseline(inputFiles, diagnostics, pretty);
        if (pretty)
        {
            using var summary = new StringWriter(CultureInfo.InvariantCulture);
            Writer.WriteErrorSummaryText(summary, Writer.FromASTDiagnostics(diagnostics, configFileNames: true), ErrorFormattingOptions);
            output.Add(RemoveTestPathPrefixes(summary.ToString(), false));
        }

        return string.Concat(output);
    }

    // error_baseline.go:78 iterateErrorBaseline. Clone/sort, every original count assertion.
    private static List<string> IterateErrorBaseline(IReadOnlyList<CompilerEmitTestFile> inputFiles, Slice<Diagnostic> inputDiagnostics, bool pretty)
    {
        var diagnostics = inputDiagnostics.AsSpan().ToArray();
        Array.Sort(diagnostics, Ast.CompareDiagnostics);
        var wrapped = Writer.WrapASTDiagnostics(Slice<Diagnostic>.FromArray(diagnostics), configFileNames: true);
        var outputLines = new StringBuilder();
        int totalNonLibraryNonConfig = 0, errorsReported = 0;
        bool firstLine = true;
        string NewLine()
        {
            if (firstLine)
            {
                firstLine = false;
                return "";
            }

            return ErrorHarnessNewLine;
        }

        var result = new List<string>();
        void OutputErrorText(ASTDiagnostic diagnostic)
        {
            string message = Writer.FlattenDiagnosticMessage(diagnostic, ErrorHarnessNewLine, TypeScript.Locale.Locales.Default);
            var lines = new List<string>();
            foreach (string entry in RemoveTestPathPrefixes(message, false).Split('\n'))
            {
                string line = entry.EndsWith('\r') ? entry[..^1] : entry;
                if (line.Length != 0)
                    lines.Add("!!! " + diagnostic.Category().Name() + " TS" + diagnostic.Code().ToString(CultureInfo.InvariantCulture) + ": " + line);
            }

            foreach (var info in diagnostic.RelatedInformation())
            {
                string location = "";
                if (info.File()is { } file)
                {
                    using var output = new StringWriter(CultureInfo.InvariantCulture);
                    Writer.WriteLocation(output, file, info.Pos(), ErrorFormattingOptions, static (writer, text, style) => writer.Write(text));
                    location = " " + output;
                }

                location = RemoveTestPathPrefixes(location, false);
                if (location.Length > 0 && IsErrorDefaultLibraryFile(info.File()!.FileName().AsString()))
                    location = DiagnosticsLocationPattern.Replace(location, "$1:--:--");
                lines.Add("!!! related TS" + info.Code().ToString(CultureInfo.InvariantCulture) + location + ": " + Writer.FlattenDiagnosticMessage(info, ErrorHarnessNewLine, TypeScript.Locale.Locales.Default));
            }

            foreach (string line in lines)
                outputLines.Append(NewLine()).Append(line);
            errorsReported++;
            if (diagnostic.File()is not { } source || !IsErrorDefaultLibraryFile(source.FileName().AsString()) && !IsErrorTsConfigFile(source.FileName().AsString()))
                totalNonLibraryNonConfig++;
        }

        string top = MinimalDiagnosticsToString(wrapped, pretty);
        top = DiagnosticsLocationPrefix.Replace(RemoveTestPathPrefixes(top, false), "$1(--,--)");
        result.Add(top + ErrorHarnessNewLine + ErrorHarnessNewLine);
        foreach (var diagnostic in wrapped)
            if (diagnostic.File()is null)
                OutputErrorText(diagnostic);
        result.Add(outputLines.ToString());
        outputLines.Clear();
        errorsReported = 0;
        // Preserve pinned map behavior: upstream checks dupeCase but never inserts into it.
        var dupeCase = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var input in inputFiles)
        {
            var fileErrors = wrapped.Where(d => d.File()is { } file && CompareErrorPaths(RemoveTestPathPrefixes(file.FileName().AsString(), false), RemoveTestPathPrefixes(input.UnitName, false)) == 0).ToArray();
            outputLines.Append(NewLine()).Append("==== ").Append(RemoveTestPathPrefixes(input.UnitName, false)).Append(" (").Append(fileErrors.Length).Append(" errors) ====");
            int markedErrorCount = 0;
            var lineStarts = Core.ComputeECMALineStarts(input.Content);
            var lines = ErrorLineDelimiter.Split(input.Content);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                if (line.EndsWith('\r'))
                    line = line[..^1];
                int start = lineStarts[index];
                int next = index == lines.Length - 1 ? input.Content.Length : lineStarts[index + 1];
                outputLines.Append(NewLine()).Append("    ").Append(line);
                foreach (var diagnostic in fileErrors)
                {
                    int errorStart = diagnostic.Pos(), end = errorStart + diagnostic.Len();
                    if (end >= start && (errorStart < next || index == lines.Length - 1))
                    {
                        int relative = errorStart - start;
                        int length = end - errorStart - Math.Max(0, start - errorStart);
                        int squiggleStart = Math.Max(0, relative);
                        outputLines.Append(NewLine()).Append("    ").Append(ErrorReplaceNonWhitespace(line.AsSpan(0, squiggleStart)));
                        int squiggleEnd = Math.Max(squiggleStart, Math.Min(squiggleStart + length, line.Length));
                        outputLines.Append('~', ErrorRuneCount(line.AsSpan(squiggleStart, squiggleEnd - squiggleStart)));
                        if (index == lines.Length - 1 || next > end)
                        {
                            OutputErrorText(diagnostic);
                            markedErrorCount++;
                        }
                    }
                }
            }

            if (markedErrorCount != fileErrors.Length)
                throw new InvalidOperationException("count of errors in " + input.UnitName + ": " + markedErrorCount + " != " + fileErrors.Length);
            bool duplicate = dupeCase.ContainsKey(SanitizeErrorTestPath(input.UnitName));
            result.Add(outputLines.ToString());
            if (duplicate)
                totalNonLibraryNonConfig -= errorsReported;
            outputLines.Clear();
            errorsReported = 0;
        }

        int library = diagnostics.Count(d => d.File()is { } f && (IsErrorDefaultLibraryFile(f.FileNameGoBytes()) || IsErrorBuiltFile(f.FileNameGoBytes())));
        int config = diagnostics.Count(d => d.File()is { } f && IsErrorTsConfigFile(f.FileNameGoBytes()));
        int supplemental = diagnostics.Count(d => d.File()is { } f && f.IsContentMapperSupplemental());
        if (totalNonLibraryNonConfig + library + config + supplemental != diagnostics.Length)
            throw new InvalidOperationException("total number of errors: " + (totalNonLibraryNonConfig + library + config + supplemental) + " != " + diagnostics.Length);
        return result;
    }

    // error_baseline.go:279 formatLocation is folded into its sole synchronous callback above.
    // util.go:51–70 classification/sanitization, raw byte paths.
    private static bool IsErrorDefaultLibraryFile(string path)
    {
        string name = TsPath.GetBaseFileName(path);
        return name.StartsWith("lib.", StringComparison.Ordinal) && name.EndsWith(".d.ts", StringComparison.Ordinal);
    }

    private static bool IsErrorBuiltFile(string path) => path.StartsWith("built/local/", StringComparison.Ordinal) || path.StartsWith("/.ts/", StringComparison.Ordinal);
    private static bool IsErrorTsConfigFile(string path) => path.Contains("tsconfig", StringComparison.Ordinal) && path.Contains("json", StringComparison.Ordinal);
    private static string SanitizeErrorTestPath(string name)
    {
        string path = Regex.Replace(name, "[\\^<>:\"|?*%]", "_");
        path = TsPath.NormalizeSlashes(path).Replace("../", "__dotdot/", StringComparison.Ordinal);
        path = CaseSensitivity.CaseInsensitive.Canonicalize(TsPath.NormalizePath(path));
        return path.StartsWith('/') ? path[1..] : path;
    }

    // Go regexp \S matches one decoded UTF8 rune; invalid bytes consume one each.
    private static string ErrorReplaceNonWhitespace(ReadOnlySpan<char> bytes)
    {
        var result = new StringBuilder();
        for (int offset = 0; offset < bytes.Length;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(bytes[offset..]);
            if (rune is '\t' or '\n' or '\f' or '\r' or ' ')
                result.Append((char)rune);
            else
                result.Append(' ');
            offset += size;
        }

        return result.ToString();
    }

    private static int ErrorRuneCount(ReadOnlySpan<char> bytes)
    {
        int count = 0;
        for (int offset = 0; offset < bytes.Length; count++)
            offset += Utf8.DecodeRuneInString(bytes[offset..]).Size;
        return count;
    }

    // tspath/path.go:1067 ComparePaths, required normalized relative-component branch.
    private static int CompareErrorPaths(string first, string second)
    {
        first = TsPath.CombinePaths("", first);
        second = TsPath.CombinePaths("", second);
        if (first == second)
            return 0;
        if (first.Length == 0)
            return -1;
        if (second.Length == 0)
            return 1;
        int aRoot = TsPath.GetRootLength(first), bRoot = TsPath.GetRootLength(second);
        int result = CaseSensitivity.CaseInsensitive.CompareValues(ByteStringValue.FromString(first[..aRoot]), ByteStringValue.FromString(second[..bRoot]));
        if (result != 0)
            return result;
        string aRest = first[aRoot..], bRest = second[bRoot..];
        bool Relative(string path) => path.Split('/').Any(part => part is "." or "..");
        if (!Relative(aRest) && !Relative(bRest))
            return CaseSensitivity.CaseInsensitive.CompareValues(ByteStringValue.FromString(aRest), ByteStringValue.FromString(bRest));
        var a = TsPath.ReducePathComponents(TsPath.GetPathComponents(first));
        var b = TsPath.ReducePathComponents(TsPath.GetPathComponents(second));
        for (int i = 1; i < Math.Min(a.Count, b.Count); i++)
        {
            result = CaseSensitivity.CaseInsensitive.CompareValues(ByteStringValue.FromString(a[i]), ByteStringValue.FromString(b[i]));
            if (result != 0)
                return result;
        }

        return a.Count.CompareTo(b.Count);
    }
}
