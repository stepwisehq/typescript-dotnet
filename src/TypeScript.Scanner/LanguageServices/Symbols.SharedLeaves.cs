// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused original shared leaf from internal/ls/symbols.go:623 at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Full Symbols owner remains pending.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private static bool isInsideNodeModules(RootedFilePath fileName) => fileName.ContainsLowercaseDirectorySequence("/node_modules/");
}
