// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Whole original server_completion_internal_test.go (2) and
// server_contentmapper_internal_test.go (5), pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using ProtocolMessage = TypeScript.Lsp.Protocol.Message;
using CompletionItem = TypeScript.Lsp.Protocol.CompletionItem;

namespace LanguageServices.Checks;
public static class ServerInternalChecks
{
    public static int RunAll()
    {
        TestCompletionItemResolveRejectsRelativeFileName();
        TestCompletionItemResolveRejectsMalformedDynamicFileName();
        TestContentMapperLoggerRequiresTrace();
        TestParseContentMapperContributions();
        TestParseContentMapperContributionsRejectsConflictingInlineMappers();
        TestParseContentMapperContributionsUsesCaseInsensitiveExtensions();
        TestParseContentMapperContributionsDefaultsOptionsToObject();
        return 7;
    }

    private sealed class Reader : ILspReader
    {
        public (ProtocolMessage? Message, Exception? Error) Read() => (null, new EndOfStreamException());
    }

    private sealed class Writer : ILspWriter
    {
        public Exception? Write(ProtocolMessage message) => null;
    }

    private static Server NewServer(Stream? stderr = null) => Server.NewServer(new ServerOptions { In = new Reader(), Out = new Writer(), Err = stderr ?? Stream.Null, Cwd = RootedDirectoryPath.FromGoString("/"), FS = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, [])), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath() });
    private static ByteStringValue B(string value) => ByteStringValue.FromString(value);
    private static void Check(bool condition, string message = "original server assertion failed")
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T actual, T expected) => Check(EqualityComparer<T>.Default.Equals(actual, expected), $"expected {expected}; actual {actual}");
    public static void TestCompletionItemResolveRejectsRelativeFileName()
    {
        var(_, error) = NewServer().HandleCompletionItemResolve(ProtocolRequestContext.Default, new CompletionItem { Data = new CompletionItemData { FileName = B("relative.ts") } }, null!);
        Equal(error?.Message, "completion item data fileName must be absolute");
    }

    public static void TestCompletionItemResolveRejectsMalformedDynamicFileName()
    {
        foreach (string name in new[]
        {
            "^/invalid",
            "^/~ts-uri~/scheme/authority/~ts-uri-escape~zz~"
        }

        )
        {
            var(_, error) = NewServer().HandleCompletionItemResolve(ProtocolRequestContext.Default, new CompletionItem { Data = new CompletionItemData { FileName = B(name) } }, null!);
            Equal(error?.Message, "completion item data fileName must be a valid dynamic path");
        }
    }

    public static void TestContentMapperLoggerRequiresTrace()
    {
        using var output = new MemoryStream();
        var server = NewServer(output);
        var logger = server.ContentMapperLogger();
        logger(B("hidden"));
        Equal(output.Length, 0L);
        server.logger.SetVerbosity(LogVerbosity.Trace);
        logger(B("visible"));
        Check(output.GetBuffer().AsSpan(0, checked((int)output.Length)).IndexOf("visible"u8) >= 0);
    }

    private static ContentMapperContribution Contribution(string contributor, string extension, string? mapper) => new()
    {
        ContributorId = B(contributor),
        Extensions = Slice<ByteStringValue>.FromArray([B(extension)]),
        InferredProjectContribution = mapper is null ? null : new()
        {
            Manifest = new()
            {
                Name = B(mapper),
                Exec = Slice<ByteStringValue>.FromArray([B(mapper)])
            }
        }
    };
    public static void TestParseContentMapperContributions()
    {
        var options = ProtocolMap<ByteStringKey, object?>.Make();
        options[ByteStringKey.FromValue(B("mode"))] = B("embedded");
        var first = Contribution("publisher.extension", ".vue", "Vue mapper");
        first.InferredProjectContribution!.Options = options;
        first.InferredProjectContribution.Manifest!.Version = B("2.3.4");
        first.InferredProjectContribution.Manifest.Cwd = B("/workspace/mapper");
        first.InferredProjectContribution.Manifest.Exec = Slice<ByteStringValue>.FromArray([B("node"), B("mapper.js")]);
        first.InferredProjectContribution.Manifest.CompilerOptions = Slice<ByteStringValue>.FromArray([B("strict")]);
        var(contributions, error) = Server.ParseContentMapperContributions(Slice<ContentMapperContribution?>.FromArray([first, Contribution("publisher.extension", ".svelte", null)]));
        Check(error is null, error?.Message ?? "");
        Equal(contributions.Mappers.Length, 1);
        Check(contributions.Extensions.AsSpan().Length == 1 && contributions.Extensions[0].EqualsValue(B(".vue")));
        var mapper = contributions.Mappers[0];
        Equal(mapper.Identity(), "publisher.extension[0] (Vue mapper@2.3.4)");
        Check(mapper.PackageDirectory.AsValue().EqualsValue(B("/workspace/mapper")));
        Check(mapper.Options!.Bytes.AsSpan().SequenceEqual("{\"mode\":\"embedded\"}"u8));
    }

    public static void TestParseContentMapperContributionsRejectsConflictingInlineMappers()
    {
        var(_, error) = Server.ParseContentMapperContributions(Slice<ContentMapperContribution?>.FromArray([Contribution("first", ".vue", "first"), Contribution("second", ".vue", "second")]));
        Check(error?.Message.Contains("both claim extension \".vue\"", StringComparison.Ordinal) == true);
    }

    public static void TestParseContentMapperContributionsUsesCaseInsensitiveExtensions()
    {
        var(_, error) = Server.ParseContentMapperContributions(Slice<ContentMapperContribution?>.FromArray([Contribution("first", ".vue", "first"), Contribution("second", ".VUE", "second")]));
        Check(error?.Message.Contains("both claim extension \".VUE\"", StringComparison.Ordinal) == true);
        (_, error) = Server.ParseContentMapperContributions(Slice<ContentMapperContribution?>.FromArray([Contribution("built-in", ".TS", null)]));
        Check(error?.Message.Contains("invalid extension \".TS\"", StringComparison.Ordinal) == true);
    }

    public static void TestParseContentMapperContributionsDefaultsOptionsToObject()
    {
        var(contributions, error) = Server.ParseContentMapperContributions(Slice<ContentMapperContribution?>.FromArray([Contribution("publisher.extension", ".vue", "mapper")]));
        Check(error is null, error?.Message ?? "");
        Check(contributions.Mappers[0].Options!.Bytes.AsSpan().SequenceEqual("{}"u8));
    }
}
