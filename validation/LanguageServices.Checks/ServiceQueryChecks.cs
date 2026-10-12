// Authored query-order controls for the actual Services/NodeBuilder cache-owner paths.
using TypeScript.Scanner;
using TypeScript.Checking;
using CompilerProgram = TypeScript.Compiler.Program;

namespace LanguageServices.Checks;
public static class ServiceQueryChecks
{
    public static readonly string[] Names = ["SignatureContextQueryRepetition", "ConfiguredHoverPreservesOrdinarySerialization"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        using var fixture = new QueryFixture();
        var checker = fixture.Checker;
        var call = fixture.Calls[0];
        var signature = checker.GetResolvedSignature(call);
        var returnType = checker.GetReturnTypeOfSignature(signature);
        var ordinary = checker.TypeToString(returnType);
        if (name == Names[0])
        {
            var symbol = checker.GetSymbolAtLocation(call.Expression());
            var definition = symbol.ValueDeclaration.Name();
            if (!checker.IsSymbolReferencedInFile(fixture.Source, definition, symbol))
                throw new InvalidOperationException("Equal identifier bytes at distinct source offsets lost references");
            var references = checker.GetReferencesToSymbolInFile(fixture.Source, symbol);
            if (references.Count != 3 || !Core.Some(references, node => ReferenceEquals(node, call.Expression()))
                || !Core.Some(references, node => ReferenceEquals(node, fixture.Calls[1].Expression())))
                throw new InvalidOperationException("Actual declaration and both call identifier references must survive byte-owner differences");
            for (int repeat = 0; repeat < 4; repeat++)
            {
                var help = Checker.GetResolvedSignatureForSignatureHelp(call, 1, checker);
                if (help.Signature == null || help.Candidates.Count == 0)
                    throw new InvalidOperationException("Signature help lost actual signature candidates");
                checker.GetContextualType(call.Arguments()[0], ContextFlags.IgnoreNodeInferences);
                checker.GetCandidateSignaturesForStringLiteralCompletions(call, call.Arguments()[0]);
                checker.GetResolvedSignature(fixture.Calls[1]);
                if (checker.TypeToString(checker.GetReturnTypeOfSignature(checker.GetResolvedSignature(call))) != ordinary)
                    throw new InvalidOperationException("Query ordering changed ordinary instantiated return serialization");
            }
        }
        else if (name == Names[1])
        {
            var verbosity = new VerbosityContext
            {
                Level = 1,
                MaxTruncationLength = 1000
            };
            for (int repeat = 0; repeat < 3; repeat++)
            {
                var expanded = checker.TypeToStringEx(returnType, call, TypeFormatFlags.UseAliasDefinedOutsideCurrentScope, verbosity);
                if (expanded.Length == 0)
                    throw new InvalidOperationException("Configured serialization returned empty output");
                checker.SignatureToStringEx(signature, call, TypeFormatFlags.WriteArrowStyleSignature, verbosity);
                if (checker.TypeToString(returnType) != ordinary)
                    throw new InvalidOperationException("Configured verbosity leaked into ordinary serialization");
            }
        }
        else
            throw new ArgumentException(name);
    }

    private sealed class QueryFixture : IDisposable
    {
        private readonly string directory;
        private readonly Action release;
        internal Checker Checker { get; }
        internal SourceFile Source { get; }
        internal List<Node> Calls { get; } = new();

        internal QueryFixture()
        {
            directory = Path.Combine(Path.GetTempPath(), "ls-query-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "main.ts").Replace('\\', '/');
            File.WriteAllText(path, "export {}; function choose<T>(value: T): T { return value; } const first = choose({ tag: 'a', count: 1 }); const second = choose({ tag: 'b', count: 2 });");
            var fs = TypeScript.Bundled.Bundled.WrapFS(TypeScript.Vfs.OsVfs.OsVfs.FS());
            var options = new CompilerOptions
            {
                NoLib = Tristate.True,
                Strict = Tristate.True,
                Target = ScriptTarget.ES2024,
                Module = ModuleKind.ESNext
            };
            var config = TypeScript.TsOptions.TsOptions.NewParsedCommandLine(options, [RootedFilePath.FromUnicode(path)], null, RootedDirectoryPath.FromUnicode(directory), fs.CaseSensitivity());
            var program = CompilerProgram.NewProgram(new() { Config = config, Host = TypeScript.Compiler.CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath()), SingleThreaded = Tristate.True });
            var source = program.GetSourceFile(RootedFilePath.FromUnicode(path)) ?? throw new InvalidOperationException("Genuine query fixture source file was not loaded");
            Source = source;
            var lease = program.GetTypeCheckerForFile(default(CancellationToken), source);
            Checker = lease.Item1;
            release = lease.Item2;
            Visitor visit = null!; // Assigned before the recursive worker is invoked.
            visit = node =>
            {
                if (node.Kind == Kind.CallExpression)
                    Calls.Add(node);
                node.ForEachChild(visit);
                return false;
            };
            source.AsNode().ForEachChild(visit);
            if (Calls.Count != 2)
                throw new InvalidOperationException("Actual fixture calls missing");
        }

        public void Dispose()
        {
            release();
            Directory.Delete(directory, true);
        }
    }
}
