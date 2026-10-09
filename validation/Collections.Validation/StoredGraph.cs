// Authored nonforcing graph. The Go producer independently discovers original
// fields; this static Native AOT consumer reads the actual managed model slots.
using System.Collections;
using TypeScript.Scanner;
using Encoder = TypeScript.Scanner.ParserValidation.Encoder;

namespace TypeScript.Checking.CollectionsValidation;
public sealed class StoredValue
{
    public string Kind { get; set; } = "";
    public bool Null { get; set; }
    public int? Ref { get; set; }
    public List<StoredValue> Values { get; set; } = [];
    public List<StoredField> Fields { get; set; } = [];
    public string Bytes { get; set; } = "";
    public long Number { get; set; }
    public bool Bool { get; set; }
}

public sealed record StoredField(string Name, StoredValue Value);
public sealed record StoredRecord(int Id, string Name, List<StoredField> Fields);
public sealed record StoredNodeType(int Node, int? Type);
public sealed record StoredState(string OwnerKind, int Owner, string Store, StoredValue Value);
public sealed record StoredInventory(string Store, string OwnerKind, bool Null, int Count, List<int> Owners);
public sealed class StoredGraphValue
{
    public List<int?> Roots { get; set; } = [];
    public List<StoredState> States { get; set; } = [];
    public List<StoredNodeType> SyntheticTypes { get; set; } = [];
    public List<StoredRecord> Records { get; set; } = [];
    public List<StoredInventory> Inventories { get; set; } = [];
    public List<StoredField> GlobalCaches { get; set; } = [];
}

public sealed partial class StoredGraph
{
    private readonly Checker checker;
    private readonly PassiveGraph binding;
    private readonly Encoder syntax;
    private readonly Dictionary<object, int> ids = new(ReferenceEqualityComparer.Instance);
    private readonly List<object> queue = [];
    private readonly StoredGraphValue output = new();
    private readonly HashSet<object> comparerOwners = new(ReferenceEqualityComparer.Instance);
    public StoredGraph(Checker checker, PassiveGraph binding, Encoder syntax)
    {
        this.checker = checker;
        this.binding = binding;
        this.syntax = syntax;
    }

    private int? Reference(object? value)
    {
        if (value is null)
            return null;
        if (ids.TryGetValue(value, out int id))
            return id;
        id = queue.Count;
        ids.Add(value, id);
        queue.Add(value);
        output.Records.Add(new(id, "", []));
        return id;
    }

    private List<StoredField> Fields(object payload)
    {
        if (comparerOwners.Contains(payload))
            return[new("currentRelation", Value(checker.VerificationCollectionsComparerOwner(payload)))];
        var description = checker.VerificationCollectionsDescribe(payload);
        if (description.Fields.Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() != description.Fields.Count)
            throw new InvalidDataException("Duplicate flattened stored field");
        var fields = description.Fields.OrderBy(field => field.Name == "instantiations" ? 1 : 0).ThenBy(field => field.Name, StringComparer.Ordinal).Select(field => new StoredField(field.Name, FieldValue(payload, field.Name, field.Value, field.Kind))).ToList().OrderBy(field => field.Name, StringComparer.Ordinal).ToList();
        if (payload is Type type)
        {
            if ((type.Flags() & (TypeFlags.Union | TypeFlags.Intersection)) != 0)
            {
                var intern = checker.VerificationCollectionsInternMembership(type);
                fields.Add(new("authoredInternMembership", new StoredValue { Kind = "inline", Fields = [new("noConstraintReduction", Value(intern.NoConstraintReduction)), new("normal", Value(intern.Normal))] }));
            }

            var origin = checker.VerificationCollectionsObjectOrigin(type);
            if (origin.Present)
                fields.Add(new("authoredObjectProducer", new StoredValue { Kind = "inline", Fields = [new("contextualType", Value(origin.ContextualType, "reference")), new("mode", Value(origin.Mode)), new("node", Value(origin.Node)), new("source", Value(origin.Source, "reference")), new("applicability", ApplicabilityValue(origin.Applicability))] }));
        }

        return fields.OrderBy(field => field.Name, StringComparer.Ordinal).ToList();
    }

