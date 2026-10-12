// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete classfields.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using EmitNodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;
namespace TypeScript.Transformers.EsTransforms;
internal sealed class ClassFieldsTransformer : Transformer
{
    [Flags]
    internal enum classFacts { None=0, ClassWasDecorated=1, NeedsClassConstructorReference=2, NeedsClassSuperReference=4, NeedsSubstitutionForThisInClassStaticField=8, WillHoistInitializersToConstructor=16 }
    // Mutable identity-bearing records are shared by accessor updates and lexical lookup.
    private sealed class privateIdentifierInfo
    {
        internal PrivateIdentifierKind kind;
        internal Node brandCheckIdentifier, variableName, methodName, getterName, setterName;
        internal bool isStatic, isValid;
    }
    private struct privateEnvironmentData { internal Node className, weakSetName; }
    private sealed class privateEnvironment
    {
        internal privateEnvironmentData data;
        internal Dictionary<ByteStringKey, privateIdentifierInfo> members;
        internal Dictionary<Node, privateIdentifierInfo> generatedIdentifiers;
    }
    private sealed class classLexicalEnvironment
    {
        internal classFacts facts;
        internal Node classConstructor, classThis, superClassReference;
    }
    private sealed class classLexicalEnv
    {
        internal classLexicalEnv previous;
        internal classLexicalEnvironment data;
        internal privateEnvironment privateEnv;
    }
    private CompilerOptions compilerOptions;
    private IReferenceResolver resolver;
    private bool shouldTransformInitializersUsingSet, shouldTransformInitializersUsingDefine, shouldTransformInitializers,
    shouldTransformPrivateElementsOrClassStaticBlocks, shouldTransformAutoAccessors, shouldTransformThisInStaticInitializers,
    shouldTransformSuperInStaticInitializers, shouldTransformPrivateStaticElementsInFile, legacyDecorators;
    private Slice<Node> pendingExpressions, pendingStatements;
    private classLexicalEnv lexicalEnvironment;
    private Node currentClassContainer, currentClassElement;
    private Dictionary<Node, Node> classAliases;
    private readonly HashSet<Node> enclosingClassDeclarations = new(ReferenceEqualityComparer.Instance);
    private bool inIterationStatement, insideComputedPropertyName;
    private Node parentNode, currentNode;
    private NodeVisitor modifierVisitor, discardedValueVisitor, heritageClauseVisitor, assignmentTargetVisitor,
    classElementVisitor, accessorFieldResultVisitor, arrayAssignmentElementVisitor, objectAssignmentElementVisitor, substitutionVisitor;
    private Func<Node, bool> isAnonymousClassNeedingAssignedName;
    private Func<Kind, Node> createModifier;
    private readonly Func<Node, bool> pendingExpressionCollector;
    private readonly Visitor constructorReferenceChecker;
    private Node constructorReferenceClassOriginal, constructorReferenceClassName;
    private ClassFieldsTransformer()
    {
        pendingExpressionCollector = collectPendingExpression;
        constructorReferenceChecker = checkConstructorReference;
    }
    private bool checkConstructorReference(Node node)
    {
        if (Ast.IsIdentifier(node) && node != constructorReferenceClassName)
        {
            var declaration = resolver.GetReferencedValueDeclaration(node);
            if (declaration == constructorReferenceClassOriginal)
            return true;
        }
        if (Ast.IsPropertyAccessExpression(node))
        return checkConstructorReference(node.Expression());
        return node.ForEachChild(constructorReferenceChecker);
    }
    private bool collectPendingExpression(Node expression)
    {
        addPendingExpressions(expression);
        return true;
    }
    // Upstream tsc/internal/transformers/estransforms/classfields.go:140:1.
    internal static Transformer NewClassFieldsTransformer(TransformOptions opts)
    {
        var languageVersion = opts.CompilerOptions.GetEmitScriptTarget();
        var useDefineForClassFields = opts.CompilerOptions.GetUseDefineForClassFields();
        if ((languageVersion >= ScriptTarget.ESNext) && useDefineForClassFields)
        {
            return null;
        }
        var tx = new ClassFieldsTransformer { compilerOptions = opts.CompilerOptions, resolver = opts.Resolver, legacyDecorators = opts.CompilerOptions.ExperimentalDecorators == Tristate.True };
        tx.shouldTransformInitializersUsingSet = !useDefineForClassFields;
        tx.shouldTransformInitializersUsingDefine = (useDefineForClassFields && (languageVersion < ScriptTarget.ES2022));
        tx.shouldTransformInitializers = (tx.shouldTransformInitializersUsingSet || tx.shouldTransformInitializersUsingDefine);
        tx.shouldTransformPrivateElementsOrClassStaticBlocks = (languageVersion < ScriptTarget.ES2022);
        tx.shouldTransformAutoAccessors = (languageVersion < ScriptTarget.ESNext);
        tx.shouldTransformThisInStaticInitializers = (languageVersion < ScriptTarget.ES2022);
        tx.shouldTransformSuperInStaticInitializers = tx.shouldTransformThisInStaticInitializers;
        var result = tx.NewTransformer(tx.visit, opts.Context);
        tx.createModifier = tx.Factory().NewModifier;
        tx.modifierVisitor = tx.EmitContext().NewNodeVisitor(tx.visitModifier);
        tx.discardedValueVisitor = tx.EmitContext().NewNodeVisitor(tx.visitDiscardedValue);
        tx.heritageClauseVisitor = tx.EmitContext().NewNodeVisitor(tx.visitHeritageClause);
        tx.assignmentTargetVisitor = tx.EmitContext().NewNodeVisitor(tx.visitAssignmentTarget);
        tx.classElementVisitor = tx.EmitContext().NewNodeVisitor(tx.visitClassElement);
        tx.accessorFieldResultVisitor = tx.EmitContext().NewNodeVisitor(tx.visitAccessorFieldResult);
        tx.arrayAssignmentElementVisitor = tx.EmitContext().NewNodeVisitor(tx.visitArrayAssignmentElement);
        tx.objectAssignmentElementVisitor = tx.EmitContext().NewNodeVisitor(tx.visitObjectAssignmentElement);
        tx.substitutionVisitor = tx.EmitContext().NewNodeVisitor(tx.visitForSubstitution);
        tx.isAnonymousClassNeedingAssignedName = tx.isAnonymousClassNeedingAssignedNameWorker;
        return result;
    }

    // requiresBlockScopedVar returns true when private field temp variables should be
    // declared as block-scoped (let) rather than function-scoped (var). This occurs when
    // a class expression is directly inside a loop body.
    // Replaces Strada's resolver.hasNodeCheckFlag(node, NodeCheckFlags.BlockScopedBindingInLoop).
    // Upstream tsc/internal/transformers/estransforms/classfields.go:199:1.
    private bool requiresBlockScopedVar()
    {
        return ((this.inIterationStatement && (this.currentClassContainer != null)) && Ast.IsClassExpression(this.currentClassContainer));
    }

