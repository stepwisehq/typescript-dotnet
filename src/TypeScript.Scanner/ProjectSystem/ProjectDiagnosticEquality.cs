// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem;
// projectcollectionbuilder.go:1340/1425 reflect.DeepEqual's concrete diagnostic closure.
// Inferred updates retain old Errors. API ToDiagnostic creates nil-file diagnostics;
// the original cache test supplies existing config Errors. SourceFile is therefore
// the same retained owner or nil on these callers, not a separately reconstructed AST.
internal static class ProjectDiagnosticEquality
{
    internal static bool Equal(Slice<Diagnostic> left, Slice<Diagnostic> right)
    {
        var visited = new HashSet<(Diagnostic Left, Diagnostic Right)>();
        return EqualSlice(left, right, visited);
    }

    private static bool EqualSlice(Slice<Diagnostic> left, Slice<Diagnostic> right, HashSet<(Diagnostic, Diagnostic)> visited)
    {
        if (left.IsNil != right.IsNil || left.Count != right.Count)
            return false;
        for (int i = 0; i < left.Count; i++)
            if (!EqualDiagnostic(left[i], right[i], visited))
                return false;
        return true;
    }

    private static bool EqualDiagnostic(Diagnostic? left, Diagnostic? right, HashSet<(Diagnostic, Diagnostic)> visited)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null)
            return false;
        if (!visited.Add((left, right)))
            return true;
        if (!ReferenceEquals(left.file, right.file) || left.loc != right.loc || left.code != right.code || left.category != right.category || left.source != right.source || left.messageText != right.messageText || left.messageKey != right.messageKey || left.reportsUnnecessary != right.reportsUnnecessary || left.reportsDeprecated != right.reportsDeprecated || left.skippedOnNoEmit != right.skippedOnNoEmit)
            return false;
        if (!EqualMessage(left.message, right.message))
            return false;
        if ((left.messageArgs == null) != (right.messageArgs == null))
            return false;
        if (left.messageArgs != null && !left.messageArgs.SequenceEqual(right.messageArgs!))
            return false;
        var a = left.repopulateInfo;
        var b = right.repopulateInfo;
        if (!ReferenceEquals(a, b) && (a == null || b == null || a.Kind != b.Kind || a.ModuleReference != b.ModuleReference || a.Mode != b.Mode || a.PackageName != b.PackageName))
            return false;
        return EqualSlice(left.messageChain, right.messageChain, visited) && EqualSlice(left.relatedInformation, right.relatedInformation, visited);
    }

    private static bool EqualMessage(Message? left, Message? right) => ReferenceEquals(left, right) || left != null && right != null && left.Code() == right.Code() && left.Category() == right.Category() && left.Key() == right.Key() && left.String() == right.String() && left.ReportsUnnecessary() == right.ReportsUnnecessary() && left.ReportsDeprecated() == right.ReportsDeprecated() && left.ElidedInCompatibilityPyramid() == right.ElidedInCompatibilityPyramid();
}
