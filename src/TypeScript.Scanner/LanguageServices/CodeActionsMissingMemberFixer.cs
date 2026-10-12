// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/codeactions_missingmemberfixer.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Change;
using TypeScript.LanguageServices.Utilities;
using CompilerProgram = TypeScript.Compiler.Program;
using LocaleValue = TypeScript.Locale.Locale;
using BuilderFlags = TypeScript.NodeBuilding.Flags;
using InternalFlags = TypeScript.NodeBuilding.InternalFlags;
using Type = TypeScript.Checking.Type;
using AutoImportFunctions = TypeScript.LanguageServices.AutoImport.AutoImport;

namespace TypeScript.LanguageServices;
[Flags]
internal enum PreserveOptionalFlags
{
    Method = 1,
    Property = 2,
    All = Method | Property
}

internal sealed partial class LanguageService
{
    internal static MissingMemberFixer newMissingMemberFixer(Tracker changeTracker, CompilerProgram program, Checker typeChecker, UserPreferences preferences, IImportAdder? importAdder, LocaleValue locale) => new(changeTracker, program, typeChecker, preferences, importAdder, locale);
    internal sealed partial class MissingMemberFixer
    {
        private readonly Tracker changeTracker;
        private readonly Checker typeChecker;
        private readonly CompilerProgram program;
        private readonly UserPreferences preferences;
        private readonly IImportAdder? importAdder;
        private readonly LocaleValue locale;
        internal MissingMemberFixer(Tracker changeTracker, CompilerProgram program, Checker typeChecker, UserPreferences preferences, IImportAdder? importAdder, LocaleValue locale)
        {
            this.changeTracker = changeTracker;
            this.typeChecker = typeChecker;
            this.program = program;
            this.preferences = preferences;
            this.importAdder = importAdder;
            this.locale = locale;
        }

        private (NodeBuilder Builder, Dictionary<Node, Symbol> Symbols) createNodeBuilder()
        {
            var idToSymbol = new Dictionary<Node, Symbol>();
            return (Checker.NewNodeBuilderEx(typeChecker, changeTracker.EmitContext, idToSymbol), idToSymbol);
        }

