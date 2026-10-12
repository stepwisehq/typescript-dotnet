// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Full remaining internal/astnav/tokens_test.go originals. Accepted unit/JSON/JSDoc
// checks remain unchanged and are consumed here. JavaScript oracle uses locked6.0.3.
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using Kind = TypeScript.Scanner.Kind;

namespace LanguageServices.Checks;
public static partial class AstNavOriginalChecks
{
    public static readonly string[] Names = ["TestGetTokenAtPosition", "TestGetTouchingPropertyName", "TestFindPrecedingToken", "TestFindNextToken"];
    private sealed record Token(string Kind, int Pos, int End);
    private readonly record struct Diff(Token? Go, Token? TS);
    public static int RunAll()
    {
        List<Exception> failures = new();
        foreach (string name in Names)
            try
            {
                RunOriginal(name);
            }
            catch (Exception error)
            {
                failures.Add(new InvalidOperationException(name, error));
            }

        if (failures.Count != 0)
            throw new AggregateException(failures);
        return Names.Length;
    }

    public static void RunOriginal(string name)
    {
        if (!Names.Contains(name))
            throw new ArgumentException(name);
        string channel = name[4..];
        // FindNextToken has only its Go JSON baseline in the pinned source.
        if (name == "TestFindNextToken")
        {
            AstNavBaselineChecks.Run(channel);
            return;
        }

        string? node = FindNode();
        if (node is null)
        {
            Console.WriteLine($"ORIGINAL_SKIP {name}: Node.js not found");
            return;
        }

        List<Exception> failures = new();
        void Sibling(string label, Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                failures.Add(new InvalidOperationException($"{name}/{label}", error));
            }
        }

        Sibling("baseline", () => RunDifferential(channel, node));
        Sibling("go baseline json", () => AstNavBaselineChecks.Run(channel));
        if (name == "TestGetTokenAtPosition")
        {
            Sibling("JSDoc type assertion", () =>
            {
                var file = Parser.ParseSourceFile(new SourceFileParseOptions("/test.js", "/test.js"), "function foo(x) {\n    const s = /**@type {string}*/(x)\n}", ScriptKind.JS);
                var token = AstNav.GetTouchingPropertyName(file, 52);
                if (token is null)
                    throw new InvalidOperationException("Expected to get a token, got nil");
                if (token.Kind != Kind.Identifier && token.Kind != Kind.ParenthesizedExpression)
                    throw new InvalidOperationException("Expected identifier or parenthesized expression");
            });
            Sibling("JSDoc type assertion with comment", () =>
            {
                var file = Parser.ParseSourceFile(new SourceFileParseOptions("/test.js", "/test.js"), "function foo(x) {\n    const s = /**@type {string}*/(x)  // Go-to-definition on x causes panic\n}", ScriptKind.JS);
                if (AstNav.GetTouchingPropertyName(file, 52)is null)
                    throw new InvalidOperationException("Expected to get a token");
            });
            Sibling("pointer equality", () =>
            {
                var file = Parser.ParseSourceFile(new SourceFileParseOptions("/file.ts", "/file.ts"), "\n\t\t\tfunction foo() {\n\t\t\t\treturn 0;\n\t\t\t}\n\t\t", ScriptKind.TS);
                if (!ReferenceEquals(AstNav.GetTokenAtPosition(file, 0), AstNav.GetTokenAtPosition(file, 0)))
                    throw new InvalidOperationException("Original token pointer equality differs");
            });
        }

