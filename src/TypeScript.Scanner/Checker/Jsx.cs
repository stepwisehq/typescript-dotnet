// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// C# counterpart of tsc/internal/checker/jsx.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using System.Diagnostics;
using TypeScript.Scanner;

namespace TypeScript.Checking;
[Flags]
internal enum JsxFlags : uint
{
    None = 0,
    IntrinsicNamedElement = 1 << 0,
    IntrinsicIndexedElement = 1 << 1,
    IntrinsicElement = IntrinsicNamedElement | IntrinsicIndexedElement,
}

internal enum JsxReferenceKind
{
    Component,
    Function,
    Mixed
}

public sealed partial class Checker
{
    internal sealed class JsxElementLinks
    {
        internal JsxFlags jsxFlags;
        internal Type resolvedJsxElementAttributesType;
        internal Symbol jsxNamespace;
        internal Symbol jsxImplicitImportContainer;
        internal Node firstJSXTagInFile;
    }

    private struct JsxElaborationElement
    {
        internal Node errorNode;
        internal Node innerExpression;
        internal Type nameType;
        internal Func<Node, Diagnostic> createDiagnostic;
    }

    internal static class JsxNames
    {
        internal const string JSX = "JSX", IntrinsicElements = "IntrinsicElements", ElementClass = "ElementClass", ElementAttributesPropertyNameContainer = "ElementAttributesProperty", ElementChildrenAttributeNameContainer = "ElementChildrenAttribute", Element = "Element", ElementType = "ElementType", IntrinsicAttributes = "IntrinsicAttributes", IntrinsicClassAttributes = "IntrinsicClassAttributes", LibraryManagedAttributes = "LibraryManagedAttributes";
    }

    private static class ReactNames
    {
        internal const string Fragment = "Fragment";
    }

    // Upstream tsc/internal/checker/jsx.go:72:1.
    private Type checkJsxElement(Node node, CheckMode checkMode)
    {
        this.checkNodeDeferred(node);
        return this.getJsxElementTypeAt(node);
    }

    // Upstream tsc/internal/checker/jsx.go:77:1.
    private void checkJsxElementDeferred(Node node)
    {
        var jsxElement = node.AsJsxElement();
        this.checkJsxOpeningLikeElementOrOpeningFragment(jsxElement.OpeningElement);
        if (isJsxIntrinsicTagName(jsxElement.ClosingElement.TagName()))
        {
            this.getIntrinsicTagSymbol(jsxElement.ClosingElement);
        }
        else
        {
            this.checkExpression(jsxElement.ClosingElement.TagName());
        }

        this.checkJsxChildren(node, CheckMode.Normal);
    }

    // Upstream tsc/internal/checker/jsx.go:89:1.
    private Type checkJsxExpression(Node node, CheckMode checkMode)
    {
        this.checkGrammarJsxExpression(node.AsJsxExpression());
        if ((node.Expression() == null))
        {
            return this.errorType;
        }

        var t = this.checkExpressionEx(node.Expression(), checkMode);
        if ((((node.AsJsxExpression().DotDotDotToken != null) && (t != this.anyType)) && !this.isArrayType(t)))
        {
            this.error(node, Diagnostics.JSX_spread_child_must_be_an_array_type);
        }

        return t;
    }

    // Upstream tsc/internal/checker/jsx.go:101:1.
    private Type checkJsxSelfClosingElement(Node node, CheckMode checkMode)
    {
        this.checkNodeDeferred(node);
        return this.getJsxElementTypeAt(node);
    }

    // Upstream tsc/internal/checker/jsx.go:106:1.
    private void checkJsxSelfClosingElementDeferred(Node node)
    {
        this.checkJsxOpeningLikeElementOrOpeningFragment(node);
    }

    // Upstream tsc/internal/checker/jsx.go:110:1.
    private Type checkJsxFragment(Node node)
    {
        this.checkJsxOpeningLikeElementOrOpeningFragment(node.AsJsxFragment().OpeningFragment);
        var nodeSourceFile = Ast.GetSourceFileOfNode(node);
        if ((((this.compilerOptions.GetJSXTransformEnabled() && (((this.compilerOptions.JsxFactory != "") || (Ast.GetPragmaFromSourceFile(nodeSourceFile, "jsx") != null)))) && (this.compilerOptions.JsxFragmentFactory == "")) && (Ast.GetPragmaFromSourceFile(nodeSourceFile, "jsxfrag") == null)))
        {
            var message = Core.IfElse((this.compilerOptions.JsxFactory != ""), Diagnostics.The_jsxFragmentFactory_compiler_option_must_be_provided_to_use_JSX_fragments_with_the_jsxFactory_compiler_option, Diagnostics.An_jsxFrag_pragma_is_required_when_using_an_jsx_pragma_with_JSX_fragments);
            this.error(node, message);
        }

        this.checkJsxChildren(node, CheckMode.Normal);
        var t = this.getJsxElementTypeAt(node);
        return Core.IfElse(this.isErrorType(t), this.anyType, t);
    }

    // Upstream tsc/internal/checker/jsx.go:126:1.
    private Type checkJsxAttributes(Node node, CheckMode checkMode)
    {
        this.checkNodeDeferred(node);
        return this.createJsxAttributesTypeFromAttributesProperty(node.Parent, checkMode);
    }

    // Upstream tsc/internal/checker/jsx.go:131:1.
    private void checkJsxOpeningLikeElementOrOpeningFragment(Node node)
    {
        var isNodeOpeningLikeElement = Ast.IsJsxOpeningLikeElement(node);
        if (isNodeOpeningLikeElement)
        {
            this.checkGrammarJsxElement(node);
        }

        this.checkJsxPreconditions(node);
        this.markJsxAliasReferenced(node);
        var sig = this.getResolvedSignature(node, null, CheckMode.Normal);
        this.checkDeprecatedSignature(sig, node);
        if (isNodeOpeningLikeElement)
        {
            var elementTypeConstraint = this.getJsxElementTypeTypeAt(node);
            if ((elementTypeConstraint != null))
            {
                var tagName = node.TagName();
                Type tagType = default;
                if (isJsxIntrinsicTagName(tagName))
                {
                    tagType = this.getStringLiteralType(tagName);
                }
                else
                {
                    tagType = this.checkExpression(tagName);
                }

                List<Diagnostic> diags = new();
                if (!this.checkTypeRelatedToEx(tagType, elementTypeConstraint, this.assignableRelation, tagName, Diagnostics.Its_type_0_is_not_a_valid_JSX_element_type, diags))
                {
                    this.addDiagnostic(Ast.NewDiagnosticChain(diags[0], Diagnostics.X_0_cannot_be_used_as_a_JSX_component, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(tagName)));
                }
            }
            else
            {
                this.checkJsxReturnAssignableToAppropriateBound(this.getJsxReferenceKind(node), this.getReturnTypeOfSignature(sig), node);
            }
        }
    }

    // Upstream tsc/internal/checker/jsx.go:160:1.
    private void checkJsxPreconditions(Node errorNode)
    {
        if ((this.compilerOptions.Jsx == JsxEmit.None))
        {
            this.error(errorNode, Diagnostics.Cannot_use_JSX_unless_the_jsx_flag_is_provided);
        }

        if ((this.noImplicitAny && (this.getJsxElementTypeAt(errorNode) == null)))
        {
            this.error(errorNode, Diagnostics.JSX_element_implicitly_has_type_any_because_the_global_type_JSX_Element_does_not_exist);
        }
    }

    // Upstream tsc/internal/checker/jsx.go:170:1.
    private void checkJsxReturnAssignableToAppropriateBound(JsxReferenceKind refKind, Type elemInstanceType, Node openingLikeElement)
    {
        List<Diagnostic> diags = new();
        switch (refKind)
        {
            case JsxReferenceKind.Function:
            {
                var sfcReturnConstraint = this.getJsxStatelessElementTypeAt(openingLikeElement);
                if ((sfcReturnConstraint != null))
                {
                    this.checkTypeRelatedToEx(elemInstanceType, sfcReturnConstraint, this.assignableRelation, openingLikeElement.TagName(), Diagnostics.Its_return_type_0_is_not_a_valid_JSX_element, diags);
                }

                break;
            }

            case JsxReferenceKind.Component:
            {
                var classConstraint = this.getJsxElementClassTypeAt(openingLikeElement);
                if ((classConstraint != null))
                {
                    this.checkTypeRelatedToEx(elemInstanceType, classConstraint, this.assignableRelation, openingLikeElement.TagName(), Diagnostics.Its_instance_type_0_is_not_a_valid_JSX_element, diags);
                }

                break;
            }

            default:
            {
                var sfcReturnConstraint2 = this.getJsxStatelessElementTypeAt(openingLikeElement);
                var classConstraint2 = this.getJsxElementClassTypeAt(openingLikeElement);
                if (((sfcReturnConstraint2 == null) || (classConstraint2 == null)))
                {
                    return;
                }

                var combined = this.getUnionType(new List<Type> { sfcReturnConstraint2, classConstraint2 });
                this.checkTypeRelatedToEx(elemInstanceType, combined, this.assignableRelation, openingLikeElement.TagName(), Diagnostics.Its_element_type_0_is_not_a_valid_JSX_element, diags);
                break;
            }
        }

        if ((GoRuntime.Len(diags) != 0))
        {
            this.addDiagnostic(Ast.NewDiagnosticChain(diags[0], Diagnostics.X_0_cannot_be_used_as_a_JSX_component, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(openingLikeElement.TagName())));
        }
    }

    // Upstream tsc/internal/checker/jsx.go:198:1.
    private List<Type> inferJsxTypeArguments(Node node, Signature signature, CheckMode checkMode, InferenceContext context)
    {
        var paramType = this.getEffectiveFirstArgumentForJsxSignature(signature, node);
        var checkAttrType = this.checkExpressionWithContextualType(node.Attributes(), paramType, context, checkMode);
        this.inferTypes(context.inferences, checkAttrType, paramType, InferencePriority.None, false);
        return this.getInferredTypes(context);
    }

