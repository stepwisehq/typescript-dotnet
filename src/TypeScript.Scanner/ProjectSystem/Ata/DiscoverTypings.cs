// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.Vfs;
using TypeScript.ProjectSystem.Logging;
using TypeScript.PackageJson;

namespace TypeScript.ProjectSystem.Ata;
internal static partial class AutomaticTypeAcquisition
{
    // discovertypings.go:21.
    internal static bool IsTypingUpToDate(CachedTyping cached, Dictionary<ByteStringKey, ByteStringValue>? versions)
    {
        if (versions == null || !versions.TryGetValue(ByteStringKey.FromString("ts" + Core.VersionMajorMinor()), out var version))
            version = versions?.GetValueOrDefault(ByteStringKey.FromString("latest"), ByteStringValue.FromString("")) ?? ByteStringValue.FromString("");
        return TypeScript.Semver.Semver.MustParse(version.Materialize()!).Compare(cached.Version) <= 0;
    }

    // discovertypings.go:30. Map keys retain raw names; path values retain original owners.
    internal static (Slice<RootedFilePath> CachedPaths, Slice<ByteStringValue> NewNames, Slice<RootedPath> FilesToWatch) DiscoverTypings(IFS fs, ILogger logger, TypingsInfo info, Slice<RootedFilePath> fileNames, RootedDirectoryPath root, SyncMap<ByteStringKey, CachedTyping> cached, Dictionary<ByteStringKey, Dictionary<ByteStringKey, ByteStringValue>?>? registry)
    {
        var inferred = new Dictionary<ByteStringKey, RootedFilePath>();
        fileNames = Core.Filter(fileNames, static file => file.HasJSFileExtension());
        var acquisition = info.TypeAcquisition!;
        if (acquisition.Include != null)
            AddInferredTypings(fs, logger, inferred, acquisition.Include, "Explicitly included types");
        Slice<RootedPath> watch = default;
        if (info.CompilerOptions.Types == null)
        {
            var directories = new HashSet<RootedDirectoryPath>();
            foreach (var file in fileNames)
                directories.Add(file.Directory());
            directories.Add(root);
            foreach (var directory in directories)
            {
                watch = AddTypingNamesAndGetFilesToWatch(fs, logger, inferred, watch, directory, "bower.json", "bower_components");
                watch = AddTypingNamesAndGetFilesToWatch(fs, logger, inferred, watch, directory, "package.json", "node_modules");
            }
        }

        if (acquisition.DisableFilenameBasedTypeAcquisition != Tristate.True)
            GetTypingNamesFromSourceFileNames(fs, logger, inferred, fileNames);
        Slice<ByteStringValue> modules = default;
        if (info.UnresolvedImports != null)
        {
            modules = Slice<ByteStringValue>.Make(0, info.UnresolvedImports.Count);
            foreach (var name in info.UnresolvedImports)
                modules = modules.Append(Core.NonRelativeModuleNameForTypingCache(name.Value));
            GoSlices.SortFunc(modules, static (a, b) => ByteStringKey.FromValue(a).CompareTo(ByteStringKey.FromValue(b)));
            modules = CompactNames(modules);
        }

        AddInferredTypings(fs, logger, inferred, modules, "Inferred typings from unresolved imports");
        foreach (var name in acquisition.Exclude ?? [])
        {
            inferred.Remove(ByteStringKey.FromString(name));
            logger.Logf("ATA:: Typing for %s is in exclude list, will be ignored.", name);
        }

        cached.Range((inferred, registry), static (name, typing, state) =>
        {
            if (state.inferred.GetValueOrDefault(name).AsValue().Length == 0 && state.registry != null && state.registry.TryGetValue(name, out var versions) && versions != null && IsTypingUpToDate(typing, versions))
                state.inferred[name] = typing.TypingsLocation;
            return true;
        });
        Slice<RootedFilePath> paths = default;
        Slice<ByteStringValue> names = default;
        foreach (var(name, path)in inferred)
            if (path.AsValue().Length != 0)
                paths = paths.Append(path);
            else
                names = names.Append(name.Value);
        logger.Logf("ATA:: Finished typings discovery: cachedTypingsPaths: %v newTypingNames: %v, filesToWatch %v", paths, names, watch);
        return (paths, names, watch);
    }

