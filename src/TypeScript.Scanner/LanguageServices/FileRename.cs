// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/file_rename.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Checking;
using TypeScript.LanguageServices.Change;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using CompilerProgram = TypeScript.Compiler.Program;
using ModuleSpecifiersApi = TypeScript.ModuleSpecifiers.ModuleSpecifiers;
using ModulePreferences = TypeScript.ModuleSpecifiers.UserPreferences;
using ModuleSpecifierOptions = TypeScript.ModuleSpecifiers.ModuleSpecifierOptions;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private delegate (RootedFilePath Path, bool Updated) PathUpdater(RootedFilePath path);
    private sealed class ToImport
    {
        internal RootedFilePath newFileName;
        internal bool updated;
    }

    private readonly record struct MovedFile(SourceFile SourceFile, RootedFilePath NewFileName);
    internal Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile> GetEditsForFileRename(ProtocolRequestContext context, DocumentUri oldURI, DocumentUri newURI)
    {
        var program = GetProgram();
        var oldPath = new RootedPath(oldURI.FileName().AsValue());
        var newPath = new RootedPath(newURI.FileName().AsValue());
        var oldToNew = createPathUpdater(oldPath, newPath);
        var tracker = Tracker.NewTracker(context, program.Options(), FormatOptions(), converters);
        updateTsconfigFiles(program, tracker, oldToNew, TsPath.RootedFilePathFromPath(oldPath), TsPath.RootedFilePathFromPath(newPath));
        updateImportsForFileRename(program, tracker, oldToNew);
        Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile> documentChanges = default;
        var oldFile = TsPath.RootedFilePathFromPath(oldPath);
        var newFile = TsPath.RootedFilePathFromPath(newPath);
        if (oldFile.IsDeclarationFile() && newFile.IsDeclarationFile())
        {
            var originalExtensions = TsPath.GetPossibleOriginalInputExtensionForExtension(oldFile.DeclarationFileExtensionValue());
            foreach (var extension in originalExtensions)
            {
                var oldOriginalPath = oldFile.ChangeFullExtension(extension);
                if (host.FileExists(oldOriginalPath))
                {
                    var newExtensions = TsPath.GetPossibleOriginalInputExtensionForExtension(newFile.DeclarationFileExtensionValue());
                    if (GoSlices.Contains(newExtensions, extension))
                    {
                        var newOriginalPath = newFile.ChangeFullExtension(extension);
                        documentChanges = documentChanges.Append(new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile { RenameFile = new RenameFile { OldUri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(oldOriginalPath), NewUri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(newOriginalPath) } });
                    }
                }
            }
        }

        var(changes, _) = tracker.GetChanges();
        foreach (var(fileName, edits)in changes)
        {
            var uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(fileName);
            var lspEdits = Slice<TextEditOrAnnotatedTextEditOrSnippetTextEdit>.Make(0, edits.Length);
            foreach (var edit in edits)
                lspEdits = lspEdits.Append(new TextEditOrAnnotatedTextEditOrSnippetTextEdit { TextEdit = edit });
            documentChanges = documentChanges.Append(new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile { TextDocumentEdit = new TextDocumentEdit { TextDocument = new OptionalVersionedTextDocumentIdentifier { Uri = uri }, Edits = lspEdits } });
        }

        return documentChanges;
    }

    private PathUpdater createPathUpdater(RootedPath oldPath, RootedPath newPath)
    {
        var sensitivity = CaseSensitivity();
        return path =>
        {
            if (sensitivity.CompareFilePaths(path, TsPath.RootedFilePathFromPath(oldPath)) == 0)
                return (TsPath.RootedFilePathFromPath(newPath), true);
            var(relative, ok) = sensitivity.RelativeFilePathFromDirectory(TsPath.RootedDirectoryPathFromPath(oldPath), path);
            return ok ? (TsPath.RootedDirectoryPathFromPath(newPath).ResolveRelativeFile(relative), true) : (default, false);
        };
    }

    private void updateTsconfigFiles(CompilerProgram program, Tracker tracker, PathUpdater oldToNew, RootedFilePath oldPath, RootedFilePath newPath)
    {
        var commandLine = program.CommandLine();
        if (commandLine?.ConfigFile == null)
            return;
        var configFile = commandLine.ConfigFile.SourceFile;
        if (configFile == null)
            return;
        var configDir = RootedFilePath.FromValue(configFile.FileNameGoValue()).Directory();
        var jsonObject = getTsConfigObjectLiteralExpression(configFile);
        if (jsonObject == null)
            return;
        forEachObjectProperty(jsonObject, (property, propertyName) =>
        {
            if (propertyName.EqualsString("files") || propertyName.EqualsString("include") || propertyName.EqualsString("exclude"))
            {
                var matched = updatePathsProperty(configFile, configDir, property, tracker, oldToNew, converters, CaseSensitivity());
                if (matched || !propertyName.EqualsString("include") || !Ast.IsArrayLiteralExpression(property.Initializer))
                    return;
                var(oldSpec, isDefault) = commandLine.GetMatchedIncludeSpec(oldPath);
                if (oldSpec != "" && !isDefault)
                {
                    var(newSpec, _) = commandLine.GetMatchedIncludeSpec(newPath);
                    if (newSpec == "")
                    {
                        var elements = property.Initializer.Elements();
                        if (elements.Length > 0)
                        {
                            var text = newPath.AsValue();
                            var(relative, ok) = CaseSensitivity().RelativePathFromDirectory(configDir, newPath);
                            if (ok)
                                text = relative.AsValue();
                            tracker.InsertNodeAfter(configFile, elements[elements.Length - 1], tracker.NodeFactory.NewStringLiteralWithTextValue(text, TokenFlags.None));
                        }
                    }
                }
            }
            else if (propertyName.EqualsString("compilerOptions"))
            {
                if (!Ast.IsObjectLiteralExpression(property.Initializer))
                    return;
                forEachObjectProperty(property.Initializer.AsObjectLiteralExpression(), (optionProperty, optionName) =>
                {
                    var option = TypeScript.TsOptions.TsOptions.CommandLineCompilerOptionsMap.Get(optionName);
                    if (option != null)
                    {
                        var elementOption = option.Elements();
                        if (option.PathKind.IsRooted() || option.Kind == CommandLineOptionKind.List && elementOption != null && elementOption.PathKind.IsRooted())
                        {
                            updatePathsProperty(configFile, configDir, optionProperty, tracker, oldToNew, converters, CaseSensitivity());
                            return;
                        }
                    }

                    if (!optionName.EqualsString("paths") || !Ast.IsObjectLiteralExpression(optionProperty.Initializer))
                        return;
                    forEachObjectProperty(optionProperty.Initializer.AsObjectLiteralExpression(), (pathsProperty, _) =>
                    {
                        if (!Ast.IsArrayLiteralExpression(pathsProperty.Initializer))
                            return;
                        foreach (var element in pathsProperty.Initializer.Elements())
                            tryUpdateConfigString(configFile, configDir, element, tracker, oldToNew, converters, CaseSensitivity());
                    });
                });
            }
        });
    }

    private static bool updatePathsProperty(SourceFile configFile, RootedDirectoryPath configDir, PropertyAssignment property, Tracker tracker, PathUpdater oldToNew, Converters converters, CaseSensitivity sensitivity)
    {
        var elements = Slice<Node>.FromArray([property.Initializer]);
        if (Ast.IsArrayLiteralExpression(property.Initializer))
            elements = property.Initializer.Elements();
        var exact = false;
        foreach (var element in elements)
            exact = tryUpdateConfigString(configFile, configDir, element, tracker, oldToNew, converters, sensitivity) || exact;
        return exact;
    }

    private static bool tryUpdateConfigString(SourceFile file, RootedDirectoryPath directory, Node element, Tracker tracker, PathUpdater oldToNew, Converters converters, CaseSensitivity sensitivity)
    {
        if (!Ast.IsStringLiteral(element))
            return false;
        var(updated, ok) = oldToNew(directory.ResolveFile(element.TextValue()));
        if (!ok)
            return false;
        var range = new TextRange(ScannerApi.GetTokenPosOfNode(element, file, false) + 1, element.End() - 1);
        var(lspRange, fidelity) = converters.ToLSPRange(file, range);
        Debug.Assert(fidelity.IsExact(), "config files are not content-mapped");
        tracker.ReplaceRangeWithText(file, lspRange, relativePathFromDirectory(directory, updated, sensitivity).AsValue());
        return true;
    }

    private ModuleSpecifier updateRelativePath(PathUpdater oldToNew, RootedFilePath oldImportFromPath, RootedFilePath newImportFromPath, ModuleSpecifier relativeSpecifier)
    {
        var oldAbsolute = oldImportFromPath.Directory().ResolveFile(relativeSpecifier.AsValue());
        var(newAbsolute, updated) = oldToNew(oldAbsolute);
        if (!updated)
            newAbsolute = oldAbsolute;
        return relativeImportPathFromDirectory(newImportFromPath.Directory(), newAbsolute, CaseSensitivity());
    }

    private void updateImportsForFileRename(CompilerProgram program, Tracker tracker, PathUpdater oldToNew)
    {
        var files = program.GetSourceFiles();
        var(checker, done) = program.GetTypeChecker(default(CancellationToken));
        try
        {
            var preferences = UserPreferences().ModuleSpecifierPreferences();
            Slice<MovedFile> movedFiles = default;
            foreach (var file in files)
            {
                var(name, moved) = oldToNew(file.OriginalFileName());
                if (moved)
                    movedFiles = movedFiles.Append(new MovedFile(file, name));
            }

            foreach (var file in files)
            {
                var oldName = file.OriginalFileName();
                var(newFromOld, moved) = oldToNew(oldName);
                var newImportFromPath = moved ? newFromOld : oldName;
                foreach (var reference in file.ReferencedFiles)
                {
                    if (!TsPath.IsExternalModuleNameRelative(reference.FileNameValue))
                        continue;
                    var updated = updateRelativePath(oldToNew, oldName, newImportFromPath, TsPath.ToModuleSpecifier(reference.FileNameValue));
                    if (!updated.AsValue().EqualsValue(reference.FileNameValue))
                        tracker.ReplaceTextRangeWithText(file, reference.TextRange, updated.AsValue());
                }

                foreach (var import in file.Imports())
                {
                    var updated = getUpdatedImportSpecifier(program, checker, file, import, oldToNew, movedFiles, newImportFromPath, moved, preferences);
                    if (updated.Length != 0 && !updated.EqualsValue(import.TextValue()))
                        tracker.ReplaceTextRangeWithText(file, createStringTextRange(file, import), updated);
                }
            }
        }
        finally
        {
            done();
        }
    }

    private ByteStringValue getUpdatedImportSpecifier(CompilerProgram program, Checker checker, SourceFile sourceFile, Node importLiteral, PathUpdater oldToNew, Slice<MovedFile> movedFiles, RootedFilePath importingFileName, bool importingFileMoved, ModulePreferences preferences)
    {
        if (isAmbientModuleSymbol(checker.GetSymbolAtLocation(importLiteral)))
            return default;
        var target = getSourceFileToImport(program, sourceFile, importLiteral, oldToNew);
        if (target == null)
        {
            var updated = getUpdatedImportSpecifierFromMovedSourceFiles(program, sourceFile, importLiteral, movedFiles, importingFileName, preferences);
            if (updated.Length != 0 && !updated.EqualsValue(importLiteral.TextValue()))
                return updated;
            if (TsPath.IsExternalModuleNameRelative(importLiteral.TextValue()))
                return updateRelativePath(oldToNew, RootedFilePath.FromValue(sourceFile.FileNameGoValue()), importingFileName, TsPath.ToModuleSpecifier(importLiteral.TextValue())).AsValue();
            return default;
        }

        if (!target.updated && !(importingFileMoved && TsPath.IsExternalModuleNameRelative(importLiteral.TextValue())))
            return default;
        return ModuleSpecifiersApi.UpdateModuleSpecifier(program.Options(), program, sourceFile, importingFileName, TsPath.ToModuleSpecifier(importLiteral.TextValue()), target.newFileName, preferences, new ModuleSpecifierOptions { OverrideImportMode = program.GetModeForUsageLocation(sourceFile, importLiteral) }).AsValue();
    }

    private static ToImport? getSourceFileToImport(CompilerProgram program, SourceFile file, Node importLiteral, PathUpdater oldToNew)
    {
        var resolved = program.GetResolvedModuleFromModuleSpecifier(file, importLiteral);
        if (resolved != null && resolved.ResolvedFileName.AsValue().Length != 0)
        {
            var(newName, updated) = oldToNew(resolved.ResolvedFileName);
            return new ToImport
            {
                newFileName = updated ? newName : resolved.ResolvedFileName,
                updated = updated
            };
        }

        return null;
    }

    private static ByteStringValue getUpdatedImportSpecifierFromMovedSourceFiles(CompilerProgram program, SourceFile file, Node importLiteral, Slice<MovedFile> movedFiles, RootedFilePath importingName, ModulePreferences preferences)
    {
        var mode = program.GetModeForUsageLocation(file, importLiteral);
        foreach (var candidate in movedFiles)
        {
            var oldSpecifier = ModuleSpecifiersApi.UpdateModuleSpecifier(program.Options(), program, file, importingName, TsPath.ToModuleSpecifier(importLiteral.TextValue()), RootedFilePath.FromValue(candidate.SourceFile.FileNameGoValue()), preferences, new ModuleSpecifierOptions { OverrideImportMode = mode });
            if (!oldSpecifier.AsValue().EqualsValue(importLiteral.TextValue()))
                continue;
            return ModuleSpecifiersApi.UpdateModuleSpecifier(program.Options(), program, file, importingName, TsPath.ToModuleSpecifier(importLiteral.TextValue()), candidate.NewFileName, preferences, new ModuleSpecifierOptions { OverrideImportMode = mode }).AsValue();
        }

        return default;
    }

    private static TextRange createStringTextRange(SourceFile file, Node node) => new(ScannerApi.GetTokenPosOfNode(node, file, false) + 1, node.End() - 1);
    private static ObjectLiteralExpression? getTsConfigObjectLiteralExpression(SourceFile? file)
    {
        if (file?.Statements != null && file.Statements.Nodes.Length > 0)
        {
            var expression = file.Statements.Nodes[0].Expression();
            if (Ast.IsObjectLiteralExpression(expression))
                return expression.AsObjectLiteralExpression();
        }

        return null;
    }

    private static void forEachObjectProperty(ObjectLiteralExpression? objectLiteral, Action<PropertyAssignment, ByteStringValue> callback)
    {
        if (objectLiteral == null)
            return;
        foreach (var property in objectLiteral.Properties.Nodes)
        {
            if (!Ast.IsPropertyAssignment(property))
                continue;
            var(name, ok) = Ast.TryGetTextValueOfPropertyName(property.Name());
            if (ok)
                callback(property.AsPropertyAssignment(), name);
        }
    }

    private static FileSpec relativePathFromDirectory(RootedDirectoryPath directory, RootedFilePath file, CaseSensitivity sensitivity)
    {
        var(relative, ok) = sensitivity.RelativePathFromDirectory(directory, file);
        return TsPath.ToFileSpec(ok ? relative.AsValue() : file.AsValue());
    }

    private static ModuleSpecifier relativeImportPathFromDirectory(RootedDirectoryPath directory, RootedFilePath file, CaseSensitivity sensitivity)
    {
        var(relative, ok) = sensitivity.RelativePathFromDirectory(directory, file);
        return ok ? relative.AsModuleSpecifier() : file.AsModuleSpecifier();
    }

    private static bool isAmbientModuleSymbol(Symbol? symbol)
    {
        if (symbol == null)
            return false;
        foreach (var declaration in symbol.Declarations)
            if (Ast.IsModuleWithStringLiteralName(declaration))
                return true;
        return false;
    }
}
