using Emit.Checks;
using Xunit;

namespace Emit.Tests;
public sealed class EsTransformTests
{
    public static IEnumerable<object[]> RuntimeCases => EsRuntimeChecks.Names.Select(name => new object[] { name });
    public static IEnumerable<object[]> ClassFieldsCases => ClassFieldsChecks.Names.Select(name => new object[] { name });
    public static IEnumerable<object[]> DecoratorCases => EsDecoratorChecks.Names.Select(name => new object[] { name });

    [Theory, MemberData(nameof(RuntimeCases))]
    public void AuthoredRuntime(string name) => EsRuntimeChecks.Run(name);
    [Theory, MemberData(nameof(ClassFieldsCases))]
    public void AuthoredClassFields(string name) => ClassFieldsChecks.Run(name);
    [Theory, MemberData(nameof(DecoratorCases))]
    public void AuthoredDecorators(string name) => EsDecoratorChecks.Run(name);
    [Fact]
    public void OriginalOrderedSet() => Assert.Equal(1, OrderedSetChecks.RunAll());
    [Fact]
    public void AuthoredOrderedSetHintedBehavior() => Assert.Equal(1, OrderedSetChecks.RunAuthoredControls());
}
