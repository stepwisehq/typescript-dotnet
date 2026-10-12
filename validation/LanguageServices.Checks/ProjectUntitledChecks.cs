// All three original project/untitled_test.go functions.
using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.Lsconv;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static class ProjectUntitledChecks
{
    private const string Content = "let x = 42;\n\nx\n\nx++;";
    private static DocumentUri Uri(string raw) => new(ByteStringValue.FromString(raw));
    private static void Check([DoesNotReturnIf(false)] bool value)
    {
        if (!value)
            throw new InvalidOperationException("original untitled assertion failed");
    }

    public static int RunAll()
    {
        List<Exception>? errors = null;
        foreach (var(index, run)in new (int, Action)[]
        {
            (0, TestUntitledReferences),
            (1, TestUntitledFileInInferredProject),
            (2, TestImportsInUntitled)
        }

        )
            try
            {
                run();
            }
            catch (Exception error)
            {
                (errors ??= new()).Add(new InvalidOperationException("original untitled " + index, error));
            }

        if (errors is not null)
            throw new AggregateException(errors);
        return 3;
    }

    public static void RunOriginal(int index)
    {
        switch (index)
        {
            case 0:
                TestUntitledReferences();
                break;
            case 1:
                TestUntitledFileInInferredProject();
                break;
            case 2:
                TestImportsInUntitled();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public static void TestUntitledReferences()
    {
        var untitled = Uri("untitled:Untitled-2");
        var converted = untitled.FileName();
        var back = Lsconv.FileNameToDocumentURI(converted);
        Check(back.Value.EqualsValue(untitled.Value));
        using var fixture = new ProjectSessionFixture([new Input("/Untitled-2.ts", Content)]);
        var ctx = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("0"));
        var uri = Uri("file:///Untitled-2.ts");
        fixture.Session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(Content), LanguageKind.TypeScript);
        var(service, error) = fixture.Session.GetLanguageService(ctx, uri);
        Check(error is null);
        Check(service is not null);
        // Original source-file lookup is a log observation, not a new expectation.
        var sourceFile = service.GetProgram().GetSourceFile(RootedFilePath.FromGoString("/Untitled-2.ts"));
        Console.WriteLine(sourceFile!.FileName());
        Position position = new()
        {
            Line = 2,
            Character = 0
        };
        var parameters = new ReferenceParams
        {
            TextDocument = new()
            {
                Uri = uri
            },
            Position = position,
            Context = new()
            {
                IncludeDeclaration = true
            }
        };
        var(response, referenceError) = service.ProvideReferences(ctx, parameters, null);
        Check(referenceError is null);
        Check(response.Locations is not null);
        Check(response.Locations.Value.Length == 3);
        var(_, definitionError) = service.ProvideDefinition(ctx, uri, position);
        Check(definitionError is null);
    }

    public static void TestUntitledFileInInferredProject()
    {
        using var fixture = new ProjectSessionFixture([]);
        var ctx = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("0"));
        var uri = Uri("untitled:Untitled-2");
        fixture.Session.DidOpenFile(ctx, Uri("untitled:Untitled-1"), 1, ByteStringValue.FromString("x\n\n"), LanguageKind.TypeScript);
        fixture.Session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(Content), LanguageKind.TypeScript);
        Check(fixture.Session.Snapshot().ProjectCollection.InferredProject()is not null);
        var(service, error) = fixture.Session.GetLanguageService(ctx, uri);
        Check(error is null);
        Check(service is not null);
        var source = service.GetProgram().GetSourceFile(uri.FileName());
        Check(source is not null);
        Check(ByteStringValue.FromView(source.TextView()).EqualsString(Content));
        var parameters = new ReferenceParams
        {
            TextDocument = new()
            {
                Uri = uri
            },
            Position = new()
            {
                Line = 2,
                Character = 0
            },
            Context = new()
            {
                IncludeDeclaration = true
            }
        };
        var(response, referenceError) = service.ProvideReferences(ctx, parameters, null);
        Check(referenceError is null);
        Check(response.Locations is not null);
        foreach (var location in response.Locations.Value)
            Check(location.Uri.Value.StartsWith("untitled:"u8));
        Check(response.Locations.Value.Length == 4);
    }

    public static void TestImportsInUntitled()
    {
        using var fixture = new ProjectSessionFixture([new Input("/home/src/Library/Caches/typescript/node_modules/@types/somelib/index.d.ts", "export const x: number;")]);
        const string content = "import \"https://deno.land/std@0.208.0/path/mod.ts\"\n\t\timport  \"./relative\"\n";
        var ctx = ProtocolRequestContext.Default;
        var uri = Uri("untitled:Untitled-1");
        fixture.Session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(content), LanguageKind.TypeScript);
        fixture.Session.WaitForBackgroundTasks();
        var(_, error) = fixture.Session.GetLanguageService(ctx, uri);
        Check(error is null);
    }
}
