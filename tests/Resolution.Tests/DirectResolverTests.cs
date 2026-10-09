using Resolution.Checks;
using Configuration.Checks;
using Xunit;

namespace Resolution.Tests;
public sealed class StaticResolverTests
{
    public static IEnumerable<object[]> Cases => StaticResolverChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => StaticResolverChecks.Run(name);
}

public sealed class ResolverBasicTests
{
    public static IEnumerable<object[]> Cases => ResolverBasicChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ResolverBasicChecks.Run(name);
}

public sealed class ResolverConcurrencyTests
{
    public static IEnumerable<object[]> Cases => ResolverConcurrencyChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ResolverConcurrencyChecks.Run(name);
}

public sealed class ResolutionDataTests
{
    public static IEnumerable<object[]> Cases => ResolutionDataChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ResolutionDataChecks.Run(name);
}

public sealed class ModuleCandidateTests
{
    public static IEnumerable<object[]> Cases => ModuleCandidateChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ModuleCandidateChecks.Run(name);
}

public sealed class ModulePackageRootTests
{
    public static IEnumerable<object[]> Cases => ModulePackageRootChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ModulePackageRootChecks.Run(name);
}

public sealed class ResolverInternalTests
{
    public static IEnumerable<object[]> Cases => ResolverInternalChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ResolverInternalChecks.Run(name);
}

public sealed class ResolverDynamicTests
{
    public static IEnumerable<object[]> Cases => ResolverDynamicChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ResolverDynamicChecks.Run(name);
}

public sealed class ResolverEntrypointTests
{
    public static IEnumerable<object[]> Cases => ResolverEntrypointChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ResolverEntrypointChecks.Run(name);
}

public sealed class ResolutionDependencyTests
{
    public static IEnumerable<object[]> Cases => ResolutionDependencyChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void AuthoredSourceControls(string name) => ResolutionDependencyChecks.Run(name);
}

public sealed class GraphByteRoleTests
{
    public static IEnumerable<object[]> Cases => GraphByteRoleChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void AuthoredSourceControl(string name) => GraphByteRoleChecks.Run(name);
}
