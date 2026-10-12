// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original js_emit_baseline.go:19-165. Inputs and output text remain raw Go bytes.
using System.Text;
using TypeScript.Scanner;

namespace Program.Checks;
public static partial class CompilerEmitBaselines
{
    // compiler_runner.go:406: these are harness-only output skips, not product exclusions.
    public static string? OriginalOutputSkipReason(string basename) => basename switch
    {
        "filesEmittingIntoSameOutput.ts" or "jsFileCompilationWithJsEmitPathSameAsInput.ts" or "grammarErrors.ts" or "jsFileCompilationEmitBlockedCorrectly.ts" => "Output order nondeterministic due to collision on filename during parallel emit.",
        "jsDeclarationsReexportAliasesEsModuleInterop.ts" => "cls.d.ts is missing statements when run concurrently.",
        "jsFileCompilationWithoutJsExtensions.ts" => "No files are emitted.",
        "typeOnlyMerge2.ts" or "typeOnlyMerge3.ts" => "Nondeterministic contents when run concurrently.",
        _ => null
    };
    public static string JavaScriptBaseline(CompilerEmitCompilation result, DeclarationRecompile? declaration, CompilerEmitCompilation? noCheck)
    {
        var options = result.Options;
        if (options.NoEmit != Tristate.True && options.EmitDeclarationOnly != Tristate.True && result.JS.Count == 0 && result.Diagnostics.Count == 0)
            throw new InvalidOperationException("Expected at least one js file to be emitted or at least one error to be created.");
        var sources = result.Prepared.OriginalOtherFiles.Concat(result.Prepared.OriginalInputFiles).ToList();
        var ts = new StringBuilder("//// [" + result.Prepared.Header + "] ////\r\n\r\n");
        for (int i = 0; i < sources.Count; i++)
        {
            var file = sources[i];
            ts.Append("//// [").Append(TsPath.GetBaseFileName(file.UnitName)).Append("]\r\n").Append(file.Content);
            if (i < sources.Count - 1)
                ts.Append("\r\n");
        }

        var js = new StringBuilder();
        foreach (var file in result.JS)
        {
            if (js.Length > 0 && js[^1] != '\n')
                js.Append("\r\n");
            if (result.Diagnostics.Count == 0 && file.UnitName.EndsWith(TsPath.ExtensionJson, StringComparison.Ordinal))
            {
                var name = TsPath.ToRootedFilePath(file.UnitName, result.CurrentDirectory());
                var parsed = Parser.ParseSourceFile(new SourceFileParseOptions(name.AsString(), CaseSensitivity.CaseSensitive.PathKey(name.AsPath()).AsString(), FileNameIsGoBytes: true), GoString.Bytes(file.Content), ScriptKind.JSON);
                if (parsed.Diagnostics().Count != 0)
                {
                    js.Append(GetErrorBaseline([file], parsed.Diagnostics(), false));
                    continue;
                }
            }

            js.Append(FileOutput(file, result.HarnessSettings));
        }

        if (result.DTS.Count > 0)
        {
            js.Append("\r\n\r\n");
            foreach (var file in result.DTS)
                js.Append(FileOutput(file, result.HarnessSettings));
        }

        if (declaration is not null && declaration.Compilation.Diagnostics.Count > 0)
        {
            js.Append("\r\n\r\n//// [DtsFileErrors]\r\n\r\n\r\n");
            js.Append(GetErrorBaseline(result.Prepared.OriginalConfigFiles.Concat(declaration.Inputs).Concat(declaration.OtherFiles).ToList(), declaration.Compilation.Diagnostics, false));
        }

        if (noCheck is not null)
        {
            void Compare(List<CompilerEmitTestFile> actual, List<CompilerEmitTestFile> expected)
            {
                foreach (var file in actual)
                {
                    var original = expected.FirstOrDefault(candidate => candidate.UnitName == file.UnitName);
                    if (original is null)
                    {
                        js.Append("\r\n\r\n!!!! File ").Append(RemoveTestPathPrefixes(file.UnitName, false)).Append(" missing from original emit, but present in noCheck emit\r\n");
                        js.Append(FileOutput(file, result.HarnessSettings));
                    }
                    else if (original.Content != file.Content)
                    {
                        js.Append("\r\n\r\n!!!! File ").Append(RemoveTestPathPrefixes(file.UnitName, false)).Append(" differs from original emit in noCheck emit\r\n");
                        js.Append("//// [").Append(result.HarnessSettings.FullEmitPaths ? RemoveTestPathPrefixes(file.UnitName, false) : TsPath.GetBaseFileName(file.UnitName)).Append("]\r\n");
                        js.Append(DiffText("Expected\tThe full check baseline", "Actual\twith noCheck set", original.Content, file.Content));
                    }
                }
            }

            Compare(noCheck.DTS, result.DTS);
            Compare(noCheck.JS, result.JS);
        }

        return js.Length > 0 ? ts.ToString() + "\r\n\r\n" + js : "<no content>";
    }
}
