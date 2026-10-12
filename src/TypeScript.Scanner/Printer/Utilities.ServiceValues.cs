// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Borrowed input/output worker for internal/printer/utilities.go:178 EscapeString.
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal static partial class PrinterUtilities
{
    internal static ByteStringValue EscapeStringValue(ByteStringValue text, QuoteChar quote)
    {
        var builder = new GoBuilder();
        builder.Grow(text.Length + 2);
        escapeStringWorker(text, quote, getLiteralTextFlags.NeverAsciiEscape, builder);
        return builder.Value();
    }
}
