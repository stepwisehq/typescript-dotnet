using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class CodeActionKindChecks
{
    // Authored admission of the four exact Go constants from lsproto/lsp.go:404–407.
    // Request kinds must contain their own TS children and retain full wire names.
    public static int RunAll()
    {
        Check(CodeActionKind.SourceFixAll, CodeActionKind.SourceFixAllTs, "source.fixAll.ts");
        Check(CodeActionKind.SourceOrganizeImports, CodeActionKind.SourceOrganizeImportsTs, "source.organizeImports.ts");
        Check(CodeActionKind.Source, CodeActionKind.SourceRemoveUnusedImportsTs, "source.removeUnusedImports.ts");
        Check(CodeActionKind.Source, CodeActionKind.SourceSortImportsTs, "source.sortImports.ts");
        return 1;
    }

    private static void Check(CodeActionKind requested, CodeActionKind child, string expected)
    {
        if (!child.Value.EqualsValue(ByteStringValue.FromString(expected)) || !requested.Contains(child))
            throw new InvalidOperationException($"Code action kind {expected} lost its full source prefix");
    }
}
