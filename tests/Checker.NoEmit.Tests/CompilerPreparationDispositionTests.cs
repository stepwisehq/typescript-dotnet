// Source-qualified original compiler fixture preparation failures at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. These assertions qualify failure
// dispositions, separately from successful original checking/walker records.
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Program.Checks;
using Xunit;

namespace Checker.NoEmit.Tests;

public sealed class CompilerPreparationDispositionTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException(name + " must identify the original preparation input.");

    private static readonly Lazy<Dictionary<string, JsonObject>> Dispositions = new(() =>
    {
        var rows = JsonNode.Parse(File.ReadAllText(Required("CHECKER_NOEMIT_PREPARATION_DISPOSITIONS")))!.AsArray();
        Dictionary<string, JsonObject> result = new(StringComparer.Ordinal);
        foreach (var node in rows)
        {
            var row = node!.AsObject();
            if (row.Count != 6 || row.Any(pair => pair.Key is not
                ("Name" or "Source" or "SourceSha256" or "Configuration" or "stage" or "expectedMessage")))
                throw new InvalidOperationException("Original preparation disposition fields differ.");
            if (row["stage"]!.GetValue<string>() is not ("configuration-expansion" or "option-preparation"))
                throw new InvalidOperationException("Unknown original preparation stage.");
            result.Add(row["Name"]!.GetValue<string>(), row);
        }

        if (result.Count != 33 || result.Values.Count(row => row["stage"]!.GetValue<string>() == "configuration-expansion") != 7)
            throw new InvalidOperationException("Exactly seven expansion and 26 option preparation failures are required.");
        return result;
    });

    private static readonly Lazy<CompilerNoEmitCases> Consumer = new(() => new(Required("CHECKER_NOEMIT_ROOT")));
    public static IEnumerable<object[]> Cases => Dispositions.Value.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalPreparationFailureRemainsAnExactFailure(string name)
    {
        var row = Dispositions.Value[name];
        string source = Path.GetFullPath(row["Source"]!.GetValue<string>());
        byte[] bytes = File.ReadAllBytes(source);
        Assert.Equal(row["SourceSha256"]!.GetValue<string>(), Convert.ToHexStringLower(SHA256.HashData(bytes)));

        InvalidOperationException error;
        if (row["stage"]!.GetValue<string>() == "configuration-expansion")
        {
            // Actual disk decode and original directive expansion occur before
            // any configuration is selected or checker is acquired.
            string decoded = CompilerNoEmitCases.DecodeSource(bytes);
            error = Assert.Throws<InvalidOperationException>(() =>
                CompilerFixtureConfigurations.Expand(CompilerFixtureParser.ExtractCompilerSettings(decoded)));
        }
        else
        {
            JsonObject request = new();
            foreach (string field in new[] { "Name", "Source", "SourceSha256", "Configuration" })
                request[field] = row[field]!.DeepClone();
            error = Assert.Throws<InvalidOperationException>(() => Consumer.Value.Observe(request));
        }

        Assert.Equal(row["expectedMessage"]!.GetValue<string>(), error.Message);
    }
}
