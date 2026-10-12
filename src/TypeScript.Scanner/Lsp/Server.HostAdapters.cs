// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/server.go:823–837,2438–2466. Ignored actual-owner draft.
using TypeScript.ContentMapper;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    internal void SetLocale(ByteStringValue localeString)
    {
        var newLocale = initLocale;
        if (!localeString.EqualsBytes("auto"u8))
        {
            var(parsed, ok) = TypeScript.Locale.Locales.Parse(localeString.Materialize()!);
            if (!ok)
                return;
            newLocale = parsed;
        }

        localeMu.EnterWriteLock();
        try
        {
            locale = newLocale;
        }
        finally
        {
            localeMu.ExitWriteLock();
        }
    }

    internal void SetCompilerOptionsForInferredProjects(ProtocolRequestContext context, CompilerOptions options)
    {
        compilerOptionsForInferredProjects = options;
        if (session is not null)
            session.DidChangeCompilerOptionsForInferredProjects(context, options);
    }

    internal (byte[]? Output, Exception? Error) NpmInstall(ProtocolRequestContext context, RootedDirectoryPath directory, Slice<ByteStringValue> args) => npmInstall!(context, directory.AsValue(), args);
    private ISpawner? ContentMapperSpawner() => spawn is null ? null : new SpawnerFunc(spawn);
    internal Action<ByteStringValue> ContentMapperLogger() => message =>
    {
        if (logger.IsTracing())
            logger.Info(message);
    };
}
