// Authored shared result roles for the original harnessutil compilation/baseline consumers.
// All text is raw Go-byte text; this is test observation, not a production emit host.
using TypeScript.Compiler;
using TypeScript.Scanner;
using TypeScript.Vfs;
using CP = TypeScript.Compiler.Program;

namespace Program.Checks;
public sealed record CompilerEmitTestFile(string UnitName, string Content);
public sealed partial class CompilerEmitCompilation
{
    public required CompilerNoEmitHarness.Compilation Prepared;
    public required EmitResult Result;
    public Slice<Diagnostic> Diagnostics;
    public readonly List<CompilerEmitTestFile> Inputs = [], JS = [], DTS = [], Maps = [], Outputs = [];
    public CP Program => Prepared.Program;
    public CompilerOptions Options => Program.Options();
    public CompilerNoEmitHarness.HarnessOptions HarnessSettings => Prepared.Harness;

    public RootedDirectoryPath CurrentDirectory() => Prepared.Harness.CurrentDirectory;
    public IFS FS() => Prepared.Host.FS();
    public int GetNumberOfJSFiles(bool includeJson) => includeJson ? JS.Count : JS.Count(file => !TsPath.FileExtensionIs(file.UnitName, TsPath.ExtensionJson));
}

public static partial class CompilerEmitBaselines
{
    // tsbaseline/util.go:26–50. Original strings.NewReplacer does one pass; replacements are not rescanned.
    private static readonly System.Text.RegularExpressions.Regex TestPathPrefixes = new("/\\.ts/|/\\.lib/|/\\.src/|bundled:///libs/|file:///\\./ts/|file:///\\./lib/|file:///\\./src/", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    public static string RemoveTestPathPrefixes(string text, bool retainTrailingDirectorySeparator) => TestPathPrefixes.Replace(text, match => match.Value.StartsWith("file:", StringComparison.Ordinal) ? "file:///" : retainTrailingDirectorySeparator ? "/" : "");
    // js_emit_baseline.go:153.
    public static string FileOutput(CompilerEmitTestFile file, CompilerNoEmitHarness.HarnessOptions settings) => "//// [" + (settings.FullEmitPaths ? RemoveTestPathPrefixes(file.UnitName, false) : TsPath.GetBaseFileName(file.UnitName)) + "]\r\n" + file.Content;
}
