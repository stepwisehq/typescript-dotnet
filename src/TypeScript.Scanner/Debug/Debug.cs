// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/debug/debug.go: Fail and default-message FailBadSyntaxKind.
namespace TypeScript.Scanner;
internal static partial class Debug
{
    public static void Fail(string reason)
    {
        if (reason.Length == 0)
            reason = "Debug failure.";
        else
            reason = "Debug failure. " + reason;
        throw new InvalidOperationException(reason);
    }

    // Foundation callers use the original omitted-message arm; arbitrary fmt.Sprint overloads are excluded.
    public static void FailBadSyntaxKind(Node node)
    {
        var msg = "Unexpected node.";
        Fail($"{msg}\nNode {node.KindString()} was unexpected.");
    }
}
