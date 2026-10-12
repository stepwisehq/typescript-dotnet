// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Full original TestSyncMapProxyFor: tsc/internal/project/dirty/syncmap_test.go.
using TypeScript.ProjectSystem.Dirty;

namespace LanguageServices.Checks;
public static class ProjectDirtyChecks
{
    public static readonly string[] Names = ["TestSyncMapProxyFor"];
    public static int RunAll()
    {
        Run(Names[0]);
        return 1;
    }

    public static void Run(string name)
    {
        if (name != Names[0])
            throw new ArgumentException(name);
        ProxyRace();
        ProxyDelegation();
        ProxyDelete();
        NoProxy();
    }

    // Authored adapter invariant: optional comparer survives every copied owner.
    public static int RunAuthoredControls()
    {
        var source = new TypeScript.Collections.SyncMap<string, int>(StringComparer.OrdinalIgnoreCase);
        source.Store("Alpha", 1);
        var clone = source.Clone();
        var map = source.ToMap();
        Check(clone.Load("ALPHA") == (1, true));
        Check(map["ALPHA"] == 1);
        clone.Store("alpha", 2);
        map["alpha"] = 3;
        Check(clone.Size() == 1);
        Check(map.Count == 1);
        Check(source.Load("Alpha") == (1, true));
        return 1;
    }

    private sealed class TestValue(string data) : ICloneable<TestValue>
    {
        internal string Data = data;
        public TestValue Clone() => new(Data);
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException("original dirty assertion failed");
    }

    private static SyncMap<string, TestValue> Create(string key, string data) => new(new() { [key] = new(data) });
    private static void ProxyRace()
    {
        var map = Create("key1", "original");
        SyncMapEntry<string, TestValue>? first = null, second = null;
        bool firstOk = false, secondOk = false;
        var a = new Thread(() => (first, firstOk) = map.Load("key1"));
        var b = new Thread(() => (second, secondOk) = map.Load("key1"));
        a.Start();
        b.Start();
        a.Join();
        b.Join();
        Check(firstOk);
        Check(secondOk);
        Check(first!.Value().Data == "original");
        Check(second!.Value().Data == "original");
        Check(!first.Dirty());
        Check(!second.Dirty());
        a = new Thread(() => first.Change(static v => v.Data = "changed_by_entry1"));
        b = new Thread(() => second.Change(static v => v.Data = "changed_by_entry2"));
        a.Start();
        b.Start();
        a.Join();
        b.Join();
        Check(first.Value().Data == second.Value().Data);
        Check(first.Dirty());
        Check(second.Dirty());
        Check(first.proxyFor is not null || second.proxyFor is not null);
        if (first.proxyFor is not null)
            Check(ReferenceEquals(second, first.proxyFor));
        if (second.proxyFor is not null)
            Check(ReferenceEquals(first, second.proxyFor));
    }

    private static void ProxyDelegation()
    {
        var map = Create("key1", "original");
        var(first, ok1) = map.Load("key1");
        var(second, ok2) = map.Load("key1");
        Check(ok1);
        Check(ok2);
        first!.Change(static v => v.Data = "changed_by_entry1");
        second!.Change(static v => v.Data = "changed_by_entry2");
        var proxy = first.proxyFor is not null ? first : second;
        var target = first.proxyFor is not null ? second : first;
        proxy.Change(static v => v.Data = "changed_through_proxy");
        Check(target.Value().Data == "changed_through_proxy");
        Check(proxy.Value().Data == "changed_through_proxy");
        bool changed = proxy.ChangeIf(static v => v.Data == "changed_through_proxy", static v => v.Data = "conditional_change");
        Check(changed);
        Check(target.Value().Data == "conditional_change");
        Check(proxy.Value().Data == "conditional_change");
        Check(target.Dirty() == proxy.Dirty());
        proxy.Locked(static v => v.Change(static value => value.Data = "locked_change"));
        Check(target.Value().Data == "locked_change");
        Check(proxy.Value().Data == "locked_change");
    }

    private static void ProxyDelete()
    {
        var map = Create("key1", "original");
        var(first, _) = map.Load("key1");
        var(second, _) = map.Load("key1");
        first!.Change(static v => v.Data = "modified");
        second!.Change(static v => v.Data = "modified2");
        var proxy = first.proxyFor is not null ? first : second;
        proxy.Delete();
        Check(!map.Load("key1").Ok);
        var map2 = Create("key2", "test");
        var(third, _) = map2.Load("key2");
        var(fourth, _) = map2.Load("key2");
        third!.Change(static v => v.Data = "modified");
        fourth!.Change(static v => v.Data = "modified2");
        var proxy2 = third.proxyFor is not null ? third : fourth;
        proxy2.DeleteIf(static v => v.Data is "modified2" or "modified");
        Check(!map2.Load("key2").Ok);
    }

    private static void NoProxy()
    {
        var map = Create("key1", "original");
        var(entry, ok) = map.Load("key1");
        Check(ok);
        entry!.Change(static v => v.Data = "changed");
        Check(entry.proxyFor is null);
        Check(entry.Dirty());
        Check(entry.Value().Data == "changed");
    }
}
