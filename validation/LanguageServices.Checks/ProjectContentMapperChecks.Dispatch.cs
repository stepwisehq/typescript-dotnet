// Whole original project/contentmapper_test.go function dispatch, not row credit.
namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    public static int RunAll()
    {
        List<Exception>? failures = null;
        (string Name, Action Run)[] tests = [
            (nameof(TestContentMapperProjectWithoutMappedFiles), TestContentMapperProjectWithoutMappedFiles),
            (nameof(TestContentMapperParallelFileLoading), TestContentMapperParallelFileLoading),
            (nameof(TestContentMapperInProject), TestContentMapperInProject),
            (nameof(TestContentMapperPackageManifestChangeReloadsConfig), TestContentMapperPackageManifestChangeReloadsConfig),
            (nameof(TestContentMapperSupplementalFileClonedOnEdit), TestContentMapperSupplementalFileClonedOnEdit),
            (nameof(TestContentMapperModuleExtensionClonedOnUnrelatedEdit), TestContentMapperModuleExtensionClonedOnUnrelatedEdit),
            (nameof(TestContentMapperLocaleChange), TestContentMapperLocaleChange),
            (nameof(TestDynamicContentMapperInProject), TestDynamicContentMapperInProject),
            (nameof(TestDynamicContentMapperRefreshesForMixedWatchBatches), TestDynamicContentMapperRefreshesForMixedWatchBatches),
            (nameof(TestUnusedDynamicContentMapperIsNotOpened), TestUnusedDynamicContentMapperIsNotOpened),
            (nameof(TestContentMappersInParallelProjectReferences), TestContentMappersInParallelProjectReferences),
            (nameof(TestContentMapperOpenFileExcludedByConfigChange), TestContentMapperOpenFileExcludedByConfigChange),
            (nameof(TestContentMapperRemovalWithOpenFile), TestContentMapperRemovalWithOpenFile),
            (nameof(TestContentMapperProcessSharedAcrossProjects), TestContentMapperProcessSharedAcrossProjects),
            (nameof(TestContentMapperInferredProjectUsesExtensionContributions), TestContentMapperInferredProjectUsesExtensionContributions),
            (nameof(TestContentMapperInferredProjectSurvivesTypingsInstall), TestContentMapperInferredProjectSurvivesTypingsInstall),
            (nameof(TestContentMapperCreatedFileAdoptedByConfiguredProject), TestContentMapperCreatedFileAdoptedByConfiguredProject),
        ];
        foreach (var test in tests)
        {
            try
            {
                test.Run();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(test.Name, error));
            }
        }

        if (failures is not null)
            throw new AggregateException(failures);
        return tests.Length;
    }

    public static void RunOriginal(int index) => RunOriginal(index, null);
    public static void RunOriginal(string name) => RunOriginal(null, name);
    private static void RunOriginal(int? index, string? name)
    {
        (string Name, Action Run)[] tests = [
            (nameof(TestContentMapperProjectWithoutMappedFiles), TestContentMapperProjectWithoutMappedFiles),
            (nameof(TestContentMapperParallelFileLoading), TestContentMapperParallelFileLoading),
            (nameof(TestContentMapperInProject), TestContentMapperInProject),
            (nameof(TestContentMapperPackageManifestChangeReloadsConfig), TestContentMapperPackageManifestChangeReloadsConfig),
            (nameof(TestContentMapperSupplementalFileClonedOnEdit), TestContentMapperSupplementalFileClonedOnEdit),
            (nameof(TestContentMapperModuleExtensionClonedOnUnrelatedEdit), TestContentMapperModuleExtensionClonedOnUnrelatedEdit),
            (nameof(TestContentMapperLocaleChange), TestContentMapperLocaleChange),
            (nameof(TestDynamicContentMapperInProject), TestDynamicContentMapperInProject),
            (nameof(TestDynamicContentMapperRefreshesForMixedWatchBatches), TestDynamicContentMapperRefreshesForMixedWatchBatches),
            (nameof(TestUnusedDynamicContentMapperIsNotOpened), TestUnusedDynamicContentMapperIsNotOpened),
            (nameof(TestContentMappersInParallelProjectReferences), TestContentMappersInParallelProjectReferences),
            (nameof(TestContentMapperOpenFileExcludedByConfigChange), TestContentMapperOpenFileExcludedByConfigChange),
            (nameof(TestContentMapperRemovalWithOpenFile), TestContentMapperRemovalWithOpenFile),
            (nameof(TestContentMapperProcessSharedAcrossProjects), TestContentMapperProcessSharedAcrossProjects),
            (nameof(TestContentMapperInferredProjectUsesExtensionContributions), TestContentMapperInferredProjectUsesExtensionContributions),
            (nameof(TestContentMapperInferredProjectSurvivesTypingsInstall), TestContentMapperInferredProjectSurvivesTypingsInstall),
            (nameof(TestContentMapperCreatedFileAdoptedByConfiguredProject), TestContentMapperCreatedFileAdoptedByConfiguredProject),
        ];
        if (index is { } selected)
            tests[selected].Run();
        else
        {
            foreach (var test in tests)
                if (test.Name == name)
                {
                    test.Run();
                    return;
                }
            throw new ArgumentException("Unknown original contentmapper assertion: " + name, nameof(name));
        }
    }
}
