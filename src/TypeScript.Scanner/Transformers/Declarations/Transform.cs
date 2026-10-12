// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete declaration counterpart, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.DiagnosticPrinting;
using ScannerApi = TypeScript.Scanner.Scanner;
using AstNodeFactory = TypeScript.Scanner.NodeFactory;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;
using NodeBuilderInternalFlags = TypeScript.NodeBuilding.InternalFlags;
using static TypeScript.Transformers.Declarations.DeclarationTransforms;

namespace TypeScript.Transformers.Declarations;
internal struct ReferencedFilePair
{
    internal SourceFile file;
    internal FileReference @ref;
}

internal record struct ThisPropertyAssignmentKey
{
    internal ByteStringKey name;
    internal Node node;
    internal bool isStatic, isPrivate;
}

internal sealed class DeclarationTransformer : Transformer, IDeclarationTransformer
{
    // transform.go:555–564: synchronous cleanup closure represented by its value snapshot.
    private readonly record struct DiagnosticContextSnapshot(GetSymbolAccessibilityDiagnostic Diagnostic, Node ErrorName, bool Suppress)
    {
        internal void Restore(DeclarationTransformer owner)
        {
            owner.state.getSymbolAccessibilityDiagnostic = Diagnostic;
            owner.state.errorNameNode = ErrorName;
            owner.suppressNewDiagnosticContexts = Suppress;
        }
    }

    // transform.go:115–129: original retained callback.
    private void reportExpandoFunctionErrors(Node node)
    {
        if (!this.state.isolatedDeclarations)
        {
            return;
        }

        var props = this.resolver.GetPropertiesOfContainerFunction(node);
        {
            foreach (var p in GoRuntime.RangeValues(props))
            {
                if (Ast.IsExpandoPropertyDeclaration(p.ValueDeclaration))
                {
                    var errorTarget = p.ValueDeclaration;
                    if (Ast.IsBinaryExpression(errorTarget))
                    {
                        errorTarget = errorTarget.AsBinaryExpression().Left;
                    }

                    this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(errorTarget, Diagnostics.Assigning_properties_to_functions_without_declaring_them_is_not_supported_with_isolatedDeclarations_Add_an_explicit_declaration_for_the_properties_assigned_to_this_function));
                }
            }
        }
    }

    private readonly Func<Node, Node> transformEnumMemberCallback;
    private Func<Kind, Node> createModifierCallback;
    private DeclarationTransformer() => transformEnumMemberCallback = transformEnumMember;
    // transform.go:2271–2317: original synchronous enum member mapper, cached once.
    private Node transformEnumMember(Node m)
    {
        if (this.shouldStripInternal(m))
        {
            return null;
        }

        var enumValue = this.resolver.GetEnumMemberValue(m);
        if ((((this.state.isolatedDeclarations && (m.Initializer() != null)) && enumValue.HasExternalReferences) && !Ast.IsComputedPropertyName(m.Name())))
        {
            this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(m, Diagnostics.Enum_member_initializers_must_be_computable_without_references_to_external_symbols_with_isolatedDeclarations));
        }

        Node newInitializer = default;
        {
            var value = enumValue.Value;
            switch (value)
            {
                case JsNum.Number number:
                {
                    if (number.IsInf())
                    {
                        if ((number.Value > 0))
                        {
                            newInitializer = this.Factory().NewIdentifier("Infinity");
                        }
                        else
                        {
                            newInitializer = this.Factory().NewPrefixUnaryExpression(Kind.MinusToken, this.Factory().NewIdentifier("Infinity"));
                        }
                    }
                    else
                    {
                        if (number.IsNaN())
                        {
                            newInitializer = this.Factory().NewIdentifier("NaN");
                        }
                        else
                        {
                            if ((number.Value >= 0))
                            {
                                newInitializer = this.Factory().NewNumericLiteral(number.String(), TokenFlags.None);
                            }
                            else
                            {
                                newInitializer = this.Factory().NewPrefixUnaryExpression(Kind.MinusToken, this.Factory().NewNumericLiteral((-number).String(), TokenFlags.None));
                            }
                        }
                    }

                    break;
                }

                case string text:
                {
                    newInitializer = this.Factory().NewStringLiteral(text, TokenFlags.None);
                    break;
                }

                case ByteStringValue textValue:
                {
                    newInitializer = this.Factory().NewStringLiteralWithTextValue(textValue, TokenFlags.None);
                    break;
                }

                default:
                {
                    newInitializer = null;
                    break;
                }
            }
        }

        var result = this.Factory().UpdateEnumMember(m.AsEnumMember(), m.Name(), newInitializer);
        this.preserveJsDoc(result, m);
        return result;
    }

    private IDeclarationEmitHost host;
    // Retained upstream transformer state; assigned but not read in the pinned implementation.
#pragma warning disable CS0414
    private CompilerOptions compilerOptions;
