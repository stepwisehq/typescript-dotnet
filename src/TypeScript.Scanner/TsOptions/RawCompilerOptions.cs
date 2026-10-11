// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/rawcompileroptions.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Raw ordered-tree JSON reader/writer adapters replace the original encoder interface, without reflection.
using TypeScript.Collections;
using TypeScript.Json;
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public sealed class RawCompilerOptions
{
    internal OrderedMap<string, object?>? values;
    public byte[] MarshalJSON() => JsonCodec.Marshal(values ?? new OrderedMap<string, object?>(0));
    public Exception? UnmarshalJSON(string goBytes)
    {
        values ??= new(0);
        return JsonCodec.UnmarshalObject(goBytes, values, "cannot unmarshal non-object JSON value into RawCompilerOptions");
    }

    public Exception? UnmarshalJSON(byte[] bytes) => UnmarshalJSON(GoString.FromBytes(bytes));
    public (CompilerOptions Options, Slice<Diagnostic> Diagnostics) Finalize(RootedDirectoryPath basePath)
    {
        var options = new CompilerOptions();
        if (values == null)
            return (options, default);
        foreach (var(key, item)in values.Entries())
        {
            var option = TsOptions.CommandLineCompilerOptionsMap.Get(key);
            if (option != null && key != option.Name)
                continue;
            var pathKind = CommandLineOptionPathKind.None;
            if (option != null)
            {
                pathKind = option.PathKind;
                if (option.Kind == CommandLineOptionKind.List && option.Elements()is { } element)
                    pathKind = element.PathKind;
            }
            else if (key == "configFilePath")
                pathKind = CommandLineOptionPathKind.File;
            else if (key != "allowNonTsExtensions" && key != "suppressOutputPathCheck")
                continue;
            object? value = item;
            if (pathKind.IsRooted())
            {
                if (option?.Kind == CommandLineOptionKind.List)
                    value = TsOptions.ParseStringArray(value)?.Select(path => (object? )(TsOptions.startsWithConfigDirTemplate(path) ? TsOptions.getSubstitutedPathWithConfigDirTemplate(path, basePath) : TsPath.GetNormalizedAbsolutePath(path, basePath.AsString()))).ToList();
                else if (value is string path && path != "")
                    value = TsOptions.startsWithConfigDirTemplate(path) ? TsOptions.getSubstitutedPathWithConfigDirTemplate(path, basePath) : TsPath.GetNormalizedAbsolutePath(path, basePath.AsString());
            }

            TsOptions.ParseCompilerOptions(key, value, options);
        }

        return (options, default);
    }
}
