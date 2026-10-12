// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete estransforms/namedevaluation.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace TypeScript.Transformers.EsTransforms;
internal static partial class EsTransforms
{
    // namedevaluation.go:16.
    internal static bool IsClassNamedEvaluationHelperBlock(EmitContext context, Node node)
    {
        if (!Ast.IsClassStaticBlockDeclaration(node) || node.AsClassStaticBlockDeclaration().Body.Statements().Count != 1)
            return false;
        var statement = node.AsClassStaticBlockDeclaration().Body.Statements()[0];
        if (Ast.IsExpressionStatement(statement))
        {
            var expression = statement.Expression();
            if (context.IsCallToHelper(expression, "__setFunctionName"))
            {
                var arguments = expression.AsCallExpression().Arguments;
                return arguments.Nodes.Count >= 2 && arguments.Nodes[1] == context.AssignedName(node);
            }
        }
        return false;
    }
    // namedevaluation.go:38.
    internal static bool ClassHasExplicitlyAssignedName(EmitContext context, Node node)
    {
        if (context.AssignedName(node) != null)
            foreach (var member in node.Members())
                if (IsClassNamedEvaluationHelperBlock(context, member)) return true;
        return false;
    }
    // namedevaluation.go:54.
    internal static bool ClassHasDeclaredOrExplicitlyAssignedName(EmitContext context, Node node) =>
        node.Name() != null || ClassHasExplicitlyAssignedName(context, node);
    // namedevaluation.go:63.
    internal static bool IsAnonymousFunctionDefinition(EmitContext context, Node node, Func<Node, bool> callback)
    {
        node = Ast.SkipOuterExpressions(node, OuterExpressionKinds.All);
        switch (node.Kind)
        {
            case Kind.ClassExpression:
                if (ClassHasDeclaredOrExplicitlyAssignedName(context, node)) return false;
                break;
            case Kind.FunctionExpression:
                if (node.AsFunctionExpression().Name() != null) return false;
                break;
            case Kind.ArrowFunction: break;
            default: return false;
        }
        return callback == null || callback(node);
    }
    // namedevaluation.go:85.
    internal static bool IsNamedEvaluation(EmitContext context, Node node) => IsNamedEvaluationAnd(context, node, null);
    // namedevaluation.go:89.
    internal static bool IsNamedEvaluationAnd(EmitContext context, Node node, Func<Node, bool> callback)
    {
        if (!Ast.IsNamedEvaluationSource(node)) return false;
        return node.Kind switch
        {
            Kind.ShorthandPropertyAssignment => IsAnonymousFunctionDefinition(context, node.AsShorthandPropertyAssignment().ObjectAssignmentInitializer, callback),
            Kind.PropertyAssignment or Kind.VariableDeclaration or Kind.Parameter or Kind.BindingElement or Kind.PropertyDeclaration => IsAnonymousFunctionDefinition(context, node.Initializer(), callback),
            Kind.BinaryExpression => IsAnonymousFunctionDefinition(context, node.AsBinaryExpression().Right, callback),
            Kind.ExportAssignment => IsAnonymousFunctionDefinition(context, node.Expression(), callback),
            _ => throw new InvalidOperationException("Unhandled case in isNamedEvaluation")
        };
    }
    // namedevaluation.go:109.
    internal static Node GetAssignedNameOfIdentifier(EmitContext context, Node name, Node expression)
    {
        var original = context.MostOriginal(Ast.SkipOuterExpressions(expression, OuterExpressionKinds.All));
        if ((Ast.IsClassDeclaration(original) || Ast.IsFunctionDeclaration(original)) &&
            original.Name() == null && Ast.HasSyntacticModifier(original, ModifierFlags.Default))
            return context.Factory.NewStringLiteral("default", TokenFlags.None);
        return context.Factory.NewStringLiteralFromNode(name);
    }
    // namedevaluation.go:118.
    internal static (Node AssignedName, Node UpdatedName) GetAssignedNameOfPropertyName(EmitContext context, Node name, ByteStringValue assignedNameText)
    {
        var factory = context.Factory;
        if (assignedNameText.Length > 0)
            return (factory.NewStringLiteralWithTextValue(assignedNameText, TokenFlags.None), name);
        if (Ast.IsPropertyNameLiteral(name) || Ast.IsPrivateIdentifier(name))
            return (factory.NewStringLiteralFromNode(name), name);
        var expression = name.Expression();
        if (Ast.IsPropertyNameLiteral(expression) && !Ast.IsIdentifier(expression))
            return (factory.NewStringLiteralFromNode(expression), name);
        if (!Ast.IsComputedPropertyName(name)) throw new InvalidOperationException("Expected computed property name");
        var assignedName = factory.NewGeneratedNameForNode(name);
        context.AddVariableDeclaration(assignedName);
        var key = factory.NewPropKeyHelper(expression);
        var assignment = factory.NewAssignmentExpression(assignedName, key);
        return (assignedName, factory.UpdateComputedPropertyName(name.AsComputedPropertyName(), assignment));
    }
    // namedevaluation.go:153.
    internal static Node CreateClassNamedEvaluationHelperBlock(EmitContext context, Node assignedName, Node thisExpression)
    {
        thisExpression ??= context.Factory.NewThisExpression();
        var factory = context.Factory;
        var expression = factory.NewSetFunctionNameHelper(thisExpression, assignedName, "");
        var statement = factory.NewExpressionStatement(expression);
        var body = factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([statement])), false);
        var block = factory.NewClassStaticBlockDeclaration(null, body);
        context.SetAssignedName(block, assignedName);
        return block;
    }
    // namedevaluation.go:176.
    internal static Node InjectClassNamedEvaluationHelperBlockIfMissing(EmitContext context, Node node, Node assignedName, Node thisExpression)
    {
        if (ClassHasExplicitlyAssignedName(context, node)) return node;
        var factory = context.Factory;
        var namedEvaluationBlock = CreateClassNamedEvaluationHelperBlock(context, assignedName, thisExpression);
        if (node.Name() != null) context.SetSourceMapRange(namedEvaluationBlock.Body().Statements()[0], node.Name().Loc);
        int insertionIndex = -1;
        var oldMembers = node.Members();
        for (int i = 0; i < oldMembers.Count; i++)
            if (IsClassThisAssignmentBlock(context, oldMembers[i])) { insertionIndex = i; break; }
        insertionIndex++;
        var leading = GoSlices.Clone(oldMembers.Slice2(0, insertionIndex));
        var trailing = GoSlices.Clone(oldMembers.Slice2(insertionIndex, oldMembers.Count));
        Slice<Node> members = default;
        members = members.Append(leading);
        members = members.Append(namedEvaluationBlock);
        members = members.Append(trailing);
        var membersList = factory.NewNodeList(members);
        membersList.Loc = node.MemberList().Loc;
        var oldNode = node;
        node = Ast.IsClassDeclaration(node)
            ? factory.UpdateClassDeclaration(node.AsClassDeclaration(), node.Modifiers(), node.Name(), node.TypeParameterList(), node.AsClassDeclaration().HeritageClauses, membersList)
            : factory.UpdateClassExpression(node.AsClassExpression(), node.Modifiers(), node.Name(), node.TypeParameterList(), node.AsClassExpression().HeritageClauses, membersList);
        context.SetAssignedName(node, assignedName);
        var classThis = context.ClassThis(oldNode);
        if (classThis != null) context.SetClassThis(node, classThis);
        return node;
    }
    // namedevaluation.go:250.
    internal static Node FinishTransformNamedEvaluation(EmitContext context, Node expression, Node assignedName, bool ignoreEmptyStringLiteral)
    {
        if (ignoreEmptyStringLiteral && Ast.IsStringLiteral(assignedName) && assignedName.TextValue().Length == 0) return expression;
        var factory = context.Factory;
        var innerExpression = Ast.SkipOuterExpressions(expression, OuterExpressionKinds.All);
        var updatedExpression = Ast.IsClassExpression(innerExpression)
            ? InjectClassNamedEvaluationHelperBlockIfMissing(context, innerExpression, assignedName, null)
            : factory.NewSetFunctionNameHelper(innerExpression, assignedName, "");
        return factory.RestoreOuterExpressions(expression, updatedExpression, OuterExpressionKinds.All);
    }
    // namedevaluation.go:273.
    internal static Node TransformNamedEvaluationOfPropertyAssignment(EmitContext context, PropertyAssignment node, bool ignoreEmptyStringLiteral, ByteStringValue assignedNameText)
    {
        var (assignedName, name) = GetAssignedNameOfPropertyName(context, node.Name(), assignedNameText);
        var initializer = FinishTransformNamedEvaluation(context, node.Initializer, assignedName, ignoreEmptyStringLiteral);
        return context.Factory.UpdatePropertyAssignment(node, null, name, null, null, initializer);
    }
    // namedevaluation.go:287.
    internal static Node TransformNamedEvaluationOfShorthandAssignmentProperty(EmitContext context, ShorthandPropertyAssignment node, bool ignoreEmptyStringLiteral, ByteStringValue assignedNameText)
    {
        var factory = context.Factory;
        var assignedName = assignedNameText.Length > 0
            ? factory.NewStringLiteralWithTextValue(assignedNameText, TokenFlags.None)
            : GetAssignedNameOfIdentifier(context, node.Name(), node.ObjectAssignmentInitializer);
        var initializer = FinishTransformNamedEvaluation(context, node.ObjectAssignmentInitializer, assignedName, ignoreEmptyStringLiteral);
        return factory.UpdateShorthandPropertyAssignment(node, null, node.Name(), null, null, node.EqualsToken, initializer);
    }
    // namedevaluation.go:315.
    internal static Node TransformNamedEvaluationOfVariableDeclaration(EmitContext context, VariableDeclaration node, bool ignoreEmptyStringLiteral, ByteStringValue assignedNameText)
    {
        var assignedName = assignedNameText.Length > 0
            ? context.Factory.NewStringLiteralWithTextValue(assignedNameText, TokenFlags.None)
            : GetAssignedNameOfIdentifier(context, node.Name(), node.Initializer);
        var initializer = FinishTransformNamedEvaluation(context, node.Initializer, assignedName, ignoreEmptyStringLiteral);
        return context.Factory.UpdateVariableDeclaration(node, node.Name(), null, null, initializer);
    }
    // namedevaluation.go:347.
    internal static Node TransformNamedEvaluationOfParameterDeclaration(EmitContext context, ParameterDeclaration node, bool ignoreEmptyStringLiteral, ByteStringValue assignedNameText)
    {
        var assignedName = assignedNameText.Length > 0
            ? context.Factory.NewStringLiteralWithTextValue(assignedNameText, TokenFlags.None)
            : GetAssignedNameOfIdentifier(context, node.Name(), node.Initializer);
        var initializer = FinishTransformNamedEvaluation(context, node.Initializer, assignedName, ignoreEmptyStringLiteral);
        return context.Factory.UpdateParameterDeclaration(node, null, node.DotDotDotToken, node.Name(), null, null, initializer);
    }
    // namedevaluation.go:383.
    internal static Node TransformNamedEvaluationOfBindingElement(EmitContext context, BindingElement node, bool ignoreEmptyStringLiteral, ByteStringValue assignedNameText)
    {
        var assignedName = assignedNameText.Length > 0
            ? context.Factory.NewStringLiteralWithTextValue(assignedNameText, TokenFlags.None)
            : GetAssignedNameOfIdentifier(context, node.Name(), node.Initializer);
        var initializer = FinishTransformNamedEvaluation(context, node.Initializer, assignedName, ignoreEmptyStringLiteral);
        return context.Factory.UpdateBindingElement(node, node.DotDotDotToken, node.PropertyName, node.Name(), initializer);
    }
    // namedevaluation.go:417.
    internal static Node TransformNamedEvaluationOfPropertyDeclaration(EmitContext context, PropertyDeclaration node, bool ignoreEmptyStringLiteral, ByteStringValue assignedNameText)
    {
        var (assignedName, name) = GetAssignedNameOfPropertyName(context, node.Name(), assignedNameText);
        var initializer = FinishTransformNamedEvaluation(context, node.Initializer, assignedName, ignoreEmptyStringLiteral);
        return context.Factory.UpdatePropertyDeclaration(node, node.Modifiers(), name, null, null, initializer);
    }
    // namedevaluation.go:438.
    internal static Node TransformNamedEvaluationOfAssignmentExpression(EmitContext context, BinaryExpression node, bool ignoreEmptyStringLiteral, ByteStringValue assignedNameText)
    {
        var assignedName = assignedNameText.Length > 0
            ? context.Factory.NewStringLiteralWithTextValue(assignedNameText, TokenFlags.None)
            : GetAssignedNameOfIdentifier(context, node.Left, node.Right);
        var right = FinishTransformNamedEvaluation(context, node.Right, assignedName, ignoreEmptyStringLiteral);
        return context.Factory.UpdateBinaryExpression(node, null, node.Left, null, node.OperatorToken, right);
    }
    // namedevaluation.go:483.
    internal static Node TransformNamedEvaluationOfExportAssignment(EmitContext context, ExportAssignment node, bool ignoreEmptyStringLiteral, ByteStringValue assignedNameText)
    {
        var factory = context.Factory;
        var assignedName = assignedNameText.Length > 0
            ? factory.NewStringLiteralWithTextValue(assignedNameText, TokenFlags.None)
            : factory.NewStringLiteral(node.IsExportEquals ? "" : "default", TokenFlags.None);
        var expression = FinishTransformNamedEvaluation(context, node.Expression, assignedName, ignoreEmptyStringLiteral);
        return factory.UpdateExportAssignment(node, null, node.IsExportEquals, null, expression);
    }
    // namedevaluation.go:513.
    internal static Node TransformNamedEvaluation(EmitContext context, Node node, bool ignoreEmptyStringLiteral, ByteStringValue assignedName) => node.Kind switch
    {
        Kind.PropertyAssignment => TransformNamedEvaluationOfPropertyAssignment(context, node.AsPropertyAssignment(), ignoreEmptyStringLiteral, assignedName),
        Kind.ShorthandPropertyAssignment => TransformNamedEvaluationOfShorthandAssignmentProperty(context, node.AsShorthandPropertyAssignment(), ignoreEmptyStringLiteral, assignedName),
        Kind.VariableDeclaration => TransformNamedEvaluationOfVariableDeclaration(context, node.AsVariableDeclaration(), ignoreEmptyStringLiteral, assignedName),
        Kind.Parameter => TransformNamedEvaluationOfParameterDeclaration(context, node.AsParameterDeclaration(), ignoreEmptyStringLiteral, assignedName),
        Kind.BindingElement => TransformNamedEvaluationOfBindingElement(context, node.AsBindingElement(), ignoreEmptyStringLiteral, assignedName),
        Kind.PropertyDeclaration => TransformNamedEvaluationOfPropertyDeclaration(context, node.AsPropertyDeclaration(), ignoreEmptyStringLiteral, assignedName),
        Kind.BinaryExpression => TransformNamedEvaluationOfAssignmentExpression(context, node.AsBinaryExpression(), ignoreEmptyStringLiteral, assignedName),
        Kind.ExportAssignment => TransformNamedEvaluationOfExportAssignment(context, node.AsExportAssignment(), ignoreEmptyStringLiteral, assignedName),
        _ => throw new InvalidOperationException("Unhandled case in transformNamedEvaluation")
    };
}
