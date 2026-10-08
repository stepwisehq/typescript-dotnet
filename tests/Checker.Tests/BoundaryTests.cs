using System.Text.Json;
using TypeScript.Checking.Validation;
using TypeScript.Scanner;
using Xunit;

namespace TypeScript.Checking.Tests;
public class BoundaryTests
{
    public static IEnumerable<object? []> Controls()
    {
        foreach (string path in new[]
        {
            "corpus/admission-controls.json",
            "object-corpus/admission-controls.json"
        }

        )
        {
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, path)));
            foreach (var input in json.RootElement.EnumerateArray())
            {
                // Exactly two historical negatives became supported. Their original
                // bytes/evidence remain unchanged; object positives replace them.
                if (path.StartsWith("corpus/", StringComparison.Ordinal) && (input.GetProperty("name").GetString()is "object" or "object-type"))
                    continue;
                yield return[input.GetProperty("name").GetString()!, input.GetRawText(), input.GetProperty("expectedFeature").GetString()];
            }
        }
    }

    [Theory, MemberData(nameof(Controls))]
    public void ExplicitAdmissionIsIndependentOfGoSemanticAgreement(string name, string json, string? feature)
    {
        var request = JsonSerializer.Deserialize(json, CheckerJson.Default.Request)!;
        var actual = Runner.Admission(request);
        Assert.True(actual.Admitted == (feature is null), name + ": unexpected admission result");
        Assert.Equal(feature, actual.Feature);
        if (feature is not null)
        {
            Assert.NotNull(actual.Kind);
            Assert.InRange(actual.Pos!.Value, -1, System.Text.Encoding.UTF8.GetByteCount(request.Text));
            Assert.InRange(actual.End!.Value, actual.Pos.Value, System.Text.Encoding.UTF8.GetByteCount(request.Text));
        }
    }

    [Fact]
    public void PublicQueriesRejectForeignNodesAndPreserveEnvironmentAndIdentity() => Assert.All(SelfTests.Run(), item => Assert.True(item.BoolValue));
    [Fact]
    public void ActualGoPagedStoresAndLazyTablesMatch()
    {
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "corpus/utility-expected.json")));
        string actual = JsonSerializer.Serialize(Utilities.Run(), CheckerJson.Default.ListUtilityObservation);
        using var parsed = JsonDocument.Parse(actual);
        Assert.Null(OracleTests.FirstDifference(expected.RootElement, parsed.RootElement, "$", 0));
    }
}
