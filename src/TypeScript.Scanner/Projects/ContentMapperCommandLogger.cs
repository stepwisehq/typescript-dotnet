// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of internal/execute/tsc/compile.go:37 newContentMapperLogger.
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal static class ContentMapperCommandLogger
{
    // The environment/writer arguments adapt the two consumed System methods. Messages retain raw Go byte strings;
    // the stream receives those bytes without UTF16/UTF8 double encoding.
    // External process mapper hosting remains a separate environment boundary.
    internal static Action<string>? NewLogger(Func<string, string?> getEnvironmentVariable, Stream errorWriter)
    {
        if (string.IsNullOrEmpty(getEnvironmentVariable("TS_CONTENT_MAPPER_DEBUG")))
            return null;
        var gate = new object ();
        return message =>
        {
            lock (gate)
            {
                errorWriter.Write(GoString.Bytes(message));
                errorWriter.WriteByte((byte)'\n');
            }
        };
    }
}
