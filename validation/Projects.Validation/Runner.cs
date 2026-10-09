// Authored disk-project projection of actual Program/checker relationships.
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TypeScript.Compiler;
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.Checking.CollectionsValidation;
using Encoder = TypeScript.Scanner.ParserValidation.Encoder;

namespace TypeScript.Checking.ProjectsValidation;
public static class Runner
{
    public static Result Run(Request request)
    {
        string root = Path.Combine(Path.GetTempPath(), "ts-project-proof-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            foreach (var(relative, text)in request.Files)
            {
                if (relative.StartsWith('/') || relative.Split('/').Any(p => p is ".." or "") || relative.Contains('\\'))
                    throw new InvalidDataException("Noncanonical fixture path");
                string path = Path.Combine(root, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, text, new UTF8Encoding(false));
            }

            File.WriteAllText(Path.Combine(root, "tsconfig.json"), request.Config, new UTF8Encoding(false));
            return Observe(Project.Load(Path.Combine(root, "tsconfig.json")), request);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static Result Observe(Project project, Request request)
    {
        string PathProjection(string path)
        {
            if (path == "")
                return "";
            if (path == project.RootDirectory)
                return "/project";
            if (path.StartsWith(project.RootDirectory + "/", StringComparison.Ordinal))
                return "/project" + path[project.RootDirectory.Length..];
            if (project.LibraryFiles.Any(f => f.FileName() == path))
                return path;
            throw new InvalidDataException("Path outside owned project/library environment: " + path);
        }

        var checker = project.Checker;
        var files = project.SourceFiles.ToList();
        var primary = files.Last();
        var fileIds = new Dictionary<SourceFile, int>(ReferenceEqualityComparer.Instance);
        var perFile = new Dictionary<SourceFile, List<Diagnostic>?>(ReferenceEqualityComparer.Instance);
        // Same original user-file checking order as Program.GetSemanticDiagnostics(nil).
        foreach (var file in files)
            perFile[file] = TypeScript.Compiler.Program.GetSemanticDiagnosticsWithChecker(file, project.Options, checker);
        var globalDiagnostics = checker.GetGlobalDiagnostics();
        var encoder = new Encoder(primary, false);
        for (int i = 0; i < files.Count; i++)
        {
            fileIds.Add(files[i], i);
            encoder.Add(files[i].AsNode());
        }

        SourceFile config = project.ConfigSourceFile;
        fileIds.Add(config, files.Count);
        encoder.Add(config.AsNode());
        int NodeId(Node node) => encoder.RawNodes.ToList().IndexOf(node)is int id && id >= 0 ? id : throw new InvalidDataException("Unrooted node");
        BoundName? NameProjection(string name, Symbol symbol)
        {
            foreach (var declaration in symbol.Declarations ?? [])
                if (declaration.Kind == Kind.SourceFile && declaration.AsSourceFile().ExternalModuleIndicator is not null && (ReferenceEquals(declaration.Symbol(), symbol) || ReferenceEquals(checker.VerificationProjectExportType(symbol).Target, declaration.Symbol())))
                {
                    string filename = declaration.AsSourceFile().FileName();
                    string withoutExtension = TsPath.RemoveFileExtension(filename);
                    string expected = "\"" + GoString.ToUtf8(withoutExtension) + "\"";
                    if (name != expected)
                        throw new InvalidDataException("Module generated name differs from source owner");
                    string projected = "\"" + GoString.ToUtf8(TsPath.RemoveFileExtension(PathProjection(filename))) + "\"";
                    return new BoundName
                    {
                        Kind = "module",
                        BytesBase64 = Encoder.Bytes(projected),
                        OwnerNodeId = encoder.Add(declaration)
                    };
                }

            return null;
        }

        var graph = new PassiveGraph(encoder, true, NameProjection);
        var semantics = new SemanticGraph(checker, graph);
        var result = new Result
        {
            DeclaredRoots = project.RootFileNames.Select(PathProjection).ToList(),
            TypesOption = project.Options.Types,
            Options = new()
            {
                ["Target"] = (int)project.Options.Target,
                ["Module"] = (int)project.Options.Module,
                ["ModuleResolution"] = (int)project.Options.ModuleResolution,
                ["NoLib"] = (int)project.Options.NoLib,
                ["NoResolve"] = (int)project.Options.NoResolve,
                ["SkipLibCheck"] = (int)project.Options.SkipLibCheck,
                ["NoEmit"] = (int)project.Options.NoEmit,
                ["AllowImportingTsExtensions"] = (int)project.Options.AllowImportingTsExtensions,
                ["NoUncheckedSideEffectImports"] = (int)project.Options.NoUncheckedSideEffectImports,
                ["ResolveJsonModule"] = (int)project.Options.ResolveJsonModule,
                ["AllowJs"] = (int)project.Options.AllowJs,
                ["StrictNullChecks"] = (int)project.Options.StrictNullChecks,
                ["StrictFunctionTypes"] = (int)project.Options.StrictFunctionTypes,
                ["NoImplicitAny"] = (int)project.Options.NoImplicitAny,
                ["NoImplicitThis"] = (int)project.Options.NoImplicitThis,
                ["StrictBindCallApply"] = (int)project.Options.StrictBindCallApply,
                ["NoImplicitReturns"] = (int)project.Options.NoImplicitReturns,
                ["ExactOptionalPropertyTypes"] = (int)project.Options.ExactOptionalPropertyTypes,
                ["NoUncheckedIndexedAccess"] = (int)project.Options.NoUncheckedIndexedAccess,
                ["StrictPropertyInitialization"] = (int)project.Options.StrictPropertyInitialization,
                ["StrictBuiltinIteratorReturn"] = (int)project.Options.StrictBuiltinIteratorReturn,
                ["UseUnknownInCatchVariables"] = (int)project.Options.UseUnknownInCatchVariables,
                ["ForceConsistentCasingInFileNames"] = (int)project.Options.ForceConsistentCasingInFileNames,
                ["DownlevelIteration"] = (int)project.Options.DownlevelIteration,
            }
        };
        List<Node> selected;
        if (request.Queries is not null)
            selected = request.Queries.Select(q => encoder.RawNodes.Single(n => Ast.GetSourceFileOfNode(n)is { } f && PathProjection(f.FileName()) == "/project/" + q.File && n.Pos() == q.Pos && (int)n.Kind == q.Kind && (q.End is null || n.End() == q.End))).ToList();
        else
        {
            selected = [];
            foreach (var n in encoder.RawNodes)
            {
                var f = Ast.GetSourceFileOfNode(n);
                if (f is null || f.IsDeclarationFile || ReferenceEquals(f, config))
                    continue;
                if ((Ast.IsVariableDeclaration(n) || Ast.IsImportSpecifier(n) || Ast.IsNamespaceImport(n) || Ast.IsImportClause(n)) && n.Name()is { Kind: Kind.Identifier } name)
                    selected.Add(name);
                if (Ast.IsCallExpression(n))
                    selected.Add(n);
            }
        }

        foreach (var n in selected)
        {
            var type = checker.GetTypeAtLocation(n);
            var symbol = checker.GetSymbolAtLocation(n);
            var signature = Ast.IsCallExpression(n) ? checker.GetResolvedSignature(n) : null;
            result.Queries.Add(new(NodeId(n), graph.Symbol(symbol), semantics.Type(type)!.Value, ProjectDisplay(checker.TypeToString(type), project, PathProjection), semantics.Signature(signature), ReferenceEquals(type, checker.GetTypeAtLocation(n)), ReferenceEquals(symbol, checker.GetSymbolAtLocation(n)), Ast.IsCallExpression(n) ? ReferenceEquals(signature, checker.GetResolvedSignature(n)) : null));
        }

        semantics.Drain();
        foreach (var f in files)
        {
            if (f.ExternalModuleIndicator is null)
                continue;
            var symbol = f.AsNode().Symbol();
            var exports = checker.GetExportsOfModule(symbol);
            var ordered = (exports ?? []).OrderBy(s => s.Name, StringComparer.Ordinal).ToList();
            var second = checker.GetExportsOfModule(symbol);
            var state = checker.VerificationProjectModule(symbol);
            var exportRecords = ordered.Select(s => new ExportRecord(Encoder.Bytes(s.Name), graph.Symbol(s)!.Value)).ToList();
            result.Modules.Add(new(graph.Symbol(symbol)!.Value, exportRecords, (exports?.Count ?? 0) == (second?.Count ?? 0) && (exports ?? []).All(s => (second ?? []).Contains(s, ReferenceEqualityComparer.Instance)), state.Present, graph.Table(state.Exports), state.TypeOnlyStars is null, state.TypeOnlyStars?.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new StarRecord(Encoder.Bytes(p.Key), graph.AddNode(p.Value)!.Value)).ToList() ?? [], state.Checked));
        }

        result.Globals = graph.Table(checker.Globals);
        result.GlobalThisSymbol = graph.Symbol(checker.GlobalThisSymbol);
        result.Binding = graph.Encode(primary);
        for (int i = 0; i < graph.RawSymbols.Count; i++)
        {
            var symbol = graph.RawSymbols[i];
            if ((symbol.Flags & SymbolFlags.Alias) == 0)
                continue;
            var immediate = checker.GetImmediateAliasedSymbol(symbol);
            var target = checker.GetAliasedSymbol(symbol);
            var only = checker.GetTypeOnlyAliasDeclaration(symbol);
            var state = checker.VerificationProjectAlias(symbol);
            result.Aliases.Add(new(i, graph.Symbol(immediate), graph.Symbol(target), graph.AddNode(only), ReferenceEquals(target, checker.GetAliasedSymbol(symbol)), state.Present, graph.Symbol(state.ImmediateTarget), graph.Symbol(state.AliasTarget), state.Referenced, graph.AddNode(state.TypeOnlyDeclaration)));
        }

        foreach (var symbol in graph.RawSymbols)
        {
            var state = checker.VerificationProjectExportType(symbol);
            if (state.Present)
                result.ExportTypes.Add(new(graph.ExistingSymbolId(symbol), true, graph.Symbol(state.Target), graph.AddNode(state.OriginatingImport)));
        }

        graph.CompleteNodes();
        foreach (var(name, keys)in checker.VerificationProjectStores())
        {
            var known = graph.RawSymbols.ToHashSet(ReferenceEqualityComparer.Instance);
            var missing = (keys ?? []).Where(s => !known.Contains(s)).ToList();
            if (missing.Count != 0 && name != "exportTypeLinks")
                throw new InvalidDataException("Unrooted actual store owner: " + name);
            var clones = new List<UnrootedExportClone>();
            foreach (var symbol in missing)
            {
                var state = checker.VerificationProjectExportType(symbol);
                if (!state.Present || state.Target is null || symbol.Declarations is not { Count: 1 } declarations || declarations[0].Kind != Kind.SourceFile || !ReferenceEquals(declarations[0].Symbol(), state.Target) || symbol.Name != state.Target.Name || symbol.Flags != (state.Target.Flags | SymbolFlags.Transient) || symbol.CheckFlags != 0 || !ReferenceEquals(symbol.Parent, state.Target.Parent) || !ReferenceEquals(symbol.ValueDeclaration, state.Target.ValueDeclaration))
                    throw new InvalidDataException("Unknown unrooted export-type clone producer");
                static bool Same(SymbolTable? a, SymbolTable? b) => (a is null) == (b is null) && (a is null || a.Count == b!.Count && a.All(p => b.TryGetValue(p.Key, out var value) && ReferenceEquals(value, p.Value)));
                if (!Same(symbol.Members, state.Target.Members) || !Same(symbol.Exports, state.Target.Exports))
                    throw new InvalidDataException("Unrooted export clone table differs from actual target");
                var projected = NameProjection(symbol.Name, symbol) ?? throw new InvalidDataException("Unrooted clone name has no owned module");
                clones.Add(new(graph.ExistingSymbolId(state.Target), state.OriginatingImport is null ? null : NodeId(state.OriginatingImport), NodeId(declarations[0]), (uint)symbol.Flags, (uint)symbol.CheckFlags, projected.BytesBase64!, 1));
            }

            result.UnrootedExportClones.AddRange(clones.GroupBy(c => c with { Count = 0 }).Select(g => g.Key with { Count = g.Count() }).OrderBy(c => c.OwnerNode).ThenBy(c => c.OriginatingImport).ThenBy(c => c.Target).ThenBy(c => c.NameBase64, StringComparer.Ordinal));
            result.Inventories.Add(new(name, keys is null, keys?.Count ?? 0, (keys ?? []).Where(s => known.Contains(s)).Select(graph.ExistingSymbolId).Order().ToList(), missing.Count));
        }

        foreach (var f in files.Append(config))
        {
            int index = fileIds[f];
            result.Files.Add(new FileRecord { Index = index, FileName = PathProjection(f.FileName()), Sha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.Latin1.GetBytes(f.Text()))), RootNodeId = NodeId(f.AsNode()), ProgramFile = !ReferenceEquals(f, config), Declaration = f.IsDeclarationFile, DefaultLibrary = !ReferenceEquals(f, config) && project.IsSourceFileDefaultLibrary(f), ExternalModule = f.ExternalModuleIndicator is not null, SkipTypeChecking = !ReferenceEquals(f, config) && TypeScript.Compiler.Program.SkipTypeChecking(f, project.Options), IsBound = f.IsBound(), SymbolCount = f.SymbolCount, ParseDiagnostics = Diagnostics(f.Diagnostics(), fileIds, project), BindDiagnostics = Diagnostics(f.BindDiagnostics(), fileIds, project), SemanticDiagnostics = perFile.TryGetValue(f, out var ds) ? Diagnostics(ds, fileIds, project) : null });
        }

