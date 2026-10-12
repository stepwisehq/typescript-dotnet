// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete commonjsmodule.go counterpart, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.DiagnosticPrinting;
using static TypeScript.Transformers.ModuleTransforms.ModuleTransforms;

namespace TypeScript.Transformers.ModuleTransforms;
internal sealed class CommonJSModuleTransformer : Transformer
{
    private NodeVisitor topLevelVisitor, topLevelNestedVisitor, discardedValueVisitor, assignmentPatternVisitor;
    private CompilerOptions compilerOptions;
    private IReferenceResolver resolver;
    private Func<HasFileName, ModuleKind> getEmitModuleFormatOfFile;
    private ModuleKind moduleKind;
    private ScriptTarget languageVersion;
    private SourceFile currentSourceFile;
    private ExternalModuleInfo currentModuleInfo;
    private Node parentNode, currentNode;
    private readonly CreateAssignmentCallback createAllExportExpressionsCallback;
    private CommonJSModuleTransformer() => createAllExportExpressionsCallback = createAllExportExpressions;
    // commonjsmodule.go:985–1023. Original synchronous local closures, explicit ref state.
    private struct VariableExportState
    {
        internal Slice<Node> statements, variables, expressions;
        internal ModifierList modifiers;
    }

    private void commitPendingVariables(VariableStatement node, ref VariableExportState state)
    {
        if (state.variables.Length == 0)
            return;
        var statement = Factory().UpdateVariableStatement(node, state.modifiers, Factory().UpdateVariableDeclarationList(node.DeclarationList.AsVariableDeclarationList(), Factory().NewNodeList(state.variables), node.DeclarationList.Flags));
        if (state.statements.Length > 0)
            EmitContext().AddEmitFlags(statement, EmitFlags.NoComments);
        state.statements = state.statements.Append(statement);
        state.variables = default;
    }

    private void commitPendingExpressions(VariableStatement node, ref VariableExportState state)
    {
        if (state.expressions.Length == 0)
            return;
        var statement = Factory().NewExpressionStatement(Factory().InlineExpressions(state.expressions));
        EmitContext().AssignCommentAndSourceMapRanges(statement, node.AsNode());
        if (state.statements.Length > 0)
            EmitContext().AddEmitFlags(statement, EmitFlags.NoComments);
        state.statements = state.statements.Append(statement);
        state.expressions = default;
    }

    private void pushVariable(VariableStatement node, ref VariableExportState state, Node variable)
    {
        commitPendingExpressions(node, ref state);
        state.variables = state.variables.Append(variable);
    }

    private void pushExpression(VariableStatement node, ref VariableExportState state, Node expression)
    {
        commitPendingVariables(node, ref state);
        state.expressions = state.expressions.Append(expression);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:32:1.
    public static Transformer NewCommonJSModuleTransformer(TransformOptions opts)
    {
        var compilerOptions = opts.CompilerOptions;
        var emitContext = opts.Context;
        var tx = new CommonJSModuleTransformer
        {
            compilerOptions = compilerOptions,
            resolver = opts.Resolver,
            getEmitModuleFormatOfFile = opts.GetEmitModuleFormatOfFile
        };
        tx.topLevelVisitor = emitContext.NewNodeVisitor(tx.visitTopLevel);
        tx.topLevelNestedVisitor = emitContext.NewNodeVisitor(tx.visitTopLevelNested);
        tx.discardedValueVisitor = emitContext.NewNodeVisitor(tx.visitDiscardedValue);
        tx.assignmentPatternVisitor = emitContext.NewNodeVisitor(tx.visitAssignmentPattern);
        tx.languageVersion = compilerOptions.GetEmitScriptTarget();
        tx.moduleKind = compilerOptions.GetEmitModuleKind();
        return tx.NewTransformer(tx.visit, emitContext);
    }

    // Pushes a new child node onto the ancestor tracking stack, returning the grandparent node to be restored later via `popNode`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:46:1.
    private Node pushNode(Node node)
    {
        Node grandparentNode = default;
        grandparentNode = this.parentNode;
        this.parentNode = this.currentNode;
        this.currentNode = node;
        return grandparentNode;
    }

    // Pops the last child node off the ancestor tracking stack, restoring the grandparent node.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:54:1.
    private void popNode(Node grandparentNode)
    {
        this.currentNode = this.parentNode;
        this.parentNode = grandparentNode;
    }

    // Visits a node at the top level of the source file.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:60:1.
    private Node visitTopLevel(Node node)
    {
        var grandparentNode = this.pushNode(node);
        try
        {
            switch (node.Kind)
            {
                case Kind.ImportDeclaration:
                {
                    node = this.visitTopLevelImportDeclaration(node.AsImportDeclaration());
                    break;
                }

                case Kind.ImportEqualsDeclaration:
                {
                    node = this.visitTopLevelImportEqualsDeclaration(node.AsImportEqualsDeclaration());
                    break;
                }

                case Kind.ExportDeclaration:
                {
                    node = this.visitTopLevelExportDeclaration(node.AsExportDeclaration());
                    break;
                }

                case Kind.ExportAssignment:
                {
                    node = this.visitTopLevelExportAssignment(node.AsExportAssignment());
                    break;
                }

                case Kind.FunctionDeclaration:
                {
                    node = this.visitTopLevelFunctionDeclaration(node.AsFunctionDeclaration());
                    break;
                }

                case Kind.ClassDeclaration:
                {
                    node = this.visitTopLevelClassDeclaration(node.AsClassDeclaration());
                    break;
                }

                case Kind.VariableStatement:
                {
                    node = this.visitTopLevelVariableStatement(node.AsVariableStatement());
                    break;
                }

                default:
                {
                    node = this.visitTopLevelNestedNoStack(node);
                    break;
                }
            }

            return node;
        }
        finally
        {
            this.popNode(grandparentNode);
        }
    }

    // Visits nested elements at the top-level of a module.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:86:1.
    private Node visitTopLevelNested(Node node)
    {
        var grandparentNode = this.pushNode(node);
        try
        {
            return this.visitTopLevelNestedNoStack(node);
        }
        finally
        {
            this.popNode(grandparentNode);
        }
    }

    // Visits nested elements at the top-level of a module without ancestor tracking.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:94:1.
    private Node visitTopLevelNestedNoStack(Node node)
    {
        switch (node.Kind)
        {
            case Kind.VariableStatement:
            {
                node = this.visitTopLevelVariableStatement(node.AsVariableStatement());
                break;
            }

            case Kind.ForStatement:
            {
                node = this.visitTopLevelNestedForStatement(node.AsForStatement());
                break;
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                node = this.visitTopLevelNestedForInOrOfStatement(node.AsForInOrOfStatement());
                break;
            }

            case Kind.DoStatement:
            {
                node = this.visitTopLevelNestedDoStatement(node.AsDoStatement());
                break;
            }

            case Kind.WhileStatement:
            {
                node = this.visitTopLevelNestedWhileStatement(node.AsWhileStatement());
                break;
            }

            case Kind.LabeledStatement:
            {
                node = this.visitTopLevelNestedLabeledStatement(node.AsLabeledStatement());
                break;
            }

            case Kind.WithStatement:
            {
                node = this.visitTopLevelNestedWithStatement(node.AsWithStatement());
                break;
            }

            case Kind.IfStatement:
            {
                node = this.visitTopLevelNestedIfStatement(node.AsIfStatement());
                break;
            }

            case Kind.SwitchStatement:
            {
                node = this.visitTopLevelNestedSwitchStatement(node.AsSwitchStatement());
                break;
            }

            case Kind.CaseBlock:
            {
                node = this.visitTopLevelNestedCaseBlock(node.AsCaseBlock());
                break;
            }

            case Kind.CaseClause:
            case Kind.DefaultClause:
            {
                node = this.visitTopLevelNestedCaseOrDefaultClause(node.AsCaseOrDefaultClause());
                break;
            }

            case Kind.TryStatement:
            {
                node = this.visitTopLevelNestedTryStatement(node.AsTryStatement());
                break;
            }

            case Kind.CatchClause:
            {
                node = this.visitTopLevelNestedCatchClause(node.AsCatchClause());
                break;
            }

            case Kind.Block:
            {
                node = this.visitTopLevelNestedBlock(node.AsBlock());
                break;
            }

            default:
            {
                node = this.visitNoStack(node, false);
                break;
            }
        }

        return node;
    }

    // Visits source elements that are not top-level or top-level nested statements.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:131:1.
    private Node visit(Node node)
    {
        var grandparentNode = this.pushNode(node);
        try
        {
            return this.visitNoStack(node, false);
        }
        finally
        {
            this.popNode(grandparentNode);
        }
    }

    // Visits source elements that are not top-level or top-level nested statements without ancestor tracking.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:139:1.
    private Node visitNoStack(Node node, bool resultIsDiscarded)
    {
        if ((!Ast.IsSourceFile(node) && (((uint)node.SubtreeFacts() & ((SubtreeFactValues.ContainsDynamicImport | SubtreeFactValues.ContainsIdentifier))) == 0)))
        {
            return node;
        }

        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                node = this.visitSourceFile(node.AsSourceFile());
                break;
            }

            case Kind.ForStatement:
            {
                node = this.visitForStatement(node.AsForStatement());
                break;
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                node = this.visitForInOrOfStatement(node.AsForInOrOfStatement());
                break;
            }

            case Kind.ExpressionStatement:
            {
                node = this.visitExpressionStatement(node.AsExpressionStatement());
                break;
            }

            case Kind.VoidExpression:
            {
                node = this.visitVoidExpression(node.AsVoidExpression());
                break;
            }

            case Kind.ParenthesizedExpression:
            {
                node = this.visitParenthesizedExpression(node.AsParenthesizedExpression(), resultIsDiscarded);
                break;
            }

            case Kind.PartiallyEmittedExpression:
            {
                node = this.visitPartiallyEmittedExpression(node.AsPartiallyEmittedExpression(), resultIsDiscarded);
                break;
            }

            case Kind.CallExpression:
            {
                node = this.visitCallExpression(node.AsCallExpression());
                break;
            }

            case Kind.TaggedTemplateExpression:
            {
                node = this.visitTaggedTemplateExpression(node.AsTaggedTemplateExpression());
                break;
            }

            case Kind.BinaryExpression:
            {
                node = this.visitBinaryExpression(node.AsBinaryExpression(), resultIsDiscarded);
                break;
            }

            case Kind.PrefixUnaryExpression:
            {
                node = this.visitPrefixUnaryExpression(node.AsPrefixUnaryExpression(), resultIsDiscarded);
                break;
            }

            case Kind.PostfixUnaryExpression:
            {
                node = this.visitPostfixUnaryExpression(node.AsPostfixUnaryExpression(), resultIsDiscarded);
                break;
            }

            case Kind.ShorthandPropertyAssignment:
            {
                node = this.visitShorthandPropertyAssignment(node.AsShorthandPropertyAssignment());
                break;
            }

            case Kind.Identifier:
            {
                node = this.visitIdentifier(node);
                break;
            }

            default:
            {
                node = this.Visitor().VisitEachChild(node);
                break;
            }
        }