        if (failures.Count != 0)
            throw new AggregateException(failures);
    }

    private static void RunDifferential(string name, string node)
    {
        string root = FindRoot();
        string tsPath = Path.Combine(root, "upstream/TypeScript/node_modules/typescript/lib/typescript.js");
        if (!File.Exists(tsPath))
            throw new FileNotFoundException("Prepare exact locked6.0.3 runtime with tools/prepare-astnav-js-environment.py; no substitute TypeScript version.", tsPath);
        using (Stream runtime = File.OpenRead(tsPath))
            if (Convert.ToHexString(SHA256.HashData(runtime)).ToLowerInvariant() != "569177652966bd528c319171c7dd22860dbf72bde116cbc4f644f1d02bb12e39")
                throw new InvalidOperationException("AstNav requires the exact integrity-verified locked6.0.3 runtime");
        byte[] bytes = File.ReadAllBytes(Path.Combine(root, "upstream/TypeScript/tsc/testdata/fixtures/services/mapCode.ts"));
        int count = bytes.Length + (name == "FindPrecedingToken" ? 1 : 0);
        Token? [] ts = GetTSTokens(node, tsPath, bytes, count, name);
        if (ts.Length != count)
            throw new InvalidOperationException("Original Node helper returned a different position count");
        SourceFile file = Parser.ParseSourceFile(new SourceFileParseOptions("/file.ts", "/file.ts"), bytes, ScriptKind.TS);
        using var output = new MemoryStream();
        int start = 0, end = 0;
        Diff current = default;
        for (int position = 0; position < ts.Length; position++)
        {
            Node? token = name switch
            {
                "GetTokenAtPosition" => AstNav.GetTokenAtPosition(file, position),
                "GetTouchingPropertyName" => AstNav.GetTouchingPropertyName(file, position),
                _ => AstNav.FindPrecedingToken(file, position)};
            Diff next = new(ToInfo(token), ts[position]);
            if (current != next)
            {
                if (current.Go != current.TS)
                    WriteRangeDiff(output, file, bytes, current, start, end, position);
                current = next;
                start = position;
                end = position;
            }

            end = position;
        }

        if (current.Go != current.TS)
            WriteRangeDiff(output, file, bytes, current, start, end, ts.Length - 1);
        string reference = Path.Combine(root, $"upstream/TypeScript/tsc/testdata/baselines/reference/astnav/{name}.mapCode.ts.baseline.txt");
        if (output.Length == 0)
        {
            if (File.Exists(reference))
                throw new InvalidOperationException($"Original {name}: expected no baseline but reference exists");
        }
        else if (!File.Exists(reference) || !output.ToArray().AsSpan().SequenceEqual(File.ReadAllBytes(reference)))
        {
            string local = Path.Combine(root, $"artifacts/language-services/astnav/{name}.mapCode.ts.baseline.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(local)!);
            File.WriteAllBytes(local, output.ToArray());
            throw new InvalidOperationException($"Original {name} TS differential baseline differs; actual {local}");
        }
    }

    private static Token? ToInfo(Node? node)
    {
        if (node is null)
            return null;
        string kind = node.Kind.String();
        int prefix = kind.IndexOf("Kind", StringComparison.Ordinal);
        if (prefix >= 0)
            kind = kind.Remove(prefix, 4);
        if (kind == "EndOfFile")
            kind = "EndOfFileToken";
        return new(kind, node.Pos(), node.End());
    }

    private static Token? [] GetTSTokens(string node, string tsPath, byte[] source, int count, string name)
    {
        string directory = Path.Combine(Path.GetTempPath(), "ts-dotnet-astnav-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "file.ts"), source);
            // Original positions are every byte position, including EOF only for preceding.
            File.WriteAllText(Path.Combine(directory, "positions.json"), "[" + string.Join(",", Enumerable.Range(0, count)) + "]");
            File.WriteAllText(Path.Combine(directory, "script.mjs"), name switch
            {
                "GetTokenAtPosition" => Script0,
                "GetTouchingPropertyName" => Script1,
                _ => Script2
            });
            string loader = "import script from './script.mjs';\nimport * as ts from " + ("\"" + JsonEncodedText.Encode(new Uri(tsPath).AbsoluteUri).ToString() + "\"") + ";\nprocess.stdout.write(JSON.stringify(await script(ts)));";
            File.WriteAllText(Path.Combine(directory, "loader.mjs"), loader);
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(node)
                {
                    WorkingDirectory = directory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.StartInfo.ArgumentList.Add(Path.Combine(directory, "loader.mjs"));
            if (!process.Start())
                throw new InvalidOperationException("Node helper failed to start");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            string result = stdout.GetAwaiter().GetResult();
            string errors = stderr.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Original Node helper exit {process.ExitCode}: {errors}");
            using JsonDocument document = JsonDocument.Parse(result + errors);
            var values = document.RootElement.EnumerateArray();
            Token? [] tokens = new Token? [document.RootElement.GetArrayLength()];
            int i = 0;
            foreach (JsonElement value in values)
                tokens[i++] = value.ValueKind == JsonValueKind.Null ? null : new Token(value.GetProperty("kind").GetString()!, value.GetProperty("pos").GetInt32(), value.GetProperty("end").GetInt32());
            return tokens;
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static void WriteRangeDiff(Stream output, SourceFile file, byte[] bytes, Diff diff, int rangeStart, int rangeEnd, int position)
    {
        Slice<int> lines = file.ECMALineMap();
        int tsStart = diff.TS?.Pos ?? position, tsEnd = diff.TS?.End ?? position;
        int goStart = diff.Go?.Pos ?? position, goEnd = diff.Go?.End ?? position;
        int tsStartLine = LineOf(tsStart, lines), tsEndLine = LineOf(tsEnd, lines);
        int goStartLine = LineOf(goStart, lines), goEndLine = LineOf(goEnd, lines);
        int[] markerLines = [tsStartLine, tsEndLine, goStartLine, goEndLine];
        Array.Sort(markerLines);
        int contextStart = Math.Max(0, Math.Min(tsStartLine, goStartLine) - 2);
        int contextEnd = Math.Min(lines.Length - 1, Math.Max(tsEndLine, goEndLine) + 2);
        int digits = contextEnd.ToString(CultureInfo.InvariantCulture).Length;
        void Write(string value) => output.Write(Encoding.UTF8.GetBytes(value));
        if (output.Length > 0)
            Write("\n\n");
        Write($"〚Positions: [{rangeStart}, {rangeEnd}]〛\n");
        Write(diff.TS is null ? "【TS: nil】\n" : $"【TS: {diff.TS.Kind} [{tsStart}, {tsEnd})】\n");
        Write(diff.Go is null ? "《Go: nil》\n" : $"《Go: {diff.Go.Kind} [{goStart}, {goEnd})》\n");
        for (int line = contextStart; line <= contextEnd; line++)
        {
            // slices.BinarySearch returns the first equal/insertion index.
            int index = 0;
            while (index < markerLines.Length && markerLines[index] < line)
                index++;
            if (index != 0 && index != markerLines.Length && line - markerLines[index - 1] > 5 && markerLines[index] - line > 5)
            {
                int skipTo = markerLines[index] - 5;
                Write(new string (' ', digits) + $" │........ {skipTo - line + 1} lines omitted ........\n");
                line = skipTo;
            }

            Write((line + 1).ToString(CultureInfo.InvariantCulture).PadLeft(digits) + " │");
            int end = line < lines.Length - 1 ? lines[line + 1] : bytes.Length + 1;
            for (int pos = lines[line]; pos < end; pos++)
            {
                if (pos == rangeEnd + 1)
                    Write("〛");
                if (diff.TS is not null && pos == tsEnd)
                    Write("】");
                if (diff.Go is not null && pos == goEnd)
                    Write("》");
                if (diff.Go is not null && pos == goStart)
                    Write("《");
                if (diff.TS is not null && pos == tsStart)
                    Write("【");
                if (pos == rangeStart)
                    Write("〚");
                if (pos < bytes.Length)
                    output.WriteByte(bytes[pos]);
            }
        }
    }

    private static int LineOf(int position, Slice<int> lines)
    {
        int low = 0, high = lines.Length;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (lines[middle] > position)
                high = middle;
            else
                low = middle + 1;
        }

        return Math.Max(low - 1, 0);
    }

    private static string? FindNode()
    {
        foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            string path = Path.Combine(folder, OperatingSystem.IsWindows() ? "node.exe" : "node");
            if (!File.Exists(path))
                continue;
            if (OperatingSystem.IsWindows())
                return path;
            const UnixFileMode executable = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            if ((File.GetUnixFileMode(path) & executable) != 0)
                return path;
        }

        return null;
    }

    private static string FindRoot()
    {
        foreach (string start in new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        }

        )
            for (DirectoryInfo? directory = new(start); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "START-HERE.md")))
                    return directory.FullName;
        throw new DirectoryNotFoundException("Repository root required for unchanged original AstNav inputs");
    }
}
