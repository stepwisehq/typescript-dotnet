// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/diagnosticwriter/diagnosticwriter.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Generic formatting uses original Go byte strings. Config AST filenames are
// explicitly raw; ordinary retained AST filenames use their scalar Unicode role.
// Content-mapped diagnostic locations follow the original source/virtual adapters.
using System.Globalization;
using System.Runtime.CompilerServices;
using TypeScript.Scanner;
using LocaleValue = TypeScript.Locale.Locale;
using AstDiagnostic = TypeScript.Scanner.Diagnostic;
using TypeScript.SpanMap;

namespace TypeScript.DiagnosticWriter;
public interface IFileLike : SourceFileLike
{
    RootedFilePath FileName();
}

public interface IDiagnostic
{
    IFileLike? File();
    int Pos();
    int End();
    int Len();
    int Code();
    DiagnosticCategory Category();
    string Source();
    string Localize(LocaleValue locale);
    IReadOnlyList<IDiagnostic>? MessageChain();
    IReadOnlyList<IDiagnostic>? RelatedInformation();
}

public sealed class ASTDiagnostic : IDiagnostic
{
    internal readonly AstDiagnostic diagnostic;
    private readonly bool configFileNames;
    private static readonly ConditionalWeakTable<SourceFile, AstFile> ordinaryFiles = new();
    private static readonly ConditionalWeakTable<SourceFile, AstFile> configFiles = new();
    private sealed class AstFile(SourceFile file, bool rawFileName) : IFileLike, ISourceTextFileLike
    {
        public RootedFilePath FileName() => rawFileName ? RootedFilePath.FromGoString(file.FileName()) : RootedFilePath.FromUnicode(file.FileName());
        public string Text() => file.Text();
        ByteStringView ISourceTextFileLike.TextView() => file.TextView();
        public Slice<int> ECMALineMap() => file.ECMALineMap();
    }

    // diagnosticwriter.go:130–146. Keep the original text owner and freshly
    // computed original-coordinate line map, rather than the virtual line map.
    private sealed class OriginalTextFile : IFileLike
    {
        private readonly RootedFilePath fileName;
        private readonly string text;
        private readonly Slice<int> lineMap;
        internal OriginalTextFile(SourceFile file, RootedFilePath fileName)
        {
            this.fileName = fileName;
            text = file.OriginalText();
            lineMap = Core.ComputeECMALineStarts(text);
        }

        public RootedFilePath FileName() => fileName;
        public string Text() => text;
        public Slice<int> ECMALineMap() => lineMap;
    }

    // diagnosticwriter.go:148–155. Supplemental virtual text uses the canonical
    // filename but retains its own source text/line-map owner.
    private sealed class RenamedFile(SourceFile file, RootedFilePath fileName) : IFileLike, ISourceTextFileLike
    {
        public RootedFilePath FileName() => fileName;
        public string Text() => file.Text();
        ByteStringView ISourceTextFileLike.TextView() => file.TextView();
        public Slice<int> ECMALineMap() => file.ECMALineMap();
    }

    private readonly record struct ResolvedLocation(TextRange Loc, bool UseOriginal = false, bool Synthesized = false);
    // diagnosticwriter.go:109–126. Mapper diagnostics already have original
    // ranges; compiler ranges map back unless wholly synthesized virtual code.
    private ResolvedLocation Resolve()
    {
        var loc = diagnostic.Loc();
        var file = diagnostic.File();
        if (file is null)
            return new(loc);
        if (diagnostic.Source() != "")
            return new(loc, UseOriginal: true);
        if (file.SpanMap()is { } spanMap)
        {
            var(mapped, fidelity) = spanMap.VirtualToOriginalSpan(loc);
            return fidelity == Fidelity.None ? new(loc, Synthesized: true) : new(mapped, UseOriginal: true);
        }

        return new(loc);
    }

    public ASTDiagnostic(AstDiagnostic diagnostic, bool configFileNames = false)
    {
        this.diagnostic = diagnostic;
        this.configFileNames = configFileNames;
    }

