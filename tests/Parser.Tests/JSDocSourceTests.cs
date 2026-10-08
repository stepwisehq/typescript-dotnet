using System.Text;
using System.Text.Json;
using TypeScript.Scanner.ParserValidation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public class JSDocSourceTests
{
    private static Response Probe(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "corpus", "helpers-requests.jsonl");
        var request = File.ReadLines(path).Select(line => JsonSerializer.Deserialize(line, ContractJson.Default.Request)!)
            .Single(request => request.Name == name);
        return Runner.Run(request);
    }

    [Fact]
    public void JSDocTypeSourceSurvivesReparse()
    {
        var result = Probe(nameof(JSDocTypeSourceSurvivesReparse));
        Assert.Equal(2, result.TextProbes.Count);
        foreach (var probe in result.TextProbes)
            Assert.Equal("(\n\"a\" |\n\"b\"\n)[]", Encoding.UTF8.GetString(Convert.FromBase64String(probe.TextBase64)));
    }

    [Fact]
    public void JSDocTypeSourcePropagatesToConstructedReparse()
    {
        var result = Probe(nameof(JSDocTypeSourcePropagatesToConstructedReparse));
        var probe = Assert.Single(result.TextProbes);
        Assert.Equal("{\nvalue: string\n}", Encoding.UTF8.GetString(Convert.FromBase64String(probe.TextBase64)));
        // Offset of the inner opening brace in the pinned upstream source text.
        Assert.Equal(15, probe.TokenPos);
    }

    [Fact]
    public void JSDocTokenPositionCanIncludeLeadingComment()
    {
        Assert.Equal(0, Assert.Single(Probe("JSDocTokenPositionIncludesComment-True").TextProbes).TokenPos);
        Assert.Equal(9, Assert.Single(Probe("JSDocTokenPositionIncludesComment-False").TextProbes).TokenPos);
    }
}
