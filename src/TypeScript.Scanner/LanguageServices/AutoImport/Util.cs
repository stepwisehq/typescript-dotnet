// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/util.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices.AutoImport;
internal static partial class AutoImport
{
    // internal/ls/autoimport/util.go:24
    internal static (ModuleID ID, RootedFilePath FileName, bool Ok) TryGetModuleIDAndFileNameOfModuleSymbol(Symbol symbol)
    {
        if (!symbol.IsExternalModule())
            return (default, default, false);
        var declaration = Ast.GetNonAugmentationDeclaration(symbol);
        if (declaration == null)
            return (default, default, false);
        if (declaration.Kind == Kind.SourceFile)
            return (ModuleID.File(PathKey.FromValue(declaration.AsSourceFile().PathKeyValue())), RootedFilePath.FromValue(declaration.AsSourceFile().FileNameGoValue()), true);
        if (Ast.IsModuleWithStringLiteralName(declaration))
            return (ModuleID.Ambient(declaration.Name().TextValue()), default, true);
        return (default, default, false);
    }

    // internal/ls/autoimport/util.go:42. Preserve all original panic cases independently of the nonthrowing worker.
    internal static (ModuleID ID, RootedFilePath FileName) GetModuleIDAndFileNameOfModuleSymbol(Symbol symbol)
    {
        if (!symbol.IsExternalModule())
            throw new InvalidOperationException("symbol is not an external module");
        var declaration = Ast.GetNonAugmentationDeclaration(symbol);
        if (declaration == null)
            throw new InvalidOperationException("module symbol has no non-augmentation declaration");
        if (declaration.Kind == Kind.SourceFile)
            return (ModuleID.File(PathKey.FromValue(declaration.AsSourceFile().PathKeyValue())), RootedFilePath.FromValue(declaration.AsSourceFile().FileNameGoValue()));
        if (Ast.IsModuleWithStringLiteralName(declaration))
            return (ModuleID.Ambient(declaration.Name().TextValue()), default);
        throw new InvalidOperationException("could not determine module ID of module symbol");
    }

    // internal/ls/autoimport/util.go:67. All offsets are original UTF8 byte indices.
    internal static Slice<int> WordIndices(ByteStringValue value)
    {
        Slice<int> indices = default;
        for (var byteIndex = 0; byteIndex < value.Length;)
        {
            var(rune, width) = Decode(value.Slice(byteIndex));
            var start = byteIndex;
            byteIndex += width;
            if (start == 0)
            {
                indices = indices.Append(start);
                continue;
            }

            if (rune == '_')
            {
                if (start + 1 < value.Length && value[start + 1] != '_')
                    indices = indices.Append(start + 1);
                continue;
            }

            // Preserve the original start+1 lookahead rather than advancing by rune width.
            if (GoUnicode.IsUpper(rune) && (GoUnicode.IsLower(DecodeLast(value.Slice(0, start)).Rune) || (start + 1 < value.Length && GoUnicode.IsLower(Decode(value.Slice(start + 1)).Rune))))
                indices = indices.Append(start);
        }

        return indices;
    }

