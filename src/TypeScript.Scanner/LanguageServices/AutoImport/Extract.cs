// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/extract.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Modules;
using TypeScript.LanguageServices.Utilities;

namespace TypeScript.LanguageServices.AutoImport;
internal sealed class ExtractorStats
{
    internal int exports;
    internal int usedChecker;
}

internal sealed class CheckerLease
{
    internal bool used;
    internal Checker checker = null!;
    // internal/ls/autoimport/extract.go:48
    internal Checker GetChecker()
    {
        used = true;
        return checker;
    }

    internal Checker? TryChecker() => used ? checker : null;
}

internal partial class SymbolExtractor
{
    protected ByteStringValue packageName;
    protected ExtractorStats stats = null!;
    protected NameResolver localNameResolver = null!;
    protected Checker checker = null!;
    protected CaseSensitivity caseSensitivity;
    protected Func<RootedFilePath, RootedFilePath>? realpath;
    // internal/ls/autoimport/extract.go:60. Stable reference owner corresponds to the Go pointer.
    internal static SymbolExtractor NewSymbolExtractor(ByteStringValue packageName, Checker checker, CaseSensitivity caseSensitivity, Func<RootedFilePath, RootedFilePath>? realpath) => new()
    {
        packageName = packageName,
        checker = checker,
        localNameResolver = new NameResolver
        {
            CompilerOptions = CompilerOptionHelpers.EmptyCompilerOptions
        },
        stats = new(),
        caseSensitivity = caseSensitivity,
        realpath = realpath,
    };
    // internal/ls/autoimport/extract.go:81
    protected ModuleID GetModuleID(SourceFile file) => realpath != null ? ModuleID.File(caseSensitivity.PathKey(realpath(RootedFilePath.FromValue(file.FileNameGoValue())).AsPath())) : ModuleID.File(PathKey.FromValue(file.PathKeyValue()));
    // internal/ls/autoimport/extract.go:91
    protected (ModuleID ID, bool Ok) GetModuleIDForSymbol(Symbol symbol)
    {
        var(moduleID, fileName, ok) = AutoImport.TryGetModuleIDAndFileNameOfModuleSymbol(symbol);
        if (!ok)
            return (default, false);
        if (fileName.AsValue().Length != 0 && realpath != null)
        {
            var declaration = Ast.GetNonAugmentationDeclaration(symbol);
            if (declaration != null && declaration.Kind == Kind.SourceFile)
                return (GetModuleID(declaration.AsSourceFile()), true);
        }

        return (moduleID, true);
    }

