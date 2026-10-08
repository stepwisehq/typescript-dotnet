using System.Text.Json;
using TypeScript.Checking.Validation;

if (args.SequenceEqual(new[] { "--utilities" }))
{
    Console.WriteLine(JsonSerializer.Serialize(Utilities.Run(), CheckerJson.Default.ListUtilityObservation));
    return;
}

if (args.SequenceEqual(new[] { "--self-tests" }))
{
    Console.WriteLine(JsonSerializer.Serialize(SelfTests.Run(), CheckerJson.Default.ListUtilityObservation));
    return;
}

if (args.Length != 0)
    throw new ArgumentException("Expected no arguments, --utilities or --self-tests");
while (Console.ReadLine()is { } line)
{
    var request = JsonSerializer.Deserialize(line, CheckerJson.Default.Request) ?? throw new JsonException("Missing request");
    Console.WriteLine(request.AdmissionOnly ? JsonSerializer.Serialize(Runner.Admission(request), CheckerJson.Default.AdmissionResult) : JsonSerializer.Serialize(Runner.Run(request), CheckerJson.Default.Result));
}
