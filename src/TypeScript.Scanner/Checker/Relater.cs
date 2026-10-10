// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of shared TypeScript relationships in tsc/internal/checker/relater.go,
// upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Includes JSX elaboration and attribute comparisons; enabled tracing remains external.
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
        return hasBaseType(source, getTargetType(target)) || isArrayType(target) && !isReadonlyArrayType(target) && isTypeDerivedFrom(source, globalReadonlyArrayType);
    }

    // relater.go:2559.
    private static string visibilityToString(ModifierFlags flags) => flags == ModifierFlags.Private ? "private" : flags == ModifierFlags.Protected ? "protected" : "public";
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
        // Authored nonforcing observer access to the actual completed cache.
        internal Dictionary<CacheHashKey, RelationComparisonResult> VerificationResults => results;
    }

    private readonly LinkStore<Symbol, VarianceLinks> varianceLinks = new();
    private List<(Symbol symbol, List<Type> typeParameters)> varianceStack;
    // relater.go:1317/1334. Arrays/tuples use their known variances; other
    // generic declarations retain the recursive marker measurement and cache.
    private List<VarianceFlags> getVariances(Type t)
    {
        if (t == globalArrayType || t == globalReadonlyArrayType || (t.objectFlags & ObjectFlags.Tuple) != 0)
            return arrayVariances;
        return getVariancesWorker(t.symbol, t.AsInterfaceType().TypeParameters());
    }

    private List<VarianceFlags> getAliasVariances(Symbol symbol) => getVariancesWorker(symbol, typeAliasLinks.Get(symbol).typeParameters);
    private List<VarianceFlags> getVariancesWorker(Symbol symbol, List<Type> typeParameters)
    {
        var links = varianceLinks.Get(symbol);
        if (links.variances == null)
        {
            int stackIndex = getVarianceStackIndex(symbol);
            if (stackIndex < 0)
            {
                int saveResolutionStart = resolutionStart;
                if (GoRuntime.Len(varianceStack) == 0)
                    resolutionStart = typeResolutions.Count;
                varianceStack = GoRuntime.Append(varianceStack, (symbol, typeParameters));
                var variances = GoRuntime.MakeSlice<VarianceFlags>(GoRuntime.Len(typeParameters));
                int count = GoRuntime.Len(typeParameters);
                for (int i = 0; i < count; i++)
                {
                    var tp = typeParameters[i];
                    var modifiers = getTypeParameterModifiers(tp);
                    VarianceFlags variance;
                    if ((modifiers & ModifierFlags.Out) != 0)
                        variance = (modifiers & ModifierFlags.In) != 0 ? VarianceFlags.Invariant : VarianceFlags.Covariant;
                    else if ((modifiers & ModifierFlags.In) != 0)
                        variance = VarianceFlags.Contravariant;
                    else
                    {
                        var saveReliabilityFlags = reliabilityFlags;
                        reliabilityFlags = 0;
                        var typeWithSuper = createMarkerType(symbol, tp, markerSuperType);
                        var typeWithSub = createMarkerType(symbol, tp, markerSubType);
                        variance = (isTypeAssignableTo(typeWithSub, typeWithSuper) ? VarianceFlags.Covariant : 0) | (isTypeAssignableTo(typeWithSuper, typeWithSub) ? VarianceFlags.Contravariant : 0);
                        if (variance == VarianceFlags.Bivariant && isTypeAssignableTo(createMarkerType(symbol, tp, markerOtherType), typeWithSuper))
                            variance = VarianceFlags.Independent;
                        if ((reliabilityFlags & RelationComparisonResult.ReportsUnmeasurable) != 0)
                            variance |= VarianceFlags.Unmeasurable;
                        if ((reliabilityFlags & RelationComparisonResult.ReportsUnreliable) != 0)
                            variance |= VarianceFlags.Unreliable;
                        reliabilityFlags = saveReliabilityFlags;
                    }

                    if (GoRuntime.Len(links.variances) != 0)
                        break;
                    variances[i] = variance;
                }

                if (GoRuntime.Len(links.variances) == 0)
                    links.variances = variances;
                varianceStack = GoRuntime.Slice(varianceStack, 0, GoRuntime.Len(varianceStack) - 1);
                if (GoRuntime.Len(varianceStack) == 0)
                    resolutionStart = saveResolutionStart;
            }
            else
            {
                int minIndex = stackIndex;
                for (int i = stackIndex + 1; i < GoRuntime.Len(varianceStack); i++)
                    if (compareSymbols(varianceStack[i].symbol, varianceStack[minIndex].symbol) < 0)
                        minIndex = i;
                if (minIndex > stackIndex)
                {
                    var saveVarianceStack = varianceStack;
                    varianceStack = null;
                    getVariancesWorker(saveVarianceStack[minIndex].symbol, saveVarianceStack[minIndex].typeParameters);
                    varianceStack = saveVarianceStack;
                }

                if (GoRuntime.Len(links.variances) == 0)
                    links.variances = new();
            }
        }

        return links.variances;
    }

    private int getVarianceStackIndex(Symbol symbol)
    {
        for (int i = 0; i < GoRuntime.Len(varianceStack); i++)
            if (varianceStack[i].symbol == symbol)
                return i;
        return -1;
    }

    private readonly Relation assignableRelation = new(), identityRelation = new(), subtypeRelation = new(), strictSubtypeRelation = new(), comparableRelation = new();
    private bool isMarkerType(Type t) => markerTypes.Contains(t);
    private bool checkTypeAssignableTo(Type source, Type target, Node errorNode, Message headMessage) => checkTypeRelatedToEx(source, target, assignableRelation, errorNode, headMessage, null);
    // relater.go:347. Type assertions use the actual comparable relation and diagnostics.
    private bool checkTypeComparableTo(Type source, Type target, Node errorNode, Message headMessage) => checkTypeRelatedToEx(source, target, comparableRelation, errorNode, headMessage, null);
    private bool checkTypeAssignableToEx(Type source, Type target, Node errorNode, Message headMessage, List<Diagnostic> output) => checkTypeRelatedToEx(source, target, assignableRelation, errorNode, headMessage, output);
    private bool isTypeAssignableTo(Type source, Type target) => isTypeRelatedTo(source, target, assignableRelation);
    internal bool isTypeIdenticalTo(Type source, Type target) => isTypeRelatedTo(source, target, identityRelation);
    internal Ternary compareTypesIdentical(Type source, Type target) => isTypeRelatedTo(source, target, identityRelation) ? Ternary.True : Ternary.False;
    private Ternary compareTypesAssignableSimple(Type source, Type target) => isTypeRelatedTo(source, target, assignableRelation) ? Ternary.True : Ternary.False;
    private Ternary compareTypesAssignableWorker(Type source, Type target, bool reportErrors) => compareTypesAssignable(source, target, reportErrors);
    private Ternary compareTypesAssignable(Type source, Type target, bool reportErrors) => compareTypesAssignableSimple(source, target);
    // Authored explicit representation of relater.go:4587's captured function.
    // The original callback captures this relater and intersection state only.
    private sealed class SignatureRelatedComparer(Relater relater, IntersectionState state)
    {
        internal readonly Relater relater = relater;
        internal readonly IntersectionState state = state;
        internal Ternary Compare(Type source, Type target, bool reportErrors) => relater.isRelatedToEx(source, target, RecursionFlags.Both, reportErrors, null, state);
    }

    internal (string Family, object Owner, string CurrentRelation, uint IntersectionState) VerificationCollectionsComparer(TypeComparer comparer)
    {
        if (comparer == compareTypesAssignable)
            return ("checker-assignable", this, "assignable", 0);
        Relater owner;
        string family;
        uint state;
        if (comparer?.Target is Relater relater && comparer == relater.isRelatedToWorker)
        {
            owner = relater;
            family = "relater-worker";
            state = 0;
        }
        else if (comparer?.Target is SignatureRelatedComparer capture && comparer == capture.Compare)
        {
            owner = capture.relater;
            family = "signature-related";
            state = (uint)capture.state;
        }
        else
            throw new InvalidOperationException("Unknown stored comparer provenance");
        if (!owner.VerificationOwnedBy(this))
            throw new InvalidOperationException("Foreign stored comparer relater");
        return (family, owner, VerificationCollectionsComparerOwner(owner), state);
    }

    internal string VerificationCollectionsComparerOwner(object value)
    {
        if (value is not Relater owner || !owner.VerificationOwnedBy(this))
            throw new InvalidOperationException("Unknown or foreign stored comparer owner");
        return owner.relation == null ? "nil" : ReferenceEquals(owner.relation, assignableRelation) ? "assignable" : ReferenceEquals(owner.relation, comparableRelation) ? "comparable" : ReferenceEquals(owner.relation, identityRelation) ? "identity" : ReferenceEquals(owner.relation, subtypeRelation) ? "subtype" : ReferenceEquals(owner.relation, strictSubtypeRelation) ? "strict-subtype" : throw new InvalidOperationException("Unknown stored comparer relation identity");
    }

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

    // Go constant values are nil, float64, or byte strings. Preserve interface
    // equality without CLR boxed identity or Double.Equals treating NaN as equal.
    private static bool declarationConstantValuesEqual(object left, object right) =>
        left is JsNum.Number a && right is JsNum.Number b ? a.Value == b.Value :
        left is double x && right is double y ? x == y : Equals(left, right);
    // relater.go:205: primitive and enum relationships.
    private bool isSimpleTypeRelatedTo(Type source, Type target, Relation relation, ErrorReporter errorReporter = null)
    {
        var s = source.flags;
        var t = target.flags;
        if (((((t & TypeFlags.Any) != 0) || ((s & TypeFlags.Never) != 0)) || (source == this.wildcardType)))
        {
            return true;
        }
        if ((((t & TypeFlags.Unknown) != 0) && !(((relation == this.strictSubtypeRelation) && ((s & TypeFlags.Any) != 0)))))
        {
            return true;
        }
        if (((t & TypeFlags.Never) != 0))
        {
            return false;
        }
        if ((((s & TypeFlags.StringLike) != 0) && ((t & TypeFlags.String) != 0)))
        {
            return true;
        }
        if (((((((s & TypeFlags.StringLiteral) != 0) && ((s & TypeFlags.EnumLiteral) != 0)) && ((t & TypeFlags.StringLiteral) != 0)) && ((t & TypeFlags.EnumLiteral) == 0)) && (declarationConstantValuesEqual(source.AsLiteralType().value, target.AsLiteralType().value))))
        {
            return true;
        }
        if ((((s & TypeFlags.NumberLike) != 0) && ((t & TypeFlags.Number) != 0)))
        {
            return true;
        }
        if (((((((s & TypeFlags.NumberLiteral) != 0) && ((s & TypeFlags.EnumLiteral) != 0)) && ((t & TypeFlags.NumberLiteral) != 0)) && ((t & TypeFlags.EnumLiteral) == 0)) && (declarationConstantValuesEqual(source.AsLiteralType().value, target.AsLiteralType().value))))
        {
            return true;
        }
        if ((((s & TypeFlags.BigIntLike) != 0) && ((t & TypeFlags.BigInt) != 0)))
        {
            return true;
        }
        if ((((s & TypeFlags.BooleanLike) != 0) && ((t & TypeFlags.Boolean) != 0)))
        {
            return true;
        }
        if ((((s & TypeFlags.ESSymbolLike) != 0) && ((t & TypeFlags.ESSymbol) != 0)))
        {
            return true;
        }
        if ((((((s & TypeFlags.Enum) != 0) && ((t & TypeFlags.Enum) != 0)) && (source.symbol.Name == target.symbol.Name)) && this.isEnumTypeRelatedTo(source.symbol, target.symbol, errorReporter)))
        {
            return true;
        }
        if ((((s & TypeFlags.EnumLiteral) != 0) && ((t & TypeFlags.EnumLiteral) != 0)))
        {
            if (((((s & TypeFlags.Union) != 0) && ((t & TypeFlags.Union) != 0)) && this.isEnumTypeRelatedTo(source.symbol, target.symbol, errorReporter)))
            {
                return true;
            }
            if ((((((s & TypeFlags.Literal) != 0) && ((t & TypeFlags.Literal) != 0)) && (declarationConstantValuesEqual(source.AsLiteralType().value, target.AsLiteralType().value))) && this.isEnumTypeRelatedTo(source.symbol, target.symbol, errorReporter)))
            {
                return true;
            }
        }
        if ((((s & TypeFlags.Undefined) != 0) && (((!this.strictNullChecks && ((t & TypeFlags.UnionOrIntersection) == 0)) || ((t & ((TypeFlags.Undefined | TypeFlags.Void))) != 0)))))
        {
            return true;
        }
        if ((((s & TypeFlags.Null) != 0) && (((!this.strictNullChecks && ((t & TypeFlags.UnionOrIntersection) == 0)) || ((t & TypeFlags.Null) != 0)))))
        {
            return true;
        }
        if (((((s & TypeFlags.Object) != 0) && ((t & TypeFlags.NonPrimitive) != 0)) && !((((relation == this.strictSubtypeRelation) && this.IsEmptyAnonymousObjectType(source)) && ((source.objectFlags & ObjectFlags.FreshLiteral) == 0)))))
        {
            return true;
        }
        if (((relation == this.assignableRelation) || (relation == this.comparableRelation)))
        {
            if (((s & TypeFlags.Any) != 0))
            {
                return true;
            }
            if ((((s & TypeFlags.Number) != 0) && ((((t & TypeFlags.Enum) != 0) || (((t & TypeFlags.NumberLiteral) != 0) && ((t & TypeFlags.EnumLiteral) != 0))))))
            {
                return true;
            }
            if (((((s & TypeFlags.NumberLiteral) != 0) && ((s & TypeFlags.EnumLiteral) == 0)) && ((((t & TypeFlags.Enum) != 0) || ((((t & TypeFlags.NumberLiteral) != 0) && ((t & TypeFlags.EnumLiteral) != 0)) && (declarationConstantValuesEqual(source.AsLiteralType().value, target.AsLiteralType().value)))))))
            {
                return true;
            }
            if (this.isUnknownLikeUnionType(target))
            {
                return true;
            }
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
            if (headMessage != null && errorNode != null && result == Ternary.False && source.symbol != null && exportTypeLinks.Has(source.symbol))
            {
                var links = exportTypeLinks.Get(source.symbol);
                if (links.originatingImport != null && !Ast.IsImportCall(links.originatingImport))
                {
                    var helpfulRetry = checkTypeRelatedTo(getTypeOfSymbol(links.target), target, relation, null);
                    if (helpfulRetry)
                        r.relatedInfo = GoRuntime.Append(r.relatedInfo, createDiagnosticForNode(links.originatingImport, Diagnostics.Type_originates_at_this_import_A_namespace_style_import_cannot_be_called_or_constructed_and_will_cause_a_failure_at_runtime_Consider_using_a_default_import_or_import_require_here_instead));
                }
            }
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

    // relater.go:745/2859. JSX hyphenated names are permitted and excluded from index checks.
    private static bool isHyphenatedJsxName(string name) => name.Contains('-');
    private static bool isIgnoredJsxProperty(Type source, Symbol property) => (source.objectFlags & ObjectFlags.JsxAttributes) != 0 && isHyphenatedJsxName(property.Name);
    // relater.go:679. Substitutions retain their base type's weak-type status.
    private bool isWeakType(Type t)
    {
        if ((t.flags & TypeFlags.Object) != 0)
        {
            var resolved = resolveStructuredTypeMembers(t);
            return GoRuntime.Len(resolved.signatures) == 0 && (resolved.indexInfos?.Count ?? 0) == 0 && (resolved.properties?.Count ?? 0) > 0 && resolved.properties.All(p => (p.Flags & SymbolFlags.Optional) != 0);
        }

        if ((t.flags & TypeFlags.Substitution) != 0)
            return isWeakType(t.AsSubstitutionType().baseType);
        if ((t.flags & TypeFlags.Intersection) != 0)
            return t.Types().All(isWeakType);
        return false;
    }

    // relater.go:4656. Source-derived index inference, including reverse mappings.
    private bool isObjectTypeWithInferableIndex(Type t)
    {
        if ((t.flags & TypeFlags.Intersection) != 0)
            return t.Types().All(isObjectTypeWithInferableIndex);
        return t.symbol != null && (t.symbol.Flags & (SymbolFlags.ObjectLiteral | SymbolFlags.TypeLiteral | SymbolFlags.Enum | SymbolFlags.ValueModule)) != 0 && (t.symbol.Flags & SymbolFlags.Class) == 0 && !typeHasCallOrConstructSignatures(t) || (t.objectFlags & (ObjectFlags.JSLiteral | ObjectFlags.ObjectRestType)) != 0 || (t.objectFlags & ObjectFlags.ReverseMapped) != 0 && isObjectTypeWithInferableIndex(t.AsReverseMappedType().source);
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
        if ((targetType.flags & TypeFlags.Object) != 0)
        {
            if (getPropertyOfObjectType(targetType, name) != null || getApplicableIndexInfoForName(targetType, name) != null || isLateBoundName(name) && getIndexInfoOfType(targetType, stringType) != null || isComparingJsxAttributes && isHyphenatedJsxName(name))
                return true;
        }

        if ((targetType.flags & TypeFlags.Substitution) != 0)
            return isKnownProperty(targetType.AsSubstitutionType().baseType, name, isComparingJsxAttributes);
        if (isExcessPropertyCheckTarget(targetType) && (targetType.flags & TypeFlags.UnionOrIntersection) != 0)
            foreach (var t in targetType.Types())
                if (isKnownProperty(t, name, isComparingJsxAttributes))
                    return true;
        return false;
    }

    private static bool isExcessPropertyCheckTarget(Type type)
    {
        return (type.flags & TypeFlags.Object) != 0 && (type.objectFlags & ObjectFlags.ObjectLiteralPatternWithComputedProperties) == 0 || (type.flags & TypeFlags.NonPrimitive) != 0 || (type.flags & TypeFlags.Substitution) != 0 && isExcessPropertyCheckTarget(type.AsSubstitutionType().baseType) || (type.flags & TypeFlags.Union) != 0 && type.Types().Any(isExcessPropertyCheckTarget) || (type.flags & TypeFlags.Intersection) != 0 && type.Types().All(isExcessPropertyCheckTarget);
    }

    private readonly record struct RecursionId(object value);
    // relater.go:765. Descriptor/List adapters borrow the same stack storage;
    // the original recursion-identity algorithm does not mutate that storage.
    private bool isDeeplyNestedType(Type t, List<Type> stack, int maxDepth) =>
        isDeeplyNestedType(t, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(stack), maxDepth);
    private bool isDeeplyNestedType(Type t, Slice<Type> stack, int maxDepth) =>
        isDeeplyNestedType(t, stack.AsSpan(), maxDepth);
    private bool isDeeplyNestedType(Type t, ReadOnlySpan<Type> stack, int maxDepth)
    {
        if (stack.Length >= maxDepth)
        {
            var target = getRecursionIdentityTarget(t);
            if ((target.flags & TypeFlags.Intersection) != 0)
            {
                // A Span cannot be captured by a delegate. Use the original
                // Go constituent loop rather than materializing a collection.
                foreach (var part in target.Types())
                    if (isDeeplyNestedType(part, stack, maxDepth))
                        return true;
                return false;
            }
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
        if ((t.flags & TypeFlags.IndexedAccess) != 0)
            return getRecursionIdentityTarget(t.AsIndexedAccessType().objectType);
        if ((t.objectFlags & ObjectFlags.InstantiatedMapped) == ObjectFlags.InstantiatedMapped)
        {
            var target = t.checker.getModifiersTypeFromMappedType(t);
            if (target != null && (target.symbol != null || (target.flags & TypeFlags.Intersection) != 0 && Core.Some(target.Types(), part => part.symbol != null)))
                return getRecursionIdentityTarget(target);
        }

        return t;
    }

    private static RecursionId getRecursionIdentityFromTarget(Type t)
    {
        if ((t.flags & TypeFlags.Object) != 0 && (t.objectFlags & (ObjectFlags.ObjectLiteral | ObjectFlags.ArrayLiteral)) == 0)
        {
            if ((t.objectFlags & ObjectFlags.Reference) != 0 && t.AsTypeReference().node != null)
                return new(t.AsTypeReference().node);
            if (t.symbol != null && !((t.objectFlags & ObjectFlags.Anonymous) != 0 && (t.symbol.Flags & SymbolFlags.Class) != 0) && (t.objectFlags & ObjectFlags.FromTypeNode) == 0)
                return new(t.symbol);
            if (isTupleType(t) && (t.objectFlags & ObjectFlags.FromTypeNode) == 0)
                return new(t.Target());
        }

        if ((t.flags & TypeFlags.TypeParameter) != 0 && t.symbol != null)
            return new(t.symbol);
        if ((t.flags & TypeFlags.Conditional) != 0)
            return new(t.AsConditionalType().root.node.AsNode());
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
        // checker/relater.go:1044. Go ranges over nil for a never property type.
        public bool matches(int index, Type t)
        {
            foreach (var s in c.getTypeOfSymbol(props[index]).Distributed() ?? [])
                if (isRelatedTo(s, t) != Ternary.False)
                    return true;
            return false;
        }
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
        if (((source.flags & TypeFlags.Union) != 0))
        {
            {
                foreach (var t in GoRuntime.RangeValues(source.Types()))
                {
                    if (!containsType(target.Types(), t))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        if ((((source.flags & TypeFlags.EnumLike) != 0) && (this.getBaseTypeOfEnumLikeType(source) == target)))
        {
            return true;
        }
        return containsType(target.Types(), source);
    }

    // relater.go:2315. Combined constraints preserve intersection/union reduction.
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
        if ((source.objectFlags & ObjectFlags.ObjectLiteral) != 0 && someType(target, isArrayLikeType))
            return Core.Find(target.Types(), type => !isArrayLikeType(type));
        return null;
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
            // relater.go:981: static private names are not unmatched-property candidates.
            if (isStaticPrivateIdentifierProperty(targetProp))
                continue;
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
        return (this.compareSignaturesRelated(source, target, Core.IfElse(ignoreReturnTypes, SignatureCheckMode.IgnoreReturnTypes, SignatureCheckMode.None), false, null, this.compareTypesAssignable, null) != Ternary.False);
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
            target = getCanonicalSignature(target);
            source = instantiateSignatureInContextOf(source, target, null, compareTypes);
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
        if (((((GoRuntime.Len(s.typeParameters) == 0) && (((s.thisParameter == null) || IsTypeAny(this.getTypeOfParameter(s.thisParameter))))) && (GoRuntime.Len(s.parameters) == 1)) && signatureHasRestParameter(s)))
        {
            var paramType = this.getTypeOfParameter(s.parameters[0]);
            Type restType = default;
            if (this.isArrayType(paramType))
            {
                restType = this.getTypeArguments(paramType)[0];
            }
            else
            {
                restType = paramType;
            }

            return (((restType.flags & ((TypeFlags.Any | TypeFlags.Never))) != 0) && ((this.getReturnTypeOfSignature(s).flags & TypeFlags.AnyOrUnknown) != 0));
        }

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
        {
            var restType = this.getTypeOfSymbol(signature.parameters[(length - 1)]);
            if (isTupleType(restType))
            {
                return ((length + restType.TargetTupleType().fixedLength) - Core.IfElse(((restType.TargetTupleType().combinedFlags & ElementFlags.Variable) != 0), 0, 1));
            }
        }

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
            {
                var restType = this.getTypeOfSymbol(signature.parameters[(GoRuntime.Len(signature.parameters) - 1)]);
                if (isTupleType(restType))
                {
                    var firstOptionalIndex = Core.FindIndex(restType.TargetTupleType().elementInfos, (info) =>
                    {
                        return ((info.flags & ElementFlags.Required) == 0);
                    });
                    var requiredCount = firstOptionalIndex;
                    if ((firstOptionalIndex < 0))
                    {
                        requiredCount = restType.TargetTupleType().fixedLength;
                    }

                    if ((requiredCount > 0))
                    {
                        minArgumentCount = ((GoRuntime.Len(signature.parameters) - 1) + requiredCount);
                    }
                }
            }

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
        {
            var restType = this.getTypeOfSymbol(signature.parameters[(GoRuntime.Len(signature.parameters) - 1)]);
            return (!isTupleType(restType) || ((restType.TargetTupleType().combinedFlags & ElementFlags.Variable) != 0));
        }

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
        var paramCount = (GoRuntime.Len(signature.parameters) - Core.IfElse(signatureHasRestParameter(signature), 1, 0));
        if ((pos < paramCount))
        {
            return this.getTypeOfParameter(signature.parameters[pos]);
        }

        if (signatureHasRestParameter(signature))
        {
            var restType = this.getTypeOfSymbol(signature.parameters[paramCount]);
            var index = (pos - paramCount);
            if (((!isTupleType(restType) || ((restType.TargetTupleType().combinedFlags & ElementFlags.Variable) != 0)) || (index < restType.TargetTupleType().fixedLength)))
            {
                return this.getIndexedAccessType(restType, this.getNumberLiteralType(new JsNum.Number(index)));
            }
        }

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
                                    sig.resolvedTypePredicate = createTypePredicateFromTypePredicateNode(typeNode, sig);
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
            var mapper = newTypeMapper(source.typeParameters, target.typeParameters);
            {
                foreach (var i in GoRuntime.RangeKeys(GoRuntime.Len(target.typeParameters)))
                {
                    var s2 = source.typeParameters[i];
                    var t2 = target.typeParameters[i];
                    if (!(((s2 == t2) || ((compareTypes(this.instantiateType(this.getConstraintOrUnknownFromTypeParameter(s2), mapper), this.getConstraintOrUnknownFromTypeParameter(t2)) != Ternary.False) && (compareTypes(this.instantiateType(this.getDefaultOrUnknownFromTypeParameter(s2), mapper), this.getDefaultOrUnknownFromTypeParameter(t2)) != Ternary.False)))))
                    {
                        return Ternary.False;
                    }
                }
            }

            source = this.instantiateSignatureEx(source, mapper, true);
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

        var mapper = newTypeMapper(targetParams, sourceParams);
        {
            foreach (var i in GoRuntime.RangeKeys(sourceParams))
            {
                var source = sourceParams[i];
                var target = targetParams[i];
                if ((source == target))
                {
                    continue;
                }

                if (!this.isTypeIdenticalTo(Core.OrElse(this.getConstraintFromTypeParameter(source), this.unknownType), this.instantiateType(Core.OrElse(this.getConstraintFromTypeParameter(target), this.unknownType), mapper)))
                {
                    return false;
                }
            }
        }

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

    // relater.go:1891/1900. Distinguish effective tuple/rest types from array rests.
    private Type getNonArrayRestType(Signature signature)
    {
        var restType = this.getEffectiveRestType(signature);
        if ((((restType != null) && !this.isArrayType(restType)) && !IsTypeAny(restType)))
        {
            return restType;
        }

        return null;
    }

    private Type getEffectiveRestType(Signature signature)
    {
        if (signatureHasRestParameter(signature))
        {
            var restType = this.getTypeOfSymbol(signature.parameters[(GoRuntime.Len(signature.parameters) - 1)]);
            if (!isTupleType(restType))
            {
                if (IsTypeAny(restType))
                {
                    return this.anyArrayType;
                }

                return restType;
            }

            if (((restType.TargetTupleType().combinedFlags & ElementFlags.Variable) != 0))
            {
                return this.sliceTupleType(restType, restType.TargetTupleType().fixedLength, 0);
            }
        }

        return null;
    }

    // relater.go:1815. Reached only for effective nonarray rest parameters.
    // relater.go:1962. The admitted ordinary parameter arm preserves raw names.
    private string getParameterNameAtPosition(Signature signature, int pos)
    {
        var paramCount = (GoRuntime.Len(signature.parameters) - Core.IfElse(signatureHasRestParameter(signature), 1, 0));
        if ((pos < paramCount))
        {
            return signature.parameters[pos].Name;
        }

        var restParameter = signature.parameters[paramCount];
        var restType = this.getTypeOfSymbol(restParameter);
        if (isTupleType(restType))
        {
            var index = (pos - paramCount);
            return this.getTupleElementLabel(restType.TargetTupleType().elementInfos[index], restParameter, index);
        }

        return restParameter.Name;
    }

    private sealed partial class Relater
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
        internal bool VerificationOwnedBy(Checker checker) => ReferenceEquals(c, checker);
        internal Ternary isRelatedToSimple(Type source, Type target) => isRelatedToEx(source, target, RecursionFlags.Both, false, null, IntersectionState.None);
        internal Ternary isRelatedToWorker(Type source, Type target, bool reportErrors) => isRelatedToEx(source, target, RecursionFlags.Both, reportErrors, null, IntersectionState.None);
        private Ternary isRelatedTo(Type source, Type target, RecursionFlags recursionFlags, bool reportErrors) => isRelatedToEx(source, target, recursionFlags, reportErrors, null, IntersectionState.None);
        internal Ternary isRelatedToEx(Type originalSource, Type originalTarget, RecursionFlags recursionFlags, bool reportErrors, Message headMessage, IntersectionState intersectionState)
        {
            if (originalSource == originalTarget)
                return Ternary.True;
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
                        // relater.go:2729. Only the first call/construct return is
                        // checked, using this relation and source-only recursion.
                        if ((calls?.Count ?? 0) > 0 && isRelatedTo(c.getReturnTypeOfSignature(calls[0]), target, RecursionFlags.Source, false) != Ternary.False ||
                            (constructs?.Count ?? 0) > 0 && isRelatedTo(c.getReturnTypeOfSignature(constructs[0]), target, RecursionFlags.Source, false) != Ternary.False)
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
                if (shouldCheckAsExcessProperty(prop, source.symbol) && !isIgnoredJsxProperty(source, prop))
                {
                    if (!c.isKnownProperty(reducedTarget, prop.Name, isComparingJsxAttributes))
                    {
                        if (reportErrors)
                        {
                            var errorTarget = c.filterType(reducedTarget, isExcessPropertyCheckTarget);
                            if (errorNode == null)
                                throw new InvalidOperationException("No errorNode in hasExcessProperties");
                            if (Ast.IsJsxAttributes(errorNode) || Ast.IsJsxOpeningLikeElement(errorNode) || Ast.IsJsxOpeningLikeElement(errorNode.Parent))
                            {
                                if (prop.ValueDeclaration != null && Ast.IsJsxAttribute(prop.ValueDeclaration) && Ast.GetSourceFileOfNode(errorNode) == Ast.GetSourceFileOfNode(prop.ValueDeclaration.Name()))
                                    errorNode = prop.ValueDeclaration.Name();
                                var propName = c.symbolToString(prop);
                                var suggestionSymbol = c.getSuggestedSymbolForNonexistentJSXAttribute(propName, errorTarget);
                                if (suggestionSymbol != null)
                                    reportError(Diagnostics.Property_0_does_not_exist_on_type_1_Did_you_mean_2, propName, c.typeToStringByteString(errorTarget), c.symbolToString(suggestionSymbol));
                                else
                                    reportError(Diagnostics.Property_0_does_not_exist_on_type_1, propName, c.typeToStringByteString(errorTarget));
                            }
                            else
                            {
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
            Ternary result = default;
            bool varianceCheckFailed = default;
            ErrorChain originalErrorChain = default;
            var saveErrorState = this.getErrorState();
            Func<List<Type>, List<Type>, List<VarianceFlags>, IntersectionState, (Ternary, bool)> relateVariances = (sourceTypeArguments, targetTypeArguments, variances, intersectionState2) =>
            {
                {
                    result = this.typeArgumentsRelatedTo(sourceTypeArguments, targetTypeArguments, variances, reportErrors, intersectionState2);
                    if ((result != Ternary.False))
                    {
                        return (result, true);
                    }
                }

                if (Core.Some(variances, (v) =>
                {
                    return ((v & VarianceFlags.AllowsStructuralFallback) != 0);
                }))
                {
                    originalErrorChain = null;
                    this.restoreErrorState(saveErrorState);
                    return (Ternary.False, false);
                }

                var allowStructuralFallback = this.c.hasCovariantVoidArgument(targetTypeArguments, variances);
                varianceCheckFailed = !allowStructuralFallback;
                if (((GoRuntime.Len(variances) != 0) && !allowStructuralFallback))
                {
                    if ((varianceCheckFailed && !((reportErrors && Core.Some(variances, (v2) =>
                    {
                        return (((v2 & VarianceFlags.VarianceMask)) == VarianceFlags.Invariant);
                    })))))
                    {
                        return (Ternary.False, true);
                    }

                    originalErrorChain = this.errorChain;
                    this.restoreErrorState(saveErrorState);
                }

                return (Ternary.False, false);
            };
            switch (true)
            {
                case true when (this.relation == this.c.identityRelation):
                {
                    switch (true)
                    {
                        case true when ((source.flags & TypeFlags.UnionOrIntersection) != 0):
                        {
                            var result2 = this.eachTypeRelatedToSomeType(source, target);
                            if ((result2 != Ternary.False))
                            {
                                result2 &= this.eachTypeRelatedToSomeType(target, source);
                            }

                            return result2;
                        }

                        case true when ((source.flags & TypeFlags.Index) != 0):
                        {
                            return this.isRelatedTo(source.Target(), target.Target(), RecursionFlags.Both, false);
                        }

                        case true when ((source.flags & TypeFlags.IndexedAccess) != 0):
                        {
                            result = this.isRelatedTo(source.AsIndexedAccessType().objectType, target.AsIndexedAccessType().objectType, RecursionFlags.Both, false);
                            if ((result != Ternary.False))
                            {
                                result &= this.isRelatedTo(source.AsIndexedAccessType().indexType, target.AsIndexedAccessType().indexType, RecursionFlags.Both, false);
                                if ((result != Ternary.False))
                                {
                                    return result;
                                }
                            }

                            break;
                        }

                        case true when ((source.flags & TypeFlags.Conditional) != 0):
                        {
                            if ((source.AsConditionalType().root.isDistributive == target.AsConditionalType().root.isDistributive))
                            {
                                result = this.isRelatedTo(source.AsConditionalType().checkType, target.AsConditionalType().checkType, RecursionFlags.Both, false);
                                if ((result != Ternary.False))
                                {
                                    result &= this.isRelatedTo(source.AsConditionalType().extendsType, target.AsConditionalType().extendsType, RecursionFlags.Both, false);
                                    if ((result != Ternary.False))
                                    {
                                        result &= this.isRelatedTo(this.c.getTrueTypeFromConditionalType(source), this.c.getTrueTypeFromConditionalType(target), RecursionFlags.Both, false);
                                        if ((result != Ternary.False))
                                        {
                                            result &= this.isRelatedTo(this.c.getFalseTypeFromConditionalType(source), this.c.getFalseTypeFromConditionalType(target), RecursionFlags.Both, false);
                                            if ((result != Ternary.False))
                                            {
                                                return result;
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                        }

                        case true when ((source.flags & TypeFlags.Substitution) != 0):
                        {
                            result = this.isRelatedTo(source.AsSubstitutionType().baseType, target.AsSubstitutionType().baseType, RecursionFlags.Both, false);
                            if ((result != Ternary.False))
                            {
                                result &= this.isRelatedTo(source.AsSubstitutionType().constraint, target.AsSubstitutionType().constraint, RecursionFlags.Both, false);
                                if ((result != Ternary.False))
                                {
                                    return result;
                                }
                            }

                            break;
                        }

                        case true when ((source.flags & TypeFlags.TemplateLiteral) != 0):
                        {
                            if (GoSlices.Equal(source.AsTemplateLiteralType().texts, target.AsTemplateLiteralType().texts))
                            {
                                result = Ternary.True;
                                var sourceTypes = source.AsTemplateLiteralType().types;
                                for (int templateIndex = 0; templateIndex < sourceTypes.Count; templateIndex++)
                                {
                                    result &= isRelatedTo(sourceTypes[templateIndex], target.AsTemplateLiteralType().types[templateIndex], RecursionFlags.Both, false);
                                    if (result == Ternary.False)
                                        return result;
                                }

                                return result;
                            }

                            break;
                        }

                        case true when ((source.flags & TypeFlags.StringMapping) != 0):
                        {
                            if (source.symbol == target.symbol)
                                return isRelatedTo(source.AsStringMappingType().target, target.AsStringMappingType().target, RecursionFlags.Both, false);
                            break;
                        }
                    }

                    if (((source.flags & TypeFlags.Object) == 0))
                    {
                        return Ternary.False;
                    }

                    break;
                }

                case true when (((source.flags & TypeFlags.UnionOrIntersection) != 0) || ((target.flags & TypeFlags.UnionOrIntersection) != 0)):
                {
                    result = this.unionOrIntersectionRelatedTo(source, target, reportErrors, intersectionState);
                    if ((result != Ternary.False))
                    {
                        return result;
                    }

                    if (!(((((source.flags & TypeFlags.Instantiable) != 0) || (((source.flags & TypeFlags.Object) != 0) && ((target.flags & TypeFlags.Union) != 0))) || (((source.flags & TypeFlags.Intersection) != 0) && ((target.flags & (((TypeFlags.Object | TypeFlags.Union) | TypeFlags.Instantiable))) != 0)))))
                    {
                        return Ternary.False;
                    }

                    break;
                }
            }

            if ((((((((source.flags & ((TypeFlags.Object | TypeFlags.Conditional))) != 0) && (source.alias != null)) && (GoRuntime.Len(source.alias.typeArguments) != 0)) && (target.alias != null)) && (source.alias.symbol == target.alias.symbol)) && !((this.c.isMarkerType(source) || this.c.isMarkerType(target)))))
            {
                var variances2 = this.c.getAliasVariances(source.alias.symbol);
                if ((GoRuntime.Len(variances2) == 0))
                {
                    return Ternary.Unknown;
                }

                var @params = this.c.typeAliasLinks.Get(source.alias.symbol).typeParameters;
                var minParams = this.c.getMinTypeArgumentCount(@params);
                var nodeIsInJsFile = Ast.IsInJSFile(source.alias.symbol.ValueDeclaration);
                var sourceTypes = this.c.fillMissingTypeArguments(source.alias.typeArguments, @params, minParams, nodeIsInJsFile);
                var targetTypes = this.c.fillMissingTypeArguments(target.alias.typeArguments, @params, minParams, nodeIsInJsFile);
                var tuple2 = relateVariances(sourceTypes, targetTypes, variances2, intersectionState);
                var varianceResult = tuple2.Item1;
                var ok = tuple2.Item2;
                if (ok)
                {
                    return varianceResult;
                }
            }

            if ((isSingleElementGenericTupleType(source) && !source.TargetTupleType().@readonly))
            {
                result = this.isRelatedTo(this.c.getTypeArguments(source)[0], target, RecursionFlags.Source, false);
                if ((result != Ternary.False))
                {
                    return result;
                }
            }

            if ((isSingleElementGenericTupleType(target) && ((target.TargetTupleType().@readonly || this.c.isMutableArrayOrTuple(this.c.getBaseConstraintOrType(source))))))
            {
                result = this.isRelatedTo(source, this.c.getTypeArguments(target)[0], RecursionFlags.Target, false);
                if ((result != Ternary.False))
                {
                    return result;
                }
            }

            switch (true)
            {
                case true when ((target.flags & TypeFlags.TypeParameter) != 0):
                {
                    if (((((source.objectFlags & ObjectFlags.Mapped) != 0) && (source.AsMappedType().declaration.NameType == null)) && (this.isRelatedTo(this.c.getIndexType(target), this.c.getConstraintTypeFromMappedType(source), RecursionFlags.Both, false) != Ternary.False)))
                    {
                        if (((getMappedTypeModifiers(source) & MappedTypeModifiers.IncludeOptional) == 0))
                        {
                            var templateType = this.c.getTemplateTypeFromMappedType(source);
                            var indexedAccessType = this.c.getIndexedAccessType(target, this.c.getTypeParameterFromMappedType(source));
                            result = this.isRelatedTo(templateType, indexedAccessType, RecursionFlags.Both, reportErrors);
                            if ((result != Ternary.False))
                            {
                                return result;
                            }
                        }
                    }

                    if (((this.relation == this.c.comparableRelation) && ((source.flags & TypeFlags.TypeParameter) != 0)))
                    {
                        {
                            var constraint = this.c.getConstraintOfTypeParameter(source);
                            if (((constraint != null) && someType(constraint, (c) =>
                            {
                                return ((c.flags & TypeFlags.TypeParameter) != 0);
                            })))
                            {
                                return this.isRelatedTo(constraint, target, RecursionFlags.Source, false);
                            }
                        }

                        return Ternary.False;
                    }

                    break;
                }

                case true when ((target.flags & TypeFlags.IndexedAccess) != 0):
                {
                    if (((source.flags & TypeFlags.IndexedAccess) != 0))
                    {
                        result = this.isRelatedTo(source.AsIndexedAccessType().objectType, target.AsIndexedAccessType().objectType, RecursionFlags.Both, reportErrors);
                        if ((result != Ternary.False))
                        {
                            result &= this.isRelatedTo(source.AsIndexedAccessType().indexType, target.AsIndexedAccessType().indexType, RecursionFlags.Both, reportErrors);
                        }

                        if ((result != Ternary.False))
                        {
                            return result;
                        }

                        if (reportErrors)
                        {
                            originalErrorChain = this.errorChain;
                        }
                    }

                    if (((this.relation == this.c.assignableRelation) || (this.relation == this.c.comparableRelation)))
                    {
                        var objectType = target.AsIndexedAccessType().objectType;
                        var indexType = target.AsIndexedAccessType().indexType;
                        var baseObjectType = this.c.getBaseConstraintOrType(objectType);
                        var baseIndexType = this.c.getBaseConstraintOrType(indexType);
                        if ((!this.c.isGenericObjectType(baseObjectType) && !this.c.isGenericIndexType(baseIndexType)))
                        {
                            var accessFlags = (AccessFlags.Writing | Core.IfElse((baseObjectType != objectType), AccessFlags.NoIndexSignatures, AccessFlags.None));
                            var constraint2 = this.c.getIndexedAccessTypeOrUndefined(baseObjectType, baseIndexType, accessFlags, null, null);
                            if ((constraint2 != null))
                            {
                                if ((reportErrors && (originalErrorChain != null)))
                                {
                                    this.restoreErrorState(saveErrorState);
                                }

                                result = this.isRelatedToEx(source, constraint2, RecursionFlags.Target, reportErrors, null, intersectionState);
                                if ((result != Ternary.False))
                                {
                                    return result;
                                }

                                if (((reportErrors && (originalErrorChain != null)) && (this.errorChain != null)))
                                {
                                    if ((chainDepth(originalErrorChain) <= chainDepth(this.errorChain)))
                                    {
                                        this.errorChain = originalErrorChain;
                                    }
                                }
                            }
                        }
                    }

                    if (reportErrors)
                    {
                        originalErrorChain = null;
                    }

                    break;
                }

                case true when ((target.flags & TypeFlags.Index) != 0):
                {
                    var targetType2 = target.AsIndexType().target;
                    if (((source.flags & TypeFlags.Index) != 0))
                    {
                        result = this.isRelatedTo(targetType2, source.AsIndexType().target, RecursionFlags.Both, false);
                        if ((result != Ternary.False))
                        {
                            return result;
                        }
                    }

                    if (isTupleType(targetType2))
                    {
                        result = this.isRelatedTo(source, this.c.getKnownKeysOfTupleType(targetType2), RecursionFlags.Target, reportErrors);
                        if ((result != Ternary.False))
                        {
                            return result;
                        }
                    }
                    else
                    {
                        var constraint3 = this.c.getSimplifiedTypeOrConstraint(targetType2);
                        if ((constraint3 != null))
                        {
                            if ((this.isRelatedTo(source, this.c.getIndexTypeEx(constraint3, (target.AsIndexType().indexFlags | IndexFlags.NoReducibleCheck)), RecursionFlags.Target, reportErrors) == Ternary.True))
                            {
                                return Ternary.True;
                            }
                        }
                        else
                        {
                            if (this.c.isGenericMappedType(targetType2))
                            {
                                var nameType = this.c.getNameTypeFromMappedType(targetType2);
                                var constraintType = this.c.getConstraintTypeFromMappedType(targetType2);
                                Type targetKeys = default;
                                if (((nameType != null) && this.c.isMappedTypeWithKeyofConstraintDeclaration(targetType2)))
                                {
                                    var mappedKeys = this.c.getApparentMappedTypeKeys(nameType, targetType2);
                                    targetKeys = this.c.getUnionType(new List<Type> { mappedKeys, nameType });
                                }
                                else
                                {
                                    if ((nameType != null))
                                    {
                                        targetKeys = nameType;
                                    }
                                    else
                                    {
                                        targetKeys = constraintType;
                                    }
                                }

                                if ((this.isRelatedTo(source, targetKeys, RecursionFlags.Target, reportErrors) == Ternary.True))
                                {
                                    return Ternary.True;
                                }
                            }
                        }
                    }

                    break;
                }

                case true when ((target.flags & TypeFlags.Conditional) != 0):
                {
                    if (this.c.isDeeplyNestedType(target, this.targetStack, 10))
                    {
                        return Ternary.Maybe;
                    }

                    var c2 = target.AsConditionalType();
                    if ((((c2.root.inferTypeParameters == null) && !this.c.isDistributionDependent(c2.root)) && !((((source.flags & TypeFlags.Conditional) != 0) && (source.AsConditionalType().root == c2.root)))))
                    {
                        var skipTrue = !this.c.isTypeAssignableTo(this.c.getPermissiveInstantiation(c2.checkType), this.c.getPermissiveInstantiation(c2.extendsType));
                        var skipFalse = (!skipTrue && this.c.isTypeAssignableTo(this.c.getRestrictiveInstantiation(c2.checkType), this.c.getRestrictiveInstantiation(c2.extendsType)));
                        if (skipTrue)
                        {
                            result = Ternary.True;
                        }
                        else
                        {
                            result = this.isRelatedToEx(source, this.c.getTrueTypeFromConditionalType(target), RecursionFlags.Target, false, null, intersectionState);
                        }

                        if ((result != Ternary.False))
                        {
                            if (skipFalse)
                            {
                                result &= Ternary.True;
                            }
                            else
                            {
                                result &= this.isRelatedToEx(source, this.c.getFalseTypeFromConditionalType(target), RecursionFlags.Target, false, null, intersectionState);
                            }

                            if ((result != Ternary.False))
                            {
                                return result;
                            }
                        }
                    }

                    break;
                }

                case true when ((target.flags & TypeFlags.TemplateLiteral) != 0):
                {
                    if ((source.flags & TypeFlags.TemplateLiteral) != 0)
                    {
                        if (relation == c.comparableRelation)
                            return c.templateLiteralTypesDefinitelyUnrelated(source.AsTemplateLiteralType(), target.AsTemplateLiteralType()) ? Ternary.False : Ternary.True;
                        c.instantiateType(source, c.reportUnreliableMapper);
                    }

                    if (c.isTypeMatchedByTemplateLiteralType(source, target.AsTemplateLiteralType(), isRelatedToWorker))
                        return Ternary.True;
                    break;
                }

                case true when ((target.flags & TypeFlags.StringMapping) != 0):
                {
                    if ((source.flags & TypeFlags.StringMapping) == 0 && c.isMemberOfStringMapping(source, target))
                        return Ternary.True;
                    break;
                }

                case true when (this.c.isGenericMappedType(target) && (this.relation != this.c.identityRelation)):
                {
                    var keysRemapped = (target.AsMappedType().declaration.NameType != null);
                    var templateType2 = this.c.getTemplateTypeFromMappedType(target);
                    var modifiers = getMappedTypeModifiers(target);
                    if (((modifiers & MappedTypeModifiers.ExcludeOptional) == 0))
                    {
                        if ((((!keysRemapped && ((templateType2.flags & TypeFlags.IndexedAccess) != 0)) && (templateType2.AsIndexedAccessType().objectType == source)) && (templateType2.AsIndexedAccessType().indexType == this.c.getTypeParameterFromMappedType(target))))
                        {
                            return Ternary.True;
                        }

                        if (!this.c.isGenericMappedType(source))
                        {
                            Type targetKeys2 = default;
                            if (keysRemapped)
                            {
                                targetKeys2 = this.c.getNameTypeFromMappedType(target);
                            }
                            else
                            {
                                targetKeys2 = this.c.getConstraintTypeFromMappedType(target);
                            }

                            var sourceKeys = this.c.getIndexTypeEx(source, IndexFlags.NoIndexSignatures);
                            var includeOptional = ((modifiers & MappedTypeModifiers.IncludeOptional) != 0);
                            Type filteredByApplicability = default;
                            if (includeOptional)
                            {
                                filteredByApplicability = this.c.intersectTypes(targetKeys2, sourceKeys);
                            }

                            if (((includeOptional && ((filteredByApplicability.flags & TypeFlags.Never) == 0)) || (!includeOptional && (this.isRelatedTo(targetKeys2, sourceKeys, RecursionFlags.Both, false) != Ternary.False))))
                            {
                                var templateType3 = this.c.getTemplateTypeFromMappedType(target);
                                var typeParameter = this.c.getTypeParameterFromMappedType(target);
                                var nonNullComponent = this.c.extractTypesOfKind(templateType3, ~TypeFlags.Nullable);
                                if (((!keysRemapped && ((nonNullComponent.flags & TypeFlags.IndexedAccess) != 0)) && (nonNullComponent.AsIndexedAccessType().indexType == typeParameter)))
                                {
                                    result = this.isRelatedTo(source, nonNullComponent.AsIndexedAccessType().objectType, RecursionFlags.Target, reportErrors);
                                    if ((result != Ternary.False))
                                    {
                                        return result;
                                    }
                                }
                                else
                                {
                                    var indexingType = typeParameter;
                                    switch (true)
                                    {
                                        case true when keysRemapped:
                                        {
                                            indexingType = Core.OrElse(filteredByApplicability, targetKeys2);
                                            break;
                                        }

                                        case true when (filteredByApplicability != null):
                                        {
                                            indexingType = this.c.getIntersectionType(new List<Type> { filteredByApplicability, typeParameter });
                                            break;
                                        }
                                    }

                                    var indexedAccessType2 = this.c.getIndexedAccessType(source, indexingType);
                                    result = this.isRelatedTo(indexedAccessType2, templateType3, RecursionFlags.Both, reportErrors);
                                    if ((result != Ternary.False))
                                    {
                                        return result;
                                    }
                                }
                            }

                            originalErrorChain = this.errorChain;
                            this.restoreErrorState(saveErrorState);
                        }
                    }

                    break;
                }
            }

            switch (true)
            {
                case true when ((source.flags & TypeFlags.TypeVariable) != 0):
                {
                    if ((((source.flags & TypeFlags.IndexedAccess) == 0) || ((target.flags & TypeFlags.IndexedAccess) == 0)))
                    {
                        var constraint4 = this.c.getConstraintOfType(source);
                        if ((constraint4 == null))
                        {
                            constraint4 = this.c.unknownType;
                        }

                        result = this.isRelatedToEx(constraint4, target, RecursionFlags.Source, false, null, intersectionState);
                        if ((result != Ternary.False))
                        {
                            return result;
                        }

                        var constraintWithThis = this.c.getTypeWithThisArgument(constraint4, source, false);
                        result = this.isRelatedToEx(constraintWithThis, target, RecursionFlags.Source, ((reportErrors && (constraint4 != this.c.unknownType)) && (((target.flags & source.flags) & TypeFlags.TypeParameter) == 0)), null, intersectionState);
                        if ((result != Ternary.False))
                        {
                            return result;
                        }

                        if (this.c.isMappedTypeGenericIndexedAccess(source))
                        {
                            var indexConstraint = this.c.getConstraintOfType(source.AsIndexedAccessType().indexType);
                            if ((indexConstraint != null))
                            {
                                result = this.isRelatedTo(this.c.getIndexedAccessType(source.AsIndexedAccessType().objectType, indexConstraint), target, RecursionFlags.Source, reportErrors);
                                if ((result != Ternary.False))
                                {
                                    return result;
                                }
                            }
                        }
                    }

                    break;
                }

                case true when ((source.flags & TypeFlags.Index) != 0):
                {
                    var isDeferredMappedIndex = (this.c.shouldDeferIndexType(source.AsIndexType().target, source.AsIndexType().indexFlags) && ((source.AsIndexType().target.objectFlags & ObjectFlags.Mapped) != 0));
                    result = this.isRelatedTo(this.c.stringNumberSymbolType, target, RecursionFlags.Source, (reportErrors && !isDeferredMappedIndex));
                    if ((result != Ternary.False))
                    {
                        return result;
                    }

                    if (isDeferredMappedIndex)
                    {
                        var mappedType = source.AsIndexType().target;
                        var nameType2 = this.c.getNameTypeFromMappedType(mappedType);
                        Type sourceMappedKeys = default;
                        if (((nameType2 != null) && this.c.isMappedTypeWithKeyofConstraintDeclaration(mappedType)))
                        {
                            sourceMappedKeys = this.c.getApparentMappedTypeKeys(nameType2, mappedType);
                        }
                        else
                        {
                            if ((nameType2 != null))
                            {
                                sourceMappedKeys = nameType2;
                            }
                            else
                            {
                                sourceMappedKeys = this.c.getConstraintTypeFromMappedType(mappedType);
                            }
                        }

                        result = this.isRelatedTo(sourceMappedKeys, target, RecursionFlags.Source, reportErrors);
                        if ((result != Ternary.False))
                        {
                            return result;
                        }
                    }

                    break;
                }

                case true when ((source.flags & TypeFlags.Conditional) != 0):
                {
                    if (this.c.isDeeplyNestedType(source, this.sourceStack, 10))
                    {
                        return Ternary.Maybe;
                    }

                    if (((target.flags & TypeFlags.Conditional) != 0))
                    {
                        var sourceParams = source.AsConditionalType().root.inferTypeParameters;
                        var sourceExtends = source.AsConditionalType().extendsType;
                        TypeMapper mapper = default;
                        if ((GoRuntime.Len(sourceParams) != 0))
                        {
                            var ctx = this.c.newInferenceContext(sourceParams, null, InferenceFlags.None, this.isRelatedToWorker);
                            this.c.inferTypes(ctx.inferences, target.AsConditionalType().extendsType, sourceExtends, (InferencePriority.NoConstraints | InferencePriority.AlwaysStrict), false);
                            sourceExtends = this.c.instantiateType(sourceExtends, ctx.mapper);
                            mapper = ctx.mapper;
                        }

                        if ((this.c.isTypeIdenticalTo(sourceExtends, target.AsConditionalType().extendsType) && (((this.isRelatedTo(source.AsConditionalType().checkType, target.AsConditionalType().checkType, RecursionFlags.Both, false) != 0) || (this.isRelatedTo(target.AsConditionalType().checkType, source.AsConditionalType().checkType, RecursionFlags.Both, false) != 0)))))
                        {
                            result = this.isRelatedTo(this.c.instantiateType(this.c.getTrueTypeFromConditionalType(source), mapper), this.c.getTrueTypeFromConditionalType(target), RecursionFlags.Both, reportErrors);
                            if ((result != Ternary.False))
                            {
                                result &= this.isRelatedTo(this.c.getFalseTypeFromConditionalType(source), this.c.getFalseTypeFromConditionalType(target), RecursionFlags.Both, reportErrors);
                            }

                            if ((result != Ternary.False))
                            {
                                return result;
                            }
                        }
                    }

                    var defaultConstraint = this.c.getDefaultConstraintOfConditionalType(source);
                    if ((defaultConstraint != null))
                    {
                        result = this.isRelatedTo(defaultConstraint, target, RecursionFlags.Source, reportErrors);
                        if ((result != Ternary.False))
                        {
                            return result;
                        }
                    }

                    if ((((target.flags & TypeFlags.Conditional) == 0) && this.c.hasNonCircularBaseConstraint(source)))
                    {
                        var distributiveConstraint = this.c.getConstraintOfDistributiveConditionalType(source);
                        if ((distributiveConstraint != null))
                        {
                            this.restoreErrorState(saveErrorState);
                            result = this.isRelatedTo(distributiveConstraint, target, RecursionFlags.Source, reportErrors);
                            if ((result != Ternary.False))
                            {
                                return result;
                            }
                        }
                    }

                    break;
                }

                case true when (((source.flags & TypeFlags.TemplateLiteral) != 0) && ((target.flags & TypeFlags.Object) == 0)):
                {
                    if ((target.flags & TypeFlags.TemplateLiteral) == 0)
                    {
                        var constraint = c.getBaseConstraintOfType(source);
                        if (constraint != null && constraint != source)
                        {
                            result = isRelatedTo(constraint, target, RecursionFlags.Source, reportErrors);
                            if (result != Ternary.False)
                                return result;
                        }
                    }

                    break;
                }

                case true when ((source.flags & TypeFlags.StringMapping) != 0):
                {
                    if ((target.flags & TypeFlags.StringMapping) != 0)
                    {
                        if (source.symbol != target.symbol)
                            return Ternary.False;
                        result = isRelatedTo(source.AsStringMappingType().target, target.AsStringMappingType().target, RecursionFlags.Both, reportErrors);
                        if (result != Ternary.False)
                            return result;
                    }
                    else
                    {
                        var constraint = c.getBaseConstraintOfType(source);
                        if (constraint != null)
                        {
                            result = isRelatedTo(constraint, target, RecursionFlags.Source, reportErrors);
                            if (result != Ternary.False)
                                return result;
                        }
                    }
                    break;
                }

                default:
                {
                    if (((((this.relation != this.c.subtypeRelation) && (this.relation != this.c.strictSubtypeRelation)) && isPartialMappedType(target)) && this.c.isEmptyObjectType(source)))
                    {
                        return Ternary.True;
                    }

                    if (this.c.isGenericMappedType(target))
                    {
                        if (this.c.isGenericMappedType(source))
                        {
                            result = this.mappedTypeRelatedTo(source, target, reportErrors);
                            if ((result != Ternary.False))
                            {
                                return result;
                            }
                        }

                        return Ternary.False;
                    }

                    var sourceIsPrimitive = ((source.flags & TypeFlags.Primitive) != 0);
                    if ((this.relation != this.c.identityRelation))
                    {
                        source = this.c.getApparentType(source);
                    }
                    else
                    {
                        if (this.c.isGenericMappedType(source))
                        {
                            return Ternary.False;
                        }
                    }

                    switch (true)
                    {
                        case true when (((((((source.objectFlags & ObjectFlags.Reference) != 0) && ((target.objectFlags & ObjectFlags.Reference) != 0)) && (source.Target() == target.Target())) && !isTupleType(source)) && !this.c.isMarkerType(source)) && !this.c.isMarkerType(target)):
                        {
                            if (this.c.isEmptyArrayLiteralType(source))
                            {
                                return Ternary.True;
                            }

                            var variances3 = this.c.getVariances(source.Target());
                            if ((GoRuntime.Len(variances3) == 0))
                            {
                                return Ternary.Unknown;
                            }

                            var tuple3 = relateVariances(this.c.getTypeArguments(source), this.c.getTypeArguments(target), variances3, intersectionState);
                            var varianceResult2 = tuple3.Item1;
                            var ok2 = tuple3.Item2;
                            if (ok2)
                            {
                                return varianceResult2;
                            }

                            break;
                        }

                        case true when (this.c.isArrayType(target) && (((this.c.isReadonlyArrayType(target) && everyType(source, this.c.isArrayOrTupleType)) || everyType(source, isMutableTupleType)))):
                        {
                            if ((this.relation != this.c.identityRelation))
                            {
                                return this.isRelatedTo(this.c.getIndexTypeOfTypeEx(source, this.c.numberType, this.c.anyType), this.c.getIndexTypeOfTypeEx(target, this.c.numberType, this.c.anyType), RecursionFlags.Both, reportErrors);
                            }

                            return Ternary.False;
                        }

                        case true when ((isGenericTupleType(source) && isTupleType(target)) && !isGenericTupleType(target)):
                        {
                            var constraint7 = this.c.getBaseConstraintOrType(source);
                            if ((constraint7 != source))
                            {
                                return this.isRelatedTo(constraint7, target, RecursionFlags.Source, reportErrors);
                            }

                            break;
                        }

                        case true when ((((((this.relation == this.c.subtypeRelation) || (this.relation == this.c.strictSubtypeRelation))) && this.c.isEmptyObjectType(target)) && ((target.objectFlags & ObjectFlags.FreshLiteral) != 0)) && !this.c.isEmptyObjectType(source)):
                        {
                            return Ternary.False;
                        }
                    }

                    if ((((source.flags & ((TypeFlags.Object | TypeFlags.Intersection))) != 0) && ((target.flags & TypeFlags.Object) != 0)))
                    {
                        var reportStructuralErrors = ((reportErrors && (this.errorChain == saveErrorState.errorChain)) && !sourceIsPrimitive);
                        result = this.propertiesRelatedTo(source, target, reportStructuralErrors, new HashSet<string> { }, false, intersectionState);
                        if ((result != Ternary.False))
                        {
                            result &= this.signaturesRelatedTo(source, target, SignatureKind.Call, reportStructuralErrors, intersectionState);
                            if ((result != Ternary.False))
                            {
                                result &= this.signaturesRelatedTo(source, target, SignatureKind.Construct, reportStructuralErrors, intersectionState);
                                if ((result != Ternary.False))
                                {
                                    result &= this.indexSignaturesRelatedTo(source, target, sourceIsPrimitive, reportStructuralErrors, intersectionState);
                                }
                            }
                        }

                        if ((result != Ternary.False))
                        {
                            if (!varianceCheckFailed)
                            {
                                return result;
                            }

                            if ((originalErrorChain != null))
                            {
                                this.errorChain = originalErrorChain;
                            }
                            else
                            {
                                if ((this.errorChain == null))
                                {
                                    this.errorChain = saveErrorState.errorChain;
                                }
                            }
                        }
                    }

                    if ((((source.flags & ((TypeFlags.Object | TypeFlags.Intersection))) != 0) && ((target.flags & TypeFlags.Union) != 0)))
                    {
                        var objectOnlyTarget = this.c.extractTypesOfKind(target, ((TypeFlags.Object | TypeFlags.Intersection) | TypeFlags.Substitution));
                        if (((objectOnlyTarget.flags & TypeFlags.Union) != 0))
                        {
                            var result3 = this.typeRelatedToDiscriminatedType(source, objectOnlyTarget);
                            if ((result3 != Ternary.False))
                            {
                                return result3;
                            }
                        }
                    }

                    break;
                }
            }

            return Ternary.False;
        }

        // relater.go:3935. Exact variance ordering; unselected source-generic
        // reliability computation is guarded before it can affect a relationship.
        private Ternary typeArgumentsRelatedTo(List<Type> sources, List<Type> targets, List<VarianceFlags> variances, bool reportErrors, IntersectionState intersectionState)
        {
            if (((GoRuntime.Len(sources) != GoRuntime.Len(targets)) && (this.relation == this.c.identityRelation)))
            {
                return Ternary.False;
            }

            var length = Math.Min(GoRuntime.Len(sources), GoRuntime.Len(targets));
            var result = Ternary.True;
            {
                foreach (var i in GoRuntime.RangeKeys(length))
                {
                    var varianceFlags = VarianceFlags.Covariant;
                    if ((i < GoRuntime.Len(variances)))
                    {
                        varianceFlags = variances[i];
                    }

                    var variance = (varianceFlags & VarianceFlags.VarianceMask);
                    if ((variance != VarianceFlags.Independent))
                    {
                        var s = sources[i];
                        var t = targets[i];
                        Ternary related = default;
                        if (((varianceFlags & VarianceFlags.Unmeasurable) != 0))
                        {
                            if ((this.relation == this.c.identityRelation))
                            {
                                related = this.isRelatedTo(s, t, RecursionFlags.Both, false);
                            }
                            else
                            {
                                related = this.c.compareTypesIdentical(s, t);
                            }
                        }
                        else
                        {
                            if (((GoRuntime.Len(this.c.varianceStack) != 0) && ((varianceFlags & VarianceFlags.Unreliable) != 0)))
                            {
                                this.c.instantiateType(s, this.c.reportUnreliableMapper);
                            }

                            if ((variance == VarianceFlags.Covariant))
                            {
                                related = this.isRelatedToEx(s, t, RecursionFlags.Both, reportErrors, null, intersectionState);
                            }
                            else
                            {
                                if ((variance == VarianceFlags.Contravariant))
                                {
                                    related = this.isRelatedToEx(t, s, RecursionFlags.Both, reportErrors, null, intersectionState);
                                }
                                else
                                {
                                    if ((variance == VarianceFlags.Bivariant))
                                    {
                                        related = this.isRelatedTo(t, s, RecursionFlags.Both, false);
                                        if ((related == Ternary.False))
                                        {
                                            related = this.isRelatedToEx(s, t, RecursionFlags.Both, reportErrors, null, intersectionState);
                                        }
                                    }
                                    else
                                    {
                                        related = this.isRelatedToEx(s, t, RecursionFlags.Both, reportErrors, null, intersectionState);
                                        if ((related != Ternary.False))
                                        {
                                            related &= this.isRelatedToEx(t, s, RecursionFlags.Both, reportErrors, null, intersectionState);
                                        }
                                    }
                                }
                            }
                        }

                        if ((related == Ternary.False))
                        {
                            return Ternary.False;
                        }

                        result &= related;
                    }
                }
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
                        // relater.go:4132. Tuple properties already cover their
                        // elements after excluding discriminants; numeric index
                        // types still include those excluded discriminants.
                        if (result != Ternary.False && !(isTupleType(source) && isTupleType(type)))
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
            {
                bool sourceIsAbstract = (sourceSignatures[0].flags & SignatureFlags.Abstract) != 0;
                bool targetIsAbstract = (targetSignatures[0].flags & SignatureFlags.Abstract) != 0;
                if (sourceIsAbstract && !targetIsAbstract)
                {
                    if (reportErrors)
                        reportError(Diagnostics.Cannot_assign_an_abstract_constructor_type_to_a_non_abstract_constructor_type);
                    return Ternary.False;
                }

                if (!constructorVisibilitiesAreCompatible(sourceSignatures[0], targetSignatures[0], reportErrors))
                    return Ternary.False;
            }

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

            TypeComparer isRelatedToWorker = new SignatureRelatedComparer(this, intersectionState).Compare;
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

        // relater.go:4610. Generic mapped sources use their template against string indexes.
        private Ternary indexSignaturesRelatedTo(Type source, Type target, bool sourceIsPrimitive, bool reportErrors, IntersectionState intersectionState)
        {
            if (relation == c.identityRelation)
                return indexSignaturesIdenticalTo(source, target);
            var infos = c.getIndexInfosOfType(target);
            bool hasStringIndex = infos?.Any(info => info.keyType == c.stringType) == true;
            var result = Ternary.True;
            foreach (var info in infos ?? [])
            {
                Ternary related;
                if (relation != c.strictSubtypeRelation && !sourceIsPrimitive && hasStringIndex && (info.valueType.flags & TypeFlags.Any) != 0)
                    related = Ternary.True;
                else if (c.isGenericMappedType(source) && hasStringIndex)
                    related = isRelatedTo(c.getTemplateTypeFromMappedType(source), info.valueType, RecursionFlags.Both, reportErrors);
                else
                    related = typeRelatedToIndexInfo(source, info, reportErrors, intersectionState);
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
                if (isIgnoredJsxProperty(source, property))
                    continue;
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
            if ((this.relation == this.c.identityRelation))
            {
                return this.propertiesIdenticalTo(source, target, excludedProperties);
            }

            var result = Ternary.True;
            if (isTupleType(target))
            {
                if (this.c.isArrayOrTupleType(source))
                {
                    if ((!target.TargetTupleType().@readonly && ((this.c.isReadonlyArrayType(source) || (isTupleType(source) && source.TargetTupleType().@readonly)))))
                    {
                        return Ternary.False;
                    }

                    var sourceArity = this.c.getTypeReferenceArity(source);
                    var targetArity = this.c.getTypeReferenceArity(target);
                    bool sourceRest = default;
                    if (isTupleType(source))
                    {
                        sourceRest = ((source.TargetTupleType().combinedFlags & ElementFlags.Rest) != 0);
                    }
                    else
                    {
                        sourceRest = true;
                    }

                    var targetHasRestElement = ((target.TargetTupleType().combinedFlags & ElementFlags.Rest) != 0);
                    var targetHasVariableElement = ((target.TargetTupleType().combinedFlags & ElementFlags.Variable) != 0);
                    int sourceMinLength = default;
                    if (isTupleType(source))
                    {
                        sourceMinLength = source.TargetTupleType().minLength;
                    }
                    else
                    {
                        sourceMinLength = 0;
                    }

                    var targetMinLength = target.TargetTupleType().minLength;
                    if ((!sourceRest && (sourceArity < targetMinLength)))
                    {
                        if (reportErrors)
                        {
                            this.reportError(Diagnostics.Source_has_0_element_s_but_target_requires_1, sourceArity, targetMinLength);
                        }

                        return Ternary.False;
                    }

                    if ((!targetHasVariableElement && (targetArity < sourceMinLength)))
                    {
                        if (reportErrors)
                        {
                            this.reportError(Diagnostics.Source_has_0_element_s_but_target_allows_only_1, sourceMinLength, targetArity);
                        }

                        return Ternary.False;
                    }

                    if ((!targetHasVariableElement && ((sourceRest || (targetArity < sourceArity)))))
                    {
                        if (reportErrors)
                        {
                            if ((sourceMinLength < targetMinLength))
                            {
                                this.reportError(Diagnostics.Target_requires_0_element_s_but_source_may_have_fewer, targetMinLength);
                            }
                            else
                            {
                                this.reportError(Diagnostics.Target_allows_only_0_element_s_but_source_may_have_more, targetArity);
                            }
                        }

                        return Ternary.False;
                    }

                    var sourceTypeArguments = this.c.getTypeArguments(source);
                    var targetTypeArguments = this.c.getTypeArguments(target);
                    var targetStartCount = getStartElementCount(target.TargetTupleType(), ElementFlags.NonRest);
                    var targetEndCount = getEndElementCount(target.TargetTupleType(), ElementFlags.NonRest);
                    var canExcludeDiscriminants = ((excludedProperties?.Count ?? 0) != 0);
                    {
                        foreach (var sourcePosition in GoRuntime.RangeKeys(sourceArity))
                        {
                            ElementFlags sourceFlags = default;
                            if (isTupleType(source))
                            {
                                sourceFlags = source.TargetTupleType().elementInfos[sourcePosition].flags;
                            }
                            else
                            {
                                sourceFlags = ElementFlags.Rest;
                            }

                            var sourcePositionFromEnd = ((sourceArity - 1) - sourcePosition);
                            int targetPosition = default;
                            if ((targetHasRestElement && (sourcePosition >= targetStartCount)))
                            {
                                targetPosition = ((targetArity - 1) - Math.Min(sourcePositionFromEnd, targetEndCount));
                            }
                            else
                            {
                                if ((sourcePosition >= targetArity))
                                {
                                    if (reportErrors)
                                    {
                                        this.reportError(Diagnostics.Target_allows_only_0_element_s_but_source_may_have_more, targetArity);
                                    }

                                    return Ternary.False;
                                }

                                targetPosition = sourcePosition;
                            }

                            var targetFlags = ElementFlags.None;
                            if ((targetPosition >= 0))
                            {
                                targetFlags = target.TargetTupleType().elementInfos[targetPosition].flags;
                            }

                            if ((((targetFlags & ElementFlags.Variadic) != 0) && ((sourceFlags & ElementFlags.Variadic) == 0)))
                            {
                                if (reportErrors)
                                {
                                    this.reportError(Diagnostics.Source_provides_no_match_for_variadic_element_at_position_0_in_target, targetPosition);
                                }

                                return Ternary.False;
                            }

                            if ((((sourceFlags & ElementFlags.Variadic) != 0) && ((targetFlags & ElementFlags.Variable) == 0)))
                            {
                                if (reportErrors)
                                {
                                    this.reportError(Diagnostics.Variadic_element_at_position_0_in_source_does_not_match_element_at_position_1_in_target, sourcePosition, targetPosition);
                                }

                                return Ternary.False;
                            }

                            if ((((targetFlags & ElementFlags.Required) != 0) && ((sourceFlags & ElementFlags.Required) == 0)))
                            {
                                if (reportErrors)
                                {
                                    this.reportError(Diagnostics.Source_provides_no_match_for_required_element_at_position_0_in_target, targetPosition);
                                }

                                return Ternary.False;
                            }

                            if (canExcludeDiscriminants)
                            {
                                if ((((sourceFlags & ElementFlags.Variable) != 0) || ((targetFlags & ElementFlags.Variable) != 0)))
                                {
                                    canExcludeDiscriminants = false;
                                }

                                if ((canExcludeDiscriminants && excludedProperties.Contains(Strconv.Itoa(sourcePosition))))
                                {
                                    continue;
                                }
                            }

                            var sourceType = this.c.removeMissingType(sourceTypeArguments[sourcePosition], (((sourceFlags & targetFlags) & ElementFlags.Optional) != 0));
                            var targetType = targetTypeArguments[targetPosition];
                            Type targetCheckType = default;
                            if ((((sourceFlags & ElementFlags.Variadic) != 0) && ((targetFlags & ElementFlags.Rest) != 0)))
                            {
                                targetCheckType = this.c.createArrayType(targetType);
                            }
                            else
                            {
                                targetCheckType = this.c.removeMissingType(targetType, ((targetFlags & ElementFlags.Optional) != 0));
                            }

                            var related = this.isRelatedToEx(sourceType, targetCheckType, RecursionFlags.Both, reportErrors, null, intersectionState);
                            if ((related == Ternary.False))
                            {
                                if ((reportErrors && (((targetArity > 1) || (sourceArity > 1)))))
                                {
                                    if ((((targetHasRestElement && (sourcePosition >= targetStartCount)) && (sourcePositionFromEnd >= targetEndCount)) && (targetStartCount != ((sourceArity - targetEndCount) - 1))))
                                    {
                                        this.reportError(Diagnostics.Type_at_positions_0_through_1_in_source_is_not_compatible_with_type_at_position_2_in_target, targetStartCount, ((sourceArity - targetEndCount) - 1), targetPosition);
                                    }
                                    else
                                    {
                                        this.reportError(Diagnostics.Type_at_position_0_in_source_is_not_compatible_with_type_at_position_1_in_target, sourcePosition, targetPosition);
                                    }
                                }

                                return Ternary.False;
                            }

                            result &= related;
                        }
                    }

                    return result;
                }

                if (((target.TargetTupleType().combinedFlags & ElementFlags.Variable) != 0))
                {
                    return Ternary.False;
                }
            }

            var requireOptionalProperties = ((((((this.relation == this.c.subtypeRelation) || (this.relation == this.c.strictSubtypeRelation))) && !isObjectLiteralType(source)) && !this.c.isEmptyArrayLiteralType(source)) && !isTupleType(source));
            var unmatchedProperty = this.c.getUnmatchedProperty(source, target, requireOptionalProperties, false);
            if ((unmatchedProperty != null))
            {
                if ((reportErrors && this.c.shouldReportUnmatchedPropertyError(source, target)))
                {
                    this.reportUnmatchedProperty(source, target, unmatchedProperty, requireOptionalProperties);
                }

                return Ternary.False;
            }

            if (isObjectLiteralType(target))
            {
                {
                    foreach (var sourceProp in GoRuntime.RangeValues(excludeProperties(this.c.getPropertiesOfType(source), excludedProperties)))
                    {
                        if ((this.c.getPropertyOfObjectType(target, sourceProp.Name) == null))
                        {
                            if (reportErrors)
                            {
                                this.reportError(Diagnostics.Property_0_does_not_exist_on_type_1, this.c.symbolToString(sourceProp), this.c.typeToStringByteString(target));
                            }

                            return Ternary.False;
                        }
                    }
                }
            }

            var properties = this.c.getPropertiesOfType(target);
            var numericNamesOnly = (isTupleType(source) && isTupleType(target));
            {
                foreach (var range7 in GoRuntime.Range(excludeProperties(properties, excludedProperties)))
                {
                    var _2 = range7.Key;
                    var targetProp = range7.Value;
                    var name = targetProp.Name;
                    if (((((targetProp.Flags & SymbolFlags.Prototype) == 0) && (((!numericNamesOnly || isNumericLiteralName(name)) || (name == "length")))) && ((!optionalsOnly || ((targetProp.Flags & SymbolFlags.Optional) != 0)))))
                    {
                        var sourceProp2 = this.c.getPropertyOfType(source, name);
                        if (((sourceProp2 != null) && (sourceProp2 != targetProp)))
                        {
                            var related2 = this.propertyRelatedTo(source, target, sourceProp2, targetProp, this.c.getNonMissingTypeOfSymbol, reportErrors, intersectionState, (this.relation == this.c.comparableRelation));
                            if ((related2 == Ternary.False))
                            {
                                return Ternary.False;
                            }

                            result &= related2;
                        }
                    }
                }
            }

            return result;
        }

        private Ternary propertyRelatedTo(Type source, Type target, Symbol sourceProp, Symbol targetProp, Func<Symbol, Type> getTypeOfSourceProperty, bool reportErrors, IntersectionState intersectionState, bool skipOptional)
        {
            var sourceFlags = getDeclarationModifierFlagsFromSymbol(sourceProp);
            var targetFlags = getDeclarationModifierFlagsFromSymbol(targetProp);
            // relater.go:4305. Private members require the same declaration; a
            // protected target requires a valid override from a derived class.
            if ((sourceFlags & ModifierFlags.Private) != 0 || (targetFlags & ModifierFlags.Private) != 0)
            {
                if (sourceProp.ValueDeclaration != targetProp.ValueDeclaration)
                {
                    if (reportErrors)
                    {
                        if ((sourceFlags & ModifierFlags.Private) != 0 && (targetFlags & ModifierFlags.Private) != 0)
                            reportError(Diagnostics.Types_have_separate_declarations_of_a_private_property_0, c.symbolToString(targetProp));
                        else
                            reportError(Diagnostics.Property_0_is_private_in_type_1_but_not_in_type_2,
                                c.symbolToString(targetProp),
                                c.typeToStringByteString((sourceFlags & ModifierFlags.Private) != 0 ? source : target),
                                c.typeToStringByteString((sourceFlags & ModifierFlags.Private) != 0 ? target : source));
                    }
                    return Ternary.False;
                }
            }
            else if ((targetFlags & ModifierFlags.Protected) != 0)
            {
                if (!c.isValidOverrideOf(sourceProp, targetProp))
                {
                    if (reportErrors)
                        reportError(Diagnostics.Property_0_is_protected_but_type_1_is_not_a_class_derived_from_2,
                            c.symbolToString(targetProp), c.typeToStringByteString(c.getDeclaringClass(sourceProp) ?? source),
                            c.typeToStringByteString(c.getDeclaringClass(targetProp) ?? target));
                    return Ternary.False;
                }
            }
            else if ((sourceFlags & ModifierFlags.Protected) != 0)
            {
                if (reportErrors)
                    reportError(Diagnostics.Property_0_is_protected_in_type_1_but_public_in_type_2,
                        c.symbolToString(targetProp), c.typeToStringByteString(source), c.typeToStringByteString(target));
                return Ternary.False;
            }
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
            // relater.go:4380. Equal private descriptions in different classes
            // identify distinct members, so prefer the specific access diagnostic.
            if (unmatchedProperty.ValueDeclaration?.Name()?.Kind == Kind.PrivateIdentifier &&
                source.symbol != null && (source.symbol.Flags & SymbolFlags.Class) != 0)
            {
                var description = unmatchedProperty.ValueDeclaration.Name().Text();
                var key = Binder.GetSymbolNameForPrivateIdentifier(source.symbol, description);
                if (c.getPropertyOfType(source, key) != null)
                {
                    reportError(Diagnostics.Property_0_in_type_1_refers_to_a_different_member_that_cannot_be_accessed_from_within_type_2,
                        description, c.symbolToString(source.symbol), c.symbolToString(target.symbol));
                    return;
                }
            }
            var props = c.getUnmatchedProperties(source, target, requireOptionalProperties, false);
            if (GoRuntime.Len(props) == 1)
            {
                var (sourceType, targetType) = c.getTypeNamesForErrorDisplay(source, target);
                var name = c.symbolToString(unmatchedProperty);
                reportError(Diagnostics.Property_0_is_missing_in_type_1_but_required_in_type_2, name, sourceType, targetType);
                if ((unmatchedProperty.Declarations?.Count ?? 0) != 0)
                    (relatedInfo ??= []).Add(NewDiagnosticForNode(unmatchedProperty.Declarations[0], Diagnostics.X_0_is_declared_here, name));
            }
            else if (tryElaborateArrayLikeErrors(source, target, false))
            {
                var (sourceType, targetType) = c.getTypeNamesForErrorDisplay(source, target);
                if (GoRuntime.Len(props) > 5)
                    reportError(Diagnostics.Type_0_is_missing_the_following_properties_from_type_1_Colon_2_and_3_more, sourceType, targetType, string.Join(", ", props.Take(4).Select(c.symbolToString)), props.Count - 4);
                else
                    reportError(Diagnostics.Type_0_is_missing_the_following_properties_from_type_1_Colon_2, sourceType, targetType, string.Join(", ", (props ?? []).Select(c.symbolToString)));
            }
        }

        private bool tryElaborateArrayLikeErrors(Type source, Type target, bool reportErrors)
        {
            if (isTupleType(source))
            {
                if ((source.TargetTupleType().@readonly && this.c.isMutableArrayOrTuple(target)))
                {
                    if (reportErrors)
                    {
                        this.reportError(Diagnostics.The_type_0_is_readonly_and_cannot_be_assigned_to_the_mutable_type_1, this.c.typeToStringByteString(source), this.c.typeToStringByteString(target));
                    }

                    return false;
                }

                return this.c.isArrayOrTupleType(target);
            }

            if ((this.c.isReadonlyArrayType(source) && this.c.isMutableArrayOrTuple(target)))
            {
                if (reportErrors)
                {
                    this.reportError(Diagnostics.The_type_0_is_readonly_and_cannot_be_assigned_to_the_mutable_type_1, this.c.typeToStringByteString(source), this.c.typeToStringByteString(target));
                }

                return false;
            }

            if (isTupleType(target))
            {
                return this.c.isArrayType(source);
            }

            return true;
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
            else if ((source.objectFlags & ObjectFlags.JsxAttributes) != 0 && (target.flags & TypeFlags.Intersection) != 0)
            {
                var targetTypes = target.Types();
                var intrinsicAttributes = c.getJsxType(JsxNames.IntrinsicAttributes, errorNode);
                var intrinsicClassAttributes = c.getJsxType(JsxNames.IntrinsicClassAttributes, errorNode);
                if (!c.isErrorType(intrinsicAttributes) && !c.isErrorType(intrinsicClassAttributes) && (targetTypes.Contains(intrinsicAttributes) || targetTypes.Contains(intrinsicClassAttributes)))
                    return;
            }
            else if ((originalTarget.flags & TypeFlags.Intersection) != 0 && (originalTarget.objectFlags & ObjectFlags.IsNeverIntersection) != 0)
            {
                var message = Diagnostics.The_intersection_0_was_reduced_to_never_because_property_1_has_conflicting_types_in_some_constituents;
                var property = Core.Find(c.getPropertiesOfUnionOrIntersectionType(originalTarget), c.isDiscriminantWithNeverType);
                if (property == null)
                {
                    message = Diagnostics.The_intersection_0_was_reduced_to_never_because_property_1_exists_in_multiple_constituents_and_is_private_in_some;
                    property = Core.Find(c.getPropertiesOfUnionOrIntersectionType(originalTarget), isConflictingPrivateProperty);
                }

                if (property != null)
                    reportError(message, c.typeToStringNoTypeReduction(originalTarget), c.symbolToString(property));
            }

            reportRelationError(headMessage, source, target);
            // relater.go:4777. A genuine generic source may need an explicit
            // constraint; keep the synthetic parameter and circularity check.
            if ((source.flags & TypeFlags.TypeParameter) != 0 && source.symbol != null && GoRuntime.Len(source.symbol.Declarations) != 0 && c.getConstraintOfType(source) == null)
            {
                var syntheticParam = c.cloneTypeParameter(source);
                syntheticParam.AsTypeParameter().constraint = c.instantiateType(target, newSimpleTypeMapper(source, syntheticParam));
                if (c.hasNonCircularBaseConstraint(syntheticParam))
                {
                    var targetConstraintString = c.typeToStringByteString(target);
                    relatedInfo = GoRuntime.Append(relatedInfo, createDiagnosticForNode(source.symbol.Declarations[0], Diagnostics.This_type_parameter_might_need_an_extends_0_constraint, targetConstraintString));
                }
            }
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

            // relater.go:4797. Indexed-access target errors refer to its object.
            var targetFlags = (target.flags & TypeFlags.IndexedAccess) != 0 && (source.flags & TypeFlags.IndexedAccess) == 0 ? target.AsIndexedAccessType().objectType.flags : target.flags;
            if ((targetFlags & TypeFlags.TypeParameter) != 0 && target != c.markerSuperTypeForCheck && target != c.markerSubTypeForCheck)
            {
                var constraint = c.getBaseConstraintOfType(target);
                if (IsDistributedTypeParameter(target) && c.isTypeAssignableTo(generalizedSource, target.AsTypeParameter().constraint))
                    reportError(Diagnostics.X_0_is_only_assignable_to_the_non_distributed_1_but_1_has_been_distributed_here, generalizedSourceType, targetType);
                else if (constraint != null && c.isTypeAssignableTo(generalizedSource, constraint))
                    reportError(Diagnostics.X_0_is_assignable_to_the_constraint_of_type_1_but_1_could_be_instantiated_with_a_different_subtype_of_constraint_2, generalizedSourceType, targetType, c.typeToStringByteString(constraint));
                else if (constraint != null && c.isTypeAssignableTo(source, constraint))
                    reportError(Diagnostics.X_0_is_assignable_to_the_constraint_of_type_1_but_1_could_be_instantiated_with_a_different_subtype_of_constraint_2, sourceType, targetType, c.typeToStringByteString(constraint));
                else
                {
                    errorChain = null;
                    reportError(Diagnostics.X_0_could_be_instantiated_with_an_arbitrary_type_which_could_be_unrelated_to_1, targetType, generalizedSourceType);
                }
            }

            if (message == null)
            {
                if (relation == c.comparableRelation)
                    message = Diagnostics.Type_0_is_not_comparable_to_type_1;
                else if (sourceType == targetType)
                    message = Diagnostics.Type_0_is_not_assignable_to_type_1_Two_different_types_with_this_name_exist_but_they_are_unrelated;
                else if (c.exactOptionalPropertyTypes && GoRuntime.Len(c.getExactOptionalUnassignableProperties(source, target)) != 0)
                    message = Diagnostics.Type_0_is_not_assignable_to_type_1_with_exactOptionalPropertyTypes_Colon_true_Consider_adding_undefined_to_the_types_of_the_target_s_properties;
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

            else if (message == Diagnostics.Argument_of_type_0_is_not_assignable_to_parameter_of_type_1 && c.exactOptionalPropertyTypes && GoRuntime.Len(c.getExactOptionalUnassignableProperties(source, target)) > 0)
                message = Diagnostics.Argument_of_type_0_is_not_assignable_to_parameter_of_type_1_with_exactOptionalPropertyTypes_Colon_true_Consider_adding_undefined_to_the_types_of_the_target_s_properties;

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
            var constraint = getConstraintOfType(t);
            if (constraint != null && constraint != t)
                return typeCouldHaveTopLevelSingletonTypes(constraint);
        }

        return isUnitType(t) || (t.flags & (TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0;
    }

    private bool isOrHasGenericConditional(Type t) => (t.flags & TypeFlags.Conditional) != 0 || (t.flags & TypeFlags.Intersection) != 0 && t.Types().Any(isOrHasGenericConditional);
    private bool elaborateError(Node node, Type source, Type target, Relation relation, Message headMessage, List<Diagnostic> diagnosticOutput)
    {
        if (node == null || isOrHasGenericConditional(target))
            return false;
        if (compilerOptions.NoCheck == Tristate.True)
            return false;
        if (elaborateDidYouMeanToCallOrConstruct(node, source, target, relation, SignatureKind.Construct, headMessage, diagnosticOutput) || elaborateDidYouMeanToCallOrConstruct(node, source, target, relation, SignatureKind.Call, headMessage, diagnosticOutput))
            return true;
        switch (node.Kind)
        {
            case Kind.AsExpression:
                if (!Ast.IsConstAssertion(node))
                    break;
                return elaborateError(node.Expression(), source, target, relation, headMessage, diagnosticOutput);
            case Kind.JsxExpression:
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
            case Kind.ArrayLiteralExpression:
                return elaborateArrayLiteral(node, source, target, relation, diagnosticOutput);
            case Kind.JsxAttributes:
                return elaborateJsxComponents(node, source, target, relation, diagnosticOutput);
        }

        return false;
    }

    // relater.go:520. Recheck an array against the target as a tuple so errors
    // retain the original failing element and contextual type.
    private bool elaborateArrayLiteral(Node node, Type source, Type target, Relation relation, List<Diagnostic> diagnosticOutput)
    {
        if ((target.flags & (TypeFlags.Primitive | TypeFlags.Never)) != 0)
            return false;
        if (!isTupleLikeType(source))
        {
            pushContextualType(node, target, false);
            source = checkArrayLiteral(node, CheckMode.Contextual | CheckMode.ForceTuple);
            popContextualType();
            if (!isTupleLikeType(source))
                return false;
        }

        bool reportedError = false;
        var elements = node.Elements();
        int count = elements?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            var element = elements[i];
            if (Ast.IsOmittedExpression(element) || isTupleLikeType(target) && getPropertyOfType(target, new JsNum.Number(i).String()) == null)
                continue;
            var nameType = getNumberLiteralType(new JsNum.Number(i));
            var checkNode = getEffectiveCheckNode(element);
            reportedError = elaborateElement(source, target, relation, checkNode, checkNode, nameType, null, null, diagnosticOutput) || reportedError;
        }

        return reportedError;
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
                continue;
            var nameType = getLiteralTypeFromProperty(getSymbolOfDeclaration(prop), TypeFlags.StringOrNumberLiteralOrUnique, false);
            if (nameType == null || (nameType.flags & TypeFlags.Never) != 0)
                continue;
            switch (prop.Kind)
            {
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                case Kind.MethodDeclaration:
                case Kind.ShorthandPropertyAssignment:
                    reportedError = elaborateElement(source, target, relation, prop.Name(), null, nameType, null, null, diagnosticOutput) || reportedError;
                    break;
                case Kind.PropertyAssignment:
                    var message = Ast.IsComputedNonLiteralName(prop.Name()) ? Diagnostics.Type_of_computed_property_s_value_is_0_which_is_not_assignable_to_type_1 : null;
                    reportedError = elaborateElement(source, target, relation, prop.Name(), prop.Initializer(), nameType, message, null, diagnosticOutput) || reportedError;
                    break;
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
        else if (exactOptionalPropertyTypes && isExactOptionalPropertyMismatch(specificSource, targetPropType))
            diags.Add(NewDiagnosticForNode(prop, Diagnostics.Type_0_is_not_assignable_to_type_1_with_exactOptionalPropertyTypes_Colon_true_Consider_adding_undefined_to_the_type_of_the_target, typeToStringByteString(specificSource), typeToStringByteString(targetPropType)));
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
            if (info?.declaration != null && !isSourceFileDefaultLibrary(Ast.GetSourceFileOfNode(info.declaration)))
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
            if (!isSourceFileDefaultLibrary(Ast.GetSourceFileOfNode(targetNode)))
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

public sealed partial class Checker
{
    private Type createMarkerType(Symbol symbol, Type source, Type target)
    {
        var mapper = newSimpleTypeMapper(source, target);
        var t = this.getDeclaredTypeOfSymbol(symbol);
        if (this.isErrorType(t))
        {
            return t;
        }

        Type result = default;
        if (((symbol.Flags & SymbolFlags.TypeAlias) != 0))
        {
            result = this.getTypeAliasInstantiation(symbol, this.instantiateTypes(this.typeAliasLinks.Get(symbol).typeParameters, mapper), null);
        }
        else
        {
            result = this.createTypeReference(t, this.instantiateTypes(t.AsInterfaceType().TypeParameters(), mapper));
        }

        this.markerTypes.Add(result);
        return result;
    }
}

public sealed partial class Checker
{
    internal ModifierFlags getTypeParameterModifiers(Type tp)
    {
        ModifierFlags flags = default;
        if ((tp.symbol != null))
        {
            {
                foreach (var d in GoRuntime.RangeValues(tp.symbol.Declarations))
                {
                    flags |= d.ModifierFlags();
                }
            }
        }

        return (flags & (((ModifierFlags.In | ModifierFlags.Out) | ModifierFlags.Const)));
    }
}

public sealed partial class Checker
{
    private bool hasCovariantVoidArgument(List<Type> typeArguments, List<VarianceFlags> variances)
    {
        {
            foreach (var range2 in GoRuntime.Range(variances))
            {
                var i = range2.Key;
                var v = range2.Value;
                if ((((v & VarianceFlags.VarianceMask) == VarianceFlags.Covariant) && ((typeArguments[i].flags & TypeFlags.Void) != 0)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private Type getRestOrAnyTypeAtPosition(Signature source, int pos)
    {
        var restType = this.getRestTypeAtPosition(source, pos, false);
        if ((restType != null))
        {
            {
                var elementType = this.getElementTypeOfArrayType(restType);
                if (((elementType != null) && IsTypeAny(elementType)))
                {
                    return this.anyType;
                }
            }
        }

        return restType;
    }
}

public sealed partial class Checker
{
    private Type getRestTypeAtPosition(Signature source, int pos, bool @readonly)
    {
        var parameterCount = this.getParameterCount(source);
        var minArgumentCount = this.getMinArgumentCount(source);
        var restType = this.getEffectiveRestType(source);
        if (((restType != null) && (pos >= (parameterCount - 1))))
        {
            if ((pos == (parameterCount - 1)))
            {
                return restType;
            }
            else
            {
                return this.createArrayType(this.getIndexedAccessType(restType, this.numberType));
            }
        }

        var length = (parameterCount - pos);
        if ((length <= 0))
        {
            return this.createTupleTypeEx(null, null, @readonly);
        }

        var types = GoRuntime.MakeSlice<Type>(length);
        var infos = GoRuntime.MakeSlice<TupleElementInfo>(length);
        {
            foreach (var i in GoRuntime.RangeKeys(types))
            {
                ElementFlags flags = default;
                if (((restType == null) || (i < (GoRuntime.Len(types) - 1))))
                {
                    types[i] = this.getTypeAtPosition(source, (i + pos));
                    flags = Core.IfElse(((i + pos) < minArgumentCount), ElementFlags.Required, ElementFlags.Optional);
                }
                else
                {
                    types[i] = restType;
                    flags = ElementFlags.Variadic;
                }

                infos[i] = new TupleElementInfo
                {
                    flags = flags,
                    labeledDeclaration = this.getNameableDeclarationAtPosition(source, (i + pos))
                };
            }
        }

        return this.createTupleTypeEx(types, infos, @readonly);
    }
}

public sealed partial class Checker
{
    private Node getNameableDeclarationAtPosition(Signature signature, int pos)
    {
        var paramCount = (GoRuntime.Len(signature.parameters) - Core.IfElse(signatureHasRestParameter(signature), 1, 0));
        if ((pos < paramCount))
        {
            var decl = signature.parameters[pos].ValueDeclaration;
            if (((decl != null) && this.isValidDeclarationForTupleLabel(decl)))
            {
                return decl;
            }

            return null;
        }

        if (signatureHasRestParameter(signature))
        {
            var restParameter = signature.parameters[paramCount];
            var restType = this.getTypeOfSymbol(restParameter);
            if (isTupleType(restType))
            {
                var elementInfos = restType.TargetTupleType().elementInfos;
                var index = (pos - paramCount);
                if ((index < GoRuntime.Len(elementInfos)))
                {
                    return elementInfos[index].labeledDeclaration;
                }

                return null;
            }

            if (((restParameter.ValueDeclaration != null) && this.isValidDeclarationForTupleLabel(restParameter.ValueDeclaration)))
            {
                return restParameter.ValueDeclaration;
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private bool isValidDeclarationForTupleLabel(Node d)
    {
        return (Ast.IsNamedTupleMember(d) || ((Ast.IsParameterDeclaration(d) && (d.Name() != null)) && Ast.IsIdentifier(d.Name())));
    }
}

public sealed partial class Checker
{
    private Type sliceTupleType(Type t, int index, int endSkipCount)
    {
        var target = t.TargetTupleType();
        var endIndex = (this.getTypeReferenceArity(t) - Math.Max(endSkipCount, 0));
        if ((index > target.fixedLength))
        {
            {
                var restArrayType = this.getRestArrayTypeOfTupleType(t);
                if ((restArrayType != null))
                {
                    return restArrayType;
                }
            }

            return this.createTupleType(null);
        }

        if ((index >= endIndex))
        {
            return this.createTupleType(null);
        }

        return this.createTupleTypeEx(GoRuntime.Slice(this.getTypeArguments(t), index, endIndex), GoRuntime.Slice(target.elementInfos, index, endIndex), false);
    }
}

public sealed partial class Checker
{
    private Type getKnownKeysOfTupleType(Type t)
    {
        var fixedLength = t.TargetTupleType().fixedLength;
        var keys = GoRuntime.MakeSlice<Type>((fixedLength + 1));
        {
            foreach (var i in GoRuntime.RangeKeys(fixedLength))
            {
                keys[i] = this.getStringLiteralType(Strconv.Itoa(i));
            }
        }

        keys[fixedLength] = this.getIndexType(Core.IfElse(t.TargetTupleType().@readonly, this.globalReadonlyArrayType, this.globalArrayType));
        return this.getUnionType(keys);
    }
}

public sealed partial class Checker
{
    private Type getRestArrayTypeOfTupleType(Type t)
    {
        {
            var restType = this.getRestTypeOfTupleType(t);
            if ((restType != null))
            {
                return this.createArrayType(restType);
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    internal string getTupleElementLabel(TupleElementInfo elementInfo, Symbol restSymbol, int index)
    {
        if ((elementInfo.labeledDeclaration != null))
        {
            return elementInfo.labeledDeclaration.Name().Text();
        }

        if ((((restSymbol != null) && (restSymbol.ValueDeclaration != null)) && Ast.IsParameterDeclaration(restSymbol.ValueDeclaration)))
        {
            return this.getTupleElementLabelFromBindingElement(restSymbol.ValueDeclaration, index, elementInfo.flags);
        }

        string rootName = "";
        if ((restSymbol != null))
        {
            rootName = restSymbol.Name;
        }
        else
        {
            rootName = "arg";
        }

        return ((rootName + "_") + Strconv.Itoa(index));
    }
}

public sealed partial class Checker
{
    private string getTupleElementLabelFromBindingElement(Node node, int index, ElementFlags elementFlags)
    {
        if ((node.Name() != null))
        {
            switch (node.Name().Kind)
            {
                case Kind.Identifier:
                {
                    var name = node.Name().Text();
                    if (hasDotDotDotToken(node))
                    {
                        if (((elementFlags & ElementFlags.Variable) != 0))
                        {
                            return name;
                        }

                        return ((name + "_") + Strconv.Itoa(index));
                    }

                    if (((elementFlags & ElementFlags.Fixed) != 0))
                    {
                        return name;
                    }

                    return (name + "_n");
                }

                case Kind.ArrayBindingPattern:
                {
                    if (hasDotDotDotToken(node))
                    {
                        var elements = node.Name().Elements();
                        var lastElement = Core.LastOrNil(elements);
                        var lastElementIsBindingElementRest = (((lastElement != null) && Ast.IsBindingElement(lastElement)) && hasDotDotDotToken(lastElement));
                        var elementCount = (GoRuntime.Len(elements) - Core.IfElse(lastElementIsBindingElementRest, 1, 0));
                        if ((index < elementCount))
                        {
                            var element = elements[index];
                            if (Ast.IsBindingElement(element))
                            {
                                return this.getTupleElementLabelFromBindingElement(element, index, elementFlags);
                            }
                        }
                        else
                        {
                            if (lastElementIsBindingElementRest)
                            {
                                return this.getTupleElementLabelFromBindingElement(lastElement, (index - elementCount), elementFlags);
                            }
                        }
                    }

                    break;
                }
            }
        }

        return ("arg_" + Strconv.Itoa(index));
    }
}

public sealed partial class Checker
{
    private TypePredicate createTypePredicateFromTypePredicateNode(Node node, Signature signature)
    {
        var predicateNode = node.AsTypePredicateNode();
        Type t = default;
        if ((predicateNode.Type != null))
        {
            t = this.getTypeFromTypeNode(predicateNode.Type);
        }

        if (Ast.IsThisTypeNode(predicateNode.ParameterName))
        {
            var kind = Core.IfElse((predicateNode.AssertsModifier != null), TypePredicateKind.AssertsThis, TypePredicateKind.This);
            return this.newTypePredicate(kind, "", 0, t);
        }

        var kind2 = Core.IfElse((predicateNode.AssertsModifier != null), TypePredicateKind.AssertsIdentifier, TypePredicateKind.Identifier);
        var name = predicateNode.ParameterName.Text();
        var index = Core.FindIndex(signature.parameters, (p) =>
        {
            return (p.Name == name);
        });
        return this.newTypePredicate(kind2, name, (int)(index), t);
    }
}

public sealed partial class Checker
{
    private sealed partial class Relater
    {
        private Ternary mappedTypeRelatedTo(Type source, Type target, bool reportErrors)
        {
            var modifiersRelated = (((this.relation == this.c.comparableRelation) || ((this.relation == this.c.identityRelation) && (getMappedTypeModifiers(source) == getMappedTypeModifiers(target)))) || ((this.relation != this.c.identityRelation) && (this.c.getCombinedMappedTypeOptionality(source) <= this.c.getCombinedMappedTypeOptionality(target))));
            if (modifiersRelated)
            {
                var targetConstraint = this.c.getConstraintTypeFromMappedType(target);
                var sourceConstraint = this.c.instantiateType(this.c.getConstraintTypeFromMappedType(source), Core.IfElse((this.c.getCombinedMappedTypeOptionality(source) < 0), this.c.reportUnmeasurableMapper, this.c.reportUnreliableMapper));
                {
                    var result = this.isRelatedTo(targetConstraint, sourceConstraint, RecursionFlags.Both, reportErrors);
                    if ((result != Ternary.False))
                    {
                        var mapper = newSimpleTypeMapper(this.c.getTypeParameterFromMappedType(source), this.c.getTypeParameterFromMappedType(target));
                        if ((this.c.instantiateType(this.c.getNameTypeFromMappedType(source), mapper) == this.c.instantiateType(this.c.getNameTypeFromMappedType(target), mapper)))
                        {
                            return (result & this.isRelatedTo(this.c.instantiateType(this.c.getTemplateTypeFromMappedType(source), mapper), this.c.getTemplateTypeFromMappedType(target), RecursionFlags.Both, reportErrors));
                        }
                    }
                }
            }

            return Ternary.False;
        }
    }
}

public sealed partial class Checker
{
    private sealed partial class Relater
    {
        private bool constructorVisibilitiesAreCompatible(Signature sourceSignature, Signature targetSignature, bool reportErrors)
        {
            if (((sourceSignature.declaration == null) || (targetSignature.declaration == null)))
            {
                return true;
            }

            var sourceAccessibility = (sourceSignature.declaration.ModifierFlags() & ModifierFlags.NonPublicAccessibilityModifier);
            var targetAccessibility = (targetSignature.declaration.ModifierFlags() & ModifierFlags.NonPublicAccessibilityModifier);
            if ((targetAccessibility == ModifierFlags.Private))
            {
                return true;
            }

            if (((targetAccessibility == ModifierFlags.Protected) && (sourceAccessibility != ModifierFlags.Private)))
            {
                return true;
            }

            if (((targetAccessibility != ModifierFlags.Protected) && (sourceAccessibility == 0)))
            {
                return true;
            }

            if (reportErrors)
            {
                this.reportError(Diagnostics.Cannot_assign_a_0_constructor_type_to_a_1_constructor_type, visibilityToString(sourceAccessibility), visibilityToString(targetAccessibility));
            }

            return false;
        }
    }
}

public sealed partial class Checker
{
    private bool isDistributionDependent(ConditionalRoot root)
    {
        return (root.isDistributive && ((this.isTypeParameterPossiblyReferenced(root.checkType, root.node.TrueType) || this.isTypeParameterPossiblyReferenced(root.checkType, root.node.FalseType))));
    }
}

public sealed partial class Checker
{
    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2354:1.
    private bool templateLiteralTypesDefinitelyUnrelated(TemplateLiteralType source, TemplateLiteralType target)
    {
        var sourceStart = source.texts[0];
        var targetStart = target.texts[0];
        var sourceEnd = source.texts[(GoRuntime.Len(source.texts) - 1)];
        var targetEnd = target.texts[(GoRuntime.Len(target.texts) - 1)];
        var startLen = Math.Min(GoRuntime.Len(sourceStart), GoRuntime.Len(targetStart));
        var endLen = Math.Min(GoRuntime.Len(sourceEnd), GoRuntime.Len(targetEnd));
        return ((GoRuntime.Slice(sourceStart, 0, startLen) != GoRuntime.Slice(targetStart, 0, startLen)) || (GoRuntime.Slice(sourceEnd, (GoRuntime.Len(sourceEnd) - endLen)) != GoRuntime.Slice(targetEnd, (GoRuntime.Len(targetEnd) - endLen))));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2365:1.
    private bool isTypeMatchedByTemplateLiteralType(Type source, TemplateLiteralType target, TypeComparer compareTypes)
    {
        var inferences = this.inferTypesFromTemplateLiteralType(source, target, compareTypes);
        if ((inferences != null))
        {
            {
                foreach (var range1 in GoRuntime.Range(inferences))
                {
                    var i = range1.Key;
                    var inference = range1.Value;
                    if (!this.isValidTypeForTemplateLiteralPlaceholder(inference, target.types[i], compareTypes))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2378:1.
    internal List<Type> inferTypesFromTemplateLiteralType(Type source, TemplateLiteralType target, TypeComparer compareTypes)
    {
        switch (true)
        {
            case true when ((source.flags & TypeFlags.StringLiteral) != 0):
            {
                return this.inferFromLiteralPartsToTemplateLiteral(new List<string> { getStringLiteralValue(source) }, null, target);
            }

            case true when ((source.flags & TypeFlags.TemplateLiteral) != 0):
            {
                if (GoSlices.Equal(source.AsTemplateLiteralType().texts, target.texts))
                {
                    return Core.MapIndex(source.AsTemplateLiteralType().types, (s, i) =>
                    {
                        if ((compareTypes(this.getBaseConstraintOrType(s), this.getBaseConstraintOrType(target.types[i]), false) != Ternary.False))
                        {
                            return s;
                        }

                        return this.getStringLikeTypeForType(s);
                    });
                }

                return this.inferFromLiteralPartsToTemplateLiteral(source.AsTemplateLiteralType().texts, source.AsTemplateLiteralType().types, target);
            }

            default:
            {
                return null;
            }
        }
    }

    // This function infers from the text parts and type parts of a source literal to a target template literal. The number
    // of text parts is always one more than the number of type parts, and a source string literal is treated as a source
    // with one text part and zero type parts. The function returns an array of inferred string or template literal types
    // corresponding to the placeholders in the target template literal, or undefined if the source doesn't match the target.
    //
    // We first check that the starting source text part matches the starting target text part, and that the ending source
    // text part ends matches the ending target text part. We then iterate through the remaining target text parts, finding
    // a match for each in the source and inferring string or template literal types created from the segments of the source
    // that occur between the matches. During this iteration, seg holds the index of the current text part in the sourceTexts
    // array and pos holds the current character position in the current text part.
    //
    // Consider inference from type `<<${string}>.<${number}-${number}>>` to type `<${string}.${string}>`, i.e.
    //
    //	sourceTexts = ['<<', '>.<', '-', '>>']
    //	sourceTypes = [string, number, number]
    //	target.texts = ['<', '.', '>']
    //
    // We first match '<' in the target to the start of '<<' in the source and '>' in the target to the end of '>>' in
    // the source. The first match for the '.' in target occurs at character 1 in the source text part at index 1, and thus
    // the first inference is the template literal type `<${string}>`. The remainder of the source makes up the second
    // inference, the template literal type `<${number}-${number}>`.
    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2418:1.
    private List<Type> inferFromLiteralPartsToTemplateLiteral(List<string> sourceTexts, List<Type> sourceTypes, TemplateLiteralType target)
    {
        var lastSourceIndex = (GoRuntime.Len(sourceTexts) - 1);
        var sourceStartText = sourceTexts[0];
        var sourceEndText = sourceTexts[lastSourceIndex];
        var targetTexts = target.texts;
        var lastTargetIndex = (GoRuntime.Len(targetTexts) - 1);
        var targetStartText = targetTexts[0];
        var targetEndText = targetTexts[lastTargetIndex];
        if (((((lastSourceIndex == 0) && (GoRuntime.Len(sourceStartText) < (GoRuntime.Len(targetStartText) + GoRuntime.Len(targetEndText)))) || !GoStrings.HasPrefix(sourceStartText, targetStartText)) || !GoStrings.HasSuffix(sourceEndText, targetEndText)))
        {
            return null;
        }

        var remainingEndText = GoRuntime.Slice(sourceEndText, 0, (GoRuntime.Len(sourceEndText) - GoRuntime.Len(targetEndText)));
        var seg = 0;
        var pos = GoRuntime.Len(targetStartText);
        List<Type> matches = default;
        Func<int, string> getSourceText = (index) =>
        {
            if ((index < lastSourceIndex))
            {
                return sourceTexts[index];
            }

            return remainingEndText;
        };
        Action<int, int> addMatch = (s, p) =>
        {
            Type matchType = default;
            if ((s == seg))
            {
                matchType = this.getStringLiteralType(StringUtil.CombineSurrogatePairs(GoRuntime.Slice(getSourceText(s), pos, p)));
            }
            else
            {
                var matchTexts = GoRuntime.MakeSlice<string>(((s - seg) + 1));
                matchTexts[0] = GoRuntime.Slice(sourceTexts[seg], pos);
                for (int copyIndex = seg + 1; copyIndex < s; copyIndex++)
                    matchTexts[copyIndex - seg] = sourceTexts[copyIndex];
                matchTexts[(s - seg)] = GoRuntime.Slice(getSourceText(s), 0, p);
                matchType = this.getTemplateLiteralType(matchTexts, GoRuntime.Slice(sourceTypes, seg, s));
            }

            matches = GoRuntime.Append(matches, matchType);
            seg = s;
            pos = p;
        };
        {
            var i = 1;
            for (; (i < lastTargetIndex); i++)
            {
                var delim = targetTexts[i];
                if ((GoRuntime.Len(delim) > 0))
                {
                    var s2 = seg;
                    var p2 = pos;
                    while (true)
                    {
                        var d = GoStrings.Index(GoRuntime.Slice(getSourceText(s2), p2), delim);
                        if ((d >= 0))
                        {
                            p2 += d;
                            break;
                        }

                        s2++;
                        if ((s2 == GoRuntime.Len(sourceTexts)))
                        {
                            return null;
                        }

                        p2 = 0;
                    }

                    addMatch(s2, p2);
                    pos += GoRuntime.Len(delim);
                }
                else
                {
                    {
                        var sourceText = getSourceText(seg);
                        if ((pos < GoRuntime.Len(sourceText)))
                        {
                            var tuple2 = StringUtil.DecodeJSStringRune(GoRuntime.Slice(sourceText, pos));
                            var size = tuple2.Item2;
                            addMatch(seg, (pos + size));
                        }
                        else
                        {
                            if ((seg < lastSourceIndex))
                            {
                                addMatch((seg + 1), 0);
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
        }

        addMatch(lastSourceIndex, GoRuntime.Len(getSourceText(lastSourceIndex)));
        return matches;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2502:1.
    internal Type getStringLikeTypeForType(Type t)
    {
        if (((t.flags & ((TypeFlags.Any | TypeFlags.StringLike))) != 0))
        {
            return t;
        }

        return this.getTemplateLiteralType(new List<string> { "", "" }, new List<Type> { t });
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/relater.go:2509:1.
    internal bool isValidTypeForTemplateLiteralPlaceholder(Type source, Type target, TypeComparer compareTypes)
    {
        switch (true)
        {
            case true when ((target.flags & TypeFlags.Intersection) != 0):
            {
                return Core.Every(target.Types(), (t) =>
                {
                    return ((t == this.emptyTypeLiteralType) || this.isValidTypeForTemplateLiteralPlaceholder(source, t, compareTypes));
                });
            }

            case true when (((target.flags & TypeFlags.String) != 0) || (compareTypes(source, target, false) != Ternary.False)):
            {
                return true;
            }

            case true when ((source.flags & TypeFlags.StringLiteral) != 0):
            {
                var value = getStringLiteralValue(source);
                return (((((((target.flags & TypeFlags.Number) != 0) && isValidNumberString(value, false)) || (((target.flags & TypeFlags.BigInt) != 0) && isValidBigIntString(value, false))) || (((target.flags & ((TypeFlags.BooleanLiteral | TypeFlags.Nullable))) != 0) && (value == target.AsIntrinsicType().intrinsicName))) || (((target.flags & TypeFlags.StringMapping) != 0) && isMemberOfStringMapping(source, target))) || (((target.flags & TypeFlags.TemplateLiteral) != 0) && this.isTypeMatchedByTemplateLiteralType(source, target.AsTemplateLiteralType(), compareTypes)));
            }

            case true when ((source.flags & TypeFlags.TemplateLiteral) != 0):
            {
                var texts = source.AsTemplateLiteralType().texts;
                return ((((GoRuntime.Len(texts) == 2) && (texts[0] == "")) && (texts[1] == "")) && (compareTypes(source.AsTemplateLiteralType().types[0], target, false) != Ternary.False));
            }
        }

        return false;
    }

    // relater.go:2531. Membership requires unchanged mapped source identity and
    // compatibility with the innermost unmapped target.
    private bool isMemberOfStringMapping(Type source, Type target)
    {
        if ((target.flags & TypeFlags.Any) != 0)
            return true;
        if ((target.flags & (TypeFlags.String | TypeFlags.TemplateLiteral)) != 0)
            return isTypeAssignableTo(source, target);
        if ((target.flags & TypeFlags.StringMapping) != 0)
        {
            var (mapped, inner) = applyTargetStringMappingToSource(source, target);
            return mapped == source && isMemberOfStringMapping(source, inner);
        }
        return false;
    }

    // relater.go:2551. Apply nested mappings from the innermost target outward.
    private (Type Mapped, Type Inner) applyTargetStringMappingToSource(Type source, Type target)
    {
        var inner = target.AsStringMappingType().target;
        if ((inner.flags & TypeFlags.StringMapping) != 0)
            (source, inner) = applyTargetStringMappingToSource(source, inner);
        return (getStringMappingType(target.symbol, source), inner);
    }
}

public sealed partial class Checker
{
    private bool isEnumTypeRelatedTo(Symbol source, Symbol target, ErrorReporter errorReporter)
    {
        var sourceSymbol = ((source.Flags & SymbolFlags.EnumMember) != 0 ? this.getParentOfSymbol(source) : source);
        var targetSymbol = ((target.Flags & SymbolFlags.EnumMember) != 0 ? this.getParentOfSymbol(target) : target);
        if ((sourceSymbol == targetSymbol))
        {
            return true;
        }
        if ((((sourceSymbol.Name != targetSymbol.Name) || ((sourceSymbol.Flags & SymbolFlags.RegularEnum) == 0)) || ((targetSymbol.Flags & SymbolFlags.RegularEnum) == 0)))
        {
            return false;
        }
        var key = new EnumRelationKey { sourceId = Ast.GetSymbolId(sourceSymbol), targetId = Ast.GetSymbolId(targetSymbol) };
        {
            var entry = this.enumRelation.TryGetValue(key, out var cached) ? cached : RelationComparisonResult.None;
            if (((entry != RelationComparisonResult.None) && !((((entry & RelationComparisonResult.Failed) != 0) && (errorReporter != null)))))
            {
                return ((entry & RelationComparisonResult.Succeeded) != 0);
            }
        }
        var targetEnumType = this.getTypeOfSymbol(targetSymbol);
        {
            foreach (var sourceProperty in GoRuntime.RangeValues(this.getPropertiesOfType(this.getTypeOfSymbol(sourceSymbol))))
            {
                if (((sourceProperty.Flags & SymbolFlags.EnumMember) != 0))
                {
                    var targetProperty = this.getPropertyOfType(targetEnumType, sourceProperty.Name);
                    if (((targetProperty == null) || ((targetProperty.Flags & SymbolFlags.EnumMember) == 0)))
                    {
                        if ((errorReporter != null))
                        {
                            errorReporter(Diagnostics.Property_0_is_missing_in_type_1, this.symbolToString(sourceProperty), this.typeToStringEx(this.getDeclaredTypeOfSymbol(targetSymbol), null, fullyQualified: true));
                        }
                        this.enumRelation[key] = RelationComparisonResult.Failed;
                        return false;
                    }
                    var sourceValue = this.getEnumMemberValue(Ast.GetDeclarationOfKind(sourceProperty, Kind.EnumMember)).Value;
                    var targetValue = this.getEnumMemberValue(Ast.GetDeclarationOfKind(targetProperty, Kind.EnumMember)).Value;
                    if (!declarationConstantValuesEqual(sourceValue, targetValue))
                    {
                        if (((sourceValue != null) && (targetValue != null)))
                        {
                            if ((errorReporter != null))
                            {
                                errorReporter(Diagnostics.Each_declaration_of_0_1_differs_in_its_value_where_2_was_expected_but_3_was_given, this.symbolToString(targetSymbol), this.symbolToString(targetProperty), this.valueToString(targetValue), this.valueToString(sourceValue));
                            }
                            this.enumRelation[key] = RelationComparisonResult.Failed;
                            return false;
                        }
                        var _2 = sourceValue as string; var sourceIsString = _2 != null;
                        var _3 = targetValue as string; var targetIsString = _3 != null;
                        if ((sourceIsString || targetIsString))
                        {
                            if ((errorReporter != null))
                            {
                                var knownStringValue = (sourceValue ?? targetValue);
                                errorReporter(Diagnostics.One_value_of_0_1_is_the_string_2_and_the_other_is_assumed_to_be_an_unknown_numeric_value, this.symbolToString(targetSymbol), this.symbolToString(targetProperty), this.valueToString(knownStringValue));
                            }
                            this.enumRelation[key] = RelationComparisonResult.Failed;
                            return false;
                        }
                    }
                }
            }
        }
        this.enumRelation[key] = RelationComparisonResult.Succeeded;
        return true;
    }
}
