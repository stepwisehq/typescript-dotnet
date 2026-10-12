// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete borrowed producer for internal/checker/utilities.go:1749 ValueToString.
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal static ByteStringValue ValueToStringValue(object value) => value switch
    {
        ByteStringValue text => ByteStringValue.Concat(ByteStringValue.FromString("\""), TypeScript.DiagnosticPrinting.PrinterUtilities.EscapeStringValue(text, TypeScript.DiagnosticPrinting.QuoteChar.DoubleQuote), ByteStringValue.FromString("\"")),
        string text => ValueToStringValue(ByteStringValue.FromString(text)),
        JsNum.Number number => ByteStringValue.FromString(number.String()),
        bool boolean => ByteStringValue.FromString(boolean ? "true" : "false"),
        JsNum.PseudoBigInt bigint => ByteStringValue.Concat(ByteStringValue.FromString(bigint.String()), ByteStringValue.FromString("n")),
        _ => throw new InvalidOperationException("unhandled value type in valueToString")};
}
