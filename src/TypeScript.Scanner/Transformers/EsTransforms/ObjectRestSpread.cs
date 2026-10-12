// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/objectrestspread.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Collections;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class ObjectRestSpreadTransformer : Transformer
{
    private CompilerOptions compilerOptions;
    private bool inExportedVariableStatement, expressionResultIsUnused;
    private Dictionary<Node, bool> parametersWithPrecedingObjectRestOrSpread;
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:20:1.
    private Node visit(Node node)
    {
        if (((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsESObjectRestOrSpread) == 0) && (this.parametersWithPrecedingObjectRestOrSpread == null)))
        {
            return node;
        }

        var expressionResultIsUnused = this.expressionResultIsUnused;
        this.expressionResultIsUnused = false;
        try
        {
            switch (node.Kind)
            {
                case Kind.SourceFile:
                {
                    return this.visitSourceFile(node.AsSourceFile());
                }

                case Kind.ObjectLiteralExpression:
                {
                    return this.visitObjectLiteralExpression(node.AsObjectLiteralExpression());
                }

                case Kind.BinaryExpression:
                {
                    return this.visitBinaryExpression(node.AsBinaryExpression(), expressionResultIsUnused);
                }

                case Kind.ExpressionStatement:
                {
                    this.expressionResultIsUnused = true;
                    return this.Visitor().VisitEachChild(node);
                }

                case Kind.ParenthesizedExpression:
                {
                    this.expressionResultIsUnused = expressionResultIsUnused;
                    return this.Visitor().VisitEachChild(node);
                }

                case Kind.ForOfStatement:
                {
                    return this.visitForOftatement(node.AsForInOrOfStatement());
                }

                case Kind.VariableStatement:
                {
                    return this.visitVariableStatement(node.AsVariableStatement());
                }

                case Kind.VariableDeclaration:
                {
                    return this.visitVariableDeclaration(node.AsVariableDeclaration());
                }

                case Kind.CatchClause:
                {
                    return this.visitCatchClause(node.AsCatchClause());
                }

                case Kind.Parameter:
                {
                    return this.visitParameter(node.AsParameterDeclaration());
                }

                case Kind.Constructor:
                {
                    return this.visitContructorDeclaration(node.AsConstructorDeclaration());
                }

                case Kind.GetAccessor:
                {
                    return this.visitGetAccessorDeclaration(node.AsGetAccessorDeclaration());
                }

                case Kind.SetAccessor:
                {
                    return this.visitSetAccessorDeclaration(node.AsSetAccessorDeclaration());
                }

                case Kind.MethodDeclaration:
                {
                    return this.visitMethodDeclaration(node.AsMethodDeclaration());
                }

                case Kind.FunctionDeclaration:
                {
                    return this.visitFunctionDeclaration(node.AsFunctionDeclaration());
                }

                case Kind.ArrowFunction:
                {
                    return this.visitArrowFunction(node.AsArrowFunction());
                }

                case Kind.FunctionExpression:
                {
                    return this.visitFunctionExpression(node.AsFunctionExpression());
                }

                default:
                {
                    return this.Visitor().VisitEachChild(node);
                }
            }
        }
        finally
        {
            this.expressionResultIsUnused = expressionResultIsUnused;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:71:1.
    private Node visitSourceFile(SourceFile node)
    {
        var visited = this.Visitor().VisitEachChild(node.AsNode());
        this.EmitContext().AddEmitHelper(visited.AsNode(), this.EmitContext().ReadEmitHelpers());
        return visited;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:77:1.
    private Node visitParameter(ParameterDeclaration node)
    {
        if ((this.parametersWithPrecedingObjectRestOrSpread != null))
        {
            {
                var ok = this.parametersWithPrecedingObjectRestOrSpread.TryGetValue(node.AsNode(), out var _);
                if (ok)
                {
                    var name = node.Name();
                    if (Ast.IsBindingPattern(name))
                    {
                        name = this.Factory().NewGeneratedNameForNode(node.AsNode());
                    }

                    return this.Factory().UpdateParameterDeclaration(node, null, node.DotDotDotToken, name, null, null, null);
                }
            }
        }

        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsObjectRestOrSpread) != 0))
        {
            return this.Factory().UpdateParameterDeclaration(node, null, node.DotDotDotToken, this.Factory().NewGeneratedNameForNode(node.AsNode()), null, null, this.Visitor().VisitNode(node.Initializer));
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:111:1.
    private Dictionary<Node, bool> collectParametersWithPrecedingObjectRestOrSpread(Node node)
    {
        Dictionary<Node, bool> result = default;
        {
            foreach (var parameter in GoRuntime.RangeValues(node.Parameters()))
            {
                if ((result != null))
                {
                    result[parameter] = true;
                }
                else
                {
                    if ((((uint)parameter.SubtreeFacts() & SubtreeFactValues.ContainsObjectRestOrSpread) != 0))
                    {
                        result = new Dictionary<Node, bool>();
                    }
                }
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:125:1.
    private Dictionary<Node, bool> enterParameterListContext(Node node)
    {
        var old = this.parametersWithPrecedingObjectRestOrSpread;
        this.parametersWithPrecedingObjectRestOrSpread = this.collectParametersWithPrecedingObjectRestOrSpread(node);
        return old;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:131:1.
    private void exitParameterListContext(Dictionary<Node, bool> scope)
    {
        this.parametersWithPrecedingObjectRestOrSpread = scope;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:135:1.
    private Node visitContructorDeclaration(ConstructorDeclaration node)
    {
        var old = this.enterParameterListContext(node.AsNode());
        try
        {
            return this.Factory().UpdateConstructorDeclaration(node, node.Modifiers(), null, this.Visitor().VisitNodes(node.Parameters), null, null, this.transformFunctionBody(node.AsNode()));
        }
        finally
        {
            this.exitParameterListContext(old);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:149:1.
    private Node visitGetAccessorDeclaration(GetAccessorDeclaration node)
    {
        var old = this.enterParameterListContext(node.AsNode());
        try
        {
            return this.Factory().UpdateGetAccessorDeclaration(node, node.Modifiers(), this.Visitor().VisitNode(node.Name()), null, this.Visitor().VisitNodes(node.Parameters), null, null, this.transformFunctionBody(node.AsNode()));
        }
        finally
        {
            this.exitParameterListContext(old);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:164:1.
    private Node visitSetAccessorDeclaration(SetAccessorDeclaration node)
    {
        var old = this.enterParameterListContext(node.AsNode());
        try
        {
            return this.Factory().UpdateSetAccessorDeclaration(node, node.Modifiers(), this.Visitor().VisitNode(node.Name()), null, this.Visitor().VisitNodes(node.Parameters), null, null, this.transformFunctionBody(node.AsNode()));
        }
        finally
        {
            this.exitParameterListContext(old);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:179:1.
    private Node visitMethodDeclaration(MethodDeclaration node)
    {
        var old = this.enterParameterListContext(node.AsNode());
        try
        {
            return this.Factory().UpdateMethodDeclaration(node, node.Modifiers(), node.AsteriskToken, this.Visitor().VisitNode(node.Name()), node.PostfixToken, null, this.Visitor().VisitNodes(node.Parameters), null, null, this.transformFunctionBody(node.AsNode()));
        }
        finally
        {
            this.exitParameterListContext(old);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:196:1.
    private Node visitFunctionDeclaration(FunctionDeclaration node)
    {
        var old = this.enterParameterListContext(node.AsNode());
        try
        {
            return this.Factory().UpdateFunctionDeclaration(node, node.Modifiers(), node.AsteriskToken, this.Visitor().VisitNode(node.Name()), null, this.Visitor().VisitNodes(node.Parameters), null, null, this.transformFunctionBody(node.AsNode()));
        }
        finally
        {
            this.exitParameterListContext(old);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:212:1.
    private Node visitArrowFunction(ArrowFunction node)
    {
        var old = this.enterParameterListContext(node.AsNode());
        try
        {
            return this.Factory().UpdateArrowFunction(node, node.Modifiers(), null, this.Visitor().VisitNodes(node.Parameters), null, null, node.EqualsGreaterThanToken, this.transformFunctionBody(node.AsNode()));
        }
        finally
        {
            this.exitParameterListContext(old);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:227:1.
    private Node visitFunctionExpression(FunctionExpression node)
    {
        var old = this.enterParameterListContext(node.AsNode());
        try
        {
            return this.Factory().UpdateFunctionExpression(node, node.Modifiers(), node.AsteriskToken, this.Visitor().VisitNode(node.Name()), null, this.Visitor().VisitNodes(node.Parameters), null, null, this.transformFunctionBody(node.AsNode()));
        }
        finally
        {
            this.exitParameterListContext(old);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:243:1.
    private Node transformFunctionBody(Node node)
    {
        this.EmitContext().StartVariableEnvironment();
        var body = this.Visitor().VisitNode(node.Body());
        var extras = this.EmitContext().EndVariableEnvironment();
        this.EmitContext().StartVariableEnvironment();
        var newStatements = this.collectObjectRestAssignments(node);
        extras = this.EmitContext().EndAndMergeVariableEnvironment(extras);
        if (((GoRuntime.Len(newStatements) == 0) && (GoRuntime.Len(extras) == 0)))
        {
            return body;
        }

        if ((body == null))
        {
            body = this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.Make(0)), true);
        }

        Slice<Node> prefix = default;
        Slice<Node> suffix = default;
        if (Ast.IsBlock(body))
        {
            var custom = false;
            {
                foreach (var range2 in GoRuntime.Range(body.Statements()))
                {
                    var i = range2.Key;
                    var statement = range2.Value;
                    if ((!custom && Ast.IsPrologueDirective(statement)))
                    {
                        prefix = prefix.Append(statement);
                    }
                    else
                    {
                        if (((this.EmitContext().EmitFlags(statement) & EmitFlags.CustomPrologue) != 0))
                        {
                            custom = true;
                            prefix = prefix.Append(statement);
                        }
                        else
                        {
                            suffix = body.Statements().Slice2(i, body.Statements().Length);
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            var ret = this.Factory().NewReturnStatement(body);
            ret.Loc = body.Loc;
            var list = this.Factory().NewNodeList(Slice<Node>.Make(0));
            list.Loc = body.Loc;
            body = this.Factory().NewBlock(list, true);
            suffix = suffix.Append(ret);
        }

        var newStatementList = this.Factory().NewNodeList(prefix.Append(extras).Append(newStatements).Append(suffix));
        newStatementList.Loc = body.StatementList().Loc;
        return this.Factory().UpdateBlock(body.AsBlock(), newStatementList, body.AsBlock().MultiLine);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:288:1.
    private Slice<Node> collectObjectRestAssignments(Node node)
    {
        var containsPrecedingObjectRestOrSpread = false;
        Slice<Node> results = default;
        {
            foreach (var parameter in GoRuntime.RangeValues(node.Parameters()))
            {
                if (containsPrecedingObjectRestOrSpread)
                {
                    if (Ast.IsBindingPattern(parameter.Name()))
                    {
                        if ((GoRuntime.Len(parameter.Name().Elements()) > 0))
                        {
                            var declarations = Transformers.FlattenDestructuringBinding(this, parameter, this.Factory().NewGeneratedNameForNode(parameter), FlattenLevel.All, false, false);
                            if ((declarations != null))
                            {
                                var declarationList = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.Make(0)), NodeFlags.None);
                                var decls = Slice<Node>.FromArray([declarations]);
                                if ((declarations.Kind == Kind.SyntaxList))
                                {
                                    decls = declarations.AsSyntaxList().Children;
                                }

                                declarationList.AsVariableDeclarationList().Declarations.Nodes = declarationList.AsVariableDeclarationList().Declarations.Nodes.Append(decls);
                                var statement = this.Factory().NewVariableStatement(null, declarationList);
                                this.EmitContext().AddEmitFlags(statement, EmitFlags.CustomPrologue);
                                results = results.Append(statement);
                            }
                        }
                        else
                        {
                            if ((parameter.Initializer() != null))
                            {
                                var name = this.Factory().NewGeneratedNameForNode(parameter);
                                var initializer = this.Visitor().VisitNode(parameter.Initializer());
                                var assignment = this.Factory().NewAssignmentExpression(name, initializer);
                                var statement2 = this.Factory().NewExpressionStatement(assignment);
                                this.EmitContext().AddEmitFlags(statement2, EmitFlags.CustomPrologue);
                                results = results.Append(statement2);
                            }
                        }
                    }
                    else
                    {
                        if ((parameter.Initializer() != null))
                        {
                            var name2 = parameter.Name().Clone(this.Factory());
                            name2.Loc = parameter.Name().Loc;
                            this.EmitContext().AddEmitFlags(name2, EmitFlags.NoSourceMap);
                            var initializer2 = this.Visitor().VisitNode(parameter.Initializer());
                            this.EmitContext().AddEmitFlags(initializer2, (EmitFlags.NoSourceMap | EmitFlags.NoComments));
                            var assignment2 = this.Factory().NewAssignmentExpression(name2, initializer2);
                            assignment2.Loc = parameter.Loc;
                            this.EmitContext().AddEmitFlags(assignment2, EmitFlags.NoComments);
                            var block = this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExpressionStatement(assignment2)])), false);
                            block.Loc = parameter.Loc;
                            this.EmitContext().AddEmitFlags(block, (((EmitFlags.SingleLine | EmitFlags.NoTrailingSourceMap) | EmitFlags.NoTokenSourceMaps) | EmitFlags.NoComments));
                            var typeCheck = this.Factory().NewTypeCheck(name2.Clone(this.Factory()), "undefined");
                            var statement3 = this.Factory().NewIfStatement(typeCheck, block, null);
                            statement3.Loc = parameter.Loc;
                            this.EmitContext().AddEmitFlags(statement3, ((((EmitFlags.NoTokenSourceMaps | EmitFlags.NoTrailingSourceMap) | EmitFlags.CustomPrologue) | EmitFlags.NoComments) | EmitFlags.StartOnNewLine));
                            results = results.Append(statement3);
                        }
                    }
                }
                else
                {
                    if ((((uint)parameter.SubtreeFacts() & SubtreeFactValues.ContainsObjectRestOrSpread) != 0))
                    {
                        containsPrecedingObjectRestOrSpread = true;
                        var declarations2 = Transformers.FlattenDestructuringBinding(this, parameter, this.Factory().NewGeneratedNameForNode(parameter), FlattenLevel.ObjectRest, false, true);
                        if ((declarations2 != null))
                        {
                            var declarationList2 = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.Make(0)), NodeFlags.None);
                            var decls2 = Slice<Node>.FromArray([declarations2]);
                            if ((declarations2.Kind == Kind.SyntaxList))
                            {
                                decls2 = declarations2.AsSyntaxList().Children;
                            }

                            declarationList2.AsVariableDeclarationList().Declarations.Nodes = declarationList2.AsVariableDeclarationList().Declarations.Nodes.Append(decls2);
                            var statement4 = this.Factory().NewVariableStatement(null, declarationList2);
                            this.EmitContext().AddEmitFlags(statement4, EmitFlags.CustomPrologue);
                            results = results.Append(statement4);
                        }
                    }
                }
            }
        }

        return results;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:378:1.
    private Node visitCatchClause(CatchClause node)
    {
        if ((((node.VariableDeclaration != null) && Ast.IsBindingPattern(node.VariableDeclaration.Name())) && (((uint)node.VariableDeclaration.Name().SubtreeFacts() & SubtreeFactValues.ContainsObjectRestOrSpread) != 0)))
        {
            var name = this.Factory().NewGeneratedNameForNode(node.VariableDeclaration.Name());
            var updatedDecl = this.Factory().UpdateVariableDeclaration(node.VariableDeclaration.AsVariableDeclaration(), node.VariableDeclaration.Name(), null, null, name);
            var visitedBindings = Transformers.FlattenDestructuringBinding(this, updatedDecl, null, FlattenLevel.ObjectRest, false, false);
            var block = this.Visitor().VisitNode(node.Block);
            if ((visitedBindings != null))
            {
                Slice<Node> decls = default;
                if ((visitedBindings.Kind == Kind.SyntaxList))
                {
                    decls = visitedBindings.AsSyntaxList().Children;
                }
                else
                {
                    decls = Slice<Node>.FromArray([visitedBindings]);
                }

                var newStatement = this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(decls), NodeFlags.None));
                var statements = Slice<Node>.FromArray([newStatement]);
                statements = statements.Append(block.Statements());
                var statementList = this.Factory().NewNodeList(statements);
                statementList.Loc = block.StatementList().Loc;
                block = this.Factory().UpdateBlock(block.AsBlock(), statementList, block.AsBlock().MultiLine);
            }

            return this.Factory().UpdateCatchClause(node, this.Factory().UpdateVariableDeclaration(node.VariableDeclaration.AsVariableDeclaration(), name, null, null, null), block);
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:412:1.
    private Node visitVariableStatement(VariableStatement node)
    {
        if (Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export))
        {
            var oldInExportedVariableStatement = this.inExportedVariableStatement;
            this.inExportedVariableStatement = true;
            var result = this.Visitor().VisitEachChild(node.AsNode());
            this.inExportedVariableStatement = oldInExportedVariableStatement;
            return result;
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:423:1.
    private Node visitVariableDeclaration(VariableDeclaration node)
    {
        if (this.inExportedVariableStatement)
        {
            this.inExportedVariableStatement = false;
            var result = this.visitVariableDeclarationWorker(node, true);
            this.inExportedVariableStatement = true;
            return result;
        }

        return this.visitVariableDeclarationWorker(node, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:433:1.
    private Node visitVariableDeclarationWorker(VariableDeclaration node, bool exported)
    {
        if ((Ast.IsBindingPattern(node.Name()) && (((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsObjectRestOrSpread) != 0)))
        {
            return Transformers.FlattenDestructuringBinding(this, node.AsNode(), null, FlattenLevel.ObjectRest, exported, false);
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:445:1.
    private Node visitForOftatement(ForInOrOfStatement node)
    {
        if (((((uint)node.Initializer.SubtreeFacts() & SubtreeFactValues.ContainsObjectRestOrSpread) != 0) || ((Ast.IsAssignmentPattern(node.Initializer) && Ast.ContainsObjectRestOrSpread(node.Initializer)))))
        {
            var initializerWithoutParens = Ast.SkipParentheses(node.Initializer);
            if ((Ast.IsVariableDeclarationList(initializerWithoutParens) || Ast.IsAssignmentPattern(initializerWithoutParens)))
            {
                TextRange bodyLocation = default;
                TextRange statementsLocation = default;
                var temp = this.Factory().NewTempVariable();
                var res = this.Visitor().VisitNode(this.Factory().CreateForOfBindingStatement(initializerWithoutParens, temp));
                var statements = Slice<Node>.Make(0, 1);
                if ((res != null))
                {
                    statements = statements.Append(res);
                }

                if (Ast.IsBlock(node.Statement))
                {
                    {
                        foreach (var statement in GoRuntime.RangeValues(node.Statement.Statements()))
                        {
                            var visited = this.Visitor().VisitEachChild(statement);
                            if ((visited != null))
                            {
                                statements = statements.Append(visited);
                            }
                        }
                    }

                    bodyLocation = node.Statement.Loc;
                    statementsLocation = node.Statement.StatementList().Loc;
                }
                else
                {
                    if ((node.Statement != null))
                    {
                        statements = statements.Append(this.Visitor().VisitEachChild(node.Statement));
                        bodyLocation = node.Statement.Loc;
                        statementsLocation = node.Statement.Loc;
                    }
                }

                var list = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(temp, null, null, null)])), NodeFlags.Let);
                list.Loc = node.Initializer.Loc;
                var expr = this.Visitor().VisitEachChild(node.Expression);
                var statementsList = this.Factory().NewNodeList(statements);
                statementsList.Loc = statementsLocation;
                var block = this.Factory().NewBlock(statementsList, true);
                block.Loc = bodyLocation;
                return this.Factory().UpdateForInOrOfStatement(node, node.AwaitModifier, list, expr, block);
            }
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:498:1.
    private Node visitBinaryExpression(BinaryExpression node, bool expressionResultIsUnused)
    {
        if ((Ast.IsDestructuringAssignment(node.AsNode()) && Ast.ContainsObjectRestOrSpread(node.Left)))
        {
            return Transformers.FlattenDestructuringAssignment(this, node.AsNode(), !expressionResultIsUnused, FlattenLevel.ObjectRest, null);
        }

        if ((node.OperatorToken.Kind == Kind.CommaToken))
        {
            this.expressionResultIsUnused = true;
            var left = this.Visitor().VisitNode(node.Left);
            this.expressionResultIsUnused = expressionResultIsUnused;
            var right = this.Visitor().VisitNode(node.Right);
            return this.Factory().UpdateBinaryExpression(node, null, left, null, node.OperatorToken, right);
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:516:1.
    private Node visitObjectLiteralExpression(ObjectLiteralExpression node)
    {
        if (((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsObjectRestOrSpread)) == 0))
        {
            return this.Visitor().VisitEachChild(node.AsNode());
        }

        var objects = this.chunkObjectLiteralElements(node.Properties);
        if (((GoRuntime.Len(objects) > 0) && (objects[0].Kind != Kind.ObjectLiteralExpression)))
        {
            objects = Slice<Node>.FromArray([this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(default(Slice<Node>)), false)]).Append(objects);
        }

        var expression = objects[0];
        if ((GoRuntime.Len(objects) > 1))
        {
            {
                foreach (var range5 in GoRuntime.Range(objects))
                {
                    var i = range5.Key;
                    var obj = range5.Value;
                    if ((i == 0))
                    {
                        continue;
                    }

                    expression = this.Factory().NewAssignHelper(Slice<Node>.FromArray([expression, obj]), this.compilerOptions.GetEmitScriptTarget());
                }
            }

            return expression;
        }

        return this.Factory().NewAssignHelper(objects, this.compilerOptions.GetEmitScriptTarget());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:559:1.
    private Slice<Node> chunkObjectLiteralElements(NodeList list)
    {
        if (((list == null) || (GoRuntime.Len(list.Nodes) == 0)))
        {
            return default;
        }

        var elements = list.Nodes;
        Slice<Node> chunkObject = default;
        var objects = Slice<Node>.Make(0, 1);
        {
            foreach (var e in GoRuntime.RangeValues(elements))
            {
                if ((e.Kind == Kind.SpreadAssignment))
                {
                    if ((GoRuntime.Len(chunkObject) > 0))
                    {
                        objects = objects.Append(this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(chunkObject), false));
                        chunkObject = default;
                    }

                    var target = e.Expression();
                    objects = objects.Append(this.Visitor().VisitNode(target));
                }
                else
                {
                    Node elem = default;
                    if ((e.Kind == Kind.PropertyAssignment))
                    {
                        elem = this.Factory().NewPropertyAssignment(null, e.Name(), null, null, this.Visitor().VisitNode(e.Initializer()));
                    }
                    else
                    {
                        elem = this.Visitor().VisitNode(e);
                    }

                    chunkObject = chunkObject.Append(elem);
                }
            }
        }

        if ((GoRuntime.Len(chunkObject) > 0))
        {
            objects = objects.Append(this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(chunkObject), false));
        }

        return objects;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go:590:1.
    internal static Transformer NewObjectRestSpreadTransformer(TransformOptions opts)
    {
        var tx = new ObjectRestSpreadTransformer
        {
            compilerOptions = opts.CompilerOptions
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }
}
