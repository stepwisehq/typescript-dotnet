// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/mapper.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Original selected mapper models/factories, including generic, hidden-this and
// fixing/nonfixing inference instantiation. Checker owns the inference state.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.Checking;
internal enum TypeMapperKind : int
{
    Unknown,
    Simple,
    Array,
    Merged
}

internal class TypeMapper
{
    // Every real mapper payload inherits this header; a bare mapper has no payload.
    internal TypeMapperData data => this as TypeMapperData;
    internal Type Map(Type t) => data.Map(t);
    internal TypeMapperKind Kind() => data.Kind();
    internal bool MapsThisOnly() => data.MapsThisOnly();
}

// mapper.go:102 embeds the TypeMapper header in each mapper payload. Managed
// inheritance retains one identity; wrapper dispatch and payload virtuals stay distinct.
internal abstract class TypeMapperData : TypeMapper
{
    internal TypeMapper mapper => this;
    internal new abstract Type Map(Type t);
    internal new virtual TypeMapperKind Kind() => TypeMapperKind.Unknown;
    internal new virtual bool MapsThisOnly() => false;
}

internal class TypeMapperBase : TypeMapperData
{
    internal override Type Map(Type t) => t;
}

internal sealed class SimpleTypeMapper : TypeMapperBase
{
    internal Type source, target;
    internal override Type Map(Type t) => t == source ? target : t;
    internal override TypeMapperKind Kind() => TypeMapperKind.Simple;
    internal override bool MapsThisOnly() => Checker.isThisTypeParameter(source);
}

internal sealed class ArrayTypeMapper : TypeMapperBase
{
    internal Slice<Type> sources, targets;
    internal override Type Map(Type t)
    {
        for (int i = 0; i < sources.Length; i++)
            if (t == sources[i])
                return targets[i];
        return t;
    }

    internal override TypeMapperKind Kind() => TypeMapperKind.Array;
    internal override bool MapsThisOnly() => sources.Length == 1 && Checker.isThisTypeParameter(sources[0]);
}

internal sealed class ArrayToSingleTypeMapper : TypeMapperBase
{
    internal Slice<Type> sources;
    internal Type target;
    internal override Type Map(Type t) => GoSlices.Contains(sources, t) ? target : t;
    internal override bool MapsThisOnly() => sources.Length == 1 && Checker.isThisTypeParameter(sources[0]);
}

internal sealed class DeferredTypeMapper : TypeMapperBase
{
    internal Slice<Type> sources;
    internal List<Func<Type>> targets;
    internal override Type Map(Type t)
    {
        for (int i = 0; i < sources.Length; i++)
            if (t == sources[i])
                return targets[i]();
        return t;
    }

    internal override bool MapsThisOnly() => sources.Length == 1 && Checker.isThisTypeParameter(sources[0]);
}

internal sealed class FunctionTypeMapper : TypeMapperBase
{
    internal Func<Type, Type> fn;
    internal override Type Map(Type t) => fn(t);
}

internal sealed class MergedTypeMapper : TypeMapperBase
{
    internal TypeMapper m1, m2;
    internal override Type Map(Type t) => m2.Map(m1.Map(t));
    internal override TypeMapperKind Kind() => TypeMapperKind.Merged;
}

internal sealed class CompositeTypeMapper : TypeMapperBase
{
    internal Checker c;
    internal TypeMapper m1, m2;
    internal override Type Map(Type t)
    {
        var t1 = m1.Map(t);
        if (t1 != t)
            return c.instantiateType(t1, m2);
        return m2.Map(t);
    }
}

public sealed partial class Checker
{
    // Authored explicit representation of the sole checker.go:17530 deferred
    // target closure. Invocation preserves the original captured inputs; the
    // verification accessor below observes them without invoking the function.
    private sealed class InferredConstraintDeferredTarget
    {
        internal Checker checker;
        internal TypeScript.Scanner.Node parent;
        internal Slice<Type> typeParameters;
        internal int index;
        internal Type Resolve() => checker.getEffectiveTypeArgumentAtIndex(parent, typeParameters, index);
    }

    private Func<Type> newInferredConstraintDeferredTarget(TypeScript.Scanner.Node parent, Slice<Type> typeParameters, int index)
    {
        var closure = new InferredConstraintDeferredTarget
        {
            checker = this,
            parent = parent,
            typeParameters = typeParameters,
            index = index
        };
        return closure.Resolve;
    }

    internal (TypeScript.Scanner.Node Parent, List<Type> TypeParameters, int Index) VerificationCollectionsDeferredTarget(Func<Type> target)
    {
        if (target?.Target is not InferredConstraintDeferredTarget closure || target != closure.Resolve)
            throw new InvalidOperationException("Unknown deferred target closure provenance");
        if (!ReferenceEquals(closure.checker, this) || closure.parent == null || closure.typeParameters.IsNil || closure.index < 0 || closure.index >= closure.typeParameters.Count)
            throw new InvalidOperationException("Absent, foreign or inconsistent deferred target captures");
        return (closure.parent, GoSlices.ToListOrNull(closure.typeParameters), closure.index);
    }

