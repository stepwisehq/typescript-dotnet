// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/ast/subtreefacts.go,
// upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
namespace TypeScript.Scanner;
[Flags]
public enum SubtreeFacts : uint
{
    ContainsTypeScript = 1u,
    ContainsJsx = 2u,
    ContainsESDecorators = 4u,
    ContainsUsing = 8u,
    ContainsClassStaticBlocks = 16u,
    ContainsESClassFields = 32u,
    ContainsLogicalAssignments = 64u,
    ContainsNullishCoalescing = 128u,
    ContainsOptionalChaining = 256u,
    ContainsMissingCatchClauseVariable = 512u,
    ContainsESObjectRestOrSpread = 1024u,
    ContainsForAwaitOrAsyncGenerator = 2048u,
    ContainsAnyAwait = 4096u,
    ContainsExponentiationOperator = 8192u,
    ContainsLexicalThis = 16384u,
    ContainsLexicalSuper = 32768u,
    ContainsRestOrSpread = 65536u,
    ContainsObjectRestOrSpread = 131072u,
    ContainsAwait = 262144u,
    ContainsDynamicImport = 524288u,
    ContainsClassFields = 1048576u,
    ContainsDecorators = 2097152u,
    ContainsIdentifier = 4194304u,
    ContainsPrivateIdentifierInExpression = 8388608u,
    ContainsInvalidTemplateEscape = 16777216u,
    FactsComputed = 33554432u,
    FactsNone = 0u,
    ContainsESNext = 12u,
    ContainsES2022 = 48u,
    ContainsES2021 = 64u,
    ContainsES2020 = 384u,
    ContainsES2019 = 512u,
    ContainsES2018 = 16780288u,
    ContainsES2017 = 4096u,
    ContainsES2016 = 8192u,
    ExclusionsNode = 33554432u,
    ExclusionsEraseable = 4294967294u,
    ExclusionsOuterExpression = 33554432u,
    ExclusionsPropertyAccess = 33554432u,
    ExclusionsElementAccess = 33554432u,
    ExclusionsArrowFunction = 33947648u,
    ExclusionsFunction = 33996800u,
    ExclusionsConstructor = 33996800u,
    ExclusionsMethod = 33996800u,
    ExclusionsAccessor = 33996800u,
    ExclusionsProperty = 33603584u,
    ExclusionsClass = 33554432u,
    ExclusionsModule = 33603584u,
    ExclusionsObjectLiteral = 33685504u,
    ExclusionsArrayLiteral = 33554432u,
    ExclusionsCall = 33554432u,
    ExclusionsNew = 33554432u,
    ExclusionsVariableDeclarationList = 33685504u,
    ExclusionsParameter = 33554432u,
    ExclusionsCatchClause = 33685504u,
    ExclusionsBindingPattern = 33619968u,
    ContainsLexicalThisOrSuper = 49152u,
}