    // Upstream tsc/internal/checker/jsx.go:205:1.
    private Type getContextualTypeForJsxExpression(Node node, ContextFlags contextFlags)
    {
        switch (true)
        {
            case true when Ast.IsJsxAttributeLike(node.Parent):
            {
                return this.getContextualType(node, contextFlags);
            }

            case true when Ast.IsJsxElement(node.Parent):
            {
                return this.getContextualTypeForChildJsxExpression(node.Parent, node, contextFlags);
            }
        }

        return null;
    }

    // Upstream tsc/internal/checker/jsx.go:215:1.
    private Type getContextualTypeForJsxAttribute(Node attribute, ContextFlags contextFlags)
    {
        if (Ast.IsJsxAttribute(attribute))
        {
            var attributesType = this.getApparentTypeOfContextualType(attribute.Parent, contextFlags);
            if (((attributesType == null) || IsTypeAny(attributesType)))
            {
                return null;
            }

            return this.getTypeOfPropertyOfContextualType(attributesType, attribute.Name().TextValue());
        }

        return this.getContextualType(attribute.Parent, contextFlags);
    }

    // Upstream tsc/internal/checker/jsx.go:229:1.
    private Type getContextualJsxElementAttributesType(Node node, ContextFlags contextFlags)
    {
        if ((Ast.IsJsxOpeningElement(node) && (contextFlags != ContextFlags.IgnoreNodeInferences)))
        {
            var index = this.findContextualNode(node.Parent, (contextFlags == ContextFlags.None));
            if ((index >= 0))
            {
                return this.contextualInfos[index].t;
            }
        }

        return this.getContextualTypeForArgumentAtIndex(node, 0);
    }

    // Upstream tsc/internal/checker/jsx.go:242:1.
    private Type getContextualTypeForChildJsxExpression(Node node, Node child, ContextFlags contextFlags)
    {
        var attributesType = this.getApparentTypeOfContextualType(node.AsJsxElement().OpeningElement.Attributes(), contextFlags);
        var jsxChildrenPropertyName = this.getJsxElementChildrenPropertyName(this.getJsxNamespaceAt(node));
        if (!(((((attributesType != null) && !IsTypeAny(attributesType)) && (jsxChildrenPropertyName != Ast.InternalSymbolNameMissing)) && (jsxChildrenPropertyName != ""))))
        {
            return null;
        }

        var realChildren = Ast.GetSemanticJsxChildren(node.Children().Nodes);
        var childIndex = GoSlices.Index(realChildren, child);
        var childFieldType = this.getTypeOfPropertyOfContextualType(attributesType, jsxChildrenPropertyName);
        if ((childFieldType == null))
        {
            return null;
        }

        if ((GoRuntime.Len(realChildren) == 1))
        {
            return childFieldType;
        }

        return this.mapTypeEx(childFieldType, (t) =>
        {
            if (this.isArrayLikeType(t))
            {
                return this.getIndexedAccessType(t, this.getNumberLiteralType(new JsNum.Number(childIndex)));
            }

            return t;
        }, true);
    }

    // Upstream tsc/internal/checker/jsx.go:266:1.
    private Type discriminateContextualTypeByJSXAttributes(Node node, Type contextualType)
    {
        var key = new DiscriminatedContextualTypeKey
        {
            nodeId = Ast.GetNodeId(node),
            typeId = contextualType.id
        };
        {
            var discriminated = this.discriminatedContextualTypes.GetValueOrDefault(key);
            if ((discriminated != null))
            {
                return discriminated;
            }
        }

        var jsxChildrenPropertyName = this.getJsxElementChildrenPropertyName(this.getJsxNamespaceAt(node));
        var discriminantProperties = Core.Filter(node.Properties(), (p) =>
        {
            var symbol = p.Symbol();
            if (((symbol == null) || !Ast.IsJsxAttribute(p)))
            {
                return false;
            }

            var initializer = p.Initializer();
            return ((((initializer == null) || this.isPossiblyDiscriminantValue(initializer))) && this.isDiscriminantProperty(contextualType, symbol.Name));
        });
        var discriminantMembers = Core.Filter(this.getPropertiesOfType(contextualType), (s) =>
        {
            if ((((s.Flags & SymbolFlags.Optional) == 0) || (node.Symbol() == null)))
            {
                return false;
            }

            var element = node.Parent.Parent;
            if ((((s.Name == jsxChildrenPropertyName) && Ast.IsJsxElement(element)) && (GoRuntime.Len(Ast.GetSemanticJsxChildren(element.Children().Nodes)) != 0)))
            {
                return false;
            }

            return ((node.Symbol().Members?[s.Name] == null) && this.isDiscriminantProperty(contextualType, s.Name));
        });
        var discriminator = new ObjectLiteralDiscriminator(this, discriminantProperties, discriminantMembers);
        var discriminated2 = this.discriminateTypeByDiscriminableItems(contextualType, discriminator);
        this.discriminatedContextualTypes[key] = discriminated2;
        return discriminated2;
    }

    // Upstream tsc/internal/checker/jsx.go:296:1.
    private bool elaborateJsxComponents(Node node, Type source, Type target, Relation relation, List<Diagnostic> diagnosticOutput)
    {
        var reportedError = false;
        {
            foreach (var prop in GoRuntime.RangeValues(node.Properties()))
            {
                if ((!Ast.IsJsxSpreadAttribute(prop) && !isHyphenatedJsxName(prop.Name().TextValue())))
                {
                    var nameType = this.getStringLiteralType(prop.Name());
                    if (((nameType != null) && ((nameType.flags & TypeFlags.Never) == 0)))
                    {
                        reportedError = (this.elaborateElement(source, target, relation, prop.Name(), prop.Initializer(), nameType, null, null, diagnosticOutput) || reportedError);
                    }
                }
            }
        }

        if ((Ast.IsJsxOpeningElement(node.Parent) && Ast.IsJsxElement(node.Parent.Parent)))
        {
            var containingElement = node.Parent.Parent;
            var childrenPropName = this.getJsxElementChildrenPropertyName(this.getJsxNamespaceAt(node));
            if ((childrenPropName == Ast.InternalSymbolNameMissing))
            {
                childrenPropName = "children";
            }

            var childrenNameType = this.getStringLiteralType(childrenPropName);
            var childrenTargetType = this.getIndexedAccessType(target, childrenNameType);
            var validChildren = Ast.GetSemanticJsxChildren(containingElement.Children().Nodes);
            if ((GoRuntime.Len(validChildren) == 0))
            {
                return reportedError;
            }

            var moreThanOneRealChildren = (GoRuntime.Len(validChildren) > 1);
            Type arrayLikeTargetParts = default;
            Type nonArrayLikeTargetParts = default;
            var iterableType = this.getGlobalIterableType();
            if ((iterableType != this.emptyGenericType))
            {
                var anyIterable = this.createIterableType(this.anyType);
                arrayLikeTargetParts = this.filterType(childrenTargetType, (t) =>
                {
                    return this.isTypeAssignableTo(t, anyIterable);
                });
                nonArrayLikeTargetParts = this.filterType(childrenTargetType, (t2) =>
                {
                    return !this.isTypeAssignableTo(t2, anyIterable);
                });
            }
            else
            {
                arrayLikeTargetParts = this.filterType(childrenTargetType, this.isArrayOrTupleLikeType);
                nonArrayLikeTargetParts = this.filterType(childrenTargetType, (t3) =>
                {
                    return !this.isArrayOrTupleLikeType(t3);
                });
            }

            Message invalidTextDiagnostic = default;
            List<object> invalidTextDiagnosticArgs = default;
            Func<(Message, List<object>)> getInvalidTextualChildDiagnostic = () =>
            {
                if ((invalidTextDiagnostic == null))
                {
                    var tagNameText = TypeScript.Scanner.Scanner.GetTextOfNodeByteString(node.Parent.TagName());
                    invalidTextDiagnostic = Diagnostics.X_0_components_don_t_accept_text_as_child_elements_Text_in_JSX_has_the_type_string_but_the_expected_type_of_1_is_2;
                    invalidTextDiagnosticArgs = new List<object>
                    {
                        tagNameText,
                        childrenPropName,
                        this.typeToStringByteString(childrenTargetType)
                    };
                }

                return (invalidTextDiagnostic, invalidTextDiagnosticArgs);
            };
            if (moreThanOneRealChildren)
            {
                if ((arrayLikeTargetParts != this.neverType))
                {
                    var realSource = this.createTupleType(this.checkJsxChildren(containingElement, CheckMode.Normal));
                    var children = this.generateJsxChildren(containingElement, getInvalidTextualChildDiagnostic);
                    reportedError = (this.elaborateIterableOrArrayLikeTargetElementwise(children, realSource, arrayLikeTargetParts, relation, diagnosticOutput) || reportedError);
                }
                else
                {
                    if (!this.isTypeRelatedTo(this.getIndexedAccessType(source, childrenNameType), childrenTargetType, relation))
                    {
                        var diag = this.error(containingElement.AsJsxElement().OpeningElement.TagName(), Diagnostics.This_JSX_tag_s_0_prop_expects_a_single_child_of_type_1_but_multiple_children_were_provided, childrenPropName, this.typeToStringByteString(childrenTargetType));
                        this.reportDiagnostic(diag, diagnosticOutput);
                        reportedError = true;
                    }
                }
            }
            else
            {
                if ((nonArrayLikeTargetParts != this.neverType))
                {
                    var child = validChildren[0];
                    var e = this.getElaborationElementForJsxChild(child, childrenNameType, getInvalidTextualChildDiagnostic);
                    if ((e.errorNode != null))
                    {
                        reportedError = (this.elaborateElement(source, target, relation, e.errorNode, e.innerExpression, e.nameType, null, e.createDiagnostic, diagnosticOutput) || reportedError);
                    }
                }
                else
                {
                    if (!this.isTypeRelatedTo(this.getIndexedAccessType(source, childrenNameType), childrenTargetType, relation))
                    {
                        var diag2 = this.error(containingElement.AsJsxElement().OpeningElement.TagName(), Diagnostics.This_JSX_tag_s_0_prop_expects_type_1_which_requires_multiple_children_but_only_a_single_child_was_provided, childrenPropName, this.typeToStringByteString(childrenTargetType));
                        this.reportDiagnostic(diag2, diagnosticOutput);
                        reportedError = true;
                    }
                }
            }
        }

        return reportedError;
    }

