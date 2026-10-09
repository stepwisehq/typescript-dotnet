// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/commandlineoption.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public enum CommandLineOptionKind
{
    None,
    String,
    Number,
    Boolean,
    Object,
    List,
    ListOrElement,
    Enum
}

public enum CommandLineOptionPathKind : byte
{
    None,
    File,
    Directory,
    FileOrDirectory,
    SourceMapLocation,
    FileSpec,
    PathPattern,
    ResolvedPathPattern,
    ConfigLocator
}

internal enum ExtraValidation
{
    None,
    Locale
}

public static class CommandLineOptionKinds
{
    public static string String(this CommandLineOptionKind kind) => kind switch
    {
        CommandLineOptionKind.None => "",
        CommandLineOptionKind.String => "string",
        CommandLineOptionKind.Number => "number",
        CommandLineOptionKind.Boolean => "boolean",
        CommandLineOptionKind.Object => "object",
        CommandLineOptionKind.List => "list",
        CommandLineOptionKind.ListOrElement => "listOrElement",
        CommandLineOptionKind.Enum => "enum",
        _ => throw new InvalidOperationException("invalid command line option kind"),
    };
    public static bool IsRooted(this CommandLineOptionPathKind kind) => kind is CommandLineOptionPathKind.File or CommandLineOptionPathKind.Directory or CommandLineOptionPathKind.FileOrDirectory or CommandLineOptionPathKind.ResolvedPathPattern;
    public static bool IsFileSystemPath(this CommandLineOptionPathKind kind) => kind is CommandLineOptionPathKind.File or CommandLineOptionPathKind.Directory or CommandLineOptionPathKind.FileOrDirectory;
}

public sealed class CommandLineOption
{
    public string Name = "", ShortName = "";
    public CommandLineOptionKind Kind;
    public CommandLineOptionPathKind PathKind;
    public bool IsFilePath, IsTSConfigOnly, IsCommandLineOnly;
    public Message? Description;
    public object? DefaultValueDescription;
    public bool ShowInSimplifiedHelpView;
    public Message? Category;
    internal ExtraValidation extraValidation;
    internal long minValue;
    internal bool listPreserveFalsyValues;
    public CommandLineOptionNameMap? ElementOptions;
    public HashSet<string>? DeprecatedKeys() => Kind == CommandLineOptionKind.Enum && TsOptions.commandLineOptionDeprecated.TryGetValue(Name, out var value) ? value : null;
    public OrderedMap<string, object?>? EnumMap() => Kind == CommandLineOptionKind.Enum && TsOptions.commandLineOptionEnumMap.TryGetValue(Name, out var value) ? value : null;
    public CommandLineOption? Elements() => (Kind is CommandLineOptionKind.List or CommandLineOptionKind.ListOrElement) && TsOptions.commandLineOptionElements.TryGetValue(Name, out var value) ? value : null;
    public bool DisallowNullOrUndefined() => Name == "extends";
}

public static partial class TsOptions
{
    public static (string Value, bool Ok) PathValueAsString(object? value) => value switch
    {
        RootedFilePath path => (path.AsString(), true),
        RootedDirectoryPath path => (path.AsString(), true),
        RootedPath path => (path.AsString(), true),
        SourceMapLocation path => (path.AsString(), true),
        _ => ("", false),
    };
    public static (List<string>? Values, bool Ok) PathValuesAsStrings(object? value) => value switch
    {
        List<RootedFilePath> paths => (paths.Select(path => path.AsString()).ToList(), true),
        List<RootedDirectoryPath> paths => (paths.Select(path => path.AsString()).ToList(), true),
        _ => (null, false),
    };
}
