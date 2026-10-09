using Configuration.Checks;
using System.Text.Json.Nodes;

if (args is ["--registry"])
{
    JsonArray rows = [];
    JsonObject classes = [];
    foreach (var group in ConfigurationCheckRegistry.Groups)
    {
        rows.Add((JsonNode)new JsonObject
        {
            ["flag"] = group.Flag, ["count"] = group.Cases.Count,
            ["classNames"] = new JsonArray((JsonNode)JsonValue.Create(group.ClassName)!)
        });
        classes[group.ClassName] = group.Cases.Count;
    }
    Console.WriteLine(new JsonObject
    {
        ["count"] = ConfigurationCheckRegistry.Groups.Sum(group => group.Cases.Count),
        ["groups"] = rows, ["xUnitClassCounts"] = classes
    }.ToJsonString());
    return 0;
}
if (args is ["--controls"])
{
    foreach (var group in ConfigurationCheckRegistry.Groups)
        foreach (string name in group.Cases) group.Run(name);
    Console.WriteLine(ConfigurationCheckRegistry.Groups.Sum(group => group.Cases.Count));
    return 0;
}
if (args is ["--check", var selectedFlag, var selectedName])
{
    var selected = ConfigurationCheckRegistry.Groups.Single(group => group.Flag == selectedFlag);
    if (!selected.Cases.Contains(selectedName)) throw new InvalidOperationException("Unknown registered original assertion case.");
    selected.Run(selectedName);
    Console.WriteLine(1);
    return 0;
}
if (args.Length == 1)
{
    var group = ConfigurationCheckRegistry.Groups.FirstOrDefault(group => group.Flag == args[0]);
    if (group is not null)
    {
        foreach (string name in group.Cases) group.Run(name);
        Console.WriteLine(group.Cases.Count);
        return 0;
    }
}
if (args.Length != 0)
{
    Console.Error.WriteLine("Unknown configuration validation mode.");
    return 2;
}
for (string? line; (line = Console.ReadLine()) is not null;)
    Console.WriteLine(ConfigurationRunner.Run(line));
return 0;
