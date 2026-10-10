// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of upstream/TypeScript/tsc/internal/checker/inference.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // inference.go:11/19. The original key is already a constituent value tuple.
    private readonly record struct InferenceKey(uint source, uint target, InferencePriority priority, bool contravariant, bool bivariant);
    private sealed class InferenceState
    {
        internal List<InferenceInfo> inferences;
        internal Type originalSource, originalTarget, propagationType;
        internal InferencePriority priority, inferencePriority;
        internal bool contravariant, bivariant;
        internal ExpandingFlags expandingFlags;
        internal Dictionary<InferenceKey, InferencePriority> visited;
        internal Slice<Type> sourceStack, targetStack;
        internal InferenceState next;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:35:1.
    private InferenceState getInferenceState()
    {
        var n = this.freeinferenceState;
        if ((n == null))
        {
            n = new InferenceState
            {
            };
        }

        this.freeinferenceState = n.next;
        return n;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:44:1.
    private void putInferenceState(InferenceState n)
    {
        n.visited?.Clear();
        n.inferences = n.inferences == null ? null : [];
        // inference.go:49/50. Reset only the headers; retain backing capacity.
        n.sourceStack = n.sourceStack.Slice2(0, 0);
        n.targetStack = n.targetStack.Slice2(0, 0);
        n.originalSource = n.originalTarget = n.propagationType = null;
        n.priority = n.inferencePriority = InferencePriority.None;
        n.contravariant = n.bivariant = false;
        n.expandingFlags = ExpandingFlags.None;
        n.next = freeinferenceState;
        freeinferenceState = n;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:56:1.
    private void inferTypes(List<InferenceInfo> inferences, Type originalSource, Type originalTarget, InferencePriority priority, bool contravariant)
    {
        var n = this.getInferenceState();
        n.inferences = inferences;
        n.originalSource = originalSource;
        n.originalTarget = originalTarget;
        n.priority = priority;
        n.inferencePriority = InferencePriority.MaxValue;
        n.contravariant = contravariant;
        this.inferFromTypes(n, originalSource, originalTarget);
        this.putInferenceState(n);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:68:1.
    private void inferFromTypes(InferenceState n, Type source, Type target)
    {
        if ((!this.couldContainTypeVariables(target) || this.isNoInferType(target)))
        {
            return;
        }

        if (((source == this.wildcardType) || (source == this.blockedStringType)))
        {
            var savePropagationType = n.propagationType;
            n.propagationType = source;
            this.inferFromTypes(n, target, target);
            n.propagationType = savePropagationType;
            return;
        }

        if ((((source.alias != null) && (target.alias != null)) && (source.alias.symbol == target.alias.symbol)))
        {
            if (((GoRuntime.Len(source.alias.typeArguments) != 0) || (GoRuntime.Len(target.alias.typeArguments) != 0)))
            {
                this.invokeOnce(n, source, target, (c, n, s, t) => c.inferFromAliasTypeArguments(n, s, t));
            }

            return;
        }

        if (((source == target) && ((source.flags & TypeFlags.UnionOrIntersection) != 0)))
        {
            {
                foreach (var t in GoRuntime.RangeValues(source.Types()))
                {
                    this.inferFromTypes(n, t, t);
                }
            }

            return;
        }

        if (((target.flags & TypeFlags.Union) != 0))
        {
            List<Type> sourceTypes = default;
            if (((source.flags & TypeFlags.Union) != 0))
            {
                sourceTypes = source.Types();
            }
            else
            {
                sourceTypes = new List<Type>
                {
                    source
                };
            }

            var tuple2 = this.inferFromMatchingTypes(n, sourceTypes, target.Types(), (c, s, t) => c.isTypeOrBaseIdenticalTo(s, t), false);
            var tempSources = tuple2.Item1;
            var tempTargets = tuple2.Item2;
            var tuple3 = this.inferFromMatchingTypes(n, tempSources, tempTargets, (c, s, t) => c.isTypeCloselyMatchedBy(s, t), true);
            var sources = tuple3.Item1;
            var targets = tuple3.Item2;
            if ((GoRuntime.Len(targets) == 0))
            {
                return;
            }

            target = this.getUnionType(targets);
            if ((GoRuntime.Len(sources) == 0))
            {
                this.inferWithPriority(n, source, target, InferencePriority.NakedTypeVariable);
                return;
            }

            source = this.getUnionType(sources);
        }
        else
        {
            if (((target.flags & TypeFlags.Intersection) != 0))
            {
                // inference.go:125 / core.go:199: preserve Every's order and first-false stop
                // without allocating a Checker-bound predicate delegate on each inference.
                bool allNonGenericObjectTypes = true;
                var targetTypes = target.Types();
                if (targetTypes != null)
                {
                    foreach (var constituent in targetTypes)
                    {
                        if (!this.isNonGenericObjectType(constituent))
                        {
                            allNonGenericObjectTypes = false;
                            break;
                        }
                    }
                }

                if ((!allNonGenericObjectTypes && ((source.flags & TypeFlags.Union) == 0)))
                {
                    List<Type> sourceTypes2 = default;
                    if (((source.flags & TypeFlags.Intersection) != 0))
                    {
                        sourceTypes2 = source.Types();
                    }
                    else
                    {
                        sourceTypes2 = new List<Type>
                        {
                            source
                        };
                    }

                    var tuple4 = this.inferFromMatchingTypes(n, sourceTypes2, target.Types(), (c, s, t) => c.isTypeIdenticalTo(s, t), false);
                    var sources2 = tuple4.Item1;
                    var targets2 = tuple4.Item2;
                    if (((GoRuntime.Len(sources2) == 0) || (GoRuntime.Len(targets2) == 0)))
                    {
                        return;
                    }

                    source = this.getIntersectionType(sources2);
                    target = this.getIntersectionType(targets2);
                }
            }
        }

        if (((target.flags & ((TypeFlags.IndexedAccess | TypeFlags.Substitution))) != 0))
        {
            if (this.isNoInferType(target))
            {
                return;
            }

            target = this.getActualTypeVariable(target);
        }

        if (((target.flags & TypeFlags.TypeVariable) != 0))
        {
            if (this.isFromInferenceBlockedSource(source))
            {
                return;
            }

            var inference = getInferenceInfoForType(n, target);
            if ((inference != null))
            {
                if ((((source.objectFlags & ObjectFlags.NonInferrableType) != 0) || (source == this.nonInferrableAnyType)))
                {
                    return;
                }

                if (!inference.isFixed)
                {
                    var candidate = Core.OrElse(n.propagationType, source);
                    if ((candidate == this.blockedStringType))
                    {
                        return;
                    }

                    if ((n.priority < inference.priority))
                    {
                        inference.candidates = null;
                        inference.contraCandidates = null;
                        inference.topLevel = true;
                        inference.priority = n.priority;
                    }

                    if ((n.priority == inference.priority))
                    {
                        if ((n.contravariant && !n.bivariant))
                        {
                            if (!GoSlices.Contains(inference.contraCandidates, candidate))
                            {
                                inference.contraCandidates = GoRuntime.Append(inference.contraCandidates, candidate);
                                clearCachedInferences(n.inferences);
                            }
                        }
                        else
                        {
                            if (!GoSlices.Contains(inference.candidates, candidate))
                            {
                                inference.candidates = GoRuntime.Append(inference.candidates, candidate);
                                clearCachedInferences(n.inferences);
                            }
                        }
                    }

                    if ((((((n.priority & InferencePriority.ReturnType) == 0) && ((target.flags & TypeFlags.TypeParameter) != 0)) && inference.topLevel) && !this.isTypeParameterAtTopLevel(n.originalTarget, target, 0)))
                    {
                        inference.topLevel = false;
                        clearCachedInferences(n.inferences);
                    }
                }

                n.inferencePriority = (InferencePriority)Math.Min((int)n.inferencePriority, (int)n.priority);
                return;
            }

            var simplified = this.getSimplifiedType(target, false);
            if ((simplified != target))
            {
                this.inferFromTypes(n, source, simplified);
            }
            else
            {
                if (((target.flags & TypeFlags.IndexedAccess) != 0))
                {
                    var indexType = this.getSimplifiedType(target.AsIndexedAccessType().indexType, false);
                    if (((indexType.flags & TypeFlags.Instantiable) != 0))
                    {
                        var simplified2 = this.distributeIndexOverObjectType(this.getSimplifiedType(target.AsIndexedAccessType().objectType, false), indexType, false);
                        if (((simplified2 != null) && (simplified2 != target)))
                        {
                            this.inferFromTypes(n, source, simplified2);
                        }
                    }
                }
            }
        }

        switch (true)
        {
            case true when (((((source.objectFlags & ObjectFlags.Reference) != 0) && ((target.objectFlags & ObjectFlags.Reference) != 0)) && (((source.AsTypeReference().target == target.AsTypeReference().target) || (this.isArrayType(source) && this.isArrayType(target))))) && !(((source.AsTypeReference().node != null) && (target.AsTypeReference().node != null)))):
            {
                this.invokeOnce(n, source, target, (c, n, s, t) => c.inferFromReferenceTypeArguments(n, s, t));
                break;
            }

            case true when (((source.flags & TypeFlags.Index) != 0) && ((target.flags & TypeFlags.Index) != 0)):
            {
                this.inferFromContravariantTypes(n, source.AsIndexType().target, target.AsIndexType().target);
                break;
            }

            case true when (((isLiteralType(source) || ((source.flags & TypeFlags.String) != 0))) && ((target.flags & TypeFlags.Index) != 0)):
            {
                var empty = this.createEmptyObjectTypeFromStringLiteral(source);
                this.inferFromContravariantTypesWithPriority(n, empty, target.AsIndexType().target, InferencePriority.LiteralKeyof);
                break;
            }

            case true when (((source.flags & TypeFlags.IndexedAccess) != 0) && ((target.flags & TypeFlags.IndexedAccess) != 0)):
            {
                this.inferFromTypes(n, source.AsIndexedAccessType().objectType, target.AsIndexedAccessType().objectType);
                this.inferFromTypes(n, source.AsIndexedAccessType().indexType, target.AsIndexedAccessType().indexType);
                break;
            }

            case true when (((source.flags & TypeFlags.StringMapping) != 0) && ((target.flags & TypeFlags.StringMapping) != 0)):
            {
                if ((source.symbol == target.symbol))
                {
                    inferFromTypes(n, source.AsStringMappingType().target, target.AsStringMappingType().target);
                }

                break;
            }

            case true when ((source.flags & TypeFlags.Substitution) != 0):
            {
                this.inferFromTypes(n, source.AsSubstitutionType().baseType, target);
                this.inferWithPriority(n, this.getSubstitutionIntersection(source), target, InferencePriority.SubstituteSource);
                break;
            }

            case true when ((target.flags & TypeFlags.Conditional) != 0):
            {
                this.invokeOnce(n, source, target, (c, n, s, t) => c.inferToConditionalType(n, s, t));
                break;
            }

            case true when ((target.flags & TypeFlags.UnionOrIntersection) != 0):
            {
                this.inferToMultipleTypes(n, source, target.Types(), target.flags);
                break;
            }

            case true when ((source.flags & TypeFlags.Union) != 0):
            {
                {
                    foreach (var range5 in GoRuntime.Range(source.Types()))
                    {
                        var _2 = range5.Key;
                        var sourceType = range5.Value;
                        this.inferFromTypes(n, sourceType, target);
                    }
                }

                break;
            }

            case true when ((target.flags & TypeFlags.TemplateLiteral) != 0):
            {
                this.inferToTemplateLiteralType(n, source, target.AsTemplateLiteralType());
                break;
            }

            default:
            {
                source = this.getReducedType(source);
                if ((this.isGenericMappedType(source) && this.isGenericMappedType(target)))
                {
                    this.invokeOnce(n, source, target, (c, n, s, t) => c.inferFromGenericMappedTypes(n, s, t));
                }

                if (!((((n.priority & InferencePriority.NoConstraints) != 0) && ((source.flags & ((TypeFlags.Intersection | TypeFlags.Instantiable))) != 0))))
                {
                    var apparentSource = this.getApparentType(source);
                    if (((apparentSource != source) && ((apparentSource.flags & ((TypeFlags.Object | TypeFlags.Intersection))) == 0)))
                    {
                        this.inferFromTypes(n, apparentSource, target);
                        return;
                    }

                    source = apparentSource;
                }

                if (((source.flags & ((TypeFlags.Object | TypeFlags.Intersection))) != 0))
                {
                    this.invokeOnce(n, source, target, (c, n, s, t) => c.inferFromObjectTypes(n, s, t));
                }

                break;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:280:1.
    private void inferFromAliasTypeArguments(InferenceState n, Type source, Type target)
    {
        var @params = this.typeAliasLinks.Get(source.alias.symbol).typeParameters;
        var minParams = this.getMinTypeArgumentCount(@params);
        var nodeIsInJsFile = Ast.IsInJSFile(source.alias.symbol.ValueDeclaration);
        var sourceTypes = this.fillMissingTypeArguments(source.alias.typeArguments, @params, minParams, nodeIsInJsFile);
        var targetTypes = this.fillMissingTypeArguments(target.alias.typeArguments, @params, minParams, nodeIsInJsFile);
        this.inferFromTypeArguments(n, sourceTypes, targetTypes, this.getAliasVariances(source.alias.symbol));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:291:1.
    private void inferFromReferenceTypeArguments(InferenceState n, Type source, Type target)
    {
        this.inferFromTypeArguments(n, this.getTypeArguments(source), this.getTypeArguments(target), this.getVariances(source.AsTypeReference().target));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:295:1.
    private void inferFromTypeArguments(InferenceState n, List<Type> sourceTypes, List<Type> targetTypes, List<VarianceFlags> variances)
    {
        {
            foreach (var i in GoRuntime.RangeKeys(Math.Min(GoRuntime.Len(sourceTypes), GoRuntime.Len(targetTypes))))
            {
                if (((i < GoRuntime.Len(variances)) && ((variances[i] & VarianceFlags.VarianceMask) == VarianceFlags.Contravariant)))
                {
                    this.inferFromContravariantTypes(n, sourceTypes[i], targetTypes[i]);
                }
                else
                {
                    this.inferFromTypes(n, sourceTypes[i], targetTypes[i]);
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:305:1.
    private void inferWithPriority(InferenceState n, Type source, Type target, InferencePriority newPriority)
    {
        var savePriority = n.priority;
        n.priority |= newPriority;
        this.inferFromTypes(n, source, target);
        n.priority = savePriority;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:312:1.
    private void inferFromContravariantTypesWithPriority(InferenceState n, Type source, Type target, InferencePriority newPriority)
    {
        var savePriority = n.priority;
        n.priority |= newPriority;
        this.inferFromContravariantTypes(n, source, target);
        n.priority = savePriority;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:319:1.
    private void inferFromContravariantTypes(InferenceState n, Type source, Type target)
    {
        n.contravariant = !n.contravariant;
        this.inferFromTypes(n, source, target);
        n.contravariant = !n.contravariant;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:325:1.
    private void inferFromContravariantTypesIfStrictFunctionTypes(InferenceState n, Type source, Type target)
    {
        if ((this.strictFunctionTypes || ((n.priority & InferencePriority.AlwaysStrict) != 0)))
        {
            this.inferFromContravariantTypes(n, source, target);
        }
        else
        {
            this.inferFromTypes(n, source, target);
        }
    }

    // Ensure an inference action is performed only once for the given source and target types.
    // This includes two things:
    // Avoiding inferring between the same pair of source and target types,
    // and avoiding circularly inferring between source and target types.
    // For an example of the last, consider if we are inferring between source type
    // `type Deep<T> = { next: Deep<Deep<T>> }` and target type `type Loop<U> = { next: Loop<U> }`.
    // We would then infer between the types of the `next` property: `Deep<Deep<T>>` = `{ next: Deep<Deep<Deep<T>>> }` and `Loop<U>` = `{ next: Loop<U> }`.
    // We will then infer again between the types of the `next` property:
    // `Deep<Deep<Deep<T>>>` and `Loop<U>`, and so on, such that we would be forever inferring
    // between instantiations of the same types `Deep` and `Loop`.
    // In particular, we would be inferring from increasingly deep instantiations of `Deep` to `Loop`,
    // such that we would go on inferring forever, even though we would never infer
    // between the same pair of types.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:346:1.
    private void invokeOnce(InferenceState n, Type source, Type target, Action<Checker, InferenceState, Type, Type> action)
    {
        var key = new InferenceKey
        {
            source = source.id,
            target = target.id,
            priority = n.priority,
            contravariant = n.contravariant,
            bivariant = n.bivariant
        };
        {
            var status = default(InferencePriority);
            var ok = n.visited != null && n.visited.TryGetValue(key, out status);
            if (ok)
            {
                n.inferencePriority = (InferencePriority)Math.Min((int)n.inferencePriority, (int)status);
                return;
            }
        }

        if ((n.visited == null))
        {
            n.visited = new Dictionary<InferenceKey, InferencePriority>();
        }

        n.visited[key] = InferencePriority.Circularity;
        var saveInferencePriority = n.inferencePriority;
        n.inferencePriority = InferencePriority.MaxValue;
        var saveExpandingFlags = n.expandingFlags;
        n.sourceStack = n.sourceStack.Append(source);
        n.targetStack = n.targetStack.Append(target);
        if (this.isDeeplyNestedType(source, n.sourceStack, 2))
        {
            n.expandingFlags |= ExpandingFlags.Source;
        }

        if (this.isDeeplyNestedType(target, n.targetStack, 2))
        {
            n.expandingFlags |= ExpandingFlags.Target;
        }

        if ((n.expandingFlags != ExpandingFlags.Both))
        {
            action(this, n, source, target);
        }
        else
        {
            n.inferencePriority = InferencePriority.Circularity;
        }

        n.targetStack = n.targetStack.Slice2(0, n.targetStack.Length - 1);
        n.sourceStack = n.sourceStack.Slice2(0, n.sourceStack.Length - 1);
        n.expandingFlags = saveExpandingFlags;
        n.visited[key] = n.inferencePriority;
        n.inferencePriority = (InferencePriority)Math.Min((int)n.inferencePriority, (int)saveInferencePriority);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:381:1.
    private (List<Type>, List<Type>) inferFromMatchingTypes(InferenceState n, List<Type> sources, List<Type> targets, Func<Checker, Type, Type, bool> matches, bool sort)
    {
        List<Type> matchedSources = default;
        List<Type> matchedTargets = default;
        {
            foreach (var t2 in GoRuntime.RangeValues(targets))
            {
                {
                    foreach (var range8 in GoRuntime.Range(sources))
                    {
                        var _2 = range8.Key;
                        var s2 = range8.Value;
                        if (matches(this, s2, t2))
                        {
                            if (!sort)
                            {
                                this.inferFromTypes(n, s2, t2);
                            }

                            matchedSources = Core.AppendIfUnique(matchedSources, s2);
                            matchedTargets = Core.AppendIfUnique(matchedTargets, t2);
                        }
                    }
                }
            }
        }

        if (sort)
        {
            GoSlices.SortFunc(matchedTargets, compareTypesAndDepth);
            {
                foreach (var range9 in GoRuntime.Range(matchedTargets))
                {
                    var _3 = range9.Key;
                    var t3 = range9.Value;
                    {
                        foreach (var range10 in GoRuntime.Range(matchedSources))
                        {
                            var _4 = range10.Key;
                            var s3 = range10.Value;
                            if (matches(this, s3, t3))
                            {
                                this.inferFromTypes(n, s3, t3);
                            }
                        }
                    }
                }
            }
        }

        if ((GoRuntime.Len(matchedSources) != 0))
        {
            sources = Core.Filter(sources, (t4) =>
            {
                return !GoSlices.Contains(matchedSources, t4);
            });
        }

        if ((GoRuntime.Len(matchedTargets) != 0))
        {
            targets = Core.Filter(targets, (t5) =>
            {
                return !GoSlices.Contains(matchedTargets, t5);
            });
        }

        return (sources, targets);
    }

    // Compare two types first by depth and then by the regular type ordering.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:421:1.
    private static int compareTypesAndDepth(Type t1, Type t2)
    {
        var d1 = getTypeDepth(t1, 3);
        var d2 = getTypeDepth(t2, 3);
        if ((d1 != d2))
        {
            return (d2 - d1);
        }

        return CompareTypes(t1, t2);
    }

    // Return the depth of the given type up to the given maximum depth. For generic aliased types
    // and type references, the depth is one plus the largest type argument depth. For union and
    // intersection types, the depth is the largest constituent type depth. For all other types,
    // the depth is zero. The maximum depth limits infinite recursion of circular types.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:434:1.
    private static int getTypeDepth(Type t, int maxDepth)
    {
        if ((maxDepth != 0))
        {
            if (((t.alias != null) && (GoRuntime.Len(t.alias.typeArguments) != 0)))
            {
                return (getTypeListDepth(t.alias.typeArguments, (maxDepth - 1)) + 1);
            }

            if (((t.objectFlags & ObjectFlags.Reference) != 0))
            {
                {
                    var typeArguments = t.checker.getTypeArguments(t);
                    if ((GoRuntime.Len(typeArguments) != 0))
                    {
                        return (getTypeListDepth(typeArguments, (maxDepth - 1)) + 1);
                    }
                }
            }

            if (((t.flags & TypeFlags.UnionOrIntersection) != 0))
            {
                return getTypeListDepth(t.Types(), maxDepth);
            }
        }

        return 0;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:451:1.
    private static int getTypeListDepth(List<Type> types, int maxDepth)
    {
        var depth = 0;
        {
            foreach (var t in GoRuntime.RangeValues(types))
            {
                depth = Math.Max(depth, getTypeDepth(t, maxDepth));
            }
        }

        return depth;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:459:1.
    private void inferToMultipleTypes(InferenceState n, Type source, List<Type> targets, TypeFlags targetFlags) =>
        inferToMultipleTypes(n, source, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(targets), targetFlags);

    // inference.go:459. Targets are a borrowed input header, never retained.
    private void inferToMultipleTypes(InferenceState n, Type source, ReadOnlySpan<Type> targets, TypeFlags targetFlags)
    {
        var typeVariableCount = 0;
        if (((targetFlags & TypeFlags.Union) != 0))
        {
            Type nakedTypeVariable = default;
            List<Type> sources = default;
            if (((source.flags & TypeFlags.Union) != 0))
            {
                sources = source.Types();
            }
            else
            {
                sources = new List<Type>
                {
                    source
                };
            }

            var matched = GoRuntime.MakeSlice<bool>(GoRuntime.Len(sources));
            var inferenceCircularity = false;
            {
                foreach (var t in targets)
                {
                    if ((getInferenceInfoForType(n, t) != null))
                    {
                        nakedTypeVariable = t;
                        typeVariableCount++;
                    }
                    else
                    {
                        {
                            foreach (var i in GoRuntime.RangeKeys(sources))
                            {
                                var saveInferencePriority = n.inferencePriority;
                                n.inferencePriority = InferencePriority.MaxValue;
                                this.inferFromTypes(n, sources[i], t);
                                if ((n.inferencePriority == n.priority))
                                {
                                    matched[i] = true;
                                }

                                inferenceCircularity = (inferenceCircularity || (n.inferencePriority == InferencePriority.Circularity));
                                n.inferencePriority = (InferencePriority)Math.Min((int)n.inferencePriority, (int)saveInferencePriority);
                            }
                        }
                    }
                }
            }

            if ((typeVariableCount == 0))
            {
                var intersectionTypeVariable = getSingleTypeVariableFromIntersectionTypes(n, targets);
                if ((intersectionTypeVariable != null))
                {
                    this.inferWithPriority(n, source, intersectionTypeVariable, InferencePriority.NakedTypeVariable);
                }

                return;
            }

            if (((typeVariableCount == 1) && !inferenceCircularity))
            {
                List<Type> unmatched = default;
                {
                    foreach (var range14 in GoRuntime.Range(sources))
                    {
                        var i2 = range14.Key;
                        var s = range14.Value;
                        if (!matched[i2])
                        {
                            unmatched = GoRuntime.Append(unmatched, s);
                        }
                    }
                }

                if ((GoRuntime.Len(unmatched) != 0))
                {
                    this.inferFromTypes(n, this.getUnionType(unmatched), nakedTypeVariable);
                    return;
                }
            }
        }
        else
        {
            {
                foreach (var t2 in targets)
                {
                    if ((getInferenceInfoForType(n, t2) != null))
                    {
                        typeVariableCount++;
                    }
                    else
                    {
                        this.inferFromTypes(n, source, t2);
                    }
                }
            }
        }

        if (((((targetFlags & TypeFlags.Intersection) != 0) && (typeVariableCount == 1)) || (((targetFlags & TypeFlags.Intersection) == 0) && (typeVariableCount > 0))))
        {
            {
                foreach (var t3 in targets)
                {
                    if ((getInferenceInfoForType(n, t3) != null))
                    {
                        this.inferWithPriority(n, source, t3, InferencePriority.NakedTypeVariable);
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:543:1.
    private static Type getSingleTypeVariableFromIntersectionTypes(InferenceState n, ReadOnlySpan<Type> types)
    {
        Type typeVariable = default;
        {
            foreach (var t in types)
            {
                if (((t.flags & TypeFlags.Intersection) == 0))
                {
                    return null;
                }

                var v = Core.Find(t.Types(), (t2) =>
                {
                    return (getInferenceInfoForType(n, t2) != null);
                });
                if (((v == null) || ((typeVariable != null) && (v != typeVariable))))
                {
                    return null;
                }

                typeVariable = v;
            }
        }

        return typeVariable;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:558:1.
    private void inferToMultipleTypesWithPriority(InferenceState n, Type source, List<Type> targets, TypeFlags targetFlags, InferencePriority newPriority) =>
        inferToMultipleTypesWithPriority(n, source, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(targets), targetFlags, newPriority);

    // inference.go:558. The borrowed targets remain synchronous across priority restoration.
    private void inferToMultipleTypesWithPriority(InferenceState n, Type source, ReadOnlySpan<Type> targets, TypeFlags targetFlags, InferencePriority newPriority)
    {
        var savePriority = n.priority;
        n.priority |= newPriority;
        this.inferToMultipleTypes(n, source, targets, targetFlags);
        n.priority = savePriority;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:565:1.
    private void inferToConditionalType(InferenceState n, Type source, Type target)
    {
        if (((source.flags & TypeFlags.Conditional) != 0))
        {
            this.inferFromTypes(n, getNonDistributedTypeParameter(source.AsConditionalType().checkType), target.AsConditionalType().checkType);
            this.inferFromTypes(n, getNonDistributedTypeParameter(source.AsConditionalType().extendsType), target.AsConditionalType().extendsType);
            this.inferFromTypes(n, getNonDistributedTypeParameter(this.getTrueTypeFromConditionalType(source)), this.getTrueTypeFromConditionalType(target));
            this.inferFromTypes(n, getNonDistributedTypeParameter(this.getFalseTypeFromConditionalType(source)), this.getFalseTypeFromConditionalType(target));
        }
        else
        {
            // inference.go:572. Nonescaping two-slot literal; evaluate true before false.
            ReadOnlySpan<Type> targetTypes =
            [
                this.getTrueTypeFromConditionalType(target),
                this.getFalseTypeFromConditionalType(target)
            ];
            this.inferToMultipleTypesWithPriority(n, source, targetTypes, target.flags, Core.IfElse(n.contravariant, InferencePriority.ContravariantConditional, InferencePriority.None));
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:577:1.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:698:1.
    private void inferFromGenericMappedTypes(InferenceState n, Type source, Type target)
    {
        this.inferFromTypes(n, this.getConstraintTypeFromMappedType(source), this.getConstraintTypeFromMappedType(target));
        this.inferFromTypes(n, this.getTemplateTypeFromMappedType(source), this.getTemplateTypeFromMappedType(target));
        var sourceNameType = this.getNameTypeFromMappedType(source);
        var targetNameType = this.getNameTypeFromMappedType(target);
        if (((sourceNameType != null) && (targetNameType != null)))
        {
            this.inferFromTypes(n, sourceNameType, targetNameType);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:710:1.
    private void inferFromObjectTypes(InferenceState n, Type source, Type target)
    {
        if (((((source.objectFlags & ObjectFlags.Reference) != 0) && ((target.objectFlags & ObjectFlags.Reference) != 0)) && (((source.Target() == target.Target()) || (this.isArrayType(source) && this.isArrayType(target))))))
        {
            this.inferFromReferenceTypeArguments(n, source, target);
            return;
        }

        if ((this.isGenericMappedType(source) && this.isGenericMappedType(target)))
        {
            this.inferFromGenericMappedTypes(n, source, target);
        }

        if ((((target.objectFlags & ObjectFlags.Mapped) != 0) && (target.AsMappedType().declaration.NameType == null)))
        {
            var constraintType = this.getConstraintTypeFromMappedType(target);
            if (this.inferToMappedType(n, source, target, constraintType))
            {
                return;
            }
        }

        if (this.typesDefinitelyUnrelated(source, target))
        {
            return;
        }

        if (this.isArrayOrTupleType(source))
        {
            if (isTupleType(target))
            {
                var sourceArity = this.getTypeReferenceArity(source);
                var targetArity = this.getTypeReferenceArity(target);
                var elementTypes = this.getTypeArguments(target);
                var elementInfos = target.TargetTupleType().elementInfos;
                if ((isTupleType(source) && this.isTupleTypeStructureMatching(source, target)))
                {
                    {
                        foreach (var i in GoRuntime.RangeKeys(targetArity))
                        {
                            this.inferFromTypes(n, this.getTypeArguments(source)[i], elementTypes[i]);
                        }
                    }

                    return;
                }

                var startLength = 0;
                var endLength = 0;
                if (isTupleType(source))
                {
                    startLength = Math.Min(source.TargetTupleType().fixedLength, target.TargetTupleType().fixedLength);
                    if (((target.TargetTupleType().combinedFlags & ElementFlags.Variable) != 0))
                    {
                        endLength = Math.Min(getEndElementCount(source.TargetTupleType(), ElementFlags.Fixed), getEndElementCount(target.TargetTupleType(), ElementFlags.Fixed));
                    }
                }

                {
                    foreach (var i2 in GoRuntime.RangeKeys(startLength))
                    {
                        this.inferFromTypes(n, this.getTypeArguments(source)[i2], elementTypes[i2]);
                    }
                }

                if ((!isTupleType(source) || ((((sourceArity - startLength) - endLength) == 1) && ((source.TargetTupleType().elementInfos[startLength].flags & ElementFlags.Rest) != 0))))
                {
                    var restType = this.getTypeArguments(source)[startLength];
                    {
                        var i3 = startLength;
                        for (; (i3 < (targetArity - endLength)); i3++)
                        {
                            var t = restType;
                            if (((elementInfos[i3].flags & ElementFlags.Variadic) != 0))
                            {
                                t = this.createArrayType(t);
                            }

                            this.inferFromTypes(n, t, elementTypes[i3]);
                        }
                    }
                }
                else
                {
                    var middleLength = ((targetArity - startLength) - endLength);
                    if ((middleLength == 2))
                    {
                        if ((((elementInfos[startLength].flags & elementInfos[(startLength + 1)].flags) & ElementFlags.Variadic) != 0))
                        {
                            var targetInfo = getInferenceInfoForType(n, elementTypes[startLength]);
                            if (((targetInfo != null) && (targetInfo.impliedArity >= 0)))
                            {
                                this.inferFromTypes(n, this.sliceTupleType(source, startLength, ((endLength + sourceArity) - targetInfo.impliedArity)), elementTypes[startLength]);
                                this.inferFromTypes(n, this.sliceTupleType(source, (startLength + targetInfo.impliedArity), endLength), elementTypes[(startLength + 1)]);
                            }
                        }
                        else
                        {
                            if ((((elementInfos[startLength].flags & ElementFlags.Variadic) != 0) && ((elementInfos[(startLength + 1)].flags & ElementFlags.Rest) != 0)))
                            {
                                {
                                    var info = getInferenceInfoForType(n, elementTypes[startLength]);
                                    if ((info != null))
                                    {
                                        var constraint = this.getBaseConstraintOfType(info.typeParameter);
                                        if ((((constraint != null) && isTupleType(constraint)) && ((constraint.TargetTupleType().combinedFlags & ElementFlags.Variable) == 0)))
                                        {
                                            var impliedArity = constraint.TargetTupleType().fixedLength;
                                            this.inferFromTypes(n, this.sliceTupleType(source, startLength, (sourceArity - ((startLength + impliedArity)))), elementTypes[startLength]);
                                            {
                                                var restType2 = this.getElementTypeOfSliceOfTupleType(source, (startLength + impliedArity), endLength, false, false);
                                                if ((restType2 != null))
                                                {
                                                    this.inferFromTypes(n, restType2, elementTypes[(startLength + 1)]);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((((elementInfos[startLength].flags & ElementFlags.Rest) != 0) && ((elementInfos[(startLength + 1)].flags & ElementFlags.Variadic) != 0)))
                                {
                                    {
                                        var info2 = getInferenceInfoForType(n, elementTypes[(startLength + 1)]);
                                        if ((info2 != null))
                                        {
                                            var constraint2 = this.getBaseConstraintOfType(info2.typeParameter);
                                            if ((((constraint2 != null) && isTupleType(constraint2)) && ((constraint2.TargetTupleType().combinedFlags & ElementFlags.Variable) == 0)))
                                            {
                                                var impliedArity2 = constraint2.TargetTupleType().fixedLength;
                                                var endIndex = (sourceArity - getEndElementCount(target.TargetTupleType(), ElementFlags.Fixed));
                                                var startIndex = (endIndex - impliedArity2);
                                                if ((startIndex >= startLength))
                                                {
                                                    var trailingSlice = this.createTupleTypeEx(GoRuntime.Slice(this.getTypeArguments(source), startIndex, endIndex), GoRuntime.Slice(source.TargetTupleType().elementInfos, startIndex, endIndex), false);
                                                    {
                                                        var restType3 = this.getElementTypeOfSliceOfTupleType(source, startLength, (endLength + impliedArity2), false, false);
                                                        if ((restType3 != null))
                                                        {
                                                            this.inferFromTypes(n, restType3, elementTypes[startLength]);
                                                        }
                                                    }

                                                    this.inferFromTypes(n, trailingSlice, elementTypes[(startLength + 1)]);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (((middleLength == 1) && ((elementInfos[startLength].flags & ElementFlags.Variadic) != 0)))
                        {
                            var priority = Core.IfElse(((elementInfos[(targetArity - 1)].flags & ElementFlags.Optional) != 0), InferencePriority.SpeculativeTuple, InferencePriority.None);
                            var sourceSlice = this.sliceTupleType(source, startLength, endLength);
                            this.inferWithPriority(n, sourceSlice, elementTypes[startLength], priority);
                        }
                        else
                        {
                            if (((middleLength == 1) && ((elementInfos[startLength].flags & ElementFlags.Rest) != 0)))
                            {
                                var restType4 = this.getElementTypeOfSliceOfTupleType(source, startLength, endLength, false, false);
                                if ((restType4 != null))
                                {
                                    this.inferFromTypes(n, restType4, elementTypes[startLength]);
                                }
                            }
                        }
                    }
                }

                {
                    foreach (var i4 in GoRuntime.RangeKeys(endLength))
                    {
                        this.inferFromTypes(n, this.getTypeArguments(source)[((sourceArity - i4) - 1)], elementTypes[((targetArity - i4) - 1)]);
                    }
                }

                return;
            }

            if (this.isArrayType(target))
            {
                this.inferFromIndexTypes(n, source, target);
                return;
            }
        }

        this.inferFromProperties(n, source, target);
        this.inferFromSignatures(n, source, target, SignatureKind.Call);
        this.inferFromSignatures(n, source, target, SignatureKind.Construct);
        this.inferFromIndexTypes(n, source, target);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:839:1.
    private void inferFromProperties(InferenceState n, Type source, Type target)
    {
        var properties = this.getPropertiesOfObjectType(target);
        {
            foreach (var targetProp in GoRuntime.RangeValues(properties))
            {
                var sourceProp = this.getPropertyOfType(source, targetProp.Name);
                if (((sourceProp != null) && !Core.Some(sourceProp.Declarations, this.isSkipDirectInferenceNode)))
                {
                    this.inferFromTypes(n, this.removeMissingType(this.getTypeOfSymbol(sourceProp), ((sourceProp.Flags & SymbolFlags.Optional) != 0)), this.removeMissingType(this.getTypeOfSymbol(targetProp), ((targetProp.Flags & SymbolFlags.Optional) != 0)));
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:849:1.
    private void inferFromSignatures(InferenceState n, Type source, Type target, SignatureKind kind)
    {
        var sourceSignatures = this.getSignaturesOfType(source, kind);
        var sourceLen = GoRuntime.Len(sourceSignatures);
        if ((sourceLen > 0))
        {
            var targetSignatures = this.getSignaturesOfType(target, kind);
            var targetLen = GoRuntime.Len(targetSignatures);
            {
                foreach (var i in GoRuntime.RangeKeys(targetLen))
                {
                    var sourceIndex = Math.Max(((sourceLen - targetLen) + i), 0);
                    this.inferFromSignature(n, this.getBaseSignature(sourceSignatures[sourceIndex]), this.getErasedSignature(targetSignatures[i]));
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:864:1.
    private void inferFromSignature(InferenceState n, Signature source, Signature target)
    {
        if (((source.flags & SignatureFlags.IsNonInferrable) == 0))
        {
            var saveBivariant = n.bivariant;
            var kind = Kind.Unknown;
            if ((target.declaration != null))
            {
                kind = target.declaration.Kind;
            }

            n.bivariant = (((n.bivariant || (kind == Kind.MethodDeclaration)) || (kind == Kind.MethodSignature)) || (kind == Kind.Constructor));
            // inference.go:864. Synchronous traversal keeps the operation's bivariant state.
            this.applyToParameterTypes(source, target, (checker: this, n), static (s, t, state) =>
            {
                state.checker.inferFromContravariantTypesIfStrictFunctionTypes(state.n, s, t);
            });
            n.bivariant = saveBivariant;
        }

        this.applyToReturnTypes(source, target, (checker: this, n), static (s2, t2, state) =>
        {
            state.checker.inferFromTypes(state.n, s2, t2);
        });
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:879:1.
    private void applyToParameterTypes(Signature source, Signature target, Action<Type, Type> callback) =>
        applyToParameterTypes(source, target, callback, static (s, t, action) => action(s, t));

    // inference.go:879. Explicit value state avoids managed capture frames;
    // legacy callbacks and state callbacks share this original traversal.
    private void applyToParameterTypes<TState>(Signature source, Signature target, TState state, Action<Type, Type, TState> callback)
    {
        var sourceCount = this.getParameterCount(source);
        var targetCount = this.getParameterCount(target);
        var sourceRestType = this.getEffectiveRestType(source);
        var targetRestType = this.getEffectiveRestType(target);
        var targetNonRestCount = targetCount;
        if ((targetRestType != null))
        {
            targetNonRestCount--;
        }

        var paramCount = targetNonRestCount;
        if ((sourceRestType == null))
        {
            paramCount = Math.Min(sourceCount, targetNonRestCount);
        }

        var sourceThisType = this.getThisTypeOfSignature(source);
        if ((sourceThisType != null))
        {
            var targetThisType = this.getThisTypeOfSignature(target);
            if ((targetThisType != null))
            {
                callback(sourceThisType, targetThisType, state);
            }
        }

        {
            foreach (var i in GoRuntime.RangeKeys(paramCount))
            {
                callback(this.getTypeAtPosition(source, i), this.getTypeAtPosition(target, i), state);
            }
        }

        if ((targetRestType != null))
        {
            callback(this.getRestTypeAtPosition(source, paramCount, (this.isConstTypeVariable(targetRestType, 0) && !someType(targetRestType, this.isMutableArrayLikeType))), targetRestType, state);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:907:1.
    private void applyToReturnTypes(Signature source, Signature target, Action<Type, Type> callback) =>
        applyToReturnTypes(source, target, callback, static (s, t, action) => action(s, t));

    // inference.go:907. Keep predicate-before-return ordering with the same
    // explicit-state adapter used by parameter traversal.
    private void applyToReturnTypes<TState>(Signature source, Signature target, TState state, Action<Type, Type, TState> callback)
    {
        var targetTypePredicate = this.getTypePredicateOfSignature(target);
        if ((targetTypePredicate != null))
        {
            var sourceTypePredicate = this.getTypePredicateOfSignature(source);
            if (((((sourceTypePredicate != null) && this.typePredicateKindsMatch(sourceTypePredicate, targetTypePredicate)) && (sourceTypePredicate.t != null)) && (targetTypePredicate.t != null)))
            {
                callback(sourceTypePredicate.t, targetTypePredicate.t, state);
                return;
            }
        }

        var targetReturnType = this.getReturnTypeOfSignature(target);
        if (this.couldContainTypeVariables(targetReturnType))
        {
            callback(this.getReturnTypeOfSignature(source), targetReturnType, state);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:922:1.
    private void inferFromIndexTypes(InferenceState n, Type source, Type target)
    {
        var priority = InferencePriority.None;
        if ((((source.objectFlags & target.objectFlags) & ObjectFlags.Mapped) != 0))
        {
            priority = InferencePriority.HomomorphicMappedType;
        }

        var indexInfos = this.getIndexInfosOfType(target);
        if (this.isObjectTypeWithInferableIndex(source))
        {
            {
                foreach (var targetInfo in GoRuntime.RangeValues(indexInfos))
                {
                    List<Type> propTypes = default;
                    {
                        foreach (var range28 in GoRuntime.Range(this.getPropertiesOfType(source)))
                        {
                            var _2 = range28.Key;
                            var prop = range28.Value;
                            if (this.isApplicableIndexType(this.getLiteralTypeFromProperty(prop, TypeFlags.StringOrNumberLiteralOrUnique, false), targetInfo.keyType))
                            {
                                var propType = this.getTypeOfSymbol(prop);
                                if (((prop.Flags & SymbolFlags.Optional) != 0))
                                {
                                    propType = this.removeMissingOrUndefinedType(propType);
                                }

                                propTypes = GoRuntime.Append(propTypes, propType);
                            }
                        }
                    }

                    {
                        foreach (var range29 in GoRuntime.Range(this.getIndexInfosOfType(source)))
                        {
                            var _3 = range29.Key;
                            var info = range29.Value;
                            if (this.isApplicableIndexType(info.keyType, targetInfo.keyType))
                            {
                                propTypes = GoRuntime.Append(propTypes, info.valueType);
                            }
                        }
                    }

                    if ((GoRuntime.Len(propTypes) != 0))
                    {
                        this.inferWithPriority(n, this.getUnionType(propTypes), targetInfo.valueType, priority);
                    }
                }
            }
        }

        {
            foreach (var range30 in GoRuntime.Range(indexInfos))
            {
                var _4 = range30.Key;
                var targetInfo2 = range30.Value;
                var sourceInfo = this.getApplicableIndexInfo(source, targetInfo2.keyType);
                if ((sourceInfo != null))
                {
                    this.inferWithPriority(n, sourceInfo.valueType, targetInfo2.valueType, priority);
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:959:1.
    private bool inferToMappedType(InferenceState n, Type source, Type target, Type constraintType)
    {
        if ((((constraintType.flags & TypeFlags.Union) != 0) || ((constraintType.flags & TypeFlags.Intersection) != 0)))
        {
            var result = false;
            {
                foreach (var t in GoRuntime.RangeValues(constraintType.Types()))
                {
                    result = this.inferToMappedType(n, source, target, t) || result;
                }
            }

            return result;
        }

        if (((constraintType.flags & TypeFlags.Index) != 0))
        {
            var inference = getInferenceInfoForType(n, constraintType.AsIndexType().target);
            if ((((inference != null) && !inference.isFixed) && !this.isFromInferenceBlockedSource(source)))
            {
                var inferredType = this.inferTypeForHomomorphicMappedType(source, target, constraintType);
                if ((inferredType != null))
                {
                    this.inferWithPriority(n, inferredType, inference.typeParameter, Core.IfElse(((source.objectFlags & ObjectFlags.NonInferrableType) != 0), InferencePriority.PartialHomomorphicMappedType, InferencePriority.HomomorphicMappedType));
                }
            }

            return true;
        }

        if (((constraintType.flags & TypeFlags.TypeParameter) != 0))
        {
            this.inferWithPriority(n, this.getIndexTypeEx(source, Core.IfElse((this.patternForType.ContainsKey(source)), IndexFlags.NoIndexSignatures, IndexFlags.None)), constraintType, InferencePriority.MappedTypeConstraint);
            var extendedConstraint = this.getConstraintOfType(constraintType);
            if (((extendedConstraint != null) && this.inferToMappedType(n, source, target, extendedConstraint)))
            {
                return true;
            }

            var propTypes = Core.Map(this.getPropertiesOfType(source), this.getTypeOfSymbol);
            var indexTypes = Core.Map(this.getIndexInfosOfType(source), (info) =>
            {
                if ((info != this.enumNumberIndexInfo))
                {
                    return info.valueType;
                }

                return this.neverType;
            });
            this.inferFromTypes(n, this.getUnionType(Core.Concatenate(propTypes, indexTypes)), this.getTemplateTypeFromMappedType(target));
            return true;
        }

        return false;
    }

    // Infer a suitable input type for a homomorphic mapped type { [P in keyof T]: X }. We construct
    // an object type with the same set of properties as the source type, where the type of each
    // property is computed by inferring from the source property type to X for the type
    // variable T[P] (i.e. we treat the type T[P] as the type variable we're inferring for).
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1015:1.
    private Type inferTypeForHomomorphicMappedType(Type source, Type target, Type constraint)
    {
        var key = new ReverseMappedTypeKey
        {
            sourceId = source.id,
            targetId = target.id,
            constraintId = constraint.id
        };
        {
            this.reverseHomomorphicMappedCache.TryGetValue(key, out var cached);
            if ((cached != null))
            {
                return cached;
            }
        }

        var t = this.createReverseMappedType(source, target, constraint);
        this.reverseHomomorphicMappedCache[key] = t;
        return t;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1025:1.
    private Type createReverseMappedType(Type source, Type target, Type constraint)
    {
        if (!(((this.getIndexInfoOfType(source, this.stringType) != null) || ((GoRuntime.Len(this.getPropertiesOfType(source)) != 0) && this.isPartiallyInferableType(source)))))
        {
            return null;
        }

        if (this.isArrayType(source))
        {
            var elementType = this.inferReverseMappedType(this.getTypeArguments(source)[0], target, constraint);
            if ((elementType == null))
            {
                return null;
            }

            return this.createArrayTypeEx(elementType, this.isReadonlyArrayType(source));
        }

        if (isTupleType(source))
        {
            var elementTypes = Core.Map(this.getElementTypes(source), (t) =>
            {
                return this.inferReverseMappedType(t, target, constraint);
            });
            if (!Core.Every(elementTypes, (t2) =>
            {
                return (t2 != null);
            }))
            {
                return null;
            }

            var elementInfos = source.TargetTupleType().elementInfos;
            if (((getMappedTypeModifiers(target) & MappedTypeModifiers.IncludeOptional) != 0))
            {
                elementInfos = Core.SameMap(elementInfos, (info) =>
                {
                    if (((info.flags & ElementFlags.Optional) != 0))
                    {
                        return new TupleElementInfo
                        {
                            flags = ElementFlags.Required,
                            labeledDeclaration = info.labeledDeclaration
                        };
                    }

                    return info;
                });
            }

            return this.createTupleTypeEx(elementTypes, elementInfos, source.TargetTupleType().@readonly);
        }

        var reversed = this.newObjectType((ObjectFlags.ReverseMapped | ObjectFlags.Anonymous), null);
        reversed.AsReverseMappedType().source = source;
        reversed.AsReverseMappedType().mappedType = target;
        reversed.AsReverseMappedType().constraintType = constraint;
        return reversed;
    }

    // We consider a type to be partially inferable if it isn't marked non-inferable or if it is
    // an object literal type with at least one property of an inferable type. For example, an object
    // literal { a: 123, b: x => true } is marked non-inferable because it contains a context sensitive
    // arrow function, but is considered partially inferable because property 'a' has an inferable type.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1071:1.
    private bool isPartiallyInferableType(Type t)
    {
        return ((((t.objectFlags & ObjectFlags.NonInferrableType) == 0) || (isObjectLiteralType(t) && Core.Some(this.getPropertiesOfType(t), (prop) =>
        {
            return this.isPartiallyInferableType(this.getTypeOfSymbol(prop));
        }))) || (isTupleType(t) && Core.Some(this.getElementTypes(t), this.isPartiallyInferableType)));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1077:1.
    private Type inferReverseMappedType(Type source, Type target, Type constraint)
    {
        var key = new ReverseMappedTypeKey
        {
            sourceId = source.id,
            targetId = target.id,
            constraintId = constraint.id
        };
        {
            var ok = this.reverseMappedCache.TryGetValue(key, out var cached);
            if (ok)
            {
                return Core.OrElse(cached, this.unknownType);
            }
        }

        this.reverseMappedSourceStack = GoRuntime.Append(this.reverseMappedSourceStack, source);
        this.reverseMappedTargetStack = GoRuntime.Append(this.reverseMappedTargetStack, target);
        var saveExpandingFlags = this.reverseExpandingFlags;
        if (this.isDeeplyNestedType(source, this.reverseMappedSourceStack, 2))
        {
            this.reverseExpandingFlags |= ExpandingFlags.Source;
        }

        if (this.isDeeplyNestedType(target, this.reverseMappedTargetStack, 2))
        {
            this.reverseExpandingFlags |= ExpandingFlags.Target;
        }

        Type t = default;
        if ((this.reverseExpandingFlags != ExpandingFlags.Both))
        {
            t = this.inferReverseMappedTypeWorker(source, target, constraint);
        }

        this.reverseMappedSourceStack = GoRuntime.Slice(this.reverseMappedSourceStack, 0, (GoRuntime.Len(this.reverseMappedSourceStack) - 1));
        this.reverseMappedTargetStack = GoRuntime.Slice(this.reverseMappedTargetStack, 0, (GoRuntime.Len(this.reverseMappedTargetStack) - 1));
        this.reverseExpandingFlags = saveExpandingFlags;
        this.reverseMappedCache[key] = t;
        return t;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1102:1.
    private Type inferReverseMappedTypeWorker(Type source, Type target, Type constraint)
    {
        var typeParameter = this.getIndexedAccessType(constraint.AsIndexType().target, this.getTypeParameterFromMappedType(target));
        var templateType = this.getTemplateTypeFromMappedType(target);
        var inference = newInferenceInfo(typeParameter);
        this.inferTypes(new List<InferenceInfo> { inference }, source, templateType, InferencePriority.None, false);
        return this.getWidenedType(Core.OrElse(this.getTypeFromInference(inference), this.unknownType));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1110:1.
    private void resolveReverseMappedTypeMembers(Type t)
    {
        var r = t.AsReverseMappedType();
        var indexInfo = this.getIndexInfoOfType(r.source, this.stringType);
        var modifiers = getMappedTypeModifiers(r.mappedType);
        var readonlyMask = ((modifiers & MappedTypeModifiers.IncludeReadonly) == 0);
        var optionalMask = Core.IfElse(((modifiers & MappedTypeModifiers.IncludeOptional) != 0), SymbolFlags.None, SymbolFlags.Optional);
        List<IndexInfo> indexInfos = default;
        if ((indexInfo != null))
        {
            indexInfos = new List<IndexInfo>
            {
                this.newIndexInfo(this.stringType, Core.OrElse(this.inferReverseMappedType(indexInfo.valueType, r.mappedType, r.constraintType), this.unknownType), (readonlyMask && indexInfo.isReadonly), null, null)
            };
        }

        var members = new SymbolTable();
        var limitedConstraint = this.getLimitedConstraint(t);
        {
            foreach (var prop in GoRuntime.RangeValues(this.getPropertiesOfType(r.source)))
            {
                if ((limitedConstraint != null))
                {
                    var propertyNameType = this.getLiteralTypeFromProperty(prop, TypeFlags.StringOrNumberLiteralOrUnique, false);
                    if (!this.isTypeAssignableTo(propertyNameType, limitedConstraint))
                    {
                        continue;
                    }
                }

                var checkFlags = (CheckFlags.ReverseMapped | Core.IfElse((readonlyMask && this.isReadonlySymbol(prop)), CheckFlags.Readonly, CheckFlags.None));
                var inferredProp = this.newSymbolEx((SymbolFlags.Property | (prop.Flags & optionalMask)), prop.Name, checkFlags);
                inferredProp.Declarations = prop.Declarations;
                this.valueSymbolLinks.Get(inferredProp).nameType = this.valueSymbolLinks.Get(prop).nameType;
                var links = this.ReverseMappedSymbolLinks.Get(inferredProp);
                links.propertyType = this.getTypeOfSymbol(prop);
                var constraintTarget = r.constraintType.AsIndexType().target;
                if (((((constraintTarget.flags & TypeFlags.IndexedAccess) != 0) && ((constraintTarget.AsIndexedAccessType().objectType.flags & TypeFlags.TypeParameter) != 0)) && ((constraintTarget.AsIndexedAccessType().indexType.flags & TypeFlags.TypeParameter) != 0)))
                {
                    var newTypeParam = constraintTarget.AsIndexedAccessType().objectType;
                    var newMappedType = this.replaceIndexedAccess(r.mappedType, constraintTarget, newTypeParam);
                    links.mappedType = newMappedType;
                    links.constraintType = this.getIndexType(newTypeParam);
                }
                else
                {
                    links.mappedType = r.mappedType;
                    links.constraintType = r.constraintType;
                }

                members[prop.Name] = inferredProp;
            }
        }

        this.setStructuredTypeMembers(t, members, default, default, indexInfos);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1156:1.
    private Type getTypeOfReverseMappedSymbol(Symbol symbol)
    {
        var links = this.valueSymbolLinks.Get(symbol);
        if ((links.resolvedType == null))
        {
            var reverseLinks = this.ReverseMappedSymbolLinks.Get(symbol);
            links.resolvedType = Core.OrElse(this.inferReverseMappedType(reverseLinks.propertyType, reverseLinks.mappedType, reverseLinks.constraintType), this.unknownType);
        }

        return links.resolvedType;
    }

    // If the original mapped type had an intersection constraint we extract its components,
    // and we make an attempt to do so even if the intersection has been reduced to a union.
    // This entire process allows us to possibly retrieve the filtering type literals.
    // e.g. { [K in keyof U & ("a" | "b") ] } -> "a" | "b"
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1169:1.
    private Type getLimitedConstraint(Type t)
    {
        var constraint = this.getConstraintTypeFromMappedType(t.AsReverseMappedType().mappedType);
        if (!((((constraint.flags & TypeFlags.Union) != 0) || ((constraint.flags & TypeFlags.Intersection) != 0))))
        {
            return null;
        }

        var origin = constraint;
        if (((constraint.flags & TypeFlags.Union) != 0))
        {
            origin = constraint.AsUnionType().origin;
        }

        if (((origin == null) || ((origin.flags & TypeFlags.Intersection) == 0)))
        {
            return null;
        }

        var constraintType = t.AsReverseMappedType().constraintType;
        var limitedConstraint = this.getIntersectionType(Core.Filter(origin.Types(), (t2) =>
        {
            return (t2 != constraintType);
        }));
        if ((limitedConstraint != this.neverType))
        {
            return limitedConstraint;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1189:1.
    private Type replaceIndexedAccess(Type instantiable, Type t, Type replacement)
    {
        return this.instantiateType(instantiable, newTypeMapper(new List<Type> { t.AsIndexedAccessType().indexType, t.AsIndexedAccessType().objectType }, new List<Type> { this.getNumberLiteralType(0), this.createTupleType(new List<Type> { replacement }) }));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1196:1.
    private bool typesDefinitelyUnrelated(Type source, Type target)
    {
        if ((isTupleType(source) && isTupleType(target)))
        {
            return tupleTypesDefinitelyUnrelated(source, target);
        }

        return ((this.getUnmatchedProperty(source, target, false, true) != null) && (this.getUnmatchedProperty(target, source, false, false) != null));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1206:1.
    private static bool tupleTypesDefinitelyUnrelated(Type source, Type target)
    {
        var s = source.TargetTupleType();
        var t = target.TargetTupleType();
        return ((((t.combinedFlags & ElementFlags.Variadic) == 0) && (t.minLength > s.minLength)) || (((t.combinedFlags & ElementFlags.Variable) == 0) && ((((s.combinedFlags & ElementFlags.Variable) != 0) || (t.fixedLength < s.fixedLength)))));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1213:1.
    private bool isTupleTypeStructureMatching(Type t1, Type t2)
    {
        if ((this.getTypeReferenceArity(t1) != this.getTypeReferenceArity(t2)))
        {
            return false;
        }

        {
            foreach (var range33 in GoRuntime.Range(t1.TargetTupleType().elementInfos))
            {
                var i = range33.Key;
                var e = range33.Value;
                if (((e.flags & ElementFlags.Variable) != (t2.TargetTupleType().elementInfos[i].flags & ElementFlags.Variable)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1225:1.
    private bool isTypeOrBaseIdenticalTo(Type s, Type t)
    {
        if ((t == this.missingType))
        {
            return (s == t);
        }

        return ((this.isTypeIdenticalTo(s, t) || (((t.flags & TypeFlags.String) != 0) && ((s.flags & TypeFlags.StringLiteral) != 0))) || (((t.flags & TypeFlags.Number) != 0) && ((s.flags & TypeFlags.NumberLiteral) != 0)));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1234:1.
    private bool isTypeCloselyMatchedBy(Type s, Type t)
    {
        return ((((((s.flags & TypeFlags.Object) != 0) && ((t.flags & TypeFlags.Object) != 0)) && (s.symbol != null)) && (s.symbol == t.symbol)) || ((((s.alias != null) && (t.alias != null)) && (GoRuntime.Len(s.alias.typeArguments) != 0)) && (s.alias.symbol == t.alias.symbol)));
    }

    // Create an object with properties named in the string literal type. Every property has type `any`.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1240:1.
    private Type createEmptyObjectTypeFromStringLiteral(Type t)
    {
        var members = new SymbolTable();
        {
            foreach (var t2 in GoRuntime.RangeValues(t.Distributed()))
            {
                if (((t2.flags & TypeFlags.StringLiteral) == 0))
                {
                    continue;
                }

                var name = getStringLiteralValue(t2);
                var literalProp = this.newSymbol(SymbolFlags.Property, name);
                this.valueSymbolLinks.Get(literalProp).resolvedType = this.anyType;
                if ((t2.symbol != null))
                {
                    literalProp.Declarations = t2.symbol.Declarations;
                    literalProp.ValueDeclaration = t2.symbol.ValueDeclaration;
                }

                members[name] = literalProp;
            }
        }

        List<IndexInfo> indexInfos = default;
        if (((t.flags & TypeFlags.String) != 0))
        {
            indexInfos = new List<IndexInfo>
            {
                this.newIndexInfo(this.stringType, this.emptyObjectType, false, null, null)
            };
        }

        return this.newAnonymousType(null, members, default, default, indexInfos);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1262:1.
    private InferenceContext newInferenceContext(List<Type> typeParameters, Signature signature, InferenceFlags flags, TypeComparer compareTypes)
    {
        if ((compareTypes == null))
        {
            compareTypes = this.compareTypesAssignable;
        }

        return this.newInferenceContextWorker(Core.Map(typeParameters, newInferenceInfo), signature, flags, compareTypes);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1269:1.
    private InferenceContext cloneInferenceContext(InferenceContext n, InferenceFlags extraFlags)
    {
        if ((n == null))
        {
            return null;
        }

        return this.newInferenceContextWorker(Core.Map(n.inferences, cloneInferenceInfo), n.signature, (n.flags | extraFlags), n.compareTypes);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1276:1.
    private InferenceContext cloneInferredPartOfContext(InferenceContext n)
    {
        var inferences = Core.Filter(n.inferences, hasInferenceCandidates);
        if ((GoRuntime.Len(inferences) == 0))
        {
            return null;
        }

        return this.newInferenceContextWorker(Core.Map(inferences, cloneInferenceInfo), n.signature, n.flags, n.compareTypes);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1284:1.
    private InferenceContext newInferenceContextWorker(List<InferenceInfo> inferences, Signature signature, InferenceFlags flags, TypeComparer compareTypes)
    {
        var n = new InferenceContext
        {
            inferences = inferences,
            signature = signature,
            flags = flags,
            compareTypes = compareTypes
        };
        n.mapper = this.newInferenceTypeMapper(n, true);
        n.nonFixingMapper = this.newInferenceTypeMapper(n, false);
        return n;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1296:1.
    private void addIntraExpressionInferenceSite(InferenceContext n, Node node, Type t)
    {
        n.intraExpressionInferenceSites = GoRuntime.Append(n.intraExpressionInferenceSites, new IntraExpressionInferenceSite(node, t));
    }

    // We collect intra-expression inference sites within object and array literals to handle cases where
    // inferred types flow between context sensitive element expressions. For example:
    //
    //	declare function foo<T>(arg: [(n: number) => T, (x: T) => void]): void;
    //	foo([_a => 0, n => n.toFixed()]);
    //
    // Above, both arrow functions in the tuple argument are context sensitive, thus both are omitted from the
    // pass that collects inferences from the non-context sensitive parts of the arguments. In the subsequent
    // pass where nothing is omitted, we need to commit to an inference for T in order to contextually type the
    // parameter in the second arrow function, but we want to first infer from the return type of the first
    // arrow function. This happens automatically when the arrow functions are discrete arguments (because we
    // infer from each argument before processing the next), but when the arrow functions are elements of an
    // object or array literal, we need to perform intra-expression inferences early.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1313:1.
    internal void inferFromIntraExpressionSites(InferenceContext n)
    {
        {
            foreach (var site in GoRuntime.RangeValues(n.intraExpressionInferenceSites))
            {
                Type contextualType = default;
                if (Ast.IsMethodDeclaration(site.node))
                {
                    contextualType = this.getContextualTypeForObjectLiteralMethod(site.node, ContextFlags.NoConstraints);
                }
                else
                {
                    contextualType = this.getContextualType(site.node, ContextFlags.NoConstraints);
                }

                if ((contextualType != null))
                {
                    this.inferTypes(n.inferences, site.t, contextualType, InferencePriority.None, false);
                }
            }
        }

        n.intraExpressionInferenceSites = null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1328:1.
    internal Type getInferredType(InferenceContext n, int index)
    {
        var inference = n.inferences[index];
        if ((inference.inferredType == null))
        {
            if ((inference.typeParameter == this.errorType))
            {
                return inference.typeParameter;
            }

            Type inferredType = default;
            Type fallbackType = default;
            if ((n.signature != null))
            {
                Type inferredCovariantType = default;
                if ((GoRuntime.Len(inference.candidates) != 0))
                {
                    inferredCovariantType = this.getCovariantInference(inference, n.signature);
                }

                Type inferredContravariantType = default;
                if ((GoRuntime.Len(inference.contraCandidates) != 0))
                {
                    inferredContravariantType = this.getContravariantInference(inference);
                }

                if (((inferredCovariantType != null) || (inferredContravariantType != null)))
                {
                    var preferCovariantType = ((inferredCovariantType != null) && (((inferredContravariantType == null) || ((((inferredCovariantType.flags & ((TypeFlags.Never | TypeFlags.Any))) == 0) && Core.Some(inference.contraCandidates, (t) =>
                    {
                        return this.isTypeAssignableTo(inferredCovariantType, t);
                    })) && Core.Every(n.inferences, (other) =>
                    {
                        return (((other != inference) && (this.getConstraintOfTypeParameter(other.typeParameter) != inference.typeParameter)) || Core.Every(other.candidates, (t2) =>
                        {
                            return this.isTypeAssignableTo(t2, inferredCovariantType);
                        }));
                    })))));
                    if (preferCovariantType)
                    {
                        inferredType = inferredCovariantType;
                        fallbackType = inferredContravariantType;
                    }
                    else
                    {
                        inferredType = inferredContravariantType;
                        fallbackType = inferredCovariantType;
                    }
                }
                else
                {
                    if (((n.flags & InferenceFlags.NoDefault) != 0))
                    {
                        inferredType = this.silentNeverType;
                    }
                    else
                    {
                        var defaultType = this.getDefaultFromTypeParameter(inference.typeParameter);
                        if ((defaultType != null))
                        {
                            inferredType = this.instantiateType(defaultType, mergeTypeMappers(this.newBackreferenceMapper(n, index), n.nonFixingMapper));
                        }
                    }
                }
            }
            else
            {
                inferredType = this.getTypeFromInference(inference);
            }

            inference.inferredType = inferredType;
            if ((inference.inferredType == null))
            {
                inference.inferredType = Core.IfElse(((n.flags & InferenceFlags.AnyDefault) != 0), this.anyType, this.unknownType);
            }

            var constraint = this.getConstraintOfTypeParameter(inference.typeParameter);
            if ((constraint != null))
            {
                var instantiatedConstraint = this.instantiateType(constraint, n.nonFixingMapper);
                if (((inferredType != null) && ((((n.flags & InferenceFlags.NoConstraintChecks) == 0) || (inference.priority == InferencePriority.ReturnType)))))
                {
                    var constraintWithThis = this.getTypeWithThisArgument(instantiatedConstraint, inferredType, false);
                    if ((n.compareTypes(inferredType, constraintWithThis, false) == Ternary.False))
                    {
                        Type filteredByConstraint = default;
                        if ((inference.priority == InferencePriority.ReturnType))
                        {
                            filteredByConstraint = this.mapType(inferredType, (t3) =>
                            {
                                return Core.IfElse((n.compareTypes(t3, constraintWithThis, false) != Ternary.False), t3, this.neverType);
                            });
                        }

                        inferredType = Core.IfElse(((filteredByConstraint != null) && ((filteredByConstraint.flags & TypeFlags.Never) == 0)), filteredByConstraint, null);
                    }
                }

                if ((inferredType == null))
                {
                    inferredType = Core.IfElse(((fallbackType != null) && (n.compareTypes(fallbackType, this.getTypeWithThisArgument(instantiatedConstraint, fallbackType, false), false) != Ternary.False)), fallbackType, instantiatedConstraint);
                }

                inference.inferredType = inferredType;
            }

            this.clearActiveMapperCaches();
        }

        return inference.inferredType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1418:1.
    private List<Type> getInferredTypes(InferenceContext n)
    {
        var result = GoRuntime.MakeSlice<Type>(GoRuntime.Len(n.inferences));
        {
            foreach (var i in GoRuntime.RangeKeys(n.inferences))
            {
                result[i] = this.getInferredType(n, i);
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1426:1.
    private TypeMapper getMapperFromContext(InferenceContext n)
    {
        if ((n == null))
        {
            return null;
        }

        return n.mapper;
    }

    // Return a type mapper that combines the context's return mapper with a mapper that erases any additional type parameters
    // to their inferences at the time of creation.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1435:1.
    private TypeMapper createOuterReturnMapper(InferenceContext context)
    {
        if ((context.outerReturnMapper == null))
        {
            var mapper = this.cloneInferenceContext(context, InferenceFlags.None).mapper;
            if ((context.returnMapper != null))
            {
                mapper = newMergedTypeMapper(context.returnMapper, mapper);
            }

            context.outerReturnMapper = mapper;
        }

        return context.outerReturnMapper;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1446:1.
    private Type getCovariantInference(InferenceInfo inference, Signature signature)
    {
        var candidates = this.unionObjectAndArrayLiteralCandidates(inference.candidates);
        var primitiveConstraint = (this.hasPrimitiveConstraint(inference.typeParameter) || this.isConstTypeVariable(inference.typeParameter, 0));
        var widenLiteralTypes = ((!primitiveConstraint && inference.topLevel) && ((inference.isFixed || !this.isTypeParameterAtTopLevelInReturnType(signature, inference.typeParameter))));
        List<Type> baseCandidates = default;
        switch (true)
        {
            case true when primitiveConstraint:
            {
                baseCandidates = Core.SameMap(candidates, this.getRegularTypeOfLiteralType);
                break;
            }

            case true when widenLiteralTypes:
            {
                baseCandidates = Core.SameMap(candidates, this.getWidenedLiteralType);
                break;
            }

            default:
            {
                baseCandidates = candidates;
                break;
            }
        }

        Type unwidenedType = default;
        if (((inference.priority & InferencePriority.PriorityImpliesCombination) != 0))
        {
            unwidenedType = this.getUnionTypeEx(baseCandidates, UnionReduction.Subtype, null, null);
        }
        else
        {
            unwidenedType = this.getCommonSupertype(baseCandidates);
        }

        return this.getWidenedType(unwidenedType);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1475:1.
    private Type getContravariantInference(InferenceInfo inference)
    {
        if (((inference.priority & InferencePriority.PriorityImpliesCombination) != 0))
        {
            return this.getIntersectionType(inference.contraCandidates);
        }

        return this.getCommonSubtype(inference.contraCandidates);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1482:1.
    private List<Type> unionObjectAndArrayLiteralCandidates(List<Type> candidates)
    {
        if ((GoRuntime.Len(candidates) > 1))
        {
            var objectLiterals = Core.Filter(candidates, isObjectOrArrayLiteralType);
            if ((GoRuntime.Len(objectLiterals) != 0))
            {
                var literalsType = this.getUnionTypeEx(objectLiterals, UnionReduction.Subtype, null, null);
                var nonLiteralTypes = Core.Filter(candidates, (t) =>
                {
                    return !isObjectOrArrayLiteralType(t);
                });
                return Core.Concatenate(nonLiteralTypes, new List<Type> { literalsType });
            }
        }

        return candidates;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1494:1.
    private bool hasPrimitiveConstraint(Type t)
    {
        var constraint = this.getConstraintOfTypeParameter(t);
        if ((constraint != null))
        {
            if (((constraint.flags & TypeFlags.Conditional) != 0))
            {
                constraint = this.getDefaultConstraintOfConditionalType(constraint);
            }

            return this.maybeTypeOfKind(constraint, (((TypeFlags.Primitive | TypeFlags.Index) | TypeFlags.TemplateLiteral) | TypeFlags.StringMapping));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1505:1.
    private bool isTypeParameterAtTopLevel(Type t, Type tp, int depth)
    {
        return (((t == tp) || (((t.flags & TypeFlags.UnionOrIntersection) != 0) && Core.Some(t.Types(), (t2) =>
        {
            return this.isTypeParameterAtTopLevel(t2, tp, depth);
        }))) || (((depth < 3) && ((t.flags & TypeFlags.Conditional) != 0)) && ((this.isTypeParameterAtTopLevel(this.getTrueTypeFromConditionalType(t), tp, (depth + 1)) || this.isTypeParameterAtTopLevel(this.getFalseTypeFromConditionalType(t), tp, (depth + 1))))));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1513:1.
    private bool isTypeParameterAtTopLevelInReturnType(Signature signature, Type typeParameter)
    {
        var typePredicate = this.getTypePredicateOfSignature(signature);
        if ((typePredicate != null))
        {
            return ((typePredicate.t != null) && this.isTypeParameterAtTopLevel(typePredicate.t, typeParameter, 0));
        }

        return this.isTypeParameterAtTopLevel(this.getReturnTypeOfSignature(signature), typeParameter, 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1521:1.
    internal Type getTypeFromInference(InferenceInfo inference)
    {
        switch (true)
        {
            case true when (GoRuntime.Len(inference.candidates) != 0):
            {
                return this.getUnionTypeEx(inference.candidates, UnionReduction.Subtype, null, null);
            }

            case true when (GoRuntime.Len(inference.contraCandidates) != 0):
            {
                return this.getIntersectionType(inference.contraCandidates);
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1531:1.
    private static InferenceInfo getInferenceInfoForType(InferenceState n, Type t)
    {
        if (((t.flags & TypeFlags.TypeVariable) != 0))
        {
            t = getNonDistributedTypeParameter(t);
            {
                foreach (var inference in GoRuntime.RangeValues(n.inferences))
                {
                    if ((t == inference.typeParameter))
                    {
                        return inference;
                    }
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1543:1.
    private Type getCommonSupertype(List<Type> types)
    {
        if ((GoRuntime.Len(types) == 1))
        {
            return types[0];
        }

        var primaryTypes = types;
        if (this.strictNullChecks)
        {
            primaryTypes = Core.SameMap(types, (t) =>
            {
                return this.filterType(t, (u) =>
                {
                    return ((u.flags & TypeFlags.Nullable) == 0);
                });
            });
        }

        Type supertype = default;
        if (this.literalTypesWithSameBaseType(primaryTypes))
        {
            supertype = this.getUnionType(primaryTypes);
        }
        else
        {
            supertype = this.getSingleCommonSupertype(primaryTypes);
        }

        if (Core.Same(primaryTypes, types))
        {
            return supertype;
        }

        return this.getNullableType(supertype, (this.getCombinedTypeFlags(types) & TypeFlags.Nullable));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1570:1.
    private Type getSingleCommonSupertype(List<Type> types)
    {
        var candidate = this.findLeftmostType(types, (c, s, t) => c.isTypeStrictSubtypeOf(s, t));
        if (Core.Every(types, (t) =>
        {
            return ((t == candidate) || this.isTypeStrictSubtypeOf(t, candidate));
        }))
        {
            return candidate;
        }

        return this.findLeftmostType(types, (c, s, t) => c.isTypeSubtypeOf(s, t));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1581:1.
    private Type findLeftmostType(List<Type> types, Func<Checker, Type, Type, bool> f)
    {
        Type candidate = default;
        {
            foreach (var t2 in GoRuntime.RangeValues(types))
            {
                if (((candidate == null) || f(this, candidate, t2)))
                {
                    candidate = t2;
                }
            }
        }

        return candidate;
    }

    // Return the leftmost type for which no type to the right is a subtype.
    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1592:1.
    private Type getCommonSubtype(List<Type> types)
    {
        Type subtype = default;
        {
            foreach (var t in GoRuntime.RangeValues(types))
            {
                if (((subtype == null) || this.isTypeSubtypeOf(t, subtype)))
                {
                    subtype = t;
                }
            }
        }

        return subtype;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1602:1.
    private TypeFlags getCombinedTypeFlags(List<Type> types)
    {
        var flags = TypeFlags.None;
        {
            foreach (var t in GoRuntime.RangeValues(types))
            {
                if (((t.flags & TypeFlags.Union) != 0))
                {
                    flags |= this.getCombinedTypeFlags(t.Types());
                }
                else
                {
                    flags |= t.flags;
                }
            }
        }

        return flags;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1614:1.
    private bool literalTypesWithSameBaseType(List<Type> types)
    {
        Type commonBaseType = default;
        {
            foreach (var t in GoRuntime.RangeValues(types))
            {
                if (((t.flags & TypeFlags.Never) == 0))
                {
                    var baseType = this.getBaseTypeOfLiteralType(t);
                    if ((commonBaseType == null))
                    {
                        commonBaseType = baseType;
                    }

                    if (((baseType == t) || (baseType != commonBaseType)))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1630:1.
    private bool isFromInferenceBlockedSource(Type t)
    {
        return ((t.symbol != null) && Core.Some(t.symbol.Declarations, this.isSkipDirectInferenceNode));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1634:1.
    private bool isSkipDirectInferenceNode(Node node)
    {
        return this.skipDirectInferenceNodes?.Contains(node) == true;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1638:1.
    private static InferenceInfo newInferenceInfo(Type typeParameter)
    {
        return new InferenceInfo
        {
            typeParameter = typeParameter,
            priority = InferencePriority.MaxValue,
            topLevel = true,
            impliedArity = -1
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1642:1.
    private static InferenceInfo cloneInferenceInfo(InferenceInfo info)
    {
        return new InferenceInfo
        {
            typeParameter = info.typeParameter,
            candidates = GoSlices.Clone(info.candidates),
            contraCandidates = GoSlices.Clone(info.contraCandidates),
            inferredType = info.inferredType,
            priority = info.priority,
            topLevel = info.topLevel,
            isFixed = info.isFixed,
            impliedArity = info.impliedArity
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1655:1.
    internal static void clearCachedInferences(List<InferenceInfo> inferences)
    {
        {
            foreach (var inference in GoRuntime.RangeValues(inferences))
            {
                if (!inference.isFixed)
                {
                    inference.inferredType = null;
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1663:1.
    private static bool hasInferenceCandidates(InferenceInfo info)
    {
        return ((GoRuntime.Len(info.candidates) != 0) || (GoRuntime.Len(info.contraCandidates) != 0));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1667:1.
    private static bool hasInferenceCandidatesOrDefault(InferenceInfo info)
    {
        return (hasInferenceCandidates(info) || hasInferenceTypeParameterDefault(info.typeParameter));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1671:1.
    private static bool hasInferenceTypeParameterDefault(Type tp)
    {
        if ((tp.symbol != null))
        {
            {
                foreach (var d in GoRuntime.RangeValues(tp.symbol.Declarations))
                {
                    if ((Ast.IsTypeParameterDeclaration(d) && (d.AsTypeParameterDeclaration().DefaultType != null)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1682:1.
    private static bool hasOverlappingInferences(List<InferenceInfo> a, List<InferenceInfo> b)
    {
        {
            foreach (var i in GoRuntime.RangeKeys(a))
            {
                if ((hasInferenceCandidates(a[i]) && hasInferenceCandidates(b[i])))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/inference.go:1691:1.
    private void mergeInferences(List<InferenceInfo> target, List<InferenceInfo> source)
    {
        {
            foreach (var i in GoRuntime.RangeKeys(target))
            {
                if ((!hasInferenceCandidates(target[i]) && hasInferenceCandidates(source[i])))
                {
                    target[i] = source[i];
                }
            }
        }
    }
}

public sealed partial class Checker
{
    private void inferToTemplateLiteralType(InferenceState n, Type source, TemplateLiteralType target)
    {
        var matches = this.inferTypesFromTemplateLiteralType(source, target, this.compareTypesAssignable);
        var types = target.types;
        if (((GoRuntime.Len(matches) != 0) || Core.Every(target.texts, (s) =>
        {
            return (s == "");
        })))
        {
            {
                foreach (var range1 in GoRuntime.Range(types))
                {
                    var i = range1.Key;
                    var target2 = range1.Value;
                    Type source2 = default;
                    if ((GoRuntime.Len(matches) != 0))
                    {
                        source2 = matches[i];
                    }
                    else
                    {
                        source2 = this.neverType;
                    }

                    if ((((source2.flags & TypeFlags.StringLiteral) != 0) && ((target2.flags & TypeFlags.TypeVariable) != 0)))
                    {
                        {
                            var inferenceContext = getInferenceInfoForType(n, target2);
                            if ((inferenceContext != null))
                            {
                                {
                                    var constraint = this.getBaseConstraintOfType(inferenceContext.typeParameter);
                                    if (((constraint != null) && !IsTypeAny(constraint)))
                                    {
                                        var allTypeFlags = TypeFlags.None;
                                        {
                                            foreach (var t in GoRuntime.RangeValues(constraint.Distributed()))
                                            {
                                                allTypeFlags |= t.flags;
                                            }
                                        }

                                        if (((allTypeFlags & TypeFlags.String) == 0))
                                        {
                                            var str = getStringLiteralValue(source2);
                                            if ((((allTypeFlags & TypeFlags.NumberLike) != 0) && !isValidNumberString(str, true)))
                                            {
                                                allTypeFlags &= ~(TypeFlags.NumberLike);
                                            }

                                            if ((((allTypeFlags & TypeFlags.BigIntLike) != 0) && !isValidBigIntString(str, true)))
                                            {
                                                allTypeFlags &= ~(TypeFlags.BigIntLike);
                                            }

                                            Func<Type, Type, Type> choose = (left, right) =>
                                            {
                                                switch (true)
                                                {
                                                    case true when ((right.flags & allTypeFlags) == 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when ((left.flags & TypeFlags.String) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when ((right.flags & TypeFlags.String) != 0):
                                                    {
                                                        return source2;
                                                    }

                                                    case true when ((left.flags & TypeFlags.TemplateLiteral) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when (((right.flags & TypeFlags.TemplateLiteral) != 0) && this.isTypeMatchedByTemplateLiteralType(source2, right.AsTemplateLiteralType(), this.compareTypesAssignable)):
                                                    {
                                                        return source2;
                                                    }

                                                    case true when ((left.flags & TypeFlags.StringMapping) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when ((right.flags & TypeFlags.StringMapping) != 0 && str == applyStringMapping(right.symbol, str)):
                                                    {
                                                        return source2;
                                                    }

                                                    case true when ((left.flags & TypeFlags.StringLiteral) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when (((right.flags & TypeFlags.StringLiteral) != 0) && (getStringLiteralValue(right) == str)):
                                                    {
                                                        return right;
                                                    }

                                                    case true when ((left.flags & TypeFlags.Number) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when ((right.flags & TypeFlags.Number) != 0):
                                                    {
                                                        return this.getNumberLiteralType(JsNum.FromString(str));
                                                    }

                                                    case true when ((left.flags & TypeFlags.Enum) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when ((right.flags & TypeFlags.Enum) != 0):
                                                    {
                                                        return this.getNumberLiteralType(JsNum.FromString(str));
                                                    }

                                                    case true when ((left.flags & TypeFlags.NumberLiteral) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when (((right.flags & TypeFlags.NumberLiteral) != 0) && (getNumberLiteralValue(right) == JsNum.FromString(str))):
                                                    {
                                                        return right;
                                                    }

                                                    case true when ((left.flags & TypeFlags.BigInt) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when ((right.flags & TypeFlags.BigInt) != 0):
                                                    {
                                                        return this.parseBigIntLiteralType(str);
                                                    }

                                                    case true when ((left.flags & TypeFlags.BigIntLiteral) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when (((right.flags & TypeFlags.BigIntLiteral) != 0) && (pseudoBigIntToString(getBigIntLiteralValue(right)) == str)):
                                                    {
                                                        return right;
                                                    }

                                                    case true when ((left.flags & TypeFlags.Boolean) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when ((right.flags & TypeFlags.Boolean) != 0):
                                                    {
                                                        switch (true)
                                                        {
                                                            case true when (str == "true"):
                                                            {
                                                                return this.trueType;
                                                            }

                                                            case true when (str == "false"):
                                                            {
                                                                return this.falseType;
                                                            }

                                                            default:
                                                            {
                                                                return this.booleanType;
                                                            }
                                                        }
                                                    }

                                                    case true when ((left.flags & TypeFlags.BooleanLiteral) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when (((right.flags & TypeFlags.BooleanLiteral) != 0) && (Core.IfElse(getBooleanLiteralValue(right), "true", "false") == str)):
                                                    {
                                                        return right;
                                                    }

                                                    case true when ((left.flags & TypeFlags.Undefined) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when (((right.flags & TypeFlags.Undefined) != 0) && (right.AsIntrinsicType().intrinsicName == str)):
                                                    {
                                                        return right;
                                                    }

                                                    case true when ((left.flags & TypeFlags.Null) != 0):
                                                    {
                                                        return left;
                                                    }

                                                    case true when (((right.flags & TypeFlags.Null) != 0) && (right.AsIntrinsicType().intrinsicName == str)):
                                                    {
                                                        return right;
                                                    }

                                                    default:
                                                    {
                                                        return left;
                                                    }
                                                }
                                            };
                                            var matchingType = this.neverType;
                                            {
                                                foreach (var range3 in GoRuntime.Range(constraint.Distributed()))
                                                {
                                                    var _2 = range3.Key;
                                                    var t2 = range3.Value;
                                                    matchingType = choose(matchingType, t2);
                                                }
                                            }

                                            if (((matchingType.flags & TypeFlags.Never) == 0))
                                            {
                                                this.inferFromTypes(n, matchingType, target2);
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    this.inferFromTypes(n, source2, target2);
                }
            }
        }
    }
}
