// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Separately authored depth observations matched by the pinned-Go additive witness.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    public static void RunModelDepthBoundary(int depth)
    {
        const string symbol = "{\"name\":\"s\",\"kind\":1,\"range\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":0}},\"selectionRange\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":0}}";
        var input = new System.Text.StringBuilder();
        for (int index = 1; index < depth; index++)
            input.Append(symbol).Append(",\"children\":[");
        input.Append(symbol).Append('}');
        for (int index = 1; index < depth; index++)
            input.Append("]}");
        DocumentSymbol result = new();
        ProtocolCodec.Unmarshal(ProtocolCodec.GetType_7a5e3c27bb3a5284(), Bytes(input.ToString()), ref result);
        int count = 1;
        var current = result;
        while (current.Children is { Length: > 0 } children)
        {
            current = children[0]!;
            count++;
        }

        Require(count == depth, "recursive model depth changed");
    }

    public static void RunAnyDepthBoundary(int depth)
    {
        string input = new string ('[', depth) + "0" + new string (']', depth);
        object? result = null;
        ProtocolCodec.Unmarshal(AnyDescriptor, Bytes(input), ref result);
        int count = 0;
        while (result is List<object?> array)
        {
            Require(array.Count == 1, "recursive any array length changed");
            result = array[0];
            count++;
        }

        Require(count == depth, "recursive any depth changed");
    }

    public static int RunDepthBoundaryControls()
    {
        foreach (int depth in new[]
        {
            16,
            256,
            2048,
            4998
        }

        )
            RunModelDepthBoundary(depth);
        foreach (int depth in new[]
        {
            16,
            256,
            2048,
            10000
        }

        )
            RunAnyDepthBoundary(depth);
        return 8;
    }
}