    // internal/ls/autoimport/extract.go:184
    internal void ExtractFromSymbol(ByteStringValue name, Symbol symbol, ModuleID moduleID, RootedFilePath moduleFileName, SourceFile file, ref Slice<Export> exports)
    {
        if (AutoImport.ShouldIgnoreSymbol(symbol))
            return;
        if (name.EqualsString(Ast.InternalSymbolNameExportStar))
        {
            var checkerLease = new CheckerLease
            {
                checker = checker
            };
            var allExports = checker.GetExportsOfModule(symbol.Parent);
            if (symbol.Parent.Exports != null)
                foreach (var(namedName, namedExport)in symbol.Parent.Exports)
                {
                    if (!namedName.Value.EqualsString(Ast.InternalSymbolNameExportStar))
                    {
                        var index = GoSlices.Index(allExports, namedExport);
                        // Keep the original Delete precondition/panic behavior, including its ignore branch.
                        if (index >= 0 || AutoImport.ShouldIgnoreSymbol(namedExport))
                            allExports = GoSlices.Delete(allExports, index, index + 1);
                    }
                }

            exports = exports.Grow(allExports.Count);
            foreach (var reexportedSymbol in allExports)
            {
                var(export, _) = CreateExport(reexportedSymbol, moduleID, moduleFileName, ExportSyntax.Star, file, checkerLease);
                if (export == null)
                    continue;
                var parent = checkerLease.GetChecker().GetMergedSymbol(reexportedSymbol.Parent);
                if (parent != null && parent.IsExternalModule())
                {
                    var(targetModuleID, ok) = GetModuleIDForSymbol(parent);
                    if (ok)
                        export.Target = new(targetModuleID, ByteStringKey.FromValue(reexportedSymbol.NameValue));
                }

                export.through = ByteStringValue.FromString(Ast.InternalSymbolNameExportStar);
                exports = exports.Append(export);
            }

            return;
        }

        var syntax = AutoImport.GetSyntax(symbol);
        var lease = new CheckerLease
        {
            checker = checker
        };
        var(result, target) = CreateExport(symbol, moduleID, moduleFileName, syntax, file, lease);
        if (result == null)
            return;
        exports = exports.Append(result);
        if (target != null)
        {
            if (syntax == ExportSyntax.Equals && (target.Flags & SymbolFlags.Namespace) != 0)
            {
                exports = exports.Grow(target.Exports?.Count ?? 0);
                if (target.Exports != null)
                    foreach (var(innerName, namedExport)in target.Exports)
                    {
                        if (innerName.Value.EqualsString(Ast.InternalSymbolNameExportStar))
                            continue;
                        var(export, _) = CreateExport(namedExport, moduleID, moduleFileName, syntax, file, lease);
                        if (export != null)
                        {
                            export.through = name;
                            exports = exports.Append(export);
                        }
                    }
            }
        }
        else if (syntax == ExportSyntax.CommonJSModuleExports)
        {
            var expression = symbol.Declarations[0].AsBinaryExpression().Right;
            if (expression.Kind == Kind.ObjectLiteralExpression)
            {
                // Original uncertainty about computed/method properties is retained; only these two kinds participate.
                exports = exports.Grow(expression.AsObjectLiteralExpression().Properties.Nodes.Count);
                foreach (var property in expression.AsObjectLiteralExpression().Properties.Nodes)
                {
                    if (Ast.IsShorthandPropertyAssignment(property) || Ast.IsPropertyAssignment(property) && property.Name().Kind == Kind.Identifier)
                    {
                        var(export, _) = CreateExport(expression.Symbol().Members[property.Name().TextValue()], moduleID, moduleFileName, syntax, file, lease);
                        if (export != null)
                        {
                            export.through = name;
                            exports = exports.Append(export);
                        }
                    }
                }
            }
        }
    }

