using System.Text.Json;
using Declarations.Checks;
using TypeSystem.Checks;

namespace TypeSystem.Validation;

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
        if (args.Length != 1) throw new ArgumentException("Expected one type-system validation mode.");
        if (args[0] is "--checker-checks" or "--mapper-semantic-checks" or "--external-semantic-boundary-checks" or "--js-casing-checks" or "--controls")
        {
            int count = 0;
            if (args[0] is "--checker-checks" or "--controls")
                foreach (string name in CheckerOriginalChecks.Cases) { CheckerOriginalChecks.Run(name); count++; }
            if (args[0] is "--mapper-semantic-checks" or "--controls")
                foreach (string name in MapperSemanticChecks.Cases) { MapperSemanticChecks.Run(name); count++; }
            if (args[0] is "--external-semantic-boundary-checks" or "--controls")
                foreach (string name in ExternalSemanticBoundaryChecks.Cases) { ExternalSemanticBoundaryChecks.Run(name); count++; }
            if (args[0] is "--js-casing-checks" or "--controls")
                foreach (string name in JsCasingChecks.Cases) { JsCasingChecks.Run(name); count++; }
            Console.WriteLine(count);
            return;
        }
        if (args[0] != "--registry") throw new ArgumentException("Unknown type-system validation mode.");
        using var writer = new Utf8JsonWriter(Console.OpenStandardOutput());
        writer.WriteStartObject();
        writer.WriteNumber("count", CheckerOriginalChecks.Cases.Count + MapperSemanticChecks.Cases.Count + ExternalSemanticBoundaryChecks.Cases.Count + JsCasingChecks.Cases.Count);
        writer.WriteStartArray("groups");
        Group("--checker-checks", "TypeSystem.Tests.CheckerOriginalTests", CheckerOriginalChecks.Cases);
        Group("--mapper-semantic-checks", "TypeSystem.Tests.MapperSemanticTests", MapperSemanticChecks.Cases);
        Group("--external-semantic-boundary-checks", "TypeSystem.Tests.ExternalSemanticBoundaryTests", ExternalSemanticBoundaryChecks.Cases);
        Group("--js-casing-checks", "TypeSystem.Tests.JsCasingTests", JsCasingChecks.Cases);
        writer.WriteEndArray();
        writer.WriteStartObject("xUnitClassCounts");
        writer.WriteNumber("TypeSystem.Tests.CheckerOriginalTests", CheckerOriginalChecks.Cases.Count);
        writer.WriteNumber("TypeSystem.Tests.MapperSemanticTests", MapperSemanticChecks.Cases.Count);
        writer.WriteNumber("TypeSystem.Tests.ExternalSemanticBoundaryTests", ExternalSemanticBoundaryChecks.Cases.Count);
        writer.WriteNumber("TypeSystem.Tests.JsCasingTests", JsCasingChecks.Cases.Count);
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