    // Upstream tsc/internal/checker/jsx.go:376. IEnumerable preserves lazy source order.
    private IEnumerable<JsxElaborationElement> generateJsxChildren(Node node, Func<(Message, List<object>)> getInvalidTextDiagnostic)
    {
        int memberOffset = 0;
        for (int i = 0; i < GoRuntime.Len(node.Children().Nodes); i++)
        {
            var child = node.Children().Nodes[i];
            var nameType = getNumberLiteralType(new JsNum.Number(i - memberOffset));
            var element = getElaborationElementForJsxChild(child, nameType, getInvalidTextDiagnostic);
            if (element.errorNode != null)
                yield return element;
            else
                memberOffset++;
        }
    }

    // Upstream tsc/internal/checker/jsx.go:393:1.
    private JsxElaborationElement getElaborationElementForJsxChild(Node child, Type nameType, Func<(Message, List<object>)> getInvalidTextDiagnostic)
    {
        switch (child.Kind)
        {
            case Kind.JsxExpression:
            {
                return new JsxElaborationElement
                {
                    errorNode = child,
                    innerExpression = child.Expression(),
                    nameType = nameType
                };
            }

            case Kind.JsxText:
            {
                if (child.AsJsxText().ContainsOnlyTriviaWhiteSpaces)
                {
                    return new JsxElaborationElement
                    {
                    };
                }

                return new JsxElaborationElement
                {
                    errorNode = child,
                    innerExpression = null,
                    nameType = nameType,
                    createDiagnostic = (prop) =>
                    {
                        var tuple1 = getInvalidTextDiagnostic();
                        var errorMessage = tuple1.Item1;
                        var errorArgs = tuple1.Item2;
                        return NewDiagnosticForNode(prop, errorMessage, errorArgs?.ToArray() ?? []);
                    }
                };
            }

            case Kind.JsxElement:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxFragment:
            {
                return new JsxElaborationElement
                {
                    errorNode = child,
                    innerExpression = child,
                    nameType = nameType
                };
            }
        }

        throw new InvalidOperationException("Unhandled case in getElaborationElementForJsxChild");
    }

    // Upstream tsc/internal/checker/jsx.go:420:1.
    private bool elaborateIterableOrArrayLikeTargetElementwise(IEnumerable<JsxElaborationElement> iterator, Type source, Type target, Relation relation, List<Diagnostic> diagnosticOutput)
    {
        var tupleOrArrayLikeTargetParts = this.filterType(target, this.isArrayOrTupleLikeType);
        var nonTupleOrArrayLikeTargetParts = this.filterType(target, (t) =>
        {
            return !this.isArrayOrTupleLikeType(t);
        });
        Type iterationType = default;
        if ((nonTupleOrArrayLikeTargetParts != this.neverType))
        {
            iterationType = this.getIterationTypeOfIterable(IterationUse.ForOf, IterationTypeKind.Yield, nonTupleOrArrayLikeTargetParts, null);
        }

        var reportedError = false;
        {
            foreach (var e in iterator)
            {
                var prop = e.errorNode;
                var next = e.innerExpression;
                var nameType = e.nameType;
                var targetPropType = iterationType;
                Type targetIndexedPropType = default;
                if ((tupleOrArrayLikeTargetParts != this.neverType))
                {
                    targetIndexedPropType = this.getBestMatchIndexedAccessTypeOrUndefined(source, tupleOrArrayLikeTargetParts, nameType);
                }

                if (((targetIndexedPropType != null) && ((targetIndexedPropType.flags & TypeFlags.IndexedAccess) == 0)))
                {
                    if ((iterationType != null))
                    {
                        targetPropType = this.getUnionType(new List<Type> { iterationType, targetIndexedPropType });
                    }
                    else
                    {
                        targetPropType = targetIndexedPropType;
                    }
                }

                if ((targetPropType == null))
                {
                    continue;
                }

                var sourcePropType = this.getIndexedAccessTypeOrUndefined(source, nameType, AccessFlags.None, null, null);
                if ((sourcePropType == null))
                {
                    continue;
                }

                var propName = this.getPropertyNameFromIndex(nameType, null);
                if (!this.checkTypeRelatedTo(sourcePropType, targetPropType, relation, null))
                {
                    var elaborated = ((next != null) && this.elaborateError(next, sourcePropType, targetPropType, relation, null, diagnosticOutput));
                    reportedError = true;
                    if (!elaborated)
                    {
                        var specificSource = sourcePropType;
                        if ((next != null))
                        {
                            specificSource = this.checkExpressionForMutableLocationWithContextualType(next, sourcePropType);
                        }

                        if ((e.createDiagnostic != null))
                        {
                            this.reportDiagnostic(e.createDiagnostic(prop), diagnosticOutput);
                        }
                        else
                        {
                            if ((this.exactOptionalPropertyTypes && this.isExactOptionalPropertyMismatch(specificSource, targetPropType)))
                            {
                                var diag = createDiagnosticForNode(prop, Diagnostics.Type_0_is_not_assignable_to_type_1_with_exactOptionalPropertyTypes_Colon_true_Consider_adding_undefined_to_the_type_of_the_target, this.typeToStringByteString(specificSource), this.typeToStringByteString(targetPropType));
                                this.reportDiagnostic(diag, diagnosticOutput);
                            }
                            else
                            {
                                var targetIsOptional = ((propName != Ast.InternalSymbolNameMissing) && ((Core.OrElse(this.getPropertyOfType(tupleOrArrayLikeTargetParts, propName), this.unknownSymbol).Flags & SymbolFlags.Optional) != 0));
                                var sourceIsOptional = ((propName != Ast.InternalSymbolNameMissing) && ((Core.OrElse(this.getPropertyOfType(source, propName), this.unknownSymbol).Flags & SymbolFlags.Optional) != 0));
                                targetPropType = this.removeMissingType(targetPropType, targetIsOptional);
                                sourcePropType = this.removeMissingType(sourcePropType, (targetIsOptional && sourceIsOptional));
                                var result = this.checkTypeRelatedToEx(specificSource, targetPropType, relation, prop, null, diagnosticOutput);
                                if ((result && (specificSource != sourcePropType)))
                                {
                                    this.checkTypeRelatedToEx(sourcePropType, targetPropType, relation, prop, null, diagnosticOutput);
                                }
                            }
                        }
                    }
                }
            }
        }

        return reportedError;
    }

    // Upstream tsc/internal/checker/jsx.go:485:1.
    private Symbol getSuggestedSymbolForNonexistentJSXAttribute(string name, Type containingType)
    {
        var properties = this.getPropertiesOfType(containingType);
        Symbol jsxSpecific = default;
        switch (name)
        {
            case "for":
            {
                jsxSpecific = Core.Find(properties, (x) =>
                {
                    return (Ast.SymbolName(x) == "htmlFor");
                });
                break;
            }

            case "class":
            {
                jsxSpecific = Core.Find(properties, (x2) =>
                {
                    return (Ast.SymbolName(x2) == "className");
                });
                break;
            }
        }

        if ((jsxSpecific != null))
        {
            return jsxSpecific;
        }

        return this.getSpellingSuggestionForName(name, properties ?? [], SymbolFlags.Value);
    }

    // Upstream tsc/internal/checker/jsx.go:500:1.
    private Type getJSXFragmentType(Node node)
    {
        var links = this.sourceFileLinks.Get(Ast.GetSourceFileOfNode(node).AsNode());
        if ((links.jsxFragmentType != null))
        {
            return links.jsxFragmentType;
        }

        var jsxFragmentFactoryName = this.getJsxNamespace(node);
        var shouldResolveFactoryReference = ((((this.compilerOptions.Jsx == JsxEmit.React) || (this.compilerOptions.JsxFragmentFactory != ""))) && (jsxFragmentFactoryName != "null"));
        if (!shouldResolveFactoryReference)
        {
            links.jsxFragmentType = this.anyType;
            return links.jsxFragmentType;
        }

        var jsxFactorySymbol = this.getJsxNamespaceContainerForImplicitImport(node);
        if ((jsxFactorySymbol == null))
        {
            var shouldModuleRefErr = ((this.compilerOptions.Jsx != JsxEmit.Preserve) && (this.compilerOptions.Jsx != JsxEmit.ReactNative));
            var flags = SymbolFlags.Value;
            if (!shouldModuleRefErr)
            {
                flags &= ~(SymbolFlags.Enum);
            }

            jsxFactorySymbol = this.resolveName(node, jsxFragmentFactoryName, flags, Diagnostics.Using_JSX_fragments_requires_fragment_factory_0_to_be_in_scope_but_it_could_not_be_found, true, false);
        }

        if ((jsxFactorySymbol == null))
        {
            links.jsxFragmentType = this.errorType;
            return links.jsxFragmentType;
        }

        if ((jsxFactorySymbol.Name == ReactNames.Fragment))
        {
            links.jsxFragmentType = this.getTypeOfSymbol(jsxFactorySymbol);
            return links.jsxFragmentType;
        }

        var resolvedAlias = jsxFactorySymbol;
        if (((jsxFactorySymbol.Flags & SymbolFlags.Alias) != 0))
        {
            resolvedAlias = this.resolveAlias(jsxFactorySymbol);
        }

        var reactExports = this.getExportsOfSymbol(resolvedAlias);
        var typeSymbol = this.getSymbol(reactExports, ReactNames.Fragment, SymbolFlags.BlockScopedVariable);
        if ((typeSymbol != null))
        {
            links.jsxFragmentType = this.getTypeOfSymbol(typeSymbol);
        }
        else
        {
            links.jsxFragmentType = this.errorType;
        }

        return links.jsxFragmentType;
    }

