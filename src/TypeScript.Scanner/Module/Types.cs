// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/module/types.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Vfs;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Resolution.Checks")]
[assembly: InternalsVisibleTo("Resolution.Validation")]
namespace TypeScript.Modules;
public interface IResolutionHost
{
    IFS FS();
    RootedDirectoryPath GetCurrentDirectory();
}

public interface IResolver
{
    RootedDirectoryPath BaseDirectory();
    (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleName(string moduleName, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? redirectedReference = null);
    (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleNameFromDirectory(string moduleName, RootedDirectoryPath containingDirectory, ResolutionMode resolutionMode);
    (ResolvedTypeReferenceDirective? Result, Slice<DiagAndArgs> Traces) ResolveTypeReferenceDirective(string typeReferenceDirectiveName, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? redirectedReference = null);
    ResolutionData GetResolutionData();
}

// Canonical built-in resolution consumes Go-byte names. Public extension resolvers
// retain their string API, projected only when that extension is actually invoked.
internal interface ITextResolver
{
    (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleName(ByteStringValue name, RootedFilePath file, ResolutionMode mode, IResolvedProjectReference? reference = null);
    (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleNameFromDirectory(ByteStringValue name, RootedDirectoryPath directory, ResolutionMode mode);
    (ResolvedTypeReferenceDirective? Result, Slice<DiagAndArgs> Traces) ResolveTypeReferenceDirective(ByteStringValue name, RootedFilePath file, ResolutionMode mode, IResolvedProjectReference? reference = null);
}
internal static class ResolverText
{
    internal static (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleName(this IResolver resolver, ByteStringValue name, RootedFilePath file, ResolutionMode mode, IResolvedProjectReference? reference = null) => resolver is ITextResolver text
        ? text.ResolveModuleName(name, file, mode, reference) : resolver.ResolveModuleName(name.Materialize()!, file, mode, reference);
    internal static (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleNameFromDirectory(this IResolver resolver, ByteStringValue name, RootedDirectoryPath directory, ResolutionMode mode) => resolver is ITextResolver text
        ? text.ResolveModuleNameFromDirectory(name, directory, mode) : resolver.ResolveModuleNameFromDirectory(name.Materialize()!, directory, mode);
    internal static (ResolvedTypeReferenceDirective? Result, Slice<DiagAndArgs> Traces) ResolveTypeReferenceDirective(this IResolver resolver, ByteStringValue name, RootedFilePath file, ResolutionMode mode, IResolvedProjectReference? reference = null) => resolver is ITextResolver text
        ? text.ResolveTypeReferenceDirective(name, file, mode, reference) : resolver.ResolveTypeReferenceDirective(name.Materialize()!, file, mode, reference);
}

public interface IResolvedProjectReference
{
    RootedFilePath ConfigName();
    CompilerOptions? CompilerOptions();
}

[Flags]
public enum NodeResolutionFeatures : int
{
    None = 0,
    Imports = 1 << 0,
    SelfName = 1 << 1,
    Exports = 1 << 2,
    ExportsPatternTrailers = 1 << 3,
    ImportsPatternRoot = 1 << 4,
    All = Imports | SelfName | Exports | ExportsPatternTrailers | ImportsPatternRoot,
    Node16Default = Imports | SelfName | Exports | ExportsPatternTrailers,
    NodeNextDefault = All,
    BundlerDefault = All,
}

// Copies retain Go value semantics. Default backing strings are normalized to Go's empty zero.
public struct PackageId : IEquatable<PackageId>
{
    private string? name, version, peerDependencies;
    private ByteStringValue subModuleName;
    public string Name { readonly get => name ?? ""; set => name = value; }
    public string SubModuleName { readonly get => SubModuleNameValue.Materialize()!; set => subModuleName = ByteStringValue.FromString(value); }
    internal ByteStringValue SubModuleNameValue { readonly get => subModuleName.IsNull ? default : subModuleName.Snapshot(); set => subModuleName = value.Snapshot(); }
    public string Version { readonly get => version ?? ""; set => version = value; }
    public string PeerDependencies { readonly get => peerDependencies ?? ""; set => peerDependencies = value; }

    public readonly string PackageName() => SubModuleName != "" ? Name + "/" + SubModuleName : Name;
    public override readonly string ToString() => PackageName() + "@" + Version + PeerDependencies;
    public readonly bool Equals(PackageId other) => Name == other.Name && SubModuleNameValue.EqualsValue(other.SubModuleNameValue) && Version == other.Version && PeerDependencies == other.PeerDependencies;
    public override readonly bool Equals(object? obj) => obj is PackageId other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Name, ByteStringKey.FromValue(SubModuleNameValue), Version, PeerDependencies);
    public static bool operator ==(PackageId left, PackageId right) => left.Equals(right);
    public static bool operator !=(PackageId left, PackageId right) => !left.Equals(right);
}

public sealed class ResolvedModule
{
    public Slice<Diagnostic> ResolutionDiagnostics;
    public RootedFilePath ResolvedFileName;
    public PathKey ResolvedPath;
    public RootedFilePath OriginalPath;
    private ByteStringValue extension;
    public string Extension { get => extension.Materialize()!; set => extension = ByteStringValue.FromString(value); }
    internal ByteStringValue ExtensionValue { get => extension.Snapshot(); set => extension = value.Snapshot(); }
    public bool ResolvedUsingTsExtension, ResolvedUsingExtraExtensions, IsExternalLibraryImport, IsCustomResolution;
    public PackageId PackageId;
    public RootedFilePath AlternateResult;
}

// The source ResolvedModule pointer receiver explicitly accepts nil. Extension
// dispatch retains that operation instead of a .NET null instance call.
public static class ResolvedModuleOperations
{
    public static bool IsResolved(this ResolvedModule? result) => result is not null && result.ResolvedFileName != default;
}

public sealed class ResolvedTypeReferenceDirective
{
    public Slice<Diagnostic> ResolutionDiagnostics;
    public bool Primary;
    public RootedFilePath ResolvedFileName;
    public PathKey ResolvedPath;
    public RootedFilePath OriginalPath;
    public PackageId PackageId;
    public bool IsExternalLibraryImport;
    public bool IsResolved() => ResolvedFileName != default;
}

public readonly record struct ModeAwareCacheKey
{
    private readonly ByteStringKey name;
    private readonly bool nameSupplied;
    // String projection is the original public presentation boundary. Cache
    // equality/hashing and internal reads retain one immutable Go-byte header.
    public string Name
    {
        get => nameSupplied ? name.ToString() : null!;
        init { nameSupplied = value is not null; name = nameSupplied ? ByteStringKey.FromString(value!) : default; }
    }
    internal ByteStringValue NameValue => nameSupplied ? name.Value : ByteStringValue.FromString(null);
    public ResolutionMode Mode { get; init; }
    public ModeAwareCacheKey(string Name, ResolutionMode Mode) : this(ByteStringValue.FromString(Name), Mode) { }
    internal ModeAwareCacheKey(ByteStringValue Name, ResolutionMode Mode)
    {
        nameSupplied = !Name.IsNull;
        name = nameSupplied ? ByteStringKey.FromValue(Name) : default;
        this.Mode = Mode;
    }
    public void Deconstruct(out string Name, out ResolutionMode Mode) { Name = this.Name; Mode = this.Mode; }
}
public sealed class ModeAwareCache<T> : Dictionary<ModeAwareCacheKey, T>
{
}

[Flags]
internal enum Extensions : int
{
    TypeScript = 1 << 0,
    JavaScript = 1 << 1,
    Declaration = 1 << 2,
    Json = 1 << 3,
    ImplementationFiles = TypeScript | JavaScript,
}

internal static class ExtensionOperations
{
    internal static string String(this Extensions extensions)
    {
        var values = new List<string>();
        if ((extensions & Extensions.TypeScript) != 0)
            values.Add("TypeScript");
        if ((extensions & Extensions.JavaScript) != 0)
            values.Add("JavaScript");
        if ((extensions & Extensions.Declaration) != 0)
            values.Add("Declaration");
        if ((extensions & Extensions.Json) != 0)
            values.Add("JSON");
        return string.Join(", ", values);
    }

    internal static List<string> Array(this Extensions extensions)
    {
        var values = new List<string>();
        if ((extensions & Extensions.TypeScript) != 0)
            values.AddRange(TsPath.SupportedTSImplementationExtensions);
        if ((extensions & Extensions.JavaScript) != 0)
            values.AddRange(TsPath.SupportedJSExtensionsFlat);
        if ((extensions & Extensions.Declaration) != 0)
            values.AddRange(TsPath.SupportedDeclarationExtensions);
        if ((extensions & Extensions.Json) != 0)
            values.Add(TsPath.ExtensionJson);
        return values;
    }
}
