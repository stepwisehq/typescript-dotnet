// Authored static Native AOT mirror; original Go independently discovers fields.
using TypeScript.Scanner;
using Encoder = TypeScript.Scanner.ParserValidation.Encoder;
using CheckType = TypeScript.Checking.Type;

namespace TypeScript.Checking.Validation;
public sealed class DataGraphValue
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<DataInstantiationAssociation>? InstantiationAssociations { get; set; }
    public bool PropertiesCacheNull { get; set; }
    public List<DeclaredObservation> Declared { get; set; } = [];
    public List<DataTypeObservation> Types { get; set; } = [];
    public List<DataRecord> Aliases { get; set; } = [];
    public List<DataRecord> Mappers { get; set; } = [];
    public List<DataRecord> Indexes { get; set; } = [];
    public List<DataLinkObservation> Links { get; set; } = [];
    public List<WidenObservation> Widenings { get; set; } = [];
    public List<ObjectCacheObservation> Caches { get; set; } = [];
    public List<DataResolvedSymbol> ResolvedSymbols { get; set; } = [];
    public List<DataKeyObservation> Keys { get; set; } = [];
}

public sealed record DataInstantiationAssociation(int Owner, int Value, string Family, List<int> Arguments, int? Alias, bool SingleSignature, bool Membership, bool RepeatedIdentity);
public sealed record DataResolvedSymbol(int Node, int? Symbol);
public sealed record DataKeyObservation(int Owner, int? Other, string Name, bool BoolValue);
public sealed record DeclaredObservation(int Node, int? BoundSymbol, int Symbol, int Type, bool RepeatedIdentity);
public sealed record DataRecord(int Id, string DataType, List<DataField> Fields);
public sealed class DataTypeObservation
{
    public List<DataPropertiesEntry> PropertiesCache { get; set; } = [];
    public int Type { get; set; }
    public int? Alias { get; set; }
    public List<DataField> Fields { get; set; } = [];
    public bool PropertiesNull { get; set; }
    public List<ObjectPropertyObservation> Properties { get; set; } = [];
}

public sealed record DataPropertiesEntry(uint Include, bool IncludeOrigin, int? Value);
public sealed record DataLinkObservation(string OwnerKind, int Owner, string Name, bool Present, List<DataField> Fields);
public sealed record DataEntry(int Key, int Value);
public sealed record DataNamedEntry(string KeyBase64, int? Node);
public sealed class DataField
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool Null { get; set; }
    public int? Ref { get; set; }
    public List<int> Refs { get; set; } = [];
    public long Number { get; set; }
    public bool Bool { get; set; }
    public string Bytes { get; set; } = "";
    public List<DataEntry> Entries { get; set; } = [];
    public List<DataNamedEntry> NamedEntries { get; set; } = [];
}

public sealed partial class Graph
{
    private DataGraphValue data = null!;
    private readonly Dictionary<TypeAlias, int> dataAliases = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<TypeMapper, int> dataMappers = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IndexInfo, int> dataIndexes = new(ReferenceEqualityComparer.Instance);
    private int? Alias(TypeAlias? alias)
    {
        if (alias is null)
            return null;
        if (dataAliases.TryGetValue(alias, out int id))
            return id;
        id = data.Aliases.Count;
        dataAliases.Add(alias, id);
        var record = new DataRecord(id, "TypeAlias", []);
        data.Aliases.Add(record);
        record.Fields.Add(F("symbol", "symbol", alias.symbol));
        record.Fields.Add(F("typeArguments", "types", alias.typeArguments));
        return id;
    }

