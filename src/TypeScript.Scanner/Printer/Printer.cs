// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/printer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal sealed partial class Printer
{
    private readonly EmitContext emitContext;
    private readonly bool removeComments;
    private readonly bool omitTrailingSemicolon;
    private readonly bool neverAsciiEscape;
    internal Printer(EmitContext context, bool removeComments = true, bool omitTrailingSemicolon = false, bool neverAsciiEscape = false)
    {
        emitContext = context;
        this.removeComments = removeComments;
        this.omitTrailingSemicolon = omitTrailingSemicolon;
        this.neverAsciiEscape = neverAsciiEscape;
    }

    private TextWriter writer;
    private bool inExtends;
    private SourceFile currentSourceFile;
    // Write (5076): diagnostic data nodes and selected source reuse; source comments/maps excluded.
    internal void Write(Node node, TextWriter output, SourceFile sourceFile = null)
    {
        var savedWriter = writer;
        var savedSourceFile = currentSourceFile;
        currentSourceFile = sourceFile;
        writer = omitTrailingSemicolon ? trailingSemicolonDeferringWriter.getTrailingSemicolonDeferringWriter(output) : output;
        writer.Clear();
        // printer.go:5076. Signature elements and type members are direct
        // printer entries, not TypeNodes subject to type precedence.
        if (node != null)
        {
            switch (node.Kind)
            {
                case Kind.ComputedPropertyName:
                    emitComputedPropertyName(node.AsComputedPropertyName());
                    break;
                case Kind.TypeParameter:
                    emitTypeParameter(node.AsTypeParameterDeclaration());
                    break;
                case Kind.Parameter:
                    emitParameter(node.AsParameterDeclaration());
                    break;
                case Kind.PropertySignature:
                case Kind.MethodSignature:
                case Kind.CallSignature:
                case Kind.ConstructSignature:
                case Kind.IndexSignature:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                case Kind.NotEmittedTypeElement:
                    emitTypeElement(node);
                    break;
                case Kind.ObjectBindingPattern:
                case Kind.ArrayBindingPattern:
                    emitBindingName(node);
                    break;
                case Kind.BindingElement:
                    emitBindingElement(node.AsBindingElement());
                    break;
                default:
                    if (Ast.IsEntityName(node))
                        emitEntityName(node);
                    else if (Ast.IsExpression(node))
                        emitExpression(node);
                    else
                        emitTypeNode(node);
                    break;
            }
        }
        writer = savedWriter;
        currentSourceFile = savedSourceFile;
    }

    private void emitKeywordTypeNode(Node node) => emitKeywordNode(node);
    private void emitKeywordNode(Node node)
    {
        if (node == null)
            return;
        // emitKeywordNodeEx -> enterTokenNode -> emitCommentsBeforeNode.
        // Selected synthesized keyword nodes have no source-range/trailing comments.
        if (shouldEmitComments(node))
            emitLeadingSyntheticCommentsOfNode(node, emitContext.EmitFlags(node));
        writeTokenText(node.Kind);
    }

    // printer.go:742. RemoveComments=false alone does not make comments emit:
    // the original printer also requires a current source file.
    private bool shouldEmitComments(Node node) => !removeComments && currentSourceFile != null && !Ast.IsSourceFile(node);
    private void writeTokenText(Kind token) => writer.WriteKeyword(TypeScript.Scanner.Scanner.TokenToString(token));
    private void emitLiteralType(Node node) => emitExpression(node.AsLiteralTypeNode().Literal);
    private void emitExpression(Node node) => emitExpression(node, OperatorPrecedence.Comma);

    // printer.go:3222. Preserve expression precedence in diagnostic computed
    // names and cloned binding signatures as well as literal type operands.
    private void emitExpression(Node node, OperatorPrecedence precedence)
    {
        bool parens = Ast.GetExpressionPrecedence(Ast.SkipPartiallyEmittedExpressions(node)) < precedence;
        if (parens) writePunctuation("(");
        emitExpressionWorker(node);
        if (parens) writePunctuation(")");
    }

    private void emitExpressionWorker(Node node)
    {
        switch (node.Kind)
        {
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
                emitLiteral(node);
                return;
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
                emitKeywordNode(node);
                return;
            case Kind.Identifier:
                emitIdentifierReference(node.AsIdentifier());
                return;
            case Kind.PrivateIdentifier:
                emitPrivateIdentifier(node.AsPrivateIdentifier());
                return;
            case Kind.ThisKeyword:
            case Kind.SuperKeyword:
            case Kind.ImportKeyword:
                emitKeywordNode(node);
                return;
            case Kind.PropertyAccessExpression:
                var property = node.AsPropertyAccessExpression();
                emitExpression(property.Expression, Ast.IsOptionalChain(node) ? OperatorPrecedence.OptionalChain : OperatorPrecedence.Member);
                if (property.QuestionDotToken == null && mayNeedDotDotForPropertyAccess(property.Expression) &&
                    !writer.HasTrailingComment() && !writer.HasTrailingWhitespace())
                    writePunctuation(".");
                writePunctuation(property.QuestionDotToken != null ? "?." : ".");
                emitMemberName(property.Name());
                return;
            case Kind.ElementAccessExpression:
                var element = node.AsElementAccessExpression();
                emitExpression(element.Expression, Ast.IsOptionalChain(node) ? OperatorPrecedence.OptionalChain : OperatorPrecedence.Member);
                if (element.QuestionDotToken != null) writePunctuation("?.");
                writePunctuation("[");
                emitExpression(element.ArgumentExpression);
                writePunctuation("]");
                return;
            case Kind.CallExpression:
                emitCallExpression(node.AsCallExpression());
                return;
            case Kind.NewExpression:
                emitNewExpression(node.AsNewExpression());
                return;
            case Kind.BinaryExpression:
                emitBinaryExpression(node.AsBinaryExpression());
                return;
            case Kind.ParenthesizedExpression:
                writePunctuation("(");
                emitExpression(node.Expression());
                writePunctuation(")");
                return;
            case Kind.ExpressionWithTypeArguments:
                emitExpression(node.Expression(), OperatorPrecedence.Member);
                emitTypeArguments(node, node.AsExpressionWithTypeArguments().TypeArguments);
                return;
            case Kind.PrefixUnaryExpression:
                var unary = node.AsPrefixUnaryExpression();
                if (unary.Operator is not (Kind.MinusToken or Kind.PlusToken))
                    throw TypeScript.Checking.Checker.unsupported("Printer.emitExpression: unary operator");
                writer.Write(unary.Operator == Kind.MinusToken ? "-" : "+");
                emitExpression(unary.Operand, OperatorPrecedence.Unary);
                return;
            default:
                throw TypeScript.Checking.Checker.unsupported("Printer.emitExpression: unsupported synthesized node");
        }
    }

    // printer.go:2777. Homogeneous literal addition is associative for printing.
    private Kind getLiteralKindOfBinaryPlusOperand(Node node)
    {
        node = Ast.SkipPartiallyEmittedExpressions(node);
        if (Ast.IsLiteralKind(node.Kind))
            return node.Kind;
        if (node.Kind == Kind.BinaryExpression)
        {
            var binary = node.AsBinaryExpression();
            if (binary.OperatorToken.Kind == Kind.PlusToken)
            {
                var leftKind = getLiteralKindOfBinaryPlusOperand(binary.Left);
                return Ast.IsLiteralKind(leftKind) && leftKind == getLiteralKindOfBinaryPlusOperand(binary.Right)
                    ? leftKind : Kind.Unknown;
            }
        }
        return Kind.Unknown;
    }

    // printer.go:2804. Source associativity determines each operand's parentheses.
    private (OperatorPrecedence left, OperatorPrecedence right) getBinaryExpressionPrecedence(BinaryExpression node)
    {
        var precedence = Ast.GetExpressionPrecedence(node.AsNode());
        var left = precedence;
        var right = precedence;
        switch (precedence)
        {
            case OperatorPrecedence.Comma:
            case OperatorPrecedence.BitwiseOR:
            case OperatorPrecedence.BitwiseXOR:
            case OperatorPrecedence.BitwiseAND:
                break;
            case OperatorPrecedence.Assignment:
                left = OperatorPrecedence.Conditional;
                right = OperatorPrecedence.Yield;
                break;
            case OperatorPrecedence.LogicalOR:
                right = OperatorPrecedence.LogicalAND;
                break;
            case OperatorPrecedence.LogicalAND:
                right = OperatorPrecedence.BitwiseOR;
                break;
            case OperatorPrecedence.Equality:
                right = OperatorPrecedence.Relational;
                break;
            case OperatorPrecedence.Relational:
                right = OperatorPrecedence.Shift;
                break;
            case OperatorPrecedence.Shift:
                right = OperatorPrecedence.Additive;
                break;
            case OperatorPrecedence.Additive:
                if (node.OperatorToken.Kind == Kind.PlusToken && isBinaryOperation(node.Right, Kind.PlusToken))
                {
                    var leftKind = getLiteralKindOfBinaryPlusOperand(node.Left);
                    if (Ast.IsLiteralKind(leftKind) && leftKind == getLiteralKindOfBinaryPlusOperand(node.Right))
                        break;
                }
                right = OperatorPrecedence.Multiplicative;
                break;
            case OperatorPrecedence.Multiplicative:
                if (node.OperatorToken.Kind == Kind.AsteriskToken && isBinaryOperation(node.Right, Kind.AsteriskToken))
                    break;
                right = OperatorPrecedence.Exponentiation;
                break;
            case OperatorPrecedence.Exponentiation:
                left = OperatorPrecedence.Update;
                break;
            default:
                throw new InvalidOperationException("Unhandled binary expression precedence: " + precedence);
        }
        return (left, right);
    }

    // printer.go:2872. Diagnostic display is single-line; emit/source-map hooks
    // are external, while source operand precedence and token spacing are retained.
    private void emitBinaryExpression(BinaryExpression node)
    {
        var (left, right) = getBinaryExpressionPrecedence(node);
        var emittedLeft = Ast.SkipPartiallyEmittedExpressions(node.Left);
        if (Ast.NodeIsSynthesized(emittedLeft) && emittedLeft.Kind == Kind.BinaryExpression &&
            mixingBinaryOperatorsRequiresParentheses(node.OperatorToken.Kind, emittedLeft.AsBinaryExpression().OperatorToken.Kind))
            left = OperatorPrecedence.Highest;
        var emittedRight = Ast.SkipPartiallyEmittedExpressions(node.Right);
        if (Ast.NodeIsSynthesized(emittedRight) && emittedRight.Kind == Kind.BinaryExpression &&
            mixingBinaryOperatorsRequiresParentheses(node.OperatorToken.Kind, emittedRight.AsBinaryExpression().OperatorToken.Kind))
            right = OperatorPrecedence.Highest;
        emitExpression(node.Left, left);
        if (node.OperatorToken.Kind != Kind.CommaToken)
            writeSpace();
        writer.WriteOperator(TypeScript.Scanner.Scanner.TokenToString(node.OperatorToken.Kind));
        writeSpace();
        emitExpression(node.Right, right);
    }

    // printer.go:2504. Numeric member access requires the additional dot only
    // when the actual literal text has neither a dot nor an exponent/specifier.
    private bool mayNeedDotDotForPropertyAccess(Node expression)
    {
        expression = Ast.SkipPartiallyEmittedExpressions(expression);
        if (!Ast.IsNumericLiteral(expression)) return false;
        string text = getLiteralText(expression);
        return (expression.AsNumericLiteral().TokenFlags & TokenFlags.WithSpecifier) == 0 &&
            !text.Contains('.') && !text.Contains('E') && !text.Contains('e');
    }

    // printer.go:2567; utilities.go:621.
    private void emitCallee(Node callee, Node parentNode)
    {
        if ((emitContext.EmitFlags(parentNode) & EmitFlags.IndirectCall) != 0)
        {
            writePunctuation("(");
            writer.WriteLiteral("0");
            writePunctuation(",");
            writeSpace();
            emitExpression(callee);
            writePunctuation(")");
        }
        else if (parentNode.Kind == Kind.CallExpression &&
            Ast.SkipPartiallyEmittedExpressions(callee) is { Kind: Kind.NewExpression } expression && expression.ArgumentList() == null)
            emitExpression(callee, OperatorPrecedence.Parentheses);
        else
            emitExpression(callee, Ast.IsOptionalChain(parentNode) ? OperatorPrecedence.OptionalChain : OperatorPrecedence.Member);
    }

    // printer.go:2583/6284. The existing single-line list emitter supplies the
    // comma/space policy; these explicit parentheses represent LFParenthesis.
    private void emitCallExpression(CallExpression node)
    {
        emitCallee(node.Expression, node.AsNode());
        if (node.QuestionDotToken != null) writePunctuation("?.");
        emitTypeArguments(node.AsNode(), node.TypeArguments);
        emitArgumentList(node.AsNode(), node.Arguments);
    }

    // printer.go:2592/6285. New-expression parentheses are optional only for nil.
    private void emitNewExpression(NewExpression node)
    {
        writer.WriteKeyword("new");
        writeSpace();
        emitExpression(node.Expression, Ast.SkipPartiallyEmittedExpressions(node.Expression).Kind == Kind.CallExpression ?
            OperatorPrecedence.Parentheses : OperatorPrecedence.Member);
        emitTypeArguments(node.AsNode(), node.TypeArguments);
        if (node.Arguments != null) emitArgumentList(node.AsNode(), node.Arguments);
    }

    private void emitArgumentList(Node parent, NodeList arguments)
    {
        writePunctuation("(");
        emitList(argument => emitExpression(argument, OperatorPrecedence.Spread), parent, arguments, ListFormat.Parameters);
        writePunctuation(")");
    }

    private void emitLiteral(Node node) => writer.WriteStringLiteral(getLiteralText(node));
    // printer.go:1118/1156, getTextOfNode:230. Synthesized diagnostic identifiers
    // have no source-file/generated-helper substitution and carry EFNoAsciiEscaping.
    private void emitIdentifierReference(Identifier node) => emitIdentifierText(node);
    private void emitIdentifierText(Identifier node) => writer.Write(getTextOfNode(node.AsNode(), false));
    private string getTextOfNode(Node node, bool includeTrivia)
    {
        bool canUseSourceFile = currentSourceFile != null && node.Parent != null && !Ast.NodeIsSynthesized(node);
        if (node.Kind is Kind.Identifier or Kind.PrivateIdentifier)
        {
            if (!canUseSourceFile || Ast.GetSourceFileOfNode(node) != currentSourceFile)
                return node.Text();
        }
        else if (node.Kind is Kind.StringLiteral or Kind.NumericLiteral or Kind.BigIntLiteral or Kind.NoSubstitutionTemplateLiteral)
            return getLiteralText(node);
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.getTextOfNode: unselected name/literal");
        return TypeScript.Scanner.Scanner.GetSourceTextOfNodeFromSourceFileByteString(currentSourceFile, node, includeTrivia);
    }

    // printer.go:2298/5076. Selected synthesized type dispatch. The diagnostic
    // printer has RemoveComments/OmitTrailingSemicolon/NeverAsciiEscape options;
    // source-map, enter/exit emit bookkeeping and name generation remain excluded.
    private void emitTypeNode(Node node, TypePrecedence precedence = TypePrecedence.Lowest)
    {
        if (node == null)
            return;
        if (inExtends && precedence <= TypePrecedence.Conditional)
            precedence = TypePrecedence.Function;
        bool savedInExtends = inExtends;
        bool parens = Ast.GetTypeNodePrecedence(node) < precedence;
        if (parens)
        {
            inExtends = false;
            writePunctuation("(");
        }

        emitTypeNodeWorker(node);
        if (parens)
            writePunctuation(")");
        inExtends = savedInExtends;
    }

    // printer.go:5473/5483/5515/5522. Selected single-line synthesized block
    // comments follow the original helper boundaries; source comments are excluded.
    private void emitLeadingSyntheticCommentsOfNode(Node node, EmitFlags emitFlags)
    {
        if ((emitFlags & EmitFlags.NoLeadingComments) != 0)
            return;
        foreach (var comment in emitContext.GetSyntheticLeadingComments(node))
            emitLeadingSynthesizedComment(comment);
    }

    private void emitLeadingSynthesizedComment(SynthesizedComment comment)
    {
        if (comment.HasLeadingNewLine || comment.HasTrailingNewLine || comment.Kind == Kind.SingleLineCommentTrivia)
            throw TypeScript.Checking.Checker.unsupported("Printer.emitLeadingSynthesizedComment: multiline diagnostic output");
        writeSynthesizedComment(comment);
        writer.WriteSpace(" ");
    }

    private static string formatSynthesizedComment(SynthesizedComment comment) => comment.Kind == Kind.MultiLineCommentTrivia ? "/*" + comment.Text + "*/" : "//" + comment.Text;
    private void writeSynthesizedComment(SynthesizedComment comment)
    {
        var text = formatSynthesizedComment(comment);
        writeCommentRangeWorker(text, comment.Kind, new(0, text.Length));
    }

    private void writeCommentRangeWorker(string text, Kind kind, TextRange loc)
    {
        if (kind != Kind.MultiLineCommentTrivia || text.AsSpan(loc.Pos(), loc.End() - loc.Pos()).ContainsAny('\r', '\n'))
            throw TypeScript.Checking.Checker.unsupported("Printer.writeCommentRangeWorker: source/multiline comment");
        // Original single-line block-comment arm trims the current line before writing.
        writer.WriteComment(text[loc.Pos()..loc.End()].Trim());
    }

    // Authored factoring keeps return branches from bypassing closing punctuation.
    private void emitTypeNodeWorker(Node node)
    {
        if (Ast.IsKeywordTypeNode(node))
        {
            emitKeywordTypeNode(node);
            return;
        }

        switch (node.Kind)
        {
            case Kind.ParenthesizedType:
                writePunctuation("(");
                emitTypeNodeOutsideExtends(node.AsParenthesizedTypeNode().Type);
                writePunctuation(")");
                return;
            case Kind.LiteralType:
                emitLiteralType(node);
                return;
            case Kind.Identifier:
                emitIdentifierReference(node.AsIdentifier());
                return;
            case Kind.TypeLiteral:
                emitTypeLiteral(node.AsTypeLiteralNode());
                return;
            case Kind.UnionType:
                emitUnionType(node.AsUnionTypeNode());
                return;
            case Kind.IntersectionType:
                emitIntersectionType(node.AsIntersectionTypeNode());
                return;
            case Kind.ThisType:
                writer.Write("this");
                return;
            case Kind.TypeReference:
                var reference = node.AsTypeReferenceNode();
                emitEntityName(reference.TypeName);
                emitTypeArguments(node, reference.TypeArguments);
                return;
            case Kind.FunctionType:
                emitFunctionType(node.AsFunctionTypeNode());
                return;
            case Kind.ConstructorType:
                emitConstructorType(node.AsConstructorTypeNode());
                return;
            case Kind.ConstructSignature:
                emitConstructSignature(node.AsConstructSignatureDeclaration());
                return;
            case Kind.ArrayType:
                emitArrayType(node.AsArrayTypeNode());
                return;
            case Kind.TupleType:
                emitTupleType(node.AsTupleTypeNode());
                return;
            case Kind.OptionalType:
                emitOptionalType(node.AsOptionalTypeNode());
                return;
            case Kind.RestType:
                emitRestType(node.AsRestTypeNode());
                return;
            case Kind.NamedTupleMember:
                emitNamedTupleMember(node.AsNamedTupleMember());
                return;
            case Kind.TemplateLiteralType:
                emitTemplateType(node.AsTemplateLiteralTypeNode());
                return;
            case Kind.ConditionalType:
                emitConditionalType(node.AsConditionalTypeNode());
                return;
            case Kind.InferType:
                emitInferType(node.AsInferTypeNode());
                return;
            case Kind.IndexedAccessType:
                emitIndexedAccessType(node.AsIndexedAccessTypeNode());
                return;
            case Kind.MappedType:
                emitMappedType(node.AsMappedTypeNode());
                return;
            case Kind.TypePredicate:
                emitTypePredicate(node.AsTypePredicateNode());
                return;
            case Kind.CallSignature:
                emitCallSignature(node.AsCallSignatureDeclaration());
                return;
            case Kind.MethodSignature:
                emitMethodSignature(node.AsMethodSignatureDeclaration());
                return;
            case Kind.ImportType:
                emitImportTypeNode(node.AsImportTypeNode());
                return;
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            case Kind.ExpressionWithTypeArguments:
            case Kind.StringLiteral:
                emitExpression(node);
                return;
            case Kind.QualifiedName:
                emitEntityName(node);
                return;
            case Kind.TypeQuery:
                emitTypeQuery(node.AsTypeQueryNode());
                return;
            case Kind.TypeOperator:
                emitTypeOperator(node.AsTypeOperatorNode());
                return;
            default:
                throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeNode: unselected synthesized type");
        }
    }

    // printer.go:2247. Generated diagnostic import types; source import-type
    // admission and attribute emission remain separate boundaries.
    private void emitImportTypeNode(ImportTypeNode node)
    {
        if (node.IsTypeOf)
        {
            writer.WriteKeyword("typeof");
            writeSpace();
        }

        writer.WriteKeyword("import");
        writePunctuation("(");
        emitTypeNodeOutsideExtends(node.Argument);
        if (node.Attributes != null)
        {
            writePunctuation(",");
            writeSpace();
            emitImportTypeNodeAttributes(node.Attributes.AsImportAttributes());
        }
        writePunctuation(")");
        if (node.Qualifier != null)
        {
            writePunctuation(".");
            emitEntityName(node.Qualifier);
        }

        emitTypeArguments(node.AsNode(), node.TypeArguments);
    }

    // printer.go:2234/4057. Preserve actual generated resolution-mode and
    // package attributes in diagnostic import types.
    private void emitImportTypeNodeAttributes(ImportAttributes node)
    {
        writePunctuation("{");
        writeSpace();
        writeTokenText(node.Token == Kind.AssertKeyword ? Kind.AssertKeyword : Kind.WithKeyword);
        writePunctuation(":");
        writeSpace();
        writePunctuation("{");
        emitList(n => emitImportAttribute(n.AsImportAttribute()), node.AsNode(), node.Attributes,
            ListFormat.CommaDelimited | ListFormat.SpaceBetweenSiblings |
            ListFormat.SpaceBetweenBraces | ListFormat.NoSpaceIfEmpty);
        writePunctuation("}");
        writeSpace();
        writePunctuation("}");
    }

    private void emitImportAttribute(ImportAttribute node)
    {
        emitPropertyName(node.Name());
        writePunctuation(":");
        writeSpace();
        emitExpression(node.Value);
    }

    // printer.go:1970. Internal globalThis formatting uses no type arguments.
    private void emitTypeQuery(TypeQueryNode node)
    {
        writer.Write("typeof");
        writeSpace();
        emitEntityName(node.ExprName);
        emitTypeArguments(node.AsNode(), node.TypeArguments);
    }

    // printer.go:2125. Selected internal finite-key and unique-symbol operators.
    private void emitTypeOperator(TypeOperatorNode node)
    {
        if (node.Operator is not (Kind.KeyOfKeyword or Kind.UniqueKeyword or Kind.ReadonlyKeyword))
            throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeOperator: unselected operator");
        writeTokenText(node.Operator);
        writeSpace();
        emitTypeNode(node.Type, node.Operator == Kind.ReadonlyKeyword ? TypePrecedence.Postfix : TypePrecedence.TypeOperator);
    }

    // printer.go:1657. Only readonly static data-property modifiers are admitted.
    private void emitPropertySignature(PropertySignatureDeclaration node)
    {
        emitModifierList(node.Modifiers());
        emitPropertyName(node.Name());
        emitTokenNode(node.PostfixToken);
        emitTypeAnnotation(node.Type);
        writeTrailingSemicolon();
    }

    private void emitModifierList(ModifierList modifiers)
    {
        if (modifiers?.Nodes == null)
            return;
        foreach (var modifier in modifiers.Nodes)
        {
            if (modifier.Kind is not (Kind.ReadonlyKeyword or Kind.AbstractKeyword or Kind.InKeyword or Kind.OutKeyword or Kind.ConstKeyword))
                throw TypeScript.Checking.Checker.unsupported("Printer.emitModifierList: modifier");
            emitKeywordNode(modifier);
            writeSpace();
        }
    }

    // printer.go:1260. Preserve every source property-name kind in type display.
    private void emitPropertyName(Node node)
    {
        if (node == null)
            return;
        switch (node.Kind)
        {
            case Kind.Identifier:
                emitIdentifierReference(node.AsIdentifier());
                break;
            case Kind.PrivateIdentifier:
                emitPrivateIdentifier(node.AsPrivateIdentifier());
                break;
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
                emitLiteral(node);
                break;
            case Kind.ComputedPropertyName:
                emitComputedPropertyName(node.AsComputedPropertyName());
                break;
            default:
                throw new InvalidOperationException("Unexpected PropertyName: " + node.Kind);
        }
    }

    // printer.go:1218. Required internal mapped literal/unique-symbol names.
    private void emitComputedPropertyName(ComputedPropertyName node)
    {
        writePunctuation("[");
        emitExpression(node.Expression, OperatorPrecedence.DisallowComma);
        writePunctuation("]");
    }

    // printer.go:1204/1288. Private members are member names, not identifiers.
    private void emitPrivateIdentifier(PrivateIdentifier node) => writer.Write(getTextOfNode(node.AsNode(), false));
    private void emitMemberName(Node node)
    {
        if (node == null) return;
        switch (node.Kind)
        {
            case Kind.Identifier: emitIdentifierText(node.AsIdentifier()); break;
            case Kind.PrivateIdentifier: emitPrivateIdentifier(node.AsPrivateIdentifier()); break;
            default: throw new InvalidOperationException("Unexpected MemberName: " + node.Kind);
        }
    }

    private void emitTokenNode(Node node)
    {
        if (node != null)
            writeTokenText(node.Kind);
    }

    // printer.go:1524.
    private void emitTypeAnnotation(Node node)
    {
        if (node == null)
            return;
        writePunctuation(":");
        writeSpace();
        emitTypeNodeOutsideExtends(node);
    }

    // printer.go:1979. NodeBuilder sets EFSingleLine for default diagnostic output.
    private void emitTypeLiteral(TypeLiteralNode node)
    {
        writePunctuation("{");
        emitList(emitTypeElement, node.AsNode(), node.Members, ListFormat.SingleLineTypeLiteralMembers | ListFormat.NoSpaceIfEmpty);
        writePunctuation("}");
    }

    private void emitTypeElement(Node node)
    {
        if (node.Kind == Kind.PropertySignature)
            emitPropertySignature(node.AsPropertySignatureDeclaration());
        else if (node.Kind == Kind.MethodSignature)
            emitMethodSignature(node.AsMethodSignatureDeclaration());
        else if (node.Kind == Kind.CallSignature)
            emitCallSignature(node.AsCallSignatureDeclaration());
        else if (node.Kind == Kind.ConstructSignature)
            emitConstructSignature(node.AsConstructSignatureDeclaration());
        else if (node.Kind == Kind.IndexSignature)
            emitIndexSignature(node.AsIndexSignatureDeclaration());
        else if (node.Kind == Kind.GetAccessor)
            emitAccessorDeclaration(Kind.GetKeyword, node);
        else if (node.Kind == Kind.SetAccessor)
            emitAccessorDeclaration(Kind.SetKeyword, node);
        else if (node.Kind == Kind.NotEmittedTypeElement)
            emitNotEmittedTypeElement(node);
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeElement: signatures");
    }

    // printer.go:3694. The synthesized element has no emitted body. Ordinary
    // type diagnostics remove its elision comments through the source policy.
    private void emitNotEmittedTypeElement(Node node)
    {
        if (!shouldEmitComments(node))
            return;
        var flags = emitContext.EmitFlags(node);
        emitLeadingSyntheticCommentsOfNode(node, flags);
        if ((flags & EmitFlags.NoTrailingComments) == 0)
            foreach (var comment in emitContext.GetSyntheticTrailingComments(node))
            {
                if (comment.HasTrailingNewLine || comment.Kind != Kind.MultiLineCommentTrivia)
                    throw TypeScript.Checking.Checker.unsupported("Printer.emitNotEmittedTypeElement: multiline comment");
                if (!writer.IsAtStartOfLine())
                    writer.WriteSpace(" ");
                writeSynthesizedComment(comment);
            }
    }

    // printer.go:1226. Type queries can also contain member-access expressions.
    private void emitEntityName(Node node)
    {
        if (node.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else if (node.Kind == Kind.QualifiedName)
        {
            emitEntityName(node.AsQualifiedName().Left);
            writePunctuation(".");
            emitMemberName(node.AsQualifiedName().Right);
        }
        else if (node.Kind == Kind.PropertyAccessExpression)
            emitExpression(node, OperatorPrecedence.DisallowComma);
        else
            throw new InvalidOperationException("Unexpected EntityName: " + node.Kind);
    }

    // printer.go:1782. Diagnostic synthetic index parameter has no modifiers,
    // rest/default/optional state; preserve punctuation/type/semicolon order.
    private void emitIndexSignature(IndexSignatureDeclaration node)
    {
        emitModifierList(node.Modifiers());
        writePunctuation("[");
        var parameters = node.Parameters?.Nodes ?? default;
        if (parameters is not { Count: 1 })
            throw TypeScript.Checking.Checker.unsupported("Printer.emitIndexSignature: parameter shape");
        var parameter = parameters[0].AsParameterDeclaration();
        emitIdentifierReference(parameter.Name().AsIdentifier());
        emitTypeAnnotation(parameter.Type);
        writePunctuation("]");
        emitTypeAnnotation(node.Type);
        writeTrailingSemicolon();
    }

    private void emitIntersectionTypeConstituent(Node node) => emitTypeNode(node, TypePrecedence.TypeOperator);
    private void emitIntersectionType(IntersectionTypeNode node) => emitList(emitIntersectionTypeConstituent, node.AsNode(), node.Types, ListFormat.IntersectionTypeConstituents);
    private void emitUnionTypeConstituent(Node node) => emitTypeNode(node, TypePrecedence.TypeOperator);
    private void emitUnionType(UnionTypeNode node) => emitList(emitUnionTypeConstituent, node.AsNode(), node.Types, ListFormat.UnionTypeConstituents);
    [Flags]
    private enum ListFormat
    {
        SingleLine = 0,
        CommaDelimited = 1 << 4,
        BarDelimited = 1 << 2,
        AmpersandDelimited = 1 << 3,
        AllowTrailingComma = 1 << 6,
        SpaceBetweenBraces = 1 << 8,
        SpaceBetweenSiblings = 1 << 9,
        SpaceAfterList = 1 << 21,
        NoSpaceIfEmpty = 1 << 19,
        SingleLineTypeLiteralMembers = SingleLine | SpaceBetweenBraces | SpaceBetweenSiblings,
        UnionTypeConstituents = BarDelimited | SpaceBetweenSiblings | SingleLine,
        IntersectionTypeConstituents = AmpersandDelimited | SpaceBetweenSiblings | SingleLine,
        Parameters = CommaDelimited | SpaceBetweenSiblings | SingleLine,
        ObjectBindingPatternElements = SingleLine | AllowTrailingComma | SpaceBetweenBraces | CommaDelimited | SpaceBetweenSiblings | NoSpaceIfEmpty,
        ArrayBindingPatternElements = SingleLine | AllowTrailingComma | CommaDelimited | SpaceBetweenSiblings | NoSpaceIfEmpty,
    }

    // printer.go:4726/4734. Fixed single-line synthesized lists: no comments,
    // events, source newline preservation, optional/bracket/trailing comma modes.
    private void emitList(Action<Node> emit, Node parentNode, NodeList children, ListFormat format) => emitListRange(emit, parentNode, children, format, -1, -1);
    private void emitListRange(Action<Node> emit, Node parentNode, NodeList children, ListFormat format, int start, int count)
    {
        bool isNil = children == null;
        int length = isNil ? 0 : children.Nodes.Count;
        if (start < 0)
            start = 0;
        if (count < 0)
            count = length - start;
        bool isEmpty = isNil || start >= length || count <= 0;
        if (isEmpty)
        {
            if ((format & ListFormat.SpaceBetweenBraces) != 0 && (format & ListFormat.NoSpaceIfEmpty) == 0)
                writeSpace();
        }
        else
        {
            int end = Math.Min(start + count, length);
            emitListItems(emit, parentNode, children.Nodes.Slice2(start, end), format,
                (format & ListFormat.AllowTrailingComma) != 0 && hasTrailingComma(parentNode, children));
        }
    }

    // printer.go:4887/4897. Original delimiter/leading/sibling/closing whitespace
    // flow with physical-newline/comment/indent branches excluded by fixed options.
    private void writeDelimiter(ListFormat format)
    {
        if ((format & ListFormat.CommaDelimited) != 0)
            writePunctuation(",");
        else if ((format & ListFormat.BarDelimited) != 0)
        {
            writeSpace();
            writePunctuation("|");
        }
        else if ((format & ListFormat.AmpersandDelimited) != 0)
        {
            writeSpace();
            writePunctuation("&");
        }
    }

    // printer.go:4800. Selected binding lists correlate transformed nodes with
    // their original element list before retaining a source trailing comma.
    private bool hasTrailingComma(Node parentNode, NodeList children)
    {
        if (!children.HasTrailingComma())
            return false;
        var originalParent = emitContext.MostOriginal(parentNode);
        if (originalParent == parentNode)
            return true;
        if (originalParent.Kind != parentNode.Kind)
            return false;
        var originalList = children;
        if (originalParent.Kind is Kind.ObjectBindingPattern or Kind.ArrayBindingPattern &&
            children == parentNode.ElementList())
            originalList = originalParent.ElementList();
        return originalList != null && originalList.HasTrailingComma();
    }

    private void emitListItems(Action<Node> emit, Node parentNode, Slice<Node> children, ListFormat format, bool hasTrailingComma)
    {
        if ((format & ListFormat.SpaceBetweenBraces) != 0)
            writeSpace();
        Node previousSibling = null;
        foreach (var child in children)
        {
            if (previousSibling != null)
            {
                writeDelimiter(format);
                if ((format & ListFormat.SpaceBetweenSiblings) != 0)
                    writeSpace();
            }

            emit(child);
            previousSibling = child;
        }

        if (hasTrailingComma && (format & ListFormat.CommaDelimited) != 0)
            writePunctuation(",");

        if ((format & (ListFormat.SpaceAfterList | ListFormat.SpaceBetweenBraces)) != 0)
            writeSpace();
    }

    private void writeSpace() => writer.WriteSpace(" ");
    private void writePunctuation(string text) => writer.WritePunctuation(text);
    private void writeTrailingSemicolon() => writer.WriteTrailingSemicolon(";");
    // printer.go:1473. Synthesized diagnostic parameters have no initializer or
    // decorators; names retain their actual per-node escape flags.
    private void emitParameter(ParameterDeclaration node)
    {
        emitModifierList(node.Modifiers());
        emitTokenNode(node.DotDotDotToken);
        emitBindingName(node.Name());
        emitTokenNode(node.QuestionToken);
        emitTypeAnnotation(node.Type);
        if (node.Initializer != null)
            throw TypeScript.Checking.Checker.unsupported("Printer.emitParameter: source initializer emit");
    }

    // printer.go:1241/2403–2436. Diagnostic binding names retain their actual
    // recursive syntax. NodeBuilder removes binding initializers before printing.
    private void emitBindingName(Node node)
    {
        if (node == null)
            return;
        switch (node.Kind)
        {
            case Kind.Identifier:
                emitIdentifierText(node.AsIdentifier());
                break;
            case Kind.ObjectBindingPattern:
                writePunctuation("{");
                emitList(n => emitBindingElement(n.AsBindingElement()), node, node.AsBindingPattern().Elements,
                    ListFormat.ObjectBindingPatternElements);
                writePunctuation("}");
                break;
            case Kind.ArrayBindingPattern:
                writePunctuation("[");
                emitList(n => emitBindingElement(n.AsBindingElement()), node, node.AsBindingPattern().Elements,
                    ListFormat.ArrayBindingPatternElements);
                writePunctuation("]");
                break;
            default:
                throw new InvalidOperationException($"Unexpected BindingName: {node.Kind}");
        }
    }

    private void emitBindingElement(BindingElement node)
    {
        emitTokenNode(node.DotDotDotToken);
        if (node.PropertyName != null)
        {
            emitPropertyName(node.PropertyName);
            writePunctuation(":");
            writeSpace();
        }
        if (node.Name() != null)
        {
            emitBindingName(node.Name());
            if (node.Initializer != null)
                throw TypeScript.Checking.Checker.unsupported("Printer.emitBindingElement: source initializer emit");
        }
    }

    // printer.go:1545. The original Parameters list mode has comma delimiters,
    // sibling spaces and parentheses; no source trailing comma is selected.
    private void emitParameters(Node parentNode, NodeList parameters)
    {
        writePunctuation("(");
        emitList(n => emitParameter(n.AsParameterDeclaration()), parentNode, parameters, ListFormat.Parameters);
        writePunctuation(")");
    }

    // printer.go:1586. Signature generic parameters, parameters and return annotation.
    private void emitSignature(Node node)
    {
        var signature = node.FunctionLikeData();
        emitTypeParameters(node, signature.TypeParameters);
        emitParameters(node, signature.Parameters);
        emitTypeAnnotation(signature.Type);
    }

    // printer.go:1678/1756. Fixed single-line diagnostic mode has no indentation
    // or generated names, source comments, hooks, or physical-newline scope work.
    private void emitMethodSignature(MethodSignatureDeclaration node)
    {
        emitModifierList(node.Modifiers());
        emitPropertyName(node.Name());
        emitTokenNode(node.PostfixToken);
        emitSignature(node.AsNode());
        writeTrailingSemicolon();
    }

    // printer.go:1730. Diagnostic accessor nodes have no function body;
    // source body emission remains outside this no-emit printer consumer.
    private void emitAccessorDeclaration(Kind token, Node node)
    {
        emitModifierList(node.Modifiers());
        writeTokenText(token);
        writeSpace();
        emitPropertyName(node.Name());
        emitSignature(node);
        if (node.Body() != null)
            throw TypeScript.Checking.Checker.unsupported("Printer.emitAccessorDeclaration: source function body emit");
        writeTrailingSemicolon();
    }

    private void emitCallSignature(CallSignatureDeclaration node)
    {
        emitSignature(node.AsNode());
        writeTrailingSemicolon();
    }

    // printer.go:1873/1884.
    private void emitTypePredicateParameterName(Node node)
    {
        if (node.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else if (node.Kind == Kind.ThisType)
            writer.Write("this");
        else
            throw new InvalidOperationException("Unexpected type predicate parameter name");
    }

    private void emitTypePredicate(TypePredicateNode node)
    {
        if (node.AssertsModifier != null)
        {
            emitTokenNode(node.AssertsModifier);
            writeSpace();
        }

        emitTypePredicateParameterName(node.ParameterName);
        if (node.Type != null)
        {
            writeSpace();
            writer.WriteKeyword("is");
            writeSpace();
            emitTypeNodeOutsideExtends(node.Type);
        }
    }

    // printer.go:1919/1937. Preserve constrained-infer ambiguity rules in returns.
    private void emitReturnType(Node node)
    {
        if (node == null)
            return;
        writePunctuation("=>");
        writeSpace();
        emitTypeNode(node, inExtends && node.Kind == Kind.InferType && node.AsInferTypeNode().TypeParameter.AsTypeParameterDeclaration().Constraint != null ? TypePrecedence.Highest : TypePrecedence.Lowest);
    }

    private void emitFunctionType(FunctionTypeNode node)
    {
        emitTypeParameters(node.AsNode(), node.TypeParameters);
        emitParameters(node.AsNode(), node.Parameters);
        writeSpace();
        emitReturnType(node.Type);
    }

    // printer.go:2271/2279. Conditional extends scope is independent of emit bookkeeping.
    private void emitTypeNodeInExtends(Node node)
    {
        var saved = inExtends;
        inExtends = true;
        emitTypeNode(node);
        inExtends = saved;
    }

    private void emitTypeNodeOutsideExtends(Node node)
    {
        var saved = inExtends;
        inExtends = false;
        emitTypeNode(node);
        inExtends = saved;
    }

    // printer.go:1443/1463/1513/1904. Diagnostic signature lists can contain
    // declarations or instantiated type arguments, exactly as the original printer.
    private void emitTypeParameter(TypeParameterDeclaration node)
    {
        emitModifierList(node.Modifiers());
        emitIdentifierReference(node.Name().AsIdentifier());
        if (node.Constraint != null)
        {
            writeSpace();
            writer.WriteKeyword("extends");
            writeSpace();
            emitTypeNodeOutsideExtends(node.Constraint);
        }

        if (node.DefaultType != null)
        {
            writeSpace();
            writer.WriteOperator("=");
            writeSpace();
            emitTypeNodeOutsideExtends(node.DefaultType);
        }
    }

    private void emitTypeParameterDeclarationNode(Node node)
    {
        if (Ast.IsTypeParameterDeclaration(node))
            emitTypeParameter(node.AsTypeParameterDeclaration());
        else
            emitTypeNodeOutsideExtends(node);
    }

    private void emitTypeParameters(Node parent, NodeList nodes)
    {
        // printer.go:1517/4755. LFTypeParameters includes LFOptionalIfEmpty.
        if (nodes == null || (nodes.Nodes.Count) == 0)
            return;
        writePunctuation("<");
        emitList(emitTypeParameterDeclarationNode, parent, nodes, ListFormat.Parameters);
        // The diagnostic builder does not synthesize ArrowFunction declarations.
        if (Ast.IsArrowFunction(parent) && nodes.HasTrailingComma())
            writePunctuation(",");
        writePunctuation(">");
    }

    private void emitTypeArguments(Node parent, NodeList nodes)
    {
        // printer.go:1908/4755. LFTypeArguments includes LFOptionalIfEmpty.
        if (nodes == null || (nodes.Nodes.Count) == 0)
            return;
        writePunctuation("<");
        emitList(emitTypeParameterDeclarationNode, parent, nodes, ListFormat.Parameters);
        writePunctuation(">");
    }

    // printer.go:1944/1766. Internal library construct storage is printable;
    // user constructor/class/new syntax remains outside the semantic profile.
    private void emitConstructorType(ConstructorTypeNode node)
    {
        emitModifierList(node.Modifiers());
        writer.WriteKeyword("new");
        writeSpace();
        emitTypeParameters(node.AsNode(), node.TypeParameters);
        emitParameters(node.AsNode(), node.Parameters);
        writeSpace();
        emitReturnType(node.Type);
    }

    private void emitConstructSignature(ConstructSignatureDeclaration node)
    {
        writer.WriteKeyword("new");
        writeSpace();
        emitSignature(node.AsNode());
        writeTrailingSemicolon();
    }

    // printer.go:1991/2004. Parsed typeof postfix operands retain source precedence.
    private void emitArrayType(ArrayTypeNode node)
    {
        emitPostfixTypeOperand(node.ElementType, node.AsNode());
        writePunctuation("[");
        writePunctuation("]");
    }

    private void emitPostfixTypeOperand(Node operand, Node parent)
    {
        emitTypeNode(operand, Ast.IsParseTreeNode(parent) && operand.Kind == Kind.TypeQuery ? TypePrecedence.TypeOperator : TypePrecedence.Postfix);
    }

    // printer.go:2014/2018/2027/2034/2042. Fixed single-line diagnostic list mode.
    private void emitTupleType(TupleTypeNode node)
    {
        writePunctuation("[");
        emitList(emitTypeNodeOutsideExtends, node.AsNode(), node.Elements, ListFormat.Parameters);
        writePunctuation("]");
    }

    private void emitRestType(RestTypeNode node)
    {
        writePunctuation("...");
        emitTypeNodeOutsideExtends(node.Type);
    }

    private void emitOptionalType(OptionalTypeNode node)
    {
        emitPostfixTypeOperand(node.Type, node.AsNode());
        writePunctuation("?");
    }

    private void emitNamedTupleMember(NamedTupleMember node)
    {
        emitTokenNode(node.DotDotDotToken);
        emitIdentifierReference(node.Name().AsIdentifier());
        emitTokenNode(node.QuestionToken);
        writePunctuation(":");
        writeSpace();
        emitTypeNodeOutsideExtends(node.Type);
    }

    // printer.go:2073/2091/2103/2133/2142/2152. Retained conditional/mapped syntax.
    private void emitConditionalType(ConditionalTypeNode node)
    {
        emitTypeNode(node.CheckType, TypePrecedence.Union);
        writeSpace();
        writer.WriteKeyword("extends");
        writeSpace();
        emitTypeNodeInExtends(node.ExtendsType);
        writeSpace();
        writePunctuation("?");
        writeSpace();
        emitTypeNodeOutsideExtends(node.TrueType);
        writeSpace();
        writePunctuation(":");
        writeSpace();
        emitTypeNodeOutsideExtends(node.FalseType);
    }

    private void emitInferTypeParameter(TypeParameterDeclaration node)
    {
        emitIdentifierReference(node.Name().AsIdentifier());
        if (node.Constraint != null)
        {
            writeSpace();
            writer.WriteKeyword("extends");
            writeSpace();
            emitTypeNodeInExtends(node.Constraint);
        }
    }

    private void emitInferType(InferTypeNode node)
    {
        writer.WriteKeyword("infer");
        writeSpace();
        emitInferTypeParameter(node.TypeParameter.AsTypeParameterDeclaration());
    }

    private void emitIndexedAccessType(IndexedAccessTypeNode node)
    {
        emitPostfixTypeOperand(node.ObjectType, node.AsNode());
        writePunctuation("[");
        emitTypeNodeOutsideExtends(node.IndexType);
        writePunctuation("]");
    }

    private void emitMappedTypeParameter(TypeParameterDeclaration node)
    {
        emitIdentifierReference(node.Name().AsIdentifier());
        writeSpace();
        writer.WriteKeyword("in");
        writeSpace();
        emitTypeNodeOutsideExtends(node.Constraint);
    }

    private void emitMappedType(MappedTypeNode node)
    {
        writePunctuation("{");
        writeSpace();
        if (node.ReadonlyToken != null)
        {
            emitTokenNode(node.ReadonlyToken);
            if (node.ReadonlyToken.Kind != Kind.ReadonlyKeyword)
                writer.WriteKeyword("readonly");
            writeSpace();
        }

        writePunctuation("[");
        emitMappedTypeParameter(node.TypeParameter.AsTypeParameterDeclaration());
        if (node.NameType != null)
        {
            writeSpace();
            writer.WriteKeyword("as");
            writeSpace();
            emitTypeNodeOutsideExtends(node.NameType);
        }

        writePunctuation("]");
        if (node.QuestionToken != null)
        {
            emitTokenNode(node.QuestionToken);
            if (node.QuestionToken.Kind != Kind.QuestionToken)
                writePunctuation("?");
        }

        if (node.Type != null)
        {
            writePunctuation(":");
            writeSpace();
            emitTypeNodeOutsideExtends(node.Type);
        }

        writeTrailingSemicolon();
        if (node.Members?.Nodes is { Count: > 0 })
        {
            writeSpace();
            emitList(emitTypeElement, node.AsNode(), node.Members, ListFormat.SpaceBetweenSiblings);
        }

        writeSpace();
        writePunctuation("}");
    }

    // printer.go:2217/2229. Internal template type spans preserve outside-extends scope.
    private void emitTemplateTypeSpan(TemplateLiteralTypeSpan node)
    {
        emitTypeNodeOutsideExtends(node.Type);
        emitLiteral(node.Literal);
    }

    private void emitTemplateType(TemplateLiteralTypeNode node)
    {
        emitLiteral(node.Head);
        emitList(n => emitTemplateTypeSpan(n.AsTemplateLiteralTypeSpan()), node.AsNode(), node.TemplateSpans, ListFormat.SingleLine);
    }
}