        return node;
    }

    // Visits source elements whose value is discarded if they are expressions.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:182:1.
    private Node visitDiscardedValue(Node node)
    {
        var grandparentNode = this.pushNode(node);
        try
        {
            return this.visitNoStack(node, true);
        }
        finally
        {
            this.popNode(grandparentNode);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:189:1.
    private Node visitAssignmentPattern(Node node)
    {
        var grandparentNode = this.pushNode(node);
        try
        {
            return this.visitAssignmentPatternNoStack(node);
        }
        finally
        {
            this.popNode(grandparentNode);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:196:1.
    private Node visitAssignmentPatternNoStack(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ObjectLiteralExpression:
            case Kind.ArrayLiteralExpression:
            {
                node = this.assignmentPatternVisitor.VisitEachChild(node);
                break;
            }

            case Kind.PropertyAssignment:
            {
                node = this.visitAssignmentProperty(node.AsPropertyAssignment());
                break;
            }

            case Kind.ShorthandPropertyAssignment:
            {
                node = this.visitShorthandAssignmentProperty(node.AsShorthandPropertyAssignment());
                break;
            }

            case Kind.SpreadAssignment:
            {
                node = this.visitAssignmentRestProperty(node.AsSpreadAssignment());
                break;
            }

            case Kind.SpreadElement:
            {
                node = this.visitAssignmentRestElement(node.AsSpreadElement());
                break;
            }

            default:
            {
                if (Ast.IsExpression(node))
                {
                    node = this.visitAssignmentElement(node);
                    break;
                }

                node = this.visitNoStack(node, false);
                break;
            }
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:228:1.
    private Node visitSourceFile(SourceFile node)
    {
        if ((node.IsDeclarationFile || !((Ast.IsEffectiveExternalModule(node, this.compilerOptions) || (((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsDynamicImport) != 0)))))
        {
            return node.AsNode();
        }

        this.currentSourceFile = node;
        this.currentModuleInfo = ModuleTransforms.CollectExternalModuleInfo(node, this.compilerOptions, this.EmitContext(), this.resolver);
        var updated = this.transformCommonJSModule(node);
        this.currentSourceFile = null;
        this.currentModuleInfo = null;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:243:1.
    private bool shouldEmitUnderscoreUnderscoreESModule()
    {
        if (((RootedFilePath.FromValue(this.currentSourceFile.FileNameGoValue()).ExtensionIsOneOf(TsPath.SupportedJSExtensionsFlat) && (this.currentSourceFile.CommonJSModuleIndicator != null)) && (((this.currentSourceFile.ExternalModuleIndicator == null) || (this.currentSourceFile.ExternalModuleIndicator.Kind == Kind.SourceFile)))))
        {
            return false;
        }

        if (((this.currentModuleInfo.exportEquals == null) && Ast.IsExternalModule(this.currentSourceFile)))
        {
            return true;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:255:1.
    private Node createUnderscoreUnderscoreESModule()
    {
        var statement = this.Factory().NewExpressionStatement(
            this.Factory().NewCallExpression(
                this.Factory().NewPropertyAccessExpression(
                    this.Factory().NewIdentifier("Object"), null,
                    this.Factory().NewIdentifier("defineProperty"), NodeFlags.None),
                null, null,
                this.Factory().NewNodeList(Slice<Node>.FromArray([
                    this.Factory().NewIdentifier("exports"),
                    this.Factory().NewStringLiteral("__esModule", TokenFlags.None),
                    this.Factory().NewObjectLiteralExpression(
                        this.Factory().NewNodeList(Slice<Node>.FromArray([
                            this.Factory().NewPropertyAssignment(
                                null, this.Factory().NewIdentifier("value"), null, null,
                                this.Factory().NewTrueExpression())])), false)])),
                NodeFlags.None));
        this.EmitContext().SetEmitFlags(statement, EmitFlags.CustomPrologue);
        return statement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:289:1.
    private Node transformCommonJSModule(SourceFile node)
    {
        this.EmitContext().StartVariableEnvironment();
        var tuple1 = this.Factory().SplitStandardPrologue(node.Statements.Nodes);
        var prologue = tuple1.Item1;
        var rest = tuple1.Item2;
        var statements = GoSlices.Clone(prologue);
        var tuple2 = this.Factory().SplitCustomPrologue(rest);
        var custom = tuple2.Item1;
        rest = tuple2.Item2;
        statements = statements.Append(this.topLevelVisitor.VisitSlice(custom).Item1);
        if (this.shouldEmitUnderscoreUnderscoreESModule())
        {
            statements = statements.Append(this.createUnderscoreUnderscoreESModule());
        }

        if ((GoRuntime.Len(this.currentModuleInfo.exportedNames) > 0))
        {
            var chunkSize = 50;
            var l = GoRuntime.Len(this.currentModuleInfo.exportedNames);
            {
                var i = 0;
                for (; (i < l); i += chunkSize)
                {
                    var right = this.Factory().NewVoidZeroExpression();
                    {
                        foreach (var nextId in GoRuntime.RangeValues(this.currentModuleInfo.exportedNames.Slice2(i, Math.Min((i + chunkSize), l))))
                        {
                            Node left = default;
                            if ((nextId.Kind == Kind.StringLiteral))
                            {
                                left = this.Factory().NewElementAccessExpression(this.Factory().NewIdentifier("exports"), null, this.Factory().NewStringLiteralFromNode(nextId), NodeFlags.None);
                            }
                            else
                            {
                                var name = nextId.Clone(this.Factory());
                                this.EmitContext().SetEmitFlags(name, (EmitFlags.NoSourceMap | EmitFlags.NoComments));
                                left = this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("exports"), null, name, NodeFlags.None);
                            }

                            right = this.Factory().NewAssignmentExpression(left, right);
                        }
                    }

                    var statement = this.Factory().NewExpressionStatement(right);
                    this.EmitContext().AddEmitFlags(statement, EmitFlags.CustomPrologue);
                    statements = statements.Append(statement);
                }
            }
        }

        var exportedFunctionsStart = GoRuntime.Len(statements);
        {
            foreach (var f in this.currentModuleInfo.exportedFunctions.Values())
            {
                statements = this.appendExportsOfClassOrFunctionDeclaration(statements, f.AsNode());
            }
        }

        {
            foreach (var range5 in GoRuntime.Range(statements.Slice2(exportedFunctionsStart, statements.Length)))
            {
                var _2 = range5.Key;
                var s = range5.Value;
                this.EmitContext().AddEmitFlags(s, EmitFlags.CustomPrologue);
            }
        }

        (rest, _) = this.topLevelVisitor.VisitSlice(rest);
        statements = statements.Append(rest);
        statements = this.appendExportEqualsIfNeeded(statements);
        statements = this.EmitContext().EndAndMergeVariableEnvironment(statements);
        var statementList = this.Factory().NewNodeList(statements);
        statementList.Loc = node.Statements.Loc;
        var result = this.Factory().UpdateSourceFile(node, statementList, node.EndOfFileToken).AsSourceFile();
        this.EmitContext().AddEmitHelper(result.AsNode(), this.EmitContext().ReadEmitHelpers());
        var externalHelpersImportDeclaration = ModuleTransforms.CreateExternalHelpersImportDeclarationIfNeeded(this.EmitContext(), result, this.compilerOptions, this.getEmitModuleFormatOfFile(node), false, false, false);
        if ((externalHelpersImportDeclaration != null))
        {
            var tuple6 = this.Factory().SplitStandardPrologue(result.Statements.Nodes);
            var prologue2 = tuple6.Item1;
            var rest2 = tuple6.Item2;
            var tuple7 = this.Factory().SplitCustomPrologue(rest2);
            var custom2 = tuple7.Item1;
            rest2 = tuple7.Item2;
            var statements2 = GoSlices.Clone(prologue2);
            statements2 = statements2.Append(custom2);
            statements2 = statements2.Append(this.topLevelVisitor.VisitNode(externalHelpersImportDeclaration));
            statements2 = statements2.Append(rest2);
            var statementList2 = this.Factory().NewNodeList(statements2);
            statementList2.Loc = result.Statements.Loc;
            result = this.Factory().UpdateSourceFile(result, statementList2, node.EndOfFileToken).AsSourceFile();
        }

        return result.AsNode();
    }

    // Adds the down-level representation of `export=` to the statement list if one exists in the source file.
    //
    // - The `statements` parameter is a statement list to which the down-level export statements are to be appended.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:386:1.
    private Slice<Node> appendExportEqualsIfNeeded(Slice<Node> statements)
    {
        if ((this.currentModuleInfo.exportEquals != null))
        {
            var expressionResult = this.visitExportEquals(this.currentModuleInfo.exportEquals);
            if ((expressionResult != null))
            {
                var statement = this.Factory().NewExpressionStatement(this.Factory().NewAssignmentExpression(this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("module"), null, this.Factory().NewIdentifier("exports"), NodeFlags.None), expressionResult));
                this.EmitContext().AssignCommentAndSourceMapRanges(statement, this.currentModuleInfo.exportEquals.AsNode());
                this.EmitContext().AddEmitFlags(statement, EmitFlags.NoComments);
                statements = statements.Append(statement);
            }
        }

        return statements;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:410:1.
    private Node visitExportEquals(ExportAssignment node)
    {
        var grandparentNode = this.pushNode(node.AsNode());
        try
        {
            return this.Visitor().VisitNode(node.Expression);
        }
        finally
        {
            this.popNode(grandparentNode);
        }
    }

    // Appends the exports of an ImportDeclaration to a statement list, returning the statement list.
    //
    //   - The `statements` parameter is a statement list to which the down-level export statements are to be appended.
    //   - The `decl` parameter is the declaration whose exports are to be recorded.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:420:1.
    private Slice<Node> appendExportsOfImportDeclaration(Slice<Node> statements, ImportDeclaration decl)
    {
        if ((this.currentModuleInfo.exportEquals != null))
        {
            return statements;
        }

        var importClause = decl.ImportClause;
        if ((importClause == null))
        {
            return statements;
        }

        var seen = new HashSet<ByteStringKey>(ByteStringKey.Comparer);
        if ((importClause.Name() != null))
        {
            statements = this.appendExportsOfDeclaration(statements, importClause, seen, false);
        }

        var namedBindings = importClause.AsImportClause().NamedBindings;
        if ((namedBindings != null))
        {
            switch (namedBindings.Kind)
            {
                case Kind.NamespaceImport:
                {
                    statements = this.appendExportsOfDeclaration(statements, namedBindings, seen, false);
                    break;
                }

                case Kind.NamedImports:
                {
                    {
                        foreach (var importBinding in GoRuntime.RangeValues(namedBindings.Elements()))
                        {
                            statements = this.appendExportsOfDeclaration(statements, importBinding, seen, true);
                        }
                    }

                    break;
                }
            }
        }

        return statements;
    }

    // Appends the exports of a VariableStatement to a statement list, returning the statement list.
    //
    //   - The `statements` parameter is a statement list to which the down-level export statements are to be appended.
    //   - The `node` parameter is the VariableStatement whose exports are to be recorded.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:455:1.
    private Slice<Node> appendExportsOfVariableStatement(Slice<Node> statements, VariableStatement node)
    {
        return this.appendExportsOfVariableDeclarationList(statements, node.DeclarationList.AsVariableDeclarationList(), false);
    }

    // Appends the exports of a VariableDeclarationList to a statement list, returning the statement list.
    //
    //   - The `statements` parameter is a statement list to which the down-level export statements are to be appended.
    //   - The `node` parameter is the VariableDeclarationList whose exports are to be recorded.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:463:1.
    private Slice<Node> appendExportsOfVariableDeclarationList(Slice<Node> statements, VariableDeclarationList node, bool isForInOrOfInitializer)
    {
        if ((this.currentModuleInfo.exportEquals != null))
        {
            return statements;
        }

        {
            foreach (var decl in GoRuntime.RangeValues(node.Declarations.Nodes))
            {
                statements = this.appendExportsOfBindingElement(statements, decl, isForInOrOfInitializer);
            }
        }

        return statements;
    }

    // Appends the exports of a VariableDeclaration or BindingElement to a statement list, returning the statement list.
    //
    //   - The `statements` parameter is a statement list to which the down-level export statements are to be appended.
    //   - The `decl` parameter is the declaration whose exports are to be recorded.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:479:1.
    private Slice<Node> appendExportsOfBindingElement(Slice<Node> statements, Node decl, bool isForInOrOfInitializer)
    {
        if (((this.currentModuleInfo.exportEquals != null) || (decl.Name() == null)))
        {
            return statements;
        }

        if (Ast.IsBindingPattern(decl.Name()))
        {
            {
                foreach (var element in GoRuntime.RangeValues(decl.Name().Elements()))
                {
                    if (!Ast.IsOmittedExpression(element))
                    {
                        statements = this.appendExportsOfBindingElement(statements, element, isForInOrOfInitializer);
                    }
                }
            }
        }
        else
        {
            if ((!Transformers.IsGeneratedIdentifier(this.EmitContext(), decl.Name()) && (((!Ast.IsVariableDeclaration(decl) || (decl.Initializer() != null)) || isForInOrOfInitializer))))
            {
                statements = this.appendExportsOfDeclaration(statements, decl, null, false);
            }
        }

        return statements;
    }

    // Appends the exports of a ClassDeclaration or FunctionDeclaration to a statement list, returning the statement list.
    //
    //   - The `statements` parameter is a statement list to which the down-level export statements are to be appended.
    //   - The `decl` parameter is the declaration whose exports are to be recorded.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:502:1.
    private Slice<Node> appendExportsOfClassOrFunctionDeclaration(Slice<Node> statements, Node decl)
    {
        if ((this.currentModuleInfo.exportEquals != null))
        {
            return statements;
        }

        var seen = new HashSet<ByteStringKey>(ByteStringKey.Comparer);
        if (Ast.HasSyntacticModifier(decl, ModifierFlags.Export))
        {
            Node exportName = default;
            if (Ast.HasSyntacticModifier(decl, ModifierFlags.Default))
            {
                exportName = this.Factory().NewIdentifier("default");
            }
            else
            {
                exportName = this.Factory().GetDeclarationName(decl);
            }

            var exportValue = this.Factory().GetLocalName(decl);
            statements = this.appendExportStatement(statements, seen, exportName, exportValue, decl.Loc, false, false);
        }

        if ((decl.Name() != null))
        {
            return this.appendExportsOfDeclaration(statements, decl, seen, false);
        }

        return statements;
    }

    // Appends the exports of a declaration to a statement list, returning the statement list.
    //
    //   - The `statements` parameter is a statement list to which the down-level export statements are to be appended.
    //   - The `decl` parameter is the declaration to export.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:531:1.
    private Slice<Node> appendExportsOfDeclaration(Slice<Node> statements, Node decl, HashSet<ByteStringKey> seen, bool liveBinding)
    {
        if ((this.currentModuleInfo.exportEquals != null))
        {
            return statements;
        }

        if ((seen == null))
        {
            seen = new HashSet<ByteStringKey>(ByteStringKey.Comparer);
        }

        {
            var name = decl.Name();
            if ((((this.currentModuleInfo.exportSpecifiers.Len() > 0) && (name != null)) && Ast.IsIdentifier(name)))
            {
                name = this.Factory().GetDeclarationName(decl);
                var exportSpecifiers = this.currentModuleInfo.exportSpecifiers.Get(ByteStringKey.FromValue(name.TextValue()));
                if ((GoRuntime.Len(exportSpecifiers) > 0))
                {
                    var exportValue = this.visitExpressionIdentifier(name);
                    {
                        foreach (var exportSpecifier in GoRuntime.RangeValues(exportSpecifiers))
                        {
                            statements = this.appendExportStatement(statements, seen, exportSpecifier.Name(), exportValue, exportSpecifier.Name().Loc, false, liveBinding);
                        }
                    }
                }
            }
        }

        return statements;
    }

    // Appends the down-level representation of an export to a statement list, returning the statement list.
    //
    //   - The `statements` parameter is a statement list to which the down-level export statements are to be appended.
    //   - The `exportName` parameter is the name of the export.
    //   - The `expression` parameter is the expression to export.
    //   - The `location` parameter is the location to use for source maps and comments for the export.
    //   - The `allowComments` parameter indicates whether to allow comments on the export.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:561:1.
    private Slice<Node> appendExportStatement(Slice<Node> statements, HashSet<ByteStringKey> seen, Node exportName, Node expression, TextRange? location, bool allowComments, bool liveBinding)
    {
        if ((exportName.Kind != Kind.StringLiteral))
        {
            if (seen.Contains(ByteStringKey.FromValue(exportName.TextValue())))
            {
                return statements;
            }

            seen.Add(ByteStringKey.FromValue(exportName.TextValue()));
        }

        statements = statements.Append(this.createExportStatement(exportName, expression, location, allowComments, liveBinding));
        return statements;
    }

    // Creates a call to the current file's export function to export a value.
    //
    //   - The `name` parameter is the bound name of the export.
    //   - The `value` parameter is the exported value.
    //   - The `location` parameter is the location to use for source maps and comments for the export.
    //   - The `allowComments` parameter indicates whether to emit comments for the statement.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:578:1.
    private Node createExportStatement(Node name, Node value, TextRange? location, bool allowComments, bool liveBinding)
    {
        var statement = this.Factory().NewExpressionStatement(this.createExportExpression(name, value, null, liveBinding));
        if ((location != null))
        {
            this.EmitContext().SetCommentRange(statement, location.Value);
        }

        this.EmitContext().AddEmitFlags(statement, EmitFlags.StartOnNewLine);
        if (!allowComments)
        {
            this.EmitContext().AddEmitFlags(statement, EmitFlags.NoComments);
        }

        return statement;
    }

    // Creates a call to the current file's export function to export a value.
    //
    //   - The `name` parameter is the bound name of the export.
    //   - The `value` parameter is the exported value.
    //   - The `location` parameter is the location to use for source maps and comments for the export.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:595:1.
    private Node createExportExpression(Node name, Node value, TextRange? location, bool liveBinding)
    {
        Node expression = default;
        if (liveBinding)
        {
            expression = this.Factory().NewCallExpression(
                this.Factory().NewPropertyAccessExpression(
                    this.Factory().NewIdentifier("Object"), null,
                    this.Factory().NewIdentifier("defineProperty"), NodeFlags.None),
                null, null,
                this.Factory().NewNodeList(Slice<Node>.FromArray([
                    this.Factory().NewIdentifier("exports"),
                    this.Factory().NewStringLiteralFromNode(name),
                    this.Factory().NewObjectLiteralExpression(
                        this.Factory().NewNodeList(Slice<Node>.FromArray([
                            this.Factory().NewPropertyAssignment(
                                null, this.Factory().NewIdentifier("enumerable"), null, null,
                                this.Factory().NewTrueExpression()),
                            this.Factory().NewPropertyAssignment(
                                null, this.Factory().NewIdentifier("get"), null, null,
                                this.Factory().NewFunctionExpression(
                                    null, null, null, null,
                                    this.Factory().NewNodeList(Slice<Node>.Make(0)), null, null,
                                    this.Factory().NewBlock(
                                        this.Factory().NewNodeList(Slice<Node>.FromArray([
                                            this.Factory().NewReturnStatement(value)])), false)))])),
                        false)])),
                NodeFlags.None);
        }
        else
        {
            Node left = default;
            if ((name.Kind == Kind.StringLiteral))
            {
                left = this.Factory().NewElementAccessExpression(this.Factory().NewIdentifier("exports"), null, this.Factory().NewStringLiteralFromNode(name), NodeFlags.None);
            }
            else
            {
                left = this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("exports"), null, name.Clone(this.Factory()), NodeFlags.None);
            }

            expression = this.Factory().NewAssignmentExpression(left, value);
        }

        if ((location != null))
        {
            this.EmitContext().SetCommentRange(expression, location.Value);
        }

        return expression;
    }

    // Creates a `require()` call to import an external module.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:679:1.
    private Node createRequireCall(Node node)
    {
        Slice<Node> args = default;
        var moduleName = ModuleTransforms.GetExternalModuleNameLiteral(this.Factory(), node, this.currentSourceFile, null, null, this.compilerOptions);
        if ((moduleName != null))
        {
            args = args.Append(ModuleTransforms.RewriteModuleSpecifier(this.EmitContext(), moduleName, this.compilerOptions));
        }

        return this.Factory().NewCallExpression(this.Factory().NewIdentifier("require"), null, null, this.Factory().NewNodeList(args), NodeFlags.None);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:694:1.
    private Node getHelperExpressionForExport(ExportDeclaration node, Node innerExpr)
    {
        if (ModuleTransforms.GetExportNeedsImportStarHelper(node))
        {
            return this.Visitor().VisitNode(this.Factory().NewImportStarHelper(innerExpr));
        }

        return innerExpr;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:701:1.
    private Node getHelperExpressionForImport(ImportDeclaration node, Node innerExpr)
    {
        if (ModuleTransforms.GetImportNeedsImportStarHelper(node))
        {
            return this.Visitor().VisitNode(this.Factory().NewImportStarHelper(innerExpr));
        }

        if (ModuleTransforms.GetImportNeedsImportDefaultHelper(node))
        {
            return this.Visitor().VisitNode(this.Factory().NewImportDefaultHelper(innerExpr));
        }

        return innerExpr;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:711:1.
    private Node visitTopLevelImportDeclaration(ImportDeclaration node)
    {
        if ((node.ImportClause == null))
        {
            var statement = this.Factory().NewExpressionStatement(this.createRequireCall(node.AsNode()));
            this.EmitContext().SetOriginal(statement, node.AsNode());
            this.EmitContext().AssignCommentAndSourceMapRanges(statement, node.AsNode());
            return statement;
        }

        Slice<Node> statements = default;
        Slice<Node> variables = default;
        var namespaceDeclaration = Ast.GetNamespaceDeclarationNode(node.AsNode());
        if (((namespaceDeclaration != null) && !Ast.IsDefaultImport(node.AsNode())))
        {
            variables = variables.Append(this.Factory().NewVariableDeclaration(namespaceDeclaration.Name().Clone(this.Factory()), null, null, this.getHelperExpressionForImport(node, this.createRequireCall(node.AsNode()))));
        }
        else
        {
            variables = variables.Append(this.Factory().NewVariableDeclaration(this.Factory().NewGeneratedNameForNode(node.AsNode()), null, null, this.getHelperExpressionForImport(node, this.createRequireCall(node.AsNode()))));
            if (((namespaceDeclaration != null) && Ast.IsDefaultImport(node.AsNode())))
            {
                variables = variables.Append(this.Factory().NewVariableDeclaration(namespaceDeclaration.Name().Clone(this.Factory()), null, null, this.Factory().NewGeneratedNameForNode(node.AsNode())));
            }
        }

        var varStatement = this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(variables), NodeFlags.Const));
        this.EmitContext().SetOriginal(varStatement, node.AsNode());
        this.EmitContext().AssignCommentAndSourceMapRanges(varStatement, node.AsNode());
        statements = statements.Append(varStatement);
        statements = this.appendExportsOfImportDeclaration(statements, node);
        return Transformers.SingleOrMany(statements, this.Factory());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:777:1.
    private Node visitTopLevelImportEqualsDeclaration(ImportEqualsDeclaration node)
    {
        if (!Ast.IsExternalModuleImportEqualsDeclaration(node.AsNode()))
        {
            throw new InvalidOperationException("import= for internal module references should be handled in an earlier transformer.");
        }

        Slice<Node> statements = default;
        if (Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export))
        {
            var statement = this.Factory().NewExpressionStatement(this.createExportExpression(node.Name(), this.createRequireCall(node.AsNode()), node.Loc, false));
            this.EmitContext().SetOriginal(statement, node.AsNode());
            this.EmitContext().AssignCommentAndSourceMapRanges(statement, node.AsNode());
            statements = statements.Append(statement);
        }
        else
        {
            var statement2 = this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(node.Name().Clone(this.Factory()), null, null, this.createRequireCall(node.AsNode()))])), NodeFlags.Const));
            this.EmitContext().SetOriginal(statement2, node.AsNode());
            this.EmitContext().AssignCommentAndSourceMapRanges(statement2, node.AsNode());
            statements = statements.Append(statement2);
        }

        statements = this.appendExportsOfDeclaration(statements, node.AsNode(), null, false);
        return Transformers.SingleOrMany(statements, this.Factory());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:823:1.
    private Node visitTopLevelExportDeclaration(ExportDeclaration node)
    {
        if ((node.ModuleSpecifier == null))
        {
            return null;
        }

        var generatedName = this.Factory().NewGeneratedNameForNode(node.AsNode());
        if (((node.ExportClause != null) && Ast.IsNamedExports(node.ExportClause)))
        {
            Slice<Node> statements = default;
            var varStatement = this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(generatedName, null, null, this.createRequireCall(node.AsNode()))])), NodeFlags.None));
            this.EmitContext().SetOriginal(varStatement, node.AsNode());
            this.EmitContext().AssignCommentAndSourceMapRanges(varStatement, node.AsNode());
            statements = statements.Append(varStatement);
            {
                foreach (var specifier in GoRuntime.RangeValues(node.ExportClause.Elements()))
                {
                    var specifierName = specifier.PropertyNameOrName();
                    var exportNeedsImportDefault = Ast.ModuleExportNameIsDefault(specifierName);
                    Node target = default;
                    if (exportNeedsImportDefault)
                    {
                        target = this.Factory().NewImportDefaultHelper(generatedName);
                    }
                    else
                    {
                        target = generatedName;
                    }

                    Node exportName = default;
                    if (Ast.IsStringLiteral(specifier.Name()))
                    {
                        exportName = this.Factory().NewStringLiteralFromNode(specifier.Name());
                    }
                    else
                    {
                        exportName = this.Factory().GetExportName(specifier.AsNode());
                    }

                    Node exportedValue = default;
                    if (Ast.IsStringLiteral(specifierName))
                    {
                        exportedValue = this.Factory().NewElementAccessExpression(target, null, specifierName, NodeFlags.None);
                    }
                    else
                    {
                        exportedValue = this.Factory().NewPropertyAccessExpression(target, null, specifierName, NodeFlags.None);
                    }

                    var statement = this.Factory().NewExpressionStatement(this.createExportExpression(exportName, exportedValue, null, true));
                    this.EmitContext().SetOriginal(statement, specifier.AsNode());
                    this.EmitContext().AssignCommentAndSourceMapRanges(statement, specifier.AsNode());
                    statements = statements.Append(statement);
                }
            }

            return Transformers.SingleOrMany(statements, this.Factory());
        }

        if ((node.ExportClause != null))
        {
            Node exportName2 = default;
            if (Ast.IsStringLiteral(node.ExportClause.Name()))
            {
                exportName2 = this.Factory().NewStringLiteralFromNode(node.ExportClause.Name());
            }
            else
            {
                exportName2 = node.ExportClause.Name().Clone(this.Factory());
            }

            var statement2 = this.Factory().NewExpressionStatement(this.createExportExpression(exportName2, this.getHelperExpressionForExport(node, this.createRequireCall(node.AsNode())), null, false));
            this.EmitContext().SetOriginal(statement2, node.AsNode());
            this.EmitContext().AssignCommentAndSourceMapRanges(statement2, node.AsNode());
            return statement2;
        }

        var statement3 = this.Factory().NewExpressionStatement(this.Visitor().VisitNode(this.Factory().NewExportStarHelper(this.createRequireCall(node.AsNode()), this.Factory().NewIdentifier("exports"))));
        this.EmitContext().SetOriginal(statement3, node.AsNode());
        this.EmitContext().AssignCommentAndSourceMapRanges(statement3, node.AsNode());
        return statement3;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:926:1.
    private Node visitTopLevelExportAssignment(ExportAssignment node)
    {
        if (node.IsExportEquals)
        {
            return null;
        }

        return this.createExportStatement(this.Factory().NewIdentifier("default"), this.Visitor().VisitNode(node.Expression), node.Loc, true, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:940:1.
    private Node visitTopLevelFunctionDeclaration(FunctionDeclaration node)
    {
        if (Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export))
        {
            return this.Factory().UpdateFunctionDeclaration(node, Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), ~ModifierFlags.ExportDefault), node.AsteriskToken, this.Factory().GetDeclarationName(node.AsNode()), null, this.Visitor().VisitNodes(node.Parameters), null, null, this.Visitor().VisitNode(node.Body));
        }
        else
        {
            return this.Visitor().VisitEachChild(node.AsNode());
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:958:1.
    private Node visitTopLevelClassDeclaration(ClassDeclaration node)
    {
        Slice<Node> statements = default;
        if (Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export))
        {
            statements = statements.Append(this.Factory().UpdateClassDeclaration(node, this.Visitor().VisitModifiers(Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), ~ModifierFlags.ExportDefault)), this.Factory().GetDeclarationName(node.AsNode()), null, this.Visitor().VisitNodes(node.HeritageClauses), this.Visitor().VisitNodes(node.Members)));
        }
        else
        {
            statements = statements.Append(this.Visitor().VisitEachChild(node.AsNode()));
        }

        statements = this.appendExportsOfClassOrFunctionDeclaration(statements, node.AsNode());
        return Transformers.SingleOrMany(statements, this.Factory());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:976:1.
    private Node visitTopLevelVariableStatement(VariableStatement node)
    {
        Slice<Node> statements = default;
        if (Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export))
        {
            VariableExportState state = default;
            {
                foreach (var originalVariable in node.DeclarationList.AsVariableDeclarationList().Declarations.Nodes)
                {
                    var variable2 = originalVariable;
                    var v = variable2.AsVariableDeclaration();
                    if ((Ast.IsIdentifier(v.Name()) && Transformers.IsLocalName(this.EmitContext(), v.Name())))
                    {
                        if ((state.modifiers == null))
                        {
                            state.modifiers = Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), ~ModifierFlags.ExportDefault);
                        }

                        if ((v.Initializer != null))
                        {
                            variable2 = this.Factory().UpdateVariableDeclaration(v, v.Name(), null, null, this.createExportExpression(v.Name(), this.Visitor().VisitNode(v.Initializer), null, false));
                        }

                        pushVariable(node, ref state, variable2);
                    }
                    else
                    {
                        if ((((v.Initializer != null) && !Ast.IsBindingPattern(v.Name())) && (((Ast.IsArrowFunction(v.Initializer) || Ast.IsFunctionExpression(v.Initializer)) || Ast.IsClassExpression(v.Initializer)))))
                        {
                            pushVariable(node, ref state, this.Factory().NewVariableDeclaration(v.Name(), v.ExclamationToken, v.Type, this.Visitor().VisitNode(v.Initializer)));
                            var propertyAccess = this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("exports"), null, v.Name(), NodeFlags.None);
                            this.EmitContext().AssignCommentAndSourceMapRanges(propertyAccess, v.Name());
                            pushExpression(node, ref state, this.Factory().NewAssignmentExpression(propertyAccess, v.Name().Clone(this.Factory())));
                        }
                        else
                        {
                            if (Ast.IsIdentifier(v.Name()))
                            {
                                var expression2 = this.transformInitializedVariable(v);
                                if ((expression2 != null))
                                {
                                    pushExpression(node, ref state, this.Visitor().VisitNode(expression2));
                                }
                            }
                            else
                            {
                                if (Ast.IsBindingPattern(v.Name()))
                                {
                                    var expression3 = this.transformInitializedVariable(v);
                                    if ((expression3 != null))
                                    {
                                        pushExpression(node, ref state, expression3);
                                    }
                                }
                                else
                                {
                                    var expression4 = Transformers.ConvertVariableDeclarationToAssignmentExpression(this.EmitContext(), v);
                                    if ((expression4 != null))
                                    {
                                        pushExpression(node, ref state, this.Visitor().VisitNode(expression4));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            commitPendingVariables(node, ref state);
            commitPendingExpressions(node, ref state);
            statements = this.appendExportsOfVariableStatement(state.statements, node);
            return Transformers.SingleOrMany(statements, this.Factory());
        }

        return this.visitTopLevelNestedVariableStatement(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1110:1.
    private Node transformInitializedVariable(VariableDeclaration node)
    {
        if ((node.Initializer == null))
        {
            return null;
        }

        var name = node.Name();
        if (Ast.IsBindingPattern(name))
        {
            var assignment = Transformers.ConvertVariableDeclarationToAssignmentExpression(this.EmitContext(), node);
            var grandparentNode = this.pushNode(assignment);
            try
            {
                return this.visitDestructuringAssignment(assignment.AsBinaryExpression(), true);
            }
            finally
            {
                this.popNode(grandparentNode);
            }
        }

        var propertyAccess = this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("exports"), null, name, NodeFlags.None);
        this.EmitContext().AssignCommentAndSourceMapRanges(propertyAccess, name);
        return this.Factory().NewAssignmentExpression(propertyAccess, node.Initializer);
    }

    // Visits a top-level nested variable statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1139:1.
    private Node visitTopLevelNestedVariableStatement(VariableStatement node)
    {
        Slice<Node> statements = default;
        statements = statements.Append(this.Visitor().VisitEachChild(node.AsNode()));
        statements = this.appendExportsOfVariableStatement(statements, node);
        return Transformers.SingleOrMany(statements, this.Factory());
    }

    // Visits a top-level nested `for` statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1148:1.
    private Node visitTopLevelNestedForStatement(ForStatement node)
    {
        if ((((node.Initializer != null) && Ast.IsVariableDeclarationList(node.Initializer)) && ((node.Initializer.Flags & NodeFlags.BlockScoped) == 0)))
        {
            var exportStatements = this.appendExportsOfVariableDeclarationList(default, node.Initializer.AsVariableDeclarationList(), false);
            if ((GoRuntime.Len(exportStatements) > 0))
            {
                Slice<Node> statements = default;
                var varDeclList = this.discardedValueVisitor.VisitNode(node.Initializer);
                var varStatement = this.Factory().NewVariableStatement(null, varDeclList);
                statements = statements.Append(varStatement);
                statements = statements.Append(exportStatements);
                var condition = this.Visitor().VisitNode(node.Condition);
                var incrementor = this.discardedValueVisitor.VisitNode(node.Incrementor);
                var body = this.EmitContext().VisitIterationBody(node.Statement, this.topLevelNestedVisitor);
                statements = statements.Append(this.Factory().UpdateForStatement(node, null, condition, incrementor, body));
                return Transformers.SingleOrMany(statements, this.Factory());
            }
        }

        return this.Factory().UpdateForStatement(node, this.discardedValueVisitor.VisitNode(node.Initializer), this.Visitor().VisitNode(node.Condition), this.discardedValueVisitor.VisitNode(node.Incrementor), this.EmitContext().VisitIterationBody(node.Statement, this.topLevelNestedVisitor));
    }

    // Visits a top-level nested `for..in` or `for..of` statement as it may contain `var` declarations that are hoisted and
    // may still be exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1190:1.
    private Node visitTopLevelNestedForInOrOfStatement(ForInOrOfStatement node)
    {
        if ((Ast.IsVariableDeclarationList(node.Initializer) && ((node.Initializer.Flags & NodeFlags.BlockScoped) == 0)))
        {
            var exportStatements = this.appendExportsOfVariableDeclarationList(default, node.Initializer.AsVariableDeclarationList(), true);
            if ((GoRuntime.Len(exportStatements) > 0))
            {
                var initializer = this.discardedValueVisitor.VisitNode(node.Initializer);
                var expression = this.Visitor().VisitNode(node.Expression);
                var body = this.EmitContext().VisitIterationBody(node.Statement, this.topLevelNestedVisitor);
                if (Ast.IsBlock(body))
                {
                    var block = body.AsBlock();
                    var bodyStatements = exportStatements.Append(block.Statements.Nodes);
                    var bodyStatementList = this.Factory().NewNodeList(bodyStatements);
                    bodyStatementList.Loc = block.Statements.Loc;
                    body = this.Factory().UpdateBlock(block, bodyStatementList, block.MultiLine);
                }
                else
                {
                    var bodyStatements2 = exportStatements.Append(body);
                    body = this.Factory().NewBlock(this.Factory().NewNodeList(bodyStatements2), true);
                }

                return this.Factory().UpdateForInOrOfStatement(node, node.AwaitModifier, initializer, expression, body);
            }
        }

        return this.Factory().UpdateForInOrOfStatement(node, node.AwaitModifier, this.discardedValueVisitor.VisitNode(node.Initializer), this.Visitor().VisitNode(node.Expression), this.EmitContext().VisitIterationBody(node.Statement, this.topLevelNestedVisitor));
    }

    // Visits a top-level nested `do` statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1232:1.
    private Node visitTopLevelNestedDoStatement(DoStatement node)
    {
        return this.Factory().UpdateDoStatement(node, this.EmitContext().VisitIterationBody(node.Statement, this.topLevelNestedVisitor), this.Visitor().VisitNode(node.Expression));
    }

    // Visits a top-level nested `while` statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1242:1.
    private Node visitTopLevelNestedWhileStatement(WhileStatement node)
    {
        return this.Factory().UpdateWhileStatement(node, this.Visitor().VisitNode(node.Expression), this.EmitContext().VisitIterationBody(node.Statement, this.topLevelNestedVisitor));
    }

    // Visits a top-level nested labeled statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1252:1.
    private Node visitTopLevelNestedLabeledStatement(LabeledStatement node)
    {
        var statement = this.topLevelNestedVisitor.VisitEmbeddedStatement(node.Statement);
        if ((statement == null))
        {
            statement = this.Factory().NewEmptyStatement();
        }

        return this.Factory().UpdateLabeledStatement(node, node.Label, statement);
    }

    // Visits a top-level nested `with` statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1262:1.
    private Node visitTopLevelNestedWithStatement(WithStatement node)
    {
        var statement = this.topLevelNestedVisitor.VisitEmbeddedStatement(node.Statement);
        if ((statement == null))
        {
            statement = this.Factory().NewEmptyStatement();
        }

        return this.Factory().UpdateWithStatement(node, this.Visitor().VisitNode(node.Expression), statement);
    }

    // Visits a top-level nested `if` statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1276:1.
    private Node visitTopLevelNestedIfStatement(IfStatement node)
    {
        var expression = this.Visitor().VisitNode(node.Expression);
        var thenStatement = this.topLevelNestedVisitor.VisitEmbeddedStatement(node.ThenStatement);
        if ((thenStatement == null))
        {
            thenStatement = this.Factory().NewBlock(this.Factory().NewNodeList(null), false);
        }

        var elseStatement = this.topLevelNestedVisitor.VisitEmbeddedStatement(node.ElseStatement);
        return this.Factory().UpdateIfStatement(node, expression, thenStatement, elseStatement);
    }

    // Visits a top-level nested `switch` statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1288:1.
    private Node visitTopLevelNestedSwitchStatement(SwitchStatement node)
    {
        return this.Factory().UpdateSwitchStatement(node, this.Visitor().VisitNode(node.Expression), this.topLevelNestedVisitor.VisitNode(node.CaseBlock));
    }

    // Visits a top-level nested case block as it may contain `var` declarations that are hoisted and may still be exported
    // with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1298:1.
    private Node visitTopLevelNestedCaseBlock(CaseBlock node)
    {
        return this.topLevelNestedVisitor.VisitEachChild(node.AsNode());
    }

    // Visits a top-level nested `case` or `default` clause as it may contain `var` declarations that are hoisted and may
    // still be exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1304:1.
    private Node visitTopLevelNestedCaseOrDefaultClause(CaseOrDefaultClause node)
    {
        return this.Factory().UpdateCaseOrDefaultClause(node, this.Visitor().VisitNode(node.Expression), this.topLevelNestedVisitor.VisitNodes(node.Statements));
    }

    // Visits a top-level nested `try` statement as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1314:1.
    private Node visitTopLevelNestedTryStatement(TryStatement node)
    {
        return this.topLevelNestedVisitor.VisitEachChild(node.AsNode());
    }

    // Visits a top-level nested `catch` clause as it may contain `var` declarations that are hoisted and may still be
    // exported with `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1320:1.
    private Node visitTopLevelNestedCatchClause(CatchClause node)
    {
        return this.Factory().UpdateCatchClause(node, node.VariableDeclaration, this.topLevelNestedVisitor.VisitNode(node.Block));
    }

    // Visits a top-level nested block as it may contain `var` declarations that are hoisted and may still be exported with
    // `export {}`.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1330:1.
    private Node visitTopLevelNestedBlock(Block node)
    {
        return this.topLevelNestedVisitor.VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1334:1.
    private Node visitForStatement(ForStatement node)
    {
        return this.Factory().UpdateForStatement(node, this.discardedValueVisitor.VisitNode(node.Initializer), this.Visitor().VisitNode(node.Condition), this.discardedValueVisitor.VisitNode(node.Incrementor), this.EmitContext().VisitIterationBody(node.Statement, this.Visitor()));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1344:1.
    private Node visitForInOrOfStatement(ForInOrOfStatement node)
    {
        return this.Factory().UpdateForInOrOfStatement(node, node.AwaitModifier, this.discardedValueVisitor.VisitNode(node.Initializer), this.Visitor().VisitNode(node.Expression), this.EmitContext().VisitIterationBody(node.Statement, this.Visitor()));
    }

    // Visits an expression statement whose value will be discarded at runtime.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1355:1.
    private Node visitExpressionStatement(ExpressionStatement node)
    {
        return this.discardedValueVisitor.VisitEachChild(node.AsNode());
    }

    // Visits a `void` expression whose value will be discarded at runtime.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1360:1.
    private Node visitVoidExpression(VoidExpression node)
    {
        return this.discardedValueVisitor.VisitEachChild(node.AsNode());
    }

    // Visits a parenthesized expression whose value may be discarded at runtime.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1365:1.
    private Node visitParenthesizedExpression(ParenthesizedExpression node, bool resultIsDiscarded)
    {
        var expression = Core.IfElse(resultIsDiscarded, this.discardedValueVisitor, this.Visitor()).VisitNode(node.Expression);
        return this.Factory().UpdateParenthesizedExpression(node, expression);
    }

    // Visits a partially emitted expression whose value may be discarded at runtime.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1371:1.
    private Node visitPartiallyEmittedExpression(PartiallyEmittedExpression node, bool resultIsDiscarded)
    {
        var expression = Core.IfElse(resultIsDiscarded, this.discardedValueVisitor, this.Visitor()).VisitNode(node.Expression);
        return this.Factory().UpdatePartiallyEmittedExpression(node, expression);
    }

    // Visits a binary expression whose value may be discarded, or which might contain an assignment to an exported
    // identifier.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1378:1.
    private Node visitBinaryExpression(BinaryExpression node, bool resultIsDiscarded)
    {
        if (Ast.IsDestructuringAssignment(node.AsNode()))
        {
            return this.visitDestructuringAssignment(node, resultIsDiscarded);
        }

        if (Ast.IsAssignmentExpression(node.AsNode(), false))
        {
            return this.visitAssignmentExpression(node);
        }

        if (Ast.IsCommaExpression(node.AsNode()))
        {
            return this.visitCommaExpression(node, resultIsDiscarded);
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1394:1.
    private Node visitAssignmentExpression(BinaryExpression node)
    {
        if (((Ast.IsIdentifier(node.Left) && ((!Transformers.IsGeneratedIdentifier(this.EmitContext(), node.Left) || ModuleTransforms.IsFileLevelReservedGeneratedIdentifier(this.EmitContext(), node.Left)))) && !Transformers.IsLocalName(this.EmitContext(), node.Left)))
        {
            var exportedNames = this.getExports(node.Left);
            if ((GoRuntime.Len(exportedNames) > 0))
            {
                var expression = this.Visitor().VisitEachChild(node.AsNode());
                {
                    foreach (var exportName in GoRuntime.RangeValues(exportedNames))
                    {
                        expression = this.createExportExpression(exportName, expression, node.Loc, false);
                    }
                }

                return expression;
            }
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Visits a destructuring assignment which might target an exported identifier.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1419:1.
    private Node visitDestructuringAssignment(BinaryExpression node, bool valueIsDiscarded)
    {
        if (this.destructuringNeedsFlattening(node.Left))
        {
            return Transformers.FlattenDestructuringAssignment(this, node.AsNode(), !valueIsDiscarded, FlattenLevel.All, this.createAllExportExpressionsCallback);
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // destructuringNeedsFlattening checks whether a destructuring assignment target contains any
    // exported identifiers that need to be flattened into individual export assignments.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1434:1.
    private bool destructuringNeedsFlattening(Node node)
    {
        if (Ast.IsObjectLiteralExpression(node))
        {
            {
                foreach (var elem in GoRuntime.RangeValues(node.Properties()))
                {
                    switch (elem.Kind)
                    {
                        case Kind.PropertyAssignment:
                        {
                            if (this.destructuringNeedsFlattening(elem.Initializer()))
                            {
                                return true;
                            }

                            break;
                        }

                        case Kind.ShorthandPropertyAssignment:
                        {
                            if (this.destructuringNeedsFlattening(elem.Name()))
                            {
                                return true;
                            }

                            break;
                        }

                        case Kind.SpreadAssignment:
                        {
                            if (this.destructuringNeedsFlattening(elem.Expression()))
                            {
                                return true;
                            }

                            break;
                        }

                        case Kind.MethodDeclaration:
                        case Kind.GetAccessor:
                        case Kind.SetAccessor:
                        {
                            return false;
                        }
                    }
                }
            }
        }
        else
        {
            if (Ast.IsArrayLiteralExpression(node))
            {
                {
                    foreach (var range16 in GoRuntime.Range(node.AsArrayLiteralExpression().Elements.Nodes))
                    {
                        var _2 = range16.Key;
                        var elem2 = range16.Value;
                        if (Ast.IsSpreadElement(elem2))
                        {
                            if (this.destructuringNeedsFlattening(elem2.Expression()))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (this.destructuringNeedsFlattening(elem2))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                if (Ast.IsIdentifier(node))
                {
                    var exportedNames = this.getExports(node);
                    if (Transformers.IsExportName(this.EmitContext(), node))
                    {
                        return (GoRuntime.Len(exportedNames) > 1);
                    }

                    if ((GoRuntime.Len(exportedNames) == 0))
                    {
                        return false;
                    }

                    if ((((GoRuntime.Len(exportedNames) == 1) && this.isDirectExport(node)) && (exportedNames[0].TextValue().EqualsValue(node.TextValue()))))
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        return false;
    }

    // createAllExportExpressions is the callback used during destructuring flattening to create
    // export expressions for each exported identifier binding.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1488:1.
    private Node createAllExportExpressions(Node name, Node value, TextRange? location)
    {
        var exportedNames = this.getExports(name);
        if ((GoRuntime.Len(exportedNames) > 0))
        {
            Node expression = default;
            if (this.isDirectExport(name))
            {
                var exportName = name.Clone(this.Factory());
                this.EmitContext().AddEmitFlags(exportName, (EmitFlags.NoComments | EmitFlags.NoSourceMap));
                var propertyAccess = this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("exports"), null, exportName, NodeFlags.None);
                this.EmitContext().AddEmitFlags(propertyAccess, EmitFlags.NoComments);
                expression = this.Factory().NewAssignmentExpression(propertyAccess, value);
                this.EmitContext().AssignCommentAndSourceMapRanges(expression, name);
            }
            else
            {
                expression = this.Factory().NewAssignmentExpression(name, value);
            }

            {
                foreach (var exportName2 in GoRuntime.RangeValues(exportedNames))
                {
                    expression = this.createExportExpression(exportName2, expression, location, false);
                }
            }

            return expression;
        }

        if (this.isDirectExport(name))
        {
            var exportName3 = name.Clone(this.Factory());
            this.EmitContext().AddEmitFlags(exportName3, (EmitFlags.NoComments | EmitFlags.NoSourceMap));
            var propertyAccess2 = this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("exports"), null, exportName3, NodeFlags.None);
            this.EmitContext().AddEmitFlags(propertyAccess2, EmitFlags.NoComments);
            var result = this.Factory().NewAssignmentExpression(propertyAccess2, value);
            this.EmitContext().AssignCommentAndSourceMapRanges(result, name);
            return result;
        }

        return this.Factory().NewAssignmentExpression(name, value);
    }

    // isDirectExport checks whether the identifier is directly exported from the source file
    // (e.g., `export let x` or `export function f()`), as opposed to being re-exported via
    // `export { x }` for a locally-declared variable.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1538:1.
    private bool isDirectExport(Node name)
    {
        var exportContainer = this.resolver.GetReferencedExportContainer(this.EmitContext().MostOriginal(name), false);
        return ((exportContainer != null) && Ast.IsSourceFile(exportContainer));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1543:1.
    private Node visitAssignmentProperty(PropertyAssignment node)
    {
        return this.Factory().UpdatePropertyAssignment(node, null, this.Visitor().VisitNode(node.Name()), null, null, this.assignmentPatternVisitor.VisitNode(node.Initializer));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1554:1.
    private Node visitShorthandAssignmentProperty(ShorthandPropertyAssignment node)
    {
        var target = this.visitDestructuringAssignmentTargetNoStack(node.Name());
        if (Ast.IsIdentifier(target))
        {
            return this.Factory().UpdateShorthandPropertyAssignment(node, null, target, null, null, node.EqualsToken, this.Visitor().VisitNode(node.ObjectAssignmentInitializer));
        }

        if ((node.ObjectAssignmentInitializer != null))
        {
            var equalsToken = node.EqualsToken;
            if ((equalsToken == null))
            {
                equalsToken = this.Factory().NewToken(Kind.EqualsToken);
            }

            target = this.Factory().NewBinaryExpression(null, target, null, equalsToken, this.Visitor().VisitNode(node.ObjectAssignmentInitializer));
        }

        var updated = this.Factory().NewPropertyAssignment(null, node.Name(), null, null, target);
        this.EmitContext().SetOriginal(updated, node.AsNode());
        this.EmitContext().AssignCommentAndSourceMapRanges(updated, node.AsNode());
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1592:1.
    private Node visitAssignmentRestProperty(SpreadAssignment node)
    {
        return this.Factory().UpdateSpreadAssignment(node, this.visitDestructuringAssignmentTarget(node.Expression));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1599:1.
    private Node visitAssignmentRestElement(SpreadElement node)
    {
        return this.Factory().UpdateSpreadElement(node, this.visitDestructuringAssignmentTarget(node.Expression));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1606:1.
    private Node visitAssignmentElement(Node node)
    {
        if (Ast.IsBinaryExpression(node))
        {
            var n = node.AsBinaryExpression();
            if ((n.OperatorToken.Kind == Kind.EqualsToken))
            {
                return this.Factory().UpdateBinaryExpression(n, null, this.visitDestructuringAssignmentTarget(n.Left), null, n.OperatorToken, this.Visitor().VisitNode(n.Right));
            }
        }

        return this.visitDestructuringAssignmentTargetNoStack(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1624:1.
    private Node visitDestructuringAssignmentTarget(Node node)
    {
        var grandparentNode = this.pushNode(node);
        try
        {
            switch (node.Kind)
            {
                case Kind.ObjectLiteralExpression:
                case Kind.ArrayLiteralExpression:
                {
                    node = this.visitAssignmentPatternNoStack(node);
                    break;
                }

                default:
                {
                    node = this.visitDestructuringAssignmentTargetNoStack(node);
                    break;
                }
            }

            return node;
        }
        finally
        {
            this.popNode(grandparentNode);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1637:1.
    private Node visitDestructuringAssignmentTargetNoStack(Node node)
    {
        if (((Ast.IsIdentifier(node) && ((!Transformers.IsGeneratedIdentifier(this.EmitContext(), node) || ModuleTransforms.IsFileLevelReservedGeneratedIdentifier(this.EmitContext(), node)))) && !Transformers.IsLocalName(this.EmitContext(), node)))
        {
            var expression = this.visitExpressionIdentifier(node);
            var exportedNames = this.getExports(node);
            if ((GoRuntime.Len(exportedNames) > 0))
            {
                var value = this.Factory().NewUniqueNameEx("value", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
                expression = this.Factory().NewAssignmentExpression(expression, value);
                {
                    foreach (var exportName in GoRuntime.RangeValues(exportedNames))
                    {
                        expression = this.createExportExpression(exportName, expression, null, false);
                    }
                }

                var statement = this.Factory().NewExpressionStatement(expression);
                var statementList = this.Factory().NewNodeList(Slice<Node>.FromArray([statement]));
                var param = this.Factory().NewParameterDeclaration(null, null, value, null, null, null);
                var valueSetter = this.Factory().NewSetAccessorDeclaration(null, this.Factory().NewIdentifier("value"), null, this.Factory().NewNodeList(Slice<Node>.FromArray([param])), null, null, this.Factory().NewBlock(statementList, false));
                var propertyList = this.Factory().NewNodeList(Slice<Node>.FromArray([valueSetter]));
                expression = this.Factory().NewObjectLiteralExpression(propertyList, false);
                expression = this.Factory().NewPropertyAccessExpression(expression, null, this.Factory().NewIdentifier("value"), NodeFlags.None);
            }

            return expression;
        }

        return this.visitNoStack(node, false);
    }

    // Visits a comma expression whose left-hand value is always discard, and whose right-hand value may be discarded at runtime.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1690:1.
    private Node visitCommaExpression(BinaryExpression node, bool resultIsDiscarded)
    {
        var left = this.discardedValueVisitor.VisitNode(node.Left);
        var right = Core.IfElse(resultIsDiscarded, this.discardedValueVisitor, this.Visitor()).VisitNode(node.Right);
        return this.Factory().UpdateBinaryExpression(node, null, left, null, node.OperatorToken, right);
    }

    // Visits a prefix unary expression that might modify an exported identifier.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1697:1.
    private Node visitPrefixUnaryExpression(PrefixUnaryExpression node, bool resultIsDiscarded)
    {
        if ((((((node.Operator == Kind.PlusPlusToken) || (node.Operator == Kind.MinusMinusToken))) && Ast.IsIdentifier(node.Operand)) && !Transformers.IsLocalName(this.EmitContext(), node.Operand)))
        {
            var exportedNames = this.getExports(node.Operand);
            if ((GoRuntime.Len(exportedNames) > 0))
            {
                var expression = this.Factory().UpdatePrefixUnaryExpression(node, node.Operator, this.Visitor().VisitNode(node.Operand));
                {
                    foreach (var exportName in GoRuntime.RangeValues(exportedNames))
                    {
                        expression = this.createExportExpression(exportName, expression, null, false);
                        this.EmitContext().AssignCommentAndSourceMapRanges(expression, node.AsNode());
                    }
                }

                return expression;
            }
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Visits a postfix unary expression that might modify an exported identifier.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1735:1.
    private Node visitPostfixUnaryExpression(PostfixUnaryExpression node, bool resultIsDiscarded)
    {
        if ((((((node.Operator == Kind.PlusPlusToken) || (node.Operator == Kind.MinusMinusToken))) && Ast.IsIdentifier(node.Operand)) && !Transformers.IsLocalName(this.EmitContext(), node.Operand)))
        {
            var exportedNames = this.getExports(node.Operand);
            if ((GoRuntime.Len(exportedNames) > 0))
            {
                Node temp = default;
                var expression = this.Factory().UpdatePostfixUnaryExpression(node, this.Visitor().VisitNode(node.Operand), node.Operator);
                if (!resultIsDiscarded)
                {
                    temp = this.Factory().NewTempVariable();
                    this.EmitContext().AddVariableDeclaration(temp);
                    expression = this.Factory().NewAssignmentExpression(temp, expression);
                    this.EmitContext().AssignCommentAndSourceMapRanges(expression, node.AsNode());
                }

                expression = this.Factory().NewCommaExpression(expression, node.Operand.Clone(this.Factory()));
                this.EmitContext().AssignCommentAndSourceMapRanges(expression, node.AsNode());
                {
                    foreach (var exportName in GoRuntime.RangeValues(exportedNames))
                    {
                        expression = this.createExportExpression(exportName, expression, null, false);
                        this.EmitContext().AssignCommentAndSourceMapRanges(expression, node.AsNode());
                    }
                }

                if ((temp != null))
                {
                    expression = this.Factory().NewCommaExpression(expression, temp.AsNode());
                    this.EmitContext().AssignCommentAndSourceMapRanges(expression, node.AsNode());
                }

                return expression;
            }
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Visits a call expression that might reference an imported symbol and thus require an indirect call, or that might
    // be an `import()` or `require()` call that may need to be rewritten.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1806:1.
    private Node visitCallExpression(CallExpression node)
    {
        var needsRewrite = false;
        if (this.compilerOptions.RewriteRelativeImportExtensions == Tristate.True)
        {
            if (((Ast.IsImportCall(node.AsNode()) && (GoRuntime.Len(node.Arguments.Nodes) > 0)) || (Ast.IsInJSFile(node.AsNode()) && Ast.IsRequireCall(node.AsNode(), false))))
            {
                needsRewrite = true;
            }
        }

        if (((node.Expression.Kind == Kind.ImportKeyword) && this.shouldTransformImportCall()))
        {
            return this.visitImportCallExpression(node, needsRewrite);
        }

        if (needsRewrite)
        {
            return this.shimOrRewriteImportOrRequireCall(node.AsCallExpression());
        }

        if (Ast.IsIdentifier(node.Expression))
        {
            var expression = this.visitExpressionIdentifier(node.Expression);
            var updated = this.Factory().UpdateCallExpression(node, expression, node.QuestionDotToken, null, this.Visitor().VisitNodes(node.Arguments), node.Flags);
            if ((!Ast.IsIdentifier(expression) && !Transformers.IsHelperName(this.EmitContext(), node.Expression)))
            {
                this.EmitContext().AddEmitFlags(updated, EmitFlags.IndirectCall);
            }

            return updated;
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1846:1.
    private bool shouldTransformImportCall()
    {
        return Ast.ShouldTransformImportCall(RootedFilePath.FromValue(this.currentSourceFile.FileNameGoValue()), this.compilerOptions, this.getEmitModuleFormatOfFile(this.currentSourceFile));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1850:1.
    private Node visitImportCallExpression(CallExpression node, bool rewriteOrShim)
    {
        if (((this.moduleKind == ModuleKind.None) && (this.languageVersion >= ScriptTarget.ES2020)))
        {
            return this.Visitor().VisitEachChild(node.AsNode());
        }

        var externalModuleName = ModuleTransforms.GetExternalModuleNameLiteral(this.Factory(), node.AsNode(), this.currentSourceFile, null, null, this.compilerOptions);
        var firstArgument = this.Visitor().VisitNode(Core.FirstOrNil(node.Arguments.Nodes));
        Node argument = default;
        if (((externalModuleName != null) && ((((firstArgument == null) || !Ast.IsStringLiteral(firstArgument)) || (!firstArgument.TextValue().EqualsValue(externalModuleName.TextValue()))))))
        {
            argument = externalModuleName;
        }
        else
        {
            if (((firstArgument != null) && rewriteOrShim))
            {
                if (Ast.IsStringLiteral(firstArgument))
                {
                    argument = ModuleTransforms.RewriteModuleSpecifier(this.EmitContext(), firstArgument, this.compilerOptions);
                }
                else
                {
                    argument = this.Factory().NewRewriteRelativeImportExtensionsHelper(firstArgument, (this.compilerOptions.Jsx == JsxEmit.Preserve));
                }
            }
            else
            {
                argument = firstArgument;
            }
        }

        return this.createImportCallExpressionCommonJS(argument);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1874:1.
    private Node createImportCallExpressionCommonJS(Node arg)
    {
        var needSyncEval = ((arg != null) && !ModuleTransforms.IsSimpleInlineableExpression(arg));
        Slice<Node> promiseResolveArguments = default;
        if (needSyncEval)
        {
            promiseResolveArguments = Slice<Node>.FromArray([this.Factory().NewTemplateExpression(this.Factory().NewTemplateHead("", "", TokenFlags.None), this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewTemplateSpan(arg, this.Factory().NewTemplateTail("", "", TokenFlags.None))])))]);
        }

        var promiseResolveCall = this.Factory().NewCallExpression(this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("Promise"), null, this.Factory().NewIdentifier("resolve"), NodeFlags.None), null, null, this.Factory().NewNodeList(promiseResolveArguments), NodeFlags.None);
        Slice<Node> requireArguments = default;
        if (needSyncEval)
        {
            requireArguments = Slice<Node>.FromArray([this.Factory().NewIdentifier("s")]);
        }
        else
        {
            if ((arg != null))
            {
                requireArguments = Slice<Node>.FromArray([arg]);
            }
        }

        var requireCall = this.Factory().NewImportStarHelper(this.Factory().NewCallExpression(this.Factory().NewIdentifier("require"), null, null, this.Factory().NewNodeList(requireArguments), NodeFlags.None));
        Slice<Node> parameters = default;
        if (needSyncEval)
        {
            parameters = Slice<Node>.FromArray([this.Factory().NewParameterDeclaration(null, null, this.Factory().NewIdentifier("s"), null, null, null)]);
        }

        var function = this.Factory().NewArrowFunction(null, null, this.Factory().NewNodeList(parameters), null, null, this.Factory().NewToken(Kind.EqualsGreaterThanToken), requireCall);
        var downleveledImport = this.Factory().NewCallExpression(this.Factory().NewPropertyAccessExpression(promiseResolveCall, null, this.Factory().NewIdentifier("then"), NodeFlags.None), null, null, this.Factory().NewNodeList(Slice<Node>.FromArray([function])), NodeFlags.None);
        return downleveledImport;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:1967:1.
    private Node shimOrRewriteImportOrRequireCall(CallExpression node)
    {
        var expression = this.Visitor().VisitNode(node.Expression);
        var argumentsList = node.Arguments;
        if ((GoRuntime.Len(node.Arguments.Nodes) > 0))
        {
            var firstArgument = this.Visitor().VisitNode(node.Arguments.Nodes[0]);
            var firstArgumentChanged = false;
            if (Ast.IsStringLiteralLike(firstArgument))
            {
                var rewritten = ModuleTransforms.RewriteModuleSpecifier(this.EmitContext(), firstArgument, this.compilerOptions);
                firstArgumentChanged = (rewritten != firstArgument);
                firstArgument = rewritten;
            }
            else
            {
                firstArgument = this.Factory().NewRewriteRelativeImportExtensionsHelper(firstArgument, (this.compilerOptions.Jsx == JsxEmit.Preserve));
                firstArgumentChanged = true;
            }

            var tuple21 = this.Visitor().VisitSlice(node.Arguments.Nodes.Slice2(1, node.Arguments.Nodes.Length));
            var rest = tuple21.Item1;
            var restChanged = tuple21.Item2;
            if ((firstArgumentChanged || restChanged))
            {
                var arguments = Slice<Node>.FromArray([firstArgument]).Append(rest);
                argumentsList = this.Factory().NewNodeList(arguments);
                argumentsList.Loc = node.Arguments.Loc;
            }
        }

        return this.Factory().UpdateCallExpression(node, expression, node.QuestionDotToken, null, argumentsList, node.Flags);
    }

    // Visits a tagged template expression that might reference an imported symbol and thus require an indirect call.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:2001:1.
    private Node visitTaggedTemplateExpression(TaggedTemplateExpression node)
    {
        if (Ast.IsIdentifier(node.Tag))
        {
            var expression = this.visitExpressionIdentifier(node.Tag);
            var updated = this.Factory().UpdateTaggedTemplateExpression(node, expression, null, null, this.Visitor().VisitNode(node.Template), node.Flags);
            if ((!Ast.IsIdentifier(expression) && !Transformers.IsHelperName(this.EmitContext(), node.Tag)))
            {
                this.EmitContext().AddEmitFlags(updated, EmitFlags.IndirectCall);
            }

            return updated;
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Visits a shorthand property assignment that might reference an imported or exported symbol.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:2030:1.
    private Node visitShorthandPropertyAssignment(ShorthandPropertyAssignment node)
    {
        var name = node.Name();
        var exportedOrImportedName = this.visitExpressionIdentifier(name);
        if ((exportedOrImportedName != name))
        {
            var expression = exportedOrImportedName;
            if ((node.ObjectAssignmentInitializer != null))
            {
                expression = this.Factory().NewAssignmentExpression(expression, this.Visitor().VisitNode(node.ObjectAssignmentInitializer));
            }

            var assignment = this.Factory().NewPropertyAssignment(null, name, null, null, expression);
            assignment.Loc = node.Loc;
            this.EmitContext().AssignCommentAndSourceMapRanges(assignment, node.AsNode());
            return assignment;
        }

        return this.Factory().UpdateShorthandPropertyAssignment(node, null, exportedOrImportedName, null, null, node.EqualsToken, this.Visitor().VisitNode(node.ObjectAssignmentInitializer));
    }

    // Visits an identifier that, if it is in an expression position, might reference an imported or exported symbol.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:2060:1.
    private Node visitIdentifier(Node node)
    {
        if (Transformers.IsIdentifierReference(node, this.parentNode))
        {
            return this.visitExpressionIdentifier(node);
        }

        return node;
    }

    // Visits an identifier in an expression position that might reference an imported or exported symbol.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:2068:1.
    private Node visitExpressionIdentifier(Node node)
    {
        {
            var info = this.EmitContext().GetAutoGenerateInfo(node);
            if ((((!(((info != null) && !info.Flags.HasAllowNameSubstitution())) && !Transformers.IsHelperName(this.EmitContext(), node)) && !Transformers.IsLocalName(this.EmitContext(), node)) && !ModuleTransforms.IsDeclarationNameOfEnumOrNamespace(this.EmitContext(), node)))
            {
                var exportContainer = this.resolver.GetReferencedExportContainer(this.EmitContext().MostOriginal(node), Transformers.IsExportName(this.EmitContext(), node));
                if (((exportContainer != null) && Ast.IsSourceFile(exportContainer)))
                {
                    var reference = this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("exports"), null, node.Clone(this.Factory()), NodeFlags.None);
                    this.EmitContext().AssignCommentAndSourceMapRanges(reference, node);
                    reference.Loc = node.Loc;
                    return reference;
                }

                var importDeclaration = this.resolver.GetReferencedImportDeclaration(this.EmitContext().MostOriginal(node));
                if ((importDeclaration != null))
                {
                    if (Ast.IsImportClause(importDeclaration))
                    {
                        var reference2 = this.Factory().NewPropertyAccessExpression(this.Factory().NewGeneratedNameForNode(importDeclaration.Parent), null, this.Factory().NewIdentifier("default"), NodeFlags.None);
                        this.EmitContext().AssignCommentAndSourceMapRanges(reference2, node);
                        reference2.Loc = node.Loc;
                        return reference2;
                    }

                    if (Ast.IsImportSpecifier(importDeclaration))
                    {
                        var name = importDeclaration.AsImportSpecifier().PropertyNameOrName();
                        var decl = Ast.FindAncestor(importDeclaration, Ast.IsImportDeclaration);
                        var target = this.Factory().NewGeneratedNameForNode(decl ?? importDeclaration);
                        Node reference3 = default;
                        if (Ast.IsStringLiteral(name))
                        {
                            reference3 = this.Factory().NewElementAccessExpression(target, null, this.Factory().NewStringLiteralFromNode(name), NodeFlags.None);
                        }
                        else
                        {
                            var referenceName = name.Clone(this.Factory());
                            this.EmitContext().AddEmitFlags(referenceName, (EmitFlags.NoSourceMap | EmitFlags.NoComments));
                            reference3 = this.Factory().NewPropertyAccessExpression(target, null, referenceName, NodeFlags.None);
                        }

                        this.EmitContext().AssignCommentAndSourceMapRanges(reference3, node);
                        reference3.Loc = node.Loc;
                        return reference3;
                    }
                }
            }
        }

        return node;
    }

    // Gets the exported names of an identifier, if it is exported.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go:2131:1.
    private Slice<Node> getExports(Node name)
    {
        if (!Transformers.IsGeneratedIdentifier(this.EmitContext(), name))
        {
            var importDeclaration = this.resolver.GetReferencedImportDeclaration(this.EmitContext().MostOriginal(name));
            if ((importDeclaration != null))
            {
                return this.currentModuleInfo.exportedBindings.Get(importDeclaration);
            }

            HashSet<Node> bindingsSet = null;
            Slice<Node> bindings = default;
            var declarations = this.resolver.GetReferencedValueDeclarations(this.EmitContext().MostOriginal(name));
            if ((!declarations.IsNil))
            {
                {
                    foreach (var declaration in GoRuntime.RangeValues(declarations))
                    {
                        var exportedBindings = this.currentModuleInfo.exportedBindings.Get(declaration);
                        {
                            foreach (var range23 in GoRuntime.Range(exportedBindings))
                            {
                                var _2 = range23.Key;
                                var binding = range23.Value;
                                if (bindingsSet?.Contains(binding) != true)
                                {
                                    (bindingsSet ??= new HashSet<Node>(ReferenceEqualityComparer.Instance)).Add(binding);
                                    bindings = bindings.Append(binding);
                                }
                            }
                        }
                    }
                }

                return bindings;
            }
        }
        else
        {
            if (ModuleTransforms.IsFileLevelReservedGeneratedIdentifier(this.EmitContext(), name))
            {
                var exportSpecifiers = this.currentModuleInfo.exportSpecifiers.Get(ByteStringKey.FromValue(name.TextValue()));
                if ((!exportSpecifiers.IsNil))
                {
                    Slice<Node> exportedNames = default;
                    {
                        foreach (var range24 in GoRuntime.Range(exportSpecifiers))
                        {
                            var _3 = range24.Key;
                            var exportSpecifier = range24.Value;
                            exportedNames = exportedNames.Append(exportSpecifier.Name());
                        }
                    }

                    return exportedNames;
                }
            }
        }

        return default;
    }
}
