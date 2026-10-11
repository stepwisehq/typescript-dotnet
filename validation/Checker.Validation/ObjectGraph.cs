// Authored static Native AOT structural observer; no reflection serialization.
using TypeScript.Scanner;
using CheckType = TypeScript.Checking.Type;

namespace TypeScript.Checking.Validation;
public sealed class ObjectGraphValue
{
    public List<ObjectTypeObservation> Types { get; set; } = [];
    public List<PropertyLinkObservation> PropertyLinks { get; set; } = [];
    public List<WidenObservation> Widenings { get; set; } = [];
    public List<ObjectCacheObservation> Caches { get; set; } = [];
    public List<UtilityObservation> RelationKeys { get; set; } = [];
}

public sealed class ObjectTypeObservation
{
    public int Type { get; set; }
    public uint Flags { get; set; }
    public uint ObjectFlags { get; set; }
    public int? Members { get; set; }
    public bool PropertiesNull { get; set; }
    public List<ObjectPropertyObservation> Properties { get; set; } = [];
    public bool RawPropertiesNull { get; set; }
    public List<int> RawProperties { get; set; } = [];
    public bool SignaturesNull { get; set; }
    public int SignatureCount { get; set; }
    public int CallSignatureCount { get; set; }
    public bool IndexInfosNull { get; set; }
    public int IndexInfoCount { get; set; }
    public int? Target { get; set; }
}

public sealed record ObjectPropertyObservation(int Symbol, int Type, uint ModifierFlags, List<int?> DeclarationSymbols);
public sealed record PropertyLinkObservation(int Symbol, bool Present, int? Target, int? ResolvedType, int? WriteType, int? NameType, int? ContainingType, bool FunctionOrConstructorChecked);
public sealed record WidenObservation(int Node, int Source, int Widened, bool RepeatedIdentity);
public sealed record ObjectCacheObservation(int Type, int? Widened, int? Regular);
public sealed partial class Graph
{
    public ObjectGraphValue Objects(List<int> widenNodes, Dictionary<int, CheckType> queried)
    {
        var value = new ObjectGraphValue();
        foreach (var key in checker.VerificationRelationKeyEquality())
            value.RelationKeys.Add(new() { Name = key.Name, BoolValue = key.BoolValue });
        foreach (int node in widenNodes)
        {
            if (!queried.TryGetValue(node, out var source))
                throw new InvalidDataException("Widening source must be a queried node");
            var first = checker.GetWidenedType(source);
            value.Widenings.Add(new(node, Type(source)!.Value, Type(first)!.Value, ReferenceEquals(first, checker.GetWidenedType(source))));
        }

        var propertySymbols = new List<Symbol>();
        var seenProperties = new HashSet<Symbol>(ReferenceEqualityComparer.Instance);
        void AddProperty(Symbol? symbol)
        {
            if (symbol is not null && seenProperties.Add(symbol))
                propertySymbols.Add(symbol);
        }

        int typeCursor = 0, linkCursor = 0;
        while (typeCursor < rawTypes.Count || linkCursor < propertySymbols.Count)
        {
            while (typeCursor < rawTypes.Count)
            {
                int id = typeCursor++;
                var type = rawTypes[id];
                if ((type.Flags() & TypeFlags.StructuredType) == 0)
                    continue;
                var properties = checker.GetPropertiesOfType(type);
                var storage = type.AsStructuredType();
                if ((storage.signatures.Length) != 0 || (storage.indexInfos?.Count ?? 0) != 0 || storage.callSignatureCount != 0)
                    throw new InvalidOperationException("Non-data structured type in object observer");
                var record = new ObjectTypeObservation
                {
                    Type = id,
                    Members = Table(storage.members),
                    PropertiesNull = properties is null,
                    RawPropertiesNull = storage.properties is null,
                    RawProperties = (storage.properties ?? []).Select(s => Symbol(s)!.Value).ToList(),
                    SignaturesNull = storage.signatures.IsNil,
                    SignatureCount = storage.signatures.Length,
                    CallSignatureCount = storage.callSignatureCount,
                    IndexInfosNull = storage.indexInfos is null,
                    IndexInfoCount = storage.indexInfos?.Count ?? 0,
                    Target = (type.Flags() & TypeFlags.Object) != 0 ? Type(type.AsObjectType().target) : null,
                };
                foreach (var symbol in properties ?? [])
                {
                    AddProperty(symbol);
                    record.Properties.Add(new(Symbol(symbol)!.Value, Type(checker.GetTypeOfSymbol(symbol))!.Value, (uint)Checker.GetDeclarationModifierFlagsFromSymbol(symbol), (symbol.Declarations ?? []).Select(n => Symbol(n.Symbol())).ToList()));
                }

                value.Types.Add(record);
                if ((type.Flags() & TypeFlags.Object) != 0)
                    value.Caches.Add(new(id, Type(checker.VerificationCachedObjectType(type, "Widened")), Type(checker.VerificationCachedObjectType(type, "RegularObjectLiteral"))));
            }

            while (linkCursor < propertySymbols.Count)
            {
                var symbol = propertySymbols[linkCursor++];
                var location = checker.valueSymbolLinks.TryGet(symbol);
                if (location.IsEmpty)
                    value.PropertyLinks.Add(new(Symbol(symbol)!.Value, false, null, null, null, null, null, false));
                else
                {
                    ref var links = ref location.Span[0];
                    AddProperty(links.target);
                    value.PropertyLinks.Add(new(Symbol(symbol)!.Value, true, Symbol(links.target), Type(links.resolvedType), Type(links.writeType), Type(links.nameType), Type(links.containingType), links.functionOrConstructorChecked));
                }
            }
        }

        foreach (var record in value.Types)
        {
            var type = rawTypes[record.Type];
            record.Flags = (uint)type.Flags();
            record.ObjectFlags = (uint)type.ObjectFlags();
        }

        return value;
    }
}