    private static (int Rune, int Width) Decode(ByteStringValue value) => value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan) : Utf8.DecodeRuneInString(value.CharSpan);
    private static (int Rune, int Width) DecodeLast(ByteStringValue value) => value.IsBytes ? Utf8.DecodeLastRuneInString(value.ByteSpan) : Utf8.DecodeLastRuneInString(value.CharSpan);
    // internal/ls/autoimport/util.go:115. Return retained declaration/symbol name headers.
    internal static ByteStringValue GetDefaultLikeExportNameFromDeclaration(Symbol symbol)
    {
        foreach (var declaration in symbol.Declarations)
        {
            if (Ast.IsExportAssignment(declaration))
            {
                var innerExpression = Ast.SkipOuterExpressions(declaration.Expression(), OuterExpressionKinds.All);
                if (Ast.IsIdentifier(innerExpression))
                    return innerExpression.TextValue();
                continue;
            }

            if (Ast.IsExportSpecifier(declaration) && declaration.Symbol().Flags == SymbolFlags.Alias && declaration.PropertyName() != null)
            {
                if (declaration.PropertyName().Kind == Kind.Identifier)
                    return declaration.PropertyName().TextValue();
                continue;
            }

            var name = Ast.GetNameOfDeclaration(declaration);
            if (name != null && name.Kind == Kind.Identifier)
                return name.TextValue();
            if (symbol.Parent != null && !TypeScript.Checking.Checker.IsExternalModuleSymbol(symbol.Parent))
                return symbol.Parent.NameValue;
        }

        return default;
    }

    // internal/ls/autoimport/util.go:143. Required set copy; checker lease covers only unresolved ambient fallback.
    internal static HashSet<ByteStringKey> GetResolvedPackageNames(CancellationToken context, TypeScript.Compiler.Program program)
    {
        var rawNames = program.ResolvedPackageNames();
        var unresolvedNames = program.UnresolvedPackageNames();
        var resolvedNames = new HashSet<ByteStringKey>(rawNames.Count);
        foreach (var name in rawNames)
            resolvedNames.Add(ByteStringKey.FromValue(TypeScript.Modules.ModuleUtilities.GetPackageNameFromTypesPackageNameValue(name.Value)));
        if (program.Options().Types is { } configuredTypes)
            foreach (var name in configuredTypes)
            {
                var value = ByteStringValue.FromString(name);
                if (!value.EqualsString("*"))
                    resolvedNames.Add(ByteStringKey.FromValue(TypeScript.Modules.ModuleUtilities.GetPackageNameFromTypesPackageNameValue(value)));
            }

        if (unresolvedNames.Count > 0)
        {
            var(checker, done) = program.GetTypeChecker(context);
            try
            {
                foreach (var name in unresolvedNames)
                {
                    var symbol = checker.TryFindAmbientModule(name.Value);
                    if (symbol == null)
                        continue;
                    var declaringFile = Ast.GetSourceFileOfModule(symbol);
                    var packageName = TypeScript.ModuleSpecifiers.ModuleSpecifiers.GetPackageNameValueFromDirectory(RootedFilePath.FromValue(declaringFile.FileNameGoValue()).AsPath());
                    if (packageName.Length != 0)
                        resolvedNames.Add(ByteStringKey.FromValue(TypeScript.Modules.ModuleUtilities.GetPackageNameFromTypesPackageNameValue(packageName)));
                }
            }
            finally
            {
                done();
            }
        }

        return resolvedNames;
    }

    // internal/ls/autoimport/util.go:86
    internal static HashSet<ByteStringKey> GetPackageNamesInNodeModules(RootedDirectoryPath nodeModulesDirectory, TypeScript.Vfs.IFS fileSystem)
    {
        var packageNames = new HashSet<ByteStringKey>();
        if (!nodeModulesDirectory.AsPath().BaseNameValue().EqualsString("node_modules"))
            throw new InvalidOperationException("nodeModulesDir is not a node_modules directory");
        var entries = fileSystem.GetAccessibleEntries(nodeModulesDirectory);
        foreach (var baseName in entries.Directories)
        {
            if (baseName[0] == '.')
                continue;
            if (baseName[0] == '@')
            {
                var scopedDirectoryPath = nodeModulesDirectory.ResolveDirectory(baseName);
                foreach (var scopedPackageDirectoryName in fileSystem.GetAccessibleEntries(scopedDirectoryPath).Directories)
                {
                    var scopedBaseName = TsPath.GetBaseFileName(scopedPackageDirectoryName);
                    var packageName = baseName.EqualsString("@types") ? TypeScript.Modules.ModuleUtilities.GetPackageNameFromTypesPackageNameValue(TsPath.CombinePaths(ByteStringValue.FromString("@types"), scopedBaseName)) : TsPath.CombinePaths(baseName, scopedBaseName);
                    packageNames.Add(ByteStringKey.FromValue(packageName));
                }

                continue;
            }

            packageNames.Add(ByteStringKey.FromValue(baseName));
        }

        return packageNames;
    }

    // internal/ls/autoimport/util.go:233. One original callback captures the required destination set.
    internal static void AddPackageJsonDependencies(TypeScript.PackageJson.PackageJson contents, HashSet<ByteStringKey> dependencies)
    {
        contents.Fields.RangeDependencies((name, _, field) =>
        {
            var value = ByteStringValue.FromString(name);
            if (value.Length == 0 || value.EqualsString("@types/") || value[0] == '.')
                return true;
            if (field == "dependencies" || field == "peerDependencies")
                dependencies.Add(ByteStringKey.FromValue(TypeScript.Modules.ModuleUtilities.GetPackageNameFromTypesPackageNameValue(value)));
            return true;
        });
    }

    // internal/ls/autoimport/util.go:179. First program wins for a repeated declaration-output path.
    internal static void AddProjectReferenceOutputMappings(TypeScript.Compiler.Program program, Dictionary<PathKey, RootedFilePath> result)
    {
        foreach (var reference in program.GetResolvedProjectReferences())
        {
            if (reference == null)
                continue;
            reference.ParseInputOutputNames();
            var outputs = reference.OutputDtsToProjectReference();
            if (outputs == null)
                continue;
            foreach (var(outputPath, mapping)in outputs)
                if (!result.ContainsKey(outputPath))
                    result[outputPath] = mapping.Source;
        }
    }

    // internal/ls/autoimport/util.go:197. All three functions retain the same bounded pool owner.
    internal static (Func<(TypeScript.Checking.Checker Checker, Action Done)> GetChecker, Action ClosePool, Func<int> GetCreatedCount) CreateCheckerPool(TypeScript.Compiler.ICheckerProgramHost program)
    {
        var pool = new AutoImportCheckerPool(program);
        return (pool.GetChecker, pool.ClosePool, pool.GetCreatedCount);
    }

    // internal/ls/autoimport/util.go:250
    internal static (Func<RootedFilePath, RootedFilePath> ToRealpath, Func<RootedFilePath, RootedFilePath> ToSymlink) GetPackageRealpathFuncs(TypeScript.Vfs.IFS fs, RootedDirectoryPath packageDirectory)
    {
        var realPackageDirectory = TsPath.RootedDirectoryPathFromPath(fs.Realpath(packageDirectory.AsPath()));
        bool isSymlinked = realPackageDirectory != packageDirectory;
        var directoryCache = new Dictionary<RootedDirectoryPath, RootedDirectoryPath>();
        RootedFilePath ToRealpath(RootedFilePath fileName)
        {
            if (isSymlinked)
            {
                var(relative, ok) = fileName.RelativeTo(packageDirectory);
                if (ok)
                    return realPackageDirectory.ResolveRelativeFile(relative);
            }

            var filePackageDirectory = TypeScript.Modules.ModuleUtilities.NodeModulePackageRootForFile(fileName);
            if (filePackageDirectory == default)
                return fileName;
            var directory = TsPath.RootedDirectoryPathFromPath(fileName.AsPath());
            var directoryPackage = TypeScript.Modules.ModuleUtilities.NodeModulePackageRootForDirectory(directory);
            if (directoryPackage != filePackageDirectory && fs.DirectoryExists(directory))
                filePackageDirectory = directoryPackage;
            if (directoryCache.TryGetValue(filePackageDirectory, out var cachedRealDirectory))
            {
                if (cachedRealDirectory == filePackageDirectory)
                    return fileName;
                return cachedRealDirectory.ResolveRelativeFile(fileName.RelativeTo(filePackageDirectory).Item1);
            }

            var realDirectory = TsPath.RootedDirectoryPathFromPath(fs.Realpath(filePackageDirectory.AsPath()));
            directoryCache[filePackageDirectory] = realDirectory;
            if (realDirectory == filePackageDirectory)
                return fileName;
            return realDirectory.ResolveRelativeFile(fileName.RelativeTo(filePackageDirectory).Item1);
        }

        if (!isSymlinked)
            return (ToRealpath, static fileName => fileName);
        RootedFilePath ToSymlink(RootedFilePath fileName)
        {
            var(relative, ok) = fileName.RelativeTo(realPackageDirectory);
            return ok ? packageDirectory.ResolveRelativeFile(relative) : fileName;
        }

        return (ToRealpath, ToSymlink);
    }

    // internal/ls/autoimport/util.go:330. ResolverOptions is an existing class;
    // explicitly copy its value fields to retain Go's by-value options parameter.
    internal static TypeScript.Modules.DefaultResolver GetModuleResolver(IRegistryCloneHost host, RootedDirectoryPath currentDirectory, Func<RootedFilePath, RootedFilePath> realpath, TypeScript.Modules.ResolverOptions options)
    {
        var resolutionHost = new AutoImportResolutionHost(TypeScript.Vfs.WrapVfs.Wrap(host.FS(), new TypeScript.Vfs.WrapVfs.Replacements { Realpath = path => realpath(TsPath.RootedFilePathFromPath(path)).AsPath() }), currentDirectory);
        var copiedOptions = new TypeScript.Modules.ResolverOptions
        {
            Host = resolutionHost,
            CompilerOptions = CompilerOptionHelpers.EmptyCompilerOptions,
            TypingsLocation = options.TypingsLocation,
            ProjectNameValue = options.ProjectNameValue,
            ExtraExtensions = options.ExtraExtensions,
            PackageJsonCache = options.PackageJsonCache
        };
        return TypeScript.Modules.DefaultResolver.NewResolver(copiedOptions);
    }
}

