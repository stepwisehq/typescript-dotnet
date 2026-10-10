// Authored regressions for full interpretation and genuine Program checking.
// These assertions are shared by xUnit and the native dispatcher; no original
// compiler test body is claimed for these disk integration inputs.
using System.Text.Json.Nodes;
using TypeScript.Compiler;

namespace Configuration.Checks;
public static class IntegrationChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["Authored.ParsedAbsoluteRootsAndAdmission"];

    public static void Run(string name)
    {
        if (name != Cases[0])
            throw new ArgumentOutOfRangeException(nameof(name));
        string fixture = Path.Combine(Path.GetTempPath(), "configuration-integration-" + Guid.NewGuid().ToString("N"));
        string root = Path.Combine(fixture, "project");
        Directory.CreateDirectory(Path.Combine(root, "src"));
        try
        {
            File.WriteAllText(Path.Combine(root, "base.json"), """
                {"compilerOptions": {
                    "target":"ESNext", "module":"ESNext", "moduleResolution":"Bundler",
                    "noEmit":true, "noLib":false, "lib":["es5"], "skipLibCheck":true, "types":[], "resolveJsonModule":false,
                    "allowImportingTsExtensions":true, "noUncheckedSideEffectImports":true,
                    "forceConsistentCasingInFileNames":true, "strict":false, "strictNullChecks":true,
                    "strictFunctionTypes":true, "noImplicitAny":true, "noImplicitThis":true,
                    "strictBindCallApply":false, "strictPropertyInitialization":false,
                    "strictBuiltinIteratorReturn":false, "useUnknownInCatchVariables":false,
                    "noImplicitReturns":false, "exactOptionalPropertyTypes":false,
                    "noUncheckedIndexedAccess":false, "allowJs":false, "noResolve":false
                }}
                """);
            string config = Path.Combine(root, "tsconfig.json");
            File.WriteAllText(config, """{"extends":"./base.json","include":["src/**/*.ts"]}""");
            string source = Path.Combine(root, "src", "é-𐀀.ts");
            File.WriteAllText(source, "export const value: number = 1;\n");
            var inspection = Project.InspectConfiguration(config);
            Check.Equal(inspection.Diagnostics.Count, 0, "Inherited configuration is valid");
            Check.Sequence(inspection.Parsed!.FileNames()!.Select(path => path.ToUnicode()).ToArray(), [source], "Genuine discovery returns the absolute Unicode filename");
            Project project = Project.LoadConfiguration(config);
            Check.Sequence(project.RootFileNames, [source], "Checking retains parsed root identity");
            Check.Equal(project.RootFiles[0].FileName(), source, "Parsed SourceFile preserves Unicode disk identity");
            Check.Equal(project.UserFiles.Single().FileName(), source, "Public checking user filename stays Unicode");
            Check.Equal(project.RootFiles[0].FileNameGoBytes(), inspection.Parsed.FileNames()!.Single().AsString(), "Source algorithms retain the exact original byte filename");
            var graph = Project.InspectResolvedGraph(config);
            Check.Equal(graph.SourceFiles.Single().FileName(), inspection.Parsed.FileNames()!.Single().AsString(), "Independent graph inspection keeps its byte filename role");
            Check.Equal(project.Check().Count, 0, "Discovered absolute root checks successfully");
            File.WriteAllText(source, "export const value: number = \"bad\";\n");
            var assignment = Project.LoadConfiguration(config).Check().Single(diagnostic => diagnostic.Code() == 2322);
            Check.Equal(assignment.File().FileName(), source, "Assignment diagnostic owner preserves public Unicode filename");
            File.WriteAllText(Path.Combine(fixture, "outside.ts"), "export const value = 1;\n");
            File.WriteAllText(config, """{"extends":"./base.json","files":["../outside.ts"]}""");
            inspection = Project.InspectConfiguration(config);
            Check.Equal(inspection.Diagnostics.Count, 0, "Full interpretation permits outside-root discovery");
            Check.Sequence(inspection.Parsed!.FileNames()!.Select(path => path.ToUnicode()).ToArray(), [Path.Combine(fixture, "outside.ts")], "Outside filename remains unchanged by interpretation");
            // Resolution contract R5 intentionally removes the former config
            // root containment restriction. The named legacy entry remains bounded.
            Project outside = Project.LoadConfiguration(config);
            Check.Equal(outside.RootFiles.Single().FileName(), Path.Combine(fixture, "outside.ts"), "Checking preserves a genuinely discovered outside-root filename");
            Check.Equal(outside.Check().Count, 0, "Supported outside-root source checks after truthful graph loading");

            string legacy = Path.Combine(root, "tscheck.json");
            File.WriteAllText(legacy, new JsonObject { ["files"] = new JsonArray(source) }.ToJsonString());
            try
            {
                _ = Project.Load(legacy);
                throw new InvalidOperationException("Legacy absolute root input must still reject.");
            }
            catch (ProjectException error)
            {
                Check.Equal(error.Feature, "path", "Legacy relative-input policy remains explicit");
            }

            File.WriteAllText(config, """{"compilerOptions":{"target":"bogus"},"files":["../outside.ts"]}""");
            try
            {
                _ = Project.LoadConfiguration(config);
                throw new InvalidOperationException("Invalid configuration must produce its actual diagnostics first.");
            }
            catch (ConfigurationDiagnosticsException error)
            {
                Check.True(error.Diagnostics.Any(diagnostic => diagnostic.Code() == 6046), "Configuration diagnostics precede checker/root admission");
            }
        }
        finally
        {
            Directory.Delete(fixture, true);
        }
    }
}
