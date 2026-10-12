// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/async.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Collections;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class AsyncTransformer : SuperAccessTransformer
{
    [Flags]
    private enum AsyncContextFlags
    {
        NonTopLevel = 1,
        HasLexicalThis = 2
    }

    private struct LexicalArgumentsInfo
    {
        internal Node binding;
        internal bool used;
    }

    private AsyncContextFlags contextFlags;
    private HashSet<ByteStringKey> enclosingFunctionParameterNames;
    private LexicalArgumentsInfo lexicalArguments;
    private NodeVisitor asyncBodyVisitor, fallbackNodeVisitor;
    private Func<Node, bool> collidesWithParameterNameCallback;
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:37:1.
    internal static Transformer NewAsyncTransformer(TransformOptions opts)
    {
        var tx = new AsyncTransformer();
        tx.collidesWithParameterNameCallback = tx.collidesWithParameterName;
        var result = tx.NewTransformer(tx.visit, opts.Context);
        tx.initSuperAccessVisitor(tx.EmitContext(), tx.Factory());
        tx.asyncBodyVisitor = tx.EmitContext().NewNodeVisitor(tx.visitAsyncBodyNode);
        tx.fallbackNodeVisitor = tx.EmitContext().NewNodeVisitor(tx.visitFallback);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:46:1.
    private Node visitSourceFile(SourceFile node)
    {
        if (node.IsDeclarationFile)
        {
            return node.AsNode();
        }

        this.setContextFlag(AsyncContextFlags.NonTopLevel, false);
        this.setContextFlag(AsyncContextFlags.HasLexicalThis, false);
        var visited = this.Visitor().VisitEachChild(node.AsNode());
        this.EmitContext().AddEmitHelper(visited, this.EmitContext().ReadEmitHelpers());
        return visited;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:58:1.
    private void setContextFlag(AsyncContextFlags flag, bool val)
    {
        if (val)
        {
            this.contextFlags |= flag;
        }
        else
        {
            this.contextFlags &= ~(flag);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:66:1.
    private bool inContext(AsyncContextFlags flags)
    {
        return ((this.contextFlags & flags) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:70:1.
    private bool inTopLevelContext()
    {
        return !this.inContext(AsyncContextFlags.NonTopLevel);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:74:1.
    private bool inHasLexicalThisContext()
    {
        return this.inContext(AsyncContextFlags.HasLexicalThis);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:78:1.
    private Node doWithContext(AsyncContextFlags flags, Func<AsyncTransformer, Node, Node> cb, Node node)
    {
        var flagsToSet = (flags & ~this.contextFlags);
        if ((flagsToSet != 0))
        {
            this.setContextFlag(flagsToSet, true);
            var result = cb(this, node);
            this.setContextFlag(flagsToSet, false);
            return result;
        }

        return cb(this, node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:89:1.
    private Node visitDefault(Node node)
    {
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:93:1.
    private Node fallbackVisitor(Node node)
    {
        if (((this.capturedSuperProperties == null) && (this.lexicalArguments.binding == null)))
        {
            return node;
        }

        this.trackSuperAccess(node);
        switch (node.Kind)
        {
            case Kind.FunctionExpression:
            case Kind.FunctionDeclaration:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.Constructor:
            {
                return node;
            }

            case Kind.Parameter:
            case Kind.BindingElement:
            case Kind.VariableDeclaration:
            {
                break;
            }

            case Kind.Identifier:
            {
                if (((((this.lexicalArguments.binding != null) && (node.TextEquals("arguments"u8))) && !Ast.IsIdentifierName(node)) && !Ast.IsLabelName(node)))
                {
                    this.lexicalArguments.used = true;
                    return this.lexicalArguments.binding;
                }

                break;
            }
        }

        return this.fallbackNodeVisitor.VisitEachChild(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:122:1.
    private Node visitFallback(Node node)
    {
        return this.fallbackVisitor(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:126:1.
    private Node visit(Node node)
    {
        bool restoreLexicalThis = false;
        try
        {
            if ((((this.EmitContext().EmitFlags(node) & EmitFlags.NoLexicalThis) != 0) && this.inHasLexicalThisContext()))
            {
                this.setContextFlag(AsyncContextFlags.HasLexicalThis, false);
                restoreLexicalThis = true;
            }

            if ((((uint)node.SubtreeFacts() & ((SubtreeFactValues.ContainsAnyAwait | SubtreeFactValues.ContainsAwait))) == 0))
            {
                return this.fallbackVisitor(node);
            }

            this.trackSuperAccess(node);
            switch (node.Kind)
            {
                case Kind.AsyncKeyword:
                {
                    return null;
                }

                case Kind.SourceFile:
                {
                    return this.visitSourceFile(node.AsSourceFile());
                }

                case Kind.AwaitExpression:
                {
                    return this.visitAwaitExpression(node.AsAwaitExpression());
                }

                case Kind.MethodDeclaration:
                {
                    return this.doWithContext((AsyncContextFlags.NonTopLevel | AsyncContextFlags.HasLexicalThis), static (tx, n) => tx.visitMethodDeclaration(n), node);
                }

                case Kind.FunctionDeclaration:
                {
                    return this.doWithContext((AsyncContextFlags.NonTopLevel | AsyncContextFlags.HasLexicalThis), static (tx, n) => tx.visitFunctionDeclaration(n), node);
                }

                case Kind.FunctionExpression:
                {
                    return this.doWithContext((AsyncContextFlags.NonTopLevel | AsyncContextFlags.HasLexicalThis), static (tx, n) => tx.visitFunctionExpression(n), node);
                }

                case Kind.ArrowFunction:
                {
                    return this.doWithContext(AsyncContextFlags.NonTopLevel, static (tx, n) => tx.visitArrowFunction(n), node);
                }

                case Kind.GetAccessor:
                {
                    return this.doWithContext((AsyncContextFlags.NonTopLevel | AsyncContextFlags.HasLexicalThis), static (tx, n) => tx.visitGetAccessorDeclaration(n), node);
                }

                case Kind.SetAccessor:
                {
                    return this.doWithContext((AsyncContextFlags.NonTopLevel | AsyncContextFlags.HasLexicalThis), static (tx, n) => tx.visitSetAccessorDeclaration(n), node);
                }

                case Kind.Constructor:
                {
                    return this.doWithContext((AsyncContextFlags.NonTopLevel | AsyncContextFlags.HasLexicalThis), static (tx, n) => tx.visitConstructorDeclaration(n), node);
                }

                case Kind.ClassDeclaration:
                case Kind.ClassExpression:
                {
                    return this.doWithContext((AsyncContextFlags.NonTopLevel | AsyncContextFlags.HasLexicalThis), static (tx, n) => tx.visitDefault(n), node);
                }

                default:
                {
                    return this.Visitor().VisitEachChild(node);
                }
            }
        }
        finally
        {
            if (restoreLexicalThis)
                this.setContextFlag(AsyncContextFlags.HasLexicalThis, true);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:165:1.
    private Node visitAsyncBodyNode(Node node)
    {
        if (EsTransforms.IsNodeWithPossibleHoistedDeclaration(node))
        {
            switch (node.Kind)
            {
                case Kind.VariableStatement:
                {
                    return this.visitVariableStatementInAsyncBody(node);
                }

                case Kind.ForStatement:
                {
                    return this.visitForStatementInAsyncBody(node.AsForStatement());
                }

                case Kind.ForInStatement:
                {
                    return this.visitForInStatementInAsyncBody(node.AsForInOrOfStatement());
                }

                case Kind.ForOfStatement:
                {
                    return this.visitForOfStatementInAsyncBody(node.AsForInOrOfStatement());
                }

                case Kind.CatchClause:
                {
                    return this.visitCatchClauseInAsyncBody(node.AsCatchClause());
                }

                case Kind.Block:
                case Kind.SwitchStatement:
                case Kind.CaseBlock:
                case Kind.CaseClause:
                case Kind.DefaultClause:
                case Kind.TryStatement:
                case Kind.DoStatement:
                case Kind.WhileStatement:
                case Kind.IfStatement:
                case Kind.WithStatement:
                case Kind.LabeledStatement:
                {
                    return this.asyncBodyVisitor.VisitEachChild(node);
                }
            }
        }

        return this.visit(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:195:1.
    private Node visitCatchClauseInAsyncBody(CatchClause node)
    {
        var catchClauseNames = new HashSet<ByteStringKey>(ByteStringKey.Comparer);
        if ((node.VariableDeclaration != null))
        {
            this.recordDeclarationName(node.VariableDeclaration, catchClauseNames);
        }

        HashSet<ByteStringKey> catchClauseUnshadowedNames = default;
        {
            foreach (var escapedName in catchClauseNames)
            {
                if (((this.enclosingFunctionParameterNames != null) && this.enclosingFunctionParameterNames.Contains(escapedName)))
                {
                    if ((catchClauseUnshadowedNames == null))
                    {
                        catchClauseUnshadowedNames = new HashSet<ByteStringKey>(this.enclosingFunctionParameterNames, ByteStringKey.Comparer);
                    }

                    catchClauseUnshadowedNames.Remove(escapedName);
                }
            }
        }

        if ((catchClauseUnshadowedNames != null))
        {
            var savedEnclosingFunctionParameterNames = this.enclosingFunctionParameterNames;
            this.enclosingFunctionParameterNames = catchClauseUnshadowedNames;
            var result = this.asyncBodyVisitor.VisitEachChild(node.AsNode());
            this.enclosingFunctionParameterNames = savedEnclosingFunctionParameterNames;
            return result;
        }

        return this.asyncBodyVisitor.VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:222:1.
    private Node visitVariableStatementInAsyncBody(Node node)
    {
        var declList = node.AsVariableStatement().DeclarationList;
        if (this.isVariableDeclarationListWithCollidingName(declList))
        {
            var expression = this.visitVariableDeclarationListWithCollidingNames(declList.AsVariableDeclarationList(), false);
            if ((expression != null))
            {
                return this.Factory().NewExpressionStatement(expression);
            }

            return null;
        }

        return this.Visitor().VisitEachChild(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:234:1.
    private Node visitForInStatementInAsyncBody(ForInOrOfStatement node)
    {
        Node visitedInitializer = default;
        if (this.isVariableDeclarationListWithCollidingName(node.Initializer))
        {
            visitedInitializer = this.visitVariableDeclarationListWithCollidingNames(node.Initializer.AsVariableDeclarationList(), true);
        }
        else
        {
            visitedInitializer = this.Visitor().VisitNode(node.Initializer);
        }

        return this.Factory().UpdateForInOrOfStatement(node, null, visitedInitializer, this.Visitor().VisitNode(node.Expression), this.asyncBodyVisitor.VisitEmbeddedStatement(node.Statement));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:251:1.
    private Node visitForOfStatementInAsyncBody(ForInOrOfStatement node)
    {
        Node visitedInitializer = default;
        if (this.isVariableDeclarationListWithCollidingName(node.Initializer))
        {
            visitedInitializer = this.visitVariableDeclarationListWithCollidingNames(node.Initializer.AsVariableDeclarationList(), true);
        }
        else
        {
            visitedInitializer = this.Visitor().VisitNode(node.Initializer);
        }

        return this.Factory().UpdateForInOrOfStatement(node, this.Visitor().VisitNode(node.AwaitModifier), visitedInitializer, this.Visitor().VisitNode(node.Expression), this.asyncBodyVisitor.VisitEmbeddedStatement(node.Statement));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:268:1.
    private Node visitForStatementInAsyncBody(ForStatement node)
    {
        var initializer = node.Initializer;
        Node visitedInitializer = default;
        if (((initializer != null) && this.isVariableDeclarationListWithCollidingName(initializer)))
        {
            visitedInitializer = this.visitVariableDeclarationListWithCollidingNames(initializer.AsVariableDeclarationList(), false);
        }
        else
        {
            visitedInitializer = this.Visitor().VisitNode(node.Initializer);
        }

        return this.Factory().UpdateForStatement(node, visitedInitializer, this.Visitor().VisitNode(node.Condition), this.Visitor().VisitNode(node.Incrementor), this.asyncBodyVisitor.VisitEmbeddedStatement(node.Statement));
    }

    // visitAwaitExpression visits an AwaitExpression node.
    //
    // This function will be called any time a ES2017 await expression is encountered.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:289:1.
    private Node visitAwaitExpression(AwaitExpression node)
    {
        if (this.inTopLevelContext())
        {
            return this.Visitor().VisitEachChild(node.AsNode());
        }

        var yieldExpr = this.Factory().NewYieldExpression(null, this.Visitor().VisitNode(node.Expression));
        yieldExpr.Loc = node.Loc;
        this.EmitContext().SetOriginal(yieldExpr, node.AsNode());
        return yieldExpr;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:303:1.
    private Node visitConstructorDeclaration(Node node)
    {
        var decl = node.AsConstructorDeclaration();
        var savedLexicalArguments = this.lexicalArguments;
        this.lexicalArguments = new LexicalArgumentsInfo
        {
        };
        var updated = this.Factory().UpdateConstructorDeclaration(decl, this.Visitor().VisitModifiers(decl.Modifiers()), null, this.EmitContext().VisitParameters(decl.Parameters, this.Visitor()), null, null, this.transformMethodBody(node));
        this.lexicalArguments = savedLexicalArguments;
        return updated;
    }

    // visitMethodDeclaration visits a MethodDeclaration node.
    //
    // This function will be called when one of the following conditions are met:
    // - The node is marked as async
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:324:1.
    private Node visitMethodDeclaration(Node node)
    {
        var decl = node.AsMethodDeclaration();
        var functionFlags = Ast.GetFunctionFlags(node);
        var savedLexicalArguments = this.lexicalArguments;
        this.lexicalArguments = new LexicalArgumentsInfo
        {
        };
        NodeList parameters = default;
        Node body = default;
        if (((functionFlags & FunctionFlags.Async) != 0))
        {
            parameters = this.transformAsyncFunctionParameterList(node);
            body = this.transformAsyncFunctionBody(node, parameters);
        }
        else
        {
            parameters = this.EmitContext().VisitParameters(decl.Parameters, this.Visitor());
            body = this.transformMethodBody(node);
        }

        var updated = this.Factory().UpdateMethodDeclaration(decl, this.Visitor().VisitModifiers(decl.Modifiers()), decl.AsteriskToken, decl.Name(), null, null, parameters, null, null, body);
        this.lexicalArguments = savedLexicalArguments;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:356:1.
    private Node visitGetAccessorDeclaration(Node node)
    {
        var decl = node.AsGetAccessorDeclaration();
        var savedLexicalArguments = this.lexicalArguments;
        this.lexicalArguments = new LexicalArgumentsInfo
        {
        };
        var updated = this.Factory().UpdateGetAccessorDeclaration(decl, this.Visitor().VisitModifiers(decl.Modifiers()), decl.Name(), null, this.EmitContext().VisitParameters(decl.Parameters, this.Visitor()), null, null, this.transformMethodBody(node));
        this.lexicalArguments = savedLexicalArguments;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:374:1.
    private Node visitSetAccessorDeclaration(Node node)
    {
        var decl = node.AsSetAccessorDeclaration();
        var savedLexicalArguments = this.lexicalArguments;
        this.lexicalArguments = new LexicalArgumentsInfo
        {
        };
        var updated = this.Factory().UpdateSetAccessorDeclaration(decl, this.Visitor().VisitModifiers(decl.Modifiers()), decl.Name(), null, this.EmitContext().VisitParameters(decl.Parameters, this.Visitor()), null, null, this.transformMethodBody(node));
        this.lexicalArguments = savedLexicalArguments;
        return updated;
    }

    // visitFunctionDeclaration visits a FunctionDeclaration node.
    //
    // This function will be called when one of the following conditions are met:
    // - The node is marked async
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:396:1.
    private Node visitFunctionDeclaration(Node node)
    {
        var decl = node.AsFunctionDeclaration();
        var functionFlags = Ast.GetFunctionFlags(node);
        var savedLexicalArguments = this.lexicalArguments;
        this.lexicalArguments = new LexicalArgumentsInfo
        {
        };
        NodeList parameters = default;
        Node body = default;
        if (((functionFlags & FunctionFlags.Async) != 0))
        {
            parameters = this.transformAsyncFunctionParameterList(node);
            body = this.transformAsyncFunctionBody(node, parameters);
        }
        else
        {
            parameters = this.EmitContext().VisitParameters(decl.Parameters, this.Visitor());
            body = this.EmitContext().VisitFunctionBody(decl.Body, this.Visitor());
        }

        var updated = this.Factory().UpdateFunctionDeclaration(decl, this.Visitor().VisitModifiers(decl.Modifiers()), decl.AsteriskToken, this.Visitor().VisitNode(decl.Name()), null, parameters, null, null, body);
        this.lexicalArguments = savedLexicalArguments;
        return updated;
    }

    // visitFunctionExpression visits a FunctionExpression node.
    //
    // This function will be called when one of the following conditions are met:
    // - The node is marked async
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:431:1.
    private Node visitFunctionExpression(Node node)
    {
        var decl = node.AsFunctionExpression();
        var functionFlags = Ast.GetFunctionFlags(node);
        var savedLexicalArguments = this.lexicalArguments;
        this.lexicalArguments = new LexicalArgumentsInfo
        {
        };
        NodeList parameters = default;
        Node body = default;
        if (((functionFlags & FunctionFlags.Async) != 0))
        {
            parameters = this.transformAsyncFunctionParameterList(node);
            body = this.transformAsyncFunctionBody(node, parameters);
        }
        else
        {
            parameters = this.EmitContext().VisitParameters(decl.Parameters, this.Visitor());
            body = this.EmitContext().VisitFunctionBody(decl.Body, this.Visitor());
        }

        var updated = this.Factory().UpdateFunctionExpression(decl, this.Visitor().VisitModifiers(decl.Modifiers()), decl.AsteriskToken, this.Visitor().VisitNode(decl.Name()), null, parameters, null, null, body);
        this.lexicalArguments = savedLexicalArguments;
        return updated;
    }

    // visitArrowFunction visits an ArrowFunction.
    //
    // This function will be called when one of the following conditions are met:
    // - The node is marked async
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:466:1.
    private Node visitArrowFunction(Node node)
    {
        bool restoreArguments = false;
        LexicalArgumentsInfo savedLexicalArguments = default;
        try
        {
            if (((this.EmitContext().EmitFlags(node) & EmitFlags.NoLexicalArguments) != 0))
            {
                savedLexicalArguments = this.lexicalArguments;
                this.lexicalArguments = new LexicalArgumentsInfo
                {
                };
                restoreArguments = true;
            }

            var decl = node.AsArrowFunction();
            var functionFlags = Ast.GetFunctionFlags(node);
            NodeList parameters = default;
            Node body = default;
            if (((functionFlags & FunctionFlags.Async) != 0))
            {
                parameters = this.transformAsyncFunctionParameterList(node);
                body = this.transformAsyncFunctionBody(node, parameters);
            }
            else
            {
                parameters = this.EmitContext().VisitParameters(decl.Parameters, this.Visitor());
                body = this.EmitContext().VisitFunctionBody(decl.Body, this.Visitor());
            }

            return this.Factory().UpdateArrowFunction(decl, this.Visitor().VisitModifiers(decl.Modifiers()), null, parameters, null, null, decl.EqualsGreaterThanToken, body);
        }
        finally
        {
            if (restoreArguments)
                this.lexicalArguments = savedLexicalArguments;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:502:1.
    private void recordDeclarationName(Node node, HashSet<ByteStringKey> names)
    {
        var name = node.Name();
        if ((name == null))
        {
            return;
        }

        if (Ast.IsIdentifier(name))
        {
            names.Add(ByteStringKey.FromValue(name.TextValue()));
        }
        else
        {
            if (Ast.IsBindingPattern(name))
            {
                {
                    foreach (var element in GoRuntime.RangeValues(name.AsBindingPattern().Elements.Nodes))
                    {
                        if (!Ast.IsOmittedExpression(element))
                        {
                            this.recordDeclarationName(element, names);
                        }
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:518:1.
    private bool isVariableDeclarationListWithCollidingName(Node node)
    {
        return ((((node != null) && Ast.IsVariableDeclarationList(node)) && ((node.Flags & NodeFlags.BlockScoped) == 0)) && Core.Some(node.AsVariableDeclarationList().Declarations.Nodes, this.collidesWithParameterNameCallback));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:525:1.
    private Node visitVariableDeclarationListWithCollidingNames(VariableDeclarationList node, bool hasReceiver)
    {
        this.hoistVariableDeclarationList(node);
        Slice<Node> variables = default;
        {
            foreach (var decl in GoRuntime.RangeValues(node.Declarations.Nodes))
            {
                if ((decl.AsVariableDeclaration().Initializer != null))
                {
                    variables = variables.Append(decl);
                }
            }
        }

        if ((GoRuntime.Len(variables) == 0))
        {
            if (hasReceiver)
            {
                var name = node.Declarations.Nodes[0].Name();
                Node target = default;
                if (Ast.IsBindingPattern(name))
                {
                    target = Transformers.ConvertBindingPatternToAssignmentPattern(this.EmitContext(), name.AsBindingPattern());
                }
                else
                {
                    target = name;
                }

                return this.Visitor().VisitNode(target);
            }

            return null;
        }

        Slice<Node> expressions = default;
        {
            foreach (var range4 in GoRuntime.Range(variables))
            {
                var _2 = range4.Key;
                var variable = range4.Value;
                expressions = expressions.Append(this.transformInitializedVariable(variable.AsVariableDeclaration()));
            }
        }

        return this.Factory().InlineExpressions(expressions);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:556:1.
    private void hoistVariableDeclarationList(VariableDeclarationList node)
    {
        {
            foreach (var decl in GoRuntime.RangeValues(node.Declarations.Nodes))
            {
                this.hoistVariable(decl);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:562:1.
    private void hoistVariable(Node node)
    {
        var name = node.Name();
        if ((name == null))
        {
            return;
        }

        if (Ast.IsIdentifier(name))
        {
            this.EmitContext().AddVariableDeclaration(name);
        }
        else
        {
            if (Ast.IsBindingPattern(name))
            {
                {
                    foreach (var element in GoRuntime.RangeValues(name.AsBindingPattern().Elements.Nodes))
                    {
                        if (!Ast.IsOmittedExpression(element))
                        {
                            this.hoistVariable(element);
                        }
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:578:1.
    private Node transformInitializedVariable(VariableDeclaration node)
    {
        Node target = default;
        if (Ast.IsBindingPattern(node.Name()))
        {
            target = Transformers.ConvertBindingPatternToAssignmentPattern(this.EmitContext(), node.Name().AsBindingPattern());
        }
        else
        {
            target = node.Name();
        }

        var converted = this.Factory().NewAssignmentExpression(target, node.Initializer);
        this.EmitContext().SetSourceMapRange(converted, node.Loc);
        return this.Visitor().VisitNode(converted);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:590:1.
    private bool collidesWithParameterName(Node node)
    {
        var name = node.Name();
        if ((name == null))
        {
            return false;
        }

        if (Ast.IsIdentifier(name))
        {
            return ((this.enclosingFunctionParameterNames != null) && name.TextValue().ContainsIn(this.enclosingFunctionParameterNames));
        }

        if (Ast.IsBindingPattern(name))
        {
            {
                foreach (var element in GoRuntime.RangeValues(name.AsBindingPattern().Elements.Nodes))
                {
                    if ((!Ast.IsOmittedExpression(element) && this.collidesWithParameterName(element)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:608:1.
    private Node transformMethodBody(Node node)
    {
        var savedCapturedSuperProperties = this.capturedSuperProperties;
        var savedHasSuperElementAccess = this.hasSuperElementAccess;
        var savedHasSuperPropertyAssignment = this.hasSuperPropertyAssignment;
        var savedSuperBinding = this.superBinding;
        var savedSuperIndexBinding = this.superIndexBinding;
        this.capturedSuperProperties = new OrderedSet<ByteStringKey>
        {
        };
        this.hasSuperElementAccess = false;
        this.hasSuperPropertyAssignment = false;
        this.superBinding = this.Factory().NewUniqueNameEx("_super", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) });
        this.superIndexBinding = this.Factory().NewUniqueNameEx("_superIndex", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) });
        this.EmitContext().StartVariableEnvironment();
        var updated = this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor());
        var emitSuperHelpers = ((((this.capturedSuperProperties.Size() > 0) || this.hasSuperElementAccess)) && (((Ast.GetFunctionFlags(this.getOriginalIfFunctionLike(node)) & FunctionFlags.AsyncGenerator)) != FunctionFlags.AsyncGenerator));
        if (emitSuperHelpers)
        {
            if ((this.capturedSuperProperties.Size() > 0))
            {
                this.EmitContext().AddInitializationStatement(this.createSuperAccessVariableStatement());
            }
        }

        var mergedStatements = this.EmitContext().EndAndMergeVariableEnvironmentList(updated.StatementList());
        if (((emitSuperHelpers && this.hasSuperElementAccess) && !updated.AsBlock().MultiLine))
        {
            var newBlock = this.Factory().NewBlock(mergedStatements, true);
            newBlock.Loc = updated.Loc;
            updated = newBlock;
        }
        else
        {
            updated = this.Factory().UpdateBlock(updated.AsBlock(), mergedStatements, updated.AsBlock().MultiLine);
        }

        if ((emitSuperHelpers && this.hasSuperElementAccess))
        {
            if (this.hasSuperPropertyAssignment)
            {
                this.EmitContext().AddEmitHelper(updated, EmitHelpers.AdvancedAsyncSuperHelper);
            }
            else
            {
                this.EmitContext().AddEmitHelper(updated, EmitHelpers.AsyncSuperHelper);
            }
        }

        this.capturedSuperProperties = savedCapturedSuperProperties;
        this.hasSuperElementAccess = savedHasSuperElementAccess;
        this.hasSuperPropertyAssignment = savedHasSuperPropertyAssignment;
        this.superBinding = savedSuperBinding;
        this.superIndexBinding = savedSuperIndexBinding;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:658:1.
    private Node createCaptureArgumentsStatement()
    {
        var variable = this.Factory().NewVariableDeclaration(this.lexicalArguments.binding, null, null, this.Factory().NewIdentifier("arguments"));
        var declList = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([variable])), NodeFlags.None);
        var statement = this.Factory().NewVariableStatement(null, declList);
        this.EmitContext().AddEmitFlags(statement, (EmitFlags.StartOnNewLine | EmitFlags.CustomPrologue));
        return statement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:671:1.
    private NodeList transformAsyncFunctionParameterList(Node node)
    {
        if (EsTransforms.IsSimpleParameterList(node.Parameters()))
        {
            return this.EmitContext().VisitParameters(node.ParameterList(), this.Visitor());
        }

        Slice<Node> newParameters = default;
        {
            foreach (var parameter in GoRuntime.RangeValues(node.Parameters()))
            {
                var param = parameter.AsParameterDeclaration();
                if (((param.Initializer != null) || (param.DotDotDotToken != null)))
                {
                    if ((node.Kind == Kind.ArrowFunction))
                    {
                        var restParameter = this.Factory().NewParameterDeclaration(null, this.Factory().NewToken(Kind.DotDotDotToken), this.Factory().NewUniqueNameEx("args", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.ReservedInNestedScopes }), null, null, null);
                        newParameters = newParameters.Append(restParameter);
                    }

                    break;
                }

                var newParameter = this.Factory().NewParameterDeclaration(null, null, this.Factory().NewGeneratedNameForNodeEx(param.Name(), new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.ReservedInNestedScopes }), null, null, null);
                newParameters = newParameters.Append(newParameter);
            }
        }

        var newParametersArray = this.Factory().NewNodeList(newParameters);
        newParametersArray.Loc = node.ParameterList().Loc;
        return newParametersArray;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:712:1.
    private Node transformAsyncFunctionBody(Node node, NodeList outerParameters)
    {
        var isArrow = (node.Kind == Kind.ArrowFunction);
        var savedCapturedSuperProperties = this.capturedSuperProperties;
        var savedHasSuperElementAccess = this.hasSuperElementAccess;
        var savedHasSuperPropertyAssignment = this.hasSuperPropertyAssignment;
        var savedSuperBinding = this.superBinding;
        var savedSuperIndexBinding = this.superIndexBinding;
        if (!isArrow)
        {
            this.capturedSuperProperties = new OrderedSet<ByteStringKey>
            {
            };
            this.hasSuperElementAccess = false;
            this.hasSuperPropertyAssignment = false;
            this.superBinding = this.Factory().NewUniqueNameEx("_super", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) });
            this.superIndexBinding = this.Factory().NewUniqueNameEx("_superIndex", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) });
        }

        NodeList innerParameters = null;
        if (!EsTransforms.IsSimpleParameterList(node.Parameters()))
        {
            innerParameters = this.EmitContext().VisitParameters(node.ParameterList(), this.Visitor());
        }

        var savedLexicalArguments = this.lexicalArguments;
        var captureLexicalArguments = (this.lexicalArguments.binding == null);
        if (captureLexicalArguments)
        {
            this.lexicalArguments = new LexicalArgumentsInfo
            {
                binding = this.Factory().NewUniqueName("arguments")
            };
        }

        Node argumentsExpression = default;
        if ((innerParameters != null))
        {
            if (isArrow)
            {
                Slice<Node> parameterBindings = default;
                var outerLen = GoRuntime.Len(outerParameters.Nodes);
                {
                    foreach (var range9 in GoRuntime.Range(node.Parameters()))
                    {
                        var i = range9.Key;
                        var param = range9.Value;
                        if ((i >= outerLen))
                        {
                            break;
                        }

                        var originalParameter = param.AsParameterDeclaration();
                        var outerParameter = outerParameters.Nodes[i].AsParameterDeclaration();
                        if (((originalParameter.Initializer != null) || (originalParameter.DotDotDotToken != null)))
                        {
                            parameterBindings = parameterBindings.Append(this.Factory().NewSpreadElement(outerParameter.Name()));
                            break;
                        }

                        parameterBindings = parameterBindings.Append(outerParameter.Name());
                    }
                }

                argumentsExpression = this.Factory().NewArrayLiteralExpression(this.Factory().NewNodeList(parameterBindings), false);
            }
            else
            {
                argumentsExpression = this.Factory().NewIdentifier("arguments");
            }
        }

        var savedEnclosingFunctionParameterNames = this.enclosingFunctionParameterNames;
        this.enclosingFunctionParameterNames = new HashSet<ByteStringKey>(ByteStringKey.Comparer);
        {
            foreach (var parameter in GoRuntime.RangeValues(node.Parameters()))
            {
                this.recordDeclarationName(parameter, this.enclosingFunctionParameterNames);
            }
        }

        var hasLexicalThis = this.inHasLexicalThisContext();
        var asyncBody = this.transformAsyncFunctionBodyWorker(node.Body());
        asyncBody = this.Factory().UpdateBlock(asyncBody.AsBlock(), this.EmitContext().EndAndMergeVariableEnvironmentList(asyncBody.StatementList()), asyncBody.AsBlock().MultiLine);
        var emitSuperHelpers = ((this.capturedSuperProperties != null) && (((this.capturedSuperProperties.Size() > 0) || this.hasSuperElementAccess)));
        if (emitSuperHelpers)
        {
            innerParameters = this.superAccessVisitor.VisitNodes(innerParameters);
            asyncBody = this.substituteSuperAccessesInBody(asyncBody);
        }

        Node result = default;
        if (!isArrow)
        {
            this.EmitContext().StartVariableEnvironment();
            if (emitSuperHelpers)
            {
                if ((this.capturedSuperProperties.Size() > 0))
                {
                    this.EmitContext().AddInitializationStatement(this.createSuperAccessVariableStatement());
                }
            }

            if ((captureLexicalArguments && this.lexicalArguments.used))
            {
                this.EmitContext().AddInitializationStatement(this.createCaptureArgumentsStatement());
            }

            var statements = Slice<Node>.FromArray([this.Factory().NewReturnStatement(this.Factory().NewAwaiterHelper(hasLexicalThis, argumentsExpression, innerParameters, asyncBody))]);
            var block = this.Factory().NewBlock(this.EmitContext().EndAndMergeVariableEnvironmentList(this.Factory().NewNodeList(statements)), true);
            block.Loc = node.Body().Loc;
            if ((emitSuperHelpers && this.hasSuperElementAccess))
            {
                if (this.hasSuperPropertyAssignment)
                {
                    this.EmitContext().AddEmitHelper(block, EmitHelpers.AdvancedAsyncSuperHelper);
                }
                else
                {
                    this.EmitContext().AddEmitHelper(block, EmitHelpers.AsyncSuperHelper);
                }
            }

            result = block;
        }
        else
        {
            result = this.Factory().NewAwaiterHelper(hasLexicalThis, argumentsExpression, innerParameters, asyncBody);
            if ((captureLexicalArguments && this.lexicalArguments.used))
            {
                var block2 = this.EmitContext().ConvertToFunctionBlock(result, true);
                if (!Ast.IsBlock(result))
                {
                    this.EmitContext().SetOriginal(block2.StatementList().Nodes[0], result);
                }

                result = this.Factory().UpdateBlock(block2.AsBlock(), this.EmitContext().MergeEnvironmentList(block2.StatementList(), Slice<Node>.FromArray([this.createCaptureArgumentsStatement()])), block2.AsBlock().MultiLine);
            }
        }

        this.enclosingFunctionParameterNames = savedEnclosingFunctionParameterNames;
        if (!isArrow)
        {
            this.capturedSuperProperties = savedCapturedSuperProperties;
            this.hasSuperElementAccess = savedHasSuperElementAccess;
            this.hasSuperPropertyAssignment = savedHasSuperPropertyAssignment;
            this.superBinding = savedSuperBinding;
            this.superIndexBinding = savedSuperIndexBinding;
            this.lexicalArguments = savedLexicalArguments;
        }
        else
        {
            if ((captureLexicalArguments && !this.lexicalArguments.used))
            {
                this.lexicalArguments = savedLexicalArguments;
            }
            else
            {
                if (captureLexicalArguments)
                {
                    this.lexicalArguments.used = false;
                }
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:876:1.
    private Node transformAsyncFunctionBodyWorker(Node body)
    {
        if (Ast.IsBlock(body))
        {
            return this.Factory().UpdateBlock(body.AsBlock(), this.asyncBodyVisitor.VisitNodes(body.StatementList()), body.AsBlock().MultiLine);
        }

        var visited = this.asyncBodyVisitor.VisitNode(body);
        var ret = this.Factory().NewReturnStatement(visited);
        ret.Loc = body.Loc;
        var list = this.Factory().NewNodeList(Slice<Node>.FromArray([ret]));
        list.Loc = body.Loc;
        var block = this.Factory().NewBlock(list, false);
        block.Loc = body.Loc;
        return block;
    }

    // assignmentTargetContainsSuperProperty checks top-down whether an assignment target
    // expression contains a super property or element access (super.x or super[x]).
    // This avoids relying on parent pointers (IsAssignmentTarget) which may not be set
    // on synthesized AST nodes from prior transforms.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:943:1.
    private Node getOriginalIfFunctionLike(Node node)
    {
        var original = this.EmitContext().MostOriginal(node);
        if (((original != null) && Ast.IsFunctionLikeDeclaration(original)))
        {
            return original;
        }

        return node;
    }
}

internal static partial class EsTransforms
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:899:1.
    internal static bool AssignmentTargetContainsSuperProperty(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                return (node.Expression().Kind == Kind.SuperKeyword);
            }

            case Kind.ParenthesizedExpression:
            {
                return AssignmentTargetContainsSuperProperty(node.AsParenthesizedExpression().Expression);
            }

            case Kind.ArrayLiteralExpression:
            {
                return Core.Some(node.AsArrayLiteralExpression().Elements.Nodes, AssignmentTargetContainsSuperProperty);
            }

            case Kind.ObjectLiteralExpression:
            {
                {
                    foreach (var prop in GoRuntime.RangeValues(node.AsObjectLiteralExpression().Properties.Nodes))
                    {
                        switch (prop.Kind)
                        {
                            case Kind.PropertyAssignment:
                            {
                                if (AssignmentTargetContainsSuperProperty(prop.AsPropertyAssignment().Initializer))
                                {
                                    return true;
                                }

                                break;
                            }

                            case Kind.ShorthandPropertyAssignment:
                            {
                                if (AssignmentTargetContainsSuperProperty(prop.AsShorthandPropertyAssignment().Name()))
                                {
                                    return true;
                                }

                                break;
                            }

                            case Kind.SpreadAssignment:
                            {
                                if (AssignmentTargetContainsSuperProperty(prop.AsSpreadAssignment().Expression))
                                {
                                    return true;
                                }

                                break;
                            }
                        }
                    }
                }

                break;
            }

            case Kind.SpreadElement:
            {
                return AssignmentTargetContainsSuperProperty(node.AsSpreadElement().Expression);
            }
        }

        return false;
    }

    // isUpdateExpression checks if a prefix/postfix unary expression is ++ or --.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:931:1.
    internal static bool IsUpdateExpression(Node node)
    {
        if (Ast.IsPrefixUnaryExpression(node))
        {
            var op = node.AsPrefixUnaryExpression().Operator;
            return ((op == Kind.PlusPlusToken) || (op == Kind.MinusMinusToken));
        }

        if (Ast.IsPostfixUnaryExpression(node))
        {
            var op2 = node.AsPostfixUnaryExpression().Operator;
            return ((op2 == Kind.PlusPlusToken) || (op2 == Kind.MinusMinusToken));
        }

        return false;
    }

    // isSimpleParameterList checks if every parameter has no initializer and an Identifier name.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:952:1.
    internal static bool IsSimpleParameterList(Slice<Node> @params)
    {
        {
            foreach (var param in GoRuntime.RangeValues(@params))
            {
                var p = param.AsParameterDeclaration();
                if (((p.Initializer != null) || !Ast.IsIdentifier(p.Name())))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // isNodeWithPossibleHoistedDeclaration checks if a node could contain hoisted declarations.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/async.go:963:1.
    internal static bool IsNodeWithPossibleHoistedDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Block:
            case Kind.VariableStatement:
            case Kind.WithStatement:
            case Kind.IfStatement:
            case Kind.SwitchStatement:
            case Kind.CaseBlock:
            case Kind.CaseClause:
            case Kind.DefaultClause:
            case Kind.LabeledStatement:
            case Kind.ForStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.DoStatement:
            case Kind.WhileStatement:
            case Kind.TryStatement:
            case Kind.CatchClause:
            {
                return true;
            }
        }

        return false;
    }
}
