// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/compiler/fileInclude.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// All Program reason families; named legacy pointer observations remain separate.
using TypeScript.Scanner;
using TypeScript.Modules;
using ConfigParser = TypeScript.TsOptions.TsOptions;
using TsScanner = TypeScript.Scanner.Scanner;

namespace TypeScript.Compiler;
internal enum IncludeReferenceKind { Import, Path, Type, Lib }

internal sealed class ReferenceFileLocation
{
    internal SourceFile file = null!;
    internal Node? node;
    internal FileReference? reference;
    internal PackageId packageId;
    internal bool isSynthetic;
    internal string Text() => node is not null
        ? isSynthetic ? "\"" + node.Text() + "\"" : file.TextView().Slice(TsScanner.SkipTriviaByteString(file.TextView(), node.Pos()), node.End()).Materialize()
        : file.TextView().Slice(reference!.Pos(), reference.End()).Materialize();
    internal Diagnostic DiagnosticAt(Message message, params object[] args) => node is not null
        ? ConfigParser.CreateDiagnosticForNodeInSourceFile(file, node, message, args)
        : Ast.NewDiagnostic(file, reference!.TextRange, message, args);
}

public sealed class FileIncludeReason
{
    internal SourceFile? importingFile;
    internal Node? specifier;
    internal FileReference? reference;
    internal IncludeReferenceKind kind;
    internal PackageId packageId;
    internal string syntheticKind = "";
    internal int rootIndex = -1, libIndex;
    internal bool isLibFile, isDefaultLib;
    internal ByteStringValue automaticTypeDirective = ByteStringValue.FromString(null);
    internal ByteStringKey canonicalSourceFile;
    internal bool IsReferencedFile() => importingFile is not null;
    // Program locates the referenced AST in its own generation. The legacy
    // direct pointer representation supplies only the original path and index.
    internal ReferenceFileLocation GetReferencedLocation(Program program)
    {
        var original = importingFile!;
        var file = program.GetSourceFileByPath(PathKey.FromValue(original.PathKeyValue()))!;
        if (kind == IncludeReferenceKind.Import)
        {
            Node node;
            if (syntheticKind != "") node = specifier!;
            else
            {
                int index = GoSlices.Index(original.Imports(), specifier!);
                if (index >= 0) node = file.Imports()[index];
                else
                {
                    var originals = Core.Filter(original.ModuleAugmentations, static item => item.Kind == TypeScript.Scanner.Kind.StringLiteral);
                    int augmentation = GoSlices.Index(originals, specifier!);
                    node = Core.Filter(file.ModuleAugmentations, static item => item.Kind == TypeScript.Scanner.Kind.StringLiteral)[augmentation];
                }
            }
            return new() { file = file, node = node, packageId = program.GetResolvedModuleFromModuleSpecifier(file, node)!.PackageId, isSynthetic = syntheticKind != "" };
        }
        var originalRefs = kind switch { IncludeReferenceKind.Path => original.ReferencedFiles, IncludeReferenceKind.Type => original.TypeReferenceDirectives, _ => original.LibReferenceDirectives };
        var currentRefs = kind switch { IncludeReferenceKind.Path => file.ReferencedFiles, IncludeReferenceKind.Type => file.TypeReferenceDirectives, _ => file.LibReferenceDirectives };
        return new() { file = file, reference = currentRefs[GoSlices.Index(originalRefs, reference!)] };
    }
    internal ReferenceFileLocation LegacyReferencedLocation() => new() { file = importingFile!, node = specifier, reference = reference, packageId = packageId, isSynthetic = syntheticKind != "" };
    internal Diagnostic ToDiagnostic(Program program, bool relativeFileName, RootedDirectoryPath relativeTo) => ToDiagnostic(program.includeProcessor, relativeFileName, relativeTo.AsValue());
    internal Diagnostic ToDiagnostic(Program program, bool relativeFileName = false, string relativeTo = "") => ToDiagnostic(program.includeProcessor, relativeFileName, relativeTo);
    internal Diagnostic ToDiagnostic(IncludeProcessor processor, bool relativeFileName = false, string relativeTo = "") => ToDiagnostic(processor, relativeFileName, ByteStringValue.FromString(relativeTo));
    private Diagnostic ToDiagnostic(IncludeProcessor processor, bool relativeFileName, ByteStringValue relativeTo)
    {
        var key = (this, relativeFileName, relativeTo.IsNull ? (ByteStringKey?)null : ByteStringKey.FromValue(relativeTo));
        var (value, found) = processor.reasonDiagnostics.Load(key);
        if (found) return value;
        return processor.reasonDiagnostics.LoadOrStore(key, ComputeDiagnostic(processor, relativeFileName, relativeTo)).Actual;
    }

