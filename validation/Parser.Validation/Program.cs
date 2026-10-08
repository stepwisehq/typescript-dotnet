using System.Text.Json;
using TypeScript.Scanner.ParserValidation;

while (Console.ReadLine() is { } line)
{
    var request = JsonSerializer.Deserialize(line, ContractJson.Default.Request) ?? throw new JsonException("Missing parse request");
    Console.WriteLine(JsonSerializer.Serialize(Runner.Run(request), ContractJson.Default.Response));
}
