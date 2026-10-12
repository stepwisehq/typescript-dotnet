// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original completions.go escapeSnippetText, shared by completion and auto insertion.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private static ByteStringValue escapeSnippetText(ByteStringValue text) => GoStrings.Replace(text, ByteStringValue.FromString("$"), ByteStringValue.FromString("\\$"), -1);
}
