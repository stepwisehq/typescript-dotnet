// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned internal/ls/lsutil/symbol_display.go.
#nullable disable
using TypeScript.Scanner;
using TypeScript.Checking;

namespace TypeScript.LanguageServices.Utilities;
internal enum ScriptElementKind
{
    Unknown = 0,
    Warning = 1,
    Keyword = 2,
    ScriptElement = 3,
    ModuleElement = 4,
    ClassElement = 5,
    LocalClassElement = 6,
    InterfaceElement = 7,
    TypeElement = 8,
    EnumElement = 9,
    EnumMemberElement = 10,
    VariableElement = 11,
    LocalVariableElement = 12,
    VariableUsingElement = 13,
    VariableAwaitUsingElement = 14,
    FunctionElement = 15,
    LocalFunctionElement = 16,
    MemberFunctionElement = 17,
    MemberGetAccessorElement = 18,
    MemberSetAccessorElement = 19,
    MemberVariableElement = 20,
    MemberAccessorVariableElement = 21,
    ConstructorImplementationElement = 22,
    CallSignatureElement = 23,
    IndexSignatureElement = 24,
    ConstructSignatureElement = 25,
    ParameterElement = 26,
    TypeParameterElement = 27,
    PrimitiveType = 28,
    Label = 29,
    Alias = 30,
    ConstElement = 31,
    LetElement = 32,
    Directory = 33,
    ExternalModuleName = 34,
    String = 35,
    Link = 36,
    LinkName = 37,
    LinkText = 38,
}

[Flags]
internal enum ScriptElementKindModifier : uint
{
    None = 0,
    Public = 1u << 1,
    Private = 1u << 2,
    Protected = 1u << 3,
    Exported = 1u << 4,
    Ambient = 1u << 5,
    Static = 1u << 6,
    Abstract = 1u << 7,
    Optional = 1u << 8,
    Deprecated = 1u << 9,
    Dts = 1u << 10,
    Ts = 1u << 11,
    Tsx = 1u << 12,
    Js = 1u << 13,
    Jsx = 1u << 14,
    Json = 1u << 15,
    Dmts = 1u << 16,
    Mts = 1u << 17,
    Mjs = 1u << 18,
    Dcts = 1u << 19,
    Cts = 1u << 20,
    Cjs = 1u << 21,
}

