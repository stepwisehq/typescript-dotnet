// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/transformers/utilities.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using EmitFactory = TypeScript.DiagnosticPrinting.NodeFactory;
namespace TypeScript.Transformers;
internal static partial class Transformers
{
    internal static bool IsGeneratedIdentifier(EmitContext context, Node name) => context.HasAutoGenerateInfo(name);
    internal static bool IsHelperName(EmitContext context, Node name) => (context.EmitFlags(name) & EmitFlags.HelperName) != 0;
    internal static bool IsLocalName(EmitContext context, Node name) => (context.EmitFlags(name) & EmitFlags.LocalName) != 0;
    internal static bool IsExportName(EmitContext context, Node name) => (context.EmitFlags(name) & EmitFlags.ExportName) != 0;
    internal static bool IsIdentifierReference(Node name, Node parent)
    {
        switch(parent.Kind)
        {
            case Kind.BinaryExpression: case Kind.PrefixUnaryExpression: case Kind.PostfixUnaryExpression:
            case Kind.YieldExpression: case Kind.AsExpression: case Kind.SatisfiesExpression:
            case Kind.ElementAccessExpression: case Kind.NonNullExpression: case Kind.SpreadElement:
            case Kind.SpreadAssignment: case Kind.ParenthesizedExpression: case Kind.ArrayLiteralExpression:
            case Kind.DeleteExpression: case Kind.TypeOfExpression: case Kind.VoidExpression:
            case Kind.AwaitExpression: case Kind.TypeAssertionExpression: case Kind.ExpressionWithTypeArguments:
            case Kind.JsxSelfClosingElement: case Kind.JsxSpreadAttribute: case Kind.JsxExpression:
            case Kind.PartiallyEmittedExpression:
                return true; // All immediate Identifier children are IdentifierReference.
            case Kind.ComputedPropertyName: case Kind.Decorator: case Kind.IfStatement:
            case Kind.DoStatement: case Kind.WhileStatement: case Kind.WithStatement:
            case Kind.ReturnStatement: case Kind.SwitchStatement: case Kind.CaseClause:
            case Kind.ThrowStatement: case Kind.ExpressionStatement: case Kind.ExportAssignment:
            case Kind.PropertyAccessExpression: case Kind.TemplateSpan:
                return parent.Expression() == name;
            case Kind.VariableDeclaration: case Kind.Parameter: case Kind.BindingElement:
            case Kind.PropertyDeclaration: case Kind.PropertySignature: case Kind.PropertyAssignment:
            case Kind.EnumMember: case Kind.JsxAttribute:
                return parent.Initializer() == name;
            case Kind.ShorthandPropertyAssignment:
                return parent.AsShorthandPropertyAssignment().ObjectAssignmentInitializer == name;
            case Kind.ForStatement:
                return parent.Initializer() == name || parent.AsForStatement().Condition == name || parent.AsForStatement().Incrementor == name;
            case Kind.ForInStatement: case Kind.ForOfStatement:
                return parent.Initializer() == name || parent.Expression() == name;
            case Kind.ImportEqualsDeclaration:
                return parent.AsImportEqualsDeclaration().ModuleReference == name;
            case Kind.ArrowFunction: return parent.Body() == name;
            case Kind.ConditionalExpression:
                return parent.AsConditionalExpression().Condition == name || parent.AsConditionalExpression().WhenTrue == name || parent.AsConditionalExpression().WhenFalse == name;
            case Kind.CallExpression: case Kind.NewExpression:
                return parent.Expression() == name || GoSlices.Contains(parent.Arguments(), name);
            case Kind.TaggedTemplateExpression: return parent.AsTaggedTemplateExpression().Tag == name;
            case Kind.ImportAttribute: return parent.AsImportAttribute().Value == name;
            case Kind.JsxOpeningElement: case Kind.JsxClosingElement: return parent.TagName() == name;
            default: return false;
        }
    }
    private static Node convertBindingElementToArrayAssignmentElement(EmitContext context, BindingElement element)
    {
        if(element.Name() == null)
        {
            var elision = context.Factory.NewOmittedExpression();
            context.SetOriginal(elision, element.AsNode()); context.AssignCommentAndSourceMapRanges(elision, element.AsNode()); return elision;
        }
        var expression = convertBindingNameToAssignmentElementTarget(context, element.Name());
        if(element.DotDotDotToken != null)
        {
            var spread = context.Factory.NewSpreadElement(expression);
            context.SetOriginal(spread, element.AsNode()); context.AssignCommentAndSourceMapRanges(spread, element.AsNode()); return spread;
        }
        if(element.Initializer != null)
        {
            var assignment = context.Factory.NewAssignmentExpression(expression,element.Initializer);
            context.SetOriginal(assignment,element.AsNode());context.AssignCommentAndSourceMapRanges(assignment,element.AsNode());return assignment;
        }
        return expression;
    }
    private static Node convertBindingElementToObjectAssignmentElement(EmitContext context,BindingElement element)
    {
        if(element.DotDotDotToken != null)
        {
            var spread=context.Factory.NewSpreadAssignment(element.Name());
            context.SetOriginal(spread,element.AsNode());context.AssignCommentAndSourceMapRanges(spread,element.AsNode());return spread;
        }
        if(element.PropertyName != null)
        {
            var expression=convertBindingNameToAssignmentElementTarget(context,element.Name());
            if(element.Initializer != null) expression=context.Factory.NewAssignmentExpression(expression,element.Initializer);
            var assignment=context.Factory.NewPropertyAssignment(null,element.PropertyName,null,null,expression);
            context.SetOriginal(assignment,element.AsNode());context.AssignCommentAndSourceMapRanges(assignment,element.AsNode());return assignment;
        }
        Node equalsToken=null;
        if(element.Initializer != null) equalsToken=context.Factory.NewToken(Kind.EqualsToken);
        var shorthand=context.Factory.NewShorthandPropertyAssignment(null,element.Name(),null,null,equalsToken,element.Initializer);
        context.SetOriginal(shorthand,element.AsNode());context.AssignCommentAndSourceMapRanges(shorthand,element.AsNode());return shorthand;
    }
    internal static Node ConvertBindingPatternToAssignmentPattern(EmitContext context,BindingPattern element)
    {
        switch(element.AsNode().Kind)
        {
            case Kind.ArrayBindingPattern:return convertBindingElementToArrayAssignmentPattern(context,element);
            case Kind.ObjectBindingPattern:return convertBindingElementToObjectAssignmentPattern(context,element);
            default:throw new InvalidOperationException("Unknown binding pattern");
        }
    }
    private static Node convertBindingElementToObjectAssignmentPattern(EmitContext context,BindingPattern element)
    {
        Slice<Node> properties=default;
        foreach(var child in element.Elements.Nodes) properties=properties.Append(convertBindingElementToObjectAssignmentElement(context,child.AsBindingElement()));
        var propertyList=context.Factory.NewNodeList(properties);propertyList.Loc=element.Elements.Loc;
        var result=context.Factory.NewObjectLiteralExpression(propertyList,false);
        context.SetOriginal(result,element.AsNode());context.AssignCommentAndSourceMapRanges(result,element.AsNode());return result;
    }
    private static Node convertBindingElementToArrayAssignmentPattern(EmitContext context,BindingPattern element)
    {
        Slice<Node> elements=default;
        foreach(var child in element.Elements.Nodes) elements=elements.Append(convertBindingElementToArrayAssignmentElement(context,child.AsBindingElement()));
        var list=context.Factory.NewNodeList(elements);list.Loc=element.Elements.Loc;
        var result=context.Factory.NewArrayLiteralExpression(list,false);
        context.SetOriginal(result,element.AsNode());context.AssignCommentAndSourceMapRanges(result,element.AsNode());return result;
    }
    private static Node convertBindingNameToAssignmentElementTarget(EmitContext context,Node element) =>
        Ast.IsBindingPattern(element)?ConvertBindingPatternToAssignmentPattern(context,element.AsBindingPattern()):element;
    internal static Node ConvertVariableDeclarationToAssignmentExpression(EmitContext context,VariableDeclaration element)
    {
        if(element.Initializer == null)return null;
        var expression=convertBindingNameToAssignmentElementTarget(context,element.Name());
        var assignment=context.Factory.NewAssignmentExpression(expression,element.Initializer);
        context.SetOriginal(assignment,element.AsNode());context.AssignCommentAndSourceMapRanges(assignment,element.AsNode());return assignment;
    }
    internal static Node SingleOrMany(Slice<Node> nodes,EmitFactory factory)
    {
        if(nodes.IsNil)return null;
        if(nodes.Length==1)return nodes[0];
        return factory.NewSyntaxList(nodes);
    }
    internal static bool IsSimpleCopiableExpression(Node expression) => Ast.IsStringLiteralLike(expression)||Ast.IsNumericLiteral(expression)||Ast.IsKeywordKind(expression.Kind)||Ast.IsIdentifier(expression);
    internal static bool IsOriginalNodeSingleLine(EmitContext context,Node node)
    {
        if(node==null)return false;
        var original=context.MostOriginal(node);if(original==null)return false;
        var source=Ast.GetSourceFileOfNode(original);if(source==null)return false;
        var startLine=TypeScript.Scanner.Scanner.GetECMALineOfPosition(source,original.Loc.Pos());
        var endLine=TypeScript.Scanner.Scanner.GetECMALineOfPosition(source,original.Loc.End());return startLine==endLine;
    }
    internal static bool IsSimpleInlineableExpression(Node expression) => !Ast.IsIdentifier(expression)&&IsSimpleCopiableExpression(expression);
    internal static Slice<int> FindSuperStatementIndexPath(Slice<Node> statements,int start)
    {
        var indices=findSuperStatementIndexPathWorker(statements,start,default);indices.AsSpan().Reverse();return indices;
    }
    private static Slice<int> findSuperStatementIndexPathWorker(Slice<Node> statements,int start,Slice<int> indices)
    {
        for(var i=start;i<statements.Length;i++)
        {
            var statement=statements[i];
            if(GetSuperCallFromStatement(statement)!=null)return indices.Append(i);
            else if(Ast.IsTryStatement(statement))
            {
                var result=findSuperStatementIndexPathWorker(statement.AsTryStatement().TryBlock.Statements(),0,indices);
                if(!result.IsNil)return result.Append(i);
            }
        }
        return default;
    }
    internal static Node GetSuperCallFromStatement(Node statement)
    {
        if(!Ast.IsExpressionStatement(statement))return null;
        var expression=Ast.SkipParentheses(statement.Expression());return Ast.IsSuperCall(expression)?expression:null;
    }
    internal static TextRange MoveRangePastModifiers(Node node)
    {
        if(Ast.IsPropertyDeclaration(node)||Ast.IsMethodDeclaration(node))return new(node.Name().Pos(),node.End());
        Node last=null;if(Ast.CanHaveModifiers(node))last=Core.LastOrNil(node.ModifierNodes());
        if(last!=null&&!Ast.PositionIsSynthesized(last.End()))return new(last.End(),node.End());
        return MoveRangePastDecorators(node);
    }
    internal static TextRange MoveRangePastDecorators(Node node)
    {
        Node last=null;if(Ast.CanHaveModifiers(node)) {var nodes=node.ModifierNodes();if(!nodes.IsNil)last=Core.FindLast(nodes,Ast.IsDecorator);}
        return last!=null&&!Ast.PositionIsSynthesized(last.End())?new(last.End(),node.End()):node.Loc;
    }
    internal static Kind GetNonAssignmentOperatorForCompoundAssignment(Kind kind)
    {
        switch(kind)
        {
            case Kind.PlusEqualsToken:return Kind.PlusToken;
            case Kind.MinusEqualsToken:return Kind.MinusToken;
            case Kind.AsteriskEqualsToken:return Kind.AsteriskToken;
            case Kind.AsteriskAsteriskEqualsToken:return Kind.AsteriskAsteriskToken;
            case Kind.SlashEqualsToken:return Kind.SlashToken;
            case Kind.PercentEqualsToken:return Kind.PercentToken;
            case Kind.LessThanLessThanEqualsToken:return Kind.LessThanLessThanToken;
            case Kind.GreaterThanGreaterThanEqualsToken:return Kind.GreaterThanGreaterThanToken;
            case Kind.GreaterThanGreaterThanGreaterThanEqualsToken:return Kind.GreaterThanGreaterThanGreaterThanToken;
            case Kind.AmpersandEqualsToken:return Kind.AmpersandToken;
            case Kind.BarEqualsToken:return Kind.BarToken;
            case Kind.CaretEqualsToken:return Kind.CaretToken;
            case Kind.BarBarEqualsToken:return Kind.BarBarToken;
            case Kind.AmpersandAmpersandEqualsToken:return Kind.AmpersandAmpersandToken;
            case Kind.QuestionQuestionEqualsToken:return Kind.QuestionQuestionToken;
            default:return kind;
        }
    }
}
