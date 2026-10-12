// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/diagnostic.go; parse/bind diagnostics plus selected checker collection/comparison slice.
#nullable disable
namespace TypeScript.Scanner;
public enum RepopulateDiagnosticKind { ModeMismatch = 1, ModuleNotFound = 2 }
public sealed class RepopulateDiagnosticInfo
{
    public RepopulateDiagnosticKind Kind;
    public string ModuleReference = "";
    public ResolutionMode Mode;
    public string PackageName = "";
}

public sealed class Diagnostic
{
    internal RepopulateDiagnosticInfo repopulateInfo;
    public RepopulateDiagnosticInfo RepopulateInfo() => repopulateInfo;
    public void SetRepopulateInfo(RepopulateDiagnosticInfo info) => repopulateInfo = info;
    internal SourceFile file;
    internal TextRange loc;
    internal Message message;
    internal int code;
    internal DiagnosticCategory category;
    internal string source = "", messageText = "", messageKey = "";
    internal bool reportsUnnecessary, reportsDeprecated, skippedOnNoEmit;
    internal List<string> messageArgs;
    internal Slice<Diagnostic> relatedInformation, messageChain;
    public SourceFile File() => file;
    public int Pos() => loc.Pos();
    public int End() => loc.End();
    public int Len() => loc.Len();
    public TextRange Loc() => loc;
    public int Code() => code;
    public DiagnosticCategory Category() => category;
    public string MessageKey() => messageKey;
    public string MessageText() => messageText;
    public List<string> MessageArgs() => messageArgs;
    public Slice<Diagnostic> RelatedInformation() => relatedInformation;
    public Slice<Diagnostic> MessageChain() => messageChain;
    public bool ReportsUnnecessary() => reportsUnnecessary;
    public bool ReportsDeprecated() => reportsDeprecated;
    public string Source() => source;
    public bool SkippedOnNoEmit() => skippedOnNoEmit;
    public void SetCategory(DiagnosticCategory value) => category = value;
    public void SetSkippedOnNoEmit() => skippedOnNoEmit = true;
    public Diagnostic SetExternalData(string source, string messageText)
    {
        this.source = source;
        this.messageText = messageText;
        return this;
    }

    public void SetFile(SourceFile value) => file = value;
    public void SetLocation(TextRange value) => loc = value;
    public Diagnostic AddRelatedInfo(Diagnostic value)
    {
        if (value is not null)
        {
            // diagnostic.go:105. Appending updates this diagnostic's slice
            // header; clones and wrappers retain their previous visible length.
            relatedInformation = relatedInformation.Append(value);
        }
        return this;
    }

    public Diagnostic SetRelatedInfo(Slice<Diagnostic> value)
    {
        relatedInformation = value;
        return this;
    }

    // Explicit legacy input adapters own a snapshot of external List storage.
    public Diagnostic SetRelatedInfo(List<Diagnostic> value) => SetRelatedInfo(value is null ? default(Slice<Diagnostic>) : Slice<Diagnostic>.FromArray(value.ToArray()));
    public Diagnostic SetMessageChain(List<Diagnostic> value) => SetMessageChain(value is null ? default(Slice<Diagnostic>) : Slice<Diagnostic>.FromArray(value.ToArray()));

    public Diagnostic AddMessageChain(Diagnostic value)
    {
        if (value is not null)
            messageChain = messageChain.Append(value);
        return this;
    }

    public Diagnostic SetMessageChain(Slice<Diagnostic> value)
    {
        messageChain = value;
        return this;
    }

    public string Localize() => message is null && messageText != "" ? ScannerString.ToJsString(messageText) : (message ?? Diagnostics.KeyToMessage(messageKey) ?? throw new InvalidOperationException("Unknown diagnostic message: " + messageKey)).FormatByteArguments(displayMessageArgs());
    // diagnostic.go:117–122. Retain external prelocalized raw text; ordinary
    // localization uses alias-adjusted raw arguments, not the display projection.
    public string Localize(TypeScript.Locale.Locale locale) => message is null && messageText != ""
        ? messageText
        : DiagnosticLocalization.Localize(locale, message, messageKey, displayMessageArgs() ?? []);
    public string String() => Localize();
    public override string ToString() => String();
    public Diagnostic Clone() => (Diagnostic)MemberwiseClone();

