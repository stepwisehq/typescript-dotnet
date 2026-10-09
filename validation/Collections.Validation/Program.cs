using System.Text.Json;
using TypeScript.Checking.CollectionsValidation;

if (args.SequenceEqual(["--controls"]))
{
    foreach (string name in CollectionControls.Cases)
        CollectionControls.Run(name);
    foreach (string name in CollectionSupportChecks.Cases)
        CollectionSupportChecks.Run(name);
    Console.WriteLine(CollectionControls.Cases.Count + CollectionSupportChecks.Cases.Count);
    return;
}

if (args.Length != 0)
    throw new ArgumentException("Expected newline-delimited Collections requests");
while (Console.ReadLine()is { } line)
{
    var request = JsonSerializer.Deserialize(line, CollectionsJson.Default.Request) ?? throw new JsonException("Missing request");
    Console.WriteLine(JsonSerializer.Serialize(Runner.Run(request), CollectionsJson.Default.Result));
}
