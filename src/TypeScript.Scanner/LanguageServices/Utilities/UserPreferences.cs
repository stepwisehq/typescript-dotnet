// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/ls/lsutil/userpreferences.go preference owner/configuration algorithms.
#nullable disable
using TypeScript.Scanner;
using TypeScript.ModuleSpecifiers;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    // userpreferences.go:116. Unknown and malformed values preserve the original tri-state distinction.
    internal static Tristate parsePreferenceTristate(object value) => value is bool boolean ? (boolean ? Tristate.True : Tristate.False) : Tristate.Unknown;
    internal static Tristate parseOrganizeImportsCaseSensitivity(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "caseinsensitive":
                    return Tristate.True;
                case "casesensitive":
                    return Tristate.False;
            }
        }

        return parsePreferenceTristate(value);
    }

    internal static object InvertPreferenceValue(object value) => value is bool boolean ? !boolean : value;
    // userpreferences.go:144. First existing path wins, even if the retained value is nil.
    internal static bool GetPreferenceConfigValue(Dictionary<string, object> config, out object value, params string[] paths)
    {
        foreach (var tag in paths)
        {
            int comma = tag.IndexOf(',');
            var path = comma < 0 ? tag : tag[..comma];
            if (GetNestedValue(config, path, out value))
            {
                if (comma >= 0 && tag[(comma + 1)..] == "invert")
                    value = InvertPreferenceValue(value);
                return true;
            }
        }

        value = null;
        return false;
    }

    internal static bool GetNestedValue(Dictionary<string, object> config, string path, out object value)
    {
        object current = config;
        // Go strings.SplitSeq retains one segment at a time rather than allocating a split array.
        int start = 0;
        while (true)
        {
            int separator = path.IndexOf('.', start);
            var part = separator < 0 ? path[start..] : path[start..separator];
            if (current is not Dictionary<string, object> map || !map.TryGetValue(part, out current))
            {
                value = null;
                return false;
            }

            if (separator < 0)
                break;
            start = separator + 1;
        }

        value = current;
        return true;
    }

    internal static void SetNestedValue(Dictionary<string, object> config, string path, object value)
    {
        var parts = path.Split('.');
        var current = config;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!current.TryGetValue(parts[i], out var item) || item is not Dictionary<string, object> next)
            {
                next = new Dictionary<string, object>(StringComparer.Ordinal);
                current[parts[i]] = next;
            }

            current = next;
        }

        current[parts[^1]] = value;
    }

    // userpreferences.go:176. Go Unicode whitespace, including byte-string advancement, rather than CLR Trim.
    internal static string NormalizeCustomConfigFileName(string name)
    {
        var value = ByteStringValue.FromString(name);
        int start = 0, end = value.Length;
        while (start < end)
        {
            var(rune, size) = Utf8.DecodeRuneInString(value.Slice(start).CharSpan);
            if (!GoUnicode.IsSpace(rune))
                break;
            start += size;
        }

        int trailingStart = end;
        for (int i = start; i < end;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(value.Slice(i).CharSpan);
            i += size;
            if (!GoUnicode.IsSpace(rune))
                trailingStart = i;
        }

        name = value.Slice(start, trailingStart).Materialize();
        return name.Contains('/') || name.Contains('\\') || name is "." or ".." ? "" : name;
    }

    internal static string[] StringSliceToArray(Slice<string> values)
    {
        var result = new string[values.Count];
        for (int i = 0; i < values.Count; i++)
            result[i] = values[i];
        return result;
    }

    internal static string ModulePreferenceText(ImportModuleSpecifierPreference value) => value.Value;
    internal static string ModulePreferenceText(ImportModuleSpecifierEndingPreference value) => value.Value;
    // userpreferences.go:242. Original editor < javascript < typescript < js/ts precedence.
    internal static UserPreferences ParseUserPreferences(Dictionary<string, object> items)
    {
        var prefs = NewDefaultUserPreferences();
        if (items == null)
            return prefs;
        if (items.TryGetValue("editor", out var editor) && editor is Dictionary<string, object> settings)
        {
            var normalized = new Dictionary<string, object>(settings.Count + 2, StringComparer.Ordinal);
            foreach (var item in settings)
                normalized[item.Key] = item.Value;
            if (normalized.TryGetValue("tabSize", out var tabSize) && !normalized.ContainsKey("indentSize"))
                normalized["indentSize"] = tabSize;
            if (normalized.TryGetValue("insertSpaces", out var insertSpaces) && !normalized.ContainsKey("convertTabsToSpaces"))
                normalized["convertTabsToSpaces"] = insertSpaces;
            prefs = prefs.WithConfig(new Dictionary<string, object>(StringComparer.Ordinal) { ["unstable"] = normalized });
        }

        foreach (var section in new[]
        {
            "javascript",
            "typescript",
            "js/ts"
        }

        )
            if (items.TryGetValue(section, out var item) && item is Dictionary<string, object> config)
                prefs = prefs.WithConfig(config);
        return prefs;
    }
}

internal partial struct UserPreferences
{
    // userpreferences.go:204. Unified ATA preference takes precedence over the deprecated option.
    internal readonly bool IsATADisabled() => AutomaticTypeAcquisitionEnabled != Tristate.Unknown ? AutomaticTypeAcquisitionEnabled != Tristate.True : DisableAutomaticTypeAcquisition == Tristate.True;
    // userpreferences.go:213. The module-specifier consumer borrows the same exclusion slice header.
    internal readonly TypeScript.ModuleSpecifiers.UserPreferences ModuleSpecifierPreferences() => new()
    {
        ImportModuleSpecifierPreference = ImportModuleSpecifierPreference,
        ImportModuleSpecifierEnding = ImportModuleSpecifierEnding,
        AutoImportSpecifierExcludeRegexes = AutoImportSpecifierExcludeRegexes
    };
    internal readonly TypeScript.Vfs.VfsMatch.SpecMatcher ParsedAutoImportFileExcludePatterns(CaseSensitivity sensitivity) => TypeScript.Vfs.VfsMatch.VfsMatch.NewSpecMatcher(AutoImportFileExcludePatterns, default, TypeScript.Vfs.VfsMatch.Usage.Exclude, sensitivity);
    internal readonly bool IsModuleSpecifierExcluded(string specifier) => TypeScript.ModuleSpecifiers.ModuleSpecifiers.IsExcludedByRegex(specifier, AutoImportSpecifierExcludeRegexes);
    // userpreferences.go:189. Only publish defaults/configuration after a successful actual JSON decode.
    internal void UnmarshalJSONFrom(string rawGoJson)
    {
        var(value, error) = TypeScript.Json.JsonCodec.ParseDictionary(rawGoJson);
        if (error != null)
            throw error;
        this = LsUtil.NewDefaultUserPreferences().WithConfig((Dictionary<string, object>)value);
    }

    internal readonly string MarshalJSONTo() => TypeScript.Json.JsonCodec.MarshalStringDeterministic(ToConfiguration());
}