    private Diagnostic ComputeDiagnostic(IncludeProcessor processor, bool relativeFileName, ByteStringValue relativeTo)
    {
        if (!IsReferencedFile() && automaticTypeDirective.IsNull && !isLibFile && canonicalSourceFile.Value.Length == 0 && processor.program is { } program && program.CommandLine().ConfigFile is null)
            return Ast.NewCompilerDiagnostic(Diagnostics.Root_file_specified_for_compilation);
        if (IsReferencedFile())
        {

            var location = processor.GetReferenceLocation(this);
            SourceFile file = location.file;
            var referencePackageId = location.packageId;
            var name = file.FileNameGoValue();
            if (relativeFileName)
            {
                var directory = processor.pathValuesAreRaw ? RootedDirectoryPath.FromValue(relativeTo) : RootedDirectoryPath.FromUnicode(relativeTo.Materialize()!);
                var (relative, ok) = (processor.program?.CaseSensitivity() ?? processor.configuration.Parsed?.CaseSensitivity())?.RelativePathFromDirectory(directory, RootedFilePath.FromValue(name)) ?? (default, false);
                if (ok) name = relative.AsValue();
            }
            string text = location.Text();
            Message message = kind switch
            {
                IncludeReferenceKind.Path => Diagnostics.Referenced_via_0_from_file_1,
                IncludeReferenceKind.Type => referencePackageId.Name != "" ? Diagnostics.Type_library_referenced_via_0_from_file_1_with_packageId_2 : Diagnostics.Type_library_referenced_via_0_from_file_1,
                IncludeReferenceKind.Lib => Diagnostics.Library_referenced_via_0_from_file_1,
                _ when syntheticKind == "importHelpers" => referencePackageId.Name != "" ? Diagnostics.Imported_via_0_from_file_1_with_packageId_2_to_import_importHelpers_as_specified_in_compilerOptions : Diagnostics.Imported_via_0_from_file_1_to_import_importHelpers_as_specified_in_compilerOptions,
                _ when syntheticKind == "jsxRuntime" => referencePackageId.Name != "" ? Diagnostics.Imported_via_0_from_file_1_with_packageId_2_to_import_jsx_and_jsxs_factory_functions : Diagnostics.Imported_via_0_from_file_1_to_import_jsx_and_jsxs_factory_functions,
                _ => referencePackageId.Name != "" ? Diagnostics.Imported_via_0_from_file_1_with_packageId_2 : Diagnostics.Imported_via_0_from_file_1
            };
            return referencePackageId.Name != "" && kind is IncludeReferenceKind.Import or IncludeReferenceKind.Type ?
                Ast.NewCompilerDiagnostic(message, text, name, referencePackageId.ToString()) : Ast.NewCompilerDiagnostic(message, text, name);
        }

        if (!automaticTypeDirective.IsNull)
        {
            bool wildcard = processor.configuration.Options.UsesWildcardTypes();
            Message message = wildcard ? packageId.Name != "" ? Diagnostics.Entry_point_for_implicit_type_library_0_with_packageId_1 : Diagnostics.Entry_point_for_implicit_type_library_0 :
                packageId.Name != "" ? Diagnostics.Entry_point_of_type_library_0_specified_in_compilerOptions_with_packageId_1 : Diagnostics.Entry_point_of_type_library_0_specified_in_compilerOptions;
            return packageId.Name != "" ? Ast.NewCompilerDiagnostic(message, automaticTypeDirective, packageId.ToString()) : Ast.NewCompilerDiagnostic(message, automaticTypeDirective);
        }
        if (isLibFile)
        {
            if (!isDefaultLib) return Ast.NewCompilerDiagnostic(Diagnostics.Library_0_specified_in_compilerOptions, processor.configuration.Options.Lib![libIndex]);
            string target = ScriptTargetName(processor.configuration.Options.GetEmitScriptTarget());
            return target != "" ? Ast.NewCompilerDiagnostic(Diagnostics.Default_library_for_target_0, target) : Ast.NewCompilerDiagnostic(Diagnostics.Default_library);
        }
        if (canonicalSourceFile.Value.Length != 0)
        {
            var canonical = processor.program!.GetSourceFileByPath(PathKey.FromValue(canonicalSourceFile.Value))!;
            var name = canonical.FileNameGoValue();
            if (relativeFileName)
            {
                var directory = processor.pathValuesAreRaw ? RootedDirectoryPath.FromValue(relativeTo) : RootedDirectoryPath.FromUnicode(relativeTo.Materialize()!);
                var (relative, ok) = processor.program!.CaseSensitivity().RelativePathFromDirectory(directory, RootedFilePath.FromValue(name));
                if (ok) name = relative.AsValue();
            }
            return Ast.NewCompilerDiagnostic(Diagnostics.Supplemental_virtual_file_produced_by_the_content_mapper_for_file_0, name);
        }
        if (rootIndex >= 0)
        {
            if (processor.configuration.Parsed is { ConfigFile: not null } config)
            {
                var file = config.FileNames()![rootIndex];
                string Name(RootedFilePath path)
                {
                    if (relativeFileName && (processor.program?.CaseSensitivity() ?? config.CaseSensitivity()).RelativePathFromDirectory((processor.pathValuesAreRaw ? RootedDirectoryPath.FromValue(relativeTo) : RootedDirectoryPath.FromUnicode(relativeTo.Materialize()!)), path)is var pair && pair.Ok)
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
                fileName = TsPath.GetRelativePathFromDirectory(relativeTo.Materialize()!, fileName);
            return Ast.NewCompilerDiagnostic(Diagnostics.Part_of_files_list_in_tsconfig_json, GoString.ToUtf8(matchedFileSpec), GoString.ToUtf8(fileName));
        }

        return Ast.NewCompilerDiagnostic(Diagnostics.Root_file_specified_for_compilation);
    }

    internal Diagnostic? ToRelatedInfo(IncludeProcessor processor)
    {
        var (value, found) = processor.includeReasonToRelatedInfo.Load(this);
        return found ? value : processor.includeReasonToRelatedInfo.LoadOrStore(this, ComputeRelatedInfo(processor)).Actual;
    }
    private Diagnostic? ComputeRelatedInfo(IncludeProcessor processor)
    {
        if (importingFile is not null)
        {
            if (syntheticKind != "") return null;
            return processor.GetReferenceLocation(this).DiagnosticAt(kind switch
            {
                IncludeReferenceKind.Path => Diagnostics.File_is_included_via_reference_here,
                IncludeReferenceKind.Type => Diagnostics.File_is_included_via_type_library_reference_here,
                IncludeReferenceKind.Lib => Diagnostics.File_is_included_via_library_reference_here,
                _ => Diagnostics.File_is_included_via_import_here
            });
        }
        if (processor.program is { } program && program.CommandLine().ConfigFile is { } configFile && (!automaticTypeDirective.IsNull || isLibFile))
        {
            var syntax = processor.GetCompilerOptionsObjectLiteralSyntax();
            Node? node = null;
            Message? message = null;
            if (!automaticTypeDirective.IsNull && !program.Options().UsesWildcardTypes())
            {
                node = ConfigParser.GetOptionsSyntaxByArrayElementValue(syntax, "types", automaticTypeDirective);
                message = Diagnostics.File_is_entry_point_of_type_library_specified_here;
            }
            if (isLibFile)
            {
                if (!isDefaultLib)
                {
                    node = ConfigParser.GetOptionsSyntaxByArrayElementValue(syntax, "lib", program.Options().Lib![libIndex]);
                    message = Diagnostics.File_is_library_specified_here;
                }
                else
                {
                    string target = ScriptTargetName(program.Options().GetEmitScriptTarget());
                    if (target != "") node = ConfigParser.ForEachPropertyAssignment(syntax, "target", ConfigParser.GetCallbackForFindingPropertyAssignmentByValue(target));
                    message = Diagnostics.File_is_default_library_for_target_specified_here;
                }
            }
            return node is not null && message is not null ? ConfigParser.CreateDiagnosticForNodeInSourceFile(configFile.SourceFile, node, message) : null;
        }
        if (processor.program is { } current && current.CommandLine().ConfigFile is null) return null;
        if (rootIndex < 0) return null;
        if (processor.configuration.Parsed is { ConfigFile: not null } config)
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

    private static string ScriptTargetName(ScriptTarget target) => (int)target switch
    {
        0 => "None", 1 => "ES5", >= 2 and <= 13 => "ES" + ((int)target + 2013),
        99 => "ESNext", 100 => "JSON", _ => "ScriptTarget(" + (int)target + ")"
    };
    internal Diagnostic DiagnosticAt(IncludeProcessor processor, Message message, params object[] args) => processor.GetReferenceLocation(this).DiagnosticAt(message, args);
    internal Diagnostic DiagnosticAt(Message message, params object[] args) => Ast.NewDiagnostic(importingFile!,
        reference is not null ? reference.TextRange : TsScanner.GetErrorRangeForNode(importingFile!, specifier!), message, args);
}
