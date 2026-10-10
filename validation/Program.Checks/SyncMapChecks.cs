// Complete original TestSyncMapWithNil assertions; sequential scheduling adaptation.
using Configuration.Checks;
using TypeScript.Collections;

namespace Program.Checks;

public static class SyncMapChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestSyncMapWithNil"];

    public static void Run(string name)
    {
        if (name != "TestSyncMapWithNil") throw new ArgumentException("Unknown original SyncMap test.", nameof(name));
        SyncMap<string, object?> map = new();
        var (got1, ok1) = map.Load("foo");
        Check.True(!ok1, "missing key");
        Check.Equal(got1, null, "missing nil value");
        map.Store("foo", null);
        var (got2, ok2) = map.Load("foo");
        Check.True(ok2, "stored key");
        Check.Equal(got2, null, "stored nil value");
        var (too, loaded) = map.LoadOrStore("too", null);
        Check.True(!loaded, "new key LoadOrStore");
        Check.Equal(too, null, "LoadOrStore nil value");
        map.Range((_, _) => true);
    }
}
