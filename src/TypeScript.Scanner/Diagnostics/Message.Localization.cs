// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Locale-aware Go UTF8 boundary; existing display-English overload stays compatible.
using DiagnosticLocale = TypeScript.Locale.Locale;

namespace TypeScript.Scanner;
public sealed partial class Message
{
    // The typed first argument cannot accidentally become an ordinary formatting
    // argument. String arguments and the result use Go's raw UTF8 representation.
    public string Localize(DiagnosticLocale locale, params object? [] args) => DiagnosticLocalization.Localize(locale, this, "", DiagnosticLocalization.StringifyArgs(args));
}

public static partial class Diagnostics
{
    public static string Localize(DiagnosticLocale locale, Message? message, string key, params string[] args) => DiagnosticLocalization.Localize(locale, message, key, args);
}