    private StoredValue FieldValue(object payload, string name, object? value, string kind)
    {
        try
        {
            return FieldValueCore(payload, name, value, kind);
        }
        catch (InvalidDataException error)
        {
            throw new InvalidDataException("Stored field " + payload.GetType().Name + "." + name + " (" + kind + "): " + error.Message, error);
        }
    }

    private StoredValue FieldValueCore(object payload, string name, object? value, string kind)
    {
        if (payload is Checker.InferenceContext && name == "compareTypes" && value is TypeComparer comparer)
        {
            var actual = checker.VerificationCollectionsComparer(comparer);
            StoredValue owner;
            if (ReferenceEquals(actual.Owner, checker))
                owner = new StoredValue
                {
                    Kind = "checker-owner",
                    Bool = true
                };
            else
            {
                comparerOwners.Add(actual.Owner);
                owner = Value(actual.Owner);
            }

            return new StoredValue
            {
                Kind = "inline",
                Fields = [new("currentRelation", Value(actual.CurrentRelation)), new("family", Value(actual.Family)), new("intersectionState", Value(actual.IntersectionState)), new("owner", owner)]
            };
        }

        if (payload is TypeMapper mapper && mapper.data is DeferredTypeMapper && name == "targets")
        {
            if (value is not List<Func<Type>> targets)
                throw new InvalidDataException("Missing deferred target list");
            var result = new StoredValue
            {
                Kind = "list"
            };
            foreach (var target in targets)
            {
                var actual = checker.VerificationCollectionsDeferredTarget(target);
                result.Values.Add(new StoredValue { Kind = "inline", Fields = [new("index", Value(actual.Index)), new("parent", Value(actual.Parent)), new("typeParameters", Value(actual.TypeParameters, "list"))] });
            }

            return result;
        }

        if (payload is Type cacheOwner && name == "instantiations" && value is IDictionary { Count: > 0 })
            return (cacheOwner.objectFlags & ObjectFlags.Mapped) != 0 || cacheOwner.AsTypeReference()is { node: not null } && cacheOwner.AsInterfaceType() == null ? InstantiationCache(cacheOwner, true) : ReferenceCache(cacheOwner);
        if (payload is TypeAliasLinks or ConditionalRoot && name == "instantiations" && value is IDictionary { Count: > 0 })
            return InstantiationCache(payload, false);
        if (payload is Type type && (type.Flags() & TypeFlags.UniqueESSymbol) != 0 && name == "name")
        {
            var owner = type.Symbol() ?? throw new InvalidDataException("Unique symbol lacks actual owner");
            if (owner.id == 0)
                throw new InvalidDataException("Unique symbol owner lacks already allocated ID");
            string expected = Ast.InternalSymbolNamePrefix + "@" + owner.Name + "@" + unchecked((ulong)owner.id).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is not string actual || actual != expected)
                throw new InvalidDataException("Unique symbol name preimage mismatch");
            return new StoredValue
            {
                Kind = "inline",
                Fields = [new("symbol", Value(owner)), new("symbolName", Value(owner.Name))]
            };
        }

