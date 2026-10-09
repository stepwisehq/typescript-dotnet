// Authored multi-file stored-state observer; no type/member resolution calls.
using TypeScript.Scanner;
using System.Globalization;
using System.Text.Json;
using TypeScript.Scanner.ParserValidation;

namespace TypeScript.Checking.CollectionsValidation;
public sealed class PassiveGraph
{
    private readonly Encoder syntax;
    private readonly bool lexicalPrivateOwner;
    private readonly Func<string, Symbol, BoundName?>? nameProjection;
    private readonly Dictionary<Symbol, int> symbolIds = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<SymbolTable, int> tableIds = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<FlowNode, int> flowIds = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<FlowList, int> listIds = new(ReferenceEqualityComparer.Instance);
    private readonly Binding output = new();
    public PassiveGraph(Encoder syntax, bool lexicalPrivateOwner = false, Func<string, Symbol, BoundName?>? nameProjection = null)
    {
        this.syntax = syntax;
        this.lexicalPrivateOwner = lexicalPrivateOwner;
        this.nameProjection = nameProjection;
    }

    public IReadOnlyList<Symbol> RawSymbols => symbolIds.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToList();

    public int ExistingSymbolId(Symbol symbol) => symbolIds.TryGetValue(symbol, out int id) ? id : throw new InvalidDataException("Unrooted cache argument symbol seed");
    public int? Flow(FlowNode? flow) => AddFlow(flow);
    public int ExistingFlowId(FlowNode flow) => flowIds.TryGetValue(flow, out int id) ? id : throw new InvalidDataException("Unrooted retained flow cache owner");
    public int? Symbol(Symbol? symbol) => AddSymbol(symbol);
    public int? Table(SymbolTable? table) => AddTable(table);
    public int? AddNode(Node? node) => node is null ? null : syntax.Add(node);
    private BoundName Name(string name, Symbol symbol)
    {
        if (nameProjection?.Invoke(name, symbol) is { } projected)
            return projected;
        foreach (var declaration in symbol.Declarations ?? [])
        {
            if (declaration.Name()is { Kind: Kind.PrivateIdentifier } identifier && name.StartsWith("\u00fe#", StringComparison.Ordinal))
            {
                var owner = (lexicalPrivateOwner ? Ast.GetContainingClass(declaration)?.Symbol() : symbol.Parent) ?? throw new InvalidOperationException("Private symbol has no parent");
                const string prefix = "\u00fe#";
                string suffix = "@" + identifier.Text();
                ulong ownerId = Ast.GetSymbolId(owner);
                string numericOwner = lexicalPrivateOwner ? unchecked((long)ownerId).ToString(CultureInfo.InvariantCulture) : ownerId.ToString(CultureInfo.InvariantCulture);
                string expected = prefix + numericOwner + suffix;
                if (name != expected)
                    throw new InvalidOperationException("Private name owner mismatch");
                return new()
                {
                    Kind = "private",
                    PrefixBase64 = Encoder.Bytes(prefix),
                    SuffixBase64 = Encoder.Bytes(suffix),
                    OwnerSymbolId = AddSymbol(owner)
                };
            }

            if (Ast.IsAmbientModule(declaration))
            {
                var module = declaration.AsModuleDeclaration();
                if (module.Attributes is null)
                    continue;
                string pattern = module.Name().Text();
                int star = pattern.IndexOf('*');
                if (star < 0 || star != pattern.LastIndexOf('*'))
                    continue;
                string prefix = "\u00fe\"" + pattern + "\"pattern@";
                string expected = prefix + Ast.GetNodeId(module.Attributes).ToString(CultureInfo.InvariantCulture);
                if (name != expected)
                    throw new InvalidOperationException("Pattern name owner mismatch");
                return new()
                {
                    Kind = "pattern",
                    PrefixBase64 = Encoder.Bytes(prefix),
                    OwnerNodeId = AddNode(module.Attributes)
                };
            }
        }

        return new()
        {
            Kind = "raw",
            BytesBase64 = Encoder.Bytes(name)
        };
    }

