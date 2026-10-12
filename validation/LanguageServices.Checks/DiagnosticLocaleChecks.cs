// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original diagnostics_test.go runtime assertions; generator tooling is separately recorded.
using TypeScript.Json;
using TypeScript.Locale;
using TypeScript.Scanner;
using DiagnosticLocale = TypeScript.Locale.Locale;

namespace LanguageServices.Checks;
public static class DiagnosticLocaleChecks
{
    public static string[] Names => ["TestLocalize", "TestLocalize_ByKey", "TestLocaleFiles", "TestLocaleFilesIgnoreStaleDiagnostics"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestLocalize":
                TestLocalize();
                break;
            case "TestLocalize_ByKey":
                TestLocalizeByKey();
                break;
            case "TestLocaleFiles":
                TestLocaleFiles();
                break;
            case "TestLocaleFilesIgnoreStaleDiagnostics":
                TestLocaleFilesIgnoreStaleDiagnostics();
                break;
            default:
                throw new ArgumentException("Unknown original diagnostic locale function", nameof(name));
        }
    }

    public static int RunAll()
    {
        foreach (string name in Names)
            Run(name);
        return Names.Length;
    }

    private static DiagnosticLocale Locale(string name) => name == "und" ? Locales.Default : Locales.Parse(name).Locale;
    private static void Equal<T>(T actual, T expected, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual);
    }

    private static void TestLocalize()
    {
        (string Name, Message Message, string Locale, object? [] Args, string Expected)[] cases = [("english default", Diagnostics.Identifier_expected, "en", [], "Identifier expected."), ("undefined locale uses english", Diagnostics.Identifier_expected, "und", [], "Identifier expected."), ("with single argument", Diagnostics.X_0_expected, "en", [")"], "')' expected."), ("with multiple arguments", Diagnostics.The_parser_expected_to_find_a_1_to_match_the_0_token_here, "en", ["{", "}"], "The parser expected to find a '}' to match the '{' token here."), ("fallback to english for unknown locale", Diagnostics.Identifier_expected, "af-ZA", [], "Identifier expected."), ("german", Diagnostics.Identifier_expected, "de-DE", [], "Es wurde ein Bezeichner erwartet."), ("french", Diagnostics.Identifier_expected, "fr-FR", [], "Identificateur attendu."), ("spanish", Diagnostics.Identifier_expected, "es-ES", [], "Se esperaba un identificador."), ("japanese", Diagnostics.Identifier_expected, "ja-JP", [], "識別子が必要です。"), ("chinese simplified", Diagnostics.Identifier_expected, "zh-CN", [], "应为标识符。"), ("korean", Diagnostics.Identifier_expected, "ko-KR", [], "식별자가 필요합니다."), ("russian", Diagnostics.Identifier_expected, "ru-RU", [], "Ожидался идентификатор."), ("german with args", Diagnostics.X_0_expected, "de-DE", [")"], "\")\" wurde erwartet."), ];
        foreach (var item in cases)
            Equal(item.Message.Localize(Locale(item.Locale), item.Args), GoString.ToUtf8(item.Expected), item.Name);
    }

    private static void TestLocalizeByKey()
    {
        Equal(Diagnostics.Localize(Locale("en"), null, "Identifier_expected_1003", []), "Identifier expected.", "by key without args");
        Equal(Diagnostics.Localize(Locale("en"), null, "_0_expected_1005", [")"]), "')' expected.", "by key with args");
    }

    private static string FindRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "START-HERE.md")))
                return directory.FullName;
        throw new InvalidOperationException("Cannot find upstream diagnostic locale fixtures");
    }

    private static void TestLocaleFiles()
    {
        string root = FindRoot();
        string[] files = Directory.GetFiles(Path.Combine(root, "upstream/TypeScript/tsc/internal/diagnostics/loc"), "*.generated.json");
        if (files.Length == 0)
            throw new InvalidOperationException("Original locale glob returned no files");
        foreach (string path in files)
        {
            string name = Path.GetFileName(path)[..^".generated.json".Length];
            ValidateLocaleFile(GoString.FromBytes(File.ReadAllBytes(path)), DiagnosticLocalization.GetLocalizedMessages(Locale(name)));
        }
    }

    private static void ValidateLocaleFile(string data, Dictionary<string, string>? runtimeMessages)
    {
        var(value, error) = JsonCodec.ParseDictionary(data);
        if (error != null)
            throw error;
        if (value is not Dictionary<string, object?> handback)
            throw new InvalidOperationException("Original locale handback is not a map");
        var active = new Dictionary<string, string>(StringComparer.Ordinal);
        var ordered = new List<KeyValuePair<string, string>>();
        foreach (string key in handback.Keys.Order(StringComparer.Ordinal))
        {
            if (handback[key] is not string text)
                throw new InvalidOperationException("Original locale value is not a string");
            Message? message = Diagnostics.KeyToMessage(key);
            if (message != null)
            {
                var localized = PlaceholderSet(text);
                var english = PlaceholderSet(message.String());
                Equal(localized.Count, english.Count, "placeholder mismatch for " + key);
                foreach (string placeholder in english)
                    if (!localized.Contains(placeholder))
                        throw new InvalidOperationException("localized diagnostic " + key + " is missing placeholder " + placeholder);
                active[key] = text;
            }

            ordered.Add(new(key, text));
        }

        // Exact demanded json.MarshalIndent ordered string-map observer, then the
        // original LF-to-CRLF conversion. Strings use the accepted Go quotation.
        GoBuilder writer = new();
        writer.WriteString("{");
        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0)
                writer.WriteString(",");
            writer.WriteString("\r\n  ");
            JsonCodec.QuoteToBuilder(writer, ByteStringValue.FromString(ordered[i].Key));
            writer.WriteString(": ");
            JsonCodec.QuoteToBuilder(writer, ByteStringValue.FromString(ordered[i].Value));
        }

        if (ordered.Count > 0)
            writer.WriteString("\r\n");
        writer.WriteString("}");
        Equal(data, writer.String(), "handback must use sorted keys and canonical formatting");
        Equal(runtimeMessages?.Count ?? 0, active.Count, "active runtime catalog size");
        foreach (var(key, expected)in active)
        {
            if (runtimeMessages == null || !runtimeMessages.TryGetValue(key, out string? actual))
                throw new InvalidOperationException("Missing active runtime translation: " + key);
            Equal(actual, expected, "active runtime translation " + key);
        }
    }

    private static HashSet<string> PlaceholderSet(string text)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '{')
            {
                int end = i + 1;
                while (end < text.Length && text[end] is >= '0' and <= '9')
                    end++;
                if (end > i + 1 && end < text.Length && text[end] == '}')
                    result.Add(text[i..(end + 1)]);
            }

        return result;
    }

    private static void TestLocaleFilesIgnoreStaleDiagnostics()
    {
        string data = "{\r\n  \"Identifier_expected_1003\": \"Known translation.\",\r\n  \"Removed_diagnostic_99999\": \"Stale translation.\"\r\n}";
        ValidateLocaleFile(data, new(StringComparer.Ordinal) { ["Identifier_expected_1003"] = "Known translation." });
        ValidateLocaleFile("{\r\n  \"Removed_diagnostic_99999\": \"Stale translation.\"\r\n}", null);
    }
}
