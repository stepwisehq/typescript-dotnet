// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/jsdoc.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using Kind = TypeScript.Scanner.Kind;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices;
internal readonly record struct JSDocTagInfo(ByteStringValue Name, ByteStringValue Text);
internal sealed partial class LanguageService
{
    // jsdoc.go:28. Resolve no aliases here; callers own that explicit operation.
    internal static ByteStringValue GetSymbolDocumentationComment(Checker checker, Symbol? symbol)
    {
        if (symbol == null)
            return default;
        Slice<ByteStringValue> parts = default;
        HashSet<Node>? seen = null;
        foreach (var declaration in symbol.Declarations)
        {
            if (declaration == null)
                continue;
            if (!(seen ??= new()).Add(declaration))
                continue;
            var documentation = getDocumentationFromDeclaration(noMappedLocation, checker, symbol, declaration, declaration, MarkupKind.PlainText, true);
            if (documentation.Length != 0 && !Core.Some(parts, documentation, static (part, expected) => part.EqualsValue(expected)))
                parts = parts.Append(documentation);
        }

        return GoStrings.Join(parts, ByteStringValue.FromString("\n"));
    }

    // jsdoc.go:52. Preserve nil headers for absent symbols and no matching tags.
    internal static Slice<JSDocTagInfo> GetSymbolJSDocTags(Symbol? symbol)
    {
        if (symbol == null)
            return default;
        Slice<JSDocTagInfo> infos = default;
        HashSet<Node>? seen = null;
        foreach (var declaration in symbol.Declarations)
        {
            if (declaration == null)
                continue;
            if (!(seen ??= new()).Add(declaration))
                continue;
            var tags = declarationJSDocTags(declaration);
            bool hasTypedef = Core.Some(tags, static tag => tag.Kind is Kind.JSDocTypedefTag or Kind.JSDocCallbackTag);
            bool hasParamOrReturn = Core.Some(tags, static tag => tag.Kind is Kind.JSDocParameterTag or Kind.JSDocReturnTag);
            if (hasTypedef && !hasParamOrReturn)
                continue;
            foreach (var tag in tags)
                infos = infos.Append(new JSDocTagInfo(tag.TagName().TextValue(), getJSDocTagText(tag)));
        }

        return infos;
    }

    // jsdoc.go:89. Last applicable JSDoc comment owns the returned borrowed tag slice.
    private static Slice<Node> declarationJSDocTags(Node node)
    {
        if ((node.Flags & NodeFlags.JSDoc) == 0)
        {
            for (Node? current = node; current != null; current = Ast.GetNextJSDocCommentLocation(current))
            {
                var docs = current.JSDoc(null);
                if (docs.Count == 0)
                    continue;
                var last = docs[docs.Count - 1].AsJSDoc();
                if (last.Tags != null)
                    return last.Tags.Nodes;
            }
        }

        return default;
    }