// internal/ls/autoimport/util.go:197. A bounded standard producer/consumer owner
// implements the private Go channel; no shared service/checker-pool replacement is introduced.
internal sealed class AutoImportCheckerPool
{
    private readonly TypeScript.Compiler.ICheckerProgramHost program;
    private readonly int maximum = Math.Max(1, Environment.ProcessorCount);
    private readonly System.Collections.Concurrent.BlockingCollection<TypeScript.Checking.Checker> pool;
    private int created;
    private int closed;
    internal AutoImportCheckerPool(TypeScript.Compiler.ICheckerProgramHost program)
    {
        this.program = program;
        pool = new(new System.Collections.Concurrent.ConcurrentQueue<TypeScript.Checking.Checker>(), maximum);
    }

    internal (TypeScript.Checking.Checker Checker, Action Done) GetChecker()
    {
        if (pool.TryTake(out var available))
            return (available, () => pool.Add(available));
        for (;;)
        {
            var current = Volatile.Read(ref created);
            if (current >= maximum)
            {
                var checker = pool.Take();
                return (checker, () => pool.Add(checker));
            }

            if (Interlocked.CompareExchange(ref created, current + 1, current) == current)
            {
                var checker = TypeScript.Checking.Checker.NewChecker(program).Checker;
                return (checker, () => pool.Add(checker));
            }
        }
    }

    internal void ClosePool()
    {
        if (Interlocked.Exchange(ref closed, 1) != 0)
            throw new InvalidOperationException("close of closed channel");
        pool.CompleteAdding();
    }

    internal int GetCreatedCount() => Volatile.Read(ref created);
}

// internal/ls/autoimport/util.go:314-326
internal sealed class AutoImportResolutionHost : TypeScript.Modules.IResolutionHost
{
    private readonly TypeScript.Vfs.IFS fs;
    private readonly RootedDirectoryPath currentDirectory;
    internal AutoImportResolutionHost(TypeScript.Vfs.IFS fs, RootedDirectoryPath currentDirectory)
    {
        this.fs = fs;
        this.currentDirectory = currentDirectory;
    }

    public RootedDirectoryPath GetCurrentDirectory() => currentDirectory;
    public TypeScript.Vfs.IFS FS() => fs;
}
