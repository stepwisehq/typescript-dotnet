using System.Text.Json.Nodes;

namespace Program.Checks;

// Four authored source-selected controls, complete fresh pinned-Go Program observations.
// checker/grammarchecks.go:792 iterates Go runes with strings.ContainsFunc.
public static class ArrowRuneChecks
{
    public static readonly string[] Names = ["arrow-u2028", "arrow-u2029", "arrow-lf", "arrow-space"];
    public static Dictionary<string, JsonObject> Read(Stream resource) => RegexByteArgumentChecks.Read(resource, Names);
    public static void Run(string resourcePath)
    {
        using var input = File.OpenRead(resourcePath);
        var rows = Read(input);
        foreach (string name in Names) RegexByteArgumentChecks.Check(rows[name]);
        Console.WriteLine("arrow-rune-checks: 4 passed");
    }
}