    private int? Mapper(TypeMapper? mapper)
    {
        if (mapper is null)
            return null;
        if (dataMappers.TryGetValue(mapper, out int id))
            return id;
        id = data.Mappers.Count;
        dataMappers.Add(mapper, id);
        string name = mapper.data switch
        {
            SimpleTypeMapper => "SimpleTypeMapper",
            ArrayTypeMapper => "ArrayTypeMapper",
            ArrayToSingleTypeMapper => "ArrayToSingleTypeMapper",
            MergedTypeMapper => "MergedTypeMapper",
            CompositeTypeMapper => "CompositeTypeMapper",
            TypeMapperBase => "TypeMapperBase",
            _ => throw new InvalidDataException("Unobserved mapper payload")};
        var record = new DataRecord(id, name, []);
        data.Mappers.Add(record);
        switch (mapper.data)
        {
            case SimpleTypeMapper m:
                record.Fields.Add(F("source", "type", m.source));
                record.Fields.Add(F("target", "type", m.target));
                break;
            case ArrayTypeMapper m:
                record.Fields.Add(F("sources", "types", m.sources));
                record.Fields.Add(F("targets", "types", m.targets));
                break;
            case ArrayToSingleTypeMapper m:
                record.Fields.Add(F("sources", "types", m.sources));
                record.Fields.Add(F("target", "type", m.target));
                break;
            case MergedTypeMapper m:
                record.Fields.Add(F("m1", "mapper", m.m1));
                record.Fields.Add(F("m2", "mapper", m.m2));
                break;
            case CompositeTypeMapper m:
                if (!ReferenceEquals(m.c, checker))
                    throw new InvalidDataException("Foreign mapper checker");
                record.Fields.Add(F("m1", "mapper", m.m1));
                record.Fields.Add(F("m2", "mapper", m.m2));
                break;
            case DeferredTypeMapper or FunctionTypeMapper:
                throw new InvalidDataException("Inference mapper outside data contract");
        }

        return id;
    }

    private int? Index(IndexInfo? index)
    {
        if (index is null)
            return null;
        if (dataIndexes.TryGetValue(index, out int id))
            return id;
        id = data.Indexes.Count;
        dataIndexes.Add(index, id);
        var record = new DataRecord(id, "IndexInfo", []);
        data.Indexes.Add(record);
        record.Fields.AddRange([F("keyType", "type", index.keyType), F("valueType", "type", index.valueType), F("isReadonly", "bool", index.isReadonly), F("declaration", "node", index.declaration), F("indexSymbol", "symbol", index.indexSymbol), F("components", "nodes", index.components)]);
        return id;
    }

    private string SeedKey(CheckType type)
    {
        var parts = new List<string>
        {
            ((uint)type.Flags()).ToString(),
            ((uint)type.ObjectFlags()).ToString(),
            checker.TypeToString(type)
        };
        if (type.Symbol()is { } symbol)
        {
            parts.Add(symbol.Name);
            foreach (var node in symbol.Declarations)
                parts.Add($"{(int)node.Kind}:{node.Pos()}:{node.End()}");
        }

        if (type.AsTypeReference()is { } reference)
            foreach (var arg in reference.resolvedTypeArguments)
            {
                parts.Add($"arg:{(uint)arg.Flags()}:{(uint)arg.ObjectFlags()}:{checker.TypeToString(arg)}");
                if (arg.Symbol()is { } s)
                    parts.Add(s.Name);
            }

        return string.Join('\0', parts);
    }

