// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original internal/project/ata/ata_test.go TestATA; all 22 sibling groups.
using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.LanguageServices;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectAtaChecks
{
    private const string Typings = "/home/src/Library/Caches/typescript";
    private sealed record AtaCase(string Name, Input[] Files, string[] Registry, Dictionary<string, string> Packages);
    public static int RunAll()
    {
        TestATA();
        return 1;
    }

    public static void TestATA()
    {
        // The candidate includes actual bundled libraries; source !bundled.Embedded skip does not apply.
        List<Exception>? failures = null;
        foreach (var row in Cases)
        {
            if (row.Name == "deduplicate from local @types packages")
            {
                Console.WriteLine("ORIGINAL_SKIP TestATA/deduplicate from local @types packages: Todo - implement removing local @types from include list");
                continue;
            }

            try
            {
                Run(row);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(row.Name, error));
            }
        }

        if (failures is not null)
            throw new AggregateException("original TestATA groups failed", failures);
    }

    private static void Require([DoesNotReturnIf(false)] bool condition, string message = "original ATA assertion failed")
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T actual, T expected) => Require(EqualityComparer<T>.Default.Equals(actual, expected), $"expected {expected}; actual {actual}");
    private static Dictionary<string, object?> Disabled(bool deprecated = false) => deprecated ? new()
    {
        ["typescript"] = new Dictionary<string, object?>
        {
            ["disableAutomaticTypeAcquisition"] = true
        }
    }

    : new()
    {
        ["js/ts"] = new Dictionary<string, object?>
        {
            ["tsserver"] = new Dictionary<string, object?>
            {
                ["automaticTypeAcquisition"] = new Dictionary<string, object?>
                {
                    ["enabled"] = false
                }
            }
        }
    };
    private static void Run(AtaCase row)
    {
        SessionOptions? options = row.Name == "ATA with WatchEnabled false should not panic" ? new()
        {
            CurrentDirectory = RootedDirectoryPath.FromGoString("/"),
            DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(),
            TypingsLocation = RootedDirectoryPath.FromGoString(Typings),
            PositionEncoding = PositionEncodingKind.UTF8,
            WatchEnabled = false,
            LoggingEnabled = true
        }

        : null;
        using var fixture = new ProjectSessionFixture(row.Files, options, row.Registry, row.Packages);
        var session = fixture.Session;
        var context = ProtocolRequestContext.Default;
        bool disabled = row.Name.StartsWith("ATA disabled via ", StringComparison.Ordinal), reenable = row.Name == "ATA re-enabled after being disabled triggers diagnostics refresh";
        if (disabled || reenable)
            session.Configure(LsUtil.ParseUserPreferences(Disabled(row.Name.EndsWith("deprecated setting", StringComparison.Ordinal))));
        string file = row.Name == "type acquisition with disableFilenameBasedTypeAcquisition:true" ? "/user/username/projects/project/jquery.js" : "/user/username/projects/project/app.js";
        var uri = new DocumentUri(ByteStringValue.FromString("file://" + file));
        string content = row.Files.First(input => input.Path == file).Contents;
        session.DidOpenFile(context, uri, 1, ByteStringValue.FromString(content), LanguageKind.JavaScript);
        session.WaitForBackgroundTasks();
        var calls = fixture.Npm.NpmInstallCalls();
        if (disabled || reenable)
        {
            Equal(calls.Length, 0);
            if (reenable)
            {
                int before = fixture.Client.RefreshDiagnosticsCallCount();
                session.Configure(LsUtil.ParseUserPreferences(new Dictionary<string, object?>()));
                session.WaitForBackgroundTasks();
                Require(fixture.Client.RefreshDiagnosticsCallCount() > before, "Expected RefreshDiagnostics call after ATA re-enabled");
            }

            return;
        }

        LanguageService Service()
        {
            var(service, error) = session.GetLanguageService(context, uri);
            Require(error is null, error?.Message ?? "");
            Require(service is not null);
            return service;
        }

        TypeScript.Compiler.Program? retainedProgram = null;
        TypeScript.Compiler.Program Program()
        {
            if (retainedProgram is not null)
                return retainedProgram;
            var program = Service().GetProgram();
            Require(program is not null);
            retainedProgram = program;
            return program;
        }

        void Installed(string name) => Require(Program().GetSourceFile(RootedFilePath.FromGoString(Typings + "/node_modules/@types/" + name + "/index.d.ts"))is not null, name + " types should be installed");
        void RegistryArguments() => Require(calls[0].Args.SequenceEqual(new[] { "install", "--ignore-scripts", "types-registry@latest" }));
        void Cwd(int i) => Equal(calls[i].Cwd, RootedDirectoryPath.FromGoString(Typings));
        switch (row.Name)
        {
            case "local module should not be picked up":
                Require(Program().GetSourceFile(RootedFilePath.FromGoString("/user/username/projects/project/config.js"))is not null, "local config.js should be included");
                Equal(calls.Length, 1);
                Equal(calls[0].Args[2], "types-registry@latest");
                break;
            case "configured projects":
                Equal(calls.Length, 2);
                Cwd(0);
                Equal(calls[0].Args[2], "types-registry@latest");
                Cwd(1);
                Require(calls[1].Args.Contains("@types/jquery@latest"));
                Equal(fixture.Client.RefreshDiagnosticsCallCount(), 1);
                break;
            case "inferred projects":
                Equal(calls.Length, 2);
                Cwd(0);
                RegistryArguments();
                Cwd(1);
                Equal(calls[1].Args[2], "@types/jquery@latest");
                Installed("jquery");
                break;
            case "type acquisition with disableFilenameBasedTypeAcquisition:true":
                Equal(calls.Length, 1);
                Cwd(0);
                RegistryArguments();
                break;
            case "discover from node_modules":
                Equal(calls.Length, 2);
                Cwd(0);
                RegistryArguments();
                Cwd(1);
                Equal(calls[1].Args[2], "@types/jquery@latest");
                break;
            case "discover from node_modules empty types":
            case "discover from node_modules explicit types":
                Equal(calls.Length, 1);
                RegistryArguments();
                break;
            case "discover from node_modules empty types has import":
                Equal(calls.Length, 2);
                RegistryArguments();
                Require(calls[1].Args.Contains("@types/jquery@latest"));
                break;
            case "discover from bower_components":
            case "discover from bower.json":
                Equal(calls.Length, 2);
                Cwd(0);
                RegistryArguments();
                Cwd(1);
                Equal(calls[1].Args[2], "@types/jquery@latest");
                Installed("jquery");
                break;
            case "Malformed package.json should be watched":
                Equal(calls.Length, 1);
                RegistryArguments();
                Require(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/user/username/projects/project/package.json"), "{ \"dependencies\": { \"commander\": \"0.0.2\" } }")is null);
                session.DidChangeWatchedFiles(context, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Changed, Uri = new DocumentUri(ByteStringValue.FromString("file:///user/username/projects/project/package.json")) }]));
                _ = session.GetLanguageService(context, uri);
                session.WaitForBackgroundTasks();
                calls = fixture.Npm.NpmInstallCalls();
                Equal(calls.Length, 2);
                Require(calls[1].Args.Contains("@types/commander@latest"));
                Installed("commander");
                break;
            case "should redo resolution that resolved to '.js' file after typings are installed":
                Equal(calls.Length, 2);
                Require(calls[1].Args.Contains("@types/commander@latest"));
                var program = Program();
                Require(program.GetSourceFile(RootedFilePath.FromGoString(Typings + "/node_modules/@types/commander/index.d.ts"))is not null);
                Require(program.GetSourceFile(RootedFilePath.FromGoString("/user/username/projects/node_modules/commander/index.js"))is null);
                break;
            case "expired cache entry (inferred project, should install typings)":
            case "expired cache entry (inferred project, should install typings) lockfile3":
                var expired = Program().GetSourceFile(RootedFilePath.FromGoString(Typings + "/node_modules/@types/jquery/index.d.ts"));
                Require(expired is not null);
                Require(ByteStringValue.FromView(expired.TextView()).EqualsString("export const y = 10"));
                break;
            case "non-expired cache entry (inferred project, should not install typings)":
            case "non-expired cache entry (inferred project, should not install typings) lockfile3":
                var cached = Program().GetSourceFile(RootedFilePath.FromGoString(Typings + "/node_modules/@types/jquery/index.d.ts"));
                Require(cached is not null);
                Require(ByteStringValue.FromView(cached.TextView()).EqualsString("export const x = 10;"));
                break;
            case "should install typings for unresolved imports":
                Equal(calls.Length, 2);
                Cwd(0);
                RegistryArguments();
                Cwd(1);
                Equal(calls[1].Args[0], "install");
                Equal(calls[1].Args[1], "--ignore-scripts");
                Require(calls[1].Args.Contains("@types/ember__component@latest"));
                Require(calls[1].Args.Contains("@types/commander@latest"));
                Require(calls[1].Args.Contains("@types/node@latest"));
                Installed("node");
                Installed("commander");
                Installed("ember__component");
                break;
            case "ATA with WatchEnabled false should not panic":
                Equal(calls.Length, 2);
                _ = Service();
                break;
            default:
                throw new InvalidOperationException("Unrecognized original ATA group: " + row.Name);
        }
    }
}
