// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/lsconv/converters.go:476-686 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.Lsp.Protocol;
using AstDiagnostic = TypeScript.Scanner.Diagnostic;
using LspDiagnostic = TypeScript.Lsp.Protocol.Diagnostic;
using LspRange = TypeScript.Lsp.Protocol.Range;
using Writer = TypeScript.DiagnosticWriter.DiagnosticWriter;

namespace TypeScript.LanguageServices.Lsconv;
internal static partial class Lsconv
{
    private readonly record struct DiagnosticOptions(bool ReportStyleChecksAsWarnings, bool RelatedInformation, Slice<DiagnosticTag> TagValueSet, bool VisualStudio);
    private static readonly HashSet<int> styleCheckDiagnostics = [Diagnostics.X_0_is_declared_but_never_used.Code(), Diagnostics.X_0_is_declared_but_its_value_is_never_read.Code(), Diagnostics.Property_0_is_declared_but_its_value_is_never_read.Code(), Diagnostics.All_imports_in_import_declaration_are_unused.Code(), Diagnostics.Unreachable_code_detected.Code(), Diagnostics.Unused_label.Code(), Diagnostics.Fallthrough_case_in_switch.Code(), Diagnostics.Not_all_code_paths_return_a_value.Code()];
    internal static LspDiagnostic DiagnosticToLSPPull(ProtocolRequestContext context, Converters converters, AstDiagnostic diagnostic, bool reportStyleChecksAsWarnings)
    {
        ref readonly var capabilities = ref context.ClientCapabilities;
        var options = capabilities.TextDocument.Diagnostic;
        return DiagnosticToLSP(context, converters, diagnostic, new(reportStyleChecksAsWarnings, options.RelatedInformation, options.TagSupport.ValueSet, capabilities.VSSupportsVisualStudioExtensions));
    }

    internal static LspDiagnostic DiagnosticToLSPPush(ProtocolRequestContext context, Converters converters, AstDiagnostic diagnostic)
    {
        ref readonly var capabilities = ref context.ClientCapabilities;
        var options = capabilities.TextDocument.PublishDiagnostics;
        return DiagnosticToLSP(context, converters, diagnostic, new(false, options.RelatedInformation, options.TagSupport.ValueSet, capabilities.VSSupportsVisualStudioExtensions));
    }

    private static LspDiagnostic DiagnosticToLSP(ProtocolRequestContext context, Converters converters, AstDiagnostic diagnostic, DiagnosticOptions options)
    {
        var severity = DiagnosticSeverity(diagnostic.Category());
        if (options.ReportStyleChecksAsWarnings && severity == TypeScript.Lsp.Protocol.DiagnosticSeverity.Error && styleCheckDiagnostics.Contains(diagnostic.Code()))
            severity = TypeScript.Lsp.Protocol.DiagnosticSeverity.Warning;
        Slice<DiagnosticRelatedInformation?> relatedInformation = default;
        if (options.RelatedInformation)
        {
            relatedInformation = Slice<DiagnosticRelatedInformation?>.Make(0, diagnostic.RelatedInformation().Count);
            foreach (var related in diagnostic.RelatedInformation())
            {
                var(script, range) = DiagnosticScriptAndRange(related.File(), related.Loc(), ByteStringValue.FromString(related.Source()));
                var(mapped, fidelity) = converters.ToLSPRange(script!, range);
                if (fidelity == Fidelity.None)
                    mapped = default;
                relatedInformation = relatedInformation.Append(new DiagnosticRelatedInformation { Location = new Location { Uri = FileNameToDocumentURI(related.File().OriginalFileName()), Range = mapped }, Message = ByteStringValue.FromString(Writer.WrapASTDiagnostic(related, true).Localize(context.Locale)) });
            }
        }

        Slice<DiagnosticTag> tags = default;
        if (options.TagValueSet.Count > 0 && (diagnostic.ReportsUnnecessary() || diagnostic.ReportsDeprecated()))
        {
            tags = Slice<DiagnosticTag>.Make(0, 2);
            if (diagnostic.ReportsUnnecessary() && ContainsTag(options.TagValueSet, DiagnosticTag.Unnecessary))
                tags = tags.Append(DiagnosticTag.Unnecessary);
            if (diagnostic.ReportsDeprecated() && ContainsTag(options.TagValueSet, DiagnosticTag.Deprecated))
                tags = tags.Append(DiagnosticTag.Deprecated);
        }

        LspRange lspRange = default;
        if (diagnostic.File()is not null)
        {
            var(script, range) = DiagnosticScriptAndRange(diagnostic.File(), diagnostic.Loc(), ByteStringValue.FromString(diagnostic.Source()));
            var(mapped, fidelity) = converters.ToLSPRange(script!, range);
            lspRange = fidelity == Fidelity.None ? default : mapped;
        }

        var source = ByteStringValue.FromString(diagnostic.Source());
        if (source.Length == 0)
            source = ByteStringValue.FromString("ts");
        var code = options.VisualStudio ? new IntegerOrString
        {
            String = ByteStringValue.FromString("TS" + diagnostic.Code().ToString(CultureInfo.InvariantCulture))
        }

        : new IntegerOrString
        {
            Integer = diagnostic.Code()
        };
        return new LspDiagnostic
        {
            Range = lspRange,
            Code = code,
            Severity = severity,
            Message = new StringOrMarkupContent
            {
                String = MessageChainToString(diagnostic, context.Locale)
            },
            Source = source,
            RelatedInformation = PtrToSliceIfNonEmpty(relatedInformation),
            Tags = PtrToSliceIfNonEmpty(tags)
        };
    }

    private static bool ContainsTag(Slice<DiagnosticTag> tags, DiagnosticTag tag)
    {
        foreach (var item in tags)
            if (item == tag)
                return true;
        return false;
    }

    internal static (IScript? Script, TextRange Range) DiagnosticScriptAndRange(SourceFile? file, TextRange range, ByteStringValue source)
    {
        if (file is null || file.SpanMap()is null)
            return (file, range);
        var original = new OriginalTextScript(file.OriginalFileName(), ((IScript)file).OriginalTextValue());
        if (source.Length != 0)
            return (original, range);
        var(mapped, fidelity) = file.SpanMap().VirtualToOriginalSpan(range);
        return (original, fidelity == Fidelity.None ? new TextRange(0, 0) : mapped);
    }

    private static TypeScript.Lsp.Protocol.DiagnosticSeverity DiagnosticSeverity(DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.Suggestion => TypeScript.Lsp.Protocol.DiagnosticSeverity.Hint,
        DiagnosticCategory.Message => TypeScript.Lsp.Protocol.DiagnosticSeverity.Information,
        DiagnosticCategory.Warning => TypeScript.Lsp.Protocol.DiagnosticSeverity.Warning,
        _ => TypeScript.Lsp.Protocol.DiagnosticSeverity.Error
    };
    private static ByteStringValue MessageChainToString(AstDiagnostic diagnostic, TypeScript.Locale.Locale locale)
    {
        if (diagnostic.MessageChain().Count == 0)
            return ByteStringValue.FromString(Writer.WrapASTDiagnostic(diagnostic, true).Localize(locale));
        // Existing diagnostic writer's TextWriter API transports Go byte strings;
        // retain those bytes at its real string boundary rather than re-encoding.
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        Writer.WriteFlattenedASTDiagnosticMessage(output, diagnostic, "\n", locale);
        return ByteStringValue.FromString(output.ToString());
    }

    private static Slice<T>? PtrToSliceIfNonEmpty<T>(Slice<T> slice) => slice.Count == 0 ? null : slice;
}
