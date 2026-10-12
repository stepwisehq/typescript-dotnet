// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete assertions and gate phases from the three internal/module race tests.
// Long-running .NET tasks replace Go goroutines; bounded signals replace t.Context.
using System.Collections.Concurrent;
using Configuration.Checks;
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.Vfs;
using VfsInfo = TypeScript.Vfs.FileInfo;

namespace Resolution.Checks;
internal class ForwardingFileSystem(IFS inner) : IFS
{
    protected IFS Inner { get; } = inner;

    public virtual CaseSensitivity CaseSensitivity() => Inner.CaseSensitivity();
    public virtual bool FileExists(RootedFilePath path) => Inner.FileExists(path);
    public virtual Exception? WriteFile(RootedFilePath path, string data) => Inner.WriteFile(path, data);
    public virtual (string Contents, bool Ok) ReadFile(RootedFilePath path) => Inner.ReadFile(path);
    public bool DirectoryExists(RootedDirectoryPath path) => Inner.DirectoryExists(path);
    public Entries GetAccessibleEntries(RootedDirectoryPath path) => Inner.GetAccessibleEntries(path);
    public VfsInfo? Stat(RootedPath path) => Inner.Stat(path);
    public RootedPath Realpath(RootedPath path) => Inner.Realpath(path);
}