        internal Slice<Node> createMemberFromSymbol(Symbol symbol, Node enclosingDeclaration, SourceFile sourceFile, Node? body, PreserveOptionalFlags preserveOptional, bool abstractMember)
        {
            var declarations = symbol.Declarations;
            var declaration = declarations.Length == 0 ? null : declarations[0];
            var quotePreference = LsUtil.GetQuotePreference(sourceFile, preferences);
            var signatureOnly = (enclosingDeclaration.Flags & NodeFlags.Ambient) != 0 || abstractMember;
            var optional = (symbol.Flags & SymbolFlags.Optional) != 0;
            var kind = declaration?.Kind ?? Kind.PropertySignature;
            var name = createDeclarationName(changeTracker.NodeFactory, typeChecker, symbol, declaration);
            var modifiers = createModifiers(symbol, declaration);
            var flags = BuilderFlags.NoTruncation;
            if (quotePreference == QuotePreference.Single)
                flags |= BuilderFlags.UseSingleQuotesForStringLiteralType;
            var type = typeChecker.GetWidenedType(typeChecker.GetTypeOfSymbolAtLocation(symbol, enclosingDeclaration));
            Slice<Node> nodes = default;
            var factory = changeTracker.NodeFactory;
            switch (kind)
            {
                case Kind.PropertySignature:
                case Kind.PropertyDeclaration:
                {
                    var(builder, symbols) = createNodeBuilder();
                    var typeNode = createTypeNode(type, enclosingDeclaration, flags, builder, symbols);
                    var question = optional && (preserveOptional & PreserveOptionalFlags.Property) != 0 ? factory.NewToken(Kind.QuestionToken) : null;
                    return nodes.Append(factory.NewPropertyDeclaration(modifiers, createPropertyName(factory, name!, quotePreference), question, typeNode, null));
                }

                case Kind.GetAccessor:
                case Kind.SetAccessor:
                {
                    var(builder, symbols) = createNodeBuilder();
                    var accessors = Ast.GetAllAccessorDeclarations(declarations, declaration!);
                    Slice<Node> ordered = default;
                    ordered = accessors.SecondAccessor == null ? ordered.Append(accessors.FirstAccessor) : ordered.Append(Slice<Node>.FromArray([accessors.FirstAccessor, accessors.SecondAccessor]));
                    foreach (var accessor in ordered)
                    {
                        if (Ast.IsGetAccessorDeclaration(accessor))
                        {
                            nodes = nodes.Append(factory.NewGetAccessorDeclaration(modifiers, createPropertyName(factory, name!, quotePreference), null, null, createTypeNode(type, enclosingDeclaration, flags, builder, symbols), null, createBody(body, quotePreference, signatureOnly)));
                        }

                        if (Ast.IsSetAccessorDeclaration(accessor))
                        {
                            var parameter = Checker.GetSetAccessorValueParameter(accessor);
                            if (parameter == null)
                                throw new InvalidOperationException("Expected set accessor to have a parameter.");
                            nodes = nodes.Append(factory.NewSetAccessorDeclaration(modifiers, createPropertyName(factory, name!, quotePreference), null, createDummyParameters(factory, 1, Slice<ByteStringValue>.FromArray([parameter.Name().TextValue()]), Slice<Node>.FromArray([createTypeNode(type, enclosingDeclaration, flags, builder, symbols)!]), 1, Ast.IsInJSFile(enclosingDeclaration)), null, null, createBody(body, quotePreference, signatureOnly)));
                        }
                    }

                    return nodes;
                }

                case Kind.MethodSignature:
                case Kind.MethodDeclaration:
                {
                    var signatures = getCallSignatures(type);
                    var preserveMethodOptional = optional && (preserveOptional & PreserveOptionalFlags.Method) != 0;
                    if (signatures.Length == 0)
                        return default;
                    if (declarations.Length == 1)
                    {
                        var method = createSignatureDeclarationFromSignature(signatures[0], Kind.MethodDeclaration, sourceFile, enclosingDeclaration, createBody(body, quotePreference, signatureOnly), modifiers, name, preserveMethodOptional);
                        if (method != null)
                            nodes = nodes.Append(method);
                        return nodes;
                    }

                    foreach (var signature in signatures)
                    {
                        if (signature.Declaration() != null && (signature.Declaration().Flags & NodeFlags.Ambient) != 0)
                            continue;
                        var method = createSignatureDeclarationFromSignature(signature, Kind.MethodDeclaration, sourceFile, enclosingDeclaration, null, modifiers, name, preserveMethodOptional);
                        if (method != null)
                            nodes = nodes.Append(method);
                    }

                    if (signatureOnly)
                        return nodes;
                    if (declarations.Length > signatures.Length)
                    {
                        var signature = typeChecker.GetSignatureFromDeclaration(declarations[declarations.Length - 1]);
                        var method = createSignatureDeclarationFromSignature(signature, Kind.MethodDeclaration, sourceFile, enclosingDeclaration, createBody(body, quotePreference, false), modifiers, name, preserveMethodOptional);
                        if (method != null)
                            nodes = nodes.Append(method);
                    }
                    else
                    {
                        var method = createSignatureDeclarationFromSignatures(signatures, name, preserveMethodOptional, modifiers, quotePreference, body, enclosingDeclaration);
                        if (method != null)
                            nodes = nodes.Append(method);
                    }

                    return nodes;
                }
            }

            return default;
        }

