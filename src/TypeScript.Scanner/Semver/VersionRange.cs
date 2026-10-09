// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/semver/version_range.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Text;
using System.Text.RegularExpressions;
using TypeScript.Scanner;

namespace TypeScript.Semver;
internal readonly record struct VersionComparator(string Operator, Version Operand);
internal struct VersionRange
{
    internal List<List<VersionComparator>?>? alternatives;
    internal string String()
    {
        var builder = new StringBuilder();
        Semver.formatDisjunction(builder, alternatives);
        return builder.ToString();
    }

    internal bool Test(Version? version) => Semver.testDisjunction(alternatives, version);
    public override string ToString() => String();
}

internal static partial class Semver
{
    private static readonly Regex logicalOrRegExp = CreateRegex(@"\|\|");
    private static readonly Regex whitespaceRegExp = CreateRegex(@"\s+");
    private static readonly Regex partialRegExp = CreateRegex(@"(?i)^([x*0]|[1-9]\d*)(?:\.([x*0]|[1-9]\d*)(?:\.([x*0]|[1-9]\d*)(?:-([a-z0-9-.]+))?(?:\+([a-z0-9-.]+))?)?)?$");
    private static readonly Regex hyphenRegExp = CreateRegex(@"(?i)^\s*([a-z0-9-+.*]+)\s+-\s+([a-z0-9-+.*]+)\s*$");
    private static readonly Regex rangeRegExp = CreateRegex(@"(?i)^([~^<>=]|<=|>=)?\s*([a-z0-9-+.*]+)$");
    internal static void formatDisjunction(StringBuilder builder, List<List<VersionComparator>?>? alternatives)
    {
        int originalLength = builder.Length;
        if (alternatives != null)
            for (int i = 0; i < alternatives.Count; i++)
            {
                if (i > 0)
                    builder.Append(" || ");
                formatAlternative(builder, alternatives[i]);
            }

        if (builder.Length == originalLength)
            builder.Append('*');
    }

    internal static void formatAlternative(StringBuilder builder, List<VersionComparator>? comparators)
    {
        if (comparators == null)
            return;
        for (int i = 0; i < comparators.Count; i++)
        {
            if (i > 0)
                builder.Append(' ');
            formatComparator(builder, comparators[i]);
        }
    }

    internal static void formatComparator(StringBuilder builder, VersionComparator comparator) => builder.Append(comparator.Operator).Append(comparator.Operand.String());
    internal static bool testDisjunction(List<List<VersionComparator>?>? alternatives, Version? version)
    {
        if (alternatives is not { Count: > 0 })
            return true;
        foreach (var alternative in alternatives)
            if (testAlternative(alternative, version))
                return true;
        return false;
    }

    internal static bool testAlternative(List<VersionComparator>? alternative, Version? version)
    {
        if (alternative != null)
            foreach (var comparator in alternative)
                if (!testComparator(comparator, version))
                    return false;
        return true;
    }

    internal static bool testComparator(VersionComparator comparator, Version? version)
    {
        int comparison = Compare(version, comparator.Operand);
        return comparator.Operator switch
        {
            "<" => comparison < 0,
            "<=" => comparison <= 0,
            "=" => comparison == 0,
            ">=" => comparison >= 0,
            ">" => comparison > 0,
            _ => throw new InvalidOperationException("Unexpected operator: " + comparator.Operator)};
    }

    internal static (VersionRange Range, bool Ok) TryParseVersionRange(string text)
    {
        var(alternatives, ok) = parseAlternatives(text);
        return (new VersionRange { alternatives = alternatives }, ok);
    }

    private static string trimSpace(string text)
    {
        static bool IsSpace(int rune) => rune is >= 0x9 and <= 0xD or 0x20 or 0x85 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A or 0x2028 or 0x2029 or 0x202F or 0x205F or 0x3000;
        int start = 0, end = text.Length;
        while (start < end)
        {
            var(rune, size) = Utf8.DecodeRuneInString(text.AsSpan(start, end - start));
            if (!IsSpace(rune))
                break;
            start += size;
        }

        while (end > start)
        {
            var(rune, size) = Utf8.DecodeLastRuneInString(text.AsSpan(start, end - start));
            if (!IsSpace(rune))
                break;
            end -= size;
        }

        return text[start..end];
    }

