// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of internal/project/ata/ata.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.Modules;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Vfs;
using TypeScript.Json;
using TypeScript.Json.Internal;

namespace TypeScript.ProjectSystem.Ata;
internal sealed class TypingsInstaller
{
    private readonly RootedDirectoryPath typingsLocation;
    private readonly IFS fs;
    private readonly INpmExecutor npmExecutor;
    private readonly SemaphoreSlim concurrencySemaphore;
    private readonly SemaphoreSlim initializationGate = new(1, 1);
    private int initialized;
    private readonly SyncMap<ByteStringKey, CachedTyping> packageNameToTypingLocation = new();
    private readonly SyncMap<ByteStringKey, bool> missingTypingsSet = new();
    private Dictionary<ByteStringKey, Dictionary<ByteStringKey, ByteStringValue>?>? typesRegistry;
    private int installRunCount;
    // Existing upstream TODO: installation currently uses latest instead of the TS version.
    private const string tsVersionToUse = "latest";
    // ata.go:76. Zero throttle capacity retains the original blocking behavior.
    internal static TypingsInstaller NewTypingsInstaller(TypingsInstallerOptions options, IFS fs, INpmExecutor npmExecutor) => new(options, fs, npmExecutor);
    private TypingsInstaller(TypingsInstallerOptions options, IFS fs, INpmExecutor npmExecutor)
    {
        typingsLocation = options.TypingsLocation;
        this.fs = fs;
        this.npmExecutor = npmExecutor;
        var capacity = checked((int)options.ThrottleLimit);
        concurrencySemaphore = new(capacity, Math.Max(1, capacity));
    }

    // ata.go:89.
    internal bool IsKnownTypesPackageName(IAtaProjectID projectID, ByteStringValue name, IFS fs, ILogger logger)
    {
        if (AutomaticTypeAcquisition.ValidatePackageName(name).Result != NameValidationResult.NameOk)
            return false;
        Init(CancellationToken.None, fs, logger);
        return typesRegistry?.ContainsKey(ByteStringKey.FromValue(name)) == true;
    }

    // ata.go:118.
    internal (TypingsInstallResult? Result, Exception? Error) InstallTypings(CancellationToken context, TypingsInstallRequest request)
    {
        var(result, error) = DiscoverAndInstallTypings(context, request);
        if (error == null)
        {
            GoSlices.SortFunc(result!.TypingsFiles, static (a, b) => ByteStringKey.FromValue(a.AsValue()).CompareTo(ByteStringKey.FromValue(b.AsValue())));
            GoSlices.SortFunc(result.FilesToWatch, static (a, b) => ByteStringKey.FromValue(a.AsValue()).CompareTo(ByteStringKey.FromValue(b.AsValue())));
            request.Logger.Log("ATA:: Got install request for: " + request.ProjectID.GoLogString());
        }

        return (result, error);
    }

    // ata.go:128.
    private (TypingsInstallResult? Result, Exception? Error) DiscoverAndInstallTypings(CancellationToken context, TypingsInstallRequest request)
    {
        Init(context, request.FS, request.Logger);
        var(cachedPaths, newNames, watch) = AutomaticTypeAcquisition.DiscoverTypings(request.FS, request.Logger, request.TypingsInfo, request.FileNames, request.ProjectRootPath, packageNameToTypingLocation, typesRegistry);
        var requestID = Interlocked.Increment(ref installRunCount);
        if (newNames.Count > 0)
        {
            var filtered = FilterTypings(request.Logger, newNames);
            if (filtered.Count != 0)
            {
                var(files, error) = InstallTypings(context, requestID, cachedPaths, filtered, request.Logger);
                return error != null ? (null, error) : (new() { TypingsFiles = files, FilesToWatch = watch }, null);
            }

            request.Logger.Log("ATA:: All typings are known to be missing or invalid - no need to install more typings");
        }
        else
            request.Logger.Log("ATA:: No new typings were requested as a result of typings discovery");
        // Upstream progress/setTypings event TODOs are not implemented there either.
        return (new() { TypingsFiles = cachedPaths, FilesToWatch = watch }, null);
    }

