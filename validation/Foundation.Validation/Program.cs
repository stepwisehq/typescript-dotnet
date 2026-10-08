using System.Diagnostics;
using System.Text.Json;
using TypeScript.Foundation;

if (args.Length == 1 && args[0] == "--fuzz")
{
    var clock = Stopwatch.StartNew();
    int passed = 0, skipped = 0;
    foreach (var row in ParserFuzzChecks.Cases())
    {
        try
        {
            if (ParserFuzzChecks.Run((string)row[0], (string)row[1], (string)row[2], (bool)row[3], (bool)row[4], (int)row[5]))
                passed++;
            else
                skipped++;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(row[0] + ": " + error);
            Environment.ExitCode = 1;
            return;
        }
    }

    if (passed == 0)
        throw new InvalidOperationException("No finite original parser seeds executed");
    using var result = new Utf8JsonWriter(Console.OpenStandardOutput());
    result.WriteStartObject();
    result.WriteNumber("originalFiniteFuzzSeeds", passed);
    result.WriteNumber("skipped", skipped);
    result.WriteNumber("elapsedSeconds", clock.Elapsed.TotalSeconds);
    result.WriteEndObject();
    result.Flush();
    return;
}

// The same assertion bodies are referenced by xUnit and this native executable.
// A failing original assertion aborts; a selected case is never counted as passed
// merely because parsing/scanning did not throw.
var cases = ScannerChecks.Cases.Select(name => (Subsystem: "scanner", Name: name)).Concat(ParserAstChecks.Cases.Select(name => (Subsystem: "parser-ast", Name: name))).Concat(SyntaxServiceChecks.Cases.Select(name => (Subsystem: "syntax-service", Name: name))).Concat(CloneGraphChecks.Cases.Select(name => (Subsystem: "clone-graph", Name: name))).Concat(DiagnosticGapChecks.Cases.Select(name => (Subsystem: "diagnostic-gap", Name: name))).ToArray();
if (args.Length == 1 && args[0] == "--list")
{
    foreach (var entry in cases)
        Console.WriteLine(entry.Subsystem + ":" + entry.Name);
    return;
}

if (args.Length == 1 && args[0] == "--counts")
{
    using var counts = new Utf8JsonWriter(Console.OpenStandardOutput());
    counts.WriteStartObject();
    counts.WriteNumber("cases", cases.Length);
    foreach (var group in cases.GroupBy(c => c.Subsystem))
        counts.WriteNumber(group.Key, group.Count());
    counts.WriteEndObject();
    counts.Flush();
    return;
}

if (args.Length > 1)
    throw new ArgumentException("Expected zero arguments, --list, or subsystem:case");
var selected = args.Length == 0 ? cases : cases.Where(entry => entry.Subsystem + ":" + entry.Name == args[0]).ToArray();
if (selected.Length == 0)
    throw new ArgumentException("Unknown foundation case: " + args[0]);
var stopwatch = Stopwatch.StartNew();
foreach (var entry in selected)
{
    try
    {
        if (entry.Subsystem == "scanner")
            ScannerChecks.Run(entry.Name);
        else if (entry.Subsystem == "parser-ast")
            ParserAstChecks.Run(entry.Name);
        else if (entry.Subsystem == "syntax-service")
            SyntaxServiceChecks.Run(entry.Name);
        else if (entry.Subsystem == "clone-graph")
            CloneGraphChecks.Run(entry.Name);
        else
            DiagnosticGapChecks.Run(entry.Name);
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(entry.Subsystem + ":" + entry.Name + ": " + error);
        Environment.ExitCode = 1;
        return;
    }
}

stopwatch.Stop();
using var output = new Utf8JsonWriter(Console.OpenStandardOutput());
output.WriteStartObject();
output.WriteNumber("cases", selected.Length);
output.WriteNumber("scannerCases", selected.Count(entry => entry.Subsystem == "scanner"));
output.WriteNumber("parserAstCases", selected.Count(entry => entry.Subsystem == "parser-ast"));
output.WriteNumber("syntaxServiceCases", selected.Count(entry => entry.Subsystem == "syntax-service"));
output.WriteNumber("cloneGraphCases", selected.Count(entry => entry.Subsystem == "clone-graph"));
output.WriteNumber("diagnosticGapCases", selected.Count(entry => entry.Subsystem == "diagnostic-gap"));
output.WriteNumber("elapsedSeconds", stopwatch.Elapsed.TotalSeconds);
output.WriteEndObject();
output.Flush();
