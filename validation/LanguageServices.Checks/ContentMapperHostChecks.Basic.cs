// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using LocaleValue = TypeScript.Locale.Locale;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    // host_test.go:234. Exact ordinary transform observations.
    private static void RunnerTransform()
    {
        var host = ProcessHosts.NewHost(CancellationToken.None, new FakeSpawner(), TypeScript.Locale.Locales.Default);
        try
        {
            var mapper = Mapper("vue");
            mapper.Version = "1.0.0";
            mapper.Exec = ["vue-mapper"];
            var(result, error) = host.Transform(mapper, new Request { FileName = new RootedFilePath("/a.vue"), Content = "export const x = 1;" });
            Assert(error is null, error?.Message ?? "transform failed");
            Assert(result.Text == "export const x = 1;", "transformed text");
            Assert(result.VirtualExtension == ".ts", "virtual extension");
            Assert(result.Mappings is not null, "mapping owner");
            Assert(result.Diagnostics?.Count == 1, "diagnostic count");
            Assert(result.Diagnostics![0].Code() == 9999, "diagnostic code");
            Assert(result.Diagnostics[0].Source() == "vue", "diagnostic source");
        }
        finally
        {
            _ = host.Close();
        }
    }

    // host_test.go:253. Exact protocol/stderr raw logging substrings.
    private static void HostLogging()
    {
        var logs = new List<string>();
        var gate = new object ();
        var spawner = new SpawnerFunc((command, directory, stderr) =>
        {
            stderr.Write("mapper diagnostic\n"u8);
            return new FakeSpawner().Spawn(command, directory, stderr);
        });
        var host = ProcessHosts.NewHostWithOptions(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default, new HostOptions(message =>
        {
            lock (gate)
                logs.Add(message.Materialize() ?? "");
        }));
        try
        {
            var mapper = Mapper("resolved");
            mapper.Package = "configured";
            mapper.Version = "1.0.0";
            var(_, error) = host.Transform(mapper, new Request { FileName = new RootedFilePath("/a.vue"), Content = "export const x = 1;" });
            Assert(error is null, error?.Message ?? "logged transform failed");
            lock (gate)
            {
                var joined = string.Join("\n", logs);
                Assert(joined.Contains("[content mapper: resolved] send: {\"jsonrpc\":\"2.0\",\"id\":\"api1\",\"method\":\"initialize\"", StringComparison.Ordinal), "initialize send log");
                Assert(joined.Contains("[content mapper: resolved] receive: {\"jsonrpc\":\"2.0\",\"id\":\"api1\",\"result\":", StringComparison.Ordinal), "initialize receive log");
                Assert(joined.Contains("[content mapper: resolved] stderr: mapper diagnostic", StringComparison.Ordinal), "stderr log");
            }
        }
        finally
        {
            _ = host.Close();
        }
    }

    // host_test.go:281. The real discard owner, not a successful fake write.
    private static void HostDiscardsStderrWithoutLogging()
    {
        var spawner = new SpawnerFunc((command, directory, stderr) =>
        {
            Assert(ReferenceEquals(stderr, Stream.Null), "stderr must be discard without logging");
            return new FakeSpawner().Spawn(command, directory, stderr);
        });
        var host = ProcessHosts.NewHost(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default);
        try
        {
            var mapper = Mapper("resolved");
            mapper.Package = "configured";
            mapper.Version = "1.0.0";
            var(_, error) = host.Transform(mapper, new Request { FileName = new RootedFilePath("/a.vue"), Content = "export const x = 1;" });
            Assert(error is null, error?.Message ?? "discard transform failed");
        }
        finally
        {
            _ = host.Close();
        }
    }

    // host_test.go:297. All three original fallback rows.
    private static void MapperDiagnosticName()
    {
        foreach (var(mapper, expected)in new (Mapper, string)[]
        {
            (new() { Package = "configured", Name = "resolved", ContributionID = "contributed" }, "resolved"),
            (new() { Package = "configured", ContributionID = "contributed" }, "configured"),
            (new() { ContributionID = "contributed" }, "contributed")
        }

        )
            Assert(mapper.DiagnosticName() == expected, "diagnostic name fallback");
    }
}
