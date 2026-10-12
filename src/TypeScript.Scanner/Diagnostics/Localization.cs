// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Direct locale lookup/cache counterpart of diagnostics.go:65–126.
using System.Collections.Concurrent;
using System.IO.Compression;
using TypeScript.Lsp.Protocol;
using TypeScript.Locale;
using DiagnosticLocale = TypeScript.Locale.Locale;
using TypeScript.Locale.XText;
using TypeScript.ProjectSystem.Logging;

namespace TypeScript.Scanner;
internal static class DiagnosticLocalization
{
    private sealed record CatalogEntry(Dictionary<string, string>? Messages);
    private static readonly ConcurrentDictionary<DiagnosticLocale, CatalogEntry> cache = new();
    private static readonly Lazy<LanguageParser.Matcher> matcher = new(() =>
    {
        string[] names = ["en", "zh-CN", "zh-TW", "cs-CZ", "de-DE", "es-ES", "fr-FR", "it-IT", "ja-JP", "ko-KR", "pl-PL", "pt-BR", "ru-RU", "tr-TR"];
        var tags = new LanguageParser.Tag[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            tags[i] = LanguageParser.Parse(names[i]).Tag;
            LanguageParser.CanonicalizeDefault(tags[i]);
        }

        return new LanguageParser.Matcher(tags);
    });
    private static readonly string[] catalogNames = ["", "zh-CN", "zh-TW", "cs-CZ", "de-DE", "es-ES", "fr-FR", "it-IT", "ja-JP", "ko-KR", "pl-PL", "pt-BR", "ru-RU", "tr-TR"];
    private static readonly Lazy<Dictionary<string, string>?>[] catalogs = CreateCatalogOwners();
    private static Lazy<Dictionary<string, string>?>[] CreateCatalogOwners()
    {
        var owners = new Lazy<Dictionary<string, string>?>[catalogNames.Length - 1];
        for (int i = 0; i < owners.Length; i++)
        {
            string name = catalogNames[i + 1];
            owners[i] = new(() => LoadLocaleData(name));
        }

        return owners;
    }

    internal static Dictionary<string, string>? GetLocalizedMessages(DiagnosticLocale locale)
    {
        if (locale == Locales.Default)
            return null;
        if (cache.TryGetValue(locale, out CatalogEntry? cached))
            return cached.Messages;
        Dictionary<string, string>? messages = null;
        var tag = LanguageParser.Parse(locale.String()).Tag;
        var(_, index, confidence) = matcher.Value.Match(tag);
        if (confidence >= LanguageParser.Confidence.Low && index > 0 && index < catalogNames.Length)
            messages = catalogs[index - 1].Value;
        // Go Load/compute/Store permits duplicate concurrent computation; lazy
        // per-catalog owners still decode once, including a cached null match.
        cache[locale] = new CatalogEntry(messages);
        return messages;
    }

    private static Dictionary<string, string>? LoadLocaleData(string name)
    {
        using Stream compressed = typeof(DiagnosticLocalization).Assembly.GetManifestResourceStream("TypeScript.Diagnostics.Locale." + name + ".json.gz") ?? throw new InvalidOperationException("Missing diagnostic locale resource: " + name);
        using GZipStream reader = new(compressed, CompressionMode.Decompress);
        using MemoryStream decoded = new();
        reader.CopyTo(decoded);
        var bytes = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(decoded.GetBuffer())).Slice(0, checked((int)decoded.Length));
        ProtocolDecoder decoder = new(bytes);
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            decoder.Finish();
            return null;
        }

        decoder.ReadObjectStart("map[string]string");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (decoder.PeekKind() != '}')
        {
            string key = decoder.ReadDecodedPropertyName().Materialize()!;
            string value = decoder.ReadString().Materialize() ?? "";
            result[key] = value;
        }

        decoder.ReadToken((byte)'}');
        decoder.Finish();
        return result;
    }

    internal static string Localize(DiagnosticLocale locale, Message? message, string key, IReadOnlyList<string> args)
    {
        message ??= Diagnostics.KeyToMessage(key);
        if (message == null)
            throw new InvalidOperationException("Unknown diagnostic message: " + key);
        string text = GoString.ToUtf8(message.String());
        if (GetLocalizedMessages(locale)is { } localized && localized.TryGetValue(message.Key(), out string? translation))
            text = translation;
        return Format(text, args);
    }

    internal static string[] StringifyArgs(object? [] args)
    {
        if (args.Length == 0)
            return[];
        var result = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
            result[i] = args[i] is string text ? text : LogFormat.Sprintf("%v", args[i]);
        return result;
    }

    internal static string Format(string text, IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return text;
        // Go SameMap retains the argument header if every byte string is valid.
        string[]? mapped = null;
        for (int i = 0; i < args.Count; i++)
        {
            string value = ToValidUtf8(args[i]);
            if (value != args[i] && mapped == null)
                mapped = args.ToArray();
            if (mapped != null)
                mapped[i] = value;
        }

        args = mapped ?? args;
        var builder = new GoBuilder();
        for (int i = 0; i < text.Length;)
        {
            if (text[i] == '{')
            {
                int end = i + 1;
                while (end < text.Length && text[end] is >= '0' and <= '9')
                    end++;
                if (end > i + 1 && end < text.Length && text[end] == '}')
                {
                    if (!long.TryParse(text.AsSpan(i + 1, end - i - 1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long index) || index >= args.Count)
                        throw new InvalidOperationException("Invalid formatting placeholder");
                    builder.WriteString(args[(int)index]);
                    i = end + 1;
                    continue;
                }
            }

            builder.WriteByte((byte)text[i++]);
        }

        return builder.String();
    }

    private static string ToValidUtf8(string value)
    {
        GoBuilder? builder = null;
        bool invalidRun = false;
        for (int i = 0; i < value.Length;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(value.AsSpan(i));
            if (rune == Utf8.RuneError && size == 1)
            {
                if (builder == null)
                {
                    builder = new GoBuilder();
                    builder.WriteString(value[..i]);
                }

                if (!invalidRun)
                    builder.WriteString("\xef\xbf\xbd");
                invalidRun = true;
            }
            else
            {
                invalidRun = false;
                builder?.WriteString(value.Substring(i, size));
            }

            i += size;
        }

        return builder?.String() ?? value;
    }
}