    // Upstream tsc/internal/checker/jsx.go:545:1.
    private Signature resolveJsxOpeningLikeElement(Node node, GoRef<Slice<Signature>> candidatesOutArray, CheckMode checkMode)
    {
        var isJsxOpenFragment = Ast.IsJsxOpeningFragment(node);
        Type exprTypes = default;
        if (!isJsxOpenFragment)
        {
            if (isJsxIntrinsicTagName(node.TagName()))
            {
                var result = this.getIntrinsicAttributesTypeFromJsxOpeningLikeElement(node);
                var fakeSignature = this.createSignatureForJSXIntrinsic(node, result);
                this.checkTypeAssignableToAndOptionallyElaborate(this.checkExpressionWithContextualType(node.Attributes(), this.getEffectiveFirstArgumentForJsxSignature(fakeSignature, node), null, CheckMode.Normal), result, node.TagName(), node.Attributes(), null, null);
                var typeArguments = node.TypeArguments();
                if ((GoRuntime.Len(typeArguments) != 0))
                {
                    this.checkSourceElements(typeArguments);
                    var sourceFile = Ast.GetSourceFileOfNode(node);
                    var typeArgumentList = node.TypeArgumentList();
                    var loc = Core.NewTextRange(TypeScript.Scanner.Scanner.SkipTrivia(sourceFile.TextView(), typeArgumentList.Loc.Pos()), typeArgumentList.Loc.End());
                    this.addDiagnostic(Ast.NewDiagnostic(sourceFile, loc, Diagnostics.Expected_0_type_arguments_but_got_1, 0, GoRuntime.Len(typeArguments)));
                }

                return fakeSignature;
            }

            exprTypes = this.checkExpression(node.TagName());
        }
        else
        {
            exprTypes = this.getJSXFragmentType(node);
        }

        var apparentType = this.getApparentType(exprTypes);
        if (this.isErrorType(apparentType))
        {
            return this.resolveErrorCall(node);
        }

        var signatures = this.getUninstantiatedJsxSignaturesOfType(exprTypes, node);
        if (this.isUntypedFunctionCall(exprTypes, apparentType, GoRuntime.Len(signatures), 0))
        {
            return this.resolveUntypedCall(node);
        }

        if ((GoRuntime.Len(signatures) == 0))
        {
            if (isJsxOpenFragment)
            {
                this.error(node, Diagnostics.JSX_element_type_0_does_not_have_any_construct_or_call_signatures, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(node));
            }
            else
            {
                this.error(node.TagName(), Diagnostics.JSX_element_type_0_does_not_have_any_construct_or_call_signatures, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(node.TagName()));
            }

            return this.resolveErrorCall(node);
        }

        return this.resolveCall(node, signatures, candidatesOutArray, checkMode, SignatureFlags.None, null);
    }

    // Check if the given signature can possibly be a signature called by the JSX opening-like element.
    // @param node a JSX opening-like element we are trying to figure its call signature
    // @param signature a candidate signature we are trying whether it is a call signature
    // @param relation a relationship to check parameter and argument type
    // Upstream tsc/internal/checker/jsx.go:591:1.
    private bool checkApplicableSignatureForJsxCallLikeElement(Node node, Signature signature, Relation relation, CheckMode checkMode, bool reportErrors, List<Diagnostic> diagnosticOutput)
    {
        var paramType = this.getEffectiveFirstArgumentForJsxSignature(signature, node);
        Type attributesType = default;
        if (Ast.IsJsxOpeningFragment(node))
        {
            attributesType = this.createJsxAttributesTypeFromAttributesProperty(node, CheckMode.Normal);
        }
        else
        {
            attributesType = this.checkExpressionWithContextualType(node.Attributes(), paramType, null, checkMode);
        }

        Type checkAttributesType = default;
        Func<bool> checkTagNameDoesNotExpectTooManyArguments = () =>
        {
            if ((this.getJsxNamespaceContainerForImplicitImport(node) != null))
            {
                return true;
            }

            Type tagType = default;
            if ((((Ast.IsJsxOpeningElement(node) || Ast.IsJsxSelfClosingElement(node))) && !((isJsxIntrinsicTagName(node.TagName()) || Ast.IsJsxNamespacedName(node.TagName())))))
            {
                tagType = this.checkExpression(node.TagName());
            }

            if ((tagType == null))
            {
                return true;
            }

            var tagCallSignatures = this.getSignaturesOfType(tagType, SignatureKind.Call);
            if ((GoRuntime.Len(tagCallSignatures) == 0))
            {
                return true;
            }

            var factory = this.getJsxFactoryEntity(node);
            if ((factory == null))
            {
                return true;
            }

            var factorySymbol = this.resolveEntityName(factory, SymbolFlags.Value, true, false, node);
            if ((factorySymbol == null))
            {
                return true;
            }

            var factoryType = this.getTypeOfSymbol(factorySymbol);
            var callSignatures = this.getSignaturesOfType(factoryType, SignatureKind.Call);
            if ((GoRuntime.Len(callSignatures) == 0))
            {
                return true;
            }

            var hasFirstParamSignatures = false;
            var maxParamCount = 0;
            {
                foreach (var sig in GoRuntime.RangeValues(callSignatures))
                {
                    var firstparam = this.getTypeAtPosition(sig, 0);
                    var signaturesOfParam = this.getSignaturesOfType(firstparam, SignatureKind.Call);
                    if ((GoRuntime.Len(signaturesOfParam) == 0))
                    {
                        continue;
                    }

                    {
                        foreach (var range2 in GoRuntime.Range(signaturesOfParam))
                        {
                            var _2 = range2.Key;
                            var paramSig = range2.Value;
                            hasFirstParamSignatures = true;
                            if (this.hasEffectiveRestParameter(paramSig))
                            {
                                return true;
                            }

                            var paramCount = this.getParameterCount(paramSig);
                            if ((paramCount > maxParamCount))
                            {
                                maxParamCount = paramCount;
                            }
                        }
                    }
                }
            }

            if (!hasFirstParamSignatures)
            {
                return true;
            }

            var absoluteMinArgCount = int.MaxValue;
            {
                foreach (var range3 in GoRuntime.Range(tagCallSignatures))
                {
                    var _3 = range3.Key;
                    var tagSig = range3.Value;
                    var tagRequiredArgCount = this.getMinArgumentCount(tagSig);
                    if ((tagRequiredArgCount < absoluteMinArgCount))
                    {
                        absoluteMinArgCount = tagRequiredArgCount;
                    }
                }
            }

            if ((absoluteMinArgCount <= maxParamCount))
            {
                return true;
            }

            if (reportErrors)
            {
                var tagName = node.TagName();
                var diag = NewDiagnosticForNode(tagName, Diagnostics.Tag_0_expects_at_least_1_arguments_but_the_JSX_factory_2_provides_at_most_3, entityNameToString(tagName), absoluteMinArgCount, entityNameToString(factory), maxParamCount);
                var tagNameSymbol = this.getSymbolAtLocation(tagName, false);
                if (((tagNameSymbol != null) && (tagNameSymbol.ValueDeclaration != null)))
                {
                    diag.AddRelatedInfo(NewDiagnosticForNode(tagNameSymbol.ValueDeclaration, Diagnostics.X_0_is_declared_here, entityNameToString(tagName)));
                }

                this.reportDiagnostic(diag, diagnosticOutput);
            }

            return false;
        };
        if (((checkMode & CheckMode.SkipContextSensitive) != 0))
        {
            checkAttributesType = this.getRegularTypeOfObjectLiteral(attributesType);
        }
        else
        {
            checkAttributesType = attributesType;
        }

        if (!checkTagNameDoesNotExpectTooManyArguments())
        {
            return false;
        }

        Node errorNode = default;
        if (reportErrors)
        {
            if (Ast.IsJsxOpeningFragment(node))
            {
                errorNode = node;
            }
            else
            {
                errorNode = node.TagName();
            }
        }

        Node attributes = default;
        if (!Ast.IsJsxOpeningFragment(node))
        {
            attributes = node.Attributes();
        }

        return this.checkTypeRelatedToAndOptionallyElaborate(checkAttributesType, paramType, relation, errorNode, attributes, null, diagnosticOutput);
    }