    // diagnostic.go:133. Display a complete alias span using its original text;
    // stored arguments retain the virtual text for serialization and code fixes.
    private List<string> displayMessageArgs()
    {
        if (file is null || source != "")
            return messageArgs;
        var (segment, ok) = TypeScript.SpanMap.SpanMaps.AliasForVirtualSpan(file.SpanMap(), loc);
        if (!ok)
            return messageArgs;
        var virtualText = file.TextView();
        string originalText = file.OriginalText();
        if (segment.VirtualStart < 0 || segment.VirtualEnd > virtualText.Length ||
            segment.OriginalStart < 0 || segment.OriginalEnd > originalText.Length)
            return messageArgs;
        var virtualName = ByteStringValue.FromView(virtualText.Slice(segment.VirtualStart, segment.VirtualEnd));
        string originalName = null;
        List<string> result = null;
        for (int index = 0; index < (messageArgs?.Count ?? 0); index++)
        {
            if (!virtualName.EqualsString(messageArgs[index]))
                continue;
            result ??= [.. messageArgs];
            result[index] = originalName ??= originalText[segment.OriginalStart..segment.OriginalEnd];
        }
        return result ?? messageArgs;
    }
}

public static partial class Ast
{
    public static Diagnostic NewDiagnosticFromSerialized(SourceFile file, TextRange loc, int code, DiagnosticCategory category, string messageKey, List<string> messageArgs, List<Diagnostic> messageChain, List<Diagnostic> relatedInformation, bool reportsUnnecessary, bool reportsDeprecated, bool skippedOnNoEmit) => NewDiagnosticFromSerialized(file, loc, code, category, messageKey, messageArgs, messageChain is null ? default(Slice<Diagnostic>) : Slice<Diagnostic>.FromArray(messageChain.ToArray()), relatedInformation is null ? default(Slice<Diagnostic>) : Slice<Diagnostic>.FromArray(relatedInformation.ToArray()), reportsUnnecessary, reportsDeprecated, skippedOnNoEmit);
    public static Diagnostic NewDiagnosticFromSerialized(SourceFile file, TextRange loc, int code, DiagnosticCategory category, string messageKey, List<string> messageArgs, Slice<Diagnostic> messageChain, Slice<Diagnostic> relatedInformation, bool reportsUnnecessary, bool reportsDeprecated, bool skippedOnNoEmit) => new()
    {
        file = file,
        loc = loc,
        code = code,
        category = category,
        messageKey = messageKey,
        messageArgs = messageArgs,
        messageChain = messageChain,
        relatedInformation = relatedInformation,
        reportsUnnecessary = reportsUnnecessary,
        reportsDeprecated = reportsDeprecated,
        skippedOnNoEmit = skippedOnNoEmit
    };
    // Text is ordinary .NET Unicode display text; serialized message arguments retain raw byte strings.
    public static Diagnostic NewDiagnosticFromText(SourceFile file, TextRange loc, int code, DiagnosticCategory category, string text, List<Diagnostic> messageChain, List<Diagnostic> relatedInformation, bool reportsUnnecessary, bool reportsDeprecated) => NewDiagnosticFromText(file, loc, code, category, text, messageChain is null ? default(Slice<Diagnostic>) : Slice<Diagnostic>.FromArray(messageChain.ToArray()), relatedInformation is null ? default(Slice<Diagnostic>) : Slice<Diagnostic>.FromArray(relatedInformation.ToArray()), reportsUnnecessary, reportsDeprecated);
    public static Diagnostic NewDiagnosticFromText(SourceFile file, TextRange loc, int code, DiagnosticCategory category, string text, Slice<Diagnostic> messageChain, Slice<Diagnostic> relatedInformation, bool reportsUnnecessary, bool reportsDeprecated) => new()
    {
        file = file,
        loc = loc,
        code = code,
        category = category,
        message = Diagnostics.NewAdHocMessage(text),
        messageChain = messageChain,
        relatedInformation = relatedInformation,
        reportsUnnecessary = reportsUnnecessary,
        reportsDeprecated = reportsDeprecated
    };
    public static Diagnostic NewExternalDiagnostic(SourceFile file, TextRange loc, string source, DiagnosticCategory category, int code, string messageText) => new()
    {
        file = file,
        loc = loc,
        source = source,
        category = category,
        code = code,
        messageText = messageText
    };
    public static Diagnostic NewDiagnostic(SourceFile file, TextRange loc, Message message, params object[] args) => new()
    {
        file = file,
        loc = loc,
        message = message,
        code = message.Code(),
        category = message.Category(),
        messageKey = message.Key(),
        reportsUnnecessary = message.ReportsUnnecessary(),
        reportsDeprecated = message.ReportsDeprecated(),
        messageArgs = args is { Length: > 0 } ? args.Select(StringifyArgument).ToList() : null
    };
    private static string StringifyArgument(object value) => value switch
    {
        string s => s,
        ByteStringValue text => text.Materialize() ?? "<nil>",
        ByteStringKey key => key.Value.Materialize() ?? "<nil>",
        bool b => b ? "true" : "false",
        null => "<nil>",
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "",
    };
}

