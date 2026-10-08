using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TypeScript.Scanner;

if (args.Length != 3) throw new ArgumentException("Usage: Scanner.Benchmarks FILE_LIST SAMPLE_MS WARMUP_MS");
var sources = File.ReadAllLines(args[0]).Select(File.ReadAllBytes).ToArray();
long inputBytes = sources.Sum(x => (long)x.Length);
if (inputBytes == 0) throw new ArgumentException("Empty benchmark workload");
int sampleMs = int.Parse(args[1]), warmupMs = int.Parse(args[2]);
var scanner = new Scanner();
const ulong Offset = 14695981039346656037UL, Prime = 1099511628211UL;

(ulong checksum, long tokens) Pass()
{
    ulong checksum = Offset;
    long tokens = 0;
    foreach (var source in sources)
    {
        scanner.Reset();
        scanner.SetText(source);
        for (int guard = 0; guard < source.Length * 3 + 10; guard++)
        {
            Kind kind = scanner.Scan();
            unchecked
            {
                checksum = (checksum ^ (ulong)kind) * Prime;
                checksum = (checksum ^ (ulong)scanner.TokenEnd()) * Prime;
                checksum = (checksum ^ (ulong)scanner.TokenFlags()) * Prime;
            }
            tokens++;
            if (kind == Kind.EndOfFile) break;
            if (guard == source.Length * 3 + 9) throw new InvalidOperationException("No EOF within progress guard");
        }
    }
    return (checksum, tokens);
}

var expected = Pass();
long warmupStart = Stopwatch.GetTimestamp();
while (Stopwatch.GetElapsedTime(warmupStart).TotalMilliseconds < warmupMs)
    if (Pass() != expected) throw new InvalidOperationException("Warmup checksum changed");

GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
long processAllocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
long threadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
long started = Stopwatch.GetTimestamp(), iterations = 0;
double elapsed;
do
{
    if (Pass() != expected) throw new InvalidOperationException("Measured checksum changed");
    iterations++;
    elapsed = Stopwatch.GetElapsedTime(started).TotalSeconds;
} while (elapsed * 1000 < sampleMs);
long processAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - processAllocatedBefore;
long threadAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - threadAllocatedBefore;
var result = new Result("csharp-native-aot", "reset-settext-scan", inputBytes, sources.Length, iterations, expected.tokens,
    expected.checksum.ToString("x16"), elapsed, processAllocatedBytes, threadAllocatedBytes,
    GC.CollectionCount(0) - gc0, GC.CollectionCount(1) - gc1, GC.CollectionCount(2) - gc2, System.Runtime.GCSettings.IsServerGC, Environment.Version.ToString());
Console.WriteLine(JsonSerializer.Serialize(result, BenchmarkJson.Default.Result));

internal sealed record Result(string Implementation, string Mode, long InputBytes, int InputFiles, long Iterations, long TokensPerPass,
    string Checksum, double ElapsedSeconds, long AllocatedBytes, long ThreadAllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections, bool ServerGc, string RuntimeVersion);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Result))]
internal partial class BenchmarkJson : JsonSerializerContext;