    public IFileLike? File()
    {
        SourceFile? file = diagnostic.File();
        if (file is null)
            return null;
        var canonical = file.CanonicalSourceFile();
        var fileName = RootedFilePath.FromValue((canonical ?? file).FileNameGoValue());
        if (Resolve().UseOriginal)
            return new OriginalTextFile(file, fileName);
        if (canonical is not null && !fileName.Equals(RootedFilePath.FromValue(file.FileNameGoValue())))
            return new RenamedFile(file, fileName);
        return configFileNames ? configFiles.GetValue(file, f => new AstFile(f, true)) : ordinaryFiles.GetValue(file, f => new AstFile(f, false));
    }

    public int Pos() => Resolve().Loc.Pos();
    public int End() => Resolve().Loc.End();
    public int Len() => Resolve().Loc.Len();
    public int Code() => diagnostic.Code();
    public DiagnosticCategory Category() => diagnostic.Category();
    public string Source() => diagnostic.Source();
    public string Localize(LocaleValue locale)
    {
        // Original prelocalized external text remains raw. Ordinary message args
        // still use diagnostics.Format's original ToValidUTF8 sanitization.
        if (diagnostic.message is null && diagnostic.MessageText() != "")
            return diagnostic.MessageText();
        return diagnostic.Localize(locale);
    }

    // diagnosticwriter.go:157–168. Preserve existing chain order and append the
    // actual synthesized-location note only when no original mapping exists.
    public IReadOnlyList<IDiagnostic> MessageChain()
    {
        var chain = diagnostic.MessageChain();
        var result = new List<IDiagnostic>(chain.Count + 1);
        foreach (var child in chain)
            result.Add(new ASTDiagnostic(child, configFileNames));
        if (Resolve().Synthesized)
            result.Add(DiagnosticWriter.WrapASTDiagnostic(Ast.NewCompilerDiagnostic(Diagnostics.This_location_is_in_virtual_code_produced_by_the_content_mapper_0_and_has_no_corresponding_location_in_the_original_file, diagnostic.File()!.ContentMapper()), configFileNames));
        return result;
    }

    public IReadOnlyList<IDiagnostic> RelatedInformation() => DiagnosticWriter.FromASTDiagnostics(diagnostic.RelatedInformation(), configFileNames);
}

public sealed class FormattingOptions
{
    public LocaleValue Locale;
    public CaseSensitivity CaseSensitivity;
    public RootedDirectoryPath CurrentDirectory;
    public string NewLine = "";
}

public sealed class ErrorSummary
{
    public int TotalErrorCount;
    public List<IDiagnostic>? GlobalErrors;
    public Dictionary<IFileLike, List<IDiagnostic>>? ErrorsByFile;
    public List<IFileLike> SortedFiles = [];
}

public static class DiagnosticWriter
{
    private const string foregroundColorEscapeGrey = "\u001b[90m";
    private const string foregroundColorEscapeRed = "\u001b[91m";
    private const string foregroundColorEscapeYellow = "\u001b[93m";
    private const string foregroundColorEscapeBlue = "\u001b[94m";
    private const string foregroundColorEscapeCyan = "\u001b[96m";
    private const string gutterStyleSequence = "\u001b[7m";
    private const string gutterSeparator = " ";
    private const string resetEscapeSequence = "\u001b[0m";
    private const string ellipsis = "...";

    public delegate void FormattedWriter(TextWriter output, string text, string formatStyle);

    // Actual locale-aware raw UTF8 summary boundary. Args already have Go's
    // string role, so neither decode them nor encode the result again.
    private static string localize(Message message, LocaleValue locale, params object[] args) =>
        message.Localize(locale, args);

    private static string formatFileNameRelativeTo(RootedFilePath fileName, RootedDirectoryPath directory, CaseSensitivity caseSensitivity)
    {
        var (relativePath, ok) = caseSensitivity.RelativePathFromDirectory(directory, fileName);
        return ok ? relativePath.AsString() : fileName.AsString();
    }

