// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/transformers/tstransforms/legacydecorators.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using CollectionFunctions = TypeScript.Collections.Collections;
using EmitNodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;
namespace TypeScript.Transformers.TsTransforms;
internal sealed class LegacyDecoratorsTransformer : Transformer
{
    // legacydecorators.go:12. These owners are reset at each source-file boundary.
    private ScriptTarget languageVersion;
    private IReferenceResolver referenceResolver;
    private Dictionary<Node, Node> classAliases;
    private Slice<ClassDeclaration> enclosingClasses;
    private readonly Visitor staticSelfReferenceVisitor;
    private Node staticSelfReferenceClass;
    private readonly Func<Node, bool> metadataDecoratorPredicate;
    private LegacyDecoratorsTransformer()
    {
        staticSelfReferenceVisitor = isOrContainsStaticSelfReference;
        metadataDecoratorPredicate = isSyntheticMetadataDecorator;
    }
    // legacydecorators.go:652. The immutable payload has no pointer identity
    // consumer; value snapshots retain the same two original slice headers.
    private readonly record struct allDecorators(
        Slice<Node> decorators = default, Slice<Slice<Node>> parameters = default);
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:25:1.
    public static Transformer NewLegacyDecoratorsTransformer(TransformOptions opt)
    {
        var tx = new LegacyDecoratorsTransformer { languageVersion = opt.CompilerOptions.GetEmitScriptTarget(), referenceResolver = opt.Resolver };
        return tx.NewTransformer(tx.visit, opt.Context);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:30:1.
    private Node visit(Node node)
    {
        if (((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsDecorators)) == 0) && (GoRuntime.Len(this.enclosingClasses) == 0))
        {
            return node;
        }
        switch (node.Kind)
        {
            case Kind.Identifier:
            {
                return this.visitIdentifier(node.AsIdentifier());
            }
            case Kind.PropertyAccessExpression:
            {
                return this.visitPropertyAccessExpression(node.AsPropertyAccessExpression());
            }
            case Kind.Decorator:
            {
                return null;
            }
            case Kind.ClassDeclaration:
            {
                return this.visitClassDeclaration(node.AsClassDeclaration());
            }
            case Kind.ClassExpression:
            {
                return this.visitClassExpression(node.AsClassExpression());
            }
            case Kind.Constructor:
            {
                return this.visitConstructorDeclaration(node.AsConstructorDeclaration());
            }
            case Kind.MethodDeclaration:
            {
                return this.visitMethodDeclaration(node.AsMethodDeclaration());
            }
            case Kind.SetAccessor:
            {
                return this.visitSetAccessorDeclaration(node.AsSetAccessorDeclaration());
            }
            case Kind.GetAccessor:
            {
                return this.visitGetAccessorDeclaration(node.AsGetAccessorDeclaration());
            }
            case Kind.PropertyDeclaration:
            {
                return this.visitPropertyDeclaration(node.AsPropertyDeclaration());
            }
            case Kind.Parameter:
            {
                return this.visitParamerDeclaration(node.AsParameterDeclaration());
            }
            case Kind.SourceFile:
            {
                this.classAliases = new Dictionary<Node, Node>(ReferenceEqualityComparer.Instance);
                this.enclosingClasses = default;
                var result = this.Visitor().VisitEachChild(node);
                this.EmitContext().AddEmitHelper(result, this.EmitContext().ReadEmitHelpers());
                this.classAliases = null;
                this.enclosingClasses = default;
                return result;
            }
            default:
            {
                return this.Visitor().VisitEachChild(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:73:1.
    private Node visitIdentifier(Identifier node)
    {
        {
            foreach (var d in GoRuntime.RangeValues(this.enclosingClasses))
            {
                {
                    var ok = this.classAliases.TryGetValue(d.AsNode(), out var _2);
                    if (ok && (this.referenceResolver.GetReferencedValueDeclaration(this.EmitContext().MostOriginal(node.AsNode())) == this.EmitContext().MostOriginal(d.AsNode())))
                    {
                        return this.classAliases[d.AsNode()];
                    }
                }
            }
        }
        return node.AsNode();
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:83:1.
    private Node visitPropertyAccessExpression(PropertyAccessExpression node)
    {
        var expression = this.Visitor().VisitNode(node.Expression);
        if (expression != node.Expression)
        {
            return this.Factory().UpdatePropertyAccessExpression(node, expression, node.QuestionDotToken, node.Name(), node.Flags);
        }
        return node.AsNode();
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:94:1.
    private static NodeList elideNodes(EmitNodeFactory f, NodeList nodes)
    {
        if (nodes == null)
        {
            return null;
        }
        if (GoRuntime.Len(nodes.Nodes) == 0)
        {
            return nodes;
        }
        var replacement = f.NewNodeList(Slice<Node>.Make(0));
        replacement.Loc = nodes.Loc;
        return replacement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:106:1.
    private static ModifierList elideModifiers(EmitNodeFactory f, ModifierList nodes)
    {
        if (nodes == null)
        {
            return null;
        }
        if (GoRuntime.Len(nodes.Nodes) == 0)
        {
            return nodes;
        }
        var replacement = f.NewModifierList(Slice<Node>.Make(0));
        replacement.Loc = nodes.Loc;
        return replacement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:118:1.
    private Node finishClassElement(Node updated, Node original)
    {
        if (updated != original)
        {
            this.EmitContext().SetCommentRange(updated, original.Loc);
            this.EmitContext().SetSourceMapRange(updated, Transformers.MoveRangePastModifiers(original));
        }
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:128:1.
    private Node visitParamerDeclaration(ParameterDeclaration node)
    {
        var updated = this.Factory().UpdateParameterDeclaration(
            node,
            elideModifiers(this.Factory(), node.Modifiers()),
            node.DotDotDotToken,
            this.Visitor().VisitNode(node.Name()),
            null,
            null,
            this.Visitor().VisitNode(node.Initializer));
        if (updated != node.AsNode())
        {
            this.EmitContext().SetCommentRange(updated, node.Loc);
            var newLoc = Transformers.MoveRangePastModifiers(node.AsNode());
            updated.Loc = newLoc;
            this.EmitContext().SetSourceMapRange(updated, newLoc);
            this.EmitContext().SetEmitFlags(updated.Name(), EmitFlags.NoTrailingSourceMap);
        }
        return updated;
    }

    // visitPropertyNameOfClassElement visits the property name of a class element,
    // for use when emitting property initializers. For a computed property on a node
    // with decorators, a temporary value is stored for later use.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:153:1.
    private Node visitPropertyNameOfClassElement(Node member)
    {
        var name = member.Name();
        if (Ast.IsComputedPropertyName(name) && Ast.HasDecorators(member))
        {
            var expression = this.Visitor().VisitNode(name.AsComputedPropertyName().Expression);
            var innerExpression = Ast.SkipPartiallyEmittedExpressions(expression);
            if (!Transformers.IsSimpleInlineableExpression(innerExpression))
            {
                var generatedName = this.Factory().NewGeneratedNameForNode(name);
                this.EmitContext().AddVariableDeclaration(generatedName);
                return this.Factory().UpdateComputedPropertyName(name.AsComputedPropertyName(), this.Factory().NewAssignmentExpression(generatedName.AsNode(), expression));
            }
        }
        return this.Visitor().VisitNode(name);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:167:1.
    private Node visitPropertyDeclaration(PropertyDeclaration node)
    {
        if (((node.Flags & NodeFlags.Ambient)) != 0)
        {
            return null;
        }
        if (Ast.HasSyntacticModifier(node.AsNode(), (ModifierFlags.Ambient | ModifierFlags.Abstract)))
        {
            return null;
        }
        return this.finishClassElement(this.Factory().UpdatePropertyDeclaration(
            node,
            this.Visitor().VisitModifiers(node.Modifiers()),
            this.visitPropertyNameOfClassElement(node.AsNode()),
            null,
            null,
            this.Visitor().VisitNode(node.Initializer)), node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:188:1.
    private Node visitGetAccessorDeclaration(GetAccessorDeclaration node)
    {
        return this.finishClassElement(this.Factory().UpdateGetAccessorDeclaration(
            node,
            this.Visitor().VisitModifiers(node.Modifiers()),
            this.visitPropertyNameOfClassElement(node.AsNode()),
            null,
            this.Visitor().VisitNodes(node.Parameters),
            null,
            null,
            this.Visitor().VisitNode(node.Body)), node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:204:1.
    private Node visitSetAccessorDeclaration(SetAccessorDeclaration node)
    {
        return this.finishClassElement(this.Factory().UpdateSetAccessorDeclaration(
            node,
            this.Visitor().VisitModifiers(node.Modifiers()),
            this.visitPropertyNameOfClassElement(node.AsNode()),
            null,
            this.Visitor().VisitNodes(node.Parameters),
            null,
            null,
            this.Visitor().VisitNode(node.Body)), node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:220:1.
    private Node visitMethodDeclaration(MethodDeclaration node)
    {
        return this.finishClassElement(this.Factory().UpdateMethodDeclaration(
            node,
            this.Visitor().VisitModifiers(node.Modifiers()),
            node.AsteriskToken,
            this.visitPropertyNameOfClassElement(node.AsNode()),
            null,
            null,
            this.Visitor().VisitNodes(node.Parameters),
            null,
            null,
            this.Visitor().VisitNode(node.Body)), node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:238:1.
    private Node visitConstructorDeclaration(ConstructorDeclaration node)
    {
        return this.Factory().UpdateConstructorDeclaration(
            node,
            this.Visitor().VisitModifiers(node.Modifiers()),
            null,
            this.Visitor().VisitNodes(node.Parameters),
            null,
            null,
            this.Visitor().VisitNode(node.Body));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:250:1.
    private Node visitClassExpression(ClassExpression node)
    {
        return this.Factory().UpdateClassExpression(
            node,
            this.Visitor().VisitModifiers(node.Modifiers()),
            node.Name(),
            null,
            this.Visitor().VisitNodes(node.HeritageClauses),
            this.Visitor().VisitNodes(node.Members));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:262:1.
    private Node visitClassDeclaration(ClassDeclaration node)
    {
        var decorated = Ast.ClassOrConstructorParameterIsDecorated(true, node.AsNode());
        if (!((decorated || Ast.ChildIsDecorated(true, node.AsNode(), null))))
        {
            return this.Visitor().VisitEachChild(node.AsNode());
        }
        if (decorated)
        {
            return this.transformClassDeclarationWithClassDecorators(node, node.Name());
        }
        return this.transformClassDeclarationWithoutClassDecorators(node, node.Name());
    }

    // Transforms a non-decorated class declaration.
    // @param node A ClassDeclaration node.
    // @param name The name of the class.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:280:1.
    private Node transformClassDeclarationWithoutClassDecorators(ClassDeclaration node, Node name)
    {
        var modifiers = this.Visitor().VisitModifiers(node.Modifiers());
        var heritageClauses = this.Visitor().VisitNodes(node.HeritageClauses);
        var initialMembers = this.Visitor().VisitNodes(node.Members);
        var tuple2 = this.transformDecoratorsOfClassElements(node, initialMembers);
        var members = tuple2.Item1;
        var decorationStatements = tuple2.Item2;
        if ((name == null) && (GoRuntime.Len(decorationStatements) > 0))
        {
            name = this.Factory().NewGeneratedNameForNode(node.AsNode());
        }
        var updated = this.Factory().UpdateClassDeclaration(node, modifiers, name, null, heritageClauses, members);
        if (GoRuntime.Len(decorationStatements) == 0)
        {
            return updated;
        }
        return this.Factory().NewSyntaxList(Slice<Node>.Make(0).Append(updated).Append(decorationStatements));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:308:1.
    private void popEnclosingClass()
    {
        this.enclosingClasses = this.enclosingClasses.Slice2(0, (GoRuntime.Len(this.enclosingClasses) - 1));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:312:1.
    private void pushEnclosingClass(ClassDeclaration cls)
    {
        this.enclosingClasses = this.enclosingClasses.Append(cls);
    }

    // Transforms a decorated class declaration and appends the resulting statements. If
    // the class requires an alias to avoid issues with double-binding, the alias is returned.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:320:1.
    private Node transformClassDeclarationWithClassDecorators(ClassDeclaration node, Node name)
    {
        var isExport = Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export);
        var isDefault = Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Default);
        ModifierList modifiers = default;
        if ((node.Modifiers() != null) && (GoRuntime.Len(node.Modifiers().Nodes) > 0))
        {
            var modifierNodes = Core.Filter(node.Modifiers().Nodes, isNotExportOrDefaultOrDecorator);
            if (GoRuntime.Len(modifierNodes) != GoRuntime.Len(node.Modifiers().Nodes))
            {
                modifiers = this.Factory().NewModifierList(modifierNodes);
                modifiers.Loc = node.Modifiers().Loc;
            }
            else
            {
                modifiers = node.Modifiers();
            }
        }
        var location = Transformers.MoveRangePastModifiers(node.AsNode());
        var classAlias = this.getClassAliasIfNeeded(node);
        if (classAlias != null)
            this.pushEnclosingClass(node);
        try
        {
            var declName = this.Factory().GetLocalNameEx(node.AsNode(), new AssignedNameOptions { AllowComments = false, AllowSourceMaps = true });
            var heritageClauses = this.Visitor().VisitNodes(node.HeritageClauses);
            var members = this.Visitor().VisitNodes(node.Members);
            var tuple3 = this.transformDecoratorsOfClassElements(node, members);
            members = tuple3.Item1;
            var decorationStatements = tuple3.Item2;
            var assignClassAliasInStaticBlock =
                this.languageVersion >= ScriptTarget.ES2022 && classAlias != null &&
                members != null && members.Nodes.Length > 0 &&
                Core.Some(members.Nodes, isClassStaticBlockDeclarationOrStaticProperty);
            if (assignClassAliasInStaticBlock)
            {
                var memberList = Slice<Node>.Make(0);
                memberList = memberList.Append(this.Factory().NewClassStaticBlockDeclaration(
                    null,
                    this.Factory().NewBlock(
                        this.Factory().NewNodeList(Slice<Node>.Make(0).Append(
                            this.Factory().NewExpressionStatement(this.Factory().NewAssignmentExpression(
                                classAlias, this.Factory().NewKeywordExpression(Kind.ThisKeyword))))),
                        false)));
                memberList = memberList.Append(members.Nodes);
                var newList = this.Factory().NewNodeList(memberList);
                newList.Loc = members.Loc;
                members = newList;
            }
            var exprName = name;
            if ((name != null) && Transformers.IsGeneratedIdentifier(this.EmitContext(), name))
            {
                exprName = null;
            }
            var classExpression = this.Factory().NewClassExpression(modifiers, exprName, null, heritageClauses, members);
            this.EmitContext().SetOriginal(classExpression, node.AsNode());
            classExpression.Loc = location;
            var varInitializer = classExpression;
            if ((classAlias != null) && !assignClassAliasInStaticBlock)
            {
                varInitializer = this.Factory().NewAssignmentExpression(classAlias, classExpression);
            }
            var varDecl = this.Factory().NewVariableDeclaration(declName, null, null, varInitializer);
            this.EmitContext().SetOriginal(varDecl, node.AsNode());
            var varDeclList = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.Make(0).Append(varDecl)), NodeFlags.Let);
            var varStatement = this.Factory().NewVariableStatement(null, varDeclList);
            this.EmitContext().SetOriginal(varStatement, node.AsNode());
            varStatement.Loc = location;
            this.EmitContext().SetCommentRange(varStatement, node.Loc);
            var statements = Slice<Node>.Make(0).Append(varStatement);
            statements = statements.Append(decorationStatements);
            statements = statements.Append(this.getConstructorDecorationStatement(node));
            if (isExport)
            {
                Node exportStatement = default;
                if (isDefault)
                {
                    exportStatement = this.Factory().NewExportDefault(declName);
                }
                else
                {
                    exportStatement = this.Factory().NewExternalModuleExport(this.Factory().GetDeclarationName(node.AsNode()));
                }
                statements = statements.Append(exportStatement);
            }
            if (GoRuntime.Len(statements) == 1)
            {
                return statements[0];
            }
            return this.Factory().NewSyntaxList(statements);
        }
        finally
        {
            if (classAlias != null)
                this.popEnclosingClass();
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:512:1.
    private bool hasInternalStaticReference(ClassDeclaration node)
    {
        var oldClass = staticSelfReferenceClass;
        staticSelfReferenceClass = this.EmitContext().MostOriginal(node.AsNode());
        try
        {
            foreach (var member in node.Members.Nodes)
                if (member.ForEachChild(staticSelfReferenceVisitor))
                    return true;
            return false;
        }
        finally
        {
            staticSelfReferenceClass = oldClass;
        }
    }

    // legacydecorators.go:515. A cached visitor retains the original synchronous
    // closure body; its class comparison target is scoped by the caller above.
    private bool isOrContainsStaticSelfReference(Node node)
    {
        if (Ast.IsIdentifier(node) &&
            referenceResolver.GetReferencedValueDeclaration(EmitContext().MostOriginal(node)) == staticSelfReferenceClass)
            return true;
        // A property-access name is not a reference to the class value.
        if (Ast.IsPropertyAccessExpression(node))
            return isOrContainsStaticSelfReference(node.Expression());
        return node.ForEachChild(staticSelfReferenceVisitor);
    }

    // Gets a local alias for a class declaration if it is a decorated class with an internal
    // reference to the static side of the class. This is necessary to avoid issues with
    // double-binding semantics for the class name.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:539:1.
    private Node getClassAliasIfNeeded(ClassDeclaration node)
    {
        if (!this.hasInternalStaticReference(node))
        {
            return null;
        }
        var nameText = ByteStringValue.FromString("default");
        if ((node.Name() != null) && !Transformers.IsGeneratedIdentifier(this.EmitContext(), node.Name()))
        {
            nameText = node.Name().TextValue();
        }
        var classAlias = this.Factory().NewUniqueName(nameText);
        this.EmitContext().AddVariableDeclaration(classAlias);
        this.classAliases[node.AsNode()] = classAlias;
        return classAlias;
    }

    // Generates a __decorate helper call for a class constructor.
    // @param node The class node.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:560:1.
    private Node getConstructorDecorationStatement(ClassDeclaration node)
    {
        var expression = this.generateConstructorDecorationExpression(node);
        if (expression != null)
        {
            var result = this.Factory().NewExpressionStatement(expression);
            this.EmitContext().SetOriginal(result, node.AsNode());
            return result;
        }
        return null;
    }

    // Generates a __decorate helper call for a class constructor.
    // @param node The class node.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:575:1.
    private Node generateConstructorDecorationExpression(ClassDeclaration node)
    {
        var allDecorators = getAllDecoratorsOfClass(node, true);
        var hasAlias = ((GoRuntime.Len(this.enclosingClasses) > 0) && (this.enclosingClasses[(GoRuntime.Len(this.enclosingClasses) - 1)] == node));
        if (hasAlias)
        {
            this.popEnclosingClass();
        }
        var decoratorExpressions = this.transformAllDecoratorsOfDeclaration(allDecorators);
        if (hasAlias)
        {
            this.pushEnclosingClass(node);
        }
        if (GoRuntime.Len(decoratorExpressions) == 0)
        {
            return null;
        }
        Node classAlias = default;
        if (this.classAliases != null)
        {
            this.classAliases.TryGetValue(node.AsNode(), out classAlias);
        }
        var localName = this.Factory().GetDeclarationNameEx(node.AsNode(), new NameOptions { AllowComments = false, AllowSourceMaps = true });
        var decorate = this.Factory().NewDecorateHelper(decoratorExpressions, localName, null, null);
        var assignmentTarget = decorate;
        if (classAlias != null)
        {
            assignmentTarget = this.Factory().NewAssignmentExpression(classAlias, decorate);
        }
        var expression = this.Factory().NewAssignmentExpression(localName, assignmentTarget);
        this.EmitContext().SetEmitFlags(expression, EmitFlags.NoComments);
        this.EmitContext().SetSourceMapRange(expression, Transformers.MoveRangePastModifiers(node.AsNode()));
        return expression;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:614:1.
    private static bool isClassStaticBlockDeclarationOrStaticProperty(Node node)
    {
        return (Ast.IsClassStaticBlockDeclaration(node) || ((Ast.IsPropertyDeclaration(node) && Ast.HasStaticModifier(node))));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:618:1.
    private static bool isNotExportOrDefaultOrDecorator(Node node)
    {
        return !(((Ast.IsDecorator(node) || (node.Kind == Kind.ExportKeyword)) || (node.Kind == Kind.DefaultKeyword)));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:622:1.
    private static bool decoratorContainsPrivateIdentifierInExpression(Node decorator)
    {
        return ((((uint)decorator.SubtreeFacts() & SubtreeFactValues.ContainsPrivateIdentifierInExpression)) != 0);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:626:1.
    private static bool parameterDecoratorsContainPrivateIdentifierInExpression(Slice<Node> parameterDecorators)
    {
        return Core.Some(parameterDecorators, decoratorContainsPrivateIdentifierInExpression);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:630:1.
    private static bool hasClassElementWithDecoratorContainingPrivateIdentifierInExpression(ClassDeclaration node)
    {
        if ((node.Members == null) || (GoRuntime.Len(node.Members.Nodes) == 0))
        {
            return false;
        }
        {
            foreach (var member in GoRuntime.RangeValues(node.Members.Nodes))
            {
                if (!Ast.CanHaveDecorators(member))
                {
                    continue;
                }
                var allDecorators = getAllDecoratorsOfClassElement(member, node, true);
                if (allDecorators == null)
                {
                    continue;
                }
                if (Core.Some(allDecorators.Value.decorators, decoratorContainsPrivateIdentifierInExpression))
                {
                    return true;
                }
                if (Core.Some(allDecorators.Value.parameters, parameterDecoratorsContainPrivateIdentifierInExpression))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Gets an allDecorators object containing the decorators for the class and the decorators for the
    // parameters of the constructor of the class.
    // @param node The class node.
    // @internal
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:665:1.
    private static allDecorators? getAllDecoratorsOfClass(ClassDeclaration node, bool useLegacyDecorators)
    {
        var decorators = node.Decorators();
        Slice<Slice<Node>> parameters = default;
        if (useLegacyDecorators)
        {
            parameters = GetDecoratorsOfParameters(Ast.GetFirstConstructorWithBody(node.AsNode()));
        }
        if ((GoRuntime.Len(decorators) == 0) && (GoRuntime.Len(parameters) == 0))
        {
            return null;
        }
        return new allDecorators { decorators = decorators, parameters = parameters };
    }

    // Gets an allDecorators object containing the decorators for the member and its parameters.
    // @param parent The class node that contains the member.
    // @param member The class member.
    // @internal
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:685:1.
    private static allDecorators? getAllDecoratorsOfClassElement(Node member, ClassDeclaration parent, bool useLegacyDecorators)
    {
        switch (member.Kind)
        {
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            {
                if (!useLegacyDecorators)
                {
                    return getAllDecoratorsOfMethod(member, false);
                }
                return getAllDecoratorsOfAccessors(member, parent, true);
            }
            case Kind.MethodDeclaration:
            {
                return getAllDecoratorsOfMethod(member, useLegacyDecorators);
            }
            case Kind.PropertyDeclaration:
            {
                return getAllDecoratorsOfProperty(member);
            }
            default:
            {
                return null;
            }
        }
    }

    // Gets an allDecorators object containing the decorators for the accessor and its parameters.
    // @param parent The class node that contains the accessor.
    // @param accessor The class accessor member.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:707:1.
    private static allDecorators? getAllDecoratorsOfAccessors(Node accessor, ClassDeclaration parent, bool useLegacyDecorators)
    {
        if (accessor.Body() == null)
        {
            return null;
        }
        var decls = Ast.GetAllAccessorDeclarations(parent.Members.Nodes, accessor);
        Node firstAccessorWithDecorators = default;
        if (Ast.HasDecorators(decls.FirstAccessor))
        {
            firstAccessorWithDecorators = decls.FirstAccessor;
        }
        else
        {
            if ((decls.SecondAccessor != null) && Ast.HasDecorators(decls.SecondAccessor))
            {
                firstAccessorWithDecorators = decls.SecondAccessor;
            }
        }
        if ((firstAccessorWithDecorators == null) || (accessor != firstAccessorWithDecorators))
        {
            return null;
        }
        var decorators = firstAccessorWithDecorators.Decorators();
        Slice<Slice<Node>> parameters = default;
        if (useLegacyDecorators && (decls.SetAccessor != null))
        {
            parameters = GetDecoratorsOfParameters(decls.SetAccessor.AsNode());
        }
        if ((GoRuntime.Len(decorators) == 0) && (GoRuntime.Len(parameters) == 0))
        {
            return null;
        }
        return new allDecorators { decorators = decorators, parameters = parameters };
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:739:1.
    private static allDecorators? getAllDecoratorsOfProperty(Node property)
    {
        var decorators = property.Decorators();
        if (GoRuntime.Len(decorators) == 0)
        {
            return null;
        }
        return new allDecorators { decorators = decorators };
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:747:1.
    private static allDecorators? getAllDecoratorsOfMethod(Node method, bool useLegacyDecorators)
    {
        if (method.Body() == null)
        {
            return null;
        }
        var decorators = method.Decorators();
        Slice<Slice<Node>> parameters = default;
        if (useLegacyDecorators)
        {
            parameters = GetDecoratorsOfParameters(method);
        }
        if ((GoRuntime.Len(decorators) == 0) && (GoRuntime.Len(parameters) == 0))
        {
            return null;
        }
        return new allDecorators { decorators = decorators, parameters = parameters };
    }

    // Gets an array of arrays of decorators for the parameters of a function-like node.
    // The offset into the result array should correspond to the offset of the parameter.
    // @param node The function-like node.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:768:1.
    internal static Slice<Slice<Node>> GetDecoratorsOfParameters(Node node)
    {
        Slice<Slice<Node>> decorators = default;
        if (node != null)
        {
            var parameters = node.Parameters();
            var firstParameterIsThis = ((GoRuntime.Len(parameters) > 0) && Ast.IsThisParameter(parameters[0]));
            var firstParameterOffset = 0;
            var numParameters = GoRuntime.Len(parameters);
            if (firstParameterIsThis)
            {
                firstParameterOffset = 1;
                numParameters = (numParameters - 1);
            }
            {
                for (int i = 0; i < numParameters; i++)
                {
                    var p = parameters[(i + firstParameterOffset)];
                    if ((GoRuntime.Len(decorators) > 0) || Ast.HasDecorators(p))
                    {
                        if (GoRuntime.Len(decorators) == 0)
                        {
                            decorators = Slice<Slice<Node>>.Make(numParameters);
                        }
                        decorators[i] = p.Decorators();
                    }
                }
            }
        }
        return decorators;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:793:1.
    private (NodeList, Slice<Node>) transformDecoratorsOfClassElements(ClassDeclaration node, NodeList members)
    {
        Slice<Node> decorationStatements = default;
        decorationStatements = decorationStatements.Append(this.getClassElementDecorationStatements(node, false));
        decorationStatements = decorationStatements.Append(this.getClassElementDecorationStatements(node, true));
        if (hasClassElementWithDecoratorContainingPrivateIdentifierInExpression(node))
        {
            Slice<Node> memberNodes = default;
            if ((members != null) && (GoRuntime.Len(members.Nodes) > 0))
            {
                memberNodes = members.Nodes;
            }
            members = this.Factory().NewNodeList(
                Slice<Node>.Make(0).Append(memberNodes).Append(
                    this.Factory().NewClassStaticBlockDeclaration(null,
                        this.Factory().NewBlock(this.Factory().NewNodeList(decorationStatements), true))));
            decorationStatements = default;
        }
        return (members, decorationStatements);
    }

    // Generates statements used to apply decorators to either the static or instance members
    // of a class.
    // @param node The class node.
    // @param isStatic A value indicating whether to generate statements for static or
    //                 instance members.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:822:1.
    private Slice<Node> getClassElementDecorationStatements(ClassDeclaration node, bool isStatic)
    {
        var exprs = this.generateClassElementDecorationExpressions(node, isStatic);
        Slice<Node> statements = default;
        {
            foreach (var e in GoRuntime.RangeValues(exprs))
            {
                statements = statements.Append(this.Factory().NewExpressionStatement(e));
            }
        }
        return statements;
    }

    // Determines whether a class member is either a static or an instance member of a class
    // that is decorated, or has parameters that are decorated.
    // @param member The class member.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:837:1.
    private static bool isDecoratedClassElement(Node member, bool isStaticElement, ClassDeclaration parent)
    {
        return ((isStaticElement == Ast.IsStatic(member)) && Ast.NodeOrChildIsDecorated(true, member, parent.AsNode(), null));
    }

    // Gets either the static or instance members of a class that are decorated, or have
    // parameters that are decorated.
    // @param node The class containing the member.
    // @param isStatic A value indicating whether to retrieve static or instance members of
    //                 the class.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:849:1.
    private static Slice<Node> getDecoratedClassElements(ClassDeclaration node, bool isStatic)
    {
        if ((node.Members == null) || (GoRuntime.Len(node.Members.Nodes) == 0))
        {
            return default;
        }
        Slice<Node> members = default;
        {
            foreach (var member in GoRuntime.RangeValues(node.Members.Nodes))
            {
                if (isDecoratedClassElement(member, isStatic, node))
                {
                    members = members.Append(member);
                }
            }
        }
        return members;
    }

    // Generates expressions used to apply decorators to either the static or instance members
    // of a class.
    // @param node The class node.
    // @param isStatic A value indicating whether to generate expressions for static or
    //                 instance members.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:870:1.
    private Slice<Node> generateClassElementDecorationExpressions(ClassDeclaration node, bool isStatic)
    {
        var members = getDecoratedClassElements(node, isStatic);
        Slice<Node> expressions = default;
        {
            foreach (var member in GoRuntime.RangeValues(members))
            {
                var expr = this.generateClassElementDecorationExpression(node, member);
                if (expr != null)
                {
                    expressions = expressions.Append(expr);
                }
            }
        }
        return expressions;
    }

    // Generates an expression used to evaluate class element decorators at runtime.
    // @param node The class node that contains the member.
    // @param member The class member.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:888:1.
    private Node generateClassElementDecorationExpression(ClassDeclaration node, Node member)
    {
        var allDecorators = getAllDecoratorsOfClassElement(member, node, true);
        var decoratorExpressions = this.transformAllDecoratorsOfDeclaration(allDecorators);
        if (GoRuntime.Len(decoratorExpressions) == 0)
        {
            return null;
        }
        var prefix = this.getClassMemberPrefix(node, member);
        var memberName = this.getExpressionForPropertyName(member, ((member.Flags & NodeFlags.Ambient) == 0));
        Node descriptor = default;
        if (Ast.IsPropertyDeclaration(member) && !Ast.HasAccessorModifier(member))
        {
            descriptor = this.Factory().NewVoidZeroExpression();
        }
        else
        {
            descriptor = this.Factory().NewKeywordExpression(Kind.NullKeyword);
        }
        var helper = this.Factory().NewDecorateHelper(decoratorExpressions, prefix, memberName, descriptor);
        this.EmitContext().SetEmitFlags(helper, EmitFlags.NoComments);
        this.EmitContext().SetSourceMapRange(helper, Transformers.MoveRangePastModifiers(member));
        return helper;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:951:1.
    private bool isSyntheticMetadataDecorator(Node node)
    {
        return this.EmitContext().IsCallToHelper(node.Expression(), "__metadata");
    }

    // Transforms all of the decorators for a declaration into an array of expressions.
    // @param allDecorators An object containing all of the decorators for the declaration.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:960:1.
    private Slice<Node> transformAllDecoratorsOfDeclaration(allDecorators? allDecorators)
    {
        if (allDecorators == null)
        {
            return default;
        }
        var mm = CollectionFunctions.GroupBy(allDecorators.Value.decorators, metadataDecoratorPredicate);
        var metadata = mm.Get(true);
        var decorators = mm.Get(false);
        Slice<Node> decoratorExpressions = default;
        decoratorExpressions = decoratorExpressions.Append(this.transformDecorators(decorators));
        decoratorExpressions = decoratorExpressions.Append(this.transformDecoratorsOfParameters(allDecorators.Value.parameters));
        decoratorExpressions = decoratorExpressions.Append(this.transformDecorators(metadata));
        return decoratorExpressions;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:977:1.
    private Slice<Node> transformDecoratorsOfParameters(Slice<Slice<Node>> parameters)
    {
        Slice<Node> results = default;
        {
            foreach (var range10 in GoRuntime.Range(parameters))
            {
                var i = range10.Key;
                var decorators = range10.Value;
                if (GoRuntime.Len(decorators) > 0)
                {
                    {
                        foreach (var decorator in GoRuntime.RangeValues(decorators))
                        {
                            var helper = this.Factory().NewParamHelper(this.Visitor().VisitNode(decorator.Expression()), i, decorator.Expression().Loc);
                            this.EmitContext().SetEmitFlags(helper, EmitFlags.NoComments);
                            results = results.Append(helper);
                        }
                    }
                }
            }
        }
        return results;
    }

    // Transforms a list of decorators into an expression.
    // @param decorator The decorator node.
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:1000:1.
    private Slice<Node> transformDecorators(Slice<Node> decorators)
    {
        Slice<Node> results = default;
        {
            foreach (var d in GoRuntime.RangeValues(decorators))
            {
                results = results.Append(this.Visitor().VisitNode(d.Expression()));
            }
        }
        return results;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:1008:1.
    private Node getClassMemberPrefix(ClassDeclaration node, Node member)
    {
        if (Ast.IsStatic(member))
        {
            return this.Factory().GetDeclarationName(node.AsNode());
        }
        return this.getClassPrototype(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:1015:1.
    private Node getClassPrototype(ClassDeclaration node)
    {
        return this.Factory().NewPropertyAccessExpression(this.Factory().GetDeclarationName(node.AsNode()), null, this.Factory().NewIdentifier("prototype"), NodeFlags.None);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go:1024:1.
    private Node getExpressionForPropertyName(Node member, bool generateNameForComputedPropertyName)
    {
        var name = member.Name();
        if (Ast.IsPrivateIdentifier(name))
        {
            return this.Factory().NewIdentifier("");
        }
        else
        {
            if (Ast.IsComputedPropertyName(name))
            {
                if (generateNameForComputedPropertyName && !Transformers.IsSimpleInlineableExpression(name.AsComputedPropertyName().Expression))
                {
                    return this.Factory().NewGeneratedNameForNode(name);
                }
                return name.AsComputedPropertyName().Expression;
            }
            else
            {
                if (Ast.IsIdentifier(name))
                {
                    return this.Factory().NewStringLiteralWithTextValue(name.TextValue(), TokenFlags.None);
                }
                else
                {
                    return this.Factory().DeepCloneNode(name);
                }
            }
        }
    }

}
