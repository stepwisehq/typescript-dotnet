// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/definition.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private readonly record struct DefinitionLocationKey(DocumentUri Uri, TypeScript.Lsp.Protocol.Range Range);
    private readonly record struct FileRange(SourceFile File, TextRange Range);
    // definition.go:19. The preference selects the genuine source-definition owner.
    internal (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) ProvideDefinition(ProtocolRequestContext context, DocumentUri uri, Position position) => UserPreferences().PreferGoToSourceDefinition
        ? ProvideSourceDefinition(context, uri, position) : provideDefinitionWorker(context, uri, position);
    private (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) provideDefinitionWorker(ProtocolRequestContext context, DocumentUri uri, Position position)
    {
        bool links = context.ClientCapabilities.TextDocument.Definition.LinkSupport;
        var (program, file) = getProgramAndFile(uri);
        var positions = converters.FromLSPPositionForSourceFile(file, position, TypeScript.SpanMap.Feature.Definition);
        var results = Slice<LocationOrLocationsOrDefinitionLinksOrNull>.Make(0, positions.Length);
        foreach (var mapped in positions)
            if (mapped.Fidelity.IsSingleSegment())
                results = results.Append(provideDefinitionAtPosition(context, program, mapped.Script, mapped.Position, links));
        return (combineDefinitionResponses(results, links), null);
    }

    // definition.go:49. Alias/call resolution and special keyword targets retain source order.
    private LocationOrLocationsOrDefinitionLinksOrNull provideDefinitionAtPosition(ProtocolRequestContext context, TypeScript.Compiler.Program program, SourceFile file, int position, bool links)
    {
        var node = TypeScript.AstNavigation.AstNav.GetTouchingPropertyName(file, position);
        var reference = getReferenceAtPosition(file, position, program);
        if (node.Kind == Kind.SourceFile)
            return new();
        var (origin, _) = createLspRangeFromNode(node, file);
        if (reference?.file != null)
            return createDefinitionLocations(origin, links, Slice<Node>.Make(0), reference, TypeScript.SpanMap.Feature.Definition);
        var (checker, done) = program.GetTypeCheckerForFile(context, file);
        try
        {
            if (node.Kind == Kind.OverrideKeyword && getSymbolForOverriddenMember(checker, node) is { } overridden)
                return createDefinitionLocations(origin, links, overridden.Declarations, null, TypeScript.SpanMap.Feature.Definition);
            if (Ast.IsJumpStatementTarget(node) && getTargetLabel(node.Parent, node.TextValue()) is { } label)
                return createDefinitionLocations(origin, links, Slice<Node>.FromArray([label]), null, TypeScript.SpanMap.Feature.Definition);
            if (node.Kind == Kind.CaseKeyword || node.Kind == Kind.DefaultKeyword && Ast.IsDefaultClause(node.Parent))
            {
                var statement = Ast.FindAncestor(node.Parent, Ast.IsSwitchStatement);
                if (statement != null)
                {
                    var targetFile = Ast.GetSourceFileOfNode(statement);
                    return createLocationFromFileAndRange(targetFile, TypeScript.Scanner.Scanner.GetRangeOfTokenAtPosition(targetFile, statement.Pos()), TypeScript.SpanMap.Feature.Definition);
                }
            }

            if (node.Kind is Kind.ReturnKeyword or Kind.YieldKeyword or Kind.AwaitKeyword)
            {
                var function = Ast.FindAncestor(node, Ast.IsFunctionLikeDeclaration);
                if (function != null)
                    return createDefinitionLocations(origin, links, Slice<Node>.FromArray([function]), null, TypeScript.SpanMap.Feature.Definition);
            }

            var declarations = getDeclarationsFromLocation(checker, node);
            var called = tryGetSignatureDeclaration(checker, node);
            if (called != null && !(Ast.IsJsxOpeningLikeElement(node.Parent) && isJsxConstructorLike(called)))
            {
                var symbol = checker.GetSymbolAtLocation(getDeclarationNameForKeyword(node));
                if (symbol != null && Core.Some(checker.GetRootSymbols(symbol), called, static (root, declaration) => symbolMatchesSignature(root, declaration)))
                {
                    if (!Ast.IsConstructorDeclaration(called))
                        declarations = default;
                    else
                        declarations = Core.Filter(declarations.Slice3(0, declarations.Length, declarations.Length), called, static (declaration, target) => declaration != target
                            && (Ast.IsClassDeclaration(declaration) || Ast.IsClassExpression(declaration)));
                }
                else
                    declarations = Core.Filter(declarations.Slice3(0, declarations.Length, declarations.Length), called, static (declaration, target) => declaration != target);
                declarations = declarations.Append(called);
            }

            return createDefinitionLocations(origin, links, declarations, reference, TypeScript.SpanMap.Feature.Definition);
        }
        finally
        {
            done();
        }
    }

    internal (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) ProvideTypeDefinition(ProtocolRequestContext context, DocumentUri uri, Position position)
    {
        bool links = context.ClientCapabilities.TextDocument.TypeDefinition.LinkSupport;
        var (program, file) = getProgramAndFile(uri);
        var positions = converters.FromLSPPositionForSourceFile(file, position, TypeScript.SpanMap.Feature.TypeDefinition);
        var results = Slice<LocationOrLocationsOrDefinitionLinksOrNull>.Make(0, positions.Length);
        foreach (var mapped in positions)
            if (mapped.Fidelity.IsSingleSegment())
                results = results.Append(provideTypeDefinitionAtPosition(context, program, mapped.Script, mapped.Position, links));
        return (combineDefinitionResponses(results, links), null);
    }

    private LocationOrLocationsOrDefinitionLinksOrNull provideTypeDefinitionAtPosition(ProtocolRequestContext context, TypeScript.Compiler.Program program, SourceFile file, int position, bool links)
    {
        var node = TypeScript.AstNavigation.AstNav.GetTouchingPropertyName(file, position);
        if (node.Kind == Kind.SourceFile)
            return new();
        var (origin, _) = createLspRangeFromNode(node, file);
        var (checker, done) = program.GetTypeCheckerForFile(context, file);
        try
        {
            node = getDeclarationNameForKeyword(node);
            var symbol = checker.GetSymbolAtLocation(node);
            if (symbol != null)
            {
                var type = getTypeOfSymbolAtLocation(checker, symbol, node);
                var declarations = getDeclarationsFromType(type);
                var argument = checker.GetFirstTypeArgumentFromKnownType(type);
                if (argument != null)
                    declarations = Core.Concatenate(getDeclarationsFromType(argument), declarations);
                if (declarations.Length != 0)
                    return createDefinitionLocations(origin, links, declarations, null, TypeScript.SpanMap.Feature.TypeDefinition);
                if ((symbol.Flags & SymbolFlags.Value) == 0 && (symbol.Flags & SymbolFlags.Type) != 0)
                    return createDefinitionLocations(origin, links, symbol.Declarations, null, TypeScript.SpanMap.Feature.TypeDefinition);
            }

            return new();
        }
        finally
        {
            done();
        }
    }

    // definition.go:162. Location is a CLR reference DTO; dedup keys retain Go's value equality.
    private static LocationOrLocationsOrDefinitionLinksOrNull combineDefinitionResponses(Slice<LocationOrLocationsOrDefinitionLinksOrNull> results, bool links)
    {
        Slice<Location> locations = default;
        Slice<LocationLink?> definitionLinks = default;
        HashSet<DefinitionLocationKey>? seen = null;
        bool add(Location location) => (seen ??= new()).Add(new(location.Uri, location.Range));
        foreach (var result in results)
        {
            if (result.DefinitionLinks is { } resultLinks)
                foreach (var link in resultLinks)
                {
                    var location = new Location
                    {
                        Uri = link!.TargetUri,
                        Range = link.TargetSelectionRange
                    };
                    if (!add(location))
                        continue;
                    definitionLinks = definitionLinks.Append(link);
                    locations = locations.Append(location);
                }

            if (result.Location is { } single && add(single))
            {
                locations = locations.Append(single);
                definitionLinks = definitionLinks.Append(new LocationLink { TargetUri = single.Uri, TargetRange = single.Range, TargetSelectionRange = single.Range });
            }

            if (result.Locations is { } resultLocations)
                foreach (var location in resultLocations)
                {
                    if (!add(location))
                        continue;
                    locations = locations.Append(location);
                    definitionLinks = definitionLinks.Append(new LocationLink { TargetUri = location.Uri, TargetRange = location.Range, TargetSelectionRange = location.Range });
                }
        }

        return links ? new LocationOrLocationsOrDefinitionLinksOrNull
        {
            DefinitionLinks = definitionLinks
        }

        : new LocationOrLocationsOrDefinitionLinksOrNull
        {
            Locations = locations
        };
    }

    // definition.go:203.
    private static Node getDeclarationNameForKeyword(Node node)
    {
        if (node.Kind >= Kind.FirstKeyword && node.Kind <= Kind.LastKeyword)
        {
            if (Ast.IsVariableDeclarationList(node.Parent))
            {
                var declarations = node.Parent.AsVariableDeclarationList().Declarations.Nodes;
                if (declarations.Length != 0 && declarations[0].Name() != null)
                    return declarations[0].Name();
            }
            else if (node.Parent.DeclarationData() != null && node.Parent.Name() != null && node.Pos() < node.Parent.Name().Pos())
                return node.Parent.Name();
        }

        return node;
    }

    // definition.go:213. Preserve declaration identity/range and content-map fidelity checks.
    private LocationOrLocationsOrDefinitionLinksOrNull createDefinitionLocations(TypeScript.Lsp.Protocol.Range origin, bool clientSupportsLink, Slice<Node> declarations, RefInfo? reference, TypeScript.SpanMap.Feature feature)
    {
        var locations = Slice<LocationLink?>.Make(0);
        HashSet<FileRange>? locationRanges = null;
        if (reference != null)
            locations = locations.Append(new LocationLink { OriginSelectionRange = origin, TargetUri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(reference.fileName) });
        foreach (var declaration in declarations)
        {
            var file = Ast.GetSourceFileOfNode(declaration);
            var name = Ast.GetNameOfDeclaration(declaration) ?? declaration;
            var nameRange = name.Kind == Kind.EmptyStatement ? new TextRange(name.Pos(), name.Pos()) : createRangeFromNode(name, file);
            if (!(locationRanges ??= new()).Add(new FileRange(file, nameRange)))
                continue;
            var contextNode = getContextNode(declaration) ?? declaration;
            var contextRange = toContextRange(nameRange, file, contextNode) ?? nameRange;
            if (!nameRange.ContainedBy(contextRange))
                contextRange = new TextRange(Math.Min(nameRange.Pos(), contextRange.Pos()), Math.Max(nameRange.End(), contextRange.End()));
            var (selection, selectionFidelity) = sourceFileRangeToLSPLocationForFeature(file, nameRange, feature);
            if (!selectionFidelity.IsSingleSegment())
                continue;
            var (target, contextFidelity) = sourceFileRangeToLSPLocation(file, contextRange);
            if (contextFidelity.IsNone() || target.Uri != selection.Uri || !lspRangeContains(target.Range, selection.Range))
                target = selection;
            locations = locations.Append(new LocationLink { OriginSelectionRange = origin, TargetSelectionRange = selection.Range, TargetUri = target.Uri, TargetRange = target.Range });
        }

        return clientSupportsLink ? new LocationOrLocationsOrDefinitionLinksOrNull
        {
            DefinitionLinks = locations
        }

        : createLocationsFromLinks(locations);
    }

    private static bool lspRangeContains(TypeScript.Lsp.Protocol.Range outer, TypeScript.Lsp.Protocol.Range inner) => ProtocolUtilities.ComparePositions(outer.Start, inner.Start) <= 0 && ProtocolUtilities.ComparePositions(inner.End, outer.End) <= 0;
    private static LocationOrLocationsOrDefinitionLinksOrNull createLocationsFromLinks(Slice<LocationLink?> links)
    {
        var locations = links.IsNil ? default : Slice<Location>.Make(links.Length);
        for (int i = 0; i < links.Length; i++)
            locations[i] = new Location
            {
                Uri = links[i]!.TargetUri,
                Range = links[i]!.TargetSelectionRange
            };
        return new LocationOrLocationsOrDefinitionLinksOrNull
        {
            Locations = locations
        };
    }

    private LocationOrLocationsOrDefinitionLinksOrNull createLocationFromFileAndRange(SourceFile file, TextRange range, TypeScript.SpanMap.Feature feature)
    {
        var (location, fidelity) = sourceFileRangeToLSPLocationForFeature(file, range, feature);
        // The source returns Location by value; do not mutate a retained CLR mapper DTO.
        var result = new Location
        {
            Uri = location.Uri,
            Range = fidelity.IsNone() ? default : location.Range
        };
        return new LocationOrLocationsOrDefinitionLinksOrNull
        {
            Location = result
        };
    }

    // definition.go:306. Preserve contextual property declarations and alias resolution.
    private static Slice<Node> getDeclarationsFromLocation(Checker checker, Node node)
    {
        if (Ast.IsIdentifier(node) && Ast.IsShorthandPropertyAssignment(node.Parent))
        {
            var shorthand = checker.GetResolvedSymbol(node);
            var declarations = shorthand != null ? shorthand.Declarations : default;
            return Core.Concatenate(declarations, getDeclarationsFromObjectLiteralElement(checker, node));
        }

        if (Ast.IsPropertyName(node) && Ast.IsBindingElement(node.Parent) && Ast.IsObjectBindingPattern(node.Parent.Parent))
        {
            var binding = node.Parent.AsBindingElement();
            if (binding.DotDotDotToken == null && node == (binding.PropertyName ?? node.Parent.Name()))
            {
                var (name, ok) = Ast.TryGetTextValueOfPropertyName(node);
                if (ok)
                {
                    var type = checker.GetTypeAtLocation(node.Parent.Parent);
                    var types = type.IsUnion() ? type.Types() : Slice<TypeScript.Checking.Type>.FromArray([type]);
                    Slice<Node> result = default;
                    foreach (var unionType in types)
                    {
                        var property = checker.GetPropertyOfType(unionType, name);
                        if (property != null)
                            result = result.Append(property.Declarations);
                    }

                    return result;
                }
            }
        }

        node = getDeclarationNameForKeyword(node);
        var symbol = checker.GetSymbolAtLocation(node);
        if (symbol != null)
        {
            if ((symbol.Flags & SymbolFlags.Class) != 0 && (symbol.Flags & (SymbolFlags.Function | SymbolFlags.Variable)) == 0 && node.Kind == Kind.ConstructorKeyword)
            {
                var constructor = symbol.Members?[Ast.InternalSymbolNameConstructor];
                if (constructor != null)
                    symbol = constructor;
            }

            if ((symbol.Flags & SymbolFlags.Alias) != 0)
            {
                var (resolved, ok) = checker.ResolveAlias(symbol);
                if (ok)
                    symbol = resolved!;
            }

            var contextual = getDeclarationsFromObjectLiteralElement(checker, node);
            if (contextual.Length != 0)
                return contextual;
            if (symbol.Declarations.Length != 0)
                return symbol.Declarations;
        }

        var indexes = checker.GetIndexSignaturesAtLocation(node);
        return indexes.Length != 0 ? indexes : default;
    }

    private static Slice<Node> getDeclarationsFromObjectLiteralElement(Checker checker, Node node)
    {
        var element = getContainingObjectLiteralElement(node);
        if (element == null)
            return default;
        var contextual = checker.GetContextualType(element.Parent, ContextFlags.None);
        if (contextual == null)
            return default;
        var properties = checker.GetPropertySymbolsFromContextualType(element, contextual, false);
        if (Core.Some(properties, node, static (property, name) => property.ValueDeclaration != null
            && Ast.IsObjectLiteralExpression(property.ValueDeclaration.Parent)
            && Ast.IsObjectLiteralElement(property.ValueDeclaration) && property.ValueDeclaration.Name() == name))
        {
            var withoutInference = checker.GetContextualType(element.Parent, ContextFlags.IgnoreNodeInferences);
            if (withoutInference != null)
            {
                var withoutProperties = checker.GetPropertySymbolsFromContextualType(element, withoutInference, false);
                if (withoutProperties.Length != 0)
                    properties = withoutProperties;
            }
        }

        Slice<Node> result = default;
        foreach (var property in properties)
            result = result.Append(property.Declarations);
        return result;
    }

    private static Node? getAncestorCallLikeExpression(Node node)
    {
        var target = Ast.FindAncestor(node, static candidate => !Ast.IsRightSideOfPropertyAccess(candidate));
        var call = target.Parent;
        return call != null && Ast.IsCallLikeExpression(call) && Ast.GetInvokedExpression(call) == target ? call : null;
    }

    private static Node? tryGetSignatureDeclaration(Checker checker, Node node)
    {
        var call = getAncestorCallLikeExpression(node);
        var signature = call != null ? checker.GetResolvedSignature(call) : null;
        if (signature != null && signature.Declaration() != null)
        {
            var declaration = signature.Declaration();
            if (Ast.IsFunctionLike(declaration) && !Ast.IsFunctionTypeNode(declaration))
                return declaration;
        }

        return null;
    }

    private static bool isJsxConstructorLike(Node node) => Ast.IsConstructorDeclaration(node) || Ast.IsConstructorTypeNode(node) || Ast.IsCallSignatureDeclaration(node) || Ast.IsConstructSignatureDeclaration(node);
    private static bool symbolMatchesSignature(Symbol? symbol, Node? declaration)
    {
        if (symbol == null || declaration == null)
            return false;
        var called = declaration.Symbol();
        if (symbol == called || called != null && symbol == called.Parent)
            return true;
        var parent = declaration.Parent;
        return parent != null && (Ast.IsAssignmentExpression(parent, false) || !Ast.IsCallLikeExpression(parent) && Ast.CanHaveSymbol(parent) && symbol == parent.Symbol());
    }

    private static Symbol? getSymbolForOverriddenMember(Checker checker, Node node)
    {
        var element = Ast.FindAncestor(node, Ast.IsClassElement);
        if (element == null || element.Name() == null)
            return null;
        var declaration = Ast.FindAncestor(element, Ast.IsClassLike);
        if (declaration == null)
            return null;
        var baseNode = Ast.GetClassExtendsHeritageElement(declaration);
        if (baseNode == null)
            return null;
        var expression = Ast.SkipParentheses(baseNode.Expression());
        var symbol = Ast.IsClassExpression(expression) ? expression.Symbol() : checker.GetSymbolAtLocation(expression);
        if (symbol == null)
            return null;
        var name = Ast.TryGetTextValueOfPropertyName(element.Name()).Item1;
        return checker.GetPropertyOfType(Ast.HasStaticModifier(element) ? checker.GetTypeOfSymbol(symbol) : checker.GetDeclaredTypeOfSymbol(symbol), name);
    }

    private static TypeScript.Checking.Type getTypeOfSymbolAtLocation(Checker checker, Symbol symbol, Node node)
    {
        var type = checker.GetTypeOfSymbolAtLocation(symbol, node);
        if (type.Symbol() == symbol || type.Symbol() != null && symbol.ValueDeclaration != null && Ast.IsVariableDeclaration(symbol.ValueDeclaration) && symbol.ValueDeclaration.Initializer() == type.Symbol().ValueDeclaration)
        {
            var signatures = checker.GetCallSignatures(type);
            if (signatures.Length == 1)
                return checker.GetReturnTypeOfSignature(signatures[0]);
        }

        return type;
    }

    private static Slice<Node> getDeclarationsFromType(TypeScript.Checking.Type type)
    {
        Slice<Node> result = default;
        foreach (var distributed in type.Distributed())
            if (distributed.Symbol() != null)
                foreach (var declaration in distributed.Symbol().Declarations)
                {
                    bool exists = false;
                    foreach (var existing in result)
                        if (existing == declaration)
                        {
                            exists = true;
                            break;
                        }

                    if (!exists)
                        result = result.Append(declaration);
                }

        return result;
    }
}
