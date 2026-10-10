using System.Text.Json;
using Program.Checks;

namespace Program.Validation;

internal static class Entry
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            string? line;
            while ((line = Console.ReadLine()) is not null) Console.WriteLine(ProgramRunner.Run(line));
            return;
        }
        if (args.Length != 1) throw new ArgumentException("Expected one Program validation mode.");
        if (args[0] != "--registry") { Console.WriteLine(ProgramCheckRegistry.Run(args[0])); return; }
        using var writer = new Utf8JsonWriter(Console.OpenStandardOutput());
        writer.WriteStartObject();
        writer.WriteNumber("count", ProgramCheckRegistry.Groups.Sum(group => group.Cases.Count));
        writer.WriteStartArray("groups");
        foreach (var group in ProgramCheckRegistry.Groups)
        {
            writer.WriteStartObject();
            writer.WriteString("flag", group.Flag);
            writer.WriteNumber("count", group.Cases.Count);
            writer.WriteStartArray("classNames");
            writer.WriteStringValue(group.ClassName);
            writer.WriteEndArray();
            writer.WriteStartArray("names");
            foreach (string name in group.Cases) writer.WriteStringValue(name);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteStartObject("xUnitClassCounts");
        foreach (var group in ProgramCheckRegistry.Groups) writer.WriteNumber(group.ClassName, group.Cases.Count);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
