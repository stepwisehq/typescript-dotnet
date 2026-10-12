namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    public static void TestSession()
    {
        // Go t.Run retains independent sibling execution after an assertion failure.
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

        Run("DidOpenFileOriginalAssertions", DidOpenFileOriginalAssertions);
        Run("WatchChangeAndDidOpenSameBatchOriginalAssertions", WatchChangeAndDidOpenSameBatchOriginalAssertions);
        Run("DidChangeFileOriginalAssertions", DidChangeFileOriginalAssertions);
        Run("DidCloseFileOriginalAssertions", DidCloseFileOriginalAssertions);
        Run("DidSaveFileOriginalAssertions", DidSaveFileOriginalAssertions);
        Run("SourceFileSharingOriginalAssertions", SourceFileSharingOriginalAssertions);
        Run("WatchedOpenAndClosedOriginalAssertions", WatchedOpenAndClosedOriginalAssertions);
        Run("WatchedNonRootProgramFileOriginalAssertions", WatchedNonRootProgramFileOriginalAssertions);
        Run("WatchedConfigDiagnosticsOriginalAssertions", WatchedConfigDiagnosticsOriginalAssertions);
        Run("WatchedDeletionOriginalAssertions", WatchedDeletionOriginalAssertions);
        Run("WatchedProgramOnlyDirectoryDeletionOriginalAssertions", WatchedProgramOnlyDirectoryDeletionOriginalAssertions);
        Run("WatchedSiblingDirectoryRefreshOriginalAssertions", WatchedSiblingDirectoryRefreshOriginalAssertions);
        Run("WatchedCreationOriginalAssertions", WatchedCreationOriginalAssertions);
        Run("WatchedIrrelevantExtensionsOriginalAssertions", WatchedIrrelevantExtensionsOriginalAssertions);
        Run("WatchedPnpmLinkOriginalAssertions", WatchedPnpmLinkOriginalAssertions);
        Run("WatchedSymlinkPackageInvalidationOriginalAssertions", WatchedSymlinkPackageInvalidationOriginalAssertions);
        Run("WatchedSymlinkIncludeOriginalAssertions", WatchedSymlinkIncludeOriginalAssertions);
        Run("WatchedRefreshFilteringOriginalAssertions", WatchedRefreshFilteringOriginalAssertions);
        Run("ConfigureRefreshOriginalAssertions", ConfigureRefreshOriginalAssertions);
        Run("ConfigureLocaleOriginalAssertions", ConfigureLocaleOriginalAssertions);
        Run("ConfigureWarningsRefreshOriginalAssertions", ConfigureWarningsRefreshOriginalAssertions);
        Run("ConfigurationParsingOriginalAssertions", ConfigurationParsingOriginalAssertions);
        Run("ClosedFileOriginalAssertions", ClosedFileOriginalAssertions);
        Run("JSConfigSelectionOriginalAssertions", JSConfigSelectionOriginalAssertions);
        if (failures is not null)
            throw new AggregateException("original TestSession groups failed", failures);
    }

    public static int RunAll()
    {
        TestSession();
        TestSessionCloseDoesNotBlockOnIdleCacheCleanTimer();
        return 2;
    }
}
