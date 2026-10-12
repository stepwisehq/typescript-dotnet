// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/ls/findallreferences_test.go TestImplementationsWorklistDoesNotBlowUp.
// T.Parallel is a scheduling hint; this shared assertion executes sequentially.
using System.Text;
using TypeScript.Scanner;
using TypeScript.LanguageServices;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;
using CompilerProgram = TypeScript.Compiler.Program;

namespace LanguageServices.Checks;
public static class FindAllReferencesChecks
{
    public static readonly string[] Names = ["TestImplementationsWorklistDoesNotBlowUp"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        if (name != Names[0])
            throw new ArgumentOutOfRangeException(nameof(name));
        const int count = 40;
        var (smallReferences, smallGroups) = Measure(count);
        var (largeReferences, largeGroups) = Measure(2 * count);
        if (largeReferences > smallReferences * 3)
            throw new InvalidOperationException($"implementations worklist references scale superlinearly: K={count} -> {smallReferences}, K={2 * count} -> {largeReferences} (expected ~linear); provideSymbolsAndEntries accumulates references without deduplicating by node");
        if (largeGroups > smallGroups * 3)
            throw new InvalidOperationException($"implementations worklist groups scale superlinearly: K={count} -> {smallGroups}, K={2 * count} -> {largeGroups} (expected ~linear); provideSymbolsAndEntries retains a group per search result without deduplicating by definition");
    }

    private static (int References, int Groups) Measure(int count)
    {
        var builder = new StringBuilder();
        builder.Append("interface I { m(): void; }\n");
        for (int i = 0; i < count; i++)
            builder.Append("const a").Append(i).Append(": I = { m() {} };\n");
        builder.Append("declare const i: I;\ni.m();\n");
        var content = builder.ToString();
        var fs = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new("/repro.ts", content), new("/tsconfig.json", "{ \"compilerOptions\": {}, \"files\": [\"repro.ts\"] }")));
        var host = TypeScript.Compiler.CompilerHosts.NewCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath());
        var (parsed, errors) = TypeScript.TsOptions.TsOptions.GetParsedCommandLineOfConfigFile(RootedFilePath.FromGoString("/tsconfig.json"), new CompilerOptions(), null, fs);
        if (errors.Count != 0 || parsed == null)
            throw new InvalidOperationException("Original implementation fixture configuration failed");
        var program = CompilerProgram.NewProgram(new() { Config = parsed, Host = host });
        program.BindSourceFiles();
        var file = program.GetSourceFile(RootedFilePath.FromGoString("/repro.ts")) ?? throw new InvalidOperationException("Original implementation fixture file missing");
        program.GetSemanticDiagnostics(CancellationToken.None, file);
        var rawContent = ByteStringValue.FromString(content);
        var converters = Converters.NewConverters(PositionEncodingKind.UTF8, _ => Lsconv.ComputeLSPLineStarts(rawContent));
        var service = new LanguageService(program, converters);
        int offset = content.LastIndexOf("i.m", StringComparison.Ordinal) + "i.".Length;
        var (position, _) = converters.ToLSPPosition(file, offset);
        var (data, ok) = service.provideSymbolsAndEntries(new ProtocolRequestContext(CancellationToken.None, default, default), new DocumentUri(ByteStringValue.FromString("file:///repro.ts")), position, false, true);
        if (!ok)
            throw new InvalidOperationException("Original implementation query failed");
        int references = 0;
        foreach (var group in data.SymbolsAndEntries)
            references += group.references.Count;
        return (references, data.SymbolsAndEntries.Count);
    }
}
