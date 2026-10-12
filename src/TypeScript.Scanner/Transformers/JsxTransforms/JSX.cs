// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete jsxtransforms/jsx.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Collections;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Transformers.JsxTransforms;
internal sealed class JSXTransformer : Transformer
{
    private CompilerOptions compilerOptions;
    private IEmitResolver emitResolver;
    private ByteStringValue importSpecifier;
    private Node filenameDeclaration;
    private readonly OrderedMap<ByteStringKey, Dictionary<string, Node>> utilizedImplicitRuntimeImports = new();
    private bool inJsxChild;
    private SourceFile currentSourceFile;
    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:32:1.
    internal static Transformer NewJSXTransformer(TransformOptions opts)
    {
        var compilerOptions = opts.CompilerOptions;
        var emitContext = opts.Context;
        var tx = new JSXTransformer
        {
            compilerOptions = compilerOptions,
            emitResolver = opts.EmitResolver
        };
        return tx.NewTransformer(tx.visit, emitContext);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:42:1.
    private Node getCurrentFileNameExpression()
    {
        if ((this.filenameDeclaration != null))
        {
            return this.filenameDeclaration.AsVariableDeclaration().Name();
        }

        var d = this.Factory().NewVariableDeclaration(this.Factory().NewUniqueNameEx("_jsxFileName", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) }), null, null, this.Factory().NewStringLiteralWithTextValue(this.currentSourceFile.FileNameGoValue(), TokenFlags.None));
        this.filenameDeclaration = d;
        return d.AsVariableDeclaration().Name();
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:58:1.
    private string getJsxFactoryCalleePrimitive(bool isStaticChildren)
    {
        if ((this.compilerOptions.Jsx == JsxEmit.ReactJSXDev))
        {
            return "jsxDEV";
        }

        if (isStaticChildren)
        {
            return "jsxs";
        }

        return "jsx";
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:68:1.
    private Node getJsxFactoryCallee(bool isStaticChildren)
    {
        var t = this.getJsxFactoryCalleePrimitive(isStaticChildren);
        return this.getImplicitImportForName(t);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:73:1.
    private Node getImplicitJsxFragmentReference()
    {
        return this.getImplicitImportForName("Fragment");
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:77:1.
    private Node getImplicitImportForName(string name)
    {
        var importSource = this.importSpecifier;
        if ((name != "createElement"))
        {
            importSource = Ast.GetJSXRuntimeImportValue(importSource, this.compilerOptions);
        }

        var tuple1 = this.utilizedImplicitRuntimeImports.Get(ByteStringKey.FromValue(importSource));
        var existing = tuple1.Item1;
        var ok = tuple1.Item2;
        if (ok)
        {
            var ok2 = existing.TryGetValue(name, out var elem);
            if (ok2)
            {
                return elem.AsImportSpecifier().Name();
            }
        }
        else
        {
            existing = new Dictionary<string, Node>();
            this.utilizedImplicitRuntimeImports.Set(ByteStringKey.FromValue(importSource), existing);
        }

        var generatedName = this.Factory().NewUniqueNameEx(("_" + name), new AutoGenerateOptions { Flags = ((GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) | GeneratedIdentifierFlags.AllowNameSubstitution) });
        var specifier = this.Factory().NewImportSpecifier(false, this.Factory().NewIdentifier(name), generatedName);
        this.emitResolver.SetReferencedImportDeclaration(generatedName, specifier);
        existing[name] = specifier;
        return specifier.Name();
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:102:1.
    private void setInChild(bool v)
    {
        this.inJsxChild = v;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:106:1.
    private Node visit(Node node)
    {
        if ((node == null))
        {
            return null;
        }

        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsJsx) == 0))
        {
            return node;
        }

        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                this.setInChild(false);
                return this.visitSourceFile(node.AsSourceFile());
            }

            case Kind.JsxElement:
            {
                return this.visitJsxElement(node.AsJsxElement());
            }

            case Kind.JsxSelfClosingElement:
            {
                return this.visitJsxSelfClosingElement(node.AsJsxSelfClosingElement());
            }

            case Kind.JsxFragment:
            {
                return this.visitJsxFragment(node.AsJsxFragment());
            }

            case Kind.JsxOpeningElement:
            {
                throw new InvalidOperationException("JsxOpeningElement should not be visited, handled in visitJsxElement");
            }

            case Kind.JsxOpeningFragment:
            {
                throw new InvalidOperationException("JsxOpeningFragment should not be visited, handled in visitJsxFragment");
            }

            case Kind.JsxText:
            {
                this.setInChild(false);
                return this.visitJsxText(node.AsJsxText());
            }

            case Kind.JsxExpression:
            {
                this.setInChild(false);
                return this.visitJsxExpression(node.AsJsxExpression());
            }
        }

        this.setInChild(false);
        return this.Visitor().VisitEachChild(node);
    }

