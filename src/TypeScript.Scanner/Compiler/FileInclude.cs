// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/compiler/fileInclude.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Root-file and ordinary import reasons; other reference families are excluded.
using TypeScript.Scanner;
using TypeScript.Modules;
using ConfigParser = TypeScript.TsOptions.TsOptions;
using TsScanner = TypeScript.Scanner.Scanner;

namespace TypeScript.Compiler;
internal enum IncludeReferenceKind { Import, Path, Type, Lib }

internal sealed class FileIncludeReason
{
    internal SourceFile? importingFile;
    internal Node? specifier;
    internal FileReference? reference;
    internal IncludeReferenceKind kind;
    internal PackageId packageId;
    internal string syntheticKind = "";
    internal int rootIndex = -1;
    internal bool IsReferencedFile() => importingFile is not null;
    internal Diagnostic ToDiagnostic(IncludeProcessor processor, bool relativeFileName = false, string relativeTo = "")
    {
        if (processor.reasonDiagnostics.TryGetValue((this, relativeFileName, relativeTo), out Diagnostic? value))
            return value;
        value = ComputeDiagnostic(processor, relativeFileName, relativeTo);
        processor.reasonDiagnostics.Add((this, relativeFileName, relativeTo), value);
        return value;
    }

    private Diagnostic ComputeDiagnostic(IncludeProcessor processor, bool relativeFileName, string relativeTo)
    {
        if (IsReferencedFile())
        {
            if (relativeFileName)
                throw new ProjectException("relativeIncludeExplanation", "Relative diagnostic path formatting is outside the selected Program query.");
            SourceFile file = importingFile!;
            string name = processor.pathValuesAreRaw ? file.FileName() : GoString.ToUtf8(file.FileName());
            string text = reference is not null ? file.Text()[reference.Pos()..reference.End()] :
                syntheticKind != "" ? "\"" + specifier!.Text() + "\"" :
                file.Text()[TsScanner.SkipTriviaByteString(file.Text(), specifier!.Pos())..specifier.End()];
            Message message = kind switch
            {
                IncludeReferenceKind.Path => Diagnostics.Referenced_via_0_from_file_1,
                IncludeReferenceKind.Type => packageId.Name != "" ? Diagnostics.Type_library_referenced_via_0_from_file_1_with_packageId_2 : Diagnostics.Type_library_referenced_via_0_from_file_1,
                IncludeReferenceKind.Lib => Diagnostics.Library_referenced_via_0_from_file_1,
                _ when syntheticKind == "importHelpers" => packageId.Name != "" ? Diagnostics.Imported_via_0_from_file_1_with_packageId_2_to_import_importHelpers_as_specified_in_compilerOptions : Diagnostics.Imported_via_0_from_file_1_to_import_importHelpers_as_specified_in_compilerOptions,
                _ when syntheticKind == "jsxRuntime" => packageId.Name != "" ? Diagnostics.Imported_via_0_from_file_1_with_packageId_2_to_import_jsx_and_jsxs_factory_functions : Diagnostics.Imported_via_0_from_file_1_to_import_jsx_and_jsxs_factory_functions,
                _ => packageId.Name != "" ? Diagnostics.Imported_via_0_from_file_1_with_packageId_2 : Diagnostics.Imported_via_0_from_file_1
            };
            return packageId.Name != "" && kind is IncludeReferenceKind.Import or IncludeReferenceKind.Type ?
                Ast.NewCompilerDiagnostic(message, text, name, packageId.ToString()) : Ast.NewCompilerDiagnostic(message, text, name);
        }

        if (rootIndex >= 0)
        {
            if (processor.configuration.Parsed is { } config)
            {
                var file = config.FileNames()![rootIndex];
                string Name(RootedFilePath path)
                {
                    if (relativeFileName && config.CaseSensitivity().RelativePathFromDirectory(RootedDirectoryPath.FromUnicode(relativeTo), path)is var pair && pair.Ok)
                        return pair.Path.AsString();
                    return path.AsString();
                }

                string spec = config.GetMatchedFileSpec(file);
                if (spec != "")
                    return Ast.NewCompilerDiagnostic(Diagnostics.Part_of_files_list_in_tsconfig_json, spec, Name(file));
                var(include, isDefault) = config.GetMatchedIncludeSpec(file);
                if (include != "")
                    return isDefault ? Ast.NewCompilerDiagnostic(Diagnostics.Matched_by_default_include_pattern_Asterisk_Asterisk_Slash_Asterisk) : Ast.NewCompilerDiagnostic(Diagnostics.Matched_by_include_pattern_0_in_1, include, Name(config.ConfigName()));
                return Ast.NewCompilerDiagnostic(Diagnostics.Root_file_specified_for_compilation);
            }

            // fileInclude.go:170–175. Diagnostic arguments remain stored even
            // when the localized explanation contains no placeholders.
            string matchedFileSpec = processor.configuration.Files[rootIndex];
            string fileName = TsPath.GetNormalizedAbsolutePath(matchedFileSpec, processor.configuration.Root);
            if (relativeFileName)
                fileName = TsPath.GetRelativePathFromDirectory(relativeTo, fileName);
            return Ast.NewCompilerDiagnostic(Diagnostics.Part_of_files_list_in_tsconfig_json, GoString.ToUtf8(matchedFileSpec), GoString.ToUtf8(fileName));
        }

        return Ast.NewCompilerDiagnostic(Diagnostics.Root_file_specified_for_compilation);
    }

    internal Diagnostic? ToRelatedInfo(IncludeProcessor processor)
    {
        if (importingFile is not null)
        {
            if (syntheticKind != "") return null;
            return DiagnosticAt(kind switch
            {
                IncludeReferenceKind.Path => Diagnostics.File_is_included_via_reference_here,
                IncludeReferenceKind.Type => Diagnostics.File_is_included_via_type_library_reference_here,
                IncludeReferenceKind.Lib => Diagnostics.File_is_included_via_library_reference_here,
                _ => Diagnostics.File_is_included_via_import_here
            });
        }
        if (rootIndex < 0)
            return null;
        if (processor.configuration.Parsed is { } config)
        {
            var file = config.FileNames()![rootIndex];
            string spec = config.GetMatchedFileSpec(file);
            if (spec != "")
            {
                var node = ConfigParser.GetTsConfigPropArrayElementValue(config.ConfigFile!.SourceFile, "files", spec);
                return node is null ? null : ConfigParser.CreateDiagnosticForNodeInSourceFile(config.ConfigFile.SourceFile, node.AsNode(), Diagnostics.File_is_matched_by_files_list_specified_here);
            }

            var(include, isDefault) = config.GetMatchedIncludeSpec(file);
            if (include != "" && !isDefault)
            {
                var node = ConfigParser.GetTsConfigPropArrayElementValue(config.ConfigFile!.SourceFile, "include", include);
                return node is null ? null : ConfigParser.CreateDiagnosticForNodeInSourceFile(config.ConfigFile.SourceFile, node.AsNode(), Diagnostics.File_is_matched_by_include_pattern_specified_here);
            }

            return null;
        }

        Node? literal = processor.configuration.GetFileLiteral(rootIndex);
        return literal is null ? null : Ast.NewDiagnostic(processor.configuration.SourceFile, TsScanner.GetErrorRangeForNode(processor.configuration.SourceFile, literal), Diagnostics.File_is_matched_by_files_list_specified_here);
    }

    internal Diagnostic DiagnosticAt(Message message, params object[] args) => Ast.NewDiagnostic(importingFile!,
        reference is not null ? reference.TextRange : TsScanner.GetErrorRangeForNode(importingFile!, specifier!), message, args);
}
