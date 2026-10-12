// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// All three original internal/lsp/stack_sanitizer_test.go functions/markdown channels.
// Draft outside compilation; actual GoRegexp replacement APIs are required.
using TypeScript.Scanner;
using TypeScript.Lsp;
using TypeScript.RegularExpressions;

namespace LanguageServices.Checks;
public static class StackSanitizerChecks
{
    private readonly record struct Case(string Name, string Input, string Baseline);
    private static readonly Case[] Cases = [new("TestSanitizedDebugStackTraceCompletionsRequest", "goroutine 1196 [running]:\nruntime/debug.Stack()\n        /usr/local/go/src/runtime/debug/stack.go:26 +0x8e\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).recover(0xc0001dae08, {0x14bc418, 0xc00bc60960}, 0xc00baf16e0)\n        /workspaces/TypeScript/tsc/internal/lsp/server.go:777 +0x65\npanic({0x1077b40?, 0x1abcb70?})\n        /usr/local/go/src/runtime/panic.go:783 +0x136\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getCompletionData.func15()\n        /workspaces/TypeScript/tsc/internal/ls/completions.go:1303 +0xfa\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getCompletionData.func18()\n        /workspaces/TypeScript/tsc/internal/ls/completions.go:1548 +0x2df\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getCompletionData(0xc004b08240, {0x14bc418, 0xc00bc60a20}, 0xc0069ef908, 0xc000272008, 0x1b, 0xc002b28e00)\n        /workspaces/TypeScript/tsc/internal/ls/completions.go:1581 +0x2b92\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getCompletionsAtPosition(0xc004b08240, {0x14bc418, 0xc00bc60a20}, 0xc000272008, 0x1b, 0x0)\n        /workspaces/TypeScript/tsc/internal/ls/completions.go:347 +0x690\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).ProvideCompletion(0xc004b08240, {0x14bc418, 0xc00bc60a20}, {0xc0092e02a0, 0x28}, {0x2, 0x4}, 0xc004580c30)\n        /workspaces/TypeScript/tsc/internal/ls/completions.go:47 +0x207\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).handleCompletion(0xc0001dae08, {0x14bc418, 0xc00bc60960}, 0xc004b08240, 0xc00baf14d0)\n        /workspaces/TypeScript/tsc/internal/lsp/server.go:1102 +0xe5\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.registerLanguageServiceWithAutoImportsRequestHandler[...].func1({0x14bc418, 0xc00bc60960}, 0xc00baf16e0)\n        /workspaces/TypeScript/tsc/internal/lsp/server.go:682 +0x32a\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).handleRequestOrNotification(0xc0001dae08, {0x14bc418, 0xc00bc60960}, 0xc00baf16e0)\n        /workspaces/TypeScript/tsc/internal/lsp/server.go:531 +0x11e\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).dispatchLoop.func1()\n        /workspaces/TypeScript/tsc/internal/lsp/server.go:414 +0x65\ncreated by github.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).dispatchLoop in goroutine 19\n        /workspaces/TypeScript/tsc/internal/lsp/server.go:438 +0x60", "completionsDebugStackTrace.md"), new("TestSanitizedReleaseStackTraceCompletionsRequest", "runtime error: invalid memory address or nil pointer dereference\ngoroutine 2331 [running]:\nruntime/debug.Stack()\n\truntime/debug/stack.go:26 +0x5e\ngithub.com/microsoft/TypeScript/tsc/cmd/tsc.runMain()\n\tgithub.com/microsoft/TypeScript/tsc/cmd/tsc/main.go:17 +0x20\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).recover(0xc0001c6e08, {0x441ae5?, 0xc000e976c0?}, 0xc00ab6c7b0)\n\tgithub.com/microsoft/TypeScript/tsc/internal/lsp/server.go:777 +0x58\npanic({0xc323a0?, 0x1780b90?})\n\truntime/panic.go:783 +0x132\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getCompletionData.func15()\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/completions.go:1303 +0xba\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getCompletionData.func18(...)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/completions.go:1548\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getCompletionData(0xc008329200, {0x10f6688, 0xc00c2871d0}, 0xc00190b308, 0xc0001fe008, 0x1b, 0xc0008a2f00)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/completions.go:1581 +0x1ed4\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getCompletionsAtPosition(0xc008329200, {0x10f6688, 0xc00c2871d0}, 0xc0001fe008, 0x1b, 0x0)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/completions.go:347 +0x35f\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).ProvideCompletion(0xc008329200, {0x10f6688, 0xc00c287110}, {0xc00b472030?, 0xc00c287110?}, {0xb472030?, 0xc0?}, 0xc00c3ea000)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/completions.go:47 +0x11c\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).handleCompletion(0x418834?, {0x10f6688?, 0xc00c287110?}, 0xc00b472030?, 0x10f6688?)\n\tgithub.com/microsoft/TypeScript/tsc/internal/lsp/server.go:1105 +0x39\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.init.func1.registerLanguageServiceWithAutoImportsRequestHandler[...].28({0x10f6688, 0xc00c287110}, 0xc00ab6c7b0)\n\tgithub.com/microsoft/TypeScript/tsc/internal/lsp/server.go:682 +0x16c\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).handleRequestOrNotification(0xc0001c6e08, {0x10f66c0?, 0xc006589180?}, 0xc00ab6c7b0)\n\tgithub.com/microsoft/TypeScript/tsc/internal/lsp/server.go:531 +0x1c6\ngithub.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).dispatchLoop.func1()\n\tgithub.com/microsoft/TypeScript/tsc/internal/lsp/server.go:414 +0x3a\ncreated by github.com/microsoft/TypeScript/tsc/internal/lsp.(*Server).dispatchLoop in goroutine 35\n\tgithub.com/microsoft/TypeScript/tsc/internal/lsp/server.go:438 +0x9f1", "completionsReleaseStackTrace.md"), new("TestSanitizedStackTraceDefeatsVSCodeGenericSecretRegex", "goroutine 7 [running]:\nruntime/debug.Stack()\n\truntime/debug/stack.go:26 +0x5e\ngithub.com/microsoft/TypeScript/tsc/cmd/tsc.runMain()\n\tgithub.com/microsoft/TypeScript/tsc/cmd/tsc/main.go:17 +0x20\ngithub.com/microsoft/TypeScript/tsc/internal/ls.(*LanguageService).getSignatureHelp(0x1)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/signature.go:42 +0x10\ngithub.com/microsoft/TypeScript/tsc/internal/ls.LookupKey(0x2)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/keys.go:7 +0x10\ngithub.com/microsoft/TypeScript/tsc/internal/ls.validateToken(0x3)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/token.go:9 +0x10\ngithub.com/microsoft/TypeScript/tsc/internal/ls.signRequest(0x4)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/sig.go:11 +0x10\ngithub.com/microsoft/TypeScript/tsc/internal/ls.setPwd(0x5)\n\tgithub.com/microsoft/TypeScript/tsc/internal/ls/pwd.go:13 +0x10", "genericSecretWorkaround.md"), ];
    public static IEnumerable<string> Names => Cases.Select(item => item.Name);

