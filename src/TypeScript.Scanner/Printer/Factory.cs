// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/factory.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using static TypeScript.DiagnosticPrinting.EmitHelpers;
using static TypeScript.DiagnosticPrinting.PrinterUtilities;
namespace TypeScript.DiagnosticPrinting;
internal struct NameOptions { internal bool AllowComments, AllowSourceMaps; }
internal struct AssignedNameOptions { internal bool AllowComments, AllowSourceMaps, IgnoreAssignedName; }
internal readonly record struct PrivateIdentifierKind
{
    private readonly string value;
    internal string Value => value ?? "";
    internal PrivateIdentifierKind(string value) => this.value = value;
    internal static readonly PrivateIdentifierKind Field = new("f"), Method = new("m"), Accessor = new("a"), Untransformed = new("untransformed");
}
// The upstream printer factory embeds ast.NodeFactory; inheritance retains
// the same shared hook-bearing factory instance for all AST/visitor consumers.
internal sealed class NodeFactory : TypeScript.Scanner.NodeFactory
{
    private readonly EmitContext emitContext;
    internal NodeFactory(EmitContext context) : base(new NodeFactoryHooks { OnCreate = context.onCreate, OnUpdate = context.onUpdate, OnClone = context.onClone }) => emitContext = context;
    private Node newGeneratedIdentifier(GeneratedIdentifierFlags kind, string text, Node node, AutoGenerateOptions options)
    {
        uint id = unchecked((uint)System.Threading.Interlocked.Increment(ref EmitContext.nextAutoGenerateId));
        if (text.Length == 0)
        {
            text = node == null ? "(auto@" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")"
                : Ast.IsMemberName(node) ? node.Text()
                : "(generated@" + Ast.GetNodeId(emitContext.getNodeForGeneratedNameWorker(node, id)).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
            text = FormatGeneratedName(false, options.Prefix, text, options.Suffix);
        }
        var name = NewIdentifier(text);
        emitContext.autoGenerate ??= new(ReferenceEqualityComparer.Instance);
        emitContext.autoGenerate[name] = new() { Id = id, Flags = kind | (options.Flags & ~GeneratedIdentifierFlags.KindMask), Prefix = options.Prefix, Suffix = options.Suffix, Node = node };
        return name;
    }
    private Node newGeneratedPrivateIdentifier(GeneratedIdentifierFlags kind, string text, Node node, AutoGenerateOptions options)
    {
        uint id = unchecked((uint)System.Threading.Interlocked.Increment(ref EmitContext.nextAutoGenerateId));
        if (text.Length == 0)
        {
            text = node == null ? "(auto@" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")"
                : Ast.IsMemberName(node) ? node.Text()
                : "(generated@" + Ast.GetNodeId(emitContext.getNodeForGeneratedNameWorker(node, id)).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
            text = FormatGeneratedName(true, options.Prefix, text, options.Suffix);
        }
        else if (!text.StartsWith('#')) throw new InvalidOperationException("First character of private identifier must be #: " + text);
        var name = NewPrivateIdentifier(text);
        emitContext.autoGenerate ??= new(ReferenceEqualityComparer.Instance);
        emitContext.autoGenerate[name] = new() { Id = id, Flags = kind | (options.Flags & ~GeneratedIdentifierFlags.KindMask), Prefix = options.Prefix, Suffix = options.Suffix, Node = node };
        return name;
    }
// Allocates a new temp variable name, but does not record it in the environment. It is recommended to pass this to either
// `AddVariableDeclaration` or `AddLexicalDeclaration` to ensure it is properly tracked, if you are not otherwise handling
// it yourself.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:62:1.
public Node NewTempVariable()
{
    return this.NewTempVariableEx(new AutoGenerateOptions {  });
}

// Allocates a new temp variable name, but does not record it in the environment. It is recommended to pass this to either
// `AddVariableDeclaration` or `AddLexicalDeclaration` to ensure it is properly tracked, if you are not otherwise handling
// it yourself.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:69:1.
public Node NewTempVariableEx(AutoGenerateOptions options)
{
    return this.newGeneratedIdentifier(GeneratedIdentifierFlags.Auto, "", null, options);
}

// Allocates a new loop variable name.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:74:1.
public Node NewLoopVariable()
{
    return this.NewLoopVariableEx(new AutoGenerateOptions {  });
}

// Allocates a new loop variable name.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:79:1.
public Node NewLoopVariableEx(AutoGenerateOptions options)
{
    return this.newGeneratedIdentifier(GeneratedIdentifierFlags.Loop, "", null, options);
}

// Allocates a new unique name based on the provided text.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:84:1.
public Node NewUniqueName(string text)
{
    return this.NewUniqueNameEx(text, new AutoGenerateOptions {  });
}

// Allocates a new unique name based on the provided text.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:89:1.
public Node NewUniqueNameEx(string text, AutoGenerateOptions options)
{
    return this.newGeneratedIdentifier(GeneratedIdentifierFlags.Unique, text, null, options);
}

// Allocates a new unique name based on the provided node.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:94:1.
public Node NewGeneratedNameForNode(Node node)
{
    return this.NewGeneratedNameForNodeEx(node, new AutoGenerateOptions {  });
}

// Allocates a new unique name based on the provided node.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:99:1.
public Node NewGeneratedNameForNodeEx(Node node, AutoGenerateOptions options)
{
    if (((GoRuntime.Len(options.Prefix) > 0) || (GoRuntime.Len(options.Suffix) > 0)))
    {
        options.Flags |= GeneratedIdentifierFlags.Optimistic;
    }
    return this.newGeneratedIdentifier(GeneratedIdentifierFlags.Node, "", node, options);
}

// Allocates a new unique private name based on the provided text.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:140:1.
public Node NewUniquePrivateName(string text)
{
    return this.NewUniquePrivateNameEx(text, new AutoGenerateOptions {  });
}

// Allocates a new unique private name based on the provided text.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:145:1.
public Node NewUniquePrivateNameEx(string text, AutoGenerateOptions options)
{
    return this.newGeneratedPrivateIdentifier(GeneratedIdentifierFlags.Unique, text, null, options);
}

// Allocates a new unique private name based on the provided node.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:150:1.
public Node NewGeneratedPrivateNameForNode(Node node)
{
    return this.NewGeneratedPrivateNameForNodeEx(node, new AutoGenerateOptions {  });
}

// Allocates a new unique private name based on the provided node.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:155:1.
public Node NewGeneratedPrivateNameForNodeEx(Node node, AutoGenerateOptions options)
{
    if (((GoRuntime.Len(options.Prefix) > 0) || (GoRuntime.Len(options.Suffix) > 0)))
    {
        options.Flags |= GeneratedIdentifierFlags.Optimistic;
    }
    return this.newGeneratedPrivateIdentifier(GeneratedIdentifierFlags.Node, "", node, options);
}

// Allocates a new StringLiteral whose source text is derived from the provided node. This is often used to create a
// string representation of an Identifier or NumericLiteral.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:165:1.
public Node NewStringLiteralFromNode(Node textSourceNode)
{
    ByteStringValue text = ByteStringValue.FromString("");
    switch (textSourceNode.Kind)
    {
        case Kind.Identifier:
        case Kind.PrivateIdentifier:
        case Kind.JsxNamespacedName:
        case Kind.StringLiteral:
        case Kind.NumericLiteral:
        case Kind.BigIntLiteral:
        case Kind.NoSubstitutionTemplateLiteral:
        case Kind.TemplateHead:
        case Kind.TemplateMiddle:
        case Kind.TemplateTail:
        case Kind.RegularExpressionLiteral:
        {
            text = textSourceNode.TextValue();
            break;
        }
    }
    var node = this.NewStringLiteralWithTextValue(text, TokenFlags.None);
    if ((this.emitContext.textSource == null))
    {
        this.emitContext.textSource = new Dictionary<Node, Node>(ReferenceEqualityComparer.Instance);
    }
    this.emitContext.textSource[node] = textSourceNode;
    return node;
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:193:1.
public Node NewThisExpression()
{
    return this.NewKeywordExpression(Kind.ThisKeyword);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:197:1.
public Node NewTrueExpression()
{
    return this.NewKeywordExpression(Kind.TrueKeyword);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:201:1.
public Node NewFalseExpression()
{
    return this.NewKeywordExpression(Kind.FalseKeyword);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:209:1.
public Node NewCommaExpression(Node left, Node right)
{
    return this.NewBinaryExpression(null, left, null, this.NewToken(Kind.CommaToken), right);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:213:1.
public Node NewAssignmentExpression(Node left, Node right)
{
    return this.NewBinaryExpression(null, left, null, this.NewToken(Kind.EqualsToken), right);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:217:1.
public Node NewLogicalORExpression(Node left, Node right)
{
    return this.NewBinaryExpression(null, left, null, this.NewToken(Kind.BarBarToken), right);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:221:1.
public Node NewLogicalANDExpression(Node left, Node right)
{
    return this.NewBinaryExpression(null, left, null, this.NewToken(Kind.AmpersandAmpersandToken), right);
}

// func (f *NodeFactory) NewLogicalANDExpression(left *ast.Expression, right *ast.Expression) *ast.Expression
// func (f *NodeFactory) NewBitwiseORExpression(left *ast.Expression, right *ast.Expression) *ast.Expression
// func (f *NodeFactory) NewBitwiseXORExpression(left *ast.Expression, right *ast.Expression) *ast.Expression
// func (f *NodeFactory) NewBitwiseANDExpression(left *ast.Expression, right *ast.Expression) *ast.Expression
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:229:1.
public Node NewStrictEqualityExpression(Node left, Node right)
{
    return this.NewBinaryExpression(null, left, null, this.NewToken(Kind.EqualsEqualsEqualsToken), right);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:233:1.
public Node NewStrictInequalityExpression(Node left, Node right)
{
    return this.NewBinaryExpression(null, left, null, this.NewToken(Kind.ExclamationEqualsEqualsToken), right);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:241:1.
public Node NewVoidZeroExpression()
{
    return this.NewVoidExpression(this.NewNumericLiteral("0", TokenFlags.None));
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:245:1.
private static Slice<Node> flattenCommaElement(Node node, Slice<Node> expressions)
{
    if (((Ast.IsBinaryExpression(node) && Ast.NodeIsSynthesized(node)) && (node.AsBinaryExpression().OperatorToken.Kind == Kind.CommaToken)))
    {
        expressions = flattenCommaElement(node.AsBinaryExpression().Left, expressions);
        expressions = flattenCommaElement(node.AsBinaryExpression().Right, expressions);
    }
    else
    {
        expressions = expressions.Append(node);
    }
    return expressions;
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:255:1.
private static Slice<Node> flattenCommaElements(Slice<Node> expressions)
{
    Slice<Node> result = default;
    {
        foreach (var expression in GoRuntime.RangeValues(expressions))
        {
            result = flattenCommaElement(expression, result);
        }
    }
    return result;
}

// Converts a slice of expressions into a single comma-delimited expression. Returns nil if expressions is nil or empty.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:264:1.
public Node InlineExpressions(Slice<Node> expressions)
{
    if ((GoRuntime.Len(expressions) == 0))
    {
        return null;
    }
    if ((GoRuntime.Len(expressions) == 1))
    {
        return expressions[0];
    }
    expressions = flattenCommaElements(expressions);
    var expression = expressions[0];
    {
        foreach (var next in GoRuntime.RangeValues(expressions.Slice2(1, expressions.Length)))
        {
            expression = this.NewCommaExpression(expression, next);
        }
    }
    return expression;
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:283:1.
public Node CreateExpressionFromEntityName(Node node)
{
    if (Ast.IsQualifiedName(node))
    {
        var left = this.CreateExpressionFromEntityName(node.AsQualifiedName().Left);
        var right = node.AsQualifiedName().Right.Clone(this.AsNodeFactory());
        right.Loc = node.AsQualifiedName().Right.Loc;
        right.Parent = node.AsQualifiedName().Right.Parent;
        var propAccess = this.NewPropertyAccessExpression(left, null, right, NodeFlags.None);
        propAccess.Loc = node.Loc;
        return propAccess;
    }
    var res = node.Clone(this.AsNodeFactory());
    res.Loc = node.Loc;
    res.Parent = node.Parent;
    return res;
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:301:1.
public Node RestoreEnclosingLabel(Node node, LabeledStatement outermostLabeledStatement)
{
    if ((outermostLabeledStatement == null))
    {
        return node;
    }
    var innerLabel = node;
    if (Ast.IsLabeledStatement(outermostLabeledStatement.Statement))
    {
        innerLabel = this.RestoreEnclosingLabel(node, outermostLabeledStatement.Statement.AsLabeledStatement());
    }
    return this.UpdateLabeledStatement(outermostLabeledStatement, outermostLabeledStatement.Label, innerLabel);
}

// CreateForOfBindingStatement creates a statement to bind the iteration value.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:317:1.
public Node CreateForOfBindingStatement(Node node, Node boundValue)
{
    if (Ast.IsVariableDeclarationList(node))
    {
        var firstDeclaration = node.AsVariableDeclarationList().Declarations.Nodes[0];
        var updatedDeclaration = this.UpdateVariableDeclaration(firstDeclaration.AsVariableDeclaration(), firstDeclaration.Name(), null, null, boundValue);
        var statement = this.NewVariableStatement(null, this.UpdateVariableDeclarationList(node.AsVariableDeclarationList(), this.NewNodeList(Slice<Node>.FromArray([updatedDeclaration])), node.AsVariableDeclarationList().Flags));
        statement.Loc = node.Loc;
        return statement;
    }
    var updatedExpression = this.NewAssignmentExpression(node, boundValue);
    updatedExpression.Loc = node.Loc;
    var statement2 = this.NewExpressionStatement(updatedExpression);
    statement2.Loc = node.Loc;
    return statement2;
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:345:1.
public Node NewTypeCheck(Node value, string tag)
{
    if ((tag == "null"))
    {
        return this.NewStrictEqualityExpression(value, this.NewKeywordExpression(Kind.NullKeyword));
    }
    else
    {
        if ((tag == "undefined"))
        {
            return this.NewStrictEqualityExpression(value, this.NewVoidZeroExpression());
        }
        else
        {
            return this.NewStrictEqualityExpression(this.NewTypeOfExpression(value), this.NewStringLiteral(tag, TokenFlags.None));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:355:1.
public Node NewMethodCall(Node @object, Node methodName, Slice<Node> argumentsList)
{
    if ((Ast.IsCallExpression(@object) && (((@object.Flags & NodeFlags.OptionalChain) != 0))))
    {
        return this.NewCallExpression(this.NewPropertyAccessExpression(@object, null, methodName, NodeFlags.None), null, null, this.NewNodeList(argumentsList), NodeFlags.OptionalChain);
    }
    return this.NewCallExpression(this.NewPropertyAccessExpression(@object, null, methodName, NodeFlags.None), null, null, this.NewNodeList(argumentsList), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:375:1.
public Node NewGlobalMethodCall(string globalObjectName, string methodName, Slice<Node> argumentsList)
{
    return this.NewMethodCall(this.NewIdentifier(globalObjectName), this.NewIdentifier(methodName), argumentsList);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:379:1.
public Node NewFunctionCallCall(Node target, Node thisArg, Slice<Node> argumentsList)
{
    if ((thisArg == null))
    {
        throw new InvalidOperationException("Attempted to construct function call call without this argument expression");
    }
    var args = Slice<Node>.FromArray([thisArg]).Append(argumentsList);
    return this.NewMethodCall(target, this.NewIdentifier("call"), args);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:387:1.
public Node NewArraySliceCall(Node array, int start)
{
    Slice<Node> args = default;
    if ((start != 0))
    {
        args = args.Append(this.NewNumericLiteral(Strconv.Itoa(start), TokenFlags.None));
    }
    return this.NewMethodCall(array, this.NewIdentifier("slice"), args);
}

// Determines whether a node is a parenthesized expression that can be ignored when recreating outer expressions.
//
// A parenthesized expression can be ignored when all of the following are true:
//
// - It's `pos` and `end` are not -1
// - It does not have a custom source map range
// - It does not have a custom comment range
// - It does not have synthetic leading or trailing comments
//
// If an outermost parenthesized expression is ignored, but the containing expression requires a parentheses around
// the expression to maintain precedence, a new parenthesized expression should be created automatically when
// the containing expression is created/updated.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:407:1.
private bool isIgnorableParen(Node node)
{
    return (((Ast.IsParenthesizedExpression(node) && Ast.NodeIsSynthesized(node)) && Ast.RangeIsSynthesized(this.emitContext.SourceMapRange(node))) && Ast.RangeIsSynthesized(this.emitContext.CommentRange(node)));
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:416:1.
private Node updateOuterExpression(Node outerExpression, Node expression)
{
    switch (outerExpression.Kind)
    {
        case Kind.ParenthesizedExpression:
        {
            return this.UpdateParenthesizedExpression(outerExpression.AsParenthesizedExpression(), expression);
        }
        case Kind.TypeAssertionExpression:
        {
            return this.UpdateTypeAssertion(outerExpression.AsTypeAssertion(), outerExpression.Type(), expression);
        }
        case Kind.AsExpression:
        {
            return this.UpdateAsExpression(outerExpression.AsAsExpression(), expression, outerExpression.Type());
        }
        case Kind.SatisfiesExpression:
        {
            return this.UpdateSatisfiesExpression(outerExpression.AsSatisfiesExpression(), expression, outerExpression.Type());
        }
        case Kind.NonNullExpression:
        {
            return this.UpdateNonNullExpression(outerExpression.AsNonNullExpression(), expression, outerExpression.Flags);
        }
        case Kind.ExpressionWithTypeArguments:
        {
            return this.UpdateExpressionWithTypeArguments(outerExpression.AsExpressionWithTypeArguments(), expression, outerExpression.TypeArgumentList());
        }
        case Kind.PartiallyEmittedExpression:
        {
            return this.UpdatePartiallyEmittedExpression(outerExpression.AsPartiallyEmittedExpression(), expression);
        }
        default:
        {
            throw new InvalidOperationException("Unexpected outer expression kind: " + outerExpression.Kind);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:437:1.
public Node RestoreOuterExpressions(Node outerExpression, Node innerExpression, OuterExpressionKinds kinds)
{
    if ((((outerExpression != null) && Ast.IsOuterExpression(outerExpression, kinds)) && !this.isIgnorableParen(outerExpression)))
    {
        return this.updateOuterExpression(outerExpression, this.RestoreOuterExpressions(outerExpression.Expression(), innerExpression, OuterExpressionKinds.All));
    }
    return innerExpression;
}

// Ensures `"use strict"` is the first statement of a slice of statements.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:448:1.
public Slice<Node> EnsureUseStrict(Slice<Node> statements)
{
    {
        foreach (var statement in GoRuntime.RangeValues(statements))
        {
            if ((Ast.IsPrologueDirective(statement) && (statement.Expression().TextEquals("use strict"u8))))
            {
                return statements;
            }
            else
            {
                break;
            }
        }
    }
    var useStrictPrologue = this.NewExpressionStatement(this.NewStringLiteral("use strict", TokenFlags.None));
    statements = Slice<Node>.FromArray([useStrictPrologue]).Append(statements);
    return statements;
}

// Splits a slice of statements into two parts: standard prologue statements and the rest of the statements
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:462:1.
public (Slice<Node>, Slice<Node>) SplitStandardPrologue(Slice<Node> source)
{
    {
        foreach (var range4 in GoRuntime.Range(source))
        {
            var i = range4.Key;
            var statement = range4.Value;
            if (!Ast.IsPrologueDirective(statement))
            {
                return (source.Slice2(0, i), source.Slice2(i, source.Length));
            }
        }
    }
    return (source, default);
}

// Splits a slice of statements into two parts: custom prologue statements (e.g., with `EmitFlags.CustomPrologue` set) and the rest of the statements
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:472:1.
public (Slice<Node>, Slice<Node>) SplitCustomPrologue(Slice<Node> source)
{
    {
        foreach (var range5 in GoRuntime.Range(source))
        {
            var i = range5.Key;
            var statement = range5.Value;
            if ((Ast.IsPrologueDirective(statement) || ((this.emitContext.EmitFlags(statement) & EmitFlags.CustomPrologue) == 0)))
            {
                return (source.Slice2(0, i), source.Slice2(i, source.Length));
            }
        }
    }
    return (default, source);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:496:1.
private Node getName(Node node, EmitFlags emitFlags, AssignedNameOptions opts)
{
    Node nodeName = default;
    if ((node != null))
    {
        if (opts.IgnoreAssignedName)
        {
            nodeName = Ast.GetNonAssignedNameOfDeclaration(node);
        }
        else
        {
            nodeName = Ast.GetNameOfDeclaration(node);
        }
    }
    if ((nodeName != null))
    {
        var name = nodeName.Clone(this);
        if (!opts.AllowComments)
        {
            emitFlags |= EmitFlags.NoComments;
        }
        if (!opts.AllowSourceMaps)
        {
            emitFlags |= EmitFlags.NoSourceMap;
        }
        this.emitContext.AddEmitFlags(name, emitFlags);
        return name;
    }
    return this.NewGeneratedNameForNode(node);
}

// Gets the local name of a declaration. This is primarily used for declarations that can be referred to by name in the
// declaration's immediate scope (classes, enums, namespaces). A local name will *never* be prefixed with a module or
// namespace export modifier like "exports." when emitted as an expression.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:524:1.
public Node GetLocalName(Node node)
{
    return this.GetLocalNameEx(node, new AssignedNameOptions {  });
}

// Gets the local name of a declaration. This is primarily used for declarations that can be referred to by name in the
// declaration's immediate scope (classes, enums, namespaces). A local name will *never* be prefixed with a module or
// namespace export modifier like "exports." when emitted as an expression.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:531:1.
public Node GetLocalNameEx(Node node, AssignedNameOptions opts)
{
    return this.getName(node, EmitFlags.LocalName, opts);
}

// Gets the export name of a declaration. This is primarily used for declarations that can be
// referred to by name in the declaration's immediate scope (classes, enums, namespaces). An
// export name will *always* be prefixed with an module or namespace export modifier like
// `"exports."` when emitted as an expression if the name points to an exported symbol.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:539:1.
public Node GetExportName(Node node)
{
    return this.GetExportNameEx(node, new AssignedNameOptions {  });
}

// Gets the export name of a declaration. This is primarily used for declarations that can be
// referred to by name in the declaration's immediate scope (classes, enums, namespaces). An
// export name will *always* be prefixed with an module or namespace export modifier like
// `"exports."` when emitted as an expression if the name points to an exported symbol.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:547:1.
public Node GetExportNameEx(Node node, AssignedNameOptions opts)
{
    return this.getName(node, EmitFlags.ExportName, opts);
}

// Gets the name of a declaration to use during emit.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:552:1.
public Node GetDeclarationName(Node node)
{
    return this.GetDeclarationNameEx(node, new NameOptions {  });
}

// Gets the name of a declaration to use during emit.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:557:1.
public Node GetDeclarationNameEx(Node node, NameOptions opts)
{
    return this.getName(node, EmitFlags.None, new AssignedNameOptions { AllowComments = opts.AllowComments, AllowSourceMaps = opts.AllowSourceMaps });
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:561:1.
public Node GetNamespaceMemberName(Node ns, Node name, NameOptions opts)
{
    if (!this.emitContext.HasAutoGenerateInfo(name))
    {
        name = name.Clone(this);
    }
    var qualifiedName = this.NewPropertyAccessExpression(ns, null, name, NodeFlags.None);
    this.emitContext.AssignCommentAndSourceMapRanges(qualifiedName, name);
    if (!opts.AllowComments)
    {
        this.emitContext.AddEmitFlags(qualifiedName, EmitFlags.NoComments);
    }
    if (!opts.AllowSourceMaps)
    {
        this.emitContext.AddEmitFlags(qualifiedName, EmitFlags.NoSourceMap);
    }
    return qualifiedName;
}

// Gets the export name of a declaration for use in expressions.
//
// An export name will *always* be prefixed with a module or namespace export modifier like
// `"exports."` when emitted as an expression if the name points to an exported symbol.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:580:1.
public Node GetExternalModuleOrNamespaceExportName(Node ns, Node node, bool allowComments, bool allowSourceMaps)
{
    if (((ns != null) && Ast.HasSyntacticModifier(node, ModifierFlags.Export)))
    {
        var nameOpts = new NameOptions { AllowComments = allowComments, AllowSourceMaps = allowSourceMaps };
        return this.GetNamespaceMemberName(ns, this.GetDeclarationNameEx(node, nameOpts), nameOpts);
    }
    return this.GetExportNameEx(node, new AssignedNameOptions { AllowComments = allowComments, AllowSourceMaps = allowSourceMaps });
}

// Allocates a new Identifier representing a reference to a helper function.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:593:1.
public Node NewUnscopedHelperName(string name)
{
    var node = this.NewIdentifier(name);
    this.emitContext.SetEmitFlags(node, EmitFlags.HelperName);
    return node;
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:601:1.
public Node NewDecorateHelper(Slice<Node> decoratorExpressions, Node target, Node memberName, Node descriptor)
{
    this.emitContext.RequestEmitHelper(decorateHelper);
    Slice<Node> argumentsArray = default;
    argumentsArray = argumentsArray.Append(this.NewArrayLiteralExpression(this.NewNodeList(decoratorExpressions), true));
    argumentsArray = argumentsArray.Append(target);
    if ((memberName != null))
    {
        argumentsArray = argumentsArray.Append(memberName);
        if ((descriptor != null))
        {
            argumentsArray = argumentsArray.Append(descriptor);
        }
    }
    return this.NewCallExpression(this.NewUnscopedHelperName("__decorate"), null, null, this.NewNodeList(argumentsArray), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:623:1.
public Node NewMetadataHelper(string metadataKey, Node metadataValue)
{
    this.emitContext.RequestEmitHelper(metadataHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__metadata"), null, null, this.NewNodeList(Slice<Node>.FromArray([this.NewStringLiteral(metadataKey, TokenFlags.None), metadataValue])), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:638:1.
public Node NewParamHelper(Node expression, int parameterOffset, TextRange location)
{
    this.emitContext.RequestEmitHelper(paramHelper);
    var helper = this.NewCallExpression(this.NewUnscopedHelperName("__param"), null, null, this.NewNodeList(Slice<Node>.FromArray([this.NewNumericLiteral(Strconv.Itoa(parameterOffset), TokenFlags.None), expression])), NodeFlags.None);
    helper.Loc = location;
    return helper;
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:653:1.
public Node NewAddDisposableResourceHelper(Node envBinding, Node value, bool async)
{
    this.emitContext.RequestEmitHelper(addDisposableResourceHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__addDisposableResource"), null, null, this.NewNodeList(Slice<Node>.FromArray([envBinding, value, this.NewKeywordExpression(Core.IfElse(async, Kind.TrueKeyword, Kind.FalseKeyword))])), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:664:1.
public Node NewDisposeResourcesHelper(Node envBinding)
{
    this.emitContext.RequestEmitHelper(disposeResourcesHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__disposeResources"), null, null, this.NewNodeList(Slice<Node>.FromArray([envBinding])), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:686:1.
public Node NewClassPrivateFieldGetHelper(Node receiver, Node state, PrivateIdentifierKind kind, Node fn)
{
    this.emitContext.RequestEmitHelper(classPrivateFieldGetHelper);
    Slice<Node> args = default;
    if ((fn == null))
    {
        args = Slice<Node>.FromArray([receiver, state, this.NewStringLiteral(kind.Value, TokenFlags.None)]);
    }
    else
    {
        args = Slice<Node>.FromArray([receiver, state, this.NewStringLiteral(kind.Value, TokenFlags.None), fn]);
    }
    return this.NewCallExpression(this.NewUnscopedHelperName("__classPrivateFieldGet"), null, null, this.NewNodeList(args), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:703:1.
public Node NewClassPrivateFieldSetHelper(Node receiver, Node state, Node value, PrivateIdentifierKind kind, Node fn)
{
    this.emitContext.RequestEmitHelper(classPrivateFieldSetHelper);
    Slice<Node> args = default;
    if ((fn == null))
    {
        args = Slice<Node>.FromArray([receiver, state, value, this.NewStringLiteral(kind.Value, TokenFlags.None)]);
    }
    else
    {
        args = Slice<Node>.FromArray([receiver, state, value, this.NewStringLiteral(kind.Value, TokenFlags.None), fn]);
    }
    return this.NewCallExpression(this.NewUnscopedHelperName("__classPrivateFieldSet"), null, null, this.NewNodeList(args), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:720:1.
public Node NewClassPrivateFieldInHelper(Node state, Node receiver)
{
    this.emitContext.RequestEmitHelper(classPrivateFieldInHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__classPrivateFieldIn"), null, null, this.NewNodeList(Slice<Node>.FromArray([state, receiver])), NodeFlags.None);
}

// Creates `Object.defineProperty(target, name, descriptor)`.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:732:1.
public Node NewObjectDefinePropertyCall(Node target, Node name, Node descriptor)
{
    return this.NewCallExpression(this.NewPropertyAccessExpression(this.NewIdentifier("Object"), null, this.NewIdentifier("defineProperty"), NodeFlags.None), null, null, this.NewNodeList(Slice<Node>.FromArray([target, name, descriptor])), NodeFlags.None);
}

// Creates `Reflect.get(target, propertyKey, receiver)`.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:748:1.
public Node NewReflectGetCall(Node target, Node propertyKey, Node receiver)
{
    return this.NewCallExpression(this.NewPropertyAccessExpression(this.NewIdentifier("Reflect"), null, this.NewIdentifier("get"), NodeFlags.None), null, null, this.NewNodeList(Slice<Node>.FromArray([target, propertyKey, receiver])), NodeFlags.None);
}

// Creates `Reflect.set(target, propertyKey, value, receiver)`.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:764:1.
public Node NewReflectSetCall(Node target, Node propertyKey, Node value, Node receiver)
{
    return this.NewCallExpression(this.NewPropertyAccessExpression(this.NewIdentifier("Reflect"), null, this.NewIdentifier("set"), NodeFlags.None), null, null, this.NewNodeList(Slice<Node>.FromArray([target, propertyKey, value, receiver])), NodeFlags.None);
}

// Creates `target.bind(thisArg, ...args)`.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:780:1.
public Node NewFunctionBindCall(Node target, Node thisArg, Slice<Node> argumentsList)
{
    var args = Slice<Node>.Make(0);
    args = args.Append(thisArg);
    args = args.Append(argumentsList);
    return this.NewMethodCall(target, this.NewIdentifier("bind"), args);
}

// Creates `(() => { ...statements })()` — an immediately invoked arrow function.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:788:1.
public Node NewImmediatelyInvokedArrowFunction(Slice<Node> statements)
{
    var arrow = this.NewArrowFunction(null, null, this.NewNodeList(Slice<Node>.FromArray([])), null, null, this.NewToken(Kind.EqualsGreaterThanToken), this.NewBlock(this.NewNodeList(statements), true));
    return this.NewCallExpression(this.NewParenthesizedExpression(arrow), null, null, this.NewNodeList(Slice<Node>.FromArray([])), NodeFlags.None);
}

// Creates `export default <expression>;`.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:808:1.
public Node NewExportDefault(Node expression)
{
    return this.NewExportAssignment(null, false, null, expression);
}

// Creates `export { <name> };`.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:813:1.
public Node NewExternalModuleExport(Node name)
{
    var specifier = this.NewExportSpecifier(false, null, name);
    var namedExports = this.NewNamedExports(this.NewNodeList(Slice<Node>.FromArray([specifier])));
    return this.NewExportDeclaration(null, false, namedExports, null, null);
}

// ES2018 Helpers
// Chains a sequence of expressions using the __assign helper or Object.assign if available in the target
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:821:1.
public Node NewAssignHelper(Slice<Node> attributesSegments, ScriptTarget scriptTarget)
{
    return this.NewCallExpression(this.NewPropertyAccessExpression(this.NewIdentifier("Object"), null, this.NewIdentifier("assign"), NodeFlags.None), null, null, this.NewNodeList(attributesSegments), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:827:1.
public Node NewRestHelper(Node value, Slice<Node> elements, Slice<Node> computedTempVariables, TextRange location)
{
    this.emitContext.RequestEmitHelper(restHelper);
    Slice<Node> propertyNames = default;
    var computedTempVariableOffset = 0;
    {
        foreach (var range6 in GoRuntime.Range(elements))
        {
            var i = range6.Key;
            var element = range6.Value;
            if ((i == (GoRuntime.Len(elements) - 1)))
            {
                break;
            }
            var propertyName = Ast.TryGetPropertyNameOfBindingOrAssignmentElement(element);
            if ((propertyName != null))
            {
                if (Ast.IsComputedPropertyName(propertyName))
                {
                    if (computedTempVariables.IsNil) throw new InvalidOperationException("Encountered computed property name but 'computedTempVariables' argument was not provided.");
                    var temp = computedTempVariables[computedTempVariableOffset];
                    computedTempVariableOffset++;
                    propertyNames = propertyNames.Append(this.NewConditionalExpression(this.NewTypeCheck(temp, "symbol"), this.NewToken(Kind.QuestionToken), temp, this.NewToken(Kind.ColonToken), this.NewBinaryExpression(null, temp, null, this.NewToken(Kind.PlusToken), this.NewStringLiteral("", TokenFlags.None))));
                }
                else
                {
                    propertyNames = propertyNames.Append(this.NewStringLiteralFromNode(propertyName));
                }
            }
        }
    }
    var propNames = this.NewArrayLiteralExpression(this.NewNodeList(propertyNames), false);
    propNames.Loc = location;
    return this.NewCallExpression(this.NewUnscopedHelperName("__rest"), null, null, this.NewNodeList(Slice<Node>.FromArray([value, propNames])), NodeFlags.None);
}

// Allocates a new Call expression to the `__await` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:871:1.
public Node NewAwaitHelper(Node expression)
{
    this.emitContext.RequestEmitHelper(awaitHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__await"), null, null, this.NewNodeList(Slice<Node>.FromArray([expression])), NodeFlags.None);
}

// Allocates a new Call expression to the `__asyncGenerator` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:883:1.
public Node NewAsyncGeneratorHelper(Node generatorFunc, bool hasLexicalThis)
{
    this.emitContext.RequestEmitHelper(awaitHelper);
    this.emitContext.RequestEmitHelper(asyncGeneratorHelper);
    this.emitContext.AddEmitFlags(generatorFunc, (EmitFlags.AsyncFunctionBody | EmitFlags.ReuseTempVariableScope));
    Node thisArg = default;
    if (hasLexicalThis)
    {
        thisArg = this.NewKeywordExpression(Kind.ThisKeyword);
    }
    else
    {
        thisArg = this.NewVoidZeroExpression();
    }
    return this.NewCallExpression(this.NewUnscopedHelperName("__asyncGenerator"), null, null, this.NewNodeList(Slice<Node>.FromArray([thisArg, this.NewIdentifier("arguments"), generatorFunc])), NodeFlags.None);
}

// Allocates a new Call expression to the `__asyncDelegator` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:914:1.
public Node NewAsyncDelegatorHelper(Node expression)
{
    this.emitContext.RequestEmitHelper(awaitHelper);
    this.emitContext.RequestEmitHelper(asyncDelegatorHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__asyncDelegator"), null, null, this.NewNodeList(Slice<Node>.FromArray([expression])), NodeFlags.None);
}

// Allocates a new Call expression to the `__asyncValues` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:927:1.
public Node NewAsyncValuesHelper(Node expression)
{
    this.emitContext.RequestEmitHelper(asyncValuesHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__asyncValues"), null, null, this.NewNodeList(Slice<Node>.FromArray([expression])), NodeFlags.None);
}

// Allocates a new Call expression to the `__awaiter` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:941:1.
public Node NewAwaiterHelper(bool hasLexicalThis, Node argumentsExpression, NodeList parameters, Node body)
{
    this.emitContext.RequestEmitHelper(awaiterHelper);
    NodeList @params = default;
    if ((parameters != null))
    {
        @params = parameters;
    }
    else
    {
        @params = this.NewNodeList(Slice<Node>.FromArray([]));
    }
    var generatorFunc = this.NewFunctionExpression(null, this.NewToken(Kind.AsteriskToken), null, null, @params, null, null, body);
    this.emitContext.AddEmitFlags(generatorFunc, (EmitFlags.AsyncFunctionBody | EmitFlags.ReuseTempVariableScope));
    Node thisArg = default;
    if (hasLexicalThis)
    {
        thisArg = this.NewKeywordExpression(Kind.ThisKeyword);
    }
    else
    {
        thisArg = this.NewVoidZeroExpression();
    }
    Node argsArg = default;
    if ((argumentsExpression != null))
    {
        argsArg = argumentsExpression;
    }
    else
    {
        argsArg = this.NewVoidZeroExpression();
    }
    return this.NewCallExpression(this.NewUnscopedHelperName("__awaiter"), null, null, this.NewNodeList(Slice<Node>.FromArray([thisArg, argsArg, this.NewVoidZeroExpression(), generatorFunc])), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1000:1.
public Node NewESDecorateClassContextObject(Node nameExpr, Node metadata)
{
    var props = Slice<Node>.FromArray([this.NewPropertyAssignment(null, this.NewIdentifier("kind"), null, null, this.NewStringLiteral("class", 0)), this.NewPropertyAssignment(null, this.NewIdentifier("name"), null, null, nameExpr), this.NewPropertyAssignment(null, this.NewIdentifier("metadata"), null, null, metadata)]);
    return this.NewObjectLiteralExpression(this.NewNodeList(props), false);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1009:1.
public Node NewESDecorateClassElementAccessGetMethod(bool nameComputed, Node nameExpr)
{
    Node accessor = default;
    if (nameComputed)
    {
        accessor = this.NewElementAccessExpression(this.NewIdentifier("obj"), null, nameExpr, NodeFlags.None);
    }
    else
    {
        accessor = this.NewPropertyAccessExpression(this.NewIdentifier("obj"), null, nameExpr, NodeFlags.None);
    }
    var objParam = this.NewParameterDeclaration(null, null, this.NewIdentifier("obj"), null, null, null);
    var arrow = this.NewArrowFunction(null, null, this.NewNodeList(Slice<Node>.FromArray([objParam])), null, null, this.NewToken(Kind.EqualsGreaterThanToken), accessor);
    return this.NewPropertyAssignment(null, this.NewIdentifier("get"), null, null, arrow);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1033:1.
public Node NewESDecorateClassElementAccessSetMethod(bool nameComputed, Node nameExpr)
{
    Node accessor = default;
    if (nameComputed)
    {
        accessor = this.NewElementAccessExpression(this.NewIdentifier("obj"), null, nameExpr, NodeFlags.None);
    }
    else
    {
        accessor = this.NewPropertyAccessExpression(this.NewIdentifier("obj"), null, nameExpr, NodeFlags.None);
    }
    var assignment = this.NewAssignmentExpression(accessor, this.NewIdentifier("value"));
    var stmt = this.NewExpressionStatement(assignment);
    var body = this.NewBlock(this.NewNodeList(Slice<Node>.FromArray([stmt])), false);
    var objParam = this.NewParameterDeclaration(null, null, this.NewIdentifier("obj"), null, null, null);
    var valueParam = this.NewParameterDeclaration(null, null, this.NewIdentifier("value"), null, null, null);
    var arrow = this.NewArrowFunction(null, null, this.NewNodeList(Slice<Node>.FromArray([objParam, valueParam])), null, null, this.NewToken(Kind.EqualsGreaterThanToken), body);
    return this.NewPropertyAssignment(null, this.NewIdentifier("set"), null, null, arrow);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1062:1.
public Node NewESDecorateClassElementAccessHasMethod(bool nameComputed, Node nameExpr)
{
    Node propertyName = default;
    if (((!nameComputed && (nameExpr != null)) && Ast.IsIdentifier(nameExpr)))
    {
        propertyName = this.NewStringLiteralFromNode(nameExpr);
    }
    else
    {
        propertyName = nameExpr;
    }
    var objParam = this.NewParameterDeclaration(null, null, this.NewIdentifier("obj"), null, null, null);
    var inExpr = this.NewBinaryExpression(null, propertyName, null, this.NewToken(Kind.InKeyword), this.NewIdentifier("obj"));
    var arrow = this.NewArrowFunction(null, null, this.NewNodeList(Slice<Node>.FromArray([objParam])), null, null, this.NewToken(Kind.EqualsGreaterThanToken), inExpr);
    return this.NewPropertyAssignment(null, this.NewIdentifier("has"), null, null, arrow);
}

// Creates the "access" object for a class element decorator context.
//
// 15.7.3 CreateDecoratorAccessObject (kind, name)
//
//  2. If _kind_ is ~field~, ~method~, ~accessor~, or ~getter~, then
//     a. Let _getAccess_ be a new Abstract Closure with parameters (_object_) that captures _kind_ and _name_ ...
//     b. Perform ! CreateDataPropertyOrThrow(_access_, "get", _getAccess_).
//  3. If _kind_ is ~field~, ~accessor~, or ~setter~, then
//     a. Let _setAccess_ be a new Abstract Closure with parameters (_object_, _value_) that captures _kind_ and _name_ ...
//     b. Perform ! CreateDataPropertyOrThrow(_access_, "set", _setAccess_).
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1098:1.
public Node NewESDecorateClassElementAccessObject(bool nameComputed, Node nameExpr, bool hasGet, bool hasSet)
{
    var accessProps = Slice<Node>.FromArray([]);
    accessProps = accessProps.Append(this.NewESDecorateClassElementAccessHasMethod(nameComputed, nameExpr));
    if (hasGet)
    {
        accessProps = accessProps.Append(this.NewESDecorateClassElementAccessGetMethod(nameComputed, nameExpr));
    }
    if (hasSet)
    {
        accessProps = accessProps.Append(this.NewESDecorateClassElementAccessSetMethod(nameComputed, nameExpr));
    }
    return this.NewObjectLiteralExpression(this.NewNodeList(accessProps), false);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1122:1.
public Node NewESDecorateClassElementContextObject(string kind, bool nameComputed, Node nameExpr, bool isStatic, bool isPrivate, bool hasGet, bool hasSet, Node metadata)
{
    Node nameValue = default;
    if (((!nameComputed && (nameExpr != null)) && ((Ast.IsPrivateIdentifier(nameExpr) || Ast.IsIdentifier(nameExpr)))))
    {
        nameValue = this.NewStringLiteralFromNode(nameExpr);
    }
    else
    {
        nameValue = nameExpr;
    }
    var accessObj = this.NewESDecorateClassElementAccessObject(nameComputed, nameExpr, hasGet, hasSet);
    Node staticExpr = default;
    if (isStatic)
    {
        staticExpr = this.NewTrueExpression();
    }
    else
    {
        staticExpr = this.NewFalseExpression();
    }
    Node privateExpr = default;
    if (isPrivate)
    {
        privateExpr = this.NewTrueExpression();
    }
    else
    {
        privateExpr = this.NewFalseExpression();
    }
    var props = Slice<Node>.FromArray([this.NewPropertyAssignment(null, this.NewIdentifier("kind"), null, null, this.NewStringLiteral(kind, 0)), this.NewPropertyAssignment(null, this.NewIdentifier("name"), null, null, nameValue), this.NewPropertyAssignment(null, this.NewIdentifier("static"), null, null, staticExpr), this.NewPropertyAssignment(null, this.NewIdentifier("private"), null, null, privateExpr), this.NewPropertyAssignment(null, this.NewIdentifier("access"), null, null, accessObj), this.NewPropertyAssignment(null, this.NewIdentifier("metadata"), null, null, metadata)]);
    return this.NewObjectLiteralExpression(this.NewNodeList(props), false);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1168:1.
public Node NewESDecorateHelper(Node ctor, Node descriptorIn, Node decorators, Node contextIn, Node initializers, Node extraInitializers)
{
    this.emitContext.RequestEmitHelper(esDecorateHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__esDecorate"), null, null, this.NewNodeList(Slice<Node>.FromArray([ctor, descriptorIn, decorators, contextIn, initializers, extraInitializers])), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1179:1.
public Node NewRunInitializersHelper(Node thisArg, Node initializers, Node value)
{
    this.emitContext.RequestEmitHelper(runInitializersHelper);
    Slice<Node> arguments = default;
    if ((value != null))
    {
        arguments = Slice<Node>.FromArray([thisArg, initializers, value]);
    }
    else
    {
        arguments = Slice<Node>.FromArray([thisArg, initializers]);
    }
    return this.NewCallExpression(this.NewUnscopedHelperName("__runInitializers"), null, null, this.NewNodeList(arguments), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1198:1.
public Node NewTemplateObjectHelper(Node cookedArray, Node rawArray)
{
    this.emitContext.RequestEmitHelper(makeTemplateObjectHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__makeTemplateObject"), null, null, this.NewNodeList(Slice<Node>.FromArray([cookedArray, rawArray])), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1209:1.
public Node NewPropKeyHelper(Node expr)
{
    this.emitContext.RequestEmitHelper(propKeyHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__propKey"), null, null, this.NewNodeList(Slice<Node>.FromArray([expr])), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1220:1.
public Node NewSetFunctionNameHelper(Node fn, Node name, string prefix)
{
    this.emitContext.RequestEmitHelper(setFunctionNameHelper);
    Slice<Node> arguments = default;
    if ((GoRuntime.Len(prefix) > 0))
    {
        arguments = Slice<Node>.FromArray([fn, name, this.NewStringLiteral(prefix, TokenFlags.None)]);
    }
    else
    {
        arguments = Slice<Node>.FromArray([fn, name]);
    }
    return this.NewCallExpression(this.NewUnscopedHelperName("__setFunctionName"), null, null, this.NewNodeList(arguments), NodeFlags.None);
}

// Allocates a new Call expression to the `__importDefault` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1240:1.
public Node NewImportDefaultHelper(Node expression)
{
    this.emitContext.RequestEmitHelper(importDefaultHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__importDefault"), null, null, this.NewNodeList(Slice<Node>.FromArray([expression])), NodeFlags.None);
}

// Allocates a new Call expression to the `__importStar` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1252:1.
public Node NewImportStarHelper(Node expression)
{
    this.emitContext.RequestEmitHelper(importStarHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__importStar"), null, null, this.NewNodeList(Slice<Node>.FromArray([expression])), NodeFlags.None);
}

// Allocates a new Call expression to the `__exportStar` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1264:1.
public Node NewExportStarHelper(Node moduleExpression, Node exportsExpression)
{
    this.emitContext.RequestEmitHelper(exportStarHelper);
    return this.NewCallExpression(this.NewUnscopedHelperName("__exportStar"), null, null, this.NewNodeList(Slice<Node>.FromArray([moduleExpression, exportsExpression])), NodeFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1275:1.
public Node NewAssignmentTargetWrapper(Node paramName, Node expression)
{
    var setAccessor = this.NewSetAccessorDeclaration(null, this.NewIdentifier("value"), null, this.NewNodeList(Slice<Node>.FromArray([this.NewParameterDeclaration(null, null, paramName, null, null, null)])), null, null, this.NewBlock(this.NewNodeList(Slice<Node>.FromArray([this.NewExpressionStatement(expression)])), false));
    var objLiteral = this.NewObjectLiteralExpression(this.NewNodeList(Slice<Node>.FromArray([setAccessor])), false);
    return this.NewPropertyAccessExpression(this.NewParenthesizedExpression(objLiteral), null, this.NewIdentifier("value"), NodeFlags.None);
}

// Allocates a new Call expression to the `__rewriteRelativeImportExtension` helper.
// Upstream upstream/TypeScript/tsc/internal/printer/factory.go:1300:1.
public Node NewRewriteRelativeImportExtensionsHelper(Node firstArgument, bool preserveJsx)
{
    this.emitContext.RequestEmitHelper(rewriteRelativeImportExtensionsHelper);
    Slice<Node> arguments = default;
    if (preserveJsx)
    {
        arguments = Slice<Node>.FromArray([firstArgument, this.NewToken(Kind.TrueKeyword)]);
    }
    else
    {
        arguments = Slice<Node>.FromArray([firstArgument]);
    }
    return this.NewCallExpression(this.NewUnscopedHelperName("__rewriteRelativeImportExtension"), null, null, this.NewNodeList(arguments), NodeFlags.None);
}

}