    // internal/ls/autoimport/extract.go:266. Required fresh export; retained symbol/name headers are borrowed.
    internal (Export? Export, Symbol? Target) CreateExport(Symbol symbol, ModuleID moduleID, RootedFilePath moduleFileName, ExportSyntax syntax, SourceFile file, CheckerLease checkerLease)
    {
        if (AutoImport.ShouldIgnoreSymbol(symbol))
            return (null, null);
        var export = new Export
        {
            ExportID = new(moduleID, ByteStringKey.FromValue(symbol.NameValue)),
            ModuleFileName = moduleFileName,
            Syntax = syntax,
            Flags = symbol.CombinedLocalAndExportSymbolFlags(),
            Path = PathKey.FromValue(file.PathKeyValue()),
            PackageName = packageName,
        };
        if (syntax == ExportSyntax.UMD)
        {
            export.ExportID = new(moduleID, ByteStringKey.FromString(Ast.InternalSymbolNameExportEquals));
            export.localName = symbol.NameValue;
        }

        Symbol? targetSymbol = null;
        if ((symbol.Flags & SymbolFlags.Alias) != 0)
        {
            targetSymbol = TryResolveSymbol(symbol, syntax, checkerLease);
            if (targetSymbol != null)
            {
                Node? declaration = null;
                if (targetSymbol.Declarations.Count > 0)
                    declaration = targetSymbol.Declarations[0];
                else if ((targetSymbol.CheckFlags & CheckFlags.Mapped) != 0)
                {
                    var mappedDeclaration = checkerLease.GetChecker().GetMappedTypeSymbolOfProperty(targetSymbol);
                    if (mappedDeclaration != null && mappedDeclaration.Declarations.Count > 0)
                        declaration = mappedDeclaration.Declarations[0];
                }

                // Original fallback deliberately indexes the source declaration before the nil panic.
                declaration ??= symbol.Declarations[0];
                if (declaration == null)
                    throw new InvalidOperationException("no declaration for aliased symbol");
                var parent = targetSymbol.Parent;
                var usedChecker = checkerLease.TryChecker();
                if (usedChecker != null)
                {
                    export.Flags = usedChecker.GetSymbolFlags(targetSymbol);
                    export.IsTypeOnly = usedChecker.GetTypeOnlyAliasDeclaration(symbol) != null;
                    parent = usedChecker.GetMergedSymbol(parent);
                }
                else
                {
                    export.Flags = targetSymbol.Flags;
                    export.IsTypeOnly = Core.Some(symbol.Declarations, Ast.IsPartOfTypeOnlyImportOrExportDeclaration);
                }

                export.ScriptElementKind = LsUtil.GetSymbolKind(checkerLease.TryChecker(), targetSymbol, declaration);
                export.ScriptElementKindModifiers = LsUtil.GetSymbolModifiers(checkerLease.TryChecker(), targetSymbol);
                var targetModuleID = ModuleID.File(PathKey.FromValue(Ast.GetSourceFileOfNode(declaration).PathKeyValue()));
                if (parent != null && parent.IsExternalModule())
                {
                    var(id, ok) = GetModuleIDForSymbol(parent);
                    if (ok)
                        targetModuleID = id;
                }

                export.Target = new(targetModuleID, ByteStringKey.FromValue(targetSymbol.NameValue));
            }
        }
        else
        {
            export.ScriptElementKind = LsUtil.GetSymbolKind(checkerLease.TryChecker(), symbol, symbol.Declarations[0]);
            export.ScriptElementKindModifiers = LsUtil.GetSymbolModifiers(checkerLease.TryChecker(), symbol);
        }

        if (symbol.NameValue.EqualsString(Ast.InternalSymbolNameDefault) || symbol.NameValue.EqualsString(Ast.InternalSymbolNameExportEquals))
        {
            var namedSymbol = NameResolver.GetLocalSymbolForExportDefault(symbol) ?? symbol;
            export.localName = AutoImport.GetDefaultLikeExportNameFromDeclaration(namedSymbol);
            if (AutoImport.IsUnusableName(export.localName))
                export.localName = export.Target.ExportNameValue;
            if (AutoImport.IsUnusableName(export.localName) && targetSymbol != null)
            {
                namedSymbol = NameResolver.GetLocalSymbolForExportDefault(targetSymbol) ?? targetSymbol;
                export.localName = AutoImport.GetDefaultLikeExportNameFromDeclaration(namedSymbol);
            }

            if (AutoImport.IsUnusableName(export.localName))
            {
                // Original spelling is retained: a case-folded path key would lose PascalCase.
                export.localName = LsUtil.ModuleSpecifierToValidIdentifierValue(AutoImport.FileNameForDefaultExportName(targetSymbol, moduleFileName, moduleID), false);
            }
        }

        if (AutoImport.IsUnusableName(export.Name()))
            return (null, null);
        Interlocked.Increment(ref stats.exports);
        if (checkerLease.TryChecker() != null)
            Interlocked.Increment(ref stats.usedChecker);
        return (export, targetSymbol);
    }

    // internal/ls/autoimport/extract.go:371. The local resolver precedes the leased checker exactly as upstream.
    private Symbol? TryResolveSymbol(Symbol symbol, ExportSyntax syntax, CheckerLease checkerLease)
    {
        if (!Ast.IsNonLocalAlias(symbol, SymbolFlags.None))
            return symbol;
        Node? location = null;
        ByteStringValue name = default;
        switch (syntax)
        {
            case ExportSyntax.Named:
                var namedDeclaration = Ast.GetDeclarationOfKind(symbol, Kind.ExportSpecifier);
                if (namedDeclaration.Parent.Parent.AsExportDeclaration().ModuleSpecifier == null)
                {
                    var node = namedDeclaration.Name() ?? namedDeclaration.PropertyName();
                    if (node.Kind == Kind.Identifier)
                    {
                        location = node;
                        name = node.TextValue();
                    }
                }

                break;
            case ExportSyntax.Equals:
                if (!symbol.NameValue.EqualsString(Ast.InternalSymbolNameExportEquals))
                    break;
                goto case ExportSyntax.DefaultDeclaration;
            case ExportSyntax.DefaultDeclaration:
                var declaration = Ast.GetDeclarationOfKind(symbol, Kind.ExportAssignment);
                if (declaration.Expression().Kind == Kind.Identifier)
                {
                    location = declaration.Expression();
                    name = location.TextValue();
                }

                break;
        }

        if (location != null)
        {
            var local = localNameResolver.Resolve(location, name, SymbolFlags.All, null, false, false);
            if (local != null && !Ast.IsNonLocalAlias(local, SymbolFlags.None))
                return local;
        }

        var usedChecker = checkerLease.GetChecker();
        var resolved = usedChecker.GetAliasedSymbol(symbol);
        return !usedChecker.IsUnknownSymbol(resolved) ? resolved : null;
    }
}