    // Get attributes type of the JSX opening-like element. The result is from resolving "attributes" property of the opening-like element.
    //
    // @param openingLikeElement a JSX opening-like element
    // @param filter a function to remove attributes that will not participate in checking whether attributes are assignable
    // @return an anonymous type (similar to the one returned by checkObjectLiteral) in which its properties are attributes property.
    // @remarks Because this function calls getSpreadType, it needs to use the same checks as checkObjectLiteral,
    // which also calls getSpreadType.
    // Upstream tsc/internal/checker/jsx.go:710:1.
    private Type createJsxAttributesTypeFromAttributesProperty(Node openingLikeElement, CheckMode checkMode)
    {
        SymbolTable allAttributesTable = default;
        if (this.strictNullChecks)
        {
            allAttributesTable = new SymbolTable();
        }

        var attributesTable = new SymbolTable();
        Symbol attributesSymbol = default;
        var attributeParent = openingLikeElement;
        var spread = this.emptyJsxObjectType;
        bool hasSpreadAnyType = default;
        Type typeToIntersect = default;
        bool explicitlySpecifyChildrenAttribute = default;
        var objectFlags = ObjectFlags.JsxAttributes;
        Func<Type> createJsxAttributesType = () =>
        {
            objectFlags |= ObjectFlags.FreshLiteral;
            var result = this.newAnonymousType(attributesSymbol, attributesTable, default, default, null);
            result.objectFlags |= ((objectFlags | ObjectFlags.ObjectLiteral) | ObjectFlags.ContainsObjectOrArrayLiteral);
            return result;
        };
        var jsxChildrenPropertyName = this.getJsxElementChildrenPropertyName(this.getJsxNamespaceAt(openingLikeElement));
        var isJsxOpenFragment = Ast.IsJsxOpeningFragment(openingLikeElement);
        if (!isJsxOpenFragment)
        {
            var attributes = openingLikeElement.Attributes();
            attributesSymbol = attributes.Symbol();
            attributeParent = attributes;
            var contextualType = this.getContextualType(attributes, ContextFlags.None);
            {
                foreach (var attributeDecl in GoRuntime.RangeValues(attributes.Properties()))
                {
                    var member = attributeDecl.Symbol();
                    if (Ast.IsJsxAttribute(attributeDecl))
                    {
                        var exprType = this.checkJsxAttribute(attributeDecl, checkMode);
                        objectFlags |= (exprType.objectFlags & ObjectFlags.PropagatingFlags);
                        var attributeSymbol = this.newSymbol((SymbolFlags.Property | member.Flags), member.Name);
                        attributeSymbol.Declarations = member.Declarations;
                        attributeSymbol.Parent = member.Parent;
                        if ((member.ValueDeclaration != null))
                        {
                            attributeSymbol.ValueDeclaration = member.ValueDeclaration;
                        }

                        var links = this.valueSymbolLinks.Get(attributeSymbol);
                        links.resolvedType = exprType;
                        links.target = member;
                        attributesTable[attributeSymbol.Name] = attributeSymbol;
                        if ((allAttributesTable != null))
                        {
                            allAttributesTable[attributeSymbol.Name] = attributeSymbol;
                        }

                        if ((attributeDecl.Name().TextValue().EqualsString(jsxChildrenPropertyName)))
                        {
                            explicitlySpecifyChildrenAttribute = true;
                        }

                        if (((((contextualType != null) && ((checkMode & CheckMode.Inferential) != 0)) && ((checkMode & CheckMode.SkipContextSensitive) == 0)) && this.isContextSensitive(attributeDecl)))
                        {
                            var inferenceContext = this.getInferenceContext(attributes);
                            TypeScript.Scanner.Debug.Assert((inferenceContext != null));
                            var inferenceNode = attributeDecl.Initializer().Expression();
                            this.addIntraExpressionInferenceSite(inferenceContext, inferenceNode, exprType);
                        }
                    }
                    else
                    {
                        TypeScript.Scanner.Debug.Assert((attributeDecl.Kind == Kind.JsxSpreadAttribute));
                        if ((GoRuntime.Len(attributesTable) != 0))
                        {
                            spread = this.getSpreadType(spread, createJsxAttributesType(), attributesSymbol, objectFlags, false);
                            attributesTable = new SymbolTable();
                        }

                        var exprType2 = this.getReducedType(this.checkExpressionEx(attributeDecl.Expression(), (checkMode & CheckMode.Inferential)));
                        if (IsTypeAny(exprType2))
                        {
                            hasSpreadAnyType = true;
                        }

                        if (this.isValidSpreadType(exprType2))
                        {
                            spread = this.getSpreadType(spread, exprType2, attributesSymbol, objectFlags, false);
                            if ((allAttributesTable != null))
                            {
                                this.checkSpreadPropOverrides(exprType2, allAttributesTable, attributeDecl);
                            }
                        }
                        else
                        {
                            this.error(attributeDecl.Expression(), Diagnostics.Spread_types_may_only_be_created_from_object_types);
                            if ((typeToIntersect != null))
                            {
                                typeToIntersect = this.getIntersectionType(new List<Type> { typeToIntersect, exprType2 });
                            }
                            else
                            {
                                typeToIntersect = exprType2;
                            }
                        }
                    }
                }
            }

            if (!hasSpreadAnyType)
            {
                if ((GoRuntime.Len(attributesTable) != 0))
                {
                    spread = this.getSpreadType(spread, createJsxAttributesType(), attributesSymbol, objectFlags, false);
                }
            }
        }

        Func<Node, bool> parentHasSemanticJsxChildren = (openingLikeElement2) =>
        {
            var parent = openingLikeElement2.Parent;
            if ((parent == null))
            {
                return false;
            }

            Slice<Node> children = default;
            switch (true)
            {
                case true when Ast.IsJsxElement(parent):
                {
                    if ((parent.AsJsxElement().OpeningElement == openingLikeElement2))
                    {
                        children = parent.Children().Nodes;
                    }

                    break;
                }

                case true when Ast.IsJsxFragment(parent):
                {
                    if ((parent.AsJsxFragment().OpeningFragment == openingLikeElement2))
                    {
                        children = parent.Children().Nodes;
                    }

                    break;
                }
            }

            return (GoRuntime.Len(Ast.GetSemanticJsxChildren(children)) != 0);
        };
        if (parentHasSemanticJsxChildren(openingLikeElement))
        {
            List<Type> childTypes = this.checkJsxChildren(openingLikeElement.Parent, checkMode);
            if (((!hasSpreadAnyType && (jsxChildrenPropertyName != Ast.InternalSymbolNameMissing)) && (jsxChildrenPropertyName != "")))
            {
                if (explicitlySpecifyChildrenAttribute)
                {
                    this.error(attributeParent, Diagnostics.X_0_are_specified_twice_The_attribute_named_0_will_be_overwritten, jsxChildrenPropertyName);
                }

                Type childrenContextualType = default;
                if (Ast.IsJsxOpeningElement(openingLikeElement))
                {
                    {
                        var contextualType2 = this.getApparentTypeOfContextualType(openingLikeElement.Attributes(), ContextFlags.None);
                        if ((contextualType2 != null))
                        {
                            childrenContextualType = this.getTypeOfPropertyOfContextualType(contextualType2, jsxChildrenPropertyName);
                        }
                    }
                }

                var childrenPropSymbol = this.newSymbol(SymbolFlags.Property, jsxChildrenPropertyName);
                var links2 = this.valueSymbolLinks.Get(childrenPropSymbol);
                switch (true)
                {
                    case true when (GoRuntime.Len(childTypes) == 1):
                    {
                        links2.resolvedType = childTypes[0];
                        break;
                    }

                    case true when ((childrenContextualType != null) && someType(childrenContextualType, this.isTupleLikeType)):
                    {
                        links2.resolvedType = this.createTupleType(childTypes);
                        break;
                    }

                    default:
                    {
                        links2.resolvedType = this.createArrayType(this.getUnionType(childTypes));
                        break;
                    }
                }

                childrenPropSymbol.ValueDeclaration = this.factory.NewPropertySignatureDeclaration(null, this.factory.NewIdentifier(jsxChildrenPropertyName), null, null, null);
                childrenPropSymbol.ValueDeclaration.Parent = attributeParent;
                childrenPropSymbol.ValueDeclaration.AsPropertySignatureDeclaration().Symbol = childrenPropSymbol;
                var childPropMap = new SymbolTable();
                childPropMap[jsxChildrenPropertyName] = childrenPropSymbol;
                spread = this.getSpreadType(spread, this.newAnonymousType(attributesSymbol, childPropMap, default, default, null), attributesSymbol, (objectFlags | this.getPropagatingFlagsOfTypes(childTypes, TypeFlags.None)), false);
            }
        }

        if (hasSpreadAnyType)
        {
            return this.anyType;
        }

        if ((typeToIntersect != null))
        {
            if ((spread != this.emptyJsxObjectType))
            {
                return this.getIntersectionType(new List<Type> { typeToIntersect, spread });
            }

            return typeToIntersect;
        }

        if ((spread == this.emptyJsxObjectType))
        {
            return createJsxAttributesType();
        }

        return spread;
    }

    // Upstream tsc/internal/checker/jsx.go:869:1.
    private Type checkJsxAttribute(Node node, CheckMode checkMode)
    {
        if ((node.Initializer() != null))
        {
            return this.checkExpressionForMutableLocation(node.Initializer(), checkMode);
        }

        return this.trueType;
    }

    // Upstream tsc/internal/checker/jsx.go:877:1.
    private List<Type> checkJsxChildren(Node node, CheckMode checkMode)
    {
        List<Type> childTypes = default;
        {
            foreach (var child in GoRuntime.RangeValues(node.Children().Nodes))
            {
                if (Ast.IsJsxText(child))
                {
                    if (!child.AsJsxText().ContainsOnlyTriviaWhiteSpaces)
                    {
                        childTypes = GoRuntime.Append(childTypes, this.stringType);
                    }
                }
                else
                {
                    if ((Ast.IsJsxExpression(child) && (child.Expression() == null)))
                    {
                        continue;
                    }
                    else
                    {
                        childTypes = GoRuntime.Append(childTypes, this.checkExpressionForMutableLocation(child, checkMode));
                    }
                }
            }
        }

        return childTypes;
    }

    // Upstream tsc/internal/checker/jsx.go:896:1.
    private Slice<Signature> getUninstantiatedJsxSignaturesOfType(Type elementType, Node caller)
    {
        if (((elementType.flags & TypeFlags.String) != 0))
        {
            return Slice<Signature>.FromArray([this.anySignature]);
        }

        if (((elementType.flags & TypeFlags.StringLiteral) != 0))
        {
            var intrinsicType = this.getIntrinsicAttributesTypeFromStringLiteralType(elementType, caller);
            if ((intrinsicType == null))
            {
                this.error(caller, Diagnostics.Property_0_does_not_exist_on_type_1, getStringLiteralValue(elementType), ("JSX." + JsxNames.IntrinsicElements));
                return default;
            }

            var fakeSignature = this.createSignatureForJSXIntrinsic(caller, intrinsicType);
            return Slice<Signature>.FromArray([fakeSignature]);
        }

        var apparentElemType = this.getApparentType(elementType);
        var signatures = this.getSignaturesOfType(apparentElemType, SignatureKind.Construct);
        if ((GoRuntime.Len(signatures) == 0))
        {
            signatures = this.getSignaturesOfType(apparentElemType, SignatureKind.Call);
        }

        if (((GoRuntime.Len(signatures) == 0) && ((apparentElemType.flags & TypeFlags.Union) != 0)))
        {
            signatures = this.getUnionSignatures(Core.Map(apparentElemType.Types(), (t) =>
            {
                return this.getUninstantiatedJsxSignaturesOfType(t, caller);
            }));
        }

        return signatures;
    }

