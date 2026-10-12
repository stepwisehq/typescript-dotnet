// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/codeactions_importfixes.go, fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.Checking;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.Lsp.Protocol;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using AutoImportFunctions = TypeScript.LanguageServices.AutoImport.AutoImport;
using AstDiagnostic = TypeScript.Scanner.Diagnostic;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private static readonly Slice<int> importFixErrorCodes = Slice<int>.FromArray([Diagnostics.Cannot_find_name_0.Code(), Diagnostics.Cannot_find_name_0_Did_you_mean_1.Code(), Diagnostics.Cannot_find_name_0_Did_you_mean_the_instance_member_this_0.Code(), Diagnostics.Cannot_find_name_0_Did_you_mean_the_static_member_1_0.Code(), Diagnostics.Cannot_find_namespace_0.Code(), Diagnostics.X_0_refers_to_a_UMD_global_but_the_current_file_is_a_module_Consider_adding_an_import_instead.Code(), Diagnostics.X_0_only_refers_to_a_type_but_is_being_used_as_a_value_here.Code(), Diagnostics.No_value_exists_in_scope_for_the_shorthand_property_0_Either_declare_one_or_provide_an_initializer.Code(), Diagnostics.X_0_cannot_be_used_as_a_value_because_it_was_imported_using_import_type.Code(), Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_jQuery_Try_npm_i_save_dev_types_Slashjquery.Code(), Diagnostics.Cannot_find_name_0_Do_you_need_to_change_your_target_library_Try_changing_the_lib_compiler_option_to_1_or_later.Code(), Diagnostics.Cannot_find_name_0_Do_you_need_to_change_your_target_library_Try_changing_the_lib_compiler_option_to_include_dom.Code(), Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_a_test_runner_Try_npm_i_save_dev_types_Slashjest_or_npm_i_save_dev_types_Slashmocha_and_then_add_jest_or_mocha_to_the_types_field_in_your_tsconfig.Code(), Diagnostics.Cannot_find_name_0_Did_you_mean_to_write_this_in_an_async_function.Code(), Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_jQuery_Try_npm_i_save_dev_types_Slashjquery_and_then_add_jquery_to_the_types_field_in_your_tsconfig.Code(), Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_a_test_runner_Try_npm_i_save_dev_types_Slashjest_or_npm_i_save_dev_types_Slashmocha.Code(), Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_node_Try_npm_i_save_dev_types_Slashnode.Code(), Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_node_Try_npm_i_save_dev_types_Slashnode_and_then_add_node_to_the_types_field_in_your_tsconfig.Code(), Diagnostics.Cannot_find_namespace_0_Did_you_mean_1.Code(), Diagnostics.Cannot_extend_an_interface_0_Did_you_mean_implements.Code(), Diagnostics.This_JSX_tag_requires_0_to_be_in_scope_but_it_could_not_be_found.Code(), ]);
    private static readonly ByteStringValue importFixID = ByteStringValue.FromString("fixMissingImport");
    private static readonly CodeFixProvider ImportFixProvider = new()
    {
        ErrorCodes = importFixErrorCodes,
        GetCodeActions = getImportCodeActions,
        FixIds = Slice<ByteStringValue>.FromArray([importFixID]),
        GetAllCodeActions = getAllImportCodeActions,
    };
    private sealed class FixInfo
    {
        internal Fix Fix = null!;
        internal ByteStringValue SymbolName, ErrorIdentifierText;
        internal bool IsJsxNamespaceFix;
    }

    private static (Slice<CodeAction> Actions, Exception? Error) getImportCodeActions(ProtocolRequestContext context, CodeFixContext fixContext)
    {
        var(checker, done) = fixContext.Program.GetTypeChecker(context);
        try
        {
            var(infos, error) = getFixInfos(checker, fixContext, fixContext.ErrorCode, fixContext.Span.Pos());
            if (error is not null)
                return (default, error);
            if (infos.Length == 0)
                return (default, null);
            Slice<CodeAction> actions = default;
            foreach (var info in infos)
            {
                var(edits, description, ok) = info.Fix.Edits(context, fixContext.SourceFile, fixContext.Program.Options(), fixContext.LS.FormatOptions(), fixContext.LS.converters, fixContext.LS.UserPreferences());
                if (ok)
                    actions = actions.Append(new CodeAction { Description = description, Changes = edits, FixID = importFixID, FixAllDescription = ByteStringValue.FromString(Diagnostics.Add_all_missing_imports.Localize(context.Locale)) });
            }

            return (actions, null);
        }
        finally
        {
            done();
        }
    }

    private static (CombinedCodeActions? Actions, Exception? Error) getAllImportCodeActions(ProtocolRequestContext context, CodeFixContext fixContext)
    {
        if (RootedFilePath.FromValue(fixContext.SourceFile.FileNameGoValue()).IsDynamic())
            return (null, null);
        var diagnostics = fixContext.Program.GetSemanticDiagnostics(context, fixContext.SourceFile);
        Slice<AstDiagnostic> importDiagnostics = default;
        foreach (var diagnostic in diagnostics)
            if (isFixableDiagnostic(diagnostic, importFixErrorCodes))
                importDiagnostics = importDiagnostics.Append(diagnostic);
        if (importDiagnostics.Length == 0)
            return (null, null);
        var(checker, done) = fixContext.Program.GetTypeChecker(context);
        try
        {
            var(view, error) = fixContext.LS.getPreparedAutoImportView(fixContext.SourceFile, checker);
            if (error is not null)
                return (null, error);
            view ??= fixContext.LS.getCurrentAutoImportView(fixContext.SourceFile, checker);
            var adder = AutoImportFunctions.NewImportAdder(context, fixContext.Program, checker, fixContext.SourceFile, view, fixContext.LS.FormatOptions(), fixContext.LS.converters, fixContext.LS.UserPreferences());
            foreach (var diagnostic in importDiagnostics)
            {
                error = addImportFromDiagnostic(checker, adder, diagnostic, fixContext);
                if (error is not null)
                    return (null, error);
            }

            if (!adder.HasFixes())
                return (null, null);
            return (new CombinedCodeActions { Description = ByteStringValue.FromString(Diagnostics.Add_all_missing_imports.Localize(context.Locale)), Changes = adder.Edits() }, null);
        }
        finally
        {
            done();
        }
    }

    private static Exception? addImportFromDiagnostic(Checker checker, IImportAdder adder, AstDiagnostic diagnostic, CodeFixContext fixContext)
    {
        var diagnosticContext = new CodeFixContext
        {
            SourceFile = fixContext.SourceFile,
            Span = new TextRange(diagnostic.Pos(), diagnostic.End()),
            ErrorCode = diagnostic.Code(),
            Program = fixContext.Program,
            LS = fixContext.LS
        };
        var(infos, error) = getFixInfos(checker, diagnosticContext, diagnostic.Code(), diagnostic.Pos());
        if (error is not null)
            return error;
        if (infos.Length > 0)
            adder.AddImportFix(infos[0].Fix);
        return null;
    }

    private static (Slice<FixInfo> Infos, Exception? Error) getFixInfos(Checker checker, CodeFixContext fixContext, int errorCode, int position)
    {
        if (RootedFilePath.FromValue(fixContext.SourceFile.FileNameGoValue()).IsDynamic())
            return (default, null);
        var token = AstNav.GetTokenAtPosition(fixContext.SourceFile, position);
        if (errorCode != Diagnostics.X_0_refers_to_a_UMD_global_but_the_current_file_is_a_module_Consider_adding_an_import_instead.Code() && !Ast.IsIdentifier(token))
            return (default, null);
        View? view = null;
        Slice<FixInfo> infos = default;
        if (errorCode == Diagnostics.X_0_refers_to_a_UMD_global_but_the_current_file_is_a_module_Consider_adding_an_import_instead.Code())
        {
            view = fixContext.LS.getCurrentAutoImportView(fixContext.SourceFile, checker);
            infos = getFixesInfoForUMDImport(token, view, checker);
        }
        else if (errorCode == Diagnostics.X_0_cannot_be_used_as_a_value_because_it_was_imported_using_import_type.Code())
        {
            var names = getSymbolNamesToImport(fixContext.SourceFile, checker, token, fixContext.Program.Options());
            Slice<FixInfo> typeOnly = default;
            foreach (var name in names)
            {
                if (!name.IsTypeOnly)
                    continue;
                var fix = getTypeOnlyPromotionFix(fixContext.SourceFile, token, name.Name, checker);
                if (fix is not null)
                    typeOnly = typeOnly.Append(new FixInfo { Fix = fix, SymbolName = name.Name, ErrorIdentifierText = token.TextValue() });
            }

            var message = fixContext.Diagnostic?.Message.String ?? default;
            if (typeOnly.Length > 1 && message.Length != 0)
                foreach (var info in typeOnly)
                    if (message.IndexOf(ByteStringValue.Concat(ByteStringValue.FromString("'"), info.SymbolName, ByteStringValue.FromString("'"))) >= 0)
                        infos = infos.Append(info);
            if (infos.Length == 0)
                infos = typeOnly;
            return (infos, null);
        }
        else
        {
            var(prepared, error) = fixContext.LS.getPreparedAutoImportView(fixContext.SourceFile, checker);
            if (error is not null)
                return (default, error);
            view = prepared;
            if (view is not null)
                infos = getFixesInfoForNonUMDImport(fixContext, token, view, checker);
        }

        view ??= fixContext.LS.getCurrentAutoImportView(fixContext.SourceFile, checker);
        return (sortFixInfo(infos, fixContext, view), null);
    }

    private static Slice<FixInfo> getFixesInfoForUMDImport(Node token, View view, Checker checker)
    {
        var symbol = getUmdSymbol(token, checker);
        if (symbol is null)
            return default;
        var export = AutoImportFunctions.SymbolToExport(symbol, checker);
        bool validTypeOnly = Ast.IsValidTypeOnlyAliasUseSite(token);
        Slice<FixInfo> result = default;
        foreach (var fix in view.GetFixes(export!, false, validTypeOnly, null))
            result = result.Append(new FixInfo { Fix = fix, SymbolName = symbol.NameValue, ErrorIdentifierText = Ast.IsIdentifier(token) ? token.TextValue() : default });
        return result;
    }

    private static Symbol? getUmdSymbol(Node token, Checker checker)
    {
        Symbol? symbol = Ast.IsIdentifier(token) ? checker.GetResolvedSymbol(token) : null;
        if (isUMDExportSymbol(symbol))
            return symbol;
        var parent = token.Parent;
        if (Ast.IsJsxOpeningLikeElement(parent) && parent.TagName() == token || Ast.IsJsxOpeningFragment(parent))
        {
            var location = Ast.IsJsxOpeningLikeElement(parent) ? token : parent;
            var name = checker.GetJsxNamespaceValue(parent);
            var parentSymbol = checker.ResolveName(name, location, SymbolFlags.Value, false);
            if (isUMDExportSymbol(parentSymbol))
                return parentSymbol;
        }

        return null;
    }

    private static bool isUMDExportSymbol(Symbol? symbol) => symbol is not null && symbol.Declarations.Length > 0 && symbol.Declarations[0] is not null && Ast.IsNamespaceExportDeclaration(symbol.Declarations[0]);
    private static Slice<FixInfo> getFixesInfoForNonUMDImport(CodeFixContext fixContext, Node token, View view, Checker checker)
    {
        bool validTypeOnly = Ast.IsValidTypeOnlyAliasUseSite(token);
        var names = getSymbolNamesToImport(fixContext.SourceFile, checker, token, fixContext.Program.Options());
        Slice<FixInfo> result = default;
        var(usagePosition, fidelity) = fixContext.LS.converters.ToLSPPosition(fixContext.SourceFile, ScannerApi.GetTokenPosOfNode(token, fixContext.SourceFile, false));
        if (!fidelity.IsExact())
            return default;
        foreach (var name in names)
        {
            if (name.IsTypeOnly)
                continue;
            if (name.Name.EqualsBytes("default"u8))
                continue;
            bool isJSXTagName = name.Name.EqualsValue(token.TextValue()) && Ast.IsJsxTagName(token);
            var kind = isJSXTagName ? QueryKind.CaseInsensitiveMatch : QueryKind.ExactMatch;
            foreach (var export in view.Search(name.Name, kind))
            {
                if (isJSXTagName && !(export.Name().EqualsValue(name.Name) || export.IsRenameable()))
                    continue;
                foreach (var fix in view.GetFixes(export, isJSXTagName, validTypeOnly, usagePosition))
                    result = result.Append(new FixInfo { Fix = fix, SymbolName = name.Name, IsJsxNamespaceFix = !name.Name.EqualsValue(token.TextValue()) });
            }
        }

        return result;
    }

    private static Fix? getTypeOnlyPromotionFix(SourceFile file, Node token, ByteStringValue name, Checker checker)
    {
        var symbol = checker.ResolveName(name, token, SymbolFlags.Value, true);
        if (symbol is null)
            return null;
        var alias = checker.GetTypeOnlyAliasDeclaration(symbol);
        if (alias is null || Ast.GetSourceFileOfNode(alias) != file)
            return null;
        return new Fix
        {
            AutoImportFix = new AutoImportFix
            {
                Kind = AutoImportFixKind.PromoteTypeOnly
            },
            TypeOnlyAliasDeclaration = alias
        };
    }

    private readonly record struct SymbolNameInfo(ByteStringValue Name, bool IsTypeOnly);
    private static Slice<SymbolNameInfo> getSymbolNamesToImport(SourceFile file, Checker checker, Node token, CompilerOptions options)
    {
        var parent = token.Parent;
        if ((Ast.IsJsxOpeningLikeElement(parent) || Ast.IsJsxClosingElement(parent)) && parent.TagName() == token && jsxModeNeedsExplicitImport(options.Jsx))
        {
            var name = checker.GetJsxNamespaceValue(file);
            if (needsJsxNamespaceFix(name, token, checker))
            {
                Slice<SymbolNameInfo> result = default;
                if (!ScannerApi.IsIntrinsicJsxName(token.TextValue()))
                {
                    var component = checker.ResolveName(token.TextValue(), token, SymbolFlags.Value, false);
                    if (component is null)
                        result = result.Append(new SymbolNameInfo(token.TextValue(), false));
                    else if (checker.GetTypeOnlyAliasDeclaration(component)is not null)
                        result = result.Append(new SymbolNameInfo(token.TextValue(), true));
                }

                bool typeOnly = false;
                var namespaceSymbol = checker.ResolveName(name, token, SymbolFlags.Value, true);
                if (namespaceSymbol is not null)
                    typeOnly = checker.GetTypeOnlyAliasDeclaration(namespaceSymbol)is not null;
                result = result.Append(new SymbolNameInfo(name, typeOnly));
                return result;
            }
        }

        bool tokenTypeOnly = false;
        var symbol = checker.ResolveName(token.TextValue(), token, SymbolFlags.Value, true);
        if (symbol is not null)
            tokenTypeOnly = checker.GetTypeOnlyAliasDeclaration(symbol)is not null;
        return Slice<SymbolNameInfo>.FromArray([new SymbolNameInfo(token.TextValue(), tokenTypeOnly)]);
    }

    private static bool needsJsxNamespaceFix(ByteStringValue name, Node token, Checker checker)
    {
        if (ScannerApi.IsIntrinsicJsxName(token.TextValue()))
            return true;
        var symbol = checker.ResolveName(name, token, SymbolFlags.Value, true);
        if (symbol is null)
            return true;
        if (Core.Some(symbol.Declarations, Ast.IsTypeOnlyImportOrExportDeclaration))
            return (symbol.Flags & SymbolFlags.Value) == 0;
        return false;
    }

    private static bool jsxModeNeedsExplicitImport(JsxEmit jsx) => jsx is JsxEmit.React or JsxEmit.ReactNative;
    private static Slice<FixInfo> sortFixInfo(Slice<FixInfo> fixes, CodeFixContext fixContext, View view)
    {
        if (fixes.Length == 0)
            return fixes;
        var sorted = Slice<FixInfo>.Make(fixes.Length);
        fixes.AsSpan().CopyTo(sorted.AsSpan());
        GoSliceSort.SortFunc(sorted.AsSpan(), (a, b) =>
        {
            int comparison = Core.CompareBooleans(a.IsJsxNamespaceFix, b.IsJsxNamespaceFix);
            return comparison != 0 ? comparison : view.CompareFixesForSorting(a.Fix, b.Fix);
        });
        return sorted;
    }
}
