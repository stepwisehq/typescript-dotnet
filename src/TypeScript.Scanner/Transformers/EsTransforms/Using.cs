// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/using.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Collections;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class UsingDeclarationTransformer : Transformer
{
    private enum UsingKind : uint
    {
        None,
        Sync,
        Async
    }

    private Dictionary<ByteStringKey, Node> exportBindings;
    private Slice<ByteStringKey> exportBindingNames;
    private Slice<Node> exportVars;
    private Node defaultExportBinding, exportEqualsBinding;
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:21:1.
    internal static Transformer NewUsingDeclarationTransformer(TransformOptions opts)
    {
        var tx = new UsingDeclarationTransformer
        {
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:34:1.
    private Node visit(Node node)
    {
        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsUsing) == 0))
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

            case Kind.Block:
            {
                node = this.visitBlock(node.AsBlock());
                break;
            }

            case Kind.ForStatement:
            {
                node = this.visitForStatement(node.AsForStatement());
                break;
            }

            case Kind.ForOfStatement:
            {
                node = this.visitForOfStatement(node.AsForInOrOfStatement());
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

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:54:1.
    private Node visitSourceFile(SourceFile node)
    {
        if (node.IsDeclarationFile)
        {
            return node.AsNode();
        }

        Node visited = default;
        var usingKind = getUsingKindOfStatements(node.Statements.Nodes);
        if ((usingKind != UsingKind.None))
        {
            this.EmitContext().StartVariableEnvironment();
            this.exportBindings = new Dictionary<ByteStringKey, Node>(ByteStringKey.Comparer);
            this.exportVars = default;
            var tuple1 = this.Factory().SplitStandardPrologue(node.Statements.Nodes);
            var prologue = tuple1.Item1;
            var rest = tuple1.Item2;
            Slice<Node> topLevelStatements = default;
            topLevelStatements = topLevelStatements.Append(this.Visitor().VisitSlice(prologue).Item1);
            var pos = 0;
            while ((pos < GoRuntime.Len(rest)))
            {
                var statement = rest[pos];
                if ((getUsingKind(statement) != UsingKind.None))
                {
                    if ((pos > 0))
                    {
                        topLevelStatements = topLevelStatements.Append(this.Visitor().VisitSlice(rest.Slice2(0, pos)).Item1);
                    }

                    break;
                }

                pos++;
            }

            if ((pos >= GoRuntime.Len(rest)))
            {
                throw new InvalidOperationException("Should have encountered at least one 'using' statement.");
            }

            var envBinding = this.createEnvBinding();
            var bodyStatements = this.transformUsingDeclarations(rest.Slice2(pos, rest.Length), envBinding, ref topLevelStatements, true);
            if ((GoRuntime.Len(this.exportBindings) > 0))
            {
                var exportSpecifiers = Slice<Node>.Make(0, this.exportBindingNames.Length);
                {
                    foreach (var name in GoRuntime.RangeValues(this.exportBindingNames))
                    {
                        var specifier = this.exportBindings[name];
                        Debug.Assert((specifier != null), "Missing export binding for hoisted export name");
                        exportSpecifiers = exportSpecifiers.Append(specifier);
                    }
                }

                topLevelStatements = topLevelStatements.Append(this.Factory().NewExportDeclaration(null, false, this.Factory().NewNamedExports(this.Factory().NewNodeList(exportSpecifiers)), null, null));
            }

            topLevelStatements = topLevelStatements.Append(this.EmitContext().EndVariableEnvironment());
            if ((GoRuntime.Len(this.exportVars) > 0))
            {
                topLevelStatements = topLevelStatements.Append(this.Factory().NewVariableStatement(this.Factory().NewModifierList(Slice<Node>.FromArray([this.Factory().NewModifier(Kind.ExportKeyword)])), this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(this.exportVars), NodeFlags.Let)));
            }

            topLevelStatements = topLevelStatements.Append(this.createDownlevelUsingStatements(bodyStatements, envBinding, (usingKind == UsingKind.Async)));
            if ((this.exportEqualsBinding != null))
            {
                topLevelStatements = topLevelStatements.Append(this.Factory().NewExportAssignment(null, true, null, this.exportEqualsBinding));
            }

            visited = this.Factory().UpdateSourceFile(node, this.Factory().NewNodeList(topLevelStatements), node.EndOfFileToken);
        }
        else
        {
            visited = this.Visitor().VisitEachChild(node.AsNode());
        }

        this.EmitContext().AddEmitHelper(visited, this.EmitContext().ReadEmitHelpers());
        this.exportVars = default;
        this.exportBindings = null;
        this.exportBindingNames = default;
        this.defaultExportBinding = null;
        this.exportEqualsBinding = null;
        return visited;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:192:1.
    private Node visitBlock(Block node)
    {
        var usingKind = getUsingKindOfStatements(node.Statements.Nodes);
        if ((usingKind != UsingKind.None))
        {
            var tuple3 = this.Factory().SplitStandardPrologue(node.Statements.Nodes);
            var prologue = tuple3.Item1;
            var rest = tuple3.Item2;
            var envBinding = this.createEnvBinding();
            Slice<Node> noTopLevelStatements = default;
            var statements = Slice<Node>.Make(0, prologue.Length + 2);
            statements = statements.Append(this.Visitor().VisitSlice(prologue).Item1);
            statements = statements.Append(this.createDownlevelUsingStatements(this.transformUsingDeclarations(rest, envBinding, ref noTopLevelStatements, false), envBinding, (usingKind == UsingKind.Async)));
            var statementList = this.Factory().NewNodeList(statements);
            statementList.Loc = node.Statements.Loc;
            return this.Factory().UpdateBlock(node, statementList, node.MultiLine);
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:211:1.
    private Node visitForStatement(ForStatement node)
    {
        if (((node.Initializer != null) && isUsingVariableDeclarationList(node.Initializer)))
        {
            return this.Visitor().VisitNode(this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableStatement(null, node.Initializer), this.Factory().UpdateForStatement(node, null, node.Condition, node.Incrementor, node.Statement)])), false));
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:241:1.
    private Node visitForOfStatement(ForInOrOfStatement node)
    {
        if (isUsingVariableDeclarationList(node.Initializer))
        {
            var forInitializer = node.Initializer.AsVariableDeclarationList();
            var forDecl = Core.FirstOrNil(forInitializer.Declarations.Nodes);
            if ((forDecl == null))
            {
                forDecl = this.Factory().NewVariableDeclaration(this.Factory().NewTempVariable(), null, null, null);
            }

            var isAwaitUsing = (getUsingKindOfVariableDeclarationList(forInitializer) == UsingKind.Async);
            var temp = this.Factory().NewGeneratedNameForNode(forDecl.Name());
            var usingVar = this.Factory().UpdateVariableDeclaration(forDecl.AsVariableDeclaration(), forDecl.Name(), null, null, temp);
            var usingVarList = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([usingVar])), Core.IfElse(isAwaitUsing, NodeFlags.AwaitUsing, NodeFlags.Using));
            var usingVarStatement = this.Factory().NewVariableStatement(null, usingVarList);
            Node statement = default;
            if (Ast.IsBlock(node.Statement))
            {
                var statements = Slice<Node>.Make(0, node.Statement.Statements().Length + 1);
                statements = statements.Append(usingVarStatement);
                statements = statements.Append(node.Statement.Statements());
                statement = this.Factory().UpdateBlock(node.Statement.AsBlock(), this.Factory().NewNodeList(statements), node.Statement.AsBlock().MultiLine);
            }
            else
            {
                statement = this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([usingVarStatement, node.Statement])), true);
            }

            return this.Visitor().VisitNode(this.Factory().UpdateForInOrOfStatement(node, node.AwaitModifier, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(temp, null, null, null)])), NodeFlags.Const), node.Expression, statement));
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:306:1.
    private Slice<Node> transformUsingDeclarations(Slice<Node> statementsIn, Node envBinding, ref Slice<Node> topLevelStatements, bool hasTopLevelStatements)
    {
        Slice<Node> statements = default;
        {
            foreach (var statement in GoRuntime.RangeValues(statementsIn))
            {
                var usingKind = getUsingKind(statement);
                if ((usingKind != UsingKind.None))
                {
                    var varStatement = statement.AsVariableStatement();
                    var declarationList = varStatement.DeclarationList;
                    Slice<Node> declarations = default;
                    {
                        foreach (var range5 in GoRuntime.Range(declarationList.AsVariableDeclarationList().Declarations.Nodes))
                        {
                            var _2 = range5.Key;
                            var declaration = range5.Value;
                            if (!Ast.IsIdentifier(declaration.Name()))
                            {
                                declarations = default;
                                break;
                            }

                            if (EsTransforms.IsNamedEvaluation(this.EmitContext(), declaration))
                            {
                                declaration = EsTransforms.TransformNamedEvaluation(this.EmitContext(), declaration, false, ByteStringValue.FromString(""));
                            }

                            var initializer = this.Visitor().VisitNode(declaration.Initializer());
                            if ((initializer == null))
                            {
                                initializer = this.Factory().NewVoidZeroExpression();
                            }

                            declarations = declarations.Append(this.Factory().UpdateVariableDeclaration(declaration.AsVariableDeclaration(), declaration.Name(), null, null, this.Factory().NewAddDisposableResourceHelper(envBinding, initializer, (usingKind == UsingKind.Async))));
                        }
                    }

                    if ((GoRuntime.Len(declarations) > 0))
                    {
                        var varList = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(declarations), NodeFlags.Const);
                        this.EmitContext().SetOriginal(varList, declarationList);
                        varList.Loc = declarationList.Loc;
                        hoistOrAppendNode(this.Factory().UpdateVariableStatement(varStatement, null, varList), ref statements, ref topLevelStatements, hasTopLevelStatements);
                        continue;
                    }
                }

                {
                    var result = this.visit(statement);
                    if ((result != null))
                    {
                        if ((result.Kind == Kind.SyntaxList))
                        {
                            {
                                foreach (var range6 in GoRuntime.Range(result.AsSyntaxList().Children))
                                {
                                    var _3 = range6.Key;
                                    var node3 = range6.Value;
                                    hoistOrAppendNode(node3, ref statements, ref topLevelStatements, hasTopLevelStatements);
                                }
                            }
                        }
                        else
                        {
                            hoistOrAppendNode(result, ref statements, ref topLevelStatements, hasTopLevelStatements);
                        }
                    }
                }
            }
        }

        return statements;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:397:1.
    private void hoistImportOrExportOrHoistedDeclaration(Node node, ref Slice<Node> topLevelStatements)
    {
        topLevelStatements = topLevelStatements.Append(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:402:1.
    private Node hoistExportAssignment(ExportAssignment node)
    {
        if (node.IsExportEquals)
        {
            return this.hoistExportEquals(node);
        }
        else
        {
            return this.hoistExportDefault(node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:410:1.
    private Node hoistExportDefault(ExportAssignment node)
    {
        if ((this.defaultExportBinding != null))
        {
            return node.AsNode();
        }

        this.defaultExportBinding = this.Factory().NewUniqueNameEx("_default", new AutoGenerateOptions { Flags = ((GeneratedIdentifierFlags.ReservedInNestedScopes | GeneratedIdentifierFlags.FileLevel) | GeneratedIdentifierFlags.Optimistic) });
        this.hoistBindingIdentifier(this.defaultExportBinding, true, this.Factory().NewIdentifier("default"), node.AsNode());
        var expression = node.Expression;
        var innerExpression = Ast.SkipOuterExpressions(expression, OuterExpressionKinds.All);
        if (EsTransforms.IsNamedEvaluation(this.EmitContext(), innerExpression))
        {
            innerExpression = EsTransforms.TransformNamedEvaluation(this.EmitContext(), innerExpression, false, ByteStringValue.FromString("default"));
            expression = this.Factory().RestoreOuterExpressions(expression, innerExpression, OuterExpressionKinds.All);
        }

        var assignment = this.Factory().NewAssignmentExpression(this.defaultExportBinding, expression);
        return this.Factory().NewExpressionStatement(assignment);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:445:1.
    private Node hoistExportEquals(ExportAssignment node)
    {
        if ((this.exportEqualsBinding != null))
        {
            return node.AsNode();
        }

        this.exportEqualsBinding = this.Factory().NewUniqueNameEx("_default", new AutoGenerateOptions { Flags = ((GeneratedIdentifierFlags.ReservedInNestedScopes | GeneratedIdentifierFlags.FileLevel) | GeneratedIdentifierFlags.Optimistic) });
        this.EmitContext().AddVariableDeclaration(this.exportEqualsBinding);
        var assignment = this.Factory().NewAssignmentExpression(this.exportEqualsBinding, node.Expression);
        return this.Factory().NewExpressionStatement(assignment);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:477:1.
    private Node hoistClassDeclaration(ClassDeclaration node)
    {
        if (((node.Name() == null) && (this.defaultExportBinding != null)))
        {
            return node.AsNode();
        }

        var isExported = Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export);
        var isDefault = Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Default);
        var expression = EsTransforms.ConvertClassDeclarationToClassExpression(this.EmitContext(), node);
        if ((node.Name() != null))
        {
            this.hoistBindingIdentifier(this.Factory().GetLocalName(node.AsNode()), (isExported && !isDefault), null, node.AsNode());
            expression = this.Factory().NewAssignmentExpression(this.Factory().GetDeclarationName(node.AsNode()), expression);
            this.EmitContext().SetOriginal(expression, node.AsNode());
            this.EmitContext().SetSourceMapRange(expression, node.Loc);
            this.EmitContext().SetCommentRange(expression, node.Loc);
            if (EsTransforms.IsNamedEvaluation(this.EmitContext(), expression))
            {
                expression = EsTransforms.TransformNamedEvaluation(this.EmitContext(), expression, false, ByteStringValue.FromString(""));
            }
        }

        if ((isDefault && (this.defaultExportBinding == null)))
        {
            this.defaultExportBinding = this.Factory().NewUniqueNameEx("_default", new AutoGenerateOptions { Flags = ((GeneratedIdentifierFlags.ReservedInNestedScopes | GeneratedIdentifierFlags.FileLevel) | GeneratedIdentifierFlags.Optimistic) });
            this.hoistBindingIdentifier(this.defaultExportBinding, true, this.Factory().NewIdentifier("default"), node.AsNode());
            expression = this.Factory().NewAssignmentExpression(this.defaultExportBinding, expression);
            this.EmitContext().SetOriginal(expression, node.AsNode());
            if (EsTransforms.IsNamedEvaluation(this.EmitContext(), expression))
            {
                expression = EsTransforms.TransformNamedEvaluation(this.EmitContext(), expression, false, ByteStringValue.FromString("default"));
            }
        }

        return this.Factory().NewExpressionStatement(expression);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:562:1.
    private Node hoistVariableStatement(VariableStatement node)
    {
        Slice<Node> expressions = default;
        var isExported = Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export);
        {
            foreach (var variable in GoRuntime.RangeValues(node.DeclarationList.AsVariableDeclarationList().Declarations.Nodes))
            {
                this.hoistBindingElement(variable, isExported, variable);
                if ((variable.Initializer() != null))
                {
                    expressions = expressions.Append(this.hoistInitializedVariable(variable.AsVariableDeclaration()));
                }
            }
        }

        if ((GoRuntime.Len(expressions) > 0))
        {
            var statement = this.Factory().NewExpressionStatement(this.Factory().InlineExpressions(expressions));
            this.EmitContext().SetOriginal(statement, node.AsNode());
            this.EmitContext().SetCommentRange(statement, node.Loc);
            this.EmitContext().SetSourceMapRange(statement, node.Loc);
            return statement;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:582:1.
    private Node hoistInitializedVariable(VariableDeclaration node)
    {
        if ((node.Initializer == null))
        {
            throw new InvalidOperationException("Expected initializer");
        }

        Node target = default;
        if (Ast.IsIdentifier(node.Name()))
        {
            target = node.Name().Clone(this.Factory());
            this.EmitContext().SetEmitFlags(target, (this.EmitContext().EmitFlags(target) & ~((EmitFlags.LocalName | EmitFlags.ExportName))));
        }
        else
        {
            target = Transformers.ConvertBindingPatternToAssignmentPattern(this.EmitContext(), node.Name().AsBindingPattern());
        }

        var assignment = this.Factory().NewAssignmentExpression(target, node.Initializer);
        this.EmitContext().SetOriginal(assignment, node.AsNode());
        this.EmitContext().SetCommentRange(assignment, node.Loc);
        this.EmitContext().SetSourceMapRange(assignment, node.Loc);
        return assignment;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:602:1.
    private void hoistBindingElement(Node node, bool isExportedDeclaration, Node original)
    {
        if (Ast.IsBindingPattern(node.Name()))
        {
            {
                foreach (var element in GoRuntime.RangeValues(node.Name().Elements()))
                {
                    if ((element.Name() != null))
                    {
                        this.hoistBindingElement(element, isExportedDeclaration, original);
                    }
                }
            }
        }
        else
        {
            this.hoistBindingIdentifier(node.Name(), isExportedDeclaration, null, original);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:615:1.
    private void hoistBindingIdentifier(Node node, bool isExport, Node exportAlias, Node original)
    {
        var name = node;
        if (!Transformers.IsGeneratedIdentifier(this.EmitContext(), node))
        {
            name = name.Clone(this.Factory());
        }

        if (isExport)
        {
            if (((exportAlias == null) && !Transformers.IsLocalName(this.EmitContext(), name)))
            {
                var varDecl = this.Factory().NewVariableDeclaration(name, null, null, null);
                if ((original != null))
                {
                    this.EmitContext().SetOriginal(varDecl, original);
                }

                this.exportVars = this.exportVars.Append(varDecl);
                return;
            }

            Node localName = default;
            Node exportName = default;
            if ((exportAlias != null))
            {
                localName = name;
                exportName = exportAlias;
            }
            else
            {
                exportName = name;
            }

            var specifier = this.Factory().NewExportSpecifier(false, localName, exportName);
            if ((original != null))
            {
                this.EmitContext().SetOriginal(specifier, original);
            }

            if ((this.exportBindings == null))
            {
                this.exportBindings = new Dictionary<ByteStringKey, Node>(ByteStringKey.Comparer);
            }

            {
                var ok = name.TextValue().TryGetValue(this.exportBindings, out var _);
                if (!ok)
                {
                    this.exportBindingNames = this.exportBindingNames.Append(ByteStringKey.FromValue(name.TextValue()));
                }
            }

            this.exportBindings[ByteStringKey.FromValue(name.TextValue())] = specifier;
        }

        this.EmitContext().AddVariableDeclaration(name);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:654:1.
    private Node createEnvBinding()
    {
        return this.Factory().NewUniqueName("env");
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:658:1.
    private Slice<Node> createDownlevelUsingStatements(Slice<Node> bodyStatements, Node envBinding, bool async)
    {
        var statements = Slice<Node>.Make(0, 2);
        var envObject = this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewPropertyAssignment(null, this.Factory().NewIdentifier("stack"), null, null, this.Factory().NewArrayLiteralExpression(null, false)), this.Factory().NewPropertyAssignment(null, this.Factory().NewIdentifier("error"), null, null, this.Factory().NewVoidZeroExpression()), this.Factory().NewPropertyAssignment(null, this.Factory().NewIdentifier("hasError"), null, null, this.Factory().NewFalseExpression())])), false);
        var envVar = this.Factory().NewVariableDeclaration(envBinding, null, null, envObject);
        var envVarList = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([envVar])), NodeFlags.Const);
        var envVarStatement = this.Factory().NewVariableStatement(null, envVarList);
        statements = statements.Append(envVarStatement);
        var tryBlock = this.Factory().NewBlock(this.Factory().NewNodeList(bodyStatements), true);
        var bodyCatchBinding = this.Factory().NewUniqueName("e");
        var catchClause = this.Factory().NewCatchClause(this.Factory().NewVariableDeclaration(bodyCatchBinding, null, null, null), this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExpressionStatement(this.Factory().NewAssignmentExpression(this.Factory().NewPropertyAccessExpression(envBinding, null, this.Factory().NewIdentifier("error"), NodeFlags.None), bodyCatchBinding)), this.Factory().NewExpressionStatement(this.Factory().NewAssignmentExpression(this.Factory().NewPropertyAccessExpression(envBinding, null, this.Factory().NewIdentifier("hasError"), NodeFlags.None), this.Factory().NewTrueExpression()))])), true));
        Node finallyBlock = default;
        if (async)
        {
            var result = this.Factory().NewUniqueName("result");
            finallyBlock = this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(result, null, null, this.Factory().NewDisposeResourcesHelper(envBinding))])), NodeFlags.Const)), this.Factory().NewIfStatement(result, this.Factory().NewExpressionStatement(this.Factory().NewAwaitExpression(result)), null)])), true);
        }
        else
        {
            finallyBlock = this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExpressionStatement(this.Factory().NewDisposeResourcesHelper(envBinding))])), true);
        }

        var tryStatement = this.Factory().NewTryStatement(tryBlock, catchClause, finallyBlock);
        statements = statements.Append(tryStatement);
        return statements;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:761:1.
    private static bool isUsingVariableDeclarationList(Node node)
    {
        return (Ast.IsVariableDeclarationList(node) && (getUsingKindOfVariableDeclarationList(node.AsVariableDeclarationList()) != UsingKind.None));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:765:1.
    private static UsingKind getUsingKindOfVariableDeclarationList(VariableDeclarationList node)
    {
        switch ((node.Flags & NodeFlags.BlockScoped))
        {
            case NodeFlags.AwaitUsing:
            {
                return UsingKind.Async;
            }

            case NodeFlags.Using:
            {
                return UsingKind.Sync;
            }

            default:
            {
                return UsingKind.None;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:776:1.
    private static UsingKind getUsingKindOfVariableStatement(VariableStatement node)
    {
        return getUsingKindOfVariableDeclarationList(node.DeclarationList.AsVariableDeclarationList());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:780:1.
    private static UsingKind getUsingKind(Node statement)
    {
        if (Ast.IsVariableStatement(statement))
        {
            return getUsingKindOfVariableStatement(statement.AsVariableStatement());
        }

        return UsingKind.None;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/using.go:787:1.
    private static UsingKind getUsingKindOfStatements(Slice<Node> statements)
    {
        var result = UsingKind.None;
        {
            foreach (var statement in GoRuntime.RangeValues(statements))
            {
                var usingKind = getUsingKind(statement);
                if ((usingKind == UsingKind.Async))
                {
                    return UsingKind.Async;
                }

                if ((usingKind > result))
                {
                    result = usingKind;
                }
            }
        }

        return result;
    }

    // using.go:309/333: original local closure bodies; explicit ref headers
    // avoid a managed display class and retain nullable-pointer distinction.
    private Node hoist(Node node, ref Slice<Node> topLevelStatements, bool hasTopLevelStatements)
    {
        if ((!hasTopLevelStatements))
        {
            return node;
        }

        switch (node.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.ImportEqualsDeclaration:
            case Kind.ExportDeclaration:
            case Kind.FunctionDeclaration:
            {
                this.hoistImportOrExportOrHoistedDeclaration(node, ref topLevelStatements);
                return null;
            }

            case Kind.ExportAssignment:
            {
                return this.hoistExportAssignment(node.AsExportAssignment());
            }

            case Kind.ClassDeclaration:
            {
                return this.hoistClassDeclaration(node.AsClassDeclaration());
            }

            case Kind.VariableStatement:
            {
                return this.hoistVariableStatement(node.AsVariableStatement());
            }
        }

        return node;
    }

    private void hoistOrAppendNode(Node node, ref Slice<Node> statements, ref Slice<Node> topLevelStatements, bool hasTopLevelStatements)
    {
        node = hoist(node, ref topLevelStatements, hasTopLevelStatements);
        if (node != null)
            statements = statements.Append(node);
    }
}