    // Upstream tsc/internal/checker/jsx.go:925:1.
    private Type getEffectiveFirstArgumentForJsxSignature(Signature signature, Node node)
    {
        if ((Ast.IsJsxOpeningFragment(node) || (this.getJsxReferenceKind(node) != JsxReferenceKind.Component)))
        {
            return this.getJsxPropsTypeFromCallSignature(signature, node);
        }

        return this.getJsxPropsTypeFromClassType(signature, node);
    }

    // Upstream tsc/internal/checker/jsx.go:932:1.
    private Type getJsxPropsTypeFromCallSignature(Signature sig, Node context)
    {
        var propsType = this.getTypeOfFirstParameterOfSignatureWithFallback(sig, this.unknownType);
        propsType = this.getJsxManagedAttributesFromLocatedAttributes(context, this.getJsxNamespaceAt(context), propsType);
        var intrinsicAttribs = this.getJsxType(JsxNames.IntrinsicAttributes, context);
        if (!this.isErrorType(intrinsicAttribs))
        {
            propsType = this.intersectTypes(intrinsicAttribs, propsType);
        }

        return propsType;
    }

    // Upstream tsc/internal/checker/jsx.go:942:1.
    private Type getJsxPropsTypeFromClassType(Signature sig, Node context)
    {
        var ns = this.getJsxNamespaceAt(context);
        var forcedLookupLocation = this.getJsxElementPropertiesName(ns);
        Type attributesType = default;
        switch (forcedLookupLocation)
        {
            case Ast.InternalSymbolNameMissing:
            {
                attributesType = this.getTypeOfFirstParameterOfSignatureWithFallback(sig, this.unknownType);
                break;
            }

            case "":
            {
                attributesType = this.getReturnTypeOfSignature(sig);
                break;
            }

            default:
            {
                attributesType = this.getJsxPropsTypeForSignatureFromMember(sig, forcedLookupLocation);
                if (((attributesType == null) && (GoRuntime.Len(context.Attributes().Properties()) != 0)))
                {
                    this.error(context, Diagnostics.JSX_element_class_does_not_support_attributes_because_it_does_not_have_a_0_property, forcedLookupLocation);
                }

                break;
            }
        }

        if ((attributesType == null))
        {
            return this.unknownType;
        }

        attributesType = this.getJsxManagedAttributesFromLocatedAttributes(context, ns, attributesType);
        if (IsTypeAny(attributesType))
        {
            return attributesType;
        }

        var apparentAttributesType = attributesType;
        var intrinsicClassAttribs = this.getJsxType(JsxNames.IntrinsicClassAttributes, context);
        if (!this.isErrorType(intrinsicClassAttribs))
        {
            var typeParams = this.getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(intrinsicClassAttribs.symbol);
            var hostClassType = this.getReturnTypeOfSignature(sig);
            Type libraryManagedAttributeType = default;
            if ((typeParams != null))
            {
                var inferredArgs = this.fillMissingTypeArguments(new List<Type> { hostClassType }, typeParams, this.getMinTypeArgumentCount(typeParams), Ast.IsInJSFile(context));
                libraryManagedAttributeType = this.instantiateType(intrinsicClassAttribs, newTypeMapper(typeParams, inferredArgs));
            }
            else
            {
                libraryManagedAttributeType = intrinsicClassAttribs;
            }

            apparentAttributesType = this.intersectTypes(libraryManagedAttributeType, apparentAttributesType);
        }

        var intrinsicAttribs = this.getJsxType(JsxNames.IntrinsicAttributes, context);
        if (!this.isErrorType(intrinsicAttribs))
        {
            apparentAttributesType = this.intersectTypes(intrinsicAttribs, apparentAttributesType);
        }

        return apparentAttributesType;
    }

    // Upstream tsc/internal/checker/jsx.go:989:1.
    private Type getJsxPropsTypeForSignatureFromMember(Signature sig, string forcedLookupLocation)
    {
        if ((sig.composite != null))
        {
            List<Type> results = default;
            {
                foreach (var signature in GoRuntime.RangeValues(sig.composite.signatures))
                {
                    var instance = this.getReturnTypeOfSignature(signature);
                    if (IsTypeAny(instance))
                    {
                        return instance;
                    }

                    var propType = this.getTypeOfPropertyOfType(instance, forcedLookupLocation);
                    if ((propType == null))
                    {
                        return null;
                    }

                    results = GoRuntime.Append(results, propType);
                }
            }

            return this.getIntersectionType(results);
        }

        var instanceType = this.getReturnTypeOfSignature(sig);
        if (IsTypeAny(instanceType))
        {
            return instanceType;
        }

        return this.getTypeOfPropertyOfType(instanceType, forcedLookupLocation);
    }

    // Upstream tsc/internal/checker/jsx.go:1018:1.
    private Type getJsxManagedAttributesFromLocatedAttributes(Node context, Symbol ns, Type attributesType)
    {
        var managedSym = this.getJsxLibraryManagedAttributes(ns);
        if ((managedSym != null))
        {
            var ctorType = this.getStaticTypeOfReferencedJsxConstructor(context);
            var result = this.instantiateAliasOrInterfaceWithDefaults(managedSym, new List<Type> { ctorType, attributesType }, Ast.IsInJSFile(context));
            if ((result != null))
            {
                return result;
            }
        }

        return attributesType;
    }

    // Upstream tsc/internal/checker/jsx.go:1030:1.
    private Type instantiateAliasOrInterfaceWithDefaults(Symbol managedSym, List<Type> typeArguments, bool inJavaScript)
    {
        var declaredManagedType = this.getDeclaredTypeOfSymbol(managedSym);
        if (((managedSym.Flags & SymbolFlags.TypeAlias) != 0))
        {
            var @params = this.typeAliasLinks.Get(managedSym).typeParameters;
            if ((GoRuntime.Len(@params) >= GoRuntime.Len(typeArguments)))
            {
                var args = this.fillMissingTypeArguments(typeArguments, @params, GoRuntime.Len(typeArguments), inJavaScript);
                if ((GoRuntime.Len(args) == 0))
                {
                    return declaredManagedType;
                }

                return this.getTypeAliasInstantiation(managedSym, args, null);
            }
        }

        if ((((declaredManagedType.objectFlags & ObjectFlags.ClassOrInterface) != 0) && (GoRuntime.Len(declaredManagedType.AsInterfaceType().TypeParameters()) >= GoRuntime.Len(typeArguments))))
        {
            var args2 = this.fillMissingTypeArguments(typeArguments, declaredManagedType.AsInterfaceType().TypeParameters(), GoRuntime.Len(typeArguments), inJavaScript);
            return this.createTypeReference(declaredManagedType, args2);
        }

        return null;
    }

    // Upstream tsc/internal/checker/jsx.go:1050:1.
    private Symbol getJsxLibraryManagedAttributes(Symbol jsxNamespace)
    {
        if ((jsxNamespace != null))
        {
            return this.getSymbol(jsxNamespace.Exports, JsxNames.LibraryManagedAttributes, SymbolFlags.Type);
        }

        return null;
    }

    // Upstream tsc/internal/checker/jsx.go:1057:1.
    private Symbol getJsxElementTypeSymbol(Symbol jsxNamespace)
    {
        if ((jsxNamespace != null))
        {
            return this.getSymbol(jsxNamespace.Exports, JsxNames.ElementType, SymbolFlags.Type);
        }

        return null;
    }

    // e.g. "props" for React.d.ts,
    // or InternalSymbolNameMissing if ElementAttributesProperty doesn't exist (which means all
    //
    //	non-intrinsic elements' attributes type is 'any'),
    //
    // or "" if it has 0 properties (which means every
    //
    //	non-intrinsic elements' attributes type is the element instance type)
    // Upstream tsc/internal/checker/jsx.go:1073:1.
    private string getJsxElementPropertiesName(Symbol jsxNamespace)
    {
        return this.getNameFromJsxElementAttributesContainer(JsxNames.ElementAttributesPropertyNameContainer, jsxNamespace);
    }

    // Upstream tsc/internal/checker/jsx.go:1077:1.
    private string getJsxElementChildrenPropertyName(Symbol jsxNamespace)
    {
        if (((this.compilerOptions.Jsx == JsxEmit.ReactJSX) || (this.compilerOptions.Jsx == JsxEmit.ReactJSXDev)))
        {
            return "children";
        }

        return this.getNameFromJsxElementAttributesContainer(JsxNames.ElementChildrenAttributeNameContainer, jsxNamespace);
    }

    // Look into JSX namespace and then look for container with matching name as nameOfAttribPropContainer.
    // Get a single property from that container if existed. Report an error if there are more than one property.
    //
    // @param nameOfAttribPropContainer a string of value JsxNames.ElementAttributesPropertyNameContainer or JsxNames.ElementChildrenAttributeNameContainer
    //
    //	if other string is given or the container doesn't exist, return undefined.
    // Upstream tsc/internal/checker/jsx.go:1091:1.
    private string getNameFromJsxElementAttributesContainer(string nameOfAttribPropContainer, Symbol jsxNamespace)
    {
        if ((jsxNamespace != null))
        {
            var jsxElementAttribPropInterfaceSym = this.getSymbol(jsxNamespace.Exports, nameOfAttribPropContainer, SymbolFlags.Type);
            if ((jsxElementAttribPropInterfaceSym != null))
            {
                var jsxElementAttribPropInterfaceType = this.getDeclaredTypeOfSymbol(jsxElementAttribPropInterfaceSym);
                var propertiesOfJsxElementAttribPropInterface = this.getPropertiesOfType(jsxElementAttribPropInterfaceType);
                if ((GoRuntime.Len(propertiesOfJsxElementAttribPropInterface) == 0))
                {
                    return "";
                }

                if ((GoRuntime.Len(propertiesOfJsxElementAttribPropInterface) == 1))
                {
                    return propertiesOfJsxElementAttribPropInterface[0].Name;
                }

                if (((GoRuntime.Len(propertiesOfJsxElementAttribPropInterface) > 1) && (GoRuntime.Len(jsxElementAttribPropInterfaceSym.Declarations) != 0)))
                {
                    this.error(jsxElementAttribPropInterfaceSym.Declarations[0], Diagnostics.The_global_type_JSX_0_may_not_have_more_than_one_property, nameOfAttribPropContainer);
                }
            }
        }

        return Ast.InternalSymbolNameMissing;
    }