    public static ASTDiagnostic WrapASTDiagnostic(AstDiagnostic diagnostic, bool configFileNames = false) => new(diagnostic, configFileNames);
    public static List<ASTDiagnostic> WrapASTDiagnostics(IReadOnlyList<AstDiagnostic>? diagnostics, bool configFileNames = false) =>
        diagnostics?.Select(d => WrapASTDiagnostic(d, configFileNames)).ToList() ?? [];
    public static List<IDiagnostic> FromASTDiagnostics(IReadOnlyList<AstDiagnostic>? diagnostics, bool configFileNames = false) =>
        diagnostics?.Select(d => (IDiagnostic)WrapASTDiagnostic(d, configFileNames)).ToList() ?? [];
    public static ASTDiagnostic[] WrapASTDiagnostics(Slice<AstDiagnostic> diagnostics, bool configFileNames = false)
    {
        var result = new ASTDiagnostic[diagnostics.Length];
        for (int i = 0; i < result.Length; i++) result[i] = WrapASTDiagnostic(diagnostics[i], configFileNames);
        return result;
    }
    public static IDiagnostic[] FromASTDiagnostics(Slice<AstDiagnostic> diagnostics, bool configFileNames = false)
    {
        var result = new IDiagnostic[diagnostics.Length];
        for (int i = 0; i < result.Length; i++) result[i] = WrapASTDiagnostic(diagnostics[i], configFileNames);
        return result;
    }
    public static List<IDiagnostic> ToDiagnostics<T>(IReadOnlyList<T>? diagnostics) where T : IDiagnostic =>
        diagnostics?.Select(d => (IDiagnostic)d).ToList() ?? [];
    public static int CompareASTDiagnostics(ASTDiagnostic a, ASTDiagnostic b) => Ast.CompareDiagnostics(a.diagnostic, b.diagnostic);

    public static void FormatDiagnosticsWithColorAndContext(TextWriter output, IReadOnlyList<IDiagnostic>? diagnostics, FormattingOptions options)
    {
        if (diagnostics is null) return;
        for (int i = 0; i < diagnostics.Count; i++)
        {
            if (i > 0) output.Write(options.NewLine);
            FormatDiagnosticWithColorAndContext(output, diagnostics[i], options);
        }
    }

    public static void FormatDiagnosticWithColorAndContext(TextWriter output, IDiagnostic diagnostic, FormattingOptions options)
    {
        if (diagnostic.File() is { } file)
        {
            WriteLocation(output, file, diagnostic.Pos(), options, writeWithStyleAndReset);
            output.Write(" - ");
        }
        writeWithStyleAndReset(output, categoryName(diagnostic.Category()), getCategoryFormat(diagnostic.Category()));
        output.Write($"{foregroundColorEscapeGrey} {diagnosticPrefix(diagnostic)}{diagnostic.Code()}: {resetEscapeSequence}");
        WriteFlattenedDiagnosticMessage(output, diagnostic, options.NewLine, options.Locale);
        if (diagnostic.File() is { } snippetFile && diagnostic.Code() != Diagnostics.File_appears_to_be_binary.Code())
        {
            output.Write(options.NewLine);
            writeCodeSnippet(output, snippetFile, diagnostic.Pos(), diagnostic.Len(), getCategoryFormat(diagnostic.Category()), "", options);
            output.Write(options.NewLine);
        }
        if (diagnostic.RelatedInformation() is { Count: > 0 } related)
            foreach (IDiagnostic information in related)
            {
                if (information.File() is { } relatedFile)
                {
                    output.Write(options.NewLine);
                    output.Write("  ");
                    WriteLocation(output, relatedFile, information.Pos(), options, writeWithStyleAndReset);
                    output.Write(" - ");
                    WriteFlattenedDiagnosticMessage(output, information, options.NewLine, options.Locale);
                    writeCodeSnippet(output, relatedFile, information.Pos(), information.Len(), foregroundColorEscapeCyan, "    ", options);
                }
                output.Write(options.NewLine);
            }
    }

