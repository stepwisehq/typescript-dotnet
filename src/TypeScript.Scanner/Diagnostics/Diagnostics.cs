// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/diagnostics/diagnostics.go.
namespace TypeScript.Scanner;
// Scanner-only English diagnostic metadata and argument formatting.
public enum DiagnosticCategory
{
    Warning,
    Error,
    Suggestion,
    Message
}

public sealed class Message(int code, DiagnosticCategory category, string key, string text, bool reportsUnnecessary = false, bool reportsDeprecated = false)
{
    public bool ReportsUnnecessary() => reportsUnnecessary;
    public bool ReportsDeprecated() => reportsDeprecated;
    public int Code() => code;
    public DiagnosticCategory Category() => category;
    public string Key() => key;
    public string String() => text;
    public override string ToString() => text;
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
