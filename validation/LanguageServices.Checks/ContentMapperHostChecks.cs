// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete 28 original internal/contentmapper/host_test.go test functions.
namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    public static IEnumerable<string> Names => ["TestRunnerTransform", "TestHostLogging", "TestHostDiscardsStderrWithoutLogging", "TestMapperDiagnosticName", "TestRunnerTransformResponseValidation", "TestHostClosesProcessWhenReadLoopFails", "TestHostReportsInitializationTimeoutBeforeClosingProcess", "TestHostReportsProcessExitBeforeInitialization", "TestRunnerTransformDiagnosticDirectives", "TestMappedDiagnosticDirectiveJSON", "TestRunnerTransformSupplementalOutputs", "TestRunnerTransformInvalidSupplementalDiagnosticDirective", "TestRunnerRejectsInvalidVirtualExtension", "TestRunnerPositionEncodings", "TestRunnerRejectsUnsupportedPositionEncoding", "TestRunnerRejectsInvalidDiagnosticSource", "TestRunnerRejectsPositionsInsideUnicodeCharacters", "TestRunnerConsolidatesByIdentity", "TestRunnerLeaseLifecycle", "TestProjectLifecycle", "TestProjectMethodsAfterHostClose", "TestProjectRejectsRelativeWatchedFiles", "TestDynamicProjectRequiresConfigIdentity", "TestStaticMapperRejectsDynamicProjectResponseFields", "TestProjectRejectsInvalidOptionDiagnosticPath", "TestRunnerForwardsProjectOptions", "TestHostSetLocaleRestartsMapper", "TestHostSetLocaleWaitsForTransform"];

    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return 28;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestRunnerTransform":
                RunnerTransform();
                break;
            case "TestHostLogging":
                HostLogging();
                break;
            case "TestHostDiscardsStderrWithoutLogging":
                HostDiscardsStderrWithoutLogging();
                break;
            case "TestMapperDiagnosticName":
                MapperDiagnosticName();
                break;
            case "TestRunnerTransformResponseValidation":
                RunnerTransformResponseValidation();
                break;
            case "TestHostClosesProcessWhenReadLoopFails":
                HostClosesProcessWhenReadLoopFails();
                break;
            case "TestHostReportsInitializationTimeoutBeforeClosingProcess":
                HostReportsInitializationTimeoutBeforeClosingProcess();
                break;
            case "TestHostReportsProcessExitBeforeInitialization":
                HostReportsProcessExitBeforeInitialization();
                break;
            case "TestRunnerTransformDiagnosticDirectives":
                RunnerTransformDiagnosticDirectives();
                break;
            case "TestMappedDiagnosticDirectiveJSON":
                MappedDiagnosticDirectiveJSON();
                break;
            case "TestRunnerTransformSupplementalOutputs":
                RunnerTransformSupplementalOutputs();
                break;
            case "TestRunnerTransformInvalidSupplementalDiagnosticDirective":
                RunnerTransformInvalidSupplementalDiagnosticDirective();
                break;
            case "TestRunnerRejectsInvalidVirtualExtension":
                RunnerRejectsInvalidVirtualExtension();
                break;
            case "TestRunnerPositionEncodings":
                RunnerPositionEncodings();
                break;
            case "TestRunnerRejectsUnsupportedPositionEncoding":
                RunnerRejectsUnsupportedPositionEncoding();
                break;
            case "TestRunnerRejectsInvalidDiagnosticSource":
                RunnerRejectsInvalidDiagnosticSource();
                break;
            case "TestRunnerRejectsPositionsInsideUnicodeCharacters":
                RunnerRejectsPositionsInsideUnicodeCharacters();
                break;
            case "TestRunnerConsolidatesByIdentity":
                RunnerConsolidatesByIdentity();
                break;
            case "TestRunnerLeaseLifecycle":
                RunnerLeaseLifecycle();
                break;
            case "TestProjectLifecycle":
                ProjectLifecycle();
                break;
            case "TestProjectMethodsAfterHostClose":
                ProjectMethodsAfterHostClose();
                break;
            case "TestProjectRejectsRelativeWatchedFiles":
                ProjectRejectsRelativeWatchedFiles();
                break;
            case "TestDynamicProjectRequiresConfigIdentity":
                DynamicProjectRequiresConfigIdentity();
                break;
            case "TestStaticMapperRejectsDynamicProjectResponseFields":
                StaticMapperRejectsDynamicProjectResponseFields();
                break;
            case "TestProjectRejectsInvalidOptionDiagnosticPath":
                ProjectRejectsInvalidOptionDiagnosticPath();
                break;
            case "TestRunnerForwardsProjectOptions":
                RunnerForwardsProjectOptions();
                break;
            case "TestHostSetLocaleRestartsMapper":
                HostSetLocaleRestartsMapper();
                break;
            case "TestHostSetLocaleWaitsForTransform":
                HostSetLocaleWaitsForTransform();
                break;
            default:
                throw new ArgumentException(name);
        }
    }
}
