// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/ls/string_completions_test.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Text;
using TypeScript.Scanner;
using TypeScript.LanguageServices;

namespace LanguageServices.Checks;
public static class CompletionChecks
{
    public static int RunAll()
    {
        TestTryRemoveDirectoryPrefixCaseFoldingShrinksPrefix();
        return 1;
    }

    public static void TestTryRemoveDirectoryPrefixCaseFoldingShrinksPrefix()
    {
        var prefix = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(Encoding.UTF8.GetBytes("/a/\u212A\u212A\u212A\u212A")));
        var path = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(Encoding.UTF8.GetBytes("/a/kkkk/x.ts")));
        var actual = LanguageService.tryRemoveDirectoryPrefix(path, prefix, CaseSensitivity.CaseInsensitive);
        if (actual == null)
            throw new InvalidOperationException("expected a non-nil result");
        if (!actual.Value.EqualsValue(ByteStringValue.FromString("x.ts")))
            throw new InvalidOperationException("Expected x.ts");
    }
}
