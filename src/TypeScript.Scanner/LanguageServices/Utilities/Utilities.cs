// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned internal/ls/lsutil/utilities.go algorithms.
#nullable disable
using TypeScript.Scanner;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    // utilities.go:15. One recursive callback owner per query, as in Go.
    internal static bool ProbablyUsesSemicolons(SourceFile file)
    {
        int withSemicolon = 0, withoutSemicolon = 0;
        const int nStatementsToObserve = 5;
        Visitor visit = default;
        visit = node =>
        {
            if ((node.Flags & NodeFlags.Reparsed) != 0)
                return false;
            if (SyntaxRequiresTrailingSemicolonOrASI(node.Kind))
            {
                var lastToken = GetLastToken(node, file);
                if (lastToken != null && lastToken.Kind == Kind.SemicolonToken)
                    withSemicolon++;
                else
                    withoutSemicolon++;
            }
            else if (SyntaxRequiresTrailingCommaOrSemicolonOrASI(node.Kind))
            {
                var lastToken = GetLastToken(node, file);
                if (lastToken != null && lastToken.Kind == Kind.SemicolonToken)
                    withSemicolon++;
                else if (lastToken != null && lastToken.Kind != Kind.CommaToken)
                {
                    var lastTokenLine = ScannerApi.GetECMALineOfPosition(file, AstNav.GetStartOfNode(lastToken, file, false));
                    var nextTokenLine = ScannerApi.GetECMALineOfPosition(file, ScannerApi.SkipTrivia(file.TextView(), lastToken.End()));
                    // Single-line object types do not provide significant missing-semicolon evidence.
                    if (lastTokenLine != nextTokenLine)
                        withoutSemicolon++;
                }
            }

            if (withSemicolon + withoutSemicolon >= nStatementsToObserve)
                return true;
            return node.ForEachChild(visit);
        };
        file.ForEachChild(visit);
        if (withSemicolon == 0 && withoutSemicolon <= 1)
            return true;
        if (withoutSemicolon == 0)
            return true;
        return withSemicolon * nStatementsToObserve > withoutSemicolon;
    }

    // utilities.go:76. Borrow actual import spelling and fall back to real Program metadata.
    internal static Tristate ShouldUseUriStyleNodeCoreModules(SourceFile file, TypeScript.Compiler.Program program)
    {
        foreach (var node in file.Imports())
        {
            var name = node.TextValue();
            if (Core.ContainsNodeCoreModule(name) && !Core.ContainsExclusivelyPrefixedNodeCoreModule(name))
                return name.StartsWith("node:"u8) ? Tristate.True : Tristate.False;
        }

        return program.UsesUriStyleNodeCoreModules();
    }

    internal static QuotePreference QuotePreferenceFromString(Node literal) => (literal.AsStringLiteral().TokenFlags & TokenFlags.SingleQuote) != 0 ? QuotePreference.Single : QuotePreference.Double;
    // utilities.go:97. Ignore synthesized helper imports when observing source quote preferences.
    internal static QuotePreference GetQuotePreference(SourceFile sourceFile, UserPreferences preferences)
    {
        if (preferences.QuotePreference.Value.Length != 0 && preferences.QuotePreference != QuotePreference.Auto)
            return preferences.QuotePreference == QuotePreference.Single ? QuotePreference.Single : QuotePreference.Double;
        var first = Core.Find(sourceFile.Imports(), static node => Ast.IsStringLiteral(node) && !Ast.NodeIsSynthesized(node.Parent));
        return first != null ? QuotePreferenceFromString(first) : QuotePreference.Double;
    }

    internal static string ModuleSymbolToValidIdentifier(Symbol moduleSymbol, bool forceCapitalize) => ModuleSymbolToValidIdentifierValue(moduleSymbol, forceCapitalize).Materialize()!;
    internal static ByteStringValue ModuleSymbolToValidIdentifierValue(Symbol moduleSymbol, bool forceCapitalize)
    {
        var name = moduleSymbol.NameValue;
        var(ambientName, ok) = Ast.TryGetAmbientModuleNameValueFromSymbolName(name);
        if (ok)
            name = ambientName;
        return ModuleSpecifierToValidIdentifierValue(name, forceCapitalize);
    }

    internal static string ModuleSpecifierToValidIdentifier(string moduleSpecifier, bool forceCapitalize) => ModuleSpecifierToValidIdentifier(ByteStringValue.FromString(moduleSpecifier), forceCapitalize);
    // utilities.go:125. Retain the two original rune-array owners and simple Unicode case mapping.
    internal static string ModuleSpecifierToValidIdentifier(ByteStringValue moduleSpecifier, bool forceCapitalize) => ModuleSpecifierToValidIdentifierValue(moduleSpecifier, forceCapitalize).Materialize()!;
    internal static ByteStringValue ModuleSpecifierToValidIdentifierValue(ByteStringValue moduleSpecifier, bool forceCapitalize)
    {
        var withoutExtension = TsPath.RemoveAnyFileExtension(moduleSpecifier);
        if (withoutExtension.EndsWith("/index"u8))
            withoutExtension = withoutExtension.Slice(0, withoutExtension.Length - 6);
        var baseName = TsPath.GetBaseFileName(withoutExtension);
        var result = Slice<int>.Make(0);
        bool lastCharWasValid = true;
        var runes = GoString.Runes(baseName);
        if (runes.Length > 0 && ScannerApi.IsIdentifierStart(runes[0]))
            result = result.Append(forceCapitalize ? UnicodeCase.ToUpper(runes[0]) : runes[0]);
        else
            lastCharWasValid = false;
        for (int i = 1; i < runes.Length; i++)
        {
            bool isValid = ScannerApi.IsIdentifierPart(runes[i]);
            if (isValid)
                result = result.Append(!lastCharWasValid ? UnicodeCase.ToUpper(runes[i]) : runes[i]);
            lastCharWasValid = isValid;
        }

        var builder = new GoBuilder();
        foreach (var rune in result)
            builder.WriteRune(rune);
        var text = builder.Value();
        return text.Length != 0 && !IsNonContextualKeyword(ScannerApi.StringToToken(text)) ? text : ByteStringValue.Concat(ByteStringValue.FromString("_"), text);
    }

    internal static bool IsNonContextualKeyword(Kind token) => Ast.IsKeywordKind(token) && !Ast.IsContextualKeyword(token);
}
