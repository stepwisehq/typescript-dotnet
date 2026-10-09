namespace Configuration.Checks;

internal static class Check
{
    public static void True(bool condition, string context)
    {
        if (!condition) throw new InvalidOperationException(context);
    }

    public static void Equal<T>(T actual, T expected, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new InvalidOperationException($"{context}: actual {actual}, expected {expected}");
    }

    public static void Sequence<T>(IReadOnlyList<T>? actual, IReadOnlyList<T>? expected, string context)
    {
        True((actual is null) == (expected is null), context + " nil state");
        if (actual is null) return;
        Equal(actual.Count, expected!.Count, context + " length");
        for (int i = 0; i < actual.Count; i++) Equal(actual[i], expected[i], context + $"[{i}]");
    }
}
