using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class BreadthFirstSearchTests
{
    [Fact]
    public void TestBreadthFirstSearchParallel() => BreadthFirstSearchChecks.TestBreadthFirstSearchParallel();
}
