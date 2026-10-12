// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Raw owner/consumer counterpart of internal/ls/lsutil/organizeimports.go:299.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    internal static ByteStringValue GetExternalModuleNameValue(Node? specifier) => specifier is not null && Ast.IsStringLiteralLike(specifier) ? specifier.TextValue() : default;
}