// The embedded Go *symbolExtractor shares one identity/state owner. Inheritance preserves
// that shared storage without copying the statistics, name resolver or checker lease.
internal sealed partial class ExportExtractor : SymbolExtractor
{
    internal IResolver moduleResolver = null!;
    internal ExtractorStats Stats() => stats;
    internal static ExportExtractor NewExportExtractor(ByteStringValue packageName, Checker checker, CaseSensitivity caseSensitivity, IResolver moduleResolver, Func<RootedFilePath, RootedFilePath>? realpath) => new()
    {
        packageName = packageName,
        checker = checker,
        localNameResolver = new NameResolver
        {
            CompilerOptions = CompilerOptionHelpers.EmptyCompilerOptions
        },
        stats = new(),
        caseSensitivity = caseSensitivity,
        realpath = realpath,
        moduleResolver = moduleResolver,
    };
    // internal/ls/autoimport/extract.go:106
    internal Slice<Export> ExtractFromFile(SourceFile file)
    {
        if (file.Symbol != null)
            return ExtractFromModule(file);
        if (file.AmbientModuleNames.Count > 0)
        {
            var exportCount = 0;
            foreach (var statement in file.Statements.Nodes)
                if (Ast.IsModuleWithStringLiteralName(statement) && IsNonPatternAmbientModuleDeclaration(file, statement.AsModuleDeclaration()))
                    exportCount += statement.AsModuleDeclaration().Symbol.Exports?.Count ?? 0;
            var exports = Slice<Export>.Make(0, exportCount);
            foreach (var statement in file.Statements.Nodes)
                if (Ast.IsModuleWithStringLiteralName(statement) && IsNonPatternAmbientModuleDeclaration(file, statement.AsModuleDeclaration()))
                    ExtractFromModuleDeclaration(statement.AsModuleDeclaration(), file, ModuleID.Ambient(statement.Name().TextValue()), default, ref exports);
            return exports;
        }

        return default;
    }

    // internal/ls/autoimport/extract.go:137
    private Slice<Export> ExtractFromModule(SourceFile file)
    {
        var moduleAugmentations = Core.MapNonNilSlice(file.ModuleAugmentations, static name => Ast.IsGlobalScopeAugmentation(name.Parent) ? null : name.Parent.AsModuleDeclaration());
        var augmentationExportCount = 0;
        foreach (var declaration in moduleAugmentations)
            augmentationExportCount += declaration!.Symbol!.Exports?.Count ?? 0;
        var moduleID = GetModuleID(file);
        var exports = Slice<Export>.Make(0, (file.Symbol!.Exports?.Count ?? 0) + augmentationExportCount);
        if (file.Symbol!.Exports != null)
            foreach (var(name, symbol)in file.Symbol!.Exports)
                ExtractFromSymbol(name.Value, symbol, moduleID, RootedFilePath.FromValue(file.FileNameGoValue()), file, ref exports);
        foreach (var declaration in moduleAugmentations)
        {
            var name = declaration!.Name().TextValue();
            var ambientID = ModuleID.Ambient(name);
            RootedFilePath moduleFileName = default;
            ModuleSpecifier unresolvedModuleSpecifier = default;
            if (TsPath.IsExternalModuleNameRelative(name))
            {
                var(resolved, _, _) = moduleResolver.ResolveModuleName(name, RootedFilePath.FromValue(file.FileNameGoValue()), ResolutionMode.CommonJS);
                if (resolved.IsResolved())
                {
                    moduleFileName = resolved!.ResolvedFileName;
                    ambientID = ModuleID.File(caseSensitivity.PathKey(moduleFileName.AsPath()));
                }
                else
                {
                    moduleFileName = RootedFilePath.FromValue(file.FileNameGoValue()).Directory().ResolveFile(name);
                    ambientID = ModuleID.File(caseSensitivity.PathKey(moduleFileName.AsPath()));
                    unresolvedModuleSpecifier = TsPath.ToModuleSpecifier(name);
                }
            }

            var exportStart = exports.Count;
            ExtractFromModuleDeclaration(declaration, file, ambientID, moduleFileName, ref exports);
            foreach (var export in exports.Slice2(exportStart))
                export.UnresolvedModuleSpecifier = unresolvedModuleSpecifier;
        }

        return exports;
    }

