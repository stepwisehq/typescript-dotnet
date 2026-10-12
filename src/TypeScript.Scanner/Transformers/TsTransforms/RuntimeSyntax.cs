// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of tstransforms/runtimesyntax.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace TypeScript.Transformers.TsTransforms;
internal sealed class RuntimeSyntaxTransformer : Transformer
{
private CompilerOptions compilerOptions;
private Node parentNode, currentNode, currentSourceFile, currentScope, currentEnum, currentNamespace;
private Dictionary<ByteStringKey, Node> currentScopeFirstDeclarationsOfName;
private IReferenceResolver resolver;
private IEmitResolver emitResolver;
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:31:1.
public static Transformer NewRuntimeSyntaxTransformer(TransformOptions opt)
{
    var compilerOptions = opt.CompilerOptions;
    var emitContext = opt.Context;
    var tx = new RuntimeSyntaxTransformer { compilerOptions = compilerOptions, resolver = opt.Resolver, emitResolver = opt.EmitResolver };
    return tx.NewTransformer(tx.visit, emitContext);
}

// Pushes a new child node onto the ancestor tracking stack, returning the grandparent node to be restored later via `popNode`.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:39:1.
private Node pushNode(Node node)
{
    Node grandparentNode = default;
    grandparentNode = this.parentNode;
    this.parentNode = this.currentNode;
    this.currentNode = node;
    return grandparentNode;
}

// Pops the last child node off the ancestor tracking stack, restoring the grandparent node.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:47:1.
private void popNode(Node grandparentNode)
{
    this.currentNode = this.parentNode;
    this.parentNode = grandparentNode;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:52:1.
private (Node, Dictionary<ByteStringKey, Node>) pushScope(Node node)
{
    Node savedCurrentScope = default;
    Dictionary<ByteStringKey, Node> savedCurrentScopeFirstDeclarationsOfName = default;
    savedCurrentScope = this.currentScope;
    savedCurrentScopeFirstDeclarationsOfName = this.currentScopeFirstDeclarationsOfName;
    switch (node.Kind)
    {
        case Kind.SourceFile:
        {
            this.currentScope = node;
            this.currentSourceFile = node;
            this.currentScopeFirstDeclarationsOfName = null;
            break;
        }
        case Kind.CaseBlock:
        case Kind.ModuleBlock:
        case Kind.Block:
        {
            this.currentScope = node;
            this.currentScopeFirstDeclarationsOfName = null;
            break;
        }
        case Kind.FunctionDeclaration:
        case Kind.ClassDeclaration:
        case Kind.VariableStatement:
        {
            this.recordDeclarationInScope(node);
            break;
        }
    }
    return (savedCurrentScope, savedCurrentScopeFirstDeclarationsOfName);
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:69:1.
private void popScope(Node savedCurrentScope, Dictionary<ByteStringKey, Node> savedCurrentScopeFirstDeclarationsOfName)
{
    if ((this.currentScope != savedCurrentScope))
    {
        this.currentScopeFirstDeclarationsOfName = savedCurrentScopeFirstDeclarationsOfName;
    }
    this.currentScope = savedCurrentScope;
}

// Visits each node in the AST
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:79:1.
private Node visit(Node node)
{
    var grandparentNode = this.pushNode(node);
    
    var tuple1 = this.pushScope(node); var savedCurrentScope = tuple1.Item1; var savedCurrentScopeFirstDeclarationsOfName = tuple1.Item2;
    try
    {
    if (((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsTypeScript) == 0) && ((((this.currentNamespace == null) && (this.currentEnum == null)) || (((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsIdentifier) == 0)))))
    {
        return node;
    }
    switch (node.Kind)
    {
        case Kind.PublicKeyword:
        case Kind.PrivateKeyword:
        case Kind.ProtectedKeyword:
        case Kind.ReadonlyKeyword:
        case Kind.OverrideKeyword:
        {
            node = null;
            break;
        }
        case Kind.EnumDeclaration:
        {
            node = this.visitEnumDeclaration(node.AsEnumDeclaration());
            break;
        }
        case Kind.ModuleDeclaration:
        {
            node = this.visitModuleDeclaration(node.AsModuleDeclaration());
            break;
        }
        case Kind.ClassDeclaration:
        {
            node = this.visitClassDeclaration(node.AsClassDeclaration());
            break;
        }
        case Kind.ClassExpression:
        {
            node = this.visitClassExpression(node.AsClassExpression());
            break;
        }
        case Kind.Constructor:
        {
            node = this.visitConstructorDeclaration(node.AsConstructorDeclaration());
            break;
        }
        case Kind.FunctionDeclaration:
        {
            node = this.visitFunctionDeclaration(node.AsFunctionDeclaration());
            break;
        }
        case Kind.VariableStatement:
        {
            node = this.visitVariableStatement(node.AsVariableStatement());
            break;
        }
        case Kind.ExportDeclaration:
        case Kind.ImportDeclaration:
        case Kind.ImportClause:
        {
            if ((((this.currentNamespace != null) && (this.currentScope != null)) && (this.currentScope.Kind != Kind.Block)))
            {
                node = null;
            }
            else
            {
                node = this.Visitor().VisitEachChild(node);
            }
            break;
        }
        case Kind.ImportEqualsDeclaration:
        {
            if (((((this.currentNamespace != null) && (this.currentScope != null)) && (this.currentScope.Kind != Kind.Block)) && (node.AsImportEqualsDeclaration().ModuleReference.Kind == Kind.ExternalModuleReference)))
            {
                node = null;
            }
            else
            {
                if (((((this.currentNamespace != null) && (this.currentScope != null)) && (this.currentScope.Kind == Kind.Block)) && (node.AsImportEqualsDeclaration().ModuleReference.Kind != Kind.ExternalModuleReference)))
                {
                    node = null;
                }
                else
                {
                    node = this.visitImportEqualsDeclaration(node.AsImportEqualsDeclaration());
                }
            }
            break;
        }
        case Kind.Identifier:
        {
            node = this.visitIdentifier(node);
            break;
        }
        case Kind.ShorthandPropertyAssignment:
        {
            node = this.visitShorthandPropertyAssignment(node.AsShorthandPropertyAssignment());
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
    finally
    {
        this.popScope(savedCurrentScope, savedCurrentScopeFirstDeclarationsOfName);
        this.popNode(grandparentNode);
    }
}

// Records that a declaration was emitted in the current scope, if it was the first declaration for the provided symbol.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:140:1.
private void recordDeclarationInScope(Node node)
{
    switch (node.Kind)
    {
        case Kind.VariableStatement:
        {
            this.recordDeclarationInScope(node.AsVariableStatement().DeclarationList);
            return ;
        }
        case Kind.VariableDeclarationList:
        {
            {
                foreach (var decl in GoRuntime.RangeValues(node.AsVariableDeclarationList().Declarations.Nodes))
                {
                    this.recordDeclarationInScope(decl);
                }
            }
            return ;
        }
        case Kind.ArrayBindingPattern:
        case Kind.ObjectBindingPattern:
        {
            {
                foreach (var range3 in GoRuntime.Range(node.Elements()))
                {
                    var _2 = range3.Key;
                    var element = range3.Value;
                    this.recordDeclarationInScope(element);
                }
            }
            return ;
        }
    }
    var name = node.Name();
    if ((name != null))
    {
        if (Ast.IsIdentifier(name))
        {
            if ((this.currentScopeFirstDeclarationsOfName == null))
            {
                this.currentScopeFirstDeclarationsOfName = new Dictionary<ByteStringKey, Node>(ByteStringKey.Comparer);
            }
            var text = name.TextValue();
            {
                var found = text.TryGetValue(this.currentScopeFirstDeclarationsOfName, out var _3);
                if (!found)
                {
                    this.currentScopeFirstDeclarationsOfName[ByteStringKey.FromValue(text)] = node;
                }
            }
        }
        else
        {
            if (Ast.IsBindingPattern(name))
            {
                this.recordDeclarationInScope(name);
            }
        }
    }
}

// Determines whether a declaration is the first declaration with the same name emitted in the current scope.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:173:1.
private bool isFirstDeclarationInScope(Node node)
{
    var name = node.Name();
    if (((name != null) && Ast.IsIdentifier(name)))
    {
        var text = name.TextValue();
        {
            Node firstDeclaration = null;
            var found = this.currentScopeFirstDeclarationsOfName != null && text.TryGetValue(this.currentScopeFirstDeclarationsOfName, out firstDeclaration);
            if (found)
            {
                return (firstDeclaration == node);
            }
        }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:184:1.
private bool isExportOfNamespace(Node node)
{
    return (((this.currentNamespace != null) && (((this.currentScope == null) || (this.currentScope.Kind != Kind.Block)))) && ((node.ModifierFlags() & ModifierFlags.Export) != 0));
}

// Gets an expression that represents a property name, such as `"foo"` for the identifier `foo`.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:189:1.
private Node getExpressionForPropertyName(EnumMember member)
{
    var name = member.Name();
    switch (name.Kind)
    {
        case Kind.PrivateIdentifier:
        {
            return this.Factory().NewIdentifier("");
        }
        case Kind.ComputedPropertyName:
        {
            var n = name.AsComputedPropertyName();
            return this.Visitor().VisitNode(n.Expression);
        }
        case Kind.Identifier:
        {
            return this.Factory().NewStringLiteralWithTextValue(name.TextValue(), TokenFlags.None);
        }
        case Kind.StringLiteral:
        {
            return this.Factory().NewStringLiteralWithTextValue(name.TextValue(), TokenFlags.None);
        }
        case Kind.NumericLiteral:
        {
            return this.Factory().NewNumericLiteralWithTextValue(name.TextValue(), TokenFlags.None);
        }
        default:
        {
            return name;
        }
    }
}

// Gets an expression like `E["A"]` that references an enum member.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:210:1.
private Node getEnumQualifiedElement(EnumDeclaration @enum, EnumMember member)
{
    var prop = this.getNamespaceQualifiedElement(this.getNamespaceContainerName(@enum.AsNode()), this.getExpressionForPropertyName(member));
    this.EmitContext().AddEmitFlags(prop, (((EmitFlags.NoComments | EmitFlags.NoNestedComments) | EmitFlags.NoSourceMap) | EmitFlags.NoNestedSourceMaps));
    return prop;
}

// Gets an expression used to refer to a namespace or @enum from within the body of its declaration.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:217:1.
private Node getNamespaceContainerName(Node node)
{
    return this.Factory().NewGeneratedNameForNode(node);
}

// Gets an expression used to refer to an export of a namespace or a member of an enum by property name.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:222:1.
private Node getNamespaceQualifiedProperty(Node ns, Node name)
{
    return this.Factory().GetNamespaceMemberName(ns, name, new NameOptions { AllowSourceMaps = true });
}

// Gets an expression used to refer to an export of a namespace or a member of an enum by indexed access.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:227:1.
private Node getNamespaceQualifiedElement(Node ns, Node expression)
{
    var qualifiedName = this.EmitContext().Factory.NewElementAccessExpression(ns, null, expression, NodeFlags.None);
    this.EmitContext().AssignCommentAndSourceMapRanges(qualifiedName, expression);
    return qualifiedName;
}

// Gets an expression used within the provided node's container for any exported references.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:234:1.
private Node getExportQualifiedReferenceToDeclaration(Node node)
{
    if (this.isExportOfNamespace(node.AsNode()))
    {
        return this.Factory().GetExternalModuleOrNamespaceExportName(this.getNamespaceContainerName(this.currentNamespace), node, false, true);
    }
    return this.Factory().GetDeclarationNameEx(node.AsNode(), new NameOptions { AllowSourceMaps = true });
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:241:1.
private (Slice<Node>, bool) addVarForDeclaration(Slice<Node> statements, Node node)
{
    this.recordDeclarationInScope(node);
    if (!this.isFirstDeclarationInScope(node))
    {
        return (statements, false);
    }
    var name = this.Factory().GetLocalNameEx(node, new AssignedNameOptions { AllowSourceMaps = true });
    var varDecl = this.Factory().NewVariableDeclaration(name, null, null, null);
    var varFlags = Core.IfElse((this.currentScope == this.currentSourceFile), NodeFlags.None, NodeFlags.Let);
    var varDecls = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([varDecl])), varFlags);
    var modifierMask = ~((ModifierFlags.TypeScriptModifier | ModifierFlags.Decorator));
    if ((this.currentNamespace != null))
    {
        modifierMask &= ~(ModifierFlags.Export);
    }
    var modifiers = Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), modifierMask);
    var varStatement = this.Factory().NewVariableStatement(modifiers, varDecls);
    this.EmitContext().SetOriginal(varDecl, node);
    this.EmitContext().SetOriginal(varStatement, node);
    if (Ast.IsEnumDeclaration(node))
    {
        this.EmitContext().SetSourceMapRange(varDecls, node.Loc);
    }
    else
    {
        this.EmitContext().SetSourceMapRange(varStatement, node.Loc);
    }
    this.EmitContext().SetCommentRange(varStatement, node.Loc);
    this.EmitContext().AddEmitFlags(varStatement, EmitFlags.NoTrailingComments);
    statements = statements.Append(varStatement);
    return (statements, true);
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:294:1.
private Node visitEnumDeclaration(EnumDeclaration node)
{
    if (!this.shouldEmitEnumDeclaration(node))
    {
        return this.EmitContext().NewNotEmittedStatement(node.AsNode());
    }
    var statements = Slice<Node>.Make(0);
    var tuple4 = this.addVarForDeclaration(statements, node.AsNode()); statements = tuple4.Item1; var varAdded = tuple4.Item2;
    var emitFlags = EmitFlags.None;
    if ((varAdded && (((this.compilerOptions.GetEmitModuleKind() != ModuleKind.System) || (this.currentScope != this.currentSourceFile)))))
    {
        emitFlags |= EmitFlags.NoLeadingComments;
    }
    var enumArg = this.Factory().NewLogicalORExpression(this.getExportQualifiedReferenceToDeclaration(node.AsNode()), this.Factory().NewAssignmentExpression(this.getExportQualifiedReferenceToDeclaration(node.AsNode()), this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(Slice<Node>.Make(0)), false)));
    if (this.isExportOfNamespace(node.AsNode()))
    {
        var localName = this.Factory().GetLocalNameEx(node.AsNode(), new AssignedNameOptions { AllowSourceMaps = true });
        enumArg = this.Factory().NewAssignmentExpression(localName, enumArg);
    }
    var enumParamName = this.Factory().NewGeneratedNameForNode(node.AsNode());
    this.EmitContext().SetSourceMapRange(enumParamName, node.Name().Loc);
    var enumParam = this.Factory().NewParameterDeclaration(null, null, enumParamName, null, null, null);
    var enumBody = this.transformEnumBody(node);
    var enumFunc = this.Factory().NewFunctionExpression(null, null, null, null, this.Factory().NewNodeList(Slice<Node>.FromArray([enumParam])), null, null, enumBody);
    var enumCall = this.Factory().NewCallExpression(this.Factory().NewParenthesizedExpression(enumFunc), null, null, this.Factory().NewNodeList(Slice<Node>.FromArray([enumArg])), NodeFlags.None);
    var enumStatement = this.Factory().NewExpressionStatement(enumCall);
    this.EmitContext().SetOriginal(enumStatement, node.AsNode());
    this.EmitContext().AssignCommentAndSourceMapRanges(enumStatement, node.AsNode());
    this.EmitContext().AddEmitFlags(enumStatement, emitFlags);
    return this.Factory().NewSyntaxList(statements.Append(enumStatement));
}

// Transforms the body of an enum declaration.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:346:1.
private Node transformEnumBody(EnumDeclaration node)
{
    var savedCurrentEnum = this.currentEnum;
    this.currentEnum = node.AsNode();
    node = this.Visitor().VisitEachChild(node.AsNode()).AsEnumDeclaration();
    var statements = Slice<Node>.Make(0);
    {
        foreach (var i in GoRuntime.RangeKeys(GoRuntime.Len(node.Members.Nodes)))
        {
            statements = this.transformEnumMember(statements, node, i);
        }
    }
    var statementList = this.Factory().NewNodeList(statements);
    statementList.Loc = node.Members.Loc;
    this.currentEnum = savedCurrentEnum;
    return this.Factory().NewBlock(statementList, true);
}

// Transforms an enum member into a statement. It is expected that `enum` has already been visited.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:371:1.
private Slice<Node> transformEnumMember(Slice<Node> statements, EnumDeclaration @enum, int index)
{
    var memberNode = @enum.Members.Nodes[index];
    var member = memberNode.AsEnumMember();
    var savedParent = this.parentNode;
    this.parentNode = this.currentNode;
    this.currentNode = memberNode;
    var expression = member.Initializer;
    bool useExplicitReverseMapping = default;
    var parseNode = this.EmitContext().ParseNode(memberNode);
    var result = this.emitResolver.GetEnumMemberValue(parseNode);
    switch (result.Value)
    {
        case JsNum.Number value:
            expression = TSUtilities.ConstantExpression(value, this.Factory()) ?? expression;
            useExplicitReverseMapping = true;
            break;
        case string value:
            expression = TSUtilities.ConstantExpression(value, this.Factory()) ?? expression;
            break;
        case ByteStringValue value: // Retained Go-string role of constant evaluation.
            expression = TSUtilities.ConstantExpression(value, this.Factory()) ?? expression;
            break;
        default:
            if (expression == null) expression = this.Factory().NewVoidZeroExpression();
            useExplicitReverseMapping = !result.IsSyntacticallyString;
            break;
    }
    expression = this.Factory().NewAssignmentExpression(this.getEnumQualifiedElement(@enum, member), expression);
    if (useExplicitReverseMapping)
    {
        expression = this.Factory().NewAssignmentExpression(this.Factory().NewElementAccessExpression(this.getNamespaceContainerName(@enum.AsNode()), null, expression, NodeFlags.None), this.getExpressionForPropertyName(member));
    }
    var memberStatement = this.Factory().NewExpressionStatement(expression);
    this.EmitContext().AssignCommentAndSourceMapRanges(expression, member.AsNode());
    this.EmitContext().AssignCommentAndSourceMapRanges(memberStatement, member.AsNode());
    statements = statements.Append(memberStatement);
    this.currentNode = this.parentNode;
    this.parentNode = savedParent;
    return statements;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:436:1.
private Node visitModuleDeclaration(ModuleDeclaration node)
{
    if (!this.shouldEmitModuleDeclaration(node))
    {
        return this.EmitContext().NewNotEmittedStatement(node.AsNode());
    }
    var statements = Slice<Node>.Make(0);
    var tuple6 = this.addVarForDeclaration(statements, node.AsNode()); statements = tuple6.Item1; var varAdded = tuple6.Item2;
    var emitFlags = EmitFlags.None;
    if ((varAdded && (((this.compilerOptions.GetEmitModuleKind() != ModuleKind.System) || (this.currentScope != this.currentSourceFile)))))
    {
        emitFlags |= EmitFlags.NoLeadingComments;
    }
    var moduleArg = this.Factory().NewLogicalORExpression(this.getExportQualifiedReferenceToDeclaration(node.AsNode()), this.Factory().NewAssignmentExpression(this.getExportQualifiedReferenceToDeclaration(node.AsNode()), this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(Slice<Node>.Make(0)), false)));
    if (this.isExportOfNamespace(node.AsNode()))
    {
        var localName = this.Factory().GetLocalNameEx(node.AsNode(), new AssignedNameOptions { AllowSourceMaps = true });
        moduleArg = this.Factory().NewAssignmentExpression(localName, moduleArg);
    }
    var moduleParamName = this.Factory().NewGeneratedNameForNode(node.AsNode());
    this.EmitContext().SetSourceMapRange(moduleParamName, node.Name().Loc);
    var moduleParam = this.Factory().NewParameterDeclaration(null, null, moduleParamName, null, null, null);
    var moduleBody = this.transformModuleBody(node, this.getNamespaceContainerName(node.AsNode()));
    var moduleFunc = this.Factory().NewFunctionExpression(null, null, null, null, this.Factory().NewNodeList(Slice<Node>.FromArray([moduleParam])), null, null, moduleBody);
    var moduleCall = this.Factory().NewCallExpression(this.Factory().NewParenthesizedExpression(moduleFunc), null, null, this.Factory().NewNodeList(Slice<Node>.FromArray([moduleArg])), NodeFlags.None);
    var moduleStatement = this.Factory().NewExpressionStatement(moduleCall);
    this.EmitContext().SetOriginal(moduleStatement, node.AsNode());
    this.EmitContext().AssignCommentAndSourceMapRanges(moduleStatement, node.AsNode());
    this.EmitContext().AddEmitFlags(moduleStatement, emitFlags);
    return this.Factory().NewSyntaxList(statements.Append(moduleStatement));
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:487:1.
private Node transformModuleBody(ModuleDeclaration node, Node namespaceLocalName)
{
    var savedCurrentNamespace = this.currentNamespace;
    var savedCurrentScope = this.currentScope;
    var savedCurrentScopeFirstDeclarationsOfName = this.currentScopeFirstDeclarationsOfName;
    this.currentNamespace = node.AsNode();
    this.currentScopeFirstDeclarationsOfName = null;
    Slice<Node> statements = default;
    this.EmitContext().StartVariableEnvironment();
    TextRange statementsLocation = default;
    TextRange blockLocation = default;
    if ((node.Body != null))
    {
        if ((node.Body.Kind == Kind.ModuleBlock))
        {
            node = this.Visitor().VisitEachChild(node.AsNode()).AsModuleDeclaration();
            var body = node.Body.AsModuleBlock();
            statements = body.Statements.Nodes;
            statementsLocation = body.Statements.Loc;
            blockLocation = body.Loc;
        }
        else
        {
            (statements, _) = this.Visitor().VisitSlice(Slice<Node>.FromArray([node.Body]));
            var moduleBlock = GetInnermostModuleDeclarationFromDottedModule(node).Body.AsModuleBlock();
            statementsLocation = moduleBlock.Statements.Loc.WithPos(-1);
        }
    }
    this.currentNamespace = savedCurrentNamespace;
    this.currentScope = savedCurrentScope;
    this.currentScopeFirstDeclarationsOfName = savedCurrentScopeFirstDeclarationsOfName;
    statements = this.EmitContext().EndAndMergeVariableEnvironment(statements);
    var statementList = this.Factory().NewNodeList(statements);
    statementList.Loc = statementsLocation;
    var block = this.Factory().NewBlock(statementList, true);
    block.Loc = blockLocation;
    if (((node.Body == null) || (node.Body.Kind != Kind.ModuleBlock)))
    {
        this.EmitContext().AddEmitFlags(block, EmitFlags.NoComments);
    }
    return block;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:554:1.
private Node visitImportEqualsDeclaration(ImportEqualsDeclaration node)
{
    if ((node.ModuleReference.Kind == Kind.ExternalModuleReference))
    {
        return this.Visitor().VisitEachChild(node.AsNode());
    }
    var moduleReference = this.Factory().CreateExpressionFromEntityName(node.ModuleReference);
    this.EmitContext().SetEmitFlags(moduleReference, (EmitFlags.NoComments | EmitFlags.NoNestedComments));
    if (!this.isExportOfNamespace(node.AsNode()))
    {
        var varDecl = this.Factory().NewVariableDeclaration(node.Name(), null, null, moduleReference);
        this.EmitContext().SetOriginal(varDecl, node.AsNode());
        var varList = this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([varDecl])), NodeFlags.None);
        var varModifiers = Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), ModifierFlags.Export);
        var varStatement = this.Factory().NewVariableStatement(varModifiers, varList);
        this.EmitContext().SetOriginal(varStatement, node.AsNode());
        this.EmitContext().AssignCommentAndSourceMapRanges(varStatement, node.AsNode());
        return varStatement;
    }
    else
    {
        var statement = this.createExportStatement(node.Name(), moduleReference, node.Loc, node.Loc, node.AsNode());
        statement.Loc = node.Loc;
        return statement;
    }
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:580:1.
private Node visitVariableStatement(VariableStatement node)
{
    if (this.isExportOfNamespace(node.AsNode()))
    {
        var expressions = Slice<Node>.Make(0);
        {
            foreach (var declaration in GoRuntime.RangeValues(node.DeclarationList.AsVariableDeclarationList().Declarations.Nodes))
            {
                var v = declaration.AsVariableDeclaration();
                if ((v.Initializer == null))
                {
                    continue;
                }
                if (Ast.IsBindingPattern(v.Name()))
                {
                    var expression = Transformers.FlattenDestructuringAssignment(this, this.Visitor().VisitNode(declaration), false, FlattenLevel.All, this.createNamespaceExportExpression);
                    if ((expression != null))
                    {
                        expressions = expressions.Append(expression);
                    }
                }
                else
                {
                    var expression2 = Transformers.ConvertVariableDeclarationToAssignmentExpression(this.EmitContext(), v);
                    if ((expression2 != null))
                    {
                        expressions = expressions.Append(expression2);
                    }
                }
            }
        }
        if ((GoRuntime.Len(expressions) == 0))
        {
            return null;
        }
        var expression3 = this.Factory().InlineExpressions(expressions);
        var statement = this.Factory().NewExpressionStatement(expression3);
        this.EmitContext().SetOriginal(statement, node.AsNode());
        this.EmitContext().AssignCommentAndSourceMapRanges(statement, node.AsNode());
        var savedCurrent = this.currentNode;
        this.currentNode = statement;
        statement = this.Visitor().VisitEachChild(statement);
        this.currentNode = savedCurrent;
        return statement;
    }
    return this.Visitor().VisitEachChild(node.AsNode());
}

// createNamespaceExportExpression creates an assignment to a namespace member for use as a
// callback during destructuring flattening.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:626:1.
private Node createNamespaceExportExpression(Node exportName, Node exportValue, TextRange? location)
{
    var memberName = this.getNamespaceQualifiedProperty(this.getNamespaceContainerName(this.currentNamespace), exportName);
    var expression = this.Factory().NewAssignmentExpression(memberName, exportValue);
    if ((location != null))
    {
        expression.Loc = location.Value;
    }
    return expression;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:635:1.
private Node visitFunctionDeclaration(FunctionDeclaration node)
{
    if (this.isExportOfNamespace(node.AsNode()))
    {
        var updated = this.Factory().UpdateFunctionDeclaration(node, this.Visitor().VisitModifiers(Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), ~ModifierFlags.Export)), node.AsteriskToken, this.Visitor().VisitNode(node.Name()), null, this.Visitor().VisitNodes(node.Parameters), null, null, this.Visitor().VisitNode(node.Body));
        var export = this.createExportStatementForDeclaration(node.AsNode());
        if ((export != null))
        {
            return this.Factory().NewSyntaxList(Slice<Node>.FromArray([updated, export]));
        }
        return updated;
    }
    return this.Visitor().VisitEachChild(node.AsNode());
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:657:1.
private Slice<ParameterDeclaration> getParameterProperties(Node constructor)
{
    Slice<ParameterDeclaration> parameterProperties = default;
    if ((constructor != null))
    {
        {
            foreach (var parameter in GoRuntime.RangeValues(constructor.Parameters()))
            {
                if (Ast.IsParameterPropertyDeclaration(parameter, constructor))
                {
                    parameterProperties = parameterProperties.Append(parameter.AsParameterDeclaration());
                }
            }
        }
    }
    return parameterProperties;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:669:1.
private Node visitClassDeclaration(ClassDeclaration node)
{
    var exported = this.isExportOfNamespace(node.AsNode());
    ModifierList modifiers = default;
    if (exported)
    {
        modifiers = this.Visitor().VisitModifiers(Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), ~ModifierFlags.ExportDefault));
    }
    else
    {
        modifiers = this.Visitor().VisitModifiers(node.Modifiers());
    }
    var name = this.Visitor().VisitNode(node.Name());
    if (((name == null) && ((exported || Ast.ChildIsDecorated(this.compilerOptions.ExperimentalDecorators == Tristate.True, node.AsNode(), null)))))
    {
        name = this.Factory().NewGeneratedNameForNode(node.AsNode());
    }
    var heritageClauses = this.Visitor().VisitNodes(node.HeritageClauses);
    var members = this.Visitor().VisitNodes(node.Members);
    var parameterProperties = this.getParameterProperties(Core.Find(node.Members.Nodes, Ast.IsConstructorDeclaration));
    if ((GoRuntime.Len(parameterProperties) > 0))
    {
        Slice<Node> newMembers = default;
        {
            foreach (var parameter in GoRuntime.RangeValues(parameterProperties))
            {
                if (Ast.IsIdentifier(parameter.Name()))
                {
                    var parameterProperty = this.Factory().NewPropertyDeclaration(null, parameter.Name().Clone(this.Factory()), null, null, null);
                    this.EmitContext().SetOriginal(parameterProperty, parameter.AsNode());
                    newMembers = newMembers.Append(parameterProperty);
                }
            }
        }
        if ((GoRuntime.Len(newMembers) > 0))
        {
            newMembers = newMembers.Append(members.Nodes);
            members = this.Factory().NewNodeList(newMembers);
            members.Loc = node.Members.Loc;
        }
    }
    var updated = this.Factory().UpdateClassDeclaration(node, modifiers, name, null, heritageClauses, members);
    if (exported)
    {
        var export = this.createExportStatementForDeclaration(node.AsNode());
        if ((export != null))
        {
            return this.Factory().NewSyntaxList(Slice<Node>.FromArray([updated, export]));
        }
    }
    return updated;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:718:1.
private Node visitClassExpression(ClassExpression node)
{
    var modifiers = this.Visitor().VisitModifiers(Transformers.ExtractModifiers(this.EmitContext(), node.Modifiers(), ~ModifierFlags.ExportDefault));
    var name = this.Visitor().VisitNode(node.Name());
    var heritageClauses = this.Visitor().VisitNodes(node.HeritageClauses);
    var members = this.Visitor().VisitNodes(node.Members);
    var parameterProperties = this.getParameterProperties(Core.Find(node.Members.Nodes, Ast.IsConstructorDeclaration));
    if ((GoRuntime.Len(parameterProperties) > 0))
    {
        Slice<Node> newMembers = default;
        {
            foreach (var parameter in GoRuntime.RangeValues(parameterProperties))
            {
                if (Ast.IsIdentifier(parameter.Name()))
                {
                    var parameterProperty = this.Factory().NewPropertyDeclaration(null, parameter.Name().Clone(this.Factory()), null, null, null);
                    this.EmitContext().SetOriginal(parameterProperty, parameter.AsNode());
                    newMembers = newMembers.Append(parameterProperty);
                }
            }
        }
        if ((GoRuntime.Len(newMembers) > 0))
        {
            newMembers = newMembers.Append(members.Nodes);
            members = this.Factory().NewNodeList(newMembers);
            members.Loc = node.Members.Loc;
        }
    }
    return this.Factory().UpdateClassExpression(node, modifiers, name, null, heritageClauses, members);
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:750:1.
private Node visitConstructorDeclaration(ConstructorDeclaration node)
{
    var modifiers = this.Visitor().VisitModifiers(node.Modifiers());
    var parameters = this.EmitContext().VisitParameters(node.ParameterList(), this.Visitor());
    var body = this.visitConstructorBody(node.Body.AsBlock(), node.AsNode());
    return this.Factory().UpdateConstructorDeclaration(node, modifiers, null, parameters, null, null, body);
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:757:1.
private Node visitConstructorBody(Block body, Node constructor)
{
    var parameterProperties = this.getParameterProperties(constructor);
    if ((GoRuntime.Len(parameterProperties) == 0))
    {
        return this.EmitContext().VisitFunctionBody(body.AsNode(), this.Visitor());
    }
    var grandparentOfBody = this.pushNode(body.AsNode());
    var tuple11 = this.pushScope(body.AsNode()); var savedCurrentScope = tuple11.Item1; var savedCurrentScopeFirstDeclarationsOfName = tuple11.Item2;
    this.EmitContext().StartVariableEnvironment();
    var tuple12 = this.Factory().SplitStandardPrologue(body.Statements.Nodes); var prologue = tuple12.Item1; var rest = tuple12.Item2;
    var statements = GoSlices.Clone(prologue);
    Slice<Node> parameterPropertyAssignments = default;
    {
        foreach (var parameter in GoRuntime.RangeValues(parameterProperties))
        {
            if (Ast.IsIdentifier(parameter.Name()))
            {
                var propertyName = parameter.Name().Clone(this.Factory());
                propertyName.Parent = parameter.Name().Parent;
                this.EmitContext().AddEmitFlags(propertyName, (EmitFlags.NoComments | EmitFlags.NoSourceMap));
                var localName = parameter.Name().Clone(this.Factory());
                localName.Parent = parameter.Name().Parent;
                this.EmitContext().AddEmitFlags(localName, EmitFlags.NoComments);
                var parameterProperty = this.Factory().NewExpressionStatement(this.Factory().NewAssignmentExpression(this.Factory().NewPropertyAccessExpression(this.Factory().NewThisExpression(), null, propertyName, NodeFlags.None), localName));
                this.EmitContext().SetOriginal(parameterProperty, parameter.AsNode());
                this.EmitContext().AddEmitFlags(parameterProperty, EmitFlags.StartOnNewLine);
                parameterPropertyAssignments = parameterPropertyAssignments.Append(parameterProperty);
            }
        }
    }
    var superPath = Transformers.FindSuperStatementIndexPath(rest, 0);
    if ((GoRuntime.Len(superPath) > 0))
    {
        statements = statements.Append(this.transformConstructorBodyWorker(rest, superPath, parameterPropertyAssignments));
    }
    else
    {
        statements = statements.Append(parameterPropertyAssignments);
        statements = statements.Append(this.Visitor().VisitSlice(rest).Item1);
    }
    statements = this.EmitContext().EndAndMergeVariableEnvironment(statements);
    var statementList = this.Factory().NewNodeList(statements);
    statementList.Loc = body.Statements.Loc;
    this.popScope(savedCurrentScope, savedCurrentScopeFirstDeclarationsOfName);
    this.popNode(grandparentOfBody);
    var updated = this.Factory().NewBlock(statementList, true);
    this.EmitContext().SetOriginal(updated, body.AsNode());
    updated.Loc = body.Loc;
    return updated;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:832:1.
private Slice<Node> transformConstructorBodyWorker(Slice<Node> statementsIn, Slice<int> superPath, Slice<Node> initializerStatements)
{
    Slice<Node> statementsOut = default;
    var superStatementIndex = superPath[0];
    var superStatement = statementsIn[superStatementIndex];
    statementsOut = statementsOut.Append(this.Visitor().VisitSlice(statementsIn.Slice2(0, superStatementIndex)).Item1);
    if (Ast.IsTryStatement(superStatement))
    {
        var tryStatement = superStatement.AsTryStatement();
        var tryBlock = tryStatement.TryBlock.AsBlock();
        var grandparentOfTryStatement = this.pushNode(tryStatement.AsNode());
        var grandparentOfTryBlock = this.pushNode(tryBlock.AsNode());
        var tuple14 = this.pushScope(tryBlock.AsNode()); var savedCurrentScope = tuple14.Item1; var savedCurrentScopeFirstDeclarationsOfName = tuple14.Item2;
        var tryBlockStatements = this.transformConstructorBodyWorker(tryBlock.Statements.Nodes, superPath.Slice2(1, superPath.Length), initializerStatements);
        this.popScope(savedCurrentScope, savedCurrentScopeFirstDeclarationsOfName);
        this.popNode(grandparentOfTryBlock);
        var tryBlockStatementList = this.Factory().NewNodeList(tryBlockStatements);
        tryBlockStatementList.Loc = tryBlock.Statements.Loc;
        statementsOut = statementsOut.Append(this.Factory().UpdateTryStatement(tryStatement, this.Factory().UpdateBlock(tryBlock, tryBlockStatementList, tryBlock.MultiLine), this.Visitor().VisitNode(tryStatement.CatchClause), this.Visitor().VisitNode(tryStatement.FinallyBlock)));
        this.popNode(grandparentOfTryStatement);
    }
    else
    {
        statementsOut = statementsOut.Append(this.Visitor().VisitSlice(statementsIn.Slice2(superStatementIndex, (superStatementIndex + 1))).Item1);
        statementsOut = statementsOut.Append(initializerStatements);
    }
    statementsOut = statementsOut.Append(this.Visitor().VisitSlice(statementsIn.Slice2((superStatementIndex + 1), statementsIn.Length)).Item1);
    return statementsOut;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:885:1.
private Node visitShorthandPropertyAssignment(ShorthandPropertyAssignment node)
{
    var name = node.Name();
    var exportedOrImportedName = this.visitExpressionIdentifier(name);
    if ((exportedOrImportedName != name))
    {
        var expression = exportedOrImportedName;
        if ((node.ObjectAssignmentInitializer != null))
        {
            var equalsToken = node.EqualsToken;
            if ((equalsToken == null))
            {
                equalsToken = this.Factory().NewToken(Kind.EqualsToken);
            }
            expression = this.Factory().NewBinaryExpression(null, expression, null, equalsToken, this.Visitor().VisitNode(node.ObjectAssignmentInitializer));
        }
        var updated = this.Factory().NewPropertyAssignment(null, node.Name(), null, null, expression);
        updated.Loc = node.Loc;
        this.EmitContext().SetOriginal(updated, node.AsNode());
        this.EmitContext().AssignCommentAndSourceMapRanges(updated, node.AsNode());
        return updated;
    }
    return this.Factory().UpdateShorthandPropertyAssignment(node, null, exportedOrImportedName, null, null, node.EqualsToken, this.Visitor().VisitNode(node.ObjectAssignmentInitializer));
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:921:1.
private Node visitIdentifier(Node node)
{
    if (Transformers.IsIdentifierReference(node, this.parentNode))
    {
        return this.visitExpressionIdentifier(node);
    }
    return node;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:928:1.
private Node visitExpressionIdentifier(Node node)
{
    if ((((((this.currentEnum != null) || (this.currentNamespace != null))) && !Transformers.IsGeneratedIdentifier(this.EmitContext(), node)) && !Transformers.IsLocalName(this.EmitContext(), node)))
    {
        var location = this.EmitContext().MostOriginal(node.AsNode());
        var container = this.resolver.GetReferencedExportContainer(location, false);
        if (((container != null) && ((Ast.IsEnumDeclaration(container) || Ast.IsModuleDeclaration(container)))))
        {
            var containerName = this.getNamespaceContainerName(container);
            var memberName = node.Clone(this.Factory());
            this.EmitContext().SetEmitFlags(memberName, (EmitFlags.NoComments | EmitFlags.NoSourceMap));
            var expression = this.Factory().GetNamespaceMemberName(containerName, memberName, new NameOptions { AllowSourceMaps = true });
            this.EmitContext().AssignCommentAndSourceMapRanges(expression, node.AsNode());
            return expression;
        }
    }
    return node;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:946:1.
private Node createExportStatementForDeclaration(Node node)
{
    var exportName = this.Factory().GetExternalModuleOrNamespaceExportName(this.getNamespaceContainerName(this.currentNamespace), node, false, true);
    var localName = this.Factory().GetLocalName(node);
    var expression = this.Factory().NewAssignmentExpression(exportName, localName);
    var exportAssignmentSourceMapRange = node.Loc;
    if ((node.Name() != null))
    {
        exportAssignmentSourceMapRange = exportAssignmentSourceMapRange.WithPos(node.Name().Pos());
    }
    this.EmitContext().SetSourceMapRange(expression, exportAssignmentSourceMapRange);
    var statement = this.Factory().NewExpressionStatement(expression);
    var exportStatementSourceMapRange = node.Loc.WithPos(-1);
    this.EmitContext().SetSourceMapRange(statement, exportStatementSourceMapRange);
    return statement;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:962:1.
private Node createExportAssignment(Node name, Node expression, TextRange exportAssignmentSourceMapRange, Node original)
{
    var exportName = this.getNamespaceQualifiedProperty(this.getNamespaceContainerName(this.currentNamespace), name);
    var exportAssignment = this.Factory().NewAssignmentExpression(exportName, expression);
    this.EmitContext().SetOriginal(exportAssignment, original);
    this.EmitContext().SetSourceMapRange(exportAssignment, exportAssignmentSourceMapRange);
    return exportAssignment;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:970:1.
private Node createExportStatement(Node name, Node expression, TextRange exportAssignmentSourceMapRange, TextRange exportStatementSourceMapRange, Node original)
{
    var exportStatement = this.Factory().NewExpressionStatement(this.createExportAssignment(name, expression, exportAssignmentSourceMapRange, original));
    this.EmitContext().SetOriginal(exportStatement, original);
    this.EmitContext().SetSourceMapRange(exportStatement, exportStatementSourceMapRange);
    return exportStatement;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:977:1.
private bool shouldEmitEnumDeclaration(EnumDeclaration node)
{
    return (!Ast.IsEnumConst(node.AsNode()) || this.compilerOptions.ShouldPreserveConstEnums());
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:981:1.
private bool shouldEmitModuleDeclaration(ModuleDeclaration node)
{
    var pn = this.EmitContext().ParseNode(node.AsNode());
    if ((pn == null))
    {
        return true;
    }
    return Ast.IsInstantiatedModule(pn, this.compilerOptions.ShouldPreserveConstEnums());
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go:990:1.
internal static ModuleDeclaration GetInnermostModuleDeclarationFromDottedModule(ModuleDeclaration moduleDeclaration)
{
    while (((moduleDeclaration.Body != null) && (moduleDeclaration.Body.Kind == Kind.ModuleDeclaration)))
    {
        moduleDeclaration = moduleDeclaration.Body.AsModuleDeclaration();
    }
    return moduleDeclaration;
}

}