internal static class SubtreeFactValues
{
    internal const uint ContainsTypeScript = 1u;
    internal const uint ContainsJsx = 2u;
    internal const uint ContainsESDecorators = 4u;
    internal const uint ContainsUsing = 8u;
    internal const uint ContainsClassStaticBlocks = 16u;
    internal const uint ContainsESClassFields = 32u;
    internal const uint ContainsLogicalAssignments = 64u;
    internal const uint ContainsNullishCoalescing = 128u;
    internal const uint ContainsOptionalChaining = 256u;
    internal const uint ContainsMissingCatchClauseVariable = 512u;
    internal const uint ContainsESObjectRestOrSpread = 1024u;
    internal const uint ContainsForAwaitOrAsyncGenerator = 2048u;
    internal const uint ContainsAnyAwait = 4096u;
    internal const uint ContainsExponentiationOperator = 8192u;
    internal const uint ContainsLexicalThis = 16384u;
    internal const uint ContainsLexicalSuper = 32768u;
    internal const uint ContainsRestOrSpread = 65536u;
    internal const uint ContainsObjectRestOrSpread = 131072u;
    internal const uint ContainsAwait = 262144u;
    internal const uint ContainsDynamicImport = 524288u;
    internal const uint ContainsClassFields = 1048576u;
    internal const uint ContainsDecorators = 2097152u;
    internal const uint ContainsIdentifier = 4194304u;
    internal const uint ContainsPrivateIdentifierInExpression = 8388608u;
    internal const uint ContainsInvalidTemplateEscape = 16777216u;
    internal const uint FactsComputed = 33554432u;
    internal const uint FactsNone = 0u;
    internal const uint ContainsESNext = 12u;
    internal const uint ContainsES2022 = 48u;
    internal const uint ContainsES2021 = 64u;
    internal const uint ContainsES2020 = 384u;
    internal const uint ContainsES2019 = 512u;
    internal const uint ContainsES2018 = 16780288u;
    internal const uint ContainsES2017 = 4096u;
    internal const uint ContainsES2016 = 8192u;
    internal const uint ExclusionsNode = 33554432u;
    internal const uint ExclusionsEraseable = 4294967294u;
    internal const uint ExclusionsOuterExpression = 33554432u;
    internal const uint ExclusionsPropertyAccess = 33554432u;
    internal const uint ExclusionsElementAccess = 33554432u;
    internal const uint ExclusionsArrowFunction = 33947648u;
    internal const uint ExclusionsFunction = 33996800u;
    internal const uint ExclusionsConstructor = 33996800u;
    internal const uint ExclusionsMethod = 33996800u;
    internal const uint ExclusionsAccessor = 33996800u;
    internal const uint ExclusionsProperty = 33603584u;
    internal const uint ExclusionsClass = 33554432u;
    internal const uint ExclusionsModule = 33603584u;
    internal const uint ExclusionsObjectLiteral = 33685504u;
    internal const uint ExclusionsArrayLiteral = 33554432u;
    internal const uint ExclusionsCall = 33554432u;
    internal const uint ExclusionsNew = 33554432u;
    internal const uint ExclusionsVariableDeclarationList = 33685504u;
    internal const uint ExclusionsParameter = 33554432u;
    internal const uint ExclusionsCatchClause = 33685504u;
    internal const uint ExclusionsBindingPattern = 33619968u;
    internal const uint ContainsLexicalThisOrSuper = 49152u;
}

public static partial class Ast
{
    internal static uint PropagateEraseableSyntaxListSubtreeFacts(NodeList children) => children is not null ? SubtreeFactValues.ContainsTypeScript : 0;
    internal static uint PropagateEraseableSyntaxSubtreeFacts(Node child) => child is not null ? SubtreeFactValues.ContainsTypeScript : 0;
    internal static uint PropagateObjectBindingElementSubtreeFacts(Node child)
    {
        var facts = PropagateSubtreeFacts(child);
        if ((facts & SubtreeFactValues.ContainsRestOrSpread) != 0)
        {
            facts &= ~SubtreeFactValues.ContainsRestOrSpread;
            facts |= SubtreeFactValues.ContainsObjectRestOrSpread | SubtreeFactValues.ContainsESObjectRestOrSpread;
        }

        return facts;
    }

    internal static uint PropagateBindingElementSubtreeFacts(Node child) => PropagateSubtreeFacts(child) & ~SubtreeFactValues.ContainsRestOrSpread;
    internal static uint PropagateSubtreeFacts(Node child) => child?.data.PropagateSubtreeFacts() ?? 0;
    internal static uint PropagateNodeListSubtreeFacts(NodeList children, Func<Node, uint> propagate)
    {
        uint facts = 0;
        if (children is not null)
            foreach (var child in children.Nodes)
                facts |= propagate(child);
        return facts;
    }

    internal static uint PropagateModifierListSubtreeFacts(ModifierList children) => PropagateNodeListSubtreeFacts(children, PropagateSubtreeFacts);
}
