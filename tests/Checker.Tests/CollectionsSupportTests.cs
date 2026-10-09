using TypeScript.Checking.CollectionsValidation;
using Xunit;

namespace TypeScript.Checking.Tests;
public sealed class CollectionsSupportTests
{
    public static IEnumerable<object[]> Cases() => CollectionSupportChecks.Cases.Select(name => new object[] { name });
    [Theory, MemberData(nameof(Cases))]
    public void ExplicitlyLabelledSelectedSupportAssertionsAreSharedWithNative(string name) => CollectionSupportChecks.Run(name);
}