    // ata.go:168.
    private (Slice<RootedFilePath> Files, Exception? Error) InstallTypings(CancellationToken context, int requestID, Slice<RootedFilePath> cached, Slice<ByteStringValue> filtered, ILogger logger)
    {
        var scoped = Slice<ByteStringValue>.Make(filtered.Count);
        for (var i = 0; i < filtered.Count; i++)
            scoped[i] = ByteStringValue.Concat(ByteStringValue.FromString("@types/"), filtered[i], ByteStringValue.FromString("@" + tsVersionToUse));
        var(packages, success) = InstallWorker(context, requestID, scoped, logger);
        if (success)
        {
            logger.Logf("ATA:: Installed typings %v", packages);
            Slice<RootedFilePath> installed = default;
            var resolver = NewResolver();
            foreach (var name in filtered)
            {
                var file = TypingToFileName(resolver, name);
                var key = ByteStringKey.FromValue(name);
                if (file.AsValue().Length == 0)
                {
                    logger.Logf("ATA:: Failed to find typing file for package '%s'", name);
                    missingTypingsSet.Store(key, true);
                    continue;
                }

                var tags = typesRegistry![key];
                if (tags == null || !tags.TryGetValue(ByteStringKey.FromString("ts" + Core.VersionMajorMinor()), out var version))
                    version = tags?.GetValueOrDefault(ByteStringKey.FromString("latest"), ByteStringValue.FromString("")) ?? ByteStringValue.FromString("");
                var parsed = TypeScript.Semver.Semver.MustParse(version.Materialize()!);
                packageNameToTypingLocation.Store(key, new() { TypingsLocation = file, Version = parsed });
                installed = installed.Append(file);
            }

            logger.Logf("ATA:: Installed typing files %v", installed);
            return (cached.Append(installed), null);
        }

        logger.Logf("ATA:: install request failed, marking packages as missing to prevent repeated requests: %v", filtered);
        foreach (var name in filtered)
            missingTypingsSet.Store(ByteStringKey.FromValue(name), true);
        return (default, new Exception("npm install failed"));
    }

    // ata.go:268.
    private (Slice<ByteStringValue> Packages, bool Success) InstallWorker(CancellationToken context, int requestID, Slice<ByteStringValue> packageNames, ILogger logger)
    {
        logger.Logf("ATA:: #%d with cwd: %s arguments: %v", requestID, typingsLocation, packageNames);
        var error = AutomaticTypeAcquisition.InstallNpmPackages(context, packageNames, concurrencySemaphore, packages =>
        {
            Slice<ByteStringValue> args = default;
            args = args.Append((ReadOnlySpan<ByteStringValue>)[ByteStringValue.FromString("install"), ByteStringValue.FromString("--ignore-scripts")]);
            args = args.Append(packages);
            args = args.Append((ReadOnlySpan<ByteStringValue>)[ByteStringValue.FromString("--save-dev"), ByteStringValue.FromString("--user-agent=\"typesInstaller/" + Core.Version() + "\"")]);
            var(output, failure) = npmExecutor.NpmInstall(context, typingsLocation, args);
            if (failure != null)
                logger.Logf("ATA:: Output is: %s", GoString.FromBytes(output.AsSpan()));
            return failure;
        });
        logger.Logf("TI:: npm install #%d completed", requestID);
        return (packageNames, error == null);
    }

    // ata.go:329.
    private Slice<ByteStringValue> FilterTypings(ILogger logger, Slice<ByteStringValue> names)
    {
        Slice<ByteStringValue> result = default;
        foreach (var name in names)
        {
            var mangled = ModuleUtilities.MangleScopedPackageName(name);
            var key = ByteStringKey.FromValue(mangled);
            if (missingTypingsSet.Load(key).Ok)
            {
                logger.Logf("ATA:: '%s':: '%s' is in missingTypingsSet - skipping...", name, mangled);
                continue;
            }

            var(validation, invalidName, scope) = AutomaticTypeAcquisition.ValidatePackageName(name);
            if (validation != NameValidationResult.NameOk)
            {
                missingTypingsSet.Store(key, true);
                logger.Log("ATA:: " + AutomaticTypeAcquisition.RenderPackageNameValidationFailure(name, validation, invalidName, scope));
                continue;
            }

            if (typesRegistry == null || !typesRegistry.TryGetValue(key, out var registry))
            {
                logger.Logf("ATA:: '%s':: Entry for package '%s' does not exist in local types registry - skipping...", name, mangled);
                continue;
            }

            var(cached, found) = packageNameToTypingLocation.Load(key);
            if (found && AutomaticTypeAcquisition.IsTypingUpToDate(cached, registry))
            {
                logger.Logf("ATA:: '%s':: '%s' already has an up-to-date typing - skipping...", name, mangled);
                continue;
            }

            result = result.Append(mangled);
        }

        return result;
    }

