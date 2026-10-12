// Copyright (c) Microsoft Corporation; original whole-function dispatch.
namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static readonly string[] LinuxNames = [
        nameof(TestFanotifyCrossWatcherSameFs),
        nameof(TestLinuxFanotifyBackendSelection),
        nameof(TestLinuxFanotifyMarkENODEVTagged),
        nameof(TestLinuxFanotifyMaybeWrapUnsupportedFilesystem),
        nameof(TestLinuxFanotifyParseDfidNameRoundTrip),
        nameof(TestLinuxFanotifyShutdownBeforeStart),
        nameof(TestLinuxFanotifySubscribeCleansUpAfterMarkFailure),
        nameof(TestLinuxFanotifyUnsupportedTagSurvivesDirWatchError),
    ];
    public static int RunLinuxAll()
    {
        foreach (string name in LinuxNames)
            RunLinux(name);
        return LinuxNames.Length;
    }

    public static void RunLinux(string name)
    {
        try
        {
            switch (name)
            {
                case nameof(TestFanotifyCrossWatcherSameFs):
                    TestFanotifyCrossWatcherSameFs();
                    break;
                case nameof(TestLinuxFanotifyBackendSelection):
                    TestLinuxFanotifyBackendSelection();
                    break;
                case nameof(TestLinuxFanotifyMarkENODEVTagged):
                    TestLinuxFanotifyMarkENODEVTagged();
                    break;
                case nameof(TestLinuxFanotifyMaybeWrapUnsupportedFilesystem):
                    TestLinuxFanotifyMaybeWrapUnsupportedFilesystem();
                    break;
                case nameof(TestLinuxFanotifyParseDfidNameRoundTrip):
                    TestLinuxFanotifyParseDfidNameRoundTrip();
                    break;
                case nameof(TestLinuxFanotifyShutdownBeforeStart):
                    TestLinuxFanotifyShutdownBeforeStart();
                    break;
                case nameof(TestLinuxFanotifySubscribeCleansUpAfterMarkFailure):
                    TestLinuxFanotifySubscribeCleansUpAfterMarkFailure();
                    break;
                case nameof(TestLinuxFanotifyUnsupportedTagSurvivesDirWatchError):
                    TestLinuxFanotifyUnsupportedTagSurvivesDirWatchError();
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
