// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/subtreefacts.go; private JSX-only fact projection.
// Every non-JSX bit is projected to zero; discard object-rest-only calculation.
#nullable disable
namespace TypeScript.Scanner;
internal static class JsxProjection
{
    public const uint ContainsTypeScript = 0u;
    public const uint ContainsJsx = 2u;
    public const uint ContainsESDecorators = 0u;
    public const uint ContainsUsing = 0u;
    public const uint ContainsClassStaticBlocks = 0u;
    public const uint ContainsESClassFields = 0u;
    public const uint ContainsLogicalAssignments = 0u;
    public const uint ContainsNullishCoalescing = 0u;
    public const uint ContainsOptionalChaining = 0u;
    public const uint ContainsMissingCatchClauseVariable = 0u;
    public const uint ContainsESObjectRestOrSpread = 0u;
    public const uint ContainsForAwaitOrAsyncGenerator = 0u;
    public const uint ContainsAnyAwait = 0u;
    public const uint ContainsExponentiationOperator = 0u;
    public const uint ContainsLexicalThis = 0u;
    public const uint ContainsLexicalSuper = 0u;
    public const uint ContainsRestOrSpread = 0u;
    public const uint ContainsObjectRestOrSpread = 0u;
    public const uint ContainsAwait = 0u;
    public const uint ContainsDynamicImport = 0u;
    public const uint ContainsClassFields = 0u;
    public const uint ContainsDecorators = 0u;
    public const uint ContainsIdentifier = 0u;
    public const uint ContainsPrivateIdentifierInExpression = 0u;
    public const uint ContainsInvalidTemplateEscape = 0u;
    public const uint FactsComputed = 0u;
    public const uint FactsNone = 0u;
    public const uint ContainsESNext = 0u;
    public const uint ContainsES2022 = 0u;
    public const uint ContainsES2021 = 0u;
    public const uint ContainsES2020 = 0u;
    public const uint ContainsES2019 = 0u;
    public const uint ContainsES2018 = 0u;
    public const uint ContainsES2017 = 0u;
    public const uint ContainsES2016 = 0u;
    public const uint ExclusionsNode = 0u;
    public const uint ExclusionsEraseable = uint.MaxValue;
    public const uint ExclusionsOuterExpression = 0u;
    public const uint ExclusionsPropertyAccess = 0u;
    public const uint ExclusionsElementAccess = 0u;
    public const uint ExclusionsArrowFunction = 0u;
    public const uint ExclusionsFunction = 0u;
    public const uint ExclusionsConstructor = 0u;
    public const uint ExclusionsMethod = 0u;
    public const uint ExclusionsAccessor = 0u;
    public const uint ExclusionsProperty = 0u;
    public const uint ExclusionsClass = 0u;
    public const uint ExclusionsModule = 0u;
    public const uint ExclusionsObjectLiteral = 0u;
    public const uint ExclusionsArrayLiteral = 0u;
    public const uint ExclusionsCall = 0u;
    public const uint ExclusionsNew = 0u;
    public const uint ExclusionsVariableDeclarationList = 0u;
    public const uint ExclusionsParameter = 0u;
    public const uint ExclusionsCatchClause = 0u;
    public const uint ExclusionsBindingPattern = 0u;
    public const uint ContainsLexicalThisOrSuper = 0u;
}

public static partial class Ast
{
    internal static uint JsxPropagate(Node child) => child?.data.PropagateJsxFacts() ?? 0u;
    internal static uint JsxListFacts(NodeList children, Func<Node, uint> propagate)
    {
        uint facts = 0;
        if (children?.Nodes is not null)
            foreach (var child in children.Nodes)
                facts |= propagate(child);
        return facts;
    }

    internal static uint JsxModifierFacts(ModifierList children) => JsxListFacts(children, JsxPropagate);
    internal static uint JsxEraseableListFacts(NodeList children) => 0;
    internal static uint JsxEraseableFacts(Node child) => 0;
}
