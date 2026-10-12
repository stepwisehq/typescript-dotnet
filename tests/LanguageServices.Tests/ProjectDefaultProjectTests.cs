using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectDefaultProjectTests
{
    [Fact]
    public void Original() => ProjectDefaultProjectChecks.TestProjectCollectionDefaultProject();
}
