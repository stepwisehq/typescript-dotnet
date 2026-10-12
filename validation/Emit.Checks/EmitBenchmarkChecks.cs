// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored timing adapter for unchanged compiler/emit_test.go original benchmarks.
// Performance evidence only: not original assertion functions or output acceptance.
using System.Diagnostics;
using System.Globalization;
using System.Text;
using TypeScript.Compiler;
using TypeScript.Scanner;

namespace Emit.Checks;
public readonly record struct EmitBenchmarkResult(string Name, int Iterations, double ElapsedMilliseconds, long AllocatedBytes);
public static class EmitBenchmarkChecks
{
    private static readonly WriteFile DiscardWrite = static (path, text, data) => null;
    public static EmitBenchmarkResult Run(string name, int iterations)
    {
        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(iterations));
        int properties = name switch
        {
            "LongLines1000" => 1000,
            "LongLines5000" => 5000,
            "LongLines10000" => 10000,
            "ManyFiles" => 500,
            "LongLinesWithLineBreaks" => 10000,
            _ => throw new ArgumentException("Unknown original emit benchmark.", nameof(name))};
        int count = name == "ManyFiles" ? 200 : 1;
        var inputs = new List<Program.Checks.CompilerBaselineFileSystem.Input>(count);
        var fileNames = new List<RootedFilePath>(count);
        for (int i = 0; i < count; i++)
        {
            string path = name == "ManyFiles" ? "/dev/src/file_" + i.ToString(CultureInfo.InvariantCulture) + ".ts" : "/dev/src/index.ts";
            inputs.Add(new(path, GenerateSource(properties, name == "LongLinesWithLineBreaks")));
            fileNames.Add(RootedFilePath.FromGoString(path));
        }

        var fs = TypeScript.Bundled.Bundled.WrapFS(new Program.Checks.CompilerBaselineFileSystem(inputs));
        var options = new CompilerOptions
        {
            Target = ScriptTarget.ES2015,
            SourceMap = Tristate.True,
            OutDir = RootedDirectoryPath.FromUnicode("/dev/out")
        };
        var config = TypeScript.TsOptions.TsOptions.NewParsedCommandLine(options, fileNames, null, RootedDirectoryPath.FromUnicode("/dev/src"), fs.CaseSensitivity());
        var program = TypeScript.Compiler.Program.NewProgram(new() { Config = config, Host = CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath()) });
        var emitOptions = new EmitOptions
        {
            WriteFile = DiscardWrite
        };
        // Like b.ResetTimer/b.ReportAllocs: preparation is outside the measured loop.
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        long started = Stopwatch.GetTimestamp();
        for (int i = 0; i < iterations; i++)
            program.Emit(default(CancellationToken), emitOptions);
        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        return new(name, iterations, elapsed, allocated);
    }

    // emit_test.go:generateLongLineTS and line-break control preserve exact ASCII input.
    private static string GenerateSource(int properties, bool lineBreaks)
    {
        var text = new StringBuilder(lineBreaks ? "export const data: Record<string, number> = {\n" : "export const data: Record<string, number> = {");
        for (int i = 0; i < properties; i++)
        {
            if (i != 0)
                text.Append(lineBreaks ? ",\n" : ", ");
            if (lineBreaks)
                text.Append("  ");
            string index = i.ToString(CultureInfo.InvariantCulture);
            text.Append("prop_").Append(index).Append(": ").Append(index);
        }

        return text.Append(lineBreaks ? "\n};\n" : "};").ToString();
    }
}
