// Authored Native AOT mirror of independently discovered original callable fields.
using TypeScript.Scanner;
using Encoder = TypeScript.Scanner.ParserValidation.Encoder;

namespace TypeScript.Checking.Validation;
public sealed record CallableQueryIdentity(int Node, bool TypeIdentity, bool SymbolIdentity);
public sealed record CallableCall(int Node, int? Signature, bool RepeatedIdentity);
public sealed record CallableCache(int Owner, string Name, int Value, bool RepeatedIdentity);
public sealed record CallableDiagnosticState(DiagnosticValue Value, bool RelatedNull, bool ChainNull, List<CallableDiagnosticState> Related, List<CallableDiagnosticState> Chain);
public sealed class CallableGraphValue
{
    public bool SourceFileLinksPresent { get; set; }
    public DataRecord? SourceFileLinks { get; set; }
    public int? NoPredicateSentinel { get; set; }
    public List<CallableQueryIdentity> Queries { get; set; } = [];
    public List<DataRecord> Signatures { get; set; } = [];
    public List<DataRecord> Predicates { get; set; } = [];
    public List<DataRecord> Composites { get; set; } = [];
    public List<CallableCall> Calls { get; set; } = [];
    public List<DataLinkObservation> SignatureLinks { get; set; } = [];
    public bool CachedSignaturesNull { get; set; }
    public List<CallableCache> CachedSignatures { get; set; } = [];
    public bool BindDiagnosticsNull { get; set; }
    public bool FileDiagnosticsNull { get; set; }
    public bool GlobalDiagnosticsNull { get; set; }
    public List<CallableDiagnosticState> BindDiagnosticStates { get; set; } = [];
    public List<CallableDiagnosticState> FileDiagnosticStates { get; set; } = [];
    public List<CallableDiagnosticState> GlobalDiagnosticStates { get; set; } = [];
}

public sealed partial class Graph
{
    private CallableGraphValue? callables;
    private readonly List<CallableQueryIdentity> callableQueries = [];
    public void RecordCallableQuery(int node, bool typeIdentity, bool symbolIdentity) => callableQueries.Add(new(node, typeIdentity, symbolIdentity));
    private readonly Dictionary<Signature, int> callableSignatures = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<TypePredicate, int> callablePredicates = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<CompositeSignature, int> callableComposites = new(ReferenceEqualityComparer.Instance);
    private int callableCacheCursor;
    private int? Signature(Signature? signature)
    {
        if (signature is null)
            return null;
        if (callableSignatures.TryGetValue(signature, out int id))
            return id;
        if (callables is null)
            throw new InvalidDataException("Signature outside callable observation");
        id = callables.Signatures.Count;
        callableSignatures.Add(signature, id);
        var record = new DataRecord(id, "Signature", []);
        callables.Signatures.Add(record);
        checker.GetReturnTypeOfSignature(signature);
        checker.GetTypePredicateOfSignature(signature);
        foreach (var parameter in signature.parameters ?? [])
            checker.getTypeOfSymbol(parameter);
        record.Fields.AddRange([F("flags", "number", signature.flags), F("minArgumentCount", "number", signature.minArgumentCount), F("resolvedMinArgumentCount", "number", signature.resolvedMinArgumentCount), F("declaration", "node", signature.declaration), F("typeParameters", "types", signature.typeParameters), F("parameters", "symbols", signature.parameters), F("thisParameter", "symbol", signature.thisParameter), F("resolvedReturnType", "type", signature.resolvedReturnType), F("resolvedTypePredicate", "predicate", signature.resolvedTypePredicate), F("target", "signature", signature.target), F("mapper", "mapper", signature.mapper), F("isolatedSignatureType", "type", signature.isolatedSignatureType), F("composite", "composite", signature.composite)]);
        return id;
    }

    private int? Predicate(TypePredicate? predicate)
    {
        if (predicate is null)
            return null;
        if (callablePredicates.TryGetValue(predicate, out int id))
            return id;
        if (callables is null)
            throw new InvalidDataException("Predicate outside callable observation");
        id = callables.Predicates.Count;
        callablePredicates.Add(predicate, id);
        var record = new DataRecord(id, "TypePredicate", []);
        callables.Predicates.Add(record);
        record.Fields.AddRange([F("kind", "number", predicate.kind), F("parameterIndex", "number", predicate.parameterIndex), F("parameterName", "bytes", predicate.parameterName), F("t", "type", predicate.t)]);
        return id;
    }

