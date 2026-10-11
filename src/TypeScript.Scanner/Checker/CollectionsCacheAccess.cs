// Authored reflection-free Collections verification support.
// Validates actual stored reference-instantiation associations against the
// original getTypeListKey boundary without resolving or allocating entities.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // Inline key descriptions, not new semantic reference/parameter objects.
    // The generalized key intentionally erases unconstrained parameter identity.
    internal sealed record CollectionsRelationArgument(string Kind, int Index, Type Type, CollectionsRelationReference Reference);
    internal sealed record CollectionsRelationReference(Type Target, List<CollectionsRelationArgument> Arguments);
    // Original relation keys can erase input reference/parameter identities.
    // Retain the latest actual producer inputs solely for passive ID lookup.
    private sealed record CollectionsRelationInput(Type Source, Type Target, bool Invalid);
    // Authored existing Collections compatibility only; genuine Program keys carry no preimages.
    private Dictionary<CacheHashKey, byte[]> collectionsKeyPreimages;
    private void verificationCollectionsCaptureKey(CacheHashKey key, ReadOnlySpan<byte> bytes)
    {
        if (!authoredCollectionsProfile) return;
        var preimages = collectionsKeyPreimages ??= new();
        if (preimages.TryGetValue(key, out var old))
        {
            if (!bytes.SequenceEqual(old)) throw new InvalidOperationException("Ambiguous observed cache hash preimage");
            return;
        }
        preimages.Add(key, bytes.ToArray());
    }

    private byte[] verificationCollectionsKeyBytes(CacheHashKey key)
    {
        if (!authoredCollectionsProfile || collectionsKeyPreimages == null ||
            !collectionsKeyPreimages.TryGetValue(key, out var bytes) || CacheHashKey.FromBytes(bytes) != key)
            throw new InvalidOperationException("Missing or mismatched observed cache hash preimage");
        return bytes;
    }

    private readonly Dictionary<CacheHashKey, CollectionsRelationInput> collectionsRelationInputs = new();
    private void verificationCollectionsCaptureRelationInputs(Type source, Type target, CacheHashKey key)
    {
        if (!authoredCollectionsProfile)
            return;
        bool invalid = source.checker != null && !ReferenceEquals(source.checker, this) || target.checker != null && !ReferenceEquals(target.checker, this);
        if (collectionsRelationInputs.TryGetValue(key, out var old))
            invalid |= old.Invalid;
        // Different inputs may legitimately have the same generalized key.
        // Their erased identities are never asserted as cache-key terms.
        collectionsRelationInputs[key] = new(source, target, invalid);
    }

    internal List<Type> VerificationCollectionsRelationInputs()
    {
        var values = new List<Type>();
        foreach (var input in collectionsRelationInputs.Values)
        {
            if (input.Invalid || input.Source == null || input.Target == null || input.Source.id == 0 || input.Target.id == 0 || input.Source.checker != null && !ReferenceEquals(input.Source.checker, this) || input.Target.checker != null && !ReferenceEquals(input.Target.checker, this))
                throw new InvalidOperationException("Invalid actual relation input provenance");
            values.Add(input.Source);
            values.Add(input.Target);
        }

        return values;
    }

    internal List<(string Name, bool IsNull, int Count, List<(Type Source, Type Target, Type Constraint, Type Value)> Entries)> VerificationCollectionsReverseMappedCaches(IReadOnlyDictionary<uint, Type> knownTypes)
    {
        var caches = new (string Name, Dictionary<ReverseMappedTypeKey, Type> Cache)[]
        {
            ("reverse-homomorphic-mapped", reverseHomomorphicMappedCache),
            ("reverse-mapped", reverseMappedCache),
        };
        var output = new List<(string Name, bool IsNull, int Count, List<(Type Source, Type Target, Type Constraint, Type Value)> Entries)>();
        Type resolve(uint id)
        {
            if (id == 0 || !knownTypes.TryGetValue(id, out var type) || type == null || type.id != id || type.checker != null && !ReferenceEquals(type.checker, this))
                throw new InvalidOperationException("Unrooted, unallocated or foreign reverse-mapped cache key term");
            return type;
        }

        foreach (var(name, cache)in caches)
        {
            if (cache == null)
            {
                output.Add((name, true, 0, null));
                continue;
            }

            int count = cache.Count;
            var keys = new HashSet<ReverseMappedTypeKey>();
            var entries = new List<(Type Source, Type Target, Type Constraint, Type Value)>();
            foreach (var(key, value)in cache)
            {
                var source = resolve(key.sourceId);
                var target = resolve(key.targetId);
                var constraint = resolve(key.constraintId);
                if (value != null && value.checker != null && !ReferenceEquals(value.checker, this))
                    throw new InvalidOperationException("Foreign reverse-mapped cache value");
                var rebuilt = new ReverseMappedTypeKey
                {
                    sourceId = source.id,
                    targetId = target.id,
                    constraintId = constraint.id
                };
                if (rebuilt != key || !keys.Add(rebuilt) || !cache.TryGetValue(rebuilt, out var found) || !ReferenceEquals(found, value))
                    throw new InvalidOperationException("Reverse-mapped cache key association mismatch");
                // A present key with null value is retained, not treated as absent.
                entries.Add((source, target, constraint, value));
            }

            if (cache.Count != count || keys.Count != count || entries.Count != count)
                throw new InvalidOperationException("Reverse-mapped cache cardinality changed");
            output.Add((name, false, count, entries));
        }

        return output;
    }

    internal List<(string Name, bool IsNull, int Count, List<(string Family, uint Result, uint IntersectionState, Type Source, Type Target, CollectionsRelationReference SourceReference, CollectionsRelationReference TargetReference)> Entries)> VerificationCollectionsRelations(IReadOnlyDictionary<uint, Type> knownTypes)
    {
        var caches = new (string Name, Relation Relation)[]
        {
            ("assignable", assignableRelation),
            ("comparable", comparableRelation),
            ("identity", identityRelation),
            ("subtype", subtypeRelation),
            ("strict-subtype", strictSubtypeRelation),
        };
        var output = new List<(string Name, bool IsNull, int Count, List<(string Family, uint Result, uint IntersectionState, Type Source, Type Target, CollectionsRelationReference SourceReference, CollectionsRelationReference TargetReference)> Entries)>();
        foreach (var(name, relation)in caches)
        {
            var cache = relation.VerificationResults;
            if (cache == null)
            {
                output.Add((name, true, 0, null));
                continue;
            }

            int count = cache.Count;
            var keys = new HashSet<CacheHashKey>();
            var entries = new List<(string Family, uint Result, uint IntersectionState, Type Source, Type Target, CollectionsRelationReference SourceReference, CollectionsRelationReference TargetReference)>();
            foreach (var(key, result)in cache)
            {
                var bytes = verificationCollectionsKeyBytes(key);
                if (bytes.Length < 13)
                    throw new InvalidOperationException("Noncanonical or truncated completed relation key");
                int offset = 1;
                int end = bytes.Length - sizeof(uint);
                var builder = new keyBuilder();
                byte tag = bytes[0];
                builder.writeByte(tag);
                Type readType()
                {
                    if (offset > end - sizeof(uint))
                        throw new InvalidOperationException("Truncated completed relation type term");
                    uint id = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)));
                    offset += sizeof(uint);
                    if (id == 0 || !knownTypes.TryGetValue(id, out var type) || type == null || type.id != id || type.checker != null && !ReferenceEquals(type.checker, this))
                        throw new InvalidOperationException("Unrooted, unallocated or foreign completed relation type term");
                    builder.writeType(type);
                    return type;
                }

                Type source = null, target = null;
                CollectionsRelationReference sourceReference = null, targetReference = null;
                string family;
                if (tag == (byte)'s')
                {
                    family = "simple";
                    source = readType();
                    target = readType();
                    if (name == "identity" && source.id > target.id)
                        throw new InvalidOperationException("Unnormalized identity relation key");
                }
                else if (tag == (byte)'g')
                {
                    family = "generic-reference";
                    int parameterCount = 0;
                    CollectionsRelationReference readReference(int depth, byte terminator)
                    {
                        if (depth > 4)
                            throw new InvalidOperationException("Completed relation reference exceeds original key depth");
                        var referenceTarget = readType();
                        var arguments = new List<CollectionsRelationArgument>();
                        while (offset < end && bytes[offset] != terminator)
                        {
                            byte kind = bytes[offset++];
                            builder.writeByte(kind);
                            if (kind == (byte)'=')
                            {
                                if (offset > end - sizeof(ulong))
                                    throw new InvalidOperationException("Truncated normalized relation parameter term");
                                ulong index = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset, sizeof(ulong)));
                                offset += sizeof(ulong);
                                if (index > (ulong)parameterCount)
                                    throw new InvalidOperationException("Unknown normalized relation parameter index");
                                if (index == (ulong)parameterCount)
                                    parameterCount++;
                                builder.writeUint64(index);
                                arguments.Add(new("parameter", (int)index, null, null));
                            }
                            else if (kind == (byte)'-')
                                arguments.Add(new("type", -1, readType(), null));
                            else if (kind == (byte)'<')
                            {
                                var nested = readReference(depth + 1, (byte)'>');
                                if (offset >= end || bytes[offset++] != (byte)'>')
                                    throw new InvalidOperationException("Missing completed relation reference terminator");
                                builder.writeByte((byte)'>');
                                arguments.Add(new("reference", -1, null, nested));
                            }
                            else
                                throw new InvalidOperationException("Unknown completed relation argument term");
                        }

                        return new(referenceTarget, arguments);
                    }

                    sourceReference = readReference(0, (byte)',');
                    if (offset >= end || bytes[offset++] != (byte)',')
                        throw new InvalidOperationException("Missing completed relation direction separator");
                    builder.writeByte((byte)',');
                    targetReference = readReference(0, 0);
                }
                else
                    throw new InvalidOperationException("Unknown completed relation key family");
                if (offset != end)
                    throw new InvalidOperationException("Trailing completed relation key terms");
                uint state = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(end, sizeof(uint)));
                if ((state & ~(uint)(IntersectionState.Source | IntersectionState.Target)) != 0)
                    throw new InvalidOperationException("Unknown completed relation intersection state");
                builder.writeUint32(state);
                var rebuilt = builder.hash();
                const uint resultMask = (uint)(RelationComparisonResult.Succeeded | RelationComparisonResult.Failed | RelationComparisonResult.ReportsMask | RelationComparisonResult.Overflow);
                if (((uint)result & ~resultMask) != 0 || result == RelationComparisonResult.None)
                    throw new InvalidOperationException("Unknown completed relation result flags");
                if (rebuilt != key || !keys.Add(rebuilt) || !cache.TryGetValue(rebuilt, out var found) || found != result)
                    throw new InvalidOperationException("Completed relation key association mismatch");
                entries.Add((family, (uint)result, state, source, target, sourceReference, targetReference));
            }

            if (cache.Count != count || keys.Count != count || entries.Count != count)
                throw new InvalidOperationException("Completed relation cache cardinality changed");
            output.Add((name, false, count, entries));
        }

        return output;
    }

    internal (bool IsNull, int Count, List<(Type Value, List<TupleElementInfo> Elements, bool Readonly)> Entries) VerificationCollectionsTupleTargets()
    {
        if (tupleTypes == null)
            return (true, 0, null);
        int count = tupleTypes.Count;
        var entries = new List<(Type Value, List<TupleElementInfo> Elements, bool Readonly)>(count);
        var keys = new HashSet<CacheHashKey>();
        foreach (var(key, value)in tupleTypes)
        {
            var tuple = value?.AsTupleType();
            if (tuple == null || value.checker != null && !ReferenceEquals(value.checker, this) || !ReferenceEquals(tuple.target, value))
                throw new InvalidOperationException("Absent, foreign or nontarget tuple cache value");
            foreach (var element in tuple.elementInfos)
                if (element.labeledDeclaration != null && element.labeledDeclaration.id == 0)
                    throw new InvalidOperationException("Tuple cache label has no already allocated node ID");
            // getTupleKey can only read existing label IDs after this guard.
            var rebuilt = getTupleKey(tuple.elementInfos, tuple.@readonly);
            if (rebuilt != key || !keys.Add(rebuilt) || !tupleTypes.TryGetValue(rebuilt, out var found) || !ReferenceEquals(found, value))
                throw new InvalidOperationException("Tuple target key association mismatch");
            entries.Add((value, GoSlices.ToListOrNull(tuple.elementInfos), tuple.@readonly));
        }

        if (tupleTypes.Count != count || keys.Count != count || entries.Count != count)
            throw new InvalidOperationException("Tuple target cache cardinality changed");
        return (false, count, entries);
    }

    internal (bool IsNull, int Count, List<(Signature Owner, Signature Value, List<Type> Arguments, string Family)> Entries) VerificationCollectionsCachedSignatures(IReadOnlyDictionary<uint, Type> knownTypes)
    {
        if (cachedSignatures == null)
            return (true, 0, null);
        int count = cachedSignatures.Count;
        var entries = new List<(Signature Owner, Signature Value, List<Type> Arguments, string Family)>(count);
        var keys = new HashSet<CachedSignatureKey>();
        foreach (var(key, value)in cachedSignatures)
        {
            if (key.sig == null || value == null)
                throw new InvalidOperationException("Signature cache owner or value is absent");
            string family;
            List<Type> arguments = null;
            CacheHashKey rebuilt;
            if (key.key == SignatureKeyBase)
            {
                family = "base";
                rebuilt = SignatureKeyBase;
            }
            else if (key.key == SignatureKeyCanonical)
            {
                family = "canonical";
                rebuilt = SignatureKeyCanonical;
            }
            else if (key.key == SignatureKeyErased)
            {
                family = "erased";
                rebuilt = SignatureKeyErased;
            }
            else if (key.key == SignatureKeyInner)
            {
                family = "inner";
                rebuilt = SignatureKeyInner;
            }
            else if (key.key == SignatureKeyOuter)
            {
                family = "outer";
                rebuilt = SignatureKeyOuter;
            }
            else
            {
                family = "type-list";
                arguments = verificationCollectionsTypeList(key.key, knownTypes);
                rebuilt = getTypeListKey(arguments);
            }

            var reconstructed = new CachedSignatureKey(key.sig, rebuilt);
            if (reconstructed != key || !keys.Add(reconstructed) || !cachedSignatures.TryGetValue(reconstructed, out var found) || !ReferenceEquals(found, value))
                throw new InvalidOperationException("Signature cache key association mismatch");
            entries.Add((key.sig, value, arguments, family));
        }

        if (cachedSignatures.Count != count || keys.Count != count || entries.Count != count)
            throw new InvalidOperationException("Signature cache cardinality changed");
        return (false, count, entries);
    }

    private List<Type> verificationCollectionsTypeList(CacheHashKey key, IReadOnlyDictionary<uint, Type> knownTypes)
    {
        var bytes = verificationCollectionsKeyBytes(key);
        if (bytes.Length < sizeof(ulong))
            throw new InvalidOperationException("Noncanonical or truncated type-list cache key");
        ulong count = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        if (count != (ulong)((bytes.Length - sizeof(ulong)) / sizeof(uint)) || (bytes.Length - sizeof(ulong)) % sizeof(uint) != 0)
            throw new InvalidOperationException("Unknown type-list cache key encoding");
        var arguments = new List<Type>((int)count);
        for (int i = 0; i < (int)count; i++)
        {
            uint id = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(sizeof(ulong) + i * sizeof(uint), sizeof(uint)));
            if (id == 0 || !knownTypes.TryGetValue(id, out var type) || type == null || type.id != id || type.checker != null && !ReferenceEquals(type.checker, this))
                throw new InvalidOperationException("Unrooted, unallocated or foreign type-list cache argument");
            arguments.Add(type);
        }

        return arguments;
    }

    internal (bool IsNull, int Count, List<(CacheHashKey Key, Type Value, List<Type> Arguments, Type Target, bool IsSelf)> Entries) VerificationCollectionsReferenceInstantiations(Type owner)
    {
        if (owner == null || owner.AsObjectType() == null)
            throw new InvalidOperationException("Reference cache owner is not an actual object type");
        if (owner.checker != null && !ReferenceEquals(owner.checker, this))
            throw new InvalidOperationException("Foreign reference cache owner");
        var cache = owner.AsObjectType().instantiations;
        if (cache == null)
            return (true, 0, null);
        var entries = new List<(CacheHashKey Key, Type Value, List<Type> Arguments, Type Target, bool IsSelf)>(cache.Count);
        if (cache.Count == 0)
            return (false, 0, entries);
        if (owner.AsInterfaceType() == null || (owner.objectFlags & ObjectFlags.Reference) == 0 || !ReferenceEquals(owner.AsTypeReference().target, owner))
            throw new InvalidOperationException("Nonempty object cache is outside the retained reference-target family");
        var reconstructedKeys = new HashSet<CacheHashKey>();
        foreach (var(key, value)in cache)
        {
            var reference = value?.AsTypeReference();
            if (reference == null || !ReferenceEquals(reference.target, owner))
                throw new InvalidOperationException("Stored reference cache value has a different target");
            if (value.checker != null && !ReferenceEquals(value.checker, this))
                throw new InvalidOperationException("Foreign reference cache value");
            var arguments = reference.resolvedTypeArguments;
            foreach (var argument in arguments)
            {
                if (argument == null || argument.id == 0 || argument.checker != null && !ReferenceEquals(argument.checker, this))
                    throw new InvalidOperationException("Reference cache argument is absent, unallocated or foreign");
            }

            // getTypeListKey writes stored IDs only. Nil and empty arguments
            // retain their original slots even though the source key uses len.
            var reconstructedKey = getTypeListKey(arguments);
            if (key != reconstructedKey || !reconstructedKeys.Add(reconstructedKey))
                throw new InvalidOperationException("Reference cache key does not uniquely match its stored arguments");
            if (!cache.TryGetValue(reconstructedKey, out var found) || !ReferenceEquals(found, value))
                throw new InvalidOperationException("Reference cache lookup does not return its stored value");
            entries.Add((key, value, GoSlices.ToListOrNull(arguments), reference.target, ReferenceEquals(value, owner)));
        }

        if (entries.Count != cache.Count || reconstructedKeys.Count != cache.Count)
            throw new InvalidOperationException("Reference cache cardinality changed during observation");
        return (false, cache.Count, entries);
    }

    // Authored key-sequence mirror. Go instrumentation retains the corresponding
    // original producer inputs; the named-only sidecar retains the complete preimage.
    // The key erases nil versus empty input slices and alias object identity, so
    // this API exposes only encoded sequences and the actual alias symbol.
    internal (bool IsNull, int Count, List<(CacheHashKey Key, Type Value, List<Type> Arguments, Symbol AliasSymbol, List<Type> AliasArguments, string Family, bool ForConstraint)> Entries) VerificationCollectionsInstantiations(object owner, IReadOnlyDictionary<uint, Type> knownTypes, IReadOnlyDictionary<ulong, Symbol> knownSymbols) => verificationCollectionsKeySequences(owner, knownTypes, knownSymbols);
    // Mapped and deferred-reference results may reduce to values that no longer
    // retain the original mapper arguments. Read the complete stored key instead.
    internal (bool IsNull, int Count, List<(CacheHashKey Key, Type Value, List<Type> Arguments, Symbol AliasSymbol, List<Type> AliasArguments, string Family, bool SingleSignature, bool IsSelf)> Entries) VerificationCollectionsReducedObjectInstantiations(Type owner, IReadOnlyDictionary<uint, Type> knownTypes, IReadOnlyDictionary<ulong, Symbol> knownSymbols)
    {
        var decoded = verificationCollectionsKeySequences(owner, knownTypes, knownSymbols);
        if (decoded.IsNull)
            return (true, 0, null);
        var entries = new List<(CacheHashKey Key, Type Value, List<Type> Arguments, Symbol AliasSymbol, List<Type> AliasArguments, string Family, bool SingleSignature, bool IsSelf)>(decoded.Count);
        foreach (var entry in decoded.Entries)
            entries.Add((entry.Key, entry.Value, entry.Arguments, entry.AliasSymbol, entry.AliasArguments, entry.Family, entry.ExtraFlag, ReferenceEquals(entry.Value, owner)));
        return (false, decoded.Count, entries);
    }

    private (bool IsNull, int Count, List<(CacheHashKey Key, Type Value, List<Type> Arguments, Symbol AliasSymbol, List<Type> AliasArguments, string Family, bool ExtraFlag)> Entries) verificationCollectionsKeySequences(object owner, IReadOnlyDictionary<uint, Type> knownTypes, IReadOnlyDictionary<ulong, Symbol> knownSymbols)
    {
        Dictionary<CacheHashKey, Type> cache;
        Memory<TypeAliasLinks> aliasLocation = default;
        bool conditional;
        string objectFamily = null;
        switch (owner)
        {
            case Memory<TypeAliasLinks> location:
                cache = location.Span[0].instantiations;
                aliasLocation = location;
                conditional = false;
                break;
            case ConditionalRoot root:
                cache = root.instantiations;
                conditional = true;
                break;
            case Type type when type.AsObjectType() != null && (type.objectFlags & ObjectFlags.Mapped) != 0:
                if (type.checker != null && !ReferenceEquals(type.checker, this))
                    throw new InvalidOperationException("Foreign mapped cache owner");
                cache = type.AsObjectType().instantiations;
                conditional = false;
                objectFamily = "mapped";
                break;
            case Type type when type.AsTypeReference() != null && type.AsTypeReference().node != null && type.AsInterfaceType() == null:
                if (type.checker != null && !ReferenceEquals(type.checker, this))
                    throw new InvalidOperationException("Foreign deferred-reference cache owner");
                cache = type.AsObjectType().instantiations;
                conditional = false;
                objectFamily = "deferred-reference";
                break;
            default:
                throw new InvalidOperationException("Unknown instantiation key-sequence owner");
        }

        if (cache == null)
            return (true, 0, null);
        var entries = new List<(CacheHashKey Key, Type Value, List<Type> Arguments, Symbol AliasSymbol, List<Type> AliasArguments, string Family, bool ForConstraint)>(cache.Count);
        var used = new HashSet<CacheHashKey>();
        int initialCount = cache.Count;
        foreach (var(key, value)in cache)
        {
            if (value == null || value.checker != null && !ReferenceEquals(value.checker, this))
                throw new InvalidOperationException("Absent or foreign instantiation value");
            var bytes = verificationCollectionsKeyBytes(key);
            int offset = 0;
            ulong ReadUint64()
            {
                if (bytes.Length - offset < sizeof(ulong))
                    throw new InvalidOperationException("Truncated instantiation key integer");
                ulong result = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset, sizeof(ulong)));
                offset += sizeof(ulong);
                return result;
            }

            List<Type> ReadTypes()
            {
                ulong count = ReadUint64();
                if (count > (ulong)((bytes.Length - offset) / sizeof(uint)))
                    throw new InvalidOperationException("Invalid instantiation key type count");
                var types = new List<Type>((int)count);
                for (int i = 0; i < (int)count; i++)
                {
                    uint id = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)));
                    offset += sizeof(uint);
                    if (id == 0 || !knownTypes.TryGetValue(id, out var type) || type == null || type.id != id || type.checker != null && !ReferenceEquals(type.checker, this))
                        throw new InvalidOperationException("Unrooted, unallocated or foreign instantiation key type");
                    types.Add(type);
                }

                return types;
            }

            var arguments = ReadTypes();
            Symbol aliasSymbol = null;
            List<Type> aliasArguments = null;
            bool forConstraint = false;
            bool seed = offset == bytes.Length;
            if (seed)
            {
                if (aliasLocation.IsEmpty || GoRuntime.Len(aliasLocation.Span[0].typeParameters) != arguments.Count)
                    throw new InvalidOperationException("Unknown seed instantiation key");
                for (int i = 0; i < arguments.Count; i++)
                    if (!ReferenceEquals(arguments[i], aliasLocation.Span[0].typeParameters[i]))
                        throw new InvalidOperationException("Alias seed does not contain its actual declared parameters");
            }
            else
            {
                byte hasAlias = bytes[offset++];
                if (hasAlias == 1)
                {
                    ulong symbolId = ReadUint64();
                    if (symbolId == 0 || !knownSymbols.TryGetValue(symbolId, out aliasSymbol) || aliasSymbol == null || unchecked((ulong)aliasSymbol.id) != symbolId)
                        throw new InvalidOperationException("Unrooted or unallocated instantiation alias symbol");
                    aliasArguments = ReadTypes();
                }
                else if (hasAlias != 0)
                    throw new InvalidOperationException("Unknown instantiation alias tag");
                if ((conditional || objectFamily != null) && offset < bytes.Length && bytes[offset] == (byte)'!')
                {
                    offset++;
                    forConstraint = true;
                }

                if (offset != bytes.Length)
                    throw new InvalidOperationException("Unrecognized instantiation key suffix");
            }

            var rebuilt = new keyBuilder();
            rebuilt.writeTypes(arguments);
            if (!seed)
            {
                rebuilt.writeByte(aliasSymbol == null ? (byte)0 : (byte)1);
                if (aliasSymbol != null)
                {
                    // Existing stored ID only: no lazy symbol ID allocation.
                    rebuilt.writeUint64(unchecked((ulong)aliasSymbol.id));
                    rebuilt.writeTypes(aliasArguments);
                }

                if (forConstraint)
                    rebuilt.writeByte((byte)'!');
            }

            var reconstructedKey = rebuilt.hash();
            if (reconstructedKey != key || !used.Add(reconstructedKey) || !cache.TryGetValue(reconstructedKey, out var found) || !ReferenceEquals(found, value))
                throw new InvalidOperationException("Instantiation key-sequence membership mismatch");
            entries.Add((key, value, arguments, aliasSymbol, aliasArguments, objectFamily ?? (conditional ? "conditional" : seed ? "alias-seed" : "alias"), forConstraint));
        }

        if (entries.Count != initialCount || used.Count != initialCount || cache.Count != initialCount)
            throw new InvalidOperationException("Instantiation key-sequence cardinality changed");
        return (false, initialCount, entries);
    }

    // Authored full object-cache mirror. Anonymous arguments are retained by the
    // original Simple/Array mapping, not recovered by invoking a mapper.
    internal (bool IsNull, int Count, List<(CacheHashKey Key, Type Value, List<Type> Arguments, TypeAlias Alias, bool SingleSignature, string Family, Type Target, bool IsSelf)> Entries) VerificationCollectionsObjectInstantiations(Type owner)
    {
        if (owner == null || owner.AsObjectType() == null || owner.checker != null && !ReferenceEquals(owner.checker, this))
            throw new InvalidOperationException("Absent, nonobject or foreign object cache owner");
        var cache = owner.AsObjectType().instantiations;
        if (cache == null)
            return (true, 0, null);
        var entries = new List<(CacheHashKey Key, Type Value, List<Type> Arguments, TypeAlias Alias, bool SingleSignature, string Family, Type Target, bool IsSelf)>(cache.Count);
        if (cache.Count == 0)
            return (false, 0, entries);
        if (owner.AsInterfaceType() != null && (owner.objectFlags & ObjectFlags.Reference) != 0)
        {
            var references = VerificationCollectionsReferenceInstantiations(owner);
            foreach (var entry in references.Entries)
                entries.Add((entry.Key, entry.Value, entry.Arguments, null, false, "reference", entry.Target, entry.IsSelf));
            return (false, references.Count, entries);
        }

        if ((owner.objectFlags & ObjectFlags.Anonymous) == 0)
            throw new InvalidOperationException("Unknown nonempty instantiated-object key family");
        var declarations = owner.symbol?.Declarations ?? default;
        if (declarations.Length == 0)
            throw new InvalidOperationException("Anonymous cache owner has no actual declaration");
        var parameterLocation = typeNodeLinks.TryGet(declarations[0]);
        var parameters = parameterLocation.IsEmpty ? default : parameterLocation.Span[0].outerTypeParameters;
        if (parameters.Length == 0)
            throw new InvalidOperationException("Anonymous cache has no stored outer type parameters");
        int count = cache.Count;
        var used = new HashSet<CacheHashKey>();
        void ValidateTypes(Slice<Type> types)
        {
            foreach (var type in types)
                if (type == null || type.id == 0 || type.checker != null && !ReferenceEquals(type.checker, this))
                    throw new InvalidOperationException("Anonymous key type is absent, unallocated or foreign");
        }

        ValidateTypes(parameters);
        foreach (var(key, value)in cache)
        {
            if (value == null || value.checker != null && !ReferenceEquals(value.checker, this))
                throw new InvalidOperationException("Absent or foreign anonymous cache value");
            bool isSelf = ReferenceEquals(owner, value);
            bool single;
            Slice<Type> arguments;
            TypeAlias alias;
            if (isSelf)
            {
                arguments = parameters;
                alias = owner.alias;
                single = false;
            }
            else
            {
                if (!ReferenceEquals(value.Target(), owner))
                    throw new InvalidOperationException("Anonymous cache value has another target");
                single = (owner.objectFlags & ObjectFlags.SingleSignatureType) != 0;
                var mapper = value.Mapper();
                if (single && mapper?.data is CompositeTypeMapper composite)
                    mapper = composite.m1;
                arguments = mapper?.data switch
                {
                    SimpleTypeMapper simple when parameters.Count == 1 && ReferenceEquals(simple.source, parameters[0]) => Slice<Type>.FromArray([simple.target]),
                    ArrayTypeMapper array when !array.sources.IsNil && GoSlices.Equal(array.sources, parameters) && array.targets.Length == parameters.Length => array.targets,
                    _ => throw new InvalidOperationException("Unknown stored anonymous key mapping")};
                alias = value.alias;
            }

            ValidateTypes(arguments);
            if (alias != null)
            {
                if (alias.symbol == null || alias.symbol.id == 0)
                    throw new InvalidOperationException("Anonymous cache alias lacks a stored symbol ID");
                ValidateTypes(alias.typeArguments);
            }

            var reconstructed = getTypeInstantiationKey(arguments, alias, single);
            if (reconstructed != key || !used.Add(key) || !cache.TryGetValue(reconstructed, out var found) || !ReferenceEquals(found, value))
                throw new InvalidOperationException("Anonymous cache key association mismatch");
            entries.Add((key, value, GoSlices.ToListOrNull(arguments), alias, single, "anonymous", owner, isSelf));
        }

        if (cache.Count != count || used.Count != count || entries.Count != count)
            throw new InvalidOperationException("Anonymous cache cardinality changed");
        return (false, count, entries);
    }
}
