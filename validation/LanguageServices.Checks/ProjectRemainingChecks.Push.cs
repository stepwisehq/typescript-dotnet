// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Whole internal/project/project_test.go:348–778; original sibling assertions.
using System.Text;
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.Utilities;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;
using LspDiagnostic = TypeScript.Lsp.Protocol.Diagnostic;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    private static ProjectSessionFixture PushFixture(string config, string source = "export const x = 1;") => new([new Input("/src/tsconfig.json", config), new Input("/src/index.ts", source)]);
    private static void OpenAndDrain(ProjectSessionFixture fixture, string path = "/src/index.ts", string source = "export const x = 1;")
    {
        Open(fixture, path, source);
        _ = Service(fixture, path);
        fixture.Session.WaitForBackgroundTasks();
    }

    private static PublishDiagnosticsParams[] Published(ProjectSessionFixture fixture, string path, int start = 0) => fixture.Client.PublishDiagnosticsCalls().Skip(start).Where(call => call.Parameters.Uri == Uri(path)).Select(call => call.Parameters).ToArray();
    private static bool HasMessage(Slice<LspDiagnostic?> diagnostics, string text)
    {
        foreach (var diagnostic in diagnostics)
            if (diagnostic!.Message.AsString().IndexOf(ByteStringValue.FromString(text)) >= 0)
                return true;
        return false;
    }

    internal static void TestPushDiagnostics()
    {
        const string invalidTarget = "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"target\": \"nope\"\n\t\t\t\t}\n\t\t\t}";
        Groups(("publishes program diagnostics on initial program creation", () =>
        {
            using var fixture = PushFixture("{\"compilerOptions\": {\"baseUrl\": \".\"}}");
            OpenAndDrain(fixture);
            var calls = fixture.Client.PublishDiagnosticsCalls();
            Check(calls.Length > 0);
            var config = Published(fixture, "/src/tsconfig.json");
            Check(config.Length > 0);
            Check(config[0].Diagnostics.Length > 0);
        }), ("publishes config parsing diagnostics on initial program creation", () =>
        {
            using var fixture = PushFixture(invalidTarget);
            OpenAndDrain(fixture);
            var calls = Published(fixture, "/src/tsconfig.json");
            Check(calls.Length > 0);
            Check(HasMessage(calls[^1].Diagnostics, "Argument for '--target' option must be:"));
        }), ("clears diagnostics when project is removed", () =>
        {
            using var fixture = new ProjectSessionFixture([new Input("/src/tsconfig.json", "{\"compilerOptions\": {\"baseUrl\": \".\"}}"), new Input("/src/index.ts", "export const x = 1;"), new Input("/src2/tsconfig.json", "{\"compilerOptions\": {}}"), new Input("/src2/index.ts", "export const y = 2;"), ]);
            OpenAndDrain(fixture);
            fixture.Session.DidCloseFile(ProtocolRequestContext.Default, Uri("/src/index.ts"));
            OpenAndDrain(fixture, "/src2/index.ts", "export const y = 2;");
            var calls = Published(fixture, "/src/tsconfig.json");
            Check(calls.Length >= 2);
            Check(calls[^1].Diagnostics.Length == 0);
        }), ("updates diagnostics when program changes", () =>
        {
            using var fixture = PushFixture("{\"compilerOptions\": {\"baseUrl\": \".\"}}");
            OpenAndDrain(fixture);
            int before = fixture.Client.PublishDiagnosticsCalls().Length;
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/src/tsconfig.json"), "{\"compilerOptions\": {}}")is null);
            WatchChange(fixture, "/src/tsconfig.json", FileChangeType.Changed);
            _ = Service(fixture, "/src/index.ts");
            fixture.Session.WaitForBackgroundTasks();
            Check(fixture.Client.PublishDiagnosticsCalls().Length > before);
            var calls = Published(fixture, "/src/tsconfig.json");
            Check(calls.Length > 0);
            Check(calls[^1].Diagnostics.Length == 0);
        }), ("updates diagnostics when a config file changes on disk with no follow-up request", () =>
        {
            using var fixture = PushFixture("{\"compilerOptions\": {}}");
            OpenAndDrain(fixture);
            int before = fixture.Client.PublishDiagnosticsCalls().Length;
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/src/tsconfig.json"), "{\"compilerOptions\": {\"target\": \"nope\"}}")is null);
            WatchChange(fixture, "/src/tsconfig.json", FileChangeType.Changed);
            // Original deliberately has no follow-up language-service request.
            fixture.Session.WaitForBackgroundTasks();
            var calls = Published(fixture, "/src/tsconfig.json", before);
            Check(calls.Length > 0);
            Check(HasMessage(calls[^1].Diagnostics, "Argument for '--target' option must be:"));
        }), ("does not publish for inferred projects", () =>
        {
            const string source = "let x: number = 'not a number';";
            using var fixture = new ProjectSessionFixture([new Input("/src/index.ts", source)]);
            OpenAndDrain(fixture, source: source);
            Check(fixture.Client.PublishDiagnosticsCalls().Length == 0);
        }), ("does not publish when validation is disabled", () =>
        {
            using var fixture = PushFixture(invalidTarget);
            var preferences = LsUtil.NewDefaultUserPreferences();
            preferences.ValidateEnabled = Tristate.False;
            fixture.Session.Configure(preferences);
            OpenAndDrain(fixture);
            foreach (var call in fixture.Client.PublishDiagnosticsCalls())
                Check(call.Parameters.Diagnostics.Length == 0);
        }), ("clears diagnostics when validation is disabled", () =>
        {
            using var fixture = PushFixture(invalidTarget);
            OpenAndDrain(fixture);
            var calls = Published(fixture, "/src/tsconfig.json");
            Check(calls.Length > 0);
            Check(calls[^1].Diagnostics.Length > 0);
            var preferences = LsUtil.NewDefaultUserPreferences();
            preferences.ValidateEnabled = Tristate.False;
            fixture.Session.Configure(preferences);
            _ = Service(fixture, "/src/index.ts");
            fixture.Session.WaitForBackgroundTasks();
            calls = Published(fixture, "/src/tsconfig.json", fixture.Client.PublishDiagnosticsCalls().Length - 1);
            if (calls.Length == 0)
                calls = Published(fixture, "/src/tsconfig.json");
            Check(calls[^1].Diagnostics.Length == 0);
        }), ("publishes global diagnostics after checking", PublishGlobalAfterChecking), ("query globals before semantic checking", () => QueryGlobals(false)), ("query globals after semantic checking", () => QueryGlobals(true)), ("cleans tsconfig diagnostics after TS files close and restores them after TS file is reopened", () =>
        {
            using var fixture = PushFixture("{\"compilerOptions\": {\"baseUrl\": \".\"}}");
            OpenAndDrain(fixture);
            var calls = Published(fixture, "/src/tsconfig.json");
            Check(calls.Length > 0);
            Check(calls[0].Diagnostics.Length == 1);
            int before = fixture.Client.PublishDiagnosticsCalls().Length;
            fixture.Session.DidCloseFile(ProtocolRequestContext.Default, Uri("/src/index.ts"));
            fixture.Session.WaitForBackgroundTasks();
            calls = Published(fixture, "/src/tsconfig.json", before);
            Check(calls.Length > 0);
            Check(calls[^1].Diagnostics.Length == 0);
            before = fixture.Client.PublishDiagnosticsCalls().Length;
            fixture.Session.DidOpenFile(ProtocolRequestContext.Default, Uri("/src/index.ts"), 2, ByteStringValue.FromString("export const x = 1;"), LanguageKind.TypeScript);
            _ = Service(fixture, "/src/index.ts");
            fixture.Session.WaitForBackgroundTasks();
            calls = Published(fixture, "/src/tsconfig.json", before);
            Check(calls.Length > 0);
            Check(calls[^1].Diagnostics.Length == 1);
        }));
    }

    private static ProtocolRequestContext DiagnosticRequest() => ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("0")).WithCheckerLifetime(CheckerLifetime.Diagnostics);
    private static void PublishGlobalAfterChecking()
    {
        const string source = "export function f() {\n\t\t\t\tusing x = { [Symbol.dispose]() {} };\n\t\t\t}";
        using var fixture = PushFixture("{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"target\": \"es2020\"\n\t\t\t\t}\n\t\t\t}", source);
        Open(fixture, "/src/index.ts", source);
        var(service, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("0")), Uri("/src/index.ts"));
        Check(error is null);
        fixture.Session.WaitForBackgroundTasks();
        for (int iteration = 0; iteration < 2; iteration++)
        {
            var program = service!.GetProgram();
            var file = program.GetSourceFile(RootedFilePath.FromGoString("/src/index.ts"));
            var diagnostics = program.GetSemanticDiagnostics(DiagnosticRequest(), file);
            Check(diagnostics.Length > 0);
            foreach (var diagnostic in diagnostics)
                Check(ReferenceEquals(diagnostic.File(), file));
            var(report, diagnosticsError) = service.ProvideDiagnostics(DiagnosticRequest(), Uri("/src/index.ts"));
            Check(diagnosticsError is null);
            Check(report.FullDocumentDiagnosticReport is not null);
            bool hasSource = false;
            foreach (var diagnostic in report.FullDocumentDiagnosticReport!.Items)
            {
                Check(diagnostic!.Message.AsString().IndexOf(ByteStringValue.FromString("Cannot find global")) < 0);
                if (diagnostic.Code?.Integer == 2550)
                    hasSource = true;
            }

            Check(hasSource);
        }

        fixture.Session.EnqueuePublishGlobalDiagnostics();
        fixture.Session.WaitForBackgroundTasks();
        var calls = Published(fixture, "/src/tsconfig.json");
        Check(calls.Length > 0);
        Check(HasMessage(calls[^1].Diagnostics, "Cannot find global"));
    }

    private static void QueryGlobals(bool checkFirst)
    {
        const string source = "type Json = string | Json[];\ntype Parsed<T> = T extends object ? { [K in keyof T]: Parsed<T[K]> } : T;\ndeclare function wrap<T>(value: T): Parsed<T>;\nexport const value = wrap({ items: [] as Json[] });";
        using var fixture = new ProjectSessionFixture([new Input("/src/tsconfig.json", "{\"compilerOptions\":{\"strict\":true,\"noEmit\":true}}"), new Input("/src/repro.ts", source)]);
        Open(fixture, "/src/repro.ts", source);
        var request = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("0"));
        var(service, error) = fixture.Session.GetLanguageService(request, Uri("/src/repro.ts"));
        Check(error is null);
        fixture.Session.WaitForBackgroundTasks();
        var output = new StringBuilder();
        void Record(string caption, object? value)
        {
            var(json, marshalError) = JsonCodec.MarshalIndentValue(value, default, ByteStringValue.FromString("  "));
            Check(marshalError is null);
            output.Append("// ").Append(caption).Append('\n').Append(GoString.ToString(json.Materialize()!)).Append("\n\n");
        }

        void CheckDocument()
        {
            var(report, diagnosticsError) = service!.ProvideDiagnostics(DiagnosticRequest(), Uri("/src/repro.ts"));
            Check(diagnosticsError is null);
            Record("Document diagnostics", report.FullDocumentDiagnosticReport is null ? null : new ProtocolPayload<RelatedFullDocumentDiagnosticReport>(ProtocolCodec.GetType_821440dfb3fa7e3f(), report.FullDocumentDiagnosticReport));
        }

        if (checkFirst)
            CheckDocument();
        for (int iteration = 0; iteration < 2; iteration++)
        {
            int before = fixture.Client.PublishDiagnosticsCalls().Length;
            var(hover, hoverError) = service!.ProvideHover(request, new HoverParams { TextDocument = new TextDocumentIdentifier { Uri = Uri("/src/repro.ts") }, Position = new Position { Line = 3, Character = 14 } });
            Check(hoverError is null);
            Record("Hover on value", hover.Hover is null ? null : new ProtocolPayload<Hover>(ProtocolCodec.GetType_c76ae86ad3014c85(), hover.Hover));
            fixture.Session.EnqueuePublishGlobalDiagnostics();
            fixture.Session.WaitForBackgroundTasks();
            List<object?> published = new();
            foreach (var call in fixture.Client.PublishDiagnosticsCalls().Skip(before))
                published.Add(new ProtocolPayload<PublishDiagnosticsParams>(ProtocolCodec.GetType_a870657479cb5aa7(), call.Parameters));
            Record("Published after hover", published);
        }

        CheckDocument();
        string name = checkFirst ? "query-globals-after-semantic-checking.jsonc" : "query-globals-before-semantic-checking.jsonc";
        string root = FindProjectRoot();
        string expected = File.ReadAllText(Path.Combine(root, "upstream/TypeScript/tsc/testdata/baselines/reference/project", name));
        Check(output.ToString() == expected);
    }

    private static string FindProjectRoot()
    {
        foreach (var start in new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        }

        )
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "START-HERE.md")))
                    return directory.FullName;
        throw new DirectoryNotFoundException("Original Project baselines require repository root");
    }
}
