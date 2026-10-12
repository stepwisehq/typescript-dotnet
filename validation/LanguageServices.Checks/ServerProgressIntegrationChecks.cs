// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Whole original server_progress_test.go TestProgressNotificationsEndToEnd.
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ServerIntegrationChecks
{
    public static void TestProgressNotificationsEndToEnd()
    {
        using var client = new LspTestClient(Options([new("/home/projects/tsconfig.json", "{}"), new("/home/projects/index.ts", "export const x = 1;")]), DefaultServerRequest);
        object gate = new();
        List<ProgressParams> collected = [];
        var end = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnServerNotification = (_, request) =>
        {
            if (request.Method != Method.Progress)
                return;
            var(parameters, error) = request.UnmarshalParams(Method.ProgressInfo.Parameters);
            if (error is not null || parameters is null)
                return;
            bool isEnd;
            lock (gate)
            {
                collected.Add(parameters);
                isEnd = parameters.Value.End is not null;
            }

            if (isEnd)
                end.TrySetResult();
        };
        var(initialized, _, initializedOK) = client.SendRequest(Method.InitializeInfo, new InitializeParams { Capabilities = new ClientCapabilities { Window = new WindowClientCapabilities { WorkDoneProgress = true } } });
        Check(initializedOK && initialized!.AsResponse().Error is null, "Initialize failed");
        client.SendNotification(Method.InitializedInfo, new InitializedParams());
        client.Server.InitComplete().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        var uri = new DocumentUri(B("file:///home/projects/index.ts"));
        client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = LanguageKind.TypeScript, Text = B("export const x = 1;") } });
        var(message, result, ok) = client.SendRequest(Method.CustomProjectInfoInfo, new ProjectInfoParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        Check(ok, "expected a response");
        Check(message!.AsResponse().Error is null);
        Check(result!.ConfigFilePath.EqualsValue(B("/home/projects/tsconfig.json")));
        // Original waits on t.Context only; this is a test deadlock-safety bound.
        end.Task.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        ProgressParams[] notifications;
        lock (gate)
            notifications = collected.ToArray();
        Check(notifications.Length >= 2, "expected at least begin+end progress notifications");
        Check(notifications[0].Value.Begin is not null, "expected first progress notification to be begin");
        Check(notifications[0].Value.Begin!.Title.EqualsValue(B("Loading")));
        Check(notifications[^1].Value.End is not null, "expected last progress notification to be end");
        var token = notifications[0].Token.String ?? default;
        Check(token.Length != 0, "expected non-empty progress token");
        for (int i = 0; i < notifications.Length; i++)
            Check((notifications[i].Token.String ?? default).EqualsValue(token), $"notification {i} has different token");
    }
}
