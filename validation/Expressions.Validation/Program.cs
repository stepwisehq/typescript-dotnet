using System.Text.Json;
using Declarations.Checks;

namespace Expressions.Validation;

internal static class Entry
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            string? line;
            while ((line = Console.ReadLine()) is not null)
                Console.WriteLine(global::Program.Checks.ProgramRunner.Run(line));
            return;
        }
        if (args.Length != 1) throw new ArgumentException("Expected one expression validation mode.");
        if (args[0] is "--checker-checks" or "--mapper-semantic-checks" or "--external-semantic-boundary-checks" or "--controls")
        {
            int count = 0;
            if (args[0] is "--checker-checks" or "--controls")
                foreach (string name in CheckerOriginalChecks.Cases) { CheckerOriginalChecks.Run(name); count++; }
            if (args[0] is "--mapper-semantic-checks" or "--controls")
                foreach (string name in MapperSemanticChecks.Cases) { MapperSemanticChecks.Run(name); count++; }
            if (args[0] is "--external-semantic-boundary-checks" or "--controls")
                foreach (string name in ExternalSemanticBoundaryChecks.Cases) { ExternalSemanticBoundaryChecks.Run(name); count++; }
            Console.WriteLine(count);
            return;
        }
        if (args[0] != "--registry") throw new ArgumentException("Unknown expression validation mode.");
        using var writer = new Utf8JsonWriter(Console.OpenStandardOutput());
        writer.WriteStartObject();
        writer.WriteNumber("count", CheckerOriginalChecks.Cases.Count + MapperSemanticChecks.Cases.Count + ExternalSemanticBoundaryChecks.Cases.Count);
        writer.WriteStartArray("groups");
        Group("--checker-checks", "Expressions.Tests.CheckerOriginalTests", CheckerOriginalChecks.Cases);
        Group("--mapper-semantic-checks", "Expressions.Tests.MapperSemanticTests", MapperSemanticChecks.Cases);
        Group("--external-semantic-boundary-checks", "Expressions.Tests.ExternalSemanticBoundaryTests", ExternalSemanticBoundaryChecks.Cases);
        writer.WriteEndArray();
        writer.WriteStartObject("xUnitClassCounts");
        writer.WriteNumber("Expressions.Tests.CheckerOriginalTests", CheckerOriginalChecks.Cases.Count);
        writer.WriteNumber("Expressions.Tests.MapperSemanticTests", MapperSemanticChecks.Cases.Count);
        writer.WriteNumber("Expressions.Tests.ExternalSemanticBoundaryTests", ExternalSemanticBoundaryChecks.Cases.Count);
        writer.WriteEndObject();
        writer.WriteEndObject();

        void Group(string flag, string className, IReadOnlyList<string> names)
        {
        writer.WriteStartObject();
        writer.WriteString("flag", flag);
        writer.WriteNumber("count", names.Count);
        writer.WriteStartArray("classNames");
        writer.WriteStringValue(className);
        writer.WriteEndArray();
        writer.WriteStartArray("names");
        foreach (string name in names) writer.WriteStringValue(name);
        writer.WriteEndArray();
        writer.WriteEndObject();
        }
    }
}
