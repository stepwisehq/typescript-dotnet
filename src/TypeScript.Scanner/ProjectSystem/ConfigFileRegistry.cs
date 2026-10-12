// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/configfileregistry.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Lsp.Protocol;

namespace TypeScript.ProjectSystem;
internal sealed class ConfiguredContentMappers
{
    internal Slice<ByteStringValue> Extensions;
    // configfileregistry.go:34. Exact first-seen uniqueness then raw-byte lexical sort.
    internal static ConfiguredContentMappers Collect(Slice<ParsedCommandLine> commandLines)
    {
        var seen = new HashSet<ByteStringKey>();
        Slice<ByteStringValue> extensions = default;
        foreach (var commandLine in commandLines)
            if (commandLine.ContentMappers()is { } mappers)
                foreach (var mapper in mappers)
                    if (mapper.Extensions is { } mapperExtensions)
                        foreach (var extension in mapperExtensions)
                        {
                            var text = ByteStringValue.FromString(extension);
                            if (seen.Add(ByteStringKey.FromValue(text)))
                                extensions = extensions.Append(text);
                        }

        extensions.AsSpan().Sort(static (left, right) => ByteStringKey.FromValue(left).CompareTo(ByteStringKey.FromValue(right)));
        return new()
        {
            Extensions = extensions
        };
    }
}

internal sealed class ConfigFileEntry : Dirty.ICloneable<ConfigFileEntry>
{
    internal RootedFilePath FileName;
    internal PendingReload PendingReload;
    internal ParsedCommandLine? CommandLine;
    internal HashSet<ID>? RetainingProjects;
    internal HashSet<PathKey>? RetainingOpenFiles, RetainingConfigs;
    internal WatchedFiles<PatternsAndIgnored>? RootFilesWatch;
    // configfileregistry.go:98 newConfigFileEntry.
    internal static ConfigFileEntry New(bool relativePatterns, RootedFilePath name) => new()
    {
        FileName = name,
        PendingReload = PendingReload.Full,
        RootFilesWatch = new(ByteStringValue.Concat(ByteStringValue.FromString("root files for "), name.AsValue()), WatchKind.Create | WatchKind.Change | WatchKind.Delete, relativePatterns, static value => value)
    };
    // configfileregistry.go:111 newExtendedConfigFileEntry.
    internal static ConfigFileEntry NewExtended(RootedFilePath name, PathKey extendingConfig) => new()
    {
        FileName = name,
        PendingReload = PendingReload.Full,
        RetainingConfigs = new()
        {
            extendingConfig
        }
    };
    // configfileregistry.go:119 Clone: sets copy eagerly; watcher and command line owners remain shared.
    public ConfigFileEntry Clone() => new()
    {
        FileName = FileName,
        PendingReload = PendingReload,
        CommandLine = CommandLine,
        RootFilesWatch = RootFilesWatch,
        RetainingProjects = RetainingProjects is null ? null : new(RetainingProjects, RetainingProjects.Comparer),
        RetainingOpenFiles = RetainingOpenFiles is null ? null : new(RetainingOpenFiles, RetainingOpenFiles.Comparer),
        RetainingConfigs = RetainingConfigs is null ? null : new(RetainingConfigs, RetainingConfigs.Comparer)
    };
}

internal sealed class ConfigFileNames : Dirty.ICloneable<ConfigFileNames>
{
    internal RootedFilePath NearestConfigFileName;
    internal Dictionary<RootedFilePath, RootedFilePath>? Ancestors;
    public ConfigFileNames Clone() => new()
    {
        NearestConfigFileName = NearestConfigFileName,
        Ancestors = Ancestors is null ? null : new(Ancestors, Ancestors.Comparer)
    };
}

internal readonly record struct TestConfigEntry(RootedFilePath FileName, IEnumerable<ID> RetainingProjects, IEnumerable<PathKey> RetainingOpenFiles, IEnumerable<PathKey> RetainingConfigs);
internal readonly record struct TestConfigFileNamesEntry(RootedFilePath NearestConfigFileName, Dictionary<RootedFilePath, RootedFilePath>? Ancestors);
internal sealed class ConfigFileRegistry
{
    internal Dictionary<PathKey, ConfigFileEntry>? Configs;
    internal Dictionary<PathKey, ConfigFileNames>? Names;
    internal ByteStringValue CustomConfigFileName;
    internal ConfiguredContentMappers? AllConfiguredContentMappers;
    // configfileregistry.go:53 contentMappers.
    internal ConfiguredContentMappers ContentMappers()
    {
        if (AllConfiguredContentMappers is not null)
            return AllConfiguredContentMappers;
        Slice<ParsedCommandLine> commands = default;
        if (Configs is not null)
            foreach (var entry in Configs.Values)
                if (entry.CommandLine is { } command)
                    commands = commands.Append(command);
        return ConfiguredContentMappers.Collect(commands);
    }

    internal ParsedCommandLine? GetConfig(PathKey path) => Configs?.GetValueOrDefault(path)?.CommandLine;
    internal bool IsTracked(PathKey path) => Configs?.ContainsKey(path) == true;
    internal RootedFilePath GetConfigFileName(PathKey path) => Names?.GetValueOrDefault(path)?.NearestConfigFileName ?? default;
    internal RootedFilePath GetAncestorConfigFileName(PathKey path, RootedFilePath higherThanConfig) => Names?.GetValueOrDefault(path)?.Ancestors?.GetValueOrDefault(higherThanConfig) ?? default;
    // configfileregistry.go:149 clone: map owners copy but entries remain shared.
    internal ConfigFileRegistry Clone() => new()
    {
        Configs = Configs is null ? null : new(Configs, Configs.Comparer),
        Names = Names is null ? null : new(Names, Names.Comparer),
        CustomConfigFileName = CustomConfigFileName,
        AllConfiguredContentMappers = AllConfiguredContentMappers
    };
    private static TestConfigEntry TestEntry(ConfigFileEntry entry) => new(entry.FileName, entry.RetainingProjects ?? (IEnumerable<ID>)Array.Empty<ID>(), entry.RetainingOpenFiles ?? (IEnumerable<PathKey>)Array.Empty<PathKey>(), entry.RetainingConfigs ?? (IEnumerable<PathKey>)Array.Empty<PathKey>());
    // configfileregistry.go:166/181/203/217. These observations retain original map-header roles.
    internal void ForEachTestConfigEntry(Action<PathKey, TestConfigEntry> callback)
    {
        if (Configs is not null)
            foreach (var(path, entry)in Configs)
                callback(path, TestEntry(entry));
    }

    internal TestConfigEntry? GetTestConfigEntry(PathKey path) => Configs?.TryGetValue(path, out var entry) == true ? TestEntry(entry) : null;
    internal void ForEachTestConfigFileNamesEntry(Action<PathKey, TestConfigFileNamesEntry> callback)
    {
        if (Names is not null)
            foreach (var(path, entry)in Names)
                callback(path, new(entry.NearestConfigFileName, entry.Ancestors));
    }

    internal TestConfigFileNamesEntry? GetTestConfigFileNamesEntry(PathKey path) => Names?.TryGetValue(path, out var entry) == true ? new(entry.NearestConfigFileName, entry.Ancestors) : null;
}
