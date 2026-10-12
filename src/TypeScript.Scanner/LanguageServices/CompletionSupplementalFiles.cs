// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Exact completions.go139–163 shared leaves, consumed by completion and CodeLens.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private static int? supplementalFileIndex(SourceFile file)
    {
        var canonical = file.CanonicalSourceFile();
        if (canonical == null)
            return null;
        var supplemental = canonical.SupplementalSourceFiles();
        for (int i = 0; i < supplemental.Length; i++)
            if (supplemental[i] == file)
                return i;
        throw new InvalidOperationException("supplemental source file is not linked from its canonical source file");
    }

    private static SourceFile? sourceFileForSupplementalFileIndex(SourceFile file, int? index)
    {
        if (index == null)
            return file;
        var supplemental = file.SupplementalSourceFiles();
        return index.Value >= 0 && index.Value < supplemental.Length ? supplemental[index.Value] : null;
    }
}
