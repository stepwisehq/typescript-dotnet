using TypeScript.Checking.CollectionsValidation;
using Xunit;

namespace TypeScript.Checker.Tests;
public sealed class CollectionsControlsTests
{
    public static IEnumerable<object[]> Cases => CollectionControls.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SharedCollectionControl(string name) => CollectionControls.Run(name);
}
