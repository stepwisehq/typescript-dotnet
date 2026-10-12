// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/codelens.go counterpart, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Whole CodeLens owner with genuine shared completion/reference/locale consumers.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.Utilities;
using TypeScript.SpanMap;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private readonly record struct CodeLensKey(CodeLensKind Kind, uint StartLine, uint StartCharacter, uint EndLine, uint EndCharacter);
    private static CodeLensKey keyForCodeLens(CodeLens lens) => new(lens.Data!.Kind, lens.Range.Start.Line, lens.Range.Start.Character, lens.Range.End.Line, lens.Range.End.Character);
    internal (CodeLensesOrNull Result, Exception? Error) ProvideCodeLenses(ProtocolRequestContext context, DocumentUri uri)
    {
        var(_, file) = getProgramAndFile(uri);
        var preferences = UserPreferences().CodeLensUserPreferences;
        if (preferences.ReferencesCodeLensEnabled != Tristate.True && preferences.ImplementationsCodeLensEnabled != Tristate.True)
            return (new(), null);
        Slice<CodeLens?> result = default;
        HashSet<CodeLensKey>? seen = null;
        var projections = Slice<SourceFile>.FromArray([file]).Append(file.SupplementalSourceFiles());
        foreach (var projection in projections)
        {
            Symbol? lastSymbol = null;
            Visitor visit = null!;
            visit = node =>
            {
                if (context.Cancellation.IsCancellationRequested)
                    return true;
                var currentSymbol = node.Symbol();
                if (!ReferenceEquals(lastSymbol, currentSymbol))
                {
                    lastSymbol = currentSymbol;
                    if (preferences.ReferencesCodeLensEnabled == Tristate.True && isValidReferenceLensNode(node, preferences))
                    {
                        var lens = newCodeLensForNode(uri, projection, node, CodeLensKind.References);
                        if (lens is not null && (seen ??= new()).Add(keyForCodeLens(lens)))
                            result = result.Append(lens);
                    }

                    if (preferences.ImplementationsCodeLensEnabled == Tristate.True && isValidImplementationsCodeLensNode(node, preferences))
                    {
                        var lens = newCodeLensForNode(uri, projection, node, CodeLensKind.Implementations);
                        if (lens is not null && (seen ??= new()).Add(keyForCodeLens(lens)))
                            result = result.Append(lens);
                    }
                }

                var saved = lastSymbol;
                node.ForEachChild(visit);
                lastSymbol = saved;
                return false;
            };
            visit(projection);
        }

        return (new() { CodeLenses = result }, null);
    }

    internal (CodeLens? Result, Exception? Error) ResolveCodeLens(ProtocolRequestContext context, CodeLens lens, ByteStringValue? showLocationsCommandName, ICrossProjectOrchestrator? orchestrator)
    {
        var uri = lens.Data!.Uri;
        var textDocument = new TextDocumentIdentifier
        {
            Uri = uri
        };
        var(program, original) = getProgramAndFile(uri);
        var file = sourceFileForSupplementalFileIndex(original, lens.Data.SupplementalFileIndex);
        if (file is null)
            return (null, new InvalidOperationException("supplemental source file index not found: " + lens.Data.SupplementalFileIndex!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Slice<Location> locations = default;
        ByteStringValue title = default;
        if (lens.Data.Kind.Equals(CodeLensKind.References))
        {
            var(data, _) = provideSymbolsAndEntriesAtPosition(context, program, file, lens.Data.Position, false, false);
            var(response, error) = provideReferencesFromData(context, new ReferenceParams { TextDocument = textDocument, Position = lens.Range.Start, Context = new() { IncludeDeclaration = false } }, orchestrator, data);
            if (error is not null)
                return (null, error);
            if (response.Locations is { } items)
                locations = items;
            title = ByteStringValue.FromString(locations.Length == 1 ? Diagnostics.X_1_reference.Localize(context.Locale) : Diagnostics.X_0_references.Localize(context.Locale, locations.Length));
        }
        else if (lens.Data.Kind.Equals(CodeLensKind.Implementations))
        {
            var(data, _) = provideSymbolsAndEntriesAtPosition(context, program, file, lens.Data.Position, false, true);
            var(response, error) = provideImplementationsFromData(context, new ImplementationParams { TextDocument = textDocument, Position = lens.Range.Start }, new SymbolEntryTransformOptions { requireLocationsResult = true, dropOriginNodes = true }, orchestrator, data);
            if (error is not null)
                return (null, error);
            if (response.Locations is { } items)
                locations = items;
            title = ByteStringValue.FromString(locations.Length == 1 ? Diagnostics.X_1_implementation.Localize(context.Locale) : Diagnostics.X_0_implementations.Localize(context.Locale, locations.Length));
        }

        var command = new Command
        {
            Title = title
        };
        if (locations.Length > 0 && showLocationsCommandName is { } commandName)
        {
            command.CommandValue = commandName;
            command.Arguments = Slice<object?>.FromArray([uri, new ProtocolPayload<Position>(ProtocolCodec.GetType_6d031af10da7a25e(), lens.Range.Start), new ProtocolPayload<Slice<Location>>(ProtocolCodec.CodeLensLocationsType, locations)]);
        }

        lens.Command = command;
        return (lens, null);
    }

    private CodeLens? newCodeLensForNode(DocumentUri uri, SourceFile file, Node node, CodeLensKind kind)
    {
        var forRange = node.Name() ?? node;
        int position = ScannerApi.SkipTrivia(file.TextView(), forRange.Pos());
        var(range, fidelity) = converters.ToLSPRangeForFeature(file, new TextRange(position, node.End()), Feature.CodeLens);
        if (fidelity.IsNone())
            return null;
        return new()
        {
            Range = range,
            Data = new()
            {
                Kind = kind,
                Uri = uri,
                Position = position,
                SupplementalFileIndex = supplementalFileIndex(file)
            }
        };
    }

    private static bool isValidImplementationsCodeLensNode(Node node, CodeLensUserPreferences preferences)
    {
        switch (node.Kind)
        {
            case Kind.InterfaceDeclaration:
                return true;
            case Kind.MethodSignature:
                return preferences.ImplementationsCodeLensShowOnInterfaceMethods == Tristate.True && node.Parent.Kind == Kind.InterfaceDeclaration;
            case Kind.MethodDeclaration:
                if (preferences.ImplementationsCodeLensShowOnAllClassMethods == Tristate.True && node.Parent.Kind == Kind.ClassDeclaration)
                    return !Ast.HasModifier(node, ModifierFlags.Private) && node.Name().Kind != Kind.PrivateIdentifier;
                goto case Kind.ClassDeclaration;
            case Kind.ClassDeclaration:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.PropertyDeclaration:
                return Ast.HasModifier(node, ModifierFlags.Abstract);
            default:
                return false;
        }
    }

    private static bool isValidReferenceLensNode(Node node, CodeLensUserPreferences preferences)
    {
        switch (node.Kind)
        {
            case Kind.FunctionDeclaration:
                if (preferences.ReferencesCodeLensShowOnAllFunctions == Tristate.True)
                    return true;
                goto case Kind.VariableDeclaration;
            case Kind.VariableDeclaration:
                return (Ast.GetCombinedModifierFlags(node) & ModifierFlags.Export) != 0;
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.EnumDeclaration:
            case Kind.EnumMember:
                return true;
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
                return node.Parent.Kind is Kind.ClassDeclaration or Kind.InterfaceDeclaration or Kind.TypeLiteral;
            default:
                return false;
        }
    }
}
