// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/taggedtemplate.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using NodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;
using TypeScript.Collections;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class TaggedTemplateTransformer : Transformer
{
    private SourceFile currentSourceFile;
    private Slice<Node> taggedTemplateStringDeclarations;
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go:22:1.
    internal static Transformer NewTaggedTemplateLiftRestrictionTransformer(TransformOptions opts)
    {
        var tx = new TaggedTemplateTransformer
        {
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go:27:1.
    private Node visit(Node node)
    {
        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsInvalidTemplateEscape) == 0))
        {
            return node;
        }

        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                return this.visitSourceFile(node.AsSourceFile());
            }

            case Kind.TaggedTemplateExpression:
            {
                return this.visitTaggedTemplateExpression(node.AsTaggedTemplateExpression());
            }

            default:
            {
                return this.Visitor().VisitEachChild(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go:41:1.
    private Node visitSourceFile(SourceFile node)
    {
        this.currentSourceFile = node;
        this.taggedTemplateStringDeclarations = default;
        var visited = this.Visitor().VisitEachChild(node.AsNode());
        if ((GoRuntime.Len(this.taggedTemplateStringDeclarations) > 0))
        {
            var visitedSourceFile = visited.AsSourceFile();
            var statements = GoSlices.Clip(visitedSourceFile.Statements.Nodes).Append(this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(this.taggedTemplateStringDeclarations), NodeFlags.None)));
            var stmtList = this.Factory().NewNodeList(statements);
            stmtList.Loc = node.Statements.Loc;
            visited = this.Factory().UpdateSourceFile(visitedSourceFile, stmtList, visitedSourceFile.EndOfFileToken);
        }

        this.EmitContext().AddEmitHelper(visited, this.EmitContext().ReadEmitHelpers());
        return visited;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go:67:1.
    private Node visitTaggedTemplateExpression(TaggedTemplateExpression node)
    {
        return this.processTaggedTemplateExpression(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go:71:1.
    private Node processTaggedTemplateExpression(TaggedTemplateExpression node)
    {
        var tag = this.Visitor().VisitNode(node.Tag);
        var template = node.Template;
        if (!hasInvalidEscape(template))
        {
            return this.Visitor().VisitEachChild(node.AsNode());
        }

        var f = this.Factory();
        var templateArguments = Slice<Node>.FromArray([null ]);
        Slice<Node> cookedStrings = default;
        Slice<Node> rawStrings = default;
        if (Ast.IsNoSubstitutionTemplateLiteral(template))
        {
            cookedStrings = cookedStrings.Append(createTemplateCooked(f, template.TemplateLiteralLikeData()));
            rawStrings = rawStrings.Append(getRawLiteral(f, template));
        }
        else
        {
            var te = template.AsTemplateExpression();
            cookedStrings = cookedStrings.Append(createTemplateCooked(f, te.Head.TemplateLiteralLikeData()));
            rawStrings = rawStrings.Append(getRawLiteral(f, te.Head));
            {
                foreach (var span in GoRuntime.RangeValues(te.TemplateSpans.Nodes))
                {
                    var ts = span.AsTemplateSpan();
                    cookedStrings = cookedStrings.Append(createTemplateCooked(f, ts.Literal.TemplateLiteralLikeData()));
                    rawStrings = rawStrings.Append(getRawLiteral(f, ts.Literal));
                    templateArguments = templateArguments.Append(this.Visitor().VisitNode(ts.Expression));
                }
            }
        }

        var helperCall = f.NewTemplateObjectHelper(f.NewArrayLiteralExpression(f.NewNodeList(cookedStrings), false), f.NewArrayLiteralExpression(f.NewNodeList(rawStrings), false));
        if (Ast.IsExternalModule(this.currentSourceFile))
        {
            var tempVar = f.NewUniqueName("templateObject");
            this.taggedTemplateStringDeclarations = this.taggedTemplateStringDeclarations.Append(f.NewVariableDeclaration(tempVar, null, null, null));
            templateArguments[0] = f.NewLogicalORExpression(tempVar, f.NewAssignmentExpression(tempVar, helperCall));
        }
        else
        {
            templateArguments[0] = helperCall;
        }

        var call = f.NewCallExpression(tag, null, null, f.NewNodeList(templateArguments), NodeFlags.None);
        call.Loc = node.Loc;
        return call;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go:128:1.
    private static Node createTemplateCooked(NodeFactory f, TemplateLiteralLikeNodeBase template)
    {
        if (((template.TemplateFlags & TokenFlags.IsInvalid) != 0))
        {
            return f.NewVoidZeroExpression();
        }

        return f.NewStringLiteralWithTextValue(template.AsNode().TextValue(), TokenFlags.None);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go:135:1.
    private static Node getRawLiteral(NodeFactory f, Node node)
    {
        var text = PrinterUtilities.templateRawTextValue(node);
        if ((text.Length == 0))
        {
            text = TypeScript.Scanner.Scanner.GetSourceTextOfNodeFromSourceFileValue(Ast.GetSourceFileOfNode(node), node, false);
            var isLast = ((node.Kind == Kind.NoSubstitutionTemplateLiteral) || (node.Kind == Kind.TemplateTail));
            var endLen = 2;
            if (isLast)
            {
                endLen = 1;
            }

            text = text.Slice(1, text.Length - endLen);
        }

        text = NormalizeNewlines(text);
        var result = f.NewStringLiteralWithTextValue(text, TokenFlags.None);
        result.Loc = node.Loc;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go:161:1.
    private static bool hasInvalidEscape(Node template)
    {
        if (Ast.IsNoSubstitutionTemplateLiteral(template))
        {
            return ((template.TemplateLiteralLikeData().TemplateFlags & TokenFlags.ContainsInvalidEscape) != 0);
        }

        var te = template.AsTemplateExpression();
        if (((te.Head.TemplateLiteralLikeData().TemplateFlags & TokenFlags.ContainsInvalidEscape) != 0))
        {
            return true;
        }

        {
            foreach (var span in GoRuntime.RangeValues(te.TemplateSpans.Nodes))
            {
                if (((span.AsTemplateSpan().Literal.TemplateLiteralLikeData().TemplateFlags & TokenFlags.ContainsInvalidEscape) != 0))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // taggedtemplate.go:14. Exact CRLF/CR normalization of raw Go bytes.
    private static ByteStringValue NormalizeNewlines(ByteStringValue text)
    {
        int first = text.IndexOf('\r');
        if (first < 0)
            return text;
        var builder = new GoBuilder();
        int start = 0;
        for (int i = first; i < text.Length; i++)
        {
            if (text[i] != '\r')
                continue;
            builder.WriteString(text.Slice(start, i));
            builder.WriteByte('\n');
            if (i + 1 < text.Length && text[i + 1] == '\n')
                i++;
            start = i + 1;
        }

        builder.WriteString(text.Slice(start));
        return builder.Value();
    }
}
