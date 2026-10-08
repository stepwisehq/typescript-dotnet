// Authored exact differential witnesses for previously unobserved syntax diagnostics.
using System.Text.Json;
using TypeScript.Scanner.ParserValidation;

namespace TypeScript.Foundation;
public static class DiagnosticGapChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["foundation-diagnostic-switch-context", "foundation-diagnostic-decorator-related", "foundation-diagnostic-private-name"];

    public static void Run(string name)
    {
        using var requestsResource = typeof(DiagnosticGapChecks).Assembly.GetManifestResourceStream("TypeScript.Foundation.DiagnosticGapRequests.jsonl") ?? throw new InvalidOperationException("Missing requests");
        using var expectedResource = typeof(DiagnosticGapChecks).Assembly.GetManifestResourceStream("TypeScript.Foundation.DiagnosticGapExpected.jsonl") ?? throw new InvalidOperationException("Missing expected graphs");
        using var requests = new StreamReader(requestsResource);
        using var expected = new StreamReader(expectedResource);
        while (requests.ReadLine()is { } input)
        {
            string output = expected.ReadLine() ?? throw new InvalidOperationException("Incomplete expected graphs");
            var request = JsonSerializer.Deserialize(input, ContractJson.Default.Request) ?? throw new InvalidOperationException("Invalid request");
            if (request.Name != name)
                continue;
            var actual = JsonSerializer.SerializeToElement(Runner.Run(request), ContractJson.Default.Response);
            using var want = JsonDocument.Parse(output);
            if (!JsonElement.DeepEquals(want.RootElement, actual))
                throw new InvalidOperationException(name + ": full original diagnostic witness graph differs");
            return;
        }

        throw new ArgumentException(name);
    }
}