        foreach (var edge in project.ResolutionEdges)
        {
            var r = edge.Resolution;
            result.Resolutions.Add(new(fileIds[edge.ImportingFile], NodeId(edge.ModuleSpecifier), Encoder.Bytes(edge.ModuleSpecifier.Text()), (int)edge.Mode, true, r.IsResolved(), PathProjection(r.ResolvedFileName.ToUnicode()), PathProjection(PathByteText.ToUnicode(r.ResolvedPath.AsString())), PathProjection(r.OriginalPath.ToUnicode()), r.Extension, r.ResolvedUsingTsExtension, r.ResolvedUsingExtraExtensions, r.IsExternalLibraryImport, r.IsCustomResolution, r.AlternateResult.ToUnicode(), ReferenceEquals(r, project.GetResolvedModule(edge.ImportingFile, edge.ModuleReference, edge.Mode))));
        }

        result.Nodes = encoder.Nodes;
        int[] local = new int[fileIds.Count];
        for (int i = 0; i < encoder.RawNodes.Count; i++)
        {
            var f = Ast.GetSourceFileOfNode(encoder.RawNodes[i]);
            if (f is null || !fileIds.TryGetValue(f, out int index))
                throw new InvalidDataException("Unknown generated node owner");
            result.NodeOwners.Add(new(i, index, local[index]++));
        }

