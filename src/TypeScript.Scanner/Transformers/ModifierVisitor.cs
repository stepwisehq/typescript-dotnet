// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/transformers/modifiervisitor.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace TypeScript.Transformers;
internal sealed class modifierVisitor : Transformer
{
    internal ModifierFlags AllowedModifiers;
    private Node visit(Node node)
    {
        var flags = Ast.ModifierToFlag(node.Kind);
        return flags != ModifierFlags.None && (flags & AllowedModifiers) == 0 ? null : node;
    }
    internal void Initialize(EmitContext context) => NewTransformer(visit, context);
}
internal static partial class Transformers
{
    internal static ModifierList ExtractModifiers(EmitContext emitContext, ModifierList modifiers, ModifierFlags allowed)
    {
        if (modifiers == null) return null;
        var transformer = new modifierVisitor { AllowedModifiers = allowed };
        transformer.Initialize(emitContext); return transformer.Visitor().VisitModifiers(modifiers);
    }
}
