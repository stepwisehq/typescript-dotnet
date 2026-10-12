// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Exact original function identities from internal/project/checkerpool_test.go.
namespace LanguageServices.Checks;
public static partial class ProjectCheckerPoolChecks
{
    public static readonly string[] Names =
    [
        "TestCheckerPoolDiagnosticsRouting",
        "TestCheckerPoolQueryRouting",
        "TestCheckerPoolRequestAffinity",
        "TestCheckerPoolSameRequestContention",
        "TestCheckerPoolSameRequestConcurrentQueries",
        "TestCheckerPoolIdleCleanup",
        "TestCheckerPoolFileAssociationCleanup",
        "TestCheckerPoolMinCheckers",
        "TestCheckerPoolDefaultIdleTimeout",
        "TestCheckerPoolQueryContention",
        "TestCheckerPoolDiagnosticsContention",
        "TestCheckerPoolCanceledCheckerDisposal",
        "TestCheckerPoolRequestAssociationCleanupOnDisposal",
        "TestCheckerPoolRequestAssociationCleanupOnContextDone",
        "TestCheckerPoolDiagnosticsRecreatedAfterIdleDisposal",
        "TestCheckerPoolCrossReleaseAffinityWithContention",
        "TestCheckerPoolLifetimeMismatchIgnoresAssociation",
        "TestCheckerPoolNoRequestID",
        "TestCheckerPoolDiagnosticsCrossReleaseAffinity",
        "TestCheckerPoolDiscardKeepsIdleCheckers",
        "TestCheckerPoolDiscardHeldCheckerSurvivesRelease",
        "TestCheckerPoolDiscardStillFunctional",
        "TestCheckerPoolDiagnosticsCheckerStableIdentity",
        "TestCheckerPoolDiagnosticsCheckerSurvivesDiscard",
        "TestCheckerPoolDiagnosticsCheckerIndependentFromQuery",
        "TestCheckerPoolAPICheckerStableIdentity",
        "TestCheckerPoolAPICheckerSurvivesDiscard",
        "TestCheckerPoolAllThreeIndependent",
        "TestCheckerPoolFileAffinity",
        "TestCheckerPoolMultipleConcurrentQueryCheckers",
        "TestCheckerPoolDoubleReleaseSafe",
        "TestCheckerPoolDefaultMaxCheckers",
        "TestCheckerPoolStaggeredIdleCleanup",
        "TestCheckerPoolDiscardIdempotent",
        "TestCheckerPoolGetGlobalDiagnosticsEmpty",
        "TestCheckerPoolTakeNewGlobalDiagnostics",
        "TestCheckerPoolAPICheckerDisposedOnCancel",
        "TestCheckerPoolNonCancelableContextNoAffinity",
        "TestCheckerPoolCleanupAfterDiscardIsNoop",
    ];
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
            case "TestCheckerPoolDiagnosticsRouting":
                TestCheckerPoolDiagnosticsRouting();
                break;
            case "TestCheckerPoolQueryRouting":
                TestCheckerPoolQueryRouting();
                break;
            case "TestCheckerPoolRequestAffinity":
                TestCheckerPoolRequestAffinity();
                break;
            case "TestCheckerPoolSameRequestContention":
                TestCheckerPoolSameRequestContention();
                break;
            case "TestCheckerPoolSameRequestConcurrentQueries":
                TestCheckerPoolSameRequestConcurrentQueries();
                break;
            case "TestCheckerPoolIdleCleanup":
                TestCheckerPoolIdleCleanup();
                break;
            case "TestCheckerPoolFileAssociationCleanup":
                TestCheckerPoolFileAssociationCleanup();
                break;
            case "TestCheckerPoolMinCheckers":
                TestCheckerPoolMinCheckers();
                break;
            case "TestCheckerPoolDefaultIdleTimeout":
                TestCheckerPoolDefaultIdleTimeout();
                break;
            case "TestCheckerPoolQueryContention":
                TestCheckerPoolQueryContention();
                break;
            case "TestCheckerPoolDiagnosticsContention":
                TestCheckerPoolDiagnosticsContention();
                break;
            case "TestCheckerPoolCanceledCheckerDisposal":
                TestCheckerPoolCanceledCheckerDisposal();
                break;
            case "TestCheckerPoolRequestAssociationCleanupOnDisposal":
                TestCheckerPoolRequestAssociationCleanupOnDisposal();
                break;
            case "TestCheckerPoolRequestAssociationCleanupOnContextDone":
                TestCheckerPoolRequestAssociationCleanupOnContextDone();
                break;
            case "TestCheckerPoolDiagnosticsRecreatedAfterIdleDisposal":
                TestCheckerPoolDiagnosticsRecreatedAfterIdleDisposal();
                break;
            case "TestCheckerPoolCrossReleaseAffinityWithContention":
                TestCheckerPoolCrossReleaseAffinityWithContention();
                break;
            case "TestCheckerPoolLifetimeMismatchIgnoresAssociation":
                TestCheckerPoolLifetimeMismatchIgnoresAssociation();
                break;
            case "TestCheckerPoolNoRequestID":
                TestCheckerPoolNoRequestID();
                break;
            case "TestCheckerPoolDiagnosticsCrossReleaseAffinity":
                TestCheckerPoolDiagnosticsCrossReleaseAffinity();
                break;
            case "TestCheckerPoolDiscardKeepsIdleCheckers":
                TestCheckerPoolDiscardKeepsIdleCheckers();
                break;
            case "TestCheckerPoolDiscardHeldCheckerSurvivesRelease":
                TestCheckerPoolDiscardHeldCheckerSurvivesRelease();
                break;
            case "TestCheckerPoolDiscardStillFunctional":
                TestCheckerPoolDiscardStillFunctional();
                break;
            case "TestCheckerPoolDiagnosticsCheckerStableIdentity":
                TestCheckerPoolDiagnosticsCheckerStableIdentity();
                break;
            case "TestCheckerPoolDiagnosticsCheckerSurvivesDiscard":
                TestCheckerPoolDiagnosticsCheckerSurvivesDiscard();
                break;
            case "TestCheckerPoolDiagnosticsCheckerIndependentFromQuery":
                TestCheckerPoolDiagnosticsCheckerIndependentFromQuery();
                break;
            case "TestCheckerPoolAPICheckerStableIdentity":
                TestCheckerPoolAPICheckerStableIdentity();
                break;
            case "TestCheckerPoolAPICheckerSurvivesDiscard":
                TestCheckerPoolAPICheckerSurvivesDiscard();
                break;
            case "TestCheckerPoolAllThreeIndependent":
                TestCheckerPoolAllThreeIndependent();
                break;
            case "TestCheckerPoolFileAffinity":
                TestCheckerPoolFileAffinity();
                break;
            case "TestCheckerPoolMultipleConcurrentQueryCheckers":
                TestCheckerPoolMultipleConcurrentQueryCheckers();
                break;
            case "TestCheckerPoolDoubleReleaseSafe":
                TestCheckerPoolDoubleReleaseSafe();
                break;
            case "TestCheckerPoolDefaultMaxCheckers":
                TestCheckerPoolDefaultMaxCheckers();
                break;
            case "TestCheckerPoolStaggeredIdleCleanup":
                TestCheckerPoolStaggeredIdleCleanup();
                break;
            case "TestCheckerPoolDiscardIdempotent":
                TestCheckerPoolDiscardIdempotent();
                break;
            case "TestCheckerPoolGetGlobalDiagnosticsEmpty":
                TestCheckerPoolGetGlobalDiagnosticsEmpty();
                break;
            case "TestCheckerPoolTakeNewGlobalDiagnostics":
                TestCheckerPoolTakeNewGlobalDiagnostics();
                break;
            case "TestCheckerPoolAPICheckerDisposedOnCancel":
                TestCheckerPoolAPICheckerDisposedOnCancel();
                break;
            case "TestCheckerPoolNonCancelableContextNoAffinity":
                TestCheckerPoolNonCancelableContextNoAffinity();
                break;
            case "TestCheckerPoolCleanupAfterDiscardIsNoop":
                TestCheckerPoolCleanupAfterDiscardIsNoop();
                break;
            default:
                throw new ArgumentException(name);
        }
    }
}
