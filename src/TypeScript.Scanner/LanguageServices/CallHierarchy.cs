// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Whole internal/ls/callhierarchy.go counterpart; original editor-response acceptance pending.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using ScannerApi = TypeScript.Scanner.Scanner;
using AstNav = TypeScript.AstNavigation.AstNav;
using CompilerProgram = TypeScript.Compiler.Program;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private static bool isNamedExpression(Node? node) => node is not null && (Ast.IsFunctionExpression(node) || Ast.IsClassExpression(node)) && node.Name()is { } name && Ast.IsIdentifier(name);
    private static bool isVariableLike(Node? node) => node is not null && (Ast.IsPropertyDeclaration(node) || Ast.IsVariableDeclaration(node));
    private static bool isAssignedExpression(Node? node)
    {
        if (node is null || !(Ast.IsFunctionExpression(node) || Ast.IsArrowFunction(node) || Ast.IsClassExpression(node)) || node.Name()is not null)
            return false;
        var parent = node.Parent;
        if (!isVariableLike(parent) || !ReferenceEquals(parent.Initializer(), node) || !Ast.IsIdentifier(parent.Name()))
            return false;
        return (Ast.GetCombinedNodeFlags(parent) & NodeFlags.Const) != 0 || Ast.IsPropertyDeclaration(parent);
    }

    private static bool isPossibleCallHierarchyDeclaration(Node? node) => node is not null && (Ast.IsSourceFile(node) || Ast.IsModuleDeclaration(node) || Ast.IsFunctionDeclaration(node) || Ast.IsFunctionExpression(node) || Ast.IsClassDeclaration(node) || Ast.IsClassExpression(node) || Ast.IsClassStaticBlockDeclaration(node) || Ast.IsMethodDeclaration(node) || Ast.IsMethodSignatureDeclaration(node) || Ast.IsGetAccessorDeclaration(node) || Ast.IsSetAccessorDeclaration(node));
    private static bool isValidCallHierarchyDeclaration(Node? node)
    {
        if (node is null)
            return false;
        if (Ast.IsSourceFile(node))
            return true;
        if (Ast.IsModuleDeclaration(node))
            return Ast.IsIdentifier(node.Name());
        return Ast.IsFunctionDeclaration(node) || Ast.IsClassDeclaration(node) || Ast.IsClassStaticBlockDeclaration(node) || Ast.IsMethodDeclaration(node) || Ast.IsMethodSignatureDeclaration(node) || Ast.IsGetAccessorDeclaration(node) || Ast.IsSetAccessorDeclaration(node) || isNamedExpression(node) || isAssignedExpression(node);
    }

    private static Node? getCallHierarchyDeclarationReferenceNode(Node? node)
    {
        if (node is null)
            return null;
        if (Ast.IsSourceFile(node))
            return node;
        if (node.Name()is { } name)
            return name;
        if (isAssignedExpression(node))
            return node.Parent.Name();
        foreach (var modifier in node.ModifierNodes())
            if (modifier.Kind == Kind.DefaultKeyword)
                return modifier;
        return null;
    }

    private static Symbol? getSymbolOfCallHierarchyDeclaration(Checker checker, Node node)
    {
        if (Ast.IsClassStaticBlockDeclaration(node))
            return null;
        var location = getCallHierarchyDeclarationReferenceNode(node);
        return location is null ? null : checker.GetSymbolAtLocation(location);
    }

    private static TextRange moveRangePastModifiers(Node node)
    {
        var modifiers = node.ModifierNodes();
        return modifiers.Length > 0 ? new(modifiers[modifiers.Length - 1].End(), node.End()) : new(node.Pos(), node.End());
    }

    private static Node? findImplementation(Checker checker, Node? node)
    {
        if (node is null)
            return null;
        if (!Ast.IsFunctionLikeDeclaration(node) || node.Body()is not null)
            return node;
        if (Ast.IsConstructorDeclaration(node))
            return Ast.GetFirstConstructorWithBody(node.Parent);
        if (Ast.IsFunctionDeclaration(node) || Ast.IsMethodDeclaration(node))
        {
            var symbol = getSymbolOfCallHierarchyDeclaration(checker, node);
            if (symbol?.ValueDeclaration is { } declaration && Ast.IsFunctionLikeDeclaration(declaration) && declaration.Body()is not null)
                return declaration;
            return null;
        }

        return node;
    }

    private readonly record struct CallHierarchyDeclarationKey(ByteStringKey File, int Position);
    private static Slice<Node> findAllInitialDeclarations(Checker checker, Node node)
    {
        if (Ast.IsClassStaticBlockDeclaration(node))
            return default;
        var symbol = getSymbolOfCallHierarchyDeclaration(checker, node);
        if (symbol is null || symbol.Declarations.IsNil)
            return default;
        var indices = Slice<int>.Make(symbol.Declarations.Length, symbol.Declarations.Length);
        var keys = Slice<CallHierarchyDeclarationKey>.Make(symbol.Declarations.Length, symbol.Declarations.Length);
        for (int i = 0; i < indices.Length; i++)
            indices[i] = i;
        for (int i = 0; i < keys.Length; i++)
        {
            var declaration = symbol.Declarations[i];
            keys[i] = new(ByteStringKey.FromValue(Ast.GetSourceFileOfNode(declaration).FileNameGoValue()), declaration.Pos());
        }

        GoSliceSort.SortFunc(indices.AsSpan(), (a, b) => keys[a].File.Equals(keys[b].File) ? keys[a].Position - keys[b].Position : keys[a].File.CompareTo(keys[b].File));
        Slice<Node> declarations = default;
        Node? last = null;
        foreach (int i in indices)
        {
            var declaration = symbol.Declarations[i];
            if (isValidCallHierarchyDeclaration(declaration))
            {
                if (last is null || !ReferenceEquals(last.Parent, declaration.Parent) || last.End() != declaration.Pos())
                    declarations = declarations.Append(declaration);
                last = declaration;
            }
        }

        return declarations;
    }

    private static object findImplementationOrAllInitialDeclarations(Checker checker, Node node)
    {
        if (Ast.IsClassStaticBlockDeclaration(node))
            return node;
        if (Ast.IsFunctionLikeDeclaration(node))
        {
            if (findImplementation(checker, node)is { } implementation)
                return implementation;
            var declarations = findAllInitialDeclarations(checker, node);
            if (!declarations.IsNil)
                return declarations;
            return node;
        }

        var all = findAllInitialDeclarations(checker, node);
        return !all.IsNil ? all : node;
    }

    private static object? resolveCallHierarchyDeclaration(CompilerProgram program, Node? location)
    {
        var(checker, done) = program.GetTypeChecker(default(CancellationToken));
        try
        {
            bool followingSymbol = false;
            while (location is not null)
            {
                if (isValidCallHierarchyDeclaration(location))
                    return findImplementationOrAllInitialDeclarations(checker, location);
                if (isPossibleCallHierarchyDeclaration(location) && Ast.FindAncestor(location, isValidCallHierarchyDeclaration)is { } ancestor)
                    return findImplementationOrAllInitialDeclarations(checker, ancestor);
                if (Ast.IsDeclarationName(location))
                {
                    if (isValidCallHierarchyDeclaration(location.Parent))
                        return findImplementationOrAllInitialDeclarations(checker, location.Parent);
                    if (isPossibleCallHierarchyDeclaration(location.Parent) && Ast.FindAncestor(location.Parent, isValidCallHierarchyDeclaration)is { } parentAncestor)
                        return findImplementationOrAllInitialDeclarations(checker, parentAncestor);
                    if (isVariableLike(location.Parent) && location.Parent.Initializer()is { } initializer && isAssignedExpression(initializer))
                        return initializer;
                    return null;
                }

                if (Ast.IsConstructorDeclaration(location))
                    return isValidCallHierarchyDeclaration(location.Parent) ? location.Parent : null;
                if (location.Kind == Kind.StaticKeyword && Ast.IsClassStaticBlockDeclaration(location.Parent))
                {
                    location = location.Parent;
                    continue;
                }

                if (Ast.IsVariableDeclaration(location) && location.Initializer()is { } value && isAssignedExpression(value))
                    return value;
                if (!followingSymbol)
                {
                    var symbol = checker.GetSymbolAtLocation(location);
                    if (symbol is not null)
                    {
                        if ((symbol.Flags & SymbolFlags.Alias) != 0)
                            symbol = checker.GetAliasedSymbol(symbol);
                        if (symbol.ValueDeclaration is { } declaration)
                        {
                            followingSymbol = true;
                            location = declaration;
                            continue;
                        }
                    }
                }

                return null;
            }

            return null;
        }
        finally
        {
            done();
        }
    }

    private static (ByteStringValue Text, int Position, int End) getCallHierarchyItemName(CompilerProgram program, Node node)
    {
        if (Ast.IsSourceFile(node))
            return (node.AsSourceFile().FileNameGoValue(), 0, 0);
        if ((Ast.IsFunctionDeclaration(node) || Ast.IsClassDeclaration(node)) && node.Name()is null)
            foreach (var modifier in node.ModifierNodes())
                if (modifier.Kind == Kind.DefaultKeyword)
                {
                    var source = Ast.GetSourceFileOfNode(node);
                    return (ByteStringValue.FromString("default"), ScannerApi.SkipTrivia(source.TextView(), modifier.Pos()), modifier.End());
                }

        if (Ast.IsClassStaticBlockDeclaration(node))
        {
            var source = Ast.GetSourceFileOfNode(node);
            int position = ScannerApi.SkipTrivia(source.TextView(), moveRangePastModifiers(node).Pos());
            var(checker, done) = program.GetTypeCheckerForFile(default(CancellationToken), source);
            try
            {
                var symbol = checker.GetSymbolAtLocation(node.Parent);
                var prefix = symbol is null ? ByteStringValue.FromString("") : ByteStringValue.Concat(checker.SymbolToStringExValue(symbol, null!, SymbolFlags.All), ByteStringValue.FromString(" "));
                return (ByteStringValue.Concat(prefix, ByteStringValue.FromString("static {}")), position, position + 6);
            }
            finally
            {
                done();
            }
        }

        var name = isAssignedExpression(node) ? node.Parent.Name() : Ast.GetNameOfDeclaration(node);
        if (name is null || !Ast.NodeIsPresent(name))
        {
            var source = Ast.GetSourceFileOfNode(node);
            if (Ast.IsFunctionDeclaration(node) || Ast.IsFunctionExpression(node))
            {
                int start = ScannerApi.SkipTrivia(source.TextView(), moveRangePastModifiers(node).Pos());
                return (ByteStringValue.FromString("(anonymous)"), start, start + 8);
            }

            if (Ast.IsClassDeclaration(node) || Ast.IsClassExpression(node))
            {
                int start = ScannerApi.SkipTrivia(source.TextView(), moveRangePastModifiers(node).Pos());
                return (ByteStringValue.FromString("(anonymous)"), start, start + 5);
            }

            Debug.Assert(name is not null, "Expected call hierarchy item to have a name");
        }

        var text = getTextOfCallHierarchyName(program, node, name!, node);
        var file = Ast.GetSourceFileOfNode(node);
        return (text, ScannerApi.SkipTrivia(file.TextView(), name!.Pos()), name.End());
    }

    private static ByteStringValue getTextOfCallHierarchyName(CompilerProgram program, Node sourceNode, Node name, Node printNode)
    {
        if (Ast.IsIdentifier(name) || Ast.IsStringOrNumericLiteralLike(name))
            return name.TextValue();
        if (Ast.IsComputedPropertyName(name) && Ast.IsStringOrNumericLiteralLike(name.Expression()))
            return name.Expression().TextValue();
        var(checker, done) = program.GetTypeCheckerForFile(default(CancellationToken), Ast.GetSourceFileOfNode(sourceNode));
        try
        {
            var symbol = checker.GetSymbolAtLocation(name);
            if (symbol is not null)
            {
                var text = checker.SymbolToStringExValue(symbol, null!, SymbolFlags.All);
                if (text.Length != 0)
                    return text;
            }

            var file = Ast.GetSourceFileOfNode(sourceNode);
            var(writer, release) = TypeScript.DiagnosticPrinting.singleLineStringWriter.GetSingleLineStringWriter();
            try
            {
                var printer = TypeScript.DiagnosticPrinting.Printer.NewPrinter(new() { RemoveComments = true }, new());
                printer.Write(printNode, file, writer);
                return writer.TextValue();
            }
            finally
            {
                release();
            }
        }
        finally
        {
            done();
        }
    }

    private static ByteStringValue getCallHierarchyItemContainerName(CompilerProgram program, Node node)
    {
        if (isAssignedExpression(node))
        {
            var parent = node.Parent;
            if (Ast.IsPropertyDeclaration(parent) && Ast.IsClassLike(parent.Parent))
            {
                if (Ast.IsClassExpression(parent.Parent))
                {
                    if (Ast.GetAssignedName(parent.Parent)is { } assigned)
                        return getTextOfCallHierarchyName(program, node, assigned, assigned);
                }
                else if (parent.Parent.Name()is { } name)
                    return getTextOfCallHierarchyName(program, node, name, name);
            }

            if (parent.Parent.Parent?.Parent is { } grandparent && Ast.IsModuleBlock(grandparent))
            {
                var module = grandparent.Parent;
                if (Ast.IsModuleDeclaration(module) && module.Name()is { } name && Ast.IsIdentifier(name))
                    return name.TextValue();
            }

            return ByteStringValue.FromString("");
        }

        switch (node.Kind)
        {
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.MethodDeclaration:
                if (node.Parent.Kind == Kind.ObjectLiteralExpression && Ast.GetAssignedName(node.Parent)is { } assigned)
                    return getTextOfCallHierarchyName(program, node, assigned, assigned);
                if (Ast.GetNameOfDeclaration(node.Parent)is { } name)
                    return getTextOfCallHierarchyName(program, node, name, name);
                break;
            case Kind.FunctionDeclaration:
            case Kind.ClassDeclaration:
            case Kind.ModuleDeclaration:
                if (Ast.IsModuleBlock(node.Parent) && Ast.IsModuleDeclaration(node.Parent.Parent) && node.Parent.Parent.Name()is { } moduleName && Ast.IsIdentifier(moduleName))
                    return moduleName.TextValue();
                break;
        }

        return ByteStringValue.FromString("");
    }

    private sealed class CallSite(Node declaration, TextRange textRange, SourceFile sourceFile)
    {
        internal readonly Node Declaration = declaration;
        internal readonly TextRange TextRange = textRange;
        internal readonly SourceFile SourceFile = sourceFile;
    }

    private sealed class CallSiteCollector(CompilerProgram program)
    {
        internal Slice<CallSite> CallSites = Slice<CallSite>.Make(0, 0);
        private void RecordCallSite(Node node)
        {
            Node? target = node.Kind switch
            {
                Kind.TaggedTemplateExpression => node.AsTaggedTemplateExpression().Tag,
                Kind.JsxOpeningElement or Kind.JsxSelfClosingElement => node.TagName(),
                Kind.PropertyAccessExpression or Kind.ElementAccessExpression or Kind.ClassStaticBlockDeclaration => node,
                Kind.CallExpression or Kind.NewExpression or Kind.Decorator => node.Expression(),
                _ => null
            };
            if (target is null)
                return;
            var declaration = resolveCallHierarchyDeclaration(program, target);
            if (declaration is null)
                return;
            var file = Ast.GetSourceFileOfNode(target);
            var range = new TextRange(ScannerApi.SkipTrivia(file.TextView(), target.Pos()), target.End());
            if (declaration is Node single)
                CallSites = CallSites.Append(new CallSite(single, range, file));
            else if (declaration is Slice<Node> nodes)
                foreach (var item in nodes)
                    CallSites = CallSites.Append(new CallSite(item, range, file));
        }

        internal void Collect(Node? node)
        {
            if (node is null || (node.Flags & NodeFlags.Ambient) != 0)
                return;
            if (isValidCallHierarchyDeclaration(node))
            {
                if (Ast.IsClassLike(node))
                    foreach (var member in node.Members())
                        if (member.Name()is { } name && Ast.IsComputedPropertyName(name))
                            Collect(name.Expression());
                return;
            }

            switch (node.Kind)
            {
                case Kind.Identifier:
                case Kind.ImportEqualsDeclaration:
                case Kind.ImportDeclaration:
                case Kind.ExportDeclaration:
                case Kind.InterfaceDeclaration:
                case Kind.TypeAliasDeclaration:
                    return;
                case Kind.ClassStaticBlockDeclaration:
                    RecordCallSite(node);
                    return;
                case Kind.TypeAssertionExpression:
                case Kind.AsExpression:
                    Collect(node.Expression());
                    return;
                case Kind.VariableDeclaration:
                case Kind.Parameter:
                    Collect(node.Name());
                    Collect(node.Initializer());
                    return;
                case Kind.CallExpression:
                case Kind.NewExpression:
                    RecordCallSite(node);
                    Collect(node.Expression());
                    foreach (var argument in node.Arguments())
                        Collect(argument);
                    return;
                case Kind.TaggedTemplateExpression:
                    RecordCallSite(node);
                    Collect(node.AsTaggedTemplateExpression().Tag);
                    Collect(node.AsTaggedTemplateExpression().Template);
                    return;
                case Kind.JsxOpeningElement:
                case Kind.JsxSelfClosingElement:
                    RecordCallSite(node);
                    Collect(node.TagName());
                    Collect(node.Attributes());
                    return;
                case Kind.Decorator:
                    RecordCallSite(node);
                    Collect(node.Expression());
                    return;
                case Kind.PropertyAccessExpression:
                case Kind.ElementAccessExpression:
                    RecordCallSite(node);
                    node.ForEachChild(child =>
                    {
                        Collect(child);
                        return false;
                    });
                    return;
                case Kind.SatisfiesExpression:
                    Collect(node.Expression());
                    return;
            }

            if (Ast.IsPartOfTypeNode(node))
                return;
            node.ForEachChild(child =>
            {
                Collect(child);
                return false;
            });
        }
    }

    private static Slice<CallSite> collectCallSites(CompilerProgram program, Checker checker, Node node)
    {
        var collector = new CallSiteCollector(program);
        switch (node.Kind)
        {
            case Kind.SourceFile:
                foreach (var statement in node.Statements())
                    collector.Collect(statement);
                break;
            case Kind.ModuleDeclaration:
                if (!Ast.HasSyntacticModifier(node, ModifierFlags.Ambient) && node.Body()is { } body && Ast.IsModuleBlock(body))
                    foreach (var statement in body.Statements())
                        collector.Collect(statement);
                break;
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
                if (findImplementation(checker, node)is { } implementation)
                {
                    foreach (var parameter in implementation.Parameters())
                        collector.Collect(parameter);
                    collector.Collect(implementation.Body());
                }

                break;
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
                foreach (var modifier in node.ModifierNodes())
                    collector.Collect(modifier);
                if (Ast.GetClassExtendsHeritageElement(node)is { } heritage)
                    collector.Collect(heritage.Expression());
                foreach (var member in node.Members())
                {
                    if (Ast.CanHaveModifiers(member))
                        foreach (var modifier in member.ModifierNodes())
                            collector.Collect(modifier);
                    if (Ast.IsPropertyDeclaration(member))
                        collector.Collect(member.Initializer());
                    else if (Ast.IsConstructorDeclaration(member) && member.Body()is { } constructorBody)
                    {
                        foreach (var parameter in member.Parameters())
                            collector.Collect(parameter);
                        collector.Collect(constructorBody);
                    }
                    else if (Ast.IsClassStaticBlockDeclaration(member))
                        collector.Collect(member);
                }

                break;
            case Kind.ClassStaticBlockDeclaration:
                collector.Collect(node.AsClassStaticBlockDeclaration().Body);
                break;
            default:
                throw new InvalidOperationException("Debug failure. Illegal value: " + node.Kind.String());
        }

        return collector.CallSites;
    }

    private CallHierarchyItem? createCallHierarchyItem(CompilerProgram program, Node node)
    {
        var file = Ast.GetSourceFileOfNode(node);
        var(name, namePos, nameEnd) = getCallHierarchyItemName(program, node);
        var container = getCallHierarchyItemContainerName(program, node);
        var kind = getSymbolKindFromNode(node);
        int start = ScannerApi.SkipTriviaEx(file.TextView(), node.Pos(), new() { StopAtComments = true });
        var(span, spanFidelity) = converters.ToLSPRangeForFeature(file, new TextRange(start, node.End()), Feature.CallHierarchy);
        var(selection, selectionFidelity) = converters.ToLSPRangeForFeature(file, new TextRange(namePos, nameEnd), Feature.CallHierarchy);
        if (!selectionFidelity.IsSingleSegment())
            return null;
        if (spanFidelity.IsNone() || file.ContentMapper().Length != 0 && !lspRangeContains(span, selection))
            span = selection;
        var item = new CallHierarchyItem
        {
            Name = name,
            Kind = kind,
            Uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(file.OriginalFileName()),
            Range = span,
            SelectionRange = selection
        };
        if (container.Length != 0)
            item.Detail = container;
        return item;
    }

    private static CallSite? convertEntryToCallSite(ReferenceEntry entry)
    {
        if (entry.kind != EntryKind.Node)
            return null;
        var node = entry.node!;
        if (!Ast.IsCallOrNewExpressionTarget(node, true, true) && !Ast.IsTaggedTemplateTag(node, true, true) && !Ast.IsDecoratorTarget(node, true, true) && !Ast.IsJsxOpeningLikeElementTagName(node, true, true) && !Ast.IsRightSideOfPropertyAccess(node) && !Ast.IsArgumentExpressionOfElementAccess(node))
            return null;
        var file = Ast.GetSourceFileOfNode(node);
        var ancestor = Ast.FindAncestor(node, isValidCallHierarchyDeclaration) ?? file;
        return new CallSite(ancestor, new TextRange(ScannerApi.SkipTrivia(file.TextView(), node.Pos()), node.End()), file);
    }

    private CallHierarchyIncomingCall? convertCallSiteGroupToIncomingCall(CompilerProgram program, Slice<CallSite> entries)
    {
        var ranges = Slice<TypeScript.Lsp.Protocol.Range>.Make(0, entries.Length);
        foreach (var entry in entries)
        {
            var(range, fidelity) = converters.ToLSPRangeForFeature(entry.SourceFile, entry.TextRange, Feature.CallHierarchy);
            if (!fidelity.IsNone())
                ranges = ranges.Append(range);
        }

        var from = createCallHierarchyItem(program, entries[0].Declaration);
        if (from is null || ranges.Length == 0)
            return null;
        GoSliceSort.SortFunc(ranges.AsSpan(), ProtocolUtilities.CompareRanges);
        return new()
        {
            From = from,
            FromRanges = ranges
        };
    }

    private CallHierarchyOutgoingCall? convertCallSiteGroupToOutgoingCall(CompilerProgram program, Slice<CallSite> entries)
    {
        var ranges = Slice<TypeScript.Lsp.Protocol.Range>.Make(0, entries.Length);
        foreach (var entry in entries)
        {
            var(range, fidelity) = converters.ToLSPRangeForFeature(entry.SourceFile, entry.TextRange, Feature.CallHierarchy);
            if (!fidelity.IsNone())
                ranges = ranges.Append(range);
        }

        var to = createCallHierarchyItem(program, entries[0].Declaration);
        if (to is null || ranges.Length == 0)
            return null;
        GoSliceSort.SortFunc(ranges.AsSpan(), ProtocolUtilities.CompareRanges);
        return new()
        {
            To = to,
            FromRanges = ranges
        };
    }

    private sealed class IncomingEntry(LanguageService service, Node node) : IHasTextDocumentPosition
    {
        // Reuse the accepted synchronous sync.Once counterpart, including done-on-panic.
        private readonly TypeScript.ProjectSystem.ProjectOnce sourceOnce = new(), uriOnce = new(), positionOnce = new();
        private SourceFile? source;
        private DocumentUri uri;
        private Position position;
        internal SourceFile GetSourceFile()
        {
            sourceOnce.Do(() => source = Ast.GetSourceFileOfNode(node));
            return source!;
        }

        public DocumentUri TextDocumentURI()
        {
            uriOnce.Do(() => uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(GetSourceFile().OriginalFileName()));
            return uri;
        }

        public Position TextDocumentPosition()
        {
            positionOnce.Do(() =>
            {
                int start = ScannerApi.GetTokenPosOfNode(node, GetSourceFile(), false);
                (position, _) = service.createLspPosition(start, GetSourceFile());
            });
            return position;
        }
    }

    private (CallHierarchyIncomingCallsOrNull Result, Exception? Error) getIncomingCalls(ProtocolRequestContext context, CompilerProgram program, Node declaration, ICrossProjectOrchestrator? orchestrator)
    {
        if (Ast.IsSourceFile(declaration) || Ast.IsModuleDeclaration(declaration) || Ast.IsClassStaticBlockDeclaration(declaration))
            return (new(), null);
        var location = getCallHierarchyDeclarationReferenceNode(declaration);
        if (location is null)
            return (new(), null);
        var file = Ast.GetSourceFileOfNode(location);
        int start = ScannerApi.GetTokenPosOfNode(location, file, false);
        var(_, fidelity) = converters.ToLSPPositionForFeature(file, start, Feature.CallHierarchy);
        if (fidelity.IsNone())
            return (new(), null);
        var incoming = new IncomingEntry(this, location);
        var(result, error) = handleCrossProject(context, incoming, orchestrator, static (service, ctx, request, data, options) => service.symbolAndEntriesToIncomingCalls(ctx, request, data, options), combineIncomingCalls, false, false, default, null);
        if (result.CallHierarchyIncomingCalls is { } calls)
            GoSliceSort.SortFunc(calls.AsSpan(), static (a, b) =>
            {
                int uri = ByteStringKey.FromValue(a!.From!.Uri.Value).CompareTo(ByteStringKey.FromValue(b!.From!.Uri.Value));
                if (uri != 0)
                    return uri;
                if (a.FromRanges.Length == 0 || b.FromRanges.Length == 0)
                    return 0;
                return ProtocolUtilities.CompareRanges(a.FromRanges[0], b.FromRanges[0]);
            });
        return (result, error);
    }

    private (CallHierarchyIncomingCallsOrNull Result, Exception? Error) symbolAndEntriesToIncomingCalls(ProtocolRequestContext context, IncomingEntry parameters, SymbolAndEntriesData data, SymbolEntryTransformOptions options)
    {
        var program = GetProgram();
        Slice<ReferenceEntry> entries = default;
        foreach (var item in data.SymbolsAndEntries)
            entries = entries.Append(item.references);
        Slice<CallSite> sites = default;
        foreach (var entry in entries)
            if (convertEntryToCallSite(entry)is { } site)
                sites = sites.Append(site);
        if (sites.Length == 0)
            return (new(), null);
        var groups = new Dictionary<ulong, Slice<CallSite>>();
        foreach (var site in sites)
        {
            ulong key = getCallSiteGroupKey(site);
            groups[key] = groups.GetValueOrDefault(key).Append(site);
        }

        Slice<CallHierarchyIncomingCall?> result = default;
        foreach (var group in groups.Values)
            if (convertCallSiteGroupToIncomingCall(program, group)is { } call)
                result = result.Append(call);
        return (new() { CallHierarchyIncomingCalls = result }, null);
    }

    private Slice<CallHierarchyOutgoingCall?> getOutgoingCalls(CompilerProgram program, Node declaration)
    {
        if ((declaration.Flags & NodeFlags.Ambient) != 0 || Ast.IsMethodSignatureDeclaration(declaration))
            return default;
        var(checker, done) = program.GetTypeChecker(default(CancellationToken));
        try
        {
            var sites = collectCallSites(program, checker, declaration);
            if (sites.Length == 0)
                return default;
            var groups = new Dictionary<ulong, Slice<CallSite>>();
            foreach (var site in sites)
            {
                ulong key = getCallSiteGroupKey(site);
                groups[key] = groups.GetValueOrDefault(key).Append(site);
            }

            Slice<CallHierarchyOutgoingCall?> result = default;
            foreach (var group in groups.Values)
                if (convertCallSiteGroupToOutgoingCall(program, group)is { } call)
                    result = result.Append(call);
            GoSliceSort.SortFunc(result.AsSpan(), static (a, b) =>
            {
                int uri = ByteStringKey.FromValue(a!.To!.Uri.Value).CompareTo(ByteStringKey.FromValue(b!.To!.Uri.Value));
                if (uri != 0)
                    return uri;
                if (a.FromRanges.Length == 0 || b.FromRanges.Length == 0)
                    return 0;
                return ProtocolUtilities.CompareRanges(a.FromRanges[0], b.FromRanges[0]);
            });
            return result;
        }
        finally
        {
            done();
        }
    }

    private static ulong getCallSiteGroupKey(CallSite site) => Ast.GetNodeId(site.Declaration);
    private readonly record struct CallHierarchyLocationKey(ByteStringKey Uri, uint StartLine, uint StartCharacter, uint EndLine, uint EndCharacter)
    {
        internal static CallHierarchyLocationKey FromItem(CallHierarchyItem item) => new(ByteStringKey.FromValue(item.Uri.Value), item.SelectionRange.Start.Line, item.SelectionRange.Start.Character, item.SelectionRange.End.Line, item.SelectionRange.End.Character);
    }

    internal (CallHierarchyItemsOrNull Result, Exception? Error) ProvidePrepareCallHierarchy(ProtocolRequestContext context, DocumentUri uri, Position position)
    {
        var(program, file) = getProgramAndFile(uri);
        var declarations = callHierarchyDeclarations(file, position, program, false);
        Slice<CallHierarchyItem?> items = default;
        HashSet<CallHierarchyLocationKey>? seen = null;
        foreach (var declaration in declarations)
            if (createCallHierarchyItem(program, declaration)is { } item && (seen ??= new()).Add(CallHierarchyLocationKey.FromItem(item)))
                items = items.Append(item);
        return items.IsNil ? (new(), null) : (new() { CallHierarchyItems = items }, null);
    }

    internal (CallHierarchyIncomingCallsOrNull Result, Exception? Error) ProvideCallHierarchyIncomingCalls(ProtocolRequestContext context, CallHierarchyItem item, ICrossProjectOrchestrator? orchestrator)
    {
        var program = GetProgram();
        var file = program.GetSourceFile(item.Uri.FileName());
        if (file is null)
            return (new(), null);
        var declarations = callHierarchyDeclarations(file, item.SelectionRange.Start, program, true);
        Slice<CallHierarchyIncomingCall?> calls = default;
        var seen = new Dictionary<CallHierarchyLocationKey, CallHierarchyIncomingCall>();
        foreach (var declaration in declarations)
        {
            var(response, error) = getIncomingCalls(context, program, declaration, orchestrator);
            if (error is not null)
                return (new(), error);
            if (response.CallHierarchyIncomingCalls is { } incoming)
                foreach (var call in incoming)
                {
                    var key = CallHierarchyLocationKey.FromItem(call!.From!);
                    if (seen.TryGetValue(key, out var existing))
                    {
                        foreach (var range in call.FromRanges)
                            if (!GoSlices.ContainsFunc(existing.FromRanges, value => ProtocolUtilities.CompareRanges(value, range) == 0))
                                existing.FromRanges = existing.FromRanges.Append(range);
                    }
                    else
                    {
                        seen[key] = call;
                        calls = calls.Append(call);
                    }
                }
        }

        return calls.Length == 0 ? (new(), null) : (new() { CallHierarchyIncomingCalls = calls }, null);
    }

    internal (CallHierarchyOutgoingCallsOrNull Result, Exception? Error) ProvideCallHierarchyOutgoingCalls(ProtocolRequestContext context, CallHierarchyItem item)
    {
        var program = GetProgram();
        var file = program.GetSourceFile(item.Uri.FileName());
        if (file is null)
            return (new(), null);
        var declarations = callHierarchyDeclarations(file, item.SelectionRange.Start, program, true);
        Slice<CallHierarchyOutgoingCall?> calls = default;
        var seen = new Dictionary<CallHierarchyLocationKey, CallHierarchyOutgoingCall>();
        foreach (var declaration in declarations)
            foreach (var call in getOutgoingCalls(program, declaration))
            {
                var key = CallHierarchyLocationKey.FromItem(call!.To!);
                if (seen.TryGetValue(key, out var existing))
                {
                    foreach (var range in call.FromRanges)
                        if (!GoSlices.ContainsFunc(existing.FromRanges, value => ProtocolUtilities.CompareRanges(value, range) == 0))
                            existing.FromRanges = existing.FromRanges.Append(range);
                }
                else
                {
                    seen[key] = call;
                    calls = calls.Append(call);
                }
            }

        return calls.Length == 0 ? (new(), null) : (new() { CallHierarchyOutgoingCalls = calls }, null);
    }

    private Slice<Node> callHierarchyDeclarations(SourceFile file, Position position, CompilerProgram program, bool allowSourceFile)
    {
        var positions = converters.FromLSPPositionForSourceFile(file, position, Feature.CallHierarchy);
        Slice<Node> declarations = default;
        HashSet<Node>? seen = null;
        foreach (var mapped in positions)
        {
            if (!mapped.Fidelity.IsSingleSegment())
                continue;
            var source = mapped.Script;
            int pos = mapped.Position;
            Node? node = source;
            if (pos != 0)
                node = AstNav.GetTouchingPropertyName(source, pos);
            if (node is null || !allowSourceFile && node.Kind == Kind.SourceFile)
                continue;
            switch (resolveCallHierarchyDeclaration(program, node))
            {
                case Node single:
                    if ((seen ??= new(ReferenceEqualityComparer.Instance)).Add(single))
                        declarations = declarations.Append(single);
                    break;
                case Slice<Node> nodes:
                    foreach (var declaration in nodes)
                        if ((seen ??= new(ReferenceEqualityComparer.Instance)).Add(declaration))
                            declarations = declarations.Append(declaration);
                    break;
            }
        }

        return declarations;
    }
}
