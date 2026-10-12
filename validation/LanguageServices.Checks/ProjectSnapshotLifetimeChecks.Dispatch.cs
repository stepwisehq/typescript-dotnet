namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    public static void TestSnapshot()
    {
        List<Exception>? failures = null;
        void Run(string name, Action assertions)
        {
            try
            {
                assertions();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(name, error));
            }
        }

        Run("creates and removes synthetic programs", SyntheticPrograms);
        Run("failed API update is not adopted", FailedAPIUpdateNotAdopted);
        Run("failed API update preserves flushed host changes", RejectedAPIStillFlushesHostChanges);
        Run("compilerHost gets frozen with snapshot's FS only once", CompilerHostFrozenOnlyOnce);
        Run("cached disk files are cleaned up", CachedDiskFilesCleanedUp);
        Run("GetFile returns nil for non-existent files", NonexistentFiles);
        Run("program change loads node_modules dependency and auto-imports includes it", ProgramChangeLoadsNodeModules);
        Run("fallback rebuild with recomputed parse options is safe for later clone", RecomputedParseOptions);
        Run("auto-import snapshot is adopted when session snapshot is unchanged", AutoImportSnapshotAdopted);
        Run("no-op watch change does not rebuild program", NoOpWatchPreservesProgram);
        if (failures is not null)
            throw new AggregateException("original TestSnapshot subtests failed", failures);
    }

    public static int RunAll()
    {
        TestSnapshot();
        TestProjectIDNarrowing();
        TestRefCountingCaches();
        return 3;
    }
}