// Selected exact comparator/collection closure from ast/diagnostic.go:258-533.
public sealed class DiagnosticsCollection
{
    private readonly object mu = new();
    private int count;
    private readonly Dictionary<ByteStringKey, Slice<Diagnostic>> fileDiagnostics = new(ByteStringKey.Comparer);
    private readonly HashSet<ByteStringKey> fileDiagnosticsSorted = new(ByteStringKey.Comparer);
    private Slice<Diagnostic> nonFileDiagnostics;
    private bool nonFileDiagnosticsSorted;
    private readonly Dictionary<diagnosticLocationKey, Diagnostic> diagnosticIndex = new();
    private readonly Dictionary<diagnosticLocationKey, Slice<Diagnostic>> diagnosticCollisions = new();
    private readonly record struct diagnosticLocationKey(ByteStringKey path, TextRange loc, int code);
    private static diagnosticLocationKey getDiagnosticLocationKey(Diagnostic diagnostic) => new(diagnostic.File()?.PathKeyKey() ?? default, diagnostic.Loc(), diagnostic.Code());
    public Diagnostic Add(Diagnostic diagnostic)
    {
        lock (mu)
        {
            var key = getDiagnosticLocationKey(diagnostic);
            if (diagnosticIndex.TryGetValue(key, out var existing))
            {
                if (Ast.EqualDiagnostics(existing, diagnostic))
                    return existing;
                if (diagnosticCollisions.TryGetValue(key, out var collisions))
                    foreach (var collision in collisions)
                        if (Ast.EqualDiagnostics(collision, diagnostic))
                            return collision;
            }

            if (existing is null)
                diagnosticIndex[key] = diagnostic;
            else
            {
                diagnosticCollisions.TryGetValue(key, out var collisions);
                diagnosticCollisions[key] = collisions.Append(diagnostic);
            }

            count++;
            if (diagnostic.File()is not null)
            {
                var path = diagnostic.File().PathKeyKey();
                fileDiagnostics.TryGetValue(path, out var diagnostics);
                fileDiagnostics[path] = diagnostics.Append(diagnostic);
                fileDiagnosticsSorted.Remove(path);
            }
            else
            {
                nonFileDiagnostics = nonFileDiagnostics.Append(diagnostic);
                nonFileDiagnosticsSorted = false;
            }

            return diagnostic;
        }
    }

    public Diagnostic Lookup(Diagnostic diagnostic)
    {
        lock (mu)
        {
            var diagnostics = diagnostic.File()is not null ? getDiagnosticsForFileLocked(diagnostic.File()) : getGlobalDiagnosticsLocked();
            var(index, found) = GoSlices.BinarySearchFunc(diagnostics, diagnostic, Ast.CompareDiagnostics);
            return found ? diagnostics[index] : null;
        }
    }

