// Authored external-consumer controls; no upstream semantic assertion claim.
using Configuration.Checks;
using TypeScript.Checking;
using TypeScript.Compiler;
using TypeScript.Scanner;
using TypeScript.Vfs;
using CompilerProgram = TypeScript.Compiler.Program;

namespace Declarations.Checks;

public static class ExternalSemanticBoundaryChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["Authored.ExternalSemanticConsumers"];

    public static void Run(string name)
    {
        if (name != Cases[0]) throw new ArgumentOutOfRangeException(nameof(name));
        const string js = "/** @type {number} */\nexport const value = \"invalid when checked\";\n";
        var checkedJs = Create("main.js", js, "\"allowJs\":true,\"checkJs\":true");
        var jsError = Unsupported(() => checkedJs.GetSemanticDiagnostics());
        Check.Equal(jsError.Feature, "javascriptSemantics", "checked JS reaches explicit external semantic consumer");

        // Program skipping and explicitly requested type queries are distinct.
        // The three natural skip-policy records are independently Go-paired.
        var skippedJs = Create("main.js", js, "\"allowJs\":true,\"checkJs\":false");
        Check.Equal(skippedJs.GetSemanticDiagnostics()?.Count ?? 0, 0, "CheckJs false preserves actual skip policy");
        var file = skippedJs.GetSourceFile(RootedFilePath.FromGoString("/main.js"))!;
        var node = file.Statements.Nodes[0].AsVariableStatement().DeclarationList
            .AsVariableDeclarationList().Declarations.Nodes[0].Name();
        var (checker, done) = skippedJs.GetTypeChecker();
        try
        {
            var queryError = Unsupported(() => checker.GetTypeAtLocation(node));
            Check.Equal(queryError.Feature, "javascriptSemantics", "explicit JS type query remains outside selected semantics");
            Check.Equal(queryError.FileName, file.FileName(), "queried external diagnostic owner");
            Check.Equal(queryError.Kind, node.Kind, "queried external node kind");
            Check.Equal(queryError.Pos, node.Pos(), "queried external byte start");
            Check.Equal(queryError.End, node.End(), "queried external byte end");
        }
        finally { done(); }

        var jsx = Create("main.tsx", "export const view = <div />;\n", "\"jsx\":\"preserve\"");
        Check.True(Unsupported(() => jsx.GetSemanticDiagnostics()).Feature.Contains("jsx", StringComparison.OrdinalIgnoreCase),
            "checked JSX fails at an actual JSX consumer");
        var decorated = Create("main.ts", "declare function decorate(target: any): void;\n@decorate class Model {}\nexport { Model };\n",
            "\"experimentalDecorators\":true");
        Check.True(Unsupported(() => decorated.GetSemanticDiagnostics()).Feature.Contains("decorator", StringComparison.OrdinalIgnoreCase),
            "checked decorator fails at an actual decorator consumer");
    }

    private static CompilerProgram Create(string name, string text, string extra)
    {
        string config = "{\"compilerOptions\":{\"target\":\"ES2022\",\"module\":\"ESNext\",\"moduleResolution\":\"Bundler\",\"noEmit\":true,\"lib\":[\"ES5\"],\"types\":[],\"skipLibCheck\":true," + extra + "},\"files\":[\"" + name + "\"]}";
        IFS fs = new ConfigurationFileSystem([
            new ConfigurationFileSystem.Input("/tsconfig.json", config),
            new ConfigurationFileSystem.Input("/" + name, text)], CaseSensitivity.CaseSensitive);
        fs = TypeScript.Bundled.Bundled.WrapFS(fs);
        var (parsed, errors) = TypeScript.TsOptions.TsOptions.GetParsedCommandLineOfConfigFile(
            RootedFilePath.FromGoString("/tsconfig.json"), null, null, fs, null);
        Check.Equal(errors?.Count ?? 0, 0, "external fixture configuration is valid");
        var program = CompilerProgram.NewProgram(new()
        {
            Config = parsed!, SingleThreaded = Tristate.True,
            Host = CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath())
        });
        Check.Equal(program.GetGlobalDiagnostics()?.Count ?? 0, 0, "external fixture uses real library globals");
        return program;
    }

    private static UnsupportedSemanticFeatureException Unsupported(Action operation)
    {
        try { operation(); }
        catch (UnsupportedSemanticFeatureException error) { return error; }
        throw new InvalidOperationException("Expected an explicit external semantic consumer failure.");
    }
}