        private Node? createSignatureDeclarationFromSignature(Signature signature, Kind kind, SourceFile sourceFile, Node enclosingDeclaration, Node? body, ModifierList? modifiers, Node? name, bool optional)
        {
            var quotePreference = LsUtil.GetQuotePreference(sourceFile, preferences);
            var flags = BuilderFlags.NoTruncation | BuilderFlags.SuppressAnyReturnType | BuilderFlags.AllowEmptyTuple;
            if (quotePreference == QuotePreference.Single)
                flags |= BuilderFlags.UseSingleQuotesForStringLiteralType;
            var(builder, symbols) = createNodeBuilder();
            var signatureDeclaration = builder.SignatureToSignatureDeclaration(signature, kind, enclosingDeclaration, flags, InternalFlags.AllowUnresolvedNames, null);
            if (signatureDeclaration == null)
                return null;
            var isJS = Ast.IsInJSFile(enclosingDeclaration);
            var parameters = signatureDeclaration.ParameterList();
            var typeParameters = isJS ? null : signatureDeclaration.TypeParameterList();
            var typeNode = isJS ? null : signatureDeclaration.Type();
            var factory = changeTracker.NodeFactory;
            if (typeParameters != null && typeParameters.Nodes.Length > 0)
            {
                var nodes = Slice<Node>.Make(0, typeParameters.Nodes.Length);
                foreach (var tp in typeParameters.Nodes)
                {
                    if (tp == null)
                        continue;
                    if (Ast.IsTypeParameterDeclaration(tp))
                    {
                        var parameter = tp.AsTypeParameterDeclaration();
                        var constraint = parameter.Constraint;
                        if (constraint != null)
                            constraint = importTypeNode(constraint, symbols);
                        var defaultType = parameter.DefaultType;
                        if (defaultType != null)
                            defaultType = importTypeNode(defaultType, symbols);
                        nodes = nodes.Append(factory.UpdateTypeParameterDeclaration(parameter, parameter.Modifiers(), parameter.Name(), constraint, parameter.Expression, defaultType));
                    }
                    else
                        nodes = nodes.Append(tp);
                }

                typeParameters = factory.NewNodeList(nodes);
            }

            if (parameters != null)
            {
                var nodes = Slice<Node>.Make(0, parameters.Nodes.Length);
                foreach (var p in parameters.Nodes)
                {
                    if (p == null)
                        continue;
                    var parameter = p.AsParameterDeclaration();
                    var parameterType = parameter.Type;
                    if (parameterType != null)
                        parameterType = importTypeNode(parameterType, symbols);
                    nodes = nodes.Append(factory.UpdateParameterDeclaration(parameter, parameter.Modifiers(), parameter.DotDotDotToken, parameter.Name(), isJS ? null : parameter.QuestionToken, parameterType, parameter.Initializer));
                }

                parameters = factory.NewNodeList(nodes);
            }

            if (typeNode != null)
                typeNode = importTypeNode(typeNode, symbols);
            var question = optional ? factory.NewToken(Kind.QuestionToken) : null;
            switch (kind)
            {
                case Kind.FunctionExpression:
                {
                    var fn = signatureDeclaration.AsFunctionExpression();
                    return factory.UpdateFunctionExpression(fn, modifiers, fn.AsteriskToken, name != null && Ast.IsIdentifier(name) ? name : null, typeParameters, parameters, typeNode, fn.FullSignature, body ?? fn.Body);
                }

                case Kind.ArrowFunction:
                {
                    var fn = signatureDeclaration.AsArrowFunction();
                    return factory.UpdateArrowFunction(fn, modifiers, typeParameters, parameters, typeNode, fn.FullSignature, fn.EqualsGreaterThanToken, body ?? fn.Body);
                }

                case Kind.MethodDeclaration:
                {
                    var method = signatureDeclaration.AsMethodDeclaration();
                    var methodName = name == null ? factory.NewIdentifier("") : createPropertyName(factory, name, quotePreference);
                    return factory.UpdateMethodDeclaration(method, modifiers, method.AsteriskToken, methodName, question, typeParameters, parameters, typeNode, method.FullSignature, body);
                }

                case Kind.FunctionDeclaration:
                {
                    var fn = signatureDeclaration.AsFunctionDeclaration();
                    return factory.UpdateFunctionDeclaration(fn, modifiers, fn.AsteriskToken, name != null && Ast.IsIdentifier(name) ? name : null, typeParameters, parameters, typeNode, fn.FullSignature, body ?? fn.Body);
                }
            }

            return null;
        }

