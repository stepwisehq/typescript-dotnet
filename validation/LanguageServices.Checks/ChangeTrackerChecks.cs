// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/ls/change/trackerimpl_test.go, complete test function.
using TypeScript.LanguageServices.Change;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class ChangeTrackerChecks
{
    public static int RunAll()
    {
        Run();
        return 1;
    }

    public static void Run()
    {
        var position = new Position
        {
            Line = 1,
            Character = 2
        };
        var range = new TypeScript.Lsp.Protocol.Range
        {
            Start = position,
            End = position
        };
        var a = new TextEdit
        {
            Range = range,
            NewText = ByteStringValue.FromString("a")
        };
        var b = new TextEdit
        {
            Range = range,
            NewText = ByteStringValue.FromString("b")
        };
        if (!Tracker.textEditsConflict(a, b, true))
            throw new InvalidOperationException("different insertions at the same position across projections should conflict");
        if (Tracker.textEditsConflict(a, b, false))
            throw new InvalidOperationException("insertions at the same position within one projection should retain their existing ordering");
    }
}