    internal static bool isThisTypeParameter(Type t) => (t.flags & TypeFlags.TypeParameter) != 0 && t.AsTypeParameter().isThisType;
    internal static Type getNonDistributedTypeParameter(Type t) => (t.flags & TypeFlags.TypeParameter) != 0 && t.AsTypeParameter().isDistributed ? t.AsTypeParameter().constraint : t;
    internal static Type getMappedType(Type t, TypeMapper mapper) => mapper.Map(getNonDistributedTypeParameter(t));
    private static TypeMapper newSimpleTypeMapper(Type source, Type target) => new SimpleTypeMapper
    {
        source = source,
        target = target
    }.mapper;
    private static TypeMapper newArrayTypeMapper(Slice<Type> sources, Slice<Type> targets) => new ArrayTypeMapper
    {
        sources = sources,
        targets = targets
    }.mapper;
    private static TypeMapper newArrayToSingleTypeMapper(Slice<Type> sources, Type target) => new ArrayToSingleTypeMapper
    {
        sources = sources,
        target = target
    }.mapper;
    private static TypeMapper newDeferredTypeMapper(Slice<Type> sources, List<Func<Type>> targets) => new DeferredTypeMapper
    {
        sources = sources,
        targets = targets
    }.mapper;
    private static TypeMapper newFunctionTypeMapper(Func<Type, Type> fn) => new FunctionTypeMapper
    {
        fn = fn
    }.mapper;
    private static TypeMapper newMergedTypeMapper(TypeMapper m1, TypeMapper m2) => new MergedTypeMapper
    {
        m1 = m1,
        m2 = m2
    }.mapper;
    private static TypeMapper newCompositeTypeMapper(Checker c, TypeMapper m1, TypeMapper m2) => new CompositeTypeMapper
    {
        c = c,
        m1 = m1,
        m2 = m2
    }.mapper;
    internal static TypeMapper newTypeMapper(Slice<Type> sources, Slice<Type> targets) => sources.Length == 1 ? newSimpleTypeMapper(sources[0], targets[0]) : newArrayTypeMapper(sources, targets);
    internal TypeMapper combineTypeMappers(TypeMapper m1, TypeMapper m2) => m1 != null ? newCompositeTypeMapper(this, m1, m2) : m2;
    private Type mapTypeWithCompositeMapper(Type t, TypeMapper m1, TypeMapper m2)
    {
        if (m1 == null)
            return getMappedType(t, m2);
        var t1 = getMappedType(t, m1);
        if (t1 != t)
            return instantiateType(t1, m2);
        return getMappedType(t, m2);
    }

    private static TypeMapper mergeTypeMappers(TypeMapper m1, TypeMapper m2) => m1 != null ? newMergedTypeMapper(m1, m2) : m2;
    internal static TypeMapper prependTypeMapping(Type source, Type target, TypeMapper mapper) => mapper == null ? newSimpleTypeMapper(getNonDistributedTypeParameter(source), target) : newMergedTypeMapper(newSimpleTypeMapper(getNonDistributedTypeParameter(source), target), mapper);
    private static TypeMapper appendTypeMapping(TypeMapper mapper, Type source, Type target) => mapper == null ? newSimpleTypeMapper(getNonDistributedTypeParameter(source), target) : newMergedTypeMapper(mapper, newSimpleTypeMapper(getNonDistributedTypeParameter(source), target));
}

// mapper.go:300/317. Fixing first consumes intra-expression sites before locking
// an inference; the nonfixing mapper must leave its candidate set open.
internal sealed class InferenceTypeMapper : TypeMapperBase
{
    internal Checker c;
    internal Checker.InferenceContext n;
    internal bool fixing;
    internal override Type Map(Type t)
    {
        int count = TypeScript.Scanner.GoRuntime.Len(n.inferences);
        for (int i = 0; i < count; i++)
        {
            var inference = n.inferences[i];
            if (t == inference.typeParameter)
            {
                if (fixing && !inference.isFixed)
                {
                    c.inferFromIntraExpressionSites(n);
                    Checker.clearCachedInferences(n.inferences);
                    inference.isFixed = true;
                }

                return c.getInferredType(n, i);
            }
        }

        return t;
    }
}

public sealed partial class Checker
{
    // mapper.go:92. Later default-parameter references map to unknown during inference.
    private TypeMapper newBackreferenceMapper(InferenceContext context, int index)
    {
        var forwardInferences = TypeScript.Scanner.GoRuntime.Slice(context.inferences, index);
        var typeParameters = TypeScript.Scanner.Core.MapSlice(forwardInferences, inference => inference.typeParameter);
        return newArrayToSingleTypeMapper(typeParameters, unknownType);
    }

    // mapper.go:307. Both mappers share the real mutable inference context.
    private TypeMapper newInferenceTypeMapper(InferenceContext n, bool fixing) => new InferenceTypeMapper
    {
        c = this,
        n = n,
        fixing = fixing
    }.mapper;
}
