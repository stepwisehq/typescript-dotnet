using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FileRenameTests
{
    [Theory]
    [MemberData(nameof(OriginalNames))]
    public void Originals(string name) => FileRenameChecks.Run(name);
    public static IEnumerable<object[]> OriginalNames() => FileRenameChecks.Names.Select(name => new object[] { name });
}
