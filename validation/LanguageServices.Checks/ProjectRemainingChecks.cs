// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Remaining original internal/project assertions, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Ignored assembly in progress; no original execution credit.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    public static readonly string[] Names = ["TestProjectProgramUpdateKind", "TestProject", "TestPushDiagnostics", "TestDisplayName", "TestProgressNotifications", "TestProjectCollectionBuilder", "TestProjectReferencesProgram", "TestAutoImportBuilderFS", "TestUpdateWatchTimeoutAndRollback"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestProjectProgramUpdateKind":
                TestProjectProgramUpdateKind();
                break;
            case "TestProject":
                TestProject();
                break;
            case "TestPushDiagnostics":
                TestPushDiagnostics();
                break;
            case "TestDisplayName":
                TestDisplayName();
                break;
            case "TestProgressNotifications":
                TestProgressNotifications();
                break;
            case "TestProjectCollectionBuilder":
                TestProjectCollectionBuilder();
                break;
            case "TestProjectReferencesProgram":
                TestProjectReferencesProgram();
                break;
            case "TestAutoImportBuilderFS":
                TestAutoImportBuilderFS();
                break;
            case "TestUpdateWatchTimeoutAndRollback":
                TestUpdateWatchTimeoutAndRollback();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException("Original Project assertion failed");
    }

    private static void Groups(params (string Name, Action Run)[] groups)
    {
        List<Exception> errors = new();
        foreach (var group in groups)
            try
            {
                group.Run();
            }
            catch (Exception error)
            {
                errors.Add(new InvalidOperationException(group.Name, error));
            }

        if (errors.Count != 0)
            throw new AggregateException(errors);
    }

    // project_test.go:779. Original parallel scheduling hints serialized; assertions unchanged.
    internal static void TestDisplayName()
    {
        static void Configured(string config, string source, string lookup, string expected)
        {
            using var fixture = new ProjectSessionFixture([new Input(config, "{}"), new Input(source, "export const x = 1;")]);
            var uri = new DocumentUri(ByteStringValue.FromString("file://" + source));
            fixture.Session.DidOpenFile(ProtocolRequestContext.Default, uri, 1, ByteStringValue.FromString("export const x = 1;"), LanguageKind.TypeScript);
            var(_, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, uri);
            Check(error is null);
            var project = fixture.Session.Snapshot().ProjectCollection.ConfiguredProject(PathKey.FromGoString(lookup));
            Check(project is not null);
            Check(project!.DisplayName(RootedDirectoryPath.FromGoString("/home/projects")).EqualsValue(ByteStringValue.FromString(expected)));
        }

        Groups(("configured project returns relative config path", () => Configured("/home/projects/tsconfig.json", "/home/projects/index.ts", "/home/projects/tsconfig.json", "tsconfig.json")), ("configured project with nested config", () => Configured("/home/projects/sub/tsconfig.json", "/home/projects/sub/index.ts", "/home/projects/sub/tsconfig.json", "sub/tsconfig.json")), ("configured project preserves config path casing", () => Configured("/home/projects/Project/tsconfig.json", "/home/projects/Project/index.ts", "/home/projects/project/tsconfig.json", "Project/tsconfig.json")), ("inferred project returns directory base name", () =>
        {
            using var fixture = new ProjectSessionFixture([new Input("/home/projects/index.ts", "export const x = 1;")], new SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString("/home/projects"), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = true, LoggingEnabled = true, PushDiagnosticsEnabled = true });
            var uri = new DocumentUri(ByteStringValue.FromString("file:///home/projects/index.ts"));
            fixture.Session.DidOpenFile(ProtocolRequestContext.Default, uri, 1, ByteStringValue.FromString("export const x = 1;"), LanguageKind.TypeScript);
            var(_, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, uri);
            Check(error is null);
            var project = fixture.Session.Snapshot().ProjectCollection.InferredProject();
            Check(project is not null);
            Check(project!.DisplayName(RootedDirectoryPath.FromGoString("/home")).EqualsValue(ByteStringValue.FromString("projects")));
        }));
    }

    // snapshotfs_test.go:864. Original symlink-cache deletion witness.
    internal static void TestAutoImportBuilderFS()
    {
        var fs = new ProjectTestFileSystem(CaseSensitivity.CaseSensitive, new Input("/real/pkg/index.d.ts", "export declare const x: number;"), new Input("/project/node_modules/pkg", Symlink: "/real/pkg"));
        var symlink = RootedFilePath.FromGoString("/project/node_modules/pkg/index.d.ts");
        var realpath = RootedFilePath.FromValue(fs.Realpath(symlink.AsPath()).AsValue());
        Check(realpath == RootedFilePath.FromGoString("/real/pkg/index.d.ts"));
        var builder = new SnapshotFSBuilder(new OverlayFS(fs, new(), PositionEncodingKind.UTF16), new(), new(), null);
        var autoImports = new AutoImportBuilderFS(builder);
        var file = autoImports.GetFile(symlink);
        Check(file is not null);
        Check(file!.ContentValue().EqualsValue(ByteStringValue.FromString("export declare const x: number;")));
        Check(fs.Remove(RootedPath.FromGoString("/real/pkg/index.d.ts"))is null);
        Check(autoImports.GetFile(realpath)is null);
    }
}
