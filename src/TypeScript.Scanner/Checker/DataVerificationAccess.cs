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
        return VerificationDataInstantiation(type, value).Membership;
    }

    internal (List<Type> Arguments, TypeAlias? Alias, bool SingleSignature, string Family, bool Membership) VerificationDataInstantiation(Type owner, Type value)
    {
        List<Type> arguments;
        TypeAlias? alias;
        bool single;
        string family;
        CacheHashKey key;
        if (owner.AsInterfaceType()is not null && value.AsTypeReference()is not null)
        {
            arguments = value.AsTypeReference().resolvedTypeArguments;
            alias = null;
            single = false;
            family = "reference";
            key = getTypeListKey(arguments);
        }
        else if ((owner.ObjectFlags() & ObjectFlags.Anonymous) != 0)
        {
            var declarations = owner.Symbol()?.Declarations;
            if (declarations is null || declarations.Count == 0)
                throw new InvalidDataException("Anonymous cache owner has no declaration");
            var parameters = typeNodeLinks.TryGet(declarations[0])?.outerTypeParameters;
            if (parameters is null || parameters.Count == 0)
                throw new InvalidDataException("Anonymous cache has no stored outer parameters");
            family = "anonymous";
            if (ReferenceEquals(owner, value))
            {
                arguments = parameters;
                alias = owner.alias;
                single = false; // Original getObjectTypeInstantiation seed key.
            }
            else
            {
                if (!ReferenceEquals(value.Target(), owner))
                    throw new InvalidDataException("Anonymous cache value has another target");
                single = (owner.ObjectFlags() & ObjectFlags.SingleSignatureType) != 0;
                var mapper = value.Mapper();
                if (single && mapper?.data is CompositeTypeMapper composite)
                    mapper = composite.m1; // Original combines extra mapper after key arguments.
                arguments = mapper?.data switch
                {
                    SimpleTypeMapper m when parameters.Count == 1 && ReferenceEquals(m.source, parameters[0]) => [m.target],
                    ArrayTypeMapper m when (m.sources ?? []).SequenceEqual(parameters) && m.targets?.Count == parameters.Count => m.targets,
                    _ => throw new InvalidDataException("Unknown stored anonymous key mapper")};
                alias = value.alias;
            }

            key = getTypeInstantiationKey(arguments, alias, single);
        }
        else
            throw new InvalidDataException("Unknown instantiated-object key family");
        bool membership = owner.AsObjectType().instantiations.TryGetValue(key, out var found) && ReferenceEquals(found, value);
        return (arguments, alias, single, family, membership);
    }

    internal bool VerificationDataCacheAssociation(Type type)
    {
        var used = new HashSet<CacheHashKey>();
        foreach (var entry in type.AsObjectType().instantiations)
        {
            var association = VerificationDataInstantiation(type, entry.Value);
            var key = association.Family == "reference" ? getTypeListKey(association.Arguments) : getTypeInstantiationKey(association.Arguments, association.Alias, association.SingleSignature);
            if (entry.Key != key || !used.Add(key) || !association.Membership)
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
