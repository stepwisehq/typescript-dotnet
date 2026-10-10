using Program.Checks;
namespace Protocols.Checks;
public static class ProtocolMapperChecks
{
    public static IReadOnlyList<string> Cases { get; } = [
        "TestCompositeProjectContentMapperSupplementalRoots",
        "TestContentMapperInvalidMappings",
        "TestContentMapperSourceFileState",
        "TestContentMapperDirectivesPreserveIncrementalGlobals"];
    public static void Run(string name)
    {
        if (!Cases.Contains(name)) throw new ArgumentOutOfRangeException(nameof(name));
        CompilerMapperChecks.Run(name);
    }
}
