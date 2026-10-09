// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/relater.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected scalar, data-object/composite and ordinary callable relations; generic, rest and construct paths remain guarded.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    [Flags]
    internal enum SignatureCheckMode : uint
    {
        None = 0,
        BivariantCallback = 1,
        StrictCallback = 2,
        IgnoreReturnTypes = 4,
        StrictArity = 8,
        StrictTopSignature = 16,
        Callback = BivariantCallback | StrictCallback
    }

    [Flags]
    internal enum MinArgumentCountFlags : uint
    {
        None = 0,
        StrongArityForUntypedJS = 1,
        VoidIsNonOptional = 2
    }

    // relater.go:161/165.
    private bool isTypeComparableTo(Type source, Type target) => isTypeRelatedTo(source, target, comparableRelation);
    private bool areTypesComparable(Type type1, Type type2) => isTypeComparableTo(type1, type2) || isTypeComparableTo(type2, type1);
    // relater.go:4995. The ordinary object/base/constraint branches are retained.
    private bool isTypeDerivedFrom(Type source, Type target)
    {
        if ((source.flags & TypeFlags.Union) != 0)
            return source.Types().All(t => isTypeDerivedFrom(t, target));
        if ((target.flags & TypeFlags.Union) != 0)
            return target.Types().Any(t => isTypeDerivedFrom(source, t));
        if ((source.flags & TypeFlags.Intersection) != 0)
            return source.Types().Any(t => isTypeDerivedFrom(t, target));
        if ((source.flags & TypeFlags.InstantiableNonPrimitive) != 0)
            return isTypeDerivedFrom(getBaseConstraintOfType(source) ?? unknownType, target);
        if (IsEmptyAnonymousObjectType(target))
            return (source.flags & (TypeFlags.Object | TypeFlags.NonPrimitive)) != 0;
        if (target == globalObjectType)
            return (source.flags & (TypeFlags.Object | TypeFlags.NonPrimitive)) != 0 && !IsEmptyAnonymousObjectType(source);
        if (target == globalFunctionType)
            return (source.flags & TypeFlags.Object) != 0 && isFunctionObjectType(source);
        if (hasBaseType(source, getTargetType(target)))
            return true;
        if (isArrayType(target))
            throw unsupported("isTypeDerivedFrom: array ancestry");
        return false;
    }

    // relater.go:2559.
    private static string visibilityToString(ModifierFlags flags) => flags == ModifierFlags.Private ? "private" : flags == ModifierFlags.Protected ? "protected" : "public";
    // relater.go:1829. Even a nonrest invocation creates a tuple of remaining
    // parameters upstream; this helper is reached only from excluded rest paths.
    private Type getRestTypeAtPosition(Signature source, int pos, bool readOnly) => throw unsupported("getRestTypeAtPosition: tuple/rest creation");
    private delegate void ErrorReporter(Message message, params object[] args);
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

    private readonly symbolArenaLinkStore<VarianceLinks> varianceLinks = new();
    private readonly List<(Symbol symbol, List<Type> typeParameters)> varianceStack = new();
    // relater.go:1317/1334. Written type parameters, arrays and tuples are outside
    // admission; the original zero-parameter cache/stack arm remains selected.
    private List<VarianceFlags> getVariances(Type t)
    {
        if ((t.objectFlags & ObjectFlags.Tuple) != 0)
            throw unsupported("getVariances: tuple");
        return getVariancesWorker(t.symbol, t.AsInterfaceType().TypeParameters());
    }

    private List<VarianceFlags> getAliasVariances(Symbol symbol) => getVariancesWorker(symbol, typeAliasLinks.Get(symbol).typeParameters);
    private List<VarianceFlags> getVariancesWorker(Symbol symbol, List<Type> typeParameters)
    {
        var links = varianceLinks.Get(symbol);
        if (links.variances == null)
        {
            if (typeParameters is { Count: > 0 })
                throw unsupported("getVariancesWorker: written generic parameters");
            int stackIndex = getVarianceStackIndex(symbol);
            if (stackIndex < 0)
            {
                int saveResolutionStart = resolutionStart;
                if (varianceStack.Count == 0)
                    resolutionStart = typeResolutions.Count;
                varianceStack.Add((symbol, typeParameters));
                var variances = new List<VarianceFlags>();
                // The admitted target has zero declared type parameters, so the
                // upstream variance-measurement loop has no iterations.
                if (links.variances is not { Count: > 0 })
                    links.variances = variances;
                varianceStack.RemoveAt(varianceStack.Count - 1);
                if (varianceStack.Count == 0)
                    resolutionStart = saveResolutionStart;
            }
            else
            {
                int minIndex = stackIndex;
                for (int i = stackIndex + 1; i < varianceStack.Count; i++)
                    if (compareSymbols(varianceStack[i].symbol, varianceStack[minIndex].symbol) < 0)
                        minIndex = i;
                if (minIndex > stackIndex)
                    throw unsupported("getVariancesWorker: generic recursive restart");
                if (links.variances is not { Count: > 0 })
                    links.variances = new();
            }
        }

        return links.variances;
    }

    private int getVarianceStackIndex(Symbol symbol)
    {
        for (int i = 0; i < varianceStack.Count; i++)
            if (varianceStack[i].symbol == symbol)
                return i;
        return -1;
    }

    private readonly Relation assignableRelation = new(), identityRelation = new(), subtypeRelation = new(), strictSubtypeRelation = new(), comparableRelation = new();
    private bool isMarkerType(Type t) => markerTypes.Contains(t);
    private bool checkTypeAssignableTo(Type source, Type target, Node errorNode, Message headMessage) => checkTypeRelatedToEx(source, target, assignableRelation, errorNode, headMessage, null);
    private bool checkTypeAssignableToEx(Type source, Type target, Node errorNode, Message headMessage, List<Diagnostic> output) => checkTypeRelatedToEx(source, target, assignableRelation, errorNode, headMessage, output);
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
        if ((t.flags & (TypeFlags.Index | TypeFlags.TemplateLiteral | TypeFlags.StringMapping | TypeFlags.Substitution | TypeFlags.IndexedAccess | TypeFlags.Conditional | TypeFlags.EnumLike)) != 0 || (t.objectFlags & (ObjectFlags.Class | ObjectFlags.Tuple | ObjectFlags.Mapped | ObjectFlags.ReverseMapped | ObjectFlags.JsxAttributes)) != 0 || (t.flags & TypeFlags.TypeParameter) != 0 && !t.AsTypeParameter().isThisType)
            throw unsupported(branch);
    }

    private bool isWeakType(Type t)
    {
        guardFiniteObject(t, "isWeakType: unselected type payload");
        if ((t.flags & TypeFlags.Object) != 0)
        {
            var resolved = resolveStructuredTypeMembers(t);
            return GoRuntime.Len(resolved.signatures) == 0 && (resolved.indexInfos?.Count ?? 0) == 0 && (resolved.properties?.Count ?? 0) > 0 && resolved.properties.All(p => (p.Flags & SymbolFlags.Optional) != 0);
        }

        if ((t.flags & TypeFlags.Intersection) != 0)
            return t.Types().All(isWeakType);
        return false;
    }

    // relater.go:4655. Reverse-mapped/JS sources are excluded before this arm.
    private bool isObjectTypeWithInferableIndex(Type t)
    {
        if ((t.flags & TypeFlags.Intersection) != 0)
            return t.Types().All(isObjectTypeWithInferableIndex);
        guardFiniteObject(t, "isObjectTypeWithInferableIndex: JS/reverse-mapped");
        return t.symbol != null && (t.symbol.Flags & (SymbolFlags.ObjectLiteral | SymbolFlags.TypeLiteral | SymbolFlags.Enum | SymbolFlags.ValueModule)) != 0 && (t.symbol.Flags & SymbolFlags.Class) == 0 && !typeHasCallOrConstructSignatures(t) || (t.objectFlags & (ObjectFlags.JSLiteral | ObjectFlags.ObjectRestType)) != 0;
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
            if (getPropertyOfObjectType(targetType, name) != null || getApplicableIndexInfoForName(targetType, name) != null || isLateBoundName(name) && getIndexInfoOfType(targetType, stringType) != null)
                return true;
        }

        if (isExcessPropertyCheckTarget(targetType) && (targetType.flags & TypeFlags.UnionOrIntersection) != 0)
            foreach (var t in targetType.Types())
                if (isKnownProperty(t, name, isComparingJsxAttributes))
                    return true;
        return false;
    }

    private static bool isExcessPropertyCheckTarget(Type type)
    {
        guardFiniteObject(type, "isExcessPropertyCheckTarget: substitution/intersection");
        return (type.flags & TypeFlags.Object) != 0 && (type.objectFlags & ObjectFlags.ObjectLiteralPatternWithComputedProperties) == 0 || (type.flags & TypeFlags.NonPrimitive) != 0 || (type.flags & TypeFlags.Union) != 0 && type.Types().Any(isExcessPropertyCheckTarget) || (type.flags & TypeFlags.Intersection) != 0 && type.Types().All(isExcessPropertyCheckTarget);
    }

    private readonly record struct RecursionId(object value);
    private bool isDeeplyNestedType(Type t, List<Type> stack, int maxDepth)
    {
        if (stack.Count >= maxDepth)
        {
            var target = getRecursionIdentityTarget(t);
            if ((target.flags & TypeFlags.Intersection) != 0)
                return target.Types().Any(part => isDeeplyNestedType(part, stack, maxDepth));
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

    private static bool hasMatchingRecursionIdentity(Type t, RecursionId identity)
    {
        var target = getRecursionIdentityTarget(t);
        return (target.flags & TypeFlags.Intersection) != 0 ? target.Types().Any(part => hasMatchingRecursionIdentity(part, identity)) : getRecursionIdentityFromTarget(target) == identity;
    }

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
        {
            if ((t.objectFlags & ObjectFlags.Reference) != 0 && t.AsTypeReference().node != null)
                return new(t.AsTypeReference().node);
            if (t.symbol != null && !((t.objectFlags & ObjectFlags.Anonymous) != 0 && (t.symbol.Flags & SymbolFlags.Class) != 0) && (t.objectFlags & ObjectFlags.FromTypeNode) == 0)
                return new(t.symbol);
        }

        if ((t.flags & TypeFlags.TypeParameter) != 0 && t.symbol != null)
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
                    if ((prop.CheckFlags & CheckFlags.NonUniformAndLiteral) == CheckFlags.NonUniformAndLiteral && !isGenericType(getTypeOfSymbol(prop)))
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
        var indexInfo = getApplicableIndexInfoForName(t, name);
        if (indexInfo != null)
            return indexInfo.valueType;
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

    // relater.go:2315. Preserve combined constraints for admitted implicit-this
    // variables and finite intersections; source generic operators are guarded.
    private Type getEffectiveConstraintOfIntersection(List<Type> types, bool targetIsUnion)
    {
        List<Type> constraints = null;
        bool hasDisjointDomainType = false;
        foreach (var type in types)
        {
            if ((type.flags & TypeFlags.Instantiable) != 0)
            {
                var constraint = getConstraintOfType(type);
                while (constraint != null && (constraint.flags & (TypeFlags.TypeParameter | TypeFlags.Index | TypeFlags.Conditional)) != 0)
                    constraint = getConstraintOfType(constraint);
                if (constraint != null)
                {
                    (constraints ??= new()).Add(constraint);
                    if (targetIsUnion)
                        constraints.Add(type);
                }
            }
            else if ((type.flags & TypeFlags.DisjointDomains) != 0 || IsEmptyAnonymousObjectType(type))
                hasDisjointDomainType = true;
        }

        if (constraints != null && (targetIsUnion || hasDisjointDomainType))
        {
            if (hasDisjointDomainType)
                foreach (var type in types)
                    if ((type.flags & TypeFlags.DisjointDomains) != 0 || IsEmptyAnonymousObjectType(type))
                        constraints.Add(type);
            return getNormalizedType(getIntersectionTypeEx(constraints, IntersectionFlags.NoConstraintReduction, null), false);
        }

        return null;
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
        t = findBestTypeForInvokable(source, target, SignatureKind.Call);
        if (t != null)
            return t;
        t = findBestTypeForInvokable(source, target, SignatureKind.Construct);
        if (t != null)
            return t;
        return findMostOverlappyType(source, target);
    }

    private Type findMatchingTypeReferenceOrTypeAliasReference(Type source, Type target)
    {
        var sourceObjectFlags = source.objectFlags;
        if ((sourceObjectFlags & (ObjectFlags.Reference | ObjectFlags.Anonymous)) != 0 && (target.flags & TypeFlags.Union) != 0)
            foreach (var part in target.Types())
                if ((part.flags & TypeFlags.Object) != 0)
                {
                    var overlapFlags = sourceObjectFlags & part.objectFlags;
                    if ((overlapFlags & ObjectFlags.Reference) != 0 && source.Target() == part.Target())
                        return part;
                    if ((overlapFlags & ObjectFlags.Anonymous) != 0 && source.alias != null && part.alias != null && source.alias.Symbol() == part.alias.Symbol())
                        return part;
                }

        return null;
    }

    // relater.go:909. Preserve signature resolution even for absent data-only slots.
    private Type findBestTypeForInvokable(Type source, Type target, SignatureKind kind)
    {
        if ((getSignaturesOfType(source, kind)?.Count ?? 0) != 0)
            return target.Types().FirstOrDefault(t => (getSignaturesOfType(t, kind)?.Count ?? 0) != 0);
        return null;
    }

    private Type findBestTypeForObjectLiteral(Type source, Type target)
    {
        guardFiniteObject(source, "findBestTypeForObjectLiteral: unselected type payload");
        foreach (var part in target.Types())
            guardFiniteObject(part, "findBestTypeForObjectLiteral: unselected type payload");
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
        var calls = getSignaturesOfStructuredType(source, SignatureKind.Call);
        var constructs = getSignaturesOfStructuredType(source, SignatureKind.Construct);
        var properties = getPropertiesOfObjectType(source);
        if (((calls?.Count ?? 0) != 0 || (constructs?.Count ?? 0) != 0) && (properties?.Count ?? 0) == 0)
        {
            if (((getSignaturesOfType(target, SignatureKind.Call)?.Count ?? 0) != 0 && (calls?.Count ?? 0) != 0) || ((getSignaturesOfType(target, SignatureKind.Construct)?.Count ?? 0) != 0 && (constructs?.Count ?? 0) != 0))
                return true;
            return false;
        }

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

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1487:1.
    private bool isSignatureAssignableTo(Signature source, Signature target, bool ignoreReturnTypes)
    {
        return (this.compareSignaturesRelated(source, target, Core.IfElse(ignoreReturnTypes, SignatureCheckMode.IgnoreReturnTypes, SignatureCheckMode.None), false, null, this.compareTypesAssignableWorker, null) != Ternary.False);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1491:1.
    private Ternary compareSignaturesRelated(Signature source, Signature target, SignatureCheckMode checkMode, bool reportErrors, ErrorReporter errorReporter, TypeComparer compareTypes, TypeMapper reportUnreliableMarkers)
    {
        if ((source == target))
        {
            return Ternary.True;
        }

        if ((!((((checkMode & SignatureCheckMode.StrictTopSignature) != 0) && this.isTopSignature(source))) && this.isTopSignature(target)))
        {
            return Ternary.True;
        }

        if (((((checkMode & SignatureCheckMode.StrictTopSignature) != 0) && this.isTopSignature(source)) && !this.isTopSignature(target)))
        {
            return Ternary.False;
        }

        var targetCount = this.getParameterCount(target);
        bool sourceHasMoreParameters = default;
        if (!this.hasEffectiveRestParameter(target))
        {
            if (((checkMode & SignatureCheckMode.StrictArity) != 0))
            {
                sourceHasMoreParameters = (this.hasEffectiveRestParameter(source) || (this.getParameterCount(source) > targetCount));
            }
            else
            {
                sourceHasMoreParameters = (this.getMinArgumentCount(source) > targetCount);
            }
        }

        if (sourceHasMoreParameters)
        {
            if ((reportErrors && (((checkMode & SignatureCheckMode.StrictArity) == 0))))
            {
                errorReporter(Diagnostics.Target_signature_provides_too_few_arguments_Expected_0_or_more_but_got_1, this.getMinArgumentCount(source), targetCount);
            }

            return Ternary.False;
        }

        if (((GoRuntime.Len(source.typeParameters) != 0) && !ReferenceEquals(source.typeParameters, target.typeParameters)))
        {
            throw unsupported("compareSignaturesRelated: generic signature context");
        }

        var sourceCount = this.getParameterCount(source);
        var sourceRestType = this.getNonArrayRestType(source);
        var targetRestType = this.getNonArrayRestType(target);
        if (((sourceRestType != null) || (targetRestType != null)))
        {
            this.instantiateType(Core.IfElse((sourceRestType != null), sourceRestType, targetRestType), reportUnreliableMarkers);
        }

        var kind = Kind.Unknown;
        if ((target.declaration != null))
        {
            kind = target.declaration.Kind;
        }

        var strictVariance = ((((((checkMode & SignatureCheckMode.Callback) == 0) && this.strictFunctionTypes) && (kind != Kind.MethodDeclaration)) && (kind != Kind.MethodSignature)) && (kind != Kind.Constructor));
        var result = Ternary.True;
        var sourceThisType = this.getThisTypeOfSignature(source);
        if (((sourceThisType != null) && (sourceThisType != this.voidType)))
        {
            var targetThisType = this.getThisTypeOfSignature(target);
            if ((targetThisType != null))
            {
                Ternary related = default;
                if (!strictVariance)
                {
                    related = compareTypes(sourceThisType, targetThisType, false);
                }

                if ((related == Ternary.False))
                {
                    related = compareTypes(targetThisType, sourceThisType, reportErrors);
                }

                if ((related == Ternary.False))
                {
                    if (reportErrors)
                    {
                        errorReporter(Diagnostics.The_this_types_of_each_signature_are_incompatible);
                    }

                    return Ternary.False;
                }

                result &= related;
            }
        }

        int paramCount = default;
        if (((sourceRestType != null) || (targetRestType != null)))
        {
            paramCount = Math.Min(sourceCount, targetCount);
        }
        else
        {
            paramCount = Math.Max(sourceCount, targetCount);
        }

        int restIndex = default;
        if (((sourceRestType != null) || (targetRestType != null)))
        {
            restIndex = (paramCount - 1);
        }
        else
        {
            restIndex = -1;
        }

        {
            foreach (var i in GoRuntime.RangeKeys(paramCount))
            {
                Type sourceType = default;
                if ((i == restIndex))
                {
                    sourceType = this.getRestOrAnyTypeAtPosition(source, i);
                }
                else
                {
                    sourceType = this.tryGetTypeAtPosition(source, i);
                }

                Type targetType = default;
                if ((i == restIndex))
                {
                    targetType = this.getRestOrAnyTypeAtPosition(target, i);
                }
                else
                {
                    targetType = this.tryGetTypeAtPosition(target, i);
                }

                if ((((sourceType != null) && (targetType != null)) && (((sourceType != targetType) || ((checkMode & SignatureCheckMode.StrictArity) != 0)))))
                {
                    Signature sourceSig = default;
                    Signature targetSig = default;
                    if ((((checkMode & SignatureCheckMode.Callback) == 0) && !this.isInstantiatedGenericParameter(source, i)))
                    {
                        sourceSig = this.getSingleCallSignature(this.GetNonNullableType(sourceType));
                    }

                    if ((((checkMode & SignatureCheckMode.Callback) == 0) && !this.isInstantiatedGenericParameter(target, i)))
                    {
                        targetSig = this.getSingleCallSignature(this.GetNonNullableType(targetType));
                    }

                    var callbacks = (((((sourceSig != null) && (targetSig != null)) && (this.getTypePredicateOfSignature(sourceSig) == null)) && (this.getTypePredicateOfSignature(targetSig) == null)) && (this.getTypeFacts(sourceType, TypeFacts.IsUndefinedOrNull) == this.getTypeFacts(targetType, TypeFacts.IsUndefinedOrNull)));
                    Ternary related2 = default;
                    if (callbacks)
                    {
                        related2 = this.compareSignaturesRelated(targetSig, sourceSig, ((checkMode & SignatureCheckMode.StrictArity) | Core.IfElse(strictVariance, SignatureCheckMode.StrictCallback, SignatureCheckMode.BivariantCallback)), reportErrors, errorReporter, compareTypes, reportUnreliableMarkers);
                    }
                    else
                    {
                        if ((((checkMode & SignatureCheckMode.Callback) == 0) && !strictVariance))
                        {
                            related2 = compareTypes(sourceType, targetType, false);
                        }

                        if ((related2 == Ternary.False))
                        {
                            related2 = compareTypes(targetType, sourceType, reportErrors);
                        }
                    }

                    if ((((((related2 != Ternary.False) && ((checkMode & SignatureCheckMode.StrictArity) != 0)) && (i >= this.getMinArgumentCount(source))) && (i < this.getMinArgumentCount(target))) && (compareTypes(sourceType, targetType, false) != Ternary.False)))
                    {
                        related2 = Ternary.False;
                    }

                    if ((related2 == Ternary.False))
                    {
                        if (reportErrors)
                        {
                            errorReporter(Diagnostics.Types_of_parameters_0_and_1_are_incompatible, this.getParameterNameAtPosition(source, i), this.getParameterNameAtPosition(target, i));
                        }

                        return Ternary.False;
                    }

                    result &= related2;
                }
            }
        }

        if (((checkMode & SignatureCheckMode.IgnoreReturnTypes) == 0))
        {
            var targetReturnType = this.getNonCircularReturnTypeOfSignature(target);
            if (((targetReturnType == this.voidType) || (targetReturnType == this.anyType)))
            {
                return result;
            }

            var sourceReturnType = this.getNonCircularReturnTypeOfSignature(source);
            var targetTypePredicate = this.getTypePredicateOfSignature(target);
            if ((targetTypePredicate != null))
            {
                var sourceTypePredicate = this.getTypePredicateOfSignature(source);
                if ((sourceTypePredicate != null))
                {
                    result &= this.compareTypePredicateRelatedTo(sourceTypePredicate, targetTypePredicate, reportErrors, errorReporter, compareTypes);
                }
                else
                {
                    if (((targetTypePredicate.kind == TypePredicateKind.Identifier) || (targetTypePredicate.kind == TypePredicateKind.This)))
                    {
                        if (reportErrors)
                        {
                            errorReporter(Diagnostics.Signature_0_must_be_a_type_predicate, this.signatureToString(source));
                        }

                        return Ternary.False;
                    }
                }
            }
            else
            {
                Ternary related3 = default;
                if (((checkMode & SignatureCheckMode.BivariantCallback) != 0))
                {
                    related3 = compareTypes(targetReturnType, sourceReturnType, false);
                }

                if ((related3 == Ternary.False))
                {
                    related3 = compareTypes(sourceReturnType, targetReturnType, reportErrors);
                }

                result &= related3;
                if (((result == Ternary.False) && reportErrors))
                {
                    Message message = default;
                    if (((GoRuntime.Len(source.parameters) == 0) && (GoRuntime.Len(target.parameters) == 0)))
                    {
                        message = Core.IfElse(((source.flags & SignatureFlags.Construct) != 0), Diagnostics.Construct_signatures_with_no_arguments_have_incompatible_return_types_0_and_1, Diagnostics.Call_signatures_with_no_arguments_have_incompatible_return_types_0_and_1);
                    }
                    else
                    {
                        message = Core.IfElse(((source.flags & SignatureFlags.Construct) != 0), Diagnostics.Construct_signature_return_types_0_and_1_are_incompatible, Diagnostics.Call_signature_return_types_0_and_1_are_incompatible);
                    }

                    errorReporter(message, this.typeToStringByteString(sourceReturnType), this.typeToStringByteString(targetReturnType));
                }
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1675:1.
    private Ternary compareTypePredicateRelatedTo(TypePredicate source, TypePredicate target, bool reportErrors, ErrorReporter errorReporter, TypeComparer compareTypes)
    {
        if ((source.kind != target.kind))
        {
            if (reportErrors)
            {
                errorReporter(Diagnostics.A_this_based_type_guard_is_not_compatible_with_a_parameter_based_type_guard);
                errorReporter(Diagnostics.Type_predicate_0_is_not_assignable_to_1, this.typePredicateToString(source), this.typePredicateToString(target));
            }

            return Ternary.False;
        }

        if (((source.kind == TypePredicateKind.Identifier) || (source.kind == TypePredicateKind.AssertsIdentifier)))
        {
            if ((source.parameterIndex != target.parameterIndex))
            {
                if (reportErrors)
                {
                    errorReporter(Diagnostics.Parameter_0_is_not_in_the_same_position_as_parameter_1, source.parameterName, target.parameterName);
                    errorReporter(Diagnostics.Type_predicate_0_is_not_assignable_to_1, this.typePredicateToString(source), this.typePredicateToString(target));
                }

                return Ternary.False;
            }
        }

        Ternary related = default;
        switch (true)
        {
            case true when (source.t == target.t):
            {
                related = Ternary.True;
                break;
            }

            case true when ((source.t != null) && (target.t != null)):
            {
                related = compareTypes(source.t, target.t, reportErrors);
                break;
            }

            default:
            {
                related = Ternary.False;
                break;
            }
        }

        if (((related == Ternary.False) && reportErrors))
        {
            errorReporter(Diagnostics.Type_predicate_0_is_not_assignable_to_1, this.typePredicateToString(source), this.typePredicateToString(target));
        }

        return related;
    }

    // Returns true if `s` is `(...args: A) => R` where `A` is `any`, `any[]`, `never`, or `never[]`, and `R` is `any` or `unknown`.
    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1708:1.
    private bool isTopSignature(Signature s)
    {
        if ((GoRuntime.Len(s.typeParameters) == 0) && (s.thisParameter == null || IsTypeAny(getTypeOfParameter(s.thisParameter))) && GoRuntime.Len(s.parameters) == 1 && signatureHasRestParameter(s))
            throw unsupported("isTopSignature: rest/array signature");
        return false;
    }

    // Return the number of parameters in a signature. The rest parameter, if present, counts as one
    // parameter. For example, the parameter count of (x: number, y: number, ...z: string[]) is 3 and
    // the parameter count of (x: number, ...args: [number, ...string[], boolean])) is also 3. In the
    // latter example, the effective rest type is [...string[], boolean].
    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1726:1.
    private int getParameterCount(Signature signature)
    {
        var length = GoRuntime.Len(signature.parameters);
        if (signatureHasRestParameter(signature))
            throw unsupported("getParameterCount: rest/tuple signature");
        return length;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1737:1.
    private int getMinArgumentCount(Signature signature)
    {
        return this.getMinArgumentCountEx(signature, MinArgumentCountFlags.None);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1741:1.
    private int getMinArgumentCountEx(Signature signature, MinArgumentCountFlags flags)
    {
        var strongArityForUntypedJS = (flags & MinArgumentCountFlags.StrongArityForUntypedJS);
        var voidIsNonOptional = (flags & MinArgumentCountFlags.VoidIsNonOptional);
        if (((voidIsNonOptional != 0) || (signature.resolvedMinArgumentCount == -1)))
        {
            var minArgumentCount = -1;
            if (signatureHasRestParameter(signature))
                throw unsupported("getMinArgumentCountEx: rest/tuple signature");
            if ((minArgumentCount == -1))
            {
                if (((strongArityForUntypedJS == 0) && ((signature.flags & SignatureFlags.IsUntypedSignatureInJSFile) != 0)))
                {
                    return 0;
                }

                minArgumentCount = (int)(signature.minArgumentCount);
            }

            if ((voidIsNonOptional != 0))
            {
                return minArgumentCount;
            }

            {
                var i = (minArgumentCount - 1);
                for (; (i >= 0); i--)
                {
                    var t = this.getTypeAtPosition(signature, i);
                    if (!someType(t, (t2) =>
                    {
                        return ((t2.flags & TypeFlags.Void) != 0);
                    }))
                    {
                        break;
                    }

                    minArgumentCount = i;
                }
            }

            signature.resolvedMinArgumentCount = (int)(minArgumentCount);
        }

        return (int)(signature.resolvedMinArgumentCount);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1782:1.
    private bool hasEffectiveRestParameter(Signature signature)
    {
        if (signatureHasRestParameter(signature))
            throw unsupported("hasEffectiveRestParameter: rest/tuple signature");
        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1790:1.
    private Type getTypeAtPosition(Signature signature, int pos)
    {
        var t = this.tryGetTypeAtPosition(signature, pos);
        if ((t != null))
        {
            return t;
        }

        return this.anyType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1798:1.
    private Type tryGetTypeAtPosition(Signature signature, int pos)
    {
        var paramCount = GoRuntime.Len(signature.parameters) - Core.IfElse(signatureHasRestParameter(signature), 1, 0);
        if (pos < paramCount)
            return getTypeOfParameter(signature.parameters[pos]);
        if (signatureHasRestParameter(signature))
            throw unsupported("tryGetTypeAtPosition: rest/tuple signature");
        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2049:1.
    internal TypePredicate getTypePredicateOfSignature(Signature sig)
    {
        if ((sig.resolvedTypePredicate == null))
        {
            switch (true)
            {
                case true when (sig.target != null):
                {
                    var targetTypePredicate = this.getTypePredicateOfSignature(sig.target);
                    if ((targetTypePredicate != null))
                    {
                        sig.resolvedTypePredicate = this.instantiateTypePredicate(targetTypePredicate, sig.mapper);
                    }

                    break;
                }

                case true when (sig.composite != null):
                {
                    sig.resolvedTypePredicate = this.getUnionOrIntersectionTypePredicate(sig.composite.signatures, sig.composite.isUnion);
                    break;
                }

                default:
                {
                    if ((sig.declaration != null))
                    {
                        var typeNode = sig.declaration.Type();
                        switch (true)
                        {
                            case true when (typeNode != null):
                            {
                                if (Ast.IsTypePredicateNode(typeNode))
                                {
                                    throw unsupported("getTypePredicateOfSignature: written type predicate");
                                }

                                break;
                            }

                            case true when ((Ast.IsFunctionLikeDeclaration(sig.declaration) && (((sig.resolvedReturnType == null) || ((sig.resolvedReturnType.flags & TypeFlags.Boolean) != 0)))) && (this.getParameterCount(sig) > 0)):
                            {
                                sig.resolvedTypePredicate = this.noTypePredicate;
                                sig.resolvedTypePredicate = this.getTypePredicateFromBody(sig.declaration);
                                break;
                            }
                        }
                    }

                    break;
                }
            }

            if ((sig.resolvedTypePredicate == null))
            {
                sig.resolvedTypePredicate = this.noTypePredicate;
            }
        }

        if ((sig.resolvedTypePredicate == this.noTypePredicate))
        {
            return null;
        }

        return sig.resolvedTypePredicate;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2083:1.
    private TypePredicate getUnionOrIntersectionTypePredicate(List<Signature> signatures, bool isUnion)
    {
        TypePredicate last = default;
        List<Type> types = default;
        {
            foreach (var sig in GoRuntime.RangeValues(signatures))
            {
                var pred = this.getTypePredicateOfSignature(sig);
                if ((pred != null))
                {
                    if ((((pred.kind != TypePredicateKind.This) && (pred.kind != TypePredicateKind.Identifier)) || ((last != null) && !this.typePredicateKindsMatch(last, pred))))
                    {
                        return null;
                    }

                    last = pred;
                    types = GoRuntime.Append(types, pred.t);
                }
                else
                {
                    Type returnType = default;
                    if (isUnion)
                    {
                        returnType = this.getReturnTypeOfSignature(sig);
                    }

                    if (((returnType != this.falseType) && (returnType != this.regularFalseType)))
                    {
                        return null;
                    }
                }
            }
        }

        if ((last == null))
        {
            return null;
        }

        var compositeType = this.getUnionOrIntersectionType(types, isUnion, UnionReduction.Literal);
        return this.newTypePredicate(last.kind, last.parameterName, last.parameterIndex, compositeType);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2187:1.
    private Signature findMatchingSignature(List<Signature> signatureList, Signature signature, bool partialMatch, bool ignoreThisTypes, bool ignoreReturnTypes)
    {
        Func<Type, Type, Ternary> compareTypes = partialMatch ? this.compareTypesSubtypeOf : this.compareTypesIdentical;
        {
            foreach (var s in GoRuntime.RangeValues(signatureList))
            {
                if ((this.compareSignaturesIdentical(s, signature, partialMatch, ignoreThisTypes, ignoreReturnTypes, compareTypes) != 0))
                {
                    return s;
                }
            }
        }

        return null;
    }

    // *
    // * See signatureRelatedTo, compareSignaturesIdentical
    //
    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2200:1.
    private Ternary compareSignaturesIdentical(Signature source, Signature target, bool partialMatch, bool ignoreThisTypes, bool ignoreReturnTypes, Func<Type, Type, Ternary> compareTypes)
    {
        if ((source == target))
        {
            return Ternary.True;
        }

        if (!this.isMatchingSignature(source, target, partialMatch))
        {
            return Ternary.False;
        }

        if ((GoRuntime.Len(source.typeParameters) != GoRuntime.Len(target.typeParameters)))
        {
            return Ternary.False;
        }

        if ((GoRuntime.Len(target.typeParameters) != 0))
        {
            throw unsupported("compareSignaturesIdentical: generic signature");
        }

        var result = Ternary.True;
        if (!ignoreThisTypes)
        {
            var sourceThisType = this.getThisTypeOfSignature(source);
            if ((sourceThisType != null))
            {
                var targetThisType = this.getThisTypeOfSignature(target);
                if ((targetThisType != null))
                {
                    var related = compareTypes(sourceThisType, targetThisType);
                    if ((related == Ternary.False))
                    {
                        return Ternary.False;
                    }

                    result &= related;
                }
            }
        }

        {
            foreach (var i2 in GoRuntime.RangeKeys(this.getParameterCount(target)))
            {
                var s3 = this.getTypeAtPosition(source, i2);
                var t3 = this.getTypeAtPosition(target, i2);
                var related2 = compareTypes(t3, s3);
                if ((related2 == Ternary.False))
                {
                    return Ternary.False;
                }

                result &= related2;
            }
        }

        if (!ignoreReturnTypes)
        {
            var sourceTypePredicate = this.getTypePredicateOfSignature(source);
            var targetTypePredicate = this.getTypePredicateOfSignature(target);
            if (((sourceTypePredicate != null) || (targetTypePredicate != null)))
            {
                result &= this.compareTypePredicatesIdentical(sourceTypePredicate, targetTypePredicate, compareTypes);
            }
            else
            {
                result &= compareTypes(this.getReturnTypeOfSignature(source), this.getReturnTypeOfSignature(target));
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2260:1.
    private bool isMatchingSignature(Signature source, Signature target, bool partialMatch)
    {
        var sourceParameterCount = this.getParameterCount(source);
        var targetParameterCount = this.getParameterCount(target);
        var sourceMinArgumentCount = this.getMinArgumentCount(source);
        var targetMinArgumentCount = this.getMinArgumentCount(target);
        var sourceHasRestParameter = this.hasEffectiveRestParameter(source);
        var targetHasRestParameter = this.hasEffectiveRestParameter(target);
        if ((((sourceParameterCount == targetParameterCount) && (sourceMinArgumentCount == targetMinArgumentCount)) && (sourceHasRestParameter == targetHasRestParameter)))
        {
            return true;
        }

        if ((partialMatch && (sourceMinArgumentCount <= targetMinArgumentCount)))
        {
            return true;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2280:1.
    private bool compareTypeParametersIdentical(List<Type> sourceParams, List<Type> targetParams)
    {
        if ((GoRuntime.Len(sourceParams) != GoRuntime.Len(targetParams)))
        {
            return false;
        }

        if (GoRuntime.Len(sourceParams) != 0)
            throw unsupported("compareTypeParametersIdentical: generic signature");
        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2303:1.
    private Ternary compareTypePredicatesIdentical(TypePredicate source, TypePredicate target, Func<Type, Type, Ternary> compareTypes)
    {
        switch (true)
        {
            case true when (((source == null) || (target == null)) || !this.typePredicateKindsMatch(source, target)):
            {
                return Ternary.False;
            }

            case true when (source.t == target.t):
            {
                return Ternary.True;
            }

            case true when ((source.t != null) && (target.t != null)):
            {
                return compareTypes(source.t, target.t);
            }
        }

        return Ternary.False;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1947:1.
    private Type getThisTypeOfSignature(Signature signature)
    {
        if ((signature.thisParameter != null))
        {
            return this.getTypeOfSymbol(signature.thisParameter);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:1954:1.
    private bool isInstantiatedGenericParameter(Signature signature, int pos)
    {
        if ((signature.target == null))
        {
            return false;
        }

        var t = this.tryGetTypeAtPosition(signature.target, pos);
        return ((t != null) && this.isGenericType(t));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2113:1.
    private bool typePredicateKindsMatch(TypePredicate a, TypePredicate b)
    {
        return ((a.kind == b.kind) && (a.parameterIndex == b.parameterIndex));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2133:1.
    internal TypePredicate instantiateTypePredicate(TypePredicate predicate, TypeMapper mapper)
    {
        var t = this.instantiateType(predicate.t, mapper);
        if ((t == predicate.t))
        {
            return predicate;
        }

        return this.newTypePredicate(predicate.kind, predicate.parameterName, predicate.parameterIndex, t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2141:1.
    private TypePredicate newTypePredicate(TypePredicateKind kind, string parameterName, int parameterIndex, Type t)
    {
        return new TypePredicate
        {
            kind = kind,
            parameterIndex = parameterIndex,
            parameterName = parameterName,
            t = t
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2145:1.
    private bool isResolvingReturnTypeOfSignature(Signature signature)
    {
        if (((signature.composite != null) && Core.Some(signature.composite.signatures, this.isResolvingReturnTypeOfSignature)))
        {
            return true;
        }

        return ((signature.resolvedReturnType == null) && (this.findResolutionCycleStartIndex(signature, TypeSystemPropertyName.ResolvedReturnType) >= 0));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2152:1.
    private List<Signature> findMatchingSignatures(List<List<Signature>> signatureLists, Signature signature, int listIndex)
    {
        if ((GoRuntime.Len(signature.typeParameters) != 0))
        {
            if ((listIndex > 0))
            {
                return null;
            }

            {
                var i = 1;
                for (; (i < GoRuntime.Len(signatureLists)); i++)
                {
                    if ((this.findMatchingSignature(signatureLists[i], signature, false, false, false) == null))
                    {
                        return null;
                    }
                }
            }

            return new List<Signature>
            {
                signature
            };
        }

        List<Signature> result = default;
        {
            foreach (var i2 in GoRuntime.RangeKeys(signatureLists))
            {
                Signature match = default;
                if ((i2 == listIndex))
                {
                    match = signature;
                }
                else
                {
                    match = this.findMatchingSignature(signatureLists[i2], signature, false, false, true);
                    if ((match == null))
                    {
                        match = this.findMatchingSignature(signatureLists[i2], signature, true, false, true);
                    }
                }

                if ((match == null))
                {
                    return null;
                }

                result = Core.AppendIfUnique(result, match);
            }
        }

        return result;
    }

    // relater.go:1891/1900. Ordinary signatures have no rest type; rest creation is excluded.
    private Type getNonArrayRestType(Signature signature)
    {
        var restType = getEffectiveRestType(signature);
        if (restType != null)
            throw unsupported("getNonArrayRestType: rest/array type");
        return null;
    }

    private Type getEffectiveRestType(Signature signature)
    {
        if (signatureHasRestParameter(signature))
            throw unsupported("getEffectiveRestType: rest/array type");
        return null;
    }

    // relater.go:1815. Reached only for effective nonarray rest parameters.
    private Type getRestOrAnyTypeAtPosition(Signature source, int pos) => throw unsupported("getRestOrAnyTypeAtPosition: tuple/array creation");
    // relater.go:1962. The admitted ordinary parameter arm preserves raw names.
    private string getParameterNameAtPosition(Signature signature, int pos)
    {
        int paramCount = GoRuntime.Len(signature.parameters) - Core.IfElse(signatureHasRestParameter(signature), 1, 0);
        if (pos < paramCount)
            return signature.parameters[pos].Name;
        throw unsupported("getParameterNameAtPosition: rest/tuple parameter");
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

            if ((source.flags & TypeFlags.TypeParameter) != 0 && c.getConstraintOfType(source) == target)
                return Ternary.True;
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
                        var calls = c.getSignaturesOfType(source, SignatureKind.Call);
                        var constructs = c.getSignaturesOfType(source, SignatureKind.Construct);
                        if (Core.Some(calls, sig => c.isTypeAssignableTo(c.getReturnTypeOfSignature(sig), target)) || Core.Some(constructs, sig => c.isTypeAssignableTo(c.getReturnTypeOfSignature(sig), target)))
                            reportError(Diagnostics.Value_of_type_0_has_no_properties_in_common_with_type_1_Did_you_mean_to_call_it, sourceString, targetString);
                        else
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
                if ((target.flags & TypeFlags.Union) != 0)
                {
                    var sourceOrigin = source.AsUnionType().origin;
                    if (sourceOrigin != null && (sourceOrigin.flags & TypeFlags.Intersection) != 0 && target.alias != null && sourceOrigin.Types().Contains(target))
                        return Ternary.True;
                    var targetOrigin = target.AsUnionType().origin;
                    if (targetOrigin != null && (targetOrigin.flags & TypeFlags.Union) != 0 && source.alias != null && targetOrigin.Types().Contains(source))
                        return Ternary.True;
                }

                if (relation == c.comparableRelation)
                    return someTypeRelatedToType(source, target, reportErrors && (source.flags & TypeFlags.Primitive) == 0, intersectionState);
                return eachTypeRelatedToType(source, target, reportErrors && (source.flags & TypeFlags.Primitive) == 0, intersectionState);
            }

            if ((target.flags & TypeFlags.Union) != 0)
                return typeRelatedToSomeType(c.getRegularTypeOfObjectLiteral(source), target, reportErrors && (source.flags & TypeFlags.Primitive) == 0 && (target.flags & TypeFlags.Primitive) == 0, intersectionState);
            if ((target.flags & TypeFlags.Intersection) != 0)
                return typeRelatedToEachType(source, target, reportErrors, IntersectionState.Target);
            // relater.go:2924. Hoist instantiable constituent constraints only for
            // primitive comparability; source parameters are excluded, implicit-this is retained.
            if (relation == c.comparableRelation && (target.flags & TypeFlags.Primitive) != 0)
            {
                var constraints = source.Types().Select(t => (t.flags & TypeFlags.Instantiable) != 0 ? c.getBaseConstraintOfType(t) ?? c.unknownType : t).ToList();
                if (!constraints.SequenceEqual(source.Types()))
                {
                    source = c.getIntersectionType(constraints);
                    if ((source.flags & TypeFlags.Never) != 0)
                        return Ternary.False;
                    if ((source.flags & TypeFlags.Intersection) == 0)
                    {
                        var result = isRelatedTo(source, target, RecursionFlags.Source, false);
                        return result != Ternary.False ? result : isRelatedTo(target, source, RecursionFlags.Source, false);
                    }
                }
            }

            return someTypeRelatedToType(source, target, false, IntersectionState.Source);
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

        // relater.go:3063. Ordered "each" intersection relation.
        private Ternary typeRelatedToEachType(Type source, Type target, bool reportErrors, IntersectionState intersectionState)
        {
            var result = Ternary.True;
            foreach (var type in target.Types())
            {
                var related = isRelatedToEx(source, type, RecursionFlags.Target, reportErrors, null, intersectionState);
                if (related == Ternary.False)
                    return Ternary.False;
                result &= related;
            }

            return result;
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
            if (relation != c.identityRelation)
            {
                if (result == Ternary.False && ((source.flags & TypeFlags.Intersection) != 0 || (source.flags & TypeFlags.TypeParameter) != 0 && (target.flags & TypeFlags.Union) != 0))
                {
                    var sourceTypes = (source.flags & TypeFlags.Intersection) != 0 ? source.Types() : new List<Type>
                    {
                        source
                    };
                    var constraint = c.getEffectiveConstraintOfIntersection(sourceTypes, (target.flags & TypeFlags.Union) != 0);
                    if (constraint != null && ((constraint.flags & TypeFlags.Union) != 0 ? constraint.Types().All(part => part != source) : constraint != source))
                        result = isRelatedToEx(constraint, target, RecursionFlags.Source, false, null, intersectionState);
                }

                if (result != Ternary.False && (intersectionState & IntersectionState.Target) == 0 && (target.flags & TypeFlags.Intersection) != 0 && !c.isGenericObjectType(target) && (source.flags & (TypeFlags.Object | TypeFlags.Intersection)) != 0)
                {
                    result &= propertiesRelatedTo(source, target, reportErrors, null, false, IntersectionState.None);
                    if (result != Ternary.False && isObjectLiteralType(source) && (source.objectFlags & ObjectFlags.FreshLiteral) != 0)
                        result &= indexSignaturesRelatedTo(source, target, false, reportErrors, IntersectionState.None);
                }
                else if (result != Ternary.False && c.isNonGenericObjectType(target) && !c.isArrayOrTupleType(target) && isSourceIntersectionNeedingExtraCheck(source, target))
                    result &= propertiesRelatedTo(source, target, reportErrors, null, true, intersectionState);
            }

            if (result != Ternary.False)
                restoreErrorState(save);
            return result;
        }

        // relater.go:3287.
        private bool isSourceIntersectionNeedingExtraCheck(Type source, Type target) => (source.flags & TypeFlags.Intersection) != 0 && (c.getApparentType(source).flags & TypeFlags.StructuredType) != 0 && !source.Types().Any(t => t == target || (t.objectFlags & ObjectFlags.NonInferrableType) != 0);
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
                if (!((source.flags & TypeFlags.Instantiable) != 0 || (source.flags & TypeFlags.Object) != 0 && (target.flags & TypeFlags.Union) != 0 || (source.flags & TypeFlags.Intersection) != 0 && (target.flags & (TypeFlags.Object | TypeFlags.Union | TypeFlags.Instantiable)) != 0))
                    return Ternary.False;
            }

            // relater.go:3709. Selected implicit-this variable constraint path.
            if ((source.flags & TypeFlags.TypeVariable) != 0)
            {
                var constraint = c.getConstraintOfType(source) ?? c.unknownType;
                var related = isRelatedToEx(constraint, target, RecursionFlags.Source, false, null, intersectionState);
                if (related != Ternary.False)
                    return related;
                var constraintWithThis = c.getTypeWithThisArgument(constraint, source, false);
                return isRelatedToEx(constraintWithThis, target, RecursionFlags.Source, reportErrors && constraint != c.unknownType && (target.flags & source.flags & TypeFlags.TypeParameter) == 0, null, intersectionState);
            }

            bool sourceIsPrimitive = (source.flags & TypeFlags.Primitive) != 0;
            if (relation != c.identityRelation)
                source = c.getApparentType(source);
            if ((relation == c.subtypeRelation || relation == c.strictSubtypeRelation) && c.isEmptyObjectType(target) && (target.objectFlags & ObjectFlags.FreshLiteral) != 0 && !c.isEmptyObjectType(source))
                return Ternary.False;
            if ((source.objectFlags & target.objectFlags & ObjectFlags.Reference) != 0 && source.Target() == target.Target() && !c.isMarkerType(source) && !c.isMarkerType(target))
            {
                var variances = c.getVariances(source.Target());
                if (variances is not { Count: > 0 })
                    return Ternary.Unknown;
                var related = typeArgumentsRelatedTo(c.getTypeArguments(source), c.getTypeArguments(target), variances, reportErrors, intersectionState);
                if (related != Ternary.False)
                    return related;
                // A nongeneric target has no declared parameters/variance entries;
                // broader written-generic fallback must never be reached.
                throw unsupported("structuredTypeRelatedToWorker: generic variance fallback");
            }

            if ((source.flags & (TypeFlags.Object | TypeFlags.Intersection)) != 0 && (target.flags & TypeFlags.Object) != 0)
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

            if ((source.flags & (TypeFlags.Object | TypeFlags.Intersection)) != 0 && (target.flags & TypeFlags.Union) != 0)
            {
                var objectOnly = c.extractTypesOfKind(target, TypeFlags.Object | TypeFlags.Intersection | TypeFlags.Substitution);
                if ((objectOnly.flags & TypeFlags.Union) != 0)
                {
                    var related = typeRelatedToDiscriminatedType(source, objectOnly);
                    if (related != Ternary.False)
                        return related;
                }
            }

            return Ternary.False;
        }

        // relater.go:3935. Exact variance ordering; unselected source-generic
        // reliability computation is guarded before it can affect a relationship.
        private Ternary typeArgumentsRelatedTo(List<Type> sources, List<Type> targets, List<VarianceFlags> variances, bool reportErrors, IntersectionState intersectionState)
        {
            if ((sources?.Count ?? 0) != (targets?.Count ?? 0) && relation == c.identityRelation)
                return Ternary.False;
            int length = Math.Min(sources?.Count ?? 0, targets?.Count ?? 0);
            var result = Ternary.True;
            for (int i = 0; i < length; i++)
            {
                var varianceFlags = variances != null && i < variances.Count ? variances[i] : VarianceFlags.Covariant;
                var variance = varianceFlags & VarianceFlags.VarianceMask;
                if (variance == VarianceFlags.Independent)
                    continue;
                var source = sources[i];
                var target = targets[i];
                Ternary related;
                if ((varianceFlags & VarianceFlags.Unmeasurable) != 0)
                    related = relation == c.identityRelation ? isRelatedTo(source, target, RecursionFlags.Both, false) : c.compareTypesIdentical(source, target);
                else
                {
                    if (c.varianceStack.Count != 0 && (varianceFlags & VarianceFlags.Unreliable) != 0)
                        throw unsupported("typeArgumentsRelatedTo: generic unreliable mapper");
                    if (variance == VarianceFlags.Covariant)
                        related = isRelatedToEx(source, target, RecursionFlags.Both, reportErrors, null, intersectionState);
                    else if (variance == VarianceFlags.Contravariant)
                        related = isRelatedToEx(target, source, RecursionFlags.Both, reportErrors, null, intersectionState);
                    else if (variance == VarianceFlags.Bivariant)
                    {
                        related = isRelatedTo(target, source, RecursionFlags.Both, false);
                        if (related == Ternary.False)
                            related = isRelatedToEx(source, target, RecursionFlags.Both, reportErrors, null, intersectionState);
                    }
                    else
                    {
                        related = isRelatedToEx(source, target, RecursionFlags.Both, reportErrors, null, intersectionState);
                        if (related != Ternary.False)
                            related &= isRelatedToEx(target, source, RecursionFlags.Both, reportErrors, null, intersectionState);
                    }
                }

                if (related == Ternary.False)
                    return Ternary.False;
                result &= related;
            }

            return result;
        }

        // relater.go:4021. Build the actual limited Cartesian discriminant matrix,
        // then compare every matched constituent's remaining data properties.
        private Ternary typeRelatedToDiscriminatedType(Type source, Type target)
        {
            var sourceProperties = c.getPropertiesOfType(source);
            var filtered = c.findDiscriminantProperties(sourceProperties, target);
            if (filtered is not { Count: > 0 })
                return Ternary.False;
            int numCombinations = 1;
            foreach (var property in filtered)
            {
                var type = c.getNonMissingTypeOfSymbol(property);
                numCombinations *= (type.flags & TypeFlags.Union) != 0 ? type.Types().Count : (type.flags & TypeFlags.Never) != 0 ? 0 : 1;
                if (numCombinations > 25 || numCombinations == 0)
                    return Ternary.False;
            }

            var sourceDiscriminantTypes = new List<List<Type>>();
            HashSet<string> excludedProperties = new(StringComparer.Ordinal);
            foreach (var property in filtered)
            {
                sourceDiscriminantTypes.Add(c.getNonMissingTypeOfSymbol(property).Distributed());
                excludedProperties.Add(property.Name);
            }

            var combinations = new List<List<Type>>();
            for (int i = 0; i < numCombinations; i++)
            {
                var combination = new Type[sourceDiscriminantTypes.Count];
                int n = i;
                for (int j = sourceDiscriminantTypes.Count - 1; j >= 0; j--)
                {
                    var types = sourceDiscriminantTypes[j];
                    combination[j] = types[n % types.Count];
                    n /= types.Count;
                }

                combinations.Add(combination.ToList());
            }

            List<Type> matchingTypes = null;
            foreach (var combination in combinations)
            {
                bool hasMatch = false;
                foreach (var type in target.Types())
                {
                    bool matched = true;
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        var sourceProperty = filtered[i];
                        var targetProperty = c.getPropertyOfType(type, sourceProperty.Name);
                        if (targetProperty == null)
                        {
                            matched = false;
                            break;
                        }

                        if (sourceProperty == targetProperty)
                            continue;
                        int index = i;
                        var related = propertyRelatedTo(source, target, sourceProperty, targetProperty, _ => combination[index], false, IntersectionState.None, c.strictNullChecks || relation == c.comparableRelation);
                        if (related == Ternary.False)
                        {
                            matched = false;
                            break;
                        }
                    }

                    if (!matched)
                        continue;
                    matchingTypes = Core.AppendIfUnique(matchingTypes, type);
                    hasMatch = true;
                }

                if (!hasMatch)
                    return Ternary.False;
            }

            var result = Ternary.True;
            foreach (var type in matchingTypes ?? [])
            {
                result &= propertiesRelatedTo(source, type, false, excludedProperties, false, IntersectionState.None);
                if (result != Ternary.False)
                {
                    result &= signaturesRelatedTo(source, type, SignatureKind.Call, false, IntersectionState.None);
                    if (result != Ternary.False)
                    {
                        result &= signaturesRelatedTo(source, type, SignatureKind.Construct, false, IntersectionState.None);
                        if (result != Ternary.False)
                            result &= indexSignaturesRelatedTo(source, type, false, false, IntersectionState.None);
                    }
                }

                if (result == Ternary.False)
                    return result;
            }

            return result;
        }

        // relater.go:4473. Ordered overload matrix and original error-state rollback.
        private Ternary signaturesRelatedTo(Type source, Type target, SignatureKind kind, bool reportErrors, IntersectionState intersectionState)
        {
            if (relation == c.identityRelation)
                return signaturesIdenticalTo(source, target, kind);
            if (source == c.anyFunctionType)
                return Ternary.True;
            if (target == c.anyFunctionType)
                return Ternary.False;
            var sourceSignatures = c.getSignaturesOfType(source, kind);
            var targetSignatures = c.getSignaturesOfType(target, kind);
            if (kind == SignatureKind.Construct && GoRuntime.Len(sourceSignatures) != 0 && GoRuntime.Len(targetSignatures) != 0)
                throw unsupported("signaturesRelatedTo: construct signatures");
            var result = Ternary.True;
            if ((source.objectFlags & ObjectFlags.Instantiated) != 0 && (target.objectFlags & ObjectFlags.Instantiated) != 0 && source.symbol == target.symbol || (source.objectFlags & ObjectFlags.Reference) != 0 && (target.objectFlags & ObjectFlags.Reference) != 0 && source.Target() == target.Target())
            {
                for (int i = 0; i < GoRuntime.Len(targetSignatures); i++)
                {
                    var related = signatureRelatedTo(sourceSignatures[i], targetSignatures[i], true, reportErrors, intersectionState);
                    if (related == Ternary.False)
                        return Ternary.False;
                    result &= related;
                }
            }
            else if (GoRuntime.Len(sourceSignatures) == 1 && GoRuntime.Len(targetSignatures) == 1)
                result = signatureRelatedTo(sourceSignatures[0], targetSignatures[0], relation == c.comparableRelation, reportErrors, intersectionState);
            else
            {
                foreach (var t in GoRuntime.RangeValues(targetSignatures))
                {
                    var saveErrorState = getErrorState();
                    var shouldElaborateErrors = reportErrors;
                    bool matched = false;
                    foreach (var sourceSignature in GoRuntime.RangeValues(sourceSignatures))
                    {
                        var related = signatureRelatedTo(sourceSignature, t, true, shouldElaborateErrors, intersectionState);
                        if (related != Ternary.False)
                        {
                            result &= related;
                            restoreErrorState(saveErrorState);
                            matched = true;
                            break;
                        }

                        shouldElaborateErrors = false;
                    }

                    // The boolean represents the original labelled continue outer.
                    if (matched)
                        continue;
                    if (shouldElaborateErrors)
                        reportError(Diagnostics.Type_0_provides_no_match_for_the_signature_1, c.typeToStringByteString(source), c.signatureToString(t));
                    return Ternary.False;
                }
            }

            return result;
        }

        // relater.go:4575.
        private Ternary signatureRelatedTo(Signature source, Signature target, bool erase, bool reportErrors, IntersectionState intersectionState)
        {
            var checkMode = SignatureCheckMode.None;
            if (relation == c.subtypeRelation)
                checkMode = SignatureCheckMode.StrictTopSignature;
            else if (relation == c.strictSubtypeRelation)
                checkMode = SignatureCheckMode.StrictTopSignature | SignatureCheckMode.StrictArity;
            if (erase)
            {
                source = c.getErasedSignature(source);
                target = c.getErasedSignature(target);
            }

            TypeComparer isRelatedToWorker = (sourceType, targetType, errors) => isRelatedToEx(sourceType, targetType, RecursionFlags.Both, errors, null, intersectionState);
            return c.compareSignaturesRelated(source, target, checkMode, reportErrors, reportError, isRelatedToWorker, c.reportUnreliableMapper);
        }

        // relater.go:4593. Actual length check and nil-slot resolution boundaries.
        // relater.go:4593. Ordered pairwise signature identity.
        private Ternary signaturesIdenticalTo(Type source, Type target, SignatureKind kind)
        {
            var sourceSignatures = c.getSignaturesOfType(source, kind);
            var targetSignatures = c.getSignaturesOfType(target, kind);
            if (GoRuntime.Len(sourceSignatures) != GoRuntime.Len(targetSignatures))
                return Ternary.False;
            var result = Ternary.True;
            for (int i = 0; i < GoRuntime.Len(sourceSignatures); i++)
            {
                var related = c.compareSignaturesIdentical(sourceSignatures[i], targetSignatures[i], false, false, false, isRelatedToSimple);
                if (related == Ternary.False)
                    return Ternary.False;
                result &= related;
            }

            return result;
        }

        // relater.go:4610. Nonempty selected indexes arise from any-base
        // inheritance even though source index-signature syntax is excluded.
        private Ternary indexSignaturesRelatedTo(Type source, Type target, bool sourceIsPrimitive, bool reportErrors, IntersectionState intersectionState)
        {
            if (relation == c.identityRelation)
                return indexSignaturesIdenticalTo(source, target);
            var infos = c.getIndexInfosOfType(target);
            bool hasStringIndex = infos?.Any(info => info.keyType == c.stringType) == true;
            var result = Ternary.True;
            foreach (var info in infos ?? [])
            {
                var related = relation != c.strictSubtypeRelation && !sourceIsPrimitive && hasStringIndex && (info.valueType.flags & TypeFlags.Any) != 0 ? Ternary.True : typeRelatedToIndexInfo(source, info, reportErrors, intersectionState);
                if (related == Ternary.False)
                    return Ternary.False;
                result &= related;
            }

            return result;
        }

        private Ternary typeRelatedToIndexInfo(Type source, IndexInfo targetInfo, bool reportErrors, IntersectionState intersectionState)
        {
            var sourceInfo = c.getApplicableIndexInfo(source, targetInfo.keyType);
            if (sourceInfo != null)
                return indexInfoRelatedTo(sourceInfo, targetInfo, reportErrors, intersectionState);
            if ((intersectionState & IntersectionState.Source) == 0 && (relation != c.strictSubtypeRelation || (source.objectFlags & ObjectFlags.FreshLiteral) != 0) && c.isObjectTypeWithInferableIndex(source))
                return membersRelatedToIndexInfo(source, targetInfo, reportErrors, intersectionState);
            if (reportErrors)
                reportError(Diagnostics.Index_signature_for_type_0_is_missing_in_type_1, c.typeToStringByteString(targetInfo.keyType), c.typeToStringByteString(source));
            return Ternary.False;
        }

        private Ternary membersRelatedToIndexInfo(Type source, IndexInfo targetInfo, bool reportErrors, IntersectionState intersectionState)
        {
            var result = Ternary.True;
            var keyType = targetInfo.keyType;
            var properties = (source.flags & TypeFlags.Intersection) != 0 ? c.getPropertiesOfUnionOrIntersectionType(source) : c.getPropertiesOfObjectType(source);
            foreach (var property in properties ?? [])
            {
                if (!c.isApplicableIndexType(c.getLiteralTypeFromProperty(property, TypeFlags.StringOrNumberLiteralOrUnique, false), keyType))
                    continue;
                var propertyType = c.getNonMissingTypeOfSymbol(property);
                var type = c.exactOptionalPropertyTypes || (propertyType.flags & TypeFlags.Undefined) != 0 || keyType == c.numberType || (property.Flags & SymbolFlags.Optional) == 0 ? propertyType : c.getTypeWithFacts(propertyType, TypeFacts.NEUndefined);
                var related = isRelatedToEx(type, targetInfo.valueType, RecursionFlags.Both, reportErrors, null, intersectionState);
                if (related == Ternary.False)
                {
                    if (reportErrors)
                        reportError(Diagnostics.Property_0_is_incompatible_with_index_signature, c.symbolToString(property));
                    return Ternary.False;
                }

                result &= related;
            }

            foreach (var info in c.getIndexInfosOfType(source) ?? [])
            {
                if (!c.isApplicableIndexType(info.keyType, keyType))
                    continue;
                var related = indexInfoRelatedTo(info, targetInfo, reportErrors, intersectionState);
                if (related == Ternary.False)
                    return Ternary.False;
                result &= related;
            }

            return result;
        }

        private Ternary indexInfoRelatedTo(IndexInfo sourceInfo, IndexInfo targetInfo, bool reportErrors, IntersectionState intersectionState)
        {
            var related = isRelatedToEx(sourceInfo.valueType, targetInfo.valueType, RecursionFlags.Both, reportErrors, null, intersectionState);
            if (related == Ternary.False && reportErrors)
            {
                if (sourceInfo.keyType == targetInfo.keyType)
                    reportError(Diagnostics.X_0_index_signatures_are_incompatible, c.typeToStringByteString(sourceInfo.keyType));
                else
                    reportError(Diagnostics.X_0_and_1_index_signatures_are_incompatible, c.typeToStringByteString(sourceInfo.keyType), c.typeToStringByteString(targetInfo.keyType));
            }

            return related;
        }

        private Ternary indexSignaturesIdenticalTo(Type source, Type target)
        {
            var sourceInfos = c.getIndexInfosOfType(source);
            var targetInfos = c.getIndexInfosOfType(target);
            if ((sourceInfos?.Count ?? 0) != (targetInfos?.Count ?? 0))
                return Ternary.False;
            foreach (var targetInfo in targetInfos ?? [])
            {
                var sourceInfo = c.getIndexInfoOfType(source, targetInfo.keyType);
                if (!(sourceInfo != null && isRelatedTo(sourceInfo.valueType, targetInfo.valueType, RecursionFlags.Both, false) != Ternary.False && sourceInfo.isReadonly == targetInfo.isReadonly))
                    return Ternary.False;
            }

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
            guardFiniteObject(originalSource, "reportErrorResults: unsupported source");
            guardFiniteObject(originalTarget, "reportErrorResults: unsupported target");
            bool sourceHasBase = c.getSingleBaseForNonAugmentingSubtype(originalSource) != null;
            bool targetHasBase = c.getSingleBaseForNonAugmentingSubtype(originalTarget) != null;
            if (originalSource.alias != null || sourceHasBase)
                source = originalSource;
            if (originalTarget.alias != null || targetHasBase)
                target = originalTarget;
            if ((source.flags & target.flags & TypeFlags.Object) != 0)
                tryElaborateArrayLikeErrors(source, target, true);
            if ((source.flags & TypeFlags.Object) != 0 && (target.flags & TypeFlags.Primitive) != 0)
                tryElaborateErrorsForPrimitivesAndObjects(source, target);
            else if (source.symbol != null && (source.flags & TypeFlags.Object) != 0 && c.globalObjectType == source)
                reportError(Diagnostics.The_Object_type_is_assignable_to_very_few_other_types_Did_you_mean_to_use_the_any_type_instead);
            if ((source.objectFlags & ObjectFlags.JsxAttributes) != 0)
                throw unsupported("reportErrorResults: JSX");
            if ((originalTarget.flags & TypeFlags.Intersection) != 0 && (originalTarget.objectFlags & ObjectFlags.IsNeverIntersection) != 0)
            {
                var property = c.getPropertiesOfUnionOrIntersectionType(originalTarget)?.FirstOrDefault(c.isDiscriminantWithNeverType);
                if (property != null)
                    reportError(Diagnostics.The_intersection_0_was_reduced_to_never_because_property_1_has_conflicting_types_in_some_constituents, c.typeToStringNoTypeReduction(originalTarget), c.symbolToString(property));
            // Private data members are excluded, so the private-conflict
            // fallback can have no matches in this admitted property model.
            }

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

            if ((target.flags & TypeFlags.IndexedAccess) != 0)
                throw unsupported("reportRelationError: indexed access");
            if ((target.flags & TypeFlags.TypeParameter) != 0)
            {
                var constraint = c.getBaseConstraintOfType(target);
                if (constraint != null && c.isTypeAssignableTo(generalizedSource, constraint))
                    reportError(Diagnostics.X_0_is_assignable_to_the_constraint_of_type_1_but_1_could_be_instantiated_with_a_different_subtype_of_constraint_2, generalizedSourceType, targetType, c.typeToStringByteString(constraint));
                else if (constraint != null && c.isTypeAssignableTo(source, constraint))
                    reportError(Diagnostics.X_0_is_assignable_to_the_constraint_of_type_1_but_1_could_be_instantiated_with_a_different_subtype_of_constraint_2, sourceType, targetType, c.typeToStringByteString(constraint));
                else
                {
                    errorChain = null;
                    reportError(Diagnostics.X_0_could_be_instantiated_with_an_arbitrary_type_which_could_be_unrelated_to_1, targetType, generalizedSourceType);
                }
            }

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
    private string getTypeNameForErrorDisplay(Type t) => typeToStringEx(t, null, flags: TypeScript.NodeBuilding.Flags.UseFullyQualifiedType);
    private bool typeCouldHaveTopLevelSingletonTypes(Type t)
    {
        if ((t.flags & TypeFlags.Boolean) != 0)
            return false;
        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
            return t.Types().Any(typeCouldHaveTopLevelSingletonTypes);
        if ((t.flags & TypeFlags.Instantiable) != 0)
        {
            if ((t.flags & TypeFlags.TypeParameter) == 0)
                throw unsupported("typeCouldHaveTopLevelSingletonTypes: unselected constrained types");
            var constraint = getConstraintOfTypeParameter(t);
            return constraint != null && typeCouldHaveTopLevelSingletonTypes(constraint);
        }

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
            case Kind.BinaryExpression:
                if (node.AsBinaryExpression().OperatorToken.Kind is Kind.EqualsToken or Kind.CommaToken)
                    return elaborateError(node.AsBinaryExpression().Right, source, target, relation, headMessage, diagnosticOutput);
                break;
            case Kind.ArrowFunction:
                return elaborateArrowFunction(node, source, target, relation, diagnosticOutput);
            case Kind.AsExpression:
            case Kind.JsxExpression:
            case Kind.ArrayLiteralExpression:
            case Kind.JsxAttributes:
                throw unsupported("elaborateError: assertions/arrays/JSX");
        }

        return false;
    }

    private bool elaborateDidYouMeanToCallOrConstruct(Node node, Type source, Type target, Relation relation, SignatureKind kind, Message headMessage, List<Diagnostic> diagnosticOutput)
    {
        if (Core.Some(getSignaturesOfType(source, kind), signature =>
        {
            var returnType = getReturnTypeOfSignature(signature);
            return (returnType.flags & (TypeFlags.Any | TypeFlags.Never)) == 0 && checkTypeRelatedTo(returnType, target, relation, null);
        }))
        {
            var diags = new List<Diagnostic>();
            if (!checkTypeRelatedToEx(source, target, relation, node, headMessage, diags))
            {
                var message = kind == SignatureKind.Construct ? Diagnostics.Did_you_mean_to_use_new_with_this_expression : Diagnostics.Did_you_mean_to_call_this_expression;
                reportDiagnostic(diags[0].AddRelatedInfo(createDiagnosticForNode(node, message)), diagnosticOutput);
                return true;
            }
        }

        return false;
    }

    // relater.go:639. Elaborate expression-bodied arrow return mismatches.
    private bool elaborateArrowFunction(Node node, Type source, Type target, Relation relation, List<Diagnostic> diagnosticOutput)
    {
        if (Ast.IsBlock(node.Body()) || Core.Some(node.Parameters(), parameter => parameter.Type() != null))
            return false;
        var sourceSig = getSingleCallSignature(source);
        if (sourceSig == null)
            return false;
        var targetSignatures = getSignaturesOfType(target, SignatureKind.Call);
        if (GoRuntime.Len(targetSignatures) == 0)
            return false;
        var returnExpression = node.Body();
        var sourceReturn = getReturnTypeOfSignature(sourceSig);
        var targetReturn = getUnionType(targetSignatures.Select(getReturnTypeOfSignature).ToList());
        if (checkTypeRelatedTo(sourceReturn, targetReturn, relation, null))
            return false;
        if (returnExpression != null && elaborateError(returnExpression, sourceReturn, targetReturn, relation, null, diagnosticOutput))
            return true;
        var diags = new List<Diagnostic>();
        checkTypeRelatedToEx(sourceReturn, targetReturn, relation, returnExpression, null, diags);
        if (diags.Count != 0)
        {
            var diagnostic = diags[0];
            if (target.symbol != null && GoRuntime.Len(target.symbol.Declarations) != 0)
            {
                var declaration = target.symbol.Declarations[0];
                diagnostic.AddRelatedInfo(createDiagnosticForNode(declaration, Diagnostics.The_expected_type_comes_from_the_return_type_of_this_signature));
            }

            if ((Ast.GetFunctionFlags(node) & FunctionFlags.Async) == 0 && getTypeOfPropertyOfType(sourceReturn, "then") == null && checkTypeRelatedTo(createPromiseType(sourceReturn), targetReturn, relation, null))
                diagnostic.AddRelatedInfo(createDiagnosticForNode(node, Diagnostics.Did_you_mean_to_mark_this_function_as_async));
            reportDiagnostic(diagnostic, diagnosticOutput);
            return true;
        }

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
                case Kind.MethodDeclaration:
                case Kind.ShorthandPropertyAssignment:
                    reportedError = elaborateElement(source, target, relation, prop.Name(), null, nameType, null, null, diagnosticOutput) || reportedError;
                    break;
                case Kind.PropertyAssignment:
                    if (prop.Name().Kind == Kind.ComputedPropertyName)
                        throw unsupported("elaborateObjectLiteral: computed name");
                    reportedError = elaborateElement(source, target, relation, prop.Name(), prop.Initializer(), nameType, null, null, diagnosticOutput) || reportedError;
                    break;
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                    throw unsupported("elaborateObjectLiteral: accessor");
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

        bool issuedElaboration = false;
        if (targetProp == null)
        {
            var info = getApplicableIndexInfo(target, nameType);
            // The selected noLib script is never a default library file.
            if (info?.declaration != null)
            {
                issuedElaboration = true;
                diagnostic.AddRelatedInfo(NewDiagnosticForNode(info.declaration, Diagnostics.The_expected_type_comes_from_this_index_signature));
            }
        }

        if (!issuedElaboration && ((targetProp?.Declarations?.Count ?? 0) != 0 || (target.symbol?.Declarations?.Count ?? 0) != 0))
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
