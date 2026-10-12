// Authored real-output witness: Go unicode.IsSpace differs from JS whitespace.
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace Emit.Checks;
public static class PrinterCommentControls
{
    public static int RunAll()
    {
        // U+200B and U+FEFF lie at the edges of the second comment line. Go
        // strings.TrimSpace must preserve both; JavaScript whitespace trims them.
        // Go calculateIndent counts the leading U+200B, so the printer emits
        // one indentation space before preserving its original bytes.
        const string source = "/*\n\u200bretain\ufeff\n*/\nlet a = 1;";
        var file = Parser.ParseSourceFile(new SourceFileParseOptions("/main.ts", "/main.ts"), source, ScriptKind.TS);
        var printer = Printer.NewPrinter(new PrinterOptions { NewLine = NewLineKind.LF }, new PrintHandlers());
        string expected = GoString.ToUtf8("/*\n \u200bretain\ufeff\n*/\nlet a = 1;\n");
        string actual = printer.EmitSourceFile(file);
        if (actual != expected)
            throw new InvalidOperationException("Multiline comment bytes differ from pinned Go output");
        return 1;
    }
}
