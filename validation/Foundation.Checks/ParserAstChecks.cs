// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified assertion adapter of tsc/internal/parser/parser_test.go and ast/*_test.go,
// upstream fed0bf24149fb1ed36039212648bafdafc1ea10e. Shared xUnit/Native AOT checks.
using System.Text;
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;
namespace TypeScript.Foundation;
public static partial class ParserAstChecks
{
    private static readonly string[] FunctionCases =
    [
        "TestHeritageClauseElementKinds", "TestParseStaticSourcePhaseImport",
        "TestParseSourceAsImportEqualsBinding", "TestParseInvalidStaticSourcePhaseImports",
        "TestParseDynamicSourcePhaseImport", "TestParseEscapedDynamicImportPhase",
        "TestJSDocImportTypeParentChain", "TestJSDocTypeSourceSurvivesReparse",
        "TestJSDocTypeSourcePropagatesToConstructedReparse", "TestSourceFilePositionMapWithNonASCIIStringLiteral",
        "TestPositionMapASCII", "TestPositionMapTwoByte", "TestPositionMapFourByte",
        "TestPositionMapMultipleNonASCII", "TestPositionMapLoneSurrogateSentinel", "TestPositionMapRoundtrip",
        "TestDiagnosticsCollectionDeduplicatesExactDiagnosticsOnAdd", "TestDiagnosticsCollectionPreservesDistinctAdHocMessages",
        "TestDiagnosticsCollectionGetsDiagnosticsForEquivalentSourceFile", "TestExternalDiagnosticIdentity",
        "TestLocalize/default-English", "TestLocalize_ByKey"
    ];
    public static IReadOnlyList<string> Cases { get; }
    static ParserAstChecks() => Cases = FunctionCases.Concat(CloneRows.Select((r, i) => $"TestDeepCloneNodeSanityCheck/{i:D3}/{r.Title}")).ToArray();
    private static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Equal<T>(T actual, T expected, string message) => Assert(EqualityComparer<T>.Default.Equals(actual, expected), $"{message}: actual={actual}, expected={expected}");
    private static SourceFile Parse(string source, ScriptKind kind = ScriptKind.TS) => Parser.ParseSourceFile(new(kind == ScriptKind.JS ? "/index.js" : "/main.ts", kind == ScriptKind.JS ? "/index.js" : "/main.ts"), source, kind);
    private static void NoDiagnostics(SourceFile file) => Equal(file.Diagnostics().Length, 0, "parse diagnostics");
    public static void Run(string caseName)
    {
        if (caseName.StartsWith("TestDeepCloneNodeSanityCheck/", StringComparison.Ordinal))
        {
            var row = int.Parse(caseName.Split('/')[1], System.Globalization.CultureInfo.InvariantCulture);
            var file = Parse(CloneRows[row].Input).AsNode(); // Upstream intentionally passes jsx=false for every row.
            var clone = new NodeFactory().DeepCloneNode(file);
            var work = new List<(Node Original, Node Copy)> { (file, clone) };
            while (work.Count > 0)
            {
                var next = new List<(Node, Node)>();
                foreach (var item in work)
                {
                    Assert(!ReferenceEquals(item.Original, item.Copy), "deep clone must have distinct node identity");
                    var originalChildren = Children(item.Original); var copyChildren = Children(item.Copy);
                    Equal(originalChildren.Count, copyChildren.Count, "VisitEachChild child count");
                    for (int i = 0; i < originalChildren.Count; i++) next.Add((originalChildren[i], copyChildren[i]));
                }
                work = next;
            }
            return;
        }
        switch (caseName)
        {
            case "TestHeritageClauseElementKinds": Heritage(); break;
            case "TestParseStaticSourcePhaseImport": StaticImports(); break;
            case "TestParseSourceAsImportEqualsBinding":
                var eq = Parse("import source = require(\"./module.js\");"); NoDiagnostics(eq); Equal(eq.Statements.Nodes.Count, 1, "statement count");
                Assert(Ast.IsImportEqualsDeclaration(eq.Statements.Nodes[0]), "import equals kind"); Equal(eq.Statements.Nodes[0].Name().Text(), "source", "binding name"); break;
            case "TestParseInvalidStaticSourcePhaseImports": InvalidStaticImports(); break;
            case "TestParseDynamicSourcePhaseImport": DynamicImport(); break;
            case "TestParseEscapedDynamicImportPhase": EscapedDynamicImports(); break;
            case "TestJSDocImportTypeParentChain": JSDocParents(); break;
            case "TestJSDocTypeSourceSurvivesReparse": JSDocSource(); break;
            case "TestJSDocTypeSourcePropagatesToConstructedReparse": JSDocConstructed(); break;
            case "TestSourceFilePositionMapWithNonASCIIStringLiteral":
                const string source = "const x = \"─\";\n\nnamespace N {\n  export const y = x;\n}\n";
                var file = Parse(source); var pm = file.GetPositionMap(); Assert(!pm.IsAsciiOnly(), "source map non ASCII");
                int end = Encoding.UTF8.GetByteCount(source[..(source.IndexOf('─') + 1)]);
                Equal(pm.UTF8ToUTF16(end), end - 2, "after box drawing"); Equal(pm.UTF8ToUTF16(Encoding.UTF8.GetByteCount(source)), Encoding.UTF8.GetByteCount(source) - 2, "end of source"); break;
            case "TestPositionMapASCII": PositionASCII(); break;
            case "TestPositionMapTwoByte": PositionTwoByte(); break;
            case "TestPositionMapFourByte": PositionFourByte(); break;
            case "TestPositionMapMultipleNonASCII":
                var multi = Ast.ComputePositionMap("à🎉x"); foreach (var pair in new[] { (0, 0), (2, 1), (6, 3), (7, 4) }) { Equal(multi.UTF8ToUTF16(pair.Item1), pair.Item2, "UTF8 boundary"); Equal(multi.UTF16ToUTF8(pair.Item2), pair.Item1, "UTF16 boundary"); } break;
            case "TestPositionMapLoneSurrogateSentinel":
                var raw = "a" + StringUtil.EncodeJSStringRune(0xD800) + "b"; var sentinel = Ast.ComputePositionMap(GoString.Bytes(raw));
                Assert(!sentinel.IsAsciiOnly(), "sentinel non ASCII"); Equal(sentinel.UTF8ToUTF16(raw.Length), 3, "sentinel length"); Equal(sentinel.UTF16ToUTF8(2), raw.Length - 1, "sentinel reverse"); break;
            case "TestPositionMapRoundtrip":
                var roundtrip = Ast.ComputePositionMap("let café = \"🎉\"; // naïve"); int utf16Length = roundtrip.UTF8ToUTF16(Encoding.UTF8.GetByteCount("let café = \"🎉\"; // naïve"));
                for (int i = 0; i <= utf16Length; i++) Equal(roundtrip.UTF8ToUTF16(roundtrip.UTF16ToUTF8(i)), i, "roundtrip"); break;
            case "TestDiagnosticsCollectionDeduplicatesExactDiagnosticsOnAdd": DiagnosticCanonical(); break;
            case "TestDiagnosticsCollectionPreservesDistinctAdHocMessages":
                var adhoc = new DiagnosticsCollection(); adhoc.Add(Ast.NewCompilerDiagnostic(Diagnostics.NewAdHocMessage("first"))); adhoc.Add(Ast.NewCompilerDiagnostic(Diagnostics.NewAdHocMessage("second"))); Equal(adhoc.GetGlobalDiagnostics().Count, 2, "distinct adhoc messages"); break;
            case "TestDiagnosticsCollectionGetsDiagnosticsForEquivalentSourceFile":
                var df = Parser.ParseSourceFile(new("/src/file.ts", "/src/file.ts"), "", ScriptKind.TS); var rf = Parser.ParseSourceFile(new("/src/file.ts", "/src/file.ts"), "", ScriptKind.TS);
                var d = Ast.NewDiagnostic(df, new(0, 0), Diagnostics.Cannot_find_name_0, "x"); var dc = new DiagnosticsCollection(); dc.Add(d);
                var ds = dc.GetDiagnosticsForFile(rf); Equal(ds.Count, 1, "equivalent source count"); Assert(ReferenceEquals(ds[0], d), "equivalent source identity"); break;
            case "TestExternalDiagnosticIdentity": ExternalDiagnostics(); break;
            case "TestLocalize/default-English":
                Equal(Diagnostics.Identifier_expected.Localize(), "Identifier expected.", "english default");
                Equal(Diagnostics.X_0_expected.Localize(")"), "')' expected.", "single argument");
                Equal(Diagnostics.The_parser_expected_to_find_a_1_to_match_the_0_token_here.Localize("{", "}"), "The parser expected to find a '}' to match the '{' token here.", "multiple arguments"); break;
            case "TestLocalize_ByKey":
                Equal(Diagnostics.Localize("Identifier_expected_1003"), "Identifier expected.", "by key without args");
                Equal(Diagnostics.Localize("_0_expected_1005", ")"), "')' expected.", "by key with args"); break;
            default: throw new ArgumentException("Unknown direct upstream assertion: " + caseName);
        }
    }
    private static List<Node> Children(Node node)
    {
        var children = new List<Node>(); node.VisitEachChild(Ast.NewNodeVisitor(n => { children.Add(n); return n; }, null!, new())); return children;
    }
    private static void Heritage()
    {
        var file = Parse("class C extends Base<number> implements Contract<string> {}\ninterface I extends Parent<boolean> {}\ninterface Invalid implements Recovery {}\ninterface MissingExtends extends A. {}\nclass MissingImplements implements B. {}\n");
        Equal(file.Statements.Nodes[0].AsClassDeclaration().HeritageClauses.Nodes[0].AsHeritageClause().Types.Nodes[0].Kind, Kind.ExpressionWithTypeArguments, "class extends");
        Equal(file.Statements.Nodes[0].AsClassDeclaration().HeritageClauses.Nodes[1].AsHeritageClause().Types.Nodes[0].Kind, Kind.TypeReference, "class implements");
        Equal(file.Statements.Nodes[1].AsInterfaceDeclaration().HeritageClauses.Nodes[0].AsHeritageClause().Types.Nodes[0].Kind, Kind.TypeReference, "interface extends");
        Equal(file.Statements.Nodes[2].AsInterfaceDeclaration().HeritageClauses.Nodes[0].AsHeritageClause().Types.Nodes[0].Kind, Kind.ExpressionWithTypeArguments, "invalid implements");
        Equal(file.Statements.Nodes[3].AsInterfaceDeclaration().HeritageClauses.Nodes[0].AsHeritageClause().Types.Nodes[0].Kind, Kind.ExpressionWithTypeArguments, "missing extends");
        Equal(file.Statements.Nodes[4].AsClassDeclaration().HeritageClauses.Nodes[0].AsHeritageClause().Types.Nodes[0].Kind, Kind.ExpressionWithTypeArguments, "missing implements");
    }
    private static void StaticImports()
    {
        (string Source, Kind Phase, string Name, bool Attributes)[] tests =
        [ ("import source a from \"./a.wasm\";", Kind.SourceKeyword, "a", false), ("import source a from \"./a.wasm\" with { type: \"webassembly\" };", Kind.SourceKeyword, "a", true),
          ("import source from from \"./module.js\";", Kind.SourceKeyword, "from", false), ("import source from \"./module.js\";", Kind.Unknown, "source", false),
          ("import source, { value } from \"./module.js\";", Kind.Unknown, "source", false), ("import s\\u006furce from \"./module.js\";", Kind.Unknown, "source", false), ("import d\\u0065fer from \"./module.js\";", Kind.Unknown, "defer", false) ];
        foreach (var test in tests)
        {
            var file = Parse(test.Source); NoDiagnostics(file); Equal(file.Statements.Nodes.Count, 1, "import statements"); var node = file.Statements.Nodes[0]; Assert(Ast.IsImportDeclaration(node), "import kind");
            var declaration = node.AsImportDeclaration(); Assert(declaration.ImportClause != null, "import clause"); var clause = declaration.ImportClause!.AsImportClause();
            Equal(clause.PhaseModifier, test.Phase, "phase"); Assert(clause.Name() != null, "binding name present"); Equal(clause.Name().Text(), test.Name, "binding name"); Equal(declaration.Attributes != null, test.Attributes, "attributes");
        }
    }
    private static void InvalidStaticImports()
    {
        (string Source, bool HasName, bool HasNamed)[] tests = [("import source \"./a.js\";", false, false), ("import source * as a from \"./a.js\";", false, true), ("import source { a } from \"./a.js\";", false, true), ("import source a, { b } from \"./a.js\";", true, true)];
        foreach (var test in tests) { var file = Parse(test.Source); NoDiagnostics(file); Equal(file.Statements.Nodes.Count, 1, "statements"); Assert(Ast.IsImportDeclaration(file.Statements.Nodes[0]), "import declaration"); var d = file.Statements.Nodes[0].AsImportDeclaration(); Assert(d.ImportClause != null, "import clause"); var c = d.ImportClause!.AsImportClause(); Equal(c.PhaseModifier, Kind.SourceKeyword, "phase"); Equal(c.Name() != null, test.HasName, "name presence"); Equal(c.NamedBindings != null, test.HasNamed, "named presence"); }
    }
    private static void DynamicImport()
    {
        var file = Parse("import.source(\"./a.wasm\", { with: { type: \"webassembly\" } });"); NoDiagnostics(file); Equal(file.Statements.Nodes.Count, 1, "statements"); var statement = file.Statements.Nodes[0]; Assert(Ast.IsExpressionStatement(statement), "expression statement"); var call = statement.Expression(); Assert(Ast.IsCallExpression(call), "call"); Assert(Ast.IsImportCall(call), "import call"); Assert((call.SubtreeFacts() & SubtreeFacts.ContainsDynamicImport) != 0, "dynamic import subtree fact"); var meta = call.Expression(); Assert(Ast.IsMetaProperty(meta), "meta property"); Equal(meta.AsMetaProperty().KeywordToken, Kind.ImportKeyword, "import keyword"); Equal(meta.Text(), "source", "phase name"); Equal(call.Arguments().Count, 2, "arguments"); Assert((file.Flags & NodeFlags.PossiblyContainsDynamicImport) != 0, "dynamic flag"); Assert((file.Flags & NodeFlags.PossiblyContainsImportMeta) == 0, "meta flag"); Assert(file.ExternalModuleIndicator == null, "not external module");
    }
    private static void EscapedDynamicImports()
    {
        foreach (var test in new[] { ("import.d\\u0065fer(\"./a.js\");", "defer"), ("import.s\\u006furce(\"./a.wasm\");", "source") })
        { var file = Parse(test.Item1); Equal(file.Diagnostics().Count, 1, "keyword escape diagnostics"); Equal(file.Diagnostics()[0].Code(), Diagnostics.Keywords_cannot_contain_escape_characters.Code(), "keyword escape code"); Equal(file.Statements.Nodes.Count, 1, "statements"); var statement = file.Statements.Nodes[0]; Assert(Ast.IsExpressionStatement(statement), "expression statement"); var call = statement.Expression(); Assert(Ast.IsCallExpression(call) && Ast.IsImportCall(call), "import call"); Assert(Ast.IsMetaProperty(call.Expression()), "meta property"); Equal(call.Expression().Text(), test.Item2, "phase name"); Assert((file.Flags & NodeFlags.PossiblyContainsDynamicImport) != 0, "dynamic flag"); Assert((file.Flags & NodeFlags.PossiblyContainsImportMeta) == 0, "meta flag"); }
    }
    private static void JSDocParents()
    {
        const string source = "test(\"\", async function () {\n  ;(/** @type {typeof import(\"a\")} */ ({}))\n})\n\ntest(\"\", async function () {\n  ;(/** @type {typeof import(\"a\")} */ a)\n})\n\ntest(\"\", async function () {\n  (/** @type {typeof import(\"a\")} */ ({}))\n  ;(/** @type {typeof import(\"a\")} */ ({}))\n})\n\ntest(\"\", async function () {\n  (/** @type {typeof import(\"a\")} */ a)\n  ;(/** @type {typeof import(\"a\")} */ a)\n})\n\ntest(\"\", async function () {\n  (/** @type {typeof import(\"a\")} */ ({}))\n  ;(/** @type {typeof import(\"a\")} */ ({}))\n})\n";
        var file = Parse(source, ScriptKind.JS); for (int i = 1; i < file.ReparsedClones.Count; i++) { var a = file.ReparsedClones[i - 1]; var b = file.ReparsedClones[i]; Assert(a.Pos() != b.Pos() || a.End() != b.End() || a.Kind != b.Kind, "duplicate reparsed clone"); }
        foreach (var import in file.Imports()) Assert(Ast.GetSourceFileOfNode(Ast.GetReparsedNodeForNode(import)) != null, "reparsed import parent chain");
    }
    private static void JSDocSource()
    {
        var file = Parse("/**\n * @typedef {(\n *   \"a\" |\n *   \"b\"\n * )[]} T\n */\nconst value = 0;", ScriptKind.JS); var alias = Core.Find(file.Statements.Nodes, Ast.IsJSTypeAliasDeclaration); Assert(alias != null, "JS type alias");
        var docs = alias!.JSDoc(file); Equal(docs.Count, 1, "JSDoc count"); var tags = docs[0].AsJSDoc().Tags; Assert(tags != null, "tags"); Equal(tags!.Nodes.Count, 1, "tag count"); var expression = tags.Nodes[0].TypeExpression(); Assert(expression != null, "type expression");
        foreach (var node in new[] { expression!.Type(), alias.Type() }) Equal(ScannerApi.GetTextOfNode(node), "(\n\"a\" |\n\"b\"\n)[]", "JSDoc type source");
    }
    private static void JSDocConstructed()
    {
        const string source = "/**\n * @param {{\n *   value: string\n * }} options\n */\nfunction foo(options) {}"; var file = Parse(source, ScriptKind.JS); var function = file.Statements.Nodes[0]; Assert(Ast.IsFunctionDeclaration(function), "function"); Equal(function.Parameters().Count, 1, "parameters"); var type = function.Parameters()[0].Type(); Assert(type != null, "constructed type"); Assert((type!.Flags & NodeFlags.Reparsed) != 0, "reparsed flag"); Equal(ScannerApi.GetTextOfNode(type), "{\nvalue: string\n}", "constructed source"); Equal(ScannerApi.GetTokenPosOfNode(type, file, false), source.IndexOf("{{", StringComparison.Ordinal) + 1, "inner brace position");
    }
    private static void PositionASCII() { const string text = "const x = 1;"; var pm = Ast.ComputePositionMap(text); Assert(pm.IsAsciiOnly(), "ASCII"); for (int i = 0; i <= text.Length; i++) { Equal(pm.UTF8ToUTF16(i), i, "ASCII UTF8"); Equal(pm.UTF16ToUTF8(i), i, "ASCII UTF16"); } }
    private static void PositionTwoByte() { const string text = "const café = 1;\nconst x = 2;"; var pm = Ast.ComputePositionMap(text); Assert(!pm.IsAsciiOnly(), "non ASCII"); for (int i = 0; i < 10; i++) Equal(pm.UTF8ToUTF16(i), i, "before é"); Equal(pm.UTF8ToUTF16(9), 9, "at é"); Equal(pm.UTF8ToUTF16(11), 10, "after é"); int x = Encoding.UTF8.GetByteCount(text[..text.LastIndexOf('x')]); Equal(pm.UTF8ToUTF16(x), x - 1, "at x"); Equal(pm.UTF16ToUTF8(x - 1), x, "reverse x"); }
    private static void PositionFourByte() { const string text = "const a = \"🎉\";\nconst b = 2;"; var pm = Ast.ComputePositionMap(text); Assert(!pm.IsAsciiOnly(), "non ASCII"); int b = Encoding.UTF8.GetByteCount(text[..text.LastIndexOf('b')]); Equal(pm.UTF8ToUTF16(b), b - 2, "at b"); Equal(pm.UTF16ToUTF8(b - 2), b, "reverse b"); }
    private static void DiagnosticCanonical()
    {
        var collection = new DiagnosticsCollection(); var first = Ast.NewCompilerDiagnostic(Diagnostics.Cannot_find_name_0, "x").AddRelatedInfo(Ast.NewCompilerDiagnostic(Diagnostics.X_0_is_declared_here, "first")); var second = Ast.NewCompilerDiagnostic(Diagnostics.Cannot_find_name_0, "x").AddRelatedInfo(Ast.NewCompilerDiagnostic(Diagnostics.X_0_is_declared_here, "first")); var different = Ast.NewCompilerDiagnostic(Diagnostics.Cannot_find_name_0, "x").AddRelatedInfo(Ast.NewCompilerDiagnostic(Diagnostics.X_0_is_declared_here, "second")); Assert(ReferenceEquals(collection.Add(first), first), "first canonical"); var canonical = collection.Add(second); Assert(ReferenceEquals(canonical, first), "duplicate canonical"); Assert(ReferenceEquals(collection.Add(different), different), "distinct related"); canonical.AddRelatedInfo(Ast.NewCompilerDiagnostic(Diagnostics.X_0_is_declared_here, "third")); Equal(collection.GetGlobalDiagnostics().Count, 2, "global count"); Equal(first.RelatedInformation().Count, 2, "related count");
    }
    private static void ExternalDiagnostics()
    {
        var file = Parser.ParseSourceFile(new("/src/file.vue", "/src/file.vue"), "", ScriptKind.TS); var loc = new TextRange(1, 2); var first = Ast.NewExternalDiagnostic(file, loc, "mapper-a", DiagnosticCategory.Error, 0, "first"); Diagnostic[] ds = [first, Ast.NewExternalDiagnostic(file, loc, "mapper-a", DiagnosticCategory.Error, 0, "second"), Ast.NewExternalDiagnostic(file, loc, "mapper-b", DiagnosticCategory.Error, 0, "first"), Ast.NewExternalDiagnostic(file, loc, "mapper-a", DiagnosticCategory.Warning, 0, "first")]; var collection = new DiagnosticsCollection(); foreach (var d in ds) { Assert(!Ast.EqualDiagnosticsNoRelatedInfo(first, d) || ReferenceEquals(first, d), "external equality"); Assert(Ast.CompareDiagnostics(first, d) != 0 || ReferenceEquals(first, d), "external compare"); collection.Add(d); } Equal(collection.GetDiagnostics().Count, ds.Length, "external count");
    }
}