    internal static (List<List<VersionComparator>?>? Alternatives, bool Ok) parseAlternatives(string text)
    {
        List<List<VersionComparator>?>? alternatives = null;
        foreach (string rawRange in logicalOrRegExp.Split(trimSpace(text)))
        {
            string range = trimSpace(rawRange);
            if (range == "")
                continue;
            List<VersionComparator>? comparators = null;
            Match hyphen = hyphenRegExp.Match(GoString.ToString(range));
            if (hyphen.Success)
            {
                var(parsed, ok) = parseHyphen(group(hyphen, 1), group(hyphen, 2));
                if (!ok)
                    return (null, false);
                if (parsed != null)
                    (comparators ??= []).AddRange(parsed);
            }
            else
            {
                foreach (string simple in whitespaceRegExp.Split(range))
                {
                    Match match = rangeRegExp.Match(GoString.ToString(trimSpace(simple)));
                    if (!match.Success)
                        return (null, false);
                    var(parsed, ok) = parseComparator(group(match, 1), group(match, 2));
                    if (!ok)
                        return (null, false);
                    if (parsed != null)
                        (comparators ??= []).AddRange(parsed);
                }
            }

            (alternatives ??= []).Add(comparators);
        }

        return (alternatives, true);
    }

    internal static (List<VersionComparator>? Comparators, bool Ok) parseHyphen(string left, string right)
    {
        var(a, leftOk) = parsePartial(left);
        if (!leftOk)
            return (null, false);
        var(b, rightOk) = parsePartial(right);
        if (!rightOk)
            return (null, false);
        List<VersionComparator>? result = null;
        if (!isWildcard(a.Major))
            (result ??= []).Add(new(">=", a.Version));
        if (!isWildcard(b.Major))
        {
            string op;
            Version operand = b.Version;
            if (isWildcard(b.Minor))
            {
                operand = operand.incrementMajor();
                op = "<";
            }
            else if (isWildcard(b.Patch))
            {
                operand = operand.incrementMinor();
                op = "<";
            }
            else
                op = "<=";
            (result ??= []).Add(new(op, operand));
        }

        return (result, true);
    }

    internal readonly record struct PartialVersion(Version Version, string Major, string Minor, string Patch);
    internal static (PartialVersion Partial, bool Ok) parsePartial(string text)
    {
        Match match = partialRegExp.Match(GoString.ToString(text));
        if (!match.Success)
            return (default, false);
        string majorText = group(match, 1), minorText = group(match, 2), patchText = group(match, 3);
        if (minorText == "")
            minorText = "*";
        if (patchText == "")
            patchText = "*";
        uint major = 0, minor = 0, patch = 0;
        Exception? error;
        if (!isWildcard(majorText))
        {
            (major, error) = getUintComponent(majorText);
            if (error != null)
                return (default, false);
            if (!isWildcard(minorText))
            {
                (minor, error) = getUintComponent(minorText);
                if (error != null)
                    return (default, false);
                if (!isWildcard(patchText))
                {
                    (patch, error) = getUintComponent(patchText);
                    if (error != null)
                        return (default, false);
                }
            }
        }

        var version = new Version
        {
            major = major,
            minor = minor,
            patch = patch
        };
        if (group(match, 4) != "")
            version.prerelease = [..group(match, 4).Split('.')];
        if (group(match, 5) != "")
            version.build = [..group(match, 5).Split('.')];
        return (new(version, majorText, minorText, patchText), true);
    }

    internal static (List<VersionComparator>? Comparators, bool Ok) parseComparator(string op, string text)
    {
        var(partial, ok) = parsePartial(text);
        if (!ok)
            return (null, false);
        if (isWildcard(partial.Major))
            return (op is "<" or ">" ? [new("<", versionZero)] : null, true);
        Version version = partial.Version;
        switch (op)
        {
            case "~":
                return ([new(">=", version), new("<", isWildcard(partial.Minor) ? version.incrementMajor() : version.incrementMinor())], true);
            case "^":
                Version upper = version.major > 0 || isWildcard(partial.Minor) ? version.incrementMajor() : version.minor > 0 || isWildcard(partial.Patch) ? version.incrementMinor() : version.incrementPatch();
                return ([new(">=", version), new("<", upper)], true);
            case "<":
            case ">=":
                if (isWildcard(partial.Minor) || isWildcard(partial.Patch))
                    version.prerelease = ["0"];
                return ([new(op, version)], true);
            case "<=":
            case ">":
                if (isWildcard(partial.Minor))
                {
                    op = op == "<=" ? "<" : ">=";
                    version = version.incrementMajor();
                    version.prerelease = ["0"];
                }
                else if (isWildcard(partial.Patch))
                {
                    op = op == "<=" ? "<" : ">=";
                    version = version.incrementMinor();
                    version.prerelease = ["0"];
                }

                return ([new(op, version)], true);
            case "=":
            case "":
                if (isWildcard(partial.Minor) || isWildcard(partial.Patch))
                {
                    Version lower = version;
                    lower.prerelease = ["0"];
                    Version high = isWildcard(partial.Minor) ? version.incrementMajor() : version.incrementMinor();
                    high.prerelease = ["0"];
                    return ([new(">=", lower), new("<", high)], true);
                }

                return ([new("=", version)], true);
            default:
                throw new InvalidOperationException("Unexpected operator: " + op);
        }
    }

    internal static bool isWildcard(string text) => text is "*" or "x" or "X";
}
