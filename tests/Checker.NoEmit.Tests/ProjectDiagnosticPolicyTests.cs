using System.IO.Compression;
using System.Text.Json.Nodes;
using TypeScript.Compiler;
using TypeScript.Scanner;
using Xunit;
using TsScanner = TypeScript.Scanner.Scanner;

namespace Checker.NoEmit.Tests;

// Ordinary disk adapter regression for compiler/program.go:GetDiagnosticsOfAnyProgram.
// Complete diagnostic references use the pinned function with genuine libraries.
public sealed class ProjectDiagnosticPolicyTests
{
    private static readonly string[] Names =
    [
        "library-binding-skipped", "library-binding-reported",
        "binding-ignore-directives", "binding-without-directives",
        "syntax-stops-semantics", "invalid-config-stops-entry",
        "program-options-stop-semantics", "noemit-override-enables-semantics",
        "listfilesonly-stops-checking", "globals-stop-semantics"
    ];

    [Theory]
    [InlineData("library-binding-skipped")]
    [InlineData("library-binding-reported")]
    [InlineData("binding-ignore-directives")]
    [InlineData("binding-without-directives")]
    [InlineData("syntax-stops-semantics")]
    [InlineData("invalid-config-stops-entry")]
    [InlineData("program-options-stop-semantics")]
    [InlineData("noemit-override-enables-semantics")]
    [InlineData("listfilesonly-stops-checking")]
    [InlineData("globals-stop-semantics")]
    public void OrdinaryAggregationMatchesPinnedGo(string name)
    {
        var row = ReadCases()[name];
        string directory = Path.Combine(Path.GetTempPath(), "tscheck-diagnostic-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var request = row["request"]!.AsObject();
            var paths = WriteFiles(request, directory);
            var overrides = new CompilerOptions();
            if (request["NoEmit"]?.GetValue<bool>() == true)
                overrides.NoEmit = Tristate.True;
            if (request["ListFilesOnly"]?.GetValue<bool>() == true)
                overrides.ListFilesOnly = Tristate.True;
            IReadOnlyList<Diagnostic> diagnostics;
            try
            {
                var project = Project.LoadConfiguration(paths[request["ConfigPath"]!.GetValue<string>()], overrides);
                diagnostics = project.Check();
            }
            catch (ConfigurationDiagnosticsException error)
            {
                // Source config errors are published before this authored disk
                // API enters checking; no checker exception is accepted here.
                diagnostics = error.Diagnostics;
            }
            var actual = new JsonObject
            {
                ["ExitCode"] = diagnostics.Count == 0 ? 0 : 1,
                ["Error"] = null,
                ["Diagnostics"] = Describe(diagnostics)
            };
            var expected = row["expected"]!["Result"]!.DeepClone();
            RerootReferenceFiles(expected, paths);
            Assert.True(JsonNode.DeepEquals(expected, actual),
                "Complete ordinary diagnostic result differs: " + name + "\n" + actual.ToJsonString());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void NoEmitOverridePreservesDiscoveredFiles()
    {
        var request = ReadCases()["noemit-override-enables-semantics"]["request"]!.AsObject();
        string directory = Path.Combine(Path.GetTempPath(), "tscheck-noemit-override-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var paths = WriteFiles(request, directory);
            string configPath = paths[request["ConfigPath"]!.GetValue<string>()];
            var configured = Project.LoadConfiguration(configPath);
            var overridden = Project.LoadConfiguration(configPath, new CompilerOptions { NoEmit = Tristate.True });
            Assert.Equal(configured.RootFiles.Select(file => file.FileName()), overridden.RootFiles.Select(file => file.FileName()));
            Assert.Equal(configured.SourceFiles.AsSpan().ToArray().Select(file => file.FileName()), overridden.SourceFiles.AsSpan().ToArray().Select(file => file.FileName()));
            Assert.Equal(new[] { 5096 }, configured.Check().Select(diagnostic => diagnostic.Code()));
            Assert.Equal(new[] { 2322 }, overridden.Check().Select(diagnostic => diagnostic.Code()));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static Dictionary<string, JsonObject> ReadCases()
    {
        using var resource = typeof(ProjectDiagnosticPolicyTests).Assembly.GetManifestResourceStream(
            "Checker.NoEmit.Tests.ProjectDiagnosticPolicyCases")
            ?? throw new InvalidOperationException("Pinned ordinary diagnostic references are missing.");
        using var reader = new StreamReader(new GZipStream(resource, CompressionMode.Decompress));
        Dictionary<string, JsonObject> cases = new(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var row = JsonNode.Parse(line)!.AsObject();
            string name = row["request"]!["Name"]!.GetValue<string>();
            Assert.Equal(name, row["expected"]!["Name"]!.GetValue<string>());
            cases.Add(name, row);
        }
        Assert.Equal(Names.Order(StringComparer.Ordinal), cases.Keys.Order(StringComparer.Ordinal));
        return cases;
    }

    private static Dictionary<string, string> WriteFiles(JsonObject request, string directory)
    {
        Dictionary<string, string> paths = new(StringComparer.Ordinal);
        foreach (var pair in request["Files"]!.AsObject())
        {
            // Explicit virtual-root -> isolated disk-root fixture adaptation.
            Assert.StartsWith("/", pair.Key);
            string path = Path.Combine(directory, pair.Key[1..]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, pair.Value!.GetValue<string>());
            paths.Add(pair.Key, path);
        }
        return paths;
    }

    private static void RerootReferenceFiles(JsonNode? node, Dictionary<string, string> paths)
    {
        if (node is JsonObject value)
        {
            if (value["File"] is { } file)
                value["File"] = paths[file.GetValue<string>()];
            foreach (var item in value.ToArray())
                RerootReferenceFiles(item.Value, paths);
        }
        else if (node is JsonArray list)
            foreach (var item in list)
                RerootReferenceFiles(item, paths);
    }

    private static JsonArray Describe(Slice<Diagnostic> diagnostics) => Describe(diagnostics.AsSpan().ToArray());

    private static JsonArray Describe(IEnumerable<Diagnostic> diagnostics)
    {
        var result = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            var file = diagnostic.File();
            int line = 0, column = 0;
            if (file is not null && diagnostic.Pos() >= 0)
            {
                var location = TsScanner.GetECMALineAndUTF16CharacterOfPosition(file, diagnostic.Pos());
                line = location.Line + 1;
                column = location.Character + 1;
            }
            result.Add((JsonNode)new JsonObject
            {
                ["File"] = file?.FileName(),
                ["Code"] = diagnostic.Code(),
                ["Category"] = diagnostic.Category().ToString(),
                ["Message"] = diagnostic.String(),
                ["ByteStart"] = diagnostic.Pos(), ["ByteEnd"] = diagnostic.End(),
                ["Line"] = line, ["Column"] = column,
                ["Chain"] = !diagnostic.MessageChain().IsNil ? Describe(diagnostic.MessageChain()) : null,
                ["Related"] = !diagnostic.RelatedInformation().IsNil ? Describe(diagnostic.RelatedInformation()) : null
            });
        }
        return result;
    }
}
