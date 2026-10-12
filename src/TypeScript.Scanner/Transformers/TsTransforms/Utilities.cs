// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of tstransforms/utilities.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using Factory = TypeScript.DiagnosticPrinting.NodeFactory;
namespace TypeScript.Transformers.TsTransforms;
internal static class TSUtilities
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/utilities.go:9:1.
    internal static Node ConstantExpression(object value, Factory factory)
    {
        switch (value)
        {
            case string text:
                return factory.NewStringLiteral(text, TokenFlags.None);
            case ByteStringValue text: // Retained Go-string role; no projection at internal boundary.
                return factory.NewStringLiteralWithTextValue(text, TokenFlags.None);
            case JsNum.Number number:
                if (number.IsInf())
                {
                    if (number > 0) return factory.NewIdentifier("Infinity");
                    return factory.NewPrefixUnaryExpression(Kind.MinusToken, factory.NewIdentifier("Infinity"));
                }
                if (number.IsNaN()) return factory.NewIdentifier("NaN");
                if (number < 0) return factory.NewPrefixUnaryExpression(Kind.MinusToken, ConstantExpression(-number, factory));
                return factory.NewNumericLiteral(number.String(), TokenFlags.None);
        }
        return null;
    }
}
