// Authored genuine ordinary Program.Emit controls; exact pinned-Go public observations.
using System.Text.Json;
using TypeScript.Compiler;
using TypeScript.Scanner;

namespace Emit.Checks;
public static class EmitModeChecks
{
    public static IEnumerable<string> Names => Expected.Keys;

    public static int RunAll()
    {
        foreach (string name in Names)
            Run(name);
        return Expected.Count;
    }

    public static void Run(string name)
    {
        using var expected = JsonDocument.Parse(Expected[name]);
        var options = new CompilerOptions
        {
            Target = ScriptTarget.ESNext,
            Module = ModuleKind.ESNext,
            SkipLibCheck = Tristate.True,
            ListEmittedFiles = Tristate.True,
            NewLine = NewLineKind.LF,
            NoEmit = name == "TargetSourceFile" ? Tristate.Unknown : Tristate.True
        };
        var fs = TypeScript.Bundled.Bundled.WrapFS(new Program.Checks.CompilerBaselineFileSystem(new[] { new Program.Checks.CompilerBaselineFileSystem.Input("/fixture/main.ts", GoString.ToUtf8("export const café: number = 1;\n")), new Program.Checks.CompilerBaselineFileSystem.Input("/fixture/other.ts", "export const other: string = 'value';\n") }));
        var config = TypeScript.TsOptions.TsOptions.NewParsedCommandLine(options, [RootedFilePath.FromUnicode("/fixture/main.ts"), RootedFilePath.FromUnicode("/fixture/other.ts")], null, RootedDirectoryPath.FromUnicode("/fixture"), fs.CaseSensitivity());
        var program = TypeScript.Compiler.Program.NewProgram(new() { Config = config, Host = CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath()), SingleThreaded = Tristate.True });
        var file = program.GetSourceFile(RootedFilePath.FromUnicode("/fixture/main.ts")) ?? throw new InvalidOperationException("Missing genuine target source file.");
        var targets = Slice<SourceFile>.Make(0, 1).Append(file);
        if (name != "TargetSourceFile")
        {
            var initial = program.Emit() ?? throw new InvalidOperationException("Missing NoEmit result.");
            Require(!initial.EmitSkipped && initial.Diagnostics.IsNil && initial.EmittedFiles.IsNil && initial.SourceMaps.IsNil, "NoEmit nil-target empty result changed.");
            var selected = program.Emit(default(CancellationToken), new() { TargetSourceFiles = targets }) ?? throw new InvalidOperationException("Missing selected NoEmit result.");
            Require(selected.EmitSkipped && selected.Diagnostics.IsNil && selected.EmittedFiles.IsNil && selected.SourceMaps.IsNil, "NoEmit selected-target skipped result changed.");
        }

        var writes = new List<(RootedFilePath Name, string Text, WriteFileData Data)>();
        var emitOptions = new EmitOptions
        {
            TargetSourceFiles = targets,
            WriteFile = (path, text, data) =>
            {
                if (name == "ForceDtsSkippedWrite")
                    data.SkippedDtsWrite = true;
                writes.Add((path, text, data));
                return null;
            }
        };
        if (name == "ForceJsDespiteNoEmit")
        {
            emitOptions.EmitOnly = EmitOnly.EmitOnlyJs;
            emitOptions.ForceEmit = true;
        }

        if (name == "ForceDtsSkippedWrite")
        {
            emitOptions.EmitOnly = EmitOnly.EmitOnlyDts;
            emitOptions.ForceEmit = true;
        }