    public Slice<Diagnostic> GetGlobalDiagnostics()
    {
        lock (mu)
            return getGlobalDiagnosticsLocked();
    }

    private Slice<Diagnostic> getGlobalDiagnosticsLocked()
    {
        if (!nonFileDiagnosticsSorted)
        {
            SortStable(nonFileDiagnostics);
            nonFileDiagnosticsSorted = true;
        }

        return GoSlices.Clone(nonFileDiagnostics);
    }

    public Slice<Diagnostic> GetDiagnosticsForFile(SourceFile file)
    {
        lock (mu)
            return getDiagnosticsForFileLocked(file);
    }

    private Slice<Diagnostic> getDiagnosticsForFileLocked(SourceFile file)
    {
        var path = file.PathKeyKey();
        fileDiagnostics.TryGetValue(path, out var diagnostics);
        if (!fileDiagnosticsSorted.Contains(path))
        {
            SortStable(diagnostics);
            fileDiagnosticsSorted.Add(path);
        }

        return GoSlices.Clone(diagnostics);
    }

    public Slice<Diagnostic> GetDiagnostics()
    {
        lock (mu)
        {
            var diagnostics = Slice<Diagnostic>.Make(0, count);
            diagnostics = diagnostics.Append(nonFileDiagnostics);
            foreach (var diags in fileDiagnostics.Values)
                diagnostics = diagnostics.Append(diags);
            GoSlices.SortFunc(diagnostics, Ast.CompareDiagnostics);
            return diagnostics;
        }
    }

    private static void SortStable(Slice<Diagnostic> diagnostics) =>
        GoSlices.SortStableFunc(diagnostics, Ast.CompareDiagnostics);

}

public static partial class Ast
{
    private static ByteStringValue getDiagnosticPath(Diagnostic d) => d.File()?.FileNameGoValue() ?? default;
    public static bool EqualDiagnostics(Diagnostic d1, Diagnostic d2) => ReferenceEquals(d1, d2) || EqualDiagnosticsNoRelatedInfo(d1, d2) && equalDiagnosticSlices(d1.RelatedInformation(), d2.RelatedInformation(), EqualDiagnostics);
    public static bool EqualDiagnosticsNoRelatedInfo(Diagnostic d1, Diagnostic d2)
    {
        if (ReferenceEquals(d1, d2))
            return true;
        return getDiagnosticPath(d1).EqualsValue(getDiagnosticPath(d2)) && d1.Loc() == d2.Loc() && d1.Code() == d2.Code() && d1.Category() == d2.Category() && d1.Source() == d2.Source() && getDiagnosticMessageIdentity(d1) == getDiagnosticMessageIdentity(d2) && compareDiagnosticArgs(d1.MessageArgs(), d2.MessageArgs()) == 0 && equalDiagnosticSlices(d1.MessageChain(), d2.MessageChain(), equalMessageChain);
    }

    private static bool equalDiagnosticSlices(Slice<Diagnostic> x, Slice<Diagnostic> y, Func<Diagnostic, Diagnostic, bool> equal)
    {
        if (GoRuntime.Len(x) != GoRuntime.Len(y))
            return false;
        for (int i = 0; i < GoRuntime.Len(x); i++)
            if (!equal(x[i], y[i]))
                return false;
        return true;
    }

    private static string getDiagnosticMessageIdentity(Diagnostic diagnostic)
    {
        if (diagnostic.MessageText() != "")
            return diagnostic.MessageText();
        if (diagnostic.message is not null && diagnostic.Code() == -1)
            return diagnostic.message.String();
        return diagnostic.MessageKey();
    }

