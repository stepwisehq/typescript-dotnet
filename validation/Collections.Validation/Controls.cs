// Authored shared xUnit/Native AOT controls. The adapted original case preserves
// three original nonnil symbol assertions; its fixed host/options are different.
using TypeScript.Scanner;

namespace TypeScript.Checking.CollectionsValidation;
public static class CollectionControls
{
    public static IReadOnlyList<string> Cases { get; } = ["AdaptedOriginal.TestGetSymbolAtLocation", "Additional.FixedEnvironmentOwnershipAndIsolation", "Additional.FixedOptionsAndSkipPolicy", "Additional.ResolvedRestAndDestructuringBoundaries", ];

    private static SourceFile Parse(string text) => Parser.ParseSourceFile(new SourceFileParseOptions("/main.ts", "/main.ts"), text, ScriptKind.TS);
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "AdaptedOriginal.TestGetSymbolAtLocation":
            {
                // checker_test.go:24. Source and three assertion selections exact;
                // original config/VFS/default-lib host replaced by fixed profile.
                var file = Parse("interface Foo {\n  bar: string;\n}\ndeclare const foo: Foo;\nfoo.bar;");
                var checker = Checker.Create(file, CheckerProfile.Collections);
                var statements = file.Statements.Nodes;
                Node[] nodes = [statements[0].Name(), statements[1].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes[0].Name(), statements[2].Expression()];
                foreach (var node in nodes)
                    Assert(checker.GetSymbolAtLocation(node)is not null, "Expected symbol to be non-nil");
                break;
            }

            case "Additional.FixedEnvironmentOwnershipAndIsolation":
            {
                var first = Checker.Create(Parse("const value=1;"), CheckerProfile.Collections);
                var second = Checker.Create(Parse("const value=1;"), CheckerProfile.Collections);
                Assert(first.SourceFiles.Count == 4 && second.SourceFiles.Count == 4, "Expected fixed environment");
                for (int i = 0; i < 3; i++)
                {
                    Assert(!ReferenceEquals(first.SourceFiles[i], second.SourceFiles[i]), "Library AST must be fresh per environment");
                    Assert(!ReferenceEquals(first.SourceFiles[i].AsNode(), second.SourceFiles[i].AsNode()), "Library node ownership must be independent");
                }

                Symbol a = first.Globals["Array"], b = second.Globals["Array"];
                Assert(!ReferenceEquals(a, b), "Bound globals must be isolated");
                Assert(first.GetTypeOfSymbol(a)is not null, "Owned library symbol API must work");
                bool rejected = false;
                try
                {
                    first.GetTypeOfSymbol(b);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }

                Assert(rejected, "Foreign library symbol must be rejected");
                break;
            }

            case "Additional.FixedOptionsAndSkipPolicy":
            {
                var checker = Checker.Create(Parse("const value=1;"), CheckerProfile.Collections);
                var options = checker.compilerOptions;
                Assert(options.SkipLibCheck == Tristate.True && options.NoResolve == Tristate.True, "Actual fixed compiler options");
                Assert(options.StrictFunctionTypes == Tristate.True && options.NoImplicitAny == Tristate.True && options.NoImplicitThis == Tristate.True && options.StrictNullChecks == Tristate.True, "Selected strict flags");
                for (int i = 0; i < 4; i++)
                    Assert(TypeScript.Compiler.Program.SkipTypeChecking(checker.SourceFiles[i], options) == (i < 3), "Declaration skip policy must differ from user checking");
                break;
            }

            case "Additional.ResolvedRestAndDestructuringBoundaries":
            {
                string[] excluded = ["type Id<X>=X;function f<T extends unknown[]>(...args:Id<T>){}", "type Id<X>=X;type V<T extends unknown[]>=[...Id<T>];", "function f<T extends unknown[]>(...args:NoInfer<T>){}", "function f<T extends unknown[]>(...args:T|[]){}", "function f<T extends unknown[]>(...args:T&unknown[]){}", "let x=0;({x}={x:1});", "let x=0;[x]=[1];", ];
                foreach (string source in excluded)
                {
                    bool rejected = false;
                    try
                    {
                        Checker.Create(Parse(source), CheckerProfile.Collections).GetDiagnostics();
                    }
                    catch (UnsupportedSemanticFeatureException)
                    {
                        rejected = true;
                    }

                    Assert(rejected, "Excluded resolved generic rest or destructuring was admitted: " + source);
                }

                break;
            }

            default:
                throw new ArgumentException("Unknown collection control", nameof(name));
        }
    }
}