        if (name == "BuilderSignatureDespiteNoEmit")
            emitOptions.EmitOnly = EmitOnly.EmitOnlyBuilderSignature;
        var result = program.Emit(default(CancellationToken), emitOptions) ?? throw new InvalidOperationException("Missing public emit result.");
        var root = expected.RootElement;
        Require(result.EmitSkipped == root.GetProperty("EmitSkipped").GetBoolean(), "EmitSkipped differs.");
        Require(result.Diagnostics.Count == root.GetProperty("Diagnostics").GetArrayLength(), "Full empty diagnostic channel differs.");
        Require(result.SourceMaps.IsNil == root.GetProperty("SourceMapsNil").GetBoolean(), "Source map nil channel differs.");
        var expectedFiles = root.GetProperty("EmittedFiles");
        Require(result.EmittedFiles.IsNil == (expectedFiles.ValueKind == JsonValueKind.Null), "Emitted-file nil channel differs.");
        Require(result.EmittedFiles.Count == (expectedFiles.ValueKind == JsonValueKind.Null ? 0 : expectedFiles.GetArrayLength()), "Emitted-file channel differs.");
        for (int i = 0; i < result.EmittedFiles.Count; i++)
            Require(result.EmittedFiles[i].AsString() == GoString.ToUtf8(expectedFiles[i].GetString()!), "Emitted filename differs.");
        var expectedWrites = root.GetProperty("Writes");
        Require(writes.Count == expectedWrites.GetArrayLength(), "Target selection/write count differs.");
        for (int i = 0; i < writes.Count; i++)
        {
            var value = expectedWrites[i];
            var write = writes[i];
            Require(write.Name.AsString() == GoString.ToUtf8(value.GetProperty("Name").GetString()!), "Callback filename differs.");
            Require(write.Text == GoString.ToUtf8(value.GetProperty("Text").GetString()!), "Exact raw callback bytes differ.");
            Require((write.Data.SourceFile?.FileNameGoBytes() ?? "") == GoString.ToUtf8(value.GetProperty("SourceFile").GetString()!), "Callback source filename differs.");
            Require(write.Data.SourceMapUrlPos == value.GetProperty("SourceMapUrlPos").GetInt32(), "SourceMapUrlPos differs.");
            Require((write.Data.BuildInfo is null) == value.GetProperty("BuildInfoNil").GetBoolean(), "Callback buildinfo channel differs.");
            Require(write.Data.Diagnostics.IsNil == value.GetProperty("DiagnosticsNil").GetBoolean() && write.Data.Diagnostics.Count == 0, "Callback diagnostic channel differs.");
            Require(write.Data.SkippedDtsWrite == value.GetProperty("SkippedDtsWrite").GetBoolean(), "Callback declaration skip differs.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static readonly IReadOnlyDictionary<string, string> Expected = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["TargetSourceFile"] = """
{"Name":"TargetSourceFile","Writes":[{"Name":"/fixture/main.js","Text":"export const café = 1;\n","SourceFile":"/fixture/main.ts","SourceMapUrlPos":-1,"BuildInfoNil":true,"DiagnosticsNil":false,"SkippedDtsWrite":false}],"EmitSkipped":false,"Diagnostics":[],"EmittedFiles":["/fixture/main.js"],"SourceMapsNil":true}
""",
        ["ForceJsDespiteNoEmit"] = """
{"Name":"ForceJsDespiteNoEmit","Writes":[{"Name":"/fixture/main.js","Text":"export const café = 1;\n","SourceFile":"/fixture/main.ts","SourceMapUrlPos":-1,"BuildInfoNil":true,"DiagnosticsNil":false,"SkippedDtsWrite":false}],"EmitSkipped":false,"Diagnostics":[],"EmittedFiles":["/fixture/main.js"],"SourceMapsNil":true}
""",
        ["BuilderSignatureDespiteNoEmit"] = """
{"Name":"BuilderSignatureDespiteNoEmit","Writes":[{"Name":"/fixture/main.d.ts","Text":"export declare const café: number;\n","SourceFile":"/fixture/main.ts","SourceMapUrlPos":-1,"BuildInfoNil":true,"DiagnosticsNil":false,"SkippedDtsWrite":false}],"EmitSkipped":false,"Diagnostics":[],"EmittedFiles":["/fixture/main.d.ts"],"SourceMapsNil":true}
""",
        ["ForceDtsSkippedWrite"] = """
{"Name":"ForceDtsSkippedWrite","Writes":[{"Name":"/fixture/main.d.ts","Text":"export declare const café: number;\n","SourceFile":"/fixture/main.ts","SourceMapUrlPos":-1,"BuildInfoNil":true,"DiagnosticsNil":false,"SkippedDtsWrite":true}],"EmitSkipped":false,"Diagnostics":[],"EmittedFiles":null,"SourceMapsNil":true}
""",
    };
}
