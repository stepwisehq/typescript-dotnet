using System.Text.Json;
using TypeScript.Scanner.BinderValidation;

if (args.SequenceEqual(new[] { "--self-test" }))
{
    foreach (string name in SelfTests.Run())
        Console.WriteLine(name);
    return;
}

if (args.Length != 0)
    throw new ArgumentException("Expected no arguments or --self-test");
while (Console.ReadLine()is { } line)
{
    var request = JsonSerializer.Deserialize(line, BinderJson.Default.Request) ?? throw new JsonException("Missing request");
    Console.WriteLine(JsonSerializer.Serialize(Runner.Run(request), BinderJson.Default.Result));
}
