// Authored friendship for static Native AOT multi-file state/cache observations.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Collections.Validation")]
namespace TypeScript.Checking;
public sealed partial class Checker
{
    // Authored read-only instrumentation. Missing entries stay absent; TryGet
    // does not invoke any semantic resolver or allocate a logical link.
    internal List<(string Store, object Value)> VerificationCollectionsNodeStates(TypeScript.Scanner.Node node)
    {
        var result = new List<(string, object)>();
        void Add(string name, object? value)
        {
            if (value is not null)
                result.Add((name, value));
        }

        Add("arrayLiteralLinks", arrayLiteralLinks.TryGet(node));
        Add("nodeLinks", nodeLinks.TryGet(node));
        Add("signatureLinks", signatureLinks.TryGet(node));
        if (node.Kind == TypeScript.Scanner.Kind.SourceFile)
            Add("sourceFileLinks", sourceFileLinks.TryGet(node));
        // Physical untouched-page presence is process-AST-ID-dependent; only
        // the actual resolvedSymbol is a selected semantic state.
        Add("symbolNodeLinks.resolvedSymbol", symbolNodeLinks.TryGet(node)?.resolvedSymbol);
        Add("typeNodeLinks", typeNodeLinks.TryGet(node));
        return result;
    }

    internal List<(string Store, object Value)> VerificationCollectionsSymbolStates(TypeScript.Scanner.Symbol symbol)
    {
        var result = new List<(string, object)>();
        void Add(string name, object? value)
        {
            if (value is not null)
                result.Add((name, value));
        }

        Add("ReverseMappedSymbolLinks", ReverseMappedSymbolLinks.TryGet(symbol));
        Add("declaredTypeLinks", declaredTypeLinks.TryGet(symbol));
        Add("deferredSymbolLinks", deferredSymbolLinks.TryGet(symbol));
        Add("exportTypeLinks", exportTypeLinks.TryGet(symbol));
        Add("mappedSymbolLinks", mappedSymbolLinks.TryGet(symbol));
        Add("markedAssignmentSymbolLinks", markedAssignmentSymbolLinks.TryGet(symbol));
        Add("membersAndExportsLinks", membersAndExportsLinks.TryGet(symbol));
        Add("moduleSymbolLinks", moduleSymbolLinks.TryGet(symbol));
        Add("symbolReferenceLinks", symbolReferenceLinks.TryGet(symbol));
        Add("symbolContainerLinks", symbolContainerLinks.TryGet(symbol));
        Add("typeAliasLinks", typeAliasLinks.TryGet(symbol));
        Add("valueSymbolLinks", valueSymbolLinks.TryGet(symbol));
        Add("varianceLinks", varianceLinks.TryGet(symbol));
        return result;
    }

    internal List<(string Store, string OwnerKind, List<object>? Keys)> VerificationCollectionsLogicalStoreKeys()
    {
        var result = new List<(string, string, List<object>? )>();
        void Add<K>(string name, string kind, List<K>? keys)
            where K : notnull => result.Add((name, kind, keys?.Select(key => (object)key).ToList()));
        Add("arrayLiteralLinks", "node", arrayLiteralLinks.VerificationKeys());
        Add("nodeLinks", "node", nodeLinks.VerificationKeys());
        Add("signatureLinks", "node", signatureLinks.VerificationKeys());
        Add("sourceFileLinks", "node", sourceFileLinks.VerificationKeys());
        Add("typeNodeLinks", "node", typeNodeLinks.VerificationKeys());
        Add("ReverseMappedSymbolLinks", "symbol", ReverseMappedSymbolLinks.VerificationKeys());
        Add("declaredTypeLinks", "symbol", declaredTypeLinks.VerificationKeys());
        Add("deferredSymbolLinks", "symbol", deferredSymbolLinks.VerificationKeys());
        Add("exportTypeLinks", "symbol", exportTypeLinks.VerificationKeys());
        Add("mappedSymbolLinks", "symbol", mappedSymbolLinks.VerificationKeys());
        Add("markedAssignmentSymbolLinks", "symbol", markedAssignmentSymbolLinks.VerificationKeys());
        Add("membersAndExportsLinks", "symbol", membersAndExportsLinks.VerificationKeys());
        Add("moduleSymbolLinks", "symbol", moduleSymbolLinks.VerificationKeys());
        Add("symbolContainerLinks", "symbol", symbolContainerLinks.VerificationKeys());
        Add("symbolReferenceLinks", "symbol", symbolReferenceLinks.VerificationKeys());
        Add("typeAliasLinks", "symbol", typeAliasLinks.VerificationKeys());
        Add("varianceLinks", "symbol", varianceLinks.VerificationKeys());
        return result;
    }
}
