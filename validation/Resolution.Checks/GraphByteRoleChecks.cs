// Authored regression for the explicit graph byte filename versus legacy scalar
// filename role. Calls the real graph parser and the existing binder producer.
using System.Text;
using Configuration.Checks;
using TypeScript.Compiler;
using TypeScript.Scanner;
using OptionsAPI = TypeScript.TsOptions.TsOptions;

namespace Resolution.Checks;
public static class GraphByteRoleChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["Graph.UnicodeFileNameBinderAndDiagnosticRoles"];

    public static void Run(string name)
    {
        if (name != Cases[0])
            throw new ArgumentException("Unknown graph byte-role control.", nameof(name));
        const string validName = "/repo/é-𝄞.ts";
        const string invalidName = "/repo/子-invalid.ts";
        const string source = "export const value = 1;";
        var fs = ResolutionFixtures.Fs(("/repo/tsconfig.json", "{\"compilerOptions\":{\"module\":\"esnext\",\"moduleResolution\":\"bundler\"},\"files\":[\"é-𝄞.ts\",\"子-invalid.ts\"]}"), (validName, source), (invalidName, "export const broken = ;"));
        var(parsed, errors) = OptionsAPI.GetParsedCommandLineOfConfigFile(RootedFilePath.FromUnicode("/repo/tsconfig.json"), null, null, fs);
        Check.True(parsed is not null && errors.Length == 0 && parsed.Errors.Length == 0, "Actual parsed Unicode graph configuration");
        var graph = ResolvedProjectGraph.FromParsedConfiguration(parsed!, fs);
        SourceFile file = GoSlices.Values(graph.SourceFiles).Single(file => file.FileName() == GoString.ToUtf8(validName));
        Binder.BindSourceFile(file);
        string expectedSymbol = GoString.ToUtf8("\"/repo/é-𝄞\"");
        Check.Equal(file.Symbol.Name, expectedSymbol, "Actual graph binder external-module byte name");
        Check.Equal(file.FileNameGoBytes(), GoString.ToUtf8(validName), "Graph byte role avoids a second UTF8 conversion");
        Check.Equal(file.PathKey(), GoString.ToUtf8(validName), "Actual graph path key retains source bytes");
        SourceFile invalid = GoSlices.Values(graph.SourceFiles).Single(file => file.FileName() == GoString.ToUtf8(invalidName));
        Diagnostic diagnostic = graph.ParseDiagnostics.First(diagnostic => diagnostic.Code() == 1109 && ReferenceEquals(diagnostic.File(), invalid));
        Check.True(ReferenceEquals(diagnostic.File(), invalid), "Actual graph parse diagnostic owner identity");
        Check.Equal(diagnostic.File().FileName(), GoString.ToUtf8(invalidName), "Actual diagnostic byte filename");
        Check.Equal(diagnostic.File().PathKey(), GoString.ToUtf8(invalidName), "Actual diagnostic byte path");
        SourceFile legacy = Parser.ParseSourceFile(new SourceFileParseOptions(validName, validName), Encoding.UTF8.GetBytes(source), ScriptKind.TS);
        Binder.BindSourceFile(legacy);
        Check.Equal(legacy.FileName(), validName, "Legacy parser filename remains scalar Unicode");
        Check.Equal(legacy.FileNameGoBytes(), GoString.ToUtf8(validName), "Legacy scalar role converts exactly once");
        Check.Equal(legacy.Symbol.Name, expectedSymbol, "Actual legacy binder matches original module byte name");

        // The managed default struct stores a null filename. Go's corresponding
        // zero string is empty; both explicit filename roles must bind it as "".
        SourceFileParseOptions[] emptyOptions =
        [
            default,
            new("", ""),
            default(SourceFileParseOptions) with { FileNameIsGoBytes = true },
            new("", "", FileNameIsGoBytes: true)
        ];
        foreach (SourceFileParseOptions options in emptyOptions)
        {
            SourceFile empty = Parser.ParseSourceFile(options, "export const x=1;", ScriptKind.TS);
            Binder.BindSourceFile(empty);
            Check.True(empty.IsBound(), "Default or empty filename module is bound");
            Check.Equal(empty.FileName(), options.FileName, "Public filename representation is preserved");
            Check.Equal(empty.FileNameGoBytes(), "", "Filename byte adapter retains the Go zero string");
            Check.Equal(empty.Symbol.Name, "\"\"", "Default or empty filename binds as a quoted-empty module name");
            Check.True(empty.Symbol.Exports["x"] is not null, "Quoted-empty module retains its actual export");
        }
    }
}
