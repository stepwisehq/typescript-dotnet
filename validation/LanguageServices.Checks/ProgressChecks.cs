// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/lsp/progress_test.go TestProgress and all eleven subtests.
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using DiagnosticMessage = TypeScript.Scanner.Message;

namespace LanguageServices.Checks;
public static class ProgressChecks
{
    public static IEnumerable<string> Names => ["StartFinishBeforeDelay", "ShowsAfterDelay", "ReportsMultipleOperations", "RefCounting", "NewTokenAfterEnd", "StartBeforeDelayThenMoreAfterDelay", "FinishWithNoActiveToken", "ShutdownDuringStartAndFinish", "ShutdownWithActiveTimer", "ZeroDelay", "FinishBeforeDelayNoBegun"];

    public static int RunAll()
    {
        var failures = new List<Exception>();
        foreach (var name in Names)
            try
            {
                Run(name);
            }
            catch (Exception error)
            {
                failures.Add(new InvalidOperationException(name, error));
            }

        if (failures.Count != 0)
            throw new AggregateException(failures);
        return 1;
    }

    private readonly record struct ProgressCall(string Method, ByteStringValue Token, ByteStringValue Title, ByteStringValue Message);
    private sealed class Reporter(CancellationToken done) : IProgressReporter
    {
        private readonly object gate = new();
        private Slice<ProgressCall> calls;
        public CancellationToken Done => done;

        public ByteStringValue Localize(DiagnosticMessage message, params object? [] arguments) => ByteStringValue.FromString(message.Localize(TypeScript.Locale.Locales.Default, arguments!));
        public void CreateWorkDoneProgress(ByteStringValue token)
        {
            lock (gate)
                calls = calls.Append(new ProgressCall("create", token, default, default));
        }

        public void SendProgress(ByteStringValue token, WorkDoneProgressBeginOrReportOrEnd value)
        {
            lock (gate)
            {
                if (value.Begin is not null)
                    calls = calls.Append(new ProgressCall("begin", token, value.Begin.Title, value.Begin.Message ?? default));
                else if (value.Report is not null)
                    calls = calls.Append(new ProgressCall("report", token, default, value.Report.Message ?? default));
                else if (value.End is not null)
                    calls = calls.Append(new ProgressCall("end", token, default, default));
            }
        }

        internal Slice<ProgressCall> GetCalls()
        {
            lock (gate)
                return GoSlices.Clone(calls);
        }
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly CancellationTokenSource Cancellation = new();
        internal readonly LanguageServicesTestClock Clock = new();
        internal readonly ProgressQuiescence Quiet = new();
        internal readonly Reporter Reporter;
        internal readonly ProjectLoadingProgress Progress;
        internal Fixture(int milliseconds)
        {
            Reporter = new Reporter(Cancellation.Token);
            Progress = ProjectLoadingProgress.NewFromReporter(Reporter, TimeSpan.FromMilliseconds(milliseconds), Clock, Quiet.OnWait);
        }

        internal void Start(string name) => Progress.Start(Diagnostics.Project_0, name);
        internal void Finish(string name) => Progress.Finish(Diagnostics.Project_0, name);
        internal void Wait()
        {
            if (Cancellation.IsCancellationRequested)
                // Go synctest quiescence is untimed; this is only a deadlock-safety bound.
                Progress.Worker.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
            else
                Quiet.Wait();
        }

        internal void Sleep(int milliseconds)
        {
            Clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
            Wait();
        }

