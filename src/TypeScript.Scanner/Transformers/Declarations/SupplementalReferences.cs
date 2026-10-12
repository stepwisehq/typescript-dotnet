// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete supplementalreferences.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Transformers.Declarations;
// supplementalreferences.go:12. The source's supplemental slice header is retained without copying.
internal sealed class SupplementalReferencesTransformer : IDeclarationTransformer
{
    private IDeclarationEmitHost host;
    private Slice<SourceFile> supplementalFiles;
    private RootedFilePath declarationFilePath;
    private bool forceDeclarationPaths;
    // supplementalreferences.go:19.
    internal static SupplementalReferencesTransformer NewSupplementalReferencesTransformer(IDeclarationEmitHost host, SourceFile sourceFile, RootedFilePath declarationFilePath, bool forceDeclarationPaths)
    {
        return new SupplementalReferencesTransformer
        {
            host = host,
            supplementalFiles = sourceFile.SupplementalSourceFiles(),
            declarationFilePath = declarationFilePath,
            forceDeclarationPaths = forceDeclarationPaths
        };
    }

    // supplementalreferences.go:28. References are appended to the actual source owner.
    public SourceFile TransformSourceFile(SourceFile sourceFile)
    {
        foreach (var supplemental in supplementalFiles)
        {
            if (!host.SourceFileMayBeEmitted(supplemental, forceDeclarationPaths))
                continue;
            var declarationPath = host.GetOutputPathsFor(supplemental, forceDeclarationPaths).DeclarationFilePath();
            if (declarationPath.AsValue().Length == 0)
                continue;
            var(relativePath, ok) = host.CaseSensitivity().RelativePathFromFile(declarationFilePath, declarationPath);
            if (!ok)
                throw new InvalidOperationException("supplemental declaration output must share a root with the primary declaration");
            sourceFile.ReferencedFiles = sourceFile.ReferencedFiles.Append(new FileReference { TextRange = Core.NewTextRange(-1, -1), FileNameValue = relativePath.AsModuleSpecifier().AsValue() });
        }

        return sourceFile;
    }

    // supplementalreferences.go:49.
    public Slice<Diagnostic> GetDiagnostics() => default;
}