internal static partial class LsUtil
{
    internal const ScriptElementKindModifier FileExtensionKindModifiers = ScriptElementKindModifier.Dts | ScriptElementKindModifier.Ts | ScriptElementKindModifier.Tsx | ScriptElementKindModifier.Js | ScriptElementKindModifier.Jsx | ScriptElementKindModifier.Json | ScriptElementKindModifier.Dmts | ScriptElementKindModifier.Mts | ScriptElementKindModifier.Mjs | ScriptElementKindModifier.Dcts | ScriptElementKindModifier.Cts | ScriptElementKindModifier.Cjs;
    private static readonly (ScriptElementKindModifier flag, string name)[] scriptElementKindModifierNames = [(ScriptElementKindModifier.Public, "public"), (ScriptElementKindModifier.Private, "private"), (ScriptElementKindModifier.Protected, "protected"), (ScriptElementKindModifier.Exported, "export"), (ScriptElementKindModifier.Ambient, "declare"), (ScriptElementKindModifier.Static, "static"), (ScriptElementKindModifier.Abstract, "abstract"), (ScriptElementKindModifier.Optional, "optional"), (ScriptElementKindModifier.Deprecated, "deprecated"), (ScriptElementKindModifier.Dts, ".d.ts"), (ScriptElementKindModifier.Ts, ".ts"), (ScriptElementKindModifier.Tsx, ".tsx"), (ScriptElementKindModifier.Js, ".js"), (ScriptElementKindModifier.Jsx, ".jsx"), (ScriptElementKindModifier.Json, ".json"), (ScriptElementKindModifier.Dmts, ".d.mts"), (ScriptElementKindModifier.Mts, ".mts"), (ScriptElementKindModifier.Mjs, ".mjs"), (ScriptElementKindModifier.Dcts, ".d.cts"), (ScriptElementKindModifier.Cts, ".cts"), (ScriptElementKindModifier.Cjs, ".cjs"), ];
    // symbol_display.go:139. Original allocated set and flag iteration order.
    internal static HashSet<string> Strings(this ScriptElementKindModifier modifiers)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in scriptElementKindModifierNames)
            if ((modifiers & entry.flag) != 0)
                result.Add(entry.name);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/symbol_display.go:162:1.
    internal static ScriptElementKind GetSymbolKind(Checker typeChecker, Symbol symbol, Node location)
    {
        var result = getSymbolKindOfConstructorPropertyMethodAccessorFunctionOrVar(typeChecker, symbol, location);
        if ((result != ScriptElementKind.Unknown))
        {
            return result;
        }

        var flags = symbol.CombinedLocalAndExportSymbolFlags();
        if (((flags & SymbolFlags.Class) != 0))
        {
            var decl = Ast.GetDeclarationOfKind(symbol, Kind.ClassExpression);
            if ((decl != null))
            {
                return ScriptElementKind.LocalClassElement;
            }

            return ScriptElementKind.ClassElement;
        }

        if (((flags & SymbolFlags.Enum) != 0))
        {
            return ScriptElementKind.EnumElement;
        }

        if (((flags & SymbolFlags.TypeAlias) != 0))
        {
            return ScriptElementKind.TypeElement;
        }

        if (((flags & SymbolFlags.Interface) != 0))
        {
            return ScriptElementKind.InterfaceElement;
        }

        if (((flags & SymbolFlags.TypeParameter) != 0))
        {
            return ScriptElementKind.TypeParameterElement;
        }

        if (((flags & SymbolFlags.EnumMember) != 0))
        {
            return ScriptElementKind.EnumMemberElement;
        }

        if (((flags & SymbolFlags.Alias) != 0))
        {
            return ScriptElementKind.Alias;
        }

        if (((flags & SymbolFlags.Module) != 0))
        {
            return ScriptElementKind.ModuleElement;
        }

        return ScriptElementKind.Unknown;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/symbol_display.go:200:1.
    private static ScriptElementKind getSymbolKindOfConstructorPropertyMethodAccessorFunctionOrVar(Checker typeChecker, Symbol symbol, Node location)
    {
        Slice<Symbol> roots = default;
        if ((typeChecker != null))
        {
            roots = typeChecker.GetRootSymbols(symbol);
        }
        else
        {
            roots = Slice<Symbol>.FromArray([symbol]);
        }

        if ((((GoRuntime.Len(roots) == 1) && ((roots[0].Flags & SymbolFlags.Method) != 0)) && (((typeChecker == null) || (GoRuntime.Len(typeChecker.GetCallSignatures(typeChecker.GetNonNullableType(typeChecker.GetTypeOfSymbolAtLocation(symbol, location)))) > 0)))))
        {
            return ScriptElementKind.MemberFunctionElement;
        }

        if ((typeChecker != null))
        {
            if (typeChecker.IsUndefinedSymbol(symbol))
            {
                return ScriptElementKind.VariableElement;
            }

            if (typeChecker.IsArgumentsSymbol(symbol))
            {
                return ScriptElementKind.LocalVariableElement;
            }

            if ((((location.Kind == Kind.ThisKeyword) && Ast.IsExpression(location)) || Ast.IsThisInTypeQuery(location)))
            {
                return ScriptElementKind.ParameterElement;
            }
        }

        var flags = symbol.CombinedLocalAndExportSymbolFlags();
        if (((flags & SymbolFlags.Variable) != 0))
        {
            if (isFirstDeclarationOfSymbolParameter(symbol))
            {
                return ScriptElementKind.ParameterElement;
            }
            else
            {
                if (((symbol.ValueDeclaration != null) && Ast.IsVarConst(symbol.ValueDeclaration)))
                {
                    return ScriptElementKind.ConstElement;
                }
                else
                {
                    if (((symbol.ValueDeclaration != null) && Ast.IsVarUsing(symbol.ValueDeclaration)))
                    {
                        return ScriptElementKind.VariableUsingElement;
                    }
                    else
                    {
                        if (((symbol.ValueDeclaration != null) && Ast.IsVarAwaitUsing(symbol.ValueDeclaration)))
                        {
                            return ScriptElementKind.VariableAwaitUsingElement;
                        }
                        else
                        {
                            if (Core.Some(symbol.Declarations, Ast.IsLet))
                            {
                                return ScriptElementKind.LetElement;
                            }
                        }
                    }
                }
            }

            if (isLocalVariableOrFunction(symbol))
            {
                return ScriptElementKind.LocalVariableElement;
            }

            return ScriptElementKind.VariableElement;
        }

        if (((flags & SymbolFlags.Function) != 0))
        {
            if (isLocalVariableOrFunction(symbol))
            {
                return ScriptElementKind.LocalFunctionElement;
            }

            return ScriptElementKind.FunctionElement;
        }

        if (((flags & SymbolFlags.GetAccessor) != 0))
        {
            return ScriptElementKind.MemberGetAccessorElement;
        }

        if (((flags & SymbolFlags.SetAccessor) != 0))
        {
            return ScriptElementKind.MemberSetAccessorElement;
        }

        if (((flags & SymbolFlags.Method) != 0))
        {
            return ScriptElementKind.MemberFunctionElement;
        }

        if (((flags & SymbolFlags.Constructor) != 0))
        {
            return ScriptElementKind.ConstructorImplementationElement;
        }

        if (((flags & SymbolFlags.Signature) != 0))
        {
            return ScriptElementKind.IndexSignatureElement;
        }

        if (((flags & SymbolFlags.Property) != 0))
        {
            if ((((typeChecker != null) && ((flags & SymbolFlags.Transient) != 0)) && ((symbol.CheckFlags & CheckFlags.Synthetic) != 0)))
            {
                ScriptElementKind unionPropertyKind = default;
                {
                    foreach (var rootSymbol in GoRuntime.RangeValues(roots))
                    {
                        if (((rootSymbol.Flags & ((SymbolFlags.PropertyOrAccessor | SymbolFlags.Variable))) != 0))
                        {
                            unionPropertyKind = ScriptElementKind.MemberVariableElement;
                            break;
                        }
                    }
                }

                if ((unionPropertyKind == ScriptElementKind.Unknown))
                {
                    var typeOfUnionProperty = typeChecker.GetTypeOfSymbolAtLocation(symbol, location);
                    if ((GoRuntime.Len(typeChecker.GetCallSignatures(typeOfUnionProperty)) > 0))
                    {
                        return ScriptElementKind.MemberFunctionElement;
                    }

                    return ScriptElementKind.MemberVariableElement;
                }

                return unionPropertyKind;
            }

            return ScriptElementKind.MemberVariableElement;
        }

        return ScriptElementKind.Unknown;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/symbol_display.go:299:1.
    private static bool isFirstDeclarationOfSymbolParameter(Symbol symbol)
    {
        Node declaration = default;
        if ((GoRuntime.Len(symbol.Declarations) > 0))
        {
            declaration = symbol.Declarations[0];
        }

        var result = Ast.FindAncestorOrQuit(declaration, (n) =>
        {
            if (Ast.IsParameterDeclaration(n))
            {
                return FindAncestorResult.True;
            }

            if (((Ast.IsBindingElement(n) || Ast.IsObjectBindingPattern(n)) || Ast.IsArrayBindingPattern(n)))
            {
                return FindAncestorResult.False;
            }

            return FindAncestorResult.Quit;
        });
        return (result != null);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/symbol_display.go:317:1.
    private static bool isLocalVariableOrFunction(Symbol symbol)
    {
        if ((symbol.Parent != null))
        {
            return false;
        }

        {
            foreach (var decl in GoRuntime.RangeValues(symbol.Declarations))
            {
                if ((decl.Kind == Kind.FunctionExpression))
                {
                    return true;
                }

                if (((decl.Kind != Kind.VariableDeclaration) && (decl.Kind != Kind.FunctionDeclaration)))
                {
                    continue;
                }

                var parent = decl.Parent;
                for (; !Ast.IsFunctionBlock(parent); parent = parent.Parent)
                {
                    if (((parent.Kind == Kind.SourceFile) || (parent.Kind == Kind.ModuleBlock)))
                    {
                        break;
                    }
                }

                if (Ast.IsFunctionBlock(parent))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/symbol_display.go:349:1.
    internal static ScriptElementKindModifier GetSymbolModifiers(Checker typeChecker, Symbol symbol)
    {
        if ((symbol == null))
        {
            return ScriptElementKindModifier.None;
        }

        var modifiers = getNormalizedSymbolModifiers(typeChecker, symbol);
        if ((((symbol.Flags & SymbolFlags.Alias) != 0) && (typeChecker != null)))
        {
            var resolvedSymbol = typeChecker.GetAliasedSymbol(symbol);
            if ((resolvedSymbol != symbol))
            {
                modifiers |= getNormalizedSymbolModifiers(typeChecker, resolvedSymbol);
            }
        }

        if (((symbol.Flags & SymbolFlags.Optional) != 0))
        {
            modifiers |= ScriptElementKindModifier.Optional;
        }

        return modifiers;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/symbol_display.go:368:1.
    private static ScriptElementKindModifier getNormalizedSymbolModifiers(Checker typeChecker, Symbol symbol)
    {
        ScriptElementKindModifier modifierSet = default;
        if ((GoRuntime.Len(symbol.Declarations) > 0))
        {
            var declaration = symbol.Declarations[0];
            var declarations = symbol.Declarations.Slice2(1, symbol.Declarations.Length);
            ModifierFlags excludeFlags = default;
            if ((((GoRuntime.Len(declarations) > 0) && isDeprecatedDeclaration(typeChecker, declaration)) && Core.Some(declarations, (d) =>
            {
                return !isDeprecatedDeclaration(typeChecker, d);
            })))
            {
                excludeFlags = ModifierFlags.Deprecated;
            }
            else
            {
                excludeFlags = ModifierFlags.None;
            }

            modifierSet = getNodeModifiers(typeChecker, declaration, excludeFlags);
        }

        return modifierSet;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/symbol_display.go:388:1.
    private static bool isDeprecatedDeclaration(Checker typeChecker, Node declaration)
    {
        if ((typeChecker != null))
        {
            return typeChecker.IsDeprecatedDeclaration(declaration);
        }

        return Ast.IsDeprecatedDeclaration(declaration);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/symbol_display.go:395:1.
    private static ScriptElementKindModifier getNodeModifiers(Checker typeChecker, Node node, ModifierFlags excludeFlags)
    {
        ScriptElementKindModifier result = default;
        ModifierFlags flags = default;
        if (Ast.IsDeclaration(node))
        {
            flags = Ast.GetCombinedModifierFlags(node);
            if (isDeprecatedDeclaration(typeChecker, node))
            {
                flags |= ModifierFlags.Deprecated;
            }

            flags &= ~(excludeFlags);
        }

        if (((flags & ModifierFlags.Private) != 0))
        {
            result |= ScriptElementKindModifier.Private;
        }

        if (((flags & ModifierFlags.Protected) != 0))
        {
            result |= ScriptElementKindModifier.Protected;
        }

        if (((flags & ModifierFlags.Public) != 0))
        {
            result |= ScriptElementKindModifier.Public;
        }

        if (((flags & ModifierFlags.Static) != 0))
        {
            result |= ScriptElementKindModifier.Static;
        }

        if (((flags & ModifierFlags.Abstract) != 0))
        {
            result |= ScriptElementKindModifier.Abstract;
        }

        if (((flags & ModifierFlags.Export) != 0))
        {
            result |= ScriptElementKindModifier.Exported;
        }

        if (((flags & ModifierFlags.Deprecated) != 0))
        {
            result |= ScriptElementKindModifier.Deprecated;
        }

        if (((flags & ModifierFlags.Ambient) != 0))
        {
            result |= ScriptElementKindModifier.Ambient;
        }

        if (((node.Flags & NodeFlags.Ambient) != 0))
        {
            result |= ScriptElementKindModifier.Ambient;
        }

        if ((node.Kind == Kind.ExportAssignment))
        {
            result |= ScriptElementKindModifier.Exported;
        }

        return result;
    }
}