    private DataField F(string name, string kind, object? value)
    {
        var field = new DataField
        {
            Name = name,
            Kind = kind,
            Null = value is null
        };
        switch (kind)
        {
            case "type":
                field.Ref = Type((CheckType? )value);
                break;
            case "symbol":
                field.Ref = Symbol((Symbol? )value);
                break;
            case "node":
                field.Ref = Node((Node? )value);
                break;
            case "table":
                field.Ref = Table((SymbolTable? )value);
                break;
            case "mapper":
                field.Ref = Mapper((TypeMapper? )value);
                break;
            case "types":
                if (value is Slice<CheckType> typeHeader)
                {
                    field.Null = typeHeader.IsNil;
                    foreach (var item in typeHeader) field.Refs.Add(Type(item)!.Value);
                }
                else
                    foreach (var item in (List<CheckType>? )value ?? [])
                        field.Refs.Add(Type(item)!.Value);
                break;
            case "symbols":
                if (value is Slice<Symbol> symbolHeader)
                {
                    field.Null = symbolHeader.IsNil;
                    foreach (var item in symbolHeader) field.Refs.Add(Symbol(item)!.Value);
                }
                else
                foreach (var item in (List<Symbol>? )value ?? [])
                    field.Refs.Add(Symbol(item)!.Value);
                break;
            case "nodes":
                if (value is Slice<Node> nodes)
                {
                    field.Null = nodes.IsNil;
                    foreach (var item in nodes) field.Refs.Add(Node(item)!.Value);
                }
                else
                    foreach (var item in (List<Node>? )value ?? [])
                        field.Refs.Add(Node(item)!.Value);
                break;
            case "indexes":
                if (value is Slice<IndexInfo> indexHeader)
                {
                    field.Null = indexHeader.IsNil;
                    foreach (var item in indexHeader) field.Refs.Add(Index(item)!.Value);
                }
                else
                foreach (var item in (List<IndexInfo>? )value ?? [])
                    field.Refs.Add(Index(item)!.Value);
                break;
            case "signature":
                field.Ref = Signature((Signature? )value);
                break;
            case "predicate":
                field.Ref = Predicate((TypePredicate? )value);
                break;
            case "composite":
                field.Ref = Composite((CompositeSignature? )value);
                break;
            case "signatures":
                if (callables is null && ((List<Signature>? )value)?.Count > 0)
                    throw new InvalidDataException("Source signature dependency outside data contract");
                foreach (var sig in (List<Signature>? )value ?? [])
                    field.Refs.Add(Signature(sig)!.Value);
                break;
            case "instantiations":
                var values = ((Dictionary<Checker.CacheHashKey, CheckType>? )value)?.Values.ToList() ?? [];
                var seeds = values.Where(t => !types.ContainsKey(t)).Select(t => (Type: t, Key: SeedKey(t))).DistinctBy(p => p.Type).OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
                if (seeds.GroupBy(p => p.Key, StringComparer.Ordinal).Any(g => g.Count() > 1))
                    throw new InvalidDataException("Ambiguous instantiation seed descriptor");
                foreach (var seed in seeds)
                    Type(seed.Type);
                field.Refs = values.Select(t => Type(t)!.Value).Order().ToList();
                break;
            case "node-map":
                if (value is Dictionary<ByteStringKey, Node> nodeMap)
                {
                    foreach (var pair in nodeMap.OrderBy(p => p.Key))
                        field.NamedEntries.Add(new(Encoder.Bytes(pair.Key.ToString()), Node(pair.Value)));
                }
                else
                foreach (var pair in ((Dictionary<string, Node>? )value ?? []).OrderBy(p => p.Key, StringComparer.Ordinal))
                    field.NamedEntries.Add(new(Encoder.Bytes(pair.Key), Node(pair.Value)));
                break;
            case "type-map":
                var map = (Dictionary<CheckType, CheckType>? )value;
                if (map is not null)
                {
                    if (map.Keys.Any(t => !types.ContainsKey(t)))
                        throw new InvalidDataException("Unrooted discriminant key requires canonical seed");
                    foreach (var key in map.Keys.OrderBy(t => types[t]))
                        field.Entries.Add(new(Type(key)!.Value, Type(map[key])!.Value));
                }

                break;
            case "bytes":
                if (value is ByteStringValue byteValue)
                    field.Bytes = Encoder.Bytes(byteValue.Materialize());
                else
                    field.Bytes = Encoder.Bytes((string)value!);
                field.Null = false;
                break;
            case "bool":
                field.Bool = (bool)value!;
                field.Null = false;
                break;
            case "number":
                field.Number = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
                field.Null = false;
                break;
            default:
                throw new InvalidDataException("Unclassified data field " + name + ":" + kind);
        }

        return field;
    }