        private Node? createSignatureDeclarationFromSignatures(Slice<Signature> signatures, Node? name, bool optional, ModifierList? modifiers, QuotePreference quotePreference, Node? body, Node enclosingDeclaration)
        {
            if (signatures.Length == 0)
                return null;
            var(builder, symbols) = createNodeBuilder();
            var maxArgsSignature = signatures[0];
            var minArgumentCount = signatures[0].MinArgumentCount();
            var hasRestParameter = false;
            foreach (var signature in signatures)
            {
                minArgumentCount = Math.Min(minArgumentCount, signature.MinArgumentCount());
                if (signature.HasRestParameter())
                    hasRestParameter = true;
                if (signature.Parameters().Length >= maxArgsSignature.Parameters().Length && (!signature.HasRestParameter() || maxArgsSignature.HasRestParameter()))
                    maxArgsSignature = signature;
            }

            var maxNonRestArgs = maxArgsSignature.Parameters().Length - (maxArgsSignature.HasRestParameter() ? 1 : 0);
            var parameterNames = Slice<ByteStringValue>.Make(0, maxArgsSignature.Parameters().Length);
            foreach (var symbol in maxArgsSignature.Parameters())
                parameterNames = parameterNames.Append(symbol.NameValue);
            var factory = changeTracker.NodeFactory;
            var parameters = createDummyParameters(factory, maxNonRestArgs, parameterNames, default, minArgumentCount, Ast.IsInJSFile(enclosingDeclaration));
            if (hasRestParameter)
            {
                var restName = ByteStringValue.FromString("rest");
                if (maxNonRestArgs < parameterNames.Length && parameterNames[maxNonRestArgs].Length != 0)
                    restName = parameterNames[maxNonRestArgs];
                var question = maxNonRestArgs >= minArgumentCount ? factory.NewToken(Kind.QuestionToken) : null;
                parameters.Nodes = parameters.Nodes.Append(factory.NewParameterDeclaration(null, factory.NewToken(Kind.DotDotDotToken), factory.NewIdentifierWithTextValue(restName), question, factory.NewArrayTypeNode(factory.NewKeywordTypeNode(Kind.UnknownKeyword)), null));
            }

            var methodName = name == null ? factory.NewIdentifier("") : createPropertyName(factory, name, quotePreference);
            return factory.NewMethodDeclaration(modifiers, null, methodName, optional ? factory.NewToken(Kind.QuestionToken) : null, null, parameters, getReturnTypeFromSignatures(signatures, enclosingDeclaration, builder, symbols), null, createBody(body, quotePreference, false));
        }

        private Slice<Signature> getCallSignatures(Type type)
        {
            if (type.IsUnion())
            {
                return Core.FlatMap(type.Types(), typeChecker, static (constituent, checker) => checker.GetCallSignatures(constituent));
            }

            return typeChecker.GetCallSignatures(type);
        }

        private Node? createTypeNode(Type type, Node enclosingDeclaration, BuilderFlags flags, NodeBuilder builder, Dictionary<Node, Symbol> symbols) => importTypeNode(builder.TypeToTypeNode(type, enclosingDeclaration, flags, InternalFlags.None, null), symbols);
        private ModifierList? createModifiers(Symbol symbol, Node? declaration)
        {
            var flags = ModifierFlags.None;
            if (declaration != null)
            {
                var effective = Checker.GetDeclarationModifierFlagsFromSymbol(symbol);
                flags = effective & ModifierFlags.Static;
                if ((effective & ModifierFlags.Public) != 0)
                    flags |= ModifierFlags.Public;
                else if ((effective & ModifierFlags.Protected) != 0)
                    flags |= ModifierFlags.Protected;
                if (Ast.IsAutoAccessorPropertyDeclaration(declaration))
                    flags |= ModifierFlags.Accessor;
            }

            if (shouldAddOverrideKeyword(declaration))
                flags |= ModifierFlags.Override;
            return flags == ModifierFlags.None ? null : changeTracker.NodeFactory.NewModifierList(Ast.CreateModifiersFromModifierFlags(flags, changeTracker.NodeFactory.NewModifier));
        }

        private bool shouldAddOverrideKeyword(Node? declaration) => declaration != null && program.Options().NoImplicitOverride == Tristate.True && Ast.HasAbstractModifier(declaration);
        private Node? getReturnTypeFromSignatures(Slice<Signature> signatures, Node enclosingDeclaration, NodeBuilder builder, Dictionary<Node, Symbol> symbols)
        {
            if (signatures.Length == 0)
                return null;
            var returnTypes = Slice<Type>.Make(0, signatures.Length);
            foreach (var signature in signatures)
                returnTypes = returnTypes.Append(typeChecker.GetReturnTypeOfSignature(signature));
            var unionType = typeChecker.GetUnionType(returnTypes);
            return importTypeNode(builder.TypeToTypeNode(unionType, enclosingDeclaration, BuilderFlags.NoTruncation, InternalFlags.AllowUnresolvedNames, null), symbols);
        }

        private Node? importTypeNode(Node? typeNode, Dictionary<Node, Symbol> symbols)
        {
            if (typeNode == null || importAdder == null)
                return typeNode;
            var(importedTypeNode, imports) = AutoImportFunctions.TryGetAutoImportableReferenceFromTypeNode(typeNode, symbols);
            if (importedTypeNode != null)
            {
                foreach (var symbol in imports)
                {
                    var exportSymbol = getExportedSymbol(symbol);
                    if (exportSymbol != null)
                        importAdder.AddImportFromExportedSymbol(exportSymbol, true);
                }

                return importedTypeNode;
            }

            var seen = new HashSet<Symbol>(); // Original make(map[*ast.Symbol]bool), not a zero set.
            foreach (var symbol in symbols.Values)
            {
                if (symbol == null || !seen.Add(symbol))
                    continue;
                var exportSymbol = getExportedSymbol(symbol);
                if (exportSymbol != null)
                    importAdder.AddImportFromExportedSymbol(exportSymbol, true);
            }

            return typeNode;
        }