    // jsdoc.go:108. The comment header stays borrowed unless the original concatenates.
    private static ByteStringValue getJSDocTagText(Node tag)
    {
        var comment = ScannerApi.GetTextOfJSDocCommentValue(tag.CommentList());
        static ByteStringValue addComment(ByteStringValue text, ByteStringValue comment) => comment.Length == 0 ? text : ByteStringValue.Concat(text, ByteStringValue.FromString(" "), comment);
        switch (tag.Kind)
        {
            case Kind.JSDocThrowsTag:
                var throwsType = tag.AsJSDocThrowsTag().TypeExpression;
                return throwsType != null ? addComment(ScannerApi.GetTextOfNodeValue(throwsType), comment) : comment;
            case Kind.JSDocImplementsTag:
                return addComment(ScannerApi.GetTextOfNodeValue(tag.AsJSDocImplementsTag().ClassName), comment);
            case Kind.JSDocAugmentsTag:
                return addComment(ScannerApi.GetTextOfNodeValue(tag.AsJSDocAugmentsTag().ClassName), comment);
            case Kind.JSDocTemplateTag:
                var template = tag.AsJSDocTemplateTag();
                var builder = new GoBuilder();
                if (template.Constraint != null)
                    builder.WriteString(ScannerApi.GetTextOfNodeValue(template.Constraint));
                if (template.TypeParameters != null)
                {
                    for (int i = 0; i < template.TypeParameters.Nodes.Count; i++)
                    {
                        if (i == 0 && builder.Len() != 0)
                            builder.WriteString(" ");
                        if (i != 0)
                            builder.WriteString(", ");
                        builder.WriteString(ScannerApi.GetTextOfNodeValue(template.TypeParameters.Nodes[i]));
                    }
                }

                if (comment.Length != 0)
                {
                    if (builder.Len() != 0)
                        builder.WriteString(" ");
                    builder.WriteString(comment);
                }

                return builder.Value();
            case Kind.JSDocTypeTag:
                return addComment(ScannerApi.GetTextOfNodeValue(tag.AsJSDocTypeTag().TypeExpression), comment);
            case Kind.JSDocSatisfiesTag:
                return addComment(ScannerApi.GetTextOfNodeValue(tag.AsJSDocSatisfiesTag().TypeExpression), comment);
            case Kind.JSDocSeeTag:
                var seeName = tag.AsJSDocSeeTag().NameExpression;
                return seeName != null ? addComment(ScannerApi.GetTextOfNodeValue(seeName), comment) : comment;
            case Kind.JSDocParameterTag:
            case Kind.JSDocPropertyTag:
                var name = tag.Name();
                return name != null ? addComment(ScannerApi.GetTextOfNodeValue(name), comment) : comment;
            default:
                return comment;
        }
    }

    private static Node? getJSDoc(Node node)
    {
        var docs = node.JSDoc(null);
        return docs.Count == 0 ? null : docs[docs.Count - 1];
    }

    // jsdoc.go:172. The seen-symbol owner spans recursive inherited-documentation queries.
    private static Node? getJSDocOrTag(Checker checker, Node? node, ref HashSet<Symbol>? seenSymbols)
    {
        if (node == null)
            return null;
        var jsdoc = getJSDoc(node);
        if (jsdoc != null)
            return jsdoc;
        if (Ast.IsParameterDeclaration(node))
        {
            var name = node.Name();
            if (Ast.IsBindingPattern(name))
                return getJSDocParameterTagByPosition(checker, node);
            return getMatchingJSDocTag(checker, node.Parent, name.TextValue(), isMatchingParameterTag, ref seenSymbols);
        }

        if (Ast.IsTypeParameterDeclaration(node))
            return getMatchingJSDocTag(checker, node.Parent, node.Name().TextValue(), isMatchingTemplateTag, ref seenSymbols);
        if (Ast.IsVariableDeclaration(node) && Ast.IsVariableDeclarationList(node.Parent) && Core.FirstOrNil(node.Parent.AsVariableDeclarationList().Declarations.Nodes) == node)
            return getJSDocOrTag(checker, node.Parent.Parent, ref seenSymbols);
        if ((Ast.IsFunctionExpressionOrArrowFunction(node) || Ast.IsClassExpression(node))
            && (Ast.IsVariableDeclaration(node.Parent) || Ast.IsPropertyDeclaration(node.Parent)
            || Ast.IsPropertyAssignment(node.Parent)) && node.Parent.Initializer() == node)
            return getJSDocOrTag(checker, node.Parent, ref seenSymbols);
        if (Ast.IsBindingElement(node) && Ast.IsObjectBindingPattern(node.Parent))
        {
            var name = node.PropertyNameOrName();
            if (Ast.IsIdentifier(name))
            {
                var objectType = checker.GetTypeAtLocation(node.Parent);
                if (objectType != null)
                {
                    var property = checker.GetPropertyOfType(objectType, name.TextValue());
                    if (property != null)
                        foreach (var declaration in property.Declarations)
                        {
                            jsdoc = getJSDoc(declaration);
                            if (jsdoc != null)
                                return jsdoc;
                        }
                }
            }
        }

        var symbol = node.Symbol();
        if (symbol != null && node.Parent != null)
        {
            if (Ast.IsFunctionDeclaration(node) || Ast.IsMethodDeclaration(node) || Ast.IsMethodSignatureDeclaration(node) || Ast.IsConstructorDeclaration(node) || Ast.IsConstructSignatureDeclaration(node))
            {
                var firstSignature = Core.Find(symbol.Declarations, Ast.IsFunctionLike);
                if (firstSignature != null && node != firstSignature)
                {
                    jsdoc = getJSDocOrTag(checker, firstSignature, ref seenSymbols);
                    if (jsdoc != null)
                        return jsdoc;
                }
            }

            if (Ast.IsClassOrInterfaceLike(node.Parent))
            {
                bool isStatic = Ast.HasStaticModifier(node);
                var classType = checker.GetDeclaredTypeOfSymbol(node.Parent.Symbol());
                if (isStatic)
                {
                    var staticBaseType = checker.GetApparentType(checker.GetBaseConstructorTypeOfClass(classType));
                    var property = checker.GetPropertyOfType(staticBaseType, symbol.NameValue);
                    if (property != null && property.ValueDeclaration != null && (seenSymbols ??= new()).Add(property))
                    {
                        jsdoc = getJSDocOrTag(checker, property.ValueDeclaration, ref seenSymbols);
                        if (jsdoc != null)
                            return jsdoc;
                    }
                }
                else
                {
                    foreach (var baseType in checker.GetBaseTypes(classType))
                    {
                        var property = checker.GetPropertyOfType(baseType, symbol.NameValue);
                        if (property != null && property.ValueDeclaration != null && (seenSymbols ??= new()).Add(property))
                        {
                            jsdoc = getJSDocOrTag(checker, property.ValueDeclaration, ref seenSymbols);
                            if (jsdoc != null)
                                return jsdoc;
                        }
                    }
                }
            }
        }

        return null;
    }

