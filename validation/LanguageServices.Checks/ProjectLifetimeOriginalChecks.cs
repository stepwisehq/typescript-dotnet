using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectLifetimeOriginalChecks
{
    private static RootedFilePath File(string path) => RootedFilePath.FromGoString(path);
    private static PathKey Key(string path) => PathKey.FromValue(ByteStringValue.FromString(path));
    private static DocumentUri Uri(string path) => new(ByteStringValue.FromString("file://" + path));
    private static void Check([DoesNotReturnIf(false)] bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("original project lifetime assertion failed");
    }

    private static string Text(Input[] files, string path) => files.Single(file => file.Path == path).Contents;
    private static void Open(Session session, string path, string text) => session.DidOpenFile(ProtocolRequestContext.Default, Uri(path), 1, ByteStringValue.FromString(text), LanguageKind.TypeScript);
    private static void Close(Session session, string path) => session.DidCloseFile(ProtocolRequestContext.Default, Uri(path));
    private static TypeScript.LanguageServices.LanguageService Service(Session session, string path)
    {
        var(service, error) = session.GetLanguageService(ProtocolRequestContext.Default, Uri(path));
        Check(error is null);
        return service!;
    }

    private static void Events(Session session, params (string Path, FileChangeType Kind)[] events) => session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray(events.Select(item => new FileEvent { Uri = Uri(item.Path), Type = item.Kind }).ToArray()));
    public static int RunAll()
    {
        TestProjectLifetime();
        return 1;
    }

    public static void TestProjectLifetime()
    {
        List<Exception>? failures = null;
        string[] names = ["configured project", "unrooted inferred projects", "file moves from inferred to configured project", "file move from inferred to configured via didOpen/didClose sequence", "newly created file joins configured project with ./-prefixed includes (didOpen before create event)", "newly created file joins configured project with ./-prefixed includes (create event before didOpen)", "tsconfig move from subdirectory to parent via didChangeWatchedFiles", "deleted open file remains in project until closed"];
        Action[] groups = [Configured, Unrooted, InferredToConfigured, MovedFile, () => NewFile(true), () => NewFile(false), MovedConfig, DeletedOpen];
        for (int index = 0; index < groups.Length; index++)
            try
            {
                groups[index]();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(names[index], error));
            }

        if (failures is not null)
            throw new AggregateException(failures);
    }

    private static void Configured()
    {
        var files = Files0();
        using var fixture = new ProjectSessionFixture(files);
        var session = fixture.Session;
        var snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 0);
        const string p1 = "/home/projects/TS/p1/src/index.ts", p2 = "/home/projects/TS/p2/src/index.ts", p3 = "/home/projects/TS/p3/src/index.ts";
        Open(session, p1, Text(files, p1));
        Open(session, p2, Text(files, p2));
        session.WaitForBackgroundTasks();
        snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 2);
        foreach (var config in new[]
        {
            "/home/projects/ts/p1/tsconfig.json",
            "/home/projects/ts/p2/tsconfig.json"
        }

        )
        {
            Check(snapshot.ProjectCollection.ConfiguredProject(Key(config))is not null);
            Check(snapshot.ConfigFileRegistry.GetConfig(Key(config))is not null);
        }

        Check(fixture.Client.WatchFilesCalls().Length == 1);
        Close(session, p1);
        Open(session, p3, Text(files, p3));
        session.WaitForBackgroundTasks();
        snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 2);
        Check(snapshot.ProjectCollection.ConfiguredProject(Key("/home/projects/ts/p1/tsconfig.json"))is null);
        Check(snapshot.ConfigFileRegistry.GetConfig(Key("/home/projects/ts/p1/tsconfig.json"))is null);
        foreach (var config in new[]
        {
            "/home/projects/ts/p2/tsconfig.json",
            "/home/projects/ts/p3/tsconfig.json"
        }

        )
        {
            Check(snapshot.ProjectCollection.ConfiguredProject(Key(config))is not null);
            Check(snapshot.ConfigFileRegistry.GetConfig(Key(config))is not null);
        }

        Check(fixture.Client.WatchFilesCalls().Length == 1);
        Check(fixture.Client.UnwatchFilesCalls().Length == 0);
        Close(session, p2);
        Close(session, p3);
        Open(session, p1, Text(files, p1));
        session.WaitForBackgroundTasks();
        snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ProjectCollection.ConfiguredProject(Key("/home/projects/ts/p1/tsconfig.json"))is not null);
        Check(snapshot.ConfigFileRegistry.GetConfig(Key("/home/projects/ts/p1/tsconfig.json"))is not null);
        Check(snapshot.ConfigFileRegistry.GetConfig(Key("/home/projects/ts/p2/tsconfig.json"))is null);
        Check(snapshot.ConfigFileRegistry.GetConfig(Key("/home/projects/ts/p3/tsconfig.json"))is null);
        Check(fixture.Client.WatchFilesCalls().Length == 1);
        Check(fixture.Client.UnwatchFilesCalls().Length == 0);
    }

    private static void Unrooted()
    {
        var files = Files1();
        using var fixture = new ProjectSessionFixture(files);
        var session = fixture.Session;
        Check(session.Snapshot().ProjectCollection.Projects().Length == 0);
        const string p1 = "/home/projects/TS/p1/src/index.ts", p2 = "/home/projects/TS/p2/src/index.ts", p3 = "/home/projects/TS/p3/src/index.ts";
        Open(session, p1, Text(files, p1));
        Open(session, p2, Text(files, p2));
        CheckInferred(session);
        Close(session, p1);
        Open(session, p3, Text(files, p3));
        CheckInferred(session);
        Close(session, p2);
        Close(session, p3);
        Open(session, p1, Text(files, p1));
        CheckInferred(session);
    }

    private static void CheckInferred(Session session)
    {
        var snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ProjectCollection.InferredProject()is not null);
    }

    private static void InferredToConfigured()
    {
        var files = Files2();
        using var fixture = new ProjectSessionFixture(files);
        var session = fixture.Session;
        const string foo = "/home/projects/ts/foo.ts", main = "/home/projects/ts/p1/main.ts", config = "/home/projects/ts/p1/tsconfig.json";
        Open(session, foo, Text(files, foo));
        CheckInferred(session);
        Check(session.Snapshot().ProjectCollection.ConfiguredProject(Key(config))is null);
        Open(session, main, Text(files, main));
        var snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ProjectCollection.InferredProject()is null);
        Check(snapshot.ProjectCollection.ConfiguredProject(Key(config))is not null);
        Check(snapshot.ConfigFileRegistry.GetConfig(Key(config))is not null);
        Close(session, main);
        snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ProjectCollection.ConfiguredProject(Key(config))is not null);
        Close(session, foo);
        snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ConfigFileRegistry.GetConfig(Key(config))is not null);
    }

    private static void MovedFile()
    {
        var files = Files3();
        using var fixture = new ProjectSessionFixture(files);
        var session = fixture.Session;
        const string old = "/home/projects/TS/p1/index.ts", next = "/home/projects/TS/p1/src/index.ts", config = "/home/projects/ts/p1/tsconfig.json";
        Open(session, old, Text(files, old));
        CheckInferred(session);
        Check(session.Snapshot().ProjectCollection.ConfiguredProject(Key(config))is null);
        Check(fixture.MutableFS.WriteFile(File(next), Text(files, old))is null);
        Check(fixture.MutableFS.Remove(File(old).AsPath())is null);
        Open(session, next, Text(files, old));
        Close(session, old);
        Events(session, (next, FileChangeType.Created), (old, FileChangeType.Deleted));
        _ = Service(session, next);
        var snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ProjectCollection.InferredProject()is null);
        Check(snapshot.ProjectCollection.ConfiguredProject(Key(config))is not null);
    }

    private static void NewFile(bool openBeforeCreate)
    {
        var files = Files4();
        using var fixture = new ProjectSessionFixture(files);
        var session = fixture.Session;
        const string existing = "/home/projects/TS/monorepo/apps/web/app/existing.ts", next = "/home/projects/TS/monorepo/apps/web/app/subdir/new.ts", text = "export const newFile = 1;";
        Open(session, existing, Text(files, existing));
        Check(Service(session, existing).GetProgram().Options().Target == ScriptTarget.ESNext);
        Check(fixture.MutableFS.WriteFile(File(next), text)is null);
        if (openBeforeCreate)
        {
            Open(session, next, text);
            Events(session, (next, FileChangeType.Created));
        }
        else
        {
            Events(session, (next, FileChangeType.Created));
            Open(session, next, text);
        }

        Check(Service(session, next).GetProgram().Options().Target == ScriptTarget.ESNext);
        Check(session.Snapshot().ProjectCollection.InferredProject()is null);
    }

    private static void MovedConfig()
    {
        var files = Files5();
        using var fixture = new ProjectSessionFixture(files);
        var session = fixture.Session;
        const string index = "/home/projects/TS/p1/src/index.ts", old = "/home/projects/TS/p1/src/tsconfig.json", next = "/home/projects/TS/p1/tsconfig.json", other = "/home/projects/TS/p1/src/other.ts";
        Open(session, index, Text(files, index));
        CheckInferred(session);
        Check(session.Snapshot().ProjectCollection.ConfiguredProject(Key("/home/projects/ts/p1/src/tsconfig.json"))is null);
        Check(fixture.MutableFS.WriteFile(File(next), Text(files, old))is null);
        Check(fixture.MutableFS.Remove(File(old).AsPath())is null);
        Events(session, (next, FileChangeType.Created), (old, FileChangeType.Deleted));
        session.WaitForBackgroundTasks();
        _ = Service(session, index);
        var snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 2);
        Check(snapshot.ProjectCollection.InferredProject()is not null);
        Check(snapshot.GetDefaultProject(Uri(index))!.ConfigFileName() == File(next));
        Open(session, other, Text(files, other));
        snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ProjectCollection.InferredProject()is null);
        Check(snapshot.ProjectCollection.ConfiguredProject(Key("/home/projects/ts/p1/tsconfig.json"))is not null);
    }

    private static void DeletedOpen()
    {
        var files = Files6();
        using var fixture = new ProjectSessionFixture(files);
        var session = fixture.Session;
        const string index = "/home/projects/TS/p1/src/index.ts", x = "/home/projects/TS/p1/src/x.ts", y = "/home/projects/TS/p1/src/y.ts";
        Open(session, index, Text(files, index));
        Open(session, x, Text(files, x));
        var program = Service(session, index).GetProgram();
        Check(program.GetSourceFile(File(index))is not null);
        Check(program.GetSourceFile(File(x))is not null);
        Check(fixture.MutableFS.Remove(File(x).AsPath())is null);
        Check(fixture.MutableFS.WriteFile(File(y), "export const y = 2;")is null);
        Events(session, (x, FileChangeType.Deleted), (y, FileChangeType.Created));
        program = Service(session, x).GetProgram();
        Check(program.GetSourceFile(File(index))is not null);
        Check(program.GetSourceFile(File(x))is not null);
        Check(program.GetSourceFile(File(y))is not null);
        Close(session, x);
        program = Service(session, index).GetProgram();
        Check(program.GetSourceFile(File(index))is not null);
        Check(program.GetSourceFile(File(x))is null);
        Check(program.GetSourceFile(File(y))is not null);
    }
}
