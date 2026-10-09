// Authored read-only module observations, shared by the static AOT validation graph.
using System.Runtime.CompilerServices;
using TypeScript.Scanner;

[assembly: InternalsVisibleTo("Projects.Validation")]
[assembly: InternalsVisibleTo("Projects.Checks")]

namespace TypeScript.Checking;

public sealed partial class Checker
{
    internal (bool Present, Symbol? ImmediateTarget, Symbol? AliasTarget, bool Referenced, Node? TypeOnlyDeclaration) VerificationProjectAlias(Symbol symbol)
    {
        var links = aliasSymbolLinks.TryGet(symbol);
        return links is null ? (false, null, null, false, null) : (true, links.immediateTarget, links.aliasTarget, links.referenced, links.typeOnlyDeclaration);
    }

    internal (bool Present, SymbolTable? Exports, Dictionary<string, Node>? TypeOnlyStars, bool Checked) VerificationProjectModule(Symbol symbol)
    {
        var links = moduleSymbolLinks.TryGet(symbol);
        return links is null ? (false, null, null, false) : (true, links.resolvedExports, links.typeOnlyExportStarMap, links.exportsChecked);
    }

    internal (bool Present, Symbol? Target, Node? OriginatingImport) VerificationProjectExportType(Symbol symbol)
    {
        var links = exportTypeLinks.TryGet(symbol);
        return links is null ? (false, null, null) : (true, links.target, links.originatingImport);
    }

    internal List<(string Name, List<Symbol>? Keys)> VerificationProjectStores() =>
    [
        ("aliasSymbolLinks", aliasSymbolLinks.VerificationKeys()),
        ("moduleSymbolLinks", moduleSymbolLinks.VerificationKeys()),
        ("exportTypeLinks", exportTypeLinks.VerificationKeys()),
    ];
}
