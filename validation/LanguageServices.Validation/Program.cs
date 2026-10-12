using LanguageServices.Checks;

// Candidate backend is opt-in; ordinary direct assertions keep their existing dispatch.
if (args is ["--fourslash-host", var hostname, var portText])
{
    if (!int.TryParse(portText, out int port) || port is < 1 or > 65535)
    {
        Console.Error.WriteLine("Invalid fourslash host-control port.");
        return 2;
    }

    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cancellation.Cancel();
    };
    return LanguageServices.Checks.Fourslash.FourslashCandidateBackend.Run(hostname, port, cancellation.Token);
}

// Isolate an original server assertion when it exercises a process/stack boundary.
if (args is ["--server-original", var originalName])
{
    ServerOriginalChecks.RunOriginal(originalName);
    Console.WriteLine($"Original server assertion {originalName}: passed");
    return 0;
}

if (args is ["--contentmapper-original", var mapperOriginalName])
{
    ProjectContentMapperChecks.RunOriginal(mapperOriginalName);
    Console.WriteLine($"Original contentmapper assertion {mapperOriginalName}: passed");
    return 0;
}

// Deliberately process-fatal; invoke only as an isolated child with a timeout.
if (args is ["--ipc-fatal-notification"])
{
    IpcAsyncChecks.RunFatalNotificationControl();
    return 1;
}

// Run stack-boundary probes in separate processes so a runtime stack failure
// cannot hide the result of the ordinary dependency assertions.
if (args is ["--selection-depth"])
{
    SelectionRangeChecks.TestSelectionRangeDepthIsLimited();
    Console.WriteLine("Original selection-range depth assertions: passed");
    return 0;
}

if (args.Length != 0)
{
    if (args.Length != 3 || args[0] != "--protocol-depth" || !int.TryParse(args[2], out int depth) || depth < 1 || args[1] is not ("model" or "any"))
    {
        Console.Error.WriteLine("Usage: LanguageServices.Validation [--protocol-depth model|any DEPTH | --selection-depth | --ipc-fatal-notification]");
        return 2;
    }

    if (args[1] == "model")
        ProtocolCodecChecks.RunModelDepthBoundary(depth);
    else
        ProtocolCodecChecks.RunAnyDepthBoundary(depth);
    Console.WriteLine($"Protocol {args[1]} depth {depth}: passed");
    return 0;
}

