// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices.Formatting;
internal delegate bool ContextPredicate(FormattingContext context);
// internal/format/rule.go:5. Actual pointer-owned rule and value token/spec descriptors.
internal sealed class RuleImpl
{
    internal string debugName = "";
    internal Slice<ContextPredicate> context;
    internal RuleAction action;
    internal RuleFlags flags;
    // rule.go:13/17/21/25.
    internal RuleAction Action() => action;
    internal Slice<ContextPredicate> Context() => context;
    internal RuleFlags Flags() => flags;
    public override string ToString() => debugName;
}

internal readonly record struct TokenRange(Slice<Kind> Tokens, bool IsSpecific);
internal readonly record struct RuleSpec(TokenRange LeftTokenRange, TokenRange RightTokenRange, RuleImpl Rule);
internal static partial class Formatting
{
    internal static readonly Slice<ContextPredicate> AnyContext = Slice<ContextPredicate>.Make(0);
    // rule.go:51. Typed overloads preserve original kind/slice/range ownership.
    internal static RuleSpec Rule(string name, object left, object right, Slice<ContextPredicate> context, RuleAction action, RuleFlags flags = RuleFlags.None) => new(ToTokenRange(left), ToTokenRange(right), new() { debugName = name, context = context, action = action, flags = flags });
    // rule.go:72. Unknown token range type is a real contract failure.
    private static TokenRange ToTokenRange(object value) => value switch
    {
        Kind kind => new(Slice<Kind>.FromArray([kind]), true),
        Slice<Kind> kinds => new(kinds, true),
        TokenRange range => range,
        _ => throw new InvalidOperationException("Unknown argument type passed to toTokenRange - only ast.Kind, []ast.Kind, and tokenRange supported")};
}

[Flags]
internal enum RuleAction
{
    None = 0,
    StopProcessingSpaceActions = 1 << 0,
    StopProcessingTokenActions = 1 << 1,
    InsertSpace = 1 << 2,
    InsertNewLine = 1 << 3,
    DeleteSpace = 1 << 4,
    DeleteToken = 1 << 5,
    InsertTrailingSemicolon = 1 << 6,
    StopAction = StopProcessingSpaceActions | StopProcessingTokenActions,
    ModifySpaceAction = InsertSpace | InsertNewLine | DeleteSpace,
    ModifyTokenAction = DeleteToken | InsertTrailingSemicolon
}

internal enum RuleFlags
{
    None,
    CanDeleteNewLines
}
