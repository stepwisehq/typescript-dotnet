// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of generic file host operations in tsc/internal/compiler/program.go.
using TypeScript.Scanner;
using TypeScript.Modules;

namespace TypeScript.Compiler;
public sealed partial class Program
{
    // HasFileName is structural in Go. SourceFile keeps its borrowed byte-header path;
    // other actual owners cross their existing raw Go string interface boundary once.
    private static (RootedFilePath File, PathKey Path) GetFileIdentity(HasFileName file) => file is SourceFile source ? (RootedFilePath.FromValue(source.FileNameGoValue()), PathKey.FromValue(source.PathKeyValue())) : (RootedFilePath.FromValue(ByteStringValue.FromString(file.FileName())), PathKey.FromValue(ByteStringValue.FromString(file.PathKey())));
    // program.go:206.
    public RootedFilePath GetSourceOfProjectReferenceIfOutputIncluded(HasFileName file)
    {
        if (file is SourceFile source)
            return GetSourceOfProjectReferenceIfOutputIncluded(source);
        var identity = GetFileIdentity(file);
        return processedFiles.outputFileToProjectReferenceSource.GetValueOrDefault(ByteStringKey.FromValue(identity.Path.AsValue()), identity.File);
    }

    // program.go:653.
    public ResolvedModule? GetResolvedModuleFromModuleSpecifier(HasFileName file, Node specifier)
    {
        if (file is SourceFile source)
            return GetResolvedModuleFromModuleSpecifier(source, specifier);
        if (!Ast.IsStringLiteralLike(specifier))
            throw new InvalidOperationException("moduleSpecifier must be a StringLiteralLike");
        if (Ast.IsSourcePhaseImport(specifier.Parent))
            return null;
        return resolvedModules.GetValueOrDefault(ByteStringKey.FromValue(GetFileIdentity(file).Path.AsValue()))?.GetValueOrDefault(new(specifier.TextValue(), GetModeForUsageLocation(file, specifier)));
    }

    // program.go:1779.
    public ResolutionMode GetModeForUsageLocation(HasFileName file, Node usage)
    {
        if (file is SourceFile source)
            return GetModeForUsageLocation(source, usage);
        var identity = GetFileIdentity(file);
        return FileLoader.GetModeForUsageLocation(identity.File, GetSourceFileMetaData(identity.Path), usage, projectReferenceFileMapper.GetCompilerOptionsForFile(identity.File, identity.Path));
    }

    // program.go:1800.
    public ResolutionMode GetDefaultResolutionModeForFile(HasFileName file)
    {
        if (file is SourceFile source)
            return GetDefaultResolutionModeForFile(source);
        var identity = GetFileIdentity(file);
        return FileLoader.GetDefaultResolutionModeForFile(identity.File, GetSourceFileMetaData(identity.Path), projectReferenceFileMapper.GetCompilerOptionsForFile(identity.File, identity.Path));
    }
}