    // jsdoc.go:245.
    private static Node? getMatchingJSDocTag(Checker checker, Node? node, ByteStringValue name, Func<Node, ByteStringValue, bool> match, ref HashSet<Symbol>? seenSymbols)
    {
        var jsdoc = getJSDocOrTag(checker, node, ref seenSymbols);
        if (jsdoc != null && jsdoc.Kind == Kind.JSDoc)
        {
            var tags = jsdoc.AsJSDoc().Tags;
            if (tags != null)
                foreach (var tag in tags.Nodes)
                    if (match(tag, name))
                        return tag;
        }

        return null;
    }

    // jsdoc.go:260. Binding-pattern parameters use positional rather than name matching.
    private static Node? getJSDocParameterTagByPosition(Checker checker, Node parameter)
    {
        var parent = parameter.Parent;
        if (parent == null)
            return null;
        var parameters = parent.Parameters();
        int parameterIndex = -1;
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i] == parameter)
            {
                parameterIndex = i;
                break;
            }
        }

        if (parameterIndex < 0)
            return null;
        HashSet<Symbol>? seenSymbols = null;
        var jsdoc = getJSDocOrTag(checker, parent, ref seenSymbols);
        if (jsdoc == null || jsdoc.Kind != Kind.JSDoc)
            return null;
        var tags = jsdoc.AsJSDoc().Tags;
        if (tags == null)
            return null;
        int tagIndex = 0;
        foreach (var tag in tags.Nodes)
        {
            if (tag.Kind == Kind.JSDocParameterTag)
            {
                if (tagIndex == parameterIndex)
                    return tag;
                tagIndex++;
            }
        }

        return null;
    }

    private static bool isMatchingParameterTag(Node tag, ByteStringValue name) => tag.Kind == Kind.JSDocParameterTag && isNodeWithName(tag, name);
    private static bool isMatchingTemplateTag(Node tag, ByteStringValue name) => tag.Kind == Kind.JSDocTemplateTag && Core.Some(tag.TypeParameters(), name, static (parameter, expected) => isNodeWithName(parameter, expected));
    private static bool isNodeWithName(Node node, ByteStringValue name)
    {
        var nodeName = node.Name();
        return Ast.IsIdentifier(nodeName) && nodeName.TextValue().EqualsValue(name);
    }

    private static (Location Location, Fidelity Fidelity) noMappedLocation(SourceFile _, TextRange __) => (new Location(), Fidelity.None);
}
