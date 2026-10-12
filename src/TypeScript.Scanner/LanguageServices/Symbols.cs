// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/symbols.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Whole document/workspace symbol owner; editor assertion acceptance remains pending.
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private readonly record struct DocumentSymbolKey(ByteStringKey Name, SymbolKind Kind, uint StartLine, uint StartCharacter, uint EndLine, uint EndCharacter);
    internal (SymbolInformationsOrDocumentSymbolsOrNull Result, Exception? Error) ProvideDocumentSymbols(ProtocolRequestContext context, DocumentUri uri)
    {
        var(_, file) = getProgramAndFile(uri);
        var projections = Slice<SourceFile>.FromArray([file]).Append(file.SupplementalSourceFiles());
        Slice<DocumentSymbol?> symbols = default;
        HashSet<DocumentSymbolKey>? seen = null;
        foreach (var projection in projections)
            foreach (var symbol in getDocumentSymbolsForChildren(context, projection, projection))
            {
                var key = new DocumentSymbolKey(ByteStringKey.FromValue(symbol!.Name), symbol.Kind, symbol.Range.Start.Line, symbol.Range.Start.Character, symbol.Range.End.Line, symbol.Range.End.Character);
                if ((seen ??= new()).Add(key))
                    symbols = symbols.Append(symbol);
            }

        if (context.ClientCapabilities.TextDocument.DocumentSymbol.HierarchicalDocumentSymbolSupport)
            return (new() { DocumentSymbols = symbols }, null);
        var infos = flattenDocumentSymbols(symbols, uri);
        var pointers = Slice<SymbolInformation?>.Make(infos.Length, infos.Length);
        for (int i = 0; i < infos.Length; i++)
            pointers[i] = infos[i];
        return (new() { SymbolInformations = pointers }, null);
    }

    private Slice<SymbolInformation> getDocumentSymbolInformations(ProtocolRequestContext context, SourceFile file, DocumentUri uri) => flattenDocumentSymbols(getDocumentSymbolsForChildren(context, file, file), uri);
    private static Slice<SymbolInformation> flattenDocumentSymbols(Slice<DocumentSymbol?> symbols, DocumentUri uri)
    {
        Slice<SymbolInformation> result = default;
        void Flatten(Slice<DocumentSymbol?> current, ByteStringValue? container)
        {
            foreach (var item in current)
            {
                var symbol = item!;
                result = result.Append(new SymbolInformation { Name = symbol.Name, Kind = symbol.Kind, Location = new() { Uri = uri, Range = symbol.Range }, ContainerName = container, Tags = symbol.Tags, Deprecated = symbol.Deprecated });
                if (symbol.Children is { Length: > 0 } children)
                    Flatten(children, symbol.Name);
            }
        }

        Flatten(symbols, null);
        return result;
    }

    private static Slice<DocumentSymbol?> mergeExpandos(Slice<DocumentSymbol?> symbols)
    {
        var merged = Slice<DocumentSymbol?>.Make(0, symbols.Length);
        Dictionary<ByteStringKey, Slice<int>>? targets = null;
        Dictionary<ByteStringKey, int>? namespaces = null;
        for (int i = 0; i < symbols.Length; i++)
        {
            var symbol = symbols[i]!;
            if (isAnonymousName(symbol.Name))
                continue;
            var name = ByteStringKey.FromValue(symbol.Name);
            if (symbol.Kind is SymbolKind.Class or SymbolKind.Function or SymbolKind.Variable)
            {
                targets ??= new();
                targets[name] = targets.GetValueOrDefault(name).Append(i);
            }

            if (symbol.Kind == SymbolKind.Namespace)
            {
                namespaces ??= new();
                if (!namespaces.ContainsKey(name))
                    namespaces[name] = i;
            }
        }

        for (int i = 0; i < symbols.Length; i++)
        {
            var symbol = symbols[i]!;
            if (symbol.Children is { } children)
                symbol.Children = mergeExpandos(children);
            if (isAnonymousName(symbol.Name))
                continue;
            var name = ByteStringKey.FromValue(symbol.Name);
            if (symbol.Kind == SymbolKind.Property)
            {
                var same = targets?.GetValueOrDefault(name) ?? default;
                for (int j = same.Length - 1; j >= 0; j--)
                {
                    mergeChildren(symbols[same[j]]!, symbol);
                    symbols[i] = null;
                }
            }

            if (symbol.Kind == SymbolKind.Namespace && namespaces is not null && namespaces.TryGetValue(name, out int target) && target != i)
            {
                mergeChildren(symbols[target]!, symbol);
                symbols[i] = null;
            }
        }

        foreach (var symbol in symbols)
            if (symbol is not null)
                merged = merged.Append(symbol);
        return merged;
    }

    // All production symbols receive a nonnull Children pointer in newDocumentSymbol.
    // No producer aliases that pointer: nullable Slice headers preserve the actual
    // response path without an additional shared cell for the unreachable nil-target arm.
    private static void mergeChildren(DocumentSymbol target, DocumentSymbol source)
    {
        if (source.Children is not { } children)
            return;
        if (target.Children is null)
            target.Children = source.Children;
        else
        {
            var merged = mergeExpandos(target.Children.Value.Append(children));
            GoSliceSort.SortFunc(merged.AsSpan(), static (a, b) => ProtocolUtilities.CompareRanges(a!.Range, b!.Range));
            target.Children = merged;
        }
    }

    private static bool isAnonymousName(ByteStringValue name) => name.EqualsBytes("<function>"u8) || name.EqualsBytes("<class>"u8) || name.EqualsBytes("export="u8) || name.EqualsBytes("default"u8) || name.EqualsBytes("constructor"u8) || name.EqualsBytes("()"u8) || name.EqualsBytes("new()"u8) || name.EqualsBytes("[]"u8) || name.EndsWith(ByteStringValue.FromString(") callback"));
    private const int maxSymbolLabelLength = 150;
    private static ByteStringValue getTextOfName(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
            case Kind.NumericLiteral:
                return node.TextValue();
            case Kind.StringLiteral:
                return ByteStringValue.Concat(ByteStringValue.FromString("\""), TypeScript.DiagnosticPrinting.PrinterUtilities.EscapeStringValue(node.TextValue(), QuoteChar.DoubleQuote), ByteStringValue.FromString("\""));
            case Kind.NoSubstitutionTemplateLiteral:
                return ByteStringValue.Concat(ByteStringValue.FromString("`"), TypeScript.DiagnosticPrinting.PrinterUtilities.EscapeStringValue(node.TextValue(), QuoteChar.Backtick), ByteStringValue.FromString("`"));
            case Kind.ComputedPropertyName:
                if (Ast.IsStringOrNumericLiteralLike(node.Expression()))
                    return getTextOfName(node.Expression());
                break;
        }

        return ScannerApi.GetTextOfNodeValue(node);
    }

    private static ByteStringValue getUnnamedNodeLabel(Node node)
    {
        var parent = Ast.WalkUpParenthesizedExpressions(node.Parent);
        if (parent is not null && Ast.IsExportAssignment(parent))
            return ByteStringValue.FromString(parent.AsExportAssignment().IsExportEquals ? "export=" : "default");
        switch (node.Kind)
        {
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
                if ((node.ModifierFlags() & ModifierFlags.Default) != 0)
                    return ByteStringValue.FromString("default");
                if (Ast.IsCallExpression(node.Parent))
                {
                    var name = getCallExpressionName(node.Parent.Expression());
                    if (name.Length != 0)
                    {
                        name = cleanCallbackText(name);
                        if (name.Length > maxSymbolLabelLength)
                            return ByteStringValue.Concat(name, ByteStringValue.FromString(" callback"));
                        var args = cleanCallbackText(getCallExpressionLiteralArgs(node.Parent));
                        return ByteStringValue.Concat(name, ByteStringValue.FromString("("), args, ByteStringValue.FromString(") callback"));
                    }
                }

                return ByteStringValue.FromString("<function>");
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
                return ByteStringValue.FromString((node.ModifierFlags() & ModifierFlags.Default) != 0 ? "default" : "<class>");
            case Kind.Constructor:
                return ByteStringValue.FromString("constructor");
            case Kind.CallSignature:
                return ByteStringValue.FromString("()");
            case Kind.ConstructSignature:
                return ByteStringValue.FromString("new()");
            case Kind.IndexSignature:
                return ByteStringValue.FromString("[]");
        }

        return ByteStringValue.FromString("");
    }

    private static ByteStringValue getCallExpressionName(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
                return node.TextValue();
            case Kind.PropertyAccessExpression:
                var left = getCallExpressionName(node.Expression());
                var right = getCallExpressionName(node.Name());
                return left.Length != 0 ? ByteStringValue.Concat(left, ByteStringValue.FromString("."), right) : right;
        }

        return ByteStringValue.FromString("");
    }

    private static ByteStringValue getCallExpressionLiteralArgs(Node call)
    {
        Slice<ByteStringValue> parts = default;
        foreach (var argument in call.Arguments())
            if (Ast.IsStringLiteralLike(argument) || Ast.IsTemplateExpression(argument))
                parts = parts.Append(ScannerApi.GetTextOfNodeValue(argument));
        return GoStrings.Join(parts, ByteStringValue.FromString(", "));
    }

    private static ByteStringValue cleanCallbackText(ByteStringValue text)
    {
        var truncated = StringUtil.TruncateByRunesValue(text, maxSymbolLabelLength);
        if (truncated.Length < text.Length)
            text = ByteStringValue.Concat(truncated, ByteStringValue.FromString("..."));
        return GoStrings.Map(static rune => StringUtil.IsLineBreak(rune) ? -1 : rune, text);
    }

    private static Node getInteriorModule(Node node)
    {
        while (node.Body()is { } body && Ast.IsModuleDeclaration(body))
            node = body;
        return node;
    }

    private static ByteStringValue getModuleName(Node node)
    {
        var result = node.Name().TextValue();
        while (node.Body()is { } body && Ast.IsModuleDeclaration(body))
        {
            node = body;
            result = ByteStringValue.Concat(result, ByteStringValue.FromString("."), node.Name().TextValue());
        }

        return result;
    }

    private DocumentSymbol? newDocumentSymbol(Node node, Node? name, Slice<DocumentSymbol?> children)
    {
        var result = new DocumentSymbol();
        var file = Ast.GetSourceFileOfNode(node);
        int nodeStart = ScannerApi.SkipTrivia(file.TextView(), node.Pos());
        name ??= Ast.GetNameOfDeclaration(node);
        ByteStringValue text;
        int nameStart, nameEnd;
        if (Ast.IsModuleDeclaration(node) && !Ast.IsAmbientModule(node))
        {
            text = getModuleName(node);
            nameStart = ScannerApi.SkipTrivia(file.TextView(), name!.Pos());
            nameEnd = getInteriorModule(node).Name().End();
        }
        else if (Ast.IsAnyExportAssignment(node) && node.AsExportAssignment().IsExportEquals)
        {
            text = ByteStringValue.FromString("export=");
            if (!Ast.NodeIsMissing(name))
            {
                nameStart = ScannerApi.SkipTrivia(file.TextView(), name!.Pos());
                nameEnd = name.End();
            }
            else
            {
                nameStart = nodeStart;
                nameEnd = node.End();
            }
        }
        else if (name is not null)
        {
            text = getTextOfName(name);
            nameStart = Math.Max(ScannerApi.SkipTrivia(file.TextView(), name.Pos()), nodeStart);
            nameEnd = Math.Max(name.End(), nodeStart);
        }
        else
        {
            text = getUnnamedNodeLabel(node);
            nameStart = nodeStart;
            nameEnd = nodeStart;
        }

        if (text.Length == 0)
            return null;
        var truncated = StringUtil.TruncateByRunesValue(text, maxSymbolLabelLength);
        if (truncated.Length < text.Length)
            text = ByteStringValue.Concat(truncated, ByteStringValue.FromString("..."));
        result.Name = text;
        result.Kind = getSymbolKindFromNode(node);
        var(selection, selectionFidelity) = converters.ToLSPRangeForFeature(file, new TextRange(nameStart, nameEnd), Feature.DocumentSymbols);
        if (!selectionFidelity.IsSingleSegment())
            return null;
        var(range, fidelity) = converters.ToLSPRangeForFeature(file, new TextRange(nodeStart, node.End()), Feature.DocumentSymbols);
        if (fidelity.IsNone())
            range = selection;
        result.Range = range;
        result.SelectionRange = selection;
        if (children.IsNil)
            children = Slice<DocumentSymbol?>.Make(0, 0);
        result.Children = children;
        return result;
    }

    private static bool isPrototypeExpando(Node target)
    {
        if (Ast.IsAccessExpression(target))
        {
            var name = Ast.GetElementOrPropertyAccessName(target);
            return name is not null && name.TextValue().EqualsBytes("prototype"u8);
        }

        return false;
    }

    private static SymbolKind getSymbolKindFromNode(Node node)
    {
        switch (node.Kind)
        {
            case Kind.SourceFile:
                return Ast.IsExternalModule(node.AsSourceFile()) ? SymbolKind.Module : SymbolKind.File;
            case Kind.ModuleDeclaration:
                return SymbolKind.Namespace;
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
                return SymbolKind.Class;
            case Kind.InterfaceDeclaration:
                return SymbolKind.Interface;
            case Kind.TypeAliasDeclaration:
            case Kind.JSDocTypedefTag:
            case Kind.JSDocCallbackTag:
                return SymbolKind.Class;
            case Kind.EnumDeclaration:
                return SymbolKind.Enum;
            case Kind.VariableDeclaration:
                return SymbolKind.Variable;
            case Kind.ArrowFunction:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
                return SymbolKind.Function;
            case Kind.GetAccessor:
            case Kind.SetAccessor:
                return SymbolKind.Property;
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
                return SymbolKind.Method;
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            case Kind.SpreadAssignment:
            case Kind.IndexSignature:
                return SymbolKind.Property;
            case Kind.CallSignature:
                return SymbolKind.Method;
            case Kind.ConstructSignature:
            case Kind.Constructor:
            case Kind.ClassStaticBlockDeclaration:
                return SymbolKind.Constructor;
            case Kind.TypeParameter:
                return SymbolKind.TypeParameter;
            case Kind.EnumMember:
                return SymbolKind.EnumMember;
            case Kind.Parameter:
                return Ast.HasSyntacticModifier(node, ModifierFlags.ParameterPropertyModifier) ? SymbolKind.Property : SymbolKind.Variable;
            case Kind.BinaryExpression:
            case Kind.CallExpression:
                var assignment = Ast.GetAssignmentDeclarationKind(node);
                if (assignment is JSDeclarationKind.ThisProperty or JSDeclarationKind.Property or JSDeclarationKind.ObjectDefinePropertyValue)
                    return SymbolKind.Property;
                break;
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.NumericLiteral:
                return SymbolKind.Property;
        }

        return SymbolKind.Variable;
    }

    private Slice<DocumentSymbol?> getDocumentSymbolsForChildren(ProtocolRequestContext context, Node node, SourceFile file)
    {
        Slice<DocumentSymbol?> symbols = default;
        HashSet<ByteStringKey>? expandoTargets = null;
        void AddSymbol(Node item, Node? name, Slice<DocumentSymbol?> children)
        {
            if ((item.Flags & NodeFlags.Reparsed) == 0)
            {
                var symbol = newDocumentSymbol(item, name, children);
                if (symbol is not null)
                    symbols = symbols.Append(symbol);
            }
        }

        Visitor visit = null!;
        Slice<DocumentSymbol?> Children(Node? item)
        {
            Slice<DocumentSymbol?> result = default;
            if (item is not null)
            {
                var savedTargets = expandoTargets;
                expandoTargets = null;
                var savedSymbols = symbols;
                symbols = default;
                item.ForEachChild(visit);
                result = symbols;
                symbols = savedSymbols;
                expandoTargets = savedTargets;
            }

            return result;
        }

        Action StartNode(Node? item, Node? name)
        {
            if (item is null)
                return static () =>
                {
                };
            var savedTargets = expandoTargets;
            expandoTargets = null;
            var savedSymbols = symbols;
            symbols = default;
            return () =>
            {
                var result = symbols;
                symbols = savedSymbols;
                expandoTargets = savedTargets;
                AddSymbol(item, name, result);
            };
        }

        Slice<DocumentSymbol?> SymbolsForNode(Node? item)
        {
            Slice<DocumentSymbol?> result = default;
            if (item is not null)
            {
                var saved = symbols;
                symbols = default;
                visit(item);
                result = symbols;
                symbols = saved;
            }

            return result;
        }

        visit = item =>
        {
            if (context.Cancellation.IsCancellationRequested)
                return true;
            if ((item.Flags & NodeFlags.Reparsed) == 0)
                foreach (var doc in item.JSDoc(file))
                    if (doc.AsJSDoc().Tags is { } tags)
                        foreach (var tag in tags.Nodes)
                            if (Ast.IsJSDocTypedefTag(tag) || Ast.IsJSDocCallbackTag(tag))
                                AddSymbol(tag, null, default);
            if (item.Parent.Kind == Kind.SourceFile && Ast.IsImportOrImportEqualsDeclaration(item))
                return false;
            switch (item.Kind)
            {
                case Kind.ClassDeclaration:
                case Kind.ClassExpression:
                case Kind.InterfaceDeclaration:
                case Kind.EnumDeclaration:
                    var className = Ast.GetDeclarationNameValue(item);
                    if (Ast.IsClassLike(item) && className.Length != 0)
                        (expandoTargets ??= new()).Add(ByteStringKey.FromValue(className));
                    AddSymbol(item, null, Children(item));
                    break;
                case Kind.ModuleDeclaration:
                    AddSymbol(item, null, Children(getInteriorModule(item)));
                    break;
                case Kind.Constructor:
                    AddSymbol(item, null, Children(item.Body()));
                    foreach (var parameter in item.Parameters())
                        if (Ast.IsParameterPropertyDeclaration(parameter, item))
                            AddSymbol(parameter, null, default);
                    break;
                case Kind.FunctionDeclaration:
                case Kind.FunctionExpression:
                case Kind.ArrowFunction:
                case Kind.MethodDeclaration:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                    var declarationName = Ast.GetDeclarationNameValue(item);
                    if (declarationName.Length != 0)
                        (expandoTargets ??= new()).Add(ByteStringKey.FromValue(declarationName));
                    AddSymbol(item, null, Children(item.Body()));
                    break;
                case Kind.VariableDeclaration:
                case Kind.BindingElement:
                case Kind.PropertyAssignment:
                case Kind.PropertyDeclaration:
                    var name = item.Name();
                    if (name is not null)
                    {
                        if (Ast.IsBindingPattern(name))
                            visit(name);
                        else
                            AddSymbol(item, null, Children(item.Initializer()));
                    }

                    break;
                case Kind.SpreadAssignment:
                    AddSymbol(item, item.Expression(), default);
                    break;
                case Kind.MethodSignature:
                case Kind.PropertySignature:
                case Kind.CallSignature:
                case Kind.ConstructSignature:
                case Kind.IndexSignature:
                case Kind.EnumMember:
                case Kind.ShorthandPropertyAssignment:
                case Kind.TypeAliasDeclaration:
                case Kind.ImportEqualsDeclaration:
                case Kind.ExportSpecifier:
                    AddSymbol(item, null, default);
                    break;
                case Kind.ImportClause:
                    if (item.Name()is { } importName)
                        AddSymbol(importName, importName, default);
                    if (item.AsImportClause().NamedBindings is { } bindings)
                    {
                        if (bindings.Kind == Kind.NamespaceImport)
                            AddSymbol(bindings, null, default);
                        else
                            foreach (var element in bindings.Elements())
                                AddSymbol(element, null, default);
                    }

                    break;
                case Kind.BinaryExpression:
                case Kind.CallExpression:
                    var assignment = Ast.GetAssignmentDeclarationKind(item);
                    switch (assignment)
                    {
                        case JSDeclarationKind.None:
                        case JSDeclarationKind.ThisProperty:
                        case JSDeclarationKind.ModuleExports:
                        case JSDeclarationKind.ExportsProperty:
                        case JSDeclarationKind.ObjectDefinePropertyExports:
                            item.ForEachChild(visit);
                            break;
                        case JSDeclarationKind.Property:
                        case JSDeclarationKind.ObjectDefinePropertyValue:
                            Node target, targetFunction, definition, propertyName;
                            if (Ast.IsBinaryExpression(item))
                            {
                                var binary = item.AsBinaryExpression();
                                target = binary.Left;
                                targetFunction = target.Expression();
                                definition = binary.Right;
                                propertyName = Ast.IsPropertyAccessExpression(target) ? target.Name() : target.AsElementAccessExpression().ArgumentExpression;
                            }
                            else
                            {
                                var args = item.Arguments();
                                targetFunction = args[0];
                                target = args[1];
                                propertyName = target;
                                definition = args[2];
                            }

                            if (isPrototypeExpando(targetFunction))
                            {
                                targetFunction = targetFunction.Expression();
                                if (Ast.IsIdentifier(targetFunction))
                                    (expandoTargets ??= new()).Add(ByteStringKey.FromValue(targetFunction.TextValue()));
                            }

                            if (Ast.IsIdentifier(targetFunction) && expandoTargets?.Contains(ByteStringKey.FromValue(targetFunction.TextValue())) == true)
                            {
                                var end = StartNode(item, targetFunction);
                                AddSymbol(target, propertyName, SymbolsForNode(definition));
                                end();
                            }
                            else
                                item.ForEachChild(visit);
                            break;
                    }

                    break;
                case Kind.ExportAssignment:
                    if (item.AsExportAssignment().IsExportEquals)
                        AddSymbol(item, null, SymbolsForNode(item.Expression()));
                    else
                        item.ForEachChild(visit);
                    break;
                default:
                    item.ForEachChild(visit);
                    break;
            }

            return false;
        };
        node.ForEachChild(visit);
        return mergeExpandos(symbols);
    }

    private readonly record struct DeclarationInfo(ByteStringValue Name, Node Declaration, int MatchScore);
    internal static (SymbolInformationsOrWorkspaceSymbolsOrNull Result, Exception? Error) ProvideWorkspaceSymbols(ProtocolRequestContext context, Slice<TypeScript.Compiler.Program> programs, TypeScript.LanguageServices.Lsconv.Converters converters, TypeScript.LanguageServices.Utilities.UserPreferences preferences, ByteStringValue query)
    {
        bool excludeLibrary = preferences.ExcludeLibrarySymbolsInNavTo == Tristate.True;
        // Original map is eagerly allocated; later programs replace equal path keys.
        var files = new Dictionary<PathKey, SourceFile>();
        foreach (var program in programs)
            foreach (var file in program.SourceFiles())
                if ((program.HasTSFile() || !file.IsDeclarationFile) && !shouldExcludeFile(file, program, excludeLibrary))
                    files[PathKey.FromValue(file.PathKeyValue())] = file;
        Slice<DeclarationInfo> infos = default;
        foreach (var file in files.Values)
        {
            if (context.Cancellation.IsCancellationRequested)
                return (new(), null);
            foreach (var entry in file.GetDeclarationMap())
            {
                int score = getMatchScore(entry.Key.Value, query);
                if (score >= 0)
                    foreach (var declaration in entry.Value)
                        infos = infos.Append(new DeclarationInfo(entry.Key.Value, declaration, score));
            }
        }

        GoSliceSort.SortFunc(infos.AsSpan(), compareDeclarationInfos);
        int count = Math.Min(infos.Length, 256);
        var symbols = Slice<SymbolInformation?>.Make(0, count);
        foreach (var info in infos.Slice2(0, count))
        {
            var node = info.Declaration;
            var file = Ast.GetSourceFileOfNode(node);
            var container = getContainerNode(node);
            ByteStringValue? containerName = container is null ? null : strPtrTo(Ast.GetDeclarationNameValue(container));
            var nameNode = Ast.GetNameOfDeclaration(node)!;
            int start = TypeScript.AstNavigation.AstNav.GetStartOfNode(nameNode, file, false);
            var(location, fidelity) = converters.ToLSPLocationForFeature(file, new TextRange(start, nameNode.End()), Feature.DocumentSymbols);
            if (!fidelity.IsSingleSegment())
                continue;
            symbols = symbols.Append(new SymbolInformation { Name = info.Name, Kind = getSymbolKindFromNode(node), Location = location, ContainerName = containerName });
        }

        return (new() { SymbolInformations = symbols }, null);
    }

    private static bool shouldExcludeFile(SourceFile file, TypeScript.Compiler.Program program, bool excludeLibrary) => excludeLibrary && (isInsideNodeModules(RootedFilePath.FromValue(file.FileNameGoValue())) || program.IsLibFile(file));
    private static bool isInsideNodeModules(RootedFilePath file) => file.ContainsLowercaseDirectorySequence("/node_modules/");
    private static int getMatchScore(ByteStringValue value, ByteStringValue pattern)
    {
        int score = 0;
        for (int offset = 0; offset < pattern.Length;)
        {
            var(p, sizeP) = decodeSymbolRune(pattern.Slice(offset));
            offset += sizeP;
            bool exact = GoUnicode.IsUpper(p);
            while (true)
            {
                var(c, size) = decodeSymbolRune(value);
                if (size == 0)
                    return -1;
                value = value.Slice(size);
                if (exact && c == p || !exact && UnicodeCase.ToLower(c) == UnicodeCase.ToLower(p))
                    break;
                score++;
            }
        }

        return score;
    }

    private static int compareDeclarationInfos(DeclarationInfo first, DeclarationInfo second)
    {
        if (first.MatchScore != second.MatchScore)
            return first.MatchScore - second.MatchScore;
        int comparison = StringUtil.CompareStringsCaseInsensitive(first.Name, second.Name);
        if (comparison != 0)
            return comparison;
        comparison = ByteStringKey.FromValue(first.Name).CompareTo(ByteStringKey.FromValue(second.Name));
        if (comparison != 0)
            return comparison;
        var a = Ast.GetSourceFileOfNode(first.Declaration);
        var b = Ast.GetSourceFileOfNode(second.Declaration);
        if (a != b)
            return ByteStringKey.FromValue(a.PathKeyValue()).CompareTo(ByteStringKey.FromValue(b.PathKeyValue()));
        return first.Declaration.Pos() - second.Declaration.Pos();
    }

    private static (int Rune, int Size) decodeSymbolRune(ByteStringValue text) => text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan) : Utf8.DecodeRuneInString(text.CharSpan);
}