#pragma warning restore CS0414
    private SymbolTrackerImpl tracker;
    private SymbolTrackerSharedState state;
    private IEmitResolver resolver;
    private RootedFilePath declarationFilePath;
    private bool needsDeclare, needsScopeFixMarker, resultHasScopeMarker, resultHasExternalModuleIndicator, suppressNewDiagnosticContexts, inClassExpressionDeclaration;
    private Node enclosingDeclaration, cjsExportAssignment, cjsExportAssignmentName;
    private HashSet<ByteStringKey> witnessedCjsExports;
    private Dictionary<ulong, Node> lateStatementReplacementMap, expandoHosts;
    private Dictionary<ulong, Slice<Node>> expandoMembers;
    private Dictionary<ulong, Slice<BinaryExpression>> deferredExpandoAssignments;
    private HashSet<ThisPropertyAssignmentKey> seenProperties;
    private Slice<Node> thisPropertyAssignmentsCollected, cjsExportMembers;
    private Slice<ReferencedFilePair> rawReferencedFiles;
    private Slice<FileReference> rawTypeReferenceDirectives, rawLibReferenceDirectives;
    private NodeVisitor bindingNameVisitor, expressionVisitor, cjsExportAssignmentVisitor, exportStrippingVisitor, thisPropertyVisitor, declareStrippingVisitor;
    private const NodeBuilderFlags declarationEmitNodeBuilderFlags = NodeBuilderFlags.MultilineObjectLiterals | NodeBuilderFlags.WriteClassExpressionAsTypeLiteral | NodeBuilderFlags.UseTypeOfFunction | NodeBuilderFlags.UseStructuralFallback | NodeBuilderFlags.AllowEmptyTuple | NodeBuilderFlags.GenerateNamesForShadowedTypeParams | NodeBuilderFlags.NoTruncation;
    private const NodeBuilderInternalFlags declarationEmitInternalNodeBuilderFlags = NodeBuilderInternalFlags.AllowUnresolvedNames;
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:50:1.
    private static ThisPropertyAssignmentKey getThisPropertyAssignmentKey(Node name, Node node, bool isStatic)
    {
        var isPrivate = Ast.IsPrivateIdentifier(name);
        if (((name != null) && !Ast.IsDynamicName(name)))
        {
            {
                var tuple1 = Ast.TryGetTextValueOfPropertyName(name);
                var nameText = tuple1.Item1;
                var ok = tuple1.Item2;
                if (ok)
                {
                    return new ThisPropertyAssignmentKey
                    {
                        name = ByteStringKey.FromValue(nameText),
                        isStatic = isStatic,
                        isPrivate = isPrivate
                    };
                }
            }
        }

        return new ThisPropertyAssignmentKey
        {
            node = node,
            isStatic = isStatic,
            isPrivate = isPrivate
        };
    }

    // TODO: Convert to transformers.TransformerFactory signature to allow more automatic composition with other transforms
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:99:1.
    public static DeclarationTransformer NewDeclarationTransformer(IDeclarationEmitHost host, IEmitResolver resolver, CompilerOptions compilerOptions, RootedFilePath declarationFilePath)
    {
        if (((resolver == null) || (resolver.EmitContext() == null)))
        {
            throw new InvalidOperationException("DeclarationTransformer requires an EmitResolver with an EmitContext");
        }

        var context = resolver.EmitContext();
        var state = new SymbolTrackerSharedState
        {
            isolatedDeclarations = (compilerOptions.IsolatedDeclarations == Tristate.True),
            stripInternal = (compilerOptions.StripInternal == Tristate.True),
            resolver = resolver
        };
        var tracker = SymbolTrackerImpl.NewSymbolTracker(host, resolver, state);
        var tx = new DeclarationTransformer
        {
            host = host,
            compilerOptions = compilerOptions,
            tracker = tracker,
            state = state,
            resolver = resolver,
            declarationFilePath = declarationFilePath
        };
        tx.state.reportExpandoFunctionErrors = tx.reportExpandoFunctionErrors;
        tx.NewTransformer(tx.visit, context);
        tx.createModifierCallback = tx.Factory().NewModifier;
        tx.bindingNameVisitor = tx.EmitContext().NewNodeVisitor(tx.visitBindingName);
        tx.expressionVisitor = tx.EmitContext().NewNodeVisitor(tx.visitNestedExpression);
        tx.exportStrippingVisitor = tx.EmitContext().NewNodeVisitor(tx.stripExportModifiers);
        tx.thisPropertyVisitor = tx.EmitContext().NewNodeVisitor(tx.visitThisPropertyAssignments);
        tx.cjsExportAssignmentVisitor = tx.EmitContext().NewNodeVisitor(tx.visitCJSExportAssignments);
        tx.declareStrippingVisitor = tx.EmitContext().NewNodeVisitor(tx.stripDeclareModifiers);
        return tx;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:140:1.
    // Genuine shared declaration-stage contract forwards to the initialized visitor.
    SourceFile IDeclarationTransformer.TransformSourceFile(SourceFile file) => TransformSourceFile(file);
    public Slice<Diagnostic> GetDiagnostics()
    {
        return this.state.diagnostics;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:144:1.
    private bool shouldStripInternal(Node node)
    {
        return ((this.state.stripInternal && (node != null)) && this.isInternalDeclaration(node, this.state.currentSourceFile));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:148:1.
    private bool isInternalDeclaration(Node node, SourceFile sourceFile)
    {
        if ((node == null))
        {
            return false;
        }

        var parseTreeNode = this.EmitContext().MostOriginal(node);
        if (!Ast.IsParseTreeNode(parseTreeNode))
        {
            return false;
        }

        if ((parseTreeNode.Kind == Kind.Parameter))
        {
            var @params = parseTreeNode.Parent.Parameters();
            var paramIdx = Core.FindIndex(@params, parseTreeNode, static (p, target) => p.AsNode() == target);
            Node previousSibling = default;
            if ((paramIdx > 0))
            {
                previousSibling = @params[(paramIdx - 1)].AsNode();
            }

            var text = sourceFile.TextView();
            Slice<CommentRange> commentRanges = default;
            if ((previousSibling != null))
            {
                var trailingPos = ScannerApi.SkipTriviaEx(text, (previousSibling.End() + 1), new SkipTriviaOptions { StopAtComments = true });
                {
                    foreach (var comment in GoRuntime.RangeKeys(ScannerApi.GetTrailingCommentRanges(this.Factory().AsNodeFactory(), text, trailingPos)))
                    {
                        commentRanges = commentRanges.Append(comment);
                    }
                }

                {
                    foreach (var comment2 in GoRuntime.RangeKeys(ScannerApi.GetLeadingCommentRanges(this.Factory().AsNodeFactory(), text, node.Pos())))
                    {
                        commentRanges = commentRanges.Append(comment2);
                    }
                }
            }
            else
            {
                var trailingPos2 = ScannerApi.SkipTriviaEx(text, node.Pos(), new SkipTriviaOptions { StopAtComments = true });
                {
                    foreach (var comment3 in GoRuntime.RangeKeys(ScannerApi.GetTrailingCommentRanges(this.Factory().AsNodeFactory(), text, trailingPos2)))
                    {
                        commentRanges = commentRanges.Append(comment3);
                    }
                }
            }

            if ((GoRuntime.Len(commentRanges) > 0))
            {
                return hasInternalAnnotation(commentRanges[(GoRuntime.Len(commentRanges) - 1)], sourceFile);
            }

            return false;
        }

        {
            foreach (var commentRange in GoRuntime.RangeKeys(this.getLeadingCommentRangesOfNode(parseTreeNode, sourceFile)))
            {
                if (hasInternalAnnotation(commentRange, sourceFile))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:201:1.
    private IEnumerable<CommentRange> getLeadingCommentRangesOfNode(Node node, SourceFile sourceFile)
    {
        if (((node == null) || (node.Kind == Kind.JsxText)))
        {
            return null;
        }

        return ScannerApi.GetLeadingCommentRanges(this.Factory().AsNodeFactory(), sourceFile.TextView(), node.Pos());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:208:1.
    private static bool hasInternalAnnotation(CommentRange commentRange, SourceFile sourceFile)
    {
        return sourceFile.TextView().Slice(commentRange.Pos, commentRange.End).AsSpan().IndexOf("@internal"u8) >= 0;
    }

    // functions as both `visitDeclarationStatements` and `transformRoot`, utilitzing SyntaxList nodes
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:224:1.
    private Node visit(Node node)
    {
        if ((node == null))
        {
            return null;
        }

        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                return this.visitSourceFile(node.AsSourceFile());
            }

            case Kind.FunctionDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.ImportEqualsDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.ClassDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.EnumDeclaration:
            case Kind.VariableStatement:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ExportDeclaration:
            case Kind.ExportAssignment:
            {
                return this.visitDeclarationStatements(node);
            }

            case Kind.BreakStatement:
            case Kind.ContinueStatement:
            case Kind.DebuggerStatement:
            case Kind.DoStatement:
            case Kind.EmptyStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.ForStatement:
            case Kind.IfStatement:
            case Kind.LabeledStatement:
            case Kind.ReturnStatement:
            case Kind.SwitchStatement:
            case Kind.ThrowStatement:
            case Kind.TryStatement:
            case Kind.WhileStatement:
            case Kind.WithStatement:
            case Kind.NotEmittedStatement:
            case Kind.Block:
            case Kind.MissingDeclaration:
            case Kind.ExpressionStatement:
            {
                return null;
            }

            default:
            {
                return this.visitDeclarationSubtree(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:274:1.
    private static SymbolAccessibilityDiagnostic? throwDiagnostic(SymbolAccessibilityResult result)
    {
        throw new InvalidOperationException("Diagnostic emitted without context");
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:278:1.
    private Node visitSourceFile(SourceFile node)
    {
        this.cjsExportAssignmentName = null;
        if (node.IsDeclarationFile)
        {
            return node.AsNode();
        }

        this.needsDeclare = true;
        this.needsScopeFixMarker = false;
        this.resultHasScopeMarker = false;
        this.enclosingDeclaration = node.AsNode();
        this.state.getSymbolAccessibilityDiagnostic = throwDiagnostic;
        this.resultHasExternalModuleIndicator = false;
        this.suppressNewDiagnosticContexts = false;
        this.state.lateMarkedStatements = Slice<Node>.Make(0);
        this.lateStatementReplacementMap = new Dictionary<ulong, Node>();
        this.expandoHosts = new Dictionary<ulong, Node>();
        this.expandoMembers = new Dictionary<ulong, Slice<Node>>();
        this.deferredExpandoAssignments = new Dictionary<ulong, Slice<BinaryExpression>>();
        this.rawReferencedFiles = Slice<ReferencedFilePair>.Make(0);
        this.rawTypeReferenceDirectives = Slice<FileReference>.Make(0);
        this.rawLibReferenceDirectives = Slice<FileReference>.Make(0);
        this.witnessedCjsExports?.Clear();
        this.state.currentSourceFile = node;
        this.collectFileReferences(node);
        this.resolver.PrecalculateDeclarationEmitVisibility(this.EmitContext().MostOriginal(node.AsNode()).AsSourceFile());
        var updated = this.transformSourceFile(node);
        this.state.currentSourceFile = null;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:308:1.
    private void collectFileReferences(SourceFile sourceFile)
    {
        this.rawReferencedFiles = this.rawReferencedFiles.Append(Core.MapSlice(sourceFile.ReferencedFiles, sourceFile, static (@ref, file) => new ReferencedFilePair { file = file, @ref = @ref }));
        this.rawTypeReferenceDirectives = this.rawTypeReferenceDirectives.Append(sourceFile.TypeReferenceDirectives);
        this.rawLibReferenceDirectives = this.rawLibReferenceDirectives.Append(sourceFile.LibReferenceDirectives);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:314:1.
    private static Slice<Node> nodeOrSyntaxListChildren(Node node)
    {
        if (Ast.IsSyntaxList(node))
        {
            return node.AsSyntaxList().Children;
        }

        return Slice<Node>.FromArray([node]);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:321:1.
    private static Slice<Node> flattenSyntaxLists(Slice<Node> nodes)
    {
        return Core.FlatMap(nodes, nodeOrSyntaxListChildren);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:325:1.
    private NodeList appendCjsExports(NodeList combinedStatements)
    {
        var result = Slice<Node>.Make(0);
        if ((this.cjsExportAssignment != null))
        {
            result = result.Append(this.cjsExportAssignment);
        }

        result = result.Append(this.cjsExportMembers);
        result = result.Append(combinedStatements.Nodes);
        var statementNodes = flattenSyntaxLists(result);
        if ((GoRuntime.Len(statementNodes) != GoRuntime.Len(combinedStatements.Nodes)))
        {
            combinedStatements = this.Factory().NewNodeList(statementNodes);
        }

        return combinedStatements;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:339:1.
    private Node transformSourceFile(SourceFile node)
    {
        this.cjsExportAssignment = null;
        this.cjsExportAssignmentName = null;
        this.cjsExportMembers = default;
        try
        {
            this.cjsExportAssignmentVisitor.VisitNode(node.AsNode());
            this.expressionVisitor.VisitNode(node.AsNode());
            NodeList combinedStatements = default;
            var statements = this.Visitor().VisitNodes(node.Statements);
            combinedStatements = this.transformAndReplaceLatePaintedStatements(statements);
            combinedStatements = this.appendCjsExports(combinedStatements);
            combinedStatements.Loc = statements.Loc;
            if (Ast.IsExternalOrCommonJSModule(node))
            {
                if (Ast.IsInJSFile(node.AsNode()))
                {
                    {
                        // Go nil SymbolTable lookup yields nil; the retained source symbol itself is required.
                        var exportEquals = node.Symbol.Exports?[Ast.InternalSymbolNameExportEquals];
                        if (((exportEquals != null) && (GoRuntime.Len(exportEquals.Declarations) > 1)))
                        {
                            {
                                foreach (var node2 in GoRuntime.RangeValues(exportEquals.Declarations))
                                {
                                    this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(node2, Diagnostics.Multiple_module_exports_assignments_cannot_be_serialized_for_declaration_emit));
                                }
                            }
                        }
                    }
                }

                if ((!this.resultHasExternalModuleIndicator || ((this.needsScopeFixMarker && !this.resultHasScopeMarker))))
                {
                    var marker = createEmptyExports(this.Factory().AsNodeFactory());
                    var newList = combinedStatements.Nodes.Append(marker);
                    var withMarker = this.Factory().NewNodeList(newList);
                    withMarker.Loc = combinedStatements.Loc;
                    combinedStatements = withMarker;
                }
            }

            var result = this.Factory().UpdateSourceFile(node, combinedStatements, node.EndOfFileToken);
            result.AsSourceFile().LibReferenceDirectives = this.getLibReferences();
            result.AsSourceFile().TypeReferenceDirectives = this.getTypeReferences();
            result.AsSourceFile().IsDeclarationFile = true;
            result.AsSourceFile().ReferencedFiles = this.getReferencedFiles(this.declarationFilePath.Directory());
            return result.AsNode();
        }
        finally
        {
            this.cjsExportAssignment = null;
            this.cjsExportAssignmentName = null;
            this.cjsExportMembers = default;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:379:1.
    private static Node createEmptyExports(AstNodeFactory factory)
    {
        return factory.NewExportDeclaration(null, false, factory.NewNamedExports(factory.NewNodeList(Slice<Node>.Make(0))), null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:383:1.
    private NodeList transformAndReplaceLatePaintedStatements(NodeList statements)
    {
        while (true)
        {
            if ((GoRuntime.Len(this.state.lateMarkedStatements) == 0))
            {
                break;
            }

            var next = this.state.lateMarkedStatements[0];
            this.state.lateMarkedStatements = this.state.lateMarkedStatements.Slice2(1, this.state.lateMarkedStatements.Length);
            var saveNeedsDeclare = this.needsDeclare;
            this.needsDeclare = ((next.Parent != null) && Ast.IsSourceFile(next.Parent));
            var result = this.transformTopLevelDeclaration(next);
            this.needsDeclare = saveNeedsDeclare;
            var original = this.EmitContext().MostOriginal(next);
            var id = Ast.GetNodeId(original);
            this.lateStatementReplacementMap[id] = result;
        }

        var results = Slice<Node>.Make(0, statements.Nodes.Length);
        {
            foreach (var statement in GoRuntime.RangeValues(statements.Nodes))
            {
                if (!Ast.IsLateVisibilityPaintedStatement(statement))
                {
                    results = results.Append(statement);
                    continue;
                }

                var original2 = this.EmitContext().MostOriginal(statement);
                var id2 = Ast.GetNodeId(original2);
                var ok = this.lateStatementReplacementMap.TryGetValue(id2, out var replacement);
                if (!ok)
                {
                    results = results.Append(statement);
                    continue;
                }

                if ((replacement == null))
                {
                    continue;
                }

                if ((replacement.Kind == Kind.SyntaxList))
                {
                    if ((!this.needsScopeFixMarker || !this.resultHasExternalModuleIndicator))
                    {
                        {
                            foreach (var range9 in GoRuntime.Range(replacement.AsSyntaxList().Children))
                            {
                                var _2 = range9.Key;
                                var elem = range9.Value;
                                if (NeedsScopeMarker(elem))
                                {
                                    this.needsScopeFixMarker = true;
                                }

                                if ((Ast.IsSourceFile(statement.Parent) && Ast.IsExternalModuleIndicator(elem)))
                                {
                                    this.resultHasExternalModuleIndicator = true;
                                }
                            }
                        }
                    }

                    results = results.Append(replacement.AsSyntaxList().Children);
                }
                else
                {
                    if (NeedsScopeMarker(replacement))
                    {
                        this.needsScopeFixMarker = true;
                    }

                    if ((Ast.IsSourceFile(statement.Parent) && Ast.IsExternalModuleIndicator(replacement)))
                    {
                        this.resultHasExternalModuleIndicator = true;
                    }

                    results = results.Append(replacement);
                }
            }
        }

        return this.Factory().NewNodeList(results);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:461:1.
    private Slice<FileReference> getReferencedFiles(RootedDirectoryPath outputFilePath)
    {
        Slice<FileReference> results = default;
        {
            foreach (var pair in GoRuntime.RangeValues(this.rawReferencedFiles))
            {
                var sourceFile = pair.file;
                var @ref = pair.@ref;
                if (!@ref.Preserve)
                {
                    continue;
                }

                var file = this.host.GetSourceFileFromReference(sourceFile, @ref);
                if ((file == null))
                {
                    continue;
                }

                RootedFilePath declFileName = default;
                if (file.IsDeclarationFile)
                {
                    declFileName = RootedFilePath.FromValue(file.FileNameGoValue());
                }
                else
                {
                    var paths = this.host.GetOutputPathsFor(file, true);
                    declFileName = paths.DeclarationFilePath();
                    if ((declFileName.AsValue().Length == 0))
                    {
                        declFileName = paths.JsFilePath();
                    }

                    if ((declFileName.AsValue().Length == 0))
                    {
                        declFileName = RootedFilePath.FromValue(file.FileNameGoValue());
                    }
                }

                if ((declFileName.AsValue().Length == 0))
                {
                    continue;
                }

                var fileName = TsPath.GetRelativePathToDirectoryOrUrl(outputFilePath.AsValue(), declFileName.AsValue(), false, this.host.CaseSensitivity());
                results = results.Append(new FileReference { TextRange = Core.NewTextRange(-1, -1), FileNameValue = fileName, ResolutionMode = @ref.ResolutionMode, Preserve = @ref.Preserve });
            }
        }

        return results;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:513:1.
    private Slice<FileReference> getLibReferences()
    {
        Slice<FileReference> result = default;
        {
            foreach (var @ref in GoRuntime.RangeValues(this.rawLibReferenceDirectives))
            {
                if (!@ref.Preserve)
                {
                    continue;
                }

                result = result.Append(new FileReference { TextRange = Core.NewTextRange(-1, -1), FileNameValue = @ref.FileNameValue, ResolutionMode = @ref.ResolutionMode, Preserve = @ref.Preserve });
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:529:1.
    private Slice<FileReference> getTypeReferences()
    {
        Slice<FileReference> result = default;
        {
            foreach (var @ref in GoRuntime.RangeValues(this.rawTypeReferenceDirectives))
            {
                if (!@ref.Preserve)
                {
                    continue;
                }

                result = result.Append(new FileReference { TextRange = Core.NewTextRange(-1, -1), FileNameValue = @ref.FileNameValue, ResolutionMode = @ref.ResolutionMode, Preserve = @ref.Preserve });
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:545:1.
    private (bool, DiagnosticContextSnapshot) setupDiagnosticContext(Node input)
    {
        var canProduceDiagnostic = CanProduceDiagnostics(input);
        var oldWithinObjectLiteralType = this.suppressNewDiagnosticContexts;
        var shouldEnterSuppressNewDiagnosticsContextContext = ((((input.Kind == Kind.TypeLiteral) || (input.Kind == Kind.MappedType))) && !(((input.Parent.Kind == Kind.TypeAliasDeclaration) || (input.Parent.Kind == Kind.JSTypeAliasDeclaration))));
        var oldDiag = this.state.getSymbolAccessibilityDiagnostic;
        if ((canProduceDiagnostic && !this.suppressNewDiagnosticContexts))
        {
            this.state.getSymbolAccessibilityDiagnostic = CreateGetSymbolAccessibilityDiagnosticForNode(input);
        }

        var oldName = this.state.errorNameNode;
        if (shouldEnterSuppressNewDiagnosticsContextContext)
        {
            this.suppressNewDiagnosticContexts = true;
        }

        return (canProduceDiagnostic, new DiagnosticContextSnapshot(oldDiag, oldName, oldWithinObjectLiteralType));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:567:1.
    private Node visitDeclarationSubtree(Node input)
    {
        if (this.shouldStripInternal(input))
        {
            return null;
        }

        if (Ast.IsDeclaration(input))
        {
            if (IsDeclarationAndNotVisible(this.EmitContext(), this.resolver, input))
            {
                return null;
            }

            if (Ast.HasDynamicName(input))
            {
                if (this.state.isolatedDeclarations)
                {
                    if (!this.resolver.IsDefinitelyReferenceToGlobalSymbolObject(input.Name().Expression()))
                    {
                        if ((Ast.IsClassDeclaration(input.Parent) || Ast.IsObjectLiteralExpression(input.Parent)))
                        {
                            this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(input, Diagnostics.Computed_property_names_on_class_or_object_literals_cannot_be_inferred_with_isolatedDeclarations));
                            return null;
                        }
                        else
                        {
                            if ((((Ast.IsInterfaceDeclaration(input.Parent) || Ast.IsTypeLiteralNode(input.Parent))) && !Ast.IsEntityNameExpression(input.Name().Expression())))
                            {
                                this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(input, Diagnostics.Computed_properties_must_be_number_or_string_literals_variables_or_dotted_expressions_with_isolatedDeclarations));
                                return null;
                            }
                        }
                    }
                }
                else
                {
                    if ((!this.resolver.IsLateBound(this.EmitContext().ParseNode(input)) || !Ast.IsEntityNameExpression(input.Name().Expression())))
                    {
                        return null;
                    }
                }
            }
        }

        if ((Ast.IsFunctionLike(input) && this.resolver.IsImplementationOfOverload(input)))
        {
            return null;
        }

        if ((input.Kind == Kind.SemicolonClassElement))
        {
            return null;
        }

        if ((Ast.IsHeritageClause(input) && (((GoRuntime.Len(input.AsHeritageClause().Types.Nodes) == 0) || (((GoRuntime.Len(input.AsHeritageClause().Types.Nodes) == 1) && Ast.NodeIsMissing(input.AsHeritageClause().Types.Nodes[0])))))))
        {
            return null;
        }

        var previousEnclosingDeclaration = this.enclosingDeclaration;
        if (IsEnclosingDeclaration(input))
        {
            this.enclosingDeclaration = input;
        }

        var tuple13 = this.setupDiagnosticContext(input);
        var canProduceDiagnostic = tuple13.Item1;
        var cleanupDiagnosticContext = tuple13.Item2;
        try
        {
            Node result = default;
            switch (input.Kind)
            {
                case Kind.MappedType:
                {
                    result = this.transformMappedTypeNode(input.AsMappedTypeNode());
                    break;
                }

                case Kind.HeritageClause:
                {
                    result = this.transformHeritageClause(input.AsHeritageClause());
                    break;
                }

                case Kind.MethodSignature:
                {
                    result = this.transformMethodSignatureDeclaration(input.AsMethodSignatureDeclaration());
                    break;
                }

                case Kind.MethodDeclaration:
                {
                    result = this.transformMethodDeclaration(input.AsMethodDeclaration());
                    break;
                }

                case Kind.ConstructSignature:
                {
                    result = this.transformConstructSignatureDeclaration(input.AsConstructSignatureDeclaration());
                    break;
                }

                case Kind.Constructor:
                {
                    result = this.transformConstructorDeclaration(input.AsConstructorDeclaration());
                    break;
                }

                case Kind.GetAccessor:
                {
                    result = this.transformGetAccesorDeclaration(input.AsGetAccessorDeclaration());
                    break;
                }

                case Kind.SetAccessor:
                {
                    result = this.transformSetAccessorDeclaration(input.AsSetAccessorDeclaration());
                    break;
                }

                case Kind.PropertyDeclaration:
                {
                    result = this.transformPropertyDeclaration(input.AsPropertyDeclaration());
                    break;
                }

                case Kind.PropertySignature:
                {
                    result = this.transformPropertySignatureDeclaration(input.AsPropertySignatureDeclaration());
                    break;
                }

                case Kind.CallSignature:
                {
                    result = this.transformCallSignatureDeclaration(input.AsCallSignatureDeclaration());
                    break;
                }

                case Kind.IndexSignature:
                {
                    result = this.transformIndexSignatureDeclaration(input.AsIndexSignatureDeclaration());
                    break;
                }

                case Kind.VariableDeclaration:
                {
                    result = this.transformVariableDeclaration(input.AsVariableDeclaration());
                    break;
                }

                case Kind.TypeParameter:
                {
                    result = this.transformTypeParameterDeclaration(input.AsTypeParameterDeclaration());
                    break;
                }

                case Kind.ExpressionWithTypeArguments:
                {
                    result = this.transformExpressionWithTypeArguments(input.AsExpressionWithTypeArguments());
                    break;
                }

                case Kind.TypeReference:
                {
                    result = this.transformTypeReference(input.AsTypeReferenceNode());
                    break;
                }

                case Kind.ConditionalType:
                {
                    result = this.transformConditionalTypeNode(input.AsConditionalTypeNode());
                    break;
                }

                case Kind.FunctionType:
                {
                    result = this.transformFunctionTypeNode(input.AsFunctionTypeNode());
                    break;
                }

                case Kind.ConstructorType:
                {
                    result = this.transformConstructorTypeNode(input.AsConstructorTypeNode());
                    break;
                }

                case Kind.ImportType:
                {
                    result = this.transformImportTypeNode(input.AsImportTypeNode());
                    break;
                }

                case Kind.TypeQuery:
                {
                    this.checkEntityNameVisibility(input.AsTypeQueryNode().ExprName, this.enclosingDeclaration);
                    result = this.Visitor().VisitEachChild(input);
                    break;
                }

                case Kind.QualifiedName:
                {
                    if ((input.AsQualifiedName().Right.Kind == Kind.PrivateIdentifier))
                    {
                        this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(input, Diagnostics.Declaration_emit_elides_private_members_but_0_refers_to_a_private_member_Write_an_explicit_type_here, input.AsQualifiedName().Right.TextValue()));
                    }

                    result = this.Visitor().VisitEachChild(input);
                    break;
                }

                case Kind.TupleType:
                {
                    result = this.Visitor().VisitEachChild(input);
                    if ((result != null))
                    {
                        if (Transformers.IsOriginalNodeSingleLine(this.EmitContext(), input))
                        {
                            this.EmitContext().AddEmitFlags(result, EmitFlags.SingleLine);
                        }
                    }

                    break;
                }

                case Kind.JSDocTypeExpression:
                {
                    result = this.transformJSDocTypeExpression(input.AsJSDocTypeExpression());
                    break;
                }

                case Kind.JSDocTypeLiteral:
                {
                    result = this.transformJSDocTypeLiteral(input.AsJSDocTypeLiteral());
                    break;
                }

                case Kind.JSDocPropertyTag:
                {
                    result = this.transformJSDocPropertyTag(input.AsJSDocParameterOrPropertyTag());
                    break;
                }

                case Kind.JSDocAllType:
                {
                    result = this.transformJSDocAllType(input.AsJSDocAllType());
                    break;
                }

                case Kind.JSDocNullableType:
                {
                    result = this.transformJSDocNullableType(input.AsJSDocNullableType());
                    break;
                }

                case Kind.JSDocNonNullableType:
                {
                    result = this.transformJSDocNonNullableType(input.AsJSDocNonNullableType());
                    break;
                }

                case Kind.JSDocOptionalType:
                {
                    result = this.transformJSDocOptionalType(input.AsJSDocOptionalType());
                    break;
                }

                case Kind.JSDocVariadicType:
                {
                    result = this.transformJSDocVariadicType(input.AsJSDocVariadicType());
                    break;
                }

                default:
                {
                    result = this.Visitor().VisitEachChild(input);
                    break;
                }
            }

            if ((((result != null) && canProduceDiagnostic) && Ast.HasDynamicName(input)))
            {
                this.checkName(input);
            }

            this.enclosingDeclaration = previousEnclosingDeclaration;
            return result;
        }
        finally
        {
            cleanupDiagnosticContext.Restore(this);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:702:1.
    private void checkName(Node node)
    {
        var oldDiag = this.state.getSymbolAccessibilityDiagnostic;
        if (!this.suppressNewDiagnosticContexts)
        {
            this.state.getSymbolAccessibilityDiagnostic = CreateGetSymbolAccessibilityDiagnosticForNodeName(node);
        }

        this.state.errorNameNode = node.Name();
        Debug.Assert(Ast.HasDynamicName(node));
        var entityName = node.Name().Expression();
        this.checkEntityNameVisibility(entityName, this.enclosingDeclaration);
        if (!this.suppressNewDiagnosticContexts)
        {
            this.state.getSymbolAccessibilityDiagnostic = oldDiag;
        }

        this.state.errorNameNode = null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:717:1.
    private Node transformMappedTypeNode(MappedTypeNode input)
    {
        Node typeNode = default;
        if ((input.Type == null))
        {
            typeNode = this.Factory().NewKeywordTypeNode(Kind.AnyKeyword);
        }
        else
        {
            typeNode = this.Visitor().Visit(input.Type);
        }

        return this.Factory().UpdateMappedTypeNode(input, input.ReadonlyToken, this.Visitor().Visit(input.TypeParameter), this.Visitor().Visit(input.NameType), input.QuestionToken, typeNode, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:736:1.
    private Node transformHeritageClause(HeritageClause clause)
    {
        var retainedClauses = Core.Filter(clause.Types.Nodes, clause.Token, static (t, token) =>
        {
            var name = Ast.GetHeritageClauseElementName(t);
            return ((Ast.IsEntityName(name) || Ast.IsEntityNameExpression(name)) || ((((token == Kind.ExtendsKeyword) && Ast.IsExpressionWithTypeArguments(t)) && (t.Expression().Kind == Kind.NullKeyword))));
        });
        if ((GoRuntime.Len(retainedClauses) == 0))
        {
            return null;
        }

        if ((GoRuntime.Len(retainedClauses) == GoRuntime.Len(clause.Types.Nodes)))
        {
            return this.Visitor().VisitEachChild(clause.AsNode());
        }

        return this.Factory().UpdateHeritageClause(clause, clause.Token, this.Visitor().VisitNodes(this.Factory().NewNodeList(retainedClauses)));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:755:1.
    private Node transformImportTypeNode(ImportTypeNode input)
    {
        if (!Ast.IsLiteralImportTypeNode(input.AsNode()))
        {
            return input.AsNode();
        }

        return this.Factory().UpdateImportTypeNode(input, input.IsTypeOf, this.Factory().UpdateLiteralTypeNode(input.Argument.AsLiteralTypeNode(), this.rewriteModuleSpecifier(input.AsNode(), input.Argument.AsLiteralTypeNode().Literal)), input.Attributes, input.Qualifier, this.Visitor().VisitNodes(input.TypeArguments));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:772:1.
    private Node transformConstructorTypeNode(ConstructorTypeNode input)
    {
        return this.Factory().UpdateConstructorTypeNode(input, this.ensureModifiers(input.AsNode()), this.Visitor().VisitNodes(input.TypeParameters), this.updateParamList(input.AsNode(), input.Parameters), this.Visitor().Visit(input.Type));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:782:1.
    private Node transformFunctionTypeNode(FunctionTypeNode input)
    {
        return this.Factory().UpdateFunctionTypeNode(input, this.Visitor().VisitNodes(input.TypeParameters), this.updateParamList(input.AsNode(), input.Parameters), this.Visitor().Visit(input.Type));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:791:1.
    private Node transformConditionalTypeNode(ConditionalTypeNode input)
    {
        var checkType = this.Visitor().Visit(input.CheckType);
        var extendsType = this.Visitor().Visit(input.ExtendsType);
        var oldEnclosingDecl = this.enclosingDeclaration;
        this.enclosingDeclaration = input.TrueType;
        var trueType = this.Visitor().Visit(input.TrueType);
        this.enclosingDeclaration = oldEnclosingDecl;
        var falseType = this.Visitor().Visit(input.FalseType);
        return this.Factory().UpdateConditionalTypeNode(input, checkType, extendsType, trueType, falseType);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:809:1.
    private Node transformTypeReference(TypeReferenceNode input)
    {
        this.checkEntityNameVisibility(input.TypeName, this.enclosingDeclaration);
        return this.Visitor().VisitEachChild(input.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:814:1.
    private Node transformExpressionWithTypeArguments(ExpressionWithTypeArguments input)
    {
        if ((Ast.IsEntityName(input.Expression) || Ast.IsEntityNameExpression(input.Expression)))
        {
            this.checkEntityNameVisibility(input.Expression, this.enclosingDeclaration);
        }

        return this.Visitor().VisitEachChild(input.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:821:1.
    private Node transformTypeParameterDeclaration(TypeParameterDeclaration input)
    {
        if ((IsPrivateMethodTypeParameter(this.resolver, input) && (((input.DefaultType != null) || (input.Constraint != null)))))
        {
            return this.Factory().UpdateTypeParameterDeclaration(input, input.Modifiers(), input.Name(), null, input.Expression, null);
        }

        return this.Visitor().VisitEachChild(input.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:835:1.
    private Node transformVariableDeclaration(VariableDeclaration input)
    {
        if (((this.state.currentSourceFile.CommonJSModuleIndicator != null) && Ast.IsVariableDeclarationInitializedToRequire(input.AsNode())))
        {
            return this.transformCjsRequireVariableDeclaration(input);
        }

        if ((Ast.IsBindingPattern(input.Name()) && hasAnyBindingInitializers(input.Name().AsBindingPattern())))
        {
            return this.recreateBindingPattern(input.Name().AsBindingPattern());
        }

        this.suppressNewDiagnosticContexts = true;
        return this.Factory().UpdateVariableDeclaration(input, this.bindingNameVisitor.VisitNode(input.Name()), null, this.ensureType(input.AsNode(), false), this.ensureNoInitializer(input.AsNode()));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:853:1.
    private static bool hasAnyBindingInitializers(BindingPattern bindingPattern)
    {
        {
            foreach (var elem in GoRuntime.RangeValues(bindingPattern.Elements.Nodes))
            {
                if (!Ast.IsBindingElement(elem))
                {
                    continue;
                }

                var e = elem.AsBindingElement();
                if ((e.Initializer != null))
                {
                    return true;
                }

                if ((((e.Name() != null) && Ast.IsBindingPattern(e.Name())) && hasAnyBindingInitializers(e.Name().AsBindingPattern())))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:869:1.
    private Node transformCjsRequireVariableDeclaration(VariableDeclaration input)
    {
        var specifier = this.rewriteModuleSpecifier(input.AsNode(), input.Initializer.AsCallExpression().Arguments.Nodes[0]);
        if (Ast.IsIdentifier(input.Name()))
        {
            return this.Factory().NewImportEqualsDeclaration(null, false, input.Name(), this.Factory().NewExternalModuleReference(specifier));
        }
        else
        {
            if (Ast.IsArrayBindingPattern(input.Name()))
            {
                return null;
            }
            else
            {
                var b = input.Name().AsBindingPattern();
                Slice<Node> importSpecifiers = default;
                {
                    foreach (var elem in GoRuntime.RangeValues(b.Elements.Nodes))
                    {
                        if (!Ast.IsIdentifier(elem.Name()))
                        {
                            continue;
                        }

                        importSpecifiers = importSpecifiers.Append(this.Factory().NewImportSpecifier(false, elem.PropertyName(), elem.Name()));
                    }
                }

                return this.Factory().NewImportDeclaration(null, this.Factory().NewImportClause(Kind.Unknown, null, this.Factory().NewNamedImports(this.Factory().NewNodeList(importSpecifiers))), specifier, null);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:901:1.
    private Node recreateBindingPattern(BindingPattern input)
    {
        Slice<Node> results = default;
        {
            foreach (var elem in GoRuntime.RangeValues(input.Elements.Nodes))
            {
                var result = this.recreateBindingElement(elem.AsBindingElement());
                if ((result == null))
                {
                    continue;
                }

                if ((result.Kind == Kind.SyntaxList))
                {
                    results = results.Append(result.AsSyntaxList().Children);
                }
                else
                {
                    results = results.Append(result);
                }
            }
        }

        if ((GoRuntime.Len(results) == 0))
        {
            return null;
        }

        if ((GoRuntime.Len(results) == 1))
        {
            return results[0];
        }

        return this.Factory().NewSyntaxList(results);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:923:1.
    private Node recreateBindingElement(BindingElement e)
    {
        if ((e.Name() == null))
        {
            return null;
        }

        if (!GetBindingNameVisible(this.resolver, e.AsNode()))
        {
            return null;
        }

        if (Ast.IsBindingPattern(e.Name()))
        {
            return this.recreateBindingPattern(e.Name().AsBindingPattern());
        }

        return this.Factory().NewVariableDeclaration(e.Name(), null, this.ensureType(e.AsNode(), false), null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:941:1.
    private Node transformIndexSignatureDeclaration(IndexSignatureDeclaration input)
    {
        var t = this.Visitor().Visit(input.Type);
        if ((t == null))
        {
            t = this.Factory().NewKeywordTypeNode(Kind.AnyKeyword);
        }

        return this.Factory().UpdateIndexSignatureDeclaration(input, this.ensureModifiers(input.AsNode()), this.updateParamList(input.AsNode(), input.Parameters), t);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:954:1.
    private Node transformCallSignatureDeclaration(CallSignatureDeclaration input)
    {
        return this.Factory().UpdateCallSignatureDeclaration(input, this.ensureTypeParams(input.AsNode(), input.TypeParameters), this.updateParamList(input.AsNode(), input.Parameters), this.ensureType(input.AsNode(), false));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:963:1.
    private Node transformPropertySignatureDeclaration(PropertySignatureDeclaration input)
    {
        if (Ast.IsPrivateIdentifier(input.Name()))
        {
            return null;
        }

        var result = this.Factory().UpdatePropertySignatureDeclaration(input, this.ensureModifiers(input.AsNode()), input.Name(), input.PostfixToken, this.ensureType(input.AsNode(), false), this.ensureNoInitializer(input.AsNode()));
        this.preservePartialJsDoc(result, input.AsNode());
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:979:1.
    private Node transformPropertyDeclaration(PropertyDeclaration input)
    {
        if (Ast.IsPrivateIdentifier(input.Name()))
        {
            return null;
        }

        var postfixToken = input.PostfixToken;
        if (((postfixToken != null) && (postfixToken.Kind == Kind.ExclamationToken)))
        {
            postfixToken = null;
        }

        return this.Factory().UpdatePropertyDeclaration(input, this.ensureModifiers(input.AsNode()), input.Name(), postfixToken, this.ensureType(input.AsNode(), false), this.ensureNoInitializer(input.AsNode()));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:998:1.
    private Node transformSetAccessorDeclaration(SetAccessorDeclaration input)
    {
        if (Ast.IsPrivateIdentifier(input.Name()))
        {
            return null;
        }

        return this.Factory().UpdateSetAccessorDeclaration(input, this.ensureModifiers(input.AsNode()), input.Name(), null, this.updateAccessorParamList(input.AsNode(), (this.resolver.GetEffectiveDeclarationFlags(this.EmitContext().ParseNode(input.AsNode()), ModifierFlags.Private) != 0)), null, null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1015:1.
    private Node transformGetAccesorDeclaration(GetAccessorDeclaration input)
    {
        if (Ast.IsPrivateIdentifier(input.Name()))
        {
            return null;
        }

        return this.Factory().UpdateGetAccessorDeclaration(input, this.ensureModifiers(input.AsNode()), input.Name(), null, this.updateAccessorParamList(input.AsNode(), (this.resolver.GetEffectiveDeclarationFlags(this.EmitContext().ParseNode(input.AsNode()), ModifierFlags.Private) != 0)), this.ensureType(input.AsNode(), false), null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1031:1.
    private NodeList updateAccessorParamList(Node input, bool isPrivate)
    {
        Slice<Node> newParams = default;
        if (!isPrivate)
        {
            var thisParam = Ast.GetThisParameter(input);
            if ((thisParam != null))
            {
                newParams = newParams.Append(this.ensureParameter(thisParam.AsParameterDeclaration()));
            }
        }

        if (Ast.IsSetAccessorDeclaration(input))
        {
            Node valueParam = default;
            if (!isPrivate)
            {
                if (((GoRuntime.Len(newParams) == 1) && (GoRuntime.Len(input.AsSetAccessorDeclaration().Parameters.Nodes) >= 2)))
                {
                    valueParam = this.ensureParameter(input.AsSetAccessorDeclaration().Parameters.Nodes[1].AsParameterDeclaration());
                }
                else
                {
                    if (((GoRuntime.Len(newParams) == 0) && (GoRuntime.Len(input.AsSetAccessorDeclaration().Parameters.Nodes) >= 1)))
                    {
                        valueParam = this.ensureParameter(input.AsSetAccessorDeclaration().Parameters.Nodes[0].AsParameterDeclaration());
                    }
                }
            }

            if ((valueParam == null))
            {
                Node t = default;
                if (!isPrivate)
                {
                    t = this.Factory().NewKeywordTypeNode(Kind.AnyKeyword);
                }

                valueParam = this.Factory().NewParameterDeclaration(null, null, this.Factory().NewIdentifier("value"), null, t, null);
            }

            newParams = newParams.Append(valueParam);
        }

        return this.Factory().NewNodeList(newParams);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1068:1.
    private Node transformConstructorDeclaration(ConstructorDeclaration input)
    {
        return this.Factory().UpdateConstructorDeclaration(input, this.ensureModifiers(input.AsNode()), null, this.updateParamList(input.AsNode(), input.Parameters), null, null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1081:1.
    private Node transformConstructSignatureDeclaration(ConstructSignatureDeclaration input)
    {
        return this.Factory().UpdateConstructSignatureDeclaration(input, this.ensureTypeParams(input.AsNode(), input.TypeParameters), this.updateParamList(input.AsNode(), input.Parameters), this.ensureType(input.AsNode(), false));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1090:1.
    private Node omitPrivateMethodType(Node input)
    {
        if ((((input.Symbol() != null) && (GoRuntime.Len(input.Symbol().Declarations) > 0)) && (input.Symbol().Declarations[0] != input)))
        {
            return null;
        }

        Node result = default;
        if (Ast.IsMethodSignatureDeclaration(input))
        {
            result = this.Factory().NewPropertySignatureDeclaration(this.ensureModifiers(input), input.Name(), null, null, null);
        }
        else
        {
            result = this.Factory().NewPropertyDeclaration(this.ensureModifiers(input), input.Name(), null, null, null);
        }

        this.preserveJsDoc(result, input);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1116:1.
    private Node transformMethodSignatureDeclaration(MethodSignatureDeclaration input)
    {
        if ((this.resolver.GetEffectiveDeclarationFlags(this.EmitContext().ParseNode(input.AsNode()), ModifierFlags.Private) != 0))
        {
            return this.omitPrivateMethodType(input.AsNode());
        }
        else
        {
            if (Ast.IsPrivateIdentifier(input.Name()))
            {
                return null;
            }
            else
            {
                return this.Factory().UpdateMethodSignatureDeclaration(input, this.ensureModifiers(input.AsNode()), input.Name(), input.PostfixToken, this.ensureTypeParams(input.AsNode(), input.TypeParameters), this.updateParamList(input.AsNode(), input.Parameters), this.ensureType(input.AsNode(), false));
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1134:1.
    private Node transformMethodDeclaration(MethodDeclaration input)
    {
        if ((this.resolver.GetEffectiveDeclarationFlags(this.EmitContext().ParseNode(input.AsNode()), ModifierFlags.Private) != 0))
        {
            return this.omitPrivateMethodType(input.AsNode());
        }
        else
        {
            if (Ast.IsPrivateIdentifier(input.Name()))
            {
                return null;
            }
            else
            {
                return this.Factory().UpdateMethodDeclaration(input, this.ensureModifiers(input.AsNode()), null, input.Name(), input.PostfixToken, this.ensureTypeParams(input.AsNode(), input.TypeParameters), this.updateParamList(input.AsNode(), input.Parameters), this.ensureType(input.AsNode(), false), null, null);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1155:1.
    private Node visitDeclarationStatements(Node input)
    {
        if (this.shouldStripInternal(input))
        {
            return null;
        }

        switch (input.Kind)
        {
            case Kind.ExportDeclaration:
            {
                if (Ast.IsSourceFile(input.Parent))
                {
                    this.resultHasExternalModuleIndicator = true;
                }

                this.resultHasScopeMarker = true;
                return this.Factory().UpdateExportDeclaration(input.AsExportDeclaration(), input.Modifiers(), input.IsTypeOnly(), input.AsExportDeclaration().ExportClause, this.rewriteModuleSpecifier(input, input.ModuleSpecifier()), input.AsExportDeclaration().Attributes);
            }

            case Kind.ExportAssignment:
            {
                return this.transformExportAssignment(input, input, input.Expression(), input.AsExportAssignment().IsExportEquals);
            }

            default:
            {
                var id = Ast.GetNodeId(this.EmitContext().MostOriginal(input));
                if ((this.lateStatementReplacementMap.GetValueOrDefault(id) == null))
                {
                    this.lateStatementReplacementMap[id] = this.transformTopLevelDeclaration(input);
                }

                return input;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1186:1.
    private Node tryGetNameOfAssignedExpression(Node unwrapped)
    {
        Node nameNode = default;
        ByteStringValue nameText = ByteStringValue.FromString("");
        if ((!Ast.IsPropertyAccessExpression(unwrapped) && (unwrapped.Name() != null)))
        {
            nameText = unwrapped.Name().TextValue();
        }
        else
        {
            if (Ast.IsIdentifier(unwrapped))
            {
                nameText = unwrapped.TextValue();
            }
        }

        if (((nameText.Length != 0) && (!nameText.EqualsBytes("default"u8))))
        {
            if (this.resolver.IsNameResolvable(this.enclosingDeclaration, nameText))
            {
                nameNode = this.Factory().NewUniqueNameEx(nameText, new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
            }
            else
            {
                nameNode = this.Factory().NewIdentifierWithTextValue(nameText);
            }
        }

        return nameNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1206:1.
    private Node getNameOfExportedAssignedExpression(Node unwrapped, bool isExportEquals)
    {
        var nameNode = this.tryGetNameOfAssignedExpression(unwrapped);
        if ((nameNode == null))
        {
            if ((isExportEquals && Ast.IsSourceFileJS(this.state.currentSourceFile)))
            {
                nameNode = this.Factory().NewUniqueNameEx("_exports", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
            }
            else
            {
                nameNode = this.Factory().NewUniqueNameEx("_default", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
            }
        }

        this.cjsExportAssignmentName = nameNode;
        return nameNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1221:1.
    private Node transformExportAssignment(Node input, Node assignment, Node expression, bool isExportEquals)
    {
        if (Ast.IsSourceFile(input.Parent))
        {
            this.resultHasExternalModuleIndicator = true;
        }

        this.resultHasScopeMarker = true;
        if ((Ast.IsIdentifier(expression) && ((Ast.IsSourceFile(input.Parent) || Ast.IsModuleBlock(input.Parent)))))
        {
            var exportAssignment = this.Factory().NewExportAssignment(null, isExportEquals, null, expression);
            this.EmitContext().AssignSourceMapRange(exportAssignment, input);
            this.preserveJsDoc(exportAssignment, input);
            return exportAssignment;
        }

        this.state.getSymbolAccessibilityDiagnostic = (_) =>
        {
            return new SymbolAccessibilityDiagnostic(input, Diagnostics.Default_export_of_the_module_has_or_is_using_private_name_0);
        };
        this.tracker.PushErrorFallbackNode(assignment);
        var unwrapped = Ast.SkipOuterExpressions(expression, OuterExpressionKinds.ExpressionTypePassthrough);
        var newId = this.getNameOfExportedAssignedExpression(unwrapped, isExportEquals);
        if (Ast.IsClassExpression(unwrapped))
        {
            Slice<Node> mods = default;
            if (this.needsDeclare)
            {
                mods = mods.Append(this.Factory().NewModifier(Kind.DeclareKeyword));
            }

            var classDecl = this.transformClassExpressionToDeclaration(unwrapped, newId, this.Factory().NewModifierList(mods));
            this.tracker.PopErrorFallbackNode();
            this.preserveJsDoc(classDecl, input);
            var exportAssignment2 = this.Factory().NewExportAssignment(null, isExportEquals, null, newId);
            this.EmitContext().AssignSourceMapRange(exportAssignment2, input);
            this.removeAllComments(exportAssignment2);
            return this.Factory().NewSyntaxList(Slice<Node>.FromArray([exportAssignment2, classDecl]));
        }
        else
        {
            if (Ast.IsFunctionLike(unwrapped))
            {
                Slice<Node> mods2 = default;
                if (this.needsDeclare)
                {
                    mods2 = mods2.Append(this.Factory().NewModifier(Kind.DeclareKeyword));
                }

                var fullSignatureType = assignment.Type();
                var funcDecl = this.transformFunctionLikeToDeclaration(unwrapped, newId, this.Factory().NewModifierList(mods2), fullSignatureType);
                this.tracker.PopErrorFallbackNode();
                this.preserveJsDoc(funcDecl, input);
                var exportAssignment3 = this.Factory().NewExportAssignment(null, isExportEquals, null, newId);
                this.EmitContext().AssignSourceMapRange(exportAssignment3, input);
                this.removeAllComments(exportAssignment3);
                return this.Factory().NewSyntaxList(Slice<Node>.FromArray([exportAssignment3, funcDecl]));
            }
        }

        this.cjsExportAssignmentName = newId;
        Node type_ = default;
        Node initializer = default;
        if (Ast.IsPrimitiveLiteralValue(UnwrapParenthesizedExpression(expression), true))
        {
            initializer = this.resolver.CreateLiteralConstValue(this.EmitContext().ParseNode(assignment), this.tracker);
        }

        if ((initializer == null))
        {
            type_ = this.ensureType(assignment, false);
        }

        var varDecl = this.Factory().NewVariableDeclaration(newId, null, type_, initializer);
        this.tracker.PopErrorFallbackNode();
        ModifierList modList = default;
        if (this.needsDeclare)
        {
            modList = this.Factory().NewModifierList(Slice<Node>.FromArray([this.Factory().NewModifier(Kind.DeclareKeyword)]));
        }
        else
        {
            modList = this.Factory().NewModifierList(Slice<Node>.Make(0));
        }

        var statement = this.Factory().NewVariableStatement(modList, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([varDecl])), NodeFlags.Const));
        var exportAssignment4 = this.Factory().NewExportAssignment(null, isExportEquals, null, newId);
        this.EmitContext().AssignSourceMapRange(exportAssignment4, input);
        this.preserveJsDoc(statement, input);
        return this.Factory().NewSyntaxList(Slice<Node>.FromArray([statement, exportAssignment4]));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1299:1.
    private Node transformFunctionLikeToDeclaration(Node unwrapped, Node funcName, ModifierList mods, Node fullSignatureType)
    {
        var d = unwrapped.FunctionLikeData();
        var sig = d.FullSignature;
        if ((sig == null))
        {
            sig = fullSignatureType;
        }

        if ((sig == null))
        {
            return this.Factory().NewFunctionDeclaration(mods, null, funcName, this.ensureTypeParams(unwrapped, d.TypeParameters), this.updateParamList(unwrapped, d.Parameters), this.ensureType(unwrapped, false), this.Visitor().VisitNode(sig), null);
        }
        else
        {
            return this.Factory().NewVariableStatement(mods, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(funcName, null, this.Visitor().VisitNode(sig), null)])), NodeFlags.Const));
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1325:1.
    private Node transformBinaryExpressionToExportDeclaration(Node input, Node name)
    {
        var propertyName = input.AsBinaryExpression().Right;
        this.tracker.handleSymbolAccessibilityError(this.resolver.IsEntityNameVisible(propertyName, this.enclosingDeclaration));
        if ((Ast.IsIdentifier(name) && (propertyName.TextValue().EqualsValue(name.TextValue()))))
        {
            propertyName = null;
        }

        return this.Factory().NewExportDeclaration(null, false, this.Factory().NewNamedExports(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExportSpecifier(false, propertyName, name)]))), null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1344:1.
    private Node transformCommonJSExport(Node input, Node name)
    {
        var res = this.transformCommonJSExportWorker(input, name);
        if ((res == null))
        {
            return res;
        }

        return this.wrapInCJSExportNamespace(res);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1352:1.
    private Node transformCommonJSExportWorker(Node input, Node name)
    {
        ByteStringValue nameText = ByteStringValue.FromString("");
        if ((Ast.IsIdentifier(name) || Ast.IsStringLiteral(name)))
        {
            nameText = name.TextValue();
        }

        if ((this.witnessedCjsExports?.Contains(ByteStringKey.FromValue(nameText)) == true && (nameText.Length != 0)))
        {
            return null;
        }

        (this.witnessedCjsExports ??= new HashSet<ByteStringKey>(ByteStringKey.Comparer)).Add(ByteStringKey.FromValue(nameText));
        this.resultHasExternalModuleIndicator = true;
        this.resultHasScopeMarker = true;
        if (((isCommonJSAliasExport(input) && Ast.IsExpressionStatement(input.Parent)) && Ast.IsSourceFile(input.Parent.Parent)))
        {
            return this.transformBinaryExpressionToExportDeclaration(input, name);
        }

        if (Ast.IsBinaryExpression(input))
        {
            {
                var rhs = UnwrapParenthesizedExpression(input.AsBinaryExpression().Right);
                if (Ast.IsClassExpression(rhs))
                {
                    var ce = rhs.AsClassExpression();
                    var classExprName = ce.Name();
                    var hasExprName = ((classExprName != null) && (classExprName.TextValue().Length > 0));
                    if (hasExprName)
                    {
                        this.tracker.watchedClassSymbol = rhs.Symbol();
                        this.tracker.classSymbolTracked = false;
                        try
                        {
                            var className = this.Factory().NewIdentifierWithTextValue(classExprName.TextValue());
                            var classMods = Slice<Node>.FromArray([this.Factory().NewModifier(Kind.ExportKeyword)]);
                            var classDecl = this.transformClassExpressionToDeclaration(rhs, className, this.Factory().NewModifierList(classMods));
                            this.preserveJsDoc(classDecl, input);
                            var namesDiffer = (!Ast.IsIdentifier(name) || (!classExprName.TextValue().EqualsValue(name.TextValue())));
                            var needsIsolation = (namesDiffer || this.tracker.classSymbolTracked);
                            if (needsIsolation)
                            {
                                var nsName = this.Factory().NewUniqueNameEx("_ns", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
                                Slice<Node> nsMods = default;
                                if (this.needsDeclare)
                                {
                                    nsMods = nsMods.Append(this.Factory().NewModifier(Kind.DeclareKeyword));
                                }

                                var nsDecl = this.Factory().NewModuleDeclaration(this.Factory().NewModifierList(nsMods), Kind.NamespaceKeyword, nsName, null, this.Factory().NewModuleBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([classDecl]))));
                                var aliasBase = ByteStringValue.FromString("_exported");
                                {
                                    var nameText2 = name.TextValue();
                                    if ((Ast.IsIdentifier(name) && ScannerApi.IsIdentifierTextByteString(ByteStringValue.Concat(ByteStringValue.FromString("_"), nameText2), LanguageVariant.Standard)))
                                    {
                                        aliasBase = ByteStringValue.Concat(ByteStringValue.FromString("_"), nameText2);
                                    }
                                }

                                var importAlias = this.Factory().NewUniqueNameEx(aliasBase, new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
                                var qualifiedName = this.Factory().NewQualifiedName(nsName, className);
                                var importDecl = this.Factory().NewImportEqualsDeclaration(null, false, importAlias, qualifiedName);
                                var exportSpecifier = this.Factory().NewExportSpecifier(false, importAlias, name);
                                var exportDecl = this.Factory().NewExportDeclaration(null, false, this.Factory().NewNamedExports(this.Factory().NewNodeList(Slice<Node>.FromArray([exportSpecifier]))), null, null);
                                this.removeAllComments(exportDecl);
                                return this.Factory().NewSyntaxList(Slice<Node>.FromArray([nsDecl, importDecl]).Append(exportDecl));
                            }

                            Slice<Node> mods = default;
                            mods = mods.Append(this.Factory().NewModifier(Kind.ExportKeyword));
                            if (this.needsDeclare)
                            {
                                mods = mods.Append(this.Factory().NewModifier(Kind.DeclareKeyword));
                            }

                            classDecl = this.Factory().UpdateClassDeclaration(classDecl.AsClassDeclaration(), this.Factory().NewModifierList(mods), classDecl.AsClassDeclaration().Name(), classDecl.AsClassDeclaration().TypeParameters, classDecl.AsClassDeclaration().HeritageClauses, classDecl.AsClassDeclaration().Members);
                            return classDecl;
                        }
                        finally
                        {
                            this.tracker.watchedClassSymbol = null;
                            this.tracker.classSymbolTracked = false;
                        }
                    }

                    Slice<Node> mods2 = default;
                    mods2 = mods2.Append(this.Factory().NewModifier(Kind.ExportKeyword));
                    if (this.needsDeclare)
                    {
                        mods2 = mods2.Append(this.Factory().NewModifier(Kind.DeclareKeyword));
                    }

                    var className2 = name;
                    if (!Ast.IsIdentifier(className2))
                    {
                        className2 = this.Factory().NewUniqueNameEx("_class", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
                    }

                    var classDecl2 = this.transformClassExpressionToDeclaration(rhs, className2, this.Factory().NewModifierList(mods2));
                    this.preserveJsDoc(classDecl2, input);
                    if (!Ast.IsIdentifier(name))
                    {
                        var exportDecl2 = this.Factory().NewExportDeclaration(null, false, this.Factory().NewNamedExports(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExportSpecifier(false, className2, name)]))), null, null);
                        this.removeAllComments(exportDecl2);
                        return this.Factory().NewSyntaxList(Slice<Node>.FromArray([classDecl2, exportDecl2]));
                    }

                    return classDecl2;
                }
            }
        }

        if (Ast.IsIdentifier(name))
        {
            if ((name.TextEquals("default"u8)))
            {
                var newId = this.Factory().NewUniqueNameEx("_default", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
                this.state.getSymbolAccessibilityDiagnostic = (_) =>
                {
                    return new SymbolAccessibilityDiagnostic(input, Diagnostics.Default_export_of_the_module_has_or_is_using_private_name_0);
                };
                this.tracker.PushErrorFallbackNode(input);
                var type_ = this.ensureType(input, false);
                var varDecl = this.Factory().NewVariableDeclaration(newId, null, type_, null);
                this.tracker.PopErrorFallbackNode();
                ModifierList modList = default;
                if (this.needsDeclare)
                {
                    modList = this.Factory().NewModifierList(Slice<Node>.FromArray([this.Factory().NewModifier(Kind.DeclareKeyword)]));
                }
                else
                {
                    modList = this.Factory().NewModifierList(Slice<Node>.Make(0));
                }

                var statement = this.Factory().NewVariableStatement(modList, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([varDecl])), NodeFlags.Const));
                var assignment = this.Factory().NewExportAssignment(input.Modifiers(), false, null, newId);
                this.preserveJsDoc(statement, input);
                this.removeAllComments(assignment);
                return this.Factory().NewSyntaxList(Slice<Node>.FromArray([statement, assignment]));
            }
            else
            {
                if (((this.resolver.GetReferencedValueDeclaration(name) == input) || (this.resolver.GetReferencedValueDeclaration(name) == null)))
                {
                    this.tracker.PushErrorFallbackNode(input);
                    var type_2 = this.ensureType(input, false);
                    var varDecl2 = this.Factory().NewVariableDeclaration(name, null, type_2, null);
                    this.tracker.PopErrorFallbackNode();
                    ModifierList modList2 = default;
                    if (this.needsDeclare)
                    {
                        modList2 = this.Factory().NewModifierList(Slice<Node>.FromArray([this.Factory().NewModifier(Kind.ExportKeyword), this.Factory().NewModifier(Kind.DeclareKeyword)]));
                    }
                    else
                    {
                        modList2 = this.Factory().NewModifierList(Slice<Node>.FromArray([this.Factory().NewModifier(Kind.ExportKeyword)]));
                    }

                    return this.Factory().NewVariableStatement(modList2, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([varDecl2])), NodeFlags.None));
                }
            }
        }

        var newId2 = this.Factory().NewUniqueNameEx("_exported", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
        this.state.getSymbolAccessibilityDiagnostic = (_2) =>
        {
            return new SymbolAccessibilityDiagnostic(input, Diagnostics.Default_export_of_the_module_has_or_is_using_private_name_0);
        };
        this.tracker.PushErrorFallbackNode(input);
        var type_3 = this.ensureType(input, false);
        var varDecl3 = this.Factory().NewVariableDeclaration(newId2, null, type_3, null);
        this.tracker.PopErrorFallbackNode();
        ModifierList modList3 = default;
        if (this.needsDeclare)
        {
            modList3 = this.Factory().NewModifierList(Slice<Node>.FromArray([this.Factory().NewModifier(Kind.DeclareKeyword)]));
        }
        else
        {
            modList3 = this.Factory().NewModifierList(Slice<Node>.Make(0));
        }

        var statement2 = this.Factory().NewVariableStatement(modList3, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([varDecl3])), NodeFlags.Const));
        var assignment2 = this.Factory().NewExportDeclaration(null, false, this.Factory().NewNamedExports(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExportSpecifier(false, newId2, name)]))), null, null);
        this.preserveJsDoc(statement2, input);
        this.removeAllComments(assignment2);
        return this.Factory().NewSyntaxList(Slice<Node>.FromArray([statement2, assignment2]));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1537:1.
    private Node wrapInCJSExportNamespace(Node content)
    {
        if ((this.cjsExportAssignmentName == null))
        {
            return content;
        }

        var nsName = this.cjsExportAssignmentName;
        Slice<Node> members = default;
        if ((content.Kind == Kind.SyntaxList))
        {
            members = content.AsSyntaxList().Children;
        }
        else
        {
            members = Slice<Node>.FromArray([content]);
        }

        Slice<Node> nsMods = default;
        if (this.needsDeclare)
        {
            nsMods = nsMods.Append(this.Factory().NewModifier(Kind.DeclareKeyword));
        }

        (members, _) = this.declareStrippingVisitor.VisitSlice(members);
        return this.Factory().NewModuleDeclaration(this.Factory().NewModifierList(nsMods), Kind.NamespaceKeyword, nsName, null, this.Factory().NewModuleBlock(this.Factory().NewNodeList(members)));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1563:1.
    private static bool isCommonJSAliasExport(Node node)
    {
        if ((Ast.IsBinaryExpression(node) && Ast.IsIdentifier(node.AsBinaryExpression().Right)))
        {
            {
                var symbol = node.Symbol();
                if (((symbol != null) && (GoRuntime.Len(symbol.Declarations) == 1)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // transformClassExpressionToDeclaration converts a class expression into a class declaration
    // for use in CJS export declarations (e.g., exports.K = class K {} or module.exports = class Thing {}).
    // This delegates to the shared buildClassMembers helper to stay in sync with transformClassDeclaration.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1575:1.
    private Node transformClassExpressionToDeclaration(Node classExpr, Node className, ModifierList modifiers)
    {
        var previousEnclosingDeclaration = this.enclosingDeclaration;
        this.enclosingDeclaration = classExpr;
        var previousInClassExpressionDeclaration = this.inClassExpressionDeclaration;
        this.inClassExpressionDeclaration = true;
        try
        {
            Slice<Node> extraMembers = default;
            if (Ast.IsInJSFile(classExpr))
            {
                extraMembers = this.collectThisPropertyAssignments(classExpr);
            }

            var members = this.buildClassMembers(classExpr, extraMembers);
            var typeParameters = this.ensureTypeParams(classExpr, classExpr.AsClassExpression().TypeParameters);
            var heritageClauses = this.Visitor().VisitNodes(classExpr.AsClassExpression().HeritageClauses);
            return this.Factory().NewClassDeclaration(modifiers, className, typeParameters, heritageClauses, members);
        }
        finally
        {
            this.enclosingDeclaration = previousEnclosingDeclaration;
            this.inClassExpressionDeclaration = previousInClassExpressionDeclaration;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1602:1.
    private Node rewriteModuleSpecifier(Node parent, Node input)
    {
        if ((input == null))
        {
            return null;
        }

        this.resultHasExternalModuleIndicator = (this.resultHasExternalModuleIndicator || (((parent.Kind != Kind.ModuleDeclaration) && (parent.Kind != Kind.ImportType))));
        return input;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1610:1.
    private void preserveJsDoc(Node updated, Node original)
    {
        this.EmitContext().AssignCommentRange(updated, original);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1615:1.
    private void preservePartialJsDoc(Node updated, Node original)
    {
        if (((original.Flags & NodeFlags.Reparsed) == 0))
        {
            return;
        }

        var jsdoc = Core.FirstOrNil(original.EagerJSDoc(Ast.GetSourceFileOfNode(original)));
        if ((jsdoc == null))
        {
            return;
        }

        var description = ScannerApi.GetTextOfJSDocCommentValue(jsdoc.AsJSDoc().Comment);
        if (description.Length == 0)
            return;
        // transform.go:1627: required owned synthetic comment; description slices remain borrowed.
        var comment = new GoBuilder();
        comment.WriteString("*\n * ");
        int start = 0;
        while (start < description.Length)
        {
            int newline = description.Slice(start).IndexOf('\n');
            if (newline < 0)
                break;
            newline += start;
            comment.WriteString(description.Slice(start, newline));
            comment.WriteString("\n * ");
            start = newline + 1;
        }

        comment.WriteString(description.Slice(start));
        comment.WriteString("\n ");
        this.EmitContext().AddSyntheticLeadingComment(updated, Kind.MultiLineCommentTrivia, comment.String(), true);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1631:1.
    private void removeAllComments(Node node)
    {
        this.EmitContext().AddEmitFlags(node, EmitFlags.NoComments);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1638:1.
    private Node ensureType(Node node, bool ignorePrivate)
    {
        if ((!ignorePrivate && (this.resolver.GetEffectiveDeclarationFlags(this.EmitContext().ParseNode(node), ModifierFlags.Private) != 0)))
        {
            return null;
        }

        if (this.shouldPrintWithInitializer(node))
        {
            return null;
        }

        if ((((!Ast.IsExportAssignment(node) && !Ast.IsBindingElement(node)) && (node.Type() != null)) && ((!Ast.IsParameterDeclaration(node) || !this.resolver.RequiresAddingImplicitUndefined(node, null, this.enclosingDeclaration)))))
        {
            if (this.state.currentSourceFile.IsJS())
            {
                var jsFlags = declarationEmitNodeBuilderFlags;
                if (this.inClassExpressionDeclaration)
                {
                    jsFlags &= ~(NodeBuilderFlags.WriteClassExpressionAsTypeLiteral);
                }

                var res = this.resolver.TryJSTypeNodeToTypeNode(node.Type(), this.enclosingDeclaration, jsFlags, declarationEmitInternalNodeBuilderFlags, this.tracker);
                if ((res != null))
                {
                    return res;
                }
            }
            else
            {
                return this.Visitor().Visit(node.Type());
            }
        }

        var oldErrorNameNode = this.state.errorNameNode;
        this.state.errorNameNode = node.Name();
        GetSymbolAccessibilityDiagnostic oldDiag = default;
        if (!this.suppressNewDiagnosticContexts)
        {
            oldDiag = this.state.getSymbolAccessibilityDiagnostic;
            if (CanProduceDiagnostics(node))
            {
                this.state.getSymbolAccessibilityDiagnostic = CreateGetSymbolAccessibilityDiagnosticForNode(node);
            }
        }

        Node typeNode = default;
        var flags = declarationEmitNodeBuilderFlags;
        if (this.inClassExpressionDeclaration)
        {
            flags &= ~(NodeBuilderFlags.WriteClassExpressionAsTypeLiteral);
        }

        if (Ast.HasInferredType(node))
        {
            typeNode = this.resolver.CreateTypeOfDeclaration(node, this.enclosingDeclaration, flags, declarationEmitInternalNodeBuilderFlags, this.tracker);
        }
        else
        {
            if (Ast.IsFunctionLike(node))
            {
                typeNode = this.resolver.CreateReturnTypeOfSignatureDeclaration(node, this.enclosingDeclaration, flags, declarationEmitInternalNodeBuilderFlags, this.tracker);
            }
            else
            {
                throw new InvalidOperationException("Debug failure. Illegal value: " + node.Kind.String());
            }
        }

        this.state.errorNameNode = oldErrorNameNode;
        if (!this.suppressNewDiagnosticContexts)
        {
            this.state.getSymbolAccessibilityDiagnostic = oldDiag;
        }

        if ((typeNode == null))
        {
            return this.Factory().NewKeywordTypeNode(Kind.AnyKeyword);
        }

        return typeNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1702:1.
    private bool shouldPrintWithInitializer(Node node)
    {
        return ((CanHaveLiteralInitializer(this.resolver, node) && (node.Initializer() != null)) && this.resolver.IsLiteralConstDeclaration(this.EmitContext().MostOriginal(node)));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1706:1.
    private void checkEntityNameVisibility(Node entityName, Node enclosingDeclaration)
    {
        var visibilityResult = this.resolver.IsEntityNameVisible(entityName, enclosingDeclaration);
        this.tracker.handleSymbolAccessibilityError(visibilityResult);
    }

    // Transforms the direct child of a source file into zero or more replacement statements
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1712:1.
    private Node transformTopLevelDeclaration(Node input)
    {
        if ((GoRuntime.Len(this.state.lateMarkedStatements) > 0))
        {
            this.state.lateMarkedStatements = Core.Filter(this.state.lateMarkedStatements, input, static (node, target) => node != target);
        }

        if (this.shouldStripInternal(input))
        {
            return null;
        }

        if ((input.Kind == Kind.ImportEqualsDeclaration))
        {
            return this.transformImportEqualsDeclaration(input.AsImportEqualsDeclaration());
        }

        if (((input.Kind == Kind.ImportDeclaration) || (input.Kind == Kind.JSImportDeclaration)))
        {
            var res = this.transformImportDeclaration(input.AsImportDeclaration());
            if (((res != null) && (res.Kind != Kind.ImportDeclaration)))
            {
                var res2 = res.Clone(this.Factory());
                res2.Kind = Kind.ImportDeclaration;
                return res2;
            }

            return res;
        }

        if ((Ast.IsDeclaration(input) && IsDeclarationAndNotVisible(this.EmitContext(), this.resolver, input)))
        {
            return null;
        }

        if ((Ast.IsFunctionLike(input) && this.resolver.IsImplementationOfOverload(input)))
        {
            return null;
        }

        var original = this.EmitContext().MostOriginal(input);
        var id = Ast.GetNodeId(original);
        var isExpandoHost = this.expandoHosts.TryGetValue(id, out var _);
        var hasDeferredExpandoAssignments = this.deferredExpandoAssignments.TryGetValue(id, out var _2);
        if ((isExpandoHost || hasDeferredExpandoAssignments))
        {
            return this.createFullExpandoBlock(id);
        }

        var previousEnclosingDeclaration = this.enclosingDeclaration;
        if (IsEnclosingDeclaration(input))
        {
            this.enclosingDeclaration = input;
        }

        var canProduceDiagnostic = CanProduceDiagnostics(input);
        var oldDiag = this.state.getSymbolAccessibilityDiagnostic;
        var oldName = this.state.errorNameNode;
        if (canProduceDiagnostic)
        {
            this.state.getSymbolAccessibilityDiagnostic = CreateGetSymbolAccessibilityDiagnosticForNode(input);
        }

        var saveNeedsDeclare = this.needsDeclare;
        Node result = default;
        switch (input.Kind)
        {
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            {
                result = this.transformTypeAliasDeclaration(input.AsTypeAliasDeclaration());
                break;
            }

            case Kind.InterfaceDeclaration:
            {
                result = this.transformInterfaceDeclaration(input.AsInterfaceDeclaration());
                break;
            }

            case Kind.FunctionDeclaration:
            {
                result = this.transformFunctionDeclaration(input.AsFunctionDeclaration());
                break;
            }

            case Kind.ModuleDeclaration:
            {
                result = this.transformModuleDeclaration(input.AsModuleDeclaration());
                break;
            }

            case Kind.ClassDeclaration:
            {
                result = this.transformClassDeclaration(input.AsClassDeclaration());
                break;
            }

            case Kind.VariableStatement:
            {
                result = this.transformVariableStatement(input.AsVariableStatement());
                break;
            }

            case Kind.EnumDeclaration:
            {
                result = this.transformEnumDeclaration(input.AsEnumDeclaration());
                break;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled top-level node in declaration emit: \"" + input.Kind.String() + "\"");
            }
        }

        this.enclosingDeclaration = previousEnclosingDeclaration;
        this.state.getSymbolAccessibilityDiagnostic = oldDiag;
        this.needsDeclare = saveNeedsDeclare;
        this.state.errorNameNode = oldName;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1792:1.
    private Node transformTypeAliasDeclaration(TypeAliasDeclaration input)
    {
        this.needsDeclare = false;
        return this.Factory().UpdateTypeAliasDeclaration(input, this.ensureModifiers(input.AsNode()), input.Name(), this.Visitor().VisitNodes(input.TypeParameters), this.Visitor().Visit(input.Type));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1803:1.
    private Node transformInterfaceDeclaration(InterfaceDeclaration input)
    {
        return this.Factory().UpdateInterfaceDeclaration(input, this.ensureModifiers(input.AsNode()), input.Name(), this.Visitor().VisitNodes(input.TypeParameters), this.Visitor().VisitNodes(input.HeritageClauses), this.Visitor().VisitNodes(input.Members));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1814:1.
    private Node transformFunctionDeclaration(FunctionDeclaration input)
    {
        if (this.resolver.IsExpandoFunctionDeclaration(input.AsNode()))
        {
            this.state.reportExpandoFunctionErrors(input.AsNode());
        }

        return this.Factory().UpdateFunctionDeclaration(input, this.ensureModifiers(input.AsNode()), null, input.Name(), this.ensureTypeParams(input.AsNode(), input.TypeParameters), this.updateParamList(input.AsNode(), input.Parameters), this.ensureType(input.AsNode(), false), null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1831:1.
    private Node transformModuleDeclaration(ModuleDeclaration input)
    {
        var mods = this.ensureModifiers(input.AsNode());
        var saveNeedsDeclare = this.needsDeclare;
        this.needsDeclare = false;
        var inner = input.Body;
        var keyword = input.Keyword;
        if (((keyword != Kind.GlobalKeyword) && (((input.Name() == null) || !Ast.IsStringLiteral(input.Name())))))
        {
            keyword = Kind.NamespaceKeyword;
        }

        var attributes = this.Visitor().Visit(input.Attributes);
        if (((inner != null) && (inner.Kind == Kind.ModuleBlock)))
        {
            var oldNeedsScopeFix = this.needsScopeFixMarker;
            var oldHasScopeFix = this.resultHasScopeMarker;
            this.resultHasScopeMarker = false;
            this.needsScopeFixMarker = false;
            var statements = this.Visitor().VisitNodes(inner.StatementList());
            var lateStatements = this.transformAndReplaceLatePaintedStatements(statements);
            if (((input.Flags & NodeFlags.Ambient) != 0))
            {
                this.needsScopeFixMarker = false;
            }

            if (((!Ast.IsGlobalScopeAugmentation(input.AsNode()) && !this.resultHasScopeMarker) && !HasScopeMarker(lateStatements)))
            {
                if (this.needsScopeFixMarker)
                {
                    lateStatements = this.Factory().NewNodeList(lateStatements.Nodes.Append(createEmptyExports(this.Factory().AsNodeFactory())));
                }
                else
                {
                    lateStatements = this.exportStrippingVisitor.VisitNodes(lateStatements);
                }
            }

            var body = this.Factory().UpdateModuleBlock(inner.AsModuleBlock(), lateStatements);
            this.needsDeclare = saveNeedsDeclare;
            this.needsScopeFixMarker = oldNeedsScopeFix;
            this.resultHasScopeMarker = oldHasScopeFix;
            return this.Factory().UpdateModuleDeclaration(input, mods, keyword, input.Name(), attributes, body);
        }

        if ((inner != null))
        {
            this.Visitor().Visit(inner);
            var original = this.EmitContext().MostOriginal(inner);
            var id = Ast.GetNodeId(original);
            var _ = this.lateStatementReplacementMap.TryGetValue(id, out var body2);
            this.lateStatementReplacementMap.Remove(id);
            return this.Factory().UpdateModuleDeclaration(input, mods, keyword, input.Name(), attributes, body2);
        }

        return this.Factory().UpdateModuleDeclaration(input, mods, keyword, input.Name(), attributes, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1908:1.
    private Node stripExportModifiers(Node statement)
    {
        if ((statement == null))
        {
            return null;
        }

        var parseNode = this.EmitContext().ParseNode(statement);
        if (((Ast.IsImportEqualsDeclaration(statement) || (((parseNode != null) && (this.resolver.GetEffectiveDeclarationFlags(parseNode, ModifierFlags.Default) != 0)))) || !Ast.CanHaveModifiers(statement)))
        {
            return statement;
        }

        var oldFlags = Ast.GetCombinedModifierFlags(statement);
        if (((oldFlags & ModifierFlags.Export) == 0))
        {
            return statement;
        }

        var newFlags = (oldFlags & ((ModifierFlags.All ^ ModifierFlags.Export)));
        var modifiers = Ast.CreateModifiersFromModifierFlags(newFlags, this.createModifierCallback);
        return Ast.ReplaceModifiers(this.Factory().AsNodeFactory(), statement, this.Factory().NewModifierList(modifiers));
    }

    // buildClassMembers builds the member list for a class-like node (ClassDeclaration or ClassExpression).
    // It handles parameter properties, private identifiers, late-bound index signatures, and visited members.
    // Extra members (e.g., this-property assignments from JS files) can be passed via extraMembers.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1931:1.
    private NodeList buildClassMembers(Node classNode, Slice<Node> extraMembers)
    {
        var ctor = Ast.GetFirstConstructorWithBody(classNode);
        Slice<Node> parameterProperties = default;
        if ((ctor != null))
        {
            var oldDiag = this.state.getSymbolAccessibilityDiagnostic;
            {
                foreach (var param in GoRuntime.RangeValues(ctor.AsConstructorDeclaration().Parameters.Nodes))
                {
                    if ((!Ast.HasSyntacticModifier(param, ModifierFlags.ParameterPropertyModifier) || this.shouldStripInternal(param)))
                    {
                        continue;
                    }

                    this.state.getSymbolAccessibilityDiagnostic = CreateGetSymbolAccessibilityDiagnosticForNode(param);
                    if ((param.Name().Kind == Kind.Identifier))
                    {
                        var updated = this.Factory().NewPropertyDeclaration(this.ensureModifiers(param), param.Name(), param.QuestionToken(), this.ensureType(param, false), this.ensureNoInitializer(param));
                        this.preserveJsDoc(updated, param);
                        parameterProperties = parameterProperties.Append(updated);
                    }
                    else
                    {
                        parameterProperties = parameterProperties.Append(this.walkBindingPattern(param.Name().AsBindingPattern(), param));
                    }
                }
            }

            this.state.getSymbolAccessibilityDiagnostic = oldDiag;
        }

        Node privateIdentifier = default;
        if (Core.Some(classNode.ClassLikeData().Members.Nodes, static (member) =>
        {
            return ((member.Name() != null) && Ast.IsPrivateIdentifier(member.Name()));
        }))
        {
            privateIdentifier = this.Factory().NewPropertyDeclaration(null, this.Factory().NewPrivateIdentifier("#private"), null, null, null);
        }

        var lateIndexes = this.resolver.CreateLateBoundIndexSignatures(classNode, this.enclosingDeclaration, declarationEmitNodeBuilderFlags, declarationEmitInternalNodeBuilderFlags, this.tracker);
        var memberNodes = Slice<Node>.Make(0, classNode.ClassLikeData().Members.Nodes.Length);
        if ((privateIdentifier != null))
        {
            memberNodes = memberNodes.Append(privateIdentifier);
        }

        memberNodes = memberNodes.Append(lateIndexes);
        memberNodes = memberNodes.Append(parameterProperties);
        memberNodes = memberNodes.Append(extraMembers);
        var visitResult = this.Visitor().VisitNodes(classNode.ClassLikeData().Members);
        if (((visitResult != null) && (GoRuntime.Len(visitResult.Nodes) > 0)))
        {
            memberNodes = memberNodes.Append(visitResult.Nodes);
        }

        return this.Factory().NewNodeList(memberNodes);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:1990:1.
    private Node transformClassDeclaration(ClassDeclaration input)
    {
        var previousEnclosingDeclaration = this.enclosingDeclaration;
        this.enclosingDeclaration = input.AsNode();
        try
        {
            this.state.errorNameNode = input.Name();
            this.tracker.PushErrorFallbackNode(input.AsNode());
            try
            {
                var modifiers = this.ensureModifiers(input.AsNode());
                var typeParameters = this.ensureTypeParams(input.AsNode(), input.TypeParameters);
                Slice<Node> extraMembers = default;
                if (Ast.IsInJSFile(input.AsNode()))
                {
                    extraMembers = this.collectThisPropertyAssignments(input.AsNode());
                }

                var members = this.buildClassMembers(input.AsNode(), extraMembers);
                var extendsClause = GetEffectiveBaseTypeNode(input.AsNode());
                if ((((extendsClause != null) && !Ast.IsEntityNameExpression(extendsClause.AsExpressionWithTypeArguments().Expression)) && (extendsClause.AsExpressionWithTypeArguments().Expression.Kind != Kind.NullKeyword)))
                {
                    this.tracker.ReportInferenceFallback(extendsClause.AsExpressionWithTypeArguments().Expression);
                    var oldId = ByteStringValue.FromString("default");
                    if (((Ast.NodeIsPresent(input.Name()) && Ast.IsIdentifier(input.Name())) && (input.Name().TextValue().Length > 0)))
                    {
                        oldId = input.Name().TextValue();
                    }

                    var newId = this.Factory().NewUniqueNameEx(ByteStringValue.Concat(oldId, ByteStringValue.FromString("_base")), new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
                    this.state.getSymbolAccessibilityDiagnostic = (_) =>
                    {
                        return new SymbolAccessibilityDiagnostic(extendsClause, Diagnostics.X_extends_clause_of_exported_class_0_has_or_is_using_private_name_1, input.Name());
                    };
                    var varDecl = this.Factory().NewVariableDeclaration(newId, null, this.resolver.CreateTypeOfExpression(extendsClause.Expression(), input.AsNode(), declarationEmitNodeBuilderFlags, declarationEmitInternalNodeBuilderFlags, this.tracker), null);
                    ModifierList mods = default;
                    if (this.needsDeclare)
                    {
                        mods = this.Factory().NewModifierList(Slice<Node>.FromArray([this.Factory().NewModifier(Kind.DeclareKeyword)]));
                    }

                    var statement = this.Factory().NewVariableStatement(mods, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([varDecl])), NodeFlags.Const));
                    var newHeritageClause = this.Factory().UpdateHeritageClause(extendsClause.Parent.AsHeritageClause(), extendsClause.Parent.AsHeritageClause().Token, this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().UpdateExpressionWithTypeArguments(extendsClause.AsExpressionWithTypeArguments(), newId, this.Visitor().VisitNodes(extendsClause.AsExpressionWithTypeArguments().TypeArguments))])));
                    var retainedHeritageClauses = this.Visitor().VisitNodes(input.HeritageClauses);
                    var heritageList = Slice<Node>.FromArray([newHeritageClause]);
                    if (((retainedHeritageClauses != null) && (GoRuntime.Len(retainedHeritageClauses.Nodes) > 0)))
                    {
                        heritageList = heritageList.Append(retainedHeritageClauses.Nodes);
                    }

                    var heritageClauses = this.Factory().NewNodeList(heritageList);
                    return this.Factory().NewSyntaxList(Slice<Node>.FromArray([statement, this.Factory().UpdateClassDeclaration(input, modifiers, input.Name(), typeParameters, heritageClauses, members)]));
                }

                return this.Factory().UpdateClassDeclaration(input, modifiers, input.Name(), typeParameters, this.Visitor().VisitNodes(input.HeritageClauses), members);
            }
            finally
            {
                this.tracker.PopErrorFallbackNode();
            }
        }
        finally
        {
            this.enclosingDeclaration = previousEnclosingDeclaration;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2084:1.
    private Node visitThisPropertyAssignments(Node node)
    {
        Node thisTarget = default;
        var isStatic = false;
        var thisContainer = Ast.GetThisContainer(node, false, false);
        thisTarget = thisContainer.Parent;
        if ((thisTarget == null))
        {
            return null;
        }

        if ((Ast.HasStaticModifier(thisContainer) || Ast.IsClassStaticBlockDeclaration(thisContainer)))
        {
            isStatic = true;
        }

        if ((thisTarget != this.enclosingDeclaration))
        {
            return null;
        }

        switch (Ast.GetAssignmentDeclarationKind(node))
        {
            case JSDeclarationKind.ThisProperty:
            {
                var name = Ast.GetNameOfDeclaration(node);
                var @base = this.resolver.GetReferencedMemberValueDeclaration(node);
                var key = getThisPropertyAssignmentKey(name, node, isStatic);
                if (((@base == null) || this.seenProperties?.Contains(key) == true))
                {
                    break;
                }

                (this.seenProperties ??= new HashSet<ThisPropertyAssignmentKey>()).Add(key);
                if ((((thisTarget.ClassLikeData().HeritageClauses != null) && (GoRuntime.Len(thisTarget.ClassLikeData().HeritageClauses.Nodes) > 0)) && !isClassExtendingNull(thisTarget)))
                {
                    this.tracker.ReportInferenceFallback(thisTarget);
                    if (this.resolver.IsThisPropertyAssignmentDeclarationRedundant(node))
                    {
                        goto caseBlockEnd;
                    }
                }

                ModifierList mods = default;
                if (isStatic)
                {
                    mods = this.Factory().NewModifierList(Slice<Node>.FromArray([this.Factory().NewModifier(Kind.StaticKeyword)]));
                }

                if (Ast.HasDynamicName(node))
                {
                    if (!Transformers.IsSimpleInlineableExpression(name))
                    {
                        break;
                    }

                    this.checkName(node);
                    name = this.Factory().NewComputedPropertyName(name);
                }

                if ((Ast.TryGetTextValueOfPropertyName(name).Item1.EqualsBytes("constructor"u8)))
                {
                    break;
                }

                if ((Ast.IsIdentifier(name) && !ScannerApi.IsIdentifierTextByteString(name.TextValue(), LanguageVariant.Standard)))
                {
                    name = this.Factory().NewStringLiteralFromNode(name);
                }

                var prop = this.Factory().NewPropertyDeclaration(mods, name, null, this.ensureType(node, false), null);
                if (Ast.IsExpressionStatement(node.Parent))
                {
                    this.preserveJsDoc(prop, node.Parent);
                }

                this.thisPropertyAssignmentsCollected = this.thisPropertyAssignmentsCollected.Append(prop);
                break;
            }
        }

        caseBlockEnd:
            ;
        return this.thisPropertyVisitor.VisitEachChild(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2153:1.
    private static bool isClassExtendingNull(Node node)
    {
        if ((node == null))
        {
            return false;
        }

        var extendsClause = Ast.GetHeritageClause(node, Kind.ExtendsKeyword);
        if ((extendsClause == null))
        {
            return false;
        }

        var types = extendsClause.AsHeritageClause().Types;
        if (((types == null) || (GoRuntime.Len(types.Nodes) != 1)))
        {
            return false;
        }

        var expr = types.Nodes[0].AsExpressionWithTypeArguments().Expression;
        return ((expr != null) && (expr.Kind == Kind.NullKeyword));
    }

    // collectThisPropertyAssignments finds `this.x = expr` assignments in constructors, methods, and static blocks
    // of JS classes and synthesizes PropertyDeclaration nodes for each unique property name.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2171:1.
    private Slice<Node> collectThisPropertyAssignments(Node classNode)
    {
        var members = classNode.ClassLikeData().Members;
        HashSet<ThisPropertyAssignmentKey> seen = null;
        {
            foreach (var member in GoRuntime.RangeValues(members.Nodes))
            {
                if ((member.Name() != null))
                {
                    var isStatic = Ast.IsStatic(member);
                    (seen ??= new HashSet<ThisPropertyAssignmentKey>()).Add(getThisPropertyAssignmentKey(member.Name(), member, isStatic));
                }
            }
        }

        this.seenProperties = seen;
        try
        {
            this.thisPropertyAssignmentsCollected = Slice<Node>.Make(0);
            try
            {
                {
                    foreach (var range19 in GoRuntime.Range(members.Nodes))
                    {
                        var _2 = range19.Key;
                        var n = range19.Value;
                        this.thisPropertyVisitor.VisitEachChild(n);
                    }
                }

                return this.thisPropertyAssignmentsCollected;
            }
            finally
            {
                this.thisPropertyAssignmentsCollected = default;
            }
        }
        finally
        {
            this.seenProperties?.Clear();
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2194:1.
    private Slice<Node> walkBindingPattern(BindingPattern pattern, Node param)
    {
        Slice<Node> elems = default;
        {
            foreach (var elem in GoRuntime.RangeValues(pattern.Elements.Nodes))
            {
                if (Ast.IsOmittedExpression(elem))
                {
                    continue;
                }

                if (Ast.IsBindingPattern(elem.Name()))
                {
                    elems = elems.Append(this.walkBindingPattern(elem.Name().AsBindingPattern(), param));
                    continue;
                }

                elems = elems.Append(this.Factory().NewPropertyDeclaration(this.ensureModifiers(param), elem.Name(), null, this.ensureType(elem, false), null));
            }
        }

        return elems;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2215:1.
    private Node transformVariableStatement(VariableStatement input)
    {
        var visible = false;
        {
            foreach (var decl in GoRuntime.RangeValues(input.DeclarationList.AsVariableDeclarationList().Declarations.Nodes))
            {
                visible = GetBindingNameVisible(this.resolver, decl);
                if (visible)
                {
                    break;
                }
            }
        }

        if (!visible)
        {
            return null;
        }

        var inputNodes = input.DeclarationList.AsVariableDeclarationList().Declarations.Nodes;
        Slice<Node> extraImports = default;
        if ((this.state.currentSourceFile.CommonJSModuleIndicator != null))
        {
            Slice<Node> normalDeclarations = default;
            Slice<Node> imports = default;
            {
                foreach (var range22 in GoRuntime.Range(inputNodes))
                {
                    var _2 = range22.Key;
                    var n = range22.Value;
                    if (Ast.IsVariableDeclarationInitializedToRequire(n))
                    {
                        imports = imports.Append(n);
                    }
                    else
                    {
                        normalDeclarations = normalDeclarations.Append(n);
                    }
                }
            }

            inputNodes = normalDeclarations;
            (extraImports, _) = this.Visitor().VisitSlice(imports);
        }

        var tuple23 = this.Visitor().VisitSlice(inputNodes);
        var nodes = tuple23.Item1;
        if ((GoRuntime.Len(nodes) == 0))
        {
            if ((GoRuntime.Len(extraImports) > 0))
            {
                return this.Factory().NewSyntaxList(extraImports);
            }

            return null;
        }

        var nodeList = this.Factory().NewNodeList(nodes);
        var modifiers = this.ensureModifiers(input.AsNode());
        Node declList = default;
        if ((Ast.IsVarUsing(input.DeclarationList) || Ast.IsVarAwaitUsing(input.DeclarationList)))
        {
            declList = this.Factory().NewVariableDeclarationList(nodeList, NodeFlags.Const);
            this.EmitContext().SetOriginal(declList, input.DeclarationList);
            this.EmitContext().SetCommentRange(declList, input.DeclarationList.Loc);
            declList.Loc = input.DeclarationList.Loc;
        }
        else
        {
            declList = this.Factory().UpdateVariableDeclarationList(input.DeclarationList.AsVariableDeclarationList(), nodeList, input.DeclarationList.Flags);
        }

        var res = this.Factory().UpdateVariableStatement(input, modifiers, declList);
        if ((GoRuntime.Len(extraImports) > 0))
        {
            return this.Factory().NewSyntaxList(extraImports.Append(res));
        }

        return res;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2270:1.
    private Node transformEnumDeclaration(EnumDeclaration input)
    {
        return this.Factory().UpdateEnumDeclaration(input, this.ensureModifiers(input.AsNode()), input.Name(), this.Factory().NewNodeList(Core.MapNonNilSlice(input.Members.Nodes, transformEnumMemberCallback)));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2321:1.
    private ModifierList ensureModifiers(Node node)
    {
        var currentFlags = (Ast.GetCombinedModifierFlags(this.EmitContext().ParseNode(node)) & ModifierFlags.All);
        var newFlags = this.ensureModifierFlags(node);
        if ((currentFlags == newFlags))
        {
            var mods = node.Modifiers();
            if ((mods == null))
            {
                return mods;
            }

            if (CanReuseModifierNodes(mods.Nodes))
            {
                return this.Factory().NewModifierList(Core.Filter(mods.Nodes, Ast.IsModifier));
            }
        }

        var result = Ast.CreateModifiersFromModifierFlags(newFlags, this.createModifierCallback);
        if ((GoRuntime.Len(result) == 0))
        {
            return null;
        }

        return this.Factory().NewModifierList(result);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2341:1.
    private ModifierFlags ensureModifierFlags(Node node)
    {
        var mask = (ModifierFlags.All ^ (((ModifierFlags.Public | ModifierFlags.Async) | ModifierFlags.Override)));
        var additions = ModifierFlags.None;
        if ((this.needsDeclare && !IsAlwaysType(node)))
        {
            additions = ModifierFlags.Ambient;
        }

        var parentIsFile = (node.Parent.Kind == Kind.SourceFile);
        if (!parentIsFile)
        {
            mask ^= ModifierFlags.Ambient;
            additions = ModifierFlags.None;
        }

        if (Ast.IsImplicitlyExportedJSDocDeclaration(node))
        {
            additions |= ModifierFlags.Export;
        }

        return MaskModifierFlags(node, mask, additions);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2358:1.
    private NodeList ensureTypeParams(Node node, NodeList @params)
    {
        if ((this.resolver.GetEffectiveDeclarationFlags(this.EmitContext().ParseNode(node), ModifierFlags.Private) != 0))
        {
            return null;
        }

        NodeList typeParameters = default;
        {
            typeParameters = this.Visitor().VisitNodes(@params);
            if ((typeParameters != null))
            {
                return typeParameters;
            }
        }

        var oldErrorNameNode = this.state.errorNameNode;
        this.state.errorNameNode = node.Name();
        GetSymbolAccessibilityDiagnostic oldDiag = default;
        if (!this.suppressNewDiagnosticContexts)
        {
            oldDiag = this.state.getSymbolAccessibilityDiagnostic;
            if (CanProduceDiagnostics(node))
            {
                this.state.getSymbolAccessibilityDiagnostic = CreateGetSymbolAccessibilityDiagnosticForNode(node);
            }
        }

        {
            var data = node.FunctionLikeData();
            if (((data != null) && (data.FullSignature != null)))
            {
                {
                    var nodes = this.resolver.CreateTypeParametersOfSignatureDeclaration(node, this.enclosingDeclaration, declarationEmitNodeBuilderFlags, declarationEmitInternalNodeBuilderFlags, this.tracker);
                    if ((!nodes.IsNil))
                    {
                        typeParameters = new NodeList
                        {
                            Loc = node.Loc,
                            Nodes = nodes
                        };
                    }
                }
            }
        }

        this.state.errorNameNode = oldErrorNameNode;
        if (!this.suppressNewDiagnosticContexts)
        {
            this.state.getSymbolAccessibilityDiagnostic = oldDiag;
        }

        return typeParameters;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2392:1.
    private NodeList updateParamList(Node node, NodeList @params)
    {
        if (((this.resolver.GetEffectiveDeclarationFlags(this.EmitContext().ParseNode(node), ModifierFlags.Private) != 0) || (GoRuntime.Len(@params.Nodes) == 0)))
        {
            return this.Factory().NewNodeList(Slice<Node>.Make(0));
        }

        var results = Slice<Node>.Make(@params.Nodes.Length);
        {
            foreach (var range24 in GoRuntime.Range(@params.Nodes))
            {
                var i = range24.Key;
                var p = range24.Value;
                results[i] = this.ensureParameter(p.AsParameterDeclaration());
            }
        }

        return this.Factory().NewNodeList(results);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2403:1.
    private Node ensureParameter(ParameterDeclaration p)
    {
        var oldDiag = this.state.getSymbolAccessibilityDiagnostic;
        if (!this.suppressNewDiagnosticContexts)
        {
            this.state.getSymbolAccessibilityDiagnostic = CreateGetSymbolAccessibilityDiagnosticForNode(p.AsNode());
        }

        Node questionToken = default;
        if (this.resolver.IsOptionalParameter(p.AsNode()))
        {
            if ((p.QuestionToken != null))
            {
                questionToken = p.QuestionToken;
            }
            else
            {
                questionToken = this.Factory().NewToken(Kind.QuestionToken);
            }
        }

        var result = this.Factory().UpdateParameterDeclaration(p, null, p.DotDotDotToken, this.bindingNameVisitor.VisitNode(p.Name()), questionToken, this.ensureType(p.AsNode(), true), this.ensureNoInitializer(p.AsNode()));
        this.state.getSymbolAccessibilityDiagnostic = oldDiag;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2429:1.
    private Node ensureNoInitializer(Node node)
    {
        if (this.shouldPrintWithInitializer(node))
        {
            var unwrappedInitializer = UnwrapParenthesizedExpression(node.Initializer());
            if (!Ast.IsPrimitiveLiteralValue(unwrappedInitializer, true))
            {
                this.tracker.ReportInferenceFallback(node);
            }

            return this.resolver.CreateLiteralConstValue(this.EmitContext().ParseNode(node), this.tracker);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2440:1.
    private Node visitBindingName(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
            case Kind.OmittedExpression:
            {
                return node;
            }

            case Kind.ArrayBindingPattern:
            case Kind.ObjectBindingPattern:
            {
                return node.VisitEachChild(this.bindingNameVisitor);
            }

            case Kind.BindingElement:
            {
                if ((((node.PropertyName() != null) && Ast.IsComputedPropertyName(node.PropertyName())) && Ast.IsEntityNameExpression(node.PropertyName().Expression())))
                {
                    this.checkEntityNameVisibility(node.PropertyName().Expression(), this.enclosingDeclaration);
                }

                return this.Factory().UpdateBindingElement(node.AsBindingElement(), node.AsBindingElement().DotDotDotToken, node.PropertyName(), this.bindingNameVisitor.VisitNode(node.Name()), null);
            }

            default:
            {
                return node;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2456:1.
    private Node transformImportEqualsDeclaration(ImportEqualsDeclaration decl)
    {
        if (!this.resolver.IsDeclarationVisible(decl.AsNode()))
        {
            return null;
        }

        if ((decl.ModuleReference.Kind == Kind.ExternalModuleReference))
        {
            var specifier = Ast.GetExternalModuleImportEqualsDeclarationExpression(decl.AsNode());
            return this.Factory().UpdateImportEqualsDeclaration(decl, decl.Modifiers(), decl.IsTypeOnly, decl.Name(), this.Factory().UpdateExternalModuleReference(decl.ModuleReference.AsExternalModuleReference(), this.rewriteModuleSpecifier(decl.AsNode(), specifier)));
        }
        else
        {
            var oldDiag = this.state.getSymbolAccessibilityDiagnostic;
            this.state.getSymbolAccessibilityDiagnostic = CreateGetSymbolAccessibilityDiagnosticForNode(decl.AsNode());
            this.checkEntityNameVisibility(decl.ModuleReference, this.enclosingDeclaration);
            this.state.getSymbolAccessibilityDiagnostic = oldDiag;
            return decl.AsNode();
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2479:1.
    private Node transformImportDeclaration(ImportDeclaration decl)
    {
        if ((decl.ImportClause == null))
        {
            return this.Factory().UpdateImportDeclaration(decl, decl.Modifiers(), decl.ImportClause, this.rewriteModuleSpecifier(decl.AsNode(), decl.ModuleSpecifier), decl.Attributes);
        }

        var phaseModifier = decl.ImportClause.AsImportClause().PhaseModifier;
        if ((phaseModifier == Kind.DeferKeyword))
        {
            phaseModifier = Kind.Unknown;
        }

        Node visibleDefaultBinding = default;
        if ((((decl.ImportClause != null) && (decl.ImportClause.Name() != null)) && this.resolver.IsDeclarationVisible(decl.ImportClause)))
        {
            visibleDefaultBinding = decl.ImportClause.Name();
        }

        if ((decl.ImportClause.AsImportClause().NamedBindings == null))
        {
            if ((visibleDefaultBinding == null))
            {
                return null;
            }

            return this.Factory().UpdateImportDeclaration(decl, decl.Modifiers(), this.Factory().UpdateImportClause(decl.ImportClause.AsImportClause(), phaseModifier, visibleDefaultBinding, null), this.rewriteModuleSpecifier(decl.AsNode(), decl.ModuleSpecifier), decl.Attributes);
        }

        if ((decl.ImportClause.AsImportClause().NamedBindings.Kind == Kind.NamespaceImport))
        {
            Node namedBindings = default;
            if (this.resolver.IsDeclarationVisible(decl.ImportClause.AsImportClause().NamedBindings))
            {
                namedBindings = decl.ImportClause.AsImportClause().NamedBindings;
            }

            if (((visibleDefaultBinding == null) && (namedBindings == null)))
            {
                return null;
            }

            return this.Factory().UpdateImportDeclaration(decl, decl.Modifiers(), this.Factory().UpdateImportClause(decl.ImportClause.AsImportClause(), phaseModifier, visibleDefaultBinding, namedBindings), this.rewriteModuleSpecifier(decl.AsNode(), decl.ModuleSpecifier), decl.Attributes);
        }

        var bindingList = Core.Filter(decl.ImportClause.AsImportClause().NamedBindings.Elements(), this.resolver, static (b, resolver) => resolver.IsDeclarationVisible(b));
        if (((GoRuntime.Len(bindingList) > 0) || (visibleDefaultBinding != null)))
        {
            Node namedImports = default;
            if ((GoRuntime.Len(bindingList) > 0))
            {
                namedImports = this.Factory().UpdateNamedImports(decl.ImportClause.AsImportClause().NamedBindings.AsNamedImports(), this.Factory().NewNodeList(bindingList));
            }

            return this.Factory().UpdateImportDeclaration(decl, decl.Modifiers(), this.Factory().UpdateImportClause(decl.ImportClause.AsImportClause(), phaseModifier, visibleDefaultBinding, namedImports), this.rewriteModuleSpecifier(decl.AsNode(), decl.ModuleSpecifier), decl.Attributes);
        }

        if (this.resolver.IsImportRequiredByAugmentation(decl))
        {
            if (this.state.isolatedDeclarations)
            {
                this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(decl.AsNode(), Diagnostics.Declaration_emit_for_this_file_requires_preserving_this_import_for_augmentations_This_is_not_supported_with_isolatedDeclarations));
            }

            return this.Factory().UpdateImportDeclaration(decl, decl.Modifiers(), null, this.rewriteModuleSpecifier(decl.AsNode(), decl.ModuleSpecifier), decl.Attributes);
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2584:1.
    private Node transformJSDocTypeExpression(JSDocTypeExpression input)
    {
        return this.Visitor().Visit(input.Type);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2588:1.
    private Node transformJSDocTypeLiteral(JSDocTypeLiteral input)
    {
        var tuple25 = this.Visitor().VisitSlice(input.JSDocPropertyTags);
        var members = tuple25.Item1;
        var replacement = this.Factory().NewTypeLiteralNode(this.Factory().NewNodeList(members));
        this.EmitContext().SetOriginal(replacement, input.AsNode());
        return replacement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2595:1.
    private Node transformJSDocPropertyTag(JSDocParameterOrPropertyTag input)
    {
        var replacement = this.Factory().NewPropertySignatureDeclaration(null, this.Visitor().Visit(input.TagName), null, this.Visitor().Visit(input.TypeExpression), null);
        this.EmitContext().SetOriginal(replacement, input.AsNode());
        return replacement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2607:1.
    private Node transformJSDocAllType(JSDocAllType input)
    {
        var replacement = this.Factory().NewKeywordTypeNode(Kind.AnyKeyword);
        this.EmitContext().SetOriginal(replacement, input.AsNode());
        return replacement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2613:1.
    private Node transformJSDocNullableType(JSDocNullableType input)
    {
        var replacement = this.Factory().NewUnionTypeNode(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Visitor().Visit(input.Type), this.Factory().NewLiteralTypeNode(this.Factory().NewKeywordExpression(Kind.NullKeyword))])));
        this.EmitContext().SetOriginal(replacement, input.AsNode());
        return replacement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2622:1.
    private Node transformJSDocNonNullableType(JSDocNonNullableType input)
    {
        return this.Visitor().Visit(input.Type);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2626:1.
    private Node transformJSDocVariadicType(JSDocVariadicType input)
    {
        var replacement = this.Factory().NewArrayTypeNode(this.Visitor().Visit(input.Type));
        this.EmitContext().SetOriginal(replacement, input.AsNode());
        return replacement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2632:1.
    private Node transformJSDocOptionalType(JSDocOptionalType input)
    {
        var replacement = this.Factory().NewUnionTypeNode(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Visitor().Visit(input.Type), this.Factory().NewKeywordTypeNode(Kind.UndefinedKeyword)])));
        this.EmitContext().SetOriginal(replacement, input.AsNode());
        return replacement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2641:1.
    private Node getNameExpressionPreferringIdentifier(Node nameExpr)
    {
        if (Ast.IsNumericLiteral(nameExpr))
        {
            nameExpr = this.Factory().NewStringLiteralWithTextValue(nameExpr.TextValue(), TokenFlags.None);
        }

        if ((Ast.IsStringLiteralLike(nameExpr) && ScannerApi.IsIdentifierTextByteString(nameExpr.TextValue(), LanguageVariant.Standard)))
        {
            var result = this.Factory().NewIdentifierWithTextValue(nameExpr.TextValue());
            var kwKind = ScannerApi.IdentifierToKeywordKind(result.AsIdentifier());
            if (((kwKind == Kind.Unknown) || (kwKind == Kind.DefaultKeyword)))
            {
                result.Parent = nameExpr.Parent;
                result.Flags &= ~(NodeFlags.Synthesized);
                return result;
            }
        }

        return nameExpr;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2661:1.
    private static bool isNotDeclareModifier(Node mod)
    {
        return (mod.Kind != Kind.DeclareKeyword);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2665:1.
    private Node stripDeclareModifiers(Node node)
    {
        if ((node == null))
        {
            return null;
        }

        var mods = node.Modifiers();
        if ((mods != null))
        {
            var flags = node.ModifierFlags();
            if (((flags & ModifierFlags.Ambient) != 0))
            {
                var filtered = Core.Filter(mods.Nodes, isNotDeclareModifier);
                node.AsMutable().SetModifiers(this.Factory().NewModifierList(filtered));
            }
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2680:1.
    private Node visitCJSExportAssignments(Node expression)
    {
        if ((expression != null))
        {
            var tuple26 = this.setupDiagnosticContext(expression);
            var cleanupDiagnosticContext = tuple26.Item2;
            try
            {
                switch (Ast.GetAssignmentDeclarationKind(expression))
                {
                    case JSDeclarationKind.ModuleExports:
                    {
                        if ((this.state.currentSourceFile.CommonJSModuleIndicator != null))
                        {
                            var result = this.transformExportAssignment(expression.Parent, expression, expression.AsBinaryExpression().Right, true);
                            if ((result != null))
                            {
                                this.cjsExportAssignment = result;
                                this.resultHasScopeMarker = true;
                                this.resultHasExternalModuleIndicator = true;
                            }
                        }

                        break;
                    }
                }

                return this.cjsExportAssignmentVisitor.VisitEachChild(expression);
            }
            finally
            {
                cleanupDiagnosticContext.Restore(this);
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2700:1.
    private Node visitNestedExpression(Node expression)
    {
        if ((expression != null))
        {
            var tuple27 = this.setupDiagnosticContext(expression);
            var cleanupDiagnosticContext = tuple27.Item2;
            try
            {
                switch (Ast.GetAssignmentDeclarationKind(expression))
                {
                    case JSDeclarationKind.Property:
                    {
                        this.transformExpandoAssignment(expression.AsBinaryExpression());
                        break;
                    }

                    case JSDeclarationKind.ExportsProperty:
                    {
                        if ((this.state.currentSourceFile.CommonJSModuleIndicator != null))
                        {
                            var result = this.transformCommonJSExport(expression, this.getNameExpressionPreferringIdentifier(Ast.GetElementOrPropertyAccessName(expression.AsBinaryExpression().Left)));
                            if ((result != null))
                            {
                                this.cjsExportMembers = this.cjsExportMembers.Append(result);
                            }
                        }

                        break;
                    }

                    case JSDeclarationKind.ObjectDefinePropertyExports:
                    {
                        if ((this.state.currentSourceFile.CommonJSModuleIndicator != null))
                        {
                            var result2 = this.transformCommonJSExport(expression, this.getNameExpressionPreferringIdentifier(expression.Arguments()[1]));
                            if ((result2 != null))
                            {
                                this.cjsExportMembers = this.cjsExportMembers.Append(result2);
                            }
                        }

                        break;
                    }
                }

                return this.expressionVisitor.VisitEachChild(expression);
            }
            finally
            {
                cleanupDiagnosticContext.Restore(this);
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2727:1.
    private void transformExpandoAssignment(BinaryExpression node)
    {
        var left = node.Left;
        var symbol = node.Symbol;
        if (((symbol == null) || ((symbol.Flags & SymbolFlags.Assignment) == 0)))
        {
            return;
        }

        var ns = Ast.GetLeftmostAccessExpression(left);
        if (((ns == null) || (ns.Kind != Kind.Identifier)))
        {
            return;
        }

        var declaration = this.resolver.GetReferencedValueDeclaration(ns);
        if ((declaration == null))
        {
            return;
        }

        if (this.shouldStripInternal(declaration))
        {
            return;
        }

        if ((Ast.IsVariableDeclaration(declaration) && (declaration.Type() != null)))
        {
            return;
        }

        if ((Ast.IsFunctionDeclaration(declaration) && (declaration.FunctionLikeData().FullSignature != null)))
        {
            return;
        }

        if ((Ast.IsVariableDeclaration(declaration) && !Ast.IsFunctionLike(declaration.Initializer())))
        {
            return;
        }

        var host = declaration.Symbol();
        if ((host == null))
        {
            return;
        }

        var name = this.Factory().NewIdentifierWithTextValue(ns.TextValue());
        var property = this.tryGetPropertyName(left);
        if (((property.Length == 0) || !ScannerApi.IsIdentifierTextByteString(property, LanguageVariant.Standard)))
        {
            return;
        }

        var hostId = this.getExpandoHostId(declaration);
        if ((Ast.IsDeclaration(declaration) && IsDeclarationAndNotVisible(this.EmitContext(), this.resolver, declaration)))
        {
            this.deferredExpandoAssignments[hostId] = this.deferredExpandoAssignments.GetValueOrDefault(hostId).Append(node);
            return;
        }

        if ((Ast.IsFunctionDeclaration(declaration) && !ShouldEmitFunctionProperties(declaration.AsFunctionDeclaration())))
        {
            return;
        }

        this.transformExpandoHost(name, declaration);
        var exportName = this.Factory().NewIdentifierWithTextValue(property);
        var localName = this.tryGetNameOfAssignedExpression(node.AsNode());
        if ((((localName == null) && !this.resolver.IsNameResolvable(this.enclosingDeclaration, property)) && !Ast.IsNonContextualKeyword(ScannerApi.StringToToken(exportName.TextValue()))))
        {
            localName = exportName;
        }

        if (((localName == null) || Ast.IsNonContextualKeyword(ScannerApi.StringToToken(localName.TextValue()))))
        {
            localName = this.Factory().NewGeneratedNameForNode(node.AsNode());
        }

        var tuple28 = this.setupDiagnosticContext(node.AsNode());
        var cleanupDiagnosticContext = tuple28.Item2;
        try
        {
            var preexistingExpandoHasExport = Core.Some(this.expandoMembers.GetValueOrDefault(hostId), Ast.IsExportDeclaration);
            if (Ast.IsIdentifier(node.Right))
            {
                if (!preexistingExpandoHasExport)
                {
                    this.addExportModifierToExpandoMembers(hostId);
                }

                var result = this.transformBinaryExpressionToExportDeclaration(node.AsNode(), exportName);
                this.expandoMembers[hostId] = this.expandoMembers.GetValueOrDefault(hostId).Append(result);
                return;
            }

            ModifierList varModifiers = default;
            if (preexistingExpandoHasExport)
            {
                varModifiers = this.Factory().NewModifierList(Ast.CreateModifiersFromModifierFlags(ModifierFlags.Export, this.createModifierCallback));
            }

            var synthesizedNamespace = this.Factory().NewModuleDeclaration(null, Kind.NamespaceKeyword, name, null, this.Factory().NewModuleBlock(this.Factory().NewNodeList(Slice<Node>.Make(0))));
            synthesizedNamespace.Parent = this.enclosingDeclaration;
            var declarationData = synthesizedNamespace.DeclarationData();
            declarationData.Symbol = host;
            var containerData = synthesizedNamespace.LocalsContainerData();
            containerData.Locals = new SymbolTable();
            containerData.Locals[ByteStringKey.FromValue(localName.TextValue())] = symbol;
            var oldEnclosing = this.enclosingDeclaration;
            this.enclosingDeclaration = synthesizedNamespace;
            try
            {
                var statements = Slice<Node>.FromArray([this.Factory().NewVariableStatement(varModifiers, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(localName, null, this.ensureType(node.AsNode(), false), null)])), NodeFlags.None))]);
                if ((!localName.TextValue().EqualsValue(exportName.TextValue())))
                {
                    var namedExports = this.Factory().NewNamedExports(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExportSpecifier(false, localName, exportName)])));
                    statements = statements.Append(this.Factory().NewExportDeclaration(null, false, namedExports, null, null));
                    if (!preexistingExpandoHasExport)
                    {
                        this.addExportModifierToExpandoMembers(hostId);
                    }
                }

                this.expandoMembers[hostId] = this.expandoMembers.GetValueOrDefault(hostId).Append(statements);
            }
            finally
            {
                this.enclosingDeclaration = oldEnclosing;
            }
        }
        finally
        {
            cleanupDiagnosticContext.Restore(this);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2862:1.
    private void addExportModifierToExpandoMembers(ulong hostId)
    {
        {
            foreach (var decl in GoRuntime.RangeValues(this.expandoMembers.GetValueOrDefault(hostId)))
            {
                var modifierFlags = (ModifierFlags.Export | Ast.GetCombinedModifierFlags(decl));
                decl.AsMutable().SetModifiers(this.Factory().NewModifierList(Ast.CreateModifiersFromModifierFlags(modifierFlags, this.createModifierCallback)));
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2871:1.
    private ulong getExpandoHostId(Node declaration)
    {
        var root = (Ast.IsVariableDeclaration(declaration) ? declaration.Parent.Parent : declaration);
        var id = Ast.GetNodeId(this.EmitContext().MostOriginal(root));
        return id;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2877:1.
    private void transformExpandoHost(Node name, Node declaration)
    {
        var root = (Ast.IsVariableDeclaration(declaration) ? declaration.Parent.Parent : declaration);
        var id = this.getExpandoHostId(declaration);
        {
            var ok = this.expandoHosts.TryGetValue(id, out var _);
            if (ok)
            {
                return;
            }
        }

        var saveNeedsDeclare = this.needsDeclare;
        this.needsDeclare = true;
        var modifierFlags = this.ensureModifierFlags(root);
        var defaultExport = (((modifierFlags & ModifierFlags.Export) != 0) && ((modifierFlags & ModifierFlags.Default) != 0));
        this.needsDeclare = saveNeedsDeclare;
        if (defaultExport)
        {
            modifierFlags |= ModifierFlags.Ambient;
            modifierFlags ^= ModifierFlags.Default;
            modifierFlags ^= ModifierFlags.Export;
        }

        var tuple30 = this.setupDiagnosticContext(declaration);
        var cleanupDiagnosticContext = tuple30.Item2;
        try
        {
            var modifiers = this.Factory().NewModifierList(Ast.CreateModifiersFromModifierFlags(modifierFlags, this.createModifierCallback));
            var replacement = Slice<Node>.Make(0);
            if (Ast.IsFunctionDeclaration(declaration))
            {
                var tuple31 = extractExpandoHostParams(declaration);
                var typeParameters = tuple31.Item1;
                var parameters = tuple31.Item2;
                var asteriskToken = tuple31.Item3;
                replacement = replacement.Append(this.Factory().UpdateFunctionDeclaration(declaration.AsFunctionDeclaration(), modifiers, asteriskToken, declaration.Name(), this.ensureTypeParams(declaration, typeParameters), this.updateParamList(declaration, parameters), this.ensureType(declaration, false), null, null));
            }
            else
            {
                if ((Ast.IsVariableDeclaration(declaration) && Ast.IsFunctionExpressionOrArrowFunction(declaration.Initializer())))
                {
                    var fn = declaration.Initializer();
                    var tuple32 = extractExpandoHostParams(fn);
                    var typeParameters2 = tuple32.Item1;
                    var parameters2 = tuple32.Item2;
                    var asteriskToken2 = tuple32.Item3;
                    replacement = replacement.Append(this.Factory().NewFunctionDeclaration(modifiers, asteriskToken2, this.Factory().NewIdentifierWithTextValue(name.TextValue()), this.ensureTypeParams(fn, typeParameters2), this.updateParamList(fn, parameters2), this.ensureType(fn, false), null, null));
                }
                else
                {
                    this.expandoHosts[id] = this.transformTopLevelDeclaration(declaration);
                    return;
                }
            }

            this.state.reportExpandoFunctionErrors(declaration);
            if (defaultExport)
            {
                if (Ast.IsSourceFile(declaration.Parent))
                {
                    this.resultHasExternalModuleIndicator = true;
                }

                this.resultHasScopeMarker = true;
                replacement = replacement.Append(this.Factory().NewExportAssignment(null, false, null, name));
            }

            this.expandoHosts[id] = this.Factory().NewSyntaxList(replacement);
            {
                var ok2 = this.lateStatementReplacementMap.TryGetValue(id, out var _3);
                if (ok2)
                {
                    this.lateStatementReplacementMap[id] = this.createFullExpandoBlock(id);
                }
            }
        }
        finally
        {
            cleanupDiagnosticContext.Restore(this);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2934:1.
    private Node createFullExpandoBlock(ulong id)
    {
        {
            var ok = this.deferredExpandoAssignments.TryGetValue(id, out var deferred);
            if (ok)
            {
                this.deferredExpandoAssignments.Remove(id);
                {
                    foreach (var assignment in GoRuntime.RangeValues(deferred))
                    {
                        this.transformExpandoAssignment(assignment);
                    }
                }
            }
        }

        var n = this.expandoHosts.GetValueOrDefault(id);
        {
            var ok2 = this.expandoMembers.TryGetValue(id, out var addOns);
            if (ok2)
            {
                ModifierList modifiers = default;
                Node name = default;
                Slice<Node> host = default;
                if (((n != null) && (n.Kind == Kind.SyntaxList)))
                {
                    {
                        foreach (var c in GoRuntime.RangeKeys(n.AsSyntaxList().IterChildren()))
                        {
                            if ((c.Name() != null))
                            {
                                name = c.Name().Clone(this.Factory());
                                if ((c.Modifiers() != null))
                                {
                                    modifiers = c.Modifiers().Clone(this.Factory().AsNodeFactory());
                                }

                                break;
                            }
                        }
                    }

                    host = n.AsSyntaxList().Children;
                }
                else
                {
                    if ((n != null))
                    {
                        name = n.Name().Clone(this.Factory());
                        if ((n.Modifiers() != null))
                        {
                            modifiers = n.Modifiers().Clone(this.Factory().AsNodeFactory());
                        }

                        host = Slice<Node>.FromArray([n]);
                    }
                }

                if ((name != null))
                {
                    var moduleDecl = this.Factory().NewModuleDeclaration(modifiers, Kind.NamespaceKeyword, name, null, this.Factory().NewModuleBlock(this.Factory().NewNodeList(addOns)));
                    var members = host.Append(moduleDecl);
                    return this.Factory().NewSyntaxList(members);
                }
            }
        }

        return n;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2983:1.
    private static (NodeList, NodeList, Node) extractExpandoHostParams(Node node)
    {
        switch (node.Kind)
        {
            case Kind.FunctionExpression:
            {
                var fn = node.AsFunctionExpression();
                return (fn.TypeParameters, fn.Parameters, fn.AsteriskToken);
            }

            case Kind.ArrowFunction:
            {
                var fn2 = node.AsArrowFunction();
                return (fn2.TypeParameters, fn2.Parameters, fn2.AsteriskToken);
            }

            default:
            {
                var fn3 = node.AsFunctionDeclaration();
                return (fn3.TypeParameters, fn3.Parameters, fn3.AsteriskToken);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/transform.go:2997:1.
    private ByteStringValue tryGetPropertyName(Node node)
    {
        if (Ast.IsElementAccessExpression(node))
        {
            return this.resolver.GetElementAccessExpressionNameValue(node.AsElementAccessExpression());
        }

        if (Ast.IsPropertyAccessExpression(node))
        {
            return node.Name().TextValue();
        }

        return ByteStringValue.FromString("");
    }
}
