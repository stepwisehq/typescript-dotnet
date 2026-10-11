// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Three complete original assertion domains from module/resolver_test.go.
using System.Reflection;
using System.Runtime.CompilerServices;
using Configuration.Checks;
using TypeScript.Collections;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;

namespace Resolution.Checks;
public static class ResolutionDataChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestResolverSharedData", "TestResolutionDataCaches", "TestResolverRejectsMismatchedPackageJsonCacheCaseSensitivity"];

    public static void Run(string name)
    {
        if (name == "TestResolverSharedData")
        {
            DataOnly();
            return;
        }

        if (name == "TestResolutionDataCaches")
        {
            Caches();
            return;
        }

        if (name == "TestResolverRejectsMismatchedPackageJsonCacheCaseSensitivity")
        {
            MismatchedCase();
            return;
        }

        throw new ArgumentException("Unknown original resolution data body.", nameof(name));
    }

    private static void DataOnly()
    {
        // Every GetFields call has a constant known type: native metadata roots are
        // finite. This inspects the actual managed fields, not a copied Go layout.
        // A newly introduced unknown host/interface/delegate/object field fails.
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Dictionary<Type, FieldInfo[]> represented = new()
        {
            [typeof(ResolutionData)] = typeof(ResolutionData).GetFields(Fields),
            [typeof(CompilerOptions)] = typeof(CompilerOptions).GetFields(Fields),
            [typeof(InfoCache)] = typeof(InfoCache).GetFields(Fields),
            [typeof(InfoCacheEntry)] = typeof(InfoCacheEntry).GetFields(Fields),
            [typeof(PackageDirectory)] = typeof(PackageDirectory).GetFields(Fields),
            [typeof(RootedPath)] = typeof(RootedPath).GetFields(Fields),
            [typeof(RootedFilePath)] = typeof(RootedFilePath).GetFields(Fields),
            [typeof(RootedDirectoryPath)] = typeof(RootedDirectoryPath).GetFields(Fields),
            [typeof(PathKey)] = typeof(PathKey).GetFields(Fields),
            [typeof(SourceMapLocation)] = typeof(SourceMapLocation).GetFields(Fields),
            [typeof(PluginImport)] = typeof(PluginImport).GetFields(Fields),
            [typeof(OrderedMap<string, List<string>?>)] = typeof(OrderedMap<string, List<string>?>).GetFields(Fields)
        };
        // Immutable byte descriptors are the known Go string representation:
        // their object-backed immutable string/byte owner is not a resolver host.
        HashSet<Type> seen = [typeof(Diagnostic), typeof(PackageJson), typeof(ByteStringValue), typeof(ByteStringKey)]; // Original leaves plus represented strings.
        Queue<(Type Type, string Path)> pending = new();
        pending.Enqueue((typeof(ResolutionData), nameof(ResolutionData)));
        while (pending.TryDequeue(out var item))
        {
            Type type = item.Type;
            if (!seen.Add(type) || type.IsPrimitive || type.IsEnum || type == typeof(string))
                continue;
            Check.True(!type.IsInterface && !type.IsPointer && !typeof(Delegate).IsAssignableFrom(type), item.Path + " must not retain an opaque reference");
            if (represented.TryGetValue(type, out FieldInfo[]? fields))
            {
                foreach (FieldInfo field in fields)
                {
                    // Monitor gate is the precise sync.Mutex representation leaf;
                    // no other object field is accepted by this exception.
                    if (type == typeof(InfoCache) && field.Name == "gate" && field.FieldType == typeof(object))
                        continue;
                    pending.Enqueue((field.FieldType, item.Path + "." + field.Name));
                }

                continue;
            }

            if (type.IsArray)
            {
                pending.Enqueue((type.GetElementType()!, item.Path + "[]"));
                continue;
            }

            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                // BCL collection internals replace source slice/map/SyncMap. Their
                // K/V domains are inspected; their implementation locks/comparers
                // are not a claim about Go runtime or .NET collection topology.
                bool container = definition == typeof(List<>) || definition == typeof(Dictionary<, >) || definition == typeof(StrongBox<>) || definition == typeof(Nullable<>) || definition == typeof(Slice<>);
                Check.True(container, item.Path + " has an unknown represented generic type " + type);
                foreach (Type argument in type.GetGenericArguments())
                    pending.Enqueue((argument, item.Path + "[]"));
                continue;
            }

            Check.True(false, item.Path + " has an unknown shared-data type " + type);
        }
    }

    private static void Caches()
    {
        var oldHost = ResolutionFixtures.Host(ResolutionFixtures.Fs(("/src/node_modules/pkg/package.json", "{\"name\":\"pkg\",\"types\":\"index.d.ts\"}"), ("/src/node_modules/pkg/index.d.ts", "export const value: number;")), "/");
        var newHost = ResolutionFixtures.Host(ResolutionFixtures.Fs(("/src/node_modules/pkg/package.json", "{\"name\":\"pkg\",\"types\":\"index.d.ts\"}"), ("/src/node_modules/pkg/index.d.ts", "export const value: number;"), ("/new/package.json", "{\"name\":\"new\"}"), ("/missing-first/package.json", "{\"name\":\"missing-first\"}")), "/");
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = oldHost, CompilerOptions = new() { Module = ModuleKind.NodeNext } });
        var(resolved, _, error) = resolver.ResolveModuleName("pkg", RootedFilePath.FromUnicode("/src/index.ts"), ResolutionMode.CommonJS, null);
        Check.True(error is null, "Original initial resolution nil error");
        Check.True(resolved?.IsResolved() == true, "Original initial resolution");
        var(cached, _, cachedError) = resolver.ResolveModuleName("pkg", RootedFilePath.FromUnicode("/src/index.ts"), ResolutionMode.CommonJS, null);
        Check.True(cachedError is null, "Original cached resolution nil error");
        Check.True(ReferenceEquals(cached, resolved), "Original cached result pointer identity");
        resolver.ResolveTypeReferenceDirective("missing", RootedFilePath.FromUnicode("/src/index.ts"), ResolutionMode.CommonJS, null);
        var paths = new OrderedMap<string, List<string>?>(1);
        paths.Set("alias/*", ["./*"]);
        resolver.getParsedPatternsForPaths(new CompilerOptions { Paths = paths });
        Check.Equal(resolver.moduleResolutionCache.Size, 1, "Original module cache size");
        Check.Equal(resolver.typeRefDirectiveResolutionCache.Size, 1, "Original directive cache size");
        Check.Equal(resolver.parsedPatternsForPaths.Size, 1, "Original pattern cache size");
        ResolutionData data = resolver.GetResolutionData();
        var rebound = data.NewResolver(oldHost);
        Check.Equal(rebound.moduleResolutionCache.Size, 0, "Original rebound module cache empty");
        Check.Equal(rebound.typeRefDirectiveResolutionCache.Size, 0, "Original rebound directive cache empty");
        Check.Equal(rebound.parsedPatternsForPaths.Size, 0, "Original rebound pattern cache empty");
        var clone = data.Clone().NewResolver(newHost);
        Check.Equal(clone.moduleResolutionCache.Size, 0, "Original clone module cache empty");
        Check.Equal(clone.typeRefDirectiveResolutionCache.Size, 0, "Original clone directive cache empty");
        Check.Equal(clone.parsedPatternsForPaths.Size, 0, "Original clone pattern cache empty");
        Check.True(ReferenceEquals(clone.GetPackageScopeForPath(RootedDirectoryPath.FromUnicode("/src/node_modules/pkg")), resolver.GetPackageScopeForPath(RootedDirectoryPath.FromUnicode("/src/node_modules/pkg"))), "Original cloned package entry pointer identity");
        Check.True(clone.GetPackageScopeForPath(RootedDirectoryPath.FromUnicode("/new"))!.Exists(), "Original clone new-host package exists");
        Check.True(!resolver.GetPackageScopeForPath(RootedDirectoryPath.FromUnicode("/new"))!.Exists(), "Original source host package missing");
        Check.True(!resolver.GetPackageScopeForPath(RootedDirectoryPath.FromUnicode("/missing-first"))!.Exists(), "Original source negative package entry");
        Check.True(clone.GetPackageScopeForPath(RootedDirectoryPath.FromUnicode("/missing-first"))!.Exists(), "Original clone independently loads source-negative package");
    }

    private static void MismatchedCase()
    {
        var host = ResolutionFixtures.Host(ResolutionFixtures.Fs());
        bool panicked = false;
        try
        {
            DefaultResolver.NewResolverWithOptions(host, new CompilerOptions(), default, "", new ResolverOptions { PackageJsonCache = new InfoCache(CaseSensitivity.CaseInsensitive) });
        }
        catch (Exception)
        {
            panicked = true;
        }

        Check.True(panicked, "Original mismatched package cache case sensitivity must panic");
    }
}
