// Authored behavior controls for semantic emit prerequisites, shared by xUnit and Native AOT.
#nullable disable
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.DiagnosticPrinting;
using TypeScript.Transformers;
namespace Emit.Checks;
public static class ResolverChecks
{
    public static readonly string[] Names = ["CopyOnWriteScopes", "ReferenceResolverDeclarations", "TrackerForwarding", "TransformerChain", "ProgramDeclarationProjection"];
    public static void Run(string name)
    {
        switch (name)
        {
            case "CopyOnWriteScopes": CopyOnWriteScopes(); break;
            case "ReferenceResolverDeclarations": ReferenceResolverDeclarations(); break;
            case "TrackerForwarding": TrackerForwarding(); break;
            case "TransformerChain": TransformerChain(); break;
            case "ProgramDeclarationProjection": ProgramDeclarationProjection(); break;
            default: throw new ArgumentException(name);
        }
    }
    public static int RunAll() { foreach (var name in Names) Run(name); return Names.Length; }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void CopyOnWriteScopes()
    {
        var map = new CopyOnWriteMap<string, int>(StringComparer.OrdinalIgnoreCase);
        Check(!map.Has("absent") && !map.TryGetValue("absent", out _), "zero map reads absent");
        var restoreEmpty = map.EnterScope(); map.Set("Alpha", 1); restoreEmpty();
        Check(!map.Has("alpha"), "empty parent restored after first write");
        map.Set("Alpha", 2);
        var restoreOuter = map.EnterScope();
        Check(map.TryGetValue("ALPHA", out var inherited) && inherited == 2, "scope reads inherit comparer");
        var restoreReadOnly = map.EnterScope(); restoreReadOnly();
        map.Set("alpha", 3); map.Set("Beta", 4);
        var restoreInner = map.EnterScope(); map.Set("ALPHA", 5); restoreInner();
        Check(map.TryGetValue("ALPHA", out var outer) && outer == 3 && map.Has("beta"), "nested write restores outer scope");
        restoreOuter();
        Check(map.TryGetValue("alpha", out var root) && root == 2 && !map.Has("beta"), "root backing survives nested scope writes");
        map.Set("gamma", 6);
        Check(map.Has("GAMMA"), "restored owner remains mutable with comparer");
        var set = new CopyOnWriteSet<string>(StringComparer.OrdinalIgnoreCase); set.Add("Root");
        var restoreSet = set.EnterScope(); set.Add("Child"); restoreSet();
        Check(set.Has("ROOT") && !set.Has("child"), "set scope write isolation");
    }
    private static void ReferenceResolverDeclarations()
    {
        var file = Parser.ParseSourceFile(new SourceFileParseOptions("/main.ts", "/main.ts"), "export {}; let value = 1; value;", ScriptKind.TS);
        Binder.BindSourceFile(file);
        Node declaration = null, reference = null;
        Visitor visit = null;
        visit = node =>
        {
            if (node.Kind == Kind.VariableDeclaration) declaration = node;
            if (node.Kind == Kind.ExpressionStatement) reference = node.Expression();
            node.ForEachChild(visit); return false;
        };
        file.ForEachChild(visit);
        var globalFile = Parser.ParseSourceFile(new SourceFileParseOptions("/global.ts", "/global.ts"), "let value = 1; value;", ScriptKind.TS);
        Binder.BindSourceFile(globalFile);
        var globalReference = globalFile.Statements.Nodes[^1].Expression();
        var resolver = new ReferenceResolver(new CompilerOptions(), new ReferenceResolverHooks());
        Check(resolver.GetReferencedValueDeclaration(globalReference) == null && resolver.GetReferencedValueDeclarations(globalReference).IsNil, "global script fallback has no supplied globals as pinned Go");
        Check(resolver.GetReferencedValueDeclaration(reference) == declaration, "genuine binder fallback resolves value declaration");
        var declarations = resolver.GetReferencedValueDeclarations(reference);
        Check(declarations.Count == 1 && declarations[0] == declaration, "value declaration order");
        Check(resolver.GetReferencedImportDeclaration(reference) == null, "local value has no import alias");
        var hooked = new ReferenceResolver(new CompilerOptions(), new ReferenceResolverHooks { GetResolvedSymbol = _ => declaration.Symbol() });
        Check(hooked.GetReferencedValueDeclaration(reference) == declaration, "resolved symbol hook preserves declaration identity");
    }
    private sealed class Tracker : TypeScript.NodeBuilding.SymbolTracker
    {
        internal readonly List<string> events = new();
        public bool TrackSymbol(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning) { events.Add("symbol"); return true; }
        public void ReportInaccessibleThisError() => events.Add("this");
        public void ReportPrivateInBaseOfClassExpression(string propertyName) => events.Add("private:" + propertyName);
        public void ReportInaccessibleUniqueSymbolError() => events.Add("unique");
        public void ReportCyclicStructureError() => events.Add("cycle");
        public void ReportLikelyUnsafeImportRequiredError(string specifier, string symbolName) => events.Add("import:" + specifier + ":" + symbolName);
        public void ReportTruncationError() => events.Add("truncate");
        public void ReportNonlocalAugmentation(SourceFile containingFile, Symbol parentSymbol, Symbol augmentingSymbol) => events.Add("augment");
        public void ReportNonSerializableProperty(string propertyName) => events.Add("property:" + propertyName);
        public void ReportInferenceFallback(Node node) => events.Add("fallback");
        public void PushErrorFallbackNode(Node node) => events.Add("push");
        public void PopErrorFallbackNode() => events.Add("pop");
    }
    private static void TrackerForwarding()
    {
        var context = new NodeBuilderImpl.NodeBuilderContext(); var target = new Tracker();
        var tracker = SymbolTrackerImpl.NewSymbolTrackerImpl(context, target);
        var nested = SymbolTrackerImpl.NewSymbolTrackerImpl(context, tracker);
        nested.PushErrorFallbackNode(null); nested.ReportInferenceFallback(null); nested.PopErrorFallbackNode();
        Check(!context.reportedDiagnostic, "fallback stack is not a reported diagnostic");
        nested.ReportPrivateInBaseOfClassExpression("hidden"); nested.ReportNonlocalAugmentation(null, null, null);
        Check(context.reportedDiagnostic && target.events.SequenceEqual(["push", "fallback", "pop", "private:hidden", "augment"]), "complete tracker forwards once through unwrapped owner");
        var symbol = new Symbol { Flags = SymbolFlags.FunctionScopedVariable, Name = "value" };
        Check(nested.TrackSymbol(symbol, null, SymbolFlags.Value), "tracker reported accessibility error propagates");
        Check(context.trackedSymbols.Count == 0, "reported error skips late-symbol recording");
    }
    private static void TransformerChain()
    {
        var context = new EmitContext(); var file = context.Factory.NewSourceFile(new SourceFileParseOptions("/chain.ts", "/chain.ts"), "", context.Factory.NewNodeList(Slice<Node>.Make(0)), context.Factory.NewToken(Kind.EndOfFile)).AsSourceFile();
        var calls = new List<int>();
        TransformerFactory first = options => new Transformer().NewTransformer(node => { calls.Add(1); return node; }, options.Context);
        TransformerFactory second = options => new Transformer().NewTransformer(node => { calls.Add(2); return node; }, options.Context);
        TransformerFactory absent = _ => null;
        var options = new TransformOptions { Context = context };
        var chain = Transformers.Chain(first, absent, second)(options);
        Check(chain.TransformSourceFile(file) == file && calls.SequenceEqual([1, 2]), "whole source transforms run left to right and omit nil instances");
        Check(Transformers.Chain(absent, absent)(options) == null, "all absent components yield absent chain");
        var single = first(options); Check(Transformers.Chain(_ => single, absent)(options) == single, "single constructed component retains identity");
        Check(Transformers.Chain(first) == first, "one factory retains identity");
        bool threw = false; try { Transformers.Chain(); } catch (InvalidOperationException) { threw = true; }
        Check(threw, "empty factory list rejected");
    }
    private static void ProgramDeclarationProjection()
    {
        string directory = Path.Combine(Path.GetTempPath(), "emit-resolver-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string fileName = Path.Combine(directory, "main.ts").Replace('\\', '/');
            File.WriteAllText(fileName, "export function choose<T extends string>(value: T): T { return value; } export const narrow = \"ok\"; export const negative = -4; export const huge = 1e999; export const truth = true; export const computed = { [\"x\"]: 1 }; export declare const uniqueKey: unique symbol; export declare const synthesized: { [K in typeof uniqueKey]: number }; export const specialized = choose<\"fixed\">; export declare const quoted: \"quoted\"; export const accessors = { /** read */ get value(): string { return \"\"; }, /** write */ set value(v: number) {} }; export const autoClass = class { /** auto */ accessor auto = 1; };");
            var fs = TypeScript.Bundled.Bundled.WrapFS(TypeScript.Vfs.OsVfs.OsVfs.FS());
            var config = TypeScript.TsOptions.TsOptions.NewParsedCommandLine(new CompilerOptions { SkipLibCheck = Tristate.True }, [RootedFilePath.FromUnicode(fileName)], null, RootedDirectoryPath.FromUnicode(directory), fs.CaseSensitivity());
            var program = TypeScript.Compiler.Program.NewProgram(new() { Config = config, Host = TypeScript.Compiler.CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath()), SingleThreaded = Tristate.True });
            var file = program.GetSourceFile(RootedFilePath.FromUnicode(fileName));
            var (checker, done) = program.GetTypeCheckerForFile(default(CancellationToken), file);
            try
            {
                Check(checker.GetDiagnostics(file).Count == 0, "genuine Program declaration fixture checks without errors");
                var named = new Dictionary<string, Node>(StringComparer.Ordinal);
                Visitor visit = null;
                visit = node => { if (node.Kind is Kind.FunctionDeclaration or Kind.VariableDeclaration) named[node.Name().Text()] = node; node.ForEachChild(visit); return false; };
                file.ForEachChild(visit);
                var context = new EmitContext(); var resolver = checker.NewEmitResolver(context);
                var flags = TypeScript.NodeBuilding.Flags.NoTruncation;
                var internalFlags = TypeScript.NodeBuilding.InternalFlags.None;
                var function = named["choose"];
                var parameters = resolver.CreateTypeParametersOfSignatureDeclaration(function, function, flags, internalFlags, null);
                Check(parameters.Count == 1 && parameters[0].Name().Text() == "T" && parameters[0].AsTypeParameterDeclaration().Constraint.Kind == Kind.StringKeyword, "nested signature type-parameter projection retains name and constraint");
                var returnType = resolver.CreateReturnTypeOfSignatureDeclaration(function, function, flags, internalFlags, null);
                Check(returnType.Kind == Kind.TypeReference && returnType.AsTypeReferenceNode().TypeName.Text() == "T", "return type uses restored signature scope");
                var expressionType = resolver.CreateTypeOfExpression(named["narrow"].Initializer(), file.AsNode(), flags, internalFlags, null);
                Check(expressionType.Kind == Kind.LiteralType && expressionType.AsLiteralTypeNode().Literal.Kind == Kind.StringLiteral && expressionType.AsLiteralTypeNode().Literal.Text() == "ok", "expression request retains regular literal as pinned Go");
                var literal = resolver.CreateLiteralConstValue(named["narrow"], null);
                Check(literal.Kind == Kind.StringLiteral && literal.Text() == "ok", "literal const string projection");
                var negative = resolver.CreateLiteralConstValue(named["negative"], null);
                Check(negative.Kind == Kind.PrefixUnaryExpression && negative.AsPrefixUnaryExpression().Operator == Kind.MinusToken && negative.AsPrefixUnaryExpression().Operand.Text() == "4", "negative numeric const keeps unary minus AST");
                var infinity = resolver.CreateLiteralConstValue(named["huge"], null);
                Check(infinity.Kind == Kind.Identifier && infinity.Text() == "Infinity", "infinite numeric const projects identifier AST");
                Check(resolver.CreateLiteralConstValue(named["truth"], null).Kind == Kind.TrueKeyword, "boolean literal const projection");
                var builder = Checker.NewNodeBuilder(checker, context);
                var computedProperty = checker.getPropertiesOfType(checker.getTypeOfSymbol(named["computed"].Symbol()))[0];
                var computedName = builder.SymbolToNode(computedProperty, SymbolFlags.Value, named["computed"], flags, TypeScript.NodeBuilding.InternalFlags.WriteComputedProps, null);
                Check(computedName == computedProperty.ValueDeclaration.Name() && computedName.Kind == Kind.ComputedPropertyName, "computed property request retains actual declaration name");
                var synthesizedProperty = checker.getPropertiesOfType(checker.getTypeOfSymbol(named["synthesized"].Symbol()))[0];
                var synthesizedName = builder.SymbolToNode(synthesizedProperty, SymbolFlags.Value, named["synthesized"], flags, TypeScript.NodeBuilding.InternalFlags.WriteComputedProps, null);
                Check(synthesizedName.Kind == Kind.ComputedPropertyName && synthesizedName.Expression().Text() == "uniqueKey", "mapped unique-symbol name uses temporary declaration scope");
                var specializedSignature = checker.getSignaturesOfType(checker.getTypeOfSymbol(named["specialized"].Symbol()), SignatureKind.Call)[0];
                var specializedNode = builder.SignatureToSignatureDeclaration(specializedSignature, Kind.CallSignature, named["specialized"], flags, internalFlags, null);
                Check(specializedNode.Type().Kind == Kind.LiteralType && specializedNode.Type().AsLiteralTypeNode().Literal.Text() == "fixed" && specializedNode.Parameters()[0].Type().AsLiteralTypeNode().Literal.Text() == "fixed", "mapped signature serializes instantiated parameter and return nodes");
                var quoted = resolver.CreateTypeOfDeclaration(named["quoted"], named["quoted"], flags | TypeScript.NodeBuilding.Flags.UseSingleQuotesForStringLiteralType, internalFlags, null);
                Check(quoted.Kind == Kind.LiteralType && (quoted.AsLiteralTypeNode().Literal.AsStringLiteral().TokenFlags & TokenFlags.SingleQuote) != 0, "reused declaration literal honors single-quote option");
                var accessorType = resolver.CreateTypeOfDeclaration(named["accessors"], named["accessors"], flags, internalFlags, null);
                var accessorProperty = checker.getPropertiesOfType(checker.getTypeOfSymbol(named["accessors"].Symbol()))[0];
                var getterDeclaration = Ast.GetDeclarationOfKind(accessorProperty, Kind.GetAccessor);
                var setterDeclaration = Ast.GetDeclarationOfKind(accessorProperty, Kind.SetAccessor);
                var accessorMembers = accessorType.AsTypeLiteralNode().Members.Nodes;
                Check(accessorMembers.Count == 2 && accessorMembers[0].Kind == Kind.GetAccessor && accessorMembers[1].Kind == Kind.SetAccessor && context.CommentRange(accessorMembers[0]).Equals(getterDeclaration.Loc) && context.CommentRange(accessorMembers[1]).Equals(setterDeclaration.Loc), "distinct accessor projections retain original comment ranges");
                var autoType = resolver.CreateTypeOfDeclaration(named["autoClass"], named["autoClass"], flags | TypeScript.NodeBuilding.Flags.WriteClassExpressionAsTypeLiteral, internalFlags, null);
                var autoMembers = autoType.AsTypeLiteralNode().Members.Nodes[0].Type().AsTypeLiteralNode().Members.Nodes;
                var autoDeclaration = named["autoClass"].Initializer().AsClassExpression().Members.Nodes[0];
                Check(autoMembers.Count == 2 && autoMembers[0].Kind == Kind.GetAccessor && autoMembers[1].Kind == Kind.SetAccessor && context.CommentRange(autoMembers[0]).Equals(autoDeclaration.Loc) && context.CommentRange(autoMembers[1]).Equals(autoMembers[1].Loc), "auto-accessor projection retains getter source range and synthetic setter range exactly");
                resolver.PrecalculateDeclarationEmitVisibility(file);
                Check(resolver.IsDeclarationVisible(function), "exported function is declaration-visible");
            }
            finally { done(); }
        }
        finally { Directory.Delete(directory, true); }
    }
}
