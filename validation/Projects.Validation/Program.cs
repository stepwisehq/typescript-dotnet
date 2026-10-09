using System.Text.Json;
using TypeScript.Checking.ProjectsValidation;
using TypeScript.Projects;

if (args.SequenceEqual(["--controls"]))
{
    foreach (string name in ProjectSupportChecks.Cases)
        ProjectSupportChecks.Run(name);
    foreach (string name in ProjectControls.Cases)
        ProjectControls.Run(name);
    Runner.RunProjectionControl();
    Console.WriteLine(ProjectSupportChecks.Cases.Count + ProjectControls.Cases.Count + 1);
    return;
}

if (args.Length != 0)
    throw new ArgumentException("Expected newline-delimited project requests");
while (Console.ReadLine()is { } line)
{
    var request = JsonSerializer.Deserialize(line, ProjectJson.Default.Request) ?? throw new JsonException("Missing request");
    Console.WriteLine(JsonSerializer.Serialize(Runner.Run(request), ProjectJson.Default.Result));
}