    private int? AddSymbol(Symbol? symbol)
    {
        if (symbol is null)
            return null;
        if (symbolIds.TryGetValue(symbol, out int id))
            return id;
        id = output.Symbols.Count;
        symbolIds.Add(symbol, id);
        var value = new BoundSymbol
        {
            Id = id,
            Flags = (uint)symbol.Flags,
            CheckFlags = (uint)symbol.CheckFlags,
            DeclarationsNull = symbol.Declarations is null
        };
        output.Symbols.Add(value);
        value.Name = Name(symbol.Name, symbol);
        foreach (var declaration in symbol.Declarations ?? [])
            value.Declarations.Add(AddNode(declaration)!.Value);
        value.ValueDeclaration = AddNode(symbol.ValueDeclaration);
        value.Parent = AddSymbol(symbol.Parent);
        value.ExportSymbol = AddSymbol(symbol.ExportSymbol);
        value.Members = AddTable(symbol.Members);
        value.Exports = AddTable(symbol.Exports);
        return id;
    }

    private int? AddTable(SymbolTable? table)
    {
        if (table is null)
            return null;
        if (tableIds.TryGetValue(table, out int id))
            return id;
        id = output.Tables.Count;
        tableIds.Add(table, id);
        var value = new BoundTable(id, []);
        output.Tables.Add(value);
        foreach (string key in table.Keys.OrderBy(key => SortKey(key, table[key]), StringComparer.Ordinal))
        {
            var symbol = table[key] ?? throw new InvalidOperationException("Nil symbol table entry");
            value.Entries.Add(new(Name(key, symbol), AddSymbol(symbol)!.Value));
        }

        return id;
    }

    private string SortKey(string name, Symbol symbol)
    {
        foreach (var declaration in symbol.Declarations ?? [])
        {
            if (declaration.Name()is { Kind: Kind.PrivateIdentifier } identifier && name.StartsWith("\u00fe#", StringComparison.Ordinal))
            {
                var owner = lexicalPrivateOwner ? Ast.GetContainingClass(declaration)?.Symbol() : symbol.Parent;
                if (owner?.Declarations is not { Count: > 0 })
                    throw new InvalidOperationException("Private sorting owner missing");
                int id = FindSyntaxOwner(owner.Declarations[0]);
                return "\u00fe#" + id.ToString("D20", CultureInfo.InvariantCulture) + "@" + identifier.Text();
            }

            if (Ast.IsAmbientModule(declaration))
            {
                var module = declaration.AsModuleDeclaration();
                if (module.Attributes is null)
                    continue;
                string pattern = module.Name().Text();
                int star = pattern.IndexOf('*');
                if (star < 0 || star != pattern.LastIndexOf('*'))
                    continue;
                return "\u00fe\"" + pattern + "\"pattern@" + FindSyntaxOwner(module.Attributes).ToString("D20", CultureInfo.InvariantCulture);
            }
        }

        return name;
    }

    private int FindSyntaxOwner(Node node)
    {
        for (int i = 0; i < syntax.RawNodes.Count; i++)
            if (ReferenceEquals(syntax.RawNodes[i], node))
                return i;
        throw new InvalidOperationException("Opaque name sorting owner outside syntax");
    }

    private int? AddFlow(FlowNode? flow)
    {
        if (flow is null)
            return null;
        if (flowIds.TryGetValue(flow, out int id))
            return id;
        id = output.Flows.Count;
        flowIds.Add(flow, id);
        var value = new BoundFlow
        {
            Id = id,
            Flags = (uint)flow.Flags
        };
        output.Flows.Add(value);
        value.Node = AddNode(flow.Node);
        value.Antecedent = AddFlow(flow.Antecedent);
        value.Antecedents = AddList(flow.Antecedents);
        return id;
    }

