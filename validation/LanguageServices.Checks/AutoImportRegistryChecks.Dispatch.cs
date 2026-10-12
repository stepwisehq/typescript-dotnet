// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// All five original registry_test.go functions and all20 actual subtest bodies.
// Go T.Parallel hints are sequential; explicit production concurrency remains real.
namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    public static readonly string[] Names = ["TestRegistryLifecycle", "TestContentMappedNodeModulesFileUsesProjectBucket", "TestHiddenDirectoriesInNodeModules", "TestAutoImportEntrypointDirectorySearch", "TestUpdateIndexesConcurrentMapSafety"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestRegistryLifecycle":
                TestRegistryLifecycle();
                break;
            case "TestContentMappedNodeModulesFileUsesProjectBucket":
                TestContentMappedNodeModulesFileUsesProjectBucket();
                break;
            case "TestHiddenDirectoriesInNodeModules":
                TestHiddenDirectoriesInNodeModules();
                break;
            case "TestAutoImportEntrypointDirectorySearch":
                TestAutoImportEntrypointDirectorySearch();
                break;
            case "TestUpdateIndexesConcurrentMapSafety":
                TestUpdateIndexesConcurrentMapSafety();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static void TestRegistryLifecycle()
    {
        // Preserve Go t.Run sibling execution and aggregate whole-function failure.
        List<Exception>? failures = null;
        RunRegistryGroup("LifecycleBuildsBuckets", LifecycleBuildsBuckets, ref failures);
        RunRegistryGroup("LifecycleSameFileChange", LifecycleSameFileChange, ref failures);
        RunRegistryGroup("LifecycleNewProgramFiles", LifecycleNewProgramFiles, ref failures);
        RunRegistryGroup("LifecycleDependencyChange", LifecycleDependencyChange, ref failures);
        RunRegistryGroup("LifecycleClosedProjectBuckets", LifecycleClosedProjectBuckets, ref failures);
        RunRegistryGroup("LifecycleDeletedNodeModules", LifecycleDeletedNodeModules, ref failures);
        RunRegistryGroup("LifecycleDeletedModulesWithPackageChange", LifecycleDeletedModulesWithPackageChange, ref failures);
        RunRegistryGroup("LifecycleDeletedPackageDirectory", LifecycleDeletedPackageDirectory, ref failures);
        RunRegistryGroup("LifecycleOpenFileDependencySelection", LifecycleOpenFileDependencySelection, ref failures);
        RunRegistryGroup("LifecycleResolvedPackagesAcrossProjects", LifecycleResolvedPackagesAcrossProjects, ref failures);
        RunRegistryGroup("LifecycleSymlinkedSourceChange", LifecycleSymlinkedSourceChange, ref failures);
        RunRegistryGroup("LifecyclePnpmWorkspaceGranularity", LifecyclePnpmWorkspaceGranularity, ref failures);
        RunRegistryGroup("LifecycleCircularWorkspaceSymlinks", LifecycleCircularWorkspaceSymlinks, ref failures);
        RunRegistryGroup("LifecycleFileExcludePatterns", LifecycleFileExcludePatterns, ref failures);
        RunRegistryGroup("LifecycleRealpathDedupAcrossBuckets", LifecycleRealpathDedupAcrossBuckets, ref failures);
        if (failures != null)
            throw new AggregateException("original TestRegistryLifecycle groups failed", failures);
    }

    private static void RunRegistryGroup(string name, Action assertions, ref List<Exception>? failures)
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
}
