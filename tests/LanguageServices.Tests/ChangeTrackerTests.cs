using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ChangeTrackerTests
{
    [Fact]
    public void TestTextEditsConflictAtSameInsertionPointAcrossProjections() => ChangeTrackerChecks.Run();
}
