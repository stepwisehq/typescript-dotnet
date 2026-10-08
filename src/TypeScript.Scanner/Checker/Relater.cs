// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/relater.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected scalar and finite anonymous-object relations; excluded generic/signature/index paths fail explicitly.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    [Flags]
    private enum IntersectionState : uint
    {
        None = 0,
        Source = 1,
        Target = 2
    }

    [Flags]
    private enum RecursionFlags : uint
    {
        None = 0,
        Source = 1,
        Target = 2,
        Both = Source | Target
    }

    [Flags]
    private enum ExpandingFlags : byte
    {
        None = 0,
        Source = 1,
        Target = 2,
        Both = Source | Target
    }

    [Flags]
    private enum RelationComparisonResult : uint
    {
        None = 0,
        Succeeded = 1,
        Failed = 2,
        ReportsUnmeasurable = 1 << 3,
        ReportsUnreliable = 1 << 4,
        ComplexityOverflow = 1 << 5,
        ReportsMask = ReportsUnmeasurable | ReportsUnreliable,
        Overflow = ComplexityOverflow
    }

    private sealed class Relation
    {
        private Dictionary<CacheHashKey, RelationComparisonResult> results;
        internal RelationComparisonResult get(CacheHashKey key) => results != null && results.TryGetValue(key, out var value) ? value : RelationComparisonResult.None;
        internal void set(CacheHashKey key, RelationComparisonResult result) => (results ??= new())[key] = result;
        internal int size() => results?.Count ?? 0;
    }

    private readonly Relation assignableRelation = new(), identityRelation = new(), subtypeRelation = new(), strictSubtypeRelation = new(), comparableRelation = new();
    private bool isTypeAssignableTo(Type source, Type target) => isTypeRelatedTo(source, target, assignableRelation);
    private bool isTypeIdenticalTo(Type source, Type target) => isTypeRelatedTo(source, target, identityRelation);
    internal Ternary compareTypesIdentical(Type source, Type target) => isTypeRelatedTo(source, target, identityRelation) ? Ternary.True : Ternary.False;
    private Ternary compareTypesAssignableSimple(Type source, Type target) => isTypeRelatedTo(source, target, assignableRelation) ? Ternary.True : Ternary.False;
    private Ternary compareTypesAssignableWorker(Type source, Type target, bool reportErrors) => compareTypesAssignableSimple(source, target);
    private bool isTypeRelatedTo(Type source, Type target, Relation relation)
    {
        if (isFreshLiteralType(source))
            source = source.AsLiteralType().regularType;
        if (isFreshLiteralType(target))
            target = target.AsLiteralType().regularType;
        if (source == target)
            return true;
        if (relation != identityRelation)
        {
            if (relation == comparableRelation && (target.flags & TypeFlags.Never) == 0 && isSimpleTypeRelatedTo(target, source, relation) || isSimpleTypeRelatedTo(source, target, relation))
                return true;
        }
        else if (((source.flags | target.flags) & (TypeFlags.UnionOrIntersection | TypeFlags.IndexedAccess | TypeFlags.Conditional | TypeFlags.Substitution)) == 0)
        {
            if (source.flags != target.flags)
                return false;
            if ((source.flags & TypeFlags.Singleton) != 0)
                return true;
        }

        if ((source.flags & target.flags & TypeFlags.Object) != 0)
        {
            var(id, _) = getRelationKey(source, target, IntersectionState.None, relation == identityRelation, false);
            var related = relation.get(id);
            if (related != RelationComparisonResult.None)
                return (related & RelationComparisonResult.Succeeded) != 0;
        }

        if (((source.flags | target.flags) & TypeFlags.StructuredOrInstantiable) != 0)
            return checkTypeRelatedTo(source, target, relation, null);
        return false;
    }

    // isSimpleTypeRelatedTo (205): selected primitive branches, enum/object throw.
    private bool isSimpleTypeRelatedTo(Type source, Type target, Relation relation, Action<Message, object[]> errorReporter = null)
    {
        var s = source.flags;
        var t = target.flags;
        if ((t & TypeFlags.Any) != 0 || (s & TypeFlags.Never) != 0 || source == wildcardType)
            return true;
        if ((t & TypeFlags.Unknown) != 0 && !(relation == strictSubtypeRelation && (s & TypeFlags.Any) != 0))
            return true;
        if ((t & TypeFlags.Never) != 0)
            return false;
        if ((s & TypeFlags.StringLike) != 0 && (t & TypeFlags.String) != 0)
            return true;
        if (((s | t) & TypeFlags.EnumLike) != 0)
            throw unsupported("isSimpleTypeRelatedTo: enum");
        if ((s & TypeFlags.NumberLike) != 0 && (t & TypeFlags.Number) != 0)
            return true;
        if ((s & TypeFlags.BigIntLike) != 0 && (t & TypeFlags.BigInt) != 0)
            return true;
        if ((s & TypeFlags.BooleanLike) != 0 && (t & TypeFlags.Boolean) != 0)
            return true;
        if ((s & TypeFlags.ESSymbolLike) != 0 && (t & TypeFlags.ESSymbol) != 0)
            return true;
        if ((s & TypeFlags.Undefined) != 0 && (!strictNullChecks && (t & TypeFlags.UnionOrIntersection) == 0 || (t & (TypeFlags.Undefined | TypeFlags.Void)) != 0))
            return true;
        if ((s & TypeFlags.Null) != 0 && (!strictNullChecks && (t & TypeFlags.UnionOrIntersection) == 0 || (t & TypeFlags.Null) != 0))
            return true;
        if ((s & TypeFlags.Object) != 0 && (t & TypeFlags.NonPrimitive) != 0)
            return !(relation == strictSubtypeRelation && IsEmptyAnonymousObjectType(source) && (source.objectFlags & ObjectFlags.FreshLiteral) == 0);
        if (relation == assignableRelation || relation == comparableRelation)
        {
            if ((s & TypeFlags.Any) != 0)
                return true;
            if (isUnknownLikeUnionType(target))
                return true;
        }

        return false;
    }

    private bool checkTypeAssignableToAndOptionallyElaborate(Type source, Type target, Node errorNode, Node expr) => checkTypeAssignableToAndOptionallyElaborate(source, target, errorNode, expr, null, null);
    private bool checkTypeAssignableToAndOptionallyElaborate(Type source, Type target, Node errorNode, Node expr, Message headMessage, List<Diagnostic> diagnosticOutput) => checkTypeRelatedToAndOptionallyElaborate(source, target, assignableRelation, errorNode, expr, headMessage, diagnosticOutput);
    private bool checkTypeRelatedToAndOptionallyElaborate(Type source, Type target, Relation relation, Node errorNode, Node expr, Message headMessage, List<Diagnostic> diagnosticOutput)
    {
        if (isTypeRelatedTo(source, target, relation))
            return true;
        if (errorNode != null && !elaborateError(expr, source, target, relation, headMessage, diagnosticOutput))
            return checkTypeRelatedToEx(source, target, relation, errorNode, headMessage, diagnosticOutput);
        return false;
    }

    private bool checkTypeRelatedTo(Type source, Type target, Relation relation, Node errorNode) => checkTypeRelatedToEx(source, target, relation, errorNode, null, null);
    private bool checkTypeRelatedToEx(Type source, Type target, Relation relation, Node errorNode) => checkTypeRelatedToEx(source, target, relation, errorNode, null, null);
    private bool checkTypeRelatedToEx(Type source, Type target, Relation relation, Node errorNode, Message headMessage, List<Diagnostic> diagnosticOutput)
    {
        var r = getRelater();
        r.relation = relation;
        r.errorNode = errorNode;
        r.relationCount = (16_000_000 - relation.size()) / 8;
        var result = r.isRelatedToEx(source, target, RecursionFlags.Both, errorNode != null, headMessage, IntersectionState.None);
        if (r.overflow)
        {
            var(id, _) = getRelationKey(source, target, IntersectionState.None, relation == identityRelation, false);
            relation.set(id, RelationComparisonResult.Failed | RelationComparisonResult.ComplexityOverflow);
            errorNode ??= currentNode;
            reportDiagnostic(NewDiagnosticForNode(errorNode, Diagnostics.Excessive_complexity_comparing_types_0_and_1, typeToStringByteString(source), typeToStringByteString(target)), diagnosticOutput);
        }
        else if (r.errorChain != null)
        {
            // Export/import retry diagnostics are outside this script-only boundary.
            if (source.symbol != null && (source.symbol.Flags & SymbolFlags.Alias) != 0)
                throw unsupported("checkTypeRelatedToEx: export/import retry");
            reportDiagnostic(createDiagnosticChainFromErrorChain(r.errorChain, r.errorNode, r.relatedInfo), diagnosticOutput);
        }

        putRelater(r);
        return result != Ternary.False;
    }

    private sealed class ErrorChain
    {
        internal ErrorChain next;
        internal Message message;
        internal object[] args;
    }

    private static Diagnostic createDiagnosticChainFromErrorChain(ErrorChain chain, Node errorNode, List<Diagnostic> relatedInfo)
    {
        while (chain != null && chain.message.ElidedInCompatibilityPyramid())
            chain = chain.next;
        if (chain == null)
            return null;
        var next = createDiagnosticChainFromErrorChain(chain.next, errorNode, relatedInfo);
        if (next == null)
            return NewDiagnosticForNode(errorNode, chain.message, chain.args).SetRelatedInfo(relatedInfo);
        return Ast.NewDiagnosticChain(next, chain.message, chain.args);
    }

    private void reportDiagnostic(Diagnostic diagnostic, List<Diagnostic> diagnosticOutput)
    {
        if (diagnostic != null)
        {
            if (diagnosticOutput != null)
                diagnosticOutput.Add(diagnostic);
            else
                addDiagnostic(diagnostic);
        }
    }

    private Relater getRelater()
    {
        var r = freeRelater ?? new Relater(this);
        freeRelater = r.next;
        return r;
    }

    private void putRelater(Relater r)
    {
        r.maybeKeysSet.Clear();
        r.maybeKeys.Clear();
        r.sourceStack.Clear();
        r.targetStack.Clear();
        r.relation = null;
        r.errorNode = null;
        r.errorChain = null;
        r.relatedInfo = null;
        r.expandingFlags = 0;
        r.overflow = false;
        r.relationCount = 0;
        r.next = freeRelater;
        freeRelater = r;
    }

    private static void guardFiniteObject(Type t, string branch)
    {
        if (t.alias != null || (t.flags & (TypeFlags.Instantiable | TypeFlags.Intersection | TypeFlags.EnumLike)) != 0 || (t.objectFlags & (ObjectFlags.ClassOrInterface | ObjectFlags.Reference | ObjectFlags.Tuple | ObjectFlags.Mapped | ObjectFlags.Instantiated | ObjectFlags.ReverseMapped | ObjectFlags.JsxAttributes)) != 0)
            throw unsupported(branch);
    }

    // The source signature/index relation loops have no iterations for admitted data-only objects.
    // Nonempty storage is rejected before any selected relation can treat it as an absent signature/index.
    private void guardAbsentSignaturesAndIndexes(Type t, string branch)
    {
        if ((t.flags & TypeFlags.Object) != 0)
        {
            var data = resolveStructuredTypeMembers(t);
            if ((data.signatures?.Count ?? 0) != 0 || (data.indexInfos?.Count ?? 0) != 0)
                throw unsupported(branch);
        }
        else if ((t.flags & TypeFlags.Union) != 0)
            foreach (var part in t.Types())
                guardAbsentSignaturesAndIndexes(part, branch);
    }

    private bool isWeakType(Type t)
    {
        guardFiniteObject(t, "isWeakType: substitution/intersection");
        if ((t.flags & TypeFlags.Object) != 0)
        {
            var resolved = resolveStructuredTypeMembers(t);
            guardAbsentSignaturesAndIndexes(t, "isWeakType: signatures/indexes");
            return (resolved.properties?.Count ?? 0) > 0 && resolved.properties.All(p => (p.Flags & SymbolFlags.Optional) != 0);
        }

        return false;
    }

    private bool hasCommonProperties(Type source, Type target, bool isComparingJsxAttributes)
    {
        foreach (var prop in getPropertiesOfType(source) ?? [])
            if (isKnownProperty(target, prop.Name, isComparingJsxAttributes))
                return true;
        return false;
    }

    private bool isKnownProperty(Type targetType, string name, bool isComparingJsxAttributes)
    {
        guardFiniteObject(targetType, "isKnownProperty: substitution/intersection");
        if (isComparingJsxAttributes)
            throw unsupported("isKnownProperty: JSX attributes");
        if ((targetType.flags & TypeFlags.Object) != 0)
        {
            guardAbsentSignaturesAndIndexes(targetType, "isKnownProperty: signatures/indexes");
            if (getPropertyOfObjectType(targetType, name) != null)
                return true;
        }

        if (isExcessPropertyCheckTarget(targetType) && (targetType.flags & TypeFlags.Union) != 0)
            foreach (var t in targetType.Types())
                if (isKnownProperty(t, name, isComparingJsxAttributes))
                    return true;
        return false;
    }

    private static bool isExcessPropertyCheckTarget(Type type)
    {
        guardFiniteObject(type, "isExcessPropertyCheckTarget: substitution/intersection");
        return (type.flags & TypeFlags.Object) != 0 && (type.objectFlags & ObjectFlags.ObjectLiteralPatternWithComputedProperties) == 0 || (type.flags & TypeFlags.NonPrimitive) != 0 || (type.flags & TypeFlags.Union) != 0 && type.Types().Any(isExcessPropertyCheckTarget);
    }

    private readonly record struct RecursionId(object value);
    private bool isDeeplyNestedType(Type t, List<Type> stack, int maxDepth)
    {
        if (stack.Count >= maxDepth)
        {
            var target = getRecursionIdentityTarget(t);
            if ((target.flags & TypeFlags.Intersection) != 0)
                throw unsupported("isDeeplyNestedType: intersection");
            var identity = getRecursionIdentityFromTarget(target);
            int count = 0;
            uint lastTypeId = 0;
            foreach (var part in stack)
                if (hasMatchingRecursionIdentity(part, identity))
                {
                    if (part.id >= lastTypeId && ++count >= maxDepth)
                        return true;
                    lastTypeId = part.id;
                }
        }

        return false;
    }

    private static bool hasMatchingRecursionIdentity(Type t, RecursionId identity) => getRecursionIdentityFromTarget(getRecursionIdentityTarget(t)) == identity;
    private static RecursionId getRecursionIdentity(Type t) => getRecursionIdentityFromTarget(getRecursionIdentityTarget(t));
    private static Type getRecursionIdentityTarget(Type t)
    {
        if ((t.flags & TypeFlags.IndexedAccess) != 0 || (t.objectFlags & ObjectFlags.InstantiatedMapped) == ObjectFlags.InstantiatedMapped)
            throw unsupported("getRecursionIdentityTarget: indexed/mapped");
        return t;
    }

    private static RecursionId getRecursionIdentityFromTarget(Type t)
    {
        guardFiniteObject(t, "getRecursionIdentityFromTarget: reference/tuple/type parameter/conditional");
        if ((t.flags & TypeFlags.Object) != 0 && (t.objectFlags & (ObjectFlags.ObjectLiteral | ObjectFlags.ArrayLiteral)) == 0)
            if (t.symbol != null && !((t.objectFlags & ObjectFlags.Anonymous) != 0 && (t.symbol.Flags & SymbolFlags.Class) != 0) && (t.objectFlags & ObjectFlags.FromTypeNode) == 0)
                return new(t.symbol);
        return new(t);
    }

    private bool isTypeSubtypeOf(Type source, Type target) => isTypeRelatedTo(source, target, subtypeRelation);
    private bool isTypeStrictSubtypeOf(Type source, Type target) => isTypeRelatedTo(source, target, strictSubtypeRelation);
    private Ternary compareTypesSubtypeOf(Type source, Type target) => isTypeSubtypeOf(source, target) ? Ternary.True : Ternary.False;
    private interface Discriminator
    {
        int len();
        string name(int index);
        bool matches(int index, Type t);
    }

    private sealed class TypeDiscriminator(Checker c, List<Symbol> props, Func<Type, Type, Ternary> isRelatedTo) : Discriminator
    {
        public int len() => props.Count;
        public string name(int index) => props[index].Name;
        public bool matches(int index, Type t) => c.getTypeOfSymbol(props[index]).Distributed().Any(s => isRelatedTo(s, t) != Ternary.False);
    }

    private Type discriminateTypeByDiscriminableItems(Type target, Discriminator discriminator)
    {
        var types = target.Types();
        var include = new Ternary[types.Count];
        for (int i = 0; i < types.Count; i++)
            if ((types[i].flags & TypeFlags.Primitive) == 0 && (getReducedType(types[i]).flags & TypeFlags.Never) == 0)
                include[i] = Ternary.True;
        for (int n = 0; n < discriminator.len(); n++)
        {
            bool matched = false;
            for (int i = 0; i < types.Count; i++)
                if (include[i] != Ternary.False)
                {
                    var targetType = getTypeOfPropertyOrIndexSignatureOfType(types[i], discriminator.name(n));
                    if (targetType != null)
                    {
                        if (discriminator.matches(n, targetType))
                            matched = true;
                        else
                            include[i] = Ternary.Maybe;
                    }
                }

            for (int i = 0; i < types.Count; i++)
                if (include[i] == Ternary.Maybe)
                    include[i] = matched ? Ternary.False : Ternary.True;
        }

        if (include.Contains(Ternary.False))
        {
            List<Type> filteredTypes = null;
            for (int i = 0; i < types.Count; i++)
                if (include[i] == Ternary.True)
                    (filteredTypes ??= []).Add(types[i]);
            var filtered = getUnionTypeEx(filteredTypes, UnionReduction.None, null, null);
            if ((filtered.flags & TypeFlags.Never) == 0)
                return filtered;
        }

        return target;
    }

    private Type findMatchingDiscriminantType(Type source, Type target, Func<Type, Type, Ternary> isRelatedTo)
    {
        if ((target.flags & TypeFlags.Union) != 0 && (source.flags & (TypeFlags.Intersection | TypeFlags.Object)) != 0)
        {
            var match = getMatchingUnionConstituentForType(target, source);
            if (match != null)
                return match;
            var props = findDiscriminantProperties(getPropertiesOfType(source), target);
            if ((props?.Count ?? 0) != 0)
            {
                var discriminated = discriminateTypeByDiscriminableItems(target, new TypeDiscriminator(this, props, isRelatedTo));
                if (discriminated != target)
                    return discriminated;
            }
        }

        return null;
    }

    private List<Symbol> findDiscriminantProperties(List<Symbol> sourceProperties, Type target)
    {
        List<Symbol> result = null;
        foreach (var property in sourceProperties ?? [])
            if (isDiscriminantProperty(target, property.Name))
                (result ??= []).Add(property);
        return result;
    }

    private bool isDiscriminantProperty(Type t, string name)
    {
        if (t != null && (t.flags & TypeFlags.Union) != 0)
        {
            var prop = getUnionOrIntersectionProperty(t, name, false);
            if (prop != null && (prop.CheckFlags & CheckFlags.SyntheticProperty) != 0)
            {
                if ((prop.CheckFlags & CheckFlags.IsDiscriminantComputed) == 0)
                {
                    prop.CheckFlags |= CheckFlags.IsDiscriminantComputed;
                    var propertyType = getTypeOfSymbol(prop);
                    guardFiniteObject(propertyType, "isDiscriminantProperty: generic type");
                    if ((prop.CheckFlags & CheckFlags.NonUniformAndLiteral) == CheckFlags.NonUniformAndLiteral)
                        prop.CheckFlags |= CheckFlags.IsDiscriminant;
                }

                return (prop.CheckFlags & CheckFlags.IsDiscriminant) != 0;
            }
        }

        return false;
    }

    private Type getMatchingUnionConstituentForType(Type unionType, Type t)
    {
        var name = getKeyPropertyName(unionType);
        if (name == "")
            return null;
        var propType = getTypeOfPropertyOfType(t, name);
        return propType == null ? null : getConstituentTypeForKeyType(unionType, propType);
    }

    private string getKeyPropertyName(Type t)
    {
        var u = t.AsUnionType();
        if (u.keyPropertyName == "")
            (u.keyPropertyName, u.constituentMap) = computeKeyPropertyNameAndMap(t);
        return u.keyPropertyName == Ast.InternalSymbolNameMissing ? "" : u.keyPropertyName;
    }

    private Type getConstituentTypeForKeyType(Type t, Type keyType)
    {
        var map = t.AsUnionType().constituentMap;
        var result = map != null && map.TryGetValue(getRegularTypeOfLiteralType(keyType), out var value) ? value : null;
        return result != unknownType ? result : null;
    }

    private (string, Dictionary<Type, Type>) computeKeyPropertyNameAndMap(Type t)
    {
        var types = t.Types();
        if (types.Count < 10 || (t.objectFlags & ObjectFlags.PrimitiveUnion) != 0 || types.Count(isObjectOrInstantiableNonPrimitive) < 10)
            return (Ast.InternalSymbolNameMissing, null);
        var name = getKeyPropertyCandidateName(types);
        if (name == "")
            return (Ast.InternalSymbolNameMissing, null);
        var map = mapTypesByKeyProperty(types, name);
        return map == null ? (Ast.InternalSymbolNameMissing, null) : (name, map);
    }

    private static bool isObjectOrInstantiableNonPrimitive(Type t) => (t.flags & (TypeFlags.Object | TypeFlags.InstantiableNonPrimitive)) != 0;
    private string getKeyPropertyCandidateName(List<Type> types)
    {
        foreach (var t in types)
            if (isObjectOrInstantiableNonPrimitive(t))
                foreach (var prop in getPropertiesOfType(t) ?? [])
                    if (isUnitType(getTypeOfSymbol(prop)))
                        return prop.Name;
        return "";
    }

    private Dictionary<Type, Type> mapTypesByKeyProperty(List<Type> types, string name)
    {
        var map = new Dictionary<Type, Type>();
        int count = 0;
        foreach (var t in types)
            if ((t.flags & (TypeFlags.Object | TypeFlags.Intersection | TypeFlags.InstantiableNonPrimitive)) != 0)
            {
                var discriminant = getTypeOfPropertyOfType(t, name);
                if (discriminant == null || !isLiteralType(discriminant))
                    return null;
                bool duplicate = false;
                foreach (var d in discriminant.Distributed())
                {
                    var key = getRegularTypeOfLiteralType(d);
                    if (!map.TryGetValue(key, out var existing) || existing == null)
                        map[key] = t;
                    else if (existing != unknownType)
                    {
                        map[key] = unknownType;
                        duplicate = true;
                    }
                }

                if (!duplicate)
                    count++;
            }

        return count >= 10 && count * 2 >= types.Count ? map : null;
    }

    private Type filterPrimitivesIfContainsNonPrimitive(Type unionType)
    {
        if (maybeTypeOfKind(unionType, TypeFlags.NonPrimitive))
        {
            var result = filterType(unionType, isNonPrimitiveType);
            if ((result.flags & TypeFlags.Never) == 0)
                return result;
        }

        return unionType;
    }

    private static bool isNonPrimitiveType(Type t) => (t.flags & TypeFlags.Primitive) == 0;
    private Type getTypeOfPropertyInTypes(List<Type> types, string name) => getUnionType(types.Select(t => getTypeOfPropertyInType(t, name)).ToList());
    private Type getTypeOfPropertyInType(Type t, string name)
    {
        t = getApparentType(t);
        var prop = (t.flags & TypeFlags.UnionOrIntersection) != 0 ? getPropertyOfUnionOrIntersectionType(t, name, false) : getPropertyOfObjectType(t, name);
        if (prop != null)
            return getTypeOfSymbol(prop);
        guardAbsentSignaturesAndIndexes(t, "getTypeOfPropertyInType: index signature");
        return undefinedType;
    }

    private bool isTypeSubsetOf(Type source, Type target)
    {
        return source == target || (source.flags & TypeFlags.Never) != 0 || (target.flags & TypeFlags.Union) != 0 && isTypeSubsetOfUnion(source, target);
    }

    private bool isTypeSubsetOfUnion(Type source, Type target)
    {
        if ((source.flags & TypeFlags.Union) != 0)
            return source.Types().All(t => target.Types().Contains(t));
        if ((source.flags & TypeFlags.Enum) != 0)
            throw unsupported("isTypeSubsetOfUnion: enum");
        return target.Types().Contains(source);
    }

    private Type getBestMatchingType(Type source, Type target, Func<Type, Type, Ternary> isRelatedTo)
    {
        var t = findMatchingDiscriminantType(source, target, isRelatedTo);
        if (t != null)
            return t;
        t = findMatchingTypeReferenceOrTypeAliasReference(source, target);
        if (t != null)
            return t;
        t = findBestTypeForObjectLiteral(source, target);
        if (t != null)
            return t;
        // Call/construct candidates are absent in the selected data-only model.
        guardAbsentSignaturesAndIndexes(source, "getBestMatchingType: invokable source");
        foreach (var part in target.Types())
            guardAbsentSignaturesAndIndexes(part, "getBestMatchingType: invokable target");
        return findMostOverlappyType(source, target);
    }

    private Type findMatchingTypeReferenceOrTypeAliasReference(Type source, Type target)
    {
        guardFiniteObject(source, "findMatchingTypeReferenceOrTypeAliasReference: reference/alias");
        foreach (var part in target.Types())
            guardFiniteObject(part, "findMatchingTypeReferenceOrTypeAliasReference: reference/alias");
        return null; // Both actual reference/alias match arms have been excluded by the guard.
    }

    private Type findBestTypeForObjectLiteral(Type source, Type target)
    {
        guardFiniteObject(source, "findBestTypeForObjectLiteral: arrays");
        foreach (var part in target.Types())
            guardFiniteObject(part, "findBestTypeForObjectLiteral: arrays");
        return null; // No selected type is array-like.
    }

    private Type findMostOverlappyType(Type source, Type target)
    {
        Type bestMatch = null;
        if ((source.flags & (TypeFlags.Primitive | TypeFlags.InstantiablePrimitive)) == 0)
        {
            int matchingCount = 0;
            foreach (var t in target.Types())
                if ((t.flags & (TypeFlags.Primitive | TypeFlags.InstantiablePrimitive)) == 0)
                {
                    var overlap = getIntersectionType([getIndexType(source), getIndexType(t)]);
                    if ((overlap.flags & TypeFlags.Index) != 0)
                        return t;
                    if (isUnitType(overlap) || (overlap.flags & TypeFlags.Union) != 0)
                    {
                        int length = (overlap.flags & TypeFlags.Union) != 0 ? overlap.Types().Count(isUnitType) : 1;
                        if (length >= matchingCount)
                        {
                            bestMatch = t;
                            matchingCount = length;
                        }
                    }
                }
        }

        return bestMatch;
    }

    private Symbol getUnmatchedProperty(Type source, Type target, bool requireOptionalProperties, bool matchDiscriminantProperties) => getUnmatchedPropertiesWorker(source, target, requireOptionalProperties, matchDiscriminantProperties, null);
    private List<Symbol> getUnmatchedProperties(Type source, Type target, bool requireOptionalProperties, bool matchDiscriminantProperties)
    {
        // Nullable list output models Go's pointer to a nil slice separately from no output pointer.
        List<Symbol> values = null;
        var output = new GoRef<List<Symbol>>(() => values, value => values = value);
        getUnmatchedPropertiesWorker(source, target, requireOptionalProperties, matchDiscriminantProperties, output);
        return output.Value;
    }

    private Symbol getUnmatchedPropertiesWorker(Type source, Type target, bool requireOptionalProperties, bool matchDiscriminantProperties, GoRef<List<Symbol>> propsOut)
    {
        foreach (var targetProp in getPropertiesOfType(target) ?? [])
        {
            if (targetProp.ValueDeclaration?.Name()?.Kind == Kind.PrivateIdentifier)
                throw unsupported("getUnmatchedPropertiesWorker: static private property");
            if (requireOptionalProperties || (targetProp.Flags & SymbolFlags.Optional) == 0 && (targetProp.CheckFlags & CheckFlags.Partial) == 0)
            {
                var sourceProp = getPropertyOfType(source, targetProp.Name);
                bool unmatched = sourceProp == null;
                if (!unmatched && matchDiscriminantProperties)
                {
                    var targetType = getTypeOfSymbol(targetProp);
                    if ((targetType.flags & TypeFlags.Unit) != 0)
                    {
                        var sourceType = getTypeOfSymbol(sourceProp);
                        unmatched = !((sourceType.flags & TypeFlags.Any) != 0 || getRegularTypeOfLiteralType(sourceType) == getRegularTypeOfLiteralType(targetType));
                    }
                }

                if (unmatched)
                {
                    if (propsOut == null)
                        return targetProp;
                    (propsOut.Value ??= []).Add(targetProp);
                }
            }
        }

        return null;
    }

    private bool shouldReportUnmatchedPropertyError(Type source, Type target)
    {
        guardAbsentSignaturesAndIndexes(source, "shouldReportUnmatchedPropertyError: signatures");
        // The source branch requires nonempty call/construct signatures, rejected above.
        return true;
    }

    private static List<Symbol> excludeProperties(List<Symbol> properties, HashSet<string> excludedProperties)
    {
        if ((excludedProperties?.Count ?? 0) == 0 || (properties?.Count ?? 0) == 0)
            return properties;
        List<Symbol> reduced = null;
        bool excluded = false;
        for (int i = 0; i < properties.Count; i++)
        {
            if (!excludedProperties.Contains(properties[i].Name))
            {
                if (excluded)
                    (reduced ??= []).Add(properties[i]);
            }
            else if (!excluded)
            {
                reduced = properties.GetRange(0, i);
                excluded = true;
            }
        }

        return excluded ? reduced : properties;
    }

    private sealed class Relater
    {
        private readonly Checker c;
        internal Relation relation;
        internal Node errorNode;
        internal ErrorChain errorChain;
        internal List<Diagnostic> relatedInfo;
        internal readonly List<CacheHashKey> maybeKeys = [];
        internal readonly HashSet<CacheHashKey> maybeKeysSet = [];
        internal readonly List<Type> sourceStack = [], targetStack = [];
        internal ExpandingFlags expandingFlags;
        internal bool overflow;
        internal int relationCount;
        internal Relater next;
        internal Relater(Checker checker) => c = checker;
        internal Ternary isRelatedToSimple(Type source, Type target) => isRelatedToEx(source, target, RecursionFlags.Both, false, null, IntersectionState.None);
        internal Ternary isRelatedToWorker(Type source, Type target, bool reportErrors) => isRelatedToEx(source, target, RecursionFlags.Both, reportErrors, null, IntersectionState.None);
        private Ternary isRelatedTo(Type source, Type target, RecursionFlags recursionFlags, bool reportErrors) => isRelatedToEx(source, target, recursionFlags, reportErrors, null, IntersectionState.None);
        internal Ternary isRelatedToEx(Type originalSource, Type originalTarget, RecursionFlags recursionFlags, bool reportErrors, Message headMessage, IntersectionState intersectionState)
        {
            if (originalSource == originalTarget)
                return Ternary.True;
            guardFiniteObject(originalSource, "isRelatedToEx: source generic/named/intersection");
            guardFiniteObject(originalTarget, "isRelatedToEx: target generic/named/intersection");
            if ((originalSource.flags & TypeFlags.Object) != 0 && (originalTarget.flags & TypeFlags.Primitive) != 0)
            {
                if (relation == c.comparableRelation && (originalTarget.flags & TypeFlags.Never) == 0 && c.isSimpleTypeRelatedTo(originalTarget, originalSource, relation) || c.isSimpleTypeRelatedTo(originalSource, originalTarget, relation, reportErrors ? reportError : null))
                    return Ternary.True;
                if (reportErrors)
                    reportErrorResults(originalSource, originalTarget, originalSource, originalTarget, headMessage);
                return Ternary.False;
            }

            var source = c.getNormalizedType(originalSource, false);
            var target = c.getNormalizedType(originalTarget, true);
            if (source == target)
                return Ternary.True;
            if (relation == c.identityRelation)
            {
                if (source.flags != target.flags)
                    return Ternary.False;
                if ((source.flags & TypeFlags.Singleton) != 0)
                    return Ternary.True;
                return recursiveTypeRelatedTo(source, target, false, IntersectionState.None, recursionFlags);
            }

            if ((source.flags & TypeFlags.TypeParameter) != 0)
                throw unsupported("isRelatedToEx: type parameter constraint");
            if ((source.flags & TypeFlags.DefinitelyNonNullable) != 0 && (target.flags & TypeFlags.Union) != 0)
            {
                var types = target.Types();
                Type candidate = null;
                if (types.Count == 2 && (types[0].flags & TypeFlags.Nullable) != 0)
                    candidate = types[1];
                else if (types.Count == 3 && (types[0].flags & TypeFlags.Nullable) != 0 && (types[1].flags & TypeFlags.Nullable) != 0)
                    candidate = types[2];
                if (candidate != null && (candidate.flags & TypeFlags.Nullable) == 0)
                {
                    target = c.getNormalizedType(candidate, true);
                    if (source == target)
                        return Ternary.True;
                }
            }

            if (relation == c.comparableRelation && (target.flags & TypeFlags.Never) == 0 && c.isSimpleTypeRelatedTo(target, source, relation) || c.isSimpleTypeRelatedTo(source, target, relation, reportErrors ? reportError : null))
                return Ternary.True;
            if (((source.flags | target.flags) & TypeFlags.StructuredOrInstantiable) != 0)
            {
                bool isPerformingExcessPropertyChecks = (intersectionState & IntersectionState.Target) == 0 && isObjectLiteralType(source) && (source.objectFlags & ObjectFlags.FreshLiteral) != 0;
                if (isPerformingExcessPropertyChecks && hasExcessProperties(source, target, reportErrors))
                {
                    if (reportErrors)
                        reportRelationError(headMessage, source, originalTarget.alias != null ? originalTarget : target);
                    return Ternary.False;
                }

                bool isPerformingCommonPropertyChecks = (relation != c.comparableRelation || isUnitType(source)) && (intersectionState & IntersectionState.Target) == 0 && (source.flags & (TypeFlags.Primitive | TypeFlags.Object | TypeFlags.Intersection)) != 0 && source != c.globalObjectType && (target.flags & (TypeFlags.Object | TypeFlags.Intersection)) != 0 && c.isWeakType(target) && ((c.getPropertiesOfType(source)?.Count ?? 0) > 0 || c.typeHasCallOrConstructSignatures(source));
                bool isComparingJsxAttributes = (source.objectFlags & ObjectFlags.JsxAttributes) != 0;
                if (isPerformingCommonPropertyChecks && !c.hasCommonProperties(source, target, isComparingJsxAttributes))
                {
                    if (reportErrors)
                    {
                        var sourceString = c.typeToStringByteString(originalSource.alias != null ? originalSource : source);
                        var targetString = c.typeToStringByteString(originalTarget.alias != null ? originalTarget : target);
                        c.guardAbsentSignaturesAndIndexes(source, "isRelatedToEx: did-you-mean-call signatures");
                        reportError(Diagnostics.Type_0_has_no_properties_in_common_with_type_1, sourceString, targetString);
                    }

                    return Ternary.False;
                }

                bool skipCaching = (source.flags & TypeFlags.Union) != 0 && source.Types().Count < 4 && (target.flags & TypeFlags.Union) == 0 || (target.flags & TypeFlags.Union) != 0 && target.Types().Count < 4 && (source.flags & TypeFlags.StructuredOrInstantiable) == 0;
                var result = skipCaching ? unionOrIntersectionRelatedTo(source, target, reportErrors, intersectionState) : recursiveTypeRelatedTo(source, target, reportErrors, intersectionState, recursionFlags);
                if (result != Ternary.False)
                    return result;
            }

            if (reportErrors)
                reportErrorResults(originalSource, originalTarget, source, target, headMessage);
            return Ternary.False;
        }

        private bool hasExcessProperties(Type source, Type target, bool reportErrors)
        {
            if (!isExcessPropertyCheckTarget(target) || !c.noImplicitAny && (target.objectFlags & ObjectFlags.JSLiteral) != 0)
                return false;
            bool isComparingJsxAttributes = (source.objectFlags & ObjectFlags.JsxAttributes) != 0;
            if ((relation == c.assignableRelation || relation == c.comparableRelation) && (c.isTypeSubsetOf(c.globalObjectType, target) || !isComparingJsxAttributes && c.isEmptyObjectType(target)))
                return false;
            var reducedTarget = target;
            List<Type> checkTypes = null;
            if ((target.flags & TypeFlags.Union) != 0)
            {
                reducedTarget = c.findMatchingDiscriminantType(source, target, isRelatedToSimple) ?? c.filterPrimitivesIfContainsNonPrimitive(target);
                checkTypes = reducedTarget.Distributed();
            }

            foreach (var prop in c.getPropertiesOfType(source) ?? [])
            {
                if (shouldCheckAsExcessProperty(prop, source.symbol))
                {
                    if (!c.isKnownProperty(reducedTarget, prop.Name, isComparingJsxAttributes))
                    {
                        if (reportErrors)
                        {
                            var errorTarget = c.filterType(reducedTarget, isExcessPropertyCheckTarget);
                            if (errorNode == null)
                                throw new InvalidOperationException("No errorNode in hasExcessProperties");
                            if (isComparingJsxAttributes)
                                throw unsupported("hasExcessProperties: JSX");
                            var objectLiteralDeclaration = source.symbol?.Declarations?.FirstOrDefault();
                            string suggestion = "";
                            if (prop.ValueDeclaration != null && Ast.IsObjectLiteralElement(prop.ValueDeclaration) && Ast.FindAncestor(prop.ValueDeclaration, d => d == objectLiteralDeclaration) != null && Ast.GetSourceFileOfNode(objectLiteralDeclaration) == Ast.GetSourceFileOfNode(errorNode))
                            {
                                var name = prop.ValueDeclaration.Name();
                                errorNode = name;
                                if (Ast.IsIdentifier(name))
                                    suggestion = c.getSuggestionForNonexistentProperty(name.Text(), errorTarget);
                            }

                            if (suggestion != "")
                                reportError(Diagnostics.Object_literal_may_only_specify_known_properties_but_0_does_not_exist_in_type_1_Did_you_mean_to_write_2, c.symbolToString(prop), c.typeToStringByteString(errorTarget), suggestion);
                            else
                                reportError(Diagnostics.Object_literal_may_only_specify_known_properties_and_0_does_not_exist_in_type_1, c.symbolToString(prop), c.typeToStringByteString(errorTarget));
                        }

                        return true;
                    }

                    if (checkTypes != null && isRelatedTo(c.getTypeOfSymbol(prop), c.getTypeOfPropertyInTypes(checkTypes, prop.Name), RecursionFlags.Both, reportErrors) == Ternary.False)
                    {
                        if (reportErrors)
                            reportError(Diagnostics.Types_of_property_0_are_incompatible, c.symbolToString(prop));
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool shouldCheckAsExcessProperty(Symbol prop, Symbol container) => prop.ValueDeclaration != null && container?.ValueDeclaration != null && prop.ValueDeclaration.Parent == container.ValueDeclaration;
        private Ternary unionOrIntersectionRelatedTo(Type source, Type target, bool reportErrors, IntersectionState intersectionState)
        {
            if ((source.flags & TypeFlags.Union) != 0)
            {
                if (source.AsUnionType().origin != null || (target.flags & TypeFlags.Union) != 0 && target.AsUnionType().origin != null)
                    throw unsupported("unionOrIntersectionRelatedTo: named/intersection union origins");
                if (relation == c.comparableRelation)
                    return someTypeRelatedToType(source, target, reportErrors && (source.flags & TypeFlags.Primitive) == 0, intersectionState);
                return eachTypeRelatedToType(source, target, reportErrors && (source.flags & TypeFlags.Primitive) == 0, intersectionState);
            }

            if ((target.flags & TypeFlags.Union) != 0)
                return typeRelatedToSomeType(c.getRegularTypeOfObjectLiteral(source), target, reportErrors && (source.flags & TypeFlags.Primitive) == 0 && (target.flags & TypeFlags.Primitive) == 0, intersectionState);
            throw unsupported("unionOrIntersectionRelatedTo: intersection");
        }

        private Ternary someTypeRelatedToType(Type source, Type target, bool reportErrors, IntersectionState intersectionState)
        {
            var types = source.Types();
            if ((source.flags & TypeFlags.Union) != 0 && types.Contains(target))
                return Ternary.True;
            for (int i = 0; i < types.Count; i++)
            {
                var related = isRelatedToEx(types[i], target, RecursionFlags.Source, reportErrors && i == types.Count - 1, null, intersectionState);
                if (related != Ternary.False)
                    return related;
            }

            return Ternary.False;
        }

        private Ternary eachTypeRelatedToType(Type source, Type target, bool reportErrors, IntersectionState intersectionState)
        {
            var result = Ternary.True;
            var sourceTypes = source.Types();
            var strippedTarget = getUndefinedStrippedTargetIfNeeded(source, target);
            var strippedTypes = (strippedTarget.flags & TypeFlags.Union) != 0 ? strippedTarget.Types() : null;
            for (int i = 0; i < sourceTypes.Count; i++)
            {
                if (strippedTypes != null && sourceTypes.Count >= strippedTypes.Count && sourceTypes.Count % strippedTypes.Count == 0)
                {
                    var related = isRelatedToEx(sourceTypes[i], strippedTypes[i % strippedTypes.Count], RecursionFlags.Both, false, null, intersectionState);
                    if (related != Ternary.False)
                    {
                        result &= related;
                        continue;
                    }
                }

                var next = isRelatedToEx(sourceTypes[i], target, RecursionFlags.Source, reportErrors, null, intersectionState);
                if (next == Ternary.False)
                    return Ternary.False;
                result &= next;
            }

            return result;
        }

        private Type getUndefinedStrippedTargetIfNeeded(Type source, Type target)
        {
            if ((source.flags & target.flags & TypeFlags.Union) != 0 && (source.Types()[0].flags & TypeFlags.Undefined) == 0 && (target.Types()[0].flags & TypeFlags.Undefined) != 0)
                return c.extractTypesOfKind(target, ~TypeFlags.Undefined);
            return target;
        }

        private Ternary typeRelatedToSomeType(Type source, Type target, bool reportErrors, IntersectionState intersectionState)
        {
            var targetTypes = target.Types();
            if ((target.flags & TypeFlags.Union) != 0)
            {
                if (targetTypes.Contains(source))
                    return Ternary.True;
                if (relation != c.comparableRelation && (target.objectFlags & ObjectFlags.PrimitiveUnion) != 0 && (source.flags & TypeFlags.EnumLiteral) == 0 && ((source.flags & (TypeFlags.StringLiteral | TypeFlags.BooleanLiteral | TypeFlags.BigIntLiteral)) != 0 || (relation == c.subtypeRelation || relation == c.strictSubtypeRelation) && (source.flags & TypeFlags.NumberLiteral) != 0))
                {
                    var literal = source.AsLiteralType();
                    var alternate = source == literal.regularType ? literal.freshType : literal.regularType;
                    Type primitive = null;
                    if ((source.flags & TypeFlags.StringLiteral) != 0)
                        primitive = c.stringType;
                    else if ((source.flags & TypeFlags.NumberLiteral) != 0)
                        primitive = c.numberType;
                    else if ((source.flags & TypeFlags.BigIntLiteral) != 0)
                        primitive = c.bigintType;
                    if (primitive != null && targetTypes.Contains(primitive) || alternate != null && targetTypes.Contains(alternate))
                        return Ternary.True;
                    return Ternary.False;
                }

                var match = c.getMatchingUnionConstituentForType(target, source);
                if (match != null)
                {
                    var related = isRelatedToEx(source, match, RecursionFlags.Target, false, null, intersectionState);
                    if (related != Ternary.False)
                        return related;
                }
            }

            foreach (var t in targetTypes)
            {
                var related = isRelatedToEx(source, t, RecursionFlags.Target, false, null, intersectionState);
                if (related != Ternary.False)
                    return related;
            }

            if (reportErrors)
            {
                var best = c.getBestMatchingType(source, target, isRelatedToSimple);
                if (best != null)
                    isRelatedToEx(source, best, RecursionFlags.Target, true, null, intersectionState);
            }

            return Ternary.False;
        }

        private Ternary eachTypeRelatedToSomeType(Type source, Type target)
        {
            var result = Ternary.True;
            foreach (var part in source.Types())
            {
                var related = typeRelatedToSomeType(part, target, false, IntersectionState.None);
                if (related == Ternary.False)
                    return Ternary.False;
                result &= related;
            }

            return result;
        }

        private Ternary recursiveTypeRelatedTo(Type source, Type target, bool reportErrors, IntersectionState intersectionState, RecursionFlags recursionFlags)
        {
            if (overflow)
                return Ternary.False;
            var(id, constrained) = getRelationKey(source, target, intersectionState, relation == c.identityRelation, false);
            var entry = relation.get(id);
            if (entry != RelationComparisonResult.None)
            {
                if (!(reportErrors && (entry & RelationComparisonResult.Failed) != 0 && (entry & RelationComparisonResult.Overflow) == 0))
                {
                    c.reliabilityFlags |= entry & RelationComparisonResult.ReportsMask;
                    if (reportErrors && (entry & RelationComparisonResult.Overflow) != 0)
                        reportError(Diagnostics.Excessive_complexity_comparing_types_0_and_1, c.typeToStringByteString(source), c.typeToStringByteString(target));
                    return (entry & RelationComparisonResult.Succeeded) != 0 ? Ternary.True : Ternary.False;
                }
            }

            if (relationCount <= 0)
            {
                overflow = true;
                return Ternary.False;
            }

            if (maybeKeysSet.Contains(id))
                return Ternary.Maybe;
            if (constrained)
            {
                var(broadestEquivalentId, _) = getRelationKey(source, target, intersectionState, relation == c.identityRelation, true);
                if (maybeKeysSet.Contains(broadestEquivalentId))
                    return Ternary.Maybe;
            }

            if (sourceStack.Count == 100 || targetStack.Count == 100)
                return Ternary.Maybe;
            int maybeStart = maybeKeys.Count;
            maybeKeys.Add(id);
            maybeKeysSet.Add(id);
            var saveExpandingFlags = expandingFlags;
            if ((recursionFlags & RecursionFlags.Source) != 0)
            {
                sourceStack.Add(source);
                if ((expandingFlags & ExpandingFlags.Source) == 0 && c.isDeeplyNestedType(source, sourceStack, 3))
                    expandingFlags |= ExpandingFlags.Source;
            }

            if ((recursionFlags & RecursionFlags.Target) != 0)
            {
                targetStack.Add(target);
                if ((expandingFlags & ExpandingFlags.Target) == 0 && c.isDeeplyNestedType(target, targetStack, 3))
                    expandingFlags |= ExpandingFlags.Target;
            }

            var saveReliabilityFlags = c.reliabilityFlags;
            c.reliabilityFlags = 0;
            var result = expandingFlags == ExpandingFlags.Both ? Ternary.Maybe : structuredTypeRelatedTo(source, target, reportErrors, intersectionState);
            var propagatingVarianceFlags = c.reliabilityFlags;
            c.reliabilityFlags |= saveReliabilityFlags;
            if ((recursionFlags & RecursionFlags.Source) != 0)
                sourceStack.RemoveAt(sourceStack.Count - 1);
            if ((recursionFlags & RecursionFlags.Target) != 0)
                targetStack.RemoveAt(targetStack.Count - 1);
            expandingFlags = saveExpandingFlags;
            if (result != Ternary.False)
            {
                if (result == Ternary.True || sourceStack.Count == 0 && targetStack.Count == 0)
                    resetMaybeStack(maybeStart, propagatingVarianceFlags, result == Ternary.True || result == Ternary.Maybe);
            }
            else
            {
                relation.set(id, RelationComparisonResult.Failed | propagatingVarianceFlags);
                relationCount--;
                resetMaybeStack(maybeStart, propagatingVarianceFlags, false);
            }

            return result;
        }

        private void resetMaybeStack(int maybeStart, RelationComparisonResult flags, bool markAllAsSucceeded)
        {
            for (int i = maybeStart; i < maybeKeys.Count; i++)
            {
                maybeKeysSet.Remove(maybeKeys[i]);
                if (markAllAsSucceeded)
                {
                    relation.set(maybeKeys[i], RelationComparisonResult.Succeeded | flags);
                    relationCount--;
                }
            }

            maybeKeys.RemoveRange(maybeStart, maybeKeys.Count - maybeStart);
        }

        private readonly record struct errorState(ErrorChain errorChain, List<Diagnostic> relatedInfo);
        // Go copies slice length during speculation. A managed snapshot preserves that prefix
        // when subsequent Add operations reuse the same list instance.
        private errorState getErrorState() => new(errorChain, relatedInfo?.ToList());
        private void restoreErrorState(errorState state)
        {
            errorChain = state.errorChain;
            relatedInfo = state.relatedInfo;
        }

        private Ternary structuredTypeRelatedTo(Type source, Type target, bool reportErrors, IntersectionState intersectionState)
        {
            var save = getErrorState();
            var result = structuredTypeRelatedToWorker(source, target, reportErrors, intersectionState);
            if (((source.flags | target.flags) & (TypeFlags.Intersection | TypeFlags.TypeParameter)) != 0)
                throw unsupported("structuredTypeRelatedTo: combined constraints/intersection extra checks");
            if (result != Ternary.False)
                restoreErrorState(save);
            return result;
        }

        private Ternary structuredTypeRelatedToWorker(Type source, Type target, bool reportErrors, IntersectionState intersectionState)
        {
            guardFiniteObject(source, "structuredTypeRelatedToWorker: generic/variance/mapped source");
            guardFiniteObject(target, "structuredTypeRelatedToWorker: generic/variance/mapped target");
            var save = getErrorState();
            if (relation == c.identityRelation)
            {
                if ((source.flags & TypeFlags.UnionOrIntersection) != 0)
                {
                    var related = eachTypeRelatedToSomeType(source, target);
                    if (related != Ternary.False)
                        related &= eachTypeRelatedToSomeType(target, source);
                    return related;
                }

                if ((source.flags & TypeFlags.Object) == 0)
                    return Ternary.False;
            }
            else if (((source.flags | target.flags) & TypeFlags.UnionOrIntersection) != 0)
            {
                var related = unionOrIntersectionRelatedTo(source, target, reportErrors, intersectionState);
                if (related != Ternary.False)
                    return related;
                if (!((source.flags & TypeFlags.Object) != 0 && (target.flags & TypeFlags.Union) != 0))
                    return Ternary.False;
            }

            bool sourceIsPrimitive = (source.flags & TypeFlags.Primitive) != 0;
            if (relation != c.identityRelation)
                source = c.getApparentType(source);
            if ((relation == c.subtypeRelation || relation == c.strictSubtypeRelation) && c.isEmptyObjectType(target) && (target.objectFlags & ObjectFlags.FreshLiteral) != 0 && !c.isEmptyObjectType(source))
                return Ternary.False;
            if ((source.flags & TypeFlags.Object) != 0 && (target.flags & TypeFlags.Object) != 0)
            {
                bool reportStructuralErrors = reportErrors && errorChain == save.errorChain && !sourceIsPrimitive;
                var result = propertiesRelatedTo(source, target, reportStructuralErrors, null, false, intersectionState);
                if (result != Ternary.False)
                {
                    result &= signaturesRelatedTo(source, target, SignatureKind.Call, reportStructuralErrors, intersectionState);
                    if (result != Ternary.False)
                        result &= signaturesRelatedTo(source, target, SignatureKind.Construct, reportStructuralErrors, intersectionState);
                    if (result != Ternary.False)
                        result &= indexSignaturesRelatedTo(source, target, sourceIsPrimitive, reportStructuralErrors, intersectionState);
                }

                if (result != Ternary.False)
                    return result;
            }

            if ((source.flags & TypeFlags.Object) != 0 && (target.flags & TypeFlags.Union) != 0)
            {
                var objectOnly = c.extractTypesOfKind(target, TypeFlags.Object | TypeFlags.Intersection | TypeFlags.Substitution);
                if ((objectOnly.flags & TypeFlags.Union) != 0)
                    throw unsupported("structuredTypeRelatedToWorker: multi-object discriminated relation");
            }

            return Ternary.False;
        }

        private Ternary signaturesRelatedTo(Type source, Type target, SignatureKind kind, bool reportErrors, IntersectionState intersectionState)
        {
            c.guardAbsentSignaturesAndIndexes(source, "signaturesRelatedTo: nonempty signature/index");
            c.guardAbsentSignaturesAndIndexes(target, "signaturesRelatedTo: nonempty signature/index");
            // The selected kind's signature loop is empty in this admitted model.
            return Ternary.True;
        }

        private Ternary indexSignaturesRelatedTo(Type source, Type target, bool sourceIsPrimitive, bool reportErrors, IntersectionState intersectionState)
        {
            c.guardAbsentSignaturesAndIndexes(source, "indexSignaturesRelatedTo: nonempty signature/index");
            c.guardAbsentSignaturesAndIndexes(target, "indexSignaturesRelatedTo: nonempty signature/index");
            return Ternary.True;
        }

        private Ternary propertiesRelatedTo(Type source, Type target, bool reportErrors, HashSet<string> excludedProperties, bool optionalsOnly, IntersectionState intersectionState)
        {
            if (relation == c.identityRelation)
                return propertiesIdenticalTo(source, target, excludedProperties);
            var result = Ternary.True;
            guardFiniteObject(source, "propertiesRelatedTo: arrays/tuples");
            guardFiniteObject(target, "propertiesRelatedTo: arrays/tuples");
            bool requireOptionalProperties = (relation == c.subtypeRelation || relation == c.strictSubtypeRelation) && !isObjectLiteralType(source);
            var unmatched = c.getUnmatchedProperty(source, target, requireOptionalProperties, false);
            if (unmatched != null)
            {
                if (reportErrors && c.shouldReportUnmatchedPropertyError(source, target))
                    reportUnmatchedProperty(source, target, unmatched, requireOptionalProperties);
                return Ternary.False;
            }

            if (isObjectLiteralType(target))
                foreach (var sourceProp in excludeProperties(c.getPropertiesOfType(source), excludedProperties) ?? [])
                    if (c.getPropertyOfObjectType(target, sourceProp.Name) == null)
                    {
                        if (reportErrors)
                            reportError(Diagnostics.Property_0_does_not_exist_on_type_1, c.symbolToString(sourceProp), c.typeToStringByteString(target));
                        return Ternary.False;
                    }

            foreach (var targetProp in excludeProperties(c.getPropertiesOfType(target), excludedProperties) ?? [])
            {
                var name = targetProp.Name;
                if ((targetProp.Flags & SymbolFlags.Prototype) == 0 && (!optionalsOnly || (targetProp.Flags & SymbolFlags.Optional) != 0))
                {
                    var sourceProp = c.getPropertyOfType(source, name);
                    if (sourceProp != null && sourceProp != targetProp)
                    {
                        var related = propertyRelatedTo(source, target, sourceProp, targetProp, c.getNonMissingTypeOfSymbol, reportErrors, intersectionState, relation == c.comparableRelation);
                        if (related == Ternary.False)
                            return Ternary.False;
                        result &= related;
                    }
                }
            }

            return result;
        }

        private Ternary propertyRelatedTo(Type source, Type target, Symbol sourceProp, Symbol targetProp, Func<Symbol, Type> getTypeOfSourceProperty, bool reportErrors, IntersectionState intersectionState, bool skipOptional)
        {
            var sourceFlags = getDeclarationModifierFlagsFromSymbol(sourceProp);
            var targetFlags = getDeclarationModifierFlagsFromSymbol(targetProp);
            if (((sourceFlags | targetFlags) & ModifierFlags.NonPublicAccessibilityModifier) != 0)
                throw unsupported("propertyRelatedTo: private/protected class property");
            if (relation == c.strictSubtypeRelation && c.isReadonlySymbol(sourceProp) && !c.isReadonlySymbol(targetProp))
                return Ternary.False;
            var related = isPropertySymbolTypeRelated(sourceProp, targetProp, getTypeOfSourceProperty, reportErrors, intersectionState);
            if (related == Ternary.False)
            {
                if (reportErrors)
                    reportError(Diagnostics.Types_of_property_0_are_incompatible, c.symbolToString(targetProp));
                return Ternary.False;
            }

            if (!skipOptional && (sourceProp.Flags & SymbolFlags.Optional) != 0 && (targetProp.Flags & SymbolFlags.ClassMember) != 0 && (targetProp.Flags & SymbolFlags.Optional) == 0)
            {
                if (reportErrors)
                    reportError(Diagnostics.Property_0_is_optional_in_type_1_but_required_in_type_2, c.symbolToString(targetProp), c.typeToStringByteString(source), c.typeToStringByteString(target));
                return Ternary.False;
            }

            return related;
        }

        private Ternary isPropertySymbolTypeRelated(Symbol sourceProp, Symbol targetProp, Func<Symbol, Type> getTypeOfSourceProperty, bool reportErrors, IntersectionState intersectionState)
        {
            bool targetIsOptional = c.strictNullChecks && (targetProp.CheckFlags & CheckFlags.Partial) != 0;
            var effectiveTarget = c.addOptionalityEx(c.getNonMissingTypeOfSymbol(targetProp), false, targetIsOptional);
            if ((effectiveTarget.flags & (relation == c.strictSubtypeRelation ? TypeFlags.Any : TypeFlags.AnyOrUnknown)) != 0)
                return Ternary.True;
            return isRelatedToEx(getTypeOfSourceProperty(sourceProp), effectiveTarget, RecursionFlags.Both, reportErrors, null, intersectionState);
        }

        private void reportUnmatchedProperty(Type source, Type target, Symbol unmatchedProperty, bool requireOptionalProperties)
        {
            if (unmatchedProperty.ValueDeclaration?.Name()?.Kind == Kind.PrivateIdentifier)
                throw unsupported("reportUnmatchedProperty: private identifier");
            var props = c.getUnmatchedProperties(source, target, requireOptionalProperties, false);
            var(sourceType, targetType) = c.getTypeNamesForErrorDisplay(source, target);
            if (props.Count == 1)
            {
                var name = c.symbolToString(unmatchedProperty);
                reportError(Diagnostics.Property_0_is_missing_in_type_1_but_required_in_type_2, name, sourceType, targetType);
                if ((unmatchedProperty.Declarations?.Count ?? 0) != 0)
                    (relatedInfo ??= []).Add(NewDiagnosticForNode(unmatchedProperty.Declarations[0], Diagnostics.X_0_is_declared_here, name));
            }
            else if (tryElaborateArrayLikeErrors(source, target, false))
            {
                if (props.Count > 5)
                    reportError(Diagnostics.Type_0_is_missing_the_following_properties_from_type_1_Colon_2_and_3_more, sourceType, targetType, string.Join(", ", props.Take(4).Select(c.symbolToString)), props.Count - 4);
                else
                    reportError(Diagnostics.Type_0_is_missing_the_following_properties_from_type_1_Colon_2, sourceType, targetType, string.Join(", ", props.Select(c.symbolToString)));
            }
        }

        private bool tryElaborateArrayLikeErrors(Type source, Type target, bool reportErrors)
        {
            guardFiniteObject(source, "tryElaborateArrayLikeErrors: readonly array/tuple");
            guardFiniteObject(target, "tryElaborateArrayLikeErrors: array/tuple");
            return true; // Actual final arm, after excluding every array/tuple arm.
        }

        private void tryElaborateErrorsForPrimitivesAndObjects(Type source, Type target)
        {
            if (source == c.globalStringType && target == c.stringType || source == c.globalNumberType && target == c.numberType || source == c.globalBooleanType && target == c.booleanType || source == c.getGlobalESSymbolType() && target == c.esSymbolType)
                reportError(Diagnostics.X_0_is_a_primitive_but_1_is_a_wrapper_object_Prefer_using_0_when_possible, c.typeToStringByteString(target), c.typeToStringByteString(source));
        }

        private Ternary propertiesIdenticalTo(Type source, Type target, HashSet<string> excludedProperties)
        {
            if ((source.flags & target.flags & TypeFlags.Object) == 0)
                return Ternary.False;
            var sourceProps = excludeProperties(c.getPropertiesOfObjectType(source), excludedProperties);
            var targetProps = excludeProperties(c.getPropertiesOfObjectType(target), excludedProperties);
            if ((sourceProps?.Count ?? 0) != (targetProps?.Count ?? 0))
                return Ternary.False;
            var result = Ternary.True;
            foreach (var sourceProp in sourceProps ?? [])
            {
                var targetProp = c.getPropertyOfObjectType(target, sourceProp.Name);
                if (targetProp == null)
                    return Ternary.False;
                var related = c.compareProperties(sourceProp, targetProp, isRelatedToSimple);
                if (related == Ternary.False)
                    return Ternary.False;
                result &= related;
            }

            return result;
        }

        private void reportErrorResults(Type originalSource, Type originalTarget, Type source, Type target, Message headMessage)
        {
            guardFiniteObject(originalSource, "reportErrorResults: named base subtype");
            guardFiniteObject(originalTarget, "reportErrorResults: named base subtype");
            // getSingleBaseForNonAugmentingSubtype requires ClassOrInterface (guarded above).
            if (originalSource.alias != null)
                source = originalSource;
            if (originalTarget.alias != null)
                target = originalTarget;
            if ((source.flags & target.flags & TypeFlags.Object) != 0)
                tryElaborateArrayLikeErrors(source, target, true);
            if ((source.flags & TypeFlags.Object) != 0 && (target.flags & TypeFlags.Primitive) != 0)
                tryElaborateErrorsForPrimitivesAndObjects(source, target);
            else if (source.symbol != null && (source.flags & TypeFlags.Object) != 0 && c.globalObjectType == source)
                reportError(Diagnostics.The_Object_type_is_assignable_to_very_few_other_types_Did_you_mean_to_use_the_any_type_instead);
            if ((source.objectFlags & ObjectFlags.JsxAttributes) != 0 || (originalTarget.flags & TypeFlags.Intersection) != 0)
                throw unsupported("reportErrorResults: JSX/intersection/type parameter");
            reportRelationError(headMessage, source, target);
        }

        private void reportRelationError(Message message, Type source, Type target)
        {
            var(sourceType, targetType) = c.getTypeNamesForErrorDisplay(source, target);
            var generalizedSource = source;
            var generalizedSourceType = sourceType;
            if ((target.flags & TypeFlags.Never) == 0 && isLiteralType(source) && !c.typeCouldHaveTopLevelSingletonTypes(target))
            {
                generalizedSource = c.getBaseTypeOfLiteralType(source);
                generalizedSourceType = c.getTypeNameForErrorDisplay(generalizedSource);
            }

            if ((target.flags & (TypeFlags.IndexedAccess | TypeFlags.TypeParameter)) != 0)
                throw unsupported("reportRelationError: indexed/type parameter");
            if (c.exactOptionalPropertyTypes)
                throw unsupported("reportRelationError: exact optional property option");
            if (message == null)
            {
                if (relation == c.comparableRelation)
                    message = Diagnostics.Type_0_is_not_comparable_to_type_1;
                else if (sourceType == targetType)
                    message = Diagnostics.Type_0_is_not_assignable_to_type_1_Two_different_types_with_this_name_exist_but_they_are_unrelated;
                else
                {
                    if ((source.flags & TypeFlags.StringLiteral) != 0 && (target.flags & TypeFlags.Union) != 0)
                    {
                        var suggested = c.getSuggestedTypeForNonexistentStringLiteralType(source, target);
                        if (suggested != null)
                        {
                            reportError(Diagnostics.Type_0_is_not_assignable_to_type_1_Did_you_mean_2, generalizedSourceType, targetType, c.typeToStringByteString(suggested));
                            return;
                        }
                    }

                    message = Diagnostics.Type_0_is_not_assignable_to_type_1;
                }
            }

            var head = getChainMessage(0);
            if (head == Diagnostics.Object_literal_may_only_specify_known_properties_and_0_does_not_exist_in_type_1 || head == Diagnostics.Object_literal_may_only_specify_known_properties_but_0_does_not_exist_in_type_1_Did_you_mean_to_write_2)
                return;
            if ((head == Diagnostics.Excessive_complexity_comparing_types_0_and_1 || head == Diagnostics.The_type_0_is_readonly_and_cannot_be_assigned_to_the_mutable_type_1) && chainArgsMatch(generalizedSourceType, targetType))
                return;
            if (head == Diagnostics.Property_0_is_missing_in_type_1_but_required_in_type_2 && !isConversionOrInterfaceImplementationMessage(message) && chainArgsMatch(null, generalizedSourceType, targetType))
                return;
            if ((head == Diagnostics.Type_0_is_missing_the_following_properties_from_type_1_Colon_2_and_3_more || head == Diagnostics.Type_0_is_missing_the_following_properties_from_type_1_Colon_2) && !isConversionOrInterfaceImplementationMessage(message) && chainArgsMatch(generalizedSourceType, targetType))
                return;
            reportError(message, generalizedSourceType, targetType);
        }

        internal void reportError(Message message, params object[] args)
        {
            if (message == Diagnostics.Types_of_property_0_are_incompatible)
            {
                var head = getChainMessage(0);
                if (head == Diagnostics.Object_literal_may_only_specify_known_properties_and_0_does_not_exist_in_type_1 || head == Diagnostics.Object_literal_may_only_specify_known_properties_but_0_does_not_exist_in_type_1_Did_you_mean_to_write_2)
                    return;
                string arg = "";
                var next = getChainMessage(1);
                if (next == Diagnostics.Call_signatures_with_no_arguments_have_incompatible_return_types_0_and_1)
                    arg = getPropertyNameArg(args[0]) + "()";
                else if (next == Diagnostics.Construct_signatures_with_no_arguments_have_incompatible_return_types_0_and_1)
                    arg = "new " + getPropertyNameArg(args[0]) + "()";
                else if (next == Diagnostics.Call_signature_return_types_0_and_1_are_incompatible)
                    arg = getPropertyNameArg(args[0]) + "(...)";
                else if (next == Diagnostics.Construct_signature_return_types_0_and_1_are_incompatible)
                    arg = "new " + getPropertyNameArg(args[0]) + "(...)";
                if (arg != "")
                {
                    message = Diagnostics.The_types_returned_by_0_are_incompatible_between_these_types;
                    args[0] = arg;
                    errorChain = errorChain.next.next;
                }

                next = getChainMessage(1);
                if (next == Diagnostics.Types_of_property_0_are_incompatible || next == Diagnostics.The_types_of_0_are_incompatible_between_these_types || next == Diagnostics.The_types_returned_by_0_are_incompatible_between_these_types)
                {
                    var dotted = addToDottedName(getPropertyNameArg(args[0]), getPropertyNameArg(errorChain.next.args[0]));
                    errorChain = errorChain.next.next;
                    if (message == Diagnostics.Types_of_property_0_are_incompatible)
                        message = Diagnostics.The_types_of_0_are_incompatible_between_these_types;
                    reportError(message, dotted);
                    return;
                }
            }

            errorChain = new ErrorChain
            {
                next = errorChain,
                message = message,
                args = args
            };
        }

        private Message getChainMessage(int index)
        {
            var e = errorChain;
            while (e != null)
            {
                if (index == 0)
                    return e.message;
                e = e.next;
                index--;
            }

            return null;
        }

        private bool chainArgsMatch(params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i] != null && !Equals(args[i], errorChain.args[i]))
                    return false;
            return true;
        }
    }

    private static string addToDottedName(string head, string tail)
    {
        if (head.StartsWith("new ", StringComparison.Ordinal))
            head = "(" + head + ")";
        int pos = 0;
        while (true)
        {
            if (tail.AsSpan(pos).StartsWith("(", StringComparison.Ordinal))
                pos++;
            else if (tail.AsSpan(pos).StartsWith("new ", StringComparison.Ordinal))
                pos += 4;
            else
                break;
        }

        var prefix = tail[..pos];
        var suffix = tail[pos..];
        return prefix + head + (suffix.StartsWith("[", StringComparison.Ordinal) ? "" : ".") + suffix;
    }

    private static string getPropertyNameArg(object arg)
    {
        var s = (string)arg;
        return s.Length != 0 && s[0] is '"' or '\'' or '`' ? "[" + s + "]" : s;
    }

    private static bool isConversionOrInterfaceImplementationMessage(Message message) => message == Diagnostics.Class_0_incorrectly_implements_interface_1 || message == Diagnostics.Class_0_incorrectly_implements_class_1_Did_you_mean_to_extend_1_and_inherit_its_members_as_a_subclass || message == Diagnostics.Conversion_of_type_0_to_type_1_may_be_a_mistake_because_neither_type_sufficiently_overlaps_with_the_other_If_this_was_intentional_convert_the_expression_to_unknown_first || message == Diagnostics.Its_instance_type_0_is_not_a_valid_JSX_element || message == Diagnostics.Its_return_type_0_is_not_a_valid_JSX_element || message == Diagnostics.Its_element_type_0_is_not_a_valid_JSX_element;
    private static int chainDepth(ErrorChain chain)
    {
        int depth = 0;
        while (chain != null)
        {
            depth++;
            chain = chain.next;
        }

        return depth;
    }

    private (string, string) getTypeNamesForErrorDisplay(Type left, Type right)
    {
        var leftStr = symbolValueDeclarationIsContextSensitive(left.symbol) ? typeToString(left, left.symbol.ValueDeclaration) : typeToStringByteString(left);
        var rightStr = symbolValueDeclarationIsContextSensitive(right.symbol) ? typeToString(right, right.symbol.ValueDeclaration) : typeToStringByteString(right);
        if (leftStr == rightStr)
        {
            leftStr = getTypeNameForErrorDisplay(left);
            rightStr = getTypeNameForErrorDisplay(right);
        }

        return (leftStr, rightStr);
    }

    private bool symbolValueDeclarationIsContextSensitive(Symbol symbol) => symbol != null && symbol.ValueDeclaration != null && Ast.IsExpression(symbol.ValueDeclaration) && !isContextSensitive(symbol.ValueDeclaration);
    private string getTypeNameForErrorDisplay(Type t) => typeToStringEx(t, null, true);
    private bool typeCouldHaveTopLevelSingletonTypes(Type t)
    {
        if ((t.flags & TypeFlags.Boolean) != 0)
            return false;
        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
            return t.Types().Any(typeCouldHaveTopLevelSingletonTypes);
        if ((t.flags & TypeFlags.Instantiable) != 0)
            throw unsupported("typeCouldHaveTopLevelSingletonTypes: constrained types");
        return isUnitType(t) || (t.flags & (TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0;
    }

    private bool isOrHasGenericConditional(Type t) => (t.flags & TypeFlags.Conditional) != 0 || (t.flags & TypeFlags.Intersection) != 0 && t.Types().Any(isOrHasGenericConditional);
    private bool elaborateError(Node node, Type source, Type target, Relation relation, Message headMessage, List<Diagnostic> diagnosticOutput)
    {
        if (node == null || isOrHasGenericConditional(target))
            return false;
        // NoCheck is fixed false in this admitted API.
        if (elaborateDidYouMeanToCallOrConstruct(node, source, target, relation, SignatureKind.Construct, headMessage, diagnosticOutput) || elaborateDidYouMeanToCallOrConstruct(node, source, target, relation, SignatureKind.Call, headMessage, diagnosticOutput))
            return true;
        switch (node.Kind)
        {
            case Kind.ParenthesizedExpression:
                return elaborateError(node.Expression(), source, target, relation, headMessage, diagnosticOutput);
            case Kind.ObjectLiteralExpression:
                return elaborateObjectLiteral(node, source, target, relation, diagnosticOutput);
            case Kind.AsExpression:
            case Kind.JsxExpression:
            case Kind.BinaryExpression:
            case Kind.ArrayLiteralExpression:
            case Kind.ArrowFunction:
            case Kind.JsxAttributes:
                throw unsupported("elaborateError: assertions/writes/arrays/callable/JSX");
        }

        return false;
    }

    private bool elaborateDidYouMeanToCallOrConstruct(Node node, Type source, Type target, Relation relation, SignatureKind kind, Message headMessage, List<Diagnostic> diagnosticOutput)
    {
        // Preserve the original getSignaturesOfType call even for empty slots:
        // resolving union members is an observable cache side effect.
        var signatures = getSignaturesOfType(source, kind);
        if (signatures?.Any(s => throw unsupported("elaborateDidYouMeanToCallOrConstruct: callable return type")) == true)
            throw unsupported("elaborateDidYouMeanToCallOrConstruct: call/construct suggestion");
        return false;
    }

    private bool elaborateObjectLiteral(Node node, Type source, Type target, Relation relation, List<Diagnostic> diagnosticOutput)
    {
        if ((target.flags & (TypeFlags.Primitive | TypeFlags.Never)) != 0)
            return false;
        bool reportedError = false;
        foreach (var prop in node.Properties() ?? [])
        {
            if (prop.Kind == Kind.SpreadAssignment)
                throw unsupported("elaborateObjectLiteral: spread");
            var nameType = getLiteralTypeFromProperty(getSymbolOfDeclaration(prop), TypeFlags.StringOrNumberLiteralOrUnique, false);
            if (nameType == null || (nameType.flags & TypeFlags.Never) != 0)
                continue;
            switch (prop.Kind)
            {
                case Kind.ShorthandPropertyAssignment:
                    reportedError = elaborateElement(source, target, relation, prop.Name(), null, nameType, null, null, diagnosticOutput) || reportedError;
                    break;
                case Kind.PropertyAssignment:
                    if (prop.Name().Kind == Kind.ComputedPropertyName)
                        throw unsupported("elaborateObjectLiteral: computed name");
                    reportedError = elaborateElement(source, target, relation, prop.Name(), prop.Initializer(), nameType, null, null, diagnosticOutput) || reportedError;
                    break;
                case Kind.MethodDeclaration:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                    throw unsupported("elaborateObjectLiteral: callable/accessor");
            }
        }

        return reportedError;
    }

    private bool elaborateElement(Type source, Type target, Relation relation, Node prop, Node next, Type nameType, Message errorMessage, Func<Node, Diagnostic> diagnosticFactory, List<Diagnostic> diagnosticOutput)
    {
        var targetPropType = getBestMatchIndexedAccessTypeOrUndefined(source, target, nameType);
        if (targetPropType == null || (targetPropType.flags & TypeFlags.IndexedAccess) != 0)
            return false;
        var sourcePropType = getIndexedAccessTypeOrUndefined(source, nameType, AccessFlags.None, null, null);
        if (sourcePropType == null || checkTypeRelatedTo(sourcePropType, targetPropType, relation, null))
            return false;
        if (next != null && elaborateError(next, sourcePropType, targetPropType, relation, null, diagnosticOutput))
            return true;
        var diags = new List<Diagnostic>();
        var specificSource = sourcePropType;
        if (next != null)
            specificSource = checkExpressionForMutableLocationWithContextualType(next, sourcePropType);
        if (diagnosticFactory != null)
            diags.Add(diagnosticFactory(prop));
        else if (exactOptionalPropertyTypes)
            throw unsupported("elaborateElement: exact optional property option");
        else
        {
            var propName = getPropertyNameFromIndex(nameType, null);
            bool targetIsOptional = ((getPropertyOfType(target, propName) ?? unknownSymbol).Flags & SymbolFlags.Optional) != 0;
            bool sourceIsOptional = ((getPropertyOfType(source, propName) ?? unknownSymbol).Flags & SymbolFlags.Optional) != 0;
            targetPropType = removeMissingType(targetPropType, targetIsOptional);
            sourcePropType = removeMissingType(sourcePropType, targetIsOptional && sourceIsOptional);
            bool result = checkTypeRelatedToEx(specificSource, targetPropType, relation, prop, errorMessage, diags);
            if (result && specificSource != sourcePropType)
                checkTypeRelatedToEx(sourcePropType, targetPropType, relation, prop, errorMessage, diags);
        }

        if (diags.Count == 0)
            return false;
        var diagnostic = diags[0];
        string propertyName = "";
        Symbol targetProp = null;
        if (isTypeUsableAsPropertyName(nameType))
        {
            propertyName = getPropertyNameFromType(nameType);
            targetProp = getPropertyOfType(target, propertyName);
        }

        if (targetProp == null)
            guardAbsentSignaturesAndIndexes(target, "elaborateElement: index signature");
        if ((targetProp?.Declarations?.Count ?? 0) != 0 || (target.symbol?.Declarations?.Count ?? 0) != 0)
        {
            var targetNode = (targetProp?.Declarations?.Count ?? 0) != 0 ? targetProp.Declarations[0] : target.symbol.Declarations[0];
            if (propertyName == "" || (nameType.flags & TypeFlags.UniqueESSymbol) != 0)
                propertyName = typeToStringByteString(nameType);
            // The only source file is explicitly noLib, hence never a default library file.
            diagnostic.AddRelatedInfo(NewDiagnosticForNode(targetNode, Diagnostics.The_expected_type_comes_from_property_0_which_is_declared_here_on_type_1, propertyName, typeToStringByteString(target)));
        }

        reportDiagnostic(diagnostic, diagnosticOutput);
        return true;
    }

    private Type getBestMatchIndexedAccessTypeOrUndefined(Type source, Type target, Type nameType)
    {
        var idx = getIndexedAccessTypeOrUndefined(target, nameType, AccessFlags.None, null, null);
        if (idx != null)
            return idx;
        if ((target.flags & TypeFlags.Union) != 0)
        {
            var best = getBestMatchingType(source, target, compareTypesAssignableSimple);
            if (best != null)
                return getIndexedAccessTypeOrUndefined(best, nameType, AccessFlags.None, null, null);
        }

        return null;
    }

    private Type checkExpressionForMutableLocationWithContextualType(Node next, Type sourcePropType)
    {
        pushContextualType(next, sourcePropType, false);
        var result = checkExpressionForMutableLocation(next, CheckMode.Contextual);
        popContextualType();
        return result;
    }
}
