// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned declaration support, fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Transformers.Declarations;
// transform.go:28: exact declaration-output consumer contract.
internal interface IDeclarationOutputPaths
{
    RootedFilePath DeclarationFilePath();
    RootedFilePath JsFilePath();
}

// transform.go:34: existing module-specifier/output host contracts retain HasFileName roles.
internal interface IDeclarationEmitHost : TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost
{
    SourceFile GetSourceFileFromReference(SourceFile origin, FileReference reference);
    IDeclarationOutputPaths GetOutputPathsFor(SourceFile file, bool forceDtsPaths);
    bool SourceFileMayBeEmitted(SourceFile file, bool forceDtsEmit);
}

// diagnostics.go:10. Nullable value preserves the Go optional pointer and copied payload.
internal delegate SymbolAccessibilityDiagnostic? GetSymbolAccessibilityDiagnostic(SymbolAccessibilityResult result);
// diagnostics.go:12. Payload is never mutated after construction in upstream consumers.
internal readonly struct SymbolAccessibilityDiagnostic
{
    internal readonly Node errorNode;
    internal readonly Message diagnosticMessage;
    internal readonly Node typeName;
    internal SymbolAccessibilityDiagnostic(Node errorNode, Message diagnosticMessage, Node typeName = null)
    {
        this.errorNode = errorNode;
        this.diagnosticMessage = diagnosticMessage;
        this.typeName = typeName;
    }
}

// compiler/emitter.go: actual common consumer contract for both declaration stages.
internal interface IDeclarationTransformer
{
    SourceFile TransformSourceFile(SourceFile sourceFile);
    Slice<Diagnostic> GetDiagnostics();
}
