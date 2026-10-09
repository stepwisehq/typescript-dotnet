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
    internal Printer(EmitContext context, bool removeComments = true, bool omitTrailingSemicolon = false)
    {
        emitContext = context;
        this.removeComments = removeComments;
        this.omitTrailingSemicolon = omitTrailingSemicolon;
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
        if (node?.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else
            emitTypeNode(node);
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
    private void emitExpression(Node node)
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
            case Kind.PrefixUnaryExpression:
                var unary = node.AsPrefixUnaryExpression();
                if (unary.Operator is not (Kind.MinusToken or Kind.PlusToken))
                    throw TypeScript.Checking.Checker.unsupported("Printer.emitExpression: unary operator");
                writer.Write(unary.Operator == Kind.MinusToken ? "-" : "+");
                emitExpression(unary.Operand);
                return;
            default:
                throw TypeScript.Checking.Checker.unsupported("Printer.emitExpression: unsupported synthesized node");
        }
    }

    private void emitLiteral(Node node) => writer.WriteStringLiteral(getLiteralText(node));
    // printer.go:1118/1156, getTextOfNode:230. Synthesized diagnostic identifiers
    // have no source-file/generated-helper substitution and carry EFNoAsciiEscaping.
    private void emitIdentifierReference(Identifier node) => emitIdentifierText(node);
    private void emitIdentifierText(Identifier node) => writer.Write(getTextOfNode(node.AsNode(), false));
    private string getTextOfNode(Node node, bool includeTrivia)
    {
        bool canUseSourceFile = currentSourceFile != null && node.Parent != null && !Ast.NodeIsSynthesized(node);
        if (node.Kind == Kind.Identifier)
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
        foreach (var comment in emitContext.GetSyntheticLeadingComments(node) ?? [])
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

    private void emitPropertyName(Node node)
    {
        if (node.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else if (node.Kind is Kind.StringLiteral or Kind.NumericLiteral)
            emitLiteral(node);
        else if (node.Kind == Kind.ComputedPropertyName)
            emitComputedPropertyName(node.AsComputedPropertyName());
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.emitPropertyName: private name");
    }

    // printer.go:1218. Required internal mapped literal/unique-symbol names.
    private void emitComputedPropertyName(ComputedPropertyName node)
    {
        writePunctuation("[");
        emitExpression(node.Expression);
        writePunctuation("]");
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
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeElement: signatures");
    }

    // printer.go:2053/2057. Selected constituents have scalar/primary precedence;
    // named references and nested composite types retain actual type precedence.
    private void emitEntityName(Node node)
    {
        if (node.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else if (node.Kind == Kind.QualifiedName)
        {
            emitEntityName(node.AsQualifiedName().Left);
            writePunctuation(".");
            emitIdentifierReference(node.AsQualifiedName().Right.AsIdentifier());
        }
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.emitEntityName: entity name");
    }

    // printer.go:1782. Diagnostic synthetic index parameter has no modifiers,
    // rest/default/optional state; preserve punctuation/type/semicolon order.
    private void emitIndexSignature(IndexSignatureDeclaration node)
    {
        emitModifierList(node.Modifiers());
        writePunctuation("[");
        var parameters = node.Parameters?.Nodes;
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
        SpaceBetweenBraces = 1 << 8,
        SpaceBetweenSiblings = 1 << 9,
        SpaceAfterList = 1 << 21,
        NoSpaceIfEmpty = 1 << 19,
        SingleLineTypeLiteralMembers = SingleLine | SpaceBetweenBraces | SpaceBetweenSiblings,
        UnionTypeConstituents = BarDelimited | SpaceBetweenSiblings | SingleLine,
        IntersectionTypeConstituents = AmpersandDelimited | SpaceBetweenSiblings | SingleLine,
        Parameters = CommaDelimited | SpaceBetweenSiblings | SingleLine,
    }

    // printer.go:4726/4734. Fixed single-line synthesized lists: no comments,
    // events, source newline preservation, optional/bracket/trailing comma modes.
    private void emitList(Action<Node> emit, Node parentNode, NodeList children, ListFormat format) => emitListRange(emit, parentNode, children, format, -1, -1);
    private void emitListRange(Action<Node> emit, Node parentNode, NodeList children, ListFormat format, int start, int count)
    {
        bool isNil = children == null;
        int length = isNil ? 0 : children.Nodes?.Count ?? 0;
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
            emitListItems(emit, parentNode, children.Nodes.GetRange(start, end - start), format);
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

    private void emitListItems(Action<Node> emit, Node parentNode, List<Node> children, ListFormat format)
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
        if (node.Name().Kind != Kind.Identifier)
            throw TypeScript.Checking.Checker.unsupported("Printer.emitParameter: binding pattern");
        emitIdentifierReference(node.Name().AsIdentifier());
        emitTokenNode(node.QuestionToken);
        emitTypeAnnotation(node.Type);
        if (node.Initializer != null)
            throw TypeScript.Checking.Checker.unsupported("Printer.emitParameter: source initializer emit");
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
        if (nodes == null)
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
        if (nodes == null)
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