    private static bool equalMessageChain(Diagnostic c1, Diagnostic c2) => ReferenceEquals(c1, c2) || c1.Code() == c2.Code() && compareDiagnosticArgs(c1.MessageArgs(), c2.MessageArgs()) == 0 && equalDiagnosticSlices(c1.MessageChain(), c2.MessageChain(), equalMessageChain);
    private static int compareDiagnosticArgs(List<string> x, List<string> y)
    {
        for (int i = 0; i < Math.Min(GoRuntime.Len(x), GoRuntime.Len(y)); i++)
        {
            int c = StringComparer.Ordinal.Compare(x[i], y[i]);
            if (c != 0)
                return c;
        }

        return GoRuntime.Len(x) - GoRuntime.Len(y);
    }

    private static int compareMessageChainSize(Slice<Diagnostic> c1, Slice<Diagnostic> c2)
    {
        int c = GoRuntime.Len(c2) - GoRuntime.Len(c1);
        if (c != 0)
            return c;
        for (int i = 0; i < GoRuntime.Len(c1); i++)
        {
            c = compareMessageChainSize(c1[i].MessageChain(), c2[i].MessageChain());
            if (c != 0)
                return c;
        }

        return 0;
    }

    private static int compareMessageChainContent(Slice<Diagnostic> c1, Slice<Diagnostic> c2)
    {
        for (int i = 0; i < GoRuntime.Len(c1); i++)
        {
            int c = compareDiagnosticArgs(c1[i].MessageArgs(), c2[i].MessageArgs());
            if (c != 0)
                return c;
            if (!c1[i].MessageChain().IsNil)
            {
                c = compareMessageChainContent(c1[i].MessageChain(), c2[i].MessageChain());
                if (c != 0)
                    return c;
            }
        }

        return 0;
    }

    private static int compareRelatedInfo(Slice<Diagnostic> r1, Slice<Diagnostic> r2)
    {
        int c = GoRuntime.Len(r2) - GoRuntime.Len(r1);
        if (c != 0)
            return c;
        for (int i = 0; i < GoRuntime.Len(r1); i++)
        {
            c = CompareDiagnostics(r1[i], r2[i]);
            if (c != 0)
                return c;
        }

        return 0;
    }

    public static int CompareDiagnostics(Diagnostic d1, Diagnostic d2)
    {
        if (ReferenceEquals(d1, d2))
            return 0;
        int c = ByteStringKey.FromValue(getDiagnosticPath(d1)).CompareTo(ByteStringKey.FromValue(getDiagnosticPath(d2)));
        if (c != 0)
            return c;
        c = d1.Pos() - d2.Pos();
        if (c != 0)
            return c;
        c = d1.End() - d2.End();
        if (c != 0)
            return c;
        c = d1.Code() - d2.Code();
        if (c != 0)
            return c;
        c = (int)d1.Category() - (int)d2.Category();
        if (c != 0)
            return c;
        c = StringComparer.Ordinal.Compare(d1.Source(), d2.Source());
        if (c != 0)
            return c;
        c = StringComparer.Ordinal.Compare(getDiagnosticMessageIdentity(d1), getDiagnosticMessageIdentity(d2));
        if (c != 0)
            return c;
        c = compareDiagnosticArgs(d1.MessageArgs(), d2.MessageArgs());
        if (c != 0)
            return c;
        c = compareMessageChainSize(d1.MessageChain(), d2.MessageChain());
        if (c != 0)
            return c;
        c = compareMessageChainContent(d1.MessageChain(), d2.MessageChain());
        if (c != 0)
            return c;
        return compareRelatedInfo(d1.RelatedInformation(), d2.RelatedInformation());
    }

    // Upstream diagnostic.go:237.
    public static Diagnostic NewCompilerDiagnostic(Message message, params object[] args) => NewDiagnostic(null, new(-1, -1), message, args);
    // Upstream diagnostic.go:230.
    public static Diagnostic NewDiagnosticChain(Diagnostic chain, Message message, params object[] args) => chain is not null ? NewDiagnostic(chain.file, chain.loc, message, args).AddMessageChain(chain).SetRelatedInfo(chain.relatedInformation) : NewDiagnostic(null, new(0, 0), message, args);
}