    // internal/ls/autoimport/extract.go:178. ref retains the original pointer-to-slice replacement contract.
    private void ExtractFromModuleDeclaration(ModuleDeclaration declaration, SourceFile file, ModuleID moduleID, RootedFilePath moduleFileName, ref Slice<Export> exports)
    {
        if (declaration!.Symbol!.Exports == null)
            return;
        foreach (var(name, symbol)in declaration!.Symbol!.Exports)
            ExtractFromSymbol(name.Value, symbol, moduleID, moduleFileName, file, ref exports);
    }

    // internal/ls/autoimport/extract.go:128
    private static bool IsNonPatternAmbientModuleDeclaration(SourceFile file, ModuleDeclaration declaration)
    {
        foreach (var module in file.PatternAmbientModules)
            if (module.Symbol == declaration.Symbol)
                return false;
        return true;
    }
}

internal static partial class AutoImport
{
    // internal/ls/autoimport/extract.go:415
    internal static bool ShouldIgnoreSymbol(Symbol symbol) => (symbol.Flags & SymbolFlags.Prototype) != 0;
    // internal/ls/autoimport/extract.go:422
    internal static ExportSyntax GetSyntax(Symbol symbol)
    {
        foreach (var declaration in symbol.Declarations)
        {
            switch (declaration.Kind)
            {
                case Kind.ExportSpecifier:
                    return ExportSyntax.Named;
                case Kind.ExportAssignment:
                    return declaration.AsExportAssignment().IsExportEquals ? ExportSyntax.Equals : ExportSyntax.DefaultDeclaration;
                case Kind.NamespaceExportDeclaration:
                    return ExportSyntax.UMD;
                case Kind.BinaryExpression:
                    switch (Ast.GetAssignmentDeclarationKind(declaration))
                    {
                        case JSDeclarationKind.ModuleExports:
                            return ExportSyntax.CommonJSModuleExports;
                        case JSDeclarationKind.ExportsProperty:
                            return ExportSyntax.CommonJSExportsProperty;
                    }

                    break;
                default:
                    return (Ast.GetCombinedModifierFlags(declaration) & ModifierFlags.Default) != 0 ? ExportSyntax.DefaultModifier : ExportSyntax.Modifier;
            }
        }

        return ExportSyntax.None;
    }

    // internal/ls/autoimport/extract.go:453
    internal static bool IsUnusableName(ByteStringValue name) => name.Length == 0 || name.EqualsString("_default") || name.EqualsString(Ast.InternalSymbolNameExportStar) || name.EqualsString(Ast.InternalSymbolNameDefault) || name.EqualsString(Ast.InternalSymbolNameExportEquals);
    // internal/ls/autoimport/extract.go:466
    internal static ByteStringValue FileNameForDefaultExportName(Symbol? targetSymbol, RootedFilePath moduleFileName, ModuleID moduleID)
    {
        if (targetSymbol != null && targetSymbol.Declarations.Count > 0)
        {
            var fileName = RootedFilePath.FromValue(Ast.GetSourceFileOfNode(targetSymbol.Declarations[0]).FileNameGoValue());
            if (fileName.AsValue().Length != 0)
                return fileName.AsValue();
        }

        return moduleFileName.AsValue().Length != 0 ? moduleFileName.AsValue() : moduleID.AsValue();
    }
}
