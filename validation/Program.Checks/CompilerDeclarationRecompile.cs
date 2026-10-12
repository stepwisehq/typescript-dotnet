// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original js_emit_baseline.go:168-297 declaration recompilation responsibilities.
using TypeScript.Scanner;
using TypeScript.TsOptions;
using OutputPaths = TypeScript.OutputPaths.OutputPaths;

namespace Program.Checks;
public static partial class CompilerEmitBaselines
{
    public sealed record DeclarationRecompile(CompilerEmitCompilation Compilation, List<CompilerEmitTestFile> Inputs, List<CompilerEmitTestFile> OtherFiles);
    public static DeclarationRecompile? RecompileDeclarations(CompilerEmitCompilation result)
    {
        var options = result.Options;
        bool contentMapped = false;
        foreach (var file in result.Program.GetSourceFiles())
            contentMapped |= file.ContentMapper() != "";
        if (options.Declaration == Tristate.True && result.Diagnostics.Count == 0)
        {
            if (options.EmitDeclarationOnly == Tristate.True)
            {
                if (result.JS.Count > 0)
                    throw new InvalidOperationException("Only declaration files should be generated when emitDeclarationOnly:true");
                if (result.DTS.Count == 0 && options.NoEmit != Tristate.True)
                    throw new InvalidOperationException("Expected at least one declaration file to be emitted when emitDeclarationOnly:true and no errors were generated");
            }
            else if (!contentMapped && result.DTS.Count != result.GetNumberOfJSFiles(false))
                throw new InvalidOperationException("There were no errors and declFiles generated did not match number of js files generated");
        }

        if (options.Declaration != Tristate.True || result.Diagnostics.Count != 0 || result.DTS.Count == 0)
            return null;
        List<CompilerEmitTestFile> input = [], other = [];
        void Add(CompilerEmitTestFile file, List<CompilerEmitTestFile> target)
        {
            if (TsPath.IsDeclarationFileName(file.UnitName) || TsPath.HasJSONFileExtension(file.UnitName))
            {
                target.Add(file);
                return;
            }

            var source = result.Program.GetSourceFile(TsPath.ToRootedFilePath(file.UnitName, result.CurrentDirectory()));
            if (source is null || !(TsPath.HasTSFileExtension(file.UnitName) || TsPath.HasJSFileExtension(file.UnitName) && options.GetAllowJS() || source.ContentMapper() != ""))
                return;
            var sourceName = TsPath.ToRootedFilePath(source.FileName(), result.CurrentDirectory());
            if (options.OutDir.AsValue().Length != 0)
            {
                var(relative, ok) = result.FS().CaseSensitivity().RelativePathFromDirectory(result.Program.CommonSourceDirectory(), sourceName);
                if (ok)
                    sourceName = options.OutDir.ResolveRelativeFile(relative);
            }

            var path = OutputPaths.ChangeToDeclarationExtension(sourceName, result.Program);
            var declaration = result.DTS.FirstOrDefault(candidate => candidate.UnitName == path.AsString());
            if (declaration is not null && !input.Any(candidate => candidate.UnitName == declaration.UnitName) && !other.Any(candidate => candidate.UnitName == declaration.UnitName))
            {
                string bom = GoString.ToUtf8("\uFEFF");
                target.Add(declaration with { Content = declaration.Content.StartsWith(bom, StringComparison.Ordinal) ? declaration.Content[bom.Length..] : declaration.Content });
            }
        }

        foreach (var file in result.Prepared.OriginalInputFiles)
            Add(file, input);
        foreach (var file in result.Prepared.OriginalOtherFiles)
            Add(file, other);
        ParsedCommandLine? config = null;
        if (result.Prepared.ActualConfiguration.ConfigFile is not null)
        {
            config = new ParsedCommandLine
            {
                ParsedConfig = new ParsedOptions
                {
                    ContentMappers = result.Prepared.ActualConfiguration.ContentMappers()
                },
                ConfigFile = result.Prepared.ActualConfiguration.ConfigFile
            };
        }

        var prepared = CompilerNoEmitHarness.PrepareFiles(input.Select(file => new CompilerFixtureParser.Unit(file.UnitName, file.Content, [])).ToList(), other.Select(file => new CompilerFixtureParser.Unit(file.UnitName, file.Content, [])).ToList(), result.HarnessSettings, options, result.CurrentDirectory(), config, result.Prepared.OriginalSymlinks, result.Prepared.TestDataDirectory, result.Prepared.Header);
        return new(ProgramRunner.CompilePreparedEmit(prepared), input, other);
    }
}