        return Value(value, kind);
    }

    private StoredValue ApplicabilityValue(Checker.CollectionsApplicabilityOrigin? a) => a == null ? new()
    {
        Kind = "inline",
        Null = true
    }

    : new()
    {
        Kind = "inline",
        Fields = [new("checkMode", Value(a.Mode)), new("node", Value(a.Node)), new("phase", Value(a.Phase)), new("relation", Value(a.Relation)), new("reportErrors", Value(a.ReportErrors)), new("signature", Value(a.Signature, "reference"))]
    };
    private string ApplicabilitySeed(Checker.CollectionsApplicabilityOrigin? a)
    {
        if (a == null)
            return "null";
        string seed = "phase:" + a.Phase + "/node:" + NodeSeed(a.Node) + "/mode:" + a.Mode + "/relation:" + a.Relation + "/report:" + (a.ReportErrors == null ? "null" : a.ReportErrors.Value ? "true" : "false") + "/signature:" + (a.Signature == null ? "null" : (uint)a.Signature.flags + "/" + (a.Signature.declaration == null ? "null" : NodeSeed(a.Signature.declaration)));
        if (a.Signature != null && ids.TryGetValue(a.Signature, out int id))
            seed += "/root:" + id;
        return seed;
    }

    private string MapperSeed(TypeMapper mapper, Dictionary<Type, int> types, HashSet<TypeMapper> seen)
    {
        if (ids.TryGetValue(mapper, out int id))
            return "root:" + id;
        if (!seen.Add(mapper))
            throw new InvalidDataException("Unrooted mapper seed cycle");
        try
        {
            string One(Type? value) => TypeSeed(value, types);
            string List(List<Type>? values) => string.Join(";", (values ?? []).Select(One));
            return mapper.data switch
            {
                SimpleTypeMapper m => "simple(" + One(m.source) + "," + One(m.target) + ")",
                ArrayTypeMapper m => "array(" + List(m.sources) + "=>" + List(m.targets) + ")",
                ArrayToSingleTypeMapper m => "array-single(" + List(m.sources) + "=>" + One(m.target) + ")",
                MergedTypeMapper m => "MergedTypeMapper(" + MapperSeed(m.m1, types, seen) + "," + MapperSeed(m.m2, types, seen) + ")",
                CompositeTypeMapper m => "CompositeTypeMapper(" + MapperSeed(m.m1, types, seen) + "," + MapperSeed(m.m2, types, seen) + ")",
                DeferredTypeMapper m => "deferred(" + List(m.sources) + "=>" + string.Join(";", (m.targets ?? []).Select(fn =>
                {
                    var t = checker.VerificationCollectionsDeferredTarget(fn);
                    return NodeSeed(t.Parent) + "/" + t.Index + "(" + List(t.TypeParameters) + ")";
                })) + ")",
                _ => throw new InvalidDataException("Unrooted mapper cache provenance: " + mapper.data.GetType().Name)};
        }
        finally
        {
            seen.Remove(mapper);
        }
    }

    private string TypeSeed(Type? type, Dictionary<Type, int> stack)
    {
        if (type is null)
            return "null";
        if (stack.TryGetValue(type, out int index))
            return "cycle:" + index;
        stack.Add(type, stack.Count);
        try
        {
            string name = type.DataType();
            string seed = ((uint)type.Flags()) + "|" + ((uint)type.ObjectFlags()) + "|" + name;
            if (ids.TryGetValue(type, out int rootedId))
                seed += "|root:" + rootedId;
            if (type.Symbol()is { } symbol)
                seed += "|symbol:" + binding.ExistingSymbolId(symbol);
            if (type.alias is { } alias)
                seed += "|alias:" + binding.ExistingSymbolId(alias.symbol) + "(" + string.Concat((alias.typeArguments ?? []).Select(arg => TypeSeed(arg, stack) + ";")) + ")";
            if (type.AsObjectType()is { } objectType)
            {
                if (objectType.target != null)
                    seed += "|target(" + TypeSeed(objectType.target, stack) + ")";
                if (objectType.mapper != null)
                    seed += "|mapper(" + MapperSeed(objectType.mapper, stack, new(ReferenceEqualityComparer.Instance)) + ")";
            }

            switch (name)
            {
                case "MappedType":
                    var mapped = type.AsMappedType();
                    if (mapped.declaration != null)
                        seed += "|declaration:" + NodeSeed(mapped.declaration.AsNode());
                    foreach (var field in new (string Name, Type? Value)[]
                    {
                        ("typeParameter", mapped.typeParameter),
                        ("constraintType", mapped.constraintType),
                        ("nameType", mapped.nameType),
                        ("templateType", mapped.templateType),
                        ("modifiersType", mapped.modifiersType),
                        ("resolvedApparentType", mapped.resolvedApparentType)
                    }

                    )
                        seed += "|" + field.Name + "(" + TypeSeed(field.Value, stack) + ")";
                    break;
                case "IndexType":
                    seed += "|type(" + TypeSeed(type.AsIndexType().target, stack) + ")";
                    break;
                case "IndexedAccessType":
                    seed += "|objectType(" + TypeSeed(type.AsIndexedAccessType().objectType, stack) + ")|indexType(" + TypeSeed(type.AsIndexedAccessType().indexType, stack) + ")|access:" + (uint)type.AsIndexedAccessType().accessFlags;
                    break;
                case "SubstitutionType":
                    seed += "|baseType(" + TypeSeed(type.AsSubstitutionType().baseType, stack) + ")|constraint(" + TypeSeed(type.AsSubstitutionType().constraint, stack) + ")";
                    break;
                case "ObjectType":
                    var origin = checker.VerificationCollectionsObjectOrigin(type);
                    if (origin.Present)
                    {
                        int nodeId = -1;
                        for (int i = 0; i < syntax.RawNodes.Count; i++)
                            if (ReferenceEquals(syntax.RawNodes[i], origin.Node))
                            {
                                nodeId = i;
                                break;
                            }

                        if (nodeId < 0)
                            throw new InvalidDataException("Unrooted object producer node");
                        seed += "|producer:" + nodeId + "/" + origin.Mode + "/context(" + TypeSeed(origin.ContextualType, stack) + ")/source(" + TypeSeed(origin.Source, stack) + ")";
                        seed += "/applicability(" + ApplicabilitySeed(origin.Applicability) + ")";
                    }

                    foreach (var member in (type.AsStructuredType().members ?? []).OrderBy(entry => entry.Key, StringComparer.Ordinal))
                    {
                        var propertySymbol = member.Value;
                        Type? stored = propertySymbol.id == 0 ? null : checker.valueSymbolLinks.TryGet(propertySymbol)?.resolvedType;
                        seed += "|member:" + Encoder.Bytes(member.Key) + "/" + (uint)propertySymbol.Flags + "/" + (uint)propertySymbol.CheckFlags + "/" + TypeSeed(stored, stack);
                    }

                    break;
                case "TypeParameter":
                    var parameter = type.AsTypeParameter();
                    seed += "|this:" + (parameter.isThisType ? "true" : "false") + "|distributed:" + (parameter.isDistributed ? "true" : "false");
                    seed += "|target(" + TypeSeed(parameter.target, stack) + ")|constraint(" + TypeSeed(parameter.constraint, stack) + ")";
                    break;
                case "IntrinsicType":
                    seed += "|" + Encoder.Bytes(type.AsIntrinsicType().IntrinsicName());
                    break;
                case "LiteralType":
                    seed += "|freshSelf:" + (ReferenceEquals(type.AsLiteralType().freshType, type) ? "true" : "false") + "|regularSelf:" + (ReferenceEquals(type.AsLiteralType().regularType, type) ? "true" : "false");
                    seed += "|" + (type.AsLiteralType().Value() switch
                    {
                        string text => Encoder.Bytes(text),
                        bool boolean => boolean ? "true" : "false",
                        double number => unchecked((ulong)BitConverter.DoubleToInt64Bits(number)).ToString("x16"),
                        JsNum.Number number => unchecked((ulong)BitConverter.DoubleToInt64Bits(number.Value)).ToString("x16"),
                        _ => throw new InvalidDataException("Unsupported literal cache seed")});
                    break;
                case "TypeReference":
                case "InterfaceType":
                case "TupleType":
                    seed += "|args(" + string.Join(";", (type.AsTypeReference().resolvedTypeArguments ?? []).Select(t => TypeSeed(t, stack))) + ")";
                    break;
                case "UnionType":
                case "IntersectionType":
                    seed += "|types(" + string.Join(";", type.Types().Select(t => TypeSeed(t, stack))) + ")";
                    break;
            }

            return seed;
        }
        finally
        {
            stack.Remove(type);
        }
    }

    private StoredValue ReferenceCache(Type owner)
    {
        var cache = checker.VerificationCollectionsObjectInstantiations(owner);
        var rows = (cache.Entries ?? []).Select(entry => (Seed: entry.Family + "|" + string.Join(";", (entry.Arguments ?? []).Select(t => TypeSeed(t, new(ReferenceEqualityComparer.Instance)))) + "|single:" + (entry.SingleSignature ? "true" : "false") + (entry.Alias == null ? "" : "|alias:" + binding.ExistingSymbolId(entry.Alias.symbol) + "(" + string.Concat((entry.Alias.typeArguments ?? []).Select(t => TypeSeed(t, new(ReferenceEqualityComparer.Instance)) + ";")) + ")"), Entry: entry)).ToList();
        if (rows.Select(row => row.Seed).Distinct(StringComparer.Ordinal).Count() != rows.Count)
            throw new InvalidDataException("Reference argument descriptor collision");
        var result = new StoredValue
        {
            Kind = "cache",
            Null = cache.IsNull
        };
        foreach (var row in rows.OrderBy(row => row.Seed, StringComparer.Ordinal))
        {
            var entry = row.Entry;
            result.Values.Add(new StoredValue { Kind = "inline", Fields = [new("alias", Value(entry.Alias, "reference")), new("arguments", Value(entry.Arguments, "list")), new("family", Value(entry.Family)), new("isSelf", Value(entry.IsSelf)), new("membership", Value(true)), new("singleSignature", Value(entry.SingleSignature)), new("target", Value(entry.Target)), new("value", Value(entry.Value))] });
        }

        if (result.Values.Count != cache.Count)
            throw new InvalidDataException("Reference cache count mismatch");
        return result;
    }

    private (Dictionary<uint, Type> Types, Dictionary<ulong, Symbol> Symbols) KnownInstantiationEntities()
    {
        var types = new Dictionary<uint, Type>();
        var symbols = new Dictionary<ulong, Symbol>();
        foreach (var symbol in binding.RawSymbols)
            if (symbol.id != 0 && !symbols.TryAdd(unchecked((ulong)symbol.id), symbol))
                throw new InvalidDataException("Duplicate actual symbol ID");
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        void Walk(object? value)
        {
            if (value is null || comparerOwners.Contains(value) || value is Node or NodeData or Symbol or SymbolTable or string or Delegate or GoOrderedNodeSet || value.GetType().IsValueType)
                return;
            if (!seen.Add(value))
                return;
            if (value is Type type && type.id != 0)
            {
                if (types.TryGetValue(type.id, out var old) && !ReferenceEquals(old, type))
                    throw new InvalidDataException("Duplicate actual type ID");
                types[type.id] = type;
            }

            if (value is Type sourceType)
            {
                var origin = checker.VerificationCollectionsObjectOrigin(sourceType);
                if (origin.Present)
                {
                    Walk(origin.Source);
                    Walk(origin.ContextualType);
                    Walk(origin.Applicability?.Signature);
                }
            }

            if (value is IDictionary map)
            {
                foreach (object? item in map.Values)
                    Walk(item);
                return;
            }

            if (value is IList list)
            {
                foreach (object? item in list)
                    Walk(item);
                return;
            }

            foreach (var field in checker.VerificationCollectionsDescribe(value).Fields)
                Walk(field.Value);
        }

        // Read-only reachability census: does not register graph IDs or follow
        // map keys, delegates, AST semantics, or unused library symbol types.
        foreach (object item in queue)
            Walk(item);
        foreach (object? item in checker.VerificationCollectionsGlobalStoredValues())
            Walk(item);
        foreach (Type item in checker.VerificationCollectionsFlowValues())
            Walk(item);
        foreach (var slot in checker.VerificationCollectionsStoredTypeSlots())
            Walk(slot.Value);
        foreach (Type input in checker.VerificationCollectionsRelationInputs())
            Walk(input);
        return (types, symbols);
    }

    private StoredValue InstantiationCache(object owner, bool isObject)
    {
        var known = KnownInstantiationEntities();
        var entries = new List<(Type Value, List<Type> Arguments, Symbol? AliasSymbol, List<Type>? AliasArguments, string Family, bool Flag, bool IsSelf)>();
        bool isNull;
        int count;
        if (isObject)
        {
            var cache = checker.VerificationCollectionsReducedObjectInstantiations((Type)owner, known.Types, known.Symbols);
            isNull = cache.IsNull;
            count = cache.Count;
            foreach (var e in cache.Entries ?? [])
                entries.Add((e.Value, e.Arguments, e.AliasSymbol, e.AliasArguments, e.Family, e.SingleSignature, e.IsSelf));
        }
        else
        {
            var cache = checker.VerificationCollectionsInstantiations(owner, known.Types, known.Symbols);
            isNull = cache.IsNull;
            count = cache.Count;
            foreach (var e in cache.Entries ?? [])
                entries.Add((e.Value, e.Arguments, e.AliasSymbol, e.AliasArguments, e.Family, e.ForConstraint, false));
        }

        string Seed((Type Value, List<Type> Arguments, Symbol? AliasSymbol, List<Type>? AliasArguments, string Family, bool Flag, bool IsSelf) e) => e.Family + "|" + string.Concat(e.Arguments.Select(t => TypeSeed(t, new(ReferenceEqualityComparer.Instance)) + ";")) + "|flag:" + (e.Flag ? "true" : "false") + (e.AliasSymbol == null ? "" : "|alias:" + binding.ExistingSymbolId(e.AliasSymbol) + "(" + string.Concat((e.AliasArguments ?? []).Select(t => TypeSeed(t, new(ReferenceEqualityComparer.Instance)) + ";")) + ")");
        var rows = entries.Select(e => (Seed: Seed(e), Entry: e)).ToList();
        if (rows.Select(r => r.Seed).Distinct(StringComparer.Ordinal).Count() != rows.Count)
            throw new InvalidDataException("Instantiation descriptor collision");
        var result = new StoredValue
        {
            Kind = "cache",
            Null = isNull
        };
        foreach (var row in rows.OrderBy(r => r.Seed, StringComparer.Ordinal))
        {
            var e = row.Entry;
            var fields = new List<StoredField>
            {
                new("aliasArguments", Value(e.AliasArguments, "list")),
                new("aliasSymbol", Value(e.AliasSymbol, "symbol")),
                new("arguments", Value(e.Arguments, "list")),
                new("family", Value(e.Family))
            };
            fields.Add(isObject ? new("isSelf", Value(e.IsSelf)) : new("forConstraint", Value(e.Flag)));
            fields.Add(new("membership", Value(true)));
            if (isObject)
                fields.Add(new("singleSignature", Value(e.Flag)));
            fields.Add(new("value", Value(e.Value)));
            result.Values.Add(new StoredValue { Kind = "inline", Fields = fields });
        }

        if (result.Values.Count != count)
            throw new InvalidDataException("Instantiation cache count mismatch");
        return result;
    }

    private StoredValue Value(object? value, string? nullKind = null)
    {
        var result = new StoredValue();
        if (value is null)
        {
            result.Kind = nullKind ?? "null";
            result.Null = true;
            return result;
        }

        switch (value)
        {
            case Node node:
                result.Kind = "node";
                result.Ref = binding.AddNode(node);
                break;
            case NodeData node:
                result.Kind = "node";
                result.Ref = binding.AddNode(node.AsNode());
                break;
            case FlowNode flow:
                result.Kind = "flow";
                result.Ref = binding.Flow(flow);
                break;
            case Symbol symbol:
                result.Kind = "symbol";
                result.Ref = binding.Symbol(symbol);
                break;
            case SymbolTable table:
                result.Kind = "table";
                result.Ref = binding.Table(table);
                break;
            case string text:
                result.Kind = "bytes";
                result.Bytes = Encoder.Bytes(text);
                break;
            case bool flag:
                result.Kind = "bool";
                result.Bool = flag;
                break;
            case Enum number:
                result.Kind = "number";
                result.Number = Convert.ToInt64(number);
                break;
            case int number:
                result.Kind = "number";
                result.Number = number;
                break;
            case uint number:
                result.Kind = "number";
                result.Number = number;
                break;
            case long number:
                result.Kind = "number";
                result.Number = number;
                break;
            case ulong number:
                result.Kind = "number";
                result.Number = unchecked((long)number);
                break;
            case double number:
                result.Kind = "float";
                result.Bytes = unchecked((ulong)BitConverter.DoubleToInt64Bits(number)).ToString("x16");
                break;
            case JsNum.Number number:
                result.Kind = "float";
                result.Bytes = unchecked((ulong)BitConverter.DoubleToInt64Bits(number.Value)).ToString("x16");
                break;
            case JsNum.PseudoBigInt number:
                result.Kind = "inline";
                result.Fields = [new("Base10Value", Value(number.Base10Value ?? "")), new("Negative", Value(number.Negative))];
                break;
            case GoOrderedNodeSet set:
                // Bounded API projection; the managed adapter has no claim to
                // reproduce private OrderedMap allocation/backing topology.
                result.Kind = "inline";
                var ordered = new StoredValue
                {
                    Kind = "list"
                };
                var membership = new StoredValue
                {
                    Kind = "list"
                };
                foreach (Node item in set.Values())
                {
                    ordered.Values.Add(Value(item));
                    membership.Values.Add(Value(set.Has(item)));
                }

                if (ordered.Values.Count != set.Size())
                    throw new InvalidDataException("Ordered node set cardinality mismatch");
                result.Fields = [new("membership", membership), new("size", Value(set.Size())), new("values", ordered)];
                break;
            case IDictionary cache:
                result.Kind = "cache";
                if (cache.Count != 0)
                    throw new InvalidDataException("Unobserved stored cache key family (" + cache.Count + " entries)");
                break;
            case List<Node> nodes:
                result.Kind = "list";
                foreach (var item in nodes)
                    result.Values.Add(Value(item, "node"));
                break;
            case List<Symbol> symbols:
                result.Kind = "list";
                foreach (var item in symbols)
                    result.Values.Add(Value(item, "symbol"));
                break;
            case List<SymbolTable> tables:
                result.Kind = "list";
                foreach (var item in tables)
                    result.Values.Add(Value(item, "table"));
                break;
            case IList list:
                result.Kind = "list";
                foreach (object? item in list)
                    result.Values.Add(Value(item, "reference"));
                break;
            case Delegate:
                throw new InvalidDataException("Unobserved function provenance");
            case Checker.IntraExpressionInferenceSite:
            case TupleElementInfo:
                result.Kind = "inline";
                result.Fields = Fields(value);
                break;
            default:
                result.Kind = "reference";
                result.Ref = Reference(value);
                break;
        }

        return result;
    }

    public StoredGraphValue Encode(IEnumerable<Type> roots)
    {
        foreach (var root in roots)
            output.Roots.Add(Reference(root));
        int nextRecord = 0, nextNode = 0, nextSymbol = 0;
        bool globalCachesRead = false;
        while (!globalCachesRead || nextRecord < queue.Count || nextNode < syntax.RawNodes.Count || nextSymbol < binding.RawSymbols.Count)
        {
            while (nextRecord < queue.Count)
            {
                var description = comparerOwners.Contains(queue[nextRecord]) ? (Name: "RelaterComparerOwner", Fields: new List<(string Name, string Kind, object Value)>()) : checker.VerificationCollectionsDescribe(queue[nextRecord]);
                output.Records[nextRecord] = new(nextRecord, description.Name, Fields(queue[nextRecord]));
                nextRecord++;
            }

            while (nextNode < syntax.RawNodes.Count)
            {
                Node node = syntax.RawNodes[nextNode];
                foreach (var state in checker.VerificationCollectionsNodeStates(node))
                    output.States.Add(new("node", nextNode, state.Store, Value(state.Value)));
                if (node.Kind == Kind.SyntheticExpression)
                {
                    var actual = node.AsSyntheticExpression().Type;
                    if (actual is not null && actual is not Type)
                        throw new InvalidDataException("Unknown synthetic checker type");
                    output.SyntheticTypes.Add(new(nextNode, Reference(actual)));
                }

                nextNode++;
            }

            var symbolSnapshot = binding.RawSymbols;
            while (nextSymbol < symbolSnapshot.Count)
            {
                foreach (var state in checker.VerificationCollectionsSymbolStates(symbolSnapshot[nextSymbol]))
                    output.States.Add(new("symbol", nextSymbol, state.Store, Value(state.Value)));
                nextSymbol++;
            }

            if (!globalCachesRead && nextRecord == queue.Count && nextNode == syntax.RawNodes.Count && nextSymbol == binding.RawSymbols.Count)
            {
                ReadGlobalCaches();
                globalCachesRead = true;
            }
        }

        var nodes = new Dictionary<Node, int>(ReferenceEqualityComparer.Instance);
        for (int id = 0; id < syntax.RawNodes.Count; id++)
            nodes.Add(syntax.RawNodes[id], id);
        foreach (var store in checker.VerificationCollectionsLogicalStoreKeys())
        {
            var owners = new List<int>();
            foreach (object key in store.Keys ?? [])
            {
                if (store.OwnerKind == "node" && key is Node node)
                {
                    if (!nodes.TryGetValue(node, out int id))
                        throw new InvalidDataException("Unrooted logical node store owner: " + store.Store + "/" + node.Kind);
                    owners.Add(id);
                }
                else if (store.OwnerKind == "symbol" && key is Symbol symbol)
                    owners.Add(binding.ExistingSymbolId(symbol));
                else
                    throw new InvalidDataException("Unknown logical store owner kind");
            }

            owners.Sort();
            if (owners.Distinct().Count() != owners.Count)
                throw new InvalidDataException("Collapsed logical store owner identity");
            output.Inventories.Add(new(store.Store, store.OwnerKind, store.Keys is null, owners.Count, owners));
        }

        return output;
    }
}
