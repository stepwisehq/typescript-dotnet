// Authored public lifetime controls for parser.go getParser/putParser.
// These assert published syntax and diagnostics, not private pool identities.
using TypeScript.Scanner;

namespace TypeScript.Foundation;

public static class ParserPoolChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["mixed-kind-failure-lifetime", "parallel-lazy-jsdoc"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "mixed-kind-failure-lifetime": MixedKinds(); break;
            case "parallel-lazy-jsdoc": ParallelJSDoc(); break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static SourceFile Parse(string name, string text, ScriptKind kind) =>
        Parser.ParseSourceFile(new(name, name), text, kind);

    private static void MixedKinds()
    {
        const string originalText = "/** @deprecated */ function original(a: number) { return a; }";
        var original = Parse("/original.ts", originalText, ScriptKind.TS);
        var function = original.Statements.Nodes[0];
        var parameters = function.Parameters();
        int nodeCount = original.NodeCount, textCount = original.TextCount;
        var malformed = Parse("/broken.ts", "const broken = ;", ScriptKind.TS);
        var diagnostics = malformed.Diagnostics();
        Require(diagnostics.Length != 0, "Malformed input publishes actual syntax diagnostics");
        var firstDiagnostic = diagnostics[0];
        int diagnosticCode = firstDiagnostic.Code();

        for (int pass = 0; pass < 3; pass++)
        {
            try
            {
                _ = Parse("/unknown.ts", "ignored", ScriptKind.Unknown);
                throw new InvalidOperationException("Unknown script kind was accepted");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("ScriptKind must be specified", StringComparison.Ordinal)) { }

            var json = Parse("/data.json", "{\"value\":[1,2]}", ScriptKind.JSON);
            Require(json.Diagnostics().Length == 0 && (json.Flags & NodeFlags.JsonFile) != 0,
                "JSON parsing retains its own context and has no syntax errors after a failed request");
            var js = Parse("/next.js", "/** @deprecated */ function next() {}", ScriptKind.JS);
            Require(js.Diagnostics().Length == 0 && js.Statements.Nodes[0].EagerJSDoc(js).Length == 1,
                "JavaScript documentation is eagerly published after JSON parsing");
            var repeat = Parse("/original.ts", originalText, ScriptKind.TS);
            Require(repeat.Diagnostics().Length == 0 && (repeat.Flags & (NodeFlags.JsonFile | NodeFlags.JavaScriptFile)) == 0,
                "TypeScript parsing does not inherit JSON or JavaScript context");
            Require(repeat.NodeCount == nodeCount && repeat.TextCount == textCount,
                "Parser factory counts describe the current file rather than earlier requests");
            Require(Parser.ParseIsolatedEntityName("Alpha.Beta")?.Kind == Kind.QualifiedName,
                "Isolated entity parsing also succeeds after mixed-kind and failed requests");
        }

        Require(original.Text() == originalText && ReferenceEquals(original.Statements.Nodes[0], function),
            "Later requests preserve the original source and published statement identity");
        Require(parameters.Length == 1 && parameters[0].Name().Text() == "a" && function.Name().Text() == "original",
            "Published parameter slots and names survive parser reuse");
        Require(malformed.Diagnostics().Length == diagnostics.Length &&
            ReferenceEquals(firstDiagnostic, malformed.Diagnostics()[0]) && firstDiagnostic.Code() == diagnosticCode &&
            ReferenceEquals(firstDiagnostic.File(), malformed),
            "Published syntax diagnostics retain identity, content and source ownership across later parses");
        CheckDocumentation(original, function);
    }

    private static void CheckDocumentation(SourceFile file, Node function)
    {
        var docs = function.JSDoc(file);
        Require(docs.Length == 1 && ReferenceEquals(docs[0].Parent, function), "Lazy documentation has the correct source parent");
        Require(docs[0].AsJSDoc().Tags.Nodes.Length == 1 && docs[0].AsJSDoc().Tags.Nodes[0].Kind == Kind.JSDocDeprecatedTag,
            "Lazy documentation retains the expected deprecated tag");
        Require(Core.Same(docs, function.JSDoc(file)) && Core.Same(docs, function.EagerJSDoc(file)),
            "Repeated and eager documentation reads share the published cache header");
    }

    private static void ParallelJSDoc()
    {
        var files = Enumerable.Range(0, 4).Select(i => Parse("/lazy" + i + ".ts",
            "/** @deprecated */ function f" + i + "() {}", ScriptKind.TS)).ToArray();
        foreach (var file in files)
            Require(file.Statements.Nodes[0].EagerJSDoc(file).IsNil, "TypeScript documentation starts lazy");
        var observed = new Slice<Node>[16];
        Parallel.For(0, observed.Length, i =>
        {
            var file = files[i % files.Length];
            var function = file.Statements.Nodes[0];
            observed[i] = function.JSDoc(file);
            CheckDocumentation(file, function);
            var other = Parse("/parallel" + i + ".tsx", "const value = <div/>;", ScriptKind.TSX);
            Require(other.Diagnostics().Length == 0 && other.Statements.Nodes.Length == 1,
                "Concurrent ordinary parsing publishes complete syntax beside lazy documentation requests");
        });
        for (int i = 0; i < observed.Length; i++)
        {
            var file = files[i % files.Length];
            var function = file.Statements.Nodes[0];
            Require(Core.Same(observed[i], function.JSDoc(file)) && ReferenceEquals(observed[i][0].Parent, function),
                "Competing first readers publish one stable documentation header per source node");
            Require(function.Name().Text() == "f" + i % files.Length && file.Diagnostics().Length == 0,
                "Parallel parser leases preserve each original file's name and diagnostics");
        }
    }
}
