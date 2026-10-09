// Authored profile/ownership controls and the adapted original import-clause test.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Compiler;

namespace TypeScript.Projects;
public static class ProjectControls
{
    public static IReadOnlyList<string> Cases { get; } = ["Profile.RequiresProjectHost", "TestGetTypeAtLocationOfTypeOnlyImportClause", "Api.SharedAndForeignOwnership", "Admission.ModuleBoundary", "Parser.AnonymousDefaultInterface"];

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "Profile.RequiresProjectHost":
                var file = Parser.ParseSourceFile(new SourceFileParseOptions("/main.ts", "/main.ts"), System.Text.Encoding.UTF8.GetBytes("export const value=1;"), ScriptKind.TS);
                try
                {
                    Checker.Create(file, CheckerProfile.Projects);
                    throw new InvalidOperationException("Missing project host accepted");
                }
                catch (UnsupportedSemanticFeatureException ex)
                {
                    Require(ex.Message.Contains("projectHost", StringComparison.Ordinal), "Wrong missing-host boundary");
                }

                break;
            case "TestGetTypeAtLocationOfTypeOnlyImportClause":
                ImportClauseAssertions();
                break;
            case "Api.SharedAndForeignOwnership":
                Ownership();
                break;
            case "Parser.AnonymousDefaultInterface":
                AnonymousInterfaceParseDiagnostic();
                break;
            case "Admission.ModuleBoundary":
                foreach (string source in new[]
                {
                    "import x = require(\"./types\");",
                    "export = 1;",
                    "const x=import(\"./types\");",
                    "type T=import(\"./types\").U;"
                }

                )
                {
                    using var fixture = new Fixture(source, "export type U=number;");
                    try
                    {
                        Project.Load(fixture.Config);
                        throw new InvalidOperationException("Excluded module form accepted: " + source);
                    }
                    catch (UnsupportedSemanticFeatureException)
                    {
                    }
                    catch (ProjectException)
                    {
                    }
                }

