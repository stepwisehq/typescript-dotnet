// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/namemap.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public sealed class NameMap
{
    internal OrderedMap<string, CommandLineOption?> optionsNames = new();
    internal Dictionary<string, string> shortOptionNames = new(StringComparer.Ordinal);
    public CommandLineOption? Get(string name) => optionsNames.GetOrZero(TsOptions.goLower(name));
    public CommandLineOption? GetFromShort(string shortName) => shortOptionNames.TryGetValue(shortName, out var name) ? Get(name) : null;
    public CommandLineOption? GetOptionDeclarationFromName(string optionName, bool allowShort)
    {
        optionName = TsOptions.goLower(optionName);
        if (allowShort && shortOptionNames.TryGetValue(optionName, out var name) && name != "")
            optionName = name;
        return Get(optionName);
    }
}

public static partial class TsOptions
{
    public static NameMap GetNameMapFromList(List<CommandLineOption>? declarations)
    {
        var result = new NameMap
        {
            optionsNames = new(declarations?.Count ?? 0)
        };
        foreach (var option in declarations ?? [])
        {
            result.optionsNames.Set(goLower(option.Name), option);
            if (option.ShortName != "")
                result.shortOptionNames[option.ShortName] = option.Name;
        }

        return result;
    }

    // strings.ToLower consumes UTF8 bytes and uses the existing pinned Go Unicode tables.
    internal static string goLower(string bytes)
    {
        var result = new GoBuilder();
        foreach (int rune in GoString.EnumerateRunes(bytes))
            result.WriteRune(UnicodeCase.ToLower(rune));
        return result.String();
    }
}
