// Complete TestProgram three-row assertion domain. Read-only fixture construction
// replaces test-only WriteFile seeding; every source byte, option and root is retained.
using Configuration.Checks;
using TypeScript.Compiler;
using TypeScript.Scanner;
using CompilerProgram = TypeScript.Compiler.Program;
using Bundled = TypeScript.Bundled.Bundled;
using ConfigParser = TypeScript.TsOptions.TsOptions;

namespace Program.Checks;

internal static class ProgramOrderChecks
{
    internal static void Run()
    {
        Check.True(Bundled.Embedded, "native tests require actual embedded pinned libraries");
        foreach (var row in ProgramOrderTables.Rows)
        {
            var fs = Bundled.WrapFS(new ConfigurationFileSystem(row.Files.Select(file =>
                new ConfigurationFileSystem.Input(file.Path, file.Text)), CaseSensitivity.CaseInsensitive));
            CompilerOptions options = new() { Target = ScriptTarget.ESNext };
            var parsed = ConfigParser.NewParsedCommandLine(options,
                [RootedFilePath.FromGoString("c:/dev/src/index.ts")], null,
                RootedDirectoryPath.FromGoString("c:/dev/src"), fs.CaseSensitivity());
            CompilerProgram program = CompilerProgram.NewProgram(new()
            {
                Config = parsed,
                Host = CompilerHosts.NewCompilerHost(fs, Bundled.LibPath()),
            });
            string prefix = Bundled.LibPath().AsString() + "/";
            string[] actual = program.GetSourceFiles().Select(file =>
            {
                string name = file.FileNameGoBytes();
                return name.StartsWith(prefix, StringComparison.Ordinal) ? name[prefix.Length..] : name;
            }).ToArray();
            string[] expected = [.. ProgramOrderTables.Libraries, .. row.Expected];
            Check.Sequence(actual, expected, row.Name);
        }
    }
}