// Authored runner over shared original assertions. This dependency gate does not
// establish service/server completion; transport and corpus gates are separate.
int originalFunctions = ProjectDirtyChecks.RunAll();
originalFunctions += DynamicQueueChecks.RunAll();
originalFunctions += ProgressChecks.RunAll();
originalFunctions += StackSanitizerChecks.RunAll();
originalFunctions += ServerOriginalChecks.RunAll();
originalFunctions += ProjectCacheChecks.RunAll();
originalFunctions += ProjectBackgroundChecks.RunAll();
originalFunctions += ProjectLoggingChecks.RunAll();
originalFunctions += ProtocolFramingChecks.RunAll();
originalFunctions += ProtocolCodecChecks.RunAll();
originalFunctions += AstNavChecks.RunAll();
originalFunctions += AstNavOriginalChecks.RunAll();
originalFunctions += ConverterChecks.RunAll();
originalFunctions += ProjectOverlayChecks.RunAll();
originalFunctions += ProjectSnapshotChecks.RunAll();
originalFunctions += ProjectSnapshotLifetimeChecks.RunAll();
originalFunctions += ProjectWatchChecks.RunAll();
originalFunctions += ProjectAtaChecks.RunAll();
originalFunctions += ProjectCustomConfigChecks.RunAll();
originalFunctions += ProjectContentMapperChecks.RunAll();
originalFunctions += ProjectDefaultProjectChecks.RunAll();
originalFunctions += ProjectExtendedConfigOwnershipChecks.RunAll();
originalFunctions += ProjectBulkCacheChecks.RunAll();
originalFunctions += ProjectConfigChangeChecks.RunAll();
originalFunctions += ProjectLifetimeOriginalChecks.RunAll();
originalFunctions += ProjectRemainingChecks.RunAll();
originalFunctions += ProjectUntitledChecks.RunAll();
originalFunctions += FileRenameChecks.RunAll();
originalFunctions += ModuleSpecifierOriginalChecks.RunAll();
originalFunctions += AtaPackageNameChecks.RunAll();
originalFunctions += AtaInstallChecks.RunAll();
originalFunctions += AtaDiscoveryChecks.RunAll();
originalFunctions += BreadthFirstSearchChecks.RunAll();
originalFunctions += UserPreferenceChecks.RunAll();
originalFunctions += LsUtilityChecks.RunAll();
originalFunctions += FormattingChecks.RunAll();
originalFunctions += ChangeTrackerChecks.RunAll();
originalFunctions += IpcAsyncChecks.RunAll();
originalFunctions += IpcTimingChecks.RunAll();
originalFunctions += ContentMapperHostChecks.RunAll();
originalFunctions += DiagnosticLocaleChecks.RunAll();
originalFunctions += AutoImportOwnerChecks.RunAll();
originalFunctions += AutoImportRealpathChecks.RunAll();
originalFunctions += AutoImportAliasResolverChecks.RunAll();
originalFunctions += AutoImportRegistryChecks.RunAll();
originalFunctions += ProjectCheckerPoolChecks.RunAll();
originalFunctions += LspWatcherChecks.RunAll();
originalFunctions += FsWatchEventChecks.RunAll();
originalFunctions += FsWatchFallbackChecks.RunAll();
originalFunctions += FsWatchWalkDirChecks.RunAll();
originalFunctions += FsWatchPathChecks.RunAll();
originalFunctions += FsWatchInternalChecks.RunAll();
originalFunctions += LanguageServiceFormattingChecks.RunAll();
originalFunctions += FsWatchBackendChecks.RunBroadAll();
originalFunctions += FsWatchBackendChecks.RunLinuxAll();
originalFunctions += ProjectSessionChecks.RunAll();
originalFunctions += FindAllReferencesChecks.RunAll();
originalFunctions += SelectionRangeChecks.RunAll();
originalFunctions += CompletionChecks.RunAll();
Console.WriteLine($"Original language-service dependency test functions executed: {originalFunctions}");
Console.WriteLine($"Adapted original watcher runtime observations executed: {FsWatchBackendChecks.RunAdaptedRuntimeObservation()}");
Console.WriteLine($"Original external normalization dependency test functions executed: {NormalizationChecks.RunAll()}");
Console.WriteLine($"Original external language-matcher dependency test functions executed: {LocaleMatcherChecks.RunAll()}");
// Regex groups include explicitly named selected assertion channels; this is not
// a count of wholly ported upstream test functions.
Console.WriteLine($"External regexp assertion groups executed: {RegexChecks.RunAll()}");
Console.WriteLine($"External regexp syntax assertion groups executed: {RegexSyntaxChecks.RunAll()}");
Console.WriteLine($"Original external regexp compiler test functions executed: {RegexCompileChecks.RunAll()}");
Console.WriteLine($"Selected original RE2 matching channels executed: {RegexRe2Checks.RunAll()}");
Console.WriteLine($"Selected original Fowler matching channels executed: {RegexFowlerChecks.RunAll()}");
Console.WriteLine($"External regexp replacement/index assertion groups executed: {RegexReplacementChecks.RunAll()}");
Console.WriteLine($"Selected original generic replacer channels executed: {GenericReplacerChecks.RunAll()}");
Console.WriteLine($"Original external quoting dependency test functions executed: {CanBackquoteChecks.RunAll()}");
Console.WriteLine($"Original external string-trimming dependency test functions executed: {TrimFuncChecks.RunAll()}");
Console.WriteLine($"Original external slice-predicate dependency test functions executed: {SlicePredicateChecks.RunAll()}");
Console.WriteLine($"External Unicode service assertion groups executed: {UnicodeServiceChecks.RunAll()}");
int normalizationChannels = NormalizationPropertyChecks.RunAll();
normalizationChannels += NormalizationSpanChecks.RunAll();
normalizationChannels += NormalizationStringChannels.RunAll();
normalizationChannels += NormalizationConformanceChecks.RunAll();
normalizationChannels += NormalizationBootstrapChecks.RunAll();
Console.WriteLine($"Selected or adapted original NFD assertion channels executed: {normalizationChannels}");
Console.WriteLine($"Original AST-navigation Go baseline channels executed: {AstNavBaselineChecks.RunAll()}");
Console.WriteLine($"Original AST-navigation token subtests executed: {AstNavChecks.RunOriginalTokenSubtests()}");
int authoredControls = ProjectDirtyChecks.RunAuthoredControls();
authoredControls += ProjectLoggingChecks.RunAuthoredControls();
authoredControls += ConsumedLogFormatChecks.RunAll();
authoredControls += CodeActionKindChecks.RunAll();
authoredControls += GoByteStringSliceChecks.RunAll();
authoredControls += ServiceQueryChecks.RunAll();
authoredControls += ProjectFileMutationChecks.RunAll();
authoredControls += ProjectGraphOwnershipChecks.RunAuthoredControls();
authoredControls += ProtocolJsonAdapterChecks.RunAll();
authoredControls += ProtocolCodecChecks.RunJSONBoundaryControls();
authoredControls += ProtocolCodecChecks.RunMapNameBoundaryControls();
authoredControls += ProtocolCodecChecks.RunZeroValueBoundaryControls();
authoredControls += ConverterChecks.RunCoordinateBoundaryControls();
authoredControls += UserPreferenceChecks.RunAuthoredControls();
authoredControls += NormalizationChecks.RunAuthoredControls();
authoredControls += ModuleRegexChecks.RunAll();
authoredControls += AutoImportWorkGroupChecks.RunAll();
authoredControls += ProjectCheckerPoolChecks.RunAuthoredControls();
authoredControls += ProjectSessionSchedulingChecks.RunAll();
authoredControls += JsonIndentChecks.RunAll();
authoredControls += SessionDiagnosticsEmitChecks.RunAll();
authoredControls += DocumentSymbolExpandoChecks.RunAll();
Console.WriteLine($"Authored dependency controls executed: {authoredControls}");
return 0;
