// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using LocaleValue = TypeScript.Locale.Locale;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    private static (Mapper A, Mapper B, Mapper Other) LeaseMappers() => (new() { Package = "a", Name = "vue", Version = "1.0.0", Exec = ["vue-mapper"] }, new() { Package = "b", Name = "vue", Version = "1.0.0", Exec = ["vue-mapper"] }, new() { Name = "svelte", Version = "2.0.0", Exec = ["svelte-mapper"] });
    private static void RunnerConsolidatesByIdentity()
    {
        var spawner = new FakeSpawner();
        var host = ProcessHosts.NewHost(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default);
        try
        {
            var(a, b, other) = LeaseMappers();
            var project = host.Project(new ProjectSpec { Mappers = [a, b, other], CompilerOptions = new() });
            try
            {
                foreach (var mapper in new[]
                {
                    a,
                    b,
                    a,
                    other
                }

                )
                {
                    var(_, error) = project.Transform(mapper, new Request { FileName = new RootedFilePath("/x"), Content = "y" });
                    Assert(error is null, error?.Message ?? "consolidated transform");
                }

                Assert(Volatile.Read(ref spawner.Spawns) == 2, "one process per identity");
            }
            finally
            {
                _ = project.Close();
            }
        }
        finally
        {
            _ = host.Close();
        }
    }

    private static void RunnerLeaseLifecycle()
    {
        var spawner = new FakeSpawner();
        var host = ProcessHosts.NewHost(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default);
        try
        {
            var(a, b, other) = LeaseMappers();
            var releaseA = host.Acquire([a, a]);
            var releaseB = host.Acquire([b]);
            var releaseOther = host.Acquire([other]);
            foreach (var mapper in new[]
            {
                a,
                other
            }

            )
            {
                var(_, error) = host.Transform(mapper, new Request { FileName = new RootedFilePath("/x"), Content = "y" });
                Assert(error is null, error?.Message ?? "leased transform");
            }

            Assert(Volatile.Read(ref spawner.Spawns) == 2, "initial leased spawn count");
            releaseA();
            Assert(Volatile.Read(ref spawner.Closes) == 0, "shared vue remains owned");
            releaseOther();
            Assert(Volatile.Read(ref spawner.Closes) == 1, "last svelte release closes process");
            releaseB();
            releaseB();
            Assert(Volatile.Read(ref spawner.Closes) == 2, "last vue release closes once");
            var releaseNew = host.Acquire([a]);
            var(_, newError) = host.Transform(a, new Request { FileName = new RootedFilePath("/x"), Content = "y" });
            Assert(newError is null, newError?.Message ?? "reacquired transform");
            Assert(Volatile.Read(ref spawner.Spawns) == 3, "lazy fresh spawn after reacquire");
            releaseNew();
            Assert(Volatile.Read(ref spawner.Closes) == 3, "reacquired close");
        }
        finally
        {
            _ = host.Close();
        }
    }

    private static void HostSetLocaleRestartsMapper()
    {
        var process = new RecordingMapper();
        var spawner = new FakeSpawner(process);
        var host = ProcessHosts.NewHost(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default);
        try
        {
            var mapper = Mapper("vue");
            mapper.Version = "1.0.0";
            mapper.Exec = ["vue-mapper"];
            var release = host.Acquire([mapper]);
            try
            {
                var(_, firstError) = host.Transform(mapper, new Request { FileName = new RootedFilePath("/a.vue"), Content = "x" });
                Assert(firstError is null && Volatile.Read(ref spawner.Spawns) == 1, "first locale process");
                var(french, valid) = TypeScript.Locale.Locales.Parse("fr");
                Assert(valid, "French locale");
                host.SetLocale(french);
                Assert(Volatile.Read(ref spawner.Closes) == 1, "locale closes old process");
                var(_, secondError) = host.Transform(mapper, new Request { FileName = new RootedFilePath("/a.vue"), Content = "x" });
                Assert(secondError is null && Volatile.Read(ref spawner.Spawns) == 2, "new locale process");
                lock (process.Gate)
                    Assert(process.ReceivedLocale == "fr", "forwarded new locale");
            }
            finally
            {
                release();
            }
        }
        finally
        {
            _ = host.Close();
        }
    }

    private static void HostSetLocaleWaitsForTransform()
    {
        var process = new BlockingMapper();
        var spawner = new FakeSpawner(process);
        var host = ProcessHosts.NewHost(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default);
        var mapper = Mapper("vue");
        mapper.Version = "1.0.0";
        mapper.Exec = ["vue-mapper"];
        var transform = Task.Run(() => host.Transform(mapper, new Request { FileName = new RootedFilePath("/a.vue"), Content = "x" }).Error);
        try
        {
            process.Started.Wait();
            var(french, valid) = TypeScript.Locale.Locales.Parse("fr");
            Assert(valid, "French locale");
            using var setStarted = new ManualResetEventSlim();
            var set = Task.Run(() =>
            {
                setStarted.Set();
                host.SetLocale(french);
            });
            setStarted.Wait();
            Assert(!set.IsCompleted, "SetLocale must wait for in-flight transform");
            process.Proceed.Set();
            Assert(transform.GetAwaiter().GetResult()is null, "blocked transform completes");
            set.GetAwaiter().GetResult();
            Assert(Volatile.Read(ref spawner.Closes) == 1, "locale reset closes one process");
        }
        finally
        {
            process.Proceed.Set();
            _ = host.Close();
        }
    }

    private static void RunnerForwardsProjectOptions()
    {
        var process = new RecordingMapper();
        var(locale, valid) = TypeScript.Locale.Locales.Parse("cs-CZ");
        Assert(valid, "Czech locale");
        var host = ProcessHosts.NewHost(CancellationToken.None, new FakeSpawner(process), locale);
        try
        {
            var mapper = Mapper("vue");
            mapper.Version = "1.0.0";
            mapper.Exec = ["vue-mapper"];
            mapper.CompilerOptions = ["target", "jsx"];
            mapper.Options = new TypeScript.Json.JsonValue("{\"strictTemplates\":true}"u8.ToArray());
            var options = new CompilerOptions
            {
                Target = ScriptTarget.ES2020,
                Strict = Tristate.True
            };
            var project = host.Project(new ProjectSpec { Mappers = [mapper], CompilerOptions = options });
            try
            {
                var(_, error) = project.Transform(mapper, new Request { FileName = new RootedFilePath("/a.vue"), Content = "x" });
                Assert(error is null, error?.Message ?? "options transform");
                var(expected, marshalError) = TypeScript.Json.JsonCodec.TryMarshal(options);
                Assert(marshalError is null, "original compiler options marshal");
                lock (process.Gate)
                {
                    Assert(process.Received == GoString.FromBytes(expected!), "forwarded complete compiler options bytes");
                    Assert(process.ReceivedOptions == "{\"strictTemplates\":true}" && process.ReceivedLocale == "cs-CZ", "mapper options/locale");
                    Assert(!process.TransformParameters.Contains("\"options\"", StringComparison.Ordinal) && !process.TransformParameters.Contains("\"compilerOptions\"", StringComparison.Ordinal), "transform excludes project options");
                }
            }
            finally
            {
                _ = project.Close();
            }
        }
        finally
        {
            _ = host.Close();
        }
    }
}
