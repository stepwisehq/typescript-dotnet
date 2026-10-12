namespace LanguageServices.Checks;
public static class ServerOriginalChecks
{
    private static readonly (string Name, Action Run)[] Originals =
    [
        ("TestCompletionAfterFileClose", ServerIntegrationChecks.TestCompletionAfterFileClose),
        ("TestCompletionWithConcurrentFileClose", ServerIntegrationChecks.TestCompletionWithConcurrentFileClose),
        ("TestCompletionForUnopenedFile", ServerIntegrationChecks.TestCompletionForUnopenedFile),
        ("TestAutoImportCompletionForUnopenedFile", ServerIntegrationChecks.TestAutoImportCompletionForUnopenedFile),
        ("TestCompletionSnapshotFreezing", ServerIntegrationChecks.TestCompletionSnapshotFreezing),
        ("TestSetContentMapperContributionsBeforeDidOpen", ServerIntegrationChecks.TestSetContentMapperContributionsBeforeDidOpen),
        ("TestFlakyDiagnosticTrackingParallelEmit", ServerIntegrationChecks.TestFlakyDiagnosticTrackingParallelEmit),
        ("TestCompletionItemResolveRejectsRelativeFileName", ServerInternalChecks.TestCompletionItemResolveRejectsRelativeFileName),
        ("TestCompletionItemResolveRejectsMalformedDynamicFileName", ServerInternalChecks.TestCompletionItemResolveRejectsMalformedDynamicFileName),
        ("TestContentMapperLoggerRequiresTrace", ServerInternalChecks.TestContentMapperLoggerRequiresTrace),
        ("TestParseContentMapperContributions", ServerInternalChecks.TestParseContentMapperContributions),
        ("TestParseContentMapperContributionsRejectsConflictingInlineMappers", ServerInternalChecks.TestParseContentMapperContributionsRejectsConflictingInlineMappers),
        ("TestParseContentMapperContributionsUsesCaseInsensitiveExtensions", ServerInternalChecks.TestParseContentMapperContributionsUsesCaseInsensitiveExtensions),
        ("TestParseContentMapperContributionsDefaultsOptionsToObject", ServerInternalChecks.TestParseContentMapperContributionsDefaultsOptionsToObject),
        ("TestServerShutdownNoDeadlock", ServerLifecycleChecks.TestServerShutdownNoDeadlock),
        ("TestServerOutgoingQueueDoesNotBlockWithoutWriter", ServerLifecycleChecks.TestServerOutgoingQueueDoesNotBlockWithoutWriter),
        ("TestWriteLoopRecoversFromUnserializableResponse", ServerLifecycleChecks.TestWriteLoopRecoversFromUnserializableResponse),
        ("TestProgressNotificationsEndToEnd", ServerIntegrationChecks.TestProgressNotificationsEndToEnd),
        ("TestInitializeCodeActionKinds", ServerIntegrationChecks.TestInitializeCodeActionKinds),
        ("TestProjectInfoConfiguredProject", ServerIntegrationChecks.TestProjectInfoConfiguredProject),
        ("TestProjectInfoInferredProject", ServerIntegrationChecks.TestProjectInfoInferredProject),
        ("TestReferencesAfterAncestorProjectConfigDeletion1", ServerIntegrationChecks.TestReferencesAfterAncestorProjectConfigDeletion1),
        ("TestSemanticTokensCRLF", ServerIntegrationChecks.TestSemanticTokensCRLF),
        ("TestSemanticTokensDefaultLibraryCaseInsensitive", ServerIntegrationChecks.TestSemanticTokensDefaultLibraryCaseInsensitive),
    ];
    public static IEnumerable<object[]> Cases() => Originals.Select(static row => new object[] { row.Name });
    public static void RunOriginal(string name)
    {
        foreach (var row in Originals)
            if (row.Name == name) { row.Run(); return; }
        throw new ArgumentException("Unknown original LSP function", nameof(name));
    }
    public static int RunAll()
    {
        foreach (var row in Originals) row.Run();
        return Originals.Length;
    }
}
