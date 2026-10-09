// Authored registry. Each shared body is called directly by both xUnit and native.
using Configuration.Checks;

namespace Resolution.Checks;
public sealed record ResolutionCheckGroup(string Flag, string ClassName, IReadOnlyList<string> Cases, Action<string> Run);
public static class ResolutionChecks
{
    public static IReadOnlyList<ResolutionCheckGroup> Groups { get; } = [new("--static-checks", "Resolution.Tests.StaticResolverTests", StaticResolverChecks.Cases, StaticResolverChecks.Run), new("--basic-checks", "Resolution.Tests.ResolverBasicTests", ResolverBasicChecks.Cases, ResolverBasicChecks.Run), new("--concurrency-checks", "Resolution.Tests.ResolverConcurrencyTests", ResolverConcurrencyChecks.Cases, ResolverConcurrencyChecks.Run), new("--data-checks", "Resolution.Tests.ResolutionDataTests", ResolutionDataChecks.Cases, ResolutionDataChecks.Run), new("--candidate-checks", "Resolution.Tests.ModuleCandidateTests", ModuleCandidateChecks.Cases, ModuleCandidateChecks.Run), new("--package-root-checks", "Resolution.Tests.ModulePackageRootTests", ModulePackageRootChecks.Cases, ModulePackageRootChecks.Run), new("--internal-checks", "Resolution.Tests.ResolverInternalTests", ResolverInternalChecks.Cases, ResolverInternalChecks.Run), new("--dynamic-checks", "Resolution.Tests.ResolverDynamicTests", ResolverDynamicChecks.Cases, ResolverDynamicChecks.Run), new("--entrypoint-checks", "Resolution.Tests.ResolverEntrypointTests", ResolverEntrypointChecks.Cases, ResolverEntrypointChecks.Run), new("--dependency-checks", "Resolution.Tests.ResolutionDependencyTests", ResolutionDependencyChecks.Cases, ResolutionDependencyChecks.Run), new("--graph-byte-role-checks", "Resolution.Tests.GraphByteRoleTests", GraphByteRoleChecks.Cases, GraphByteRoleChecks.Run)];

    public static int Run(string flag)
    {
        int count = 0;
        HashSet<string> originalNames = [];
        bool matched = false;
        foreach (ResolutionCheckGroup group in Groups)
        {
            if (flag != "--controls" && flag != group.Flag)
                continue;
            matched = true;
            foreach (string name in group.Cases)
            {
                if (!originalNames.Add(name))
                    throw new InvalidOperationException("Duplicate original assertion body in shared registry: " + name);
                group.Run(name);
                count++;
            }
        }

        if (!matched)
            throw new ArgumentException("Unknown resolver shared-check flag.", nameof(flag));
        return count;
    }
}
