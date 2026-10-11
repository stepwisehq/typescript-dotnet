// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/tsconfigparsing.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Modules;
using TypeScript.Collections;
using TypeScript.Vfs;
using TypeScript.Vfs.VfsMatch;

namespace TypeScript.TsOptions;
public sealed class CommandLineOptionNameMap : Dictionary<string, CommandLineOption>
{
    public CommandLineOptionNameMap() : base(StringComparer.Ordinal)
    {
    }

    public CommandLineOption? Get(string name) => TryGetValue(name, out var value) || TryGetValue(TsOptions.goLower(name), out value) ? value : null;
    public CommandLineOption? GetSpellingSuggestion(string name) => Core.GetSpellingSuggestion(name, Values, option => option.Name, (a, b) => Math.Sign(string.CompareOrdinal(a.Name, b.Name)));
}

public static partial class TsOptions
{
    internal const string defaultIncludeSpec = "**/*";
    private readonly record struct propOfRaw(List<object?>? sliceValue, string wrongValue = "");
    public static ParsedCommandLine ParseJsonSourceFileConfigFileContent(TsConfigSourceFile sourceFile, IFS fs, RootedDirectoryPath basePath, CompilerOptions? existingOptions = null, OrderedMap<string, object?>? existingOptionsRaw = null, List<PathKey>? resolutionStack = null, IExtendedConfigCache? extendedConfigCache = null) => parseJsonConfigFileContentWorker(null, sourceFile, fs, basePath, existingOptions, existingOptionsRaw, sourceFile.FileName(), resolutionStack, extendedConfigCache);
    public static ParsedCommandLine ParseJsonConfigFileContent(object? json, IFS fs, RootedDirectoryPath basePath, CompilerOptions? existingOptions = null, RootedFilePath configFileName = default, List<PathKey>? resolutionStack = null, IExtendedConfigCache? extendedConfigCache = null) => parseJsonConfigFileContentWorker(normalizeJsonValue(json) as OrderedMap<string, object?> ?? new(), null, fs, basePath, existingOptions, null, configFileName, resolutionStack, extendedConfigCache);
    public static (ParsedCommandLine? Parsed, Slice<Diagnostic> Errors) GetParsedCommandLineOfConfigFile(RootedFilePath fileName, CompilerOptions? options, OrderedMap<string, object?>? rawOptions, IFS fs, IExtendedConfigCache? cache = null) => GetParsedCommandLineOfConfigFilePath(fileName, fs.CaseSensitivity().PathKey(fileName.AsPath()), options, rawOptions, fs, cache);
    public static (ParsedCommandLine? Parsed, Slice<Diagnostic> Errors) GetParsedCommandLineOfConfigFilePath(RootedFilePath fileName, PathKey path, CompilerOptions? options, OrderedMap<string, object?>? rawOptions, IFS fs, IExtendedConfigCache? cache = null)
    {
        var(text, errors) = tryReadFile(fileName, fs.ReadFile, Slice<Diagnostic>.Make(0));
        if (errors is { Length: > 0 })
            return (null, errors);
        var source = NewTsconfigSourceFileFromFilePath(fileName, path, text);
        return (ParseJsonSourceFileConfigFileContent(source, fs, fileName.Directory(), options, rawOptions, null, cache), default);
    }

