// Bounded parser benchmark adapter; accepted parser sources remain unchanged.
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TypeScript.Scanner;

if (args.Length != 3)
    throw new ArgumentException("Usage: Parser.Benchmarks WORKLOAD_JSON SAMPLE_MS WARMUP_MS");
var workload = JsonSerializer.Deserialize(File.ReadAllText(args[0]), BenchmarkJson.Default.Workload) ?? throw new ArgumentException("Missing workload");
var sources = workload.Files.Select(item => new Source(new(item.FileName, item.FileName, new(item.ExternalModuleJSX, item.ExternalModuleForce)), File.ReadAllBytes(item.Path), (ScriptKind)item.ScriptKind)).ToArray();
long inputBytes = sources.Sum(source => (long)source.Text.Length);
if (inputBytes == 0 || sources.Length == 0)
    throw new ArgumentException("Empty benchmark workload");
int sampleMs = int.Parse(args[1]), warmupMs = int.Parse(args[2]);
if (sampleMs <= 0 || warmupMs < 0)
    throw new ArgumentException("Invalid duration");
const ulong Offset = 14695981039346656037UL, Prime = 1099511628211UL;
static void Mix(ref ulong checksum, long value)
{
    unchecked
    {
        checksum = (checksum ^ (ulong)value) * Prime;
    }
}

static long ObserveDiagnostics(ref ulong checksum, List<Diagnostic>? diagnostics, int channel)
{
    Mix(ref checksum, channel);
    Mix(ref checksum, diagnostics?.Count ?? 0);
    if (diagnostics is not null)
        foreach (var diagnostic in diagnostics)
        {
            Mix(ref checksum, diagnostic.Code());
            Mix(ref checksum, diagnostic.Pos());
            Mix(ref checksum, diagnostic.End());
            Mix(ref checksum, (int)diagnostic.Category());
        }

    return diagnostics?.Count ?? 0;
}

Signature Pass()
{
    ulong checksum = Offset;
    long nodes = 0, identifiers = 0, texts = 0, parseDiagnostics = 0, jsDiagnostics = 0, jsDocDiagnostics = 0;
    foreach (var source in sources)
    {
        // Public raw-byte input conversion is included. Lazy TS JSDoc is not forced.
        var file = Parser.ParseSourceFile(source.Options, source.Text, source.Kind);
        Mix(ref checksum, (int)file.Kind);
        Mix(ref checksum, file.Pos());
        Mix(ref checksum, file.End());
        Mix(ref checksum, (uint)file.Flags);
        Mix(ref checksum, file.NodeCount);
        Mix(ref checksum, file.IdentifierCount);
        Mix(ref checksum, file.TextCount);
        nodes += file.NodeCount;
        identifiers += file.IdentifierCount;
        texts += file.TextCount;
        parseDiagnostics += ObserveDiagnostics(ref checksum, file.Diagnostics(), 0);
        jsDiagnostics += ObserveDiagnostics(ref checksum, file.JSDiagnostics(), 1);
        jsDocDiagnostics += ObserveDiagnostics(ref checksum, file.JSDocDiagnostics(), 2);
        GC.KeepAlive(file);
    }

    return new(checksum, nodes, identifiers, texts, parseDiagnostics, jsDiagnostics, jsDocDiagnostics);
}

var expected = Pass();
long warmupStart = Stopwatch.GetTimestamp();
while (Stopwatch.GetElapsedTime(warmupStart).TotalMilliseconds < warmupMs)
    if (Pass() != expected)
        throw new InvalidOperationException("Warmup observer changed");
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
long threadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
long started = Stopwatch.GetTimestamp(), iterations = 0;
double elapsed;
do
{
    if (Pass() != expected)
        throw new InvalidOperationException("Measured observer changed");
    iterations++;
    elapsed = Stopwatch.GetElapsedTime(started).TotalSeconds;
}
while (elapsed * 1000 < sampleMs);
long allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
long threadAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - threadAllocatedBefore;
var result = new Result("csharp-native-aot", "parse-source-file-plus-counters-and-diagnostics", inputBytes, sources.Length, iterations, expected.Nodes, expected.Identifiers, expected.Texts, expected.ParseDiagnostics, expected.JsDiagnostics, expected.JsDocDiagnostics, expected.Checksum.ToString("x16"), elapsed, allocatedBytes, threadAllocatedBytes, GC.CollectionCount(0) - gc0, GC.CollectionCount(1) - gc1, GC.CollectionCount(2) - gc2, System.Runtime.GCSettings.IsServerGC, Environment.Version.ToString());
Console.WriteLine(JsonSerializer.Serialize(result, BenchmarkJson.Default.Result));
internal sealed record Workload(List<InputFile> Files);
internal sealed record InputFile(string Path, string FileName, int ScriptKind, bool ExternalModuleJSX, bool ExternalModuleForce);
internal sealed record Source(SourceFileParseOptions Options, byte[] Text, ScriptKind Kind);
internal readonly record struct Signature(ulong Checksum, long Nodes, long Identifiers, long Texts, long ParseDiagnostics, long JsDiagnostics, long JsDocDiagnostics);
internal sealed record Result(string Implementation, string Mode, long InputBytes, int InputFiles, long Iterations, long NodesPerPass, long IdentifiersPerPass, long TextsPerPass, long ParseDiagnosticsPerPass, long JsDiagnosticsPerPass, long JsDocDiagnosticsPerPass, string Checksum, double ElapsedSeconds, long AllocatedBytes, long ThreadAllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections, bool ServerGc, string RuntimeVersion);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Workload))]
[JsonSerializable(typeof(Result))]
internal partial class BenchmarkJson : JsonSerializerContext;