    public static int RunAll()
    {
        foreach (var item in Cases)
            Run(item.Name);
        return Cases.Length;
    }

    public static void Run(string name)
    {
        var item = Cases.Single(item => item.Name == name);
        var input = ByteStringValue.FromString(GoString.ToUtf8(item.Input));
        var output = StackSanitizer.SanitizeStackTrace(input);
        if (name == "TestSanitizedStackTraceDefeatsVSCodeGenericSecretRegex")
        {
            var regex = GoRegexp.MustCompile(ByteStringValue.FromString(@"(?i)(key|token|sig|secret|signature|password|passwd|pwd|android:value)[^a-zA-Z0-9]"));
            var location = regex.FindStringIndex(output);
            if (!location.IsNil)
                throw new InvalidOperationException("Sanitized stack would be redacted by VS Code generic-secret expression");
        }

        var builder = new GoBuilder();
        builder.WriteString("Test name: `");
        builder.WriteString(item.Name);
        builder.WriteString("`\n\n# Unsanitized input:\n\n````\n");
        builder.WriteString(input);
        builder.WriteString("\n````\n\n# Sanitized output:\n\n````\n");
        builder.WriteString(output);
        builder.WriteString("\n````\n");
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        string? baseline = null;
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "upstream/TypeScript/tsc/testdata/baselines/reference/lsp/stackSanitizer", item.Baseline);
            if (File.Exists(candidate))
            {
                baseline = candidate;
                break;
            }

            directory = directory.Parent;
        }

        if (baseline == null)
            throw new FileNotFoundException(item.Baseline);
        var expected = ByteStringView.FromOwnedBytes(File.ReadAllBytes(baseline));
        if (!builder.Value().EqualsValue(ByteStringValue.FromView(expected)))
            throw new InvalidOperationException("Original sanitizer markdown baseline mismatch: " + item.Baseline);
    }
}
