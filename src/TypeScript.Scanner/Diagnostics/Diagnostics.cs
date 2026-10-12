// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/diagnostics/diagnostics.go.
namespace TypeScript.Scanner;
// Diagnostic metadata and retained display-English formatting; locale-aware raw
// UTF8 lookup/cache is separately implemented in Localization.cs.
public enum DiagnosticCategory
{
    Warning,
    Error,
    Suggestion,
    Message
}

public sealed partial class Message(int code, DiagnosticCategory category, string key, string text, bool reportsUnnecessary = false, bool reportsDeprecated = false, bool elidedInCompatibilityPyramid = false)
{
    public bool ElidedInCompatibilityPyramid() => elidedInCompatibilityPyramid;
    public bool ReportsUnnecessary() => reportsUnnecessary;
    public bool ReportsDeprecated() => reportsDeprecated;
    public int Code() => code;
    public DiagnosticCategory Category() => category;
    public string Key() => key;
    public string String() => text;
    public string Localize(params object[] args) => Format(args);
    public override string ToString() => text;
    // Upstream diagnostics.go:128-145 Format sanitizes each stored byte argument
    // using strings.ToValidUTF8: one replacement for each consecutive invalid run.
    // Public Format receives ordinary .NET display strings; this internal path keeps
    // diagnostic storage raw and applies Go's byte-formatting boundary exactly once.
    internal string FormatByteArguments(IEnumerable<string>? args) => Format(args?.Select(s => (object)validUtf8Argument(s)).ToArray() ?? []);
    private static string validUtf8Argument(string arg)
    {
        var result = new System.Text.StringBuilder(arg.Length);
        bool inInvalidRun = false;
        for (int i = 0; i < arg.Length;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(arg.AsSpan(i));
            i += size;
            if (rune == Utf8.RuneError && size == 1)
            {
                if (!inInvalidRun)
                    result.Append('\uFFFD');
                inInvalidRun = true;
            }
            else
            {
                inInvalidRun = false;
                result.Append(new System.Text.Rune(rune).ToString());
            }
        }

        return result.ToString();
    }

    public string Format(params object[] args)
    {
        if (args.Length == 0)
            return text;
        var result = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length;)
        {
            if (text[i] == '{')
            {
                int end = i + 1;
                while (end < text.Length && text[end] is >= '0' and <= '9')
                    end++;
                if (end > i + 1 && end < text.Length && text[end] == '}')
                {
                    if (!int.TryParse(text.AsSpan(i + 1, end - i - 1), out int index) || index >= args.Length)
                        throw new InvalidOperationException("Invalid formatting placeholder");
                    result.Append(Convert.ToString(args[index], System.Globalization.CultureInfo.InvariantCulture));
                    i = end + 1;
                    continue;
                }
            }

            result.Append(text[i++]);
        }

        return result.ToString();
    }
}

public static partial class Diagnostics
{
    private static readonly Lazy<Dictionary<string, Message>> messagesByKey = new(() =>
    {
        var messages = new Dictionary<string, Message>(StringComparer.Ordinal);
        foreach (var message in GetAllMessages())
            messages[message.Key()] = message;
        return messages;
    });
    // Compatibility display-English entrypoint; typed locale overload retains the
    // upstream raw UTF8 lookup/format boundary.
    internal static Message? KeyToMessage(string key) => messagesByKey.Value.TryGetValue(key, out var message) ? message : null;
    public static string Localize(string key, params string[] args)
    {
        var message = KeyToMessage(key) ?? throw new InvalidOperationException("Unknown diagnostic message: " + key);
        return message.Format(args.Cast<object>().ToArray());
    }

    // Ad-hoc message text uses the same ordinary .NET Unicode representation as catalog text.
    public static Message NewAdHocMessage(string message) => new(-1, DiagnosticCategory.Error, "-1", message);
}

public static class DiagnosticCategoryExtensions
{
    public static string Name(this DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.Warning => "warning",
        DiagnosticCategory.Error => "error",
        DiagnosticCategory.Suggestion => "suggestion",
        DiagnosticCategory.Message => "message",
        _ => throw new InvalidOperationException("Unhandled diagnostic category")};
}
