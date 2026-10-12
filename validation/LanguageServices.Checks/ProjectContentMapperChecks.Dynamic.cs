// Three whole original dynamic mapper state functions, outside compilation.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    private static Input[] DynamicFiles(bool withMappedFile, bool withOptions = false)
    {
        var files = new List<Input>
        {
            new("/home/project/tsconfig.json", withOptions ? "{ \"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"], \"options\": { \"mode\": \"project\" } } ] }" : "{ \"contentMappers\": [{ \"package\": \"mapper\", \"extensions\": [\".box\"] }] }"),
            new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.DynamicVerbatimMapper)),
            new("/home/project/main.ts", withMappedFile ? "import { value } from \"./app.box\"; value;" : "export const value = 1;")
        };
        if (withMappedFile)
        {
            files.Add(new("/home/project/app.box", "export const value = 1;\n"));
            files.Add(new("/home/project/mapper.config.json", "{ \"version\": 1 }"));
        }

        return files.ToArray();
    }

    public static void TestDynamicContentMapperInProject()
    {
        using var fixture = new ProjectSessionFixture(DynamicFiles(true, true), Options(), spawner: ContentMapperProjectFixture.NewSpawner());
        var session = fixture.Session;
        var ctx = ProtocolRequestContext.Default;
        var uri = Uri("file:///home/project/main.ts");
        session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString("import { value } from \"./app.box\"; value;"), LanguageKind.TypeScript);
        var(service, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        var program = service.GetProgram();
        var mapped = program.GetSourceFile(File("/home/project/app.box"));
        Require(mapped is not null);
        Require(!mapped.IsContentMapperFailureStub());
        Require(fixture.MutableFS.WriteFile(File("/home/project/mapper.config.json"), "{ \"version\": 2 }")is null);
        Watched(session, "/home/project/mapper.config.json");
        (service, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        Require(!ReferenceEquals(service.GetProgram(), program));
        mapped = service.GetProgram().GetSourceFile(File("/home/project/app.box"));
        Require(mapped is not null);
        Require(!mapped.IsContentMapperFailureStub());
    }

    public static void TestDynamicContentMapperRefreshesForMixedWatchBatches()
    {
        List<Exception>? failures = null;
        foreach (string name in new[]
        {
            "excessive events",
            "changed files make project fully dirty before mapper deletion",
            "changed files make project fully dirty before mapper creation"
        }

        )
        {
            try
            {
                var lifecycle = new ContentMapperProjectFixture.ProjectLifecycle();
                using var fixture = new ProjectSessionFixture(DynamicFiles(true), Options(), spawner: ContentMapperProjectFixture.NewSpawner(lifecycle));
                var session = fixture.Session;
                var ctx = ProtocolRequestContext.Default;
                var uri = Uri("file:///home/project/main.ts");
                session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString("import { value } from \"./app.box\"; value;"), LanguageKind.TypeScript);
                var(_, error) = session.GetLanguageService(ctx, uri);
                Require(error is null, error?.Message ?? "");
                Require(Volatile.Read(ref lifecycle.Opens) == 1);
                Require(Volatile.Read(ref lifecycle.Closes) == 0);
                FileEvent[] events;
                if (name == "excessive events")
                {
                    events = new FileEvent[1001];
                    for (int i = 0; i < events.Length; i++)
                        events[i] = new()
                        {
                            Uri = Uri("file:///home/project/noise-" + i + ".ts"),
                            Type = FileChangeType.Changed
                        };
                    events[0] = new()
                    {
                        Uri = Uri("file:///home/project/mapper.config.json"),
                        Type = FileChangeType.Changed
                    };
                    events[1] = new()
                    {
                        Uri = uri,
                        Type = FileChangeType.Changed
                    };
                }
                else
                    events = [new()
                    {
                        Uri = uri,
                        Type = FileChangeType.Changed
                    }, new()
                    {
                        Uri = Uri("file:///home/project/app.box"),
                        Type = FileChangeType.Changed
                    }, new()
                    {
                        Uri = Uri("file:///home/project/mapper.config.json"),
                        Type = name.EndsWith("deletion", StringComparison.Ordinal) ? FileChangeType.Deleted : FileChangeType.Created
                    }

                    ];
                session.DidChangeWatchedFiles(ctx, Slice<FileEvent>.FromArray(events));
                session.WaitForBackgroundTasks();
                (_, error) = session.GetLanguageService(ctx, uri);
                Require(error is null, error?.Message ?? "");
                Require(Volatile.Read(ref lifecycle.Closes) == 1);
                Require(Volatile.Read(ref lifecycle.Opens) == 2);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(name, error));
            }
        }

        if (failures is not null)
            throw new AggregateException(failures);
    }

    public static void TestUnusedDynamicContentMapperIsNotOpened()
    {
        var lifecycle = new ContentMapperProjectFixture.ProjectLifecycle();
        using var fixture = new ProjectSessionFixture(DynamicFiles(false), Options(), spawner: ContentMapperProjectFixture.NewSpawner(lifecycle));
        var ctx = ProtocolRequestContext.Default;
        var uri = Uri("file:///home/project/main.ts");
        fixture.Session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString("export const value = 1;"), LanguageKind.TypeScript);
        var(_, error) = fixture.Session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(Volatile.Read(ref lifecycle.Opens) == 0);
    }
}
