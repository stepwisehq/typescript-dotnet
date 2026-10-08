using TypeScript.Scanner;

namespace TypeScript.Scanner.BinderValidation;
public static class SelfTests
{
    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static string[] Run()
    {
        var file = Parser.ParseSourceFile(default(SourceFileParseOptions), "", ScriptKind.TS);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int attempts = 0;
        var first = Task.Run(() => file.BindOnce(() =>
        {
            Interlocked.Increment(ref attempts);
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("BindOnce release timeout");
            file.SymbolCount = 123;
        }));
        try
        {
            Require(entered.Wait(TimeSpan.FromSeconds(5)), "BindOnce callback did not enter");
            Require(!file.IsBound(), "Binding published before callback completion");
            var competitors = Enumerable.Range(0, 6).Select(_ => Task.Run(() => file.BindOnce(() => Interlocked.Increment(ref attempts)))).ToArray();
            release.Set();
            Task.WaitAll(competitors.Append(first).ToArray());
            Require(file.IsBound() && attempts == 1 && file.SymbolCount == 123, "Concurrent BindOnce publication differs");
        }
        finally
        {
            release.Set();
            first.GetAwaiter().GetResult();
        }

        var failed = Parser.ParseSourceFile(default(SourceFileParseOptions), "", ScriptKind.TS);
        int failures = 0;
        try
        {
            failed.BindOnce(() =>
            {
                failures++;
                throw new InvalidOperationException("deliberate failure");
            });
            throw new Exception("Expected callback failure");
        }
        catch (InvalidOperationException error)when (error.Message == "deliberate failure")
        {
        }

        failed.BindOnce(() => failures++);
        Require(!failed.IsBound() && failures == 1, "Failed BindOnce retried or published");
        var module = Parser.ParseSourceFile(default(SourceFileParseOptions), "export const x=1;", ScriptKind.TS);
        Task.WaitAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Binder.BindSourceFile(module))).ToArray());
        Require(module.IsBound() && module.Symbol.Name == "\"\"" && module.Symbol.Exports["x"] is not null, "Default options or concurrent Binder differs");
        var symbol = module.Symbol;
        var exports = symbol.Exports;
        var declaration = exports["x"];
        Binder.BindSourceFile(module);
        Require(ReferenceEquals(symbol, module.Symbol) && ReferenceEquals(exports, symbol.Exports) && ReferenceEquals(declaration, exports["x"]), "Repeated Binder replaced identities");
        return["concurrent-bind-once-publication", "failed-bind-once-no-retry", "default-options-module-name", "concurrent-binder-idempotent-identities"];
    }
}
