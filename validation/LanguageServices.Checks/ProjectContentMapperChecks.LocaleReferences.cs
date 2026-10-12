using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using TypeScript.Locale;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    public static void TestContentMapperLocaleChange()
    {
        const string main = "import { value } from \"./app.box\"; value;";
        Input[] files = [new("/home/project/tsconfig.json", "{ \"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ] }"), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.VerbatimMapper)), new("/home/project/app.box", "export const value = 1;\n"), new("/home/project/main.ts", main)];
        var spawner = new RecordingSpawner(ContentMapperProjectFixture.NewSpawner());
        using var fixture = new ProjectSessionFixture(files, Options(), spawner: spawner);
        object localeGate = new();
        var currentLocale = Locales.Default;
        fixture.Client.GetLocaleFunc = () =>
        {
            lock (localeGate)
                return currentLocale;
        };
        fixture.Client.SetLocaleFunc = value =>
        {
            var(updated, ok) = Locales.Parse(value.Materialize()!);
            Require(ok);
            lock (localeGate)
                currentLocale = updated;
        };
        var session = fixture.Session;
        var ctx = ProtocolRequestContext.Default.WithLocale(Locales.Default);
        int reads = fixture.Client.GetLocaleCalls().Length;
        var uri = Uri("file:///home/project/main.ts");
        session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(main), LanguageKind.TypeScript);
        var(_, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(fixture.Client.GetLocaleCalls().Length == reads + 1);
        Require(Volatile.Read(ref spawner.Spawns) == 1);
        var preferences = session.Config();
        preferences.Locale = "fr";
        session.Configure(preferences);
        Require(Volatile.Read(ref spawner.Closes) == 1);
        (_, error) = session.GetLanguageService(ProtocolRequestContext.Default, uri);
        Require(error is null, error?.Message ?? "");
        Require(Volatile.Read(ref spawner.Spawns) == 2);
    }

    public static void TestContentMappersInParallelProjectReferences()
    {
        Input[] files = [new("/home/project/tsconfig.json", "{\n\t\t\t\"files\": [\"src/index.d.ts\"],\n\t\t\t\"references\": [{ \"path\": \"./a\" }, { \"path\": \"./b\" }]\n\t\t}"), new("/home/project/src/index.d.ts", "export {};"), new("/home/project/a/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"composite\": true },\n\t\t\t\"files\": [\"../src/index.d.ts\"],\n\t\t\t\"contentMappers\": [{ \"package\": \"mapper\", \"extensions\": [\".vue\"] }]\n\t\t}"), new("/home/project/b/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"composite\": true },\n\t\t\t\"files\": [\"../src/index.d.ts\"],\n\t\t\t\"contentMappers\": [{ \"package\": \"mapper\", \"extensions\": [\".svelte\"] }]\n\t\t}"), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper))];
        using var fixture = new ProjectSessionFixture(files, Options(), spawner: ContentMapperProjectFixture.NewSpawner());
        fixture.Session.DidOpenFile(ProtocolRequestContext.Default, Uri("file:///home/project/src/index.d.ts"), 1, ByteStringValue.FromString("export {};"), LanguageKind.TypeScript);
        fixture.Session.WaitForBackgroundTasks();
        var calls = fixture.Client.RegisterContentMapperExtensionsCalls();
        Require(calls.Length > 0);
        var extensions = (ByteStringValue[])calls[^1].Clone();
        GoSliceSort.SortFunc(extensions.AsSpan(), static (a, b) => ByteStringKey.FromValue(a).CompareTo(ByteStringKey.FromValue(b)));
        Require(extensions.Length == 2);
        Require(extensions[0].EqualsString(".svelte"));
        Require(extensions[1].EqualsString(".vue"));
    }
}