    // discovertypings.go:107.
    private static void AddInferredTyping(Dictionary<ByteStringKey, RootedFilePath> inferred, ByteStringValue name) => inferred.TryAdd(ByteStringKey.FromValue(name), default);
    // discovertypings.go:113. The filesystem parameter is retained from the original signature.
    private static void AddInferredTypings(IFS fs, ILogger logger, Dictionary<ByteStringKey, RootedFilePath> inferred, Slice<string> names, string message)
    {
        logger.Logf("ATA:: %s: %v", message, names);
        foreach (var name in names)
            AddInferredTyping(inferred, ByteStringValue.FromString(name));
    }

    // Existing compiler option lists are retained directly; no bridging slice copy.
    private static void AddInferredTypings(IFS fs, ILogger logger, Dictionary<ByteStringKey, RootedFilePath> inferred, List<string> names, string message)
    {
        logger.Logf("ATA:: %s: %v", message, names);
        foreach (var name in names)
            AddInferredTyping(inferred, ByteStringValue.FromString(name));
    }

    private static void AddInferredTypings(IFS fs, ILogger logger, Dictionary<ByteStringKey, RootedFilePath> inferred, Slice<ByteStringValue> names, string message)
    {
        logger.Logf("ATA:: %s: %v", message, names);
        foreach (var name in names)
            AddInferredTyping(inferred, name);
    }

    // discovertypings.go:131.
    private static void GetTypingNamesFromSourceFileNames(IFS fs, ILogger logger, Dictionary<ByteStringKey, RootedFilePath> inferred, Slice<RootedFilePath> files)
    {
        var hasJsx = false;
        Slice<ByteStringValue> names = default;
        foreach (var file in files)
        {
            hasJsx = hasJsx || file.ExtensionIs(".jsx");
            var cleaned = RemoveMinAndVersionNumbers(TsPath.RemoveFileExtension(TsPath.ToFileNameLowerCaseGoBytes(file.BaseNameValue())));
            if (safeFileNameToTypeName.TryGetValue(ByteStringKey.FromValue(cleaned), out var name))
                names = names.Append(name);
        }

        if (names.Count > 0)
            AddInferredTypings(fs, logger, inferred, names, "Inferred typings from file names");
        if (hasJsx)
        {
            logger.Log("ATA:: Inferred 'react' typings due to presence of '.jsx' extension");
            AddInferredTyping(inferred, ByteStringValue.FromString("react"));
        }
    }

