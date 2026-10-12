// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned internal/ls/lsutil/organizeimports.go counterpart.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    private const int importKindOrderSideEffect = 0, importKindOrderTypeOnly = 1, importKindOrderNamespace = 2, importKindOrderDefault = 3, importKindOrderNamed = 4, importKindOrderImportEquals = 5, importKindOrderRequire = 6, importKindOrderUnknown = 7;
    private sealed class namedImportSortResult
    {
        internal Func<string, string, int> namedImportComparer;
        internal OrganizeImportsTypeOrder typeOrder;
        internal bool isSorted;
    }

    private struct caseSensitivityDetectionResult
    {
        internal Func<string, string, int> comparer;
        internal bool isSorted;
    }

    // FilterImportDeclarations filters out non-import declarations from a list of statements.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:18:1.
    internal static Slice<Node> FilterImportDeclarations(Slice<Node> statements)
    {
        return Core.Filter(statements, (stmt) =>
        {
            return (stmt.Kind == Kind.ImportDeclaration);
        });
    }

    // GetDetectionLists returns the lists of comparers and type orders to test for organize imports detection.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:25:1.
    internal static (Slice<Func<string, string, int>>, Slice<OrganizeImportsTypeOrder>) GetDetectionLists(UserPreferences preferences)
    {
        Slice<Func<string, string, int>> comparersToTest = default;
        Slice<OrganizeImportsTypeOrder> typeOrdersToTest = default;
        if ((preferences.OrganizeImportsSort != OrganizeImportsSort.Auto))
        {
            comparersToTest = Slice<Func<string, string, int>>.FromArray([getOrganizeImportsPresetStringComparer(preferences.OrganizeImportsSort)]);
        }
        else
        {
            if (!(preferences.OrganizeImportsIgnoreCase == Tristate.Unknown))
            {
                comparersToTest = Slice<Func<string, string, int>>.FromArray([getOrganizeImportsStringComparer(preferences, (preferences.OrganizeImportsIgnoreCase == Tristate.True))]);
            }
            else
            {
                comparersToTest = Slice<Func<string, string, int>>.FromArray([getOrganizeImportsStringComparer(preferences, true), getOrganizeImportsStringComparer(preferences, false)]);
            }
        }

        if ((preferences.OrganizeImportsTypeOrder != OrganizeImportsTypeOrder.Auto))
        {
            typeOrdersToTest = Slice<OrganizeImportsTypeOrder>.FromArray([preferences.OrganizeImportsTypeOrder]);
        }
        else
        {
            typeOrdersToTest = Slice<OrganizeImportsTypeOrder>.FromArray([OrganizeImportsTypeOrder.Last, OrganizeImportsTypeOrder.Inline, OrganizeImportsTypeOrder.First]);
        }

        return (comparersToTest, typeOrdersToTest);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:50:1.
    internal static OrganizeImportsSort ResolveOrganizeImportsSort(UserPreferences preferences)
    {
        if ((preferences.OrganizeImportsSort != OrganizeImportsSort.Auto))
        {
            return preferences.OrganizeImportsSort;
        }

        if ((preferences.OrganizeImportsCollation == OrganizeImportsCollation.Unicode))
        {
            switch (preferences.OrganizeImportsIgnoreCase)
            {
                case Tristate.True:
                {
                    return OrganizeImportsSort.NaturalIgnoreCase;
                }

                case Tristate.False:
                {
                    return OrganizeImportsSort.Natural;
                }

                default:
                {
                    return OrganizeImportsSort.Auto;
                }
            }
        }

        switch (preferences.OrganizeImportsIgnoreCase)
        {
            case Tristate.True:
            {
                return OrganizeImportsSort.OrdinalIgnoreCase;
            }

            case Tristate.False:
            {
                return OrganizeImportsSort.Ordinal;
            }

            default:
            {
                return OrganizeImportsSort.Auto;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:76:1.
    private static Func<string, string, int> getOrganizeImportsOrdinalStringComparer(bool ignoreCase)
    {
        if (ignoreCase)
        {
            return StringUtil.CompareStringsCaseInsensitiveEslintCompatible;
        }

        return StringUtil.CompareStringsCaseSensitive;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:83:1.
    private static Func<string, string, int> getOrganizeImportsNaturalStringComparer(bool caseSensitive)
    {
        return (a2, b2) =>
        {
            return compareOrganizeImportsNaturalStrings(a2, b2, caseSensitive);
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:89:1.
    private static Func<string, string, int> getOrganizeImportsUnicodeStringComparer(bool ignoreCase, UserPreferences preferences)
    {
        var caseFirst = preferences.OrganizeImportsCaseFirst;
        var numeric = (preferences.OrganizeImportsNumericCollation == Tristate.True);
        var accents = !(preferences.OrganizeImportsAccentCollation == Tristate.False);
        return (a2, b2) =>
        {
            return compareOrganizeImportsUnicodeStrings(a2, b2, ignoreCase, caseFirst, numeric, accents);
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:99:1.
    private static int compareOrganizeImportsNaturalStrings(string a, string b, bool caseSensitive)
    {
        {
            var cmp = compareStringsNumeric(naturalCollationKey(a), naturalCollationKey(b));
            if ((cmp != 0))
            {
                return cmp;
            }
        }

        if (caseSensitive)
        {
            {
                var cmp2 = compareOrganizeImportsCaseUpperFirst(a, b);
                if ((cmp2 != 0))
                {
                    return cmp2;
                }
            }
        }

        return Math.Sign(GoStrings.Compare(a, b));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:113:1.
    private static int compareOrganizeImportsUnicodeStrings(string a, string b, bool ignoreCase, OrganizeImportsCaseFirst caseFirst, bool numeric, bool accents)
    {
        {
            var cmp = compareOrganizeImportsUnicodeKeys(naturalCollationKey(a), naturalCollationKey(b), numeric);
            if ((cmp != 0))
            {
                return cmp;
            }
        }

        if (accents)
        {
            {
                var cmp2 = compareOrganizeImportsUnicodeKeys(GoStrings.ToLower(ByteStringValue.FromString(a)), GoStrings.ToLower(ByteStringValue.FromString(b)), numeric);
                if ((cmp2 != 0))
                {
                    return cmp2;
                }
            }
        }

        if (!ignoreCase)
        {
            {
                var cmp3 = compareOrganizeImportsCase(a, b, caseFirst);
                if ((cmp3 != 0))
                {
                    return cmp3;
                }
            }
        }

        return Math.Sign(GoStrings.Compare(a, b));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:133:1.
    private static ByteStringValue naturalCollationKey(string s)
    {
        return GoStrings.ToLower(removeDiacritics(ByteStringValue.FromString(s)));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:137:1.
    private static ByteStringValue removeDiacritics(ByteStringValue s)
    {
        return GoStrings.Map(static (r) =>
        {
            if (GoUnicode.Is(GoUnicode.Mn, r))
            {
                return -1;
            }

            return r;
        }, TypeScript.UnicodeNormalization.Norm.String(s));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:146:1.
    private static int compareOrganizeImportsUnicodeKeys(ByteStringValue a, ByteStringValue b, bool numeric)
    {
        if (numeric)
        {
            return compareStringsNumeric(a, b);
        }

        return compareNumericByteHeaders(a, b);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:153:1.
    private static int compareStringsNumeric(ByteStringValue a, ByteStringValue b)
    {
        while (((a.Length > 0) && (b.Length > 0)))
        {
            if ((isASCIIDigit(a[0]) && isASCIIDigit(b[0])))
            {
                var aRunEnd = asciiDigitRunEnd(a);
                var bRunEnd = asciiDigitRunEnd(b);
                {
                    var cmp = compareNumericText(a.Slice(0, aRunEnd), b.Slice(0, bRunEnd));
                    if ((cmp != 0))
                    {
                        return cmp;
                    }
                }

                a = a.Slice(aRunEnd, a.Length);
                b = b.Slice(bRunEnd, b.Length);
                continue;
            }

            var tuple1 = (a.IsBytes ? Utf8.DecodeRuneInString(a.ByteSpan) : Utf8.DecodeRuneInString(a.CharSpan));
            var aRune = tuple1.Item1;
            var aSize = tuple1.Item2;
            var tuple2 = (b.IsBytes ? Utf8.DecodeRuneInString(b.ByteSpan) : Utf8.DecodeRuneInString(b.CharSpan));
            var bRune = tuple2.Item1;
            var bSize = tuple2.Item2;
            if ((aRune != bRune))
            {
                return aRune.CompareTo(bRune);
            }

            a = a.Slice(aSize, a.Length);
            b = b.Slice(bSize, b.Length);
        }

        return a.Length.CompareTo(b.Length);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:181:1.
    private static bool isASCIIDigit(int ch)
    {
        return ((ch >= '0') && (ch <= '9'));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:185:1.
    private static int asciiDigitRunEnd(ByteStringValue s)
    {
        var i = 0;
        while (((i < s.Length) && isASCIIDigit(s[i])))
        {
            i++;
        }

        return i;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:193:1.
    private static int compareNumericText(ByteStringValue a, ByteStringValue b)
    {
        var aDigits = a;
        while (aDigits.Length > 0 && aDigits[0] == '0')
            aDigits = aDigits.Slice(1);
        var bDigits = b;
        while (bDigits.Length > 0 && bDigits[0] == '0')
            bDigits = bDigits.Slice(1);
        if ((aDigits.Length == 0))
        {
            aDigits = ByteStringValue.FromString("0");
        }

        if ((bDigits.Length == 0))
        {
            bDigits = ByteStringValue.FromString("0");
        }

        if ((aDigits.Length != bDigits.Length))
        {
            return aDigits.Length.CompareTo(bDigits.Length);
        }

        {
            var cmp = compareNumericByteHeaders(aDigits, bDigits);
            if ((cmp != 0))
            {
                return cmp;
            }
        }

        return Math.Sign(compareNumericByteHeaders(a, b));
    }

    // Go strings.Compare over borrowed byte headers, without substring materialization.
    private static int compareNumericByteHeaders(ByteStringValue a, ByteStringValue b)
    {
        var length = Math.Min(a.Length, b.Length);
        for (var i = 0; i < length; i++)
            if (a[i] != b[i])
                return a[i] < b[i] ? -1 : 1;
        return a.Length.CompareTo(b.Length);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:212:1.
    private static int compareOrganizeImportsCaseUpperFirst(string a, string b)
    {
        return compareOrganizeImportsCase(a, b, OrganizeImportsCaseFirst.Upper);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:216:1.
    private static int compareOrganizeImportsCase(string a, string b, OrganizeImportsCaseFirst caseFirst)
    {
        var aRunes = GoString.Runes(a);
        var bRunes = GoString.Runes(b);
        var minLen = Math.Min(GoRuntime.Len(aRunes), GoRuntime.Len(bRunes));
        {
            foreach (var i in GoRuntime.RangeKeys(minLen))
            {
                var aUpper = GoUnicode.IsUpper(aRunes[i]);
                var bUpper = GoUnicode.IsUpper(bRunes[i]);
                if ((aUpper != bUpper))
                {
                    switch (caseFirst)
                    {
                        case OrganizeImportsCaseFirst.Upper:
                        {
                            if (aUpper)
                            {
                                return -1;
                            }

                            return 1;
                        }

                        case OrganizeImportsCaseFirst.Lower:
                        {
                            if (!aUpper)
                            {
                                return -1;
                            }

                            return 1;
                        }

                        default:
                        {
                            if (aUpper)
                            {
                                return 1;
                            }

                            return -1;
                        }
                    }
                }
            }
        }

        return GoRuntime.Len(aRunes).CompareTo(GoRuntime.Len(bRunes));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:248:1.
    internal static Func<string, string, int> getOrganizeImportsPresetStringComparer(OrganizeImportsSort sort)
    {
        switch (sort)
        {
            case OrganizeImportsSort.OrdinalIgnoreCase:
            {
                return getOrganizeImportsOrdinalStringComparer(true);
            }

            case OrganizeImportsSort.Natural:
            {
                return getOrganizeImportsNaturalStringComparer(true);
            }

            case OrganizeImportsSort.NaturalIgnoreCase:
            {
                return getOrganizeImportsNaturalStringComparer(false);
            }

            default:
            {
                return getOrganizeImportsOrdinalStringComparer(false);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:261:1.
    private static Func<string, string, int> getOrganizeImportsStringComparer(UserPreferences preferences, bool ignoreCase)
    {
        if ((preferences.OrganizeImportsSort != OrganizeImportsSort.Auto))
        {
            return getOrganizeImportsPresetStringComparer(preferences.OrganizeImportsSort);
        }

        if ((preferences.OrganizeImportsCollation == OrganizeImportsCollation.Unicode))
        {
            return getOrganizeImportsUnicodeStringComparer(ignoreCase, preferences);
        }

        return getOrganizeImportsOrdinalStringComparer(ignoreCase);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:271:1.
    private static Node getModuleSpecifierExpression(Node declaration)
    {
        switch (declaration.Kind)
        {
            case Kind.ImportEqualsDeclaration:
            {
                var importEquals = declaration.AsImportEqualsDeclaration();
                if ((importEquals.ModuleReference.Kind == Kind.ExternalModuleReference))
                {
                    return importEquals.ModuleReference.Expression();
                }

                return null;
            }

            case Kind.ImportDeclaration:
            {
                return declaration.ModuleSpecifier();
            }

            case Kind.VariableStatement:
            {
                var declarations = declaration.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes;
                if ((GoRuntime.Len(declarations) > 0))
                {
                    var initializer = declarations[0].Initializer();
                    if (((initializer != null) && (initializer.Kind == Kind.CallExpression)))
                    {
                        var callExpr = initializer.AsCallExpression();
                        if ((GoRuntime.Len(callExpr.Arguments.Nodes) > 0))
                        {
                            return callExpr.Arguments.Nodes[0];
                        }
                    }
                }

                return null;
            }

            default:
            {
                return null;
            }
        }
    }

    // GetExternalModuleName returns the module name from a module specifier expression.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:299:1.
    internal static string GetExternalModuleName(Node specifier)
    {
        if (((specifier != null) && Ast.IsStringLiteralLike(specifier.AsNode())))
        {
            return specifier.Text();
        }

        return "";
    }

    // CompareModuleSpecifiers compares two module specifiers using the given comparer.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:307:1.
    internal static int CompareModuleSpecifiers(Node m1, Node m2, Func<string, string, int> comparer)
    {
        var name1 = GetExternalModuleName(m1);
        var name2 = GetExternalModuleName(m2);
        {
            var cmp = Core.CompareBooleans((name1 == ""), (name2 == ""));
            if ((cmp != 0))
            {
                return cmp;
            }
        }

        {
            var cmp2 = Core.CompareBooleans(TsPath.IsExternalModuleNameRelative(name1), TsPath.IsExternalModuleNameRelative(name2));
            if ((cmp2 != 0))
            {
                return cmp2;
            }
        }

        return comparer(name1, name2);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:319:1.
    private static int compareImportKind(Node s1, Node s2)
    {
        return getImportKindOrder(s1).CompareTo(getImportKindOrder(s2));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:342:1.
    private static int getImportKindOrder(Node s1)
    {
        switch (s1.Kind)
        {
            case Kind.ImportDeclaration:
            {
                var importDecl = s1.AsImportDeclaration();
                if ((importDecl.ImportClause == null))
                {
                    return importKindOrderSideEffect;
                }

                var importClause = importDecl.ImportClause.AsImportClause();
                if (importClause.IsTypeOnly())
                {
                    return importKindOrderTypeOnly;
                }

                if (((importClause.NamedBindings != null) && (importClause.NamedBindings.Kind == Kind.NamespaceImport)))
                {
                    return importKindOrderNamespace;
                }

                if ((importClause.Name() != null))
                {
                    return importKindOrderDefault;
                }

                return importKindOrderNamed;
            }

            case Kind.ImportEqualsDeclaration:
            {
                return importKindOrderImportEquals;
            }

            case Kind.VariableStatement:
            {
                return importKindOrderRequire;
            }

            default:
            {
                return importKindOrderUnknown;
            }
        }
    }

    // CompareImportsOrRequireStatements compares two import or require statements.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:370:1.
    internal static int CompareImportsOrRequireStatements(Node s1, Node s2, Func<string, string, int> comparer)
    {
        {
            var cmp = CompareModuleSpecifiers(getModuleSpecifierExpression(s1), getModuleSpecifierExpression(s2), comparer);
            if ((cmp != 0))
            {
                return cmp;
            }
        }

        return compareImportKind(s1, s2);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:377:1.
    private static int compareImportOrExportSpecifiers(Node s1, Node s2, Func<string, string, int> comparer, UserPreferences preferences)
    {
        var typeOrder = preferences.OrganizeImportsTypeOrder;
        var s1Name = s1.Name().Text();
        var s2Name = s2.Name().Text();
        switch (typeOrder)
        {
            case OrganizeImportsTypeOrder.First:
            {
                {
                    var cmp = Core.CompareBooleans(s2.IsTypeOnly(), s1.IsTypeOnly());
                    if ((cmp != 0))
                    {
                        return cmp;
                    }
                }

                return comparer(s1Name, s2Name);
            }

            case OrganizeImportsTypeOrder.Inline:
            {
                return comparer(s1Name, s2Name);
            }

            default:
            {
                {
                    var cmp2 = Core.CompareBooleans(s1.IsTypeOnly(), s2.IsTypeOnly());
                    if ((cmp2 != 0))
                    {
                        return cmp2;
                    }
                }

                return comparer(s1Name, s2Name);
            }
        }
    }

    // GetNamedImportSpecifierComparer returns a comparer function for sorting import specifiers.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:400:1.
    internal static Func<Node, Node, int> GetNamedImportSpecifierComparer(UserPreferences preferences, Func<string, string, int> comparer)
    {
        if ((comparer == null))
        {
            var ignoreCase = false;
            if (!(preferences.OrganizeImportsIgnoreCase == Tristate.Unknown))
            {
                ignoreCase = (preferences.OrganizeImportsIgnoreCase == Tristate.True);
            }

            comparer = getOrganizeImportsStringComparer(preferences, ignoreCase);
        }

        return (s12, s22) =>
        {
            return compareImportOrExportSpecifiers(s12, s22, comparer, preferences);
        };
    }

    // GetImportSpecifierInsertionIndex returns the index at which to insert a new import specifier.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:414:1.
    internal static int GetImportSpecifierInsertionIndex(Slice<Node> sortedImports, Node newImport, Func<Node, Node, int> comparer)
    {
        return Core.BinarySearchUniqueFunc(sortedImports, (mid, value) =>
        {
            return comparer(value, newImport);
        }).Index;
    }

    // GetImportDeclarationInsertIndex returns the index at which to insert a new import declaration.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:421:1.
    internal static int GetImportDeclarationInsertIndex(Slice<Node> sortedImports, Node newImport, Func<Node, Node, int> comparer)
    {
        return Core.BinarySearchUniqueFunc(sortedImports, (mid, value) =>
        {
            return comparer(value, newImport);
        }).Index;
    }

    // GetOrganizeImportsStringComparerWithDetection returns a string comparer based on detecting the order of import statements by the module specifier
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:428:1.
    internal static (Func<string, string, int>, bool) GetOrganizeImportsStringComparerWithDetection(Slice<Node> originalImportDecls, UserPreferences preferences)
    {
        var tuple4 = DetectModuleSpecifierCaseBySort(Slice<Slice<Node>>.FromArray([originalImportDecls]), getComparers(preferences));
        var result = tuple4.Item1;
        var sorted = tuple4.Item2;
        return (result, sorted);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:433:1.
    private static Slice<Func<string, string, int>> getComparers(UserPreferences preferences)
    {
        if (((preferences.OrganizeImportsSort != OrganizeImportsSort.Auto) || !(preferences.OrganizeImportsIgnoreCase == Tristate.Unknown)))
        {
            var ignoreCase = false;
            if (!(preferences.OrganizeImportsIgnoreCase == Tristate.Unknown))
            {
                ignoreCase = (preferences.OrganizeImportsIgnoreCase == Tristate.True);
            }

            return Slice<Func<string, string, int>>.FromArray([getOrganizeImportsStringComparer(preferences, ignoreCase)]);
        }

        return Slice<Func<string, string, int>>.FromArray([getOrganizeImportsStringComparer(preferences, true), getOrganizeImportsStringComparer(preferences, false)]);
    }

    // DetectNamedImportOrganizationBySort detects the order of named imports throughout the file by considering the named imports in each statement as a group
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:454:1.
    internal static (Func<string, string, int>, OrganizeImportsTypeOrder, bool) DetectNamedImportOrganizationBySort(Slice<Node> originalGroups, Slice<Func<string, string, int>> comparersToTest, Slice<OrganizeImportsTypeOrder> typesToTest)
    {
        var result = detectNamedImportOrganizationBySort(originalGroups, comparersToTest, typesToTest);
        if ((result == null))
        {
            return (null, OrganizeImportsTypeOrder.Last, false);
        }

        return (result.namedImportComparer, result.typeOrder, true);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:466:1.
    private static namedImportSortResult detectNamedImportOrganizationBySort(Slice<Node> originalGroups, Slice<Func<string, string, int>> comparersToTest, Slice<OrganizeImportsTypeOrder> typesToTest)
    {
        bool bothNamedImports = default;
        Slice<Node> importDeclsWithNamed = default;
        {
            foreach (var imp in originalGroups)
            {
                if ((imp.AsImportDeclaration().ImportClause == null))
                {
                    continue;
                }

                var clause = imp.AsImportDeclaration().ImportClause.AsImportClause();
                if (((clause.NamedBindings == null) || (clause.NamedBindings.Kind != Kind.NamedImports)))
                {
                    continue;
                }

                var namedImports = clause.NamedBindings.AsNamedImports();
                if ((GoRuntime.Len(namedImports.Elements.Nodes) == 0))
                {
                    continue;
                }

                if (!bothNamedImports)
                {
                    var hasTypeOnly = false;
                    var hasRegular = false;
                    {
                        var range6Items = namedImports.Elements.Nodes;
                        for (int range6Index = 0; range6Index < range6Items.Count; range6Index++)
                        {
                            var _2 = range6Index;
                            var elem = range6Items[range6Index];
                            if (elem.IsTypeOnly())
                            {
                                hasTypeOnly = true;
                            }
                            else
                            {
                                hasRegular = true;
                            }
                        }
                    }

                    if ((hasTypeOnly && hasRegular))
                    {
                        bothNamedImports = true;
                    }
                }

                importDeclsWithNamed = importDeclsWithNamed.Append(imp);
            }
        }

        if ((GoRuntime.Len(importDeclsWithNamed) == 0))
        {
            return null;
        }

        var namedImportsByDecl = Slice<Slice<Node>>.Make(0, importDeclsWithNamed.Count);
        {
            var range7Items = importDeclsWithNamed;
            for (int range7Index = 0; range7Index < range7Items.Count; range7Index++)
            {
                var _3 = range7Index;
                var imp2 = range7Items[range7Index];
                var clause2 = imp2.AsImportDeclaration().ImportClause.AsImportClause();
                var namedImports2 = clause2.NamedBindings.AsNamedImports();
                namedImportsByDecl = namedImportsByDecl.Append(namedImports2.Elements.Nodes);
            }
        }

        if ((!bothNamedImports || (GoRuntime.Len(typesToTest) == 0)))
        {
            var namesList = Slice<Slice<string>>.Make(namedImportsByDecl.Count);
            {
                var range8Items = namedImportsByDecl;
                for (int range8Index = 0; range8Index < range8Items.Count; range8Index++)
                {
                    var i = range8Index;
                    var imports = range8Items[range8Index];
                    var names = Slice<string>.Make(imports.Count);
                    {
                        var range9Items = imports;
                        for (int range9Index = 0; range9Index < range9Items.Count; range9Index++)
                        {
                            var j = range9Index;
                            var imp3 = range9Items[range9Index];
                            names[j] = imp3.Name().Text();
                        }
                    }

                    namesList[i] = names;
                }
            }

            var sortState = detectCaseSensitivityBySort(namesList, comparersToTest);
            var typeOrder = OrganizeImportsTypeOrder.Last;
            if ((GoRuntime.Len(typesToTest) == 1))
            {
                typeOrder = typesToTest[0];
            }

            return new namedImportSortResult
            {
                namedImportComparer = sortState.comparer,
                typeOrder = typeOrder,
                isSorted = sortState.isSorted
            };
        }

        var bestDiff = new Dictionary<OrganizeImportsTypeOrder, int>
        {
            [OrganizeImportsTypeOrder.First] = int.MaxValue,
            [OrganizeImportsTypeOrder.Last] = int.MaxValue,
            [OrganizeImportsTypeOrder.Inline] = int.MaxValue
        };
        var bestComparer = new Dictionary<OrganizeImportsTypeOrder, Func<string, string, int>>
        {
            [OrganizeImportsTypeOrder.First] = comparersToTest[0],
            [OrganizeImportsTypeOrder.Last] = comparersToTest[0],
            [OrganizeImportsTypeOrder.Inline] = comparersToTest[0]
        };
        {
            var range10Items = comparersToTest;
            for (int range10Index = 0; range10Index < range10Items.Count; range10Index++)
            {
                var _4 = range10Index;
                var curComparer = range10Items[range10Index];
                var currDiff = new Dictionary<OrganizeImportsTypeOrder, int>
                {
                    [OrganizeImportsTypeOrder.First] = 0,
                    [OrganizeImportsTypeOrder.Last] = 0,
                    [OrganizeImportsTypeOrder.Inline] = 0
                };
                {
                    var range11Items = namedImportsByDecl;
                    for (int range11Index = 0; range11Index < range11Items.Count; range11Index++)
                    {
                        var _5 = range11Index;
                        var importDecl = range11Items[range11Index];
                        {
                            var range12Items = typesToTest;
                            for (int range12Index = 0; range12Index < range12Items.Count; range12Index++)
                            {
                                var _6 = range12Index;
                                var typeOrder2 = range12Items[range12Index];
                                var prefs = new UserPreferences
                                {
                                    OrganizeImportsTypeOrder = typeOrder2
                                };
                                var diff = measureSortedness(importDecl, (n1, n2) =>
                                {
                                    return compareImportOrExportSpecifiers(n1, n2, curComparer, prefs);
                                });
                                currDiff[typeOrder2] = (currDiff[typeOrder2] + diff);
                            }
                        }
                    }
                }

                {
                    var range13Items = typesToTest;
                    for (int range13Index = 0; range13Index < range13Items.Count; range13Index++)
                    {
                        var _7 = range13Index;
                        var typeOrder3 = range13Items[range13Index];
                        if ((currDiff[typeOrder3] < bestDiff[typeOrder3]))
                        {
                            bestDiff[typeOrder3] = currDiff[typeOrder3];
                            bestComparer[typeOrder3] = curComparer;
                        }
                    }
                }
            }
        }

        {
            var range14Items = typesToTest;
            for (int range14Index = 0; range14Index < range14Items.Count; range14Index++)
            {
                var _8 = range14Index;
                var bestTypeOrder = range14Items[range14Index];
                var isBest = true;
                {
                    var range15Items = typesToTest;
                    for (int range15Index = 0; range15Index < range15Items.Count; range15Index++)
                    {
                        var _9 = range15Index;
                        var testTypeOrder = range15Items[range15Index];
                        if ((bestDiff[testTypeOrder] < bestDiff[bestTypeOrder]))
                        {
                            isBest = false;
                            break;
                        }
                    }
                }

                if (isBest)
                {
                    return new namedImportSortResult
                    {
                        namedImportComparer = bestComparer[bestTypeOrder],
                        typeOrder = bestTypeOrder,
                        isSorted = (bestDiff[bestTypeOrder] == 0)
                    };
                }
            }
        }

        return new namedImportSortResult
        {
            namedImportComparer = bestComparer[OrganizeImportsTypeOrder.Last],
            typeOrder = OrganizeImportsTypeOrder.Last,
            isSorted = (bestDiff[OrganizeImportsTypeOrder.Last] == 0)
        };
    }

    // DetectModuleSpecifierCaseBySort detects the order of module specifiers based on import statements throughout the module/file
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:603:1.
    internal static (Func<string, string, int>, bool) DetectModuleSpecifierCaseBySort(Slice<Slice<Node>> importDeclsByGroup, Slice<Func<string, string, int>> comparersToTest)
    {
        var moduleSpecifiersByGroup = Slice<Slice<string>>.Make(0, importDeclsByGroup.Count);
        {
            foreach (var importGroup in importDeclsByGroup)
            {
                var moduleNames = Slice<string>.Make(0, importGroup.Count);
                {
                    var range17Items = importGroup;
                    for (int range17Index = 0; range17Index < range17Items.Count; range17Index++)
                    {
                        var _2 = range17Index;
                        var decl = range17Items[range17Index];
                        {
                            var expr = getModuleSpecifierExpression(decl);
                            if ((expr != null))
                            {
                                moduleNames = moduleNames.Append(GetExternalModuleName(expr));
                            }
                            else
                            {
                                moduleNames = moduleNames.Append("");
                            }
                        }
                    }
                }

                moduleSpecifiersByGroup = moduleSpecifiersByGroup.Append(moduleNames);
            }
        }

        var result = detectCaseSensitivityBySort(moduleSpecifiersByGroup, comparersToTest);
        return (result.comparer, result.isSorted);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:620:1.
    private static caseSensitivityDetectionResult detectCaseSensitivityBySort(Slice<Slice<string>> originalGroups, Slice<Func<string, string, int>> comparersToTest)
    {
        Func<string, string, int> bestComparer = default;
        var bestDiff = int.MaxValue;
        {
            foreach (var curComparer in comparersToTest)
            {
                var diffOfCurrentComparer = 0;
                {
                    var range19Items = originalGroups;
                    for (int range19Index = 0; range19Index < range19Items.Count; range19Index++)
                    {
                        var _2 = range19Index;
                        var listToSort = range19Items[range19Index];
                        if ((GoRuntime.Len(listToSort) <= 1))
                        {
                            continue;
                        }

                        var diff = measureSortedness(listToSort, curComparer);
                        diffOfCurrentComparer += diff;
                    }
                }

                if ((diffOfCurrentComparer < bestDiff))
                {
                    bestDiff = diffOfCurrentComparer;
                    bestComparer = curComparer;
                }
            }
        }

        if (((bestComparer == null) && (GoRuntime.Len(comparersToTest) > 0)))
        {
            bestComparer = comparersToTest[0];
        }

        return new caseSensitivityDetectionResult
        {
            comparer = bestComparer,
            isSorted = (bestDiff == 0)
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:651:1.
    private static int measureSortedness<T>(Slice<T> arr, Func<T, T, int> comparer)
    {
        var i = 0;
        {
            for (int j = 0; j < arr.Count - 1; j++)
            {
                if ((comparer(arr[j], arr[(j + 1)]) > 0))
                {
                    i++;
                }
            }
        }

        return i;
    }

    // GetNamedImportSpecifierComparerWithDetection returns a specifier comparer based on detecting the existing sort order within a single import statement
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/organizeimports.go:662:1.
    internal static (Func<Node, Node, int>, Tristate) GetNamedImportSpecifierComparerWithDetection(Node importDecl, SourceFile sourceFile, UserPreferences preferences)
    {
        Func<Node, Node, int> specifierComparer = default;
        Tristate isSorted = default;
        var tuple21 = GetDetectionLists(preferences);
        var comparersToTest = tuple21.Item1;
        var typeOrdersToTest = tuple21.Item2;
        Node importStmt = default;
        if ((importDecl.Kind == Kind.ImportDeclaration))
        {
            importStmt = importDecl;
        }

        specifierComparer = GetNamedImportSpecifierComparer(preferences, comparersToTest[0]);
        isSorted = Tristate.Unknown;
        if (((((ResolveOrganizeImportsSort(preferences) == OrganizeImportsSort.Auto) || (preferences.OrganizeImportsTypeOrder == OrganizeImportsTypeOrder.Auto))) && (importStmt != null)))
        {
            var detectFromDecl = detectNamedImportOrganizationBySort(Slice<Node>.FromArray([importStmt]), comparersToTest, typeOrdersToTest);
            if ((detectFromDecl != null))
            {
                isSorted = (detectFromDecl.isSorted ? Tristate.True : Tristate.False);
                specifierComparer = GetNamedImportSpecifierComparer(new UserPreferences { OrganizeImportsTypeOrder = detectFromDecl.typeOrder }, detectFromDecl.namedImportComparer);
            }
            else
            {
                if ((sourceFile != null))
                {
                    var allImports = FilterImportDeclarations(sourceFile.Statements.Nodes);
                    var detectFromFile = detectNamedImportOrganizationBySort(allImports, comparersToTest, typeOrdersToTest);
                    if ((detectFromFile != null))
                    {
                        isSorted = (detectFromFile.isSorted ? Tristate.True : Tristate.False);
                        specifierComparer = GetNamedImportSpecifierComparer(new UserPreferences { OrganizeImportsTypeOrder = detectFromFile.typeOrder }, detectFromFile.namedImportComparer);
                    }
                }
            }
        }

        return (specifierComparer, isSorted);
    }
}
