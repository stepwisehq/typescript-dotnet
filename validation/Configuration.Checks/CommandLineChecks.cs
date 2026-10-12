// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete assertions of all eight commandlineparser_test.go functions at the pin.
// This read fixture supplies only operations these originals call; other FS
// operations fail explicitly. It is not a general virtual filesystem port.
using TypeScript.Scanner;
using TypeScript.Vfs;
using T = TypeScript.TsOptions.TsOptions;
using W = TypeScript.DiagnosticWriter.DiagnosticWriter;

namespace Configuration.Checks;

public static partial class CommandLineChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    ["TestRemovedWatchOptions", "TestResponseFileDoesNotPanic", "TestResponseFileParsing", "TestParseCommandLineTypeRootsRelativePath", "TestCommandLineParseResult", "TestParseCommandLineVerifyNull", "TestParseBuildCommandLine", "TestCustomConditionsNullOverride"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestRemovedWatchOptions": RemovedWatch(); break;
            case "TestResponseFileDoesNotPanic": ResponseDoesNotPanic(); break;
            case "TestResponseFileParsing": ResponseParsing(); break;
            case "TestParseCommandLineTypeRootsRelativePath": TypeRoots(); break;
            case "TestCommandLineParseResult":
            case "TestParseCommandLineVerifyNull":
            case "TestParseBuildCommandLine": OriginalScenarios(name); break;
            case "TestCustomConditionsNullOverride": CustomConditionsNull(); break;
            default: throw new InvalidOperationException(name);
        }
    }

    private sealed class ReadFixture(Dictionary<string, string> files) : IFS
    {
        public CaseSensitivity CaseSensitivity() => TypeScript.Scanner.CaseSensitivity.CaseSensitive;
        public Exception? WriteFile(RootedFilePath path, string data) => new IOException("Authored command-line fixture is read-only.");
        public (string Contents, bool Ok) ReadFile(RootedFilePath path) => files.TryGetValue(path.AsString(), out var contents) ? (contents, true) : ("", false);
        public bool FileExists(RootedFilePath path) => throw new InvalidOperationException("Unselected CLI fixture FileExists");
        public bool DirectoryExists(RootedDirectoryPath path) => throw new InvalidOperationException("Unselected CLI fixture DirectoryExists");
        public Entries GetAccessibleEntries(RootedDirectoryPath path) => throw new InvalidOperationException("Unselected CLI fixture GetAccessibleEntries");
        public TypeScript.Vfs.FileInfo? Stat(RootedPath path) => throw new InvalidOperationException("Unselected CLI fixture Stat");
        public RootedPath Realpath(RootedPath path) => throw new InvalidOperationException("Unselected CLI fixture Realpath");
    }

    private static void RemovedWatch()
    {
        var fs = new ReadFixture([]);
        var cwd = RootedDirectoryPath.FromGoString("/project");
        foreach (var (option, expected) in removedWatchBaselines)
        {
            List<string> args = ["--watch", "--" + option];
            var parsed = T.ParseCommandLine(args, fs, cwd);
            Check.True(parsed.CompilerOptions().Watch == Tristate.True, option + " watch");
            Check.Equal(parsed.Errors!.Count, 1, option + " errors");
            var build = T.ParseBuildCommandLine(args, fs, cwd);
            Check.True(build.CompilerOptions.Watch == Tristate.True, option + " build watch");
            Check.Equal(build.Errors!.Count, 1, option + " build errors");
            using StringWriter text = new();
            var format = new TypeScript.DiagnosticWriter.FormattingOptions { NewLine = "\n" };
            W.WriteFormatDiagnostics(text, W.FromASTDiagnostics(parsed.Errors), format);
            W.WriteFormatDiagnostics(text, W.FromASTDiagnostics(build.Errors), format);
            Check.Equal(GoString.Bytes(text.ToString()).AsSpan().SequenceEqual(Convert.FromBase64String(expected)), true, option + " exact original baseline bytes");
        }
    }

    private static void ResponseDoesNotPanic()
    {
        string directory = Path.Combine(Path.GetTempPath(), "configuration-response-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var cwd = TsPath.RootedDirectoryPathFromAbsolute(GoString.ToUtf8(directory));
            foreach (string arg in new[] { "@", "@blah" })
            {
                // Original export_test.go maps nil test declarations to the real
                // compiler diagnostics before entering the production worker.
                var parsed = T.parseCommandLineWorker(T.CompilerOptionsDidYouMeanDiagnostics, [arg], TypeScript.Vfs.OsVfs.OsVfs.FS(), cwd);
                Check.True(parsed.errors.Count > 0, "expected an error for empty/non-existent response file");
            }
        }
        finally { Directory.Delete(directory); }
    }

    private static void ResponseParsing()
    {
        var cwd = RootedDirectoryPath.FromGoString("/project");
        var fixtures = new Dictionary<string, string>[]
        {
            new() { ["/project/args.txt"] = "--strict --outDir dist" },
            new() { ["/project/a.txt"] = "@/project/b.txt --strict", ["/project/b.txt"] = "@/project/a.txt --outDir dist" }
        };
        for (int i = 0; i < fixtures.Length; i++)
        {
            var parsed = T.ParseCommandLine([i == 0 ? "@args.txt" : "@a.txt"], new ReadFixture(fixtures[i]), cwd);
            Check.Equal(parsed.Errors!.Count, 0, "response errors");
            Check.True(parsed.CompilerOptions().Strict == Tristate.True, "response strict");
            Check.Equal(parsed.CompilerOptions().OutDir, RootedDirectoryPath.FromGoString("/project/dist"), "response outDir");
        }
    }

    private static void TypeRoots()
    {
        var fs = new ReadFixture(new() { ["/home/project/bug.ts"] = "let x = 1;" });
        var parsed = T.ParseCommandLine(["--typeRoots", "t", "bug.ts"], fs, RootedDirectoryPath.FromGoString("/home/project"));
        var roots = parsed.CompilerOptions().TypeRoots;
        Check.True(roots is not null, "typeRoots should not be nil");
        Check.Equal(roots!.Count, 1, "typeRoots length");
        Check.Equal(roots[0], RootedDirectoryPath.FromGoString("/home/project/t"), "typeRoots value");
    }
}