    private static void writeCodeSnippet(TextWriter output, IFileLike file, int start, int length, string squiggleColor, string indent, FormattingOptions options)
    {
        var (firstLine, firstCharacter) = TypeScript.Scanner.Scanner.GetECMALineAndUTF16CharacterOfPosition(file, start);
        var (lastLine, lastCharacter) = TypeScript.Scanner.Scanner.GetECMALineAndUTF16CharacterOfPosition(file, start + length);
        if (length == 0) lastCharacter++;
        int lastFileLine = TypeScript.Scanner.Scanner.GetECMALineOfPosition(file, file.TextView().Length);
        bool hasMoreThanFiveLines = lastLine - firstLine >= 4;
        int gutterWidth = Strconv.Itoa(lastLine + 1).Length;
        if (hasMoreThanFiveLines) gutterWidth = Math.Max(ellipsis.Length, gutterWidth);
        for (int i = firstLine; i <= lastLine; i++)
        {
            output.Write(options.NewLine);
            if (hasMoreThanFiveLines && firstLine + 1 < i && i < lastLine - 1)
            {
                output.Write(indent + gutterStyleSequence + ellipsis.PadLeft(gutterWidth) + resetEscapeSequence + gutterSeparator + options.NewLine);
                i = lastLine - 1;
            }
            int lineStart = TypeScript.Scanner.Scanner.GetECMAPositionOfLineAndByteOffset(file, i, 0);
            int lineEnd = i < lastFileLine ? TypeScript.Scanner.Scanner.GetECMAPositionOfLineAndByteOffset(file, i + 1, 0) : file.TextView().Length;
            string lineContent = GoStrings.TrimRightFunc(file.TextView().Slice(lineStart, lineEnd), GoUnicode.IsSpace).Materialize().Replace('\t', ' ');
            output.Write(indent + gutterStyleSequence + Strconv.Itoa(i + 1).PadLeft(gutterWidth) + resetEscapeSequence + gutterSeparator + lineContent + options.NewLine);
            output.Write(indent + gutterStyleSequence + "".PadLeft(gutterWidth) + resetEscapeSequence + gutterSeparator + squiggleColor);
            if (i == firstLine)
            {
                int lastForLine = i == lastLine ? lastCharacter : Core.UTF16Len(lineContent);
                output.Write(new string(' ', firstCharacter));
                output.Write(new string('~', lastForLine - firstCharacter));
            }
            else if (i == lastLine) output.Write(new string('~', lastCharacter));
            else output.Write(new string('~', Core.UTF16Len(lineContent)));
            output.Write(resetEscapeSequence);
        }
    }

