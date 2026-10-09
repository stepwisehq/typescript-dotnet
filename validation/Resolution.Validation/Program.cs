using System.Text.Json;
using Resolution.Checks;

if (args.Length == 0)
{
    string? line;
    while ((line = Console.ReadLine()) is not null) Console.WriteLine(ResolutionRunner.Run(line));
    return;
}

if (args.Length != 1)
    throw new ArgumentException("Expected a resolver validation mode.");
if (args[0] == "--registry")
{
    using var writer = new Utf8JsonWriter(Console.OpenStandardOutput());
    writer.WriteStartObject();
    writer.WriteNumber("count", ResolutionChecks.Groups.Sum(group => group.Cases.Count));
    writer.WriteStartArray("groups");
    foreach (ResolutionCheckGroup group in ResolutionChecks.Groups)
    {
        writer.WriteStartObject();
        writer.WriteString("flag", group.Flag);
        writer.WriteNumber("count", group.Cases.Count);
        writer.WriteStartArray("classNames");
        writer.WriteStringValue(group.ClassName);
        writer.WriteEndArray();
        writer.WriteStartArray("originalNames");
        foreach (string name in group.Cases)
            writer.WriteStringValue(name);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    writer.WriteEndArray();
    writer.WriteStartObject("xUnitClassCounts");
    foreach (ResolutionCheckGroup group in ResolutionChecks.Groups)
        writer.WriteNumber(group.ClassName, group.Cases.Count);
    writer.WriteEndObject();
    writer.WriteEndObject();
}
else
    Console.WriteLine(ResolutionChecks.Run(args[0]));
