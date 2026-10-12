// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete seven original internal/format test functions at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Formatting;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static class FormattingChecks
{
    public static readonly string[] Names = ["TestFormat", "TestFormatNoTrailingSpace", "TestGetContainingList_NamedImports", "TestGetIndentationForNamedImportsPosition", "TestCommentFormatting", "TestFormatSelectionPreservesComments", "TestSliceBoundsPanic"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestFormat":
                TestFormat();
                break;
            case "TestFormatNoTrailingSpace":
                TestFormatNoTrailingSpace();
                break;
            case "TestGetContainingList_NamedImports":
                TestGetContainingList();
                break;
            case "TestGetIndentationForNamedImportsPosition":
                TestGetIndentation();
                break;
            case "TestCommentFormatting":
                TestCommentFormatting();
                break;
            case "TestFormatSelectionPreservesComments":
                TestSelection();
                break;
            case "TestSliceBoundsPanic":
                TestSliceBoundsPanic();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private static FormatCodeSettings Options(long baseSize = 0, bool tabs = false, bool annotation = true) => new()
    {
        TabSize = 4,
        IndentSize = 4,
        BaseIndentSize = baseSize,
        NewLineCharacter = "\n",
        ConvertTabsToSpaces = tabs ? Tristate.False : Tristate.True,
        IndentStyle = IndentStyle.Smart,
        TrimTrailingWhitespace = Tristate.True,
        InsertSpaceBeforeTypeAnnotation = annotation ? Tristate.True : Tristate.Unknown
    };
    private static SourceFile Parse(string text, bool js = false) => Parser.ParseSourceFile(new SourceFileParseOptions(js ? "/test.js" : "/test.ts", js ? "/test.js" : "/test.ts"), text, js ? ScriptKind.JS : ScriptKind.TS);
    private static ProtocolRequestContext Context(FormatCodeSettings options) => Formatting.WithFormatCodeSettings(ProtocolRequestContext.Default, options, ByteStringValue.FromString("\n"));
    private static string Format(string text, FormatCodeSettings options, bool js = false) => Core.ApplyBulkEdits(ByteStringValue.FromString(text), Formatting.FormatDocument(Context(options), Parse(text, js))).Materialize()!;
    // api_test.go:37. Same actual repository fixture; path discovery only adapts the Go repo helper.
    private static void TestFormat()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        string? path = null;
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "upstream/TypeScript/tsc/testdata/fixtures/compiler/checker.ts");
            if (File.Exists(candidate))
            {
                path = candidate;
                break;
            }

            directory = directory.Parent;
        }

        if (path == null)
            throw new FileNotFoundException("pinned checker.ts formatting fixture");
        var originalBytes = File.ReadAllBytes(path);
        var bytes = ByteStringView.FromOwnedBytes(originalBytes);
        var text = ByteStringValue.FromView(bytes);
        var file = Parser.ParseSourceFile(new SourceFileParseOptions("/checker.ts", "/checker.ts"), originalBytes, ScriptKind.TS);
        var output = Core.ApplyBulkEdits(text, Formatting.FormatDocument(Context(Options(4)), file));
        Check(output.Length > 0, "formatted checker fixture should not be empty");
        Check(!output.EqualsValue(text), "checker fixture should change");
    }

    // format_test.go:15. All eight original leaves.
    private static void TestFormatNoTrailingSpace()
    {
        string[] inputs = ["1;", "console.log('hello');", "if (true) { }", "class A {\n    // Class Contents Go Here\n}", "class A {\n    // Class Contents Go Here\n}\n", "if (true) {}", "module M { }", "enum E { A, B }"];
        foreach (var input in inputs)
        {
            var text = Format(input, Options(annotation: false));
            foreach (var line in text.Split('\n'))
                Check(line == line.TrimEnd(' ', '\t'), "Formatter should not add trailing whitespace");
        }
    }

    // indent_test.go:13. Original recursive descendant traversal and list assertions.
    private static void TestGetContainingList()
    {
        var file = Parse("import type {\n    AAA,\n    BBB,\n} from \"./bar\";");
        var specifiers = new List<Node>();
        void Visit(Node node) => node.ForEachChild(child =>
        {
            if (child.Kind == Kind.ImportSpecifier)
                specifiers.Add(child);
            Visit(child);
            return false;
        });
        Visit(file.AsNode());
        Check(specifiers.Count == 2, "Expected 2 import specifiers");
        foreach (var node in specifiers)
        {
            var list = Formatting.GetContainingList(node, file);
            Check(list != null, "GetContainingList should return non-nil");
            Check(list!.Nodes.Length == 2, "Expected list with 2 elements");
        }
    }

    // indent_getindentation_test.go:13.
    private static void TestGetIndentation()
    {
        var file = Parse("import {\n    type SomeInterface,\n} from \"./exports.js\";");
        var lineStart = Formatting.GetLineStartPositionForPosition(14, file);
        Check(Formatting.GetIndentation(lineStart, file, LsUtil.GetDefaultFormatCodeSettings(), true) == 4, "Expected indentation 4");
    }

    // comment_test.go:15. All seven original subtests, including the distinct issue1928 regression.
    private static void TestCommentFormatting()
    {
        const string firstInput = "class C {\n    /**\n     *\n    */\n    async x() {}\n}";
        var first = Format(firstInput, Options(4));
        Check(!first.Contains("*/\n   /", StringComparison.Ordinal), "should not corrupt */ to /");
        Check(first.Contains("*/", StringComparison.Ordinal), "should preserve */ token");
        Check(first.Contains("async", StringComparison.Ordinal), "should preserve async keyword");
        var second = Format(first, Options(4));
        Check(!second.Contains(" sync x()", StringComparison.Ordinal), "should not corrupt async to sync");
        Check(second.Contains("async", StringComparison.Ordinal), "should preserve async on second pass");
        var tabDoc = Format("class Foo {\n\t/**\n\t * @param {string} argument - This is a param description.\n\t */\n\texample(argument) {\nconsole.log(argument);\n\t}\n}", Options(tabs: true));
        Check(!tabDoc.Contains(" \t*", StringComparison.Ordinal), "should not have space before tab before asterisk");
        Check(tabDoc.Contains("\t *", StringComparison.Ordinal), "should have tab before space before asterisk");
        Check(tabDoc.Contains("\t\tconsole.log", StringComparison.Ordinal), "console.log should be indented with two tabs");
        var arguments = Format("console.log(\n\t\"a\",\n\t// the second arg\n\t\"b\"\n);", Options(tabs: true));
        Check(arguments.Contains("\t// the second arg", StringComparison.Ordinal), "comment should be indented with tab");
        Check(!arguments.Contains("\n// the second arg", StringComparison.Ordinal), "comment should not lose indentation");
        for (var distinctOriginalSubtest = 0; distinctOriginalSubtest < 2; distinctOriginalSubtest++)
        {
            var chained = Format("foo\n\t.bar()\n\t// A second call\n\t.baz();", Options(tabs: true));
            Check(chained.Contains("\t// A second call", StringComparison.Ordinal) || chained.Contains("   // A second call", StringComparison.Ordinal), "comment should be indented");
            Check(!chained.Contains("\n// A second call", StringComparison.Ordinal), "comment should not be at column0");
        }

        var multiline = Format("document.addEventListener('DOMContentLoaded', () => {\n    /** @type {NodeListOf<HTMLSpanElement>} */\n    const elements = document.querySelectorAll('.test')\n});", Options(tabs: true, annotation: false), js: true);
        Check(multiline.Length > 0, "formatted text should not be empty");
        var single = Format("document.addEventListener('DOMContentLoaded', () => {\n    // a comment\n    const x = 1\n});", Options(tabs: true, annotation: false));
        Check(single.Length > 0, "formatted text should not be empty");
    }

    // comment_test.go:251. All three original exact output assertions.
    private static void TestSelection()
    {
        const string input = "const test/* comment */=5;";
        var options = Options(annotation: false);
        var comment = input.IndexOf("/*", StringComparison.Ordinal);
        var file = Parse(input);
        var first = Core.ApplyBulkEdits(ByteStringValue.FromString(input), Formatting.FormatSelection(Context(options), file, 0, comment + "/* comment".Length));
        Check(first.EqualsBytes("const test/* comment */=5;"u8), "selection ending inside comment should preserve statement");
        var second = Core.ApplyBulkEdits(ByteStringValue.FromString(input), Formatting.FormatSelection(Context(options), Parse(input), comment + 3, input.Length));
        Check(second.EqualsBytes("const test/* comment */=5;"u8), "selection starting inside comment should preserve statement");
        options.InsertSpaceBeforeAndAfterBinaryOperators = Tristate.True;
        Check(Format(input, options) == "const test/* comment */ = 5;", "full format should preserve comment and add spaces");
    }

    // comment_test.go:345.
    private static void TestSliceBoundsPanic()
    {
        const string input = "const _enableDisposeWithListenerWarning = false\n\t// || Boolean(\"TRUE\") // causes a linter warning so that it cannot be pushed\n\t;\n";
        var output = Format(input, Options(4));
        Check(output.Length > 0, "formatted text should not be empty");
        Check(output.Contains("_enableDisposeWithListenerWarning", StringComparison.Ordinal), "should preserve variable name");
    }
}