        private Symbol? getExportedSymbol(Symbol symbol)
        {
            symbol = typeChecker.GetExportSymbolOfSymbol(symbol);
            return symbol == null || symbol.Parent == null ? null : symbol;
        }

        internal Node? createIndexSignatureDeclarationFromType(Node classDeclaration, Type implementedType, Type keyType)
        {
            var info = typeChecker.GetIndexInfoOfType(implementedType, keyType);
            if (info == null)
                return null;
            var builder = Checker.NewNodeBuilder(typeChecker, changeTracker.EmitContext);
            return builder.IndexInfoToIndexSignatureDeclaration(info, classDeclaration, BuilderFlags.None, InternalFlags.None, null);
        }

        private Node? createBody(Node? body, QuotePreference quotePreference, bool signatureOnly)
        {
            if (signatureOnly)
                return null;
            body = changeTracker.NodeFactory.DeepCloneNode(body);
            return body ?? createStubbedMethodBody(quotePreference);
        }

        private Node createStubbedMethodBody(QuotePreference quotePreference)
        {
            var flags = quotePreference == QuotePreference.Single ? TokenFlags.SingleQuote : TokenFlags.None;
            var factory = changeTracker.NodeFactory;
            return factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([factory.NewThrowStatement(factory.NewNewExpression(factory.NewIdentifier("Error"), null, factory.NewNodeList(Slice<Node>.FromArray([factory.NewStringLiteralWithTextValue(ByteStringValue.FromString(Diagnostics.Method_not_implemented.Localize(locale)), flags)]))))])), true);
        }
    }

    private static NodeList createDummyParameters(NodeFactory factory, int argCount, Slice<ByteStringValue> names, Slice<Node> types, int minArgumentCount, bool inJS)
    {
        var parameters = Slice<Node>.Make(0, argCount);
        var nameCounts = new Dictionary<ByteStringKey, int>();
        for (var i = 0; i < argCount; i++)
        {
            var name = i < names.Length && names[i].Length != 0 ? names[i] : ByteStringValue.FromString("arg" + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var key = ByteStringKey.FromValue(name);
            nameCounts.TryGetValue(key, out var count);
            nameCounts[key] = count + 1;
            if (count > 0)
                name = ByteStringValue.Concat(name, ByteStringValue.FromString(count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var question = i >= minArgumentCount ? factory.NewToken(Kind.QuestionToken) : null;
            var type = inJS ? null : i < types.Length && types[i] != null ? types[i] : factory.NewKeywordTypeNode(Kind.UnknownKeyword);
            parameters = parameters.Append(factory.NewParameterDeclaration(null, null, factory.NewIdentifierWithTextValue(name), question, type, null));
        }

        return factory.NewNodeList(parameters);
    }

    private static Node? createDeclarationName(NodeFactory factory, Checker checker, Symbol? symbol, Node? declaration)
    {
        if (symbol != null && (symbol.CheckFlags & CheckFlags.Mapped) != 0)
        {
            var nameType = checker.GetNameTypeOfSymbol(symbol);
            if (nameType != null && Checker.IsTypeUsableAsPropertyName(nameType))
                return factory.NewIdentifierWithTextValue(Checker.getPropertyNameValueFromType(nameType));
        }

        if (declaration?.Name() != null)
            return declaration.Name().Clone(factory);
        return symbol == null ? null : factory.NewIdentifierWithTextValue(symbol.NameValue);
    }

    private static Node createPropertyName(NodeFactory factory, Node node, QuotePreference quotePreference)
    {
        if (Ast.IsIdentifier(node) && node.TextValue().EqualsValue(ByteStringValue.FromString("constructor")))
        {
            var flags = quotePreference == QuotePreference.Single ? TokenFlags.SingleQuote : TokenFlags.None;
            return factory.NewComputedPropertyName(factory.NewStringLiteralWithTextValue(node.TextValue(), flags));
        }

        return factory.DeepCloneNode(node);
    }
}
