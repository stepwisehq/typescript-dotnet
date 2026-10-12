// Authored genuine disk emit/write boundary controls; not original upstream assertions.
using TypeScript.Compiler;
using TypeScript.Scanner;
using TypeScript.Vfs;
using TypeScript.Vfs.OsVfs;

namespace Emit.Checks;
public static class EmitWriteFailureChecks
{
    public static int RunAll()
    {
        WriteFailure();
        ExistingOutputDirectory();
        RawOutputBytes();
        return 3;
    }

    public static void WriteFailure() => WriteFailure(false);
    public static void ExistingOutputDirectory() => WriteFailure(true);
    private static void WriteFailure(bool existingOutputDirectory)
    {
        string root = Path.Combine(Path.GetTempPath(), "emit-write-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "main.ts"), "export const value = 1;\n");
            string outputDirectory = existingOutputDirectory ? "out" : "blocker/out";
            if (existingOutputDirectory)
                Directory.CreateDirectory(Path.Combine(root, "out", "main.js"));
            else
                File.WriteAllText(Path.Combine(root, "blocker"), "retained blocker");
            File.WriteAllText(Path.Combine(root, "tsconfig.json"), "{\"compilerOptions\":{\"target\":\"esnext\",\"module\":\"esnext\",\"skipLibCheck\":true,\"listEmittedFiles\":true,\"outDir\":\"" + outputDirectory + "\"},\"files\":[\"main.ts\"]}");
            var output = RootedFilePath.FromUnicode(Path.Combine(root, outputDirectory, "main.js"));
            // The actual platform error is retained, not replaced by a permissive callback.
            var error = OutputTextWriters.Write(OsVfs.FS(), output, ByteStringValue.FromString("probe")) ?? throw new InvalidOperationException("Actual OS failure fixture must reject output writes.");
            string expectedError = existingOutputDirectory ? "open " + output.AsString() + ": is a directory" : "mkdir " + Path.Combine(root, "blocker").Replace('\\', '/') + ": not a directory";
            if (error.Message != expectedError)
                throw new InvalidOperationException("Actual OS error differs from pinned Go: " + error.Message);
            var result = Project.LoadConfiguration(Path.Combine(root, "tsconfig.json")).Compile();
            var emitted = result.EmitResult ?? throw new InvalidOperationException("Expected real emit result.");
            if (result.ExitCode != 2 || emitted.EmitSkipped || !emitted.EmittedFiles.IsNil || emitted.Diagnostics.Count != 1)
                throw new InvalidOperationException("Write failure changed emit status, skip or emitted-file observations.");
            var actual = TsDiagnosticProjection.Project(emitted.Diagnostics, root)[0];
            // Exact full DTO captured by the durable pinned-Go witness, including syscall text.
            const string expected = """
{"Config":[],"Program":[],"Syntactic":[],"Global":[],"Semantic":[{"File":null,"Start":-1,"Length":0,"End":-1,"Code":5033,"Category":1,"Key":"Could_not_write_file_0_Colon_1_5033","Message":"Could not write file '/blocker/out/main.js': mkdir /blocker: not a directory.","Args":["/blocker/out/main.js","mkdir /blocker: not a directory"],"Source":"","ReportsUnnecessary":false,"ReportsDeprecated":false,"SkippedOnNoEmit":false,"Chain":[],"Related":[],"Repopulate":null}]}
""";
            string expectedDiagnostic = existingOutputDirectory ? expected.Replace("/blocker/out/main.js", "/out/main.js", StringComparison.Ordinal).Replace("mkdir /blocker: not a directory", "open /out/main.js: is a directory", StringComparison.Ordinal) : expected;
            TsDiagnosticProjection.Check("ActualOsWriteFailure", new([], [], [], [], [actual]), expectedDiagnostic);
            if (!existingOutputDirectory && File.ReadAllText(Path.Combine(root, "blocker")) != "retained blocker")
                throw new InvalidOperationException("Failed emit modified its regular-file parent.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    public static void RawOutputBytes()
    {
        string root = Path.Combine(Path.GetTempPath(), "emit-bytes-" + Guid.NewGuid().ToString("N"));
        try
        {
            byte[] payload = [0, 0xff, 0x80, 0xc3, 0xa9, 13, 10, 10];
            var content = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(payload));
            var withBom = StringUtil.AddUTF8ByteOrderMark(content);
            var path = RootedFilePath.FromUnicode(Path.Combine(root, "nested", "output.bin"));
            var error = OutputTextWriters.Write(OsVfs.FS(), path, withBom);
            if (error != null)
                throw new InvalidOperationException("Actual raw byte write failed.", error);
            byte[] expected = [0xef, 0xbb, 0xbf, ..payload];
            if (!File.ReadAllBytes(Path.Combine(root, "nested", "output.bin")).AsSpan().SequenceEqual(expected))
                throw new InvalidOperationException("Raw output bytes, BOM or newline boundary changed.");
            if (!StringUtil.AddUTF8ByteOrderMark(withBom).ByteSpan.SequenceEqual(expected))
                throw new InvalidOperationException("Existing BOM must not be duplicated.");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
