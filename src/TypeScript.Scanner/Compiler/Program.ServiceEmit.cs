// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// compiler/program.go:695–717,1491–1493,1642–1659,1890–2090.
// Explicit retained request values join only actual checker checkout owners.
using TypeScript.Scanner;

namespace TypeScript.Compiler;
public sealed partial class Program
{
    private Slice<Diagnostic> GetSyntacticDiagnosticsForEmit(CompilerRequestContext context, SourceFile? file) => GetSyntacticDiagnostics(context.Cancellation, file);
    private Slice<Diagnostic> GetBindDiagnosticsForEmit(CompilerRequestContext context, SourceFile? file) => GetBindDiagnostics(context.Cancellation, file);
    private Slice<Diagnostic> GetSemanticDiagnosticsForEmit(CompilerRequestContext context, SourceFile? file) => context.Request is { } request ? GetSemanticDiagnostics(request, file) : GetSemanticDiagnostics(context.Cancellation, file);
    private Slice<Diagnostic> GetDeclarationDiagnosticsForEmit(CompilerRequestContext context, SourceFile? source) => context.Request is { } request ? GetDeclarationDiagnostics(request, source) : GetDeclarationDiagnostics(context.Cancellation, source);
    // This genuine full-context overload is also consumed by ls/diagnostics.go.
    internal Slice<Diagnostic> GetDeclarationDiagnostics(TypeScript.Lsp.Protocol.ProtocolRequestContext context, SourceFile? source = null) => collectDiagnostics(context.Cancellation, source, true, (_, file) => getDeclarationDiagnosticsForFile(context, file));
    private Slice<Diagnostic> getDeclarationDiagnosticsForFile(TypeScript.Lsp.Protocol.ProtocolRequestContext context, SourceFile file)
    {
        if (file.IsDeclarationFile)
            return Slice<Diagnostic>.Make(0);
        var(cached, loaded) = DeclarationDiagnosticCache.Load(file);
        if (loaded)
            return cached;
        var(host, done) = CompilerEmitHost.New(new CompilerRequestContext(context), this, file);
        try
        {
            return DeclarationDiagnosticCache.LoadOrStore(file, Emitter.getDeclarationDiagnostics(host, file)).Actual;
        }
        finally
        {
            done();
        }
    }
}
