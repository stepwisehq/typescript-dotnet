using System.Text.Json;
using TypeScript.Scanner.Validation;

while (Console.ReadLine() is { } line)
{
    var request = JsonSerializer.Deserialize(line, ContractJson.Default.Request) ?? throw new JsonException("Missing request");
    Console.WriteLine(JsonSerializer.Serialize(Runner.Run(request), ContractJson.Default.Response));
}
