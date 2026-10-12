// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Four original internal/ls/format_test.go functions, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Go T.Parallel scheduling hints are sequential; the original assertions are unchanged.
using TypeScript.Scanner;
using TypeScript.LanguageServices;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static class LanguageServiceFormattingChecks
{
    public static readonly string[] Names = ["TestNonOverlappingFormattingRanges", "TestGetFormattingEditsAfterKeystroke_EmptyFile", "TestGetFormattingEditsAfterKeystroke_SimpleStatement", "TestGetFormattingEditsForRange_FunctionBody"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        if (name == Names[0])
        {
            (TextRange[] Candidates, TextRange[] Expected)[] rows = [([new(10, 15), new(0, 5)], [new(0, 5), new(10, 15)]), ([new(0, 10), new(0, 20)], [new(0, 20)]), ([new(5, 15), new(0, 20)], [new(0, 20)]), ([new(5, 15), new(0, 10)], [new(0, 10), new(10, 15)])];
            foreach (var row in rows)
            {
                var candidates = Slice<MappedFormattingRange>.Make(row.Candidates.Length);
                for (int i = 0; i < candidates.Length; i++)
                    candidates.AsSpan()[i].originalRange = row.Candidates[i];
                var actual = LanguageService.nonOverlappingFormattingRanges(candidates);
                if (actual.Length != row.Expected.Length)
                    throw new InvalidOperationException("Original formatting range count differs");
                for (int i = 0; i < actual.Length; i++)
                    if (!actual[i].originalRange.Equals(row.Expected[i]))
                        throw new InvalidOperationException("Original formatting range differs");
            }

            return;
        }

        var service = new LanguageService(); // Original zero-value owner; these workers do not access Program.
        var context = new ProtocolRequestContext(CancellationToken.None, default, default);
        var options = LsUtil.GetDefaultFormatCodeSettings();
        if (name == Names[1] || name == Names[2])
        {
            var text = name == Names[1] ? "" : "const x = 1";
            var file = TypeScript.Scanner.Parser.ParseSourceFile(new SourceFileParseOptions("/index.ts", "/index.ts"), text, ScriptKind.TS);
            _ = service.getFormattingEditsAfterKeystroke(context, file, options, text.Length, ByteStringValue.FromString("\n"));
            return; // Original assertion is absence of panic, not a fabricated output expectation.
        }

        if (name == Names[3])
        {
            (string Text, int Start, int End)[] rows = [("function foo() {\n    return (1  + 2);\n}", 21, 38), ("function\nf() {\n}", 9, 13), ("function f() {\n  \n}", 15, 17), ("function f() {\n}", 15, 15)];
            foreach (var row in rows)
            {
                var file = TypeScript.Scanner.Parser.ParseSourceFile(new SourceFileParseOptions("/test.ts", "/test.ts"), row.Text, ScriptKind.TS);
                _ = service.getFormattingEditsForRange(context, file, options, new TextRange(row.Start, row.End));
            }

            return;
        }

        throw new ArgumentOutOfRangeException(nameof(name));
    }
}
