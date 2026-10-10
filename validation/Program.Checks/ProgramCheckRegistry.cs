namespace Program.Checks;

public sealed record ProgramCheckGroup(string Flag, string ClassName, IReadOnlyList<string> Cases, Action<string> Run);

public static class ProgramCheckRegistry
{
    public static IReadOnlyList<ProgramCheckGroup> Groups { get; } =
    [
        new("--program-checks", "Program.Tests.ProgramTests", ProgramChecks.Cases, ProgramChecks.Run),
        new("--checker-pool-checks", "Program.Tests.CheckerPoolTests", CheckerPoolChecks.Cases, CheckerPoolChecks.Run),
        new("--cached-vfs-checks", "Program.Tests.CachedVfsTests", CachedVfsChecks.Cases, CachedVfsChecks.Run),
        new("--known-symlink-checks", "Program.Tests.KnownSymlinkTests", KnownSymlinkChecks.Cases, KnownSymlinkChecks.Run),
        new("--sync-map-checks", "Program.Tests.SyncMapTests", SyncMapChecks.Cases, SyncMapChecks.Run),
        new("--ignored-path-checks", "Program.Tests.IgnoredPathTests", IgnoredPathChecks.Cases, IgnoredPathChecks.Run),
        new("--bundled-checks", "Program.Tests.BundledTests", BundledChecks.Cases, BundledChecks.Run),
        new("--span-map-checks", "Program.Tests.SpanMapTests", SpanMapChecks.Names, SpanMapChecks.Run),
        new("--parse-result-checks", "Program.Tests.ParseResultTests", ParseResultChecks.Cases.Select(row => (string)row[0]).ToList(), ParseResultChecks.Run),
        new("--compiler-mapper-checks", "Program.Tests.CompilerMapperTests", CompilerMapperChecks.Cases, CompilerMapperChecks.Run),
        new("--construction-boundary-checks", "Program.Tests.ConstructionBoundaryTests", ConstructionBoundaryChecks.Cases, ConstructionBoundaryChecks.Run)
    ];

    public static int Run(string flag)
    {
        var selected = flag == "--controls" ? Groups : Groups.Where(group => group.Flag == flag).ToList();
        if (selected.Count == 0) throw new ArgumentException("Unknown Program check group: " + flag);
        int total = 0;
        foreach (var group in selected)
            foreach (string name in group.Cases) { group.Run(name); total++; }
        return total;
    }
}
