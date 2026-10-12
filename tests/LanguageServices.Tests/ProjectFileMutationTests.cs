using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectFileMutationTests
{
    public static IEnumerable<object[]> Names => ProjectFileMutationChecks.Names.Select(static name => new object[] { name });

    [Theory, MemberData(nameof(Names))]
    public void AuthoredRequiredMutations(string name) => ProjectFileMutationChecks.Run(name);
}
