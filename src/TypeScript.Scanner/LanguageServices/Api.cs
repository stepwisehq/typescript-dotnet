// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/api.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using AstNav = TypeScript.AstNavigation.AstNav;
using CheckerType = TypeScript.Checking.Type;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal static readonly Exception ErrNoSourceFile = new InvalidOperationException("source file not found");
    internal static readonly Exception ErrNoTokenAtPosition = new InvalidOperationException("no token found at position");
    // The genuine completions.go:35 sentinel is consumed by the registry preparation path.
    // Its feature workers are separate counterparts, not successful substitutes here.
    internal static readonly Exception ErrNeedsAutoImports = new InvalidOperationException("completion list needs auto imports");
    // api.go:20. The inner exception preserves the original wrapped error identity.
    internal (Symbol? Symbol, Exception? Error) GetSymbolAtPosition(CancellationToken context, RootedFilePath fileName, int position) => GetSymbolAtPosition(ProtocolRequestContext.Default.WithCancellationToken(context), fileName, position);
    internal (Symbol? Symbol, Exception? Error) GetSymbolAtPosition(ProtocolRequestContext context, RootedFilePath fileName, int position)
    {
        var(currentProgram, file) = tryGetProgramAndFile(fileName);
        if (file == null)
            return (null, new InvalidOperationException(ErrNoSourceFile.Message + ": " + fileName.AsString(), ErrNoSourceFile));
        var token = AstNav.GetTokenAtPosition(file, position);
        if (token == null)
            return (null, new InvalidOperationException(ErrNoTokenAtPosition.Message + ": " + fileName.AsString() + ":" + position.ToString(System.Globalization.CultureInfo.InvariantCulture), ErrNoTokenAtPosition));
        var(checker, done) = currentProgram.GetTypeCheckerForFile(context, file);
        try
        {
            return (checker.GetSymbolAtLocation(token), null);
        }
        finally
        {
            done();
        }
    }

    internal Symbol? GetSymbolAtLocation(CancellationToken context, Node node) => GetSymbolAtLocation(ProtocolRequestContext.Default.WithCancellationToken(context), node);
    internal Symbol? GetSymbolAtLocation(ProtocolRequestContext context, Node node)
    {
        var currentProgram = GetProgram();
        var(checker, done) = currentProgram.GetTypeCheckerForFile(context, Ast.GetSourceFileOfNode(node));
        try
        {
            return checker.GetSymbolAtLocation(node);
        }
        finally
        {
            done();
        }
    }

    internal CheckerType GetTypeOfSymbol(CancellationToken context, Symbol symbol) => GetTypeOfSymbol(ProtocolRequestContext.Default.WithCancellationToken(context), symbol);
    internal CheckerType GetTypeOfSymbol(ProtocolRequestContext context, Symbol symbol)
    {
        var(checker, done) = GetProgram().GetTypeChecker(context);
        try
        {
            return checker.GetTypeOfSymbolAtLocation(symbol, null);
        }
        finally
        {
            done();
        }
    }
}
