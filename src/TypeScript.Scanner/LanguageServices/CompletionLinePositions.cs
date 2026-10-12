// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/completions.go:3027-3053, pin fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Shared original completion helpers also consumed by folding ranges.
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private static int getLineOfPosition(SourceFile file, int position) => ScannerApi.GetECMALineOfPosition(file, position);
    private static int getLineEndOfPosition(SourceFile file, int position)
    {
        int line = getLineOfPosition(file, position);
        var starts = ScannerApi.GetECMALineStarts(file);
        int last = line + 1 >= starts.Length ? file.End() : starts[line + 1] - 1;
        var text = file.TextView();
        if (last > 0 && last < text.Length && text[last] == '\n' && text[last - 1] == '\r')
            return last - 1;
        return last;
    }
}
