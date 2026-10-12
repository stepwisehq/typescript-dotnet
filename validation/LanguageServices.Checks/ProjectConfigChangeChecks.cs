// Whole original project/configfilechanges_test.go function.
using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectConfigChangeChecks
{
    private static RootedFilePath File(string path) => RootedFilePath.FromGoString(path);
    private static DocumentUri Uri(string path) => new(ByteStringValue.FromString("file://" + path));
    private static void Check([DoesNotReturnIf(false)] bool value)
    {
        if (!value)
            throw new InvalidOperationException("original config-change assertion failed");
    }

    private static void Open(Session session, string path, string text) => session.DidOpenFile(ProtocolRequestContext.Default, Uri(path), 1, ByteStringValue.FromString(text), LanguageKind.TypeScript);
    private static TypeScript.LanguageServices.LanguageService Service(Session session, string path)
    {
        var(service, error) = session.GetLanguageService(ProtocolRequestContext.Default, Uri(path));
        Check(error is null);
        return service!;
    }

    private static void Watched(Session session, string path, FileChangeType kind) => session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Uri = Uri(path), Type = kind }]));
    public static int RunAll()
    {
        TestConfigFileChanges();
        return 1;
    }

    public static void TestConfigFileChanges()
    {
        Action[] groups = [() => OptionsChange("/src/tsconfig.json", "{\"extends\": \"../tsconfig.base.json\", \"compilerOptions\": {\"target\": \"esnext\"}, \"references\": [{\"path\": \"../utils\"}]}", static options => options.Target == ScriptTarget.ESNext), () => OptionsChange("/tsconfig.base.json", "{\"compilerOptions\": {\"strict\": false}}", static options => options.Strict == Tristate.False), () => OptionsChange("/tsconfig.more-base.json", "{\"compilerOptions\": {\"verbatimModuleSyntax\": true}}", static options => options.VerbatimModuleSyntax == Tristate.True), Referenced, Deleted, CreatedThenDeleted, MissingExtended];
        string[] names = ["should update program options on config file change", "should update project on extended config file change", "should update project on doubly extended config file change", "should update project on referenced config file change", "should close project on config file deletion", "config file creation then deletion", "should update project when missing extended config is created"];
        List<Exception>? failures = null;
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

    private static void OptionsChange(string path, string text, Func<CompilerOptions, bool> expected)
    {
        using var fixture = new ProjectSessionFixture(Files());
        var session = fixture.Session;
        Open(session, "/src/index.ts", IndexText);
        Check(fixture.MutableFS.WriteFile(File(path), text)is null);
        Watched(session, path, FileChangeType.Changed);
        Check(expected(Service(session, "/src/index.ts").GetProgram().Options()));
    }

    private static void Referenced()
    {
        using var fixture = new ProjectSessionFixture(Files());
        var session = fixture.Session;
        Open(session, "/src/index.ts", IndexText);
        var before = session.Snapshot();
        Check(fixture.MutableFS.WriteFile(File("/utils/tsconfig.json"), "{\"compilerOptions\": {\"composite\": true, \"target\": \"esnext\"}}")is null);
        Watched(session, "/utils/tsconfig.json", FileChangeType.Changed);
        _ = Service(session, "/src/index.ts");
        Check(!ReferenceEquals(before, session.Snapshot()));
    }

    private static void Deleted()
    {
        using var fixture = new ProjectSessionFixture(Files());
        var session = fixture.Session;
        Open(session, "/src/index.ts", IndexText);
        Check(fixture.MutableFS.Remove(File("/src/tsconfig.json").AsPath())is null);
        Watched(session, "/src/tsconfig.json", FileChangeType.Deleted);
        _ = Service(session, "/src/index.ts");
        var snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ProjectCollection.InferredProject()is not null);
    }

    private static void CreatedThenDeleted()
    {
        using var fixture = new ProjectSessionFixture(Files());
        var session = fixture.Session;
        Open(session, "/src/subfolder/foo.ts", FooText);
        Check(fixture.MutableFS.WriteFile(File("/src/subfolder/tsconfig.json"), "{}")is null);
        Watched(session, "/src/subfolder/tsconfig.json", FileChangeType.Created);
        _ = Service(session, "/src/subfolder/foo.ts");
        var snapshot = session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 2);
        Check(snapshot.GetDefaultProject(Uri("/src/subfolder/foo.ts"))!.ConfigFileName().AsValue().EqualsString("/src/subfolder/tsconfig.json"));
        Check(fixture.MutableFS.Remove(File("/src/subfolder/tsconfig.json").AsPath())is null);
        Watched(session, "/src/subfolder/tsconfig.json", FileChangeType.Deleted);
        _ = Service(session, "/src/subfolder/foo.ts");
        snapshot = session.Snapshot();
        Check(snapshot.GetDefaultProject(Uri("/src/subfolder/foo.ts"))!.ConfigFileName().AsValue().EqualsString("/src/tsconfig.json"));
        Check(snapshot.ProjectCollection.Projects().Length == 2);
        Open(session, "/src/index.ts", IndexText);
        Check(session.Snapshot().ProjectCollection.Projects().Length == 1);
    }

    private static void MissingExtended()
    {
        using var fixture = new ProjectSessionFixture(Files().Where(static file => file.Path != "/tsconfig.base.json"));
        var session = fixture.Session;
        Open(session, "/src/index.ts", IndexText);
        Check(fixture.MutableFS.WriteFile(File("/tsconfig.base.json"), "{\"compilerOptions\": {\"strict\": true}}")is null);
        Watched(session, "/tsconfig.base.json", FileChangeType.Created);
        Check(Service(session, "/src/index.ts").GetProgram().Options().Strict == Tristate.True);
    }
}
