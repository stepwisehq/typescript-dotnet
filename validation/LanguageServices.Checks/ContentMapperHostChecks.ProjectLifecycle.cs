// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using LocaleValue = TypeScript.Locale.Locale;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    // host_test.go:972. Complete original static/dynamic reuse, identity, watch,
    // handle, lifecycle and timing observations, in original call order.
    private static void ProjectLifecycle()
    {
        var process = new RecordingMapper
        {
            DynamicConfig = true
        };
        var spawner = new FakeSpawner(process);
        var host = ProcessHosts.NewHost(CancellationToken.None, spawner, TypeScript.Locale.Locales.Default);
        try
        {
            var staticMapper = Mapper("static");
            staticMapper.Version = "1.0.0";
            staticMapper.Options = new TypeScript.Json.JsonValue("{\"mode\":\"static\"}"u8.ToArray());
            var staticProject = host.Project(new ProjectSpec { ConfigFileName = new RootedFilePath("/repo/tsconfig.json"), Mappers = [staticMapper], CompilerOptions = new() });
            Assert(Volatile.Read(ref spawner.Spawns) == 0, "static identity must not spawn");
            var(staticIdentities, staticError) = staticProject.Identities();
            Assert(staticError is null && staticIdentities?.Count == 1, "static identity");
            Assert(staticProject.Close()is null, "static project close");
            var a = Mapper("dynamic");
            a.Version = "1.0.0";
            a.DynamicConfig = true;
            a.CompilerOptions = ["jsx"];
            a.Options = new TypeScript.Json.JsonValue("{\"mode\":\"a\"}"u8.ToArray());
            var b = Mapper("dynamic");
            b.Version = "1.0.0";
            b.DynamicConfig = true;
            b.Options = new TypeScript.Json.JsonValue("{\"mode\":\"b\"}"u8.ToArray());
            var aOptions = new CompilerOptions();
            var projectA = host.Project(new ProjectSpec { ConfigFileName = new RootedFilePath("/repo/a/tsconfig.json"), Mappers = [a, b], CompilerOptions = aOptions });
            var reversed = host.Project(new ProjectSpec { ConfigFileName = new RootedFilePath("/repo/reversed/tsconfig.json"), Mappers = [b, a], CompilerOptions = aOptions });
            var differentOptions = host.Project(new ProjectSpec { ConfigFileName = new RootedFilePath("/repo/options/tsconfig.json"), Mappers = [a], CompilerOptions = new() { Jsx = JsxEmit.React } });
            var projectB = host.Project(new ProjectSpec { ConfigFileName = new RootedFilePath("/repo/b/tsconfig.json"), Mappers = [a], CompilerOptions = new() });
            var again = host.Project(new ProjectSpec { ConfigFileName = new RootedFilePath("/repo/a/tsconfig.json"), Mappers = [a, b], CompilerOptions = aOptions });
            Assert(Volatile.Read(ref spawner.Spawns) == 0, "dynamic project retrieval must not start mapper");
            var(identitiesA, errorA) = projectA.Identities();
            Assert(errorA is null, errorA?.Message ?? "project A identities");
            var(identitiesReversed, errorReversed) = reversed.Identities();
            Assert(errorReversed is null, errorReversed?.Message ?? "reversed identities");
            var(identitiesDifferent, errorDifferent) = differentOptions.Identities();
            Assert(errorDifferent is null, errorDifferent?.Message ?? "different options identities");
            var(identitiesB, errorB) = projectB.Identities();
            Assert(errorB is null, errorB?.Message ?? "project B identities");
            Assert(identitiesA?.Count == 2 && identitiesReversed?.Count == 2, "two ordered identities");
            Assert(identitiesA![0] == identitiesReversed![1] && identitiesA[1] == identitiesReversed[0], "identity independent of mapper order");
            Assert(identitiesA[0] != identitiesDifferent![0], "compiler options contribute to identity");
            Assert(identitiesB?.Count == 1 && Volatile.Read(ref spawner.Spawns) == 1, "dynamic projects share one process");
            var(watchedA, watchErrorA) = projectA.WatchedFiles();
            Assert(watchErrorA is null, watchErrorA?.Message ?? "A watched files");
            var(watchedB, watchErrorB) = projectB.WatchedFiles();
            Assert(watchErrorB is null, watchErrorB?.Message ?? "B watched files");
            Assert(watchedA?.Count == 1 && watchedB?.Count == 1 && watchedA[0].AsString() == "/repo/a/mapper.config.js" && watchedB[0].AsString() == "/repo/b/mapper.config.js", "project-local watched paths");
            var(_, transformError) = projectA.Transform(b, new Request { FileName = new RootedFilePath("/repo/a/file.ext"), Content = "x" });
            Assert(transformError is null, transformError?.Message ?? "project A transform");
            lock (process.Gate)
                Assert(process.ProjectHandles.Take(2).Contains(process.TransformHandle, StringComparer.Ordinal), "transform uses opened handle");
            Assert(again.Close()is null, "retained A close");
            Assert(projectA.Close()is null, "A close");
            Assert(reversed.Close()is null, "reversed close");
            Assert(differentOptions.Close()is null, "different options close");
            Assert(projectB.Close()is null, "B close");
            var timings = host.Timings();
            var dynamicTiming = timings.Mappers![a.Identity()];
            Assert(dynamicTiming.Spawn.Count == 1 && dynamicTiming.Initialize.Count == 1 && dynamicTiming.OpenProject.Count == 6 && dynamicTiming.Transform.Count == 1 && dynamicTiming.CloseProject.Count == 6, "complete original dynamic timing counts");
            lock (process.Gate)
                Assert(process.ProjectHandles.Count == 6 && process.ClosedHandles.Count == 6, "all six project handles closed");
        }
        finally
        {
            _ = host.Close();
        }
    }
}
