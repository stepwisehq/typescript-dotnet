// Authored nonforcing observation of selected complete checker cache maps.
// Keys are validated against already allocated, known entities. No semantic
// getter, mapper, ID allocation or inverse guess from a reduced result is used.
#nullable disable
using System.Buffers.Binary;
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal sealed record CollectionsTypeCacheEntry(Type Value, Type Owner = null, int Kind = -1, Type Object = null, Type Index = null, uint AccessFlags = 0, Symbol AliasSymbol = null, List<Type> AliasArguments = null, Type Base = null, Type Constraint = null, List<string> Texts = null, List<Type> Types = null);
    internal sealed record CollectionsTypeCache(string Name, bool IsNull, int Count, List<CollectionsTypeCacheEntry> Entries);
    // Authored metadata for the sole context-free Widened publication. The
    // reduced value can erase its input owner, so retain that actual input.
    private sealed record CollectionsWidenedInput(Type Source, Type Value, bool Ambiguous);
    private readonly Dictionary<uint, CollectionsWidenedInput> collectionsWidenedInputs = new();
    private void verificationCollectionsCaptureWidenedOwner(Type source, Type value)
    {
        if (!authoredCollectionsProfile)
            return;
        var input = new CollectionsWidenedInput(source, value, false);
        if (collectionsWidenedInputs.TryGetValue(source.id, out var old))
            input = input with
            {
                Ambiguous = old.Ambiguous || !ReferenceEquals(old.Source, source) || !ReferenceEquals(old.Value, value)
            };
        collectionsWidenedInputs[source.id] = input;
    }

    private void verificationCollectionsValidateWidenedInput(uint id, Type value)
    {
        if (!collectionsWidenedInputs.TryGetValue(id, out var input) || input.Ambiguous || input.Source == null || input.Source.id == 0 || input.Source.id != id || input.Source.checker != null && !ReferenceEquals(input.Source.checker, this) || !ReferenceEquals(input.Value, value) || !cachedTypes.TryGetValue(new CachedTypeKey(CachedTypeKind.Widened, id), out var stored) || !ReferenceEquals(stored, value))
            throw new InvalidOperationException("Unknown or changed Widened cache publication");
    }

    internal (bool IsNull, int Count, List<(Type Owner, uint Use, Type Yield, Type Return, Type Next)> Entries) VerificationCollectionsIterationTypes(IReadOnlyDictionary<uint, Type> knownTypes)
    {
        int count = iterationTypesCache.Count;
        var keys = new HashSet<IterationTypesKey>();
        var entries = new List<(Type Owner, uint Use, Type Yield, Type Return, Type Next)>();
        void validateValue(Type value)
        {
            if (value != null && (value.id == 0 || value.checker != null && !ReferenceEquals(value.checker, this)))
                throw new InvalidOperationException("Unallocated or foreign iteration cache value");
        }

        foreach (var(key, value)in iterationTypesCache)
        {
            if (key.typeId == 0 || !knownTypes.TryGetValue(key.typeId, out var owner) || owner == null || owner.id != key.typeId || owner.checker != null && !ReferenceEquals(owner.checker, this) || (key.use & ~IterationUse.CacheFlags) != 0)
                throw new InvalidOperationException("Unrooted or invalid iteration cache key");
            validateValue(value.yieldType);
            validateValue(value.returnType);
            validateValue(value.nextType);
            var rebuilt = new IterationTypesKey(owner.id, key.use & IterationUse.CacheFlags);
            if (rebuilt != key || !keys.Add(rebuilt) || !iterationTypesCache.TryGetValue(rebuilt, out var found) || !ReferenceEquals(found.yieldType, value.yieldType) || !ReferenceEquals(found.returnType, value.returnType) || !ReferenceEquals(found.nextType, value.nextType))
                throw new InvalidOperationException("Iteration cache association mismatch");
            // A present negative result has three actual null fields.
            entries.Add((owner, (uint)key.use, value.yieldType, value.returnType, value.nextType));
        }

        if (iterationTypesCache.Count != count || keys.Count != count || entries.Count != count)
            throw new InvalidOperationException("Iteration cache cardinality changed");
        return (false, count, entries);
    }

    internal (bool IsNull, int Count, List<(Type Owner, uint Include, bool IncludeOrigin, Type Value)> Entries) VerificationCollectionsPropertiesTypes(IReadOnlyDictionary<uint, Type> knownTypes)
    {
        int count = propertiesTypes.Count;
        var keys = new HashSet<PropertiesTypesKey>();
        var entries = new List<(Type Owner, uint Include, bool IncludeOrigin, Type Value)>();
        foreach (var(key, value)in propertiesTypes)
        {
            if (key.typeId == 0 || !knownTypes.TryGetValue(key.typeId, out var owner) || owner == null || owner.id != key.typeId || owner.checker != null && !ReferenceEquals(owner.checker, this) || (key.include & ~(TypeFlags.StringLike | TypeFlags.NumberLike | TypeFlags.ESSymbolLike)) != 0)
                throw new InvalidOperationException("Unrooted or invalid properties type key");
            if (value != null && (value.id == 0 || value.checker != null && !ReferenceEquals(value.checker, this)))
                throw new InvalidOperationException("Unallocated or foreign properties type value");
            var rebuilt = new PropertiesTypesKey(owner.id, key.include, key.includeOrigin);
            if (rebuilt != key || !keys.Add(rebuilt) || !propertiesTypes.TryGetValue(rebuilt, out var found) || !ReferenceEquals(found, value))
                throw new InvalidOperationException("Properties type association mismatch");
            entries.Add((owner, (uint)key.include, key.includeOrigin, value));
        }

        if (propertiesTypes.Count != count || keys.Count != count || entries.Count != count)
            throw new InvalidOperationException("Properties type cardinality changed");
        return (false, count, entries);
    }

    // Census inputs are actual retained fields/values, not decoded or created
    // entities. The returned authored list has no semantic header identity.
    internal List<object> VerificationCollectionsGlobalStoredValues()
    {
        var values = new List<object>();
        foreach (var(id, input)in collectionsWidenedInputs)
        {
            verificationCollectionsValidateWidenedInput(id, input.Value);
            values.Add(input.Source);
        }

        foreach (var value in iterationTypesCache.Values)
        {
            values.Add(value.yieldType);
            values.Add(value.returnType);
            values.Add(value.nextType);
        }

        foreach (var value in propertiesTypes.Values)
            values.Add(value);
        foreach (var value in cachedTypes.Values)
            values.Add(value);
        foreach (var value in indexedAccessTypes.Values)
            values.Add(value);
        foreach (var value in substitutionTypes.Values)
            values.Add(value);
        foreach (var value in templateLiteralTypes.Values)
            values.Add(value);
        foreach (var value in tupleTypes.Values)
            values.Add(value);
        foreach (var value in reverseHomomorphicMappedCache.Values)
            values.Add(value);
        foreach (var value in reverseMappedCache.Values)
            values.Add(value);
        foreach (var(key, value)in cachedSignatures)
        {
            values.Add(key.sig);
            values.Add(value);
        }

        return values;
    }

    // Finite direct Checker field census for already-retained key owners.
    // These values are lookup inputs only, not eager semantic graph roots.
    // Never call lazy global resolvers or assign an ID for an absent slot.
    internal List<(string Name, object Value)> VerificationCollectionsStoredTypeSlots()
    {
        return new List<(string Name, object Value)>
        {
            ("anyType", anyType),
            ("autoType", autoType),
            ("wildcardType", wildcardType),
            ("errorType", errorType),
            ("unresolvedType", unresolvedType),
            ("nonInferrableAnyType", nonInferrableAnyType),
            ("intrinsicMarkerType", intrinsicMarkerType),
            ("unknownType", unknownType),
            ("undefinedType", undefinedType),
            ("undefinedWideningType", undefinedWideningType),
            ("missingType", missingType),
            ("undefinedOrMissingType", undefinedOrMissingType),
            ("optionalType", optionalType),
            ("nullType", nullType),
            ("nullWideningType", nullWideningType),
            ("stringType", stringType),
            ("numberType", numberType),
            ("bigintType", bigintType),
            ("regularFalseType", regularFalseType),
            ("falseType", falseType),
            ("regularTrueType", regularTrueType),
            ("trueType", trueType),
            ("booleanType", booleanType),
            ("esSymbolType", esSymbolType),
            ("voidType", voidType),
            ("neverType", neverType),
            ("silentNeverType", silentNeverType),
            ("implicitNeverType", implicitNeverType),
            ("unreachableNeverType", unreachableNeverType),
            ("nonPrimitiveType", nonPrimitiveType),
            ("emptyObjectType", emptyObjectType),
            ("emptyGenericType", emptyGenericType),
            ("emptyTypeLiteralType", emptyTypeLiteralType),
            ("globalObjectType", globalObjectType),
            ("globalStringType", globalStringType),
            ("globalNumberType", globalNumberType),
            ("globalBooleanType", globalBooleanType),
            ("anyFunctionType", anyFunctionType),
            ("anySignature", anySignature),
            ("unknownSignature", unknownSignature),
            ("resolvingSignature", resolvingSignature),
            ("silentNeverSignature", silentNeverSignature),
            ("blockedStringType", blockedStringType),
            ("numericStringType", numericStringType),
            ("nanType", nanType),
            ("globalBigIntType", globalBigIntType),
            ("globalESSymbolType", globalESSymbolType),
            ("globalFunctionType", globalFunctionType),
            ("globalPromiseType", globalPromiseType),
            ("uniqueLiteralType", uniqueLiteralType),
            ("stringNumberSymbolType", stringNumberSymbolType),
            ("noConstraintType", noConstraintType),
            ("circularConstraintType", circularConstraintType),
            ("resolvingDefaultType", resolvingDefaultType),
            ("globalArrayType", globalArrayType),
            ("globalReadonlyArrayType", globalReadonlyArrayType),
            ("anyArrayType", anyArrayType),
            ("autoArrayType", autoArrayType),
            ("anyReadonlyArrayType", anyReadonlyArrayType),
            ("globalIterableType", globalIterableType),
            ("globalIterableIteratorType", globalIterableIteratorType),
            ("globalAsyncIterableType", globalAsyncIterableType),
            ("globalAsyncIterableIteratorType", globalAsyncIterableIteratorType),
            ("unknownEmptyObjectType", unknownEmptyObjectType),
            ("unknownUnionType", unknownUnionType),
            ("stringOrNumberType", stringOrNumberType),
            ("markerSuperType", markerSuperType),
            ("markerSubType", markerSubType),
            ("markerOtherType", markerOtherType),
            ("markerSuperTypeForCheck", markerSuperTypeForCheck),
            ("markerSubTypeForCheck", markerSubTypeForCheck),
            ("globalThisType", globalThisType),
            ("numberOrBigIntType", numberOrBigIntType),
            ("typeofType", typeofType),
            ("emptyStringType", emptyStringType),
            ("zeroType", zeroType),
            ("zeroBigIntType", zeroBigIntType),
            ("globalCallableFunctionType", globalCallableFunctionType),
            ("globalNewableFunctionType", globalNewableFunctionType),
        };
    }

    internal List<CollectionsTypeCache> VerificationCollectionsTypeCaches(IReadOnlyDictionary<uint, Type> knownTypes, IReadOnlyDictionary<ulong, Symbol> knownSymbols)
    {
        Type resolve(uint id)
        {
            if (id == 0 || !knownTypes.TryGetValue(id, out var type) || type == null || type.id != id || type.checker != null && !ReferenceEquals(type.checker, this))
                throw new InvalidOperationException("Unrooted, unallocated or foreign global type-cache key term");
            return type;
        }

        void validateValue(Type value)
        {
            if (value != null && (value.id == 0 || value.checker != null && !ReferenceEquals(value.checker, this)))
                throw new InvalidOperationException("Unallocated or foreign global type-cache value");
        }

        void complete(int current, int count, int keyCount, int entryCount)
        {
            if (current != count || keyCount != count || entryCount != count)
                throw new InvalidOperationException("Global type-cache cardinality changed");
        }

        var output = new List<CollectionsTypeCache>();
        {
            int count = cachedTypes.Count;
            var keys = new HashSet<CachedTypeKey>();
            var entries = new List<CollectionsTypeCacheEntry>();
            foreach (var(key, value)in cachedTypes)
            {
                // Decorator contexts require excluded source decorators; no
                // other raw enum value is silently treated as a known family.
                if (key.kind < CachedTypeKind.LiteralUnionBaseType || key.kind > CachedTypeKind.SyntheticType)
                    throw new InvalidOperationException("Unselected global cached-type key kind");
                var owner = resolve(key.typeId);
                if (key.kind == CachedTypeKind.Widened)
                {
                    verificationCollectionsValidateWidenedInput(key.typeId, value);
                    if (!ReferenceEquals(collectionsWidenedInputs[key.typeId].Source, owner))
                        throw new InvalidOperationException("Widened cache owner identity mismatch");
                }

                validateValue(value);
                var rebuilt = new CachedTypeKey(key.kind, owner.id);
                if (rebuilt != key || !keys.Add(rebuilt) || !cachedTypes.TryGetValue(rebuilt, out var found) || !ReferenceEquals(found, value))
                    throw new InvalidOperationException("Global cached-type association mismatch");
                entries.Add(new(value, Owner: owner, Kind: (int)key.kind));
            }

            complete(cachedTypes.Count, count, keys.Count, entries.Count);
            output.Add(new("cached-types", false, count, entries));
        }

        {
            int count = indexedAccessTypes.Count;
            var keys = new HashSet<CacheHashKey>();
            var entries = new List<CollectionsTypeCacheEntry>();
            foreach (var(key, value)in indexedAccessTypes)
            {
                if (key.preimage == null)
                    throw new InvalidOperationException("Missing indexed-access key preimage");
                byte[] bytes = Convert.FromBase64String(key.preimage);
                if (Convert.ToBase64String(bytes) != key.preimage || bytes.Length < 13)
                    throw new InvalidOperationException("Noncanonical or truncated indexed-access key");
                int offset = 0;
                uint read32()
                {
                    if (offset > bytes.Length - sizeof(uint))
                        throw new InvalidOperationException("Truncated indexed-access uint32 term");
                    uint result = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)));
                    offset += sizeof(uint);
                    return result;
                }

                ulong read64()
                {
                    if (offset > bytes.Length - sizeof(ulong))
                        throw new InvalidOperationException("Truncated indexed-access uint64 term");
                    ulong result = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset, sizeof(ulong)));
                    offset += sizeof(ulong);
                    return result;
                }

                var objectType = resolve(read32());
                var indexType = resolve(read32());
                uint flags = read32();
                const uint flagMask = (uint)(TypeScript.Checking.AccessFlags.IncludeUndefined | TypeScript.Checking.AccessFlags.NoIndexSignatures | TypeScript.Checking.AccessFlags.Writing | TypeScript.Checking.AccessFlags.CacheSymbol | TypeScript.Checking.AccessFlags.AllowMissing | TypeScript.Checking.AccessFlags.ExpressionPosition | TypeScript.Checking.AccessFlags.ReportDeprecated | TypeScript.Checking.AccessFlags.SuppressNoImplicitAnyError | TypeScript.Checking.AccessFlags.Contextual);
                if ((flags & ~flagMask) != 0)
                    throw new InvalidOperationException("Unknown indexed-access key flags");
                byte aliasTag = bytes[offset++];
                Symbol aliasSymbol = null;
                List<Type> aliasArguments = null;
                if (aliasTag == 1)
                {
                    ulong symbolId = read64();
                    if (symbolId == 0 || !knownSymbols.TryGetValue(symbolId, out aliasSymbol) || aliasSymbol == null || unchecked((ulong)aliasSymbol.id) != symbolId)
                        throw new InvalidOperationException("Unrooted or unallocated indexed-access alias symbol");
                    ulong argumentCount = read64();
                    if (argumentCount > (ulong)((bytes.Length - offset) / sizeof(uint)))
                        throw new InvalidOperationException("Truncated indexed-access alias arguments");
                    aliasArguments = new List<Type>((int)argumentCount);
                    for (int i = 0; i < (int)argumentCount; i++)
                        aliasArguments.Add(resolve(read32()));
                }
                else if (aliasTag != 0)
                    throw new InvalidOperationException("Unknown indexed-access alias key tag");
                if (offset != bytes.Length)
                    throw new InvalidOperationException("Trailing indexed-access key terms");
                var builder = new keyBuilder();
                builder.writeType(objectType);
                builder.writeType(indexType);
                builder.writeUint32(flags);
                builder.writeByte(aliasTag);
                if (aliasSymbol != null)
                {
                    builder.writeUint64(unchecked((ulong)aliasSymbol.id));
                    builder.writeTypes(aliasArguments);
                }

                var rebuilt = builder.hash();
                validateValue(value);
                if (value == null || (value.flags & TypeFlags.IndexedAccess) == 0)
                    throw new InvalidOperationException("Indexed-access cache has another payload");
                var data = value.AsIndexedAccessType();
                if (!ReferenceEquals(data.objectType, objectType) || !ReferenceEquals(data.indexType, indexType) || (uint)data.accessFlags != (flags & (uint)TypeScript.Checking.AccessFlags.Persistent))
                    throw new InvalidOperationException("Indexed-access value contradicts its key inputs");
                var storedAlias = value.alias;
                if (!ReferenceEquals(storedAlias?.symbol, aliasSymbol) || (storedAlias?.typeArguments?.Count ?? 0) != (aliasArguments?.Count ?? 0))
                    throw new InvalidOperationException("Indexed-access value alias contradicts its key");
                for (int i = 0; i < (aliasArguments?.Count ?? 0); i++)
                    if (!ReferenceEquals(storedAlias.typeArguments[i], aliasArguments[i]))
                        throw new InvalidOperationException("Indexed-access value alias argument contradicts its key");
                if (rebuilt != key || !keys.Add(rebuilt) || !indexedAccessTypes.TryGetValue(rebuilt, out var found) || !ReferenceEquals(found, value))
                    throw new InvalidOperationException("Indexed-access key association mismatch");
                entries.Add(new(value, Object: objectType, Index: indexType, AccessFlags: flags, AliasSymbol: aliasSymbol, AliasArguments: aliasArguments));
            }

            complete(indexedAccessTypes.Count, count, keys.Count, entries.Count);
            output.Add(new("indexed-access", false, count, entries));
        }

        {
            int count = substitutionTypes.Count;
            var keys = new HashSet<SubstitutionTypeKey>();
            var entries = new List<CollectionsTypeCacheEntry>();
            foreach (var(key, value)in substitutionTypes)
            {
                var baseType = resolve(key.baseId);
                var constraint = resolve(key.constraintId);
                validateValue(value);
                if (value == null || (value.flags & TypeFlags.Substitution) == 0 || !ReferenceEquals(value.AsSubstitutionType().baseType, baseType) || !ReferenceEquals(value.AsSubstitutionType().constraint, constraint))
                    throw new InvalidOperationException("Substitution cache value contradicts its key");
                var rebuilt = new SubstitutionTypeKey(baseType.id, constraint.id);
                if (rebuilt != key || !keys.Add(rebuilt) || !substitutionTypes.TryGetValue(rebuilt, out var found) || !ReferenceEquals(found, value))
                    throw new InvalidOperationException("Substitution key association mismatch");
                entries.Add(new(value, Base: baseType, Constraint: constraint));
            }

            complete(substitutionTypes.Count, count, keys.Count, entries.Count);
            output.Add(new("substitution", false, count, entries));
        }

        {
            int count = templateLiteralTypes.Count;
            var keys = new HashSet<CacheHashKey>();
            var entries = new List<CollectionsTypeCacheEntry>();
            foreach (var(key, value)in templateLiteralTypes)
            {
                validateValue(value);
                if (value == null || (value.flags & TypeFlags.TemplateLiteral) == 0)
                    throw new InvalidOperationException("Template cache has another payload");
                var data = value.AsTemplateLiteralType();
                if (data.texts == null || data.types == null || data.types.Count == 0 || data.texts.Count != data.types.Count + 1 || data.texts.Any(text => text == null))
                    throw new InvalidOperationException("Invalid retained template span shape");
                foreach (var type in data.types)
                    if (type == null || !ReferenceEquals(resolve(type.id), type))
                        throw new InvalidOperationException("Unrooted retained template placeholder");
                var rebuilt = getTemplateTypeKey(data.texts, data.types);
                if (rebuilt != key || !keys.Add(rebuilt) || !templateLiteralTypes.TryGetValue(rebuilt, out var found) || !ReferenceEquals(found, value))
                    throw new InvalidOperationException("Template key association mismatch");
                entries.Add(new(value, Texts: data.texts, Types: data.types));
            }

            complete(templateLiteralTypes.Count, count, keys.Count, entries.Count);
            output.Add(new("template-literal", false, count, entries));
        }

        return output;
    }
}