        result.Types = semantics.Types;
        result.Signatures = semantics.Signatures;
        result.GlobalDiagnostics = Diagnostics(globalDiagnostics, fileIds, project);
        result.ProgramDiagnostics = project.ProgramDiagnostics.Count == 0 ? null : Diagnostics(project.ProgramDiagnostics, fileIds, project);
        result.ConfigDiagnostics = null;
        result.ExitCode = (globalDiagnostics?.Count ?? 0) + project.ProgramDiagnostics.Count + perFile.Values.Sum(ds => ds?.Count ?? 0) > 0 ? 1 : 0;
        return result;
    }

    // Only owned import-type tokens outside quoted literals and identifier prefixes
    // relocate. Public Unicode and internal Go-byte strings use the same scanner.
    private static string ProjectDisplay(string text, Project project, Func<string, string> projectPath, bool rawBytes = false)
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        string Token(string path) => "import(\"" + GoString.ToString(TypeScript.DiagnosticPrinting.Printer.EscapeString(GoString.ToUtf8(TsPath.RemoveFileExtension(path)), '"')) + "\")";
        foreach (var file in project.UserFiles.Where(f => f.ExternalModuleIndicator is not null))
            tokens.Add(Token(file.FileName()), Token(projectPath(file.FileName())));
        string projected = RelocateRenderedImportTypes(rawBytes ? GoString.ToString(text) : text, tokens);
        return rawBytes ? GoString.ToUtf8(projected) : projected;
    }

    private static string RelocateRenderedImportTypes(string text, IReadOnlyDictionary<string, string> tokens)
    {
        var result = new StringBuilder(text.Length);
        int position = 0;
        bool IdentifierBefore(int index)
        {
            if (index == 0)
                return false;
            int value = text[index - 1];
            if (char.IsLowSurrogate(text[index - 1]) && index > 1 && char.IsHighSurrogate(text[index - 2]))
                value = char.ConvertToUtf32(text[index - 2], text[index - 1]);
            return TypeScript.Scanner.Scanner.IsIdentifierPart(value);
        }

        while (position < text.Length)
        {
            char ch = text[position];
            if (ch is '"' or '\'' or '`')
            {
                char quote = ch;
                result.Append(ch);
                position++;
                while (position < text.Length)
                {
                    ch = text[position++];
                    result.Append(ch);
                    if (ch == '\\' && position < text.Length)
                        result.Append(text[position++]);
                    else if (ch == quote)
                        break;
                }

                continue;
            }

            bool replaced = false;
            if (!IdentifierBefore(position) && text.AsSpan(position).StartsWith("import(", StringComparison.Ordinal))
                foreach (var token in tokens)
                    if (text.AsSpan(position).StartsWith(token.Key, StringComparison.Ordinal))
                    {
                        result.Append(token.Value);
                        position += token.Key.Length;
                        replaced = true;
                        break;
                    }

            if (!replaced)
                result.Append(text[position++]);
        }

        return result.ToString();
    }

    // Authored observation controls, shared by xUnit and native. They do not
    // query a checker or claim original compiler assertions.
    public static void RunProjectionControl()
    {
        const string token = "import(\"/host/é\")";
        const string replacement = "import(\"/project/é\")";
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [token] = replacement
        };
        var cases = new (string Input, string Expected)[]
        {
            ("typeof " + token, "typeof " + replacement),
            ("'" + token + "'", "'" + token + "'"),
            ("`" + token + "`", "`" + token + "`"),
            ("\"escaped \\\" " + token + "\"", "\"escaped \\\" " + token + "\""),
            ("my" + token, "my" + token),
            ("é" + token, "é" + token),
            ("a\u0301" + token, "a\u0301" + token),
            ("a\u203f" + token, "a\u203f" + token),
            ("\U00010400" + token, "\U00010400" + token),
            ("'\"/host/é\"' | typeof " + token, "'\"/host/é\"' | typeof " + replacement),
            ("typeof import(\"/host/unowned\")", "typeof import(\"/host/unowned\")")
        };
        foreach (var item in cases)
        {
            if (RelocateRenderedImportTypes(item.Input, tokens) != item.Expected)
                throw new InvalidOperationException("Rendered import-type path role changed: " + item.Input);
            string bytes = GoString.ToUtf8(item.Input);
            if (GoString.ToUtf8(RelocateRenderedImportTypes(GoString.ToString(bytes), tokens)) != GoString.ToUtf8(item.Expected))
                throw new InvalidOperationException("Raw-byte rendered import path role changed");
        }
    }

    // Exact relocation of filename-role diagnostic arguments only. Stored source
    // diagnostics remain untouched; the private clone uses original localization.
    private static List<FullDiagnostic>? Diagnostics(IEnumerable<Diagnostic>? values, Dictionary<SourceFile, int> files, Project project) => values?.Select(original =>
    {
        var d = original.Clone();
        int[] positions = d.Code() switch
        {
            6053 or 2306 => [0],
            1149 or 1261 => [0, 1],
            1409 => [1],
            1393 => [1],
            _ => []
        };
        if (d.MessageArgs()is { } args)
        {
            var projected = args.ToList();
            foreach (int argumentIndex in positions)
            {
                if (argumentIndex >= args.Count)
                    continue;
                string prefix = GoString.ToUtf8(project.RootDirectory) + "/";
                if (args[argumentIndex].StartsWith(prefix, StringComparison.Ordinal))
                    projected[argumentIndex] = "/project/" + args[argumentIndex][prefix.Length..];
            }

            if (d.Code() == 2757 && projected.Count > 0)
                projected[0] = ProjectDisplay(projected[0], project, path => "/project/" + path[(project.RootDirectory.Length + 1)..], rawBytes: true);
            if (d.Code()is 1192 or 2613 && projected.Count > 0)
                foreach (var file in project.UserFiles.Where(f => f.ExternalModuleIndicator is not null))
                {
                    string moduleName = "\"" + TsPath.RemoveFileExtension(GoString.ToUtf8(file.FileName())) + "\"";
                    if (projected[0] == moduleName)
                        projected[0] = "\"" + TsPath.RemoveFileExtension(GoString.ToUtf8("/project/" + file.FileName()[(project.RootDirectory.Length + 1)..])) + "\"";
                }

            d.messageArgs = projected;
        }

        return new FullDiagnostic
        {
            Code = d.Code(),
            Category = (int)d.Category(),
            Pos = d.Pos(),
            End = d.End(),
            Key = d.MessageKey(),
            ArgsBase64 = d.MessageArgs()?.Select(Encoder.Bytes).ToList(),
            Text = d.String(),
            FileIndex = d.File()is { } f ? files.TryGetValue(f, out int index) ? index : throw new InvalidDataException("Diagnostic outside project/config sources") : null,
            SourceBase64 = Encoder.Bytes(d.Source()),
            ReportsUnnecessary = d.ReportsUnnecessary(),
            ReportsDeprecated = d.ReportsDeprecated(),
            SkippedOnNoEmit = d.SkippedOnNoEmit(),
            Chain = Diagnostics(d.MessageChain(), files, project),
            Related = Diagnostics(d.RelatedInformation(), files, project)
        };
    }).ToList();
    private sealed class SemanticGraph(Checker checker, PassiveGraph graph)
    {
        private readonly Dictionary<Type, int> types = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<Signature, int> signatures = new(ReferenceEqualityComparer.Instance);
        private readonly List<Type> tq = [];
        private readonly List<Signature> sq = [];
        public List<TypeRecord> Types { get; } = [];
        public List<SignatureRecord> Signatures { get; } = [];

        public int? Type(Type? type)
        {
            if (type is null)
                return null;
            if (types.TryGetValue(type, out int id))
                return id;
            if (type.checker is not null && !ReferenceEquals(type.checker, checker))
                throw new InvalidDataException("Foreign project type owner");
            id = tq.Count;
            types.Add(type, id);
            tq.Add(type);
            Types.Add(null!);
            return id;
        }

        public int? Signature(Signature? signature)
        {
            if (signature is null)
                return null;
            if (signatures.TryGetValue(signature, out int id))
                return id;
            id = sq.Count;
            signatures.Add(signature, id);
            sq.Add(signature);
            Signatures.Add(null!);
            return id;
        }

        private List<int>? Ts(List<Type>? values) => values?.Select(t => Type(t)!.Value).ToList();
        public void Drain()
        {
            int ti = 0, si = 0;
            while (ti < tq.Count || si < sq.Count)
            {
                while (ti < tq.Count)
                {
                    var t = tq[ti];
                    var d = t.AsStructuredType();
                    var row = new TypeRecord
                    {
                        Id = ti,
                        DataType = t.DataType(),
                        Flags = (uint)t.Flags(),
                        ObjectFlags = (uint)t.ObjectFlags(),
                        Symbol = graph.Symbol(t.Symbol()),
                        Target = Type(t.AsObjectType()?.target),
                        Arguments = Ts(t.AsTypeReference()?.resolvedTypeArguments),
                        Constituents = (t.Flags() & TypeFlags.UnionOrIntersection) != 0 ? Ts(t.AsUnionOrIntersectionType().types) : null,
                        Members = graph.Table(d?.members),
                        Properties = d?.properties?.Select(s => graph.Symbol(s)!.Value).ToList(),
                        CallSignatureCount = d?.callSignatureCount ?? 0
                    };
                    if ((t.Flags() & TypeFlags.TypeParameter) != 0)
                    {
                        var p = t.AsTypeParameter();
                        row.Target = Type(p.target);
                        row.Constraint = Type(p.constraint);
                        row.Default = Type(p.resolvedDefaultType);
                    }

                    if ((t.Flags() & TypeFlags.Index) != 0)
                        row.Target = Type(t.AsIndexType().target);
                    if (t.Alias()is { } alias)
                    {
                        row.AliasSymbol = graph.Symbol(alias.Symbol());
                        row.AliasArguments = Ts(alias.TypeArguments());
                    }

                    row.Signatures = d?.signatures?.Select(s => Signature(s)!.Value).ToList();
                    if (row.DataType == "IntrinsicType")
                        row.IntrinsicNameBase64 = Encoder.Bytes(t.AsIntrinsicType().IntrinsicName());
                    if (row.DataType == "LiteralType")
                        switch (t.AsLiteralType().Value())
                        {
                            case string value:
                                row.LiteralKind = "bytes";
                                row.LiteralValue = Encoder.Bytes(value);
                                break;
                            case JsNum.Number value:
                                row.LiteralKind = "number";
                                row.LiteralValue = BitConverter.DoubleToUInt64Bits(value.Value).ToString("x16", CultureInfo.InvariantCulture);
                                break;
                            case bool value:
                                row.LiteralKind = "bool";
                                row.LiteralValue = value ? "true" : "false";
                                break;
                            default:
                                throw new InvalidDataException("Unknown selected literal scalar");
                        }

                    Types[ti++] = row;
                }

                while (si < sq.Count)
                {
                    var s = sq[si];
                    Signatures[si] = new SignatureRecord
                    {
                        Id = si,
                        Flags = (uint)s.Flags(),
                        MinArgumentCount = s.MinArgumentCount(),
                        ResolvedMinArgumentCount = s.resolvedMinArgumentCount,
                        Declaration = graph.AddNode(s.Declaration()),
                        TypeParameters = Ts(s.TypeParameters()),
                        Parameters = s.Parameters()?.Select(s => graph.Symbol(s)!.Value).ToList(),
                        ThisParameter = graph.Symbol(s.ThisParameter()),
                        ReturnType = Type(checker.GetReturnTypeOfSignature(s)),
                        Target = Signature(s.Target())
                    };
                    si++;
                }
            }
        }
    }
}
