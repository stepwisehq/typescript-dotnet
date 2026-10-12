// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/project/checkerpool_test.go assertions; fixture uses the genuine Session.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class ProjectCheckerPoolChecks
{
    private static (Session Session, ProjectCheckerPool Pool) Setup(CheckerPoolOptions options)
    {
        var fs = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new BaselineFS.Input("/src/tsconfig.json", "{ \"compilerOptions\": { \"noLib\": true } }"), new BaselineFS.Input("/src/index.ts", "export const x: number = 1;")));
        var session = Session.NewSession(new SessionInit { BackgroundContext = ProtocolRequestContext.Default, Options = new SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString("/"), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = false, LoggingEnabled = true, CheckerPoolOptions = options }, FS = fs, Logger = LogCollector.NewTestLogger() });
        session.DidOpenFile(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString("file:///src/index.ts")), 1, ByteStringValue.FromString("export const x: number = 1;"), LanguageKind.TypeScript);
        // Resolve the actual configured project; no substitute Program is created.
        var snapshot = session.Snapshot();
        var project = snapshot.ProjectCollection.ConfiguredProject(CaseSensitivity.CaseInsensitive.PathKey(RootedPath.FromGoString("/src/tsconfig.json")));
        if (project is null || project.checkerPool is null)
            throw new Exception("expected configured project and checker pool");
        return (session, project.checkerPool);
    }

    public static void TestCheckerPoolDiagnosticsRouting()
    {
        var(session, pool) = Setup(new(4, TimeSpan.FromSeconds(10)));
        try
        {
            var context = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("diag-req-1")).WithCheckerLifetime(CheckerLifetime.Diagnostics);
            var(checker, release) = pool.GetChecker(context, null);
            try
            {
                Require(checker is not null && ReferenceEquals(pool.checkers[0], checker), "diagnostics should use checker index 0");
            }
            finally
            {
                release();
            }
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolQueryRouting()
    {
        var(session, pool) = Setup(new(4, TimeSpan.FromSeconds(10)));
        try
        {
            var context = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("query-req-1")).WithCheckerLifetime(CheckerLifetime.Temporary);
            var(checker, release) = pool.GetChecker(context, null);
            try
            {
                Require(checker is not null && !ReferenceEquals(pool.checkers[0], checker), "query should not use checker index 0");
            }
            finally
            {
                release();
            }
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolRequestAffinity()
    {
        var(session, pool) = Setup(new(4, TimeSpan.FromSeconds(10)));
        using var cancellation = new CancellationTokenSource();
        try
        {
            var context = ProtocolRequestContext.Default.WithCancellationToken(cancellation.Token).WithRequestID(ByteStringValue.FromString("req-affinity")).WithCheckerLifetime(CheckerLifetime.Temporary);
            var(first, releaseFirst) = pool.GetChecker(context, null);
            releaseFirst();
            var(second, releaseSecond) = pool.GetChecker(context, null);
            releaseSecond();
            Require(ReferenceEquals(first, second), "same request ID should return the same checker after release");
        }
        finally
        {
            cancellation.Cancel();
            session.Close();
        }
    }

    public static void TestCheckerPoolAllThreeIndependent()
    {
        var(session, original) = Setup(new(4, TimeSpan.FromSeconds(10)));
        try
        {
            var(service, error) = session.GetLanguageService(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString("file:///src/index.ts")));
            Require(error is null && service is not null, "expected language service");
            var pool = new ProjectCheckerPool(new(4, TimeSpan.FromSeconds(30)), service!.GetProgram(), static _ =>
            {
            });
            var diagnostics = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("ded-req")).WithCheckerLifetime(CheckerLifetime.Diagnostics);
            var temporary = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("tmp-req")).WithCheckerLifetime(CheckerLifetime.Temporary);
            var api = ProtocolRequestContext.Default.WithCheckerLifetime(CheckerLifetime.API);
            var(dedicatedChecker, dedicatedRelease) = pool.GetChecker(diagnostics, null);
            var(temporaryChecker, temporaryRelease) = pool.GetChecker(temporary, null);
            var(apiChecker, apiRelease) = pool.GetChecker(api, null);
            try
            {
                Require(!ReferenceEquals(dedicatedChecker, temporaryChecker), "diagnostics and temporary should be different");
                Require(!ReferenceEquals(dedicatedChecker, apiChecker), "diagnostics and API should be different");
                Require(!ReferenceEquals(temporaryChecker, apiChecker), "temporary and API should be different");
            }
            finally
            {
                dedicatedRelease();
                temporaryRelease();
                apiRelease();
                pool.Discard();
            }
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolDefaultMaxCheckers()
    {
        var(session, pool) = Setup(new(0, TimeSpan.FromSeconds(10)));
        try
        {
            Require(pool.opts.MaxCheckers == 4, "zero MaxCheckers should default to 4");
            Require(pool.checkers.Length == 4, "expected four checker slots");
            Require(pool.querySem.CurrentCount == 3, "querySem capacity should be MaxCheckers-1");
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolGetGlobalDiagnosticsEmpty()
    {
        var(session, pool) = Setup(new(4, TimeSpan.FromSeconds(10)));
        try
        {
            Require(pool.GetGlobalDiagnostics().Length == 0, "global diagnostics should be empty initially");
        }
        finally
        {
            session.Close();
        }
    }

    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
