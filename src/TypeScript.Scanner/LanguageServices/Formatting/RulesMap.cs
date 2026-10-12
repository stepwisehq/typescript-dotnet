// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.LanguageServices.Formatting;
internal static partial class Formatting
{
    private const int MaskBitSize = 5, RuleMask = 31, MapRowLength = (int)Kind.LastToken + 1;
    // rulesmap.go:67. One immutable globally retained table, initialized once lazily.
    private static readonly Lazy<Slice<Slice<RuleImpl>>> RulesMap = new(BuildRulesMap, LazyThreadSafetyMode.ExecutionAndPublication);
    // rulesmap.go:10. Caller-owned result header appends accepted rules in bucket priority order.
    internal static Slice<RuleImpl> GetRules(FormattingContext context, Slice<RuleImpl> rules)
    {
        var bucket = RulesMap.Value[GetRuleBucketIndex(context.currentTokenSpan.Kind, context.nextTokenSpan.Kind)];
        RuleAction actionMask = RuleAction.None;
        foreach (var rule in bucket)
        {
            if ((rule.Action() & ~GetRuleActionExclusion(actionMask)) == 0)
                continue;
            bool accepted = true;
            foreach (var predicate in rule.Context())
                if (!predicate(context))
                {
                    accepted = false;
                    break;
                }

            if (!accepted)
                continue;
            rules = rules.Append(rule);
            actionMask |= rule.Action();
        }

        return rules;
    }

    // rulesmap.go:34.
    private static int GetRuleBucketIndex(Kind row, Kind column)
    {
        Debug.Assert(row <= Kind.LastKeyword && column <= Kind.LastKeyword, "Must compute formatting context from tokens");
        return (int)row * MapRowLength + (int)column;
    }

    // rulesmap.go:49.
    private static RuleAction GetRuleActionExclusion(RuleAction action)
    {
        RuleAction mask = RuleAction.None;
        if ((action & RuleAction.StopProcessingSpaceActions) != 0)
            mask |= RuleAction.ModifySpaceAction;
        if ((action & RuleAction.StopProcessingTokenActions) != 0)
            mask |= RuleAction.ModifyTokenAction;
        if ((action & RuleAction.ModifySpaceAction) != 0)
            mask |= RuleAction.ModifySpaceAction;
        if ((action & RuleAction.ModifyTokenAction) != 0)
            mask |= RuleAction.ModifyTokenAction;
        return mask;
    }

    // rulesmap.go:69. Nil inner buckets are preserved until their first insertion.
    private static Slice<Slice<RuleImpl>> BuildRulesMap()
    {
        var map = Slice<Slice<RuleImpl>>.Make(MapRowLength * MapRowLength);
        var state = Slice<int>.Make(map.Count);
        foreach (var rule in GetAllRules())
            foreach (var left in rule.LeftTokenRange.Tokens)
                foreach (var right in rule.RightTokenRange.Tokens)
                {
                    int index = GetRuleBucketIndex(left, right);
                    map[index] = AddRule(map[index], rule.Rule, rule.LeftTokenRange.IsSpecific && rule.RightTokenRange.IsSpecific, state, index);
                }

        return map;
    }

    // rulesmap.go:113. Six packed five-bit insertion counters preserve the original priority.
    private static Slice<RuleImpl> AddRule(Slice<RuleImpl> rules, RuleImpl rule, bool specific, Slice<int> constructionState, int bucket)
    {
        int position = (rule.Action() & RuleAction.StopAction) != 0 ? (specific ? 0 : 5) : rule.Context().Count != 0 ? (specific ? 10 : 15) : (specific ? 20 : 25);
        int state = constructionState[bucket];
        rules = rules.Insert(GetRuleInsertionIndex(state, position), rule);
        constructionState[bucket] = IncreaseInsertionIndex(state, position);
        return rules;
    }

    // rulesmap.go:142.
    private static int GetRuleInsertionIndex(int bitmap, int position)
    {
        int index = 0;
        for (int pos = 0; pos <= position; pos += MaskBitSize)
        {
            index += bitmap & RuleMask;
            bitmap >>= MaskBitSize;
        }

        return index;
    }

    // rulesmap.go:151.
    private static int IncreaseInsertionIndex(int bitmap, int position)
    {
        int value = ((bitmap >> position) & RuleMask) + 1;
        Debug.Assert((value & RuleMask) == value, "Adding more rules into the sub-bucket than allowed. Maximum allowed is 32 rules.");
        return (bitmap & ~(RuleMask << position)) | (value << position);
    }
}