    // ata.go:361. sync.Once remains completed even if its callback panics.
    private void Init(CancellationToken context, IFS fs, ILogger logger)
    {
        if (Volatile.Read(ref initialized) != 0)
            return;
        initializationGate.Wait();
        try
        {
            if (initialized != 0)
                return;
            try
            {
                logger.Log("ATA:: Global cache location '" + typingsLocation.AsString() + "'");
                ProcessCacheLocation(fs, logger);
                EnsureTypingsLocationExists(fs, logger);
                logger.Log("ATA:: Updating types-registry@latest npm package...");
                var(_, error) = npmExecutor.NpmInstall(context, typingsLocation, Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("install"), ByteStringValue.FromString("--ignore-scripts"), ByteStringValue.FromString("types-registry@latest")]));
                if (error == null)
                    logger.Log("ATA:: Updated types-registry npm package");
                else
                    logger.Logf("ATA:: Error updating types-registry package: %v", error);
                typesRegistry = LoadTypesRegistryFile(fs, logger);
            }
            finally
            {
                Volatile.Write(ref initialized, 1);
            }
        }
        finally
        {
            initializationGate.Release();
        }
    }

    // ata.go:414.
    private void ProcessCacheLocation(IFS fs, ILogger logger)
    {
        logger.Log("ATA:: Processing cache location " + typingsLocation.AsString());
        var package = typingsLocation.ResolveFile("package.json");
        var packageLock = typingsLocation.ResolveFile("package-lock.json");
        logger.Log("ATA:: Trying to find '" + package.AsString() + "'...");
        if (fs.FileExists(package) && fs.FileExists(packageLock))
        {
            var(config, configContent) = ParseNpmConfig(fs, logger, package);
            var(lockfile, lockContent) = ParseNpmLock(fs, logger, packageLock);
            logger.Log("ATA:: Loaded content of " + package.AsString() + ": " + configContent);
            logger.Log("ATA:: Loaded content of " + packageLock.AsString() + ": " + lockContent);
            var resolver = NewResolver();
            var dependencies = config.DevDependencies;
            var lockedPackages = lockfile.Packages;
            var lockedDependencies = lockfile.Dependencies;
            if (dependencies != null && (lockedPackages != null || lockedDependencies != null))
                foreach (var key in dependencies.Keys)
                {
                    NpmDependencyEntry entry = default;
                    var found = lockedPackages?.TryGetValue("node_modules/" + key, out entry) == true;
                    if (!found)
                        found = lockedDependencies?.TryGetValue(key, out entry) == true;
                    if (!found)
                        continue;
                    var name = TsPath.GetBaseFileName(ByteStringValue.FromString(key));
                    if (name.Length == 0)
                        continue;
                    var nameKey = ByteStringKey.FromValue(name);
                    var file = TypingToFileName(resolver, name);
                    if (file.AsValue().Length == 0)
                    {
                        missingTypingsSet.Store(nameKey, true);
                        continue;
                    }

                    var(existing, present) = packageNameToTypingLocation.Load(nameKey);
                    if (present)
                    {
                        if (existing.TypingsLocation == file)
                            continue;
                        logger.Log("ATA:: New typing for package " + name.Materialize() + " from " + file.AsString() + " conflicts with existing typing file " + existing.TypingsLocation.AsString());
                    }

                    logger.Log("ATA:: Adding entry into typings cache: " + name.Materialize() + " => " + file.AsString());
                    var text = entry.Version ?? "";
                    if (text.Length == 0)
                        continue;
                    packageNameToTypingLocation.Store(nameKey, new() { TypingsLocation = file, Version = TypeScript.Semver.Semver.MustParse(text) });
                }
        }

        logger.Log("ATA:: Finished processing cache location " + typingsLocation.AsString());
    }

    // ata.go:401–412. Closed typed owners retain the original struct/map shape.
    private struct NpmConfig
    {
        internal Dictionary<string, object?>? DevDependencies;
    }

    private struct NpmDependencyEntry
    {
        internal string? Version;
    }

    private struct NpmLock
    {
        internal Dictionary<string, NpmDependencyEntry>? Dependencies;
        internal Dictionary<string, NpmDependencyEntry>? Packages;
    }

    // ata.go:473. The generic union is represented by two closed decoders;
    // parse failures are ignored but fields decoded before failure remain retained.
    private static (NpmConfig Value, string Contents) ParseNpmConfig(IFS fs, ILogger logger, RootedFilePath path)
    {
        var(content, _) = fs.ReadFile(path);
        NpmConfig result = default;
        var(fields, _, _) = ReadTypedObject(new JsonValue(GoString.Bytes(content)), "ata.npmConfig");
        if (fields != null)
            foreach (var(name, value)in fields)
                if (name == "devDependencies")
                {
                    var(map, error) = JsonCodec.ParseDictionary(GoString.FromBytes(value.Bytes));
                    result.DevDependencies = map as Dictionary<string, object?>;
                    if (error != null)
                        break;
                }

        return (result, content);
    }

    private static (NpmLock Value, string Contents) ParseNpmLock(IFS fs, ILogger logger, RootedFilePath path)
    {
        var(content, _) = fs.ReadFile(path);
        NpmLock result = default;
        var(fields, _, _) = ReadTypedObject(new JsonValue(GoString.Bytes(content)), "ata.npmLock");
        if (fields != null)
            foreach (var(name, value)in fields)
            {
                if (name is not ("dependencies" or "packages"))
                    continue;
                var(map, failure) = ParseDependencyEntries(value);
                if (name == "dependencies")
                    result.Dependencies = map;
                else
                    result.Packages = map;
                if (failure != null)
                    break;
            }

        return (result, content);
    }

    private static (Dictionary<string, NpmDependencyEntry>? Value, Exception? Error) ParseDependencyEntries(JsonValue token)
    {
        var(fields, isNull, error) = ReadTypedObject(token, "map[string]ata.npmDependecyEntry");
        if (isNull || fields == null)
            return (null, error);
        var result = new Dictionary<string, NpmDependencyEntry>(StringComparer.Ordinal);
        foreach (var(name, value)in fields)
        {
            NpmDependencyEntry entry = default;
            var(members, _, failure) = ReadTypedObject(value, "ata.npmDependecyEntry");
            if (members != null)
                foreach (var(member, field)in members)
                    if (member == "version")
                    {
                        var(parsed, parseError) = JsonCodec.Parse(field);
                        if (parseError != null || parsed != null && parsed is not string)
                        {
                            failure = parseError ?? TypedJsonError(field, "string");
                            break;
                        }

                        entry.Version = (string? )parsed ?? "";
                    }

            result[name] = entry;
            if (failure != null)
                return (result, failure);
        }

        return (result, error);
    }

    // ata.go:479.
    private void EnsureTypingsLocationExists(IFS fs, ILogger logger)
    {
        var path = typingsLocation.ResolveFile("package.json");
        logger.Log("ATA:: Npm config file: " + path.AsString());
        if (!fs.FileExists(path))
        {
            logger.Logf("ATA:: Npm config file: '%s' is missing, creating new one...", path);
            var error = fs.WriteFile(path, "{ \"private\": true }");
            if (error != null)
                logger.Logf("ATA:: Npm config file write failed: %v", error);
        }
    }

    // ata.go:492. Resolver failure retains the original nil-result failure behavior.
    private RootedFilePath TypingToFileName(IResolver resolver, ByteStringValue name)
    {
        var file = typingsLocation.ResolveFile("index.d.ts");
        var(result, _, _) = ResolverText.ResolveModuleName(resolver, name, file, ResolutionMode.None);
        return result!.ResolvedFileName;
    }

    // ata.go:498. Closed typed JSON decoding retains nil maps and JSON/v2 errors.
    private Dictionary<ByteStringKey, Dictionary<ByteStringKey, ByteStringValue>?>? LoadTypesRegistryFile(IFS fs, ILogger logger)
    {
        var path = typingsLocation.ResolveFile("node_modules/types-registry/index.json");
        var(content, found) = fs.ReadFile(path);
        if (found)
        {
            var(outer, error) = ReadRegistryOuter(new JsonValue(GoString.Bytes(content)));
            if (error == null && outer != null && outer.TryGetValue(ByteStringKey.FromString("entries"), out var entries))
                return entries;
            logger.Logf("ATA:: Error when loading types registry file '%s': %v", path, error);
        }
        else
            logger.Logf("ATA:: Error reading types registry file '%s'", path);
        return new();
    }

    private static (Dictionary<ByteStringKey, Dictionary<ByteStringKey, Dictionary<ByteStringKey, ByteStringValue>?>?>? Value, Exception? Error) ReadRegistryOuter(JsonValue token)
    {
        var(fields, isNull, error) = ReadTypedObject(token, "map[string]map[string]map[string]string");
        if (isNull || fields == null)
            return (null, error);
        var result = new Dictionary<ByteStringKey, Dictionary<ByteStringKey, Dictionary<ByteStringKey, ByteStringValue>?>?>();
        foreach (var(name, value)in fields)
        {
            var(entry, failure) = ReadRegistryPackages(value);
            result[ByteStringKey.FromString(name)] = entry;
            if (failure != null)
                return (result, failure);
        }

        return (result, error);
    }

    private static (Dictionary<ByteStringKey, Dictionary<ByteStringKey, ByteStringValue>?>? Value, Exception? Error) ReadRegistryPackages(JsonValue token)
    {
        var(fields, isNull, error) = ReadTypedObject(token, "map[string]map[string]string");
        if (isNull || fields == null)
            return (null, error);
        var result = new Dictionary<ByteStringKey, Dictionary<ByteStringKey, ByteStringValue>?>();
        foreach (var(name, value)in fields)
        {
            var(entry, failure) = ReadRegistryTags(value);
            result[ByteStringKey.FromString(name)] = entry;
            if (failure != null)
                return (result, failure);
        }

        return (result, error);
    }

    private static (Dictionary<ByteStringKey, ByteStringValue>? Value, Exception? Error) ReadRegistryTags(JsonValue token)
    {
        var(fields, isNull, error) = ReadTypedObject(token, "map[string]string");
        if (isNull || fields == null)
            return (null, error);
        var result = new Dictionary<ByteStringKey, ByteStringValue>();
        foreach (var(name, value)in fields)
        {
            var raw = GoString.FromBytes(value.Bytes).AsSpan().TrimStart();
            if (raw.Length != 0 && raw[0] != '"' && raw[0] != 'n')
                return (result, TypedJsonError(value, "string"));
            var(parsed, failure) = JsonCodec.Parse(value);
            if (failure != null)
                return (result, failure);
            result[ByteStringKey.FromString(name)] = ByteStringValue.FromString((string? )parsed ?? "");
        }

        return (result, error);
    }

    private static (List<(string Name, JsonValue Value)>? Fields, bool IsNull, Exception? Error) ReadTypedObject(JsonValue token, string type)
    {
        var raw = GoString.FromBytes(token.Bytes);
        var lexical = JsonDiagnosticReader.AnalyzeRawValue(raw, sourcePointer: token.Pointer, sourceOffset: token.SourceOffset);
        var trimmed = raw.AsSpan().TrimStart();
        if (trimmed.Length == 0)
            return (null, false, lexical);
        var first = trimmed[0];
        if (first != '{' && first != 'n')
            return (null, false, lexical ?? TypedJsonError(token, type));
        var(fields, isNull, error) = JsonCodec.ParseObjectFields(token.Bytes!, token.SourceOffset, token.Pointer);
        if (fields != null)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < fields.Count; i++)
                if (!seen.Add(fields[i].Name))
                {
                    // JSON/v2 stops at the duplicate name; earlier destination fields remain.
                    fields.RemoveRange(i, fields.Count - i);
                    break;
                }
        }

        return (fields, isNull, lexical ?? error);
    }

    private static Exception TypedJsonError(JsonValue token, string type)
    {
        var raw = GoString.FromBytes(token.Bytes).AsSpan().TrimStart();
        var kind = raw[0] == '"' ? "string" : raw[0] == '[' ? "array" : raw[0] == '{' ? "object" : raw[0] is 't' or 'f' ? "boolean" : raw[0] == 'n' ? "null" : "number";
        var prefix = GoString.FromBytes(token.Bytes).Length - raw.Length;
        return JsonTextErrors.AsException(JsonTextErrors.SemanticError("unmarshal", kind, "", type, token.Pointer, token.SourceOffset + prefix, null));
    }

    private DefaultResolver NewResolver() => DefaultResolver.NewResolver(new() { Host = new ResolutionHost(fs, typingsLocation), CompilerOptions = new() { ModuleResolution = ModuleResolutionKind.NodeNext } });
    // ata.go:68,72.
    private sealed class ResolutionHost(IFS fs, RootedDirectoryPath currentDirectory) : IResolutionHost
    {
        public IFS FS() => fs;
        public RootedDirectoryPath GetCurrentDirectory() => currentDirectory;
    }
}
