// Original project/contentmapper_test.go first two complete functions; assembled outside compilation.
using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using TypeScript.ContentMapper;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    private static void Require([DoesNotReturnIf(false)] bool condition, string message = "original contentmapper assertion failed")
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static DocumentUri Uri(string value) => new(ByteStringValue.FromString(value));
    private static SessionOptions Options(bool trusted = true) => new()
    {
        CurrentDirectory = RootedDirectoryPath.FromGoString("/home/project"),
        DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(),
        TypingsLocation = RootedDirectoryPath.FromGoString("/home/src/Library/Caches/typescript"),
        PositionEncoding = PositionEncodingKind.UTF8,
        RunExternalCode = trusted
    };
    private sealed class RecordingSpawner(ISpawner inner) : ISpawner
    {
        internal int Spawns, Closes;
        public (Stream? Duplex, Exception? Error) Spawn(Slice<ByteStringValue> command, ByteStringValue directory, Stream stderr)
        {
            var(stream, error) = inner.Spawn(command, directory, stderr);
            if (error is not null)
                return (null, error);
            Interlocked.Increment(ref Spawns);
            return (new RecordingStream(stream!, this), null);
        }
    }

    private sealed class RecordingStream(Stream inner, RecordingSpawner owner) : Stream
    {
        private int closed;
        public override bool CanRead => inner.CanRead;
        public override bool CanWrite => inner.CanWrite;
        public override bool CanSeek => inner.CanSeek;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override void Flush() => inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long length) => inner.SetLength(length);
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.Exchange(ref closed, 1) == 0)
                    Interlocked.Increment(ref owner.Closes);
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    public static void TestContentMapperProjectWithoutMappedFiles()
    {
        List<Exception>? failures = null;
        foreach (bool hasMapper in new[]
        {
            false,
            true
        }

        )
        {
            try
            {
                string config = hasMapper ? "{\n\t\t\t\t\t\"compilerOptions\": { \"noLib\": true },\n\t\t\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t\t\t\t}" : "{\"compilerOptions\": {\"noLib\": true}}";
                Input[] files = [new("/home/project/tsconfig.json", config), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper)), new("/home/project/main.ts", "export {};")];
                var spawner = new RecordingSpawner(ContentMapperProjectFixture.NewSpawner());
                using var fixture = new ProjectSessionFixture(files, Options()with { TypingsLocation = default }, spawner: spawner);
                fixture.Session.DidOpenFile(ProtocolRequestContext.Default, Uri("file:///home/project/main.ts"), 1, ByteStringValue.FromString("export {};"), LanguageKind.TypeScript);
                var(service, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, Uri("file:///home/project/main.ts"));
                Require(error is null, error?.Message ?? "");
                Require(service is not null);
                var program = service.GetProgram();
                var mapperProject = program.ContentMapperProject();
                Require((mapperProject is not null) == hasMapper);
                Require(ReferenceEquals(program.ContentMapperProject(), mapperProject));
                Require(Volatile.Read(ref spawner.Spawns) == 0);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException("hasMapper=" + hasMapper, error));
            }
        }

        if (failures is not null)
            throw new AggregateException(failures);
    }

    public static void TestContentMapperParallelFileLoading()
    {
        var files = new List<Input>
        {
            new("/home/project/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"noLib\": true },\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t\t}"),
            new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper)),
            new("/home/project/main.ts", "export {};")
        };
        const int fileCount = 32;
        for (int i = 0; i < fileCount; i++)
            files.Add(new("/home/project/file" + i + ".box", "export const version = #{target};\n"));
        using var fixture = new ProjectSessionFixture(files, Options()with { TypingsLocation = default }, spawner: ContentMapperProjectFixture.NewSpawner());
        fixture.Session.DidOpenFile(ProtocolRequestContext.Default, Uri("file:///home/project/main.ts"), 1, ByteStringValue.FromString("export {};"), LanguageKind.TypeScript);
        var(service, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, Uri("file:///home/project/main.ts"));
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        var mapperProject = service.GetProgram().ContentMapperProject();
        Require(mapperProject is not null);
        Require(ReferenceEquals(service.GetProgram().ContentMapperProject(), mapperProject));
        for (int i = 0; i < fileCount; i++)
        {
            string name = "/home/project/file" + i + ".box";
            var file = service.GetProgram().GetSourceFile(RootedFilePath.FromGoString(name));
            Require(file is not null, "expected " + name + " to be loaded");
            if (!ByteStringValue.FromView(file.TextView()).EqualsString("const __VERSION = \"1.0.0\";\nexport const version = 7;\n"))
            {
                var detail = new System.Text.StringBuilder();
                void AppendDiagnostic(TypeScript.Scanner.Diagnostic diagnostic)
                {
                    detail.Append("\n  ").Append(diagnostic.Code()).Append(": ").Append(diagnostic.Localize());
                    foreach (var child in diagnostic.MessageChain())
                        AppendDiagnostic(child);
                }
                foreach (var diagnostic in service.GetProgram().GetContentMapperDiagnostics())
                    AppendDiagnostic(diagnostic);
                foreach (var diagnostic in file.Diagnostics())
                    AppendDiagnostic(diagnostic);
                Require(false, "unexpected transformed text for " + name + ": " + GoQuote.Quote(ByteStringValue.FromView(file.TextView()).Materialize()!) + detail);
            }
        }
    }
}
