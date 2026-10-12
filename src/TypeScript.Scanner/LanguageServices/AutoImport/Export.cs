// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/export.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.Utilities;

namespace TypeScript.LanguageServices.AutoImport;
internal enum ModuleIDKind : byte
{
    Invalid,
    File,
    Ambient
}

// Original comparable Go value: discriminant plus retained path/specifier headers.
internal readonly record struct ModuleID
{
    private readonly PathKey path;
    private readonly ModuleSpecifier specifier;
    private readonly ModuleIDKind kind;
    private ModuleID(PathKey path, ModuleSpecifier specifier, ModuleIDKind kind)
    {
        this.path = path;
        this.specifier = specifier;
        this.kind = kind;
    }

    internal static ModuleID File(PathKey path) => new(path, default, ModuleIDKind.File);
    internal static ModuleID Ambient(ByteStringValue specifier) => new(default, TsPath.ToModuleSpecifier(specifier), ModuleIDKind.Ambient);
    internal ByteStringValue AsValue() => kind switch
    {
        ModuleIDKind.File => path.AsValue(),
        ModuleIDKind.Ambient => specifier.AsValue(),
        _ => default,
    };
    internal bool IsAmbient() => kind == ModuleIDKind.Ambient;
    internal (PathKey Path, bool Ok) AsPathKey() => kind == ModuleIDKind.File ? (path, true) : (default, false);
    internal (ModuleSpecifier Specifier, bool Ok) AsModuleSpecifier() => kind == ModuleIDKind.Ambient ? (specifier, true) : (default, false);
}

internal readonly record struct ExportID(ModuleID ModuleID, ByteStringKey ExportName)
{
    internal ByteStringValue ExportNameValue => ExportName.Value;
}

internal enum ExportSyntax : long
{
    None,
    Modifier,
    Named,
    DefaultModifier,
    DefaultDeclaration,
    Equals,
    UMD,
    Star,
    CommonJSModuleExports,
    CommonJSExportsProperty,
}

internal sealed class Export : INamed
{
    internal ExportID ExportID;
    internal ModuleID ModuleID => ExportID.ModuleID;
    internal ByteStringValue ExportName => ExportID.ExportNameValue;

    internal RootedFilePath ModuleFileName;
    internal ModuleSpecifier UnresolvedModuleSpecifier;
    internal ExportSyntax Syntax;
    internal SymbolFlags Flags;
    internal ByteStringValue localName;
    internal ByteStringValue through;
    internal ExportID Target;
    internal bool IsTypeOnly;
    internal ScriptElementKind ScriptElementKind;
    internal ScriptElementKindModifier ScriptElementKindModifiers;
    internal PathKey Path;
    internal ByteStringValue PackageName;
    // internal/ls/autoimport/export.go:118
    public ByteStringValue Name()
    {
        if (localName.Length != 0)
            return localName;
        if (ExportName.EqualsString(Ast.InternalSymbolNameExportEquals))
            return Target.ExportNameValue;
        return ExportName;
    }

    internal bool IsRenameable() => ExportName.EqualsString(Ast.InternalSymbolNameExportEquals) || ExportName.EqualsString(Ast.InternalSymbolNameDefault);
    internal ByteStringValue AmbientModuleName() => ModuleID.IsAmbient() ? ModuleID.AsValue() : default;
    internal bool IsUnresolvedAlias() => Flags == SymbolFlags.Alias;
}

internal static partial class AutoImport
{
    // internal/ls/autoimport/export.go:142
    internal static Export? SymbolToExport(Symbol symbol, Checker checker)
    {
        if (symbol.Parent != null && Checker.IsExternalModuleSymbol(symbol.Parent))
        {
            var(moduleID, moduleFileName, ok) = TryGetModuleIDAndFileNameOfModuleSymbol(symbol.Parent);
            return ok ? ExtractFirstExport(symbol, checker, moduleID, moduleFileName, Ast.GetSourceFileOfModule(symbol.Parent)) : null;
        }

        var declaration = Core.FirstOrNil(symbol.Declarations);
        if (declaration == null)
            return null;
        var file = Ast.GetSourceFileOfNode(declaration);
        if (file.Symbol == null)
            return null;
        var moduleSymbol = checker.GetMergedSymbol(file.Symbol);
        var moduleIDForFile = ModuleID.File(PathKey.FromValue(file.PathKeyValue()));
        var fileName = RootedFilePath.FromValue(file.FileNameGoValue());
        var target = checker.GetMergedSymbol(checker.SkipAlias(symbol));
        var export = TryGetModuleExport(ByteStringValue.FromString(Ast.InternalSymbolNameDefault), target, moduleSymbol, checker, moduleIDForFile, fileName, file);
        if (export != null)
            return export;
        export = TryGetModuleExport(ByteStringValue.FromString(Ast.InternalSymbolNameExportEquals), target, moduleSymbol, checker, moduleIDForFile, fileName, file);
        if (export != null)
            return export;
        return TryGetModuleExport(symbol.NameValue, target, moduleSymbol, checker, moduleIDForFile, fileName, file);
    }

    // internal/ls/autoimport/export.go:173
    private static Export? TryGetModuleExport(ByteStringValue exportName, Symbol target, Symbol moduleSymbol, Checker checker, ModuleID moduleID, RootedFilePath moduleFileName, SourceFile file)
    {
        var exported = checker.TryGetMemberInModuleExportsAndProperties(exportName, moduleSymbol);
        return exported != null && checker.GetMergedSymbol(checker.SkipAlias(exported)) == target ? ExtractFirstExport(exported, checker, moduleID, moduleFileName, file) : null;
    }

    // internal/ls/autoimport/export.go:181
    private static Export? ExtractFirstExport(Symbol symbol, Checker checker, ModuleID moduleID, RootedFilePath moduleFileName, SourceFile file)
    {
        Slice<Export> exports = default;
        var extractor = SymbolExtractor.NewSymbolExtractor(default, checker, CaseSensitivity.CaseInsensitive, null);
        extractor.ExtractFromSymbol(symbol.NameValue, symbol, moduleID, moduleFileName, file, ref exports);
        return Core.FirstOrNil(exports);
    }
}