                break;
            default:
                throw new ArgumentException("Unknown project control: " + name);
        }
    }

    // Original Program probe: the required anonymous default interface spelling
    // is invalid at this pin. This covers parse failure, not semantic completeness.
    private static void AnonymousInterfaceParseDiagnostic()
    {
        using var fixture = new Fixture("import type Shape from \"./types\"; const value:Shape={value:1};", "export default interface {value:number}");
        var project = Project.Load(fixture.Config);
        var diagnostics = project.Check();
        Require(diagnostics.Count == 1 && project.ParseDiagnostics.Count == 1, "Expected only the original parse diagnostic");
        var diagnostic = diagnostics[0];
        Require(diagnostic.Code() == 1003 && diagnostic.Category() == DiagnosticCategory.Error && diagnostic.Pos() == 25 && diagnostic.End() == 26, "Original anonymous interface parse code/range differs");
        Require(diagnostic.MessageKey() == "Identifier_expected_1003" && diagnostic.String() == "Identifier expected.", "Original parse message differs");
        Require(diagnostic.MessageArgs()is null && diagnostic.MessageChain()is null && diagnostic.RelatedInformation()is null && diagnostic.Source() == "", "Original parse payload differs");
        Require(!diagnostic.ReportsUnnecessary() && !diagnostic.ReportsDeprecated() && !diagnostic.SkippedOnNoEmit(), "Original parse flags differ");
        Require(diagnostic.File()?.FileName() == Path.Combine(Path.GetDirectoryName(fixture.Config)!, "types.ts"), "Original parse file ownership differs");
        Require(project.ProgramDiagnostics.Count == 0, "Unexpected program diagnostic for anonymous interface");
    }

    // checker/checker_test.go TestGetTypeAtLocationOfTypeOnlyImportClause:
    // exact source and three identity assertions; host/options/files adapted only.
    private static void ImportClauseAssertions()
    {
        using var fixture = new Fixture("import type { U } from \"./types\";\nimport type * as types from \"./types\";\nimport { U as V } from \"./types\";\nimport type D from \"./types\";\nexport const u: U = 1;\nexport const v: V = 1;\nexport type W = types.U;\nexport type E = D;", "export type U = number;\nexport default interface D { x: number }");
        var project = Project.Load(fixture.Config);
        var checker = project.Checker;
        var file = project.UserFiles.Single(f => f.FileName().EndsWith("/main.ts", StringComparison.Ordinal));
        Node Clause(int index) => file.Statements.Nodes[index].AsImportDeclaration().ImportClause;
        var regular = checker.GetTypeAtLocation(Clause(2));
        foreach (int index in new[]
        {
            0,
            1
        }

        )
        {
            var type = checker.GetTypeAtLocation(Clause(index));
            Require(type is not null, "Expected nonnil import clause type");
            Require(ReferenceEquals(type, regular), "Original type-only clause identity differs");
        }

        var defaultClause = checker.GetTypeAtLocation(Clause(3));
        var defaultReference = checker.GetTypeAtLocation(file.Statements.Nodes[7].AsTypeAliasDeclaration().Type);
        Require(ReferenceEquals(defaultClause, defaultReference), "Original default clause/reference identity differs");
    }

    private static void Ownership()
    {
        using var one = new Fixture("import {value} from \"./types\";const x=value;", "export const value=1;");
        using var two = new Fixture("import {value} from \"./types\";const x=value;", "export const value=1;");
        var a = Project.Load(one.Config);
        var b = Project.Load(two.Config);
        var checker = a.Checker;
        Require(ReferenceEquals(checker, a.Checker), "Project returned a different checker");
        Require(!ReferenceEquals(a.LibraryFiles[0], b.LibraryFiles[0]), "Fresh environment shared mutable library AST");
        var array = checker.Globals["Array"];
        var type = checker.GetTypeOfSymbol(array);
        Require(ReferenceEquals(type, checker.GetTypeOfSymbol(array)), "Owned library symbol type identity changed");
        var foreign = b.Checker.Globals["Array"];
        try
        {
            checker.GetTypeOfSymbol(foreign);
            throw new InvalidOperationException("Foreign library symbol accepted");
        }
        catch (ArgumentException)
        {
        }

        var foreignFile = b.UserFiles.Single(f => f.FileName().EndsWith("/main.ts", StringComparison.Ordinal));
        try
        {
            checker.GetTypeAtLocation(foreignFile.AsNode());
            throw new InvalidOperationException("Foreign source node accepted");
        }
        catch (ArgumentException)
        {
        }

        var ownedFile = a.UserFiles.Single(f => f.FileName().EndsWith("/main.ts", StringComparison.Ordinal));
        var ownedName = ownedFile.Statements.Nodes[0].AsImportDeclaration().ImportClause.AsImportClause().NamedBindings.AsNamedImports().Elements.Nodes[0].Name();
        var foreignName = foreignFile.Statements.Nodes[0].AsImportDeclaration().ImportClause.AsImportClause().NamedBindings.AsNamedImports().Elements.Nodes[0].Name();
        var ownedAlias = checker.GetSymbolAtLocation(ownedName)!;
        var foreignAlias = b.Checker.GetSymbolAtLocation(foreignName)!;
        Require(ReferenceEquals(checker.GetAliasedSymbol(ownedAlias), checker.GetAliasedSymbol(ownedAlias)), "Owned alias target identity changed");
        foreach (Action action in new Action[]
        {
            () => checker.GetAliasedSymbol(foreignAlias),
            () => checker.GetImmediateAliasedSymbol(foreignAlias),
            () => checker.GetTargetSymbol(foreignAlias),
            () => checker.GetTypeOnlyAliasDeclaration(foreignAlias),
            () => checker.GetExportsOfModule(b.UserFiles.Single(f => f.FileName().EndsWith("/types.ts", StringComparison.Ordinal)).AsNode().Symbol()!)
        }

        )
        {
            try
            {
                action();
                throw new InvalidOperationException("Foreign project alias/module symbol accepted");
            }
            catch (ArgumentException)
            {
            }
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "project-controls-" + Guid.NewGuid().ToString("N"));
        internal string Config => Path.Combine(root, "tsconfig.json");

        internal Fixture(string main, string types)
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Config, "{\"files\":[\"main.ts\"]}");
            File.WriteAllText(Path.Combine(root, "main.ts"), main);
            File.WriteAllText(Path.Combine(root, "types.ts"), types);
        }

        public void Dispose() => Directory.Delete(root, true);
    }
}
