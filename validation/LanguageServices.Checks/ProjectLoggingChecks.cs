// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original logging/logtree_test.go entries; meaningful authored observations remain separate.
using System.Text.RegularExpressions;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class ProjectLoggingChecks
{
    public static readonly string[] Names = ["TestLogTreeImplementsLogger", "TestLogTree"];
    public static readonly string[] AuthoredNames = ["RawLoggingAndFormats", "TreeOwnershipAndConcurrentPublication"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static int RunAuthoredControls()
    {
        foreach (var name in AuthoredNames)
            RunAuthored(name);
        return AuthoredNames.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestLogTreeImplementsLogger":
                ILogger logger = new LogTree();
                _ = logger;
                break;
            case "TestLogTree":
                break; // Pinned original has an empty body, not a behavioral assertion.
            default:
                throw new ArgumentException(name);
        }
    }

    public static void RunAuthored(string name)
    {
        switch (name)
        {
            case "RawLoggingAndFormats":
                RawLoggingAndFormats();
                break;
            case "TreeOwnershipAndConcurrentPublication":
                TreeOwnershipAndConcurrentPublication();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException("authored logging assertion failed");
    }

    private static void RawLoggingAndFormats()
    {
        var logger = LogCollector.NewTestLogger();
        string raw = GoString.ToUtf8("café");
        logger.Logf("%q %6d %v %v", raw, 12, true, new GoLogDuration(1_234_000));
        string prefix = Logger.FormatTime(DateTimeOffset.FromUnixTimeSeconds(1349085672).ToLocalTime());
        Check(logger.String() == prefix + " " + GoQuote.Quote(raw) + "     12 true 1.234ms\n");
        Check(logger.Verbose()is null);
        logger.SetVerbose(true);
        Check(ReferenceEquals(logger.Verbose(), logger));
        var nop = Logger.NewNopLogger();
        nop.Logf("%not-used", new object ());
        nop.SetVerbose(true);
        Check(!nop.IsVerbose());
        Check(nop.Verbose()is null);
        Check(LogFormat.Sprint(1, 2, "x", 3) == "1 2x3");
        // Actual ATA/project value headers retain raw-byte text, nil/empty and offsets.
        var borrowed = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(GoString.Bytes(raw)));
        Check(LogFormat.Sprintf("%v", Slice<ByteStringValue>.FromArray([borrowed, ByteStringValue.FromString("x")])) == "[" + raw + " x]");
        Check(LogFormat.Sprintf("%v", default(Slice<ByteStringValue>)) == "[]");
        Check(LogFormat.Sprintf("%v", Slice<ByteStringValue>.Make(0)) == "[]");
        Check(LogFormat.Sprintf("%v", Slice<RootedFilePath>.FromArray([RootedFilePath.FromGoString("/a.ts")])) == "[/a.ts]");
        Check(LogFormat.Sprintf("%v", Slice<RootedPath>.FromArray([RootedPath.FromGoString("/a")])) == "[/a]");
        Check(new GoLogDuration(0).GoLogString() == "0s");
        Check(new GoLogDuration(-1_000).GoLogString() == "-" + GoString.ToUtf8("1µs"));
        Check(new GoLogDuration(3_600_000_000_000).GoLogString() == "1h0m0s");
    }

    private static void TreeOwnershipAndConcurrentPublication()
    {
        var tree = LogTree.NewLogTree("snapshot");
        tree.SetVerbose(true);
        var child = tree.Fork("project");
        Check(child.IsVerbose());
        child.Log("file");
        var embedded = LogTree.NewLogTree("worker");
        embedded.Log("result");
        tree.Embed(embedded);
        string output = tree.String();
        // Runtime timestamps are actual clock observations; compare their specified
        // shape and message/indent/order without inventing an upstream fixed clock.
        output = Regex.Replace(output, @"\[\d\d:\d\d:\d\d\.\d\d\d\]", "[time]");
        Check(output == "======== snapshot ========\n[time] project\n\t[time] file\n[time] worker\n\t[time] result\n");
        bool threw = false;
        try
        {
            child.String();
        }
        catch (InvalidOperationException error)
        {
            threw = error.Message == "can only call String on root LogTree";
        }

        Check(threw);
        var concurrent = LogTree.NewLogTree("concurrent");
        Thread[] writers = Enumerable.Range(0, 10).Select(i => new Thread(() => concurrent.Logf("worker %d", i))).ToArray();
        foreach (var writer in writers)
            writer.Start();
        foreach (var writer in writers)
            writer.Join();
        output = Regex.Replace(concurrent.String(), @"\[\d\d:\d\d:\d\d\.\d\d\d\]", "[time]");
        string[] lines = output.Split('\n');
        Check(lines.Length == 12);
        var actual = new HashSet<string>(lines[1..^1], StringComparer.Ordinal);
        for (int i = 0; i < 10; i++)
            Check(actual.Contains("[time] worker " + i));
    }
}
