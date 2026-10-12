using LanguageServices.Checks;

// Run stack-boundary probes in separate processes so a runtime stack failure
// cannot hide the result of the ordinary dependency assertions.
if (args.Length != 0)
{
    if (args.Length != 3 || args[0] != "--protocol-depth" ||
        !int.TryParse(args[2], out int depth) || depth < 1 ||
        args[1] is not ("model" or "any"))
    {
        Console.Error.WriteLine("Usage: LanguageServices.Validation [--protocol-depth model|any DEPTH]");
        return 2;
    }
    if (args[1] == "model") ProtocolCodecChecks.RunModelDepthBoundary(depth);
    else ProtocolCodecChecks.RunAnyDepthBoundary(depth);
    Console.WriteLine($"Protocol {args[1]} depth {depth}: passed");
    return 0;
}

// Authored runner over shared original assertions. This dependency gate does not
// establish service/server completion; transport and corpus gates are separate.
int originalFunctions = ProjectDirtyChecks.RunAll();
originalFunctions += ProjectCacheChecks.RunAll();
originalFunctions += ProjectBackgroundChecks.RunAll();
originalFunctions += ProjectLoggingChecks.RunAll();
originalFunctions += ProtocolFramingChecks.RunAll();
originalFunctions += ProtocolCodecChecks.RunAll();
originalFunctions += AstNavChecks.RunAll();
originalFunctions += ConverterChecks.RunAll();
originalFunctions += ProjectOverlayChecks.RunAll();
originalFunctions += ProjectSnapshotChecks.RunAll();
originalFunctions += ProjectWatchChecks.RunAll();
originalFunctions += AtaPackageNameChecks.RunAll();
originalFunctions += AtaInstallChecks.RunAll();
originalFunctions += AtaDiscoveryChecks.RunAll();
originalFunctions += BreadthFirstSearchChecks.RunAll();
originalFunctions += UserPreferenceChecks.RunAll();
originalFunctions += LsUtilityChecks.RunAll();
originalFunctions += FormattingChecks.RunAll();
Console.WriteLine($"Original language-service dependency test functions executed: {originalFunctions}");
Console.WriteLine($"Original external normalization dependency test functions executed: {NormalizationChecks.RunAll()}");
// Regex groups include explicitly named selected assertion channels; this is not
// a count of wholly ported upstream test functions.
Console.WriteLine($"External regexp assertion groups executed: {RegexChecks.RunAll()}");
Console.WriteLine($"External regexp syntax assertion groups executed: {RegexSyntaxChecks.RunAll()}");
Console.WriteLine($"Original external regexp compiler test functions executed: {RegexCompileChecks.RunAll()}");
Console.WriteLine($"Selected original RE2 matching channels executed: {RegexRe2Checks.RunAll()}");
Console.WriteLine($"Selected original Fowler matching channels executed: {RegexFowlerChecks.RunAll()}");
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
Console.WriteLine($"Authored dependency controls executed: {authoredControls}");
return 0;
