// Authored checks of Program's lazy genuine checker acquisition and scheduler.
// These are behavioral controls, not translations of upstream test bodies.
using Configuration.Checks;
using TypeScript.Checking;
using TypeScript.Compiler;
using TypeScript.Scanner;
using TypeScript.Vfs;
using CompilerProgram = TypeScript.Compiler.Program;

namespace Program.Checks;

public static class ConstructionBoundaryChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["WorkGroupScheduling", "LazySemanticAdmission"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "WorkGroupScheduling": Scheduling(); break;
            case "LazySemanticAdmission": SemanticAdmission(); break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static void Scheduling()
    {
        foreach (bool singleThreaded in new[] { true, false })
        {
            var work = Core.NewWorkGroup(singleThreaded);
            int completed = 0;
            work.Queue(() =>
            {
                Interlocked.Increment(ref completed);
                work.Queue(() =>
                {
                    Interlocked.Increment(ref completed);
                    work.Queue(() => Interlocked.Increment(ref completed));
                });
            });
            work.RunAndWait();
            Check.Equal(completed, 3, "recursive queued work finishes before return");
            Throws<InvalidOperationException>(() => work.Queue(() => { }), "late Queue fails");

            var failed = Core.NewWorkGroup(singleThreaded);
            var expected = new InvalidOperationException("authored worker failure");
            failed.Queue(() => throw expected);
            Check.True(ReferenceEquals(Throws<InvalidOperationException>(failed.RunAndWait, "worker failure propagates"), expected), "original managed worker exception retained");
            Throws<InvalidOperationException>(() => failed.Queue(() => { }), "failed group is finished");
        }

        var serial = Core.NewWorkGroup(true);
        List<int> order = [];
        serial.Queue(() => order.Add(1));
        serial.Queue(() => { order.Add(2); serial.Queue(() => order.Add(3)); });
        serial.RunAndWait();
        Check.Sequence(order, new[] { 2, 3, 1 }, "single-threaded recursive LIFO");
    }

    private static void SemanticAdmission()
    {
        // Authored Go signed-integer range witness: tuple normalization reaches
        // the shared runtime adapter with a negative upper bound.
        Check.Sequence(GoRuntime.RangeKeys(-1).ToList(), Array.Empty<int>(), "negative Go integer range is empty");
        Check.Sequence(GoRuntime.RangeKeys(0).ToList(), Array.Empty<int>(), "zero Go integer range is empty");
        Check.Sequence(GoRuntime.RangeKeys(3).ToList(), new[] { 0, 1, 2 }, "positive Go integer range");

        var declarations = Construct(new Dictionary<string, string>
        {
            ["/src/tsconfig.json"] = "{\"compilerOptions\":{\"noLib\":true,\"types\":[]},\"files\":[\"ambient.d.ts\"]}",
            ["/src/ambient.d.ts"] = "declare interface Ambient { value: string; }"
        });
        Check.Equal(declarations.GetSourceFiles().Count, 1, "declaration environment construction is unrestricted");
        var declarationChecker = declarations.GetTypeChecker();
        try
        {
            var file = declarations.GetSourceFile(RootedFilePath.FromGoString("/src/ambient.d.ts"))!;
            Check.True(declarationChecker.Checker.GetSymbolAtLocation(file.Statements.Nodes[0].Name()) is not null,
                "declaration-only acquisition binds actual ambient interface");
        }
        finally { declarationChecker.Done(); }
        var missingGlobals = declarations.GetGlobalDiagnostics();
        Check.Equal(missingGlobals.Length, 10, "actual noLib environment reports missing globals");
        Check.True(GoSlices.Values(missingGlobals).All(diagnostic => diagnostic.Code() == Diagnostics.Cannot_find_global_type_0.Code()),
            "missing globals are real source diagnostics, not acquisition rejection");

        var fullEnvironment = Construct(new Dictionary<string, string>
        {
            ["/src/tsconfig.json"] = "{\"compilerOptions\":{\"target\":\"esnext\",\"types\":[]},\"files\":[\"main.ts\"]}",
            ["/src/main.ts"] = "export const value = 1;"
        }, bundled: true);
        Check.True(fullEnvironment.GetSourceFiles().Count > 4, "full genuine bundled environment loads before checker acquisition");
        var fullChecker = fullEnvironment.GetTypeChecker();
        try
        {
            var file = fullEnvironment.GetSourceFile(RootedFilePath.FromGoString("/src/main.ts"))!;
            var declaration = file.Statements.Nodes[0].AsVariableStatement().DeclarationList
                .AsVariableDeclarationList().Declarations.Nodes[0];
            Check.True(fullChecker.Checker.GetSymbolAtLocation(declaration.Name()) is not null,
                "actual later-library environment supports public source symbol query");
        }
        finally { fullChecker.Done(); }
        Check.Equal(fullEnvironment.GetGlobalDiagnostics().Length, 0, "actual later library globals initialize");

        var libraries = FixedLibraryEnvironment.Load();
        var inputs = new Dictionary<string, string>
        {
            ["/src/tsconfig.json"] = "{\"files\":[\"main.ts\"]}",
            ["/src/main.ts"] = "const value: number = 1;"
        };
        foreach (var library in libraries) inputs.Add(library.FileName(), library.Text());
        IFS fs = ProgramChecks.FS(inputs);
        var parsed = ProgramChecks.Parse(fs);
        // Authored source-API control: use the existing admitted option record,
        // with all three unchanged declaration byte streams explicitly rooted.
        parsed.SetCompilerOptions(ProjectConfiguration.CreateOptions());
        parsed = parsed.WithFileNames(new[] { ProgramChecks.File("/src/main.ts") }.Concat(libraries.Select(file => ProgramChecks.File(file.FileName()))).ToList());
        var admitted = CompilerProgram.NewProgram(new ProgramOptions { Config = parsed, Host = ProgramChecks.Host(fs), SingleThreaded = Tristate.True });
        Check.Equal(admitted.GetSourceFiles().Count, 4, "supported profile retains user and all three declarations");
        var acquired = admitted.GetTypeChecker();
        Check.True(acquired.Checker is not null, "supported profile acquires the existing checker");
        acquired.Done();
        Check.Equal(admitted.GetSemanticDiagnostics().Length, 0, "supported simple program semantic diagnostics");
    }

    private static CompilerProgram Construct(Dictionary<string, string> inputs, bool bundled = false)
    {
        IFS fs = ProgramChecks.FS(inputs);
        if (bundled) fs = TypeScript.Bundled.Bundled.WrapFS(fs);
        var host = CompilerHosts.NewCompilerHost(fs, bundled ? TypeScript.Bundled.Bundled.LibPath() : RootedDirectoryPath.FromGoString("/"));
        return CompilerProgram.NewProgram(new ProgramOptions { Config = ProgramChecks.Parse(fs), Host = host, SingleThreaded = Tristate.True });
    }

    private static T Throws<T>(Action action, string context) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
        throw new InvalidOperationException(context + ": expected " + typeof(T).Name);
    }
}
