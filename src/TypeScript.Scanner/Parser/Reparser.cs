// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of upstream/TypeScript/tsc/internal/parser/reparser.go, upstream fed0bf24149f.
#nullable disable
namespace TypeScript.Scanner;
public partial class Parser
{
    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:13:1.
    private void finishReparsedNode(Node node, Node locationNode)
    {
        node.Flags = (this.contextFlags | NodeFlags.Reparsed);
        node.Loc = locationNode.Loc;
        this.overrideParentInImmediateChildren(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:19:1.
    private void finishMutatedNode(Node node)
    {
        this.overrideParentInImmediateChildren(node);
    }

    // Deep-clone the given node and add the clone to the reparsed clone list. The list is used by ast.GetReparsedNodeForNode
    // to locate reparsed clones of JSDoc nodes. Since the binder attaches symbols to reparsed nodes and not to JSDoc nodes, we
    // need the mapping when obtaining symbols and types from JSDoc nodes.
    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:26:1.
    private Node addDeepCloneReparse(Node node)
    {
        var clone = this.factory.DeepCloneReparse(node);
        if ((clone != null))
        {
            this.reparsedClones = GoRuntime.Append(this.reparsedClones, clone);
        }

        return clone;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:34:1.
    private Node addTransformedReparse(Node newNode, Node old)
    {
        this.finishReparsedNode(newNode, old);
        newNode.Flags |= NodeFlags.ReparserTransformedLiteral;
        this.reparsedClones = GoRuntime.Append(this.reparsedClones, newNode);
        return newNode;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:41:1.
    private Node checkNonIdentifierName(Node name)
    {
        if ((name == null))
        {
            return null;
        }

        if ((Ast.IsIdentifier(name) && !Scanner.IsValidIdentifierByteString(name.AsIdentifier().Text)))
        {
            var errLoc = name.Loc;
            if ((errLoc.Len() == 0))
            {
                errLoc = Core.NewTextRange((name.Loc.Pos() - 1), name.Loc.Pos());
            }

            this.parseErrorAtRange(errLoc, Diagnostics.Identifier_expected);
        }

        return name;
    }

    // Hosted tags find a host and add their children to the correct location under the host.
    // Unhosted tags add synthetic nodes to the reparse list.
    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:58:1.
    private void reparseTags(Node parent, Slice<Node> jsDoc)
    {
        {
            foreach (var j in GoRuntime.RangeValues(jsDoc))
            {
                var isLast = (j == jsDoc[(GoRuntime.Len(jsDoc) - 1)]);
                var tags = j.AsJSDoc().Tags;
                if ((tags == null))
                {
                    continue;
                }

                {
                    foreach (var range2 in GoRuntime.Range(tags.Nodes))
                    {
                        var _2 = range2.Key;
                        var tag = range2.Value;
                        this.reparseUnhosted(tag, parent, j);
                        if (isLast)
                        {
                            this.reparseHosted(tag, parent, j);
                        }
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:74:1.
    private void reparseUnhosted(Node tag, Node parent, Node jsDoc)
    {
        switch (tag.Kind)
        {
            case Kind.JSDocTypedefTag:
            {
                var typeExpression = tag.TypeExpression();
                if ((typeExpression == null))
                {
                    break;
                }

                var fullName = tag.Name();
                var isNamespace = ((fullName != null) && Ast.IsModuleDeclaration(fullName));
                ModifierList modifiers = default;
                if (isNamespace)
                {
                    modifiers = this.createExportModifier(tag);
                }

                var typeAlias = this.factory.NewJSTypeAliasDeclaration(modifiers, this.addDeepCloneReparse(this.checkNonIdentifierName(this.getInnermostNameOfJSDocNamespace(fullName))), null, null);
                typeAlias.AsTypeAliasDeclaration().TypeParameters = this.gatherTypeParameters(jsDoc, true);
                Node t = default;
                switch (typeExpression.Kind)
                {
                    case Kind.JSDocTypeExpression:
                    {
                        t = this.addDeepCloneReparse(typeExpression.Type());
                        break;
                    }

                    case Kind.JSDocTypeLiteral:
                    {
                        t = this.reparseJSDocTypeLiteral(typeExpression);
                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException(("typedef tag type expression should be a name reference or a type expression" + typeExpression.Kind.String()));
                    }
                }

                typeAlias.AsTypeAliasDeclaration().Type = t;
                this.finishReparsedNode(typeAlias, tag);
                this.jsdocInfos = GoRuntime.Append(this.jsdocInfos, new JSDocInfo { parent = typeAlias, jsDocs = this.nodeSliceArena.NewSlice1Header(jsDoc) });
                typeAlias.Flags |= NodeFlags.HasJSDoc;
                var result = this.wrapInJSDocNamespace(fullName, typeAlias, false);
                this.reparseList = GoRuntime.Append(this.reparseList, result);
                break;
            }

            case Kind.JSDocCallbackTag:
            {
                var typeExpression2 = tag.TypeExpression();
                if ((typeExpression2 == null))
                {
                    break;
                }

                var fullName2 = tag.Name();
                var isNamespace2 = ((fullName2 != null) && Ast.IsModuleDeclaration(fullName2));
                ModifierList modifiers2 = default;
                if (isNamespace2)
                {
                    modifiers2 = this.createExportModifier(tag);
                }

                var functionType = this.reparseJSDocSignature(typeExpression2, tag, jsDoc, tag, null);
                var typeAlias2 = this.factory.NewJSTypeAliasDeclaration(modifiers2, this.addDeepCloneReparse(this.getInnermostNameOfJSDocNamespace(fullName2)), null, functionType);
                typeAlias2.AsTypeAliasDeclaration().TypeParameters = this.gatherTypeParameters(jsDoc, true);
                this.finishReparsedNode(typeAlias2, tag);
                this.jsdocInfos = GoRuntime.Append(this.jsdocInfos, new JSDocInfo { parent = typeAlias2, jsDocs = this.nodeSliceArena.NewSlice1Header(jsDoc) });
                typeAlias2.Flags |= NodeFlags.HasJSDoc;
                var result2 = this.wrapInJSDocNamespace(fullName2, typeAlias2, false);
                this.reparseList = GoRuntime.Append(this.reparseList, result2);
                break;
            }

            case Kind.JSDocImportTag:
            {
                var importTag = tag.AsJSDocImportTag();
                if ((importTag.ImportClause == null))
                {
                    break;
                }

                var importClause = this.addDeepCloneReparse(importTag.ImportClause);
                importClause.AsImportClause().PhaseModifier = Kind.TypeKeyword;
                var importDeclaration = this.factory.NewJSImportDeclaration(this.factory.DeepCloneReparseModifiers(importTag.Modifiers()), importClause, this.addDeepCloneReparse(importTag.ModuleSpecifier), this.addDeepCloneReparse(importTag.Attributes));
                this.finishReparsedNode(importDeclaration, tag);
                this.reparseList = GoRuntime.Append(this.reparseList, importDeclaration);
                break;
            }

            case Kind.JSDocOverloadTag:
            {
                if (((((Ast.IsFunctionDeclaration(parent) || Ast.IsMethodDeclaration(parent)) || Ast.IsConstructorDeclaration(parent))) && ((this.parsingContexts & ((1 << (int)(ParsingContext.ObjectLiteralMembers)))) == 0)))
                {
                    this.reparseList = GoRuntime.Append(this.reparseList, this.reparseJSDocSignature(tag.AsJSDocOverloadTag().TypeExpression, parent, jsDoc, tag, parent.Modifiers()));
                }

                break;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:146:1.
    private Node reparseJSDocSignature(Node jsSignature, Node fun, Node jsDoc, Node tag, ModifierList modifiers)
    {
        Node signature = default;
        var clonedModifiers = this.factory.DeepCloneReparseModifiers(modifiers);
        switch (fun.Kind)
        {
            case Kind.FunctionDeclaration:
            {
                signature = this.factory.NewFunctionDeclaration(clonedModifiers, null, this.factory.DeepCloneReparse(this.checkNonIdentifierName(fun.Name())), null, null, null, null, null);
                break;
            }

            case Kind.MethodDeclaration:
            {
                signature = this.factory.NewMethodDeclaration(clonedModifiers, null, this.factory.DeepCloneReparse(this.checkNonIdentifierName(fun.Name())), null, null, null, null, null, null);
                break;
            }

            case Kind.Constructor:
            {
                signature = this.factory.NewConstructorDeclaration(clonedModifiers, null, null, null, null, null);
                break;
            }

            case Kind.JSDocCallbackTag:
            {
                signature = this.factory.NewFunctionTypeNode(null, null, this.factory.NewKeywordTypeNode(Kind.AnyKeyword));
                break;
            }

            default:
            {
                throw new InvalidOperationException(("Unexpected kind " + fun.Kind.String()));
            }
        }

        if ((tag.Kind != Kind.JSDocCallbackTag))
        {
            signature.FunctionLikeData().TypeParameters = this.gatherTypeParameters(jsDoc, false);
        }

        var parameters = this.nodeSliceArena.NewSliceHeader(0);
        {
            foreach (var range3 in GoRuntime.Range(jsSignature.Parameters()))
            {
                var pi = range3.Key;
                var param = range3.Value;
                Node parameter = default;
                if ((param.Kind == Kind.JSDocThisTag))
                {
                    var thisTag = param.AsJSDocThisTag();
                    var thisIdent = this.factory.NewIdentifier("this");
                    thisIdent.Loc = thisTag.Loc;
                    thisIdent.Flags = (this.contextFlags | NodeFlags.Reparsed);
                    parameter = this.factory.NewParameterDeclaration(null, null, thisIdent, null, null, null);
                    if ((thisTag.TypeExpression != null))
                    {
                        parameter.AsParameterDeclaration().Type = this.addDeepCloneReparse(thisTag.TypeExpression.Type());
                    }
                }
                else
                {
                    if (((param.Kind == Kind.JSDocParameterTag) || (param.Kind == Kind.JSDocPropertyTag)))
                    {
                        var jsparam = param.AsJSDocParameterOrPropertyTag();
                        if (Ast.IsQualifiedName(jsparam.Name()))
                        {
                            continue;
                        }

                        Node dotDotDotToken = default;
                        Node paramType = default;
                        if ((jsparam.TypeExpression != null))
                        {
                            if ((jsparam.TypeExpression.Type().Kind == Kind.JSDocVariadicType))
                            {
                                dotDotDotToken = this.factory.NewToken(Kind.DotDotDotToken);
                                dotDotDotToken.Loc = jsparam.Loc;
                                dotDotDotToken.Flags = (this.contextFlags | NodeFlags.Reparsed);
                                var variadicType = jsparam.TypeExpression.Type().AsJSDocVariadicType();
                                paramType = this.reparseJSDocTypeLiteral(variadicType.Type);
                            }
                            else
                            {
                                paramType = this.reparseJSDocTypeLiteral(jsparam.TypeExpression.Type());
                            }
                        }

                        var name = jsparam.Name();
                        if ((Ast.IsIdentifier(name) && !Scanner.IsValidIdentifierByteString(name.AsIdentifier().Text)))
                        {
                            var result = new GoBuilder
                            {
                            };
                            {
                                foreach (var range4 in GoRuntime.Range(name.AsIdentifier().Text))
                                {
                                    var i = range4.Key;
                                    var ch = range4.Value;
                                    if ((i == 0))
                                    {
                                        if (!Scanner.IsIdentifierStart(ch))
                                        {
                                            result.WriteRune('_');
                                        }
                                        else
                                        {
                                            result.WriteRune(ch);
                                        }

                                        continue;
                                    }
                                    else
                                    {
                                        if (!Scanner.IsIdentifierPart(ch))
                                        {
                                            result.WriteRune('_');
                                        }
                                        else
                                        {
                                            result.WriteRune(ch);
                                        }
                                    }
                                }
                            }

                            if ((result.Len() == 0))
                            {
                                result.WriteRune('_');
                                result.WriteString(Strconv.Itoa(pi));
                            }

                            name = this.addTransformedReparse(this.factory.NewIdentifier(result.String()), name);
                        }
                        else
                        {
                            name = this.addDeepCloneReparse(name);
                        }

                        parameter = this.factory.NewParameterDeclaration(null, dotDotDotToken, name, this.makeQuestionIfOptional(jsparam), paramType, null);
                    }
                }

                this.finishReparsedNode(parameter, param);
                parameters = GoRuntime.Append(parameters, parameter);
                this.reparseJSDocComment(parameter, param);
            }
        }

        signature.FunctionLikeData().Parameters = this.newNodeList(jsSignature.AsJSDocSignature().Parameters.Loc, parameters);
        if (((jsSignature.Type() != null) && (jsSignature.Type().TypeExpression() != null)))
        {
            signature.FunctionLikeData().Type = this.addDeepCloneReparse(jsSignature.Type().TypeExpression().Type());
        }

        var loc = jsSignature;
        if ((tag.Kind == Kind.JSDocOverloadTag))
        {
            loc = tag.TagName();
        }

        this.finishReparsedNode(signature, loc);
        return signature;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:244:1.
    private Node reparseJSDocTypeLiteral(Node t)
    {
        if ((t == null))
        {
            return null;
        }

        if ((t.Kind == Kind.JSDocTypeLiteral))
        {
            var jstypeliteral = t.AsJSDocTypeLiteral();
            var isArrayType = jstypeliteral.IsArrayType;
            var properties = this.nodeSliceArena.NewSliceHeader(0);
            {
                foreach (var prop in GoRuntime.RangeValues(jstypeliteral.JSDocPropertyTags))
                {
                    if (((prop.Kind != Kind.JSDocPropertyTag) && (prop.Kind != Kind.JSDocParameterTag)))
                    {
                        continue;
                    }

                    var jsprop = prop.AsJSDocParameterOrPropertyTag();
                    var name = prop.Name();
                    if ((name.Kind == Kind.QualifiedName))
                    {
                        name = name.AsQualifiedName().Right;
                    }

                    if ((Ast.IsIdentifier(name) && !Scanner.IsValidIdentifierByteString(name.AsIdentifier().Text)))
                    {
                        name = this.addTransformedReparse(this.factory.NewStringLiteral(name.AsIdentifier().Text, TokenFlags.None), name);
                    }
                    else
                    {
                        name = this.addDeepCloneReparse(name);
                    }

                    var property = this.factory.NewPropertySignatureDeclaration(null, name, this.makeQuestionIfOptional(jsprop), null, null);
                    if ((jsprop.TypeExpression != null))
                    {
                        property.AsPropertySignatureDeclaration().Type = this.reparseJSDocTypeLiteral(jsprop.TypeExpression.Type());
                    }

                    this.finishReparsedNode(property, prop);
                    properties = GoRuntime.Append(properties, property);
                    this.reparseJSDocComment(property, prop);
                }
            }

            t = this.factory.NewTypeLiteralNode(this.newNodeList(jstypeliteral.Loc, properties));
            if (isArrayType)
            {
                this.finishReparsedNode(t, jstypeliteral.AsNode());
                t = this.factory.NewArrayTypeNode(t);
            }

            this.finishReparsedNode(t, jstypeliteral.AsNode());
            return t;
        }

        return this.addDeepCloneReparse(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:285:1.
    private void reparseJSDocComment(Node node, Node tag)
    {
        {
            var comment = tag.CommentList();
            if ((comment != null))
            {
                var newComment = this.factory.NewNodeList(Core.MapSlice(comment.Nodes, this.factory.DeepCloneReparse));
                newComment.Loc = comment.Loc;
                var propJSDoc = this.factory.NewJSDoc(newComment, null);
                this.finishReparsedNode(propJSDoc, tag);
                propJSDoc.Parent = node;
                this.jsdocInfos = GoRuntime.Append(this.jsdocInfos, new JSDocInfo { parent = node, jsDocs = this.nodeSliceArena.NewSlice1Header(propJSDoc) });
                node.Flags |= NodeFlags.HasJSDoc;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:297:1.
    private NodeList gatherTypeParameters(Node j, bool typedefOrCallback)
    {
        Slice<Node> typeParameters = default;
        var pos = -1;
        var endPos = -1;
        var firstTemplate = true;
        {
            foreach (var tag in GoRuntime.RangeValues(j.AsJSDoc().Tags.Nodes))
            {
                if ((!typedefOrCallback && ((Ast.IsJSDocTypedefTag(tag) || Ast.IsJSDocCallbackTag(tag)))))
                {
                    return null;
                }

                if (!Ast.IsJSDocTemplateTag(tag))
                {
                    continue;
                }

                if (firstTemplate)
                {
                    pos = tag.Pos();
                    firstTemplate = false;
                }

                endPos = tag.End();
                var constraint = tag.AsJSDocTemplateTag().Constraint;
                var firstTypeParameter = true;
                {
                    foreach (var range7 in GoRuntime.Range(tag.TypeParameters()))
                    {
                        var _2 = range7.Key;
                        var tp = range7.Value;
                        Node reparse = default;
                        if (((constraint != null) && firstTypeParameter))
                        {
                            reparse = this.factory.NewTypeParameterDeclaration(this.factory.DeepCloneReparseModifiers(tp.Modifiers()), this.addDeepCloneReparse(this.checkNonIdentifierName(tp.Name())), this.addDeepCloneReparse(constraint.Type()), null, this.addDeepCloneReparse(tp.AsTypeParameterDeclaration().DefaultType));
                            this.finishReparsedNode(reparse, tp);
                        }
                        else
                        {
                            reparse = this.addDeepCloneReparse(tp);
                        }

                        if ((typeParameters.IsNil))
                        {
                            typeParameters = this.nodeSliceArena.NewSliceHeader(0);
                        }

                        typeParameters = GoRuntime.Append(typeParameters, reparse);
                        firstTypeParameter = false;
                    }
                }
            }
        }

        if ((GoRuntime.Len(typeParameters) == 0))
        {
            return null;
        }
        else
        {
            return this.newNodeList(Core.NewTextRange(pos, endPos), typeParameters);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:346:1.
    private void reparseHosted(Node tag, Node parent, Node jsDoc)
    {
        switch (tag.Kind)
        {
            case Kind.JSDocTypeTag:
            {
                switch (parent.Kind)
                {
                    case Kind.VariableStatement:
                    {
                        if ((parent.AsVariableStatement().DeclarationList != null))
                        {
                            {
                                foreach (var declaration in GoRuntime.RangeValues(parent.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes))
                                {
                                    if (((declaration.Type() == null) && (tag.TypeExpression() != null)))
                                    {
                                        declaration.AsMutable().SetType(this.addDeepCloneReparse(tag.TypeExpression().Type()));
                                        this.finishMutatedNode(declaration);
                                        return;
                                    }
                                }
                            }
                        }

                        break;
                    }

                    case Kind.VariableDeclaration:
                    case Kind.ExportAssignment:
                    case Kind.PropertyDeclaration:
                    case Kind.PropertyAssignment:
                    case Kind.ShorthandPropertyAssignment:
                    case Kind.GetAccessor:
                    {
                        if (((parent.Type() == null) && (tag.TypeExpression() != null)))
                        {
                            parent.AsMutable().SetType(this.addDeepCloneReparse(tag.TypeExpression().Type()));
                            this.finishMutatedNode(parent);
                            return;
                        }

                        break;
                    }

                    case Kind.Parameter:
                    {
                        if (((parent.Type() == null) && (tag.TypeExpression() != null)))
                        {
                            parent.AsMutable().SetType(this.reparseJSDocTypeLiteral(tag.TypeExpression().Type()));
                            this.finishMutatedNode(parent);
                            return;
                        }

                        break;
                    }

                    case Kind.ExpressionStatement:
                    {
                        if ((parent.Expression().Kind == Kind.BinaryExpression))
                        {
                            var bin = parent.Expression().AsBinaryExpression();
                            {
                                var kind = Ast.GetAssignmentDeclarationKind(bin.AsNode());
                                if (((kind != JSDeclarationKind.None) && (tag.TypeExpression() != null)))
                                {
                                    bin.AsMutable().SetType(this.addDeepCloneReparse(tag.TypeExpression().Type()));
                                    this.finishMutatedNode(bin.AsNode());
                                    return;
                                }
                            }
                        }

                        break;
                    }

                    case Kind.ReturnStatement:
                    case Kind.ParenthesizedExpression:
                    {
                        if (((parent.Expression() != null) && (tag.TypeExpression() != null)))
                        {
                            parent.AsMutable().SetExpression(this.makeNewCast(this.addDeepCloneReparse(tag.TypeExpression().Type()), parent.Expression(), true));
                            this.finishMutatedNode(parent);
                            return;
                        }

                        break;
                    }
                }

                {
                    var fun = getFunctionLikeHost(parent);
                    if ((fun != null))
                    {
                        var noTypedParams = Core.Every(fun.Parameters(), (param) =>
                        {
                            return (param.Type() == null);
                        });
                        if (((((fun.TypeParameterList() == null) && (fun.Type() == null)) && noTypedParams) && (tag.TypeExpression() != null)))
                        {
                            fun.FunctionLikeData().FullSignature = this.addDeepCloneReparse(tag.TypeExpression().Type());
                            this.finishMutatedNode(fun);
                        }
                    }
                }

                break;
            }

            case Kind.JSDocSatisfiesTag:
            {
                switch (parent.Kind)
                {
                    case Kind.VariableStatement:
                    {
                        if ((parent.AsVariableStatement().DeclarationList != null))
                        {
                            {
                                foreach (var range9 in GoRuntime.Range(parent.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes))
                                {
                                    var _2 = range9.Key;
                                    var declaration2 = range9.Value;
                                    if (((declaration2.Initializer() != null) && (tag.TypeExpression() != null)))
                                    {
                                        declaration2.AsMutable().SetInitializer(this.makeNewCast(this.addDeepCloneReparse(tag.TypeExpression().Type()), declaration2.Initializer(), false));
                                        this.finishMutatedNode(declaration2);
                                        break;
                                    }
                                }
                            }
                        }

                        break;
                    }

                    case Kind.VariableDeclaration:
                    case Kind.PropertyDeclaration:
                    case Kind.PropertyAssignment:
                    {
                        if (((parent.Initializer() != null) && (tag.TypeExpression() != null)))
                        {
                            parent.AsMutable().SetInitializer(this.makeNewCast(this.addDeepCloneReparse(tag.TypeExpression().Type()), parent.Initializer(), false));
                            this.finishMutatedNode(parent);
                        }

                        break;
                    }

                    case Kind.ShorthandPropertyAssignment:
                    {
                        var shorthand = parent.AsShorthandPropertyAssignment();
                        if (((shorthand.ObjectAssignmentInitializer != null) && (tag.AsJSDocSatisfiesTag().TypeExpression != null)))
                        {
                            shorthand.ObjectAssignmentInitializer = this.makeNewCast(this.addDeepCloneReparse(tag.AsJSDocSatisfiesTag().TypeExpression.Type()), shorthand.ObjectAssignmentInitializer, false);
                            this.finishMutatedNode(parent);
                        }

                        break;
                    }

                    case Kind.ReturnStatement:
                    case Kind.ParenthesizedExpression:
                    case Kind.ExportAssignment:
                    {
                        if (((parent.Expression() != null) && (tag.TypeExpression() != null)))
                        {
                            parent.AsMutable().SetExpression(this.makeNewCast(this.addDeepCloneReparse(tag.TypeExpression().Type()), parent.Expression(), false));
                            this.finishMutatedNode(parent);
                        }

                        break;
                    }

                    case Kind.ExpressionStatement:
                    {
                        if ((parent.Expression().Kind == Kind.BinaryExpression))
                        {
                            var bin2 = parent.Expression().AsBinaryExpression();
                            {
                                var kind2 = Ast.GetAssignmentDeclarationKind(bin2.AsNode());
                                if (((kind2 != JSDeclarationKind.None) && (tag.TypeExpression() != null)))
                                {
                                    bin2.Right = this.makeNewCast(this.addDeepCloneReparse(tag.TypeExpression().Type()), bin2.Right, false);
                                    this.finishMutatedNode(bin2.AsNode());
                                }
                            }
                        }

                        break;
                    }
                }

                break;
            }

            case Kind.JSDocTemplateTag:
            {
                {
                    var fun2 = getFunctionLikeHost(parent);
                    if ((fun2 != null))
                    {
                        if (((fun2.TypeParameters().IsNil) && (fun2.FunctionLikeData().FullSignature == null)))
                        {
                            fun2.FunctionLikeData().TypeParameters = this.gatherTypeParameters(jsDoc, false);
                            this.finishMutatedNode(fun2);
                        }
                    }
                    else
                    {
                        if ((parent.Kind == Kind.ClassDeclaration))
                        {
                            var @class = parent.AsClassDeclaration();
                            if ((@class.TypeParameters == null))
                            {
                                @class.TypeParameters = this.gatherTypeParameters(jsDoc, false);
                                this.finishMutatedNode(parent);
                            }
                        }
                        else
                        {
                            if ((parent.Kind == Kind.ClassExpression))
                            {
                                var class2 = parent.AsClassExpression();
                                if ((class2.TypeParameters == null))
                                {
                                    class2.TypeParameters = this.gatherTypeParameters(jsDoc, false);
                                    this.finishMutatedNode(parent);
                                }
                            }
                        }
                    }
                }

                break;
            }

            case Kind.JSDocParameterTag:
            {
                {
                    var fun3 = getFunctionLikeHost(parent);
                    if (((fun3 != null) && (fun3.FunctionLikeData().FullSignature == null)))
                    {
                        var parameterTag = tag.AsJSDocParameterOrPropertyTag();
                        {
                            var tuple10 = findMatchingParameter(fun3, parameterTag, jsDoc);
                            var param2 = tuple10.Item1;
                            var ok = tuple10.Item2;
                            if (ok)
                            {
                                if (((param2.Type == null) && (parameterTag.TypeExpression != null)))
                                {
                                    param2.AsParameterDeclaration().Type = this.reparseJSDocTypeLiteral(parameterTag.TypeExpression.Type());
                                }

                                if ((param2.QuestionToken == null))
                                {
                                    {
                                        var question = this.makeQuestionIfOptional(parameterTag);
                                        if ((question != null))
                                        {
                                            param2.QuestionToken = question;
                                        }
                                    }
                                }

                                this.finishMutatedNode(param2.AsNode());
                            }
                        }
                    }
                }

                break;
            }

            case Kind.JSDocThisTag:
            {
                {
                    var fun4 = getFunctionLikeHost(parent);
                    if ((fun4 != null))
                    {
                        var @params = fun4.Parameters();
                        if (((GoRuntime.Len(@params) == 0) || (((@params[0].Name().Kind != Kind.ThisKeyword) && !Ast.IsThisIdentifier(@params[0].Name())))))
                        {
                            var thisParam = this.factory.NewParameterDeclaration(null, null, this.factory.NewIdentifier("this"), null, null, null);
                            if ((tag.AsJSDocThisTag().TypeExpression != null))
                            {
                                thisParam.AsParameterDeclaration().Type = this.addDeepCloneReparse(tag.AsJSDocThisTag().TypeExpression.Type());
                            }

                            this.finishReparsedNode(thisParam, tag.TagName());
                            var newParams = this.nodeSliceArena.NewSliceHeader((GoRuntime.Len(@params) + 1));
                            newParams[0] = thisParam;
                            // reparser.go:510 identity-only copy into the fresh arena region.
                            @params.AsSpan().CopyTo(newParams.AsSpan().Slice(1));

                            fun4.FunctionLikeData().Parameters = this.newNodeList(fun4.ParameterList().Loc, newParams);
                            this.finishMutatedNode(fun4);
                        }
                    }
                }

                break;
            }

            case Kind.JSDocReturnTag:
            {
                {
                    var fun5 = getFunctionLikeHost(parent);
                    if (((fun5 != null) && (fun5.FunctionLikeData().FullSignature == null)))
                    {
                        if (((fun5.Type() == null) && (tag.TypeExpression() != null)))
                        {
                            fun5.FunctionLikeData().Type = this.addDeepCloneReparse(tag.TypeExpression().Type());
                            this.finishMutatedNode(fun5);
                        }
                    }
                }

                break;
            }

            case Kind.JSDocReadonlyTag:
            case Kind.JSDocPrivateTag:
            case Kind.JSDocPublicTag:
            case Kind.JSDocProtectedTag:
            case Kind.JSDocOverrideTag:
            {
                if ((parent.Kind == Kind.ExpressionStatement))
                {
                    parent = parent.Expression();
                }

                switch (parent.Kind)
                {
                    case Kind.MethodDeclaration:
                    case Kind.GetAccessor:
                    case Kind.SetAccessor:
                    {
                        if (((this.parsingContexts & ((1 << (int)(ParsingContext.ObjectLiteralMembers)))) != 0))
                        {
                            return;
                        }

                        goto case Kind.PropertyDeclaration;
                    }

                    case Kind.PropertyDeclaration:
                    case Kind.Constructor:
                    case Kind.BinaryExpression:
                    {
                        Kind keyword = default;
                        switch (tag.Kind)
                        {
                            case Kind.JSDocReadonlyTag:
                            {
                                keyword = Kind.ReadonlyKeyword;
                                break;
                            }

                            case Kind.JSDocPrivateTag:
                            {
                                keyword = Kind.PrivateKeyword;
                                break;
                            }

                            case Kind.JSDocPublicTag:
                            {
                                keyword = Kind.PublicKeyword;
                                break;
                            }

                            case Kind.JSDocProtectedTag:
                            {
                                keyword = Kind.ProtectedKeyword;
                                break;
                            }

                            case Kind.JSDocOverrideTag:
                            {
                                keyword = Kind.OverrideKeyword;
                                break;
                            }
                        }

                        var modifier = this.factory.NewModifier(keyword);
                        modifier.Loc = tag.Loc;
                        modifier.Flags = (this.contextFlags | NodeFlags.Reparsed);
                        Slice<Node> nodes = default;
                        TextRange loc = default;
                        if ((parent.Modifiers() == null))
                        {
                            nodes = this.nodeSliceArena.NewSliceHeader(1);
                            nodes[0] = modifier;
                            loc = tag.Loc;
                        }
                        else
                        {
                            nodes = GoRuntime.Append(parent.ModifierNodes(), modifier);
                            loc = parent.Modifiers().Loc;
                        }

                        parent.AsMutable().SetModifiers(this.newModifierList(loc, nodes));
                        this.finishMutatedNode(parent);
                        break;
                    }
                }

                break;
            }

            case Kind.JSDocImplementsTag:
            {
                {
                    var class3 = getClassLikeData(parent);
                    if ((class3 != null))
                    {
                        var implementsTag = tag.AsJSDocImplementsTag();
                        if ((class3.HeritageClauses != null))
                        {
                            {
                                var implementsClause = Core.Find(class3.HeritageClauses.Nodes, (node) =>
                                {
                                    return (node.AsHeritageClause().Token == Kind.ImplementsKeyword);
                                });
                                if ((implementsClause != null))
                                {
                                    implementsClause.AsHeritageClause().Types.Nodes = GoRuntime.Append(implementsClause.AsHeritageClause().Types.Nodes, this.addDeepCloneReparse(implementsTag.ClassName));
                                    this.finishMutatedNode(implementsClause);
                                    return;
                                }
                            }
                        }

                        var typesList = this.newNodeList(implementsTag.ClassName.Loc, this.nodeSliceArena.NewSlice1Header(this.addDeepCloneReparse(implementsTag.ClassName)));
                        var heritageClause = this.factory.NewHeritageClause(Kind.ImplementsKeyword, typesList);
                        this.finishReparsedNode(heritageClause, implementsTag.ClassName);
                        if ((class3.HeritageClauses == null))
                        {
                            var heritageClauses = this.newNodeList(implementsTag.ClassName.Loc, this.nodeSliceArena.NewSlice1Header(heritageClause));
                            class3.HeritageClauses = heritageClauses;
                        }
                        else
                        {
                            class3.HeritageClauses.Nodes = GoRuntime.Append(class3.HeritageClauses.Nodes, heritageClause);
                        }

                        this.finishMutatedNode(parent);
                    }
                }

                break;
            }

            case Kind.JSDocAugmentsTag:
            {
                {
                    var class4 = getClassLikeData(parent);
                    if (((class4 != null) && (class4.HeritageClauses != null)))
                    {
                        {
                            var extendsClause = Core.Find(class4.HeritageClauses.Nodes, (node2) =>
                            {
                                return (node2.AsHeritageClause().Token == Kind.ExtendsKeyword);
                            });
                            if (((extendsClause != null) && (GoRuntime.Len(extendsClause.AsHeritageClause().Types.Nodes) == 1)))
                            {
                                var target = extendsClause.AsHeritageClause().Types.Nodes[0].AsExpressionWithTypeArguments();
                                var source = tag.ClassName().AsExpressionWithTypeArguments();
                                if (Ast.HasSamePropertyAccessName(target.Expression, source.Expression))
                                {
                                    if (((target.TypeArguments == null) && (source.TypeArguments != null)))
                                    {
                                        var newArguments = this.nodeSliceArena.NewSliceHeader(GoRuntime.Len(source.TypeArguments.Nodes));
                                        {
                                            foreach (var range12 in GoRuntime.Range(source.TypeArguments.Nodes))
                                            {
                                                var i2 = range12.Key;
                                                var arg = range12.Value;
                                                newArguments[i2] = this.addDeepCloneReparse(arg);
                                            }
                                        }

                                        target.TypeArguments = this.newNodeList(source.TypeArguments.Loc, newArguments);
                                        this.finishMutatedNode(target.AsNode());
                                    }
                                }
                            }
                        }
                    }
                }

                break;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:615:1.
    private Node makeQuestionIfOptional(JSDocParameterOrPropertyTag parameter)
    {
        Node questionToken = default;
        if ((parameter.IsBracketed || ((parameter.TypeExpression != null) && (parameter.TypeExpression.Type().Kind == Kind.JSDocOptionalType))))
        {
            questionToken = this.factory.NewToken(Kind.QuestionToken);
            questionToken.Loc = parameter.Loc;
            questionToken.Flags = (this.contextFlags | NodeFlags.Reparsed);
        }

        return questionToken;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:625:1.
    private static (ParameterDeclaration, bool) findMatchingParameter(Node fun, JSDocParameterOrPropertyTag parameterTag, Node jsDoc)
    {
        var tagIndex = -1;
        var paramCount = -1;
        {
            foreach (var tag in GoRuntime.RangeValues(jsDoc.AsJSDoc().Tags.Nodes))
            {
                if ((tag.Kind == Kind.JSDocParameterTag))
                {
                    paramCount++;
                    if ((tag.AsJSDocParameterOrPropertyTag() == parameterTag))
                    {
                        tagIndex = paramCount;
                        break;
                    }
                }
            }
        }

        {
            foreach (var range14 in GoRuntime.Range(fun.Parameters()))
            {
                var parameterIndex = range14.Key;
                var parameter = range14.Value;
                if ((parameter.Name().Kind == Kind.Identifier))
                {
                    if (((parameterTag.Name().Kind == Kind.Identifier) && ((((parameter.Name().Text() == parameterTag.Name().Text())) || (((parameterIndex == tagIndex) && (GoRuntime.Len(parameterTag.Name().Text()) == 0)))))))
                    {
                        return (parameter.AsParameterDeclaration(), true);
                    }
                }
                else
                {
                    if ((parameterIndex == tagIndex))
                    {
                        return (parameter.AsParameterDeclaration(), true);
                    }
                }
            }
        }

        return (null, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:650:1.
    private static Node skipSatisfiesExpressions(Node node)
    {
        while (((node != null) && (node.Kind == Kind.SatisfiesExpression)))
        {
            node = node.Expression();
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:657:1.
    private static Node getFunctionLikeHost(Node host)
    {
        var fun = host;
        switch (host.Kind)
        {
            case Kind.VariableStatement:
            {
                {
                    var nodes = host.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes;
                    if ((GoRuntime.Len(nodes) != 0))
                    {
                        fun = nodes[0].Initializer();
                    }
                }

                break;
            }

            case Kind.PropertyAssignment:
            case Kind.PropertyDeclaration:
            {
                fun = host.Initializer();
                break;
            }

            case Kind.ExportAssignment:
            case Kind.ReturnStatement:
            {
                fun = host.Expression();
                break;
            }

            case Kind.ExpressionStatement:
            {
                fun = Ast.GetRightMostAssignedExpression(host.Expression());
                break;
            }
        }

        fun = skipSatisfiesExpressions(fun);
        if (Ast.IsFunctionLike(fun))
        {
            return fun;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:678:1.
    private Node makeNewCast(Node t, Node e, bool isAssertion)
    {
        Node assert = default;
        if (isAssertion)
        {
            assert = this.factory.NewAsExpression(e, t);
        }
        else
        {
            assert = this.factory.NewSatisfiesExpression(e, t);
        }

        this.finishNodeWithEnd(assert, e.Pos(), e.End());
        return assert;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:689:1.
    private static ClassLikeBase getClassLikeData(Node parent)
    {
        ClassLikeBase @class = default;
        switch (parent.Kind)
        {
            case Kind.ClassDeclaration:
            {
                @class = parent.AsClassDeclaration().ClassLikeData();
                break;
            }

            case Kind.ClassExpression:
            {
                @class = parent.AsClassExpression().ClassLikeData();
                break;
            }
        }

        return @class;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:700:1.
    private ModifierList createExportModifier(Node locationNode)
    {
        var exportModifier = this.factory.NewModifier(Kind.ExportKeyword);
        exportModifier.Loc = locationNode.Loc;
        exportModifier.Flags = (this.contextFlags | NodeFlags.Reparsed);
        var nodes = this.nodeSliceArena.NewSlice1Header(exportModifier);
        return this.newModifierList(locationNode.Loc, nodes);
    }

    // getInnermostNameOfJSDocNamespace returns the innermost identifier from a
    // JSDoc namespace chain (ModuleDeclaration). For a simple identifier, it returns
    // the identifier itself. For "A.B.C", it returns the identifier "C".
    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:711:1.
    private Node getInnermostNameOfJSDocNamespace(Node fullName)
    {
        if ((fullName == null))
        {
            return null;
        }

        while ((fullName.Kind == Kind.ModuleDeclaration))
        {
            var body = fullName.AsModuleDeclaration().Body;
            if ((body == null))
            {
                return fullName.Name();
            }

            fullName = body;
        }

        return fullName;
    }

    // wrapInJSDocNamespace wraps a statement (typically a type alias) in namespace
    // declarations corresponding to a JSDoc dotted name. For example, given name
    // "A.B.C" and a type alias for C, this produces:
    //
    //	namespace A { namespace B { type C = ... } }
    //
    // If the name is a simple identifier (not a ModuleDeclaration), it returns the
    // statement as-is.
    // Upstream upstream/TypeScript/tsc/internal/parser/reparser.go:733:1.
    private Node wrapInJSDocNamespace(Node fullName, Node statement, bool nested)
    {
        if (((fullName == null) || !Ast.IsModuleDeclaration(fullName)))
        {
            return statement;
        }

        var wrapped = this.wrapInJSDocNamespace(fullName.Body(), statement, true);
        var block = this.factory.NewModuleBlock(this.newNodeList(fullName.Loc, this.nodeSliceArena.NewSlice1Header(wrapped)));
        this.finishReparsedNode(block, fullName);
        ModifierList modifiers = default;
        if (nested)
        {
            modifiers = this.createExportModifier(fullName);
        }

        var result = this.factory.NewModuleDeclaration(modifiers, Kind.NamespaceKeyword, this.addDeepCloneReparse(fullName.Name()), null, block);
        this.finishReparsedNode(result, fullName);
        this.reparsedClones = GoRuntime.Append(this.reparsedClones, result);
        return result;
    }
}
