// Authored read-only observations of actual upstream callable cache storage.
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal int VerificationCallableUniqueSymbolCount() => uniqueESSymbolTypes.Count;
    internal bool VerificationCallableUniqueSymbolMembership(Type value) => value.Symbol()is { } symbol && uniqueESSymbolTypes.TryGetValue(symbol, out var cached) && ReferenceEquals(cached, value);
    internal Memory<SourceFileLinks> VerificationCallableSourceFileLinks() => sourceFileLinks.TryGet(sourceFile.AsNode());
    internal Memory<SignatureLinks> VerificationCallableSignatureLinks(Node node) => signatureLinks.TryGet(node);
    internal TypePredicate VerificationCallableNoTypePredicate() => noTypePredicate;
    internal int VerificationCallableCachedSignatureCount() => cachedSignatures.Count;
    internal bool VerificationCallableCachedSignaturesNil() => cachedSignatures is null;
    internal List<(string Name, Signature Value)> VerificationCallableCachedSignatures(Signature owner)
    {
        List<(string Name, Signature Value)> entries = [];
        (string Name, CacheHashKey Key)[] keys = [("base", SignatureKeyBase), ("canonical", SignatureKeyCanonical), ("erased", SignatureKeyErased), ("inner", SignatureKeyInner), ("outer", SignatureKeyOuter)];
        foreach (var entry in keys)
            if (cachedSignatures.TryGetValue(new CachedSignatureKey(owner, entry.Key), out var value))
                entries.Add((entry.Name, value));
        if (cachedSignatures.Keys.Count(key => ReferenceEquals(key.sig, owner)) != entries.Count)
            throw new InvalidDataException("Callable cached signature uses unobserved type-list key family");
        return entries;
    }
}
