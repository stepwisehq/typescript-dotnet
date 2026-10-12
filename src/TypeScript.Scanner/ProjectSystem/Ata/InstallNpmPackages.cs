// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem.Ata;
internal static partial class AutomaticTypeAcquisition
{
    // internal/project/ata/ata.go:291 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
    // Every batch retains a slice header over the original package-name owner.
    internal static Exception? InstallNpmPackages(CancellationToken context, Slice<ByteStringValue> packageNames, SemaphoreSlim concurrencySemaphore, Func<Slice<ByteStringValue>, Exception?> installPackages)
    {
        var group = Core.NewThrottleGroup(context, concurrencySemaphore);
        var currentCommandStart = 0;
        var currentCommandEnd = 0;
        long currentCommandSize = 100;
        foreach (var packageName in packageNames)
        {
            currentCommandSize = unchecked(currentCommandSize + packageName.Length + 1);
            if (currentCommandSize < 8000)
            {
                currentCommandEnd++;
            }
            else
            {
                var packages = packageNames.Slice2(currentCommandStart, currentCommandEnd);
                group.Go(() => installPackages(packages));
                currentCommandStart = currentCommandEnd;
                currentCommandSize = unchecked(100 + packageName.Length + 1);
                currentCommandEnd++;
            }
        }

        if (currentCommandStart < packageNames.Count)
        {
            var packages = packageNames.Slice2(currentCommandStart, currentCommandEnd);
            group.Go(() => installPackages(packages));
        }

        return group.Wait();
    }
}
