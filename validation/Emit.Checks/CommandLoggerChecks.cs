// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original execute/tsc/emit_test.go:39 TestContentMapperLoggerEnvironmentVariable.
using TypeScript.Compiler;
using TypeScript.Scanner;

namespace Emit.Checks;
public static class CommandLoggerChecks
{
    public static int RunAll()
    {
        bool enabled = false;
        using var stderr = new MemoryStream();
        string? EnvironmentVariable(string name) => name == "TS_CONTENT_MAPPER_DEBUG" && enabled ? "1" : null;
        if (ContentMapperCommandLogger.NewLogger(EnvironmentVariable, stderr)is not null)
            throw new InvalidOperationException("Disabled content mapper logger must be nil.");
        enabled = true;
        var logger = ContentMapperCommandLogger.NewLogger(EnvironmentVariable, stderr) ?? throw new InvalidOperationException("Enabled content mapper logger must be nonnil.");
        Parallel.For(0, 10, _ => logger("mapper log"));
        if (GoString.FromBytes(stderr.ToArray()) != string.Concat(Enumerable.Repeat("mapper log\n", 10)))
            throw new InvalidOperationException("Concurrent content mapper logging changed exact original lines.");
        return 1;
    }

    public static int RunAuthoredControls()
    {
        using var stderr = new MemoryStream();
        var logger = ContentMapperCommandLogger.NewLogger(_ => "1", stderr)!;
        logger(GoString.ToUtf8("café"));
        if (!stderr.ToArray().AsSpan().SequenceEqual("café\n"u8))
            throw new InvalidOperationException("Raw UTF8 logger message was encoded twice.");
        return 1;
    }
}