    public static string FlattenDiagnosticMessage(IDiagnostic diagnostic, string newLine, LocaleValue locale)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        WriteFlattenedDiagnosticMessage(output, diagnostic, newLine, locale);
        return output.ToString();
    }

    public static void WriteFlattenedASTDiagnosticMessage(TextWriter output, AstDiagnostic diagnostic, string newLine, LocaleValue locale) =>
        WriteFlattenedDiagnosticMessage(output, WrapASTDiagnostic(diagnostic), newLine, locale);

    public static void WriteFlattenedDiagnosticMessage(TextWriter output, IDiagnostic diagnostic, string newLine, LocaleValue locale)
    {
        output.Write(diagnostic.Localize(locale));
        if (diagnostic.MessageChain() is { } children)
            foreach (IDiagnostic child in children) flattenDiagnosticMessageChain(output, child, newLine, locale, 1);
    }

    private static void flattenDiagnosticMessageChain(TextWriter output, IDiagnostic chain, string newLine, LocaleValue locale, int level)
    {
        output.Write(newLine + new string(' ', level * 2) + chain.Localize(locale));
        if (chain.MessageChain() is { } children)
            foreach (IDiagnostic child in children) flattenDiagnosticMessageChain(output, child, newLine, locale, level + 1);
    }

    private static string diagnosticPrefix(IDiagnostic diagnostic) => diagnostic.Source() is { Length: > 0 } source ? source : "TS";
    private static string categoryName(DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.Error => "error", DiagnosticCategory.Warning => "warning",
        DiagnosticCategory.Suggestion => "suggestion", DiagnosticCategory.Message => "message",
        _ => throw new InvalidOperationException("Unhandled diagnostic category")
    };
    private static string getCategoryFormat(DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.Error => foregroundColorEscapeRed, DiagnosticCategory.Warning => foregroundColorEscapeYellow,
        DiagnosticCategory.Suggestion => foregroundColorEscapeGrey, DiagnosticCategory.Message => foregroundColorEscapeBlue,
        _ => throw new InvalidOperationException("Unhandled diagnostic category")
    };

    private static void writeWithStyleAndReset(TextWriter output, string text, string style) => output.Write(style + text + resetEscapeSequence);

    public static void WriteLocation(TextWriter output, IFileLike file, int pos, FormattingOptions? options, FormattedWriter write)
    {
        var (line, character) = TypeScript.Scanner.Scanner.GetECMALineAndUTF16CharacterOfPosition(file, pos);
        string name = options is null ? file.FileName().AsString() : formatFileNameRelativeTo(file.FileName(), options.CurrentDirectory, options.CaseSensitivity);
        write(output, name, foregroundColorEscapeCyan);
        output.Write(':');
        write(output, Strconv.Itoa(line + 1), foregroundColorEscapeYellow);
        output.Write(':');
        write(output, Strconv.Itoa(character + 1), foregroundColorEscapeYellow);
    }

    public static void WriteErrorSummaryText(TextWriter output, IReadOnlyList<IDiagnostic>? diagnostics, FormattingOptions options)
    {
        ErrorSummary summary = getErrorSummary(diagnostics);
        int count = summary.TotalErrorCount;
        if (count == 0) return;
        IFileLike? first = summary.SortedFiles.FirstOrDefault();
        string firstName = prettyPathForFileError(first, first is null ? null : summary.ErrorsByFile![first], options);
        int files = summary.ErrorsByFile?.Count ?? 0;
        string message = count == 1
            ? summary.GlobalErrors is { Count: > 0 } || firstName == ""
                ? localize(Diagnostics.Found_1_error, options.Locale)
                : localize(Diagnostics.Found_1_error_in_0, options.Locale, firstName)
            : files == 0
                ? localize(Diagnostics.Found_0_errors, options.Locale, count)
                : files == 1
                    ? localize(Diagnostics.Found_0_errors_in_the_same_file_starting_at_Colon_1, options.Locale, count, firstName)
                    : localize(Diagnostics.Found_0_errors_in_1_files, options.Locale, count, files);
        output.Write(options.NewLine + message + options.NewLine + options.NewLine);
        if (files > 1)
        {
            writeTabularErrorsDisplay(output, summary, options);
            output.Write(options.NewLine);
        }
    }

    private static ErrorSummary getErrorSummary(IReadOnlyList<IDiagnostic>? diagnostics)
    {
        var result = new ErrorSummary();
        if (diagnostics is not null)
            foreach (IDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Category() != DiagnosticCategory.Error) continue;
                result.TotalErrorCount++;
                if (diagnostic.File() is not { } file) (result.GlobalErrors ??= []).Add(diagnostic);
                else
                {
                    result.ErrorsByFile ??= new(ReferenceEqualityComparer.Instance);
                    if (!result.ErrorsByFile.TryGetValue(file, out var errors)) result.ErrorsByFile.Add(file, errors = []);
                    errors.Add(diagnostic);
                }
            }
        if (result.ErrorsByFile is not null)
            result.SortedFiles = result.ErrorsByFile.Keys.OrderBy(file => file.FileName().AsString(), StringComparer.Ordinal).ToList();
        return result;
    }

    private static void writeTabularErrorsDisplay(TextWriter output, ErrorSummary summary, FormattingOptions options)
    {
        int maxErrors = summary.ErrorsByFile!.Values.Max(errors => errors.Count);
        string header = localize(Diagnostics.Errors_Files, options.Locale);
        int headingLength = header.Split(' ')[0].Length;
        int countLength = Strconv.Itoa(maxErrors).Length;
        int paddingGoal = Math.Max(headingLength, countLength);
        output.Write(new string(' ', Math.Max(countLength - headingLength, 0)) + header + options.NewLine);
        foreach (IFileLike file in summary.SortedFiles)
        {
            List<IDiagnostic> errors = summary.ErrorsByFile[file];
            output.Write(Strconv.Itoa(errors.Count).PadLeft(paddingGoal) + "  " + prettyPathForFileError(file, errors, options) + options.NewLine);
        }
    }

    private static string prettyPathForFileError(IFileLike? file, IReadOnlyList<IDiagnostic>? errors, FormattingOptions options)
    {
        if (file is null || errors is null || errors.Count == 0) return "";
        int line = TypeScript.Scanner.Scanner.GetECMALineOfPosition(file, errors[0].Pos());
        return formatFileNameRelativeTo(file.FileName(), options.CurrentDirectory, options.CaseSensitivity) + foregroundColorEscapeGrey + ":" + Strconv.Itoa(line + 1) + resetEscapeSequence;
    }

    public static void WriteFormatDiagnostics(TextWriter output, IReadOnlyList<IDiagnostic>? diagnostics, FormattingOptions options)
    {
        if (diagnostics is not null)
            foreach (IDiagnostic diagnostic in diagnostics) WriteFormatDiagnostic(output, diagnostic, options);
    }

    public static void WriteFormatDiagnostic(TextWriter output, IDiagnostic diagnostic, FormattingOptions options)
    {
        if (diagnostic.File() is { } file)
        {
            var (line, character) = TypeScript.Scanner.Scanner.GetECMALineAndUTF16CharacterOfPosition(file, diagnostic.Pos());
            output.Write(formatFileNameRelativeTo(file.FileName(), options.CurrentDirectory, options.CaseSensitivity) + $"({line + 1},{character + 1}): ");
        }
        output.Write(categoryName(diagnostic.Category()) + $" {diagnosticPrefix(diagnostic)}{diagnostic.Code()}: ");
        WriteFlattenedDiagnosticMessage(output, diagnostic, options.NewLine, options.Locale);
        output.Write(options.NewLine);
    }

    public static void FormatDiagnosticsStatusWithColorAndTime(TextWriter output, string time, IDiagnostic diagnostic, FormattingOptions options)
    {
        output.Write('[');
        writeWithStyleAndReset(output, time, foregroundColorEscapeGrey);
        output.Write("] ");
        WriteFlattenedDiagnosticMessage(output, diagnostic, options.NewLine, options.Locale);
    }

    public static void FormatDiagnosticsStatusAndTime(TextWriter output, string time, IDiagnostic diagnostic, FormattingOptions options)
    {
        output.Write(time + " - ");
        WriteFlattenedDiagnosticMessage(output, diagnostic, options.NewLine, options.Locale);
    }

    public static readonly IReadOnlyList<int> ScreenStartingCodes =
    [Diagnostics.Starting_compilation_in_watch_mode.Code(), Diagnostics.File_change_detected_Starting_incremental_compilation.Code()];

    public static bool TryClearScreen(TextWriter output, IDiagnostic diagnostic, CompilerOptions options)
    {
        if (options.PreserveWatchOutput != Tristate.True && options.ExtendedDiagnostics != Tristate.True && options.Diagnostics != Tristate.True && ScreenStartingCodes.Contains(diagnostic.Code()))
        {
            output.Write("\u001b[2J\u001b[3J\u001b[H");
            return true;
        }
        return false;
    }
}