    private int? AddList(FlowList? list)
    {
        if (list is null)
            return null;
        if (listIds.TryGetValue(list, out int id))
            return id;
        id = output.FlowLists.Count;
        listIds.Add(list, id);
        var value = new BoundFlowList
        {
            Id = id
        };
        output.FlowLists.Add(value);
        value.Flow = AddFlow(list.Flow);
        value.Next = AddList(list.Next);
        return id;
    }

    private BoundNode Fields(int id)
    {
        var fields = new List<BoundField>();
        foreach (var field in syntax.RawNodes[id].BoundFields())
        {
            string name = char.ToUpperInvariant(field.Role[0]) + field.Role[1..];
            int? reference = field.Type switch
            {
                "symbol" => AddSymbol((Symbol? )field.Value),
                "table" => AddTable((SymbolTable? )field.Value),
                "node" => AddNode((Node? )field.Value),
                "flow" => AddFlow((FlowNode? )field.Value),
                "flowList" => AddList((FlowList? )field.Value),
                _ => throw new InvalidOperationException("Unknown bound field type: " + field.Type),
            };
            fields.Add(new(name, field.Type, reference));
        }

        return new(id, fields);
    }

    private static void SameIdentities<T>(Dictionary<T, int> first, Dictionary<T, int> second)
        where T : notnull
    {
        if (first.Count != second.Count || first.Any(pair => !second.TryGetValue(pair.Key, out int id) || id != pair.Value))
            throw new InvalidOperationException("Repeated binding replaced graph identities");
    }

    public void AssertSameIdentities(PassiveGraph second)
    {
        if (syntax.RawNodes.Count != second.syntax.RawNodes.Count || syntax.RawNodes.Where((node, id) => !ReferenceEquals(node, second.syntax.RawNodes[id])).Any())
            throw new InvalidOperationException("Repeated binding replaced syntax identities");
        SameIdentities(symbolIds, second.symbolIds);
        SameIdentities(tableIds, second.tableIds);
        SameIdentities(flowIds, second.flowIds);
        SameIdentities(listIds, second.listIds);
    }

    private static void ValidateDiagnostics(IEnumerable<Diagnostic>? items, SourceFile file)
    {
        foreach (var diagnostic in items ?? [])
        {
            if (diagnostic.ReportsUnnecessary() || diagnostic.ReportsDeprecated() || diagnostic.SkippedOnNoEmit() || diagnostic.Source() != "" || (diagnostic.File()is not null && !ReferenceEquals(diagnostic.File(), file)))
                throw new InvalidOperationException("Unexpected binder diagnostic metadata outside canonical schema");
            ValidateDiagnostics(diagnostic.RelatedInformation(), file);
            ValidateDiagnostics(diagnostic.MessageChain(), file);
        }
    }

    public void CompleteNodes()
    {
        while (output.Nodes.Count < syntax.RawNodes.Count)
            output.Nodes.Add(Fields(output.Nodes.Count));
        syntax.FinishParents();
    }

    public Binding Encode(SourceFile file)
    {
        while (output.Nodes.Count < syntax.RawNodes.Count)
            output.Nodes.Add(Fields(output.Nodes.Count));
        output.GlobalExports = AddTable(file.GlobalExports);
        output.PatternsNull = file.PatternAmbientModules is null;
        foreach (var pattern in file.PatternAmbientModules ?? [])
            output.Patterns.Add(new(Encoder.Bytes(pattern.Pattern.Text), pattern.Pattern.StarIndex, AddSymbol(pattern.Symbol)!.Value));
        while (output.Nodes.Count < syntax.RawNodes.Count)
            output.Nodes.Add(Fields(output.Nodes.Count));
        ValidateDiagnostics(file.BindDiagnostics(), file);
        output.Diagnostics = Encoder.Diagnostics(file.BindDiagnostics());
        output.IsBound = file.IsBound();
        output.SymbolCount = file.SymbolCount;
        syntax.FinishParents();
        return output;
    }
}
