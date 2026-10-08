using System.Globalization;
using System.Text;
using TypeScript.Scanner;
using TypeScript.Scanner.ParserValidation;
using Encoder = TypeScript.Scanner.ParserValidation.Encoder;
using CheckType = TypeScript.Checking.Type;

namespace TypeScript.Checking.Validation;
public sealed partial class Graph
{
    private readonly Checker checker;
    private readonly Encoder syntax;
    private readonly Result result;
    private readonly bool objectObservations;
    private readonly List<CheckType> rawTypes = [];
    public Graph(Checker checker, Encoder syntax, Result result, bool objectObservations = false)
    {
        this.checker = checker;
        this.syntax = syntax;
        this.result = result;
        this.objectObservations = objectObservations;
    }

    private readonly Dictionary<CheckType, int> types = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Symbol, int> symbols = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<SymbolTable, int> tables = new(ReferenceEqualityComparer.Instance);
    private int? Node(Node? node)
    {
        if (node is null)
            return null;
        for (int i = 0; i < syntax.RawNodes.Count; i++)
            if (ReferenceEquals(node, syntax.RawNodes[i]))
                return i;
        throw new InvalidOperationException("Semantic symbol declaration outside single-file syntax");
    }

    public int? Symbol(Symbol? symbol)
    {
        if (symbol is null)
            return null;
        if (symbols.TryGetValue(symbol, out int id))
            return id;
        id = result.Symbols.Count;
        symbols.Add(symbol, id);
        var value = new SymbolValue
        {
            Id = id,
            Flags = (uint)symbol.Flags,
            CheckFlags = (uint)symbol.CheckFlags,
            NameBase64 = Encoder.Bytes(symbol.Name),
            DeclarationsNull = symbol.Declarations is null
        };
        result.Symbols.Add(value);
        foreach (var declaration in symbol.Declarations ?? [])
            value.Declarations.Add(Node(declaration)!.Value);
        value.ValueDeclaration = Node(symbol.ValueDeclaration);
        value.Parent = Symbol(symbol.Parent);
        value.ExportSymbol = Symbol(symbol.ExportSymbol);
        value.Members = Table(symbol.Members);
        value.Exports = Table(symbol.Exports);
        return id;
    }

    public int? Table(SymbolTable? table)
    {
        if (table is null)
            return null;
        if (tables.TryGetValue(table, out int id))
            return id;
        id = result.Tables.Count;
        tables.Add(table, id);
        var value = new TableValue(id, []);
        result.Tables.Add(value);
        foreach (string key in table.Keys.Order(StringComparer.Ordinal))
            value.Entries.Add(new(Encoder.Bytes(key), Symbol(table[key])!.Value));
        return id;
    }

    public int? Type(CheckType? type)
    {
        if (type is null)
            return null;
        if (types.TryGetValue(type, out int id))
            return id;
        id = result.Types.Count;
        types.Add(type, id);
        rawTypes.Add(type);
        var value = new TypeValue
        {
            Id = id,
            Flags = (uint)type.Flags(),
            ObjectFlags = (uint)type.ObjectFlags(),
            DataType = type.DataType()
        };
        result.Types.Add(value);
        value.Text = checker.TypeToString(type);
        value.Symbol = Symbol(type.Symbol());
        if (type.Alias()is { } alias)
        {
            value.AliasSymbol = Symbol(alias.Symbol());
            foreach (var argument in alias.TypeArguments() ?? [])
                value.AliasTypeArguments.Add(Type(argument)!.Value);
        }

        switch (value.DataType)
        {
            case "IntrinsicType":
                value.IntrinsicNameBase64 = Encoder.Bytes(type.AsIntrinsicType().IntrinsicName());
                break;
            case "LiteralType":
                var literal = type.AsLiteralType();
                value.LiteralValue = literal.Value() switch
                {
                    string text => new()
                    {
                        Kind = "string",
                        BytesBase64 = Encoder.Bytes(text)
                    },
                    bool boolean => new()
                    {
                        Kind = "bool",
                        Value = boolean
                    },
                    JsNum.Number number => new()
                    {
                        Kind = "number",
                        BitsHex = BitConverter.DoubleToUInt64Bits(number.Value).ToString("x16", CultureInfo.InvariantCulture)
                    },
                    null => new()
                    {
                        Kind = "nil"
                    },
                    _ => throw new InvalidOperationException("Unknown scalar literal value"),
                };
                value.FreshType = Type(literal.FreshType());
                value.RegularType = Type(literal.RegularType());
                break;
            case "UnionType":
                foreach (var member in type.Types())
                    value.Types.Add(Type(member)!.Value);
                break;
            case "ObjectType" when objectObservations:
                break;
            default:
                throw new InvalidOperationException("Unsupported type payload in accepted semantic corpus: " + value.DataType);
        }

        return id;
    }
}

