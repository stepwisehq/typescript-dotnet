using System.Security.Cryptography;
using System.Text;
using TypeScript.Scanner;
using TypeScript.Scanner.ParserValidation;
using Encoder = TypeScript.Scanner.ParserValidation.Encoder;

namespace TypeScript.Checking.CollectionsValidation;
public static class Runner
{
    private static List<FullDiagnostic>? Diagnostics(IEnumerable<Diagnostic>? values, Dictionary<SourceFile, int> fileIds) => values?.Select(d => new FullDiagnostic { Code = d.Code(), Category = (int)d.Category(), Pos = d.Pos(), End = d.End(), Key = d.MessageKey(), ArgsBase64 = d.MessageArgs()?.Select(Encoder.Bytes).ToList(), Text = d.String(), FileIndex = d.File()is { } f ? fileIds.TryGetValue(f, out int index) ? index : throw new InvalidDataException("Diagnostic outside fixed files") : null, SourceBase64 = Encoder.Bytes(d.Source()), ReportsUnnecessary = d.ReportsUnnecessary(), ReportsDeprecated = d.ReportsDeprecated(), SkippedOnNoEmit = d.SkippedOnNoEmit(), Chain = Diagnostics(d.MessageChain(), fileIds), Related = Diagnostics(d.RelatedInformation(), fileIds) }).ToList();
    public static Result Run(Request request)
    {
        if (request.Profile != "Collections" || request.FileName != "/main.ts")
            throw new InvalidDataException("Expected fixed Collections user file");
        byte[] bytes = request.TextBytesBase64 is { } raw ? Convert.FromBase64String(raw) : Encoding.UTF8.GetBytes(request.Text);
        var file = Parser.ParseSourceFile(new SourceFileParseOptions("/main.ts", "/main.ts"), bytes, ScriptKind.TS);
        var checker = Checker.Create(file, CheckerProfile.Collections);
        var files = checker.SourceFiles;
        if (files.Count != 4 || !ReferenceEquals(files[3], file))
            throw new InvalidDataException("Fixed file order differs");
        var fileIds = new Dictionary<SourceFile, int>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < files.Count; i++)
            fileIds.Add(files[i], i);
        var userDiagnostics = checker.GetDiagnostics();
        var encoder = new Encoder(file, false);
        var result = new Result();
        for (int i = 0; i < files.Count; i++)
        {
            SourceFile f = files[i];
            int root = encoder.Add(f.AsNode());
            var opts = f.ParseOptions();
            result.Files.Add(new FileRecord { Index = i, FileName = f.FileName(), Sha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.Latin1.GetBytes(f.Text()))), RootNodeId = root, Declaration = f.IsDeclarationFile, ExternalModuleJSX = opts.ExternalModuleIndicatorOptions.JSX, ExternalModuleForce = opts.ExternalModuleIndicatorOptions.Force, SkipTypeChecking = TypeScript.Compiler.Program.SkipTypeChecking(f, checker.compilerOptions), IsBound = f.IsBound(), SymbolCount = f.SymbolCount, ParseDiagnostics = Diagnostics(f.Diagnostics(), fileIds), BindDiagnostics = Diagnostics(f.BindDiagnostics(), fileIds), ProgramSemanticDiagnostics = Diagnostics(TypeScript.Compiler.Program.GetSemanticDiagnosticsWithChecker(f, checker.compilerOptions, checker), fileIds) });
        }

        int parsedNodeCount = encoder.RawNodes.Count;
        var graph = new PassiveGraph(encoder, true);
        var ids = request.QueryNodeIds ?? encoder.RawNodes.Where(n => Ast.IsVariableDeclaration(n) && n.Name()is { Kind: Kind.Identifier } && ReferenceEquals(Ast.GetSourceFileOfNode(n), file)).Select(n => encoder.RawNodes.ToList().IndexOf(n.Name())).ToList();
        var queriedTypes = new List<Type>();
        foreach (int id in ids)
        {
            if (id < 0 || id >= encoder.RawNodes.Count || !ReferenceEquals(Ast.GetSourceFileOfNode(encoder.RawNodes[id]), file))
                throw new InvalidDataException("Query outside user syntax");
            Node n = encoder.RawNodes[id];
            var t = checker.GetTypeAtLocation(n);
            queriedTypes.Add(t);
            var s = checker.GetSymbolAtLocation(n);
            result.Queries.Add(new(id, checker.TypeToString(t), (uint)t.Flags(), (uint)t.ObjectFlags(), t.DataType(), graph.Symbol(s), ReferenceEquals(t, checker.GetTypeAtLocation(n)), ReferenceEquals(s, checker.GetSymbolAtLocation(n))));
        }

        result.Globals = graph.Table(checker.Globals);
        result.GlobalThisSymbol = graph.Symbol(checker.GlobalThisSymbol);
        result.Binding = graph.Encode(file);
        if (request.StoredObservations)
        {
            result.StoredGraph = new StoredGraph(checker, graph, encoder).Encode(queriedTypes);
            graph.CompleteNodes();
        }

        result.Nodes = encoder.Nodes;
        var localIds = new int[files.Count];
        for (int id = 0; id < encoder.RawNodes.Count; id++)
        {
            var root = encoder.RawNodes[id];
            var visited = new HashSet<Node>(ReferenceEqualityComparer.Instance);
            while (root.Parent is { } parent)
            {
                if (!visited.Add(root))
                    throw new InvalidDataException("Cyclic stored node parent chain");
                root = parent;
            }

            int? fileIndex = null, localIndex = null;
            if (root.Kind == Kind.SourceFile)
            {
                if (!fileIds.TryGetValue(root.AsSourceFile(), out int index))
                    throw new InvalidDataException("Stored node outside fixed files");
                fileIndex = index;
                if (id < parsedNodeCount)
                    localIndex = localIds[index]++;
            }
            else if (id < parsedNodeCount)
                throw new InvalidDataException("Parsed node lost file ownership");
            result.NodeOwners.Add(new(id, fileIndex, localIndex));
        }

        result.GlobalDiagnostics = Diagnostics(checker.GetGlobalDiagnostics(), fileIds);
        return result;
    }
}