    // Upstream tsc/internal/checker/jsx.go:1114:1.
    private Type getStaticTypeOfReferencedJsxConstructor(Node context)
    {
        if (Ast.IsJsxOpeningFragment(context))
        {
            return this.getJSXFragmentType(context);
        }

        if (isJsxIntrinsicTagName(context.TagName()))
        {
            var result = this.getIntrinsicAttributesTypeFromJsxOpeningLikeElement(context);
            var fakeSignature = this.createSignatureForJSXIntrinsic(context, result);
            return this.getOrCreateTypeFromSignature(fakeSignature);
        }

        var tagType = this.checkExpressionCached(context.TagName());
        if (((tagType.flags & TypeFlags.StringLiteral) != 0))
        {
            var result2 = this.getIntrinsicAttributesTypeFromStringLiteralType(tagType, context);
            if ((result2 == null))
            {
                return this.errorType;
            }

            var fakeSignature2 = this.createSignatureForJSXIntrinsic(context, result2);
            return this.getOrCreateTypeFromSignature(fakeSignature2);
        }

        return tagType;
    }

    // Upstream tsc/internal/checker/jsx.go:1135:1.
    private Type getIntrinsicAttributesTypeFromStringLiteralType(Type t, Node location)
    {
        var intrinsicElementsType = this.getJsxType(JsxNames.IntrinsicElements, location);
        if (!this.isErrorType(intrinsicElementsType))
        {
            var stringLiteralTypeName = getStringLiteralValue(t);
            var intrinsicProp = this.getPropertyOfType(intrinsicElementsType, stringLiteralTypeName);
            if ((intrinsicProp != null))
            {
                return this.getTypeOfSymbol(intrinsicProp);
            }

            var indexSignatureType = this.getIndexTypeOfType(intrinsicElementsType, this.stringType);
            if ((indexSignatureType != null))
            {
                return indexSignatureType;
            }

            return null;
        }

        return this.anyType;
    }

    // Upstream tsc/internal/checker/jsx.go:1157:1.
    private JsxReferenceKind getJsxReferenceKind(Node node)
    {
        if (isJsxIntrinsicTagName(node.TagName()))
        {
            return JsxReferenceKind.Mixed;
        }

        var tagType = this.getApparentType(this.checkExpression(node.TagName()));
        if ((GoRuntime.Len(this.getSignaturesOfType(tagType, SignatureKind.Construct)) != 0))
        {
            return JsxReferenceKind.Component;
        }

        if ((GoRuntime.Len(this.getSignaturesOfType(tagType, SignatureKind.Call)) != 0))
        {
            return JsxReferenceKind.Function;
        }

        return JsxReferenceKind.Mixed;
    }

    // Upstream tsc/internal/checker/jsx.go:1171:1.
    private Signature createSignatureForJSXIntrinsic(Node node, Type result)
    {
        var elementType = this.errorType;
        {
            var @namespace = this.getJsxNamespaceAt(node);
            if ((@namespace != null))
            {
                {
                    var typeSymbol = this.getSymbol(this.getExportsOfSymbol(@namespace), JsxNames.Element, SymbolFlags.Type);
                    if ((typeSymbol != null))
                    {
                        elementType = this.getDeclaredTypeOfSymbol(typeSymbol);
                    }
                }
            }
        }

        var parameterSymbol = this.newSymbol(SymbolFlags.FunctionScopedVariable, "props");
        this.valueSymbolLinks.Get(parameterSymbol).resolvedType = result;
        return this.newSignature(SignatureFlags.None, null, null, null, new List<Symbol> { parameterSymbol }, elementType, null, 1);
    }

    // Get attributes type of the given intrinsic opening-like Jsx element by resolving the tag name.
    // The function is intended to be called from a function which has checked that the opening element is an intrinsic element.
    // @param node an intrinsic JSX opening-like element
    // Upstream tsc/internal/checker/jsx.go:1188:1.
    private Type getIntrinsicAttributesTypeFromJsxOpeningLikeElement(Node node)
    {
        TypeScript.Scanner.Debug.Assert(isJsxIntrinsicTagName(node.TagName()));
        var links = this.jsxElementLinks.Get(node);
        if ((links.resolvedJsxElementAttributesType != null))
        {
            return links.resolvedJsxElementAttributesType;
        }

        var symbol = this.getIntrinsicTagSymbol(node);
        if (((links.jsxFlags & JsxFlags.IntrinsicNamedElement) != 0))
        {
            links.resolvedJsxElementAttributesType = Core.OrElse(this.getTypeOfSymbol(symbol), this.errorType);
            return links.resolvedJsxElementAttributesType;
        }

        if (((links.jsxFlags & JsxFlags.IntrinsicIndexedElement) != 0))
        {
            var indexInfo = this.getApplicableIndexInfoForName(this.getJsxType(JsxNames.IntrinsicElements, node), node.TagName().TextValue());
            if ((indexInfo != null))
            {
                links.resolvedJsxElementAttributesType = indexInfo.valueType;
                return links.resolvedJsxElementAttributesType;
            }
        }

        links.resolvedJsxElementAttributesType = this.errorType;
        return links.resolvedJsxElementAttributesType;
    }

    // Looks up an intrinsic tag name and returns a symbol that either points to an intrinsic
    // property (in which case nodeLinks.jsxFlags will be IntrinsicNamedElement) or an intrinsic
    // string index signature (in which case nodeLinks.jsxFlags will be IntrinsicIndexedElement).
    // May also return unknownSymbol if both of these lookups fail.
    // Upstream tsc/internal/checker/jsx.go:1214:1.
    private Symbol getIntrinsicTagSymbol(Node node)
    {
        var links = this.symbolNodeLinks.Get(node);
        if ((links.resolvedSymbol != null))
        {
            return links.resolvedSymbol;
        }

        var intrinsicElementsType = this.getJsxType(JsxNames.IntrinsicElements, node);
        if (!this.isErrorType(intrinsicElementsType))
        {
            var tagName = node.TagName();
            if ((!Ast.IsIdentifier(tagName) && !Ast.IsJsxNamespacedName(tagName)))
            {
                throw new InvalidOperationException("Invalid tag name");
            }

            var propName = tagName.TextValue();
            var intrinsicProp = this.getPropertyOfType(intrinsicElementsType, propName);
            if ((intrinsicProp != null))
            {
                this.jsxElementLinks.Get(node).jsxFlags |= JsxFlags.IntrinsicNamedElement;
                links.resolvedSymbol = intrinsicProp;
                return links.resolvedSymbol;
            }

            var indexSymbol = this.getApplicableIndexSymbol(intrinsicElementsType, this.getStringLiteralType(propName));
            if ((indexSymbol != null))
            {
                this.jsxElementLinks.Get(node).jsxFlags |= JsxFlags.IntrinsicIndexedElement;
                links.resolvedSymbol = indexSymbol;
                return links.resolvedSymbol;
            }

            if ((this.getTypeOfPropertyOrIndexSignatureOfType(intrinsicElementsType, propName) != null))
            {
                this.jsxElementLinks.Get(node).jsxFlags |= JsxFlags.IntrinsicIndexedElement;
                links.resolvedSymbol = intrinsicElementsType.symbol;
                return links.resolvedSymbol;
            }

            this.error(node, Diagnostics.Property_0_does_not_exist_on_type_1, tagName.Text(), ("JSX." + JsxNames.IntrinsicElements));
            links.resolvedSymbol = this.unknownSymbol;
            return links.resolvedSymbol;
        }

        if (this.noImplicitAny)
        {
            this.error(node, Diagnostics.JSX_element_implicitly_has_type_any_because_no_interface_JSX_0_exists, JsxNames.IntrinsicElements);
        }

        links.resolvedSymbol = this.unknownSymbol;
        return links.resolvedSymbol;
    }

    // Upstream tsc/internal/checker/jsx.go:1257:1.
    private Type getJsxStatelessElementTypeAt(Node location)
    {
        var jsxElementType = this.getJsxElementTypeAt(location);
        if ((jsxElementType == null))
        {
            return null;
        }

        return this.getUnionType(new List<Type> { jsxElementType, this.nullType });
    }

    // Upstream tsc/internal/checker/jsx.go:1265:1.
    private Type getJsxElementClassTypeAt(Node location)
    {
        var t = this.getJsxType(JsxNames.ElementClass, location);
        if (this.isErrorType(t))
        {
            return null;
        }

        return t;
    }

    // Upstream tsc/internal/checker/jsx.go:1273:1.
    private Type getJsxElementTypeAt(Node location)
    {
        return this.getJsxType(JsxNames.Element, location);
    }

    // Upstream tsc/internal/checker/jsx.go:1277:1.
    private Type getJsxElementTypeTypeAt(Node location)
    {
        var ns = this.getJsxNamespaceAt(location);
        if ((ns == null))
        {
            return null;
        }

        var sym = this.getJsxElementTypeSymbol(ns);
        if ((sym == null))
        {
            return null;
        }

        var t = this.instantiateAliasOrInterfaceWithDefaults(sym, null, Ast.IsInJSFile(location));
        if (((t == null) || this.isErrorType(t)))
        {
            return null;
        }

        return t;
    }