        public void Dispose()
        {
            Cancellation.Cancel();
            Wait();
            Cancellation.Dispose();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static void Run(string name)
    {
        using var fixture = new Fixture(name == "ZeroDelay" ? 0 : name == "StartBeforeDelayThenMoreAfterDelay" ? 200 : name is "StartFinishBeforeDelay" or "ShowsAfterDelay" or "ShutdownWithActiveTimer" or "FinishBeforeDelayNoBegun" ? 500 : 100);
        switch (name)
        {
            case "StartFinishBeforeDelay":
                fixture.Start("myProject");
                fixture.Wait();
                fixture.Finish("myProject");
                fixture.Wait();
                fixture.Sleep(600);
                Check(fixture.Reporter.GetCalls().Length == 0, "expected no progress calls for fast operation");
                break;
            case "ShowsAfterDelay":
                fixture.Start("myProject");
                fixture.Wait();
                fixture.Sleep(500);
                var shown = fixture.Reporter.GetCalls();
                Check(shown.Length == 2, "expected2 calls(create+begin)");
                Check(shown[0].Method == "create", "expected create");
                Check(shown[1].Method == "begin", "expected begin");
                Check(shown[1].Title.EqualsValue(ByteStringValue.FromString(GoString.ToUtf8(Diagnostics.Loading.String()))), "expected loading title");
                fixture.Finish("myProject");
                fixture.Wait();
                Check(Last(fixture).Method == "end", "expected end");
                break;
            case "ReportsMultipleOperations":
                fixture.Start("projA");
                fixture.Start("projB");
                fixture.Wait();
                fixture.Sleep(100);
                var multiple = fixture.Reporter.GetCalls();
                Check(multiple.Length >= 2, "expected at least2 calls");
                Check(multiple[0].Method == "create", "expected create");
                Check(multiple[1].Method == "begin", "expected begin");
                fixture.Finish("projA");
                fixture.Wait();
                Check(GoSlices.ContainsFunc(fixture.Reporter.GetCalls(), static call => call.Method == "report"), "expected report after partial finish");
                fixture.Finish("projB");
                fixture.Wait();
                Check(Last(fixture).Method == "end", "expected end");
                break;
            case "RefCounting":
                fixture.Start("proj");
                fixture.Start("proj");
                fixture.Wait();
                fixture.Sleep(100);
                fixture.Finish("proj");
                fixture.Wait();
                Check(!GoSlices.ContainsFunc(fixture.Reporter.GetCalls(), static call => call.Method == "end"), "unexpected end with refcount>0");
                fixture.Finish("proj");
                fixture.Wait();
                Check(Last(fixture).Method == "end", "expected end when refcount reaches0");
                break;
            case "NewTokenAfterEnd":
                fixture.Start("proj");
                fixture.Wait();
                fixture.Sleep(100);
                var firstToken = fixture.Reporter.GetCalls()[0].Token;
                fixture.Finish("proj");
                fixture.Wait();
                fixture.Start("proj2");
                fixture.Wait();
                fixture.Sleep(100);
                ByteStringValue secondToken = default;
                foreach (var call in fixture.Reporter.GetCalls())
                    if (call.Method == "create" && !call.Token.EqualsValue(firstToken))
                    {
                        secondToken = call.Token;
                        break;
                    }

                Check(secondToken.Length != 0, "expected a new token for second cycle");
                Check(!firstToken.EqualsValue(secondToken), "expected different tokens");
                fixture.Finish("proj2");
                fixture.Wait();
                break;
            case "StartBeforeDelayThenMoreAfterDelay":
                fixture.Start("projA");
                fixture.Wait();
                fixture.Sleep(200);
                Check(fixture.Reporter.GetCalls().Length >= 2, "expected create+begin after delay");
                fixture.Start("projB");
                fixture.Wait();
                Check(Last(fixture).Method == "report", "expected report for new start after delay");
                fixture.Finish("projA");
                fixture.Finish("projB");
                fixture.Wait();
                break;
            case "FinishWithNoActiveToken":
                fixture.Finish("proj");
                fixture.Wait();
                Check(fixture.Reporter.GetCalls().Length == 0, "expected no calls for orphan finish");
                break;
            case "ShutdownDuringStartAndFinish":
                fixture.Cancellation.Cancel();
                fixture.Wait();
                for (int i = 0; i < 64; i++)
                    fixture.Progress.channel.Writer.WriteAsync(new(Diagnostics.Project_0, ["fill"], false)).AsTask().GetAwaiter().GetResult();
                fixture.Start("proj");
                fixture.Finish("proj");
                break;
            case "ShutdownWithActiveTimer":
                fixture.Start("proj");
                fixture.Wait();
                fixture.Cancellation.Cancel();
                fixture.Wait();
                break;
            case "ZeroDelay":
                fixture.Start("proj");
                fixture.Wait();
                var immediate = fixture.Reporter.GetCalls();
                Check(immediate.Length == 2, "expected2 calls(create+begin)");
                Check(immediate[0].Method == "create", "expected create");
                Check(immediate[1].Method == "begin", "expected begin");
                Check(immediate[1].Message.EqualsBytes("Project 'proj'"u8), "expected Project 'proj' message");
                fixture.Finish("proj");
                fixture.Wait();
                Check(Last(fixture).Method == "end", "expected end");
                break;
            case "FinishBeforeDelayNoBegun":
                fixture.Start("proj");
                fixture.Wait();
                fixture.Finish("proj");
                fixture.Wait();
                Check(!GoSlices.ContainsFunc(fixture.Reporter.GetCalls(), static call => call.Method == "end"), "unexpected end when begun=false");
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static ProgressCall Last(Fixture fixture)
    {
        var calls = fixture.Reporter.GetCalls();
        return calls[calls.Length - 1];
    }
}
