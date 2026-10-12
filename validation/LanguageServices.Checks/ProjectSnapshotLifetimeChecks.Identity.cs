// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshot_test.go TestProjectIDNarrowing: complete original assertions.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    public static void TestProjectIDNarrowing()
    {
        static void Require(bool value)
        {
            if (!value)
                throw new InvalidOperationException("original project ID narrowing assertion failed");
        }

        var configured = new ID(ByteStringValue.FromString("/project/tsconfig.json"));
        var(configuredID, ok) = configured.Configured();
        Require(ok);
        Require(configuredID.PathKey() == TsPath.PathKeyFromCanonical("/project/tsconfig.json"));
        Require(!configured.Inferred().Ok);
        Require(!configured.Synthetic().Ok);
        var inferred = Projects.InferredProjectID.AsID();
        var(inferredID, inferredOK) = inferred.Inferred();
        Require(inferredOK);
        Require(inferredID == Projects.InferredProjectID);
        Require(!inferred.Configured().Ok);
        var synthetic = Projects.NewSyntheticProjectID(1).AsID();
        var(syntheticID, syntheticOK) = synthetic.Synthetic();
        Require(syntheticOK);
        Require(syntheticID == Projects.NewSyntheticProjectID(1));
        Require(!synthetic.Configured().Ok);
        var(canonicalSyntheticID, canonicalOK) = new ID(ByteStringValue.FromString("/dev/null/synthetic/01")).Synthetic();
        Require(canonicalOK);
        Require(canonicalSyntheticID == Projects.NewSyntheticProjectID(1));
        Require(new ID(ByteStringValue.FromString("/dev/null/synthetic/invalid")).Configured().Ok);
        Require(!Projects.ParseConfiguredProjectID(TsPath.PathKeyFromCanonical("/dev/null/inferred")).Ok);
        Require(!Projects.ParseConfiguredProjectID(TsPath.PathKeyFromCanonical(Projects.NewSyntheticProjectID(1).AsID().GoLogString())).Ok);
    }
}
