// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete inliners/constenum.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Transformers.Inliners;
internal sealed class ConstEnumInliningTransformer : Transformer
{
    private CompilerOptions compilerOptions;
    private IEmitResolver emitResolver;
    // Retained upstream state declaration; the pinned inliner never reads or assigns it.
#pragma warning disable CS0169
    private SourceFile currentSourceFile;
#pragma warning restore CS0169
    // constenum.go:22.
    internal static Transformer NewConstEnumInliningTransformer(TransformOptions options)
    {
        if (options.CompilerOptions.GetIsolatedModules())
            Debug.Fail("const enums are not inlined under isolated modules");
        var tx = new ConstEnumInliningTransformer
        {
            compilerOptions = options.CompilerOptions,
            emitResolver = options.EmitResolver
        };
        return tx.NewTransformer(tx.visit, options.Context);
    }

    // constenum.go:32.
    private Node visit(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                var parse = EmitContext().ParseNode(node);
                if (parse == null)
                    return Visitor().VisitEachChild(node);
                var value = emitResolver.GetConstantValue(parse);
                if (value != null)
                {
                    Node replacement = null;
                    switch (value)
                    {
                        case JsNum.Number number:
                            if (number.IsInf())
                            {
                                replacement = number.Abs() == number ? Factory().NewIdentifier("Infinity") : Factory().NewPrefixUnaryExpression(Kind.MinusToken, Factory().NewIdentifier("Infinity"));
                            }
                            else if (number.IsNaN())
                                replacement = Factory().NewIdentifier("NaN");
                            else if (number.Abs() == number)
                                replacement = Factory().NewNumericLiteral(number.String(), TokenFlags.None);
                            else
                                replacement = Factory().NewPrefixUnaryExpression(Kind.MinusToken, Factory().NewNumericLiteral(number.Abs().String(), TokenFlags.None));
                            break;
                        // Go string constants may retain the accepted evaluator byte header.
                        case ByteStringValue textValue:
                            replacement = Factory().NewStringLiteralWithTextValue(textValue, TokenFlags.None);
                            break;
                        case string text:
                            replacement = Factory().NewStringLiteral(text, TokenFlags.None);
                            break;
                        case JsNum.PseudoBigInt bigint:
                            if (bigint == default)
                                replacement = Factory().NewBigIntLiteral("0", TokenFlags.None);
                            else if (!bigint.Negative)
                                replacement = Factory().NewBigIntLiteral(bigint.Base10Value, TokenFlags.None);
                            else
                                replacement = Factory().NewPrefixUnaryExpression(Kind.MinusToken, Factory().NewBigIntLiteral(bigint.Base10Value, TokenFlags.None));
                            break;
                    }

                    if (compilerOptions.RemoveComments != Tristate.True)
                    {
                        var original = EmitContext().MostOriginal(node);
                        if (original != null && !Ast.NodeIsSynthesized(original))
                        {
                            var originalText = ScannerApi.GetTextOfNodeValue(original);
                            var escapedText = safeMultiLineComment(originalText);
                            EmitContext().AddSyntheticTrailingComment(replacement, Kind.MultiLineCommentTrivia, escapedText, false);
                        }
                    }

                    return replacement;
                }

                return Visitor().VisitEachChild(node);
            }
        }

        return Visitor().VisitEachChild(node);
    }

    // constenum.go:86. The comment is a required newly owned string; source inspection stays borrowed.
    private static string safeMultiLineComment(ByteStringValue text)
    {
        var builder = new GoBuilder();
        builder.Grow(text.Length + 2);
        builder.WriteByte(' ');
        while (true)
        {
            int index = -1;
            for (int i = 0; i + 1 < text.Length; i++)
            {
                if (text[i] == '*' && text[i + 1] == '/')
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                break;
            builder.WriteString(text.Slice(0, index));
            builder.WriteString("*_/");
            text = text.Slice(index + 2);
        }

        builder.WriteString(text);
        builder.WriteByte(' ');
        return builder.String();
    }
}
