// Authored read-only observations; private helpers execute the ported algorithms.
namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal object? VerificationDataModuleSymbolLinks(TypeScript.Scanner.Symbol symbol) => moduleSymbolLinks.TryGet(symbol);
    internal bool VerificationDataPropertiesTypesNil() => propertiesTypes is null;
    internal List<(uint Include, bool IncludeOrigin, Type? Value)> VerificationDataPropertiesTypes(Type type) => propertiesTypes.Where(p => p.Key.typeId == type.id).OrderBy(p => (uint)p.Key.include).ThenBy(p => p.Key.includeOrigin).Select(p => ((uint)p.Key.include, p.Key.includeOrigin, (Type? )p.Value)).ToList();
    internal List<(string Name, bool BoolValue)> VerificationDataReferenceKeys(Type type)
    {
        var forward = getRelationKey(type, type, IntersectionState.None, false, false);
        var again = getRelationKey(type, type, IntersectionState.None, false, false);
        var ignored = getRelationKey(type, type, IntersectionState.None, false, true);
        var state = getRelationKey(type, type, IntersectionState.Source, false, false);
        return[("reference-key.generic-arguments", isTypeReferenceWithGenericArguments(type)), ("reference-key.same", forward.key == again.key), ("reference-key.ignore-equivalent", forward.key == ignored.key), ("reference-key.state-distinct", forward.key != state.key), ("reference-key.constrained", forward.constrained)];
    }

    internal (TypeScript.Scanner.Symbol? Symbol, bool Present) VerificationDataMergedSymbol(TypeScript.Scanner.Symbol symbol) => mergedSymbols.TryGetValue(symbol, out var merged) ? (merged, true) : (null, false);
    internal bool VerificationDataKeyMembership(Type type, Type value)
    {
        if (type.AsInterfaceType()is null || value.AsTypeReference()is null)
            throw new InvalidDataException("Instantiation key association requires additional exact observer");
        return type.AsObjectType().instantiations.TryGetValue(getTypeListKey(value.AsTypeReference().resolvedTypeArguments), out var found) && ReferenceEquals(found, value);
    }

    internal bool VerificationDataCacheAssociation(Type type)
    {
        if (type.AsInterfaceType()is null)
            throw new InvalidDataException("Unknown instantiated-object key family");
        foreach (var entry in type.AsObjectType().instantiations)
        {
            if (entry.Value.AsTypeReference()is null)
                throw new InvalidDataException("Unknown reference cache value");
            if (entry.Key != getTypeListKey(entry.Value.AsTypeReference().resolvedTypeArguments))
                return false;
        }

        return true;
    }

    internal bool VerificationDataInternMembership(Type type)
    {
        if ((type.Flags() & TypeFlags.Union) != 0)
            return unionTypes.TryGetValue(getUnionKey(type.Types(), type.AsUnionType().origin, type.alias), out var found) && ReferenceEquals(found, type);
        if ((type.Flags() & TypeFlags.Intersection) != 0)
            return intersectionTypes.TryGetValue(getIntersectionKey(type.Types(), IntersectionFlags.None, type.alias), out var found) && ReferenceEquals(found, type);
        throw new InvalidDataException("Noncomposite intern membership");
    }

    internal List<(string Name, bool BoolValue)> VerificationDataKeyEquality(Type source, Type target, TypeAlias? alias1, TypeAlias? alias2)
    {
        List<Type> types = [source, target], reverse = [target, source];
        var forward = getRelationKey(source, target, IntersectionState.None, false, false);
        var again = getRelationKey(source, target, IntersectionState.None, false, false);
        var identity = getRelationKey(source, target, IntersectionState.None, true, false);
        var identityReverse = getRelationKey(target, source, IntersectionState.None, true, false);
        var reversed = getRelationKey(target, source, IntersectionState.None, false, false);
        var ignored = getRelationKey(source, target, IntersectionState.None, false, true);
        return[("type-list.same", getTypeListKey(types) == getTypeListKey(types)), ("type-list.reverse", getTypeListKey(types) == getTypeListKey(reverse)), ("alias.same", getAliasKey(alias1) == getAliasKey(alias1)), ("alias.other", getAliasKey(alias1) == getAliasKey(alias2)), ("union.alias", getUnionKey(types, null, alias1) == getUnionKey(types, null, alias2)), ("intersection.alias", getIntersectionKey(types, IntersectionFlags.None, alias1) == getIntersectionKey(types, IntersectionFlags.None, alias2)), ("intersection.no-constraint-alias", getIntersectionKey(types, IntersectionFlags.NoConstraintReduction, alias1) == getIntersectionKey(types, IntersectionFlags.NoConstraintReduction, alias2)), ("instantiation.alias", getTypeAliasInstantiationKey(types, alias1) == getTypeAliasInstantiationKey(types, alias2)), ("relation.same", forward.key == again.key), ("relation.reverse", forward.key == reversed.key), ("relation.identity-reverse", identity.key == identityReverse.key), ("relation.ignore", forward.key == ignored.key), ("relation.constrained", forward.constrained)];
    }
}