    // Upstream tsc/internal/checker/jsx.go:1293:1.
    private Type getJsxType(string name, Node location)
    {
        {
            var @namespace = this.getJsxNamespaceAt(location);
            if ((@namespace != null))
            {
                {
                    var exports = this.getExportsOfSymbol(@namespace);
                    if ((exports != null))
                    {
                        {
                            var typeSymbol = this.getSymbol(exports, name, SymbolFlags.Type);
                            if ((typeSymbol != null))
                            {
                                return this.getDeclaredTypeOfSymbol(typeSymbol);
                            }
                        }
                    }
                }
            }
        }

        return this.errorType;
    }

    // Upstream tsc/internal/checker/jsx.go:1304:1.
    private Symbol getJsxNamespaceAt(Node location)
    {
        JsxElementLinks links = default;
        if ((location != null))
        {
            links = this.jsxElementLinks.Get(location);
        }

        if ((((links != null) && (links.jsxNamespace != null)) && (links.jsxNamespace != this.unknownSymbol)))
        {
            return links.jsxNamespace;
        }

        if (((links == null) || (links.jsxNamespace != this.unknownSymbol)))
        {
            var resolvedNamespace = this.getJsxNamespaceContainerForImplicitImport(location);
            if (((resolvedNamespace == null) || (resolvedNamespace == this.unknownSymbol)))
            {
                var namespaceName = this.getJsxNamespace(location);
                resolvedNamespace = this.resolveName(location, namespaceName, SymbolFlags.Namespace, null, false, false);
            }

            if ((resolvedNamespace != null))
            {
                var candidate = this.resolveSymbol(this.getSymbol(this.getExportsOfSymbol(this.resolveSymbol(resolvedNamespace)), JsxNames.JSX, SymbolFlags.Namespace));
                if (((candidate != null) && (candidate != this.unknownSymbol)))
                {
                    if ((links != null))
                    {
                        links.jsxNamespace = candidate;
                    }

                    return candidate;
                }
            }

            if ((links != null))
            {
                links.jsxNamespace = this.unknownSymbol;
            }
        }

        var s = this.resolveSymbol(this.getGlobalSymbol(JsxNames.JSX, SymbolFlags.Namespace, null));
        if ((s == this.unknownSymbol))
        {
            return null;
        }

        return s;
    }

    // Upstream tsc/internal/checker/jsx.go:1339:1.
    private string getJsxNamespace(Node location)
    {
        if ((location != null))
        {
            var file = Ast.GetSourceFileOfNode(location);
            if ((file != null))
            {
                var links = this.sourceFileLinks.Get(file.AsNode());
                if (Ast.IsJsxOpeningFragment(location))
                {
                    if ((links.localJsxFragmentNamespace != ""))
                    {
                        return links.localJsxFragmentNamespace;
                    }

                    var jsxFragmentPragma = Ast.GetPragmaFromSourceFile(file, "jsxfrag");
                    if ((jsxFragmentPragma != null))
                    {
                        links.localJsxFragmentFactory = this.parseIsolatedEntityName(jsxFragmentPragma.Args["factory"].Value);
                        if ((links.localJsxFragmentFactory != null))
                        {
                            links.localJsxFragmentNamespace = Ast.GetFirstIdentifier(links.localJsxFragmentFactory).Text();
                            return links.localJsxFragmentNamespace;
                        }
                    }

                    var entity = this.getJsxFragmentFactoryEntity(location);
                    if ((entity != null))
                    {
                        links.localJsxFragmentFactory = entity;
                        links.localJsxFragmentNamespace = Ast.GetFirstIdentifier(entity).Text();
                        return links.localJsxFragmentNamespace;
                    }
                }
                else
                {
                    var localJsxNamespace = this.getLocalJsxNamespace(file);
                    if ((localJsxNamespace != ""))
                    {
                        links.localJsxNamespace = localJsxNamespace;
                        return links.localJsxNamespace;
                    }
                }
            }
        }

        if ((this._jsxNamespace == ""))
        {
            this._jsxNamespace = "React";
            if ((this.compilerOptions.JsxFactory != ""))
            {
                this._jsxFactoryEntity = this.parseIsolatedEntityName(this.compilerOptions.JsxFactory);
                if ((this._jsxFactoryEntity != null))
                {
                    this._jsxNamespace = Ast.GetFirstIdentifier(this._jsxFactoryEntity).Text();
                }
            }
            else
            {
                if ((this.compilerOptions.ReactNamespace != ""))
                {
                    this._jsxNamespace = this.compilerOptions.ReactNamespace;
                }
            }
        }

        if ((this._jsxFactoryEntity == null))
        {
            this._jsxFactoryEntity = this.factory.NewQualifiedName(this.factory.NewIdentifier(this._jsxNamespace), this.factory.NewIdentifier("createElement"));
        }

        return this._jsxNamespace;
    }

    // Upstream tsc/internal/checker/jsx.go:1388:1.
    private string getLocalJsxNamespace(SourceFile file)
    {
        var links = this.sourceFileLinks.Get(file.AsNode());
        if ((links.localJsxNamespace != ""))
        {
            return links.localJsxNamespace;
        }

        var jsxPragma = Ast.GetPragmaFromSourceFile(file, "jsx");
        if ((jsxPragma != null))
        {
            links.localJsxFactory = this.parseIsolatedEntityName(jsxPragma.Args["factory"].Value);
            if ((links.localJsxFactory != null))
            {
                links.localJsxNamespace = Ast.GetFirstIdentifier(links.localJsxFactory).Text();
                return links.localJsxNamespace;
            }
        }

        return "";
    }

    // Upstream tsc/internal/checker/jsx.go:1404:1.
    private Node getJsxFactoryEntity(Node location)
    {
        if ((location != null))
        {
            this.getJsxNamespace(location);
            {
                var localJsxFactory = this.sourceFileLinks.Get(Ast.GetSourceFileOfNode(location).AsNode()).localJsxFactory;
                if ((localJsxFactory != null))
                {
                    return localJsxFactory;
                }
            }
        }

        return this._jsxFactoryEntity;
    }

    // Upstream tsc/internal/checker/jsx.go:1414:1.
    private Node getJsxFragmentFactoryEntity(Node location)
    {
        if ((location != null))
        {
            var file = Ast.GetSourceFileOfNode(location);
            if ((file != null))
            {
                var links = this.sourceFileLinks.Get(file.AsNode());
                if ((links.localJsxFragmentFactory != null))
                {
                    return links.localJsxFragmentFactory;
                }

                var jsxFragPragma = Ast.GetPragmaFromSourceFile(file, "jsxfrag");
                if ((jsxFragPragma != null))
                {
                    links.localJsxFragmentFactory = this.parseIsolatedEntityName(jsxFragPragma.Args["factory"].Value);
                    return links.localJsxFragmentFactory;
                }
            }
        }

        if ((this.compilerOptions.JsxFragmentFactory != ""))
        {
            return this.parseIsolatedEntityName(this.compilerOptions.JsxFragmentFactory);
        }

        return null;
    }

    // Upstream tsc/internal/checker/jsx.go:1435:1.
    private Node parseIsolatedEntityName(string name)
    {
        var result = Parser.ParseIsolatedEntityName(GoString.Bytes(name));
        if ((result != null))
        {
            markAsSynthetic(result);
        }

        return result;
    }

    // Upstream tsc/internal/checker/jsx.go:1443:1.
    private static bool markAsSynthetic(Node node)
    {
        node.Loc = Core.NewTextRange(-1, -1);
        node.ForEachChild(markAsSynthetic);
        return false;
    }

    // Upstream tsc/internal/checker/jsx.go:1449:1.
    private Symbol getJsxNamespaceContainerForImplicitImport(Node location)
    {
        var file = Ast.GetSourceFileOfNode(location);
        var links = this.jsxElementLinks.Get(file.AsNode());
        if ((links.jsxImplicitImportContainer != null))
        {
            return Core.IfElse((links.jsxImplicitImportContainer == this.unknownSymbol), null, links.jsxImplicitImportContainer);
        }

        var canonicalErrorTag = links.firstJSXTagInFile;
        if ((canonicalErrorTag == null))
        {
            Visitor visit = default;
            visit = (node) =>
            {
                if ((Ast.IsJsxElement(node) || Ast.IsJsxSelfClosingElement(node)))
                {
                    links.firstJSXTagInFile = node;
                    return true;
                }

                if (Ast.IsJsxFragment(node))
                {
                    links.firstJSXTagInFile = node.AsJsxFragment().OpeningFragment;
                    return true;
                }

                return node.ForEachChild(visit);
            };
            file.ForEachChild(visit);
            canonicalErrorTag = links.firstJSXTagInFile;
        }

        var tuple1 = this.getJSXRuntimeImportSpecifier(file);
        var moduleReference = tuple1.Item1;
        var specifier = tuple1.Item2;
        if ((moduleReference == ""))
        {
            return null;
        }

        var errorMessage = Diagnostics.This_JSX_tag_requires_the_module_path_0_to_exist_but_none_could_be_found_Make_sure_you_have_types_for_the_appropriate_package_installed;
        var mod = this.resolveExternalModule(Core.OrElse(specifier, canonicalErrorTag), moduleReference, errorMessage, canonicalErrorTag, false, null);
        Symbol result = default;
        if (((mod != null) && (mod != this.unknownSymbol)))
        {
            result = this.getMergedSymbol(this.resolveSymbol(mod));
        }

        links.jsxImplicitImportContainer = Core.OrElse(result, this.unknownSymbol);
        return result;
    }

    // Upstream tsc/internal/checker/jsx.go:1486:1.
    private (string, Node) getJSXRuntimeImportSpecifier(SourceFile file)
    {
        return this.program.GetJSXRuntimeImportSpecifier(PathKey.FromGoString(file.PathKey()));
    }
}