    // *
    // * The react jsx/jsxs transform falls back to `createElement` when an explicit `key` argument comes after a spread
    // 
    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:141:1.
    private static bool hasKeyAfterPropsSpread(Node node)
    {
        var spread = false;
        var opener = node;
        if ((node.Kind == Kind.JsxElement))
        {
            opener = node.AsJsxElement().OpeningElement;
        }

        {
            foreach (var elem in GoRuntime.RangeValues(opener.Attributes().Properties()))
            {
                if ((Ast.IsJsxSpreadAttribute(elem) && ((!Ast.IsObjectLiteralExpression(elem.Expression()) || Core.Some(elem.Expression().Properties(), Ast.IsSpreadAssignment)))))
                {
                    spread = true;
                }
                else
                {
                    if ((((spread && Ast.IsJsxAttribute(elem)) && Ast.IsIdentifier(elem.Name())) && (elem.Name().TextEquals("key"u8))))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:157:1.
    private bool shouldUseCreateElement(Node node)
    {
        return ((this.importSpecifier.Length == 0) || hasKeyAfterPropsSpread(node));
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:161:1.
    private static Slice<Node> insertStatementAfterPrologue<T>(Slice<Node> to, Node statement, Func<T, Node, bool> isPrologueDirective, T callee2)
    {
        if ((statement == null))
        {
            return to;
        }

        var statementIdx = 0;
        for (; (statementIdx < GoRuntime.Len(to)); statementIdx++)
        {
            if (!isPrologueDirective(callee2, to[statementIdx]))
            {
                break;
            }
        }

        return to.Insert(statementIdx, statement);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:175:1.
    private bool isAnyPrologueDirective(Node node)
    {
        return (Ast.IsPrologueDirective(node) || (((this.EmitContext().EmitFlags(node) & EmitFlags.CustomPrologue) != 0)));
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:179:1.
    private Slice<Node> insertStatementAfterCustomPrologue(Slice<Node> to, Node statement)
    {
        return insertStatementAfterPrologue<JSXTransformer>(to, statement, static (tx, node) => tx.isAnyPrologueDirective(node), this);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:183:1.
    private static int sortImportSpecifiers(Node a, Node b)
    {
        var res = ByteStringKey.FromValue(a.PropertyName().TextValue()).CompareTo(ByteStringKey.FromValue(b.PropertyName().TextValue()));
        if ((res != 0))
        {
            return res;
        }

        return ByteStringKey.FromValue(a.AsImportSpecifier().Name().TextValue()).CompareTo(ByteStringKey.FromValue(b.AsImportSpecifier().Name().TextValue()));
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:191:1.
    private static Slice<Node> getSortedSpecifiers(Dictionary<string, Node> m)
    {
        var res = Slice<Node>.Make(0, m.Count);
        foreach (var value in m.Values)
            res = res.Append(value);
        res.AsSpan().Sort(sortImportSpecifiers);
        return res;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:197:1.
    private Node visitSourceFile(SourceFile file)
    {
        if (file.IsDeclarationFile)
        {
            return file.AsNode();
        }

        this.currentSourceFile = file;
        this.importSpecifier = Ast.GetJSXImplicitImportBaseValue(this.compilerOptions, file);
        this.filenameDeclaration = null;
        this.utilizedImplicitRuntimeImports.Clear();
        var visited = this.Visitor().VisitEachChild(file.AsNode());
        this.EmitContext().AddEmitHelper(visited.AsNode(), this.EmitContext().ReadEmitHelpers());
        var statements = visited.Statements();
        var statementsUpdated = false;
        if ((this.filenameDeclaration != null))
        {
            statements = this.insertStatementAfterCustomPrologue(statements, this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.filenameDeclaration])), NodeFlags.Const)));
            statementsUpdated = true;
        }

        if ((this.utilizedImplicitRuntimeImports.Size() > 0))
        {
            if (Ast.IsExternalModule(file))
            {
                statementsUpdated = true;
                var newStatements = Slice<Node>.Make(0, this.utilizedImplicitRuntimeImports.Size());
                {
                    foreach (var range3 in this.utilizedImplicitRuntimeImports.EntryValues())
                    {
                        var importSource = range3.Key.Value;
                        var importSpecifiersMap = range3.Value;
                        var s = this.Factory().NewImportDeclaration(null, this.Factory().NewImportClause(Kind.Unknown, null, this.Factory().NewNamedImports(this.Factory().NewNodeList(getSortedSpecifiers(importSpecifiersMap)))), this.Factory().NewStringLiteralWithTextValue(importSource, TokenFlags.None), null);
                        Ast.SetParentInChildren(s);
                        newStatements = newStatements.Append(s);
                    }
                }

                {
                    foreach (var e in GoRuntime.RangeValues(newStatements))
                    {
                        statements = this.insertStatementAfterCustomPrologue(statements, e);
                    }
                }
            }
            else
            {
                if (Ast.IsExternalOrCommonJSModule(file))
                {
                    statementsUpdated = true;
                    var newStatements2 = Slice<Node>.Make(0, this.utilizedImplicitRuntimeImports.Size());
                    {
                        foreach (var range5 in this.utilizedImplicitRuntimeImports.EntryValues())
                        {
                            var importSource2 = range5.Key.Value;
                            var importSpecifiersMap2 = range5.Value;
                            var sorted = getSortedSpecifiers(importSpecifiersMap2);
                            var asBindingElems = Slice<Node>.Make(0, sorted.Length);
                            {
                                foreach (var range6 in GoRuntime.Range(sorted))
                                {
                                    var _2 = range6.Key;
                                    var elem = range6.Value;
                                    asBindingElems = asBindingElems.Append(this.Factory().NewBindingElement(null, elem.PropertyName(), elem.AsImportSpecifier().Name(), null));
                                }
                            }

                            var s2 = this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(this.Factory().NewBindingPattern(Kind.ObjectBindingPattern, this.Factory().NewNodeList(asBindingElems)), null, null, this.Factory().NewCallExpression(this.Factory().NewIdentifier("require"), null, null, this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewStringLiteralWithTextValue(importSource2, TokenFlags.None)])), NodeFlags.None))])), NodeFlags.Const));
                            Ast.SetParentInChildren(s2);
                            newStatements2 = newStatements2.Append(s2);
                        }
                    }

                    {
                        foreach (var range7 in GoRuntime.Range(newStatements2))
                        {
                            var _3 = range7.Key;
                            var e2 = range7.Value;
                            statements = this.insertStatementAfterCustomPrologue(statements, e2);
                        }
                    }
                }
                else
                {
                }
            }
        }

        if (statementsUpdated)
        {
            visited = this.Factory().UpdateSourceFile(file, this.Factory().NewNodeList(statements), file.EndOfFileToken);
        }

        this.currentSourceFile = null;
        this.importSpecifier = default;
        this.filenameDeclaration = null;
        this.utilizedImplicitRuntimeImports.Clear();
        return visited;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:280:1.
    private Node visitJsxElement(JsxElement element)
    {
        Func<JSXTransformer, Node, NodeList, TextRange, Node> tagTransform = static (tx, value, list, range) => tx.visitJsxOpeningLikeElementJSX(value, list, range);
        if (this.shouldUseCreateElement(element.AsNode()))
        {
            tagTransform = static (tx, value, list, range) => tx.visitJsxOpeningLikeElementCreateElement(value, list, range);
        }

        var location = Core.NewTextRange(ScannerApi.SkipTrivia(this.currentSourceFile.TextView(), element.Pos()), element.End());
        return tagTransform(this, element.OpeningElement, element.Children, location);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:289:1.
    private Node visitJsxSelfClosingElement(JsxSelfClosingElement element)
    {
        Func<JSXTransformer, Node, NodeList, TextRange, Node> tagTransform = static (tx, value, list, range) => tx.visitJsxOpeningLikeElementJSX(value, list, range);
        if (this.shouldUseCreateElement(element.AsNode()))
        {
            tagTransform = static (tx, value, list, range) => tx.visitJsxOpeningLikeElementCreateElement(value, list, range);
        }

        var location = Core.NewTextRange(ScannerApi.SkipTrivia(this.currentSourceFile.TextView(), element.Pos()), element.End());
        return tagTransform(this, element.AsNode(), null, location);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:298:1.
    private Node visitJsxFragment(JsxFragment fragment)
    {
        Func<JSXTransformer, JsxOpeningFragment, NodeList, TextRange, Node> tagTransform = static (tx, value, list, range) => tx.visitJsxOpeningFragmentJSX(value, list, range);
        if ((this.importSpecifier.Length == 0))
        {
            tagTransform = static (tx, value, list, range) => tx.visitJsxOpeningFragmentCreateElement(value, list, range);
        }

        var location = Core.NewTextRange(ScannerApi.SkipTrivia(this.currentSourceFile.TextView(), fragment.Pos()), fragment.End());
        return tagTransform(this, fragment.OpeningFragment.AsJsxOpeningFragment(), fragment.Children, location);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:307:1.
    private Node convertJsxChildrenToChildrenPropObject(Slice<Node> children)
    {
        var prop = this.convertJsxChildrenToChildrenPropAssignment(children);
        if ((prop == null))
        {
            return null;
        }

        return this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(Slice<Node>.FromArray([prop])), false);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:315:1.
    private Node transformJsxChildToExpression(Node node)
    {
        var prev = this.inJsxChild;
        this.setInChild(true);
        try
        {
            return this.Visitor().VisitNode(node);
        }
        finally
        {
            this.setInChild(prev);
        }
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:322:1.
    private Node convertJsxChildrenToChildrenPropAssignment(Slice<Node> children)
    {
        var nonWhitespceChildren = Ast.GetSemanticJsxChildren(children);
        if (((GoRuntime.Len(nonWhitespceChildren) == 1) && (((nonWhitespceChildren[0].Kind != Kind.JsxExpression) || (nonWhitespceChildren[0].AsJsxExpression().DotDotDotToken == null)))))
        {
            var result = this.transformJsxChildToExpression(nonWhitespceChildren[0]);
            if ((result == null))
            {
                return null;
            }

            return this.Factory().NewPropertyAssignment(null, this.Factory().NewIdentifier("children"), null, null, result);
        }

        var results = Slice<Node>.Make(0, nonWhitespceChildren.Length);
        {
            foreach (var child in GoRuntime.RangeValues(nonWhitespceChildren))
            {
                var res = this.transformJsxChildToExpression(child);
                if ((res == null))
                {
                    continue;
                }

                this.EmitContext().SetEmitFlags(res, (this.EmitContext().EmitFlags(res) & ~EmitFlags.StartOnNewLine));
                results = results.Append(res);
            }
        }

        if ((GoRuntime.Len(results) == 0))
        {
            return null;
        }

        return this.Factory().NewPropertyAssignment(null, this.Factory().NewIdentifier("children"), null, null, this.Factory().NewArrayLiteralExpression(this.Factory().NewNodeList(results), false));
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:348:1.
    private Node getTagName(Node node)
    {
        if ((node.Kind == Kind.JsxElement))
        {
            return this.getTagName(node.AsJsxElement().OpeningElement);
        }
        else
        {
            if (Ast.IsJsxOpeningLikeElement(node))
            {
                var tagName = node.TagName();
                if ((Ast.IsIdentifier(tagName) && ScannerApi.IsIntrinsicJsxName(tagName.TextValue())))
                {
                    return this.Factory().NewStringLiteralWithTextValue(tagName.TextValue(), TokenFlags.None);
                }
                else
                {
                    if (Ast.IsJsxNamespacedName(tagName))
                    {
                        return this.Factory().NewStringLiteralWithTextValue(ByteStringValue.Concat(tagName.AsJsxNamespacedName().Namespace.TextValue(), ByteStringValue.FromString(":"), tagName.AsJsxNamespacedName().Name().TextValue()), TokenFlags.None);
                    }
                    else
                    {
                        return this.Factory().CreateExpressionFromEntityName(tagName);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException(("unhandled node kind passed to getTagName: " + node.KindString()));
            }
        }
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:367:1.
    private Node visitJsxOpeningLikeElementJSX(Node element, NodeList children, TextRange location)
    {
        var tagName = this.getTagName(element);
        Node childrenProp = default;
        if (((children != null) && (GoRuntime.Len(children.Nodes) > 0)))
        {
            childrenProp = this.convertJsxChildrenToChildrenPropAssignment(children.Nodes);
        }

        Node keyAttr = default;
        var attrs = element.Attributes().Properties();
        {
            foreach (var range9 in GoRuntime.Range(attrs))
            {
                var i = range9.Key;
                var p = range9.Value;
                if (((((p.Kind == Kind.JsxAttribute) && (p.AsJsxAttribute().Name() != null)) && Ast.IsIdentifier(p.AsJsxAttribute().Name())) && (p.AsJsxAttribute().Name().TextEquals("key"u8))))
                {
                    keyAttr = p;
                    attrs = GoSlices.Clone(attrs);
                    attrs = GoSlices.Delete(attrs, i, (i + 1));
                    break;
                }
            }
        }

        Node @object = default;
        if ((GoRuntime.Len(attrs) > 0))
        {
            @object = this.transformJsxAttributesToObjectProps(attrs, childrenProp);
        }
        else
        {
            var objectChildren = Slice<Node>.Make(0);
            if ((childrenProp != null))
            {
                objectChildren = objectChildren.Append(childrenProp);
            }

            @object = this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(objectChildren), false);
        }

        return this.visitJsxOpeningLikeElementOrFragmentJSX(tagName, @object, keyAttr, children, location);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:402:1.
    private Node transformJsxAttributesToObjectProps(Slice<Node> attrs, Node childrenProp)
    {
        var target = this.compilerOptions.GetEmitScriptTarget();
        if ((target >= ScriptTarget.ES2018))
        {
            return this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(this.transformJsxAttributesToProps(attrs, childrenProp)), false);
        }

        return this.transformJsxAttributesToExpression(attrs, childrenProp);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:411:1.
    private Node transformJsxAttributesToExpression(Slice<Node> attrs, Node childrenProp)
    {
        var expressions = Slice<Node>.Make(0, 2);
        var properties = Slice<Node>.Make(0, attrs.Length);
        {
            foreach (var attr in GoRuntime.RangeValues(attrs))
            {
                if (Ast.IsJsxSpreadAttribute(attr))
                {
                    if ((Ast.IsObjectLiteralExpression(attr.Expression()) && !hasProto(attr.Expression().AsObjectLiteralExpression())))
                    {
                        {
                            foreach (var range11 in GoRuntime.Range(attr.Expression().Properties()))
                            {
                                var _2 = range11.Key;
                                var prop = range11.Value;
                                if (Ast.IsSpreadAssignment(prop))
                                {
                                    (expressions, properties) = this.combinePropertiesIntoNewExpression(expressions, properties);
                                    expressions = expressions.Append(this.Visitor().VisitNode(prop.Expression()));
                                    continue;
                                }

                                properties = properties.Append(this.Visitor().VisitNode(prop));
                            }
                        }

                        continue;
                    }

                    (expressions, properties) = this.combinePropertiesIntoNewExpression(expressions, properties);
                    expressions = expressions.Append(this.Visitor().VisitNode(attr.Expression()));
                    continue;
                }

                properties = properties.Append(this.transformJsxAttributeToObjectLiteralElement(attr.AsJsxAttribute()));
            }
        }

        if ((childrenProp != null))
        {
            properties = properties.Append(childrenProp);
        }

        (expressions, _) = this.combinePropertiesIntoNewExpression(expressions, properties);
        if (((GoRuntime.Len(expressions) > 0) && !Ast.IsObjectLiteralExpression(expressions[0])))
        {
            expressions = Slice<Node>.FromArray([this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(Slice<Node>.Make(0)), false)]).Append(expressions);
        }

        if ((GoRuntime.Len(expressions) == 1))
        {
            return expressions[0];
        }

        return this.Factory().NewAssignHelper(expressions, this.compilerOptions.GetEmitScriptTarget());
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:456:1.
    private (Slice<Node>, Slice<Node>) combinePropertiesIntoNewExpression(Slice<Node> expressions, Slice<Node> props)
    {
        if ((GoRuntime.Len(props) == 0))
        {
            return (expressions, props);
        }

        var newObj = this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(props), false);
        expressions = expressions.Append(newObj);
        return (expressions, default);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:465:1.
    private Slice<Node> transformJsxAttributesToProps(Slice<Node> attrs, Node childrenProp)
    {
        var props = Slice<Node>.Make(0, attrs.Length);
        {
            foreach (var attr in GoRuntime.RangeValues(attrs))
            {
                if ((attr.Kind == Kind.JsxSpreadAttribute))
                {
                    var res = this.transformJsxSpreadAttributesToProps(attr.AsJsxSpreadAttribute());
                    props = props.Append(res);
                }
                else
                {
                    props = props.Append(this.transformJsxAttributeToObjectLiteralElement(attr.AsJsxAttribute()));
                }
            }
        }

        if ((childrenProp != null))
        {
            props = props.Append(childrenProp);
        }

        return props;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:481:1.
    private static bool hasProto(ObjectLiteralExpression obj)
    {
        {
            foreach (var p in GoRuntime.RangeValues(obj.Properties.Nodes))
            {
                if (((Ast.IsPropertyAssignment(p) && ((Ast.IsStringLiteral(p.Name()) || Ast.IsIdentifier(p.Name())))) && (p.Name().TextEquals("__proto__"u8))))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:490:1.
    private Slice<Node> transformJsxSpreadAttributesToProps(JsxSpreadAttribute node)
    {
        if ((Ast.IsObjectLiteralExpression(node.Expression) && !hasProto(node.Expression.AsObjectLiteralExpression())))
        {
            var tuple14 = this.Visitor().VisitSlice(node.Expression.Properties());
            var res = tuple14.Item1;
            return res;
        }

        return Slice<Node>.FromArray([this.Factory().NewSpreadAssignment(this.Visitor().VisitNode(node.Expression))]);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:498:1.
    private Node transformJsxAttributeToObjectLiteralElement(JsxAttribute node)
    {
        var name = this.getAttributeName(node);
        var expression = this.transformJsxAttributeInitializer(node.Initializer);
        return this.Factory().NewPropertyAssignment(null, name, null, null, expression);
    }

    // *
    // * Emit an attribute name, which is quoted if it needs to be quoted. Because
    // * these emit into an object literal property name, we don't need to be worried
    // * about keywords, just non-identifier characters
    // 
    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:509:1.
    private Node getAttributeName(JsxAttribute node)
    {
        var name = node.Name();
        if (Ast.IsIdentifier(name))
        {
            var text = name.TextValue();
            if (ScannerApi.IsIdentifierTextByteString(text, LanguageVariant.Standard))
            {
                return name;
            }

            return this.Factory().NewStringLiteralWithTextValue(text, TokenFlags.None);
        }

        return this.Factory().NewStringLiteralWithTextValue(ByteStringValue.Concat(name.AsJsxNamespacedName().Namespace.TextValue(), ByteStringValue.FromString(":"), name.AsJsxNamespacedName().Name().TextValue()), TokenFlags.None);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:524:1.
    private Node transformJsxAttributeInitializer(Node node)
    {
        if ((node == null))
        {
            return this.Factory().NewTrueExpression();
        }

        if ((node.Kind == Kind.StringLiteral))
        {
            var res = this.Factory().NewStringLiteralWithTextValue(decodeEntities(node.TextValue()), node.AsStringLiteral().TokenFlags);
            res.Loc = node.Loc;
            res.AsStringLiteral().TokenFlags = node.AsStringLiteral().TokenFlags;
            return res;
        }

        if ((node.Kind == Kind.JsxExpression))
        {
            if ((node.Expression() == null))
            {
                return this.Factory().NewTrueExpression();
            }

            return this.Visitor().VisitNode(node.Expression());
        }

        if (((Ast.IsJsxElement(node) || Ast.IsJsxSelfClosingElement(node)) || Ast.IsJsxFragment(node)))
        {
            this.setInChild(false);
            return this.Visitor().VisitNode(node);
        }

        throw new InvalidOperationException(("Unhandled node kind found in jsx initializer: " + node.KindString()));
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:550:1.
    private Node visitJsxOpeningLikeElementOrFragmentJSX(Node tagName, Node @object, Node keyAttr, NodeList children, TextRange location)
    {
        Slice<Node> nonWhitespaceChildren = default;
        if ((children != null))
        {
            nonWhitespaceChildren = Ast.GetSemanticJsxChildren(children.Nodes);
        }

        var isStaticChildren = ((GoRuntime.Len(nonWhitespaceChildren) > 1) || ((((GoRuntime.Len(nonWhitespaceChildren) == 1) && Ast.IsJsxExpression(nonWhitespaceChildren[0])) && (nonWhitespaceChildren[0].AsJsxExpression().DotDotDotToken != null))));
        var args = Slice<Node>.Make(0, 3);
        args = args.Append(tagName).Append(@object);
        if ((keyAttr != null))
        {
            args = args.Append(this.transformJsxAttributeInitializer(keyAttr.Initializer()));
        }

        if ((this.compilerOptions.Jsx == JsxEmit.ReactJSXDev))
        {
            var originalFile = this.EmitContext().MostOriginal(this.currentSourceFile.AsNode());
            if (((originalFile != null) && Ast.IsSourceFile(originalFile)))
            {
                if ((keyAttr == null))
                {
                    args = args.Append(this.Factory().NewVoidZeroExpression());
                }

                if (isStaticChildren)
                {
                    args = args.Append(this.Factory().NewTrueExpression());
                }
                else
                {
                    args = args.Append(this.Factory().NewFalseExpression());
                }

                var tuple15 = ScannerApi.GetECMALineAndUTF16CharacterOfPosition(originalFile.AsSourceFile(), location.Pos());
                var line = tuple15.Item1;
                var col = tuple15.Item2;
                args = args.Append(this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewPropertyAssignment(null, this.Factory().NewIdentifier("fileName"), null, null, this.getCurrentFileNameExpression()), this.Factory().NewPropertyAssignment(null, this.Factory().NewIdentifier("lineNumber"), null, null, this.Factory().NewNumericLiteral(Strconv.FormatInt((long)((line + 1)), 10), TokenFlags.None)), this.Factory().NewPropertyAssignment(null, this.Factory().NewIdentifier("columnNumber"), null, null, this.Factory().NewNumericLiteral(Strconv.FormatInt(((long)(col) + 1), 10), TokenFlags.None))])), false));
                args = args.Append(this.Factory().NewThisExpression());
            }
        }

        var element = this.Factory().NewCallExpression(this.getJsxFactoryCallee(isStaticChildren), null, null, this.Factory().NewNodeList(args), NodeFlags.None);
        element.Loc = location;
        if (this.inJsxChild)
        {
            this.EmitContext().AddEmitFlags(element, EmitFlags.StartOnNewLine);
        }

        return element;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:605:1.
    private Node visitJsxOpeningFragmentJSX(JsxOpeningFragment fragment, NodeList children, TextRange location)
    {
        Node childrenProps = default;
        if (((children != null) && (GoRuntime.Len(children.Nodes) > 0)))
        {
            var result = this.convertJsxChildrenToChildrenPropObject(children.Nodes);
            if ((result != null))
            {
                childrenProps = result;
            }
        }

        if ((childrenProps == null))
        {
            childrenProps = this.Factory().NewObjectLiteralExpression(this.Factory().NewNodeList(Slice<Node>.Make(0)), false);
        }

        return this.visitJsxOpeningLikeElementOrFragmentJSX(this.getImplicitJsxFragmentReference(), childrenProps, null, children, location);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:625:1.
    private Node createReactNamespace(ByteStringValue reactNamespace, Node parent)
    {
        if ((reactNamespace.Length == 0))
        {
            reactNamespace = ByteStringValue.FromString("React");
        }

        var react = this.Factory().NewIdentifierWithTextValue(reactNamespace);
        react.Flags &= ~(NodeFlags.Synthesized);
        react.Parent = this.EmitContext().ParseNode(parent);
        {
            var container = this.emitResolver.GetReferencedExportContainer(react, false);
            if (((container != null) && Ast.IsModuleDeclaration(container)))
            {
                var containerName = this.Factory().NewGeneratedNameForNode(container);
                return this.Factory().NewPropertyAccessExpression(containerName, null, react, NodeFlags.None);
            }
        }

        return react;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:651:1.
    private Node createJsxFactoryExpressionFromEntityName(Node e, Node parent)
    {
        if (Ast.IsQualifiedName(e))
        {
            var left = this.createJsxFactoryExpressionFromEntityName(e.AsQualifiedName().Left, parent);
            var right = this.Factory().NewIdentifierWithTextValue(e.AsQualifiedName().Right.TextValue());
            return this.Factory().NewPropertyAccessExpression(left, null, right, NodeFlags.None);
        }

        return this.createReactNamespace(e.TextValue(), parent);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:660:1.
    private Node createJsxPseudoFactoryExpression(Node parent, Node e, string target)
    {
        if ((e != null))
        {
            return this.createJsxFactoryExpressionFromEntityName(e, parent);
        }

        return this.Factory().NewPropertyAccessExpression(this.createReactNamespace(ByteStringValue.FromString(this.compilerOptions.ReactNamespace), parent), null, this.Factory().NewIdentifier(target), NodeFlags.None);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:672:1.
    private Node createJsxFactoryExpression(Node parent)
    {
        var e = this.emitResolver.GetJsxFactoryEntity(this.currentSourceFile.AsNode());
        return this.createJsxPseudoFactoryExpression(parent, e, "createElement");
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:677:1.
    private Node createJsxFragmentFactoryExpression(Node parent)
    {
        var e = this.emitResolver.GetJsxFragmentFactoryEntity(this.currentSourceFile.AsNode());
        return this.createJsxPseudoFactoryExpression(parent, e, "Fragment");
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:682:1.
    private Node visitJsxOpeningLikeElementCreateElement(Node element, NodeList children, TextRange location)
    {
        var tagName = this.getTagName(element);
        var attrs = element.Attributes().Properties();
        Node objectProperties = default;
        if ((GoRuntime.Len(attrs) > 0))
        {
            objectProperties = this.transformJsxAttributesToObjectProps(attrs, null);
        }
        else
        {
            objectProperties = this.Factory().NewKeywordExpression(Kind.NullKeyword);
        }

        Node callee = default;
        if ((this.importSpecifier.Length == 0))
        {
            callee = this.createJsxFactoryExpression(element);
        }
        else
        {
            callee = this.getImplicitImportForName("createElement");
        }

        Slice<Node> newChildren = default;
        if (((children != null) && (GoRuntime.Len(children.Nodes) > 0)))
        {
            {
                foreach (var c in GoRuntime.RangeValues(children.Nodes))
                {
                    var res = this.transformJsxChildToExpression(c);
                    if ((res != null))
                    {
                        newChildren = newChildren.Append(res);
                    }
                }
            }
        }

        if ((GoRuntime.Len(newChildren) > 1))
        {
            {
                foreach (var range17 in GoRuntime.Range(newChildren))
                {
                    var _2 = range17.Key;
                    var child = range17.Value;
                    this.EmitContext().AddEmitFlags(child, EmitFlags.StartOnNewLine);
                }
            }
        }

        var args = Slice<Node>.Make(0, newChildren.Length + 2);
        args = args.Append(tagName);
        args = args.Append(objectProperties);
        args = args.Append(newChildren);
        var result = this.Factory().NewCallExpression(callee, null, null, this.Factory().NewNodeList(args), NodeFlags.None);
        result.Loc = location;
        if (this.inJsxChild)
        {
            this.EmitContext().AddEmitFlags(result, EmitFlags.StartOnNewLine);
        }

        return result;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:736:1.
    private Node visitJsxOpeningFragmentCreateElement(JsxOpeningFragment fragment, NodeList children, TextRange location)
    {
        var tagName = this.createJsxFragmentFactoryExpression(fragment.AsNode());
        var callee = this.createJsxFactoryExpression(fragment.AsNode());
        Slice<Node> newChildren = default;
        if (((children != null) && (GoRuntime.Len(children.Nodes) > 0)))
        {
            {
                foreach (var c in GoRuntime.RangeValues(children.Nodes))
                {
                    var res = this.transformJsxChildToExpression(c);
                    if ((res != null))
                    {
                        newChildren = newChildren.Append(res);
                    }
                }
            }
        }

        if ((GoRuntime.Len(newChildren) > 1))
        {
            {
                foreach (var range19 in GoRuntime.Range(newChildren))
                {
                    var _2 = range19.Key;
                    var child = range19.Value;
                    this.EmitContext().AddEmitFlags(child, EmitFlags.StartOnNewLine);
                }
            }
        }

        var args = Slice<Node>.Make(0, newChildren.Length + 2);
        args = args.Append(tagName);
        args = args.Append(this.Factory().NewKeywordExpression(Kind.NullKeyword));
        args = args.Append(newChildren);
        var result = this.Factory().NewCallExpression(callee, null, null, this.Factory().NewNodeList(args), NodeFlags.None);
        result.Loc = location;
        if (this.inJsxChild)
        {
            this.EmitContext().AddEmitFlags(result, EmitFlags.StartOnNewLine);
        }

        return result;
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:777:1.
    private Node visitJsxText(JsxText text)
    {
        var fixedText = fixupWhitespaceAndDecodeEntities(text.TextValue());
        if ((fixedText.Length == 0))
        {
            return null;
        }

        return this.Factory().NewStringLiteralWithTextValue(fixedText, TokenFlags.None);
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:785:1.
    private static void addLineOfJsxText(GoBuilder b, ByteStringValue trimmedLine, bool isInitial)
    {
        var decoded = decodeEntities(trimmedLine);
        if (!isInitial)
        {
            b.WriteString(" ");
        }

        b.WriteString(decoded);
    }

    // *
    // * JSX trims whitespace at the end and beginning of lines, except that the
    // * start/end of a tag is considered a start/end of a line only if that line is
    // * on the same line as the closing tag. See examples in
    // * tests/cases/conformance/jsx/tsxReactEmitWhitespace.tsx
    // * See also https://www.w3.org/TR/html4/struct/text.html#h-9.1 and https://www.w3.org/TR/CSS2/text.html#white-space-model
    // *
    // * An equivalent algorithm would be:
    // * - If there is only one line, return it.
    // * - If there is only whitespace (but multiple lines), return `undefined`.
    // * - Split the text into lines.
    // * - 'trimRight' the first line, 'trimLeft' the last line, 'trim' middle lines.
    // * - Decode entities on each line (individually).
    // * - Remove empty lines and join the rest with " ".
    // 
    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:810:1.
    private static ByteStringValue fixupWhitespaceAndDecodeEntities(ByteStringValue text)
    {
        var acc = new GoBuilder
        {
        };
        var initial = true;
        var firstNonWhitespace = 0;
        var lastNonWhitespaceEnd = -1;
        {
            var i = 0;
            for (; (i < text.Length); i++)
            {
                var tuple20 = text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan[i..]) : Utf8.DecodeRuneInString(text.CharSpan[i..]);
                var c = tuple20.Item1;
                var size = tuple20.Item2;
                if (StringUtil.IsLineBreak(c))
                {
                    if (((firstNonWhitespace != -1) && (lastNonWhitespaceEnd != -1)))
                    {
                        addLineOfJsxText(acc, text.Slice(firstNonWhitespace, (lastNonWhitespaceEnd + 1)), initial);
                        initial = false;
                    }

                    firstNonWhitespace = -1;
                }
                else
                {
                    if (!StringUtil.IsWhiteSpaceSingleLine(c))
                    {
                        lastNonWhitespaceEnd = ((i + size) - 1);
                        if ((firstNonWhitespace == -1))
                        {
                            firstNonWhitespace = i;
                        }
                    }
                }

                if ((size > 1))
                {
                    i += ((size - 1));
                }
            }
        }

        if ((firstNonWhitespace != -1))
        {
            addLineOfJsxText(acc, text.Slice(firstNonWhitespace, text.Length), initial);
        }

        return acc.Value();
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:852:1.
    private Node visitJsxExpression(JsxExpression expression)
    {
        var e = this.Visitor().VisitNode(expression.Expression);
        if ((expression.DotDotDotToken != null))
        {
            return this.Factory().NewSpreadElement(e);
        }

        return e;
    }

    // *
    // * Replace entities like "&nbsp;", "&#123;", and "&#xDEADBEEF;" with the characters they encode.
    // * See https://en.wikipedia.org/wiki/List_of_XML_and_HTML_character_entity_references
    // 
    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:864:1.
    private static ByteStringValue decodeEntities(ByteStringValue text)
    {
        var i = text.IndexOf('&');
        if ((i < 0))
        {
            return text;
        }

        GoBuilder result = new GoBuilder();
        result.Grow(text.Length);
        while (true)
        {
            result.WriteString(text.Slice(0, i));
            text = text.Slice(i, text.Length);
            var semi = text.IndexOf(';');
            if ((semi < 0))
            {
                break;
            }

            while (true)
            {
                var nextAmp = text.Slice(1, semi).IndexOf('&');
                if ((nextAmp < 0))
                {
                    break;
                }

                result.WriteString(text.Slice(0, (nextAmp + 1)));
                text = text.Slice((nextAmp + 1), text.Length);
                semi -= (nextAmp + 1);
            }

            var entity = text.Slice(1, semi);
            var tuple21 = decodeEntity(entity);
            var decoded = tuple21.Item1;
            var ok = tuple21.Item2;
            if (ok)
            {
                result.WriteJSStringRune(decoded);
            }
            else
            {
                result.WriteString(text.Slice(0, (semi + 1)));
            }

            text = text.Slice((semi + 1), text.Length);
            i = text.IndexOf('&');
            if ((i < 0))
            {
                break;
            }
        }

        result.WriteString(text);
        return result.Value();
    }

    // Upstream tsc/internal/transformers/jsxtransforms/jsx.go:914:1.
    private static (int, bool) decodeEntity(ByteStringValue entity)
    {
        if ((entity.Length == 0))
        {
            return (0, false);
        }

        if ((entity[0] == '#'))
        {
            entity = entity.Slice(1, entity.Length);
            if ((entity.Length == 0))
            {
                return (0, false);
            }

            var @base = 10;
            if ((entity[0] == 'x'))
            {
                @base = 16;
                entity = entity.Slice(1, entity.Length);
            }

            if ((entity.Length == 0))
            {
                return (0, false);
            }

            {
                for (int offset = 0; offset < entity.Length;)
                {
                    var decodedRune = entity.IsBytes ? Utf8.DecodeRuneInString(entity.ByteSpan[offset..]) : Utf8.DecodeRuneInString(entity.CharSpan[offset..]);
                    int c = decodedRune.Rune;
                    offset += decodedRune.Size;
                    if (((@base == 16) && !StringUtil.IsHexDigit(c)))
                    {
                        return (0, false);
                    }

                    if (((@base == 10) && !StringUtil.IsDigit(c)))
                    {
                        return (0, false);
                    }
                }
            }

            var tuple23 = Strconv.ParseInt(entity, @base, 32);
            var parsed = tuple23.Item1;
            var err = tuple23.Item2;
            if (err)
            {
                return (0, false);
            }

            return ((int)(parsed), true);
        }

        var ok = entity.TryGetValue(entities, out var r);
        return (r, ok);
    }

    // jsx.go:954. Complete pinned HTML entity names and codepoints.
    private static readonly Dictionary<string, int> entities = new(StringComparer.Ordinal)
    {
        ["quot"] = 0x0022,
        ["amp"] = 0x0026,
        ["apos"] = 0x0027,
        ["lt"] = 0x003C,
        ["gt"] = 0x003E,
        ["nbsp"] = 0x00A0,
        ["iexcl"] = 0x00A1,
        ["cent"] = 0x00A2,
        ["pound"] = 0x00A3,
        ["curren"] = 0x00A4,
        ["yen"] = 0x00A5,
        ["brvbar"] = 0x00A6,
        ["sect"] = 0x00A7,
        ["uml"] = 0x00A8,
        ["copy"] = 0x00A9,
        ["ordf"] = 0x00AA,
        ["laquo"] = 0x00AB,
        ["not"] = 0x00AC,
        ["shy"] = 0x00AD,
        ["reg"] = 0x00AE,
        ["macr"] = 0x00AF,
        ["deg"] = 0x00B0,
        ["plusmn"] = 0x00B1,
        ["sup2"] = 0x00B2,
        ["sup3"] = 0x00B3,
        ["acute"] = 0x00B4,
        ["micro"] = 0x00B5,
        ["para"] = 0x00B6,
        ["middot"] = 0x00B7,
        ["cedil"] = 0x00B8,
        ["sup1"] = 0x00B9,
        ["ordm"] = 0x00BA,
        ["raquo"] = 0x00BB,
        ["frac14"] = 0x00BC,
        ["frac12"] = 0x00BD,
        ["frac34"] = 0x00BE,
        ["iquest"] = 0x00BF,
        ["Agrave"] = 0x00C0,
        ["Aacute"] = 0x00C1,
        ["Acirc"] = 0x00C2,
        ["Atilde"] = 0x00C3,
        ["Auml"] = 0x00C4,
        ["Aring"] = 0x00C5,
        ["AElig"] = 0x00C6,
        ["Ccedil"] = 0x00C7,
        ["Egrave"] = 0x00C8,
        ["Eacute"] = 0x00C9,
        ["Ecirc"] = 0x00CA,
        ["Euml"] = 0x00CB,
        ["Igrave"] = 0x00CC,
        ["Iacute"] = 0x00CD,
        ["Icirc"] = 0x00CE,
        ["Iuml"] = 0x00CF,
        ["ETH"] = 0x00D0,
        ["Ntilde"] = 0x00D1,
        ["Ograve"] = 0x00D2,
        ["Oacute"] = 0x00D3,
        ["Ocirc"] = 0x00D4,
        ["Otilde"] = 0x00D5,
        ["Ouml"] = 0x00D6,
        ["times"] = 0x00D7,
        ["Oslash"] = 0x00D8,
        ["Ugrave"] = 0x00D9,
        ["Uacute"] = 0x00DA,
        ["Ucirc"] = 0x00DB,
        ["Uuml"] = 0x00DC,
        ["Yacute"] = 0x00DD,
        ["THORN"] = 0x00DE,
        ["szlig"] = 0x00DF,
        ["agrave"] = 0x00E0,
        ["aacute"] = 0x00E1,
        ["acirc"] = 0x00E2,
        ["atilde"] = 0x00E3,
        ["auml"] = 0x00E4,
        ["aring"] = 0x00E5,
        ["aelig"] = 0x00E6,
        ["ccedil"] = 0x00E7,
        ["egrave"] = 0x00E8,
        ["eacute"] = 0x00E9,
        ["ecirc"] = 0x00EA,
        ["euml"] = 0x00EB,
        ["igrave"] = 0x00EC,
        ["iacute"] = 0x00ED,
        ["icirc"] = 0x00EE,
        ["iuml"] = 0x00EF,
        ["eth"] = 0x00F0,
        ["ntilde"] = 0x00F1,
        ["ograve"] = 0x00F2,
        ["oacute"] = 0x00F3,
        ["ocirc"] = 0x00F4,
        ["otilde"] = 0x00F5,
        ["ouml"] = 0x00F6,
        ["divide"] = 0x00F7,
        ["oslash"] = 0x00F8,
        ["ugrave"] = 0x00F9,
        ["uacute"] = 0x00FA,
        ["ucirc"] = 0x00FB,
        ["uuml"] = 0x00FC,
        ["yacute"] = 0x00FD,
        ["thorn"] = 0x00FE,
        ["yuml"] = 0x00FF,
        ["OElig"] = 0x0152,
        ["oelig"] = 0x0153,
        ["Scaron"] = 0x0160,
        ["scaron"] = 0x0161,
        ["Yuml"] = 0x0178,
        ["fnof"] = 0x0192,
        ["circ"] = 0x02C6,
        ["tilde"] = 0x02DC,
        ["Alpha"] = 0x0391,
        ["Beta"] = 0x0392,
        ["Gamma"] = 0x0393,
        ["Delta"] = 0x0394,
        ["Epsilon"] = 0x0395,
        ["Zeta"] = 0x0396,
        ["Eta"] = 0x0397,
        ["Theta"] = 0x0398,
        ["Iota"] = 0x0399,
        ["Kappa"] = 0x039A,
        ["Lambda"] = 0x039B,
        ["Mu"] = 0x039C,
        ["Nu"] = 0x039D,
        ["Xi"] = 0x039E,
        ["Omicron"] = 0x039F,
        ["Pi"] = 0x03A0,
        ["Rho"] = 0x03A1,
        ["Sigma"] = 0x03A3,
        ["Tau"] = 0x03A4,
        ["Upsilon"] = 0x03A5,
        ["Phi"] = 0x03A6,
        ["Chi"] = 0x03A7,
        ["Psi"] = 0x03A8,
        ["Omega"] = 0x03A9,
        ["alpha"] = 0x03B1,
        ["beta"] = 0x03B2,
        ["gamma"] = 0x03B3,
        ["delta"] = 0x03B4,
        ["epsilon"] = 0x03B5,
        ["zeta"] = 0x03B6,
        ["eta"] = 0x03B7,
        ["theta"] = 0x03B8,
        ["iota"] = 0x03B9,
        ["kappa"] = 0x03BA,
        ["lambda"] = 0x03BB,
        ["mu"] = 0x03BC,
        ["nu"] = 0x03BD,
        ["xi"] = 0x03BE,
        ["omicron"] = 0x03BF,
        ["pi"] = 0x03C0,
        ["rho"] = 0x03C1,
        ["sigmaf"] = 0x03C2,
        ["sigma"] = 0x03C3,
        ["tau"] = 0x03C4,
        ["upsilon"] = 0x03C5,
        ["phi"] = 0x03C6,
        ["chi"] = 0x03C7,
        ["psi"] = 0x03C8,
        ["omega"] = 0x03C9,
        ["thetasym"] = 0x03D1,
        ["upsih"] = 0x03D2,
        ["piv"] = 0x03D6,
        ["ensp"] = 0x2002,
        ["emsp"] = 0x2003,
        ["thinsp"] = 0x2009,
        ["zwnj"] = 0x200C,
        ["zwj"] = 0x200D,
        ["lrm"] = 0x200E,
        ["rlm"] = 0x200F,
        ["ndash"] = 0x2013,
        ["mdash"] = 0x2014,
        ["lsquo"] = 0x2018,
        ["rsquo"] = 0x2019,
        ["sbquo"] = 0x201A,
        ["ldquo"] = 0x201C,
        ["rdquo"] = 0x201D,
        ["bdquo"] = 0x201E,
        ["dagger"] = 0x2020,
        ["Dagger"] = 0x2021,
        ["bull"] = 0x2022,
        ["hellip"] = 0x2026,
        ["permil"] = 0x2030,
        ["prime"] = 0x2032,
        ["Prime"] = 0x2033,
        ["lsaquo"] = 0x2039,
        ["rsaquo"] = 0x203A,
        ["oline"] = 0x203E,
        ["frasl"] = 0x2044,
        ["euro"] = 0x20AC,
        ["image"] = 0x2111,
        ["weierp"] = 0x2118,
        ["real"] = 0x211C,
        ["trade"] = 0x2122,
        ["alefsym"] = 0x2135,
        ["larr"] = 0x2190,
        ["uarr"] = 0x2191,
        ["rarr"] = 0x2192,
        ["darr"] = 0x2193,
        ["harr"] = 0x2194,
        ["crarr"] = 0x21B5,
        ["lArr"] = 0x21D0,
        ["uArr"] = 0x21D1,
        ["rArr"] = 0x21D2,
        ["dArr"] = 0x21D3,
        ["hArr"] = 0x21D4,
        ["forall"] = 0x2200,
        ["part"] = 0x2202,
        ["exist"] = 0x2203,
        ["empty"] = 0x2205,
        ["nabla"] = 0x2207,
        ["isin"] = 0x2208,
        ["notin"] = 0x2209,
        ["ni"] = 0x220B,
        ["prod"] = 0x220F,
        ["sum"] = 0x2211,
        ["minus"] = 0x2212,
        ["lowast"] = 0x2217,
        ["radic"] = 0x221A,
        ["prop"] = 0x221D,
        ["infin"] = 0x221E,
        ["ang"] = 0x2220,
        ["and"] = 0x2227,
        ["or"] = 0x2228,
        ["cap"] = 0x2229,
        ["cup"] = 0x222A,
        ["int"] = 0x222B,
        ["there4"] = 0x2234,
        ["sim"] = 0x223C,
        ["cong"] = 0x2245,
        ["asymp"] = 0x2248,
        ["ne"] = 0x2260,
        ["equiv"] = 0x2261,
        ["le"] = 0x2264,
        ["ge"] = 0x2265,
        ["sub"] = 0x2282,
        ["sup"] = 0x2283,
        ["nsub"] = 0x2284,
        ["sube"] = 0x2286,
        ["supe"] = 0x2287,
        ["oplus"] = 0x2295,
        ["otimes"] = 0x2297,
        ["perp"] = 0x22A5,
        ["sdot"] = 0x22C5,
        ["lceil"] = 0x2308,
        ["rceil"] = 0x2309,
        ["lfloor"] = 0x230A,
        ["rfloor"] = 0x230B,
        ["lang"] = 0x2329,
        ["rang"] = 0x232A,
        ["loz"] = 0x25CA,
        ["spades"] = 0x2660,
        ["clubs"] = 0x2663,
        ["hearts"] = 0x2665,
        ["diams"] = 0x2666,
    };
}
