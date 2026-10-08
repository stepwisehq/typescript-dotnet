// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/mapper.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Original mapper models/factories; inference-context mappers excluded by source contract.
#nullable disable
namespace TypeScript.Checking;
internal enum TypeMapperKind : int
{
    Unknown,
    Simple,
    Array,
    Merged
}

internal sealed class TypeMapper
{
    internal TypeMapperData data;
    internal Type Map(Type t) => data.Map(t);
    internal TypeMapperKind Kind() => data.Kind();
    internal bool MapsThisOnly() => data.MapsThisOnly();
}

internal abstract class TypeMapperData
{
    internal readonly TypeMapper mapper;
    protected TypeMapperData()
    {
        mapper = new()
        {
            data = this
        };
    }

    internal abstract Type Map(Type t);
    internal virtual TypeMapperKind Kind() => TypeMapperKind.Unknown;
    internal virtual bool MapsThisOnly() => false;
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
    internal List<Type> sources, targets;
    internal override Type Map(Type t)
    {
        for (int i = 0; i < (sources?.Count ?? 0); i++)
            if (t == sources[i])
                return targets[i];
        return t;
    }

    internal override TypeMapperKind Kind() => TypeMapperKind.Array;
    internal override bool MapsThisOnly() => sources?.Count == 1 && Checker.isThisTypeParameter(sources[0]);
}

internal sealed class ArrayToSingleTypeMapper : TypeMapperBase
{
    internal List<Type> sources;
    internal Type target;
    internal override Type Map(Type t) => sources?.Contains(t) == true ? target : t;
    internal override bool MapsThisOnly() => sources?.Count == 1 && Checker.isThisTypeParameter(sources[0]);
}

internal sealed class DeferredTypeMapper : TypeMapperBase
{
    internal List<Type> sources;
    internal List<Func<Type>> targets;
    internal override Type Map(Type t)
    {
        for (int i = 0; i < (sources?.Count ?? 0); i++)
            if (t == sources[i])
                return targets[i]();
        return t;
    }

    internal override bool MapsThisOnly() => sources?.Count == 1 && Checker.isThisTypeParameter(sources[0]);
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
    internal static bool isThisTypeParameter(Type t) => (t.flags & TypeFlags.TypeParameter) != 0 && t.AsTypeParameter().isThisType;
    private static Type getNonDistributedTypeParameter(Type t) => (t.flags & TypeFlags.TypeParameter) != 0 && t.AsTypeParameter().isDistributed ? t.AsTypeParameter().constraint : t;
    internal static Type getMappedType(Type t, TypeMapper mapper) => mapper.Map(getNonDistributedTypeParameter(t));
    private static TypeMapper newSimpleTypeMapper(Type source, Type target) => new SimpleTypeMapper
    {
        source = source,
        target = target
    }.mapper;
    private static TypeMapper newArrayTypeMapper(List<Type> sources, List<Type> targets) => new ArrayTypeMapper
    {
        sources = sources,
        targets = targets
    }.mapper;
    private static TypeMapper newArrayToSingleTypeMapper(List<Type> sources, Type target) => new ArrayToSingleTypeMapper
    {
        sources = sources,
        target = target
    }.mapper;
    private static TypeMapper newDeferredTypeMapper(List<Type> sources, List<Func<Type>> targets) => new DeferredTypeMapper
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
    private static TypeMapper newTypeMapper(List<Type> sources, List<Type> targets) => sources?.Count == 1 ? newSimpleTypeMapper(sources[0], targets[0]) : newArrayTypeMapper(sources, targets);
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
    private static TypeMapper prependTypeMapping(Type source, Type target, TypeMapper mapper) => mapper == null ? newSimpleTypeMapper(getNonDistributedTypeParameter(source), target) : newMergedTypeMapper(newSimpleTypeMapper(getNonDistributedTypeParameter(source), target), mapper);
    private static TypeMapper appendTypeMapping(TypeMapper mapper, Type source, Type target) => mapper == null ? newSimpleTypeMapper(getNonDistributedTypeParameter(source), target) : newMergedTypeMapper(mapper, newSimpleTypeMapper(getNonDistributedTypeParameter(source), target));
}
