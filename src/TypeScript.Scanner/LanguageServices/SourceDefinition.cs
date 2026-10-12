// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/sourcedefinition.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.Checking;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Lsp.Protocol;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private readonly record struct SourceDeclarationCandidate(Node Node, int Depth);
    private readonly record struct SourceDeclarationKey(RootedFilePath FileName, TextRange Range);
    // sourcedefinition.go:24. Preserve the standard link preference across projections.
    internal (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) ProvideSourceDefinition(ProtocolRequestContext context, DocumentUri uri, Position position)
    {
        var (program, file) = getProgramAndFile(uri);
        var positions = converters.FromLSPPositionForSourceFile(file, position, TypeScript.SpanMap.Feature.Definition);
        var results = Slice<LocationOrLocationsOrDefinitionLinksOrNull>.Make(0, positions.Length);
        foreach (var mapped in positions)
        {
            if (!mapped.Fidelity.IsSingleSegment())
                continue;
            var (result, error) = provideSourceDefinitionAtPosition(context, program, mapped.Script, mapped.Position);
            if (error != null)
                return (new(), error);
            results = results.Append(result);
        }

        return (combineDefinitionResponses(results, context.ClientCapabilities.TextDocument.Definition.LinkSupport), null);
    }

    private (LocationOrLocationsOrDefinitionLinksOrNull Result, Exception? Error) provideSourceDefinitionAtPosition(ProtocolRequestContext context, TypeScript.Compiler.Program program, SourceFile file, int position)
    {
        bool links = context.ClientCapabilities.TextDocument.Definition.LinkSupport;
        var resolver = newSourceDefResolver(program, RootedFilePath.FromValue(file.FileNameGoValue()));
        var node = TypeScript.AstNavigation.AstNav.GetTouchingPropertyName(file, position);
        if (node.Kind == Kind.SourceFile)
        {
            var (declarations, reference) = resolver.resolveTripleSlashReference(file, position, program);
            if (declarations.Length != 0)
            {
                var (range, _) = createLspRangeFromBounds(reference!.Pos(), reference.End(), file);
                return (createDefinitionLocations(range, links, declarations, null, TypeScript.SpanMap.Feature.Definition), null);
            }

            return (new(), null);
        }

        var (origin, _) = createLspRangeFromNode(node, file);
        var specifier = findContainingModuleSpecifier(node);
        if (specifier != null && Ast.IsSourcePhaseImport(specifier.Parent))
            return (provideDefinitionAtPosition(context, program, file, position, links), null);
        if (node == specifier)
        {
            var mode = program.GetModeForUsageLocation(file, specifier!);
            var implementation = resolver.resolveImplementation(specifier!.TextValue(), mode);
            if (implementation.AsValue().Length != 0 && resolver.getOrParseSourceFile(implementation) is { } source)
                return (createDefinitionLocations(origin, links, getSourceDefinitionEntryDeclarations(source), null, TypeScript.SpanMap.Feature.Definition), null);
            return (provideDefinitionAtPosition(context, program, file, position, links), null);
        }

        // Phase 1 uses only parsing and the NoDts resolver; no checker checkout.
        RootedFilePath resolvedImplementation = default;
        if (specifier != null)
            resolvedImplementation = resolver.resolveImplementation(specifier.TextValue(), program.GetModeForUsageLocation(file, specifier));
        if (resolvedImplementation.AsValue().Length != 0)
        {
            var names = getCandidateSourceDeclarationNames(node, null);
            var results = resolver.searchImplementationFile(node, resolvedImplementation, names);
            if (results.Length != 0 && (!Ast.IsPartOfTypeNode(node) && !Ast.IsPartOfTypeOnlyImportOrExportDeclaration(node) || hasConcreteSourceDeclarations(results)))
                return (createDefinitionLocations(origin, links, uniqueDeclarationNodes(results), null, TypeScript.SpanMap.Feature.Definition), null);
        }

        // Phase 2 holds the real checker lease only while obtaining its declarations/specifier.
        var (checkerDeclarations, moduleSpecifier) = getSourceDefCheckerInfo(context, program, file, node);
        // Phase 3 never reacquires the checker; resolve/map actual implementation declarations.
        var declarationsFromSource = resolver.resolveFromCheckerInfo(node, resolvedImplementation, checkerDeclarations, moduleSpecifier);
        if (declarationsFromSource.Length == 0)
        {
            if (specifier != null && resolvedImplementation.AsValue().Length != 0 && !hasConcreteSourceDeclarations(checkerDeclarations))
            {
                var source = resolver.getOrParseSourceFile(resolvedImplementation);
                if (source != null)
                    return (createDefinitionLocations(origin, links, getSourceDefinitionEntryDeclarations(source), null, TypeScript.SpanMap.Feature.Definition), null);
            }

            return (provideDefinitionAtPosition(context, program, file, position, links), null);
        }

        return (createDefinitionLocations(origin, links, declarationsFromSource, null, TypeScript.SpanMap.Feature.Definition), null);
    }

    private sealed class SourceDefResolutionHost : TypeScript.Modules.IResolutionHost
    {
        internal TypeScript.Vfs.IFS fs = null!;
        internal RootedDirectoryPath currentDirectory;
        public TypeScript.Vfs.IFS FS() => fs;
        public RootedDirectoryPath GetCurrentDirectory() => currentDirectory;
    }

    private SourceDefResolver newSourceDefResolver(TypeScript.Compiler.Program program, RootedFilePath from)
    {
        var options = program.Options();
        var noDts = options.Clone();
        noDts.NoDtsResolution = Tristate.True;
        return new SourceDefResolver
        {
            ls = this,
            fs = program.Host().FS(),
            options = options,
            getSourceFile = program.GetSourceFile,
            resolveFrom = from,
            resolver = TypeScript.Modules.DefaultResolver.NewResolver(new TypeScript.Modules.ResolverOptions { Host = new SourceDefResolutionHost { fs = program.Host().FS(), currentDirectory = program.BaseDirectory() }, CompilerOptions = noDts, TypingsLocation = program.GetGlobalTypingsCacheLocation(), ExtraExtensions = program.CommandLine().ContentMapperExtensions() })
        };
    }

    private sealed class SourceDefResolver
    {
        internal LanguageService ls = null!;
        internal TypeScript.Vfs.IFS fs = null!;
        internal CompilerOptions options = null!;
        internal Func<RootedFilePath, SourceFile?> getSourceFile = null!;
        internal RootedFilePath resolveFrom;
        internal TypeScript.Modules.DefaultResolver resolver = null!;
        internal Dictionary<RootedFilePath, SourceFile?>? parsedFiles;
        internal Slice<Node> resolveFromCheckerInfo(Node node, RootedFilePath implementation, Slice<Node> checkerDeclarations, ByteStringValue moduleSpecifier)
        {
            if (implementation.AsValue().Length == 0 && moduleSpecifier.Length != 0)
                implementation = resolveImplementation(moduleSpecifier, inferImpliedNodeFormat(resolveFrom));
            if (checkerDeclarations.Length == 0 && implementation.AsValue().Length != 0)
            {
                var results = searchImplementationFile(node, implementation, getCandidateSourceDeclarationNames(node, null));
                if (!results.IsNil)
                    return uniqueDeclarationNodes(results);
            }

            Slice<Node> declarations = default;
            foreach (var declaration in checkerDeclarations)
                declarations = declarations.Append(mapDeclarationToSource(node, declaration, implementation));
            declarations = uniqueDeclarationNodes(declarations);
            return hasConcreteSourceDeclarations(declarations) ? declarations : default;
        }

        internal (Slice<Node> Declarations, FileReference? Reference) resolveTripleSlashReference(SourceFile file, int position, TypeScript.Compiler.Program program)
        {
            var reference = getReferenceAtPosition(file, position, program);
            if (reference?.file == null)
                return (default, null);
            if (!reference.file.IsDeclarationFile)
                return (getSourceDefinitionEntryDeclarations(reference.file), reference.reference);
            var name = RootedFilePath.FromValue(reference.file.FileNameGoValue());
            var implementation = findImplementationFileFromDtsFileName(name, inferImpliedNodeFormat(name));
            if (implementation.AsValue().Length == 0)
                return (default, null);
            var source = getOrParseSourceFile(implementation);
            return source == null ? (default, null) : (getSourceDefinitionEntryDeclarations(source), reference.reference);
        }

        internal Slice<Node> searchImplementationFile(Node original, RootedFilePath implementation, Slice<ByteStringValue> names)
        {
            if (implementation.AsValue().Length == 0)
                return default;
            var file = getOrParseSourceFile(implementation);
            if (file == null)
                return default;
            if (isDefaultImportName(original))
            {
                HashSet<RootedFilePath>? seenDefaults = null;
                var defaults = findDeclarationsInFile(implementation, Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("default")]), ref seenDefaults);
                return defaults.Length != 0 ? filterPreferredSourceDeclarations(original, defaults) : getSourceDefinitionEntryDeclarations(file);
            }

            HashSet<RootedFilePath>? seenDeclarations = null;
            var declarations = findDeclarationsInFile(implementation, names, ref seenDeclarations);
            return declarations.Length != 0 ? filterPreferredSourceDeclarations(original, declarations) : default;
        }

        internal Slice<Node> mapDeclarationToSource(Node original, Node declaration, RootedFilePath implementation)
        {
            var (file, position) = getFileAndStartPosFromDeclaration(declaration);
            var name = RootedFilePath.FromValue(file.FileNameGoValue());
            var mapped = ls.tryGetSourcePosition(name, position);
            if (mapped != null && getOrParseSourceFile(mapped.FileName) is { } source)
                return Slice<Node>.FromArray([findClosestDeclarationNode(source, mapped.Pos)]);
            if (!name.IsDeclarationFile())
                return Slice<Node>.FromArray([declaration]);
            if (implementation.AsValue().Length == 0)
            {
                var declarationName = RootedFilePath.FromValue(Ast.GetSourceFileOfNode(declaration).FileNameGoValue());
                implementation = findImplementationFileFromDtsFileName(declarationName, inferImpliedNodeFormat(declarationName));
            }

            return searchImplementationFile(original, implementation, getCandidateSourceDeclarationNames(original, declaration));
        }

        internal RootedFilePath findImplementationFileFromDtsFileName(RootedFilePath name, ResolutionMode mode)
        {
            var extension = TypeScript.Modules.ModuleUtilities.TryGetJSExtensionForFileName(name, options);
            if (extension.Length != 0)
            {
                var candidate = name.ChangeExtension(extension);
                if (fs.FileExists(candidate))
                    return candidate;
            }

            var parts = TypeScript.ModuleSpecifiers.ModuleSpecifiers.GetNodeModulePathParts(name);
            if (parts == null || parts.HasNestedNodeModules || parts.IsDirectNodeModulesFile)
                return default;
            var packageName = TypeScript.Modules.ModuleUtilities.GetPackageNameFromTypesPackageNameValue(TypeScript.Modules.ModuleUtilities.UnmangleScopedPackageNameValue(parts.PackageName));
            if (packageName.Length == 0)
                return default;
            if (parts.PackageRelativePath.AsValue().Length != 0)
            {
                var specifier = TsPath.ToModuleSpecifier(packageName).ResolveRelative(parts.PackageRelativePath).RemoveFileExtension();
                var implementation = resolveImplementation(specifier.AsValue(), mode);
                if (implementation.AsValue().Length != 0)
                    return implementation;
            }

            return resolveImplementation(packageName, mode);
        }

        internal RootedFilePath resolveImplementation(ByteStringValue name, ResolutionMode mode) => resolveImplementationFrom(name, resolveFrom, mode);
        internal RootedFilePath resolveImplementationFrom(ByteStringValue name, RootedFilePath from, ResolutionMode preferred)
        {
            var modes = Slice<ResolutionMode>.FromArray([preferred]);
            if (preferred != ResolutionMode.ESNext)
                modes = modes.Append(ResolutionMode.ESNext);
            if (preferred != ResolutionMode.CommonJS)
                modes = modes.Append(ResolutionMode.CommonJS);
            foreach (var mode in modes)
            {
                var (resolved, _, _) = resolver.ResolveModuleName(name, from, mode, null);
                if (resolved != null && resolved.IsResolved() && !resolved.ResolvedFileName.IsDeclarationFile())
                    return resolved.ResolvedFileName;
            }

            return default;
        }

        internal SourceFile? getOrParseSourceFile(RootedFilePath name)
        {
            var file = getSourceFile(name);
            if (file != null)
                return file;
            if (parsedFiles != null && parsedFiles.TryGetValue(name, out file))
                return file;
            file = null;
            var (text, ok) = ls.ReadFile(name);
            if (ok)
            {
                var source = text.IsBytes ? text.ByteView : ByteStringView.FromGoBytes(text.Materialize());
                file = Parser.ParseSourceFileOwned(new SourceFileParseOptions(name.AsValue(), ls.program.PathKeyForFileName(name).AsValue(), FileNameIsGoBytes: true), source, Core.EnsureScriptKindFromFileName(name));
                Binder.BindSourceFile(file);
            }

            (parsedFiles ??= new())[name] = file;
            return file;
        }

        internal ResolutionMode inferImpliedNodeFormat(RootedFilePath name)
        {
            string packageType = "";
            var scope = resolver.GetPackageScopeForPath(name.Directory());
            if (scope != null && scope.Exists())
            {
                var (value, ok) = scope.Contents!.Fields.Type.GetValue();
                if (ok)
                    packageType = value;
            }

            return (ResolutionMode)Ast.GetImpliedNodeFormatForFile(name, packageType);
        }

        internal Slice<Node> findDeclarationsInFile(RootedFilePath name, Slice<ByteStringValue> names, ref HashSet<RootedFilePath>? seen)
        {
            if (name.AsValue().Length == 0 || names.Length == 0 || !(seen ??= new()).Add(name))
                return default;
            var file = getOrParseSourceFile(name);
            if (file == null)
                return default;
            var declarations = findDeclarationNodesByName(file, names);
            if (declarations.Length != 0 && hasConcreteSourceDeclarations(declarations))
                return declarations;
            Slice<Node> forwarded = default;
            foreach (var forwardedFile in getForwardedImplementationFiles(file))
                forwarded = forwarded.Append(findDeclarationsInFile(forwardedFile, names, ref seen));
            if (forwarded.Length == 0)
                return declarations;
            if (hasConcreteSourceDeclarations(forwarded))
                return uniqueDeclarationNodes(forwarded);
            return uniqueDeclarationNodes(declarations.Slice3(0, declarations.Length, declarations.Length).Append(forwarded));
        }

        internal Slice<RootedFilePath> getForwardedImplementationFiles(SourceFile file)
        {
            var name = RootedFilePath.FromValue(file.FileNameGoValue());
            var mode = inferImpliedNodeFormat(name);
            Slice<RootedFilePath> files = default;
            foreach (var import in file.Imports())
            {
                var implementation = resolveImplementationFrom(import.TextValue(), name, mode);
                if (implementation.AsValue().Length != 0)
                    files = files.Append(implementation);
            }

            return Core.Deduplicate(files);
        }
    }

    private static (Slice<Node> Declarations, ByteStringValue ModuleSpecifier) getSourceDefCheckerInfo(ProtocolRequestContext context, TypeScript.Compiler.Program program, SourceFile file, Node node)
    {
        var (checker, done) = program.GetTypeCheckerForFile(context, file);
        try
        {
            var declarations = getDeclarationsFromLocation(checker, node);
            bool propertyName = node.Parent != null && Ast.IsAccessExpression(node.Parent) && node.Parent.Name() == node;
            if (declarations.Length == 0 && propertyName && node.Parent!.Expression() is { } left)
            {
                var property = checker.GetPropertyOfType(checker.GetTypeAtLocation(left), node.TextValue());
                if (property != null)
                    declarations = property.Declarations;
            }

            var called = tryGetSignatureDeclaration(checker, node);
            if (called != null)
                declarations = Core.Filter(declarations, static declaration => !Ast.IsFunctionLike(declaration)).Append(called);
            ByteStringValue moduleSpecifier = default;
            var resolveNode = node;
            if (propertyName)
            {
                var expression = node.Parent!.Expression();
                while (expression != null && Ast.IsAccessExpression(expression))
                    expression = expression.Expression();
                if (expression != null)
                    resolveNode = expression;
            }

            var symbol = checker.GetSymbolAtLocation(resolveNode);
            if (symbol != null)
                foreach (var declaration in symbol.Declarations)
                {
                    if (!Ast.IsImportSpecifier(declaration) && !Ast.IsImportClause(declaration) && !Ast.IsNamespaceImport(declaration) && !Ast.IsImportEqualsDeclaration(declaration))
                        continue;
                    var specifier = NodeBuilderImpl.TryGetModuleSpecifierFromDeclaration(declaration);
                    if (specifier == null)
                        continue;
                    moduleSpecifier = specifier.TextValue();
                    break;
                }

            return (declarations, moduleSpecifier);
        }
        finally
        {
            done();
        }
    }

    private static bool isDefaultImportName(Node? node) => node != null && node.Parent != null && Ast.IsImportClause(node.Parent) && node.Parent.Name() == node && node.Parent.Parent != null && Ast.IsDefaultImport(node.Parent.Parent);
    private static Node getSourceDefinitionEntryNode(SourceFile file) => file.Statements.Nodes.Length != 0 ? file.Statements.Nodes[0] : file;
    private static Slice<Node> getSourceDefinitionEntryDeclarations(SourceFile file) => Slice<Node>.FromArray([getSourceDefinitionEntryNode(file)]);
    private static Node? findContainingModuleSpecifier(Node node)
    {
        for (Node? current = node; current != null; current = current.Parent)
            if (Ast.IsAnyImportOrReExport(current) || Ast.IsRequireCall(current, true) || Ast.IsImportCall(current))
            {
                var specifier = Ast.GetExternalModuleName(current);
                if (specifier != null && Ast.IsStringLiteralLike(specifier))
                    return specifier;
            }

        return null;
    }

    // sourcedefinition.go:548. Name headers remain borrowed through the search key owner.
    private static Slice<ByteStringValue> getCandidateSourceDeclarationNames(Node? original, Node? declaration)
    {
        Slice<ByteStringValue> names = default;
        if (declaration != null)
        {
            var name = Ast.GetNameOfDeclaration(declaration);
            if (name != null)
            {
                var (text, _) = Ast.TryGetTextValueOfPropertyName(name);
                if (text.Length != 0)
                    names = names.Append(text);
            }

            if (declaration.Kind == Kind.ExportAssignment)
                names = names.Append(ByteStringValue.FromString("default"));
            if ((Ast.IsFunctionDeclaration(declaration) || Ast.IsClassDeclaration(declaration)) && (declaration.ModifierFlags() & ModifierFlags.ExportDefault) == ModifierFlags.ExportDefault)
                names = names.Append(ByteStringValue.FromString("default"));
            if ((Ast.IsImportSpecifier(declaration) || Ast.IsExportSpecifier(declaration)) && declaration.PropertyName() is { } property)
                names = names.Append(property.TextValue());
        }

        if (original != null)
        {
            if (Ast.IsIdentifier(original) || Ast.IsPrivateIdentifier(original))
                names = names.Append(original.TextValue());
            if (isDefaultImportName(original))
                names = names.Append(ByteStringValue.FromString("default"));
            if (original.Parent != null && (Ast.IsImportSpecifier(original.Parent) || Ast.IsExportSpecifier(original.Parent)) && original.Parent.PropertyName() is { } property)
                names = names.Append(property.TextValue());
        }

        return names;
    }

    private static Slice<Node> findDeclarationNodesByName(SourceFile file, Slice<ByteStringValue> names)
    {
        names = Core.Deduplicate(Core.Filter(names, static name => name.Length != 0));
        if (names.Length == 0)
            return default;
        HashSet<ByteStringKey>? wanted = null;
        bool wantDefault = false;
        foreach (var name in names)
        {
            if (name.EqualsValue(ByteStringValue.FromString("default")))
            {
                wantDefault = true;
                continue;
            }

            (wanted ??= new()).Add(ByteStringKey.FromValue(name));
        }

        Slice<SourceDeclarationCandidate> candidates = default;
        int minDepth = int.MaxValue;
        Visitor visit = null!;
        visit = node =>
        {
            bool matched = false;
            if (Ast.GetNameOfDeclaration(node) is { } name)
            {
                var (text, _) = Ast.TryGetTextValueOfPropertyName(name);
                if (text.Length != 0 && wanted?.Contains(ByteStringKey.FromValue(text)) == true)
                    matched = true;
            }

            if (wantDefault && node.Kind == Kind.ExportAssignment)
                matched = true;
            if (wantDefault && (Ast.IsFunctionDeclaration(node) || Ast.IsClassDeclaration(node)) && (node.ModifierFlags() & ModifierFlags.ExportDefault) == ModifierFlags.ExportDefault)
                matched = true;
            if (matched)
            {
                int depth = getContainerDepth(node);
                candidates = candidates.Append(new SourceDeclarationCandidate(node, depth));
                if (depth < minDepth)
                    minDepth = depth;
            }

            return node.ForEachChild(visit);
        };
        file.ForEachChild(visit);
        Slice<Node> declarations = default;
        foreach (var candidate in candidates)
            if (candidate.Depth == minDepth)
                declarations = declarations.Append(candidate.Node);
        return uniqueDeclarationNodes(declarations);
    }

    private static int getContainerDepth(Node node)
    {
        int depth = 0;
        Node? current = node;
        while (current != null)
        {
            current = getContainerNode(current);
            depth++;
        }

        return depth;
    }

    private static Slice<Node> filterPreferredSourceDeclarations(Node? original, Slice<Node> declarations)
    {
        if (declarations.Length <= 1 || original == null)
            return declarations;
        var preferred = getPropertyLikeSourceDeclarations(original, declarations);
        if (preferred.Length != 0)
            return preferred;
        preferred = Core.Filter(declarations, isConcreteSourceDeclaration);
        return preferred.Length != 0 ? preferred : declarations;
    }

    private static Slice<Node> getPropertyLikeSourceDeclarations(Node original, Slice<Node> declarations)
    {
        if (original.Parent == null || !Ast.IsAccessExpression(original.Parent) || original.Parent.Name() != original)
            return default;
        return Core.Filter(declarations, static node => node.Kind is Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment or Kind.PropertyDeclaration or Kind.PropertySignature or Kind.MethodDeclaration or Kind.MethodSignature or Kind.GetAccessor or Kind.SetAccessor or Kind.EnumMember);
    }

    private static bool hasConcreteSourceDeclarations(Slice<Node> declarations) => Core.Some(declarations, isConcreteSourceDeclaration);
    private static bool isConcreteSourceDeclaration(Node node)
    {
        if (!Ast.IsDeclaration(node) || node.Kind == Kind.ExportAssignment)
            return false;
        if ((Ast.IsBinaryExpression(node) || Ast.IsCallExpression(node)) && Ast.GetAssignmentDeclarationKind(node) != JSDeclarationKind.None)
            return false;
        return node.Kind is not (Kind.Parameter or Kind.TypeParameter or Kind.BindingElement or Kind.ImportClause or Kind.ImportSpecifier or Kind.NamespaceImport or Kind.ExportSpecifier or Kind.PropertyAccessExpression or Kind.ElementAccessExpression);
    }

    private static Slice<Node> uniqueDeclarationNodes(Slice<Node> nodes)
    {
        HashSet<SourceDeclarationKey>? seen = null;
        var result = Slice<Node>.Make(0, nodes.Length);
        foreach (var node in nodes)
        {
            if (node == null)
                continue;
            var key = new SourceDeclarationKey(RootedFilePath.FromValue(Ast.GetSourceFileOfNode(node).FileNameGoValue()), node.Loc);
            if (!(seen ??= new()).Add(key))
                continue;
            result = result.Append(node);
        }

        return result;
    }

    private static Node findClosestDeclarationNode(SourceFile file, int position)
    {
        for (Node? current = TypeScript.AstNavigation.AstNav.GetTouchingPropertyName(file, position); current != null; current = current.Parent)
            if (Ast.IsDeclaration(current) || current.Kind == Kind.ExportAssignment)
                return current;
        return getSourceDefinitionEntryNode(file);
    }
}
