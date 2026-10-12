using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectBulkCacheTests
{
    [Fact]
    public void Original() => ProjectBulkCacheChecks.TestBulkCacheInvalidation();
}
