// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// TestProgramSharedData: finite reflection over ACTUAL managed fields. Constant
// GetFields roots retain native metadata; unknown host/delegate/object fields fail.
using System.Reflection;
using System.Runtime.CompilerServices;
using Configuration.Checks;
using TypeScript.Collections;
using TypeScript.Compiler;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;
using TypeScript.Symlinks;
using TypeScript.TsOptions;
using CompilerProgram = TypeScript.Compiler.Program;

namespace Program.Checks;
internal static class ProgramDataChecks
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private static readonly Dictionary<Type, FieldInfo[]> represented = new()
    {
        [typeof(CompilerProgram)] = typeof(CompilerProgram).GetFields(Fields),
        [typeof(ProgramConfig)] = typeof(ProgramConfig).GetFields(Fields),
        [typeof(ProgramOptions)] = typeof(ProgramOptions).GetFields(Fields),
        [typeof(ProgramHosts)] = typeof(ProgramHosts).GetFields(Fields),
        [typeof(ProgramFactories)] = typeof(ProgramFactories).GetFields(Fields),
        [typeof(ResolutionData)] = typeof(ResolutionData).GetFields(Fields),
        [typeof(CompilerOptions)] = typeof(CompilerOptions).GetFields(Fields),
        [typeof(InfoCache)] = typeof(InfoCache).GetFields(Fields),
        [typeof(InfoCacheEntry)] = typeof(InfoCacheEntry).GetFields(Fields),
        [typeof(PackageDirectory)] = typeof(PackageDirectory).GetFields(Fields),
        [typeof(RootedPath)] = typeof(RootedPath).GetFields(Fields),
        [typeof(RootedFilePath)] = typeof(RootedFilePath).GetFields(Fields),
        [typeof(RootedDirectoryPath)] = typeof(RootedDirectoryPath).GetFields(Fields),
        [typeof(PathKey)] = typeof(PathKey).GetFields(Fields),
        [typeof(SourceMapLocation)] = typeof(SourceMapLocation).GetFields(Fields),
        [typeof(PluginImport)] = typeof(PluginImport).GetFields(Fields),
        [typeof(ProcessedFiles)] = typeof(ProcessedFiles).GetFields(Fields),
        [typeof(DuplicateSourceFile)] = typeof(DuplicateSourceFile).GetFields(Fields),
        [typeof(LibFile)] = typeof(LibFile).GetFields(Fields),
        [typeof(RedirectsFile)] = typeof(RedirectsFile).GetFields(Fields),
        [typeof(JsxRuntimeImportSpecifier)] = typeof(JsxRuntimeImportSpecifier).GetFields(Fields),
        [typeof(ProjectReferenceFileMapper)] = typeof(ProjectReferenceFileMapper).GetFields(Fields),
        [typeof(ProjectReferenceFileMapperBuilder)] = typeof(ProjectReferenceFileMapperBuilder).GetFields(Fields),
        [typeof(SourceOutputAndProjectReference)] = typeof(SourceOutputAndProjectReference).GetFields(Fields),
        [typeof(SourceFileParseOptions)] = typeof(SourceFileParseOptions).GetFields(Fields),
        [typeof(ExternalModuleIndicatorOptions)] = typeof(ExternalModuleIndicatorOptions).GetFields(Fields),
        [typeof(SourceFileMetaData)] = typeof(SourceFileMetaData).GetFields(Fields),
        [typeof(KnownSymlinks)] = typeof(KnownSymlinks).GetFields(Fields),
        [typeof(KnownDirectoryLink)] = typeof(KnownDirectoryLink).GetFields(Fields),
        [typeof(PackageNamesInfo)] = typeof(PackageNamesInfo).GetFields(Fields),
        [typeof(FileIncludeData)] = typeof(FileIncludeData).GetFields(Fields),
        [typeof(FileIncludeReason)] = typeof(FileIncludeReason).GetFields(Fields),
        [typeof(ReferenceFileLocation)] = typeof(ReferenceFileLocation).GetFields(Fields),
        [typeof(ObjectLiteralExpression)] = typeof(ObjectLiteralExpression).GetFields(Fields),
        [typeof(NodeData)] = typeof(NodeData).GetFields(Fields),
        [typeof(NodeList)] = typeof(NodeList).GetFields(Fields),
        [typeof(Symbol)] = typeof(Symbol).GetFields(Fields),
        [typeof(SymbolTable)] = typeof(SymbolTable).GetFields(Fields),
        [typeof(ProcessingDiagnostic)] = typeof(ProcessingDiagnostic).GetFields(Fields),
        [typeof(IncludeProcessor)] = typeof(IncludeProcessor).GetFields(Fields),
        [typeof(ProjectConfiguration)] = typeof(ProjectConfiguration).GetFields(Fields),
        [typeof(DiagnosticsCollection)] = typeof(DiagnosticsCollection).GetFields(Fields),
        [typeof(DiagnosticsCollection).GetNestedType("diagnosticLocationKey", BindingFlags.NonPublic)!] = typeof(DiagnosticsCollection).GetNestedType("diagnosticLocationKey", BindingFlags.NonPublic)!.GetFields(Fields),
        [typeof(FileReference)] = typeof(FileReference).GetFields(Fields),
        [typeof(Message)] = typeof(Message).GetFields(Fields),
        [typeof(ResolvedModule)] = typeof(ResolvedModule).GetFields(Fields),
        [typeof(ResolvedTypeReferenceDirective)] = typeof(ResolvedTypeReferenceDirective).GetFields(Fields),
        [typeof(PackageId)] = typeof(PackageId).GetFields(Fields),
        [typeof(ModeAwareCacheKey)] = typeof(ModeAwareCacheKey).GetFields(Fields),
        [typeof(TextRange)] = typeof(TextRange).GetFields(Fields),
        [typeof(ProgramLazyValue<>)] = typeof(ProgramLazyValue<>).GetFields(Fields),
        [typeof(OrderedMap<, >)] = typeof(OrderedMap<, >).GetFields(Fields),
        [typeof(ModeAwareCache<>)] = typeof(ModeAwareCache<>).GetFields(Fields),
        [typeof(SyncMap<, >)] = typeof(SyncMap<, >).GetFields(Fields),
        [typeof(SyncSet<>)] = typeof(SyncSet<>).GetFields(Fields),
    };
    internal static void Run()
    {
        HashSet<Type> leaves = [typeof(Node), typeof(SourceFile), typeof(Diagnostic), typeof(ParsedCommandLine), typeof(PackageJson)];
        foreach (Type type in new[]
        {
            typeof(ProgramConfig),
            typeof(ResolutionData),
            typeof(ProcessedFiles),
            typeof(ProgramLazyValue<HashSet<string>>),
            typeof(ProgramLazyValue<KnownSymlinks>),
            typeof(ProgramLazyValue<PackageNamesInfo>)
        }

        )
            Check.True(DataOnly(type, leaves), "original shared data " + type);
        foreach (Type type in new[]
        {
            typeof(ProgramOptions),
            typeof(ProgramHosts),
            typeof(ProgramFactories),
            typeof(DefaultResolver),
            typeof(FileLoader),
            typeof(ProjectReferenceFileMapperBuilder),
            typeof(Action),
            typeof(object),
            typeof(System.Threading.Channels.Channel<int>),
            typeof(SyncMap<string, Action>),
            typeof(Dictionary<string, List<Owner>>)
        }

        )
            Check.True(!DataOnly(type, leaves), "shared data must not retain hosts: " + type);
        var retained = new HashSet<Type>(leaves)
        {
            typeof(ICompilerHost),
            typeof(TypeScript.Tracing.Tracing),
            typeof(ICheckerPool),
            typeof(CheckerPool),
            typeof(Exception)
        };
        Check.True(DataOnly(typeof(CompilerProgram), retained), "original retained Program data");
        Check.True(!DataOnly(typeof(ProgramOptions), retained), "retained ProgramOptions still has factories");
        Check.True(!DataOnly(typeof(ProgramFactories), retained), "retained factories still rejected");
        foreach (FieldInfo field in represented[typeof(ProgramFactories)])
            Check.True(typeof(Delegate).IsAssignableFrom(field.FieldType), "factory field must be function " + field.Name);
    }

    private readonly record struct Owner(object owner);
    private static bool DataOnly(Type root, HashSet<Type> leaves)
    {
        var seen = new HashSet<Type>(leaves);
        var pending = new Queue<Type>();
        pending.Enqueue(root);
        while (pending.TryDequeue(out Type? type))
        {
            if (!seen.Add(type) || type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(UInt128) || type.IsGenericParameter)
                continue;
            if (type.IsArray)
            {
                pending.Enqueue(type.GetElementType()!);
                continue;
            }

            Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (type.IsGenericType)
                foreach (Type argument in type.GetGenericArguments())
                    pending.Enqueue(argument);
            // These are explicit slice/map/pointer/tuple representation adapters,
            // never generic permission for arbitrary interfaces or host objects.
            if (definition == typeof(List<>) || definition == typeof(Dictionary<, >) || definition == typeof(HashSet<>) || definition == typeof(Nullable<>) || definition == typeof(StrongBox<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(KeyValuePair<, >) || definition == typeof(ValueTuple<, >) || definition == typeof(ValueTuple<,, >) || definition == typeof(ValueTuple<,,, >))
                continue;
            if (type.IsInterface || type.IsPointer || typeof(Delegate).IsAssignableFrom(type))
                return false;
            if (!represented.TryGetValue(definition, out FieldInfo[]? fields))
                return false;
            if (type.BaseType is { } baseType && baseType != typeof(object) && baseType != typeof(ValueType))
                pending.Enqueue(baseType);
            foreach (FieldInfo field in fields)
            {
                // Exact lock fields replace Go mutex/Once storage. A new object
                // field anywhere else is not automatically accepted.
                if (field.FieldType == typeof(object) && (definition == typeof(InfoCache) && field.Name == "gate" || definition == typeof(SyncMap<, >) && field.Name == "gate" || definition == typeof(ProgramLazyValue<>) && field.Name == "gate" || definition == typeof(IncludeProcessor) && field.Name is "diagnosticsGate" or "compilerOptionsSyntaxGate" || definition == typeof(DiagnosticsCollection) && field.Name == "mu" || definition == typeof(CompilerProgram) && field.Name is "commonSourceDirectoryGate" or "sourceFilesToEmitGate" or "packagesMapGate" or "hasTSFileGate"))
                    continue;
                // Existing graph observation associations are authored payloads,
                // absent from the upstream processedFiles data domain. Their
                // schema/value comparisons remain separate accepted obligations.
                if (definition == typeof(ProcessedFiles) && (field.Name == "resolutionEdges" && field.FieldType == typeof(List<ProjectResolutionEdge>) || field.Name == "graphEdges" && field.FieldType == typeof(List<ResolvedProjectEdge>) || field.Name == "pathReferences" && field.FieldType == typeof(List<ResolvedProjectReferenceEdge>) || field.Name == "typeDirectives" && field.FieldType == typeof(List<ResolvedProjectTypeDirective>)))
                    continue;
                pending.Enqueue(field.FieldType);
            }
        }

        return true;
    }
}
