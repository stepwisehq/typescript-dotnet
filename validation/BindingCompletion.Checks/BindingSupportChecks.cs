// Authored binding API observations; deterministic expected values come from pinned original Go.
// Seeded table/flow controls test public model fidelity separately from production graph coverage.
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeScript.Scanner;

namespace TypeScript.Binding;
public static class BindingSupportChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["Tables", "Symbols", "AtomicIds", "SyntheticFlows", "BindOnceSuccess", "BindOnceFailure", "CloneBindingState", "FileIsolation"];

    private static SourceFile Parse(string text) => Parser.ParseSourceFile(new("/same.ts", "/same.ts"), text, ScriptKind.TS);
    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private static bool Same(object? a, object? b) => ReferenceEquals(a, b);
    private static string Bytes(string text) => Convert.ToBase64String(Encoding.Latin1.GetBytes(text));
    private static JsonNode? Json(object? value) => value switch
    {
        null => null,
        bool v => JsonValue.Create(v),
        string v => JsonValue.Create(v),
        int v => JsonValue.Create(v),
        uint v => JsonValue.Create(v),
        object[] values => new JsonArray(values.Select(Json).ToArray()),
        Dictionary<string, object?> values => new JsonObject(values.Select(v => KeyValuePair.Create(v.Key, Json(v.Value)))),
        _ => throw new InvalidOperationException("Unencoded binding observation: " + value.GetType().Name)};
    private static Dictionary<string, object?> Map(params (string Name, object? Value)[] values) => values.ToDictionary(v => v.Name, v => v.Value);
    public static void Run(string name)
    {
        object actual = name switch
        {
            "Tables" => Tables(),
            "Symbols" => Symbols(),
            "AtomicIds" => AtomicIds(),
            "SyntheticFlows" => SyntheticFlows(),
            "BindOnceSuccess" => BindOnceSuccess(),
            "BindOnceFailure" => BindOnceFailure(),
            "CloneBindingState" => CloneBindingState(),
            "FileIsolation" => FileIsolation(),
            _ => throw new ArgumentException("Unknown binding support case", nameof(name))};
        using var stream = typeof(BindingSupportChecks).Assembly.GetManifestResourceStream("TypeScript.Binding.Support.json") ?? throw new InvalidOperationException("Missing independent Go binding support observations");
        using var expected = JsonDocument.Parse(stream);
        using var observed = JsonDocument.Parse(Json(actual)!.ToJsonString());
        var original = expected.RootElement.GetProperty(name);
        if (!JsonElement.DeepEquals(original, observed.RootElement))
            throw new InvalidOperationException(name + ": original=" + original.GetRawText() + ", actual=" + observed.RootElement.GetRawText());
    }

    private static object Tables()
    {
        var symbol = new Symbol();
        var file = Parse("");
        bool initialNil = symbol.Members is null && symbol.Exports is null && file.Locals is null;
        var members = Ast.GetMembers(symbol);
        var exports = Ast.GetExports(symbol);
        var locals = Ast.GetLocals(file.AsNode());
        bool empty = members.Count == 0 && exports.Count == 0 && locals.Count == 0;
        bool repeat = Same(members, Ast.GetMembers(symbol)) && Same(exports, Ast.GetExports(symbol)) && Same(locals, Ast.GetLocals(file.AsNode()));
        bool separate = !Same(members, exports) && !Same(members, locals) && !Same(exports, locals);
        bool absent = members["absent"] is null;
        var entry = new Symbol
        {
            Name = "x",
            Flags = SymbolFlags.Property
        };
        members["x"] = entry;
        var other = new Symbol
        {
            Members = members,
            Exports = exports
        };
        bool seededShared = Same(Ast.GetMembers(other), members) && Same(Ast.GetExports(other), exports) && Same(other.Members["x"], entry);
        var missingClass = Parse("class {}");
        Binder.BindSourceFile(missingClass);
        var emptyType = Parse("type T = {};");
        Binder.BindSourceFile(emptyType);
        return Map(("initialNil", initialNil), ("empty", empty), ("repeat", repeat), ("separate", separate), ("absent", absent), ("seededShared", seededShared), ("membersCount", members.Count), ("naturalEmptyLocals", missingClass.Locals is { Count: 0 }), ("emptyTypeMembersNil", emptyType.Statements.Nodes[0].Type().Symbol().Members is null));
    }

    private static object Symbols()
    {
        var file = Parse("export class C { #p=1; static s=2; } export var x=1; export var x=2;");
        Binder.BindSourceFile(file);
        var module = file.Symbol;
        var c = module.Exports["C"];
        var x = module.Exports["x"];
        var privateSymbol = c.Members.Values.Single(s => s.ValueDeclaration?.Name()?.Kind == Kind.PrivateIdentifier);
        var prototype = c.Exports["prototype"];
        var xLocal = file.Locals["x"];
        var transient = new Symbol
        {
            Flags = SymbolFlags.Transient,
            Declarations = x.Declarations
        };
        var sourceOwners = new[]
        {
            module,
            c,
            x,
            privateSymbol,
            prototype,
            xLocal
        }.Select(s => (object)Same(Ast.GetSourceFileOfSymbol(s), file)).ToArray();
        string[] names = ["x", "__x", Ast.InternalSymbolNameType, "a" + Ast.InternalSymbolNamePrefix + "b", "___x"];
        var escaped = names.Select(n => (object)new object[] { Bytes(n), Bytes(Ast.EscapeAllInternalSymbolNames(n)), Bytes(Ast.EscapeInternalSymbolName(n)), Bytes(Ast.EscapeSymbolName(n)) }).ToArray();
        return Map(("owners", sourceOwners), ("transientOwnerNull", Ast.GetSourceFileOfSymbol(transient)is null), ("moduleExternal", module.IsExternalModule()), ("classExternal", c.IsExternalModule()), ("static", new object[] { c.Exports["s"].IsStatic(), privateSymbol.IsStatic(), prototype.IsStatic() }), ("privateDisplay", Bytes(Ast.SymbolName(privateSymbol))), ("privateRawDiffers", privateSymbol.Name != Ast.SymbolName(privateSymbol)), ("xDeclarationCount", x.Declarations.Count), ("xLocalExport", Same(xLocal.ExportSymbol, x)), ("combined", (uint)xLocal.CombinedLocalAndExportSymbolFlags()), ("escaped", escaped), ("checkFlagsZero", new[] { module, c, x, privateSymbol, prototype, xLocal }.All(s => s.CheckFlags == 0)));
    }

    private static object AtomicIds()
    {
        var a = new NodeFactory().NewIdentifier("a");
        var b = new NodeFactory().NewIdentifier("b");
        var s = new Symbol();
        var t = new Symbol();
        var nodeIds = new ulong[32];
        var symbolIds = new ulong[32];
        Parallel.For(0, nodeIds.Length, i =>
        {
            nodeIds[i] = Ast.GetNodeId(a);
            symbolIds[i] = Ast.GetSymbolId(s);
        });
        return Map(("nodeStable", nodeIds.All(id => id != 0 && id == nodeIds[0])), ("symbolStable", symbolIds.All(id => id != 0 && id == symbolIds[0])), ("nodeDistinct", Ast.GetNodeId(b) != nodeIds[0]), ("symbolDistinct", Ast.GetSymbolId(t) != symbolIds[0]));
    }

    private static object SyntheticFlows()
    {
        var node = Parse("switch(x){case 1:break;}").Statements.Nodes[0];
        var range = Ast.NewFlowSwitchClauseData(node, 1, 3);
        var empty = Ast.NewFlowSwitchClauseData(node, 2, 2);
        var target = new FlowNode
        {
            Flags = FlowFlags.BranchLabel
        };
        var head = new FlowNode
        {
            Flags = FlowFlags.Start
        };
        var tail = new FlowList
        {
            Flow = head
        };
        var list = new FlowList
        {
            Flow = target,
            Next = tail
        };
        var reduce = Ast.NewFlowReduceLabelData(target, list);
        return Map(("switch", new object[] { (int)range.Kind, range.Pos(), range.End(), (uint)range.Flags, Same(range.AsFlowSwitchClauseData().SwitchStatement, node), range.AsFlowSwitchClauseData().ClauseStart, range.AsFlowSwitchClauseData().ClauseEnd, range.AsFlowSwitchClauseData().IsEmpty(), empty.AsFlowSwitchClauseData().IsEmpty() }), ("reduce", new object[] { (int)reduce.Kind, reduce.Pos(), reduce.End(), (uint)reduce.Flags, Same(reduce.AsFlowReduceLabelData().Target, target), Same(reduce.AsFlowReduceLabelData().Antecedents, list), Same(reduce.AsFlowReduceLabelData().Antecedents.Next, tail), Same(tail.Flow, head) }));
    }

    private static object BindOnceSuccess()
    {
        var file = Parse("");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var started = new CountdownEvent(6);
        using var returned = new ManualResetEventSlim();
        int attempts = 0;
        var callerStates = new bool[6];
        var first = Task.Run(() => file.BindOnce(() =>
        {
            Interlocked.Increment(ref attempts);
            entered.Set();
            Require(release.Wait(TimeSpan.FromSeconds(10)), "Success callback release timeout");
            file.SymbolCount = 123;
            file.GlobalExports = new SymbolTable();
        }));
        try
        {
            Require(entered.Wait(TimeSpan.FromSeconds(10)), "Success callback entry timeout");
            bool unpublished = !file.IsBound();
            var competitors = Enumerable.Range(0, 6).Select(i => Task.Factory.StartNew(() =>
            {
                started.Signal();
                file.BindOnce(() => Interlocked.Increment(ref attempts));
                callerStates[i] = file.IsBound() && file.SymbolCount == 123 && file.GlobalExports is { Count: 0 };
                returned.Set();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
            Require(started.Wait(TimeSpan.FromSeconds(10)), "BindOnce competitors did not start");
            bool waitersHeld = !returned.Wait(TimeSpan.FromMilliseconds(100));
            release.Set();
            Require(Task.WaitAll(competitors.Append(first).ToArray(), TimeSpan.FromSeconds(10)), "BindOnce callers did not finish after release");
            var table = file.GlobalExports;
            file.BindOnce(() => attempts++);
            return Map(("unpublished", unpublished), ("waitersHeld", waitersHeld), ("callerStates", callerStates.Select(v => (object)v).ToArray()), ("bound", file.IsBound()), ("attempts", attempts), ("symbolCount", file.SymbolCount), ("tablePublished", table is { Count: 0 }), ("tableStable", Same(table, file.GlobalExports)));
        }
        finally
        {
            release.Set();
            Require(first.Wait(TimeSpan.FromSeconds(10)), "BindOnce first callback did not finish after release");
            first.GetAwaiter().GetResult();
        }
    }

    private static object BindOnceFailure()
    {
        var file = Parse("");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var started = new CountdownEvent(6);
        using var returned = new ManualResetEventSlim();
        int callbackCompleted = 0;
        int attempts = 0;
        var callerStates = new bool[6];
        var failure = new InvalidOperationException("deliberate bind failure");
        var first = Task.Run(() =>
        {
            try
            {
                file.BindOnce(() =>
                {
                    try
                    {
                        Interlocked.Increment(ref attempts);
                        file.SymbolCount = 71;
                        file.GlobalExports = new SymbolTable();
                        entered.Set();
                        Require(release.Wait(TimeSpan.FromSeconds(10)), "Failure callback release timeout");
                        throw failure;
                    }
                    finally
                    {
                        Volatile.Write(ref callbackCompleted, 1);
                    }
                });
                throw new InvalidOperationException("Missing callback failure");
            }
            catch (InvalidOperationException error)when (ReferenceEquals(error, failure))
            {
            }
        });
        try
        {
            Require(entered.Wait(TimeSpan.FromSeconds(10)), "Failure callback entry timeout");
            bool unpublished = !file.IsBound();
            var competitors = Enumerable.Range(0, 6).Select(i => Task.Factory.StartNew(() =>
            {
                started.Signal();
                file.BindOnce(() => Interlocked.Increment(ref attempts));
                callerStates[i] = Volatile.Read(ref callbackCompleted) == 1 && !file.IsBound() && file.SymbolCount == 71 && file.GlobalExports is { Count: 0 };
                returned.Set();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
            Require(started.Wait(TimeSpan.FromSeconds(10)), "BindOnce competitors did not start");
            bool waitersHeld = !returned.Wait(TimeSpan.FromMilliseconds(100));
            release.Set();
            Require(Task.WaitAll(competitors.Append(first).ToArray(), TimeSpan.FromSeconds(10)), "BindOnce callers did not finish after release");
            var partial = file.GlobalExports;
            file.BindOnce(() => attempts++);
            Binder.BindSourceFile(file);
            return Map(("unpublished", unpublished), ("waitersHeld", waitersHeld), ("callerStates", callerStates.Select(v => (object)v).ToArray()), ("bound", file.IsBound()), ("attempts", attempts), ("symbolCount", file.SymbolCount), ("partialTable", partial is { Count: 0 }), ("partialStable", Same(partial, file.GlobalExports)));
        }
        finally
        {
            release.Set();
            Require(first.Wait(TimeSpan.FromSeconds(10)), "BindOnce first callback did not finish after release");
            first.GetAwaiter().GetResult();
        }
    }

    private static object FreshState(SourceFile file) => new object[]
    {
        file.IsBound(),
        file.Symbol is null,
        file.Locals is null,
        file.NextContainer is null,
        file.BindDiagnostics().IsNil,
        file.SymbolCount,
        file.PatternAmbientModules.IsNil,
        file.GlobalExports is null
    };
    private static object CloneBindingState()
    {
        var file = Parse("export class C {} export let x=1; export let x=2;");
        Binder.BindSourceFile(file);
        var factory = new NodeFactory();
        var clone = file.AsNode().Clone(factory).AsSourceFile();
        var unchanged = factory.UpdateSourceFile(file, file.Statements, file.EndOfFileToken);
        var changed = factory.UpdateSourceFile(file, factory.NewNodeList(file.Statements.Nodes), file.EndOfFileToken).AsSourceFile();
        var declaration = file.Statements.Nodes[0];
        var declClone = declaration.Clone(factory);
        return Map(("originalBound", file.IsBound()), ("originalDiagnostics", file.BindDiagnostics()is { Count: > 0 }), ("cloneState", FreshState(clone)), ("changedState", FreshState(changed)), ("unchangedIdentity", Same(unchanged, file.AsNode())), ("cloneStatementsShared", Same(clone.Statements, file.Statements)), ("cloneModuleIndicatorShared", Same(clone.ExternalModuleIndicator, file.ExternalModuleIndicator)), ("cloneFlags", (uint)clone.Flags), ("flagsEqual", clone.Flags == file.Flags), ("declaration", new object[] { !Same(declClone, declaration), declClone.Symbol()is null, declClone.LocalSymbol()is null, declClone.Locals()is null, Same(declClone.Name(), declaration.Name()), declClone.Flags == declaration.Flags, declClone.Pos() == declaration.Pos(), declClone.End() == declaration.End() }));
    }

    private static object FileIsolation()
    {
        const string text = "export class C { #p=1; } export let x=1;";
        var a = Parse(text);
        var b = Parse(text);
        Binder.BindSourceFile(a);
        Binder.BindSourceFile(b);
        var aSymbol = a.Symbol;
        var aLocals = a.Locals;
        var aExports = a.Symbol.Exports;
        var aClass = aExports["C"];
        Binder.BindSourceFile(a);
        return Map(("different", new object[] { !Same(a, b), !Same(a.Symbol, b.Symbol), !Same(a.Locals, b.Locals), !Same(a.Symbol.Exports, b.Symbol.Exports), !Same(aClass, b.Symbol.Exports["C"]), !Same(aClass.Members, b.Symbol.Exports["C"].Members) }), ("repeat", new object[] { Same(aSymbol, a.Symbol), Same(aLocals, a.Locals), Same(aExports, a.Symbol.Exports), Same(aClass, a.Symbol.Exports["C"]) }), ("owners", new object[] { Same(Ast.GetSourceFileOfSymbol(aClass), a), Same(Ast.GetSourceFileOfSymbol(b.Symbol.Exports["C"]), b) }), ("countsEqual", a.SymbolCount == b.SymbolCount));
    }
}