    internal static ParsedCommandLine parseJsonConfigFileContentWorker(OrderedMap<string, object?>? json, TsConfigSourceFile? sourceFile, IFS fs, RootedDirectoryPath basePath, CompilerOptions? existingOptions, OrderedMap<string, object?>? existingOptionsRaw, RootedFilePath configFileName, List<PathKey>? resolutionStack, IExtendedConfigCache? cache)
    {
        if ((json == null) == (sourceFile == null))
            throw new InvalidOperationException("Expected exactly one configuration input");
        var namesBase = configFileName.AsValue().Length != 0 ? configFileName.Directory() : basePath;
        var(parsed, errors) = parseConfig(json, sourceFile, fs, basePath, configFileName, resolutionStack, cache);
        mergeParsedCompilerOptions(parsed.options, new() { CompilerOptions = existingOptions! }, existingOptionsRaw);
        handleOptionConfigDirTemplateSubstitution(parsed.options, namesBase);
        var raw = parseJsonToStringKey(parsed.raw);
        if (configFileName.AsValue().Length != 0 && parsed.options != null)
            parsed.options.CompilerOptions.ConfigFilePath = configFileName;
        propOfRaw getPropFromRaw(string name, Func<object?, bool> validateElement, string elementType)
        {
            var(value, present) = raw.Get(name);
            if (present && value != null)
            {
                if (value is List<object?> values)
                {
                    if (sourceFile == null && !Core.Every(values, validateElement))
                        errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.Compiler_option_0_requires_a_value_of_type_1, name, elementType));
                    return new(values);
                }

                if (sourceFile == null)
                {
                    errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.Compiler_option_0_requires_a_value_of_type_1, name, "Array"));
                    return new(null, "not-array");
                }
            }

            return new(null, "no-prop");
        }

        var referencesOfRaw = getPropFromRaw("references", value => value is OrderedMap<string, object?>, "object");
        var filesOfRaw = getPropFromRaw("files", isStringValue, "string");
        if (filesOfRaw.sliceValue != null || filesOfRaw.wrongValue == "")
        {
            bool noReferences = referencesOfRaw.wrongValue is "no-prop" or "not-array" || (referencesOfRaw.sliceValue?.Count ?? 0) == 0;
            if (filesOfRaw.sliceValue is { Count: 0 } && noReferences && raw.GetOrZero("extends") == null)
            {
                if (sourceFile != null)
                {
                    string name = configFileName.AsValue().Length != 0 ? configFileName.AsString() : "tsconfig.json";
                    var node = ForEachTsConfigPropArray(sourceFile.SourceFile, "files", property => property.Initializer);
                    errors = errors.Append(CreateDiagnosticForNodeInSourceFile(sourceFile.SourceFile, node!, Diagnostics.The_files_list_in_config_file_0_is_empty, name));
                }
                else
                    errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.The_files_list_in_config_file_0_is_empty, configFileName));
            }
        }

        var includesOfRaw = getPropFromRaw("include", isStringValue, "string");
        var excludesOfRaw = getPropFromRaw("exclude", isStringValue, "string");
        bool defaultInclude = false;
        if (excludesOfRaw.wrongValue == "no-prop" && parsed.options != null)
        {
            var outDirectory = parsed.options.CompilerOptions.OutDir;
            var declarationDirectory = parsed.options.CompilerOptions.DeclarationDir;
            if (outDirectory.AsValue().Length != 0 || declarationDirectory.AsValue().Length != 0)
            {
                var values = new List<object?>();
                if (outDirectory.AsValue().Length != 0)
                    values.Add(outDirectory.AsValue());
                if (declarationDirectory.AsValue().Length != 0)
                    values.Add(declarationDirectory.AsValue());
                excludesOfRaw = new(values);
            }
        }

        if (filesOfRaw.sliceValue == null && includesOfRaw.sliceValue == null)
        {
            includesOfRaw = new(new() { defaultIncludeSpec });
            defaultInclude = true;
        }

        List<PathPattern>? includes = null, includesBefore = null, excludes = null;
        List<FileSpec>? files = null, filesBefore = null;
        if (includesOfRaw.sliceValue != null)
        {
            var(validated, validationErrors) = validateSpecs(includesOfRaw.sliceValue, true, tsconfigToSourceFile(sourceFile), "include");
            includesBefore = validated;
            if (!validationErrors.IsNil)
                errors = errors.Append(validationErrors);
            includes = substitutePatterns(includesBefore, namesBase) ?? includesBefore;
        }

        if (excludesOfRaw.sliceValue != null)
        {
            var(validated, validationErrors) = validateSpecs(excludesOfRaw.sliceValue, false, tsconfigToSourceFile(sourceFile), "exclude");
            excludes = validated;
            if (!validationErrors.IsNil)
                errors = errors.Append(validationErrors);
            excludes = substitutePatterns(excludes, namesBase) ?? excludes;
        }

        if (filesOfRaw.sliceValue != null)
        {
            foreach (var value in Core.Filter(filesOfRaw.sliceValue, isStringValue)!)
                (filesBefore ??= new()).Add(TsPath.ToFileSpec(parsePathText(value)));
            files = substituteFileSpecs(filesBefore, namesBase) ?? filesBefore;
        }

        var sensitivity = fs.CaseSensitivity();
        var validatedNames = new List<RootedFilePath>(files?.Count ?? 0);
        var specByPath = new Dictionary<PathKey, FileSpec>(files?.Count ?? 0);
        for (int i = 0; i < (files?.Count ?? 0); i++)
        {
            var name = files![i].AsValue().Length == 0 ? TsPath.RootedFilePathFromPath(namesBase.AsPath()) : TsPath.ToRootedFilePath(files[i].AsValue(), namesBase);
            validatedNames.Add(name);
            specByPath.TryAdd(sensitivity.PathKey(name.AsPath()), filesBefore![i]);
        }

        var specs = new configFileSpecs
        {
            filesSpecs = filesOfRaw.sliceValue,
            includeSpecs = includesOfRaw.sliceValue,
            excludeSpecs = excludesOfRaw.sliceValue,
            validatedFilesSpec = files,
            validatedFileNames = validatedNames,
            fileSpecByPath = specByPath,
            validatedIncludeSpecs = includes,
            validatedExcludeSpecs = excludes,
            validatedFilesSpecBeforeSubstitution = filesBefore,
            validatedIncludeSpecsBeforeSubstitution = includesBefore,
            isDefaultIncludeSpec = defaultInclude,
        };
        if (sourceFile != null)
            sourceFile.configFileSpecs = specs;
        var mapperSource = tsconfigToSourceFile(sourceFile);
        List<TypeScript.ContentMapper.Mapper>? mappers = null;
        List<int>? mapperIndices = null;
        var mappersOfRaw = getPropFromRaw("contentMappers", value => value is OrderedMap<string, object?>, "object");
        for (int i = 0; i < (mappersOfRaw.sliceValue?.Count ?? 0); i++)
        {
            var(mapper, mapperErrors) = parseContentMapper(mappersOfRaw.sliceValue![i]);
            if (!mapperErrors.IsNil)
                foreach (var error in mapperErrors)
                    errors = errors.Append(setContentMapperDiagnosticLocation(error, mapperSource, getContentMapperSyntax(mapperSource, i, "")));
            if (mapper != null)
            {
                (mappers ??= new()).Add(mapper);
                (mapperIndices ??= new()).Add(i);
            }
        }

        int extensionCount = mappers?.Sum(mapper => mapper.Extensions?.Count ?? 0) ?? 0;
        var seenExtensions = new HashSet<string>(extensionCount, StringComparer.Ordinal);
        List<string>? mapperExtensions = new(extensionCount);
        var nativeExtensions = TsPath.AllSupportedExtensionsWithJson.SelectMany(group => group).ToList();
        for (int i = 0; i < (mappers?.Count ?? 0); i++)
        {
            var mapper = mappers![i];
            var valid = new List<string>(mapper.Extensions?.Count ?? 0);
            if (mapper.Extensions != null)
                foreach (var extension in mapper.Extensions)
                {
                    var node = getContentMapperExtensionSyntax(mapperSource, mapperIndices![i], extension);
                    string canonical = sensitivity.Canonicalize(extension);
                    Message? message = !extension.StartsWith('.') ? Diagnostics.Content_mapper_file_extension_0_must_begin_with_a : nativeExtensions.Any(native => UnicodeCase.EqualFold(native, extension)) ? Diagnostics.Content_mapper_file_extension_0_is_a_built_in_extension_and_cannot_be_registered_by_a_content_mapper : null;
                    if (message == null && !seenExtensions.Add(canonical))
                        message = Diagnostics.Content_mapper_file_extension_0_is_registered_by_more_than_one_content_mapper;
                    if (message != null)
                        errors = errors.Append(setContentMapperDiagnosticLocation(Ast.NewCompilerDiagnostic(message, extension), mapperSource, node));
                    else
                    {
                        mapperExtensions.Add(extension);
                        valid.Add(extension);
                    }
                }

            mapper.Extensions = valid;
        }

        if ((mappers?.Count ?? 0) > 0 && parsed.options?.CompilerOptions.RunExternalCode != Tristate.True)
        {
            errors = errors.Append(setContentMapperDiagnosticLocation(Ast.NewCompilerDiagnostic(Diagnostics.Content_mappers_require_the_runExternalCode_command_line_flag_to_be_enabled), mapperSource, getContentMappersKeySyntax(mapperSource)));
            mappers = null;
            mapperExtensions = null;
        }
        else if ((mappers?.Count ?? 0) > 0)
        {
            var containing = configFileName.AsValue().Length != 0 ? configFileName : namesBase.ResolveFile("tsconfig.json");
            var resolved = new List<TypeScript.ContentMapper.Mapper>(mappers!.Count);
            for (int i = 0; i < mappers.Count; i++)
            {
                var mapper = mappers[i];
                var(manifest, directory, error) = resolveContentMapperManifest(fs, containing, mapper.Package);
                mapper.PackageDirectory = directory;
                if (error != null)
                {
                    errors = errors.Append(setContentMapperDiagnosticLocation(error, mapperSource, getContentMapperSyntax(mapperSource, mapperIndices![i], "package")));
                    continue;
                }

                mapper.Manifest = manifest;
                resolved.Add(mapper);
            }

            mappers = resolved;
            mapperExtensions = null;
            foreach (var mapper in mappers)
                if (mapper.Extensions != null)
                    (mapperExtensions ??= new()).AddRange(mapper.Extensions);
        }

        (List<RootedFilePath> Files, int LiteralCount) getFileNames(RootedDirectoryPath directory)
        {
            var result = getFileNamesFromConfigSpecs(specs, directory, parsed.options!.CompilerOptions, fs, mapperExtensions);
            if (shouldReportNoInputFiles(result.Files, canJsonReportNoInputFiles(raw), resolutionStack))
                errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.No_inputs_were_found_in_config_file_0_Specified_include_paths_were_1_and_exclude_paths_were_2, configFileName, TypeScript.Json.JsonCodec.MarshalString(specs.includeSpecs ?? new List<string>()), TypeScript.Json.JsonCodec.MarshalString(specs.excludeSpecs ?? new List<string>())));
            return result;
        }

        List<ProjectReference>? getProjectReferences(RootedDirectoryPath directory)
        {
            List<ProjectReference>? result = null;
            var references = getPropFromRaw("references", value => value is OrderedMap<string, object?>, "object");
            if (references.sliceValue == null)
                return result;
            result = new();
            for (int i = 0; i < references.sliceValue.Count; i++)
            {
                var reference = parseProjectReference(references.sliceValue[i]);
                if (reference == null)
                    continue;
                if (!reference.hasPath || !reference.pathValid)
                {
                    errors = errors.Append(createDiagnosticAtProjectReferenceProperty(sourceFile, i, "path", Diagnostics.Compiler_option_0_requires_a_value_of_type_1, "reference.path", "string"));
                    continue;
                }

                if (reference.path == "")
                {
                    errors = errors.Append(createDiagnosticAtProjectReferenceProperty(sourceFile, i, "path", Diagnostics.Compiler_option_0_cannot_be_given_an_empty_string, "reference.path"));
                    continue;
                }

                if (reference.hasCircular && !reference.circularValid)
                    errors = errors.Append(createDiagnosticAtProjectReferenceProperty(sourceFile, i, "circular", Diagnostics.Compiler_option_0_requires_a_value_of_type_1, "reference.circular", "boolean"));
                result.Add(new() { Path = TsPath.ToRootedPath(reference.path, directory), OriginalPath = reference.path, Circular = reference.reference.Circular });
            }

            return result;
        }

        var(fileNames, literalLength) = getFileNames(namesBase);
        var compileOnSave = new System.Runtime.CompilerServices.StrongBox<bool>(false);
        if (parsed.raw is OrderedMap<string, object?> rawMap && rawMap.GetOrZero("compileOnSave")is bool flag)
            compileOnSave = new(flag);
        var references = getProjectReferences(namesBase);
        return new()
        {
            ParsedConfig = new()
            {
                CompilerOptions = parsed.options!.CompilerOptions,
                TypeAcquisition = parsed.typeAcquisition,
                FileNames = fileNames,
                ProjectReferences = references,
                ContentMappers = mappers
            },
            ConfigFile = sourceFile,
            Raw = parsed.raw,
            Errors = errors,
            CompileOnSave = compileOnSave,
            configFileSpecs = specs,
            baseDirectory = namesBase,
            caseSensitivity = sensitivity,
            literalFileNamesLen = literalLength,
        };
    }

    // Static specializations of the original ~string generic preserve nil/unchanged slice identity.
    private static List<PathPattern>? substitutePatterns(List<PathPattern>? values, RootedDirectoryPath directory)
    {
        List<PathPattern>? result = null;
        for (int i = 0; i < (values?.Count ?? 0); i++)
            if (startsWithConfigDirTemplate(values![i].AsValue()))
            {
                result ??= new(values);
                result[i] = PathPattern.FromValue(getSubstitutedPathWithConfigDirTemplate(values[i].AsValue(), directory));
            }

        return result;
    }

    private static List<FileSpec>? substituteFileSpecs(List<FileSpec>? values, RootedDirectoryPath directory)
    {
        List<FileSpec>? result = null;
        for (int i = 0; i < (values?.Count ?? 0); i++)
            if (startsWithConfigDirTemplate(values![i].AsValue()))
            {
                result ??= new(values);
                result[i] = FileSpec.FromValue(getSubstitutedPathWithConfigDirTemplate(values[i].AsValue(), directory));
            }

        return result;
    }

    public static T? ForEachPropertyAssignment<T>(ObjectLiteralExpression? objectLiteral, string key, Func<PropertyAssignment, T?> callback, params string[] alternateKeys)
        where T : class
    {
        if (objectLiteral?.Properties?.Nodes != null)
            foreach (var property in objectLiteral.Properties.Nodes)
            {
                if (!Ast.IsPropertyAssignment(property))
                    continue;
                var(name, ok) = Ast.TryGetTextValueOfPropertyName(property.Name());
                if (ok && (name.EqualsValue(ByteStringValue.FromString(key)) || alternateKeys.Length > 0 && name.EqualsValue(ByteStringValue.FromString(alternateKeys[0]))))
                    return callback(property.AsPropertyAssignment());
            }

        return null;
    }

    internal static ObjectLiteralExpression? getTsConfigObjectLiteralExpression(SourceFile? sourceFile)
    {
        var expression = sourceFile?.Statements?.Nodes is { Count: > 0 } statements ? statements[0].Expression() : null;
        return Ast.IsObjectLiteralExpression(expression) ? expression!.AsObjectLiteralExpression() : null;
    }

    public static T? ForEachTsConfigPropArray<T>(SourceFile? sourceFile, string propertyKey, Func<PropertyAssignment, T?> callback)
        where T : class => sourceFile != null ? ForEachPropertyAssignment(getTsConfigObjectLiteralExpression(sourceFile), propertyKey, callback) : null;
    public static Func<PropertyAssignment, Node?> GetCallbackForFindingPropertyAssignmentByValue(string value) => property => Ast.IsArrayLiteralExpression(property.Initializer) ? Core.Find(property.Initializer.Elements(), element => Ast.IsStringLiteral(element) && element.TextValue().EqualsValue(ByteStringValue.FromString(value))) : null;
    public static StringLiteral? GetTsConfigPropArrayElementValue(SourceFile? sourceFile, string key, string value)
    {
        var callback = GetCallbackForFindingPropertyAssignmentByValue(value);
        return ForEachTsConfigPropArray(sourceFile, key, property => callback(property)?.AsStringLiteral());
    }

    internal static StringLiteral? GetTsConfigPropArrayElementValue(SourceFile? sourceFile, string key, ByteStringValue value) =>
        ForEachTsConfigPropArray(sourceFile, key, property => Ast.IsArrayLiteralExpression(property.Initializer)
            ? Core.Find(property.Initializer.Elements(), value, static (element, value) => Ast.IsStringLiteral(element) && element.TextValue().EqualsValue(value))?.AsStringLiteral() : null);

    public static Node? GetOptionsSyntaxByArrayElementValue(ObjectLiteralExpression? objectLiteral, string key, string value) => ForEachPropertyAssignment(objectLiteral, key, GetCallbackForFindingPropertyAssignmentByValue(value));
    internal static Node? GetOptionsSyntaxByArrayElementValue(ObjectLiteralExpression? objectLiteral, string key, ByteStringValue value) =>
        ForEachPropertyAssignment(objectLiteral, key, property => Ast.IsArrayLiteralExpression(property.Initializer)
            ? Core.Find(property.Initializer.Elements(), value, static (element, value) => Ast.IsStringLiteral(element) && element.TextValue().EqualsValue(value)) : null);

    public static Diagnostic? CreateDiagnosticAtReferenceSyntax(ParsedCommandLine config, int index, Message message, params object[] args) => config.ConfigFile == null ? null : ForEachTsConfigPropArray(config.ConfigFile.SourceFile, "references", property => Ast.IsArrayLiteralExpression(property.Initializer) && property.Initializer.Elements().Count > index ? CreateDiagnosticForNodeInSourceFile(config.ConfigFile.SourceFile, property.Initializer.Elements()[index], message, args) : null);
    internal static Diagnostic createDiagnosticAtProjectReferenceProperty(TsConfigSourceFile? sourceFile, int index, string propertyName, Message message, params object[] args)
    {
        var node = sourceFile != null ? ForEachTsConfigPropArray(sourceFile.SourceFile, "references", property =>
        {
            if (!Ast.IsArrayLiteralExpression(property.Initializer))
                return null;
            var elements = property.Initializer.Elements();
            if (elements.Count <= index || !Ast.IsObjectLiteralExpression(elements[index]))
                return null;
            return ForEachPropertyAssignment(elements[index].AsObjectLiteralExpression(), propertyName, property => property.Initializer) ?? elements[index];
        }) : null;
        return CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(tsconfigToSourceFile(sourceFile), node, message, args);
    }

    internal static Node? getContentMapperSyntax(SourceFile? sourceFile, int index, string subKey) => ForEachTsConfigPropArray(sourceFile, "contentMappers", property =>
    {
        if (!Ast.IsArrayLiteralExpression(property.Initializer))
            return property.Initializer;
        var elements = property.Initializer.Elements();
        if (index < 0 || index >= elements.Count)
            return property.Initializer;
        var element = elements[index];
        return subKey != "" && Ast.IsObjectLiteralExpression(element) ? ForEachPropertyAssignment(element.AsObjectLiteralExpression(), subKey, property => property.Initializer) ?? element : element;
    });
    internal static Node? getContentMappersKeySyntax(SourceFile? sourceFile) => ForEachTsConfigPropArray(sourceFile, "contentMappers", property => property.AsNode().Name());
    internal static Node? getContentMapperExtensionSyntax(SourceFile? sourceFile, int index, string extension)
    {
        var node = getContentMapperSyntax(sourceFile, index, "extensions");
        return node != null && Ast.IsArrayLiteralExpression(node) ? Core.Find(node.Elements(), element => Ast.IsStringLiteral(element) && element.TextValue().EqualsValue(ByteStringValue.FromString(extension))) ?? node : node;
    }

    internal static Diagnostic setContentMapperDiagnosticLocation(Diagnostic diagnostic, SourceFile? sourceFile, Node? node)
    {
        if (sourceFile != null && node != null)
        {
            diagnostic.SetFile(sourceFile);
            diagnostic.SetLocation(new(global::TypeScript.Scanner.Scanner.SkipTriviaByteString(sourceFile.TextView(), node.Pos()), node.End()));
        }

        return diagnostic;
    }

    public static (SourceFile? File, TextRange Range) GetContentMapperOptionDiagnosticLocation(ParsedCommandLine? config, TypeScript.ContentMapper.Mapper mapper, List<TypeScript.ContentMapper.OptionPathSegment>? path)
    {
        if (config?.ConfigFile == null)
            return (null, Core.UndefinedTextRange());
        int index = config.ContentMappers()?.IndexOf(mapper) ?? -1;
        var mapperNode = getContentMapperSyntax(config.ConfigFile.SourceFile, index, "");
        var node = getContentMapperSyntax(config.ConfigFile.SourceFile, index, "options") ?? mapperNode;
        if (path != null)
            foreach (var segment in path)
            {
                Node? next = null;
                if (segment.IsIndex && Ast.IsArrayLiteralExpression(node))
                {
                    var elements = node!.Elements();
                    if (segment.Index < elements.Count)
                        next = elements[segment.Index];
                }
                else if (!segment.IsIndex && Ast.IsObjectLiteralExpression(node))
                    next = ForEachPropertyAssignment(node!.AsObjectLiteralExpression(), segment.Property, property => property.Initializer);
                if (next == null)
                    break;
                node = next;
            }

        return node == null ? (null, Core.UndefinedTextRange()) : (config.ConfigFile.SourceFile, new(global::TypeScript.Scanner.Scanner.SkipTriviaByteString(config.ConfigFile.SourceFile.TextView(), node.Pos()), node.End()));
    }

    internal static bool invalidTrailingRecursion(string spec) => invalidTrailingRecursion(ByteStringValue.FromString(spec));
    internal static bool invalidTrailingRecursion(ByteStringValue spec)
    {
        var value = spec.EndsWith("/"u8) ? spec.Slice(0, spec.Length - 1) : spec;
        return value.EqualsBytes("**"u8) || value.EndsWith("/**"u8);
    }

    internal static bool invalidDotDotAfterRecursiveWildcard(string value) => invalidDotDotAfterRecursiveWildcard(ByteStringValue.FromString(value));
    internal static bool invalidDotDotAfterRecursiveWildcard(ByteStringValue value)
    {
        int wildcard = value.StartsWith("**/"u8) ? 0 : value.IndexOf("/**/"u8);
        if (wildcard == -1)
            return false;
        int lastDot = value.EndsWith("/.."u8) ? value.Length : value.LastIndexOf(ByteStringValue.FromString("/../"));
        return lastDot > wildcard;
    }

    internal static Message? specToDiagnostic(string spec, bool disallowTrailingRecursion) => specToDiagnostic(ByteStringValue.FromString(spec), disallowTrailingRecursion);
    internal static Message? specToDiagnostic(ByteStringValue spec, bool disallowTrailingRecursion) => disallowTrailingRecursion && invalidTrailingRecursion(spec) ? Diagnostics.File_specification_cannot_end_in_a_recursive_directory_wildcard_Asterisk_Asterisk_Colon_0 : invalidDotDotAfterRecursiveWildcard(spec) ? Diagnostics.File_specification_cannot_contain_a_parent_directory_that_appears_after_a_recursive_directory_wildcard_Asterisk_Asterisk_Colon_0 : null;
    internal static (List<PathPattern>? Specs, Slice<Diagnostic> Errors) validateSpecs(object specs, bool disallowTrailingRecursion, SourceFile? sourceFile, string key)
    {
        List<PathPattern>? final = null;
        Slice<Diagnostic> errors = default;
        foreach (var value in (List<object?>)specs)
        {
            if (value is not (string or ByteStringValue))
                continue;
            var spec = parsePathText(value);
            var message = specToDiagnostic(spec, disallowTrailingRecursion);
            if (message != null)
                errors = errors.Append(CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, GetTsConfigPropArrayElementValue(sourceFile, key, spec)?.AsNode(), message, spec));
            else
                (final ??= new()).Add(TsPath.ToPathPattern(spec));
        }

        return (final, errors);
    }

    internal static bool canJsonReportNoInputFiles(OrderedMap<string, object?> raw) => !raw.Has("files") && !raw.Has("references");
    internal static bool shouldReportNoInputFiles(List<RootedFilePath>? files, bool canReport, List<PathKey>? stack) => (files?.Count ?? 0) == 0 && canReport && (stack?.Count ?? 0) == 0;
    internal static bool isStringValue(object? value) => value is string or ByteStringValue;
    internal static (parsedTsconfig Config, Slice<Diagnostic> Errors) parseOwnConfigOfJsonSourceFile(SourceFile sourceFile, IFS fs, RootedDirectoryPath basePath, RootedFilePath configFileName)
    {
        var compilerOptions = getDefaultCompilerOptions(configFileName);
        var acquisition = getDefaultTypeAcquisition(configFileName);
        List<RootedFilePath>? extendedPaths = null;
        List<Node>? rootCompilerOptions = null;
        (object? , Slice<Diagnostic> ) onPropertySet(string key, object? value, PropertyAssignment assignment, CommandLineOption? parent, CommandLineOption? option)
        {
            Slice<Diagnostic> errors = default;
            if (option != null && !ReferenceEquals(option, extendsOptionDeclaration))
                (value, errors) = convertJsonOption(option, value, basePath, assignment, assignment.Initializer, sourceFile);
            if (parent != null && parent.Name != "undefined" && value != null)
            {
                if (option != null && option.Name != "")
                {
                    switch (parent.Name)
                    {
                        case "compilerOptions":
                            compilerOptions.ParseOption(option.Name, value);
                            break;
                        case "typeAcquisition":
                            ParseTypeAcquisition(option.Name, value, acquisition);
                            break;
                    }
                }
                else if (key != "" && extraKeyDiagnostics(parent.Name) != null && parent.ElementOptions != null)
                {
                    var possible = parent.ElementOptions.Get(key) ?? parent.ElementOptions.GetSpellingSuggestion(key);
                    errors = errors.Append(possible != null && possible.Name != key ? CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, assignment.AsNode().Name(), extraKeyDidYouMeanDiagnostics(parent.Name)!, key, possible.Name) : createUnknownOptionError(key, extraKeyDiagnostics(parent.Name)!, "", assignment.AsNode().Name(), sourceFile, null, null, null));
                }
            }
            else if (ReferenceEquals(parent, tsconfigRootOptionsMap))
            {
                if (ReferenceEquals(option, extendsOptionDeclaration))
                {
                    var(paths, pathErrors) = getExtendsConfigPathOrArray(value, fs, basePath, configFileName, assignment, assignment.Initializer, sourceFile);
                    extendedPaths = paths;
                    if (!pathErrors.IsNil)
                        errors = errors.Append(pathErrors);
                }
                else if (option == null)
                {
                    if (key == "excludes")
                        errors = errors.Append(CreateDiagnosticForNodeInSourceFile(sourceFile, assignment.AsNode().Name(), Diagnostics.Unknown_option_excludes_Did_you_mean_exclude));
                    if (Core.Find(optionsForCompiler, option => option.Name == key) != null)
                        (rootCompilerOptions ??= new()).Add(assignment.AsNode().Name());
                }
            }

            return (value, errors);
        }

        var(raw, allErrors) = convertConfigFileToObject(sourceFile, new() { rootOptions = tsconfigRootOptionsMap, onPropertySet = onPropertySet });
        if ((rootCompilerOptions?.Count ?? 0) != 0 && raw is OrderedMap<string, object?> map && !map.Has("compilerOptions"))
            allErrors = allErrors.Append(CreateDiagnosticForNodeInSourceFile(sourceFile, rootCompilerOptions![0], Diagnostics.X_0_should_be_set_inside_the_compilerOptions_object_of_the_config_json_file, Ast.GetTextOfPropertyName(rootCompilerOptions[0])));
        return (new() { raw = raw, options = compilerOptions, typeAcquisition = acquisition, extendedConfigPaths = extendedPaths }, allErrors);
    }

    internal static (TsConfigSourceFile Source, Slice<Diagnostic> Errors) readJsonConfigFile(RootedFilePath fileName, PathKey path, Func<RootedFilePath, (string Text, bool Ok)> readFile)
    {
        var(text, errors) = tryReadFile(fileName, readFile, Slice<Diagnostic>.Make(0));
        if (text != "")
            return (NewTsconfigSourceFileFromFilePath(fileName, path, text), errors);
        var factory = new NodeFactory();
        var source = new TsConfigSourceFile
        {
            SourceFile = factory.NewSourceFile(new SourceFileParseOptions(fileName.AsValue(), path.AsValue(), FileNameIsGoBytes: true), "", factory.NewNodeList(Slice<Node>.Make(0)), factory.NewToken(Kind.EndOfFile)).AsSourceFile()
        };
        source.SourceFile.SetDiagnostics(errors!);
        return (source, errors);
    }

    internal static (parsedTsconfig? Config, Slice<Diagnostic> Errors) getExtendedConfig(TsConfigSourceFile? sourceFile, RootedFilePath fileName, IFS fs, List<PathKey>? stack, IExtendedConfigCache? cache, extendsResult result)
    {
        var path = fs.CaseSensitivity().PathKey(fileName.AsPath());
        var entry = cache != null && stack?.Contains(path) != true ? cache.GetExtendedConfig(fileName, path, stack, fs) : ParseExtendedConfig(fileName, path, stack, fs, cache);
        Slice<Diagnostic> errors = default;
        if (entry.errors.Length > 0) errors = errors.Append(entry.errors);
        if (entry.extendedResult != null && sourceFile != null)
        {
            result.extendedSourceFiles.Add(entry.extendedResult.FileName());
            if (entry.extendedResult.ExtendedSourceFiles != null)
                foreach (var file in entry.extendedResult.ExtendedSourceFiles)
                    result.extendedSourceFiles.Add(file);
        }

        return (entry.extendedConfig, errors);
    }

    public static ExtendedConfigCacheEntry ParseExtendedConfig(RootedFilePath fileName, PathKey path, List<PathKey>? stack, IFS fs, IExtendedConfigCache? cache)
    {
        var(source, errors) = readJsonConfigFile(fileName, path, fs.ReadFile);
        var entry = new ExtendedConfigCacheEntry
        {
            extendedResult = source
        };
        if (errors is { Length: > 0 })
        {
            entry.errors = errors;
            return entry;
        }

        if (source.SourceFile.Diagnostics()is { Length: > 0 } diagnostics)
        {
            entry.errors = diagnostics;
            return entry;
        }

        (entry.extendedConfig, entry.errors) = parseConfig(null, source, fs, fileName.Directory(), fileName, stack, cache);
        return entry;
    }

    internal static (parsedTsconfig Config, Slice<Diagnostic> Errors) parseConfig(OrderedMap<string, object?>? json, TsConfigSourceFile? sourceFile, IFS fs, RootedDirectoryPath basePath, RootedFilePath fileName, List<PathKey>? stack, IExtendedConfigCache? cache)
    {
        var sensitivity = fs.CaseSensitivity();
        var path = sensitivity.PathKey(fileName.AsValue().Length != 0 ? fileName.AsPath() : basePath.AsPath());
        Slice<Diagnostic> errors = default;
        if (stack?.Contains(path) == true)
        {
            errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.Circularity_detected_while_resolving_configuration_Colon_0));
            if ((json?.Size() ?? 0) == 0)
                return (new() { raw = json }, errors);
            var(raw, conversionErrors) = convertToObject(sourceFile!.SourceFile);
            if (!conversionErrors.IsNil)
                errors = errors.Append(conversionErrors);
            return (new() { raw = raw }, errors);
        }

        var(ownConfig, ownErrors) = json != null ? parseOwnConfigOfJson(json, fs, basePath, fileName) : parseOwnConfigOfJsonSourceFile(sourceFile!.SourceFile, fs, basePath, fileName);
        if (!ownErrors.IsNil)
            errors = errors.Append(ownErrors);
        if (ownConfig.options?.CompilerOptions.Paths != null)
            ownConfig.options.CompilerOptions.PathsBasePath = basePath;
        void applyExtendedConfig(extendsResult result, RootedFilePath extendedPath)
        {
            var(extended, extendedErrors) = getExtendedConfig(sourceFile, extendedPath, fs, stack, cache, result);
            if (!extendedErrors.IsNil)
                errors = errors.Append(extendedErrors);
            if (extended?.options == null)
                return;
            var extendedRaw = extended.raw;
            ByteStringValue relativeDifference = ByteStringValue.FromString("");
            void setPropertyValue(string name)
            {
                if (ownConfig.raw is OrderedMap<string, object?> ownMap && ownMap.Has(name))
                    return;
                if (name is not ("include" or "exclude" or "files") || extendedRaw is not OrderedMap<string, object?> extendedMap || !extendedMap.Has(name) || extendedMap.GetOrZero(name)is not List<object?> values)
                    return;
                var converted = Core.Map(values, value =>
                {
                    if (value is not (string or ByteStringValue))
                        return value;
                    var text = parsePathText(value);
                    if (startsWithConfigDirTemplate(text) || TsPath.IsRootedDiskPath(text))
                        return value;
                    if (relativeDifference.Length == 0)
                        relativeDifference = TsPath.ConvertToRelativePath(extendedPath.Directory().AsValue(), basePath, sensitivity);
                    return TsPath.CombinePaths(relativeDifference, text);
                });
                switch (name)
                {
                    case "include":
                        result.include = converted;
                        break;
                    case "exclude":
                        result.exclude = converted;
                        break;
                    case "files":
                        result.files = converted;
                        break;
                }
            }

            setPropertyValue("include");
            setPropertyValue("exclude");
            setPropertyValue("files");
            if (extendedRaw is OrderedMap<string, object?> rawMap)
            {
                if (rawMap.Has("contentMappers"))
                    result.contentMappers = rawMap.GetOrZero("contentMappers") as List<object?>;
                if (rawMap.Has("compileOnSave") && rawMap.GetOrZero("compileOnSave")is bool compileOnSave)
                    result.compileOnSave = compileOnSave;
            }

            mergeParsedCompilerOptions(result.options, extended.options, extendedRaw);
        }

        if ((ownConfig.extendedConfigPaths?.Count ?? 0) != 0)
        {
            // Each recursive branch receives the original slice header value; managed lists copy that header's values.
            stack = stack == null ? new()
            {
                path
            }

            : new(stack)
            {
                path
            };
            var result = new extendsResult
            {
                options = new()
                {
                    CompilerOptions = new(),
                    unresolvedPaths = new()
                }
            };
            foreach (var extendedPath in ownConfig.extendedConfigPaths!)
                applyExtendedConfig(result, extendedPath);
            var ownRaw = (OrderedMap<string, object?>)ownConfig.raw!;
            if (result.include != null)
                ownRaw.Set("include", result.include);
            if (result.exclude != null)
                ownRaw.Set("exclude", result.exclude);
            if (result.files != null)
                ownRaw.Set("files", result.files);
            if (result.contentMappers != null && !ownRaw.Has("contentMappers"))
                ownRaw.Set("contentMappers", result.contentMappers);
            if (result.compileOnSave && !ownRaw.Has("compileOnSave"))
                ownRaw.Set("compileOnSave", result.compileOnSave);
            if (sourceFile != null)
            {
                foreach (var extendedFile in result.extendedSourceFiles)
                {
                    sourceFile.ExtendedSourceFiles ??= new();
                    int index = sourceFile.ExtendedSourceFiles.BinarySearch(extendedFile, Comparer<RootedFilePath>.Create((left, right) => left.AsPath().Compare(right.AsPath())));
                    if (index < 0)
                        sourceFile.ExtendedSourceFiles.Insert(~index, extendedFile);
                }
            }

            ownConfig.options = mergeParsedCompilerOptions(result.options, ownConfig.options, ownConfig.raw);
        }

        return (ownConfig, errors);
    }

    internal static bool isCompilerOptionsValue(CommandLineOption? option, object? value)
    {
        if (option == null)
            return false;
        if (value == null)
            return !option.DisallowNullOrUndefined();
        return option.Kind switch
        {
            CommandLineOptionKind.List => value is System.Collections.IList,
            CommandLineOptionKind.ListOrElement => value is System.Collections.IList || isCompilerOptionsValue(option.Elements(), value),
            CommandLineOptionKind.String or CommandLineOptionKind.Enum => value is string || option.PathKind.IsRooted() && value is ByteStringValue,
            CommandLineOptionKind.Boolean => value is bool,
            CommandLineOptionKind.Number => value is double,
            CommandLineOptionKind.Object => value is OrderedMap<string, object?>,
            _ => false,
        };
    }

    internal static object normalizeNonListOptionValue(CommandLineOption option, RootedDirectoryPath basePath, object value)
    {
        if (!option.PathKind.IsRooted())
            return value;
        var path = TsPath.NormalizeSlashes(parsePathText(value));
        if (!startsWithConfigDirTemplate(path))
            path = TsPath.GetNormalizedAbsolutePath(path, basePath.AsValue());
        return path.Length == 0 ? ByteStringValue.FromString(".") : path;
    }

    internal static (List<object?>? Value, Slice<Diagnostic> Errors) convertJsonOptionOfListType(CommandLineOption option, object? value, RootedDirectoryPath basePath, PropertyAssignment? assignment, Node? expression, SourceFile? sourceFile)
    {
        Slice<Diagnostic> errors = default;
        if (value is not List<object?> values)
            return (null, errors);
        Node? elementExpression = null;
        var mapped = new List<object?>(values.Count);
        for (int index = 0; index < values.Count; index++)
        {
            if (expression != null)
                elementExpression = expression.Elements()[index];
            var(converted, conversionErrors) = convertJsonOption(option.Elements()!, values[index], basePath, assignment, elementExpression, sourceFile);
            if (!conversionErrors.IsNil)
                errors = errors.Append(conversionErrors);
            mapped.Add(converted);
        }

        var filtered = option.listPreserveFalsyValues ? mapped : Core.Filter(mapped, value => value != null && value is not false && value is not 0 && value is not "");
        return (filtered, errors);
    }

    internal static (object? Value, Slice<Diagnostic> Errors) convertJsonOption(CommandLineOption option, object? value, RootedDirectoryPath basePath, PropertyAssignment? assignment, Node? expression, SourceFile? sourceFile)
    {
        if (option.IsCommandLineOnly)
            return (null, Slice<Diagnostic>.FromArray([CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, assignment?.AsNode().Name(), Diagnostics.Option_0_can_only_be_specified_on_command_line, option.Name)]));
        if (!isCompilerOptionsValue(option, value))
            return (null, Slice<Diagnostic>.FromArray([CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, expression, Diagnostics.Compiler_option_0_requires_a_value_of_type_1, option.Name, getCompilerOptionValueTypeString(option))]));
        switch (option.Kind)
        {
            case CommandLineOptionKind.List:
                return convertJsonOptionOfListType(option, value, basePath, assignment, expression, sourceFile);
            case CommandLineOptionKind.ListOrElement:
                // The original reflects the value here, including its nil-value panic.
                if (value == null)
                    throw new InvalidOperationException("reflect: call of reflect.Value.Kind on zero Value");
                return value is System.Collections.IList ? convertJsonOptionOfListType(option, value, basePath, assignment, expression, sourceFile) : convertJsonOption(option.Elements()!, value, basePath, assignment, expression, sourceFile);
            case CommandLineOptionKind.Enum:
                return value == null ? (null, default) : convertJsonOptionOfEnumType(option, (string)value, expression, sourceFile);
        }

        var(validated, errors) = validateJsonOptionValue(option, value, expression, sourceFile);
        return errors.Length > 0 || validated == null ? (validated, errors) : (normalizeNonListOptionValue(option, basePath, validated), errors);
    }

    internal static (T Result, Slice<Diagnostic> Errors) convertOptionsFromJson<T>(CommandLineOptionNameMap names, object? jsonOptions, RootedDirectoryPath basePath, T result)
        where T : IOptionParser
    {
        if (jsonOptions is not OrderedMap<string, object?> map)
            return (result, default);
        Slice<Diagnostic> errors = default;
        foreach (var(key, value)in map.Entries())
        {
            var option = names.Get(key);
            if (option != null && option.Name != key)
            {
                errors = errors.Append(CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(null, null, result.UnknownDidYouMeanDiagnostic(), key, option.Name));
                continue;
            }

            if (option == null)
            {
                errors = errors.Append(createUnknownOptionError(key, result.UnknownOptionDiagnostic(), "", null, null, null, result.UnknownDidYouMeanDiagnostic(), names));
                continue;
            }

            var(converted, conversionErrors) = convertJsonOption(option, value, basePath, null, null, default);
            if (!conversionErrors.IsNil)
                errors = errors.Append(conversionErrors);
            result.ParseOption(key, converted);
        }

        return (result, errors);
    }

    internal static object? normalizeJsonValue(object? value)
    {
        switch (value)
        {
            case OrderedMap<string, object?> ordered:
                foreach (var(key, child)in ordered.Entries())
                    ordered.Set(key, normalizeJsonValue(child));
                return ordered;
            case Dictionary<string, object?> dictionary:
                var result = new OrderedMap<string, object?>(dictionary.Count);
                foreach (var key in dictionary.Keys.Order(StringComparer.Ordinal))
                    result.Set(key, normalizeJsonValue(dictionary[key]));
                return result;
            case System.Collections.IList values:
                var list = new List<object?>(values.Count);
                foreach (var item in values)
                    list.Add(normalizeJsonValue(item));
                return list;
            default:
                return value;
        }
    }

    internal static (object? Value, Slice<Diagnostic> Errors) convertToObject(SourceFile sourceFile) => convertToJson(sourceFile, sourceFile.Statements?.Nodes is { Count: > 0 } statements ? statements[0].Expression() : null, true, default);
    internal static (parsedCompilerOptions Options, Slice<Diagnostic> Errors) convertCompilerOptionsFromJsonWorker(object? jsonOptions, RootedDirectoryPath basePath, RootedFilePath configFileName)
    {
        var options = getDefaultCompilerOptions(configFileName);
        var(_, errors) = convertOptionsFromJson(CommandLineCompilerOptionsMap, jsonOptions, basePath, new compilerOptionsParser { CompilerOptions = options.CompilerOptions, unresolvedPaths = options.unresolvedPaths });
        if (configFileName.AsValue().Length != 0)
            options.CompilerOptions.ConfigFilePath = configFileName;
        return (options, errors);
    }

    internal static (TypeAcquisition Options, Slice<Diagnostic> Errors) convertTypeAcquisitionFromJsonWorker(object? jsonOptions, RootedDirectoryPath basePath, RootedFilePath configFileName)
    {
        var options = getDefaultTypeAcquisition(configFileName);
        var(_, errors) = convertOptionsFromJson(typeAcquisitionDeclaration!.ElementOptions!, jsonOptions, basePath, new typeAcquisitionParser { TypeAcquisition = options });
        return (options, errors);
    }

    internal static (parsedTsconfig Config, Slice<Diagnostic> Errors) parseOwnConfigOfJson(OrderedMap<string, object?> json, IFS fs, RootedDirectoryPath basePath, RootedFilePath configFileName)
    {
        Slice<Diagnostic> errors = default;
        if (json.Has("excludes"))
            errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.Unknown_option_excludes_Did_you_mean_exclude));
        var(options, optionErrors) = convertCompilerOptionsFromJsonWorker(json.GetOrZero("compilerOptions"), basePath, configFileName);
        var(acquisition, acquisitionErrors) = convertTypeAcquisitionFromJsonWorker(json.GetOrZero("typeAcquisition"), basePath, configFileName);
        if (!optionErrors.IsNil)
            errors = errors.Append(optionErrors);
        if (!acquisitionErrors.IsNil)
            errors = errors.Append(acquisitionErrors);
        var(compileOnSave, hasCompileOnSave) = json.Get("compileOnSave");
        if (hasCompileOnSave)
        {
            var(converted, conversionErrors) = convertJsonOption(compileOnSaveCommandLineOption!, compileOnSave, basePath, null, null, default);
            if (!conversionErrors.IsNil)
                errors = errors.Append(conversionErrors);
            json.Set("compileOnSave", converted);
        }

        List<RootedFilePath>? extendedPaths = null;
        var extended = json.GetOrZero("extends");
        if (extended != null && extended is not "")
        {
            var(paths, pathErrors) = getExtendsConfigPathOrArray(extended, fs, basePath, configFileName, null, null, default);
            extendedPaths = paths;
            if (!pathErrors.IsNil)
                errors = errors.Append(pathErrors);
        }

        return (new() { raw = json, options = options, typeAcquisition = acquisition, extendedConfigPaths = extendedPaths }, errors);
    }

    internal static (List<RootedFilePath>? Paths, Slice<Diagnostic> Errors) getExtendsConfigPathOrArray(object? value, IFS fs, RootedDirectoryPath basePath, RootedFilePath fileName, PropertyAssignment? assignment, Node? expression, SourceFile? sourceFile)
    {
        List<RootedFilePath>? paths = null;
        var newBase = fileName.AsValue().Length != 0 ? fileName.Directory() : basePath;
        if (value == null)
        {
            var(_, errors) = convertJsonOption(extendsOptionDeclaration!, value, basePath, assignment, expression, sourceFile);
            return (paths, errors);
        }

        if (value is string path)
        {
            var(resolved, errors) = getExtendsConfigPath(path, fs, newBase, expression, sourceFile);
            if (resolved.AsValue().Length != 0)
                (paths ??= new()).Add(resolved);
            return (paths, errors);
        }

        Slice<Diagnostic> allErrors = default;
        if (value is List<object?> values)
        {
            for (int index = 0; index < values.Count; index++)
            {
                var element = expression?.Elements()[index];
                Slice<Diagnostic> errors;
                if (values[index] is string text)
                {
                    var(resolved, resolutionErrors) = getExtendsConfigPath(text, fs, newBase, element, sourceFile);
                    if (resolved.AsValue().Length != 0)
                        (paths ??= new()).Add(resolved);
                    errors = resolutionErrors;
                }
                else
                    (_, errors) = convertJsonOption(extendsOptionDeclaration!.Elements()!, value, basePath, assignment, element, sourceFile);
                if (!errors.IsNil)
                    allErrors = allErrors.Append(errors);
            }
        }
        else
            (_, allErrors) = convertJsonOption(extendsOptionDeclaration!, value, basePath, assignment, expression, sourceFile);
        return (paths, allErrors);
    }

    internal static (RootedFilePath Path, Slice<Diagnostic> Errors) getExtendsConfigPath(string extendedConfig, IFS fs, RootedDirectoryPath basePath, Node? expression, SourceFile? sourceFile)
    {
        extendedConfig = TsPath.NormalizeSlashes(extendedConfig);
        if (TsPath.IsRootedDiskPath(extendedConfig) || extendedConfig.StartsWith("./", StringComparison.Ordinal) || extendedConfig.StartsWith("../", StringComparison.Ordinal))
        {
            var path = TsPath.ToRootedFilePath(extendedConfig, basePath);
            if (!fs.FileExists(path) && !path.ExtensionIs(TsPath.ExtensionJson))
            {
                path = path.AppendSuffix(TsPath.ExtensionJson);
                if (!fs.FileExists(path))
                    return (default, Slice<Diagnostic>.FromArray([CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, expression, Diagnostics.File_0_not_found, extendedConfig)]));
            }

            return (path, default);
        }

        var resolved = TypeScript.Modules.DefaultResolver.ResolveConfig(extendedConfig, basePath.ResolveFile("tsconfig.json"), fs);
        if (resolved.IsResolved())
            return (resolved.ResolvedFileName, default);
        return (default, Slice<Diagnostic>.FromArray([CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, expression, extendedConfig == "" ? Diagnostics.Compiler_option_0_cannot_be_given_an_empty_string : Diagnostics.File_0_not_found, extendedConfig == "" ? "extends" : extendedConfig)]));
    }

    internal static bool hasFileWithHigherPriorityExtension(RootedFilePath file, List<List<string>> extensions, Func<RootedFilePath, bool> hasFile)
    {
        List<string>? group = null;
        foreach (var items in extensions)
            if (file.ExtensionIsOneOf(items))
                (group ??= new()).AddRange(items);
        if ((group?.Count ?? 0) == 0)
            return false;
        foreach (var extension in group!)
        {
            if (file.ExtensionIs(extension) && (extension != TsPath.ExtensionTs || !file.ExtensionIs(TsPath.ExtensionDts)))
                return false;
            if (hasFile(file.ChangeExtension(extension)))
            {
                if (extension == TsPath.ExtensionDts && (file.ExtensionIs(TsPath.ExtensionJs) || file.ExtensionIs(TsPath.ExtensionJsx)))
                    continue;
                return true;
            }
        }

        return false;
    }

    internal static void removeWildcardFilesWithLowerPriorityExtension<T>(RootedFilePath file, OrderedMap<PathKey, T> wildcardFiles, List<List<string>> extensions, Func<RootedFilePath, PathKey> keyMapper)
    {
        List<string>? group = null;
        foreach (var items in extensions)
            if (file.ExtensionIsOneOf(items))
                (group ??= new()).AddRange(items);
        if (group == null)
            return;
        for (int i = group.Count - 1; i >= 0; i--)
        {
            if (file.ExtensionIs(group[i]))
                return;
            wildcardFiles.Delete(keyMapper(file.ChangeExtension(group[i])));
        }
    }

    internal static (List<RootedFilePath> Files, int LiteralCount) getFileNamesFromConfigSpecs(configFileSpecs specs, RootedDirectoryPath basePath, CompilerOptions options, IFS host, List<string>? extraExtensions)
    {
        PathKey keyMapper(RootedFilePath file) => host.CaseSensitivity().PathKey(file.AsPath());
        var literalFiles = new OrderedMap<PathKey, RootedFilePath>();
        var wildcardFiles = new OrderedMap<PathKey, RootedFilePath>();
        var wildcardJsonFiles = new OrderedMap<PathKey, RootedFilePath>();
        var extensions = GetSupportedExtensions(options, extraExtensions);
        var extensionsWithJson = GetSupportedExtensionsWithJsonIfResolveJsonModule(options, extensions);
        for (int i = 0; i < (specs.validatedFilesSpec?.Count ?? 0); i++)
            literalFiles.Set(keyMapper(specs.validatedFileNames![i]), specs.validatedFileNames[i]);
        SpecMatcher? jsonMatchers = null;
        if ((specs.validatedIncludeSpecs?.Count ?? 0) > 0)
        {
            var files = VfsMatch.ReadDirectory(host, basePath, extensionsWithJson.SelectMany(group => group).ToList(), specs.validatedExcludeSpecs, specs.validatedIncludeSpecs, VfsMatch.UnlimitedDepth);
            foreach (var file in files)
                {
                    if (file.ExtensionIs(TsPath.ExtensionJson))
                    {
                        if (jsonMatchers == null)
                        {
                            var includes = Core.Filter(specs.validatedIncludeSpecs, include => include.AsValue().EndsWith(".json"u8));
                            jsonMatchers = VfsMatch.NewSpecMatcher(includes, basePath, Usage.Files, host.CaseSensitivity());
                        }

                        int index = jsonMatchers?.MatchFileNameIndex(file) ?? -1;
                        if (index != -1)
                        {
                            var key = keyMapper(file);
                            if (!literalFiles.Has(key) && !wildcardJsonFiles.Has(key))
                                wildcardJsonFiles.Set(key, file);
                        }

                        continue;
                    }

                    if (hasFileWithHigherPriorityExtension(file, extensions, name => literalFiles.Has(keyMapper(name)) || wildcardFiles.Has(keyMapper(name))))
                        continue;
                    removeWildcardFilesWithLowerPriorityExtension(file, wildcardFiles, extensions, keyMapper);
                    var wildcardKey = keyMapper(file);
                    if (!literalFiles.Has(wildcardKey) && !wildcardFiles.Has(wildcardKey))
                        wildcardFiles.Set(wildcardKey, file);
                }
        }

        var result = new List<RootedFilePath>(literalFiles.Size() + wildcardFiles.Size() + wildcardJsonFiles.Size());
        result.AddRange(literalFiles.Values());
        result.AddRange(wildcardFiles.Values());
        result.AddRange(wildcardJsonFiles.Values());
        return (result, literalFiles.Size());
    }

    public static List<List<string>> GetSupportedExtensions(CompilerOptions options, List<string>? extraExtensions)
    {
        var builtins = options.GetAllowJS() ? TsPath.AllSupportedExtensions : TsPath.SupportedTSExtensions;
        if ((extraExtensions?.Count ?? 0) == 0)
            return builtins;
        var flat = builtins.SelectMany(group => group).ToList();
        List<List<string>>? result = null;
        foreach (var extension in extraExtensions!)
            if (!flat.Contains(extension))
                (result ??= new()).Add(new() { extension });
        return (result?.Count ?? 0) == 0 ? builtins : builtins.Concat(result!).ToList();
    }

    public static List<List<string>> GetSupportedExtensionsWithJsonIfResolveJsonModule(CompilerOptions? options, List<List<string>> extensions)
    {
        if (options == null || !options.GetResolveJsonModule())
            return extensions;
        if (ReferenceEquals(extensions, TsPath.AllSupportedExtensions))
            return TsPath.AllSupportedExtensionsWithJson;
        if (ReferenceEquals(extensions, TsPath.SupportedTSExtensions))
            return TsPath.SupportedTSExtensionsWithJson;
        return extensions.Concat(new[] { new List<string> { TsPath.ExtensionJson } }).ToList();
    }

    internal static SourceFile? tsconfigToSourceFile(TsConfigSourceFile? source) => source?.SourceFile;
    public static TsConfigSourceFile NewTsconfigSourceFileFromFilePath(RootedFilePath fileName, PathKey path, string text) => new()
    {
        SourceFile = Parser.ParseSourceFile(new SourceFileParseOptions(fileName.AsValue(), path.AsValue(), FileNameIsGoBytes: true), GoString.Bytes(text), ScriptKind.JSON),
    };
    public static (object? Value, Slice<Diagnostic> Errors) ParseConfigFileTextToJson(RootedFilePath fileName, PathKey path, string text)
    {
        var source = NewTsconfigSourceFileFromFilePath(fileName, path, text).SourceFile;
        var(value, errors) = convertConfigFileToObject(source, default);
        if (source.Diagnostics()is { Length: > 0 } diagnostics)
            errors = Slice<Diagnostic>.FromArray([diagnostics[0]]);
        return (value, errors);
    }

    internal static (object? Value, Slice<Diagnostic> Errors) convertConfigFileToObject(SourceFile sourceFile, jsonConversionNotifier? notifier)
    {
        var root = sourceFile.Statements?.Nodes is { Count: > 0 } statements ? statements[0].Expression() : null;
        if (root != null && root.Kind != Kind.ObjectLiteralExpression)
        {
            string name = TsPath.GetBaseFileName(sourceFile.FileName()) == "jsconfig.json" ? "jsconfig.json" : "tsconfig.json";
            var errors = Slice<Diagnostic>.FromArray([
                CreateDiagnosticForNodeInSourceFile(sourceFile, root, Diagnostics.The_root_value_of_a_0_file_must_be_an_object, name)
            ]);
            if (Ast.IsArrayLiteralExpression(root))
            {
                var firstObject = Core.Find(root.Elements(), Ast.IsObjectLiteralExpression);
                if (firstObject != null)
                    return convertToJson(sourceFile, firstObject, true, notifier);
            }

            return (new OrderedMap<string, object?>(), errors);
        }

        return convertToJson(sourceFile, root, true, notifier);
    }

    internal static (object? Value, Slice<Diagnostic> Errors) convertToJson(SourceFile sourceFile, Node? root, bool returnValue, jsonConversionNotifier? notifier)
    {
        if (root == null)
            return (returnValue ? new EmptyConfigurationObject() : null, default);
        return convertPropertyValueToJson(sourceFile, root, notifier?.rootOptions, returnValue, notifier);
    }

    internal static bool isDoubleQuotedString(Node node) => Ast.IsStringLiteral(node);
    internal static (object? Value, Slice<Diagnostic> Errors) convertPropertyValueToJson(SourceFile sourceFile, Node valueExpression, CommandLineOption? option, bool returnValue, jsonConversionNotifier? notifier)
    {
        switch (valueExpression.Kind)
        {
            case Kind.TrueKeyword:
                return (true, default);
            case Kind.FalseKeyword:
                return (false, default);
            case Kind.NullKeyword:
                return (null, default);
            case Kind.StringLiteral:
                return isDoubleQuotedString(valueExpression) ? (valueExpression.Text(), default) : (valueExpression.Text(), Slice<Diagnostic>.FromArray([Ast.NewDiagnostic(sourceFile, valueExpression.Loc, Diagnostics.String_literal_with_double_quotes_expected)]));
            case Kind.NumericLiteral:
                return (JsNum.FromString(valueExpression.TextValue()).Value, default);
            case Kind.PrefixUnaryExpression:
                var unary = valueExpression.AsPrefixUnaryExpression();
                if (unary.Operator != Kind.MinusToken || unary.Operand.Kind != Kind.NumericLiteral)
                    break;
                return (-JsNum.FromString(unary.Operand.TextValue()).Value, default);
            case Kind.ObjectLiteralExpression:
                var(objectValue, objectErrors) = convertObjectLiteralExpressionToJson(sourceFile, returnValue, valueExpression.AsObjectLiteralExpression(), option, notifier);
                return (objectValue, objectErrors);
            case Kind.ArrayLiteralExpression:
                return convertArrayLiteralExpressionToJson(sourceFile, valueExpression.Elements(), option, returnValue);
        }

        return (null, Slice<Diagnostic>.FromArray([option != null ? Ast.NewDiagnostic(sourceFile, valueExpression.Loc, Diagnostics.Compiler_option_0_requires_a_value_of_type_1, option.Name, getCompilerOptionValueTypeString(option)) : Ast.NewDiagnostic(sourceFile, valueExpression.Loc, Diagnostics.Property_value_can_only_be_string_literal_numeric_literal_true_false_null_object_literal_or_array_literal)]));
    }

    internal static (OrderedMap<string, object?>? Value, Slice<Diagnostic> Errors) convertObjectLiteralExpressionToJson(SourceFile sourceFile, bool returnValue, ObjectLiteralExpression node, CommandLineOption? objectOption, jsonConversionNotifier? notifier)
    {
        OrderedMap<string, object?>? result = returnValue ? new() : null;
        Slice<Diagnostic> errors = default;
        if (node.Properties is not null)
            foreach (var element in node.Properties.Nodes)
            {
                if (element.Kind != Kind.PropertyAssignment)
                {
                    errors = errors.Append(Ast.NewDiagnostic(sourceFile, element.Loc, Diagnostics.Property_assignment_expected));
                    continue;
                }

                if (element.QuestionToken()is { } token)
                    errors = errors.Append(Ast.NewDiagnostic(sourceFile, token.Loc, Diagnostics.The_0_modifier_can_only_be_used_in_TypeScript_files, "?"));
                string key = "";
                if (!Ast.IsComputedNonLiteralName(element.Name()))
                    (key, _) = Ast.TryGetTextOfPropertyName(element.Name());
                CommandLineOption? option = null;
                if (key != "" && objectOption?.ElementOptions != null)
                {
                    option = objectOption.ElementOptions.Get(key);
                    if (option != null && option.Name != key)
                        option = null;
                }

                var assignment = element.AsPropertyAssignment();
                var(value, conversionErrors) = convertPropertyValueToJson(sourceFile, assignment.Initializer, option, returnValue, notifier);
                if (!conversionErrors.IsNil)
                    errors = errors.Append(conversionErrors);
                if (key != "")
                {
                    if (returnValue)
                        result!.Set(key, value);
                    if (notifier != null)
                    {
                        var(_, notifiedErrors) = notifier.onPropertySet(key, value, assignment, objectOption, option);
                        if (!notifiedErrors.IsNil)
                            errors = errors.Append(notifiedErrors);
                    }
                }
            }

        return (result, errors);
    }

    internal static (object? Value, Slice<Diagnostic> Errors) convertArrayLiteralExpressionToJson(SourceFile sourceFile, Slice<Node> elements, CommandLineOption? elementOption, bool returnValue)
    {
        if (!returnValue)
        {
            if (!elements.IsNil)
                foreach (var element in elements)
                    convertPropertyValueToJson(sourceFile, element, elementOption, returnValue, default);
            return (null, default);
        }

        if (elements.Length == 0)
            return (new List<object?>(), default);
        Slice<Diagnostic> errors = default;
        List<object?>? values = null;
        foreach (var element in elements)
        {
            var(value, conversionErrors) = convertPropertyValueToJson(sourceFile, element, elementOption, returnValue, default);
            if (!conversionErrors.IsNil)
                errors = errors.Append(conversionErrors);
            if (value != null)
                (values ??= new()).Add(value);
        }

        return (values, errors);
    }

    internal static (object? Value, Slice<Diagnostic> Errors) validateJsonOptionValue(CommandLineOption option, object? value, Node? valueExpression, SourceFile? sourceFile)
    {
        if (value == null)
            return (null, default);
        Slice<Diagnostic> errors = default;
        if (option.extraValidation == ExtraValidation.Locale && !TypeScript.Locale.Locales.Parse((string)value).Ok)
            errors = errors.Append(CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, valueExpression, Diagnostics.Locale_must_be_an_IETF_BCP_47_language_tag_Examples_Colon_0_1, "en", "ja-jp"));
        return errors is { Length: > 0 } ? (null, errors) : (value, errors);
    }

    internal static T convertMapToOptions<T>(OrderedMap<string, object?>? options, T result)
        where T : IOptionParser
    {
        if (options != null)
            foreach (var(key, value)in options.Entries())
                result.ParseOption(key, value);
        return result;
    }

    internal static CommandLineOptionNameMap commandLineOptionsToMap(List<CommandLineOption> options)
    {
        var result = new CommandLineOptionNameMap();
        foreach (var option in options)
        {
            result[option.Name] = option;
            result[goLower(option.Name)] = option;
        }

        return result;
    }

    internal const string configDirTemplate = "${configDir}";
    internal static bool startsWithConfigDirTemplate(string value) => startsWithConfigDirTemplate(ByteStringValue.FromString(value));
    internal static bool startsWithConfigDirTemplate(ByteStringValue value) => GoStrings.ToLower(value).StartsWith("${configdir}"u8);
    internal static string getSubstitutedPathWithConfigDirTemplate(string value, RootedDirectoryPath basePath) => getSubstitutedPathWithConfigDirTemplate(ByteStringValue.FromString(value), basePath).Materialize()!;
    internal static ByteStringValue getSubstitutedPathWithConfigDirTemplate(ByteStringValue value, RootedDirectoryPath basePath)
    {
        // Preserve first-match, case-sensitive replacement after prefix validation.
        int index = value.IndexOf("${configDir}"u8);
        if (index >= 0)
        {
            var builder = new GoBuilder();
            builder.Grow(value.Length - configDirTemplate.Length + 2);
            builder.WriteString(value.Slice(0, index));
            builder.WriteString("./");
            builder.WriteString(value.Slice(index + configDirTemplate.Length));
            value = builder.Value();
        }
        return TsPath.GetNormalizedAbsolutePath(value, basePath.AsValue());
    }

    internal static List<string>? getSubstitutedStringArrayWithConfigDirTemplate(List<string>? list, RootedDirectoryPath basePath)
    {
        List<string>? result = null;
        if (list != null)
            for (int i = 0; i < list.Count; i++)
            {
                if (!startsWithConfigDirTemplate(list[i]))
                    continue;
                result ??= new(list);
                result[i] = getSubstitutedPathWithConfigDirTemplate(list[i], basePath);
            }

        return result;
    }
}