public static class Runner
{
    private static List<DiagnosticValue> Diagnostics(IEnumerable<Diagnostic>? diagnostics) => (diagnostics ?? []).Select(d => new DiagnosticValue { Code = d.Code(), Category = (int)d.Category(), Pos = d.Pos(), End = d.End(), Key = d.MessageKey(), ArgsNull = d.MessageArgs()is null, ArgsBase64 = (d.MessageArgs() ?? []).Select(Encoder.Bytes).ToList(), Text = d.String(), FileNameBase64 = d.File()is { } file ? Convert.ToBase64String(Encoding.UTF8.GetBytes(file.FileName())) : null, ReportsUnnecessary = d.ReportsUnnecessary(), ReportsDeprecated = d.ReportsDeprecated(), SkippedOnNoEmit = d.SkippedOnNoEmit(), SourceBase64 = Encoder.Bytes(d.Source()), Related = Diagnostics(d.RelatedInformation()), Chain = Diagnostics(d.MessageChain()), }).ToList();
    public static AdmissionResult Admission(Request request)
    {
        var file = TypeScript.Scanner.ParserValidation.Runner.Parse(request.ParserInput());
        try
        {
            Checker.Create(file).GetDiagnostics();
            return new(true, null, null, null, null);
        }
        catch (UnsupportedSemanticFeatureException error)
        {
            return new(false, error.Feature, (int)error.Kind, error.Pos, error.End);
        }
    }

    public static Result Run(Request request)
    {
        var parserRequest = request.ParserInput();
        var file = TypeScript.Scanner.ParserValidation.Runner.Parse(parserRequest);
        var encoder = new Encoder(file, request.ResolveJSDoc);
        var result = new Result
        {
            Parser = TypeScript.Scanner.ParserValidation.Runner.Encode(file, parserRequest, encoder)
        };
        var checker = Checker.Create(file);
        var graph = new Graph(checker, encoder, result, request.ObjectObservations);
        var fileDiagnostics = checker.GetDiagnostics();
        var ids = request.QueryNodeIds ?? encoder.RawNodes.Select((node, id) => (node, id)).Where(pair => Ast.IsIdentifier(pair.node) || Ast.IsLiteralExpression(pair.node) || pair.node.Kind is Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword).Select(pair => pair.id).ToList();
        var queried = new Dictionary<int, CheckType>();
        foreach (int id in ids)
        {
            if (id < 0 || id >= encoder.RawNodes.Count)
                throw new InvalidDataException("Invalid query node ID");
            var node = encoder.RawNodes[id];
            var type = checker.GetTypeAtLocation(node);
            queried.Add(id, type);
            result.Queries.Add(new(id, graph.Type(type)!.Value, graph.Symbol(checker.GetSymbolAtLocation(node))));
        }

        foreach (var pair in request.RelationPairs)
        {
            if (pair.Count != 2 || !queried.TryGetValue(pair[0], out var source) || !queried.TryGetValue(pair[1], out var target))
                throw new InvalidDataException("Relation endpoints must be query nodes");
            result.Relations.Add(new(pair[0], pair[1], checker.IsTypeAssignableTo(source, target)));
        }

        result.GlobalThisSymbol = graph.Symbol(checker.GlobalThisSymbol);
        result.Locals = graph.Table(file.Locals);
        result.Globals = graph.Table(checker.Globals);
        if (!ReferenceEquals(checker.Globals, checker.GlobalThisSymbol.Exports))
            throw new InvalidOperationException("Globals/globalThis exports identity differs");
        result.BindDiagnostics = Diagnostics(file.BindDiagnostics());
        result.CheckerDiagnostics = Diagnostics(fileDiagnostics);
        result.GlobalDiagnostics = Diagnostics(checker.GetGlobalDiagnostics());
        if (request.ObjectObservations)
            result.ObjectGraph = graph.Objects(request.WidenNodeIds, queried);
        return result;
    }
}
