// Copyright (c) Microsoft Corporation; original whole-function dispatch.
namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static readonly string[] BroadNames = [
        nameof(TestAtomicSave),
        nameof(TestAtomicSaveFileWatch),
        nameof(TestConsolidatedChildWatchFiltersAgainstRequestedDir),
        nameof(TestConsolidatedChildWatchIgnoresEventsBeforeSubscribe),
        nameof(TestConsolidatedSymlinkChildMapsSharedLogicalPath),
        nameof(TestConsolidatedSymlinkChildTerminatesFromSharedLogicalPath),
        nameof(TestDefaultBackendMatchesPlatform),
        nameof(TestFastRecursiveWatcherConsolidatesSiblingDirectories),
        nameof(TestFastRecursiveWatcherDoesNotConsolidateSymlinkOutsideRoot),
        nameof(TestFileWatchCreate),
        nameof(TestFileWatchDelete),
        nameof(TestFileWatchDeleteAndRecreate),
        nameof(TestFileWatchIgnoresSiblings),
        nameof(TestFileWatchMultipleSameDir),
        nameof(TestFileWatchNonExistentTarget),
        nameof(TestFileWatchUpdate),
        nameof(TestNonRecursiveAndRecursiveSameDir),
        nameof(TestNonRecursiveDirCreate),
        nameof(TestNonRecursiveFileCreate),
        nameof(TestNonRecursiveFileDelete),
        nameof(TestNonRecursiveFileUpdate),
        nameof(TestNonRecursiveGrandchildIgnored),
        nameof(TestNonRecursiveNewSubdirContentIgnored),
        nameof(TestNonRecursiveWithDeniedSubdir),
        nameof(TestRecreateSubdirAndModify),
        nameof(TestRecursiveMoveInPrePopulated),
        nameof(TestRecursiveSubscribeSymlinkedDirectoryDoesNotFollowDescendantSymlink),
        nameof(TestRecursiveWatchWithIgnoreDoesNotFilterByLogicalRoot),
        nameof(TestRecursiveWithDeniedSubdir),
        nameof(TestRenameDirOutOfTreeNoStaleEvents),
        nameof(TestReplaceDirWithFile),
        nameof(TestReplaceParentDirWithDifferent),
        nameof(TestRoundTripRename),
        nameof(TestSubscribeAppendToFile),
        nameof(TestSubscribeCloseThenReSubscribe),
        nameof(TestSubscribeCoalesceCreateThenDelete),
        nameof(TestSubscribeCoalesceCreateUpdate),
        nameof(TestSubscribeCoalesceDeleteCreateAsUpdate),
        nameof(TestSubscribeCoalesceMultipleUpdates),
        nameof(TestSubscribeCoalesceUpdateDelete),
        nameof(TestSubscribeConcurrentSubscribeUnsubscribe),
        nameof(TestSubscribeDeepNestedCreate),
        nameof(TestSubscribeDirCreate),
        nameof(TestSubscribeDirDelete),
        nameof(TestSubscribeDirRename),
        nameof(TestSubscribeManyFilesAtOnce),
        nameof(TestSubscribeMissingDirError),
        nameof(TestSubscribeMultipleDifferentDirs),
        nameof(TestSubscribeMultipleSameDir),
        nameof(TestSubscribeNestedDirDeletionCleansDescendants),
        nameof(TestSubscribeNoEventsAfterUnsubscribe),
        nameof(TestSubscribeNonASCIIPath),
        nameof(TestSubscribeNotADirError),
        nameof(TestSubscribeRejectsNilCallback),
        nameof(TestSubscribeRejectsRelativePath),
        nameof(TestSubscribeRenameDir),
        nameof(TestSubscribeReplaceFileWithDir),
        nameof(TestSubscribeSubdirCreate),
        nameof(TestSubscribeSubdirDeleteWithFiles),
        nameof(TestSubscribeSubfileCreate),
        nameof(TestSubscribeSubfileDelete),
        nameof(TestSubscribeSubfileRename),
        nameof(TestSubscribeSubfileUpdate),
        nameof(TestSubscribeSymlinkCreate),
        nameof(TestSubscribeSymlinkDelete),
        nameof(TestSubscribeSymlinkedDirectoryRebasesTargetEvents),
        nameof(TestSubscribeTruncateFile),
        nameof(TestSubscribeUnsubscribeIdempotent),
        nameof(TestSubscribeWatchedDirDeleted),
        nameof(TestUnavailableBackendReturnsError),
        nameof(TestWatchDirectoriesBatch),
        nameof(TestWatchFileCreate),
        nameof(TestWatchFileDelete),
        nameof(TestWatchFileRename),
        nameof(TestWatchFileRenameExisting),
        nameof(TestWatchFileUpdate),
    ];
    public static int RunBroadAll()
    {
        foreach (string name in BroadNames)
            RunBroad(name);
        return BroadNames.Length;
    }

    public static void RunBroad(string name)
    {
        try
        {
            switch (name)
            {
                case nameof(TestAtomicSave):
                    TestAtomicSave();
                    break;
                case nameof(TestAtomicSaveFileWatch):
                    TestAtomicSaveFileWatch();
                    break;
                case nameof(TestConsolidatedChildWatchFiltersAgainstRequestedDir):
                    TestConsolidatedChildWatchFiltersAgainstRequestedDir();
                    break;
                case nameof(TestConsolidatedChildWatchIgnoresEventsBeforeSubscribe):
                    TestConsolidatedChildWatchIgnoresEventsBeforeSubscribe();
                    break;
                case nameof(TestConsolidatedSymlinkChildMapsSharedLogicalPath):
                    TestConsolidatedSymlinkChildMapsSharedLogicalPath();
                    break;
                case nameof(TestConsolidatedSymlinkChildTerminatesFromSharedLogicalPath):
                    TestConsolidatedSymlinkChildTerminatesFromSharedLogicalPath();
                    break;
                case nameof(TestDefaultBackendMatchesPlatform):
                    TestDefaultBackendMatchesPlatform();
                    break;
                case nameof(TestFastRecursiveWatcherConsolidatesSiblingDirectories):
                    TestFastRecursiveWatcherConsolidatesSiblingDirectories();
                    break;
                case nameof(TestFastRecursiveWatcherDoesNotConsolidateSymlinkOutsideRoot):
                    TestFastRecursiveWatcherDoesNotConsolidateSymlinkOutsideRoot();
                    break;
                case nameof(TestFileWatchCreate):
                    TestFileWatchCreate();
                    break;
                case nameof(TestFileWatchDelete):
                    TestFileWatchDelete();
                    break;
                case nameof(TestFileWatchDeleteAndRecreate):
                    TestFileWatchDeleteAndRecreate();
                    break;
                case nameof(TestFileWatchIgnoresSiblings):
                    TestFileWatchIgnoresSiblings();
                    break;
                case nameof(TestFileWatchMultipleSameDir):
                    TestFileWatchMultipleSameDir();
                    break;
                case nameof(TestFileWatchNonExistentTarget):
                    TestFileWatchNonExistentTarget();
                    break;
                case nameof(TestFileWatchUpdate):
                    TestFileWatchUpdate();
                    break;
                case nameof(TestNonRecursiveAndRecursiveSameDir):
                    TestNonRecursiveAndRecursiveSameDir();
                    break;
                case nameof(TestNonRecursiveDirCreate):
                    TestNonRecursiveDirCreate();
                    break;
                case nameof(TestNonRecursiveFileCreate):
                    TestNonRecursiveFileCreate();
                    break;
                case nameof(TestNonRecursiveFileDelete):
                    TestNonRecursiveFileDelete();
                    break;
                case nameof(TestNonRecursiveFileUpdate):
                    TestNonRecursiveFileUpdate();
                    break;
                case nameof(TestNonRecursiveGrandchildIgnored):
                    TestNonRecursiveGrandchildIgnored();
                    break;
                case nameof(TestNonRecursiveNewSubdirContentIgnored):
                    TestNonRecursiveNewSubdirContentIgnored();
                    break;
                case nameof(TestNonRecursiveWithDeniedSubdir):
                    TestNonRecursiveWithDeniedSubdir();
                    break;
                case nameof(TestRecreateSubdirAndModify):
                    TestRecreateSubdirAndModify();
                    break;
                case nameof(TestRecursiveMoveInPrePopulated):
                    TestRecursiveMoveInPrePopulated();
                    break;
                case nameof(TestRecursiveSubscribeSymlinkedDirectoryDoesNotFollowDescendantSymlink):
                    TestRecursiveSubscribeSymlinkedDirectoryDoesNotFollowDescendantSymlink();
                    break;
                case nameof(TestRecursiveWatchWithIgnoreDoesNotFilterByLogicalRoot):
                    TestRecursiveWatchWithIgnoreDoesNotFilterByLogicalRoot();
                    break;
                case nameof(TestRecursiveWithDeniedSubdir):
                    TestRecursiveWithDeniedSubdir();
                    break;
                case nameof(TestRenameDirOutOfTreeNoStaleEvents):
                    TestRenameDirOutOfTreeNoStaleEvents();
                    break;
                case nameof(TestReplaceDirWithFile):
                    TestReplaceDirWithFile();
                    break;
                case nameof(TestReplaceParentDirWithDifferent):
                    TestReplaceParentDirWithDifferent();
                    break;
                case nameof(TestRoundTripRename):
                    TestRoundTripRename();
                    break;
                case nameof(TestSubscribeAppendToFile):
                    TestSubscribeAppendToFile();
                    break;
                case nameof(TestSubscribeCloseThenReSubscribe):
                    TestSubscribeCloseThenReSubscribe();
                    break;
                case nameof(TestSubscribeCoalesceCreateThenDelete):
                    TestSubscribeCoalesceCreateThenDelete();
                    break;
                case nameof(TestSubscribeCoalesceCreateUpdate):
                    TestSubscribeCoalesceCreateUpdate();
                    break;
                case nameof(TestSubscribeCoalesceDeleteCreateAsUpdate):
                    TestSubscribeCoalesceDeleteCreateAsUpdate();
                    break;
                case nameof(TestSubscribeCoalesceMultipleUpdates):
                    TestSubscribeCoalesceMultipleUpdates();
                    break;
                case nameof(TestSubscribeCoalesceUpdateDelete):
                    TestSubscribeCoalesceUpdateDelete();
                    break;
                case nameof(TestSubscribeConcurrentSubscribeUnsubscribe):
                    TestSubscribeConcurrentSubscribeUnsubscribe();
                    break;
                case nameof(TestSubscribeDeepNestedCreate):
                    TestSubscribeDeepNestedCreate();
                    break;
                case nameof(TestSubscribeDirCreate):
                    TestSubscribeDirCreate();
                    break;
                case nameof(TestSubscribeDirDelete):
                    TestSubscribeDirDelete();
                    break;
                case nameof(TestSubscribeDirRename):
                    TestSubscribeDirRename();
                    break;
                case nameof(TestSubscribeManyFilesAtOnce):
                    TestSubscribeManyFilesAtOnce();
                    break;
                case nameof(TestSubscribeMissingDirError):
                    TestSubscribeMissingDirError();
                    break;
                case nameof(TestSubscribeMultipleDifferentDirs):
                    TestSubscribeMultipleDifferentDirs();
                    break;
                case nameof(TestSubscribeMultipleSameDir):
                    TestSubscribeMultipleSameDir();
                    break;
                case nameof(TestSubscribeNestedDirDeletionCleansDescendants):
                    TestSubscribeNestedDirDeletionCleansDescendants();
                    break;
                case nameof(TestSubscribeNoEventsAfterUnsubscribe):
                    TestSubscribeNoEventsAfterUnsubscribe();
                    break;
                case nameof(TestSubscribeNonASCIIPath):
                    TestSubscribeNonASCIIPath();
                    break;
                case nameof(TestSubscribeNotADirError):
                    TestSubscribeNotADirError();
                    break;
                case nameof(TestSubscribeRejectsNilCallback):
                    TestSubscribeRejectsNilCallback();
                    break;
                case nameof(TestSubscribeRejectsRelativePath):
                    TestSubscribeRejectsRelativePath();
                    break;
                case nameof(TestSubscribeRenameDir):
                    TestSubscribeRenameDir();
                    break;
                case nameof(TestSubscribeReplaceFileWithDir):
                    TestSubscribeReplaceFileWithDir();
                    break;
                case nameof(TestSubscribeSubdirCreate):
                    TestSubscribeSubdirCreate();
                    break;
                case nameof(TestSubscribeSubdirDeleteWithFiles):
                    TestSubscribeSubdirDeleteWithFiles();
                    break;
                case nameof(TestSubscribeSubfileCreate):
                    TestSubscribeSubfileCreate();
                    break;
                case nameof(TestSubscribeSubfileDelete):
                    TestSubscribeSubfileDelete();
                    break;
                case nameof(TestSubscribeSubfileRename):
                    TestSubscribeSubfileRename();
                    break;
                case nameof(TestSubscribeSubfileUpdate):
                    TestSubscribeSubfileUpdate();
                    break;
                case nameof(TestSubscribeSymlinkCreate):
                    TestSubscribeSymlinkCreate();
                    break;
                case nameof(TestSubscribeSymlinkDelete):
                    TestSubscribeSymlinkDelete();
                    break;
                case nameof(TestSubscribeSymlinkedDirectoryRebasesTargetEvents):
                    TestSubscribeSymlinkedDirectoryRebasesTargetEvents();
                    break;
                case nameof(TestSubscribeTruncateFile):
                    TestSubscribeTruncateFile();
                    break;
                case nameof(TestSubscribeUnsubscribeIdempotent):
                    TestSubscribeUnsubscribeIdempotent();
                    break;
                case nameof(TestSubscribeWatchedDirDeleted):
                    TestSubscribeWatchedDirDeleted();
                    break;
                case nameof(TestUnavailableBackendReturnsError):
                    TestUnavailableBackendReturnsError();
                    break;
                case nameof(TestWatchDirectoriesBatch):
                    TestWatchDirectoriesBatch();
                    break;
                case nameof(TestWatchFileCreate):
                    TestWatchFileCreate();
                    break;
                case nameof(TestWatchFileDelete):
                    TestWatchFileDelete();
                    break;
                case nameof(TestWatchFileRename):
                    TestWatchFileRename();
                    break;
                case nameof(TestWatchFileRenameExisting):
                    TestWatchFileRenameExisting();
                    break;
                case nameof(TestWatchFileUpdate):
                    TestWatchFileUpdate();
                    break;
                default:
                    throw new ArgumentException("unknown original function", nameof(name));
            }
        }
        catch (OriginalSkip skipped)
        {
            Console.WriteLine($"ORIGINAL_SKIP {name}: {skipped.Message}");
        }
    }
}
