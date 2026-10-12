// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshot_test.go setup: actual bundled MapFS/session, deliberately no client/npm host.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private sealed class Fixture : IDisposable
    {
        internal readonly ProjectTestFileSystem FS;
        internal readonly Session Session;
        internal Fixture(params BaselineFS.Input[] files)
        {
            FS = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, files);
            Session = Session.NewSession(new SessionInit { BackgroundContext = ProtocolRequestContext.Default, FS = TypeScript.Bundled.Bundled.WrapFS(FS), Options = new SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString("/"), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), TypingsLocation = RootedDirectoryPath.FromGoString("/home/src/Library/Caches/typescript"), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = false, LoggingEnabled = false } });
        }

        public void Dispose() => Session.Close();
    }

    private static void Check([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string assertion)
    {
        if (!condition)
            throw new InvalidOperationException("original snapshot assertion: " + assertion);
    }
}