    // classExpressionNeedsBlockScopedTemp returns true when the class expression's temp variable
    // must be block-scoped. This is more specific than requiresBlockScopedVar: the class temp only
    // needs to be block-scoped when the class expression has a non-static property with a computed
    // property name inside a loop (matching the checker's BlockScopedBindingInLoop on the class node).
    // Upstream tsc/internal/transformers/estransforms/classfields.go:207:1.
    private bool classExpressionNeedsBlockScopedTemp()
    {
        if (!this.requiresBlockScopedVar())
        {
            return false;
        }
        {
            foreach (var member in GoRuntime.RangeValues(this.currentClassContainer.Members()))
            {
                if (((Ast.IsPropertyDeclaration(member) && !Ast.HasStaticModifier(member)) && (member.Name() != null)) && Ast.IsComputedPropertyName(member.Name()))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:220:1.
    private Node visitSourceFile(SourceFile node)
    {
        if (node.IsDeclarationFile)
        {
            return node.AsNode();
        }
        this.lexicalEnvironment = null;
        this.shouldTransformPrivateStaticElementsInFile = ((this.EmitContext().EmitFlags(node.AsNode()) & EmitFlags.TransformPrivateStaticElements) != 0);
        this.classAliases = new Dictionary<Node, Node>(ReferenceEqualityComparer.Instance);
        this.enclosingClassDeclarations.Clear();
        var visited = this.Visitor().VisitEachChild(node.AsNode());
        this.EmitContext().AddEmitHelper(visited, this.EmitContext().ReadEmitHelpers());
        this.classAliases = null;
        this.enclosingClassDeclarations.Clear();
        return visited;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:235:1.
    private Node visitModifier(Node node)
    {
        if (node.Kind == Kind.AccessorKeyword)
        {
            if (this.shouldTransformAutoAccessorsInCurrentClass())
            {
                return null;
            }
            return node;
        }
        if (Ast.IsModifier(node))
        {
            return node;
        }
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:248:1.
    private Node pushNode(Node node)
    {
        Node grandparentNode = default;
        grandparentNode = this.parentNode;
        this.parentNode = this.currentNode;
        this.currentNode = node;
        return grandparentNode;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:255:1.
    private void popNode(Node grandparentNode)
    {
        this.currentNode = this.parentNode;
        this.parentNode = grandparentNode;
    }

    // visitForSubstitution visits nodes solely for class alias substitution in subtrees
    // that don't contain class field or lexical this/super transforms. It substitutes
    // identifiers that reference class declarations with their aliases, while skipping
    // the .Name() of PropertyAccessExpressions since Strada's onSubstituteNode only
    // fires for EmitHint.Expression, which excludes property access names.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:265:1.
    private Node visitForSubstitution(Node node)
    {
        if (node.Kind == Kind.Identifier)
        {
            return this.visitIdentifier(node.AsIdentifier());
        }
        if ((node.Kind == Kind.PropertyAccessExpression) && Ast.IsIdentifier(node.AsPropertyAccessExpression().Name()))
        {
            return this.visitPropertyAccessExpressionForSubstitution(node.AsPropertyAccessExpression());
        }
        return this.substitutionVisitor.VisitEachChild(node);
    }

    // visit is the main visitor.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:276:1.
    private Node visit(Node node)
    {
        var grandparentNode = this.pushNode(node);
        try
        {
            if (((uint)node.SubtreeFacts() & ((SubtreeFactValues.ContainsClassFields | SubtreeFactValues.ContainsLexicalThisOrSuper))) == 0)
            {
                if ((this.currentClassContainer != null) && (GoRuntime.Len(this.classAliases) > 0))
                {
                    return this.visitForSubstitution(node);
                }
                return node;
            }
            switch (node.Kind)
            {
                case Kind.SourceFile:
                {
                    return this.visitSourceFile(node.AsSourceFile());
                }
                case Kind.ClassDeclaration:
                {
                    return this.visitClassDeclaration(node.AsClassDeclaration());
                }
                case Kind.ClassExpression:
                {
                    return this.visitClassExpression(node.AsClassExpression());
                }
                case Kind.ClassStaticBlockDeclaration:
                case Kind.PropertyDeclaration:
                {
                    throw new InvalidOperationException("Use `classElementVisitor` instead.");
                }
                case Kind.PropertyAssignment:
                {
                    return this.visitPropertyAssignment(node.AsPropertyAssignment());
                }
                case Kind.VariableStatement:
                {
                    return this.visitVariableStatement(node.AsVariableStatement());
                }
                case Kind.VariableDeclaration:
                {
                    return this.visitVariableDeclaration(node.AsVariableDeclaration());
                }
                case Kind.Parameter:
                {
                    return this.visitParameterDeclaration(node.AsParameterDeclaration());
                }
                case Kind.BindingElement:
                {
                    return this.visitBindingElement(node.AsBindingElement());
                }
                case Kind.ExportAssignment:
                {
                    return this.visitExportAssignment(node.AsExportAssignment());
                }
                case Kind.PrivateIdentifier:
                {
                    return this.visitPrivateIdentifier(node);
                }
                case Kind.PropertyAccessExpression:
                {
                    return this.visitPropertyAccessExpression(node.AsPropertyAccessExpression());
                }
                case Kind.ElementAccessExpression:
                {
                    return this.visitElementAccessExpression(node.AsElementAccessExpression());
                }
                case Kind.PrefixUnaryExpression:
                case Kind.PostfixUnaryExpression:
                {
                    return this.visitPreOrPostfixUnaryExpression(node, false);
                }
                case Kind.BinaryExpression:
                {
                    return this.visitBinaryExpression(node.AsBinaryExpression(), false);
                }
                case Kind.ParenthesizedExpression:
                {
                    return this.visitParenthesizedExpression(node.AsParenthesizedExpression(), false);
                }
                case Kind.CallExpression:
                {
                    return this.visitCallExpression(node.AsCallExpression());
                }
                case Kind.ExpressionStatement:
                {
                    return this.visitExpressionStatement(node.AsExpressionStatement());
                }
                case Kind.TaggedTemplateExpression:
                {
                    return this.visitTaggedTemplateExpression(node.AsTaggedTemplateExpression());
                }
                case Kind.ForStatement:
                {
                    return this.visitForStatement(node.AsForStatement());
                }
                case Kind.ForInStatement:
                case Kind.ForOfStatement:
                case Kind.DoStatement:
                case Kind.WhileStatement:
                {
                    return this.setInIterationStatementAnd(true, static (tx, value) => tx.visitEachChildOfNode(value), node);
                }
                case Kind.ThisKeyword:
                {
                    return this.visitThisExpression(node);
                }
                case Kind.FunctionDeclaration:
                case Kind.FunctionExpression:
                {
                    return this.setInIterationStatementAnd(false, static (tx, value) => tx.visitFunctionExpressionOrDeclaration(value), node);
                }
                case Kind.Constructor:
                case Kind.MethodDeclaration:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                {
                    return this.setInIterationStatementAnd(false, static (tx, value) => tx.setClassElementAndVisitEachChild(value), node);
                }
                default:
                {
                    return this.Visitor().VisitEachChild(node);
                }
            }
        }
        finally { this.popNode(grandparentNode); }
    }

    // visitDiscardedValue visits a node in an expression whose result is discarded.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:343:1.
    private Node visitDiscardedValue(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PrefixUnaryExpression:
            case Kind.PostfixUnaryExpression:
            {
                return this.visitPreOrPostfixUnaryExpression(node, true);
            }
            case Kind.BinaryExpression:
            {
                return this.visitBinaryExpression(node.AsBinaryExpression(), true);
            }
            case Kind.ParenthesizedExpression:
            {
                return this.visitParenthesizedExpression(node.AsParenthesizedExpression(), true);
            }
            default:
            {
                return this.visit(node);
            }
        }
    }

    // visitHeritageClause visits a node in a HeritageClause.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:357:1.
    private Node visitHeritageClause(Node node)
    {
        switch (node.Kind)
        {
            case Kind.HeritageClause:
            {
                return this.heritageClauseVisitor.VisitEachChild(node);
            }
            case Kind.ExpressionWithTypeArguments:
            {
                return this.visitExpressionWithTypeArgumentsInHeritageClause(node.AsExpressionWithTypeArguments());
            }
            default:
            {
                return this.visit(node);
            }
        }
    }

    // visitAssignmentTarget visits the assignment target of a destructuring assignment.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:369:1.
    private Node visitAssignmentTarget(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ObjectLiteralExpression:
            case Kind.ArrayLiteralExpression:
            {
                return this.visitAssignmentPattern(node);
            }
            default:
            {
                return this.visit(node);
            }
        }
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:378:1.
    private Node visitDestructuringAssignmentTarget(Node node)
    {
        if (Ast.IsObjectLiteralExpression(node) || Ast.IsArrayLiteralExpression(node))
        {
            return this.visitAssignmentPattern(node);
        }
        if (Ast.IsPropertyAccessExpression(node) && Ast.IsPrivateIdentifier(node.AsPropertyAccessExpression().Name()))
        {
            return this.wrapPrivateIdentifierForDestructuringTarget(node);
        }
        if (((((this.shouldTransformSuperInStaticInitializers && (this.currentClassElement != null)) && Ast.IsSuperProperty(node)) && EsTransforms.IsStaticPropertyDeclarationOrClassStaticBlock(this.currentClassElement)) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null))
        {
            var data = this.lexicalEnvironment.data;
            if ((data.facts & classFacts.ClassWasDecorated) != 0)
            {
                return this.visitInvalidSuperProperty(node);
            }
            if ((data.classConstructor != null) && (data.superClassReference != null))
            {
                Node name = default;
                if (Ast.IsElementAccessExpression(node))
                {
                    name = this.Visitor().VisitNode(node.AsElementAccessExpression().ArgumentExpression);
                }
                else
                {
                    if (Ast.IsPropertyAccessExpression(node) && Ast.IsIdentifier(node.AsPropertyAccessExpression().Name()))
                    {
                        name = this.Factory().NewStringLiteralFromNode(node.AsPropertyAccessExpression().Name());
                    }
                }
                if (name != null)
                {
                    var temp = this.Factory().NewTempVariable();
                    var setExpr = this.Factory().NewReflectSetCall(data.superClassReference, name, temp, data.classConstructor);
                    return this.Factory().NewAssignmentTargetWrapper(temp, setExpr);
                }
            }
        }
        return this.Visitor().VisitEachChild(node);
    }

    // visitClassElement visits a member of a class.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:416:1.
    private Node visitClassElement(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Constructor:
            {
                return this.setCurrentClassElementAnd(node, static (tx, value) => tx.visitConstructorDeclaration(value), node);
            }
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.MethodDeclaration:
            {
                return this.setCurrentClassElementAnd(node, static (tx, value) => tx.visitMethodOrAccessorDeclaration(value), node);
            }
            case Kind.PropertyDeclaration:
            {
                return this.setCurrentClassElementAnd(node, static (tx, value) => tx.visitPropertyDeclaration(value), node);
            }
            case Kind.ClassStaticBlockDeclaration:
            {
                return this.setCurrentClassElementAnd(node, static (tx, value) => tx.visitClassStaticBlockDeclaration(value), node);
            }
            case Kind.ComputedPropertyName:
            {
                return this.visitComputedPropertyName(node.AsComputedPropertyName());
            }
            case Kind.SemicolonClassElement:
            {
                return node;
            }
            default:
            {
                if (Ast.IsModifierLike(node))
                {
                    return this.visitModifier(node);
                }
                return this.visit(node);
            }
        }
    }

    // visitPropertyName visits a property name of a class member.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:439:1.
    private Node visitPropertyName(Node name)
    {
        if (Ast.IsComputedPropertyName(name))
        {
            return this.visitComputedPropertyName(name.AsComputedPropertyName());
        }
        return this.Visitor().VisitNode(name);
    }

    // visitAccessorFieldResult visits the results of an auto-accessor field transformation in a second pass.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:447:1.
    private Node visitAccessorFieldResult(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertyDeclaration:
            {
                return this.transformFieldInitializer(node.AsPropertyDeclaration());
            }
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            {
                return this.visitClassElement(node);
            }
            default:
            {
                Debug.Fail($"Expected node to either be a PropertyDeclaration, GetAccessorDeclaration, or SetAccessorDeclaration\nNode {node.KindString()} was unexpected.");
                return null;
            }
        }
    }

    // visitIdentifier replaces Strada's onSubstituteNode/trySubstituteClassAlias. Instead of
    // substituting at emit time using NodeCheckFlags.ConstructorReference, we resolve the
    // identifier to its declaration and check if that declaration has a registered alias.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:462:1.
    private Node visitIdentifier(Identifier node)
    {
        var declaration = this.resolver.GetReferencedValueDeclaration(this.EmitContext().MostOriginal(node.AsNode()));
        if (declaration != null)
        {
            {
                var ok = this.classAliases.TryGetValue(declaration, out var alias);
                if (ok && this.enclosingClassDeclarations.Contains(declaration))
                {
                    var clone = alias.Clone(this.Factory());
                    this.EmitContext().SetSourceMapRange(clone, node.Loc);
                    this.EmitContext().SetCommentRange(clone, node.Loc);
                    return clone;
                }
            }
        }
        return node.AsNode();
    }

    // visitPrivateIdentifier handles an undeclared private name. Replace it with an empty
    // identifier to indicate a problem with the code.
    // Note: private identifiers in statement position (e.g., `#;`) are intercepted earlier
    // by visitExpressionStatement, which preserves them so the runtime throws a SyntaxError.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:479:1.
    private Node visitPrivateIdentifier(Node node)
    {
        if (!this.shouldTransformPrivateElementsOrClassStaticBlocks)
        {
            return node;
        }
        if ((this.parentNode != null) && Ast.IsStatement(this.parentNode))
        {
            return node;
        }
        var result = this.Factory().NewIdentifier("");
        this.EmitContext().SetOriginal(result, node);
        return result;
    }

    // transformPrivateIdentifierInInExpression visits `#id in expr`.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:492:1.
    private Node transformPrivateIdentifierInInExpression(BinaryExpression node)
    {
        var info = this.accessPrivateIdentifier(node.Left);
        if (info != null)
        {
            var receiver = this.Visitor().VisitNode(node.Right);
            var result = this.Factory().NewClassPrivateFieldInHelper(info.brandCheckIdentifier, receiver);
            this.EmitContext().SetOriginal(result, node.AsNode());
            return result;
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:504:1.
    private Node visitPropertyAssignment(PropertyAssignment node)
    {
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node.AsNode(), this.isAnonymousClassNeedingAssignedName))
        {
            node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node.AsNode(), false, default).AsPropertyAssignment();
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:518:1.
    private Node visitVariableStatement(VariableStatement node)
    {
        var savedPendingStatements = this.pendingStatements;
        this.pendingStatements = default;
        var visitedNode = this.Visitor().VisitEachChild(node.AsNode());
        if (GoRuntime.Len(this.pendingStatements) > 0)
        {
            var result = Slice<Node>.Make(0, 1 + this.pendingStatements.Length);
            result = result.Append(visitedNode);
            result = result.Append(this.pendingStatements);
            this.pendingStatements = savedPendingStatements;
            return this.Factory().NewSyntaxList(result);
        }
        this.pendingStatements = savedPendingStatements;
        return visitedNode;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:536:1.
    private Node visitVariableDeclaration(VariableDeclaration node)
    {
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node.AsNode(), this.isAnonymousClassNeedingAssignedName))
        {
            node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node.AsNode(), false, default).AsVariableDeclaration();
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:557:1.
    private Node visitParameterDeclaration(ParameterDeclaration node)
    {
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node.AsNode(), this.isAnonymousClassNeedingAssignedName))
        {
            node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node.AsNode(), false, default).AsParameterDeclaration();
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:580:1.
    private Node visitBindingElement(BindingElement node)
    {
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node.AsNode(), this.isAnonymousClassNeedingAssignedName))
        {
            node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node.AsNode(), false, default).AsBindingElement();
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:603:1.
    private Node visitExportAssignment(ExportAssignment node)
    {
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node.AsNode(), this.isAnonymousClassNeedingAssignedName))
        {
            var assignedName = "";
            if (!node.IsExportEquals)
            {
                assignedName = "default";
            }
            node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node.AsNode(), true, ByteStringValue.FromString(assignedName)).AsExportAssignment();
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:623:1.
    private Node injectPendingExpressions(Node expression)
    {
        if (GoRuntime.Len(this.pendingExpressions) > 0)
        {
            if (Ast.IsParenthesizedExpression(expression))
            {
                this.pendingExpressions = this.pendingExpressions.Append(expression.Expression());
                expression = this.Factory().UpdateParenthesizedExpression(expression.AsParenthesizedExpression(), this.Factory().InlineExpressions(this.pendingExpressions));
            }
            else
            {
                var exprs = this.pendingExpressions.Append(expression);
                expression = this.Factory().InlineExpressions(exprs);
            }
            this.pendingExpressions = default;
        }
        return expression;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:640:1.
    private Node visitComputedPropertyName(ComputedPropertyName node)
    {
        var savedLexicalEnvironment = this.lexicalEnvironment;
        var savedInsideComputedPropertyName = this.insideComputedPropertyName;
        this.insideComputedPropertyName = true;
        if ((this.lexicalEnvironment != null) && (this.lexicalEnvironment.previous != null))
        {
            this.lexicalEnvironment = this.lexicalEnvironment.previous;
        }
        var expression = this.Visitor().VisitNode(node.Expression);
        this.lexicalEnvironment = savedLexicalEnvironment;
        this.insideComputedPropertyName = savedInsideComputedPropertyName;
        return this.Factory().UpdateComputedPropertyName(node, this.injectPendingExpressions(expression));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:656:1.
    private Node visitConstructorDeclaration(Node node)
    {
        if (this.currentClassContainer != null)
        {
            return this.transformConstructor(node.AsConstructorDeclaration(), this.currentClassContainer);
        }
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:663:1.
    private bool shouldTransformClassElementToWeakMap(Node node)
    {
        if (this.shouldTransformPrivateElementsOrClassStaticBlocks)
        {
            return true;
        }
        return this.shouldAlwaysTransformPrivateStaticElements(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:670:1.
    private bool shouldAlwaysTransformPrivateStaticElements(Node node)
    {
        return (Ast.HasStaticModifier(node) && ((this.EmitContext().EmitFlags(node) & EmitFlags.TransformPrivateStaticElements) != 0));
    }

    // nodeHasTransformPrivateStaticElementsFlag checks the emit flag on a class node (not a member).
    // Unlike shouldAlwaysTransformPrivateStaticElements, this does not check HasStaticModifier,
    // since class nodes themselves don't have a static modifier.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:677:1.
    private bool nodeHasTransformPrivateStaticElementsFlag(Node node)
    {
        return ((this.EmitContext().EmitFlags(node) & EmitFlags.TransformPrivateStaticElements) != 0);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:681:1.
    private Node visitMethodOrAccessorDeclaration(Node node)
    {
        Debug.Assert(!Ast.HasDecorators(node));
        if (!Ast.IsPrivateIdentifierClassElementDeclaration(node) || !this.shouldTransformClassElementToWeakMap(node))
        {
            return this.classElementVisitor.VisitEachChild(node);
        }
        var tuple2 = this.getPrivateIdentifier(this.getPrivateIdentifierEnvironment(), node.Name()); var info = tuple2.Item1;
        Debug.Assert((info != null), "Undeclared private name for property declaration.");
        if ((info.kind == PrivateIdentifierKind.Untransformed) || !info.isValid)
        {
            return node;
        }
        var functionName = this.getHoistedFunctionName(node);
        if (functionName != null)
        {
            var modifiers = this.extractNonStaticNonAccessorModifiers(node);
            this.EmitContext().StartVariableEnvironment();
            var saved = this.inIterationStatement;
            this.inIterationStatement = false;
            var body = this.EmitContext().VisitFunctionBody(node.Body(), this.Visitor());
            var @params = this.Visitor().VisitNodes(node.ParameterList());
            this.inIterationStatement = saved;
            var funcExpr = this.Factory().NewFunctionExpression(modifiers, node.BodyData().AsteriskToken, functionName, null, @params, null, null, body);
            var assignment = this.Factory().NewAssignmentExpression(functionName, funcExpr);
            this.addPendingExpressions(assignment);
        }
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:714:1.
    private ModifierList extractNonStaticNonAccessorModifiers(Node node)
    {
        return Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), ~(ModifierFlags.Static | ModifierFlags.Accessor));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:718:1.
    private Node setCurrentClassElementAnd(Node classElement, Func<ClassFieldsTransformer, Node, Node> visitor, Node node2)
    {
        if (classElement != this.currentClassElement)
        {
            var saved = this.currentClassElement;
            this.currentClassElement = classElement;
            var result = visitor(this, node2);
            this.currentClassElement = saved;
            return result;
        }
        return visitor(this, node2);
    }

    // visitEachChildOfNode just calls Visitor.VisitEachChild, but is necessary to avoid repeated closure allocations when passing as a callback.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:730:1.
    private Node visitEachChildOfNode(Node node)
    {
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:734:1.
    private Node setInIterationStatementAnd(bool inIteration, Func<ClassFieldsTransformer, Node, Node> visitor, Node node2)
    {
        if (this.inIterationStatement != inIteration)
        {
            var saved = this.inIterationStatement;
            this.inIterationStatement = inIteration;
            var result = visitor(this, node2);
            this.inIterationStatement = saved;
            return result;
        }
        return visitor(this, node2);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:745:1.
    private Node clearClassElementAndVisitEachChild(Node node)
    {
        return this.setCurrentClassElementAnd(null, static (tx, value) => tx.visitEachChildOfNode(value), node);
    }

    // visitFunctionExpressionOrDeclaration handles lexical environment scoping for function
    // expressions and declarations, mirroring Strada's onEmitNode behavior.
    //
    // In Strada, onEmitNode checks whether a FunctionExpression has been registered in
    // lexicalEnvironmentMap (via its original node). If found, the lexical environment is
    // restored; otherwise it is cleared (since regular functions create a new `this` scope).
    //
    // Since Corsa performs substitution eagerly (no emit-time hooks), we replicate this by
    // preserving currentClassElement for function expressions whose original node is a class
    // member of the current class. This allows visitThisExpression to correctly substitute
    // `this` -> `_classThis` inside synthesized functions (e.g., ES decorator descriptor
    // methods for static private auto-accessors).
    // Upstream tsc/internal/transformers/estransforms/classfields.go:761:1.
    private Node visitFunctionExpressionOrDeclaration(Node node)
    {
        if (this.currentClassElement != null)
        {
            var original = this.EmitContext().MostOriginal(node);
            if ((original != node) && (this.currentClassContainer != null))
            {
                {
                    foreach (var member in GoRuntime.RangeValues(this.currentClassContainer.Members()))
                    {
                        if ((this.EmitContext().MostOriginal(member) == original) && Ast.IsStatic(member))
                        {
                            return this.visitEachChildOfNode(node);
                        }
                    }
                }
            }
        }
        return this.setCurrentClassElementAnd(null, static (tx, value) => tx.visitEachChildOfNode(value), node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:781:1.
    private Node setClassElementAndVisitEachChild(Node node)
    {
        return this.setCurrentClassElementAnd(node, static (tx, value) => tx.visitEachChildOfNode(value), node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:785:1.
    private Node getHoistedFunctionName(Node node)
    {
        Debug.Assert(((node.Name() != null) && Ast.IsPrivateIdentifier(node.Name())));
        var info = this.accessPrivateIdentifier(node.Name());
        Debug.Assert((info != null), "Undeclared private name for property declaration.");
        if (info.kind == PrivateIdentifierKind.Method)
        {
            return info.methodName;
        }
        if (info.kind == PrivateIdentifierKind.Accessor)
        {
            if (Ast.IsGetAccessorDeclaration(node))
            {
                return info.getterName;
            }
            if (Ast.IsSetAccessorDeclaration(node))
            {
                return info.setterName;
            }
        }
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:803:1.
    private Node tryGetClassThis()
    {
        {
            var classThis = this.tryGetClassThisNoContainer();
            if (classThis != null)
            {
                return classThis;
            }
        }
        if (this.currentClassContainer != null)
        {
            return this.currentClassContainer.Name();
        }
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:813:1.
    private Node tryGetClassThisNoContainer()
    {
        var lex = this.getClassLexicalEnvironment();
        if (lex.classThis != null)
        {
            return lex.classThis;
        }
        if (lex.classConstructor != null)
        {
            return lex.classConstructor;
        }
        return null;
    }

    // transformAutoAccessor transforms an auto-accessor property:
    //
    //	accessor x = 1;
    //
    // into:
    //
    //	#x = 1;
    //	get x() { return this.#x; }
    //	set x(value) { this.#x = value; }
    // Upstream tsc/internal/transformers/estransforms/classfields.go:833:1.
    private Node transformAutoAccessor(PropertyDeclaration node)
    {
        var commentRange = this.EmitContext().CommentRange(node.AsNode());
        var sourceMapRange = this.EmitContext().SourceMapRange(node.AsNode());
        var name = node.Name();
        var getterName = name;
        var setterName = name;
        if (Ast.IsComputedPropertyName(name) && !Transformers.IsSimpleInlineableExpression(name.Expression()))
        {
            var cacheAssignment = EsTransforms.FindComputedPropertyNameCacheAssignment(this.EmitContext(), name);
            if (cacheAssignment != null)
            {
                getterName = this.Factory().UpdateComputedPropertyName(name.AsComputedPropertyName(), this.Visitor().VisitNode(name.Expression()));
                setterName = this.Factory().UpdateComputedPropertyName(name.AsComputedPropertyName(), cacheAssignment.Left);
            }
            else
            {
                var temp = this.Factory().NewTempVariable();
                this.EmitContext().SetSourceMapRange(temp, name.Expression().Loc);
                this.EmitContext().AddVariableDeclaration(temp);
                var expression = this.Visitor().VisitNode(name.Expression());
                var assignment = this.Factory().NewAssignmentExpression(temp, expression);
                this.EmitContext().SetSourceMapRange(assignment, name.Expression().Loc);
                getterName = this.Factory().UpdateComputedPropertyName(name.AsComputedPropertyName(), assignment);
                setterName = this.Factory().UpdateComputedPropertyName(name.AsComputedPropertyName(), temp);
            }
        }
        var modifiers = this.modifierVisitor.VisitModifiers(node.Modifiers());
        var backingField = EsTransforms.CreateAccessorPropertyBackingField(this.Factory(), node, modifiers, node.Initializer);
        this.EmitContext().SetOriginal(backingField, node.AsNode());
        this.EmitContext().AddEmitFlags(backingField, EmitFlags.NoComments);
        this.EmitContext().SetSourceMapRange(backingField, sourceMapRange);
        Node receiver = default;
        if (Ast.IsStatic(node.AsNode()))
        {
            receiver = this.tryGetClassThis();
            if (receiver == null)
            {
                receiver = this.Factory().NewThisExpression();
            }
        }
        else
        {
            receiver = this.Factory().NewThisExpression();
        }
        var getter = this.createAccessorPropertyGetRedirector(node, modifiers, getterName, receiver);
        this.EmitContext().SetOriginal(getter, node.AsNode());
        this.EmitContext().SetCommentRange(getter, commentRange);
        this.EmitContext().SetSourceMapRange(getter, sourceMapRange);
        ModifierList setterModifiers = default;
        if (modifiers != null)
        {
            setterModifiers = this.Factory().NewModifierList(Ast.CreateModifiersFromModifierFlags(modifiers.ModifierFlags, this.createModifier));
        }
        var setter = this.createAccessorPropertySetRedirector(node, setterModifiers, setterName, receiver);
        this.EmitContext().SetOriginal(setter, node.AsNode());
        this.EmitContext().AddEmitFlags(setter, EmitFlags.NoComments);
        this.EmitContext().SetSourceMapRange(setter, sourceMapRange);
        var tuple4 = this.accessorFieldResultVisitor.VisitSlice(Slice<Node>.FromArray([backingField, getter, setter])); var visited = tuple4.Item1;
        return this.Factory().NewSyntaxList(visited);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:895:1.
    private Node transformPrivateFieldInitializer(PropertyDeclaration node)
    {
        if (this.shouldTransformClassElementToWeakMap(node.AsNode()))
        {
            var tuple5 = this.getPrivateIdentifier(this.getPrivateIdentifierEnvironment(), node.Name()); var info = tuple5.Item1;
            Debug.Assert((info != null), "Undeclared private name for property declaration.");
            if ((info.kind == PrivateIdentifierKind.Untransformed) || !info.isValid)
            {
                return node.AsNode();
            }
            if (info.isStatic && !this.shouldTransformPrivateElementsOrClassStaticBlocks)
            {
                var statement = this.transformPropertyOrClassStaticBlock(node.AsNode(), this.Factory().NewThisExpression());
                if (statement != null)
                {
                    return this.Factory().NewClassStaticBlockDeclaration(null, this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([statement])), true));
                }
            }
            return null;
        }
        if ((((this.shouldTransformInitializersUsingSet && !Ast.HasStaticModifier(node.AsNode())) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null)) && ((this.lexicalEnvironment.data.facts & classFacts.WillHoistInitializersToConstructor) != 0))
        {
            return this.Factory().UpdatePropertyDeclaration(node, this.Visitor().VisitModifiers(node.Modifiers()), node.Name(), null, null, null);
        }
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node.AsNode(), this.isAnonymousClassNeedingAssignedName))
        {
            node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node.AsNode(), false, default).AsPropertyDeclaration();
        }
        return this.Factory().UpdatePropertyDeclaration(node,
            this.modifierVisitor.VisitModifiers(node.Modifiers()),
            this.visitPropertyName(node.Name()),
            null,
            null,
            this.Visitor().VisitNode(node.Initializer));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:949:1.
    private Node transformPublicFieldInitializer(PropertyDeclaration node)
    {
        if (this.shouldTransformInitializers && !Ast.IsAutoAccessorPropertyDeclaration(node.AsNode()))
        {
            var expr = this.getPropertyNameExpressionIfNeeded(node.Name(), ((node.Initializer != null) || this.compilerOptions.GetUseDefineForClassFields()));
            if (expr != null)
            {
                EsTransforms.FlattenCommaList(expr).Visit(pendingExpressionCollector);
            }
            if (Ast.IsStatic(node.AsNode()) && !this.shouldTransformPrivateElementsOrClassStaticBlocks)
            {
                var initializerStatement = this.transformPropertyOrClassStaticBlock(node.AsNode(), this.Factory().NewThisExpression());
                if (initializerStatement != null)
                {
                    var staticBlock = this.Factory().NewClassStaticBlockDeclaration(null,
                        this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([initializerStatement])),
                            false));
                    this.EmitContext().SetOriginal(staticBlock, node.AsNode());
                    this.EmitContext().SetCommentRange(staticBlock, node.Loc);
                    this.EmitContext().AddEmitFlags(initializerStatement, EmitFlags.NoComments);
                    return staticBlock;
                }
            }
            return null;
        }
        return this.Factory().UpdatePropertyDeclaration(node,
            this.modifierVisitor.VisitModifiers(node.Modifiers()),
            this.visitPropertyName(node.Name()),
            null,
            null,
            this.Visitor().VisitNode(node.Initializer));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:993:1.
    private Node transformFieldInitializer(PropertyDeclaration node)
    {
        Debug.Assert(!Ast.HasDecorators(node.AsNode()), "Decorators should already have been transformed and elided.");
        if (Ast.IsPrivateIdentifierClassElementDeclaration(node.AsNode()))
        {
            return this.transformPrivateFieldInitializer(node);
        }
        return this.transformPublicFieldInitializer(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1001:1.
    private bool shouldTransformAutoAccessorsInCurrentClass()
    {
        if (this.shouldTransformAutoAccessors)
        {
            return true;
        }
        return (((this.lexicalEnvironment != null) && (this.lexicalEnvironment.data != null)) && ((this.lexicalEnvironment.data.facts & classFacts.WillHoistInitializersToConstructor) != 0));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1011:1.
    private Node visitPropertyDeclaration(Node node)
    {
        var propDecl = node.AsPropertyDeclaration();
        if (Ast.IsAutoAccessorPropertyDeclaration(node) && ((this.shouldTransformAutoAccessorsInCurrentClass() || (Ast.HasStaticModifier(node) && this.shouldAlwaysTransformPrivateStaticElements(node)))))
        {
            return this.transformAutoAccessor(propDecl);
        }
        return this.transformFieldInitializer(propDecl);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1022:1.
    private Node createPrivateIdentifierAccess(privateIdentifierInfo info, Node receiver)
    {
        receiver = this.Visitor().VisitNode(receiver);
        return this.createPrivateIdentifierAccessHelper(info, receiver);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1027:1.
    private Node createPrivateIdentifierAccessHelper(privateIdentifierInfo info, Node receiver)
    {
        this.EmitContext().SetCommentRange(receiver, Core.NewTextRange(-1, receiver.End()));
        switch (info.kind)
        {
            case var kind when kind == PrivateIdentifierKind.Accessor:
            {
                return this.Factory().NewClassPrivateFieldGetHelper(receiver, info.brandCheckIdentifier, info.kind, info.getterName);
            }
            case var kind when kind == PrivateIdentifierKind.Method:
            {
                return this.Factory().NewClassPrivateFieldGetHelper(receiver, info.brandCheckIdentifier, info.kind, info.methodName);
            }
            case var kind when kind == PrivateIdentifierKind.Field:
            {
                Node f = default;
                if (info.isStatic)
                {
                    f = info.variableName;
                }
                return this.Factory().NewClassPrivateFieldGetHelper(receiver, info.brandCheckIdentifier, info.kind, f);
            }
            case var kind when kind == PrivateIdentifierKind.Untransformed:
            {
                Debug.Fail("Access helpers should not be created for untransformed private elements");
                return null;
            }
        }
        Debug.Fail("Unknown private element type");
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1064:1.
    private Node visitPropertyAccessExpression(PropertyAccessExpression node)
    {
        if (Ast.IsPrivateIdentifier(node.Name()))
        {
            var info = this.accessPrivateIdentifier(node.Name());
            if (info != null)
            {
                var result = this.createPrivateIdentifierAccess(info, node.Expression);
                this.EmitContext().SetOriginal(result, node.AsNode());
                result.Loc = node.Loc;
                return result;
            }
        }
        if ((((((this.shouldTransformSuperInStaticInitializers && (this.currentClassElement != null)) && Ast.IsSuperProperty(node.AsNode())) && Ast.IsIdentifier(node.Name())) && EsTransforms.IsStaticPropertyDeclarationOrClassStaticBlock(this.currentClassElement)) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null))
        {
            var data = this.lexicalEnvironment.data;
            if ((data.facts & classFacts.ClassWasDecorated) != 0)
            {
                return this.visitInvalidSuperProperty(node.AsNode());
            }
            if ((data.classConstructor != null) && (data.superClassReference != null))
            {
                var superProperty = this.Factory().NewReflectGetCall(data.superClassReference, this.Factory().NewStringLiteralFromNode(node.Name()), data.classConstructor);
                this.EmitContext().SetOriginal(superProperty, node.Expression);
                superProperty.Loc = node.Expression.Loc;
                return superProperty;
            }
        }
        if (Ast.IsIdentifier(node.Name()))
        {
            return this.visitPropertyAccessExpressionForSubstitution(node);
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // visitPropertyAccessExpressionForSubstitution visits only the expression of a PropertyAccessExpression,
    // leaving the name unchanged. This prevents the name from being treated as a standalone identifier
    // reference and incorrectly substituted with a class alias.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:1108:1.
    private Node visitPropertyAccessExpressionForSubstitution(PropertyAccessExpression node)
    {
        var expression = this.Visitor().VisitNode(node.Expression);
        if (expression != node.Expression)
        {
            return this.Factory().UpdatePropertyAccessExpression(node, expression, node.QuestionDotToken, node.Name(), node.Flags);
        }
        return node.AsNode();
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1116:1.
    private Node visitElementAccessExpression(ElementAccessExpression node)
    {
        if (((((this.shouldTransformSuperInStaticInitializers && (this.currentClassElement != null)) && Ast.IsSuperProperty(node.AsNode())) && EsTransforms.IsStaticPropertyDeclarationOrClassStaticBlock(this.currentClassElement)) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null))
        {
            var data = this.lexicalEnvironment.data;
            if ((data.facts & classFacts.ClassWasDecorated) != 0)
            {
                return this.visitInvalidSuperProperty(node.AsNode());
            }
            if ((data.classConstructor != null) && (data.superClassReference != null))
            {
                var superProperty = this.Factory().NewReflectGetCall(data.superClassReference, this.Visitor().VisitNode(node.ArgumentExpression), data.classConstructor);
                this.EmitContext().SetOriginal(superProperty, node.Expression);
                superProperty.Loc = node.Expression.Loc;
                return superProperty;
            }
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1140:1.
    private Node visitPreOrPostfixUnaryExpression(Node node, bool discarded)
    {
        Kind @operator = default;
        Node operand = default;
        if (Ast.IsPrefixUnaryExpression(node))
        {
            @operator = node.AsPrefixUnaryExpression().Operator;
            operand = node.AsPrefixUnaryExpression().Operand;
        }
        else
        {
            @operator = node.AsPostfixUnaryExpression().Operator;
            operand = node.AsPostfixUnaryExpression().Operand;
        }
        if ((@operator == Kind.PlusPlusToken) || (@operator == Kind.MinusMinusToken))
        {
            var operandSkipped = Ast.SkipParentheses(operand);
            if (Ast.IsPropertyAccessExpression(operandSkipped) && Ast.IsPrivateIdentifier(operandSkipped.Name()))
            {
                var info = this.accessPrivateIdentifier(operandSkipped.Name());
                if (info != null)
                {
                    var receiver = this.Visitor().VisitNode(operandSkipped.Expression());
                    var tuple7 = this.createCopiableReceiverExpr(receiver); var readExpression = tuple7.Item1; var initializeExpression = tuple7.Item2;
                    var expression = this.createPrivateIdentifierAccessHelper(info, readExpression);
                    Node temp = default;
                    if (!Ast.IsPrefixUnaryExpression(node) && !discarded)
                    {
                        temp = this.Factory().NewTempVariable();
                        this.EmitContext().AddVariableDeclaration(temp);
                    }
                    expression = EsTransforms.ExpandPreOrPostfixIncrementOrDecrementExpression(this.Factory(), this.EmitContext(), node, expression, temp);
                    var assignReceiver = readExpression;
                    if (initializeExpression != null)
                    {
                        assignReceiver = initializeExpression;
                    }
                    expression = this.createPrivateIdentifierAssignment(info, assignReceiver, expression, Kind.EqualsToken);
                    this.EmitContext().SetOriginal(expression, node);
                    expression.Loc = node.Loc;
                    if (temp != null)
                    {
                        expression = this.Factory().NewCommaExpression(expression, temp);
                        expression.Loc = node.Loc;
                    }
                    return expression;
                }
            }
            else
            {
                if (((((this.shouldTransformSuperInStaticInitializers && (this.currentClassElement != null)) && Ast.IsSuperProperty(operandSkipped)) && EsTransforms.IsStaticPropertyDeclarationOrClassStaticBlock(this.currentClassElement)) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null))
                {
                    var data = this.lexicalEnvironment.data;
                    if ((data.facts & classFacts.ClassWasDecorated) != 0)
                    {
                        var visitedExpr = this.visitInvalidSuperProperty(operandSkipped);
                        if (Ast.IsPrefixUnaryExpression(node))
                        {
                            return this.Factory().UpdatePrefixUnaryExpression(node.AsPrefixUnaryExpression(), node.AsPrefixUnaryExpression().Operator, visitedExpr);
                        }
                        return this.Factory().UpdatePostfixUnaryExpression(node.AsPostfixUnaryExpression(), visitedExpr, node.AsPostfixUnaryExpression().Operator);
                    }
                    if ((data.classConstructor != null) && (data.superClassReference != null))
                    {
                        Node setterName = default;
                        Node getterName = default;
                        if (Ast.IsPropertyAccessExpression(operandSkipped))
                        {
                            if (Ast.IsIdentifier(operandSkipped.Name()))
                            {
                                getterName = this.Factory().NewStringLiteralFromNode(operandSkipped.Name());
                                setterName = getterName;
                            }
                        }
                        else
                        {
                            if (Ast.IsElementAccessExpression(operandSkipped))
                            {
                                if (Transformers.IsSimpleInlineableExpression(operandSkipped.AsElementAccessExpression().ArgumentExpression))
                                {
                                    getterName = operandSkipped.AsElementAccessExpression().ArgumentExpression;
                                    setterName = getterName;
                                }
                                else
                                {
                                    getterName = this.Factory().NewTempVariable();
                                    this.EmitContext().AddVariableDeclaration(getterName);
                                    setterName = this.Factory().NewAssignmentExpression(getterName,
                                        this.Visitor().VisitNode(operandSkipped.AsElementAccessExpression().ArgumentExpression));
                                }
                            }
                        }
                        if ((setterName != null) && (getterName != null))
                        {
                            var expression2 = this.Factory().NewReflectGetCall(data.superClassReference, getterName, data.classConstructor);
                            expression2.Loc = operandSkipped.Loc;
                            Node temp2 = default;
                            if (!discarded)
                            {
                                temp2 = this.Factory().NewTempVariable();
                                this.EmitContext().AddVariableDeclaration(temp2);
                            }
                            expression2 = EsTransforms.ExpandPreOrPostfixIncrementOrDecrementExpression(this.Factory(), this.EmitContext(), node, expression2, temp2);
                            expression2 = this.Factory().NewReflectSetCall(data.superClassReference, setterName, expression2, data.classConstructor);
                            this.EmitContext().SetOriginal(expression2, node);
                            expression2.Loc = node.Loc;
                            if (temp2 != null)
                            {
                                expression2 = this.Factory().NewCommaExpression(expression2, temp2);
                                expression2.Loc = node.Loc;
                            }
                            return expression2;
                        }
                    }
                }
            }
        }
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1244:1.
    private Node visitForStatement(ForStatement node)
    {
        var initializer = this.discardedValueVisitor.VisitNode(node.Initializer);
        var condition = this.Visitor().VisitNode(node.Condition);
        var incrementor = this.discardedValueVisitor.VisitNode(node.Incrementor);
        var saved = this.inIterationStatement;
        this.inIterationStatement = true;
        var body = this.EmitContext().VisitIterationBody(node.Statement, this.Visitor());
        this.inIterationStatement = saved;
        return this.Factory().UpdateForStatement(node, initializer, condition, incrementor, body);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1255:1.
    private Node visitExpressionStatement(ExpressionStatement node)
    {
        if (Ast.IsPrivateIdentifier(node.Expression) && this.shouldTransformPrivateElementsOrClassStaticBlocks)
        {
            return node.AsNode();
        }
        return this.Factory().UpdateExpressionStatement(node, this.discardedValueVisitor.VisitNode(node.Expression));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1269:1.
    private (Node, Node) createCopiableReceiverExpr(Node receiver)
    {
        Node readExpression = default;
        Node initializeExpression = default;
        var clone = receiver;
        if (!Ast.NodeIsSynthesized(receiver))
        {
            clone = receiver.Clone(this.Factory());
        }
        if (Transformers.IsSimpleInlineableExpression(receiver))
        {
            return (clone, null);
        }
        readExpression = this.Factory().NewTempVariable();
        this.EmitContext().AddVariableDeclaration(readExpression);
        initializeExpression = this.Factory().NewAssignmentExpression(readExpression, clone);
        return (readExpression, initializeExpression);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1283:1.
    private Node visitCallExpression(CallExpression node)
    {
        if ((Ast.IsPropertyAccessExpression(node.Expression) && Ast.IsPrivateIdentifier(node.Expression.AsPropertyAccessExpression().Name())) && (this.accessPrivateIdentifier(node.Expression.AsPropertyAccessExpression().Name()) != null))
        {
            var tuple8 = this.createCallBinding(node.Expression); var thisArg = tuple8.Item1; var target = tuple8.Item2;
            var visitedTarget = this.Visitor().VisitNode(target);
            var visitedThisArg = this.Visitor().VisitNode(thisArg);
            var visitedArgs = this.Visitor().VisitNodes(node.Arguments);
            var allArgs = Slice<Node>.Make(0, 1 + visitedArgs.Nodes.Length);
            allArgs = allArgs.Append(visitedThisArg);
            allArgs = allArgs.Append(visitedArgs.Nodes);
            if ((node.Flags & NodeFlags.OptionalChain) != 0)
            {
                return this.Factory().UpdateCallExpression(node,
                    this.Factory().NewPropertyAccessExpression(visitedTarget,
                        node.QuestionDotToken,
                        this.Factory().NewIdentifier("call"),
                        NodeFlags.OptionalChain),
                    null,
                    null,
                    this.Factory().NewNodeList(allArgs),
                    node.Flags);
            }
            return this.Factory().UpdateCallExpression(node,
                this.Factory().NewPropertyAccessExpression(visitedTarget,
                    null,
                    this.Factory().NewIdentifier("call"),
                    NodeFlags.None),
                null,
                null,
                this.Factory().NewNodeList(allArgs),
                node.Flags);
        }
        if ((((((this.shouldTransformSuperInStaticInitializers && (this.currentClassElement != null)) && Ast.IsSuperProperty(node.Expression)) && EsTransforms.IsStaticPropertyDeclarationOrClassStaticBlock(this.currentClassElement)) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null)) && (this.lexicalEnvironment.data.classConstructor != null))
        {
            var invocation = this.Factory().NewFunctionCallCall(this.Visitor().VisitNode(node.Expression),
                this.lexicalEnvironment.data.classConstructor,
                this.Visitor().VisitNodes(node.Arguments).Nodes);
            this.EmitContext().SetOriginal(invocation, node.AsNode());
            invocation.Loc = node.Loc;
            return invocation;
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1338:1.
    private Node visitTaggedTemplateExpression(TaggedTemplateExpression node)
    {
        if ((Ast.IsPropertyAccessExpression(node.Tag) && Ast.IsPrivateIdentifier(node.Tag.AsPropertyAccessExpression().Name())) && (this.accessPrivateIdentifier(node.Tag.AsPropertyAccessExpression().Name()) != null))
        {
            var tuple9 = this.createCallBinding(node.Tag); var thisArg = tuple9.Item1; var target = tuple9.Item2;
            var bindExpr = this.Factory().NewCallExpression(this.Factory().NewPropertyAccessExpression(this.Visitor().VisitNode(target),
                    null,
                    this.Factory().NewIdentifier("bind"),
                    NodeFlags.None),
                null,
                null,
                this.Factory().NewNodeList(Slice<Node>.FromArray([this.Visitor().VisitNode(thisArg)])),
                NodeFlags.None);
            return this.Factory().UpdateTaggedTemplateExpression(node, bindExpr, null, null, this.Visitor().VisitNode(node.Template), node.Flags);
        }
        if ((((((this.shouldTransformSuperInStaticInitializers && (this.currentClassElement != null)) && Ast.IsSuperProperty(node.Tag)) && EsTransforms.IsStaticPropertyDeclarationOrClassStaticBlock(this.currentClassElement)) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null)) && (this.lexicalEnvironment.data.classConstructor != null))
        {
            var invocation = this.Factory().NewFunctionBindCall(this.Visitor().VisitNode(node.Tag), this.lexicalEnvironment.data.classConstructor, default);
            this.EmitContext().SetOriginal(invocation, node.AsNode());
            invocation.Loc = node.Loc;
            return this.Factory().UpdateTaggedTemplateExpression(node, invocation, null, null, this.Visitor().VisitNode(node.Template), node.Flags);
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1386:1.
    private Node transformClassStaticBlockDeclaration(Node node)
    {
        if (this.shouldTransformPrivateElementsOrClassStaticBlocks)
        {
            if (EsTransforms.IsClassThisAssignmentBlock(this.EmitContext(), node))
            {
                var result = this.Visitor().VisitNode(node.AsClassStaticBlockDeclaration().Body.AsBlock().Statements.Nodes[0].Expression());
                if (Ast.IsAssignmentExpression(result, true))
                {
                    var binary = result.AsBinaryExpression();
                    if (binary.Left == binary.Right)
                    {
                        return null;
                    }
                }
                return result;
            }
            if (EsTransforms.IsClassNamedEvaluationHelperBlock(this.EmitContext(), node))
            {
                return this.Visitor().VisitNode(node.AsClassStaticBlockDeclaration().Body.AsBlock().Statements.Nodes[0].Expression());
            }
            this.EmitContext().StartVariableEnvironment();
            var statements = this.setCurrentClassElementAndVisitStatements(node, node.AsClassStaticBlockDeclaration().Body.AsBlock().Statements.Nodes);
            statements = this.EmitContext().EndAndMergeVariableEnvironment(statements);
            var iife = this.Factory().NewImmediatelyInvokedArrowFunction(statements);
            var arrowFunction = Ast.SkipParentheses(iife.Expression());
            this.EmitContext().SetOriginal(arrowFunction, node);
            this.EmitContext().AddEmitFlags(arrowFunction, EmitFlags.NoLexicalArguments);
            arrowFunction.AsArrowFunction().Body.AsBlock().Statements.Loc = node.AsClassStaticBlockDeclaration().Body.AsBlock().Statements.Loc;
            this.EmitContext().SetOriginal(iife, node);
            this.EmitContext().AssignSourceMapRange(iife, node);
            this.EmitContext().AddEmitFlags(arrowFunction, EmitFlags.NoLexicalThis);
            return iife;
        }
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1424:1.
    private Slice<Node> setCurrentClassElementAndVisitStatements(Node classElement, Slice<Node> statements)
    {
        var savedCurrentClassElement = this.currentClassElement;
        this.currentClassElement = classElement;
        var tuple10 = this.Visitor().VisitSlice(statements); var result = tuple10.Item1;
        this.currentClassElement = savedCurrentClassElement;
        return result;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1432:1.
    private bool isAnonymousClassNeedingAssignedNameWorker(Node node)
    {
        if (Ast.IsClassExpression(node) && (node.Name() == null))
        {
            var staticPropertiesOrClassStaticBlocks = this.getStaticPropertiesAndClassStaticBlock(node);
            if (Core.Some(staticPropertiesOrClassStaticBlocks, EmitContext(), static (n, context) => EsTransforms.IsClassNamedEvaluationHelperBlock(context, n)))
            {
                return false;
            }
            var hasTransformableStatics = (((this.shouldTransformPrivateElementsOrClassStaticBlocks || this.nodeHasTransformPrivateStaticElementsFlag(node))) && Core.Some(staticPropertiesOrClassStaticBlocks, this.shouldTransformInitializers, static (n, transformInitializers) => Ast.IsClassStaticBlockDeclaration(n) || Ast.IsPrivateIdentifierClassElementDeclaration(n) || transformInitializers && Ast.IsInitializedProperty(n)));
            return hasTransformableStatics;
        }
        return false;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1452:1.
    private Node visitBinaryExpression(BinaryExpression node, bool discarded)
    {
        if (Ast.IsDestructuringAssignment(node.AsNode()))
        {
            var savedPendingExpressions = this.pendingExpressions;
            this.pendingExpressions = default;
            var updated = this.Factory().UpdateBinaryExpression(node,
                null,
                this.assignmentTargetVisitor.VisitNode(node.Left),
                null,
                node.OperatorToken,
                this.Visitor().VisitNode(node.Right));
            Node result = default;
            if (GoRuntime.Len(this.pendingExpressions) > 0)
            {
                var exprs = this.pendingExpressions.Append(updated);
                result = this.Factory().InlineExpressions(exprs);
            }
            else
            {
                result = updated;
            }
            this.pendingExpressions = savedPendingExpressions;
            return result;
        }
        if (Ast.IsAssignmentExpression(node.AsNode(), false))
        {
            if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node.AsNode(), this.isAnonymousClassNeedingAssignedName))
            {
                node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node.AsNode(), false, default).AsBinaryExpression();
                Debug.Assert(((node.AsNode() != null) && Ast.IsAssignmentExpression(node.AsNode(), false)));
            }
            var left = Ast.SkipOuterExpressions(node.Left, (OuterExpressionKinds.PartiallyEmittedExpressions | OuterExpressionKinds.Parentheses));
            if (Ast.IsPropertyAccessExpression(left) && Ast.IsPrivateIdentifier(left.Name()))
            {
                var info = this.accessPrivateIdentifier(left.Name());
                if (info != null)
                {
                    var result2 = this.createPrivateIdentifierAssignment(info, left.Expression(), node.Right, node.OperatorToken.Kind);
                    this.EmitContext().SetOriginal(result2, node.AsNode());
                    result2.Loc = node.Loc;
                    return result2;
                }
            }
            else
            {
                if (((((this.shouldTransformSuperInStaticInitializers && (this.currentClassElement != null)) && Ast.IsSuperProperty(node.Left)) && EsTransforms.IsStaticPropertyDeclarationOrClassStaticBlock(this.currentClassElement)) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null))
                {
                    var data = this.lexicalEnvironment.data;
                    if ((data.facts & classFacts.ClassWasDecorated) != 0)
                    {
                        return this.Factory().UpdateBinaryExpression(node,
                            null,
                            this.visitInvalidSuperProperty(node.Left),
                            null,
                            node.OperatorToken,
                            this.Visitor().VisitNode(node.Right));
                    }
                    if ((data.classConstructor != null) && (data.superClassReference != null))
                    {
                        Node setterName = default;
                        if (Ast.IsElementAccessExpression(node.Left))
                        {
                            setterName = this.Visitor().VisitNode(node.Left.AsElementAccessExpression().ArgumentExpression);
                        }
                        else
                        {
                            if (Ast.IsPropertyAccessExpression(node.Left) && Ast.IsIdentifier(node.Left.AsPropertyAccessExpression().Name()))
                            {
                                setterName = this.Factory().NewStringLiteralFromNode(node.Left.AsPropertyAccessExpression().Name());
                            }
                        }
                        if (setterName != null)
                        {
                            var expression = this.Visitor().VisitNode(node.Right);
                            if (Ast.IsCompoundAssignment(node.OperatorToken.Kind))
                            {
                                var getterName = setterName;
                                if (!Transformers.IsSimpleInlineableExpression(setterName))
                                {
                                    getterName = this.Factory().NewTempVariable();
                                    this.EmitContext().AddVariableDeclaration(getterName);
                                    setterName = this.Factory().NewAssignmentExpression(getterName, setterName);
                                }
                                var superPropertyGet = this.Factory().NewReflectGetCall(data.superClassReference, getterName, data.classConstructor);
                                this.EmitContext().SetOriginal(superPropertyGet, node.Left);
                                superPropertyGet.Loc = node.Left.Loc;
                                expression = this.Factory().NewBinaryExpression(null,
                                    superPropertyGet,
                                    null,
                                    this.Factory().NewToken(Transformers.GetNonAssignmentOperatorForCompoundAssignment(node.OperatorToken.Kind)),
                                    expression);
                                expression.Loc = node.Loc;
                            }
                            Node temp = default;
                            if (!discarded)
                            {
                                temp = this.Factory().NewTempVariable();
                                this.EmitContext().AddVariableDeclaration(temp);
                            }
                            if (temp != null)
                            {
                                expression = this.Factory().NewAssignmentExpression(temp, expression);
                                expression.Loc = node.Loc;
                            }
                            expression = this.Factory().NewReflectSetCall(data.superClassReference, setterName, expression, data.classConstructor);
                            this.EmitContext().SetOriginal(expression, node.AsNode());
                            expression.Loc = node.Loc;
                            if (temp != null)
                            {
                                expression = this.Factory().NewCommaExpression(expression, temp);
                                expression.Loc = node.Loc;
                            }
                            return expression;
                        }
                    }
                }
            }
        }
        if ((node.OperatorToken.Kind == Kind.InKeyword) && Ast.IsPrivateIdentifier(node.Left))
        {
            return this.transformPrivateIdentifierInInExpression(node);
        }
        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1614:1.
    private Node visitParenthesizedExpression(ParenthesizedExpression node, bool discarded)
    {
        if (discarded)
        {
            var expression = this.discardedValueVisitor.VisitNode(node.Expression);
            return this.Factory().UpdateParenthesizedExpression(node, expression);
        }
        var expression2 = this.Visitor().VisitNode(node.Expression);
        return this.Factory().UpdateParenthesizedExpression(node, expression2);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1627:1.
    private Node createPrivateIdentifierAssignment(privateIdentifierInfo info, Node receiver, Node right, Kind @operator)
    {
        receiver = this.Visitor().VisitNode(receiver);
        right = this.Visitor().VisitNode(right);
        if (Ast.IsCompoundAssignment(@operator))
        {
            var tuple11 = this.createCopiableReceiverExpr(receiver); var readExpression = tuple11.Item1; var initializeExpression = tuple11.Item2;
            if (initializeExpression != null)
            {
                receiver = initializeExpression;
            }
            else
            {
                receiver = readExpression;
            }
            right = this.Factory().NewBinaryExpression(null,
                this.createPrivateIdentifierAccessHelper(info,
                    readExpression),
                null,
                this.Factory().NewToken(Transformers.GetNonAssignmentOperatorForCompoundAssignment(@operator)),
                right);
        }
        this.EmitContext().SetCommentRange(receiver, Core.NewTextRange(-1, receiver.End()));
        switch (info.kind)
        {
            case var kind when kind == PrivateIdentifierKind.Accessor:
            {
                return this.Factory().NewClassPrivateFieldSetHelper(receiver, info.brandCheckIdentifier, right, info.kind, info.setterName);
            }
            case var kind when kind == PrivateIdentifierKind.Method:
            {
                return this.Factory().NewClassPrivateFieldSetHelper(receiver, info.brandCheckIdentifier, right, info.kind, null);
            }
            case var kind when kind == PrivateIdentifierKind.Field:
            {
                Node f = default;
                if (info.isStatic)
                {
                    f = info.variableName;
                }
                return this.Factory().NewClassPrivateFieldSetHelper(receiver, info.brandCheckIdentifier, right, info.kind, f);
            }
            case var kind when kind == PrivateIdentifierKind.Untransformed:
            {
                Debug.Fail("Access helpers should not be created for untransformed private elements");
                return null;
            }
        }
        Debug.Fail("Unknown private element type");
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1686:1.
    private Slice<Node> getPrivateInstanceMethodsAndAccessors(Node node)
    {
        return Core.Filter(node.Members(), EsTransforms.IsNonStaticMethodOrAccessorWithPrivateName);
    }

    // memberContainsConstructorReference checks if a class member's body contains an identifier
    // that resolves to the class declaration. Replaces Strada's resolver.hasNodeCheckFlag(member,
    // NodeCheckFlags.ContainsConstructorReference) by walking the AST with the EmitResolver.
    // Only checks member bodies (not computed property names), since computed property names
    // are evaluated during class definition when the binding is still correct.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:1695:1.
    private bool memberContainsConstructorReference(Node member, Node classDecl)
    {
        var previousOriginal = constructorReferenceClassOriginal;
        var previousName = constructorReferenceClassName;
        constructorReferenceClassOriginal = EmitContext().MostOriginal(classDecl);
        constructorReferenceClassName = Ast.GetNameOfDeclaration(classDecl);
        try
        {
            if (Ast.IsClassStaticBlockDeclaration(member))
            {
                var body = member.AsClassStaticBlockDeclaration().Body;
                if ((body != null) && checkConstructorReference(body.AsNode()))
                {
                    return true;
                }
            }
            else
            {
                var body2 = member.Body();
                if ((body2 != null) && checkConstructorReference(body2))
                {
                    return true;
                }
            }
            if (Ast.IsPropertyDeclaration(member))
            {
                var init = member.Initializer();
                if ((init != null) && checkConstructorReference(init))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        {
            constructorReferenceClassOriginal = previousOriginal;
            constructorReferenceClassName = previousName;
        }
    }

    // classContainsConstructorReference checks if any member of a class contains
    // references to the class's own constructor. Replaces Strada's
    // resolver.hasNodeCheckFlag(node, NodeCheckFlags.ContainsConstructorReference).
    // Upstream tsc/internal/transformers/estransforms/classfields.go:1738:1.
    private bool classContainsConstructorReference(Node node)
    {
        {
            foreach (var member in GoRuntime.RangeValues(node.Members()))
            {
                if (this.memberContainsConstructorReference(member, node))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1747:1.
    private classFacts getClassFacts(Node node)
    {
        var facts = classFacts.None;
        var original = this.EmitContext().MostOriginal(node);
        if (Ast.IsClassLike(original) && Ast.ClassOrConstructorParameterIsDecorated(this.legacyDecorators, original))
        {
            facts |= classFacts.ClassWasDecorated;
        }
        if (this.shouldTransformPrivateElementsOrClassStaticBlocks && ((EsTransforms.ClassHasClassThisAssignment(this.EmitContext(), node) || EsTransforms.ClassHasExplicitlyAssignedName(this.EmitContext(), node))))
        {
            facts |= classFacts.NeedsClassConstructorReference;
        }
        bool containsPublicInstanceFields = default;
        bool containsInitializedPublicInstanceFields = default;
        bool containsInstancePrivateElements = default;
        bool containsInstanceAutoAccessors = default;
        {
            foreach (var member in GoRuntime.RangeValues(node.Members()))
            {
                if (Ast.IsStatic(member))
                {
                    if (((member.Name() != null) && ((Ast.IsPrivateIdentifier(member.Name()) || Ast.IsAutoAccessorPropertyDeclaration(member)))) && this.shouldTransformPrivateElementsOrClassStaticBlocks)
                    {
                        facts |= classFacts.NeedsClassConstructorReference;
                    }
                    else
                    {
                        if (((Ast.IsAutoAccessorPropertyDeclaration(member) && this.shouldTransformAutoAccessors) && (node.Name() == null)) && (this.EmitContext().ClassThis(node) == null))
                        {
                            facts |= classFacts.NeedsClassConstructorReference;
                        }
                    }
                    if (Ast.IsPropertyDeclaration(member) || Ast.IsClassStaticBlockDeclaration(member))
                    {
                        if (this.shouldTransformThisInStaticInitializers && (((uint)member.SubtreeFacts() & SubtreeFactValues.ContainsLexicalThis) != 0))
                        {
                            facts |= classFacts.NeedsSubstitutionForThisInClassStaticField;
                            if ((facts & classFacts.ClassWasDecorated) == 0)
                            {
                                facts |= classFacts.NeedsClassConstructorReference;
                            }
                        }
                        if (this.shouldTransformSuperInStaticInitializers && (((uint)member.SubtreeFacts() & SubtreeFactValues.ContainsLexicalSuper) != 0))
                        {
                            if ((facts & classFacts.ClassWasDecorated) == 0)
                            {
                                facts |= (classFacts.NeedsClassConstructorReference | classFacts.NeedsClassSuperReference);
                            }
                        }
                    }
                }
                else
                {
                    if (!Ast.HasAbstractModifier(this.EmitContext().MostOriginal(member)))
                    {
                        if (Ast.IsAutoAccessorPropertyDeclaration(member))
                        {
                            containsInstanceAutoAccessors = true;
                            containsInstancePrivateElements = (containsInstancePrivateElements || Ast.IsPrivateIdentifierClassElementDeclaration(member));
                        }
                        else
                        {
                            if (Ast.IsPrivateIdentifierClassElementDeclaration(member))
                            {
                                containsInstancePrivateElements = true;
                                if (this.memberContainsConstructorReference(member, node))
                                {
                                    facts |= classFacts.NeedsClassConstructorReference;
                                }
                            }
                            else
                            {
                                if (Ast.IsPropertyDeclaration(member))
                                {
                                    containsPublicInstanceFields = true;
                                    containsInitializedPublicInstanceFields = (containsInitializedPublicInstanceFields || (member.Initializer() != null));
                                }
                            }
                        }
                    }
                }
            }
        }
        var willHoistInitializersToConstructor = (((((this.shouldTransformInitializersUsingDefine && containsPublicInstanceFields)) || ((this.shouldTransformInitializersUsingSet && containsInitializedPublicInstanceFields))) || ((this.shouldTransformPrivateElementsOrClassStaticBlocks && containsInstancePrivateElements))) || (((this.shouldTransformPrivateElementsOrClassStaticBlocks && containsInstanceAutoAccessors) && this.shouldTransformAutoAccessors)));
        if (willHoistInitializersToConstructor)
        {
            facts |= classFacts.WillHoistInitializersToConstructor;
        }
        return facts;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1815:1.
    private Node visitExpressionWithTypeArgumentsInHeritageClause(ExpressionWithTypeArguments node)
    {
        var facts = classFacts.None;
        if ((this.lexicalEnvironment != null) && (this.lexicalEnvironment.data != null))
        {
            facts = this.lexicalEnvironment.data.facts;
        }
        if ((facts & classFacts.NeedsClassSuperReference) != 0)
        {
            var temp = this.Factory().NewTempVariableEx(new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.ReservedInNestedScopes });
            this.EmitContext().AddVariableDeclaration(temp);
            this.getClassLexicalEnvironment().superClassReference = temp;
            return this.Factory().UpdateExpressionWithTypeArguments(node, this.Factory().NewAssignmentExpression(temp, this.Visitor().VisitNode(node.Expression)), null);
        }
        return this.heritageClauseVisitor.VisitEachChild(node.AsNode());
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1835:1.
    private Node visitInNewClassLexicalEnvironment(Node node, Func<ClassFieldsTransformer, Node, classFacts, Node> visitor)
    {
        var savedCurrentClassContainer = this.currentClassContainer;
        var savedPendingExpressions = this.pendingExpressions;
        var savedLexicalEnvironment = this.lexicalEnvironment;
        this.currentClassContainer = node;
        this.pendingExpressions = default;
        this.startClassLexicalEnvironment();
        var original = this.EmitContext().MostOriginal(node);
        this.enclosingClassDeclarations.Add(original);
        if (this.shouldTransformPrivateElementsOrClassStaticBlocks || this.nodeHasTransformPrivateStaticElementsFlag(node))
        {
            var name = Ast.GetNameOfDeclaration(node);
            if ((name != null) && Ast.IsIdentifier(name))
            {
                this.getPrivateIdentifierEnvironment().data.className = name;
            }
            else
            {
                {
                    var assignedName = this.EmitContext().AssignedName(node);
                    if (assignedName != null)
                    {
                        if (Ast.IsStringLiteral(assignedName))
                        {
                            {
                                var textSourceNode = this.EmitContext().TextSource(assignedName);
                                if ((textSourceNode != null) && Ast.IsIdentifier(textSourceNode))
                                {
                                    this.getPrivateIdentifierEnvironment().data.className = textSourceNode;
                                }
                                else
                                {
                                    if (TypeScript.Scanner.Scanner.IsIdentifierTextByteString(assignedName.TextValue(), LanguageVariant.Standard))
                                    {
                                        var prefixName = this.Factory().NewIdentifierWithTextValue(assignedName.TextValue());
                                        this.getPrivateIdentifierEnvironment().data.className = prefixName;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        if (this.shouldTransformPrivateElementsOrClassStaticBlocks)
        {
            var privateInstanceMethodsAndAccessors = this.getPrivateInstanceMethodsAndAccessors(node);
            if (GoRuntime.Len(privateInstanceMethodsAndAccessors) > 0)
            {
                this.getPrivateIdentifierEnvironment().data.weakSetName = this.createHoistedVariableForClass(ByteStringValue.FromString("instances"), privateInstanceMethodsAndAccessors[0].Name(), "");
            }
        }
        var facts2 = this.getClassFacts(node);
        if (facts2 != classFacts.None)
        {
            this.getClassLexicalEnvironment().facts = facts2;
        }
        var result = visitor(this, node, facts2);
        this.enclosingClassDeclarations.Remove(original);
        this.endClassLexicalEnvironment();
        Debug.Assert((this.lexicalEnvironment == savedLexicalEnvironment));
        this.currentClassContainer = savedCurrentClassContainer;
        this.pendingExpressions = savedPendingExpressions;
        this.lexicalEnvironment = savedLexicalEnvironment;
        return result;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1889:1.
    private Node visitClassDeclaration(ClassDeclaration node)
    {
        return this.visitInNewClassLexicalEnvironment(node.AsNode(), static (tx, value, facts) => tx.visitClassDeclarationInNewClassLexicalEnvironment(value, facts));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:1893:1.
    private Node visitClassDeclarationInNewClassLexicalEnvironment(Node node, classFacts facts)
    {
        var classDecl = node.AsClassDeclaration();
        Node pendingClassReferenceAssignment = default;
        if ((facts & classFacts.NeedsClassConstructorReference) != 0)
        {
            if (this.shouldTransformPrivateElementsOrClassStaticBlocks && (this.EmitContext().ClassThis(node) != null))
            {
                var classThis = this.EmitContext().ClassThis(node);
                this.getClassLexicalEnvironment().classConstructor = classThis;
                pendingClassReferenceAssignment = this.Factory().NewAssignmentExpression(classThis, this.Factory().GetLocalName(node));
            }
            else
            {
                var temp = this.Factory().NewTempVariableEx(new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.ReservedInNestedScopes });
                this.EmitContext().AddVariableDeclaration(temp);
                this.getClassLexicalEnvironment().classConstructor = temp.Clone(this.Factory());
                pendingClassReferenceAssignment = this.Factory().NewAssignmentExpression(temp, this.Factory().GetLocalName(node));
            }
        }
        if (this.EmitContext().ClassThis(node) != null)
        {
            this.getClassLexicalEnvironment().classThis = this.EmitContext().ClassThis(node);
        }
        var isClassWithConstructorReference = this.classContainsConstructorReference(node);
        var alias = this.getClassLexicalEnvironment().classConstructor;
        if (isClassWithConstructorReference && (alias != null))
        {
            this.classAliases[this.EmitContext().MostOriginal(node)] = alias;
        }
        var modifiers = this.modifierVisitor.VisitModifiers(classDecl.Modifiers());
        var heritageClauses = this.heritageClauseVisitor.VisitNodes(classDecl.HeritageClauses);
        var tuple14 = this.transformClassMembers(node); var members = tuple14.Item1; var membersPrologue = tuple14.Item2;
        Slice<Node> statements = default;
        if (pendingClassReferenceAssignment != null)
        {
            this.pendingExpressions = Slice<Node>.FromArray([pendingClassReferenceAssignment]).Append(this.pendingExpressions);
        }
        if (GoRuntime.Len(this.pendingExpressions) > 0)
        {
            statements = statements.Append(this.Factory().NewExpressionStatement(this.Factory().InlineExpressions(this.pendingExpressions)));
        }
        var name = classDecl.Name();
        if (this.shouldTransformInitializersUsingSet || this.shouldTransformPrivateElementsOrClassStaticBlocks)
        {
            var staticProperties = this.getStaticPropertiesAndClassStaticBlock(node);
            if (GoRuntime.Len(staticProperties) > 0)
            {
                if (name == null)
                {
                    name = this.Factory().NewGeneratedNameForNode(node);
                }
                statements = this.addPropertyOrClassStaticBlockStatements(statements, staticProperties, this.Factory().GetLocalName(node));
            }
        }
        var isExport = Ast.HasSyntacticModifier(node, ModifierFlags.Export);
        var isDefault = Ast.HasSyntacticModifier(node, ModifierFlags.Default);
        if (((GoRuntime.Len(statements) > 0) && isExport) && isDefault)
        {
            modifiers = Transformers.ExtractModifiers(this.EmitContext(), modifiers, ~ModifierFlags.ExportDefault);
            var exportAssignment = this.Factory().NewExportAssignment(null, false, null, this.Factory().GetLocalName(node));
            statements = statements.Append(exportAssignment);
        }
        var updatedClass = this.Factory().UpdateClassDeclaration(classDecl, modifiers, name, null, heritageClauses, members);
        var result = Slice<Node>.Make(0, 1 + statements.Length + 1);
        if (membersPrologue != null)
        {
            result = result.Append(this.Factory().NewExpressionStatement(membersPrologue));
        }
        result = result.Append(updatedClass);
        result = result.Append(statements);
        return this.Factory().NewSyntaxList(result);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2003:1.
    private Node visitClassExpression(ClassExpression node)
    {
        return this.visitInNewClassLexicalEnvironment(node.AsNode(), static (tx, value, facts) => tx.visitClassExpressionInNewClassLexicalEnvironment(value, facts));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2007:1.
    private Node visitClassExpressionInNewClassLexicalEnvironment(Node node, classFacts facts)
    {
        var classExpr = node.AsClassExpression();
        var isDecoratedClassDeclaration = ((facts & classFacts.ClassWasDecorated) != 0);
        if (this.EmitContext().ClassThis(node) != null)
        {
            this.getClassLexicalEnvironment().classThis = this.EmitContext().ClassThis(node);
        }
        Node temp = default;
        if ((facts & classFacts.NeedsClassConstructorReference) != 0)
        {
            if (((this.shouldTransformPrivateElementsOrClassStaticBlocks || this.nodeHasTransformPrivateStaticElementsFlag(node))) && (this.EmitContext().ClassThis(node) != null))
            {
                var classThis = this.EmitContext().ClassThis(node);
                this.getClassLexicalEnvironment().classConstructor = classThis;
                temp = classThis;
            }
            else
            {
                temp = this.Factory().NewTempVariableEx(new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.ReservedInNestedScopes });
                if (this.classExpressionNeedsBlockScopedTemp())
                {
                    this.EmitContext().AddLexicalDeclaration(temp);
                }
                else
                {
                    this.EmitContext().AddVariableDeclaration(temp);
                }
                this.getClassLexicalEnvironment().classConstructor = temp.Clone(this.Factory());
            }
        }
        var staticPropertiesOrClassStaticBlocks = this.getStaticPropertiesAndClassStaticBlock(node);
        var isClassWithConstructorReference = false;
        var hasTransformableStatics = false;
        var deferTempDeclaration = false;
        if (!isDecoratedClassDeclaration)
        {
            isClassWithConstructorReference = this.classContainsConstructorReference(node);
            hasTransformableStatics = (((this.shouldTransformPrivateElementsOrClassStaticBlocks || this.nodeHasTransformPrivateStaticElementsFlag(node))) && Core.Some(staticPropertiesOrClassStaticBlocks, this.shouldTransformInitializers, static (n, transformInitializers) => Ast.IsClassStaticBlockDeclaration(n) || Ast.IsPrivateIdentifierClassElementDeclaration(n) || transformInitializers && Ast.IsInitializedProperty(n)));
            var willHavePrivatePendingExpressions = (this.shouldTransformPrivateElementsOrClassStaticBlocks && Core.Some(node.Members(), this, static (n, tx) => Ast.IsPrivateIdentifierClassElementDeclaration(n) && !Ast.HasStaticModifier(n) && tx.shouldTransformClassElementToWeakMap(n)));
            var willNeedTempWrapper = (hasTransformableStatics || willHavePrivatePendingExpressions);
            if ((isClassWithConstructorReference && willNeedTempWrapper) && (this.getClassLexicalEnvironment().classConstructor == null))
            {
                temp = this.Factory().NewTempVariableEx(new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.ReservedInNestedScopes });
                deferTempDeclaration = true;
                this.getClassLexicalEnvironment().classConstructor = temp.Clone(this.Factory());
            }
            {
                var alias = this.getClassLexicalEnvironment().classConstructor;
                if ((isClassWithConstructorReference && willNeedTempWrapper) && (alias != null))
                {
                    this.classAliases[this.EmitContext().MostOriginal(node)] = alias;
                }
            }
        }
        var modifiers = this.modifierVisitor.VisitModifiers(classExpr.Modifiers());
        var heritageClauses = this.heritageClauseVisitor.VisitNodes(classExpr.HeritageClauses);
        var tuple15 = this.transformClassMembers(node); var members = tuple15.Item1; var membersPrologue = tuple15.Item2;
        if (deferTempDeclaration)
        {
            if (this.classExpressionNeedsBlockScopedTemp())
            {
                this.EmitContext().AddLexicalDeclaration(temp);
            }
            else
            {
                this.EmitContext().AddVariableDeclaration(temp);
            }
        }
        var classExpression = this.Factory().UpdateClassExpression(classExpr, modifiers, classExpr.Name(), null, heritageClauses, members);
        Slice<Node> expressions = default;
        if (membersPrologue != null)
        {
            expressions = expressions.Append(membersPrologue);
        }
        if (!isDecoratedClassDeclaration)
        {
            if (hasTransformableStatics || (GoRuntime.Len(this.pendingExpressions) > 0))
            {
                if (temp == null)
                {
                    temp = this.Factory().NewTempVariableEx(new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.ReservedInNestedScopes });
                    if (this.classExpressionNeedsBlockScopedTemp())
                    {
                        this.EmitContext().AddLexicalDeclaration(temp);
                    }
                    else
                    {
                        this.EmitContext().AddVariableDeclaration(temp);
                    }
                    this.getClassLexicalEnvironment().classConstructor = temp.Clone(this.Factory());
                    if (isClassWithConstructorReference)
                    {
                        this.classAliases[this.EmitContext().MostOriginal(node)] = this.getClassLexicalEnvironment().classConstructor;
                    }
                }
                expressions = expressions.Append(this.Factory().NewAssignmentExpression(temp, classExpression));
                expressions = expressions.Append(this.pendingExpressions);
                expressions = expressions.Append(this.generateInitializedPropertyExpressionsOrClassStaticBlock(staticPropertiesOrClassStaticBlocks, temp));
                expressions = expressions.Append(temp.Clone(this.Factory()));
            }
            else
            {
                expressions = expressions.Append(classExpression);
            }
        }
        else
        {
            if (GoRuntime.Len(this.pendingExpressions) > 0)
            {
                {
                    foreach (var expr in GoRuntime.RangeValues(this.pendingExpressions))
                    {
                        this.pendingStatements = this.pendingStatements.Append(this.Factory().NewExpressionStatement(expr));
                    }
                }
            }
            if (GoRuntime.Len(staticPropertiesOrClassStaticBlocks) > 0)
            {
                var classThisOrName = this.EmitContext().ClassThis(node);
                if (classThisOrName == null)
                {
                    classThisOrName = this.Factory().GetLocalName(node);
                }
                this.pendingStatements = this.addPropertyOrClassStaticBlockStatements(this.pendingStatements, staticPropertiesOrClassStaticBlocks, classThisOrName);
            }
            if (temp != null)
            {
                expressions = expressions.Append(this.Factory().NewAssignmentExpression(temp, classExpression));
            }
            else
            {
                if (this.shouldTransformPrivateElementsOrClassStaticBlocks && (this.EmitContext().ClassThis(node) != null))
                {
                    expressions = expressions.Append(this.Factory().NewAssignmentExpression(this.EmitContext().ClassThis(node), classExpression));
                }
                else
                {
                    expressions = expressions.Append(classExpression);
                }
            }
        }
        if (GoRuntime.Len(expressions) > 1)
        {
            this.EmitContext().AddEmitFlags(classExpression, EmitFlags.Indented);
            {
                foreach (var range17 in GoRuntime.Range(expressions))
                {
                    var _2 = range17.Key;
                    var expr2 = range17.Value;
                    this.EmitContext().AddEmitFlags(expr2, EmitFlags.StartOnNewLine);
                }
            }
        }
        return this.Factory().InlineExpressions(expressions);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2181:1.
    private Node visitClassStaticBlockDeclaration(Node node)
    {
        if (!this.shouldTransformPrivateElementsOrClassStaticBlocks)
        {
            return this.Visitor().VisitEachChild(node);
        }
        return null;
    }

    // visitThisExpression replaces Strada's substituteThisExpression / onSubstituteNode.
    // Strada substitutes `this` at emit time; we do it eagerly during transformation.
    //
    // The Strada noSubstitution set (ensureDynamicThisIfNeeded) is not needed because
    // transformAutoAccessor() passes the receiver directly rather than emitting `this`.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:2194:1.
    private Node visitThisExpression(Node node)
    {
        if (((this.insideComputedPropertyName && this.shouldTransformThisInStaticInitializers) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null))
        {
            if (((this.lexicalEnvironment.data.facts & classFacts.ClassWasDecorated) == 0) || this.legacyDecorators)
            {
                {
                    var classThis = this.tryGetClassThisNoContainer();
                    if (classThis != null)
                    {
                        return classThis;
                    }
                }
            }
        }
        if ((((this.shouldTransformThisInStaticInitializers && (this.currentClassElement != null)) && ((Ast.IsClassStaticBlockDeclaration(this.currentClassElement) || ((Ast.IsPropertyDeclaration(this.currentClassElement) && Ast.HasStaticModifier(this.currentClassElement)))))) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null))
        {
            {
                var classThis2 = this.tryGetClassThisNoContainer();
                if (classThis2 != null)
                {
                    return classThis2;
                }
            }
            if (((this.lexicalEnvironment.data.facts & classFacts.ClassWasDecorated) != 0) && this.legacyDecorators)
            {
                return this.Factory().NewParenthesizedExpression(this.Factory().NewVoidZeroExpression());
            }
        }
        return node;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2223:1.
    private (NodeList, Node) transformClassMembers(Node node)
    {
        NodeList members = default;
        Node prologue = default;
        var shouldTransformPrivateStaticElementsInClass = ((this.EmitContext().EmitFlags(node) & EmitFlags.TransformPrivateStaticElements) != 0);
        if (this.shouldTransformPrivateElementsOrClassStaticBlocks || this.shouldTransformPrivateStaticElementsInFile)
        {
            {
                foreach (var member in GoRuntime.RangeValues(node.Members()))
                {
                    if (Ast.IsPrivateIdentifierClassElementDeclaration(member))
                    {
                        if (this.shouldTransformClassElementToWeakMap(member))
                        {
                            this.addPrivateIdentifierToEnvironment(member);
                        }
                        else
                        {
                            var env = this.getPrivateIdentifierEnvironment();
                            this.setPrivateIdentifier(env, member.Name(), new privateIdentifierInfo { kind = PrivateIdentifierKind.Untransformed });
                        }
                    }
                }
            }
            if (this.shouldTransformPrivateElementsOrClassStaticBlocks)
            {
                if (GoRuntime.Len(this.getPrivateInstanceMethodsAndAccessors(node)) > 0)
                {
                    this.createBrandCheckWeakSetForPrivateMethods();
                }
            }
            if (this.shouldTransformAutoAccessorsInCurrentClass())
            {
                {
                    foreach (var range19 in GoRuntime.Range(node.Members()))
                    {
                        var _2 = range19.Key;
                        var member2 = range19.Value;
                        if (Ast.IsAutoAccessorPropertyDeclaration(member2))
                        {
                            var storageName = this.Factory().NewGeneratedPrivateNameForNodeEx(member2.Name(), new AutoGenerateOptions { Suffix = "_accessor_storage" });
                            if (this.shouldTransformPrivateElementsOrClassStaticBlocks || (shouldTransformPrivateStaticElementsInClass && Ast.HasStaticModifier(member2)))
                            {
                                this.addPrivateIdentifierPropertyDeclarationToEnvironment(member2, storageName);
                            }
                            else
                            {
                                var env2 = this.getPrivateIdentifierEnvironment();
                                {
                                    var tuple20 = this.getPrivateIdentifier(env2, storageName); var ok = tuple20.Item2;
                                    if (!ok)
                                    {
                                        this.setPrivateIdentifier(env2, storageName, new privateIdentifierInfo { kind = PrivateIdentifierKind.Untransformed });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        members = this.classElementVisitor.VisitNodes(node.MemberList());
        Node syntheticConstructor = default;
        if (!Core.Some(members.Nodes, Ast.IsConstructorDeclaration))
        {
            syntheticConstructor = this.transformConstructor(null, node);
        }
        Node syntheticStaticBlock = default;
        if (!this.shouldTransformPrivateElementsOrClassStaticBlocks && (GoRuntime.Len(this.pendingExpressions) > 0))
        {
            var statement = this.Factory().NewExpressionStatement(this.Factory().InlineExpressions(this.pendingExpressions));
            if (((uint)statement.SubtreeFacts() & SubtreeFactValues.ContainsLexicalThisOrSuper) != 0)
            {
                var temp = this.Factory().NewTempVariable();
                this.EmitContext().AddVariableDeclaration(temp);
                var arrow = this.Factory().NewArrowFunction(null,
                    null,
                    this.Factory().NewNodeList(default(Slice<Node>)),
                    null,
                    null,
                    this.Factory().NewToken(Kind.EqualsGreaterThanToken),
                    this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([statement])),
                        false));
                prologue = this.Factory().NewAssignmentExpression(temp, arrow);
                statement = this.Factory().NewExpressionStatement(this.Factory().NewCallExpression(temp, null, null, this.Factory().NewNodeList(default(Slice<Node>)), NodeFlags.None));
            }
            var block = this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([statement])), false);
            syntheticStaticBlock = this.Factory().NewClassStaticBlockDeclaration(null, block);
            this.pendingExpressions = default;
        }
        if ((syntheticConstructor != null) || (syntheticStaticBlock != null))
        {
            var membersArray = Slice<Node>.Make(0, members.Nodes.Length + 2);
            var classThisIdx = Core.FindIndex(members.Nodes, EmitContext(), static (n, context) => EsTransforms.IsClassThisAssignmentBlock(context, n));
            var namedEvalIdx = Core.FindIndex(members.Nodes, EmitContext(), static (n, context) => EsTransforms.IsClassNamedEvaluationHelperBlock(context, n));
            if (classThisIdx >= 0)
            {
                membersArray = membersArray.Append(members.Nodes[classThisIdx]);
            }
            if (namedEvalIdx >= 0)
            {
                membersArray = membersArray.Append(members.Nodes[namedEvalIdx]);
            }
            if (syntheticConstructor != null)
            {
                membersArray = membersArray.Append(syntheticConstructor);
            }
            if (syntheticStaticBlock != null)
            {
                membersArray = membersArray.Append(syntheticStaticBlock);
            }
            {
                foreach (var range21 in GoRuntime.Range(members.Nodes))
                {
                    var i = range21.Key;
                    var member3 = range21.Value;
                    if ((i != classThisIdx) && (i != namedEvalIdx))
                    {
                        membersArray = membersArray.Append(member3);
                    }
                }
            }
            members = this.Factory().NewNodeList(membersArray);
            members.Loc = node.MemberList().Loc;
        }
        return (members, prologue);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2348:1.
    private void createBrandCheckWeakSetForPrivateMethods()
    {
        var env = this.getPrivateIdentifierEnvironment();
        var weakSetName = env.data.weakSetName;
        Debug.Assert((weakSetName != null), "weakSetName should be set in private identifier environment");
        this.addPendingExpressions(this.Factory().NewAssignmentExpression(weakSetName,
                this.Factory().NewNewExpression(this.Factory().NewIdentifier("WeakSet"),
                    null,
                    this.Factory().NewNodeList(default(Slice<Node>)))));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2365:1.
    private Node transformConstructor(ConstructorDeclaration constructor, Node container)
    {
        if (((this.lexicalEnvironment == null) || (this.lexicalEnvironment.data == null)) || ((this.lexicalEnvironment.data.facts & classFacts.WillHoistInitializersToConstructor) == 0))
        {
            if (constructor != null)
            {
                return this.Visitor().VisitEachChild(constructor.AsNode());
            }
            return null;
        }
        var extendsClauseElement = Ast.GetClassExtendsHeritageElement(container);
        var isDerivedClass = ((extendsClauseElement != null) && (Ast.SkipOuterExpressions(extendsClauseElement.Expression(), OuterExpressionKinds.All).Kind != Kind.NullKeyword));
        NodeList parameters = default;
        if (constructor != null)
        {
            parameters = this.Visitor().VisitNodes(constructor.Parameters);
        }
        var body = this.transformConstructorBody(container, constructor, isDerivedClass);
        if (body == null)
        {
            if (constructor != null)
            {
                return this.Visitor().VisitEachChild(constructor.AsNode());
            }
            return null;
        }
        if (constructor != null)
        {
            Debug.Assert((parameters != null));
            return this.Factory().UpdateConstructorDeclaration(constructor, null, null, parameters, null, null, body);
        }
        if (parameters == null)
        {
            parameters = this.Factory().NewNodeList(default(Slice<Node>));
        }
        var result = this.Factory().NewConstructorDeclaration(null, null, parameters, null, null, body);
        result.Loc = container.Loc;
        return result;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2424:1.
    private Slice<Node> transformConstructorBodyWorker(Slice<Node> statementsOut, Slice<Node> statementsIn, int statementOffset, Slice<int> superPath, int superPathDepth, Slice<Node> initializerStatements, ConstructorDeclaration constructor)
    {
        var superStatementIndex = superPath[superPathDepth];
        var superStatement = statementsIn[superStatementIndex];
        var tuple22 = this.Visitor().VisitSlice(statementsIn.Slice2(statementOffset, superStatementIndex)); var visited = tuple22.Item1;
        statementsOut = statementsOut.Append(visited);
        statementOffset = (superStatementIndex + 1);
        if (Ast.IsTryStatement(superStatement))
        {
            var tryBlock = superStatement.AsTryStatement().TryBlock.AsBlock();
            var tryBlockStatements = this.transformConstructorBodyWorker(default, tryBlock.Statements.Nodes, 0, superPath, (superPathDepth + 1), initializerStatements, constructor);
            var tryStatementList = this.Factory().NewNodeList(tryBlockStatements);
            tryStatementList.Loc = tryBlock.Statements.Loc;
            var catchClause = this.Visitor().VisitNode(superStatement.AsTryStatement().CatchClause);
            var finallyBlock = this.Visitor().VisitNode(superStatement.AsTryStatement().FinallyBlock);
            var updated = this.Factory().UpdateTryStatement(superStatement.AsTryStatement(),
                this.Factory().UpdateBlock(tryBlock,
                    tryStatementList,
                    tryBlock.MultiLine),
                catchClause,
                finallyBlock);
            statementsOut = statementsOut.Append(updated);
        }
        else
        {
            var tuple23 = this.Visitor().VisitSlice(statementsIn.Slice2(superStatementIndex, (superStatementIndex + 1))); var visitedSuper = tuple23.Item1;
            statementsOut = statementsOut.Append(visitedSuper);
            while (statementOffset < GoRuntime.Len(statementsIn))
            {
                var stmt = statementsIn[statementOffset];
                var orig = this.EmitContext().MostOriginal(stmt);
                if (Ast.IsParameterPropertyDeclaration(orig, constructor.AsNode()))
                {
                    statementOffset++;
                }
                else
                {
                    break;
                }
            }
            statementsOut = statementsOut.Append(initializerStatements);
        }
        var tuple24 = this.Visitor().VisitSlice(statementsIn.Slice2(statementOffset, statementsIn.Length)); var visited2 = tuple24.Item1;
        statementsOut = statementsOut.Append(visited2);
        return statementsOut;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2503:1.
    private Node transformConstructorBody(Node container, ConstructorDeclaration constructor, bool isDerivedClass)
    {
        var instanceProperties = this.getProperties(container, false, false);
        var properties = instanceProperties;
        if (!this.compilerOptions.GetUseDefineForClassFields())
        {
            properties = Core.Filter(properties, static prop => prop.Initializer() != null || Ast.IsPrivateIdentifier(prop.Name()) || Ast.HasAccessorModifier(prop));
        }
        var privateMethodsAndAccessors = this.getPrivateInstanceMethodsAndAccessors(container);
        var needsConstructorBody = ((GoRuntime.Len(properties) > 0) || (GoRuntime.Len(privateMethodsAndAccessors) > 0));
        if ((constructor == null) && !needsConstructorBody)
        {
            return this.EmitContext().VisitFunctionBody(null, this.Visitor());
        }
        this.EmitContext().StartVariableEnvironment();
        var needsSyntheticConstructor = ((constructor == null) && isDerivedClass);
        Slice<Node> statements = default;
        Slice<Node> initializerStatements = default;
        var receiver = this.Factory().NewThisExpression();
        initializerStatements = this.addInstanceMethodStatements(initializerStatements, privateMethodsAndAccessors, receiver);
        if (constructor != null)
        {
            var parameterProperties = Core.Filter(instanceProperties, (context: EmitContext(), constructor: constructor.AsNode()), static (prop, state) => Ast.IsParameterPropertyDeclaration(state.context.MostOriginal(prop), state.constructor));
            var nonParameterProperties = Core.Filter(properties, (context: EmitContext(), constructor: constructor.AsNode()), static (prop, state) => !Ast.IsParameterPropertyDeclaration(state.context.MostOriginal(prop), state.constructor));
            initializerStatements = this.addPropertyOrClassStaticBlockStatements(initializerStatements, parameterProperties, receiver);
            initializerStatements = this.addPropertyOrClassStaticBlockStatements(initializerStatements, nonParameterProperties, receiver);
        }
        else
        {
            initializerStatements = this.addPropertyOrClassStaticBlockStatements(initializerStatements, properties, receiver);
        }
        if ((constructor != null) && (constructor.Body != null))
        {
            var body = constructor.Body.AsBlock();
            {
                foreach (var stmt in GoRuntime.RangeValues(body.Statements.Nodes))
                {
                    if (Ast.IsPrologueDirective(stmt))
                    {
                        statements = statements.Append(stmt);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            var statementOffset = GoRuntime.Len(statements);
            var superPath = Transformers.FindSuperStatementIndexPath(body.Statements.Nodes, statementOffset);
            if (GoRuntime.Len(superPath) > 0)
            {
                statements = this.transformConstructorBodyWorker(statements, body.Statements.Nodes, statementOffset, superPath, 0, initializerStatements, constructor);
            }
            else
            {
                while (statementOffset < GoRuntime.Len(body.Statements.Nodes))
                {
                    var stmt2 = body.Statements.Nodes[statementOffset];
                    var orig = this.EmitContext().MostOriginal(stmt2);
                    if (Ast.IsParameterPropertyDeclaration(orig, constructor.AsNode()))
                    {
                        statementOffset++;
                    }
                    else
                    {
                        break;
                    }
                }
                statements = statements.Append(initializerStatements);
                var tuple26 = this.Visitor().VisitSlice(body.Statements.Nodes.Slice2(statementOffset, body.Statements.Nodes.Length)); var visited = tuple26.Item1;
                statements = statements.Append(visited);
            }
        }
        else
        {
            if (needsSyntheticConstructor)
            {
                var superCall = this.Factory().NewExpressionStatement(this.Factory().NewCallExpression(this.Factory().NewKeywordExpression(Kind.SuperKeyword),
                        null,
                        null,
                        this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewSpreadElement(this.Factory().NewIdentifier("arguments"))])),
                        NodeFlags.None));
                statements = statements.Append(superCall);
            }
            statements = statements.Append(initializerStatements);
        }
        statements = this.EmitContext().EndAndMergeVariableEnvironment(statements);
        if ((GoRuntime.Len(statements) == 0) && (constructor == null))
        {
            return null;
        }
        bool multiLine = default;
        if (((constructor != null) && (constructor.Body != null)) && (GoRuntime.Len(constructor.Body.AsBlock().Statements.Nodes) >= GoRuntime.Len(statements)))
        {
            multiLine = constructor.Body.AsBlock().MultiLine;
        }
        else
        {
            multiLine = (GoRuntime.Len(statements) > 0);
        }
        var statementList = this.Factory().NewNodeList(statements);
        if ((constructor != null) && (constructor.Body != null))
        {
            statementList.Loc = constructor.Body.AsBlock().Statements.Loc;
        }
        else
        {
            statementList.Loc = Core.NewTextRange(container.MemberList().Loc.Pos(), container.MemberList().Loc.End());
        }
        var block = this.Factory().NewBlock(statementList, multiLine);
        if ((constructor != null) && (constructor.Body != null))
        {
            block.Loc = constructor.Body.Loc;
        }
        return block;
    }

    // addPropertyOrClassStaticBlockStatements generates assignment statements for property initializers.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:2637:1.
    private Slice<Node> addPropertyOrClassStaticBlockStatements(Slice<Node> statements, Slice<Node> properties, Node receiver)
    {
        {
            foreach (var property in GoRuntime.RangeValues(properties))
            {
                if (Ast.IsStatic(property) && !this.shouldTransformPrivateElementsOrClassStaticBlocks)
                {
                    continue;
                }
                var statement = this.transformPropertyOrClassStaticBlock(property, receiver);
                if (statement != null)
                {
                    statements = statements.Append(statement);
                }
            }
        }
        return statements;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2650:1.
    private Node transformPropertyOrClassStaticBlock(Node property, Node receiver)
    {
        Node expression = default;
        if (Ast.IsClassStaticBlockDeclaration(property))
        {
            expression = this.setCurrentClassElementAnd(property, static (tx, value) => tx.transformClassStaticBlockDeclaration(value), property);
        }
        else
        {
            expression = this.transformProperty(property.AsPropertyDeclaration(), receiver);
        }
        if (expression == null)
        {
            return null;
        }
        var statement = this.Factory().NewExpressionStatement(expression);
        this.EmitContext().SetOriginal(statement, property);
        this.EmitContext().AddEmitFlags(statement, (this.EmitContext().EmitFlags(property) & EmitFlags.NoComments));
        this.EmitContext().SetCommentRange(statement, property.Loc);
        var propertyOriginalNode = this.EmitContext().MostOriginal(property);
        if (Ast.IsParameterDeclaration(propertyOriginalNode))
        {
            this.EmitContext().SetSourceMapRange(statement, propertyOriginalNode.Loc);
            this.EmitContext().AddEmitFlags(statement, EmitFlags.NoComments);
        }
        else
        {
            this.EmitContext().SetSourceMapRange(statement, Transformers.MoveRangePastModifiers(property));
        }
        this.EmitContext().SetSyntheticLeadingComments(expression, default);
        this.EmitContext().SetSyntheticTrailingComments(expression, default);
        if (Ast.HasAccessorModifier(propertyOriginalNode))
        {
            this.EmitContext().AddEmitFlags(statement, EmitFlags.NoComments);
        }
        return statement;
    }

    // generateInitializedPropertyExpressionsOrClassStaticBlock generates assignment expressions for property initializers.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:2690:1.
    private Slice<Node> generateInitializedPropertyExpressionsOrClassStaticBlock(Slice<Node> propertiesOrClassStaticBlocks, Node receiver)
    {
        Slice<Node> expressions = default;
        {
            foreach (var property in GoRuntime.RangeValues(propertiesOrClassStaticBlocks))
            {
                Node expression = default;
                if (Ast.IsClassStaticBlockDeclaration(property))
                {
                    expression = this.setCurrentClassElementAnd(property, static (tx, value) => tx.transformClassStaticBlockDeclaration(value), property);
                }
                else
                {
                    expression = this.transformProperty(property.AsPropertyDeclaration(), receiver);
                }
                if (expression == null)
                {
                    continue;
                }
                this.EmitContext().SetOriginalEx(expression, property, true);
                this.EmitContext().AssignCommentAndSourceMapRanges(expression, property);
                expressions = expressions.Append(expression);
            }
        }
        return expressions;
    }

    // transformProperty transforms a property initializer into an assignment expression.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:2713:1.
    private Node transformProperty(PropertyDeclaration property, Node receiver)
    {
        var savedCurrentClassElement = this.currentClassElement;
        var transformed = this.transformPropertyWorker(property, receiver);
        if ((transformed != null) && Ast.HasStaticModifier(property.AsNode()))
        {
            this.EmitContext().AddEmitFlags(transformed, EmitFlags.NoLexicalThis);
        }
        if (((((transformed != null) && Ast.HasStaticModifier(property.AsNode())) && (this.lexicalEnvironment != null)) && (this.lexicalEnvironment.data != null)) && (this.lexicalEnvironment.data.facts != 0))
        {
            this.EmitContext().SetOriginal(transformed, property.AsNode());
            this.EmitContext().SetSourceMapRange(transformed, this.EmitContext().SourceMapRange(property.Name()));
        }
        this.currentClassElement = savedCurrentClassElement;
        return transformed;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2729:1.
    private Node transformPropertyWorker(PropertyDeclaration property, Node receiver)
    {
        var emitAssignment = !this.compilerOptions.GetUseDefineForClassFields();
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), property.AsNode(), this.isAnonymousClassNeedingAssignedName))
        {
            property = EsTransforms.TransformNamedEvaluation(this.EmitContext(), property.AsNode(), false, default).AsPropertyDeclaration();
        }
        var propertyName = property.Name();
        if (Ast.HasAccessorModifier(property.AsNode()))
        {
            propertyName = this.Factory().NewGeneratedPrivateNameForNodeEx(property.Name(), new AutoGenerateOptions { Suffix = "_accessor_storage" });
        }
        else
        {
            if (Ast.IsComputedPropertyName(propertyName) && !Transformers.IsSimpleInlineableExpression(propertyName.Expression()))
            {
                propertyName = this.Factory().UpdateComputedPropertyName(propertyName.AsComputedPropertyName(), this.Factory().NewGeneratedNameForNode(propertyName));
            }
        }
        if (Ast.HasStaticModifier(property.AsNode()))
        {
            this.currentClassElement = property.AsNode();
        }
        if (Ast.IsPrivateIdentifier(propertyName) && this.shouldTransformClassElementToWeakMap(property.AsNode()))
        {
            var info = this.accessPrivateIdentifier(propertyName);
            if (info != null)
            {
                if (info.kind == PrivateIdentifierKind.Field)
                {
                    if (!info.isStatic)
                    {
                        return EsTransforms.CreatePrivateInstanceFieldInitializer(this.Factory(), receiver, this.Visitor().VisitNode(property.Initializer), info.brandCheckIdentifier);
                    }
                    return EsTransforms.CreatePrivateStaticFieldInitializer(this.Factory(), info.variableName, this.Visitor().VisitNode(property.Initializer));
                }
                return null;
            }
            else
            {
                Debug.Fail("Undeclared private name for property declaration.");
            }
        }
        if (((Ast.IsPrivateIdentifier(propertyName) || Ast.HasStaticModifier(property.AsNode()))) && (property.Initializer == null))
        {
            return null;
        }
        if (Ast.HasAbstractModifier(this.EmitContext().MostOriginal(property.AsNode())))
        {
            return null;
        }
        var initializer = this.Visitor().VisitNode(property.Initializer);
        var propertyOriginalNode = this.EmitContext().MostOriginal(property.AsNode());
        if (Ast.IsParameterPropertyDeclaration(propertyOriginalNode, propertyOriginalNode.Parent) && Ast.IsIdentifier(propertyName))
        {
            var localName = propertyName.Clone(this.Factory());
            if (initializer != null)
            {
                if ((((Ast.IsParenthesizedExpression(initializer) && Ast.IsCommaExpression(initializer.Expression())) && this.EmitContext().IsCallToHelper(initializer.Expression().AsBinaryExpression().Left, "__runInitializers")) && Ast.IsVoidExpression(initializer.Expression().AsBinaryExpression().Right)) && Ast.IsNumericLiteral(initializer.Expression().AsBinaryExpression().Right.Expression()))
                {
                    initializer = initializer.Expression().AsBinaryExpression().Left;
                }
                initializer = this.Factory().InlineExpressions(Slice<Node>.FromArray([initializer, localName]));
            }
            else
            {
                initializer = localName;
            }
            this.EmitContext().AddEmitFlags(propertyName, (EmitFlags.NoComments | EmitFlags.NoSourceMap));
            this.EmitContext().SetSourceMapRange(localName, propertyOriginalNode.Name().Loc);
            this.EmitContext().AddEmitFlags(localName, EmitFlags.NoComments);
        }
        else
        {
            if (initializer == null)
            {
                initializer = this.Factory().NewVoidZeroExpression();
            }
        }
        if (emitAssignment || Ast.IsPrivateIdentifier(propertyName))
        {
            var memberAccess = EsTransforms.CreateMemberAccessForPropertyName(this.Factory(), this.EmitContext(), receiver, propertyName, propertyName);
            this.EmitContext().AddEmitFlags(memberAccess, EmitFlags.NoLeadingComments);
            return this.Factory().NewAssignmentExpression(memberAccess, initializer);
        }
        Node name = default;
        if (Ast.IsComputedPropertyName(propertyName))
        {
            name = propertyName.Expression();
        }
        else
        {
            if (Ast.IsIdentifier(propertyName))
            {
                name = this.Factory().NewStringLiteralWithTextValue(propertyName.TextValue(), TokenFlags.None);
            }
            else
            {
                name = propertyName;
            }
        }
        var descriptor = this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewPropertyAssignment(null,
                        this.Factory().NewIdentifier("enumerable"),
                        null,
                        null,
                        this.Factory().NewTrueExpression()),
                    this.Factory().NewPropertyAssignment(null,
                        this.Factory().NewIdentifier("configurable"),
                        null,
                        null,
                        this.Factory().NewTrueExpression()),
                    this.Factory().NewPropertyAssignment(null,
                        this.Factory().NewIdentifier("writable"),
                        null,
                        null,
                        this.Factory().NewTrueExpression()),
                    this.Factory().NewPropertyAssignment(null,
                        this.Factory().NewIdentifier("value"),
                        null,
                        null,
                        initializer)])),
            true);
        return this.Factory().NewObjectDefinePropertyCall(receiver, name, descriptor);
    }

    // addInstanceMethodStatements generates brand-check initializer for private methods.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:2836:1.
    private Slice<Node> addInstanceMethodStatements(Slice<Node> statements, Slice<Node> methods, Node receiver)
    {
        if (!this.shouldTransformPrivateElementsOrClassStaticBlocks || (GoRuntime.Len(methods) == 0))
        {
            return statements;
        }
        var env = this.getPrivateIdentifierEnvironment();
        var weakSetName = env.data.weakSetName;
        Debug.Assert((weakSetName != null), "weakSetName should be set in private identifier environment");
        return statements.Append(this.Factory().NewExpressionStatement(EsTransforms.CreatePrivateInstanceMethodInitializer(this.Factory(), receiver, weakSetName)));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2853:1.
    private Node visitInvalidSuperProperty(Node node)
    {
        if (Ast.IsPropertyAccessExpression(node))
        {
            return this.Factory().UpdatePropertyAccessExpression(node.AsPropertyAccessExpression(), this.Factory().NewVoidZeroExpression(), null, node.Name(), node.Flags);
        }
        return this.Factory().UpdateElementAccessExpression(node.AsElementAccessExpression(),
            this.Factory().NewVoidZeroExpression(),
            null,
            this.Visitor().VisitNode(node.AsElementAccessExpression().ArgumentExpression),
            node.Flags);
    }

    // getPropertyNameExpressionIfNeeded transforms a computed property name, then either returns an expression
    // which caches the value of the result or the expression itself if the value is either unused or safe to
    // inline into multiple locations.
    // shouldHoist indicates whether the expression needs to be reused (i.e., for an initializer or a decorator).
    // Upstream tsc/internal/transformers/estransforms/classfields.go:2876:1.
    private Node getPropertyNameExpressionIfNeeded(Node name, bool shouldHoist)
    {
        if (!Ast.IsComputedPropertyName(name))
        {
            return null;
        }
        var cacheAssignment = EsTransforms.FindComputedPropertyNameCacheAssignment(this.EmitContext(), name);
        var savedLexicalEnvironment = this.lexicalEnvironment;
        var savedInsideComputedPropertyName = this.insideComputedPropertyName;
        this.insideComputedPropertyName = true;
        if ((this.lexicalEnvironment != null) && (this.lexicalEnvironment.previous != null))
        {
            this.lexicalEnvironment = this.lexicalEnvironment.previous;
        }
        var expression = this.Visitor().VisitNode(name.Expression());
        this.lexicalEnvironment = savedLexicalEnvironment;
        this.insideComputedPropertyName = savedInsideComputedPropertyName;
        var innerExpression = Ast.SkipPartiallyEmittedExpressions(expression);
        var inlinable = Transformers.IsSimpleInlineableExpression(innerExpression);
        var alreadyTransformed = ((cacheAssignment != null) || (((Ast.IsAssignmentExpression(innerExpression, true) && Ast.IsIdentifier(innerExpression.AsBinaryExpression().Left)) && Transformers.IsGeneratedIdentifier(this.EmitContext(), innerExpression.AsBinaryExpression().Left))));
        if ((!alreadyTransformed && !inlinable) && shouldHoist)
        {
            var generatedName = this.Factory().NewGeneratedNameForNode(name);
            if (this.requiresBlockScopedVar())
            {
                this.EmitContext().AddLexicalDeclaration(generatedName);
            }
            else
            {
                this.EmitContext().AddVariableDeclaration(generatedName);
            }
            return this.Factory().NewAssignmentExpression(generatedName, expression);
        }
        if (inlinable || Ast.IsIdentifier(innerExpression))
        {
            return null;
        }
        return expression;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2910:1.
    private void startClassLexicalEnvironment()
    {
        this.lexicalEnvironment = new classLexicalEnv { previous = this.lexicalEnvironment };
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2914:1.
    private void endClassLexicalEnvironment()
    {
        this.lexicalEnvironment = this.lexicalEnvironment.previous;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2918:1.
    private classLexicalEnvironment getClassLexicalEnvironment()
    {
        Debug.Assert((this.lexicalEnvironment != null));
        if (this.lexicalEnvironment.data == null)
        {
            this.lexicalEnvironment.data = new classLexicalEnvironment {  };
        }
        return this.lexicalEnvironment.data;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2926:1.
    private privateEnvironment getPrivateIdentifierEnvironment()
    {
        Debug.Assert((this.lexicalEnvironment != null));
        if (this.lexicalEnvironment.privateEnv == null)
        {
            this.lexicalEnvironment.privateEnv = new privateEnvironment { members = new Dictionary<ByteStringKey, privateIdentifierInfo>() };
        }
        return this.lexicalEnvironment.privateEnv;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2936:1.
    private void addPendingExpressions(params ReadOnlySpan<Node> exprs)
    {
        this.pendingExpressions = this.pendingExpressions.Append(exprs);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2940:1.
    private void addPrivateIdentifierPropertyDeclarationToEnvironment(Node node, Node name)
    {
        var lex = this.getClassLexicalEnvironment();
        var env = this.getPrivateIdentifierEnvironment();
        var isStatic = Ast.HasStaticModifier(node);
        var tuple29 = this.getPrivateIdentifier(env, name); var previousInfo = tuple29.Item1;
        var isValid = (!this.isReservedPrivateName(name) && (previousInfo == null));
        if (isStatic)
        {
            var brandCheckIdentifier = lex.classThis;
            if (brandCheckIdentifier == null)
            {
                brandCheckIdentifier = lex.classConstructor;
            }
            var variableName = this.createHoistedVariableForPrivateName(name, "");
            this.setPrivateIdentifier(env, name, new privateIdentifierInfo { kind = PrivateIdentifierKind.Field, isStatic = true, brandCheckIdentifier = brandCheckIdentifier, variableName = variableName, isValid = isValid });
        }
        else
        {
            var weakMapName = this.createHoistedVariableForPrivateName(name, "");
            this.setPrivateIdentifier(env, name, new privateIdentifierInfo { kind = PrivateIdentifierKind.Field, isStatic = false, brandCheckIdentifier = weakMapName, isValid = isValid });
            this.addPendingExpressions(this.Factory().NewAssignmentExpression(weakMapName,
                    this.Factory().NewNewExpression(this.Factory().NewIdentifier("WeakMap"),
                        null,
                        this.Factory().NewNodeList(default(Slice<Node>)))));
        }
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:2981:1.
    private void addPrivateIdentifierMethodToEnvironment(Node name, classLexicalEnvironment lex, privateEnvironment env, bool isStatic, bool isValid)
    {
        var methodName = this.createHoistedVariableForPrivateName(name, "");
        Node brandCheckIdentifier = default;
        if (isStatic)
        {
            brandCheckIdentifier = lex.classThis;
            if (brandCheckIdentifier == null)
            {
                brandCheckIdentifier = lex.classConstructor;
            }
            Debug.Assert((brandCheckIdentifier != null), "classConstructor should be set in private identifier environment");
        }
        else
        {
            brandCheckIdentifier = env.data.weakSetName;
        }
        this.setPrivateIdentifier(env, name, new privateIdentifierInfo { kind = PrivateIdentifierKind.Method, methodName = methodName, brandCheckIdentifier = brandCheckIdentifier, isStatic = isStatic, isValid = isValid });
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3002:1.
    private void addPrivateIdentifierGetAccessorToEnvironment(Node name, classLexicalEnvironment lex, privateEnvironment env, bool isStatic, bool isValid, privateIdentifierInfo previousInfo)
    {
        var getterName = this.createHoistedVariableForPrivateName(name, "_get");
        Node brandCheckIdentifier = default;
        if (isStatic)
        {
            brandCheckIdentifier = lex.classThis;
            if (brandCheckIdentifier == null)
            {
                brandCheckIdentifier = lex.classConstructor;
            }
            Debug.Assert((brandCheckIdentifier != null), "classConstructor should be set in private identifier environment");
        }
        else
        {
            brandCheckIdentifier = env.data.weakSetName;
            Debug.Assert((brandCheckIdentifier != null), "weakSetName should be set in private identifier environment");
        }
        if ((((previousInfo != null) && (previousInfo.kind == PrivateIdentifierKind.Accessor)) && (previousInfo.isStatic == isStatic)) && (previousInfo.getterName == null))
        {
            previousInfo.getterName = getterName;
        }
        else
        {
            this.setPrivateIdentifier(env, name, new privateIdentifierInfo { kind = PrivateIdentifierKind.Accessor, getterName = getterName, brandCheckIdentifier = brandCheckIdentifier, isStatic = isStatic, isValid = isValid });
        }
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3029:1.
    private void addPrivateIdentifierSetAccessorToEnvironment(Node name, classLexicalEnvironment lex, privateEnvironment env, bool isStatic, bool isValid, privateIdentifierInfo previousInfo)
    {
        var setterName = this.createHoistedVariableForPrivateName(name, "_set");
        Node brandCheckIdentifier = default;
        if (isStatic)
        {
            brandCheckIdentifier = lex.classThis;
            if (brandCheckIdentifier == null)
            {
                brandCheckIdentifier = lex.classConstructor;
            }
            Debug.Assert((brandCheckIdentifier != null), "classConstructor should be set in private identifier environment");
        }
        else
        {
            brandCheckIdentifier = env.data.weakSetName;
            Debug.Assert((brandCheckIdentifier != null), "weakSetName should be set in private identifier environment");
        }
        if ((((previousInfo != null) && (previousInfo.kind == PrivateIdentifierKind.Accessor)) && (previousInfo.isStatic == isStatic)) && (previousInfo.setterName == null))
        {
            previousInfo.setterName = setterName;
        }
        else
        {
            this.setPrivateIdentifier(env, name, new privateIdentifierInfo { kind = PrivateIdentifierKind.Accessor, setterName = setterName, brandCheckIdentifier = brandCheckIdentifier, isStatic = isStatic, isValid = isValid });
        }
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3056:1.
    private void addPrivateIdentifierAutoAccessorToEnvironment(Node node, Node name, classLexicalEnvironment lex, privateEnvironment env, bool isStatic, bool isValid)
    {
        var getterName = this.createHoistedVariableForPrivateName(name, "_get");
        var setterName = this.createHoistedVariableForPrivateName(name, "_set");
        Node brandCheckIdentifier = default;
        if (isStatic)
        {
            brandCheckIdentifier = lex.classThis;
            if (brandCheckIdentifier == null)
            {
                brandCheckIdentifier = lex.classConstructor;
            }
            Debug.Assert((brandCheckIdentifier != null), "classConstructor should be set in private identifier environment");
        }
        else
        {
            brandCheckIdentifier = env.data.weakSetName;
            Debug.Assert((brandCheckIdentifier != null), "weakSetName should be set in private identifier environment");
        }
        this.setPrivateIdentifier(env, name, new privateIdentifierInfo { kind = PrivateIdentifierKind.Accessor, getterName = getterName, setterName = setterName, brandCheckIdentifier = brandCheckIdentifier, isStatic = isStatic, isValid = isValid });
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3081:1.
    private void addPrivateIdentifierToEnvironment(Node node)
    {
        var lex = this.getClassLexicalEnvironment();
        var env = this.getPrivateIdentifierEnvironment();
        var name = node.Name();
        var isStatic = Ast.HasStaticModifier(node);
        var tuple30 = this.getPrivateIdentifier(env, name); var previousInfo = tuple30.Item1;
        var isValid = (!this.isReservedPrivateName(name) && (previousInfo == null));
        if (Ast.IsAutoAccessorPropertyDeclaration(node))
        {
            this.addPrivateIdentifierAutoAccessorToEnvironment(node, name, lex, env, isStatic, isValid);
        }
        else
        {
            if (Ast.IsPropertyDeclaration(node))
            {
                this.addPrivateIdentifierPropertyDeclarationToEnvironment(node, name);
            }
            else
            {
                if (Ast.IsMethodDeclaration(node))
                {
                    this.addPrivateIdentifierMethodToEnvironment(name, lex, env, isStatic, isValid);
                }
                else
                {
                    if (Ast.IsGetAccessorDeclaration(node))
                    {
                        this.addPrivateIdentifierGetAccessorToEnvironment(name, lex, env, isStatic, isValid, previousInfo);
                    }
                    else
                    {
                        if (Ast.IsSetAccessorDeclaration(node))
                        {
                            this.addPrivateIdentifierSetAccessorToEnvironment(name, lex, env, isStatic, isValid, previousInfo);
                        }
                    }
                }
            }
        }
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3102:1.
    private void setPrivateIdentifier(privateEnvironment env, Node name, privateIdentifierInfo info)
    {
        if (this.EmitContext().HasAutoGenerateInfo(name))
        {
            if (env.generatedIdentifiers == null)
            {
                env.generatedIdentifiers = new Dictionary<Node, privateIdentifierInfo>(ReferenceEqualityComparer.Instance);
            }
            env.generatedIdentifiers[this.EmitContext().GetNodeForGeneratedName(name)] = info;
        }
        else
        {
            env.members[ByteStringKey.FromValue(name.TextValue())] = info;
        }
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3113:1.
    private (privateIdentifierInfo, bool) getPrivateIdentifier(privateEnvironment env, Node name)
    {
        if (this.EmitContext().HasAutoGenerateInfo(name))
        {
            if (env.generatedIdentifiers == null) return (null, false);
            var ok = env.generatedIdentifiers.TryGetValue(this.EmitContext().GetNodeForGeneratedName(name), out var info);
            return (info, ok);
        }
        var ok2 = env.members.TryGetValue(ByteStringKey.FromValue(name.TextValue()), out var info2);
        return (info2, ok2);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3122:1.
    private Node createHoistedVariableForClass(ByteStringValue nameText, Node node, string suffix)
    {
        var env = this.getPrivateIdentifierEnvironment();
        Node identifier = default;
        if (env.data.className != null)
        {
            var prefix = ByteStringValue.Concat(ByteStringValue.FromString("_"), env.data.className.TextValue(), ByteStringValue.FromString("_"));
            identifier = this.Factory().NewUniqueNameEx(ByteStringValue.Concat(prefix,
                    nameText),
                new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.ReservedInNestedScopes), Suffix = suffix });
        }
        else
        {
            identifier = this.Factory().NewUniqueNameEx(ByteStringValue.Concat(ByteStringValue.FromString("_"),
                    nameText),
                new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.ReservedInNestedScopes), Suffix = suffix });
        }
        if (this.requiresBlockScopedVar())
        {
            this.EmitContext().AddLexicalDeclaration(identifier);
        }
        else
        {
            this.EmitContext().AddVariableDeclaration(identifier);
        }
        return identifier;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3145:1.
    private Node createHoistedVariableForClassFromNode(Node name, string suffix)
    {
        var env = this.getPrivateIdentifierEnvironment();
        string prefix = "";
        if (env.data.className != null)
        {
            prefix = ByteStringValue.Concat(ByteStringValue.FromString("_"), env.data.className.TextValue(), ByteStringValue.FromString("_")).Materialize();
        }
        else
        {
            prefix = "_";
        }
        var identifier = this.Factory().NewGeneratedNameForNodeEx(name,
            new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.ReservedInNestedScopes), Prefix = prefix, Suffix = suffix });
        if (this.requiresBlockScopedVar())
        {
            this.EmitContext().AddLexicalDeclaration(identifier);
        }
        else
        {
            this.EmitContext().AddVariableDeclaration(identifier);
        }
        return identifier;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3166:1.
    private Node createHoistedVariableForPrivateName(Node name, string suffix)
    {
        if (this.EmitContext().HasAutoGenerateInfo(name))
        {
            return this.createHoistedVariableForClassFromNode(name, suffix);
        }
        var text = name.TextValue();
        if ((GoRuntime.Len(text) >= 1) && (text[0] == '#'))
        {
            text = text.Slice(1);
        }
        return this.createHoistedVariableForClass(text, name, suffix);
    }

    // accessPrivateIdentifier accesses an already defined PrivateIdentifier in the current
    // PrivateIdentifierEnvironment.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:3181:1.
    private privateIdentifierInfo accessPrivateIdentifier(Node name)
    {
        {
            var env = this.lexicalEnvironment;
            for (; (env != null); env = env.previous)
            {
                if (env.privateEnv != null)
                {
                    {
                        var tuple31 = this.getPrivateIdentifier(env.privateEnv, name); var info = tuple31.Item1; var ok = tuple31.Item2;
                        if (ok)
                        {
                            if (info.kind == PrivateIdentifierKind.Untransformed)
                            {
                                return null;
                            }
                            return info;
                        }
                    }
                }
            }
        }
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3195:1.
    private Node wrapPrivateIdentifierForDestructuringTarget(Node node)
    {
        var prop = node.AsPropertyAccessExpression();
        var parameter = this.Factory().NewGeneratedNameForNode(node);
        var info = this.accessPrivateIdentifier(prop.Name());
        if (info == null)
        {
            return this.Visitor().VisitEachChild(node);
        }
        var receiver = prop.Expression;
        var isThisOrSuperProperty = ((prop.Expression.Kind == Kind.ThisKeyword) || (prop.Expression.Kind == Kind.SuperKeyword));
        if (isThisOrSuperProperty || !Transformers.IsSimpleCopiableExpression(prop.Expression))
        {
            receiver = this.Factory().NewTempVariableEx(new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.ReservedInNestedScopes });
            this.EmitContext().AddVariableDeclaration(receiver);
            this.pendingExpressions = this.pendingExpressions.Append(this.Factory().NewAssignmentExpression(receiver, this.Visitor().VisitNode(prop.Expression)));
        }
        var assignExpr = this.createPrivateIdentifierAssignment(info, receiver, parameter, Kind.EqualsToken);
        return this.Factory().NewAssignmentTargetWrapper(parameter, assignExpr);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3220:1.
    private Node visitAssignmentElement(Node node)
    {
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node, this.isAnonymousClassNeedingAssignedName))
        {
            node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node, false, default);
        }
        if (Ast.IsAssignmentExpression(node, true))
        {
            var left = this.visitDestructuringAssignmentTarget(node.AsBinaryExpression().Left);
            var right = this.Visitor().VisitNode(node.AsBinaryExpression().Right);
            return this.Factory().UpdateBinaryExpression(node.AsBinaryExpression(), null, left, null, node.AsBinaryExpression().OperatorToken, right);
        }
        return this.visitDestructuringAssignmentTarget(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3247:1.
    private Node visitAssignmentRestElement(Node node)
    {
        var spread = node.AsSpreadElement();
        if (Ast.IsLeftHandSideExpression(spread.Expression))
        {
            var expr = this.visitDestructuringAssignmentTarget(spread.Expression);
            return this.Factory().UpdateSpreadElement(spread, expr);
        }
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3256:1.
    private Node visitArrayAssignmentElement(Node node)
    {
        if (Ast.IsArrayBindingOrAssignmentElement(node))
        {
            if (Ast.IsSpreadElement(node))
            {
                return this.visitAssignmentRestElement(node);
            }
            if (node.Kind != Kind.OmittedExpression)
            {
                return this.visitAssignmentElement(node);
            }
        }
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3268:1.
    private Node visitAssignmentProperty(Node node)
    {
        var prop = node.AsPropertyAssignment();
        var name = this.Visitor().VisitNode(prop.Name());
        var init = prop.Initializer;
        if (Ast.IsAssignmentExpression(init, true))
        {
            var assignElem = this.visitAssignmentElement(init);
            return this.Factory().UpdatePropertyAssignment(prop, null, name, null, null, assignElem);
        }
        if (Ast.IsLeftHandSideExpression(init))
        {
            var target = this.visitDestructuringAssignmentTarget(init);
            return this.Factory().UpdatePropertyAssignment(prop, null, name, null, null, target);
        }
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3294:1.
    private Node visitShorthandAssignmentProperty(Node node)
    {
        if (EsTransforms.IsNamedEvaluationAnd(this.EmitContext(), node, this.isAnonymousClassNeedingAssignedName))
        {
            node = EsTransforms.TransformNamedEvaluation(this.EmitContext(), node, false, default);
        }
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3311:1.
    private Node visitAssignmentRestProperty(Node node)
    {
        var spread = node.AsSpreadAssignment();
        if (Ast.IsLeftHandSideExpression(spread.Expression))
        {
            var expr = this.visitDestructuringAssignmentTarget(spread.Expression);
            return this.Factory().UpdateSpreadAssignment(spread, expr);
        }
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3320:1.
    private Node visitObjectAssignmentElement(Node node)
    {
        Debug.Assert(((node != null) && Ast.IsObjectLiteralElement(node)));
        if (Ast.IsSpreadAssignment(node))
        {
            return this.visitAssignmentRestProperty(node);
        }
        if (Ast.IsShorthandPropertyAssignment(node))
        {
            return this.visitShorthandAssignmentProperty(node);
        }
        if (Ast.IsPropertyAssignment(node))
        {
            return this.visitAssignmentProperty(node);
        }
        return this.Visitor().VisitEachChild(node);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3334:1.
    private Node visitAssignmentPattern(Node node)
    {
        if (Ast.IsArrayLiteralExpression(node))
        {
            return this.Factory().UpdateArrayLiteralExpression(node.AsArrayLiteralExpression(),
                this.arrayAssignmentElementVisitor.VisitNodes(node.AsArrayLiteralExpression().Elements),
                node.AsArrayLiteralExpression().MultiLine);
        }
        return this.Factory().UpdateObjectLiteralExpression(node.AsObjectLiteralExpression(),
            this.objectAssignmentElementVisitor.VisitNodes(node.AsObjectLiteralExpression().Properties),
            node.AsObjectLiteralExpression().MultiLine);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3457:1.
    private (Node, Node) createCallBinding(Node node)
    {
        Node thisArg = default;
        Node target = default;
        if (Ast.IsSuperProperty(node))
        {
            return (this.Factory().NewThisExpression(), node);
        }
        if (Ast.IsPropertyAccessExpression(node))
        {
            var expr = node.AsPropertyAccessExpression();
            if (EsTransforms.ShouldBeCapturedInTempVariable(expr.Expression))
            {
                thisArg = this.Factory().NewTempVariable();
                this.EmitContext().AddVariableDeclaration(thisArg);
                target = this.Factory().NewPropertyAccessExpression(this.Factory().NewParenthesizedExpression(this.Factory().NewAssignmentExpression(thisArg,
                            expr.Expression)),
                    null,
                    expr.Name(),
                    NodeFlags.None);
                return (thisArg, target);
            }
            return (expr.Expression, node);
        }
        thisArg = this.Factory().NewVoidZeroExpression();
        target = node;
        return (thisArg, target);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3493:1.
    private Node createAccessorPropertyGetRedirector(PropertyDeclaration node, ModifierList modifiers, Node name, Node receiver)
    {
        var backingFieldName = this.Factory().NewGeneratedPrivateNameForNodeEx(node.Name(), new AutoGenerateOptions { Suffix = "_accessor_storage" });
        var returnExpr = this.Factory().NewPropertyAccessExpression(receiver, null, backingFieldName, NodeFlags.None);
        var returnStmt = this.Factory().NewReturnStatement(returnExpr);
        var body = this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([returnStmt])), false);
        return this.Factory().NewGetAccessorDeclaration(modifiers, name, null, this.Factory().NewNodeList(Slice<Node>.Make(0)), null, null, body);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3514:1.
    private Node createAccessorPropertySetRedirector(PropertyDeclaration node, ModifierList modifiers, Node name, Node receiver)
    {
        var backingFieldName = this.Factory().NewGeneratedPrivateNameForNodeEx(node.Name(), new AutoGenerateOptions { Suffix = "_accessor_storage" });
        var valueParam = this.Factory().NewParameterDeclaration(null, null, this.Factory().NewIdentifier("value"), null, null, null);
        var assignExpr = this.Factory().NewAssignmentExpression(this.Factory().NewPropertyAccessExpression(receiver,
                null,
                backingFieldName,
                NodeFlags.None),
            this.Factory().NewIdentifier("value"));
        var exprStmt = this.Factory().NewExpressionStatement(assignExpr);
        var body = this.Factory().NewBlock(this.Factory().NewNodeList(Slice<Node>.FromArray([exprStmt])), false);
        return this.Factory().NewSetAccessorDeclaration(modifiers, name, null, this.Factory().NewNodeList(Slice<Node>.FromArray([valueParam])), null, null, body);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3391:1.
    private bool isReservedPrivateName(Node node)
    {
        return (!((Ast.IsPrivateIdentifier(node) && this.EmitContext().HasAutoGenerateInfo(node))) && (node.TextEquals("#constructor"u8)));
    }
    // Upstream tsc/internal/transformers/estransforms/classfields.go:3400:1.
    private Slice<Node> getProperties(Node node, bool requireInitializer, bool isStatic)
    {
        Slice<Node> result = default;
        {
            foreach (var member in GoRuntime.RangeValues(node.Members()))
            {
                if ((Ast.IsPropertyDeclaration(member) && ((!requireInitializer || (member.Initializer() != null)))) && (Ast.HasStaticModifier(member) == isStatic))
                {
                    result = result.Append(member);
                }
            }
        }
        return result;
    }
    // Upstream tsc/internal/transformers/estransforms/classfields.go:3412:1.
    private Slice<Node> getStaticPropertiesAndClassStaticBlock(Node node)
    {
        Slice<Node> result = default;
        {
            foreach (var member in GoRuntime.RangeValues(node.Members()))
            {
                if (Ast.IsClassStaticBlockDeclaration(member) || ((Ast.IsPropertyDeclaration(member) && Ast.HasStaticModifier(member))))
                {
                    result = result.Append(member);
                }
            }
        }
        return result;
    }
}

internal static partial class EsTransforms
{
    // Upstream tsc/internal/transformers/estransforms/classfields.go:3365:1.
    internal static Node CreatePrivateStaticFieldInitializer(EmitNodeFactory factory, Node variableName, Node initializer)
    {
        if (initializer == null)
        {
            initializer = factory.NewVoidZeroExpression();
        }
        return factory.NewAssignmentExpression(variableName,
            factory.NewObjectLiteralExpression(factory.NewNodeList(Slice<Node>.FromArray([factory.NewPropertyAssignment(null,
                            factory.NewIdentifier("value"),
                            null,
                            null,
                            initializer)])),
                false));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3380:1.
    internal static Node CreatePrivateInstanceFieldInitializer(EmitNodeFactory factory, Node receiver, Node initializer, Node weakMapName)
    {
        if (initializer == null)
        {
            initializer = factory.NewVoidZeroExpression();
        }
        return factory.NewMethodCall(weakMapName, factory.NewIdentifier("set"), Slice<Node>.FromArray([receiver, initializer]));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3387:1.
    internal static Node CreatePrivateInstanceMethodInitializer(EmitNodeFactory factory, Node receiver, Node weakSetName)
    {
        return factory.NewMethodCall(weakSetName, factory.NewIdentifier("add"), Slice<Node>.FromArray([receiver]));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3395:1.
    internal static bool IsStaticPropertyDeclarationOrClassStaticBlock(Node node)
    {
        return (Ast.IsClassStaticBlockDeclaration(node) || ((Ast.IsPropertyDeclaration(node) && Ast.HasStaticModifier(node))));
    }

    // classHasClassThisAssignment checks if a class has a static block that is a class-this assignment.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:3423:1.
    internal static bool ClassHasClassThisAssignment(EmitContext emitContext, Node node)
    {
        {
            foreach (var member in GoRuntime.RangeValues(node.Members()))
            {
                if (IsClassThisAssignmentBlock(emitContext, member))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3432:1.
    internal static bool IsNonStaticMethodOrAccessorWithPrivateName(Node member)
    {
        return ((!Ast.IsStatic(member) && ((Ast.IsMethodOrAccessor(member) || Ast.IsAutoAccessorPropertyDeclaration(member)))) && Ast.IsPrivateIdentifier(member.Name()));
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3438:1.
    internal static Node CreateMemberAccessForPropertyName(EmitNodeFactory factory, EmitContext emitContext, Node receiver, Node name, Node location)
    {
        if (Ast.IsComputedPropertyName(name))
        {
            var expression = factory.NewElementAccessExpression(receiver, null, name.Expression(), NodeFlags.None);
            expression.Loc = location.Loc;
            return expression;
        }
        Node expression2 = default;
        if (Ast.IsIdentifier(name) || Ast.IsPrivateIdentifier(name))
        {
            expression2 = factory.NewPropertyAccessExpression(receiver, null, name, NodeFlags.None);
        }
        else
        {
            expression2 = factory.NewElementAccessExpression(receiver, null, name, NodeFlags.None);
        }
        emitContext.SetCommentRange(expression2, name.Loc);
        emitContext.SetSourceMapRange(expression2, name.Loc);
        emitContext.AddEmitFlags(expression2, EmitFlags.NoNestedSourceMaps);
        return expression2;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3483:1.
    internal static bool ShouldBeCapturedInTempVariable(Node node)
    {
        var target = Ast.SkipParentheses(node);
        switch (target.Kind)
        {
            case Kind.Identifier:
            case Kind.ThisKeyword:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.StringLiteral:
            {
                return false;
            }
            default:
            {
                return true;
            }
        }
    }

    // flattenCommaList decomposes a comma expression tree into a sequence of expressions.
    // Upstream tsc/internal/transformers/estransforms/classfields.go:3547:1.
    internal static CommaListSequence FlattenCommaList(Node node) => new(node);

    internal readonly struct CommaListSequence(Node node)
    {
        internal bool Visit(Func<Node, bool> yield) => FlattenCommaListWorker(node, yield);
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3553:1.
    internal static bool FlattenCommaListWorker(Node node, Func<Node, bool> yield)
    {
        if (Ast.IsParenthesizedExpression(node) && Ast.NodeIsSynthesized(node))
        {
            return FlattenCommaListWorker(node.Expression(), yield);
        }
        else
        {
            if (Ast.IsCommaExpression(node.AsNode()))
            {
                return (FlattenCommaListWorker(node.AsBinaryExpression().Left, yield) && FlattenCommaListWorker(node.AsBinaryExpression().Right, yield));
            }
            else
            {
                return yield(node);
            }
        }
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3564:1.
    internal static BinaryExpression FindComputedPropertyNameCacheAssignment(EmitContext emitContext, Node name)
    {
        var node = name.Expression();
        while (true)
        {
            node = Ast.SkipOuterExpressions(node, 0);
            if (Ast.IsBinaryExpression(node) && (node.AsBinaryExpression().OperatorToken.Kind == Kind.CommaToken))
            {
                node = node.AsBinaryExpression().Right;
                continue;
            }
            if (Ast.IsAssignmentExpression(node, true) && Ast.IsIdentifier(node.AsBinaryExpression().Left))
            {
                return node.AsBinaryExpression();
            }
            break;
        }
        return null;
    }

    // Upstream tsc/internal/transformers/estransforms/classfields.go:3580:1.
    internal static Node ExpandPreOrPostfixIncrementOrDecrementExpression(EmitNodeFactory factory, EmitContext emitContext, Node node, Node expression, Node resultVariable)
    {
        Kind @operator = default;
        Node operand = default;
        if (Ast.IsPrefixUnaryExpression(node))
        {
            @operator = node.AsPrefixUnaryExpression().Operator;
            operand = node.AsPrefixUnaryExpression().Operand;
        }
        else
        {
            @operator = node.AsPostfixUnaryExpression().Operator;
            operand = node.AsPostfixUnaryExpression().Operand;
        }
        var temp = factory.NewTempVariable();
        emitContext.AddVariableDeclaration(temp);
        expression = factory.NewAssignmentExpression(temp, expression);
        expression.Loc = operand.Loc;
        Node operation = default;
        if (Ast.IsPrefixUnaryExpression(node))
        {
            operation = factory.NewPrefixUnaryExpression(@operator, temp);
        }
        else
        {
            operation = factory.NewPostfixUnaryExpression(temp, @operator);
        }
        operation.Loc = node.Loc;
        if (resultVariable != null)
        {
            operation = factory.NewAssignmentExpression(resultVariable, operation);
            operation.Loc = node.Loc;
        }
        expression = factory.NewCommaExpression(expression, operation);
        expression.Loc = node.Loc;
        if (Ast.IsPostfixUnaryExpression(node))
        {
            expression = factory.NewCommaExpression(expression, temp);
            expression.Loc = node.Loc;
        }
        return expression;
    }

}