    // discovertypings.go:164.
    private static Slice<RootedPath> AddTypingNamesAndGetFilesToWatch(IFS fs, ILogger logger, Dictionary<ByteStringKey, RootedFilePath> inferred, Slice<RootedPath> watch, RootedDirectoryPath root, string manifestName, string modulesName)
    {
        var manifestPath = root.ResolveFile(manifestName);
        Slice<string> manifestNames = default;
        var(content, found) = fs.ReadFile(manifestPath);
        if (found)
        {
            watch = watch.Append(manifestPath.AsPath());
            var(manifest, error) = PackageJsonParser.ParseFields(GoString.Bytes(content));
            if (error == null)
            {
                AppendNames(ref manifestNames, manifest.DependencyFields.Dependencies.Value);
                AppendNames(ref manifestNames, manifest.DependencyFields.DevDependencies.Value);
                AppendNames(ref manifestNames, manifest.DependencyFields.OptionalDependencies.Value);
                AppendNames(ref manifestNames, manifest.DependencyFields.PeerDependencies.Value);
                AddInferredTypings(fs, logger, inferred, manifestNames, "Typing names in '" + manifestPath.AsString() + "' dependencies");
            }
        }

        var folder = root.ResolveDirectory(modulesName);
        watch = watch.Append(folder.AsPath());
        if (!fs.DirectoryExists(folder))
            return watch;
        Slice<string> packageNames = default;
        Slice<RootedFilePath> dependencyManifests = default;
        if (manifestNames.Count > 0)
        {
            foreach (var name in manifestNames)
                dependencyManifests = dependencyManifests.Append(folder.ResolveFile(TsPath.CombinePaths(name, manifestName)));
        }
        else
        {
            foreach (var path in TypeScript.Vfs.VfsMatch.VfsMatch.ReadDirectory(fs, folder, [".json"], (List<PathPattern>? )null, null, 3))
            {
                if (!path.BaseNameValue().EqualsValue(ByteStringValue.FromString(manifestName)))
                    continue;
                var components = path.ComponentsValues();
                var count = components.Count;
                var part = components[count - 3];
                var(first, _) = part.IsBytes ? Utf8.DecodeRuneInString(part.ByteSpan) : Utf8.DecodeRuneInString(part.CharSpan);
                var scoped = first == '@';
                if (scoped && TsPath.ToFileNameLowerCaseGoBytes(components[count - 4]).EqualsValue(ByteStringValue.FromString(modulesName)) || !scoped && TsPath.ToFileNameLowerCaseGoBytes(components[count - 3]).EqualsValue(ByteStringValue.FromString(modulesName)))
                    dependencyManifests = dependencyManifests.Append(path);
            }
        }

        logger.Logf("ATA:: Searching for typing names in %s; all files: %v", folder, dependencyManifests);
        foreach (var path in dependencyManifests)
        {
            var(packageContent, exists) = fs.ReadFile(path);
            if (!exists)
                continue;
            var(manifest, error) = PackageJsonParser.ParseFields(GoString.Bytes(packageContent));
            if (error != null || string.IsNullOrEmpty(manifest.Name.Value))
                continue;
            var ownTypes = manifest.Types.Value;
            if (string.IsNullOrEmpty(ownTypes))
                ownTypes = manifest.Typings.Value;
            if (!string.IsNullOrEmpty(ownTypes))
            {
                var absolute = path.Directory().ResolveFile(ownTypes);
                if (fs.FileExists(absolute))
                {
                    logger.Logf("ATA::     Package '%s' provides its own types.", manifest.Name.Value);
                    inferred[ByteStringKey.FromString(manifest.Name.Value)] = absolute;
                }
                else
                    logger.Logf("ATA::     Package '%s' provides its own types but they are missing.", manifest.Name.Value);
            }
            else
                packageNames = packageNames.Append(manifest.Name.Value);
        }

        AddInferredTypings(fs, logger, inferred, packageNames, "    Found package names");
        return watch;
    }

    // Go slices.Compact over borrowed name headers; clear discarded tail owners.
    private static Slice<ByteStringValue> CompactNames(Slice<ByteStringValue> names)
    {
        if (names.Count < 2)
            return names;
        var write = 1;
        for (var read = 1; read < names.Count; read++)
            if (!names[read].EqualsValue(names[write - 1]))
                names[write++] = names[read];
        names.AsSpan()[write..].Clear();
        return names.Slice2(0, write);
    }

    private static void AppendNames(ref Slice<string> names, Dictionary<string, string>? map)
    {
        if (map != null)
            foreach (var name in map.Keys)
                names = names.Append(name);
    }

    // internal/project/ata/discovertypings.go:291 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
    // This backward scan returns a retained substring header, exactly as Go does.
    internal static ByteStringValue RemoveMinAndVersionNumbers(ByteStringValue fileName)
    {
        var end = fileName.Length;
        for (var position = end; position > 0;)
        {
            var(ch, size) = LastRune(fileName, position);
            if (ch >= '0' && ch <= '9')
            {
                while (true)
                {
                    position -= size;
                    (ch, size) = LastRune(fileName, position);
                    if (position <= 0 || ch < '0' || ch > '9')
                        break;
                }
            }
            else if (position > 4 && (ch == 'n' || ch == 'N'))
            {
                position -= size;
                (ch, size) = LastRune(fileName, position);
                if (ch != 'i' && ch != 'I')
                    break;
                position -= size;
                (ch, size) = LastRune(fileName, position);
                if (ch != 'm' && ch != 'M')
                    break;
                position -= size;
                (ch, size) = LastRune(fileName, position);
            }
            else
            {
                break;
            }

            if (ch != '-' && ch != '.')
                break;
            position -= size;
            end = position;
        }

        return fileName.Slice(0, end);
    }

    private static (int Rune, int Size) LastRune(ByteStringValue source, int end) => source.IsBytes ? Utf8.DecodeLastRuneInString(source.ByteSpan[..end]) : Utf8.DecodeLastRuneInString(source.CharSpan[..end]);
}