    private int? Composite(CompositeSignature? composite)
    {
        if (composite is null)
            return null;
        if (callableComposites.TryGetValue(composite, out int id))
            return id;
        if (callables is null)
            throw new InvalidDataException("Composite outside callable observation");
        id = callables.Composites.Count;
        callableComposites.Add(composite, id);
        var record = new DataRecord(id, "CompositeSignature", []);
        callables.Composites.Add(record);
        record.Fields.AddRange([F("isUnion", "bool", composite.isUnion), F("signatures", "signatures", GoSlices.ToListOrNull(composite.signatures))]);
        return id;
    }

    private void StartCallables()
    {
        callables = new()
        {
            Queries = callableQueries,
            CachedSignaturesNull = checker.VerificationCallableCachedSignaturesNil()
        };
        result.CallableGraph = callables;
        for (int id = 0; id < syntax.RawNodes.Count; id++)
            if (syntax.RawNodes[id].Kind == Kind.CallExpression)
            {
                var node = syntax.RawNodes[id];
                var signature = checker.GetResolvedSignature(node);
                callables.Calls.Add(new(id, Signature(signature), ReferenceEquals(signature, checker.GetResolvedSignature(node))));
            }

        for (int id = 0; id < syntax.RawNodes.Count; id++)
        {
            var location = checker.VerificationCallableSignatureLinks(syntax.RawNodes[id]);
            if (location.IsEmpty)
                callables.SignatureLinks.Add(new("node", id, "signatureLinks", false, []));
            else
            {
                ref var link = ref location.Span[0];
                callables.SignatureLinks.Add(new("node", id, "signatureLinks", true, [F("resolvedSignature", "signature", link.resolvedSignature), F("effectsSignature", "signature", link.effectsSignature), F("decoratorSignature", "signature", link.decoratorSignature)]));
            }
        }

        var sourceLocation = checker.VerificationCallableSourceFileLinks();
        callables.SourceFileLinksPresent = !sourceLocation.IsEmpty;
        if (!sourceLocation.IsEmpty)
        {
            ref var sourceLinks = ref sourceLocation.Span[0];
            callables.SourceFileLinks = new(0, "SourceFileLinks", [F("typeChecked", "bool", sourceLinks.typeChecked), F("unusedChecked", "bool", sourceLinks.unusedChecked), F("externalHelpersModule", "symbol", sourceLinks.externalHelpersModule), F("requestedExternalEmitHelpers", "number", sourceLinks.requestedExternalEmitHelpers), F("deferredNodes", "nodes", sourceLinks.deferredNodes?.Values().ToList() ?? []), F("identifierCheckNodes", "nodes", GoSlices.ToListOrNull(sourceLinks.identifierCheckNodes)), F("localJsxNamespace", "bytes", sourceLinks.localJsxNamespace), F("localJsxFragmentNamespace", "bytes", sourceLinks.localJsxFragmentNamespace), F("localJsxFactory", "node", sourceLinks.localJsxFactory), F("localJsxFragmentFactory", "node", sourceLinks.localJsxFragmentFactory), F("jsxFragmentType", "type", sourceLinks.jsxFragmentType)]);
        }
        callables.NoPredicateSentinel = Predicate(checker.VerificationCallableNoTypePredicate());
    }

    private void FinishCallableCaches()
    {
        while (callables is not null && callableCacheCursor < callables.Signatures.Count)
        {
            int id = callableCacheCursor++;
            var owner = callableSignatures.Single(p => p.Value == id).Key;
            foreach (var entry in checker.VerificationCallableCachedSignatures(owner))
                callables.CachedSignatures.Add(new(id, entry.Name, Signature(entry.Value)!.Value, checker.VerificationCallableCachedSignatures(owner).Any(p => p.Name == entry.Name && ReferenceEquals(p.Value, entry.Value))));
        }
    }
}
