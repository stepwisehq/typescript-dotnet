// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original tsbaseline/contentmapper_baseline.go public baseline observation.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TypeScript.Scanner;
using TypeScript.DiagnosticWriter;
using Writer = TypeScript.DiagnosticWriter.DiagnosticWriter;

namespace Program.Checks;
public static partial class CompilerEmitBaselines
{
    private static readonly Regex ContentMapperAnsiEscape = new("\u001b\\[[0-9;]*m", RegexOptions.CultureInvariant);
    public static string? ContentMapperBaseline(CompilerEmitCompilation result)
    {
        List<SourceFile> files = [];
        foreach (var file in result.Program.GetSourceFiles())
            if (result.Program.GetContentMapper(file)is not null)
                files.Add(file);
        if (files.Count == 0)
            return null;
        var names = files.Select(file => file.FileName()).ToHashSet(StringComparer.Ordinal);
        var output = new StringBuilder();
        static string NewLine(string text) => text.Length == 0 || text.EndsWith('\n') ? text : text + "\n";
        foreach (var file in files)
        {
            var mapper = result.Program.GetContentMapper(file)!;
            string scriptKind = Enum.IsDefined(file.ScriptKind) ? "ScriptKind" + file.ScriptKind : "ScriptKind(" + (int)file.ScriptKind + ")";
            output.Append("//// [").Append(RemoveTestPathPrefixes(file.FileName(), false)).Append("] (ScriptKind: ").Append(scriptKind).Append(", ContentMapper: [");
            output.AppendJoin(' ', mapper.Extensions ?? []).Append("])\n");
            output.Append("--- Original ---\n").Append(NewLine(file.OriginalText()));
            output.Append("--- Transformed ---\n").Append(NewLine(file.Text())).Append('\n');
        }

        Slice<Diagnostic> diagnostics = default;
        foreach (var diagnostic in result.Diagnostics)
            if (diagnostic.File()is { } file && names.Contains(file.FileName()))
                diagnostics = diagnostics.Append(diagnostic);
        output.Append("=== Diagnostics ===\n\n");
        if (diagnostics.Count == 0)
            return output.Append("<no content>\n").ToString();
        using var rendered = new StringWriter(CultureInfo.InvariantCulture);
        Writer.FormatDiagnosticsWithColorAndContext(rendered, Writer.FromASTDiagnostics(diagnostics), new FormattingOptions { NewLine = "\n" });
        return output.Append(RemoveTestPathPrefixes(ContentMapperAnsiEscape.Replace(rendered.ToString(), ""), false)).ToString();
    }
}
