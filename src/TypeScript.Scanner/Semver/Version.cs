// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/semver/version.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using System.Text.RegularExpressions;
using TypeScript.Scanner;

namespace TypeScript.Semver;
internal struct Version
{
    internal uint major, minor, patch;
    internal List<string>? prerelease, build;
    internal Version incrementMajor() => new()
    {
        major = unchecked(major + 1)
    };
    internal Version incrementMinor() => new()
    {
        major = major,
        minor = unchecked(minor + 1)
    };
    internal Version incrementPatch() => new()
    {
        major = major,
        minor = minor,
        patch = unchecked(patch + 1)
    };
    internal int Compare(Version? other) => Semver.Compare(this, other);
    internal string String()
    {
        string result = major.ToString(CultureInfo.InvariantCulture) + "." + minor.ToString(CultureInfo.InvariantCulture) + "." + patch.ToString(CultureInfo.InvariantCulture);
        if (prerelease is { Count: > 0 })
            result += "-" + string.Join('.', prerelease);
        if (build is { Count: > 0 })
            result += "+" + string.Join('.', build);
        return result;
    }

    public override string ToString() => String();
}

internal sealed class SemverParseError(string origInput) : Exception("Could not parse version string from " + GoQuote.Quote(origInput));
internal static partial class Semver
{
    // RE2's ASCII shorthand classes and absolute end anchor differ from .NET's.
    // ASCII case folding additionally includes exactly long-s and Kelvin sign.
    internal static string group(Match match, int index) => GoString.ToUtf8(match.Groups[index].Value);
    internal static Regex CreateRegex(string source)
    {
        string pattern = source.Replace("(?i)", "", StringComparison.Ordinal).Replace("a-zA-Z", "a-zA-Z\u017F\u212A", StringComparison.Ordinal).Replace("a-z0", "a-zA-Z\u017F\u212A0", StringComparison.Ordinal).Replace("a-z-", "a-zA-Z\u017F\u212A-", StringComparison.Ordinal).Replace("[x*0]", "[xX*0]", StringComparison.Ordinal).Replace(@"\d", "[0-9]", StringComparison.Ordinal).Replace(@"\s", @"[\t\n\f\r ]", StringComparison.Ordinal);
        if (pattern.EndsWith('$'))
            pattern = pattern[..^1] + @"\z";
        return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    }

    private static readonly Regex versionRegexp = CreateRegex(@"(?i)^(0|[1-9]\d*)(?:\.(0|[1-9]\d*)(?:\.(0|[1-9]\d*)(?:-([a-z0-9-.]+))?(?:\+([a-z0-9-.]+))?)?)?$");
    private static readonly Regex prereleaseRegexp = CreateRegex(@"(?i)^(?:0|[1-9]\d*|[a-z-][a-z0-9-]*)(?:\.(?:0|[1-9]\d*|[a-zA-Z-][a-zA-Z0-9-]*))*$");
    internal static readonly Regex prereleasePartRegexp = CreateRegex(@"(?i)^(?:0|[1-9]\d*|[a-z-][a-z0-9-]*)$");
    private static readonly Regex buildRegExp = CreateRegex(@"(?i)^[a-z0-9-]+(?:\.[a-z0-9-]+)*$");
    internal static readonly Regex buildPartRegExp = CreateRegex(@"(?i)^[a-z0-9-]+$");
    private static readonly Regex numericIdentifierRegExp = CreateRegex(@"^(?:0|[1-9]\d*)$");
    internal static readonly Version versionZero = new()
    {
        prerelease = ["0"]
    };
    internal static int Compare(Version? a, Version? b)
    {
        if (a is null)
            return b is null ? 0 : -1;
        if (b is null)
            return 1;
        var left = a.Value;
        var right = b.Value;
        int result = left.major.CompareTo(right.major);
        if (result != 0)
            return result;
        result = left.minor.CompareTo(right.minor);
        if (result != 0)
            return result;
        result = left.patch.CompareTo(right.patch);
        return result != 0 ? result : comparePreReleaseIdentifiers(left.prerelease, right.prerelease);
    }

    internal static int comparePreReleaseIdentifiers(List<string>? left, List<string>? right)
    {
        int leftCount = left?.Count ?? 0, rightCount = right?.Count ?? 0;
        if (leftCount == 0)
            return rightCount == 0 ? 0 : 1;
        if (rightCount == 0)
            return -1;
        for (int i = 0; i < Math.Min(leftCount, rightCount); i++)
        {
            int result = comparePreReleaseIdentifier(left![i], right![i]);
            if (result != 0)
                return result;
        }

        return leftCount.CompareTo(rightCount);
    }

    internal static int comparePreReleaseIdentifier(string left, string right)
    {
        // These identifiers can contain the two non-ASCII simple-fold characters
        // admitted by the upstream regex. Go lexical order is UTF-8 byte order.
        int result = CompareBytes(left, right);
        if (result == 0)
            return 0;
        bool leftNumeric = numericIdentifierRegExp.IsMatch(GoString.ToString(left)), rightNumeric = numericIdentifierRegExp.IsMatch(GoString.ToString(right));
        if (leftNumeric || rightNumeric)
        {
            if (!rightNumeric)
                return -1;
            if (!leftNumeric)
                return 1;
            var(leftNumber, leftError) = getUintComponent(left);
            var(rightNumber, rightError) = getUintComponent(right);
            if (leftError != null || rightError != null)
            {
                int lengths = left.Length.CompareTo(right.Length);
                return lengths == 0 ? result : lengths;
            }

            return leftNumber.CompareTo(rightNumber);
        }

        return result;
    }

    internal static int CompareBytes(string left, string right)
    {
        return Math.Sign(string.CompareOrdinal(left, right));
    }

    internal static (Version Version, Exception? Error) TryParseVersion(string text)
    {
        Version result = default;
        Match match = versionRegexp.Match(GoString.ToString(text));
        if (!match.Success)
            return (result, new SemverParseError(text));
        var(major, error) = getUintComponent(group(match, 1));
        result.major = major;
        if (error != null)
            return (result, error);
        string minorText = group(match, 2), patchText = group(match, 3);
        if (minorText != "")
        {
            (result.minor, error) = getUintComponent(minorText);
            if (error != null)
                return (result, error);
        }

        if (patchText != "")
        {
            (result.patch, error) = getUintComponent(patchText);
            if (error != null)
                return (result, error);
        }

        string pre = group(match, 4), build = group(match, 5);
        if (pre != "")
        {
            if (!prereleaseRegexp.IsMatch(GoString.ToString(pre)))
                return (result, new SemverParseError(text));
            result.prerelease = [..pre.Split('.')];
        }

        if (build != "")
        {
            if (!buildRegExp.IsMatch(GoString.ToString(build)))
                return (result, new SemverParseError(text));
            result.build = [..build.Split('.')];
        }

        return (result, null);
    }

    internal static Version MustParse(string text)
    {
        var(value, error) = TryParseVersion(text);
        if (error != null)
            throw error;
        return value;
    }

    internal static (uint Value, Exception? Error) getUintComponent(string text)
    {
        // strconv.ParseUint returns MaxUint32 as the value on overflow.
        if (uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out uint value))
            return (value, null);
        bool digits = text.Length > 0 && text.All(ch => ch is >= '0' and <= '9');
        return (digits ? uint.MaxValue : 0, new FormatException("strconv.ParseUint: parsing " + GoQuote.Quote(text) + (digits ? ": value out of range" : ": invalid syntax")));
    }
}
