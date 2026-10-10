// Authored opt-in observations of genuine Program semantic APIs.
// No options, files or diagnostic text are changed by this observer.
using System.Text.Json.Nodes;
using TypeScript.Checking;
using TypeScript.Scanner;
using CompilerProgram = TypeScript.Compiler.Program;

namespace Program.Checks;

public static partial class ProgramRunner
{
    private static JsonObject ObserveSemantics(Observer observer, CompilerProgram program, JsonObject input)
    {
        Keys(input, "PerFile", "Repeat", "IncrementalFiles", "Queries");
        var result = new JsonObject
        {
            ["globalBefore"] = observer.Diagnostics(program.GetGlobalDiagnostics()),
            ["bulk"] = observer.Diagnostics(program.GetSemanticDiagnostics())
        };
        if (input["PerFile"]?.GetValue<bool>() == true)
        {
            JsonArray rows = [];
            foreach (var file in program.SourceFiles())
                rows.Add((JsonNode)new JsonObject { ["file"] = observer.File(file),
                    ["diagnostics"] = observer.Diagnostics(program.GetSemanticDiagnostics(default, file)) });
            result["perFile"] = rows;
        }
        if (input["IncrementalFiles"] is JsonArray names)
        {
            var files = names.Select(name => program.GetSourceFile(RootedFilePath.FromGoString(
                GoString.ToUtf8(name!.GetValue<string>()))) ?? throw new ArgumentException("Unknown incremental semantic fixture file")).ToList();
            var actual = program.GetSemanticDiagnosticsForIncremental(default, files);
            JsonArray rows = [];
            foreach (var file in files)
                rows.Add((JsonNode)new JsonObject { ["file"] = observer.File(file), ["diagnostics"] = observer.Diagnostics(actual[file]) });
            result["incremental"] = rows;
        }
        result["globalAfter"] = observer.Diagnostics(program.GetGlobalDiagnostics());
        if (input["Repeat"]?.GetValue<bool>() == true)
        {
            result["repeatBulk"] = observer.Diagnostics(program.GetSemanticDiagnostics());
            result["repeatGlobal"] = observer.Diagnostics(program.GetGlobalDiagnostics());
        }
        if (input["Queries"] is JsonArray queries)
        {
            JsonArray rows = [];
            foreach (var query in queries)
                rows.Add((JsonNode)ObserveSemanticQuery(observer, program, query!.AsObject()));
            result["queries"] = rows;
            result["globalAfterQueries"] = observer.Diagnostics(program.GetGlobalDiagnostics());
        }
        return result;
    }

    private static JsonObject ObserveSemanticQuery(Observer observer, CompilerProgram program, JsonObject query)
    {
        Keys(query, "File", "Identifier", "Operation", "Occurrence");
        int occurrence = query["Occurrence"]?.GetValue<int>() ?? 0;
        if (occurrence < 0) throw new ArgumentException("Negative semantic identifier occurrence");
        var file = program.GetSourceFile(RootedFilePath.FromGoString(Input(query, "File")))
            ?? throw new ArgumentException("Unknown semantic query file");
        string identifier = Input(query, "Identifier");
        string operation = query["Operation"]!.GetValue<string>();
        Node? selected = null;
        int current = 0;
        void Visit(Node node)
        {
            if (Ast.IsIdentifier(node) && node.Text() == identifier)
            {
                if (current == occurrence) selected = node;
                current++;
            }
            node.ForEachChild(child => { Visit(child); return false; });
        }
        Visit(file.AsNode());
        if (selected is null) throw new ArgumentException("Unknown semantic identifier occurrence");
        var (checker, done) = program.GetTypeChecker();
        try
        {
            JsonObject result = new() { ["file"] = observer.File(file), ["pos"] = selected.Pos(),
                ["end"] = selected.End(), ["kind"] = (int)selected.Kind, ["operation"] = operation };
            if (operation == "symbol")
            {
                var symbol = checker.GetSymbolAtLocation(selected);
                result["symbol"] = symbol is null ? null : new JsonObject
                    { ["nameBase64"] = Bytes(symbol.Name), ["flags"] = (long)symbol.Flags };
                return result;
            }
            var type = checker.GetTypeAtLocation(selected);
            if (operation is "declared" or "base")
                type = checker.GetDeclaredTypeOfSymbol(checker.GetSymbolAtLocation(selected)
                    ?? throw new ArgumentException("Declared-type query requires actual symbol"));
            string Display(TypeScript.Checking.Type actual) => Bytes(GoString.ToUtf8(checker.TypeToString(actual)));
            result["typeBase64"] = Display(type);
            switch (operation)
            {
                case "type": case "declared": break;
                case "properties":
                    var properties = checker.GetPropertiesOfType(type);
                    JsonArray? propertyRows = properties is null ? null : [];
                    if (properties is not null)
                        foreach (var property in properties)
                            propertyRows!.Add((JsonNode)new JsonObject { ["displayNameBase64"] = Bytes(GoString.ToUtf8(checker.SymbolToString(property))),
                                ["typeBase64"] = Display(checker.GetTypeOfSymbol(property)) });
                    result["properties"] = propertyRows;
                    break;
                case "base":
                    var bases = checker.GetBaseTypes(type);
                    JsonArray? baseRows = bases is null ? null : [];
                    if (bases is not null)
                        foreach (var basis in bases) baseRows!.Add((JsonNode)JsonValue.Create(Display(basis))!);
                    result["bases"] = baseRows;
                    break;
                case "construct":
                    var signatures = checker.GetSignaturesOfType(type, SignatureKind.Construct);
                    JsonArray? signatureRows = signatures is null ? null : [];
                    if (signatures is not null)
                        foreach (var signature in signatures)
                        {
                            var parameters = signature.Parameters();
                            JsonArray? parameterRows = parameters is null ? null : [];
                            if (parameters is not null)
                                foreach (var parameter in parameters)
                                    parameterRows!.Add((JsonNode)new JsonObject { ["nameBase64"] = Bytes(parameter.Name),
                                        ["typeBase64"] = Display(checker.GetTypeOfSymbol(parameter)) });
                            signatureRows!.Add((JsonNode)new JsonObject { ["flags"] = (int)signature.Flags(),
                                ["minArgumentCount"] = signature.MinArgumentCount(), ["parameters"] = parameterRows,
                                ["returnTypeBase64"] = Display(checker.GetReturnTypeOfSignature(signature)) });
                        }
                    result["constructSignatures"] = signatureRows;
                    break;
                default: throw new ArgumentException("Unknown semantic query operation");
            }
            return result;
        }
        finally { done(); }
    }
}
