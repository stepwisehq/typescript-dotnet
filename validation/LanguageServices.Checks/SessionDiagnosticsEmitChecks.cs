// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored differential: actual server.go diagnostic-flake flow, not an original function.
// Complete expectations are retained from the pinned Go four-row reference capture.
using Emit.Checks;
using TypeScript.Compiler;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class SessionDiagnosticsEmitChecks
{
    private static string Config(bool noEmitOnError) => "{\"compilerOptions\":{\"skipLibCheck\":true,\"strict\":true,\"declaration\":true,\"target\":\"esnext\",\"module\":\"esnext\",\"noEmitOnError\":" + (noEmitOnError ? "true" : "false") + "},\"files\":[\"input.ts\"]}";
    internal readonly record struct ExpectedOutput(string Name, string Text);
    // Expected complete diagnostic JSON and output bytes come exclusively from the pinned Go witness.
    internal static void RunCase(string name, string text, bool noEmitOnError, string expectedDiagnostics, string expectedEmitDiagnostics, bool expectedSkipped, ExpectedOutput[] expectedOutputs)
    {
        using var fixture = new ProjectSessionFixture([new BaselineFS.Input("/fixture/tsconfig.json", Config(noEmitOnError)), new BaselineFS.Input("/fixture/input.ts", text)]);
        var context = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("authored-context-emit-" + name)).WithCheckerLifetime(CheckerLifetime.Diagnostics);
        var uri = new DocumentUri(ByteStringValue.FromString("file:///fixture/input.ts"));
        fixture.Session.DidOpenFile(context, uri, 1, ByteStringValue.FromString(text), LanguageKind.TypeScript);
        var (service, error) = fixture.Session.GetLanguageService(context, uri);
        if (error is not null) throw error;
        var (before, beforeError) = service!.ProvideDiagnostics(context, uri);
        if (beforeError is not null) throw beforeError;
        var beforeBytes = ProtocolCodec.Marshal(Method.TextDocumentDiagnosticInfo.Result, before);
        Require(beforeBytes.EqualsValue(ByteStringValue.FromString(expectedDiagnostics)), "complete pre-emit diagnostics differ from pinned Go");
        var outputs = new List<ExpectedOutput>();
        var result = service.GetProgram().Emit(context, new EmitOptions { WriteFile = (path, content, _) => { lock (outputs) outputs.Add(new(path.AsString(), content)); return null; } });
        Require(result is not null && result.EmitSkipped == expectedSkipped, "context-aware emit disposition differs");
        var projected = TsDiagnosticProjection.Project(result!.Diagnostics, "");
        var channels = new TsDiagnosticChannels([], [], [], [], projected);
        var expectedChannels = "{\"Config\":[],\"Program\":[],\"Syntactic\":[],\"Global\":[],\"Semantic\":" + expectedEmitDiagnostics + "}";
        TsDiagnosticProjection.Check(name, channels, expectedChannels);
        var (after, afterError) = service.ProvideDiagnostics(context, uri);
        if (afterError is not null) throw afterError;
        var afterBytes = ProtocolCodec.Marshal(Method.TextDocumentDiagnosticInfo.Result, after);
        Require(afterBytes.EqualsValue(ByteStringValue.FromString(expectedDiagnostics)), "complete post-emit diagnostics differ from pinned Go");
        outputs.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));
        Require(outputs.Count == expectedOutputs.Length, "emit output count differs");
        for (int i = 0; i < outputs.Count; i++)
            Require(outputs[i] == expectedOutputs[i], "emit path/content bytes differ");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
