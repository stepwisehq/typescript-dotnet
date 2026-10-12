// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/semantictokens.go, fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Ignored complete-owner assembly; never used as a partial runtime provider.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using TypeScript.LanguageServices.Lsconv;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;
using ProtocolRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private static readonly Slice<SemanticTokenType> tokenTypes = Slice<SemanticTokenType>.FromArray([SemanticTokenType.Namespace, SemanticTokenType.Class, SemanticTokenType.Enum, SemanticTokenType.Interface, SemanticTokenType.Struct, SemanticTokenType.TypeParameter, SemanticTokenType.Type, SemanticTokenType.Parameter, SemanticTokenType.Variable, SemanticTokenType.Property, SemanticTokenType.EnumMember, SemanticTokenType.Decorator, SemanticTokenType.Event, SemanticTokenType.Function, SemanticTokenType.Method, SemanticTokenType.Macro, SemanticTokenType.Label, SemanticTokenType.Comment, SemanticTokenType.String, SemanticTokenType.Keyword, SemanticTokenType.Number, SemanticTokenType.Regexp, SemanticTokenType.Operator]);
    private static readonly Slice<SemanticTokenModifier> tokenModifiers = Slice<SemanticTokenModifier>.FromArray([SemanticTokenModifier.Declaration, SemanticTokenModifier.Definition, SemanticTokenModifier.Readonly, SemanticTokenModifier.Static, SemanticTokenModifier.Deprecated, SemanticTokenModifier.Abstract, SemanticTokenModifier.Async, SemanticTokenModifier.Modification, SemanticTokenModifier.Documentation, SemanticTokenModifier.DefaultLibrary, new(ByteStringValue.FromString("local"))]);
    private enum TokenType
    {
        Namespace,
        Class,
        Enum,
        Interface,
        Struct,
        TypeParameter,
        Type,
        Parameter,
        Variable,
        Property,
        EnumMember,
        Decorator,
        Event,
        Function,
        Method,
        Macro,
        Label,
        Comment,
        String,
        Keyword,
        Number,
        Regexp,
        Operator
    }

    [Flags]
    private enum TokenModifier
    {
        Declaration = 1,
        Definition = 2,
        Readonly = 4,
        Static = 8,
        Deprecated = 16,
        Abstract = 32,
        Async = 64,
        Modification = 128,
        Documentation = 256,
        DefaultLibrary = 512,
        Local = 1024
    }

    private readonly record struct SemanticToken(Node Node, SourceFile? File, TokenType Type, TokenModifier Modifier);
    internal static SemanticTokensLegend SemanticTokensLegend(ResolvedSemanticTokensClientCapabilities capabilities)
    {
        var types = Slice<ByteStringValue>.Make(0, tokenTypes.Length);
        foreach (var type in tokenTypes)
            if (GoSlices.ContainsFunc(capabilities.TokenTypes, value => value.EqualsValue(type.Value)))
                types = types.Append(type.Value);
        var modifiers = Slice<ByteStringValue>.Make(0, tokenModifiers.Length);
        foreach (var modifier in tokenModifiers)
            if (GoSlices.ContainsFunc(capabilities.TokenModifiers, value => value.EqualsValue(modifier.Value)))
                modifiers = modifiers.Append(modifier.Value);
        return new()
        {
            TokenTypes = types,
            TokenModifiers = modifiers
        };
    }

    internal (SemanticTokensOrNull Result, Exception? Error) ProvideSemanticTokens(ProtocolRequestContext context, DocumentUri uri)
    {
        var(program, file) = getProgramAndFile(uri);
        var supplemental = file.SupplementalSourceFiles();
        var files = Slice<SourceFile>.Make(1, 1 + supplemental.Length);
        files[0] = file;
        files = files.Append(supplemental);
        var tokens = Slice<SemanticToken>.Make(0, files.Length);
        foreach (var projection in files)
        {
            var(checker, done) = program.GetTypeCheckerForFile(context, projection);
            try
            {
                foreach (var token in collectSemanticTokens(context, checker, projection, program))
                    tokens = tokens.Append(token with { File = projection });
            }
            finally
            {
                done();
            }
        }

        sortSemanticTokens(tokens, converters);
        if (tokens.Length == 0)
            return (new(), null);
        return (new() { SemanticTokens = new() { Data = encodeSemanticTokens(context, tokens, converters) } }, null);
    }

    internal (SemanticTokensOrNull Result, Exception? Error) ProvideSemanticTokensRange(ProtocolRequestContext context, DocumentUri uri, ProtocolRange range)
    {
        var(program, file) = getProgramAndFile(uri);
        var mapped = converters.FromLSPRangeIntersectingForSourceFile(file, range, Feature.SemanticTokens);
        var tokens = Slice<SemanticToken>.Make(0, mapped.Length);
        HashSet<SemanticToken>? seen = null;
        foreach (var item in mapped)
        {
            var projection = item.Script;
            var(checker, done) = program.GetTypeCheckerForFile(context, projection);
            try
            {
                foreach (var token in collectSemanticTokensInRange(context, checker, projection, program, item.Span.Pos(), item.Span.End()))
                {
                    var retained = token with
                    {
                        File = projection
                    };
                    if ((seen ??= new()).Add(retained))
                        tokens = tokens.Append(retained);
                }
            }
            finally
            {
                done();
            }
        }

        sortSemanticTokens(tokens, converters);
        if (tokens.Length == 0)
            return (new(), null);
        return (new() { SemanticTokens = new() { Data = encodeSemanticTokens(context, tokens, converters) } }, null);
    }

    private static void sortSemanticTokens(Slice<SemanticToken> tokens, Converters converters)
    {
        GoSliceSort.SortFunc(tokens.AsSpan(), (a, b) =>
        {
            var(aRange, _) = semanticTokenLSPRange(a, converters);
            var(bRange, _) = semanticTokenLSPRange(b, converters);
            int comparison = aRange.Start.Line.CompareTo(bRange.Start.Line);
            if (comparison != 0)
                return comparison;
            comparison = aRange.Start.Character.CompareTo(bRange.Start.Character);
            if (comparison != 0)
                return comparison;
            comparison = ByteStringKey.FromValue(a.File!.PathKeyValue()).CompareTo(ByteStringKey.FromValue(b.File!.PathKeyValue()));
            if (comparison != 0)
                return comparison;
            return a.Node.Pos().CompareTo(b.Node.Pos());
        });
    }

    private static (ProtocolRange Range, Fidelity Fidelity) semanticTokenLSPRange(SemanticToken token, Converters converters)
    {
        int start = ScannerApi.GetTokenPosOfNode(token.Node, token.File!, false);
        return converters.ToLSPRangeForFeature(token.File!, new TextRange(start, token.Node.End()), Feature.SemanticTokens);
    }

    private static Slice<uint> encodeSemanticTokens(ProtocolRequestContext context, Slice<SemanticToken> tokens, Converters converters)
    {
        var typeMapping = new Dictionary<TokenType, uint>();
        var modifierMapping = new Dictionary<SemanticTokenModifier, uint>();
        var capabilities = context.ClientCapabilities.TextDocument.SemanticTokens;
        uint clientIndex = 0;
        for (int i = 0; i < tokenTypes.Length; i++)
            if (GoSlices.ContainsFunc(capabilities.TokenTypes, value => value.EqualsValue(tokenTypes[i].Value)))
                typeMapping[(TokenType)i] = clientIndex++;
        uint clientBit = 0;
        foreach (var modifier in tokenModifiers)
            if (GoSlices.ContainsFunc(capabilities.TokenModifiers, value => value.EqualsValue(modifier.Value)))
                modifierMapping[modifier] = clientBit++;
        var encoded = Slice<uint>.Make(0, tokens.Length * 5);
        uint previousLine = 0, previousCharacter = 0;
        foreach (var token in tokens)
        {
            if (!typeMapping.TryGetValue(token.Type, out uint clientType))
                continue;
            uint mask = 0;
            for (int i = 0; i < tokenModifiers.Length; i++)
                if (((int)token.Modifier & (1 << i)) != 0 && modifierMapping.TryGetValue(tokenModifiers[i], out uint bit))
                    mask |= 1u << (int)bit;
            var(range, fidelity) = semanticTokenLSPRange(token, converters);
            if (!fidelity.IsExact())
                continue;
            var start = range.Start;
            var end = range.End;
            uint length;
            if (start.Line == end.Line)
                length = unchecked(end.Character - start.Character);
            else
                throw new InvalidOperationException(FormattableString.Invariant($"semantic tokens: token spans multiple lines: start=({start.Line},{start.Character}) end=({end.Line},{end.Character}) for token at offset {token.Node.Pos()}"));
            uint line = start.Line, character = start.Character;
            if (encoded.Length > 0 && line == previousLine && character == previousCharacter)
                continue;
            if (encoded.Length > 0 && (line < previousLine || line == previousLine && character < previousCharacter))
                throw new InvalidOperationException(FormattableString.Invariant($"semantic tokens: positions must be strictly increasing: prev=({previousLine},{previousCharacter}) current=({line},{character}) for token at offset {token.Node.Pos()}"));
            uint deltaLine = unchecked(line - previousLine);
            uint deltaCharacter = deltaLine == 0 ? unchecked(character - previousCharacter) : character;
            encoded = encoded.Append(deltaLine).Append(deltaCharacter).Append(length).Append(clientType).Append(mask);
            previousLine = line;
            previousCharacter = character;
        }

        return encoded;
    }

    private static TokenType tokenFromDeclarationMapping(Kind kind) => kind switch
    {
        Kind.VariableDeclaration => TokenType.Variable,
        Kind.Parameter => TokenType.Parameter,
        Kind.PropertyDeclaration => TokenType.Property,
        Kind.ModuleDeclaration => TokenType.Namespace,
        Kind.EnumDeclaration => TokenType.Enum,
        Kind.EnumMember => TokenType.EnumMember,
        Kind.ClassDeclaration or Kind.ClassExpression => TokenType.Class,
        Kind.MethodDeclaration => TokenType.Method,
        Kind.FunctionDeclaration or Kind.FunctionExpression => TokenType.Function,
        Kind.MethodSignature => TokenType.Method,
        Kind.GetAccessor or Kind.SetAccessor => TokenType.Property,
        Kind.PropertySignature => TokenType.Property,
        Kind.InterfaceDeclaration => TokenType.Interface,
        Kind.TypeAliasDeclaration => TokenType.Type,
        Kind.TypeParameter => TokenType.TypeParameter,
        Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment => TokenType.Property,
        _ => (TokenType)(-1)};
    private static (TokenType Type, bool Found) classifySymbol(Symbol symbol, SemanticMeaning meaning)
    {
        var flags = symbol.Flags;
        if ((flags & SymbolFlags.Class) != 0)
            return (TokenType.Class, true);
        if ((flags & SymbolFlags.Enum) != 0)
            return (TokenType.Enum, true);
        if ((flags & SymbolFlags.TypeAlias) != 0)
            return (TokenType.Type, true);
        if ((flags & SymbolFlags.Interface) != 0 && (meaning & SemanticMeaning.Type) != 0)
            return (TokenType.Interface, true);
        if ((flags & SymbolFlags.TypeParameter) != 0)
            return (TokenType.TypeParameter, true);
        var declaration = symbol.ValueDeclaration;
        if (declaration is null && symbol.Declarations.Length > 0)
            declaration = symbol.Declarations[0];
        if (declaration is not null)
        {
            if (Ast.IsBindingElement(declaration))
                declaration = getDeclarationForBindingElement(declaration);
            var type = tokenFromDeclarationMapping(declaration.Kind);
            if ((int)type >= 0)
                return (type, true);
        }

        return (0, false);
    }

    private static Node getDeclarationForBindingElement(Node element)
    {
        while (true)
        {
            var parent = element.Parent;
            if (parent is not null && Ast.IsBindingPattern(parent))
            {
                var grandparent = parent.Parent;
                if (grandparent is not null && Ast.IsBindingElement(grandparent))
                {
                    element = grandparent;
                    continue;
                }

                return parent.Parent;
            }

            return element;
        }
    }

    private static bool isInImportClause(Node node)
    {
        var parent = node.Parent;
        return parent is not null && (Ast.IsImportClause(parent) || Ast.IsImportSpecifier(parent) || Ast.IsNamespaceImport(parent));
    }

    private static bool isExpressionInCallExpression(Node node)
    {
        while (Ast.IsRightSideOfQualifiedNameOrPropertyAccess(node))
            node = node.Parent;
        var parent = node.Parent;
        return parent is not null && Ast.IsCallExpression(parent) && parent.Expression() == node;
    }

    private static bool isInfinityOrNaNString(ByteStringValue text) => text.EqualsBytes("Infinity"u8) || text.EqualsBytes("NaN"u8);
    private static bool isLocalDeclaration(Node declaration, SourceFile file)
    {
        if (Ast.IsBindingElement(declaration))
            declaration = getDeclarationForBindingElement(declaration);
        if (Ast.IsVariableDeclaration(declaration))
        {
            var parent = declaration.Parent;
            if (parent is not null && Ast.IsCatchClause(parent))
                return Ast.GetSourceFileOfNode(declaration) == file;
            if (parent is not null && Ast.IsVariableDeclarationList(parent))
            {
                var grandparent = parent.Parent;
                if (grandparent is not null)
                    return (!Ast.IsSourceFile(grandparent.Parent) || Ast.IsCatchClause(grandparent)) && Ast.GetSourceFileOfNode(declaration) == file;
            }
        }
        else if (Ast.IsFunctionDeclaration(declaration))
        {
            var parent = declaration.Parent;
            return parent is not null && !Ast.IsSourceFile(parent) && Ast.GetSourceFileOfNode(declaration) == file;
        }

        return false;
    }

    private Slice<SemanticToken> collectSemanticTokens(ProtocolRequestContext context, Checker checker, SourceFile file, TypeScript.Compiler.Program program) => collectSemanticTokensInRange(context, checker, file, program, file.Pos(), file.End());
    private Slice<SemanticToken> collectSemanticTokensInRange(ProtocolRequestContext context, Checker checker, SourceFile file, TypeScript.Compiler.Program program, int start, int end)
    {
        var tokens = Slice<SemanticToken>.Make(0, 0);
        bool inJSXElement = false;
        Visitor visit = null!;
        visit = node =>
        {
            if (context.Cancellation.IsCancellationRequested)
                return false;
            if (node is null || (node.Flags & NodeFlags.Reparsed) != 0)
                return false;
            if (node.Pos() >= end || node.End() <= start)
                return false;
            bool previousJSX = inJSXElement;
            if (Ast.IsJsxElement(node) || Ast.IsJsxSelfClosingElement(node))
                inJSXElement = true;
            else if (Ast.IsJsxExpression(node))
                inJSXElement = false;
            if ((Ast.IsIdentifier(node) || Ast.IsPrivateIdentifier(node)) && node.TextValue().Length != 0 && !inJSXElement && !isInImportClause(node) && !isInfinityOrNaNString(node.TextValue()))
            {
                var symbol = checker.GetSymbolAtLocation(node);
                if (symbol is not null)
                {
                    if ((symbol.Flags & SymbolFlags.Alias) != 0)
                        symbol = checker.GetAliasedSymbol(symbol);
                    var(type, found) = classifySymbol(symbol, getMeaningFromLocation(node));
                    if (found)
                    {
                        TokenModifier modifier = 0;
                        var parent = node.Parent;
                        if (parent is not null)
                        {
                            bool declaration = Ast.IsBindingElement(parent) || tokenFromDeclarationMapping(parent.Kind) == type;
                            if (declaration && parent.Name() == node)
                                modifier |= TokenModifier.Declaration;
                        }

                        if (type == TokenType.Parameter && Ast.IsRightSideOfQualifiedNameOrPropertyAccess(node))
                            type = TokenType.Property;
                        type = reclassifyByType(checker, node, type);
                        var declarationNode = symbol.ValueDeclaration;
                        if (declarationNode is not null)
                        {
                            var modifiers = Ast.GetCombinedModifierFlags(declarationNode);
                            var flags = Ast.GetCombinedNodeFlags(declarationNode);
                            if ((modifiers & ModifierFlags.Static) != 0)
                                modifier |= TokenModifier.Static;
                            if ((modifiers & ModifierFlags.Async) != 0)
                                modifier |= TokenModifier.Async;
                            if (type != TokenType.Class && type != TokenType.Interface && ((modifiers & ModifierFlags.Readonly) != 0 || (flags & NodeFlags.Const) != 0 || (symbol.Flags & SymbolFlags.EnumMember) != 0))
                                modifier |= TokenModifier.Readonly;
                            if ((type == TokenType.Variable || type == TokenType.Function) && isLocalDeclaration(declarationNode, file))
                                modifier |= TokenModifier.Local;
                            var declarationFile = Ast.GetSourceFileOfNode(declarationNode);
                            if (declarationFile is not null && program.IsSourceFileDefaultLibrary(PathKey.FromValue(declarationFile.PathKeyValue())))
                                modifier |= TokenModifier.DefaultLibrary;
                        }
                        else if (!symbol.Declarations.IsNil)
                        {
                            foreach (var declaration in symbol.Declarations)
                            {
                                var declarationFile = Ast.GetSourceFileOfNode(declaration);
                                if (declarationFile is not null && program.IsSourceFileDefaultLibrary(PathKey.FromValue(declarationFile.PathKeyValue())))
                                {
                                    modifier |= TokenModifier.DefaultLibrary;
                                    break;
                                }
                            }
                        }

                        tokens = tokens.Append(new SemanticToken(node, null, type, modifier));
                    }
                }
            }

            node.ForEachChild(visit);
            inJSXElement = previousJSX;
            return false;
        };
        visit(file);
        return context.Cancellation.IsCancellationRequested ? default : tokens;
    }

    private static TokenType reclassifyByType(Checker checker, Node node, TokenType type)
    {
        if (type is TokenType.Variable or TokenType.Property or TokenType.Parameter)
        {
            var valueType = checker.GetTypeAtLocation(node);
            if (valueType is not null)
            {
                bool Test(Func<TypeScript.Checking.Type, bool> condition)
                {
                    if (condition(valueType))
                        return true;
                    return (valueType.Flags() & TypeFlags.Union) != 0 && GoSlices.ContainsFunc(valueType.AsUnionType().Types(), condition);
                }

                if (type != TokenType.Parameter && Test(t => checker.GetSignaturesOfTypeSlice(t, SignatureKind.Construct).Length > 0))
                    return TokenType.Class;
                bool hasCall = Test(t => checker.GetSignaturesOfTypeSlice(t, SignatureKind.Call).Length > 0);
                if (hasCall)
                {
                    bool noProperties = !Test(t =>
                    {
                        var objectType = t.AsObjectType();
                        return objectType is not null && objectType.Properties().Length > 0;
                    });
                    if (noProperties || isExpressionInCallExpression(node))
                        return type == TokenType.Property ? TokenType.Method : TokenType.Function;
                }
            }
        }

        return type;
    }
}
