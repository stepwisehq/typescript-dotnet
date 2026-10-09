// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/contentmappers.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.Vfs;

namespace TypeScript.TsOptions;
public static partial class TsOptions
{
    internal sealed class contentMapperResolutionHost
    {
        internal IFS fs = null!;
        internal RootedDirectoryPath currentDirectory;
        internal IFS FS() => fs;
        internal RootedDirectoryPath GetCurrentDirectory() => currentDirectory;
    }

    internal static (Manifest Manifest, RootedDirectoryPath Directory, Diagnostic? Diagnostic) resolveContentMapperManifest(IFS fs, RootedFilePath containingFile, string packageName)
    {
        var host = new contentMapperResolutionHost
        {
            fs = fs,
            currentDirectory = containingFile.Directory()
        };
        var resolver = new TypeScript.Modules.DefaultResolver(host.FS(), host.GetCurrentDirectory(), new CompilerOptions { ModuleResolution = ModuleResolutionKind.Bundler });
        var resolved = resolver.ResolvePackageDirectory(packageName, containingFile, ResolutionMode.None);
        if (resolved == null || resolved.ResolvedFileName == default)
            return (new(), default, Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_package_0_could_not_be_resolved, packageName));
        var directory = TsPath.RootedDirectoryPathFromPath(resolved.ResolvedFileName.AsPath());
        var(contents, read) = fs.ReadFile(directory.ResolveFile("package.json"));
        if (!read)
            return (new(), directory, Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_package_0_could_not_be_resolved, packageName));
        var(fields, error) = TypeScript.PackageJson.PackageJsonParser.ParseFields(GoString.Bytes(contents));
        if (error != null)
            return (new(), directory, Ast.NewCompilerDiagnostic(Diagnostics.The_package_json_of_the_content_mapper_package_0_could_not_be_parsed, packageName));
        var(name, _) = fields.Name.GetValue();
        if (name == "")
            return (new(), directory, Ast.NewCompilerDiagnostic(Diagnostics.The_package_json_of_the_content_mapper_package_0_does_not_specify_a_name, packageName));
        var(version, _) = fields.Version.GetValue();
        var(mapper, hasMapper) = fields.ContentMapper.GetValue();
        if (!hasMapper)
            return (new(), directory, Ast.NewCompilerDiagnostic(Diagnostics.The_package_json_of_the_content_mapper_package_0_does_not_declare_a_typescript_contentMapper_object, packageName));
        var(exec, hasExec) = mapper.Exec.GetValue();
        if (!hasExec || (exec?.Count ?? 0) == 0)
            return (new(), directory, Ast.NewCompilerDiagnostic(Diagnostics.The_typescript_contentMapper_exec_of_the_content_mapper_package_0_must_be_a_non_empty_array_of_strings, packageName));
        var(options, _) = mapper.CompilerOptions.GetValue();
        var(dynamicConfig, _) = mapper.DynamicConfig.GetValue();
        return (new() { Name = name, Version = version, Exec = exec, CompilerOptions = options, DynamicConfig = dynamicConfig }, directory, null);
    }
}