internal sealed class parsedCompilerOptions : IOptionParser
{
    internal CompilerOptions CompilerOptions = new();
    internal Dictionary<string, object?>? unresolvedPaths;
    public void ParseOption(string key, object? value) => new compilerOptionsParser
    {
        CompilerOptions = CompilerOptions,
        unresolvedPaths = unresolvedPaths
    }.ParseOption(key, value);
    public Message UnknownOptionDiagnostic() => TsOptions.extraKeyDiagnostics("compilerOptions")!;
    public Message UnknownDidYouMeanDiagnostic() => TsOptions.extraKeyDidYouMeanDiagnostics("compilerOptions")!;
}

internal readonly record struct EmptyConfigurationObject;
internal sealed class parsedTsconfig
{
    internal object? raw;
    internal parsedCompilerOptions? options;
    internal TypeAcquisition? typeAcquisition;
    internal List<RootedFilePath>? extendedConfigPaths;
}

internal sealed class extendsResult
{
    internal parsedCompilerOptions? options;
    internal List<object?>? include, exclude, files, contentMappers;
    internal bool compileOnSave;
    internal HashSet<RootedFilePath> extendedSourceFiles = new();
}

public interface IExtendedConfigCache
{
    ExtendedConfigCacheEntry GetExtendedConfig(RootedFilePath fileName, PathKey path, List<PathKey>? resolutionStack, IFS fs);
}

