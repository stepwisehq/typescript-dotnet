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
        var location = aliasSymbolLinks.TryGet(symbol);
        if (location.IsEmpty)
            return (false, null, null, false, null);
        ref var links = ref location.Span[0];
        return (true, links.immediateTarget, links.aliasTarget, links.referenced, links.typeOnlyDeclaration);
    }

    internal (bool Present, SymbolTable? Exports, Dictionary<ByteStringKey, Node>? TypeOnlyStars, bool Checked) VerificationProjectModule(Symbol symbol)
    {
        var location = moduleSymbolLinks.TryGet(symbol);
        if (location.IsEmpty)
            return (false, null, null, false);
        ref var links = ref location.Span[0];
        return (true, links.resolvedExports, links.typeOnlyExportStarMap, links.exportsChecked);
    }

    internal (bool Present, Symbol? Target, Node? OriginatingImport) VerificationProjectExportType(Symbol symbol)
    {
        var location = exportTypeLinks.TryGet(symbol);
        if (location.IsEmpty)
            return (false, null, null);
        ref var links = ref location.Span[0];
        return (true, links.target, links.originatingImport);
    }

    internal List<(string Name, List<Symbol>? Keys)> VerificationProjectStores() =>
    [
        ("aliasSymbolLinks", aliasSymbolLinks.VerificationKeys()),
        ("moduleSymbolLinks", moduleSymbolLinks.VerificationKeys()),
        ("exportTypeLinks", exportTypeLinks.VerificationKeys()),
    ];
}