public static class ResolverConcurrencyChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestResolveModuleNameTrailingSlashRace", "TestResolveSubpathNilContentsRace", "TestResolvePeerDependencyNilContentsRace"];

    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);
    private sealed record Result(string Name, bool Resolved, Exception? Panic);
    private static void Wait(ManualResetEventSlim signal, string description) => Check.True(signal.Wait(Limit), "Timed out waiting for " + description);
    private static Task Start(Action action) => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    private static void Join(Task[] workers) => Check.True(Task.WaitAll(workers, Limit), "Timed out waiting for original resolver race workers");
    private sealed class BlockingFileSystem(IFS inner, RootedFilePath target) : ForwardingFileSystem(inner), IDisposable
    {
        public readonly ManualResetEventSlim Gate = new();
        public readonly BlockingCollection<bool> Arrived = new(2);
        public override bool FileExists(RootedFilePath path)
        {
            if (path == target)
            {
                Check.True(Arrived.TryAdd(true, Limit), "Original blocking FileExists arrival channel");
                Wait(Gate, "blocking FileExists release");
            }

            return base.FileExists(path);
        }

        public void Dispose()
        {
            Gate.Dispose();
            Arrived.Dispose();
        }
    }

    private sealed class FlipFileSystem(IFS inner, RootedFilePath target) : ForwardingFileSystem(inner), IDisposable
    {
        private int count;
        public readonly ManualResetEventSlim FirstArrived = new(), SecondArrived = new(), ReadArrived = new();
        public readonly ManualResetEventSlim FirstGate = new(), SecondGate = new(), ReadGate = new();
        public override bool FileExists(RootedFilePath path)
        {
            if (path == target)
            {
                int call = Interlocked.Increment(ref count);
                if (call == 1)
                {
                    FirstArrived.Set();
                    Wait(FirstGate, "first FileExists release");
                    return false;
                }

                if (call == 2)
                {
                    SecondArrived.Set();
                    Wait(SecondGate, "second FileExists release");
                    return base.FileExists(path);
                }
            }

            return base.FileExists(path);
        }

        public override (string Contents, bool Ok) ReadFile(RootedFilePath path)
        {
            if (path == target)
            {
                ReadArrived.Set();
                Wait(ReadGate, "target package.json ReadFile release");
            }

            return base.ReadFile(path);
        }

        public void ReleaseAll()
        {
            FirstGate.Set();
            SecondGate.Set();
            ReadGate.Set();
        }

        public void Dispose()
        {
            FirstArrived.Dispose();
            SecondArrived.Dispose();
            ReadArrived.Dispose();
            FirstGate.Dispose();
            SecondGate.Dispose();
            ReadGate.Dispose();
        }
    }

    public static void Run(string name)
    {
        if (name == "TestResolveModuleNameTrailingSlashRace")
        {
            TrailingSlash();
            return;
        }

        if (name == "TestResolveSubpathNilContentsRace")
        {
            NilContents(peer: false);
            return;
        }

        if (name == "TestResolvePeerDependencyNilContentsRace")
        {
            NilContents(peer: true);
            return;
        }

        throw new ArgumentException("Unknown original resolver race body.", nameof(name));
    }

    private static void TrailingSlash()
    {
        using var fs = new BlockingFileSystem(ResolutionFixtures.Fs(("/repo/node_modules/pkg/package.json", "{\"name\":\"pkg\",\"types\":\"./typings/index.d.ts\"}"), ("/repo/node_modules/pkg/typings/index.d.ts", "export const x: number;"), ("/repo/src/a/file.ts", ""), ("/repo/src/b/file.ts", "")), RootedFilePath.FromUnicode("/repo/node_modules/pkg/package.json"));
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(fs), CompilerOptions = ResolutionFixtures.Bundler() });
        using var results = new BlockingCollection<Result>(2);
        Task[] workers = new[]
        {
            "pkg",
            "pkg/"
        }.Select(specifier => Start(() =>
        {
            string file = specifier.EndsWith('/') ? "/repo/src/b/file.ts" : "/repo/src/a/file.ts";
            try
            {
                var(resolved, _, _) = resolver.ResolveModuleName(specifier, RootedFilePath.FromUnicode(file), ResolutionMode.ESNext, null);
                results.Add(new(specifier, resolved?.IsResolved() == true, null));
            }
            catch (Exception error)
            {
                results.Add(new(specifier, false, error));
            }
        })).ToArray();
        try
        {
            Check.True(fs.Arrived.TryTake(out _, Limit), "First original FileExists gate arrival");
            Check.True(fs.Arrived.TryTake(out _, Limit), "Second original FileExists gate arrival");
            fs.Gate.Set();
            Join(workers);
            foreach (Result result in results)
            {
                Check.True(result.Panic is null, "Original trailing-slash worker did not panic: " + result.Panic);
                Check.True(result.Resolved, result.Name + " original concurrent resolution");
            }
        }
        finally
        {
            fs.Gate.Set();
            Join(workers);
        }
    }

    private static void NilContents(bool peer)
    {
        string target = peer ? "/repo/node_modules/peer/package.json" : "/repo/node_modules/pkg/package.json";
        var files = peer ? ResolutionFixtures.Fs(("/repo/node_modules/pkg/package.json", "{\"name\":\"pkg\",\"version\":\"1.0.0\",\"types\":\"index.d.ts\",\"peerDependencies\":{\"peer\":\"*\"}}"), ("/repo/node_modules/pkg/index.d.ts", "export declare const x: number;"), (target, "{\"name\":\"peer\",\"version\":\"2.0.0\"}"), ("/repo/src/a/file.ts", ""), ("/repo/src/b/file.ts", "")) : ResolutionFixtures.Fs((target, "{\"name\":\"pkg\",\"version\":\"1.0.0\"}"), ("/repo/node_modules/pkg/sub/index.d.ts", "export declare const sub: number;"), ("/repo/node_modules/pkg/sub/index.js", "exports.sub = 1;"), ("/repo/src/a/file.ts", ""), ("/repo/src/b/file.ts", ""));
        using var fs = new FlipFileSystem(files, RootedFilePath.FromUnicode(target));
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(fs), CompilerOptions = ResolutionFixtures.Bundler() });
        using var results = new BlockingCollection<Result>(2);
        Task[] workers = new[]
        {
            "/repo/src/a/file.ts",
            "/repo/src/b/file.ts"
        }.Select(file => Start(() =>
        {
            try
            {
                var(resolved, _, _) = resolver.ResolveModuleName(peer ? "pkg" : "pkg/sub", RootedFilePath.FromUnicode(file), ResolutionMode.ESNext, null);
                results.Add(new(file, resolved?.IsResolved() == true, null));
            }
            catch (Exception error)
            {
                results.Add(new(file, false, error));
            }
        })).ToArray();
        Result? first = null;
        try
        {
            Wait(fs.FirstArrived, "first original package.json FileExists arrival");
            Wait(fs.SecondArrived, "second original package.json FileExists arrival");
            fs.FirstGate.Set();
            if (peer)
            {
                Check.True(results.TryTake(out first, Limit), "First original peer lookup completed before second release");
            }

            fs.SecondGate.Set();
            Wait(fs.ReadArrived, "original package.json ReadFile arrival");
            fs.ReadGate.Set();
            Join(workers);
            if (first is not null)
                AssertResult(first);
            foreach (Result result in results)
                AssertResult(result);
        }
        finally
        {
            fs.ReleaseAll();
            Join(workers);
        }

        static void AssertResult(Result result)
        {
            Check.True(result.Panic is null, "Original nil-Contents dereference did not panic: " + result.Panic);
            Check.True(result.Resolved, result.Name + " original concurrent resolution");
        }
    }
}
