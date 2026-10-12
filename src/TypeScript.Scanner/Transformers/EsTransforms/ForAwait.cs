// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/forawait.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Collections;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class ForAwaitTransformer : SuperAccessTransformer
{
    [Flags]
    private enum ForAwaitHierarchyFacts
    {
        None = 0,
        HasLexicalThis = 1,
        IterationContainer = 2,
        AncestorFactsMask = 3
    }

    // Original fields are assigned but never read in pinned forawait.go.
#pragma warning disable CS0414
    private CompilerOptions compilerOptions;
    private FunctionFlags enclosingFunctionFlags;
    private ForAwaitHierarchyFacts hierarchyFacts;
    private bool exportedVariableStatement;
#pragma warning restore CS0414
    private NodeVisitor fallbackNodeVisitor, noAsyncModifierVisitor;
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:56:1.
    internal static Transformer NewForAwaitTransformer(TransformOptions opts)
    {
        var tx = new ForAwaitTransformer
        {
            compilerOptions = opts.CompilerOptions
        };
        var result = tx.NewTransformer(tx.visit, opts.Context);
        tx.initSuperAccessVisitor(tx.EmitContext(), tx.Factory());
        tx.fallbackNodeVisitor = tx.EmitContext().NewNodeVisitor(tx.visitFallback);
        tx.noAsyncModifierVisitor = tx.EmitContext().NewNodeVisitor((node) =>
        {
            if ((node.Kind == Kind.AsyncKeyword))
            {
                return null;
            }

            return node;
        });
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:72:1.
    private bool affectsSubtree(ForAwaitHierarchyFacts excludeFacts, ForAwaitHierarchyFacts includeFacts)
    {
        return (this.hierarchyFacts != (((this.hierarchyFacts & ~excludeFacts) | includeFacts)));
    }

    // enterSubtree sets the HierarchyFacts for this node prior to visiting this node's subtree,
    // returning the facts set prior to modification.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:78:1.
    private ForAwaitHierarchyFacts enterSubtree(ForAwaitHierarchyFacts excludeFacts, ForAwaitHierarchyFacts includeFacts)
    {
        var ancestorFacts = this.hierarchyFacts;
        this.hierarchyFacts = ((((this.hierarchyFacts & ~excludeFacts) | includeFacts)) & ForAwaitHierarchyFacts.AncestorFactsMask);
        return ancestorFacts;
    }

    // exitSubtree restores the HierarchyFacts for this node's ancestor after visiting this node's
    // subtree.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:86:1.
    private void exitSubtree(ForAwaitHierarchyFacts ancestorFacts)
    {
        this.hierarchyFacts = ancestorFacts;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:90:1.
    private ModifierList visitModifiersNoAsync(ModifierList modifiers)
    {
        return this.noAsyncModifierVisitor.VisitModifiers(modifiers);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:94:1.
    private Node doWithHierarchyFacts(Func<ForAwaitTransformer, Node, Node> cb, Node node, ForAwaitHierarchyFacts excludeFacts, ForAwaitHierarchyFacts includeFacts)
    {
        if (this.affectsSubtree(excludeFacts, includeFacts))
        {
            var ancestorFacts = this.enterSubtree(excludeFacts, includeFacts);
            var result = cb(this, node);
            this.exitSubtree(ancestorFacts);
            return result;
        }

        return cb(this, node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:104:1.
    private Node visitDefault(Node node)
    {
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:108:1.
    private Node fallbackVisitor(Node node)
    {
        if ((this.capturedSuperProperties == null))
        {
            return node;
        }

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
        }

        this.trackSuperAccess(node);
        return this.fallbackNodeVisitor.VisitEachChild(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:122:1.
    private Node visitFallback(Node node)
    {
        return this.fallbackVisitor(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:126:1.
    private Node visit(Node node)
    {
        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsForAwaitOrAsyncGenerator) == 0))
        {
            return this.fallbackVisitor(node);
        }

        this.trackSuperAccess(node);
        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                return this.visitSourceFile(node.AsSourceFile());
            }

            case Kind.AwaitExpression:
            {
                return this.visitAwaitExpression(node.AsAwaitExpression());
            }

            case Kind.YieldExpression:
            {
                return this.visitYieldExpression(node.AsYieldExpression());
            }

            case Kind.ReturnStatement:
            {
                return this.visitReturnStatement(node.AsReturnStatement());
            }

            case Kind.LabeledStatement:
            {
                return this.visitLabeledStatement(node.AsLabeledStatement());
            }

            case Kind.DoStatement:
            case Kind.WhileStatement:
            case Kind.ForInStatement:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitDefault(n), node, ForAwaitHierarchyFacts.None, ForAwaitHierarchyFacts.IterationContainer);
            }

            case Kind.ForOfStatement:
            {
                return this.visitForOfStatement(node.AsForInOrOfStatement(), null);
            }

            case Kind.ForStatement:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitDefault(n), node, ForAwaitHierarchyFacts.None, ForAwaitHierarchyFacts.IterationContainer);
            }

            case Kind.Constructor:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitConstructorDeclaration(n), node, ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.HasLexicalThis);
            }

            case Kind.MethodDeclaration:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitMethodDeclaration(n), node, ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.HasLexicalThis);
            }

            case Kind.GetAccessor:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitGetAccessorDeclaration(n), node, ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.HasLexicalThis);
            }

            case Kind.SetAccessor:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitSetAccessorDeclaration(n), node, ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.HasLexicalThis);
            }

            case Kind.FunctionDeclaration:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitFunctionDeclaration(n), node, ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.HasLexicalThis);
            }

            case Kind.FunctionExpression:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitFunctionExpression(n), node, ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.HasLexicalThis);
            }

            case Kind.ArrowFunction:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitArrowFunction(n), node, ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.None);
            }

            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            {
                return this.doWithHierarchyFacts(static (tx, n) => tx.visitDefault(n), node, ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.HasLexicalThis);
            }

            default:
            {
                return this.Visitor().VisitEachChild(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:219:1.
    private Node visitAwaitExpression(AwaitExpression node)
    {
        if ((((this.enclosingFunctionFlags & FunctionFlags.Async) != 0) && ((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0)))
        {
            var result = this.Factory().NewYieldExpression(null, this.Factory().NewAwaitHelper(this.Visitor().VisitNode(node.Expression)));
            result.Loc = node.Loc;
            this.EmitContext().SetOriginal(result, node.AsNode());
            return result;
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:232:1.
    private Node visitYieldExpression(YieldExpression node)
    {
        if ((((this.enclosingFunctionFlags & FunctionFlags.Async) != 0) && ((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0)))
        {
            if ((node.AsteriskToken != null))
            {
                var expression = this.Visitor().VisitNode(node.Expression);
                var asyncValuesResult = this.Factory().NewAsyncValuesHelper(expression);
                asyncValuesResult.Loc = expression.Loc;
                var asyncDelegatorResult = this.Factory().NewAsyncDelegatorHelper(asyncValuesResult);
                asyncDelegatorResult.Loc = expression.Loc;
                var innerYield = this.Factory().UpdateYieldExpression(node, node.AsteriskToken, asyncDelegatorResult);
                var awaitedYield = this.Factory().NewAwaitHelper(innerYield);
                var result = this.Factory().NewYieldExpression(null, awaitedYield);
                result.Loc = node.Loc;
                this.EmitContext().SetOriginal(result, node.AsNode());
                return result;
            }

            Node innerExpression = default;
            if ((node.Expression != null))
            {
                innerExpression = this.Visitor().VisitNode(node.Expression);
            }
            else
            {
                innerExpression = this.Factory().NewVoidZeroExpression();
            }

            var result2 = this.Factory().NewYieldExpression(null, this.createDownlevelAwait(innerExpression));
            result2.Loc = node.Loc;
            this.EmitContext().SetOriginal(result2, node.AsNode());
            return result2;
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:279:1.
    private Node visitReturnStatement(ReturnStatement node)
    {
        if ((((this.enclosingFunctionFlags & FunctionFlags.Async) != 0) && ((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0)))
        {
            Node expression = default;
            if ((node.Expression != null))
            {
                expression = this.Visitor().VisitNode(node.Expression);
            }
            else
            {
                expression = this.Factory().NewVoidZeroExpression();
            }

            return this.Factory().UpdateReturnStatement(node, this.createDownlevelAwait(expression));
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:296:1.
    private Node visitLabeledStatement(LabeledStatement node)
    {
        if (((this.enclosingFunctionFlags & FunctionFlags.Async) != 0))
        {
            var statement = unwrapInnermostStatementOfLabel(node);
            if (((statement.Kind == Kind.ForOfStatement) && (statement.AsForInOrOfStatement().AwaitModifier != null)))
            {
                return this.visitForOfStatement(statement.AsForInOrOfStatement(), node);
            }

            return this.Factory().RestoreEnclosingLabel(this.Visitor().VisitNode(statement), node);
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // unwrapInnermostStatementOfLabel follows LabeledStatement chains to find the innermost statement.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:308:1.
    private static Node unwrapInnermostStatementOfLabel(LabeledStatement node)
    {
        while (true)
        {
            if ((node.Statement.Kind != Kind.LabeledStatement))
            {
                return node.Statement;
            }

            node = node.Statement.AsLabeledStatement();
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:317:1.
    private Node visitSourceFile(SourceFile node)
    {
        var ancestorFacts = this.enterSubtree(ForAwaitHierarchyFacts.IterationContainer, ForAwaitHierarchyFacts.None);
        this.exportedVariableStatement = false;
        var visited = this.Visitor().VisitEachChild(node.AsNode());
        this.EmitContext().AddEmitHelper(visited, this.EmitContext().ReadEmitHelpers());
        this.exitSubtree(ancestorFacts);
        return visited;
    }

    // visitForOfStatement visits a ForOfStatement and converts it into a ES2015-compatible ForOfStatement.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:330:1.
    private Node visitForOfStatement(ForInOrOfStatement node, LabeledStatement outermostLabeledStatement)
    {
        var ancestorFacts = this.enterSubtree(ForAwaitHierarchyFacts.None, ForAwaitHierarchyFacts.IterationContainer);
        Node result = default;
        if ((node.AwaitModifier != null))
        {
            result = this.transformForAwaitOfStatement(node, outermostLabeledStatement, ancestorFacts);
        }
        else
        {
            result = this.Factory().RestoreEnclosingLabel(this.Visitor().VisitEachChild(node.AsNode()), outermostLabeledStatement);
        }

        this.exitSubtree(ancestorFacts);
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:342:1.
    private Node convertForOfStatementHead(ForInOrOfStatement node, Node boundValue, Node nonUserCode)
    {
        var f = this.Factory();
        var value = f.NewTempVariable();
        this.EmitContext().AddVariableDeclaration(value);
        var iteratorValueExpression = f.NewAssignmentExpression(value, boundValue);
        var iteratorValueStatement = f.NewExpressionStatement(iteratorValueExpression);
        this.EmitContext().SetSourceMapRange(iteratorValueStatement, node.Expression.Loc);
        var exitNonUserCodeExpression = f.NewAssignmentExpression(nonUserCode, f.NewKeywordExpression(Kind.FalseKeyword));
        var exitNonUserCodeStatement = f.NewExpressionStatement(exitNonUserCodeExpression);
        this.EmitContext().SetSourceMapRange(exitNonUserCodeStatement, node.Expression.Loc);
        var statements = Slice<Node>.FromArray([iteratorValueStatement, exitNonUserCodeStatement]);
        var binding = this.Factory().CreateForOfBindingStatement(node.Initializer, value);
        statements = statements.Append(this.Visitor().VisitNode(binding));
        TextRange bodyLocation = default;
        TextRange statementsLocation = default;
        var statement = this.Visitor().VisitEmbeddedStatement(node.Statement);
        if (Ast.IsBlock(statement))
        {
            statements = statements.Append(statement.Statements());
            bodyLocation = statement.Loc;
            statementsLocation = statement.StatementList().Loc;
        }
        else
        {
            statements = statements.Append(statement);
        }

        var stmtList = f.NewNodeList(statements);
        stmtList.Loc = statementsLocation;
        var block = f.NewBlock(stmtList, true);
        block.Loc = bodyLocation;
        return block;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:376:1.
    private Node createDownlevelAwait(Node expression)
    {
        if (((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0))
        {
            return this.Factory().NewYieldExpression(null, this.Factory().NewAwaitHelper(expression));
        }

        return this.Factory().NewAwaitExpression(expression);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:386:1.
    private Node transformForAwaitOfStatement(ForInOrOfStatement node, LabeledStatement outermostLabeledStatement, ForAwaitHierarchyFacts ancestorFacts)
    {
        var f = this.Factory();
        var expression = this.Visitor().VisitNode(node.Expression);
        Node iterator = default;
        if (Ast.IsIdentifier(expression))
        {
            iterator = f.NewGeneratedNameForNode(expression);
        }
        else
        {
            iterator = f.NewTempVariable();
        }

        Node result = default;
        if (Ast.IsIdentifier(expression))
        {
            result = f.NewGeneratedNameForNode(iterator);
        }
        else
        {
            result = f.NewTempVariable();
        }

        var nonUserCode = f.NewTempVariable();
        var done = f.NewTempVariable();
        this.EmitContext().AddVariableDeclaration(done);
        var errorRecord = f.NewUniqueName("e");
        var catchVariable = f.NewGeneratedNameForNode(errorRecord);
        var returnMethod = f.NewTempVariable();
        var callValues = f.NewAsyncValuesHelper(expression);
        callValues.Loc = node.Expression.Loc;
        var callNext = f.NewCallExpression(f.NewPropertyAccessExpression(iterator, null, f.NewIdentifier("next"), NodeFlags.None), null, null, f.NewNodeList(Slice<Node>.Make(0)), NodeFlags.None);
        var getDone = f.NewPropertyAccessExpression(result, null, f.NewIdentifier("done"), NodeFlags.None);
        var getValue = f.NewPropertyAccessExpression(result, null, f.NewIdentifier("value"), NodeFlags.None);
        var callReturn = f.NewFunctionCallCall(returnMethod, iterator, Slice<Node>.Make(0));
        this.EmitContext().AddVariableDeclaration(errorRecord);
        this.EmitContext().AddVariableDeclaration(returnMethod);
        Node initializer = default;
        if (((ancestorFacts & ForAwaitHierarchyFacts.IterationContainer) != 0))
        {
            initializer = f.InlineExpressions(Slice<Node>.FromArray([f.NewAssignmentExpression(errorRecord, f.NewVoidZeroExpression()), callValues]));
        }
        else
        {
            initializer = callValues;
        }

        var iteratorDecl = f.NewVariableDeclaration(iterator, null, null, initializer);
        iteratorDecl.Loc = node.Expression.Loc;
        var varDeclList = f.NewVariableDeclarationList(f.NewNodeList(Slice<Node>.FromArray([f.NewVariableDeclaration(nonUserCode, null, null, f.NewKeywordExpression(Kind.TrueKeyword)), iteratorDecl, f.NewVariableDeclaration(result, null, null, null)])), NodeFlags.None);
        varDeclList.Loc = node.Expression.Loc;
        var condition = f.InlineExpressions(Slice<Node>.FromArray([f.NewAssignmentExpression(result, this.createDownlevelAwait(callNext)), f.NewAssignmentExpression(done, getDone), f.NewPrefixUnaryExpression(Kind.ExclamationToken, done)]));
        var incrementor = f.NewAssignmentExpression(nonUserCode, f.NewKeywordExpression(Kind.TrueKeyword));
        var forStatement = f.NewForStatement(varDeclList, condition, incrementor, this.convertForOfStatementHead(node, getValue, nonUserCode));
        forStatement.Loc = node.Loc;
        this.EmitContext().AddEmitFlags(forStatement, EmitFlags.NoTokenTrailingSourceMaps);
        this.EmitContext().SetOriginal(forStatement, node.AsNode());
        var tryBlock = f.NewBlock(f.NewNodeList(Slice<Node>.FromArray([f.RestoreEnclosingLabel(forStatement, outermostLabeledStatement)])), true);
        var catchBody = f.NewBlock(f.NewNodeList(Slice<Node>.FromArray([f.NewExpressionStatement(f.NewAssignmentExpression(errorRecord, f.NewObjectLiteralExpression(f.NewNodeList(Slice<Node>.FromArray([f.NewPropertyAssignment(null, f.NewIdentifier("error"), null, null, catchVariable)])), false)))])), false);
        this.EmitContext().AddEmitFlags(catchBody, EmitFlags.SingleLine);
        var catchClause = f.NewCatchClause(f.NewVariableDeclaration(catchVariable, null, null, null), catchBody);
        var innerIfCondition = f.NewBinaryExpression(null, f.NewBinaryExpression(null, f.NewPrefixUnaryExpression(Kind.ExclamationToken, nonUserCode), null, f.NewToken(Kind.AmpersandAmpersandToken), f.NewPrefixUnaryExpression(Kind.ExclamationToken, done)), null, f.NewToken(Kind.AmpersandAmpersandToken), f.NewAssignmentExpression(returnMethod, f.NewPropertyAccessExpression(iterator, null, f.NewIdentifier("return"), NodeFlags.None)));
        var innerIfStatement = f.NewIfStatement(innerIfCondition, f.NewExpressionStatement(this.createDownlevelAwait(callReturn)), null);
        this.EmitContext().AddEmitFlags(innerIfStatement, EmitFlags.SingleLine);
        var innerTryBlock = f.NewBlock(f.NewNodeList(Slice<Node>.FromArray([innerIfStatement])), false);
        var innerFinallyIf = f.NewIfStatement(errorRecord, f.NewThrowStatement(f.NewPropertyAccessExpression(errorRecord, null, f.NewIdentifier("error"), NodeFlags.None)), null);
        this.EmitContext().AddEmitFlags(innerFinallyIf, EmitFlags.SingleLine);
        var innerFinallyBlock = f.NewBlock(f.NewNodeList(Slice<Node>.FromArray([innerFinallyIf])), false);
        this.EmitContext().AddEmitFlags(innerFinallyBlock, EmitFlags.SingleLine);
        var innerTryStatement = f.NewTryStatement(innerTryBlock, null, innerFinallyBlock);
        var finallyBlock = f.NewBlock(f.NewNodeList(Slice<Node>.FromArray([innerTryStatement])), true);
        return f.NewTryStatement(tryBlock, catchClause, finallyBlock);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:531:1.
    private Node visitConstructorDeclaration(Node node)
    {
        var decl = node.AsConstructorDeclaration();
        var savedEnclosingFunctionFlags = this.enclosingFunctionFlags;
        this.enclosingFunctionFlags = Ast.GetFunctionFlags(node);
        var updated = this.Factory().UpdateConstructorDeclaration(decl, decl.Modifiers(), null, this.EmitContext().VisitParameters(decl.Parameters, this.Visitor()), null, null, this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor()));
        this.enclosingFunctionFlags = savedEnclosingFunctionFlags;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:548:1.
    private Node visitGetAccessorDeclaration(Node node)
    {
        var decl = node.AsGetAccessorDeclaration();
        var savedEnclosingFunctionFlags = this.enclosingFunctionFlags;
        this.enclosingFunctionFlags = Ast.GetFunctionFlags(node);
        var updated = this.Factory().UpdateGetAccessorDeclaration(decl, decl.Modifiers(), this.Visitor().VisitNode(decl.Name()), null, this.EmitContext().VisitParameters(decl.Parameters, this.Visitor()), null, null, this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor()));
        this.enclosingFunctionFlags = savedEnclosingFunctionFlags;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:566:1.
    private Node visitSetAccessorDeclaration(Node node)
    {
        var decl = node.AsSetAccessorDeclaration();
        var savedEnclosingFunctionFlags = this.enclosingFunctionFlags;
        this.enclosingFunctionFlags = Ast.GetFunctionFlags(node);
        var updated = this.Factory().UpdateSetAccessorDeclaration(decl, decl.Modifiers(), this.Visitor().VisitNode(decl.Name()), null, this.EmitContext().VisitParameters(decl.Parameters, this.Visitor()), null, null, this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor()));
        this.enclosingFunctionFlags = savedEnclosingFunctionFlags;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:584:1.
    private Node visitMethodDeclaration(Node node)
    {
        var decl = node.AsMethodDeclaration();
        var savedEnclosingFunctionFlags = this.enclosingFunctionFlags;
        this.enclosingFunctionFlags = Ast.GetFunctionFlags(node);
        ModifierList modifiers = default;
        if (((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0))
        {
            modifiers = this.visitModifiersNoAsync(decl.Modifiers());
        }
        else
        {
            modifiers = decl.Modifiers();
        }

        Node asteriskToken = default;
        if (((this.enclosingFunctionFlags & FunctionFlags.Async) != 0))
        {
            asteriskToken = null;
        }
        else
        {
            asteriskToken = decl.AsteriskToken;
        }

        NodeList parameters = default;
        Node body = default;
        if ((((this.enclosingFunctionFlags & FunctionFlags.Async) != 0) && ((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0)))
        {
            parameters = this.transformAsyncGeneratorFunctionParameterList(node);
            body = this.transformAsyncGeneratorFunctionBody(node);
        }
        else
        {
            parameters = this.EmitContext().VisitParameters(decl.Parameters, this.Visitor());
            body = this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor());
        }

        var updated = this.Factory().UpdateMethodDeclaration(decl, modifiers, asteriskToken, this.Visitor().VisitNode(decl.Name()), null, null, parameters, null, null, body);
        this.enclosingFunctionFlags = savedEnclosingFunctionFlags;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:629:1.
    private Node visitFunctionDeclaration(Node node)
    {
        var decl = node.AsFunctionDeclaration();
        var savedEnclosingFunctionFlags = this.enclosingFunctionFlags;
        this.enclosingFunctionFlags = Ast.GetFunctionFlags(node);
        ModifierList modifiers = default;
        if (((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0))
        {
            modifiers = this.visitModifiersNoAsync(decl.Modifiers());
        }
        else
        {
            modifiers = decl.Modifiers();
        }

        Node asteriskToken = default;
        if (((this.enclosingFunctionFlags & FunctionFlags.Async) != 0))
        {
            asteriskToken = null;
        }
        else
        {
            asteriskToken = decl.AsteriskToken;
        }

        NodeList parameters = default;
        Node body = default;
        if ((((this.enclosingFunctionFlags & FunctionFlags.Async) != 0) && ((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0)))
        {
            parameters = this.transformAsyncGeneratorFunctionParameterList(node);
            body = this.transformAsyncGeneratorFunctionBody(node);
        }
        else
        {
            parameters = this.EmitContext().VisitParameters(decl.Parameters, this.Visitor());
            body = this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor());
        }

        var updated = this.Factory().UpdateFunctionDeclaration(decl, modifiers, asteriskToken, decl.Name(), null, parameters, null, null, body);
        this.enclosingFunctionFlags = savedEnclosingFunctionFlags;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:673:1.
    private Node visitArrowFunction(Node node)
    {
        var decl = node.AsArrowFunction();
        var savedEnclosingFunctionFlags = this.enclosingFunctionFlags;
        this.enclosingFunctionFlags = Ast.GetFunctionFlags(node);
        var updated = this.Factory().UpdateArrowFunction(decl, decl.Modifiers(), null, this.EmitContext().VisitParameters(decl.Parameters, this.Visitor()), null, null, decl.EqualsGreaterThanToken, this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor()));
        this.enclosingFunctionFlags = savedEnclosingFunctionFlags;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:691:1.
    private Node visitFunctionExpression(Node node)
    {
        var decl = node.AsFunctionExpression();
        var savedEnclosingFunctionFlags = this.enclosingFunctionFlags;
        this.enclosingFunctionFlags = Ast.GetFunctionFlags(node);
        ModifierList modifiers = default;
        if (((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0))
        {
            modifiers = this.visitModifiersNoAsync(decl.Modifiers());
        }
        else
        {
            modifiers = decl.Modifiers();
        }

        Node asteriskToken = default;
        if (((this.enclosingFunctionFlags & FunctionFlags.Async) != 0))
        {
            asteriskToken = null;
        }
        else
        {
            asteriskToken = decl.AsteriskToken;
        }

        NodeList parameters = default;
        Node body = default;
        if ((((this.enclosingFunctionFlags & FunctionFlags.Async) != 0) && ((this.enclosingFunctionFlags & FunctionFlags.Generator) != 0)))
        {
            parameters = this.transformAsyncGeneratorFunctionParameterList(node);
            body = this.transformAsyncGeneratorFunctionBody(node);
        }
        else
        {
            parameters = this.EmitContext().VisitParameters(decl.Parameters, this.Visitor());
            body = this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor());
        }

        var updated = this.Factory().UpdateFunctionExpression(decl, modifiers, asteriskToken, decl.Name(), null, parameters, null, null, body);
        this.enclosingFunctionFlags = savedEnclosingFunctionFlags;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:735:1.
    private NodeList transformAsyncGeneratorFunctionParameterList(Node node)
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

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go:761:1.
    private Node transformAsyncGeneratorFunctionBody(Node node)
    {
        var f = this.Factory();
        NodeList innerParameters = default;
        if (!EsTransforms.IsSimpleParameterList(node.Parameters()))
        {
            innerParameters = this.EmitContext().VisitParameters(node.ParameterList(), this.Visitor());
        }

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
        this.superBinding = f.NewUniqueNameEx("_super", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) });
        this.superIndexBinding = f.NewUniqueNameEx("_superIndex", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) });
        var asyncBody = f.UpdateBlock(node.Body().AsBlock(), this.Visitor().VisitNodes(node.Body().StatementList()), node.Body().AsBlock().MultiLine);
        asyncBody = f.UpdateBlock(asyncBody.AsBlock(), this.EmitContext().EndAndMergeVariableEnvironmentList(asyncBody.StatementList()), asyncBody.AsBlock().MultiLine);
        var emitSuperHelpers = ((this.capturedSuperProperties.Size() > 0) || this.hasSuperElementAccess);
        if (emitSuperHelpers)
        {
            asyncBody = this.substituteSuperAccessesInBody(asyncBody);
        }

        NodeList innerParams = default;
        if ((innerParameters != null))
        {
            innerParams = innerParameters;
        }
        else
        {
            innerParams = f.NewNodeList(Slice<Node>.Make(0));
        }

        Node name = default;
        if ((node.Name() != null))
        {
            name = f.NewGeneratedNameForNode(node.Name());
        }

        var generatorFunc = f.NewFunctionExpression(null, f.NewToken(Kind.AsteriskToken), name, null, innerParams, null, null, asyncBody);
        var returnStatement = f.NewReturnStatement(f.NewAsyncGeneratorHelper(generatorFunc, ((this.hierarchyFacts & ForAwaitHierarchyFacts.HasLexicalThis) != 0)));
        this.EmitContext().StartVariableEnvironment();
        if (emitSuperHelpers)
        {
            if ((this.capturedSuperProperties.Size() > 0))
            {
                this.EmitContext().AddInitializationStatement(this.createSuperAccessVariableStatement());
            }
        }

        var outerStatements = Slice<Node>.FromArray([returnStatement]);
        var block = f.UpdateBlock(node.Body().AsBlock(), this.EmitContext().EndAndMergeVariableEnvironmentList(f.NewNodeList(outerStatements)), node.Body().AsBlock().MultiLine);
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

        this.capturedSuperProperties = savedCapturedSuperProperties;
        this.hasSuperElementAccess = savedHasSuperElementAccess;
        this.hasSuperPropertyAssignment = savedHasSuperPropertyAssignment;
        this.superBinding = savedSuperBinding;
        this.superIndexBinding = savedSuperIndexBinding;
        return block;
    }
}
