using TypeScript.Scanner;
using Xunit;

namespace TypeScript.Scanner.BinderTests;
public class BindOnceTests
{
    private static SourceFile Empty() => Parser.ParseSourceFile(new("/empty.ts", "/empty.ts"), "", ScriptKind.TS);
    [Fact]
    public async Task BindOncePublishesAfterSingleConcurrentCompletion()
    {
        var file = Empty();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int attempts = 0;
        var first = Task.Run(() => file.BindOnce(() =>
        {
            Interlocked.Increment(ref attempts);
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("BindOnce test release timeout");
            file.SymbolCount = 123;
        }));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            Assert.False(file.IsBound());
            var competitors = Enumerable.Range(0, 6).Select(_ => Task.Run(() => file.BindOnce(() => Interlocked.Increment(ref attempts)))).ToArray();
            release.Set();
            await Task.WhenAll(competitors.Append(first));
            Assert.True(file.IsBound());
            Assert.Equal(1, attempts);
            Assert.Equal(123, file.SymbolCount);
        }
        finally
        {
            release.Set();
            await first;
        }
    }

    [Fact]
    public void FailedBindOnceDoesNotPublishOrRetry()
    {
        var file = Empty();
        int attempts = 0;
        var failure = new InvalidOperationException("deliberate bind failure");
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => file.BindOnce(() =>
        {
            attempts++;
            throw failure;
        })));
        Assert.False(file.IsBound());
        file.BindOnce(() => attempts++);
        Assert.Equal(1, attempts);
        Assert.False(file.IsBound());
    }

    [Fact]
    public async Task ConcurrentBinderPublishesOneStableSymbolGraph()
    {
        var file = Parser.ParseSourceFile(new("/module.ts", "/module.ts"), "export class C { #x=1; m(){return this.#x;} }", ScriptKind.TS);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Binder.BindSourceFile(file))));
        Assert.True(file.IsBound());
        var module = Assert.IsType<Symbol>(file.Symbol);
        var exports = Assert.IsType<SymbolTable>(module.Exports);
        var declaration = Assert.IsType<Symbol>(exports["C"]);
        var members = Assert.IsType<SymbolTable>(declaration.Members);
        Binder.BindSourceFile(file);
        Assert.Same(module, file.Symbol);
        Assert.Same(exports, module.Exports);
        Assert.Same(declaration, exports["C"]);
        Assert.Same(members, declaration.Members);
        Assert.True(file.BindDiagnostics().IsNil || file.BindDiagnostics().Count == 0);
    }

    [Fact]
    public void DefaultFileNameBindsAsQuotedEmptyModuleName()
    {
        var file = Parser.ParseSourceFile(default(SourceFileParseOptions), "export const x=1;", ScriptKind.TS);
        Binder.BindSourceFile(file);
        Assert.True(file.IsBound());
        Assert.Equal("\"\"", file.Symbol.Name);
        Assert.NotNull(file.Symbol.Exports["x"]);
    }
}
