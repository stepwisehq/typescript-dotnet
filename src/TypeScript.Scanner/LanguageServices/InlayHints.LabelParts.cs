// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete inlay_hints.go getInlayHintLabelParts, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private sealed partial class InlayHintState
    {
        private Slice<InlayHintLabelPart?> GetInlayHintLabelParts(Node node, Dictionary<Node, Symbol> identifiers)
        {
            Slice<InlayHintLabelPart?> parts = default;
            void Add(ByteStringValue text) => parts = parts.Append(new InlayHintLabelPart { Value = text });
            void List(Slice<Node> nodes, ByteStringValue separator)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (i > 0)
                        Add(separator);
                    Display(nodes[i]);
                }
            }

            void Parameters(Node item)
            {
                if (item.TypeParameters().Length > 0)
                {
                    Add(ByteStringValue.FromString("<"));
                    List(item.TypeParameters(), ByteStringValue.FromString(", "));
                    Add(ByteStringValue.FromString(">"));
                }

                Add(ByteStringValue.FromString("("));
                List(item.Parameters(), ByteStringValue.FromString(", "));
                Add(ByteStringValue.FromString(")"));
            }

            void Display(Node? item)
            {
                if (item is null)
                    return;
                var token = ScannerApi.TokenToString(item.Kind);
                if (token.Length != 0)
                {
                    Add(ByteStringValue.FromString(token));
                    return;
                }

                if (Ast.IsLiteralExpression(item))
                {
                    Add(GetLiteralText(item));
                    return;
                }

                switch (item.Kind)
                {
                    case Kind.Identifier:
                        var identifierText = item.TextValue();
                        Node? name = null;
                        if (identifiers.TryGetValue(item, out var symbol) && symbol.Declarations.Length != 0)
                            name = Ast.GetNameOfDeclaration(symbol.Declarations[0]);
                        if (name is not null)
                            parts = parts.Append(GetNodeDisplayPart(identifierText, name));
                        else
                            Add(identifierText);
                        break;
                    case Kind.QualifiedName:
                        Display(item.AsQualifiedName().Left);
                        Add(ByteStringValue.FromString("."));
                        Display(item.AsQualifiedName().Right);
                        break;
                    case Kind.TypePredicate:
                        if (item.AsTypePredicateNode().AssertsModifier is not null)
                            Add(ByteStringValue.FromString("asserts "));
                        Display(item.AsTypePredicateNode().ParameterName);
                        if (item.Type()is not null)
                        {
                            Add(ByteStringValue.FromString(" is "));
                            Display(item.Type());
                        }

                        break;
                    case Kind.TypeReference:
                        Display(item.AsTypeReferenceNode().TypeName);
                        if (item.TypeArguments().Length > 0)
                        {
                            Add(ByteStringValue.FromString("<"));
                            List(item.TypeArguments(), ByteStringValue.FromString(","));
                            Add(ByteStringValue.FromString(">"));
                        }

                        break;
                    case Kind.TypeParameter:
                        if (item.ModifierNodes().Length > 0)
                            List(item.ModifierNodes(), ByteStringValue.FromString(""));
                        Display(item.Name());
                        if (item.AsTypeParameterDeclaration().Constraint is { } constraint)
                        {
                            Add(ByteStringValue.FromString(" extends "));
                            Display(constraint);
                        }

                        if (item.AsTypeParameterDeclaration().DefaultType is { } defaultType)
                        {
                            Add(ByteStringValue.FromString(" = "));
                            Display(defaultType);
                        }

                        break;
                    case Kind.Parameter:
                        if (item.ModifierNodes().Length > 0)
                            List(item.ModifierNodes(), ByteStringValue.FromString(" "));
                        if (item.AsParameterDeclaration().DotDotDotToken is not null)
                            Add(ByteStringValue.FromString("..."));
                        Display(item.Name());
                        if (item.QuestionToken()is not null)
                            Add(ByteStringValue.FromString("?"));
                        if (item.Type()is not null)
                        {
                            Add(ByteStringValue.FromString(": "));
                            Display(item.Type());
                        }

                        break;
                    case Kind.ConstructorType:
                        Add(ByteStringValue.FromString("new "));
                        Parameters(item);
                        Add(ByteStringValue.FromString(" => "));
                        Display(item.Type());
                        break;
                    case Kind.TypeQuery:
                        Add(ByteStringValue.FromString("typeof "));
                        Display(item.AsTypeQueryNode().ExprName);
                        if (item.TypeArguments().Length > 0)
                        {
                            Add(ByteStringValue.FromString("<"));
                            List(item.TypeArguments(), ByteStringValue.FromString(", "));
                            Add(ByteStringValue.FromString(">"));
                        }

                        break;
                    case Kind.TypeLiteral:
                        Add(ByteStringValue.FromString("{"));
                        if (item.Members().Length > 0)
                        {
                            Add(ByteStringValue.FromString(" "));
                            List(item.Members(), ByteStringValue.FromString("; "));
                            Add(ByteStringValue.FromString(" "));
                        }

                        Add(ByteStringValue.FromString("}"));
                        break;
                    case Kind.ArrayType:
                        Display(item.AsArrayTypeNode().ElementType);
                        Add(ByteStringValue.FromString("[]"));
                        break;
                    case Kind.TupleType:
                        Add(ByteStringValue.FromString("["));
                        List(item.Elements(), ByteStringValue.FromString(", "));
                        Add(ByteStringValue.FromString("]"));
                        break;
                    case Kind.NamedTupleMember:
                        if (item.AsNamedTupleMember().DotDotDotToken is not null)
                            Add(ByteStringValue.FromString("..."));
                        Display(item.Name());
                        if (item.QuestionToken()is not null)
                            Add(ByteStringValue.FromString("?"));
                        Add(ByteStringValue.FromString(": "));
                        Display(item.Type());
                        break;
                    case Kind.OptionalType:
                        Display(item.Type());
                        Add(ByteStringValue.FromString("?"));
                        break;
                    case Kind.RestType:
                        Add(ByteStringValue.FromString("..."));
                        Display(item.Type());
                        break;
                    case Kind.UnionType:
                        if (item.AsUnionTypeNode().Types is { } union)
                            List(union.Nodes, ByteStringValue.FromString(" | "));
                        break;
                    case Kind.IntersectionType:
                        if (item.AsIntersectionTypeNode().Types is { } intersection)
                            List(intersection.Nodes, ByteStringValue.FromString(" & "));
                        break;
                    case Kind.ConditionalType:
                        var conditional = item.AsConditionalTypeNode();
                        Display(conditional.CheckType);
                        Add(ByteStringValue.FromString(" extends "));
                        Display(conditional.ExtendsType);
                        Add(ByteStringValue.FromString(" ? "));
                        Display(conditional.TrueType);
                        Add(ByteStringValue.FromString(" : "));
                        Display(conditional.FalseType);
                        break;
                    case Kind.InferType:
                        Add(ByteStringValue.FromString("infer "));
                        Display(item.AsInferTypeNode().TypeParameter);
                        break;
                    case Kind.ParenthesizedType:
                        Add(ByteStringValue.FromString("("));
                        Display(item.Type());
                        Add(ByteStringValue.FromString(")"));
                        break;
                    case Kind.TypeOperator:
                        Add(ByteStringValue.FromString(ScannerApi.TokenToString(item.AsTypeOperatorNode().Operator)));
                        Display(item.Type());
                        break;
                    case Kind.IndexedAccessType:
                        Display(item.AsIndexedAccessTypeNode().ObjectType);
                        Add(ByteStringValue.FromString("["));
                        Display(item.AsIndexedAccessTypeNode().IndexType);
                        Add(ByteStringValue.FromString("]"));
                        break;
                    case Kind.MappedType:
                        var mapped = item.AsMappedTypeNode();
                        Add(ByteStringValue.FromString("{ "));
                        if (mapped.ReadonlyToken is { } readOnly)
                        {
                            if (readOnly.Kind == Kind.PlusToken)
                                Add(ByteStringValue.FromString("+"));
                            else if (readOnly.Kind == Kind.MinusToken)
                                Add(ByteStringValue.FromString("-"));
                            Add(ByteStringValue.FromString("readonly "));
                        }

                        Add(ByteStringValue.FromString("["));
                        Display(mapped.TypeParameter);
                        if (mapped.NameType is { } nameType)
                        {
                            Add(ByteStringValue.FromString(" as "));
                            Display(nameType);
                        }

                        Add(ByteStringValue.FromString("]"));
                        if (item.QuestionToken()is { } question)
                        {
                            if (question.Kind == Kind.PlusToken)
                                Add(ByteStringValue.FromString("+"));
                            else if (question.Kind == Kind.MinusToken)
                                Add(ByteStringValue.FromString("-"));
                            Add(ByteStringValue.FromString("?"));
                        }

                        Add(ByteStringValue.FromString(": "));
                        if (item.Type()is not null)
                            Display(item.Type());
                        Add(ByteStringValue.FromString("; }"));
                        break;
                    case Kind.LiteralType:
                        Display(item.AsLiteralTypeNode().Literal);
                        break;
                    case Kind.FunctionType:
                        Parameters(item);
                        Add(ByteStringValue.FromString(" => "));
                        Display(item.Type());
                        break;
                    case Kind.ImportType:
                        var import = item.AsImportTypeNode();
                        if (import.IsTypeOf)
                            Add(ByteStringValue.FromString("typeof "));
                        Add(ByteStringValue.FromString("import("));
                        Display(import.Argument);
                        Add(ByteStringValue.FromString(")"));
                        if (import.Qualifier is not null)
                        {
                            Add(ByteStringValue.FromString("."));
                            Display(import.Qualifier);
                        }

                        if (item.TypeArguments().Length > 0)
                        {
                            Add(ByteStringValue.FromString("<"));
                            List(item.TypeArguments(), ByteStringValue.FromString(", "));
                            Add(ByteStringValue.FromString(">"));
                        }

                        break;
                    case Kind.PropertySignature:
                        if (item.ModifierNodes().Length > 0)
                        {
                            List(item.ModifierNodes(), ByteStringValue.FromString(" "));
                            Add(ByteStringValue.FromString(" "));
                        }

                        Display(item.Name());
                        if (item.PostfixToken()is { } postfixProperty)
                            Add(ByteStringValue.FromString(ScannerApi.TokenToString(postfixProperty.Kind)));
                        if (item.Type()is not null)
                        {
                            Add(ByteStringValue.FromString(": "));
                            Display(item.Type());
                        }

                        break;
                    case Kind.IndexSignature:
                        Add(ByteStringValue.FromString("["));
                        List(item.Parameters(), ByteStringValue.FromString(", "));
                        Add(ByteStringValue.FromString("]"));
                        if (item.Type()is not null)
                        {
                            Add(ByteStringValue.FromString(": "));
                            Display(item.Type());
                        }

                        break;
                    case Kind.MethodSignature:
                        if (item.ModifierNodes().Length > 0)
                        {
                            List(item.ModifierNodes(), ByteStringValue.FromString(" "));
                            Add(ByteStringValue.FromString(" "));
                        }

                        Display(item.Name());
                        if (item.PostfixToken()is { } postfixMethod)
                            Add(ByteStringValue.FromString(ScannerApi.TokenToString(postfixMethod.Kind)));
                        Parameters(item);
                        if (item.Type()is not null)
                        {
                            Add(ByteStringValue.FromString(": "));
                            Display(item.Type());
                        }

                        break;
                    case Kind.CallSignature:
                        Parameters(item);
                        if (item.Type()is not null)
                        {
                            Add(ByteStringValue.FromString(": "));
                            Display(item.Type());
                        }

                        break;
                    case Kind.ConstructSignature:
                        Add(ByteStringValue.FromString("new "));
                        Parameters(item);
                        if (item.Type()is not null)
                        {
                            Add(ByteStringValue.FromString(": "));
                            Display(item.Type());
                        }

                        break;
                    case Kind.ArrayBindingPattern:
                        Add(ByteStringValue.FromString("["));
                        List(item.Elements(), ByteStringValue.FromString(", "));
                        Add(ByteStringValue.FromString("]"));
                        break;
                    case Kind.ObjectBindingPattern:
                        Add(ByteStringValue.FromString("{"));
                        if (item.Elements().Length > 0)
                        {
                            Add(ByteStringValue.FromString(" "));
                            List(item.Elements(), ByteStringValue.FromString(", "));
                            Add(ByteStringValue.FromString(" "));
                        }

                        Add(ByteStringValue.FromString("}"));
                        break;
                    case Kind.BindingElement:
                        Display(item.Name());
                        break;
                    case Kind.PrefixUnaryExpression:
                        Add(ByteStringValue.FromString(ScannerApi.TokenToString(item.AsPrefixUnaryExpression().Operator)));
                        Display(item.AsPrefixUnaryExpression().Operand);
                        break;
                    case Kind.TemplateLiteralType:
                        Display(item.AsTemplateLiteralTypeNode().Head);
                        foreach (var templateSpan in item.AsTemplateLiteralTypeNode().TemplateSpans.Nodes)
                            Display(templateSpan);
                        break;
                    case Kind.TemplateHead:
                    case Kind.TemplateMiddle:
                    case Kind.TemplateTail:
                        Add(GetLiteralText(item));
                        break;
                    case Kind.TemplateLiteralTypeSpan:
                        Display(item.Type());
                        Display(item.AsTemplateLiteralTypeSpan().Literal);
                        break;
                    case Kind.ThisType:
                        Add(ByteStringValue.FromString("this"));
                        break;
                    case Kind.ComputedPropertyName:
                        Add(ByteStringValue.FromString("["));
                        Display(item.Expression());
                        Add(ByteStringValue.FromString("]"));
                        break;
                    case Kind.PropertyAccessExpression:
                        Display(item.Expression());
                        Add(ByteStringValue.FromString("."));
                        Display(item.Name());
                        break;
                    case Kind.ElementAccessExpression:
                        Display(item.Expression());
                        Add(ByteStringValue.FromString("["));
                        Display(item.AsElementAccessExpression().ArgumentExpression);
                        Add(ByteStringValue.FromString("]"));
                        break;
                    default:
                        throw new InvalidOperationException("Debug failure. Bad syntax kind: " + item.Kind.String());
                }
            }

            Display(node);
            return parts;
        }
    }
}
