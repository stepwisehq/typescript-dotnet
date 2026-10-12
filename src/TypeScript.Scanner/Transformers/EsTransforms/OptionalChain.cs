// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/optionalchain.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Collections;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class OptionalChainTransformer : Transformer
{
    private readonly record struct FlattenResult(Node expression, Slice<Node> chain);
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:14:1.
    private Node visit(Node node)
    {
        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsOptionalChaining) == 0))
        {
            return node;
        }

        switch (node.Kind)
        {
            case Kind.CallExpression:
            {
                return this.visitCallExpression(node.AsCallExpression(), false);
            }

            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                if (((node.Flags & NodeFlags.OptionalChain) != 0))
                {
                    return this.visitOptionalExpression(node, false, false);
                }

                return this.Visitor().VisitEachChild(node);
            }

            case Kind.DeleteExpression:
            {
                return this.visitDeleteExpression(node.AsDeleteExpression());
            }

            default:
            {
                return this.Visitor().VisitEachChild(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:34:1.
    private Node visitCallExpression(CallExpression node, bool captureThisArg)
    {
        if (((node.Flags & NodeFlags.OptionalChain) != 0))
        {
            return this.visitOptionalExpression(node.AsNode(), captureThisArg, false);
        }

        if (Ast.IsParenthesizedExpression(node.Expression))
        {
            var unwrapped = Ast.SkipParentheses(node.Expression);
            if (((unwrapped.Flags & NodeFlags.OptionalChain) != 0))
            {
                var expression = this.visitParenthesizedExpression(node.Expression.AsParenthesizedExpression(), true, false);
                var args = this.Visitor().VisitNodes(node.Arguments);
                if (Ast.IsSyntheticReferenceExpression(expression))
                {
                    var res = this.Factory().NewFunctionCallCall(expression.AsSyntheticReferenceExpression().Expression, expression.AsSyntheticReferenceExpression().ThisArg, args.Nodes);
                    res.Loc = node.Loc;
                    this.EmitContext().SetOriginal(res, node.AsNode());
                    return res;
                }

                return this.Factory().UpdateCallExpression(node, expression, null, null, args, node.Flags);
            }
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:57:1.
    private Node visitParenthesizedExpression(ParenthesizedExpression node, bool captureThisArg, bool isDelete)
    {
        var expr = this.visitNonOptionalExpression(node.Expression, captureThisArg, isDelete);
        if (Ast.IsSyntheticReferenceExpression(expr))
        {
            var synth = expr.AsSyntheticReferenceExpression();
            var res = this.Factory().NewSyntheticReferenceExpression(this.Factory().UpdateParenthesizedExpression(node, synth.Expression), synth.ThisArg);
            this.EmitContext().SetOriginal(res, node.AsNode());
            return res;
        }

        return this.Factory().UpdateParenthesizedExpression(node, expr);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:70:1.
    private Node visitPropertyOrElementAccessExpression(Node node, bool captureThisArg, bool isDelete)
    {
        if (((node.Flags & NodeFlags.OptionalChain) != 0))
        {
            return this.visitOptionalExpression(node.AsNode(), captureThisArg, isDelete);
        }

        var expression = this.Visitor().VisitNode(node.Expression());
        Debug.Assert(((expression == null) || !Ast.IsSyntheticReferenceExpression(expression)));
        Node thisArg = default;
        if (captureThisArg)
        {
            if (!Transformers.IsSimpleCopiableExpression(expression))
            {
                thisArg = this.Factory().NewTempVariable();
                this.EmitContext().AddVariableDeclaration(thisArg);
                expression = this.Factory().NewAssignmentExpression(thisArg, expression);
            }
            else
            {
                thisArg = expression;
            }
        }

        if ((node.Kind == Kind.PropertyAccessExpression))
        {
            var p = node.AsPropertyAccessExpression();
            expression = this.Factory().UpdatePropertyAccessExpression(p, expression, null, this.Visitor().VisitNode(p.Name()), p.Flags);
        }
        else
        {
            var p2 = node.AsElementAccessExpression();
            expression = this.Factory().UpdateElementAccessExpression(p2, expression, null, this.Visitor().VisitNode(p2.AsElementAccessExpression().ArgumentExpression), p2.Flags);
        }

        if ((thisArg != null))
        {
            var res = this.Factory().NewSyntheticReferenceExpression(expression, thisArg);
            this.EmitContext().SetOriginal(res, node.AsNode());
            return res;
        }

        return expression;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:105:1.
    private Node visitDeleteExpression(DeleteExpression node)
    {
        var unwrapped = Ast.SkipParentheses(node.Expression);
        if (((unwrapped.Flags & NodeFlags.OptionalChain) != 0))
        {
            return this.visitNonOptionalExpression(node.Expression, false, true);
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:113:1.
    private Node visitNonOptionalExpression(Node node, bool captureThisArg, bool isDelete)
    {
        switch (node.Kind)
        {
            case Kind.ParenthesizedExpression:
            {
                return this.visitParenthesizedExpression(node.AsParenthesizedExpression(), captureThisArg, isDelete);
            }

            case Kind.ElementAccessExpression:
            case Kind.PropertyAccessExpression:
            {
                return this.visitPropertyOrElementAccessExpression(node, captureThisArg, isDelete);
            }

            case Kind.CallExpression:
            {
                return this.visitCallExpression(node.AsCallExpression(), captureThisArg);
            }

            default:
            {
                return this.Visitor().VisitNode(node.AsNode());
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:131:1.
    private static bool isNonNullChain(Node node)
    {
        return (Ast.IsNonNullExpression(node) && ((node.Flags & NodeFlags.OptionalChain) != 0));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:135:1.
    private static FlattenResult flattenChain(Node chain)
    {
        Debug.Assert(!isNonNullChain(chain));
        var links = Slice<Node>.FromArray([chain]);
        while ((!Ast.IsTaggedTemplateExpression(chain) && (chain.QuestionDotToken() == null)))
        {
            chain = Ast.SkipPartiallyEmittedExpressions(chain.Expression());
            Debug.Assert(!isNonNullChain(chain));
            links = Slice<Node>.FromArray([chain]).Append(links);
        }

        return new FlattenResult(chain.Expression(), links);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:146:1.
    private static bool isCallChain(Node node)
    {
        return (Ast.IsCallExpression(node) && ((node.Flags & NodeFlags.OptionalChain) != 0));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:150:1.
    private Node visitOptionalExpression(Node node, bool captureThisArg, bool isDelete)
    {
        var r = flattenChain(node);
        var expression = r.expression;
        var chain = r.chain;
        var left = this.visitNonOptionalExpression(Ast.SkipPartiallyEmittedExpressions(expression), isCallChain(chain[0]), false);
        Node leftThisArg = default;
        var capturedLeft = left;
        if (Ast.IsSyntheticReferenceExpression(left))
        {
            leftThisArg = left.AsSyntheticReferenceExpression().ThisArg;
            capturedLeft = left.AsSyntheticReferenceExpression().Expression;
        }

        var leftExpression = this.Factory().RestoreOuterExpressions(expression, capturedLeft, OuterExpressionKinds.PartiallyEmittedExpressions);
        if (!Transformers.IsSimpleCopiableExpression(capturedLeft))
        {
            capturedLeft = this.Factory().NewTempVariable();
            this.EmitContext().AddVariableDeclaration(capturedLeft);
            leftExpression = this.Factory().NewAssignmentExpression(capturedLeft, leftExpression);
        }

        var rightExpression = capturedLeft;
        Node thisArg = default;
        {
            foreach (var range1 in GoRuntime.Range(chain))
            {
                var i = range1.Key;
                var segment = range1.Value;
                switch (segment.Kind)
                {
                    case Kind.ElementAccessExpression:
                    case Kind.PropertyAccessExpression:
                    {
                        if (((i == (GoRuntime.Len(chain) - 1)) && captureThisArg))
                        {
                            if (!Transformers.IsSimpleCopiableExpression(rightExpression))
                            {
                                thisArg = this.Factory().NewTempVariable();
                                this.EmitContext().AddVariableDeclaration(thisArg);
                                rightExpression = this.Factory().NewAssignmentExpression(thisArg, rightExpression);
                            }
                            else
                            {
                                thisArg = rightExpression;
                            }
                        }

                        if ((segment.Kind == Kind.ElementAccessExpression))
                        {
                            rightExpression = this.Factory().NewElementAccessExpression(rightExpression, null, this.Visitor().VisitNode(segment.AsElementAccessExpression().ArgumentExpression), NodeFlags.None);
                        }
                        else
                        {
                            rightExpression = this.Factory().NewPropertyAccessExpression(rightExpression, null, this.Visitor().VisitNode(segment.AsPropertyAccessExpression().Name()), NodeFlags.None);
                        }

                        break;
                    }

                    case Kind.CallExpression:
                    {
                        if (((i == 0) && (leftThisArg != null)))
                        {
                            if (!this.EmitContext().HasAutoGenerateInfo(leftThisArg))
                            {
                                leftThisArg = leftThisArg.Clone(this.Factory());
                                this.EmitContext().AddEmitFlags(leftThisArg, EmitFlags.NoComments);
                            }

                            var callThisArg = leftThisArg;
                            if ((leftThisArg.Kind == Kind.SuperKeyword))
                            {
                                callThisArg = this.Factory().NewThisExpression();
                            }

                            rightExpression = this.Factory().NewFunctionCallCall(rightExpression, callThisArg, this.Visitor().VisitNodes(segment.ArgumentList()).Nodes);
                        }
                        else
                        {
                            rightExpression = this.Factory().NewCallExpression(rightExpression, null, null, this.Visitor().VisitNodes(segment.ArgumentList()), NodeFlags.None);
                        }

                        break;
                    }
                }

                this.EmitContext().SetOriginal(rightExpression, segment);
            }
        }

        Node target = default;
        if (isDelete)
        {
            target = this.Factory().NewConditionalExpression(EsTransforms.CreateNotNullCondition(this.EmitContext(), leftExpression, capturedLeft, true), this.Factory().NewToken(Kind.QuestionToken), this.Factory().NewTrueExpression(), this.Factory().NewToken(Kind.ColonToken), this.Factory().NewDeleteExpression(rightExpression));
        }
        else
        {
            target = this.Factory().NewConditionalExpression(EsTransforms.CreateNotNullCondition(this.EmitContext(), leftExpression, capturedLeft, true), this.Factory().NewToken(Kind.QuestionToken), this.Factory().NewVoidZeroExpression(), this.Factory().NewToken(Kind.ColonToken), rightExpression);
        }

        target.Loc = node.Loc;
        if ((thisArg != null))
        {
            target = this.Factory().NewSyntheticReferenceExpression(target, thisArg);
        }

        this.EmitContext().SetOriginal(target, node.AsNode());
        return target;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go:237:1.
    internal static Transformer NewOptionalChainTransformer(TransformOptions opts)
    {
        var tx = new OptionalChainTransformer
        {
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }
}
