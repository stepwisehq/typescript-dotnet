// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Candidate-owned counterpart of internal/fourslash/statebaseline.go printStateDiff.
using System.Globalization;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;

namespace LanguageServices.Checks.Fourslash;
internal sealed partial class StateProjection
{
    // Go still produces fsDiffer output over its authoritative filesystem. This
    // returns only the three original Session-owned projection channels.
    internal string Observe(Session session, bool initialized, string[] openFiles, CaseSensitivity sensitivity)
    {
        if (!initialized)
            return "";
        var snapshot = session.Snapshot();
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        PrintProjects(snapshot, writer);
        PrintOpenFiles(snapshot, openFiles, sensitivity, writer);
        PrintConfigs(snapshot, writer);
        return writer.ToString();
    }
}
