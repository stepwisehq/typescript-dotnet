// The original retained mapper fixtures/assertions are unchanged. This group
// exercises their restored syntactic + semantic + Program diagnostic channels.
namespace Declarations.Checks;

public static class MapperSemanticChecks
{
    public static IReadOnlyList<string> Cases { get; } =
        ["TestCompositeProjectContentMapperSupplementalRoots", "TestContentMapperInvalidMappings", "TestContentMapperSourceFileState"];

    public static void Run(string name)
    {
        if (!Cases.Contains(name)) throw new ArgumentOutOfRangeException(nameof(name));
        global::Program.Checks.CompilerMapperChecks.Run(name);
    }
}