    private List<DataField> Fields(TypeData payload)
    {
        List<DataField> fields = [];
        if (payload is UniqueESSymbolType unique)
        {
            var symbol = unique.AsType().Symbol() ?? throw new InvalidDataException("Unique symbol has no owner");
            string expected = "\xfe@" + symbol.Name + "@" + Ast.GetSymbolId(symbol).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (unique.name != expected)
                throw new InvalidDataException("Unique symbol raw name does not match actual owner");
            fields.Add(new() { Name = "name", Kind = "unique-symbol-name", Ref = Symbol(symbol), Bytes = Encoder.Bytes(symbol.Name) });
        }

        if (payload is ConstrainedType c)
            fields.Add(F("resolvedBaseConstraint", "type", c.resolvedBaseConstraint));
        if (payload is StructuredType s)
            fields.AddRange([F("members", "table", s.members), F("properties", "symbols", s.properties), F("signatures", "signatures", GoSlices.ToListOrNull(s.signatures)), F("callSignatureCount", "number", s.callSignatureCount), F("indexInfos", "indexes", s.indexInfos), F("objectTypeWithoutAbstractConstructSignatures", "type", s.objectTypeWithoutAbstractConstructSignatures)]);
        if (payload is ObjectType o)
            fields.AddRange([F("target", "type", o.target), F("mapper", "mapper", o.mapper), F("instantiations", "instantiations", o.instantiations)]);
        if (payload is TypeReference r)
            fields.AddRange([F("node", "node", r.node), F("resolvedTypeArguments", "types", r.resolvedTypeArguments)]);
        if (payload is InterfaceType i)
            fields.AddRange([F("allTypeParameters", "types", i.allTypeParameters), F("outerTypeParameterCount", "number", i.outerTypeParameterCount), F("thisType", "type", i.thisType), F("baseTypesResolved", "bool", i.baseTypesResolved), F("declaredMembersResolved", "bool", i.declaredMembersResolved), F("resolvedBaseConstructorType", "type", i.resolvedBaseConstructorType), F("resolvedBaseTypes", "types", i.resolvedBaseTypes), F("declaredMembers", "table", i.declaredMembers), F("declaredCallSignatures", "signatures", GoSlices.ToListOrNull(i.declaredCallSignatures)), F("declaredConstructSignatures", "signatures", GoSlices.ToListOrNull(i.declaredConstructSignatures)), F("declaredIndexInfos", "indexes", i.declaredIndexInfos)]);
        if (payload is UnionOrIntersectionType u)
            fields.AddRange([F("types", "types", u.types), F("propertyCache", "table", u.propertyCache), F("propertyCacheWithoutFunctionPropertyAugment", "table", u.propertyCacheWithoutFunctionPropertyAugment), F("resolvedProperties", "symbols", u.resolvedProperties)]);
        if (payload is UnionType union)
            fields.AddRange([F("resolvedReducedType", "type", union.resolvedReducedType), F("regularType", "type", union.regularType), F("origin", "type", union.origin), F("keyPropertyName", "bytes", union.keyPropertyName), F("constituentMap", "type-map", union.constituentMap)]);
        if (payload is IntersectionType intersection)
            fields.AddRange([F("resolvedApparentType", "type", intersection.resolvedApparentType), F("uniqueLiteralFilledInstantiation", "type", intersection.uniqueLiteralFilledInstantiation)]);
        if (payload is TypeParameter p)
            fields.AddRange([F("constraint", "type", p.constraint), F("target", "type", p.target), F("mapper", "mapper", p.mapper), F("isThisType", "bool", p.isThisType), F("isDistributed", "bool", p.isDistributed), F("resolvedDefaultType", "type", p.resolvedDefaultType), F("distributedType", "type", p.distributedType)]);
        if (payload is IndexType index)
            fields.AddRange([F("target", "type", index.target), F("indexFlags", "number", index.indexFlags)]);
        if (payload is not (IntrinsicType or LiteralType or UniqueESSymbolType or ObjectType or UnionOrIntersectionType or TypeParameter or IndexType))
            throw new InvalidDataException("Unobserved data type payload");
        return fields;
    }

