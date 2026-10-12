// Copyright (c) Microsoft Corporation. All rights reserved.
// All16 original lspwatcher_test.go assertion functions, shared xUnit/Native.
namespace LanguageServices.Checks;
public static partial class LspWatcherChecks
{
    public static readonly string[] Names = [nameof(TestWatcher_CreateChangeDelete), nameof(TestWatcher_KindFilter), nameof(TestRootFromGlob), nameof(TestWatchRootFromRelativePattern), nameof(TestWatcher_BookkeepingAndOverflow), nameof(TestWatcher_NonRecursiveGlobIsNotRecursive), nameof(TestWatcher_RealBackend_MissingThenCreate), nameof(TestWatcher_MissingDirectoryTracksAncestor), nameof(TestWatcher_MissingDirectoryPromotesOnCreate), nameof(TestWatcher_MultiLevelDescend), nameof(TestWatcher_AtomicTreeCreateRace), nameof(TestWatcher_SyntheticCreateDepth), nameof(TestWatcher_TerminatedFallsBackAndRecovers), nameof(TestWatcher_GenuineFailureRollsBackForRetry), nameof(TestWatcher_WatchTerminatedDoesNotDropEvents), nameof(TestWatcher_CloseWhileWatchFilesReconciles), ];
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
            case nameof(TestWatcher_CreateChangeDelete):
                TestWatcher_CreateChangeDelete();
                break;
            case nameof(TestWatcher_KindFilter):
                TestWatcher_KindFilter();
                break;
            case nameof(TestRootFromGlob):
                TestRootFromGlob();
                break;
            case nameof(TestWatchRootFromRelativePattern):
                TestWatchRootFromRelativePattern();
                break;
            case nameof(TestWatcher_BookkeepingAndOverflow):
                TestWatcher_BookkeepingAndOverflow();
                break;
            case nameof(TestWatcher_NonRecursiveGlobIsNotRecursive):
                TestWatcher_NonRecursiveGlobIsNotRecursive();
                break;
            case nameof(TestWatcher_RealBackend_MissingThenCreate):
                TestWatcher_RealBackend_MissingThenCreate();
                break;
            case nameof(TestWatcher_MissingDirectoryTracksAncestor):
                TestWatcher_MissingDirectoryTracksAncestor();
                break;
            case nameof(TestWatcher_MissingDirectoryPromotesOnCreate):
                TestWatcher_MissingDirectoryPromotesOnCreate();
                break;
            case nameof(TestWatcher_MultiLevelDescend):
                TestWatcher_MultiLevelDescend();
                break;
            case nameof(TestWatcher_AtomicTreeCreateRace):
                TestWatcher_AtomicTreeCreateRace();
                break;
            case nameof(TestWatcher_SyntheticCreateDepth):
                TestWatcher_SyntheticCreateDepth();
                break;
            case nameof(TestWatcher_TerminatedFallsBackAndRecovers):
                TestWatcher_TerminatedFallsBackAndRecovers();
                break;
            case nameof(TestWatcher_GenuineFailureRollsBackForRetry):
                TestWatcher_GenuineFailureRollsBackForRetry();
                break;
            case nameof(TestWatcher_WatchTerminatedDoesNotDropEvents):
                TestWatcher_WatchTerminatedDoesNotDropEvents();
                break;
            case nameof(TestWatcher_CloseWhileWatchFilesReconciles):
                TestWatcher_CloseWhileWatchFilesReconciles();
                break;
            default:
                throw new ArgumentException(name);
        }
    }
}