public sealed class ExtendedConfigCacheEntry
{
    internal TsConfigSourceFile? extendedResult;
    internal parsedTsconfig? extendedConfig;
    internal Slice<Diagnostic> errors;
    public List<RootedFilePath>? ExtendedFileNames() => extendedResult?.ExtendedSourceFiles;
}

public sealed class TsConfigSourceFile
{
    public List<RootedFilePath>? ExtendedSourceFiles;
    internal configFileSpecs? configFileSpecs;
    public SourceFile SourceFile = null!;
    public RootedFilePath FileName() => RootedFilePath.FromValue(SourceFile.FileNameGoValue());
    public PathKey Path() => PathKey.FromValue(SourceFile.PathKeyValue());
}

internal sealed class jsonConversionNotifier
{
    internal CommandLineOption? rootOptions;
    internal Func<string, object?, PropertyAssignment, CommandLineOption?, CommandLineOption?, (object? Value, Slice<Diagnostic> Errors)> onPropertySet = null!;
}

internal sealed class configFileSpecs
{
    internal object? filesSpecs, includeSpecs, excludeSpecs;
    internal List<FileSpec>? validatedFilesSpec;
    internal List<RootedFilePath>? validatedFileNames;
    internal Dictionary<PathKey, FileSpec>? fileSpecByPath;
    internal List<PathPattern>? validatedIncludeSpecs, validatedExcludeSpecs;
    internal List<FileSpec>? validatedFilesSpecBeforeSubstitution;
    internal List<PathPattern>? validatedIncludeSpecsBeforeSubstitution;
    internal bool isDefaultIncludeSpec;
    internal PathPattern getMatchedIncludeSpec(RootedFilePath fileName, RootedDirectoryPath currentDirectory, CaseSensitivity caseSensitivity)
    {
        if ((validatedIncludeSpecs?.Count ?? 0) == 0)
            return default;
        for (int index = 0; index < validatedIncludeSpecs!.Count; index++)
        {
            var matcher = VfsMatch.NewSpecMatcher(new List<PathPattern> { validatedIncludeSpecs[index] }, currentDirectory, Usage.Files, caseSensitivity);
            if (matcher?.MatchFileName(fileName) == true)
                return validatedIncludeSpecsBeforeSubstitution![index];
        }

        return default;
    }

    internal FileSpec getMatchedFileSpec(PathKey path) => fileSpecByPath != null && fileSpecByPath.TryGetValue(path, out var value) ? value : default;
}
