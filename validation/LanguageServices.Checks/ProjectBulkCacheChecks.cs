// Whole original project/bulkcache_test.go TestBulkCacheInvalidation.
using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectBulkCacheChecks
{
    private static DocumentUri Uri(string path) => new(ByteStringValue.FromString(path));
    private static RootedFilePath File(string path) => RootedFilePath.FromGoString(path);
    private static void Check([DoesNotReturnIf(false)] bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("original bulk-cache assertion failed");
    }

    private static void Groups(params ReadOnlySpan<Action> actions)
    {
        List<Exception>? failures = null;
        foreach (var action in actions)
            try
            {
                action();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }

        if (failures is not null)
            throw new AggregateException(failures);
    }

    private static Action Named(string name, Action action) => () =>
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            throw new InvalidOperationException(name, error);
        }
    };
    private static List<FileEvent> Events(string prefix)
    {
        List<FileEvent> events = new();
        for (int i = 0; i < 1001; i++)
            events.Add(new() { Uri = Uri(prefix + i + ".js"), Type = FileChangeType.Created });
        return events;
    }

    private static List<FileEvent> TsEvents(string prefix)
    {
        List<FileEvent> events = new();
        for (int i = 0; i < 1001; i++)
            events.Add(new() { Uri = Uri(prefix + i + ".ts"), Type = FileChangeType.Created });
        return events;
    }

    private static void Watched(Session session, List<FileEvent> events) => session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray(events.ToArray()));
    private static void Open(Session session, string path, string text) => session.DidOpenFile(ProtocolRequestContext.Default, Uri("file://" + path), 1, ByteStringValue.FromString(text), LanguageKind.TypeScript);
    private static TypeScript.LanguageServices.LanguageService Service(Session session, string path)
    {
        var(service, error) = session.GetLanguageService(ProtocolRequestContext.Default, Uri("file://" + path));
        Check(error is null);
        return service!;
    }

    public static int RunAll()
    {
        TestBulkCacheInvalidation();
        return 1;
    }

    public static void TestBulkCacheInvalidation()
    {
        Groups(Named("large number of node_modules changes invalidates only node_modules cache", () => Groups(Named("with file existing in cache", () => NodeModules(true)), Named("without file existing in cache", () => NodeModules(false)))), Named("large number of changes outside node_modules", () => Groups(Named("with event matching include glob", () => Outside(true)), Named("without event matching include glob", () => Outside(false)))), Named("large number of changes outside node_modules causes project reevaluation", Reevaluate), Named("config file names cache", () => Groups(Named("excessive changes only in node_modules does not affect config file names cache", () => Discovery(false)), Named("excessive changes outside node_modules clears config file names cache", () => Discovery(true)))), Named("excessive changes in dist folder do not invalidate", Dist), Named("bulk invalidation with retained config whose command line is nil", RetainedNilConfig));
    }

    private static void NodeModules(bool cached)
    {
        using var fixture = new ProjectSessionFixture(BaseFiles());
        var session = fixture.Session;
        Open(session, "/project/src/index.ts", IndexText);
        var service = Service(session, "/project/src/index.ts");
        Check(service.GetProgram().Options().Target == ScriptTarget.ES2015);
        var before = session.Snapshot().ConfigFileRegistry;
        Check(fixture.MutableFS.WriteFile(File("/project/tsconfig.json"), UpdatedConfig)is null);
        Check(fixture.MutableFS.WriteFile(File("/project/node_modules/@types/node/fs.d.ts"), "new text")is null);
        var events = Events("file:///project/node_modules/generated/file");
        if (cached)
        {
            events.Add(new() { Uri = Uri("file:///project/node_modules/@types/node/index.d.ts"), Type = FileChangeType.Changed });
            events.Add(new() { Uri = Uri("file:///project/node_modules/@types/node/console.d.ts"), Type = FileChangeType.Changed });
        }

        Watched(session, events);
        service = Service(session, "/project/src/index.ts");
        var after = session.Snapshot();
        Check(service.GetProgram().Options().Target == ScriptTarget.ES2015);
        Check(ReferenceEquals(before, after.ConfigFileRegistry));
        Check(after.GetFile(File("/project/node_modules/@types/node/fs.d.ts"))!.ContentValue().EqualsString(cached ? "new text" : ""));
    }

    private static void Outside(bool include)
    {
        using var fixture = new ProjectSessionFixture(BaseFiles());
        var session = fixture.Session;
        Open(session, "/project/src/index.ts", IndexText);
        var service = Service(session, "/project/src/index.ts");
        Check(service.GetProgram().Options().Target == ScriptTarget.ES2015);
        Check(fixture.MutableFS.WriteFile(File("/project/tsconfig.json"), UpdatedConfig)is null);
        Check(fixture.MutableFS.WriteFile(File("/project/src/rootFile.ts"), "console.log(\"root file\")")is null);
        var events = TsEvents("file:///project/generated/file");
        if (include)
            events.Add(new() { Uri = Uri("file:///project/src/rootFile.ts"), Type = FileChangeType.Created });
        Watched(session, events);
        service = Service(session, "/project/src/index.ts");
        Check(service.GetProgram().Options().Target == (include ? ScriptTarget.ESNext : ScriptTarget.ES2015));
        Check((service.GetProgram().GetSourceFile(File("/project/src/rootFile.ts"))is not null) == include);
    }

    private static void Reevaluate()
    {
        using var fixture = new ProjectSessionFixture(BaseFiles());
        var session = fixture.Session;
        Open(session, "/project/src/utils/lib.ts", "export function util() { return \"util\"; }");
        Check(session.Snapshot().GetDefaultProject(Uri("file:///project/src/utils/lib.ts"))!.ConfigFileName().AsValue().EqualsString("/project/tsconfig.json"));
        var service = Service(session, "/project/src/utils/lib.ts");
        Check(service.GetProgram().Options().Strict == Tristate.True);
        Check(fixture.MutableFS.WriteFile(File("/project/src/utils/tsconfig.json"), NestedConfig)is null);
        Watched(session, TsEvents("file:///project/src/generated/file"));
        service = Service(session, "/project/src/utils/lib.ts");
        Check(session.Snapshot().GetDefaultProject(Uri("file:///project/src/utils/lib.ts"))!.ConfigFileName().AsValue().EqualsString("/project/src/utils/tsconfig.json"));
        Check(service.GetProgram().Options().Strict == Tristate.False);
        Check(service.GetProgram().Options().Target == ScriptTarget.ESNext);
    }

    private static void Discovery(bool discover)
    {
        using var fixture = new ProjectSessionFixture([new Input("/project/src/index.ts", "console.log(\"test\");")]);
        var session = fixture.Session;
        Open(session, "/project/src/index.ts", "console.log(\"test\");");
        var snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.InferredProject()is not null);
        Check(snapshot.GetDefaultProject(Uri("file:///project/src/index.ts"))!.Kind == ProjectKind.Inferred);
        Check(fixture.MutableFS.WriteFile(File("/project/tsconfig.json"), DiscoveryConfig)is null);
        var events = discover ? TsEvents("file:///project/src/generated/file") : Events("file:///project/node_modules/generated/file");
        if (discover)
            events.Add(new() { Uri = Uri("file:///project/src/generated/tsconfig.json"), Type = FileChangeType.Created });
        Watched(session, events);
        _ = Service(session, "/project/src/index.ts");
        snapshot = session.Snapshot();
        var project = snapshot.GetDefaultProject(Uri("file:///project/src/index.ts"));
        if (discover)
        {
            Check(project!.Kind == ProjectKind.Configured);
            Check(project.ConfigFileName().AsValue().EqualsString("/project/tsconfig.json"));
        }
        else
            Check(ReferenceEquals(project, snapshot.ProjectCollection.InferredProject()));
    }

    private static void Dist()
    {
        using var fixture = new ProjectSessionFixture([new Input("/project/src/index.ts", "console.log(\"test\");")]);
        var session = fixture.Session;
        Open(session, "/project/src/index.ts", "console.log(\"test\");");
        Check(session.Snapshot().GetDefaultProject(Uri("file:///project/src/index.ts"))!.Kind == ProjectKind.Inferred);
        Check(fixture.MutableFS.WriteFile(File("/project/tsconfig.json"), DistConfig)is null);
        Watched(session, Events("file:///project/dist/generated/file"));
        _ = Service(session, "/project/src/index.ts");
        Check(session.Snapshot().GetDefaultProject(Uri("file:///project/src/index.ts"))!.Kind == ProjectKind.Inferred);
    }

    private static void RetainedNilConfig()
    {
        using var fixture = new ProjectSessionFixture([new Input("/project/tsconfig.json", SolutionConfig), new Input("/project/app/main.ts", "export const main = 1;")]);
        var session = fixture.Session;
        Open(session, "/project/app/main.ts", "export const main = 1;");
        _ = Service(session, "/project/app/main.ts");
        Check(fixture.MutableFS.WriteFile(File("/project/app/tsconfig.json"), AppConfig)is null);
        var events = TsEvents("file:///project/app/generated/file");
        events.Add(new() { Uri = Uri("file:///project/newdir/tsconfig.json"), Type = FileChangeType.Created });
        Watched(session, events);
        _ = Service(session, "/project/app/main.ts");
    }
}