    private List<(string Name, object? Data)> SymbolLinks(Symbol symbol)
    {
        var location = checker.valueSymbolLinks.TryGet(symbol);
        object? valueLinks = Checker.VerificationLinkLocation(location);
        return [("valueSymbolLinks", valueLinks), ("typeAliasLinks", Checker.VerificationLinkLocation(checker.typeAliasLinks.TryGet(symbol))), ("declaredTypeLinks", Checker.VerificationLinkLocation(checker.declaredTypeLinks.TryGet(symbol))), ("deferredSymbolLinks", Checker.VerificationLinkLocation(checker.deferredSymbolLinks.TryGet(symbol))), ("mappedSymbolLinks", Checker.VerificationLinkLocation(checker.mappedSymbolLinks.TryGet(symbol))), ("symbolReferenceLinks", Checker.VerificationLinkLocation(checker.symbolReferenceLinks.TryGet(symbol))), ("moduleSymbolLinks", checker.VerificationDataModuleSymbolLinks(symbol))];
    }

    private List<DataField> LinkFields(object payload)
    {
        if (payload is Memory<ValueSymbolLinks> location)
        {
            ref var l = ref location.Span[0];
            return [F("resolvedType", "type", l.resolvedType), F("writeType", "type", l.writeType), F("target", "symbol", l.target), F("mapper", "mapper", l.mapper), F("nameType", "type", l.nameType), F("containingType", "type", l.containingType), F("functionOrConstructorChecked", "bool", l.functionOrConstructorChecked)];
        }

        if (payload is Memory<TypeAliasLinks> typeAliasLinksLocation)
        {
            ref var l = ref typeAliasLinksLocation.Span[0];
            return [F("declaredType", "type", l.declaredType), F("typeParameters", "types", l.typeParameters), F("instantiations", "instantiations", l.instantiations), F("isConstructorDeclaredProperty", "bool", l.isConstructorDeclaredProperty)];
        }
        if (payload is Memory<DeclaredTypeLinks> declaredTypeLinksLocation)
        {
            ref var l = ref declaredTypeLinksLocation.Span[0];
            return [F("declaredType", "type", l.declaredType), F("interfaceChecked", "bool", l.interfaceChecked), F("indexSignaturesChecked", "bool", l.indexSignaturesChecked), F("typeParametersChecked", "bool", l.typeParametersChecked), F("enumChecked", "bool", l.enumChecked)];
        }
        if (payload is Memory<DeferredSymbolLinks> deferredSymbolLinksLocation)
        {
            ref var l = ref deferredSymbolLinksLocation.Span[0];
            return [F("parent", "type", l.parent), F("constituents", "types", l.constituents), F("writeConstituents", "types", l.writeConstituents)];
        }
        if (payload is Memory<SymbolReferenceLinks> symbolReferenceLinksLocation)
        {
            ref var l = ref symbolReferenceLinksLocation.Span[0];
            return [F("referenceKinds", "number", l.referenceKinds)];
        }
        if (payload is Memory<MappedSymbolLinks> mappedSymbolLinksLocation)
        {
            ref var l = ref mappedSymbolLinksLocation.Span[0];
            return [F("keyType", "type", l.keyType), F("syntheticOrigin", "symbol", l.syntheticOrigin)];
        }
        if (payload is Memory<ModuleSymbolLinks> moduleSymbolLinksLocation)
        {
            ref var l = ref moduleSymbolLinksLocation.Span[0];
            return [F("resolvedExports", "table", l.resolvedExports), F("typeOnlyExportStarMap", "node-map", l.typeOnlyExportStarMap), F("exportsChecked", "bool", l.exportsChecked)];
        }
        if (payload is Memory<NodeLinks> nodeLinksLocation)
        {
            ref var l = ref nodeLinksLocation.Span[0];
            return [F("flags", "number", l.flags), F("declarationRequiresScopeChange", "number", l.declarationRequiresScopeChange), F("hasReportedStatementInAmbientContext", "bool", l.hasReportedStatementInAmbientContext)];
        }
        if (payload is Memory<TypeNodeLinks> typeNodeLinksLocation)
        {
            ref var l = ref typeNodeLinksLocation.Span[0];
            return [F("resolvedType", "type", l.resolvedType), F("outerTypeParameters", "types", l.outerTypeParameters)];
        }
        throw new InvalidDataException("Unobserved private symbol link");
    }
    public DataGraphValue Data(Request request, Dictionary<int, CheckType> queried)
    {
        data = new();
        if (request.CallableObservations)
            StartCallables();
        data.PropertiesCacheNull = checker.VerificationDataPropertiesTypesNil();
        foreach (int id in request.DeclaredNodeIds)
        {
            var node = syntax.RawNodes[id];
            var symbol = checker.GetSymbolAtLocation(node.Name()) ?? throw new InvalidDataException("Missing named declaration symbol");
            var type = checker.getDeclaredTypeOfSymbol(symbol);
            data.Declared.Add(new(id, Symbol(node.Symbol()), Symbol(symbol)!.Value, Type(type)!.Value, ReferenceEquals(type, checker.getDeclaredTypeOfSymbol(symbol))));
        }

        foreach (int id in request.WidenNodeIds)
        {
            var source = queried[id];
            var wide = checker.GetWidenedType(source);
            data.Widenings.Add(new(id, Type(source)!.Value, Type(wide)!.Value, ReferenceEquals(wide, checker.GetWidenedType(source))));
        }

        for (int id = 0; id < syntax.RawNodes.Count; id++)
        {
            var node = syntax.RawNodes[id];
            foreach (var link in new List<(string Name, object? Data)>
            {
                ("nodeLinks", Checker.VerificationLinkLocation(checker.nodeLinks.TryGet(node))),
                ("typeNodeLinks", Checker.VerificationLinkLocation(checker.typeNodeLinks.TryGet(node)))
            }

            )
                data.Links.Add(new("node", id, link.Name, link.Data is not null, link.Data is null ? [] : LinkFields(link.Data)));
            var symbolLocation = checker.symbolNodeLinks.TryGet(node);
            data.ResolvedSymbols.Add(new(id, Symbol(symbolLocation.IsEmpty ? null : symbolLocation.Span[0].resolvedSymbol)));
        }

        int cursor = 0, linkCursor = 0;
        while (cursor < rawTypes.Count || linkCursor < result.Symbols.Count || (callables is not null && callableCacheCursor < callables.Signatures.Count))
        {
            while (cursor < rawTypes.Count)
            {
                int id = cursor++;
                var type = rawTypes[id];
                var record = new DataTypeObservation
                {
                    Type = id,
                    Alias = Alias(type.Alias())
                };
                data.Types.Add(record);
                if ((type.Flags() & TypeFlags.StructuredType) != 0)
                {
                    var props = checker.GetPropertiesOfType(type);
                    record.PropertiesNull = props.IsNil;
                    foreach (var symbol in props)
                        record.Properties.Add(new(Symbol(symbol)!.Value, Type(checker.GetTypeOfSymbol(symbol))!.Value, (uint)Checker.GetDeclarationModifierFlagsFromSymbol(symbol), GoSlices.Values(symbol.Declarations).Select(n => Symbol(n.Symbol())).ToList()));
                }

                record.Fields = Fields(type.data);
                foreach (var entry in checker.VerificationDataPropertiesTypes(type))
                    record.PropertiesCache.Add(new(entry.Include, entry.IncludeOrigin, Type(entry.Value)));
                if ((type.Flags() & TypeFlags.Object) != 0)
                    data.Caches.Add(new(id, Type(checker.VerificationCachedObjectType(type, "Widened")), Type(checker.VerificationCachedObjectType(type, "RegularObjectLiteral"))));
            }

            while (linkCursor < result.Symbols.Count)
            {
                int id = linkCursor++;
                var symbol = symbols.Single(p => p.Value == id).Key;
                foreach (var link in SymbolLinks(symbol))
                    data.Links.Add(new("symbol", id, link.Name, link.Data is not null, link.Data is null ? [] : LinkFields(link.Data)));
                var merged = checker.VerificationDataMergedSymbol(symbol);
                data.Links.Add(new("symbol", id, "mergedSymbols", merged.Present, merged.Present ? [F("target", "symbol", merged.Symbol)] : []));
            }

            FinishCallableCaches();
        }

        if (callables is not null && callables.CachedSignatures.Count != checker.VerificationCallableCachedSignatureCount())
            throw new InvalidDataException("Callable cached signature has an unobserved owner");
        foreach (var record in data.Types)
        {
            var type = rawTypes[record.Type];
            result.Types[record.Type].Flags = (uint)type.Flags();
            result.Types[record.Type].ObjectFlags = (uint)type.ObjectFlags();
        }

        if (rawTypes.Count(t => (t.Flags() & TypeFlags.UniqueESSymbol) != 0) != checker.VerificationCallableUniqueSymbolCount())
            throw new InvalidDataException("Unique symbol cache contains unobserved values");
        foreach (var record in data.Types)
        {
            var type = rawTypes[record.Type];
            if ((type.ObjectFlags() & ObjectFlags.Reference) != 0)
                foreach (var key in checker.VerificationDataReferenceKeys(type))
                    data.Keys.Add(new(record.Type, record.Type, key.Name, key.BoolValue));
            if ((type.Flags() & TypeFlags.UniqueESSymbol) != 0)
            {
                data.Keys.Add(new(record.Type, null, "unique-symbol.cache-membership", checker.VerificationCallableUniqueSymbolMembership(type)));
                data.Keys.Add(new(record.Type, null, "unique-symbol.cache-repeated", checker.VerificationCallableUniqueSymbolMembership(type)));
            }

            if ((type.Flags() & TypeFlags.UnionOrIntersection) != 0)
                data.Keys.Add(new(record.Type, null, "composite.intern-membership", checker.VerificationDataInternMembership(type)));
            foreach (var field in record.Fields)
                if (field.Kind == "instantiations")
                {
                    if (field.Refs.Count > 0)
                        data.Keys.Add(new(record.Type, null, "reference.instantiations-all-key-associated", checker.VerificationDataCacheAssociation(type)));
                    foreach (int id in field.Refs)
                    {
                        data.Keys.Add(new(record.Type, id, "reference.instantiation-membership", checker.VerificationDataKeyMembership(type, rawTypes[id])));
                        if ((type.ObjectFlags() & ObjectFlags.Anonymous) != 0)
                        {
                            var entry = checker.VerificationDataInstantiation(type, rawTypes[id]);
                            if (entry.Arguments.Any(t => !types.ContainsKey(t)))
                                throw new InvalidDataException("Unrooted anonymous instantiation argument after graph fixedpoint");
                            if (entry.Alias is not null && !dataAliases.ContainsKey(entry.Alias))
                                throw new InvalidDataException("Unrooted anonymous instantiation alias after graph fixedpoint");
                            (data.InstantiationAssociations ??= []).Add(new(record.Type, id, entry.Family, entry.Arguments.Select(t => types[t]).ToList(), entry.Alias is null ? null : dataAliases[entry.Alias], entry.SingleSignature, entry.Membership, checker.VerificationDataInstantiation(type, rawTypes[id]).Membership));
                        }
                    }
                }
        }

        if (rawTypes.Count > 1)
        {
            TypeAlias? first = null, second = null;
            foreach (var record in data.Types)
                if (rawTypes[record.Type].Alias()is { } alias)
                {
                    if (first is null)
                        first = alias;
                    else if (second is null && !ReferenceEquals(alias, first))
                        second = alias;
                }

            foreach (var key in checker.VerificationDataKeyEquality(rawTypes[0], rawTypes[1], first, second))
                data.Keys.Add(new(0, 1, key.Name, key.BoolValue));
        }

        return data;
    }
}
