using System.Text.Json;
using TypeScript.Binding;
using TypeScript.Scanner.BinderValidation;

if (args.SequenceEqual(new[] { "--support-checks" }))
{
    foreach (string name in BindingSupportChecks.Cases)
    {
        BindingSupportChecks.Run(name);
        Console.WriteLine("PASS " + name);
    }

    Console.WriteLine("TOTAL " + BindingSupportChecks.Cases.Count);
    return;
}

if (args.SequenceEqual(new[] { "--helper-checks" }))
{
    foreach (string name in BindingHelperChecks.Cases)
    {
        BindingHelperChecks.Run(name);
        Console.WriteLine("PASS " + name);
    }

    Console.WriteLine("TOTAL " + BindingHelperChecks.Cases.Count);
    return;
}

if (args.SequenceEqual(new[] { "--counts" }))
{
    Console.WriteLine("{\"support\":" + BindingSupportChecks.Cases.Count + ",\"helpers\":" + BindingHelperChecks.Cases.Count + "}");
    return;
}

if (args.Length != 0)
    throw new ArgumentException("Expected no arguments, --support-checks or --counts");
while (Console.ReadLine()is { } line)
{
    var request = JsonSerializer.Deserialize(line, CompletionJson.Default.CompletionRequest) ?? throw new JsonException("Missing completion request");
    Console.WriteLine(JsonSerializer.Serialize(CompletionRunner.Run(request), CompletionJson.Default.CompletionResult));
}
