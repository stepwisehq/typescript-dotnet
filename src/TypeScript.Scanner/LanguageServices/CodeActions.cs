// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/codeactions.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using CompilerProgram = TypeScript.Compiler.Program;
using AstDiagnostic = TypeScript.Scanner.Diagnostic;
using LspDiagnostic = TypeScript.Lsp.Protocol.Diagnostic;
using LspCodeAction = TypeScript.Lsp.Protocol.CodeAction;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private sealed class CodeFixProvider
    {
        internal Slice<int> ErrorCodes;
        internal Func<ProtocolRequestContext, CodeFixContext, (Slice<CodeAction> Actions, Exception? Error)> GetCodeActions = null!;
        internal Slice<ByteStringValue> FixIds;
        internal Func<ProtocolRequestContext, CodeFixContext, (CombinedCodeActions? Actions, Exception? Error)>? GetAllCodeActions;
    }

    private sealed class CodeFixContext
    {
        internal SourceFile SourceFile = null!;
        internal TextRange Span;
        internal int ErrorCode;
        internal CompilerProgram Program = null!;
        internal LanguageService LS = null!;
        internal LspDiagnostic? Diagnostic;
        internal CodeActionParams? Params;
    }

    private sealed class CodeAction
    {
        internal ByteStringValue Description;
        internal Slice<TextEdit> Changes;
        internal ByteStringValue FixID, FixAllDescription;
        internal int Compare(CodeAction other)
        {
            int comparison = ByteStringKey.FromValue(Description).CompareTo(ByteStringKey.FromValue(other.Description));
            if (comparison != 0)
                return comparison;
            comparison = Changes.Length.CompareTo(other.Changes.Length);
            if (comparison != 0)
                return comparison;
            for (var i = 0; i < Changes.Length; i++)
            {
                comparison = Changes[i].Compare(other.Changes[i]);
                if (comparison != 0)
                    return comparison;
            }

            return 0;
        }
    }

    private sealed class CombinedCodeActions
    {
        internal ByteStringValue Description;
        internal Slice<TextEdit> Changes;
    }

    // Go package initialization follows provider dependencies across source files.
    // A nested holder runs after this partial owner's provider fields initialize,
    // retaining one immutable header instead of relying on C# file ordering.
    private static class CodeFixProviderOwners
    {
        internal static readonly Slice<CodeFixProvider> Values = Slice<CodeFixProvider>.FromArray([ImportFixProvider, IsolatedDeclarationsFixProvider, FixClassIncorrectlyImplementsInterfaceProvider]);
    }

    private static Slice<CodeFixProvider> codeFixProviders => CodeFixProviderOwners.Values;

    internal (CommandOrCodeActionArrayOrNull Result, Exception? Error) ProvideCodeActions(ProtocolRequestContext context, CodeActionParams parameters)
    {
        var(program, file) = getProgramAndFile(parameters.TextDocument.Uri);
        Slice<CommandOrCodeAction> actions = default;
        if (parameters.Context?.Only is { } only)
        {
            foreach (var kind in only)
            {
                foreach (var matchingKind in getOrganizeImportsActionsForKind(kind))
                    actions = actions.Append(createOrganizeImportsAction(context, program, file, matchingKind));
                if (isFixAllKind(kind))
                {
                    var(fixAllAction, error) = createFixAllAction(context, program, file, parameters.TextDocument.Uri);
                    if (error != null)
                        return (new(), error);
                    if (fixAllAction != null)
                        actions = actions.Append(fixAllAction);
                }
            }
        }

        if (parameters.Context?.Diagnostics is { } diagnostics && wantsQuickFixes(parameters.Context.Only))
        {
            var fixIdSeen = new Dictionary<ByteStringKey, CodeFixProvider>();
            Slice<CodeAction> seen = default;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic?.Code?.Integer is not { } code)
                    continue;
                foreach (var provider in codeFixProviders)
                {
                    if (!codeFixProviderMatchesLSPDiagnostic(provider, diagnostic))
                        continue;
                    foreach (var mapped in converters.FromLSPRangeForSourceFile(file, diagnostic.Range, Feature.CodeActions))
                    {
                        var fixContext = new CodeFixContext
                        {
                            SourceFile = mapped.Script,
                            Span = mapped.Span,
                            ErrorCode = code,
                            Program = program,
                            LS = this,
                            Diagnostic = diagnostic,
                            Params = parameters
                        };
                        var(providerActions, error) = provider.GetCodeActions(context, fixContext);
                        if (error != null)
                            return (new(), error);
                        foreach (var action in providerActions)
                        {
                            var(index, found) = GoSlices.BinarySearchFunc(seen, action, static (left, right) => left.Compare(right));
                            if (found)
                                continue;
                            seen = seen.Insert(index, action);
                            actions = actions.Append(convertToLSPCodeAction(action, diagnostic, parameters.TextDocument.Uri));
                            if (action.FixID.Length != 0)
                                fixIdSeen[ByteStringKey.FromValue(action.FixID)] = provider;
                        }
                    }
                }
            }

            var(fixAllActions, fixAllError) = getFixAllQuickFixes(context, program, file, parameters.TextDocument.Uri, fixIdSeen);
            if (fixAllError != null)
                return (new(), fixAllError);
            actions = actions.Append(fixAllActions);
        }

        return (new() { CommandOrCodeActionArray = actions }, null);
    }

    private (Slice<CommandOrCodeAction> Actions, Exception? Error) getFixAllQuickFixes(ProtocolRequestContext context, CompilerProgram program, SourceFile file, DocumentUri uri, Dictionary<ByteStringKey, CodeFixProvider> fixIdSeen)
    {
        Slice<CommandOrCodeAction> actions = default;
        HashSet<CodeFixProvider>? seen = null;
        foreach (var provider in fixIdSeen.Values)
        {
            if (seen?.Contains(provider) == true)
                continue;
            (seen ??= new()).Add(provider);
            if (provider.GetAllCodeActions == null || !hasMultipleFixableDiagnostics(context, program, file, provider.ErrorCodes))
                continue;
            var(combined, error) = provider.GetAllCodeActions(context, new CodeFixContext { SourceFile = file, Program = program, LS = this });
            if (error != null)
                return (default, error);
            if (combined != null && combined.Changes.Length > 0)
            {
                var changes = ProtocolMap<DocumentUri, Slice<TextEdit?>>.Make();
                changes[uri] = nullableTextEditHeader(combined.Changes);
                actions = actions.Append(new CommandOrCodeAction { CodeAction = new LspCodeAction { Title = combined.Description, Kind = CodeActionKind.QuickFix, Edit = new WorkspaceEdit { Changes = changes } } });
            }
        }

        return (actions, null);
    }

    private static bool hasMultipleFixableDiagnostics(ProtocolRequestContext context, CompilerProgram program, SourceFile file, Slice<int> codes)
    {
        var count = 0;
        foreach (var diagnostic in getAllDiagnostics(context, program, file))
        {
            if (isFixableDiagnostic(diagnostic, codes) && ++count >= 2)
                return true;
        }

        return false;
    }

    private static bool codeFixProviderMatchesLSPDiagnostic(CodeFixProvider provider, LspDiagnostic diagnostic)
    {
        if (diagnostic.Source is { } source && !source.EqualsString("ts"))
            return false;
        return diagnostic.Code?.Integer is { } code && containsErrorCode(provider.ErrorCodes, code);
    }

    private static bool isFixableDiagnostic(AstDiagnostic diagnostic, Slice<int> codes) => diagnostic.Source() == "" && containsErrorCode(codes, diagnostic.Code());
    private static bool isFixAllKind(CodeActionKind kind) => kind.Contains(CodeActionKind.SourceFixAllTs);
    private static bool wantsQuickFixes(Slice<CodeActionKind>? only)
    {
        if (only == null || only.Value.Length == 0)
            return true;
        foreach (var kind in only.Value)
            if (kind.Contains(CodeActionKind.QuickFix))
                return true;
        return false;
    }

    private (CommandOrCodeAction? Action, Exception? Error) createFixAllAction(ProtocolRequestContext context, CompilerProgram program, SourceFile file, DocumentUri uri)
    {
        var changes = ProtocolMap<DocumentUri, Slice<TextEdit?>>.Make();
        foreach (var provider in codeFixProviders)
        {
            if (provider.GetAllCodeActions == null)
                continue;
            var(combined, error) = provider.GetAllCodeActions(context, new CodeFixContext { SourceFile = file, Program = program, LS = this });
            if (error != null)
                return (null, error);
            if (combined != null && combined.Changes.Length > 0)
            {
                changes.TryGetValue(uri, out var prior);
                changes[uri] = prior.Append(nullableTextEditHeader(combined.Changes));
            }
        }

        if (changes.Count == 0)
            return (null, null);
        return (new CommandOrCodeAction { CodeAction = new LspCodeAction { Title = ByteStringValue.FromString(Diagnostics.Fix_All.Localize(context.Locale)), Kind = CodeActionKind.SourceFixAllTs, Edit = new WorkspaceEdit { Changes = changes } } }, null);
    }

    private static ByteStringValue getOrganizeImportsActionTitle(ProtocolRequestContext context, CodeActionKind kind)
    {
        var message = kind == CodeActionKind.SourceRemoveUnusedImportsTs ? Diagnostics.Remove_Unused_Imports : kind == CodeActionKind.SourceSortImportsTs ? Diagnostics.Sort_Imports : Diagnostics.Organize_Imports;
        return ByteStringValue.FromString(message.Localize(context.Locale));
    }

    private static Slice<CodeActionKind> getOrganizeImportsActionsForKind(CodeActionKind requested)
    {
        var kinds = Slice<CodeActionKind>.FromArray([CodeActionKind.SourceOrganizeImportsTs, CodeActionKind.SourceRemoveUnusedImportsTs, CodeActionKind.SourceSortImportsTs]);
        Slice<CodeActionKind> result = default;
        foreach (var kind in kinds)
            if (requested.Contains(kind))
                result = result.Append(kind);
        return GoSlices.Contains(result, requested) ? Slice<CodeActionKind>.FromArray([requested]) : result;
    }

    private CommandOrCodeAction createOrganizeImportsAction(ProtocolRequestContext context, CompilerProgram program, SourceFile file, CodeActionKind kind)
    {
        var title = getOrganizeImportsActionTitle(context, kind);
        var changes = OrganizeImports(context, file, program, kind);
        if (changes.Count == 0)
            return new CommandOrCodeAction
            {
                CodeAction = new LspCodeAction
                {
                    Title = title,
                    Kind = kind,
                    Edit = new WorkspaceEdit
                    {
                        Changes = ProtocolMap<DocumentUri, Slice<TextEdit?>>.Make()
                    }
                }
            };
        var lspChanges = ProtocolMap<DocumentUri, Slice<TextEdit?>>.Make();
        foreach (var(name, edits)in changes)
            lspChanges[TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(name)] = nullableTextEditHeader(edits);
        return new CommandOrCodeAction
        {
            CodeAction = new LspCodeAction
            {
                Title = title,
                Kind = kind,
                Edit = new WorkspaceEdit
                {
                    Changes = lspChanges
                }
            }
        };
    }

    // Go []*TextEdit has pointer elements; nullability changes only the managed
    // header annotation, retaining the exact array/offset/length/capacity owner.
    private static Slice<TextEdit?> nullableTextEditHeader(Slice<TextEdit> edits) => edits!;
    private static ProtocolMap<DocumentUri, Slice<TextEdit?>> codeActionChanges(DocumentUri uri, Slice<TextEdit> edits)
    {
        var changes = ProtocolMap<DocumentUri, Slice<TextEdit?>>.Make();
        changes[uri] = nullableTextEditHeader(edits);
        return changes;
    }

    private static bool containsErrorCode(Slice<int> codes, int code) => GoSlices.Contains(codes, code);
    private static CommandOrCodeAction convertToLSPCodeAction(CodeAction action, LspDiagnostic diagnostic, DocumentUri uri) => new()
    {
        CodeAction = new LspCodeAction
        {
            Title = action.Description,
            Kind = CodeActionKind.QuickFix,
            Edit = new WorkspaceEdit
            {
                Changes = codeActionChanges(uri, action.Changes)
            },
            Diagnostics = Slice<LspDiagnostic?>.FromArray([diagnostic])
        }
    };
}
